/***************************************************************************
Copyright 2004-2005,2008, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

Written by Maarten JB van Ettinger.

****************************************************************************/
using System;
using System.IO;
using System.Runtime.InteropServices;

using Communication.IO.Tools;

using ECGConversion;
using ECGConversion.ECGDemographics;
using ECGConversion.ECGDiagnostic;
using ECGConversion.ECGGlobalMeasurements;
using ECGConversion.ECGSignals;

namespace ECGConversion.SCP
{
	/// <summary>
	/// Class containing the entire SCP format.
	/// </summary>
	public class SCPFormat : IECGFormat, ISignal
	{
		// settings for support with other readers.
		private bool _QRSSubtractionSupport
		{
			get
			{
				return _Config["QRS Subtraction"] == null || string.Compare(_Config["QRS Subtraction"], "true", true) == 0;
			}
		}

		private bool _BimodalCompressionUsed
		{
			get
			{
				try
				{
					string temp1 = _Config["Bimodal Comppression Rate"];

					if (temp1 == null)
						return false;

					int temp2 = int.Parse(temp1);

					switch (temp2)
					{
						case 2:case 4:
							return true;
						default:
							break;
					}
				}
				catch {}

				return false;
			}
		}

		private int _BimodalCompressionRate
		{
			get
			{
				try
				{
					return int.Parse(_Config["Bimodal Comppression Rate"]);
				}
				catch {}

				return 4;
			}
		}

		private EncodingType _EncodingType
		{
			get
			{
				try
				{
					return (EncodingType) Enum.Parse(typeof(EncodingType), _Config["Compression Type"], true);
				}
				catch {}

				return EncodingType.DefaultHuffman;
			}
		}

		private byte _DifferenceDataSection5Used
		{
			get
			{
				try
				{
					int temp = int.Parse(_Config["Difference Data Median"]);

					if ((temp >= 0)
					&&	(temp <= 2))
						return (byte) temp;
				}
				catch {}

				return 2;
			}
		}

		private byte _DifferenceDataSection6Used
		{
			get
			{
				try
				{
					int temp = int.Parse(_Config["Difference Data Rhythm"]);

					if ((temp >= 0)
					&&	(temp <= 2))
						return (byte) temp;
				}
				catch {}

				return 2;
			}
		}

		private bool _UseLeadMeasurements
		{
			get
			{
				return string.Compare(_Config["Use Lead Measurements"], "true", true) == 0;
			}
		}

		// Static settings of format.
		public static byte DefaultSectionVersion = 20;
		public static byte DefaultProtocolVersion = 20;
		private static int _MinFileLength = 158;
		private static int _MinNrSections = 12;
		private static int _MinNrWorkingSections = 2;
		// data structure of format.
		private ushort _CRC = 0;
		private int _Length;
		private SCPSection[] _Default = {	new SCPSection0(),
											new SCPSection1(),
											new SCPSection2(),
											new SCPSection3(),
											new SCPSection4(),
											new SCPSection5(),
											new SCPSection6(),
											new SCPSection7(),
											new SCPSection8(),
											new SCPSectionUnknown(),
											new SCPSection10(),
											new SCPSection11()};
		private SCPSection[] _Manufactor = null;

		public SCPFormat()
		{
			_Config = new ECGConfig(new string[]{"Compression Type", "Difference Data Median", "Difference Data Rhythm", "QRS Subtraction", "Bimodal Comppression Rate", "Use Lead Measurements"}, 3, new ECGConfig.CheckConfigFunction(this._ConfigurationWorks));

			_Config["Compression Type"] = EncodingType.DefaultHuffman.ToString();
			_Config["Difference Data Median"] = "2";
			_Config["Difference Data Rhythm"] = "2";
			_Config["QRS Subtraction"] = "false";
			_Config["Use Lead Measurements"] = "true";
		}

		public bool _ConfigurationWorks()
		{
			try
			{
				Enum.Parse(typeof(EncodingType), _Config["Compression Type"], true);
				int ddm = int.Parse(_Config["Difference Data Median"]),
					ddr = int.Parse(_Config["Difference Data Rhythm"]);

				if ((ddm >= 0)
				&&	(ddm <= 2)
				&&	(ddr >= 0)
				&&	(ddr <= 2)
				&&	((_Config["Use Lead Measurements"] == null)
				||	 (string.Compare(_Config["Use Lead Measurements"], "true", true) == 0)
				||	 (string.Compare(_Config["Use Lead Measurements"], "false", true) == 0)))
				{
					string temp1 = _Config["Bimodal Comppression Rate"];

					if (temp1 == null)
						return true;

					int temp2 = int.Parse(temp1);

					switch (temp2)
					{
						case 1:case 2:case 4:
							return true;
						default:
							break;
					}

				}
			}
			catch {}

			return false;
		}

		#region IECGFormat Members
		public override int Read(Stream input, int offset)
		{
			if ((input != null)
			&&  (input.CanRead))
			{
				// Reading information from stream to byte array.
				input.Seek(offset, SeekOrigin.Begin);
				byte[] buffer1 = new byte[Marshal.SizeOf(_CRC) + Marshal.SizeOf(_Length)];
				BytesTool.readStream(input, buffer1, 0, buffer1.Length);
				int length = (int) BytesTool.readBytes(buffer1, Marshal.SizeOf(_CRC), Marshal.SizeOf(_Length), true);
				if ((offset + length) <= input.Length)
				{
					byte[] buffer2 = new byte[length];
					BytesTool.readStream(input, buffer2, buffer1.Length, buffer2.Length - buffer1.Length);
					BytesTool.copy(buffer2, 0, buffer1, 0, buffer1.Length);
					// Read using function for byte array.
					return Read(buffer2, 0) << 1;
				}
			}
			return 0x1;
		}
		public override int Read(string file, int offset)
		{
			if (file != null)
			{
				// Opening stream to file.
				Stream read = new FileStream(file, FileMode.Open);
				// Read using function for stream.
				int err = Read(read, offset);
				// Close stream to file.
				read.Close();
				return err << 1;
			}
			return 0x1;
		}
		public override int Read(byte[] buffer, int offset)
		{
			// Read in pointers (section0)
			int err = _Default[0].Read(buffer, offset + Marshal.SizeOf(_CRC) + Marshal.SizeOf(_Length), 0);
			if ((err != 0)
			||  !(_Default[0] is SCPSection0))
			{
				return 0x1;
			}
			SCPSection0 pointers = (SCPSection0) _Default[0];
			ushort nrleads = 0;

			// set extra space for extra sections.
			if (pointers.getNrPointers() > _MinNrSections)
			{
				_Manufactor = new SCPSection[pointers.getNrPointers() - _MinNrSections];
			}

			System.Text.Encoding usedEncoding = null;

			// read in all section but pointers (section0).
			for (int loper=1;loper < pointers.getNrPointers();loper++)
			{				

				// Special case for SCP Section 5 and 6 (they need to know the nr of leads used).
				if ((loper < _MinNrSections)
				&&	(_Default[loper] is SCPSection5))
				{
					((SCPSection5)_Default[loper]).setNrLeads(nrleads);
				}
				else if ((loper < _MinNrSections)
					&&	 (_Default[loper] is SCPSection6))
				{
					((SCPSection6)_Default[loper]).setNrLeads(nrleads);
				}
				// Section works if length if greater then size of section header.
				if (pointers.getLength(loper) > 16)
				{
					if (loper < _MinNrSections)
					{
						int ret = _Default[loper].Read(buffer, offset + pointers.getIndex(loper) - 1, pointers.getLength(loper));
						if (ret != 0)
						{
							err |= (0x2 << loper);
						}

						if (usedEncoding != null)
							_Default[loper].SetEncoding(usedEncoding);
					}
					else
					{
						_Manufactor[loper - _MinNrSections] = new SCPSectionUnknown();
						
						int ret = _Manufactor[loper - _MinNrSections].Read(buffer, offset + pointers.getIndex(loper) - 1, pointers.getLength(loper));
						if (ret != 0)
						{
							err |= (0x2 << loper);
						}

						if (usedEncoding != null)
							_Manufactor[loper - _MinNrSections].SetEncoding(usedEncoding);
					}
				}

				if ((loper < _MinNrSections)
				&&	(_Default[loper] is SCPSection1))
				{
					usedEncoding = ((SCPSection1)_Default[loper]).getLanguageSupportCode();

					_Default[0].SetEncoding(usedEncoding);
					_Default[1].SetEncoding(usedEncoding);
				}
				else if ((loper < _MinNrSections)
					&&   (_Default[loper] is SCPSection3))
				{
					nrleads = ((SCPSection3)_Default[loper]).getNrLeads();
				}
			}
			return err;
		}
		public override int Write(string file)
		{
			if (file != null)
			{
				// open stream to write to.
				Stream output = new FileStream(file, FileMode.Create);
				// use write function for streams.
				int ret = Write(output);
				// close stream after writing.
				output.Close();
				return ret << 1;
			}
			return 0x1;
		}
		public override int Write(Stream output)
		{
			if ((output != null)
			&&  (output.CanWrite))
			{
				// set pointers
				setPointers();
				byte[] buffer = new byte[getFileSize()];
				// use write function for byte arrays.
				int err = Write(buffer, 0);
				if (err == 0)
				{
					output.Write(buffer, 0, buffer.Length);
				}
				return err << 1;
			}
			return 0x1;
		}
		public override int Write(byte[] buffer, int offset)
		{
			// Check if format works.
			if (Works())
			{
				_Length = getFileSize();
				if ((buffer != null)
				&&  ((offset + _Length) <= buffer.Length)
				&&  (_Default[0] is SCPSection0))
				{
					// Write length of file.
					BytesTool.writeBytes(_Length, buffer, offset + Marshal.SizeOf(_CRC), Marshal.SizeOf(_Length), true);
					SCPSection0 pointers = (SCPSection0) _Default[0];
					// Write all sections in format.
					for (int loper=0;loper < pointers.getNrPointers();loper++)
					{
						if (loper < _MinNrSections)
						{
							_Default[loper].Write(buffer, offset + pointers.getIndex(loper) - 1);
						}
						else if ((pointers.getLength(loper) > SCPSection.Size)
							&&	 (_Manufactor[loper - _MinNrSections] != null))
						{
							_Manufactor[loper - _MinNrSections].Write(buffer, offset + pointers.getIndex(loper) - 1);
						}
					}
					// Calculate CRC of byte array.
					CRCTool crctool = new CRCTool();
					crctool.Init(CRCTool.CRCCode.CRC_CCITT);
					_CRC = crctool.CalcCRCITT(buffer, offset + Marshal.SizeOf(_CRC), _Length - Marshal.SizeOf(_CRC));
					BytesTool.writeBytes(_CRC, buffer, offset, Marshal.SizeOf(_CRC), true);
					return 0x0;
				}
				return 0x2;
			}
			return 0x1;
		}
		public override bool CheckFormat(Stream input, int offset)
		{
			if ((input != null)
			&&  input.CanRead)
			{
				byte[] buffer1 = new byte[Marshal.SizeOf(_CRC) + Marshal.SizeOf(_Length)];
				input.Seek(offset, SeekOrigin.Begin);
				if (BytesTool.readStream(input, buffer1, 0, buffer1.Length) == buffer1.Length)
				{
					ushort crc = (ushort) BytesTool.readBytes(buffer1, 0, Marshal.SizeOf(_CRC), true);
					int length = (int) BytesTool.readBytes(buffer1, Marshal.SizeOf(_CRC), Marshal.SizeOf(_Length), true);
					if ((offset + length) <= input.Length
					&&  (length >= _MinFileLength))
					{
						byte[] buffer2 = new byte[length];
						BytesTool.copy(buffer2, 0, buffer1, 0, buffer1.Length);
						BytesTool.readStream(input, buffer2, buffer1.Length, length - buffer1.Length);
						buffer1 = null;
						return CheckSCP(buffer2, 0, crc, length);
					}
				}
			}
			return false;
		}
		public override bool CheckFormat(string file, int offset)
		{
			if (file != null)
			{
				Stream read = new FileStream(file, FileMode.Open);
				bool ret = CheckFormat(read, offset);
				read.Close();
				return ret;
			}
			return false;
		}
		public override bool CheckFormat(byte[] buffer, int offset)
		{
			ushort crc = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_CRC), true);
			int length = (int) BytesTool.readBytes(buffer, offset + Marshal.SizeOf(_CRC), Marshal.SizeOf(_Length), true);
			if (((offset + length) < buffer.Length)
			&&	(length < _MinFileLength))
			{
				return false;
			}
			return CheckSCP(buffer, offset, crc, length);
		}
		public override void Anonymous(byte type)
		{
			if ((_Default[1] != null)
			&&  (_Default[1].Works())
			&&  (_Default[1] is SCPSection1))
			{
				((SCPSection1)_Default[1]).Anonymous(type);
			}
		}
		public override int getFileSize()
		{
			if (Works()
			&&  (_Default[0] is SCPSection0))
			{
				SCPSection0 pointers = (SCPSection0) _Default[0];
				int sum = Marshal.SizeOf(_CRC) + Marshal.SizeOf(_Length);
				for (int loper=0;loper < pointers.getNrPointers();loper++)
				{
					sum += pointers.getLength(loper);
				}
				return sum;
			}
			return 0;
		}
		public override IDemographic Demographics
		{
			get
			{
				if (_Default[1] is SCPSection1)
					return (SCPSection1)_Default[1];

				return null;
			}
		}
		public override IDiagnostic Diagnostics
		{
			get
			{
				if (_Default[8] is SCPSection8)
					return (SCPSection8) _Default[8];

				return null;
			}
		}
		public override IGlobalMeasurement GlobalMeasurements
		{
			get
			{
				if (_Default[7] is SCPSection7)
					return (SCPSection7) _Default[7];

				return null;
			}
		}
		public override ISignal Signals
		{
			get
			{
				return this;
			}
		}

		public override ECGConversion.ECGLeadMeasurements.ILeadMeasurement LeadMeasurements
		{
			get
			{
				if (_UseLeadMeasurements
				&&	(_Default[10] is SCPSection10))
					return (SCPSection10) _Default[10];

				return null;
			}
		}


		public override bool Works()
		{
			if ((_Default.Length == _MinNrSections)
			&&  (_Default[0] is SCPSection0)
			&&  (_MinNrSections + (_Manufactor != null ? _Manufactor.Length : 0)) == ((SCPSection0)_Default[0]).getNrPointers())
			{
				for (int loper=0;loper < _MinNrWorkingSections;loper++)
				{
					if (_Default[loper] == null)
					{
						return false;
					}
				}
				return (_Default[0].Works() &&  _Default[1].Works());
			}
			return false;
		}
		public override void Empty()
		{
			EmptyFormat();
		}
		#endregion
		#region ISignal Members
		// Signal Manupalations
		public int getSignals(out Signals signals)
		{
			signals = new Signals();
			int err = getSignalsToObj(signals);
			if (err != 0)
			{
				signals = null;
			}
			return err;
		}
		public int getSignalsToObj(Signals signals)
		{
			if (signals != null)
			{
				if (((ISignal)_Default[3]).getSignalsToObj(signals) != 0)
				{
					return 2;
				}

				short[][] medianData = null;
				if (((ISignal)_Default[4]).getSignalsToObj(signals) == 0)
				{
					SCPSection5 median = (SCPSection5) _Default[5];
					
					if (median == null)
					{
						return 4;
					}

					medianData = median.DecodeData((SCPSection2) _Default[2], signals.MedianLength);

					signals.MedianAVM = median.getAVM();
					signals.MedianSamplesPerSecond = median.getSamplesPerSecond();

					for (int loper=0;loper < signals.NrLeads;loper++)
					{
						signals[loper].Median = medianData[loper];
					}
				}

				SCPSection6 rhythm = (SCPSection6) _Default[6];
				short[][] rhythmData = rhythm.DecodeData((SCPSection2) _Default[2], (SCPSection3) _Default[3], (SCPSection4) _Default[4], ((SCPSection5) _Default[5]).getSamplesPerSecond());
				signals.RhythmAVM = rhythm.getAVM();

				if (rhythmData == null)
				{
					return 8;
				}

				if ((medianData != null)
				&&  (((SCPSection3) _Default[3]).isMediansUsed()))
				{
					if (signals.RhythmAVM <= signals.MedianAVM)
					{
						ECGTool.ChangeMultiplier(medianData, signals.MedianAVM, signals.RhythmAVM);
						signals.MedianAVM = signals.RhythmAVM;
					}
					else
					{
						ECGTool.ChangeMultiplier(rhythmData, signals.RhythmAVM, signals.MedianAVM);
						signals.RhythmAVM = signals.MedianAVM;
					}

					signals.RhythmSamplesPerSecond = signals.MedianSamplesPerSecond;

					((SCPSection4) _Default[4]).AddMedians((SCPSection3) _Default[3], rhythmData, medianData);
				}
				else
				{
					signals.RhythmAVM = rhythm.getAVM();
					signals.RhythmSamplesPerSecond = rhythm.getSamplesPerSecond();
				}

				for (int loper=0;loper < signals.NrLeads;loper++)
				{
					signals[loper].Rhythm = rhythmData[loper];
				}

				return 0;
			}
			return 1;
		}
		public int setSignals(Signals signals)
		{
			if ((signals != null)
			&&  (signals.NrLeads > 0))
			{
				// Decide wich encoding to use.
				switch (_EncodingType)
				{
					case EncodingType.None:
						((SCPSection2)_Default[2]).UseNoHuffman();
					break;
					case EncodingType.OptimizedHuffman:
						// not implemented!
						((SCPSection2)_Default[2]).UseStandard();
					break;
					case EncodingType.DefaultHuffman:
					default:
						((SCPSection2)_Default[2]).UseStandard();
					break;
				}

				if (((ISignal)_Default[3]).setSignals(signals) != 0)
				{
					return 2;
				}

				SCPSection5 median = (SCPSection5) _Default[5];
				median.setAVM(signals.MedianAVM);
				median.setSamplesPerSecond(signals.MedianSamplesPerSecond);

				SCPSection6 rhythm = (SCPSection6) _Default[6];
				rhythm.setAVM(signals.RhythmAVM);
				rhythm.setSamplesPerSecond(signals.RhythmSamplesPerSecond);

				short[][] rhythmData = new short[signals.NrLeads][];
				short[][] medianData = new short[signals.NrLeads][];
				for (int loper=0;loper < signals.NrLeads;loper++)
				{
					if (signals[loper].Rhythm == null)
					{
						return 4;
					}
					rhythmData[loper] = signals[loper].Rhythm;
					if ((medianData == null)
					||  (signals[loper].Median == null))
					{
						medianData = null;
					}
					else
					{
						medianData[loper] = signals[loper].Median;
					}
				}

				if (medianData != null)
				{
					if (((ISignal)_Default[4]).setSignals(signals) != 0)
					{
						return 8;
					}

					if (signals.MedianSamplesPerSecond < signals.RhythmSamplesPerSecond )
					{
						median.setSamplesPerSecond(signals.RhythmSamplesPerSecond);
						ECGTool.ResampleSignal(medianData, signals.MedianSamplesPerSecond, signals.RhythmSamplesPerSecond, out medianData);
					}

					if (median.EncodeData(medianData, (SCPSection2) _Default[2], (ushort)((signals.MedianLength * signals.MedianSamplesPerSecond) / 1000), (_EncodingType == EncodingType.None ? (byte)0 : _DifferenceDataSection5Used)) != 0)
					{
						return 16;
					}

					if (signals.QRSZone != null)
					{
						if (signals.RhythmAVM <= signals.MedianAVM)
						{
							ECGTool.ChangeMultiplier(medianData, signals.MedianAVM, signals.RhythmAVM);
						}
						else
						{
							ECGTool.ChangeMultiplier(rhythmData, signals.RhythmAVM, signals.MedianAVM);
							rhythm.setAVM(signals.MedianAVM);
						}
					}

					ECGTool.ResampleSignal(rhythmData, signals.RhythmSamplesPerSecond, median.getSamplesPerSecond(), out rhythmData);

					if (_QRSSubtractionSupport
					&&	(signals.QRSZone != null))
					{
						((SCPSection3) _Default[3]).setMediansUsed(true);
						((SCPSection4) _Default[4]).SubtractMedians((SCPSection3) _Default[3], rhythmData, medianData);
					}
				}

				if (_BimodalCompressionUsed
				&&	(_BimodalCompressionRate > 0)
				&&	(medianData != null)
				&&	(signals.QRSZone != null))
				{
					// Bimodal Compression must be set in set section 6.
					rhythm.setBimodal(true);
					rhythm.setSamplesPerSecond(signals.MedianSamplesPerSecond / _BimodalCompressionRate);

					// Determine QRS zones for bimodal compression
					GlobalMeasurements global;
					((IGlobalMeasurement)_Default[7]).getGlobalMeasurements(out global);	
					((SCPSection4)_Default[4]).setProtected(global, median.getSamplesPerSecond(), _BimodalCompressionRate, ((SCPSection3)_Default[3]).getMinBegin(), ((SCPSection3)_Default[3]).getMaxEnd());
				}

				if (rhythm.EncodeData(rhythmData, (SCPSection2) _Default[2], (SCPSection3) _Default[3], (SCPSection4) _Default[4], signals.MedianSamplesPerSecond, (_EncodingType == EncodingType.None ? (byte)0 : _DifferenceDataSection6Used)) != 0)
				{
					return 32;
				}
				return 0;
			}
			return 1;
		}
		#endregion
		/// <summary>
		/// Function to check SCP.
		/// </summary>
		/// <param name="buffer">byte array to do check in</param>
		/// <param name="offset">position to start checking</param>
		/// <param name="crc">value crc should be</param>
		/// <param name="length">length of section</param>
		/// <returns>0 on success</returns>
		public bool CheckSCP(byte[] buffer, int offset, ushort crc, int length)
		{
			CRCTool crctool = new CRCTool();
			crctool.Init(CRCTool.CRCCode.CRC_CCITT);
			if (crctool.CalcCRCITT(buffer, offset + Marshal.SizeOf(_CRC), length - Marshal.SizeOf(_CRC)) == crc)
			{
				return true;
			}
			return false;
		}
		/// <summary>
		/// Function to set pointers.
		/// </summary>
		public void setPointers()
		{
			if (_Default[0] is SCPSection0)
			{
				SCPSection0 pointers = (SCPSection0) _Default[0];
				pointers.setNrPointers(_MinNrSections + (_Manufactor != null ? _Manufactor.Length : 0));
				int sum = Marshal.SizeOf(_CRC) + Marshal.SizeOf(_Length) + 1;
				for (int loper=0;loper < _MinNrSections;loper++)
				{
					ushort id = (ushort) loper;
					int length = _Default[loper].getLength();
					int index = (length > SCPSection.Size ? sum : 0);
					pointers.setPointer(loper, id, length, index);
					sum += length;
				}
				if (_Manufactor != null)
				{
					for (int loper=0;loper < _Manufactor.Length;loper++)
					{
						ushort id = _Manufactor[loper].getSectionID();
						int length = _Manufactor[loper].getLength();
						int index = (length > SCPSection.Size ? sum : 0);
						pointers.setPointer(_MinNrSections + loper, id, length, index);
						sum += length;
					}
				}

				_Length = sum - 1;

				// Determine file Protocol Compatibility Level.
				if (_Default[0].Works()
				&&  _Default[1].Works()
				&&  (_Default[1] is SCPSection1))
				{
					if (_Default[2].Works()
					&&  _Default[3].Works())
					{
						if (_Default[4].Works()
						&&  _Default[5].Works()
						&&  _Default[6].Works())
						{
							((SCPSection1)_Default[1]).setProtocolCompatibilityLevel(SCPSection1.ProtocolCompatibility.CatIV);
						}
						else if (_Default[5].Works())
						{
							((SCPSection1)_Default[1]).setProtocolCompatibilityLevel(SCPSection1.ProtocolCompatibility.CatIII);
						}
						else if (_Default[6].Works())
						{
							((SCPSection1)_Default[1]).setProtocolCompatibilityLevel(SCPSection1.ProtocolCompatibility.CatII);
						}
						else if (_Default[7].Works()
							&&   _Default[8].Works())
						{
							((SCPSection1)_Default[1]).setProtocolCompatibilityLevel(SCPSection1.ProtocolCompatibility.CatI);
						}
					}
					else if (_Default[7].Works()
						&&   _Default[8].Works())
					{
						((SCPSection1)_Default[1]).setProtocolCompatibilityLevel(SCPSection1.ProtocolCompatibility.CatI);
					}
				}
			}
		}
		/// <summary>
		/// Function to empty entire format.
		/// </summary>
		private void EmptyFormat()
		{
			for (int loper=0;loper < _MinNrSections;loper++)
			{
				_Default[loper].Empty();
			}
			_Manufactor = null;
		}
		#region IDisposable Members
		public override void Dispose()
		{
			base.Dispose();

			_CRC = 0;
			_Length = 0;
			if (_Default != null)
			{
				for (int loper=0;loper < _Default.Length;loper++)
				{
					if (_Default[loper] != null)
					{
						_Default[loper].Empty();
						_Default[loper] = null;
					}
				}
				_Default = null;
			}
			if (_Manufactor != null)
			{
				for (int loper=0;loper < _Manufactor.Length;loper++)
				{
					if (_Manufactor[loper] != null)
					{
						_Manufactor[loper].Empty();
						_Manufactor[loper] = null;
					}
				}
				_Manufactor = null;
			}
		}
		#endregion
		/// <summary>
		/// Function to convert to SCP.
		/// </summary>
		/// <param name="src">an ECG file to convert</param>
		/// <param name="dst">SCP file returned</param>
		/// <returns>0 on success</returns>
		public static int ToSCP(IECGFormat src, ECGConfig cfg, out IECGFormat dst)
		{
			dst = null;
			if (src != null)
			{
				dst = new SCPFormat();

				if ((cfg != null)
				&&	((dst.Config == null)
				||	 !dst.Config.Set(cfg)))
					return 1;

				int err = ECGConverter.Convert(src, dst);
				if (err != 0)
				{
					return err;
				}

				((SCPFormat)dst).setPointers();

				return 0;
			}
			return 1;
		}
		/// <summary>
		/// Enumration to set encoding used during encoding of signal.
		/// </summary>
		/// <remarks>
		/// OptimizedHuffman is same as DefaultHuffman, because it isn't implemented
		/// </remarks>
		public enum EncodingType
		{None = 0, DefaultHuffman, OptimizedHuffman}
	}
}
