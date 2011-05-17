/***************************************************************************
Copyright 2011, M.J.B. van Ettinger Jr., The Netherlands

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

Written by Maarten van Ettinger.

****************************************************************************/
using System;
using System.IO;
using System.Runtime.InteropServices;

using Communication.IO.Tools;

using ECGConversion.ECGDemographics;
using ECGConversion.ECGSignals;

namespace ECGConversion.OmronECG
{
	/// <summary>
	/// Summary description for OmronECGFormat.
	/// </summary>
	public sealed class OmronECGFormat : IECGFormat, IDemographic, ISignal
	{
		public const Int32 HeaderSize = 0x3C;
		public static Boolean LittleEndian = true;

		/// <summary>
		/// 0x00: First four bytes of file
		/// </summary>
		public const String PrefixOne = "ECG\n";

		/// <summary>
		/// 0x04: 5th and 6th byte of the file.
		/// </summary>
		public const Int16 PrefixTwo = 0x2716;

		/// <summary>
		/// 0x06: Unknown value nr 1
		/// </summary>
		public Int16 Unknown0;

		/// <summary>
		/// 0x08: Acquisition Date of the ECG (Seconds since January 1st 1970)
		/// </summary>
		public UInt32 AcquisitionDate;

		/// <summary>
		/// 0x0C: Nr of samples in file
		/// </summary>
		public Int32 NrOfSamples;

		/// <summary>
		/// 0x10: Unkown value nr 2
		/// </summary>
		public Int16 Unknown1;

		/// <summary>
		/// 0x12: Samples per second
		/// </summary>
		public Int16 SamplesPerSecond;

		/// <summary>
		/// 0x14-0x40: Unknown array of 2-byte integers
		/// </summary>
		public Int16[] Unknown2 = new Int16[20];

		/// <summary>
		/// Segment containing signal data.
		/// </summary>
		public Byte[] SignalData;

		#region IECGFormat Members
		public override int Read(Stream input, int offset)
		{
			if ((input != null)
			&&  (input.CanRead))
			{
				// Reading information from stream to byte array.
				input.Seek(offset, SeekOrigin.Begin);
				byte[] buffer1 = new byte[HeaderSize];
				BytesTool.readStream(input, buffer1, 0, buffer1.Length);
				Int32 length = (Int32) BytesTool.readBytes(buffer1, 0x0C, Marshal.SizeOf(NrOfSamples), LittleEndian);

				if (length > 0)
				{
					// 12 bits per sample
					length = length + (length >> 1);

					if ((offset + buffer1.Length + length) <= input.Length)
					{
						byte[] buffer2 = new byte[buffer1.Length + length];
						BytesTool.readStream(input, buffer2, buffer1.Length, buffer2.Length - buffer1.Length);
						BytesTool.copy(buffer2, 0, buffer1, 0, buffer1.Length);
						// Read using function for byte array.
						return Read(buffer2, 0) << 1;
					}
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
			if ((buffer.Length - offset) >= HeaderSize)
			{
				// read in prefix part one.
				int size = PrefixOne.Length;
				String prefixOne = BytesTool.readString(buffer, offset, size);
				offset += size;

				// read in prefix part two.
				size = Marshal.SizeOf(PrefixTwo);
				Int16 prefixTwo = (Int16)BytesTool.readBytes(buffer, offset, size, LittleEndian);
				offset += size;

				if ((string.Compare(prefixOne, PrefixOne, false) == 0)
				&&	(prefixTwo == PrefixTwo))
				{
					// read in unknown0
					size = Marshal.SizeOf(Unknown0);
					Unknown0 = (Int16)BytesTool.readBytes(buffer, offset, size, LittleEndian);
					offset += size;

					// read in AcquisitionDate
					size = Marshal.SizeOf(AcquisitionDate);
					AcquisitionDate = (UInt32)BytesTool.readBytes(buffer, offset, size, LittleEndian);
					offset += size;

					// read in NrOfSamples
					size = Marshal.SizeOf(NrOfSamples);
					NrOfSamples = (Int16)BytesTool.readBytes(buffer, offset, size, LittleEndian);
					offset += size;

					// read in Unknown1
					size = Marshal.SizeOf(Unknown1);
					Unknown1 = (Int16)BytesTool.readBytes(buffer, offset, size, LittleEndian);
					offset += size;

					// read in SamplesPerSecond
					size = Marshal.SizeOf(SamplesPerSecond);
					SamplesPerSecond = (Int16)BytesTool.readBytes(buffer, offset, size, LittleEndian);
					offset += size;

					// read in Unknown2
					size = Marshal.SizeOf(typeof(Int16));
					for (int i=0;i < Unknown2.Length;i++)
					{
						Unknown2[i] = (Int16)BytesTool.readBytes(buffer, offset, size, LittleEndian);
						offset += size;
					}

					SignalData = new Byte[NrOfSamples + (NrOfSamples >> 1)];
					for (int i=0;i < SignalData.Length;i++)
						SignalData[i] = buffer[offset++];
					
					return 0;
				}
			}

			return 1;

			 
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
/*			if ((output != null)
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
			}*/
			return 0x1;
		}
		public override int Write(byte[] buffer, int offset)
		{
/*			// Check if format works.
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
			}*/
			return 0x1;
		}
		public override bool CheckFormat(Stream input, int offset)
		{
			if ((input != null)
			&&  (input.CanRead))
			{
				// Reading information from stream to byte array.
				input.Seek(offset, SeekOrigin.Begin);
				byte[] buffer = new byte[HeaderSize];
				BytesTool.readStream(input, buffer, 0, buffer.Length);

				// read in prefix part one.
				int size = PrefixOne.Length;
				String prefixOne = BytesTool.readString(buffer, offset, size);
				offset += size;

				// read in prefix part two.
				size = Marshal.SizeOf(PrefixTwo);
				Int16 prefixTwo = (Int16)BytesTool.readBytes(buffer, offset, size, LittleEndian);
				offset += size;

				if ((string.Compare(prefixOne, PrefixOne, false) == 0)
				&&	(prefixTwo == PrefixTwo))
				{
					Int32 length = (Int32) BytesTool.readBytes(buffer, 0x0C, Marshal.SizeOf(NrOfSamples), LittleEndian);

					if (length > 0)
					{
						// 12 bits per sample
						length = length + (length >> 1);

						if ((offset + buffer.Length + length) <= input.Length)
						{
							return true;
						}
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
			if (buffer.Length >= HeaderSize)
			{
				// read in prefix part one.
				int size = PrefixOne.Length;
				String prefixOne = BytesTool.readString(buffer, offset, size);
				offset += size;

				// read in prefix part two.
				size = Marshal.SizeOf(PrefixTwo);
				Int16 prefixTwo = (Int16)BytesTool.readBytes(buffer, offset, size, LittleEndian);
				offset += size;

				if ((string.Compare(prefixOne, PrefixOne, false) == 0)
					&&	(prefixTwo == PrefixTwo))
				{
					Int32 length = (Int32) BytesTool.readBytes(buffer, 0x0C, Marshal.SizeOf(NrOfSamples), LittleEndian);

					if (length > 0)
					{
						// 12 bits per sample
						length = length + (length >> 1);

						return (offset + HeaderSize + length) <= buffer.Length;
					}
				}
			}

			return false;
		}
		public override void Anonymous(byte type) {}

		public override int getFileSize()
		{
			return Works() ? HeaderSize + SignalData.Length : 0;
		}
		public override ECGConversion.ECGDemographics.IDemographic Demographics
		{
			get {return this;}
		}
		public override ECGConversion.ECGDiagnostic.IDiagnostic Diagnostics
		{
			get {return null;}
		}
		public override ECGConversion.ECGGlobalMeasurements.IGlobalMeasurement GlobalMeasurements
		{
			get {return null;}
		}
		public override ECGConversion.ECGSignals.ISignal Signals
		{
			get {return this;}
		}

		public override ECGConversion.ECGLeadMeasurements.ILeadMeasurement LeadMeasurements
		{
			get {return null;}
		}

		public override bool Works()
		{
			// 12 bits per sample
			int nrSingalBytes = NrOfSamples + (NrOfSamples >> 1);

			return (AcquisitionDate != 0)
				&& (SamplesPerSecond >= 0)
				&& (SignalData != null)
				&& (SignalData.Length == nrSingalBytes);
		}
		public override void Empty()
		{
			Unknown0 = 0;
			Unknown1 = 0;
			for (int i=0;i < Unknown2.Length;i++)
				Unknown2[i] = 0;

			AcquisitionDate = 0;
			NrOfSamples = 0;
			SamplesPerSecond = 0;
			SignalData = null;
		}
		#endregion

		#region IDemographic Members
		public void Init() {}
		
		public string LastName
		{
			get {return "NoLastName";}
			set {}
		}
		
		public string FirstName
		{
			get {return null;}
			set {}
		}
		
		public string PatientID
		{
			get {return "NoPatientID";}
			set {}
		}
		
		public string SecondLastName
		{
			get {return null;}
			set {}
		}
		
		public string PrefixName
		{
			get {return null;}
			set {}
		}
		
		public string SuffixName
		{
			get {return null;}
			set {}
		}
		
		public int getPatientAge(out ushort val, out AgeDefinition def)
		{
			val = 0;
			def = AgeDefinition.Unspecified;

			return 1;
		}
		
		public int setPatientAge(ushort val, AgeDefinition def)
		{
			return 1;
		}
		
		public Date PatientBirthDate
		{
			get {return null;}
			set {}
		}
		
		public int getPatientHeight(out ushort val, out HeightDefinition def)
		{
			val = 0;
			def = HeightDefinition.Unspecified;

			return 1;
		}
		
		public int setPatientHeight(ushort val, HeightDefinition def)
		{
			return 1;
		}
		
		public int getPatientWeight(out ushort val, out WeightDefinition def)
		{
			val = 0;
			def = WeightDefinition.Unspecified;

			return 1;
		}
		
		public int setPatientWeight(ushort val, WeightDefinition def)
		{
			return 1;
		}
		
		public Sex Gender
		{
			get {return Sex.Null;}
			set {}
		}
		
		public Race PatientRace
		{
			get {return Race.Null;}
			set {}
		}
		
		public AcquiringDeviceID AcqMachineID
		{
			get {return new AcquiringDeviceID(true);}
			set{}
		}
		
		public AcquiringDeviceID AnalyzingMachineID
		{
			get {return null;}
			set{}
		}

		public DateTime TimeAcquisition
		{
			get
			{
				return new DateTime(1970, 1, 1, 0, 0, 0).Add(TimeSpan.FromSeconds(AcquisitionDate));
			}
			set
			{
				DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0);

				AcquisitionDate = (UInt32) (value - origin).TotalSeconds;
			}
		}
		
		public ushort BaselineFilter
		{
			get {return 0;}
			set {}
		}

		public ushort LowpassFilter
		{
			get {return 0;}
			set {}
		}

		public byte FilterBitmap
		{
			get {return 0;}
			set {}
		}
		
		public string[] FreeTextFields 
		{
			get {return null;}
			set{}
		}
		
		public string SequenceNr
		{
			get {return null;}
			set{}
		}
		
		public string AcqInstitution
		{
			get {return null;}
			set{}
		}
		
		public string AnalyzingInstitution
		{
			get {return null;}
			set{}
		}
		
		public string AcqDepartment
		{
			get {return null;}
			set{}
		}
		
		public string AnalyzingDepartment
		{
			get {return null;}
			set{}
		}
		
		public string ReferringPhysician
		{
			get {return null;}
			set{}
		}
		
		public string OverreadingPhysician 
		{
			get {return null;}
			set{}
		}
		
		public string TechnicianDescription
		{
			get {return null;}
			set{}
		}
		
		public ushort SystolicBloodPressure
		{
			get {return 0;}
			set{}
		}
		
		public ushort DiastolicBloodPressure
		{
			get {return 0;}
			set{}
		}

		public Drug[] Drugs
		{
			get {return null;}
			set{}
		}
		
		public string[] ReferralIndication
		{
			get {return null;}
			set{}
		}

		public string RoomDescription
		{
			get {return null;}
			set{}
		}

		public byte StatCode
		{
			get {return 0;}
			set{}
		}

		#endregion

		#region ISignal Members

		public int setSignals(Signals signals)
		{
			// TODO:  Add OmronECGFormat.setSignals implementation
			return 1;
		}

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
			if (!Works())
				return 1;

			// get the samples per seconds and the AVM.
			signals.RhythmSamplesPerSecond = this.SamplesPerSecond;
			signals.RhythmAVM = 10.0;

			// only one lead in this file
			signals.NrLeads = 1;

			signals[0] = new Signal();
			signals[0].Type = LeadType.Unknown;
			signals[0].RhythmStart = 0;
			signals[0].RhythmEnd = this.NrOfSamples;
			signals[0].Rhythm = new short[this.NrOfSamples];

			for (int i=0;i < this.NrOfSamples;i++)
			{

				Int32 pos = i + (i >> 1);
				UInt16 val = 0;

				if ((i & 0x1) == 0x0)
				{
					val |= (UInt16) this.SignalData[pos];
					val |= (UInt16)((this.SignalData[pos+1] & 0x0f) << 8);
				}
				else
				{
					val |= (UInt16) (this.SignalData[pos] >> 4);
					val |= (UInt16) (this.SignalData[pos+1] << 4);
				}

				signals[0].Rhythm[i] = (Int16) ((Int32)val - 1024);
			}

			return 0;
		}

		#endregion
	}
}
