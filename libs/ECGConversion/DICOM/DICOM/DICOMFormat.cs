/***************************************************************************
Copyright 2012-2013, van Ettinger Information Technology, Lopik, The Netherlands
Copyright 2008-2010, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

using org.dicomcs.data;
using org.dicomcs.dict;

using Communication.IO.Tools;
using ECGConversion;
using ECGConversion.ECGDemographics;
using ECGConversion.ECGDiagnostic;
using ECGConversion.ECGGlobalMeasurements;
using ECGConversion.ECGSignals;

namespace ECGConversion.DICOM
{
	/// <summary>
	/// Summary description for DICOMFormat.
	/// </summary>
	public sealed class DICOMFormat : IECGFormat, ISignal, IDemographic, IDiagnostic, IGlobalMeasurement
	{
		public static int ParseInt(string str)
		{
			int ret = int.MinValue;

			if (str != null)
			{
				if (str.Contains("."))
				{
					if (str.EndsWith("."))
						str += "0";

					double temp = double.Parse(str, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);

					if ((temp > int.MaxValue)
					||	(temp < int.MinValue))
					{
						throw new Exception("Value outside the allowed range (int)!");
					}

					if (temp >= 0)
					{
						ret = (int) Math.Floor(temp);
					}
					else
					{
						ret = (int) Math.Ceiling(temp);
					}
				}
				else
				{
					ret = int.Parse(str, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
				}
			}

			return ret;
		}

		public static ushort ParseUShort(string str)
		{
			ushort ret = ushort.MinValue;
			
			if (str != null)
			{
				if (str.Contains("."))
				{
					if (str.EndsWith("."))
						str += "0";
					
					double temp = double.Parse(str, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);

					if ((temp > ushort.MaxValue)
					||	(temp < ushort.MinValue))
					{
						throw new Exception("Value outside the allowed range (ushort)!");
					}

					ret = (ushort) Math.Floor(temp);
				}
				else
				{
					ret = ushort.Parse(str, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
				}
			}
			
			return ret;
		}

		public static double ParseDouble(string str)
		{
			double ret = double.NaN;

			if (str != null)
			{
				if (str.EndsWith("."))
					str += "0";

				ret = double.Parse(str, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
			}

			return ret;
		}

		public static float ParseFloat(string str)
		{
			float ret = float.NaN;
			
			if (str != null)
			{
				if (str.EndsWith("."))
					str += "0";
				
				ret = float.Parse(str, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
			}
			
			return ret;
		}

		private enum GenerateSequenceNr
		{
			// default generate sequence nr values
			False = 0,
			True = 1,
			Always = 2,
			// Other values that are allowed
			No = 0,
			Yes = 1,
			// value that is not allowed
			Error = 0xff,
		}

		private bool _MortaraCompat
		{
			get
			{
				return string.Compare(_Config["Mortara Compatibility"], "true", true) == 0;
			}
		}

		private string _UIDPrefix
		{
			get
			{
				string uidPrefix = _Config["UID Prefix"];

				return uidPrefix == null ? "1.2.826.0.1.34471.2.44.6." : uidPrefix;
			}
		}

		private GenerateSequenceNr _GenerateSequenceNr
		{
			get
			{
				string cfg = _Config["Generate SequenceNr"];

				if ((cfg == null)
				||	(cfg.Length == 0))
				{
					return GenerateSequenceNr.False;
				}

				try
				{
					return (GenerateSequenceNr) ECGConverter.EnumParse(typeof(GenerateSequenceNr), cfg, true);
				} catch {}

				return GenerateSequenceNr.Error;
			}
		}

		private int StartPerBeatMeasurement
		{
			get
			{
				int ret = 100;

				try
				{
					ret = int.Parse(_Config["Start PerBeat Measurement"], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
				}
				catch
				{
					ret = 100;
				}

				return ret;
			}
		}

		public Dataset DICOMData
		{
			get
			{
				return _DICOMData;
			}
		}

		private Dataset _DICOMData = null;
		private ushort _HighpassFilter = 0;
		private ushort _LowpassFilter = 0;
		private byte _FilterMap = 0;
		private ushort _FiducialPoint = 0;
		private QRSZone[] _QRSZone = null;

		public DICOMFormat()
		{
			Empty();

			string[] cfgValue = {"Mortara Compatibility", "UID Prefix", "Generate SequenceNr", "Start PerBeat Measurement"};

			_Config = new ECGConfig(cfgValue, 2, new ECGConversion.ECGConfig.CheckConfigFunction(this._ConfigurationWorks));
			_Config["Mortara Compatibility"] = "false";
			_Config["UID Prefix"] = "1.2.826.0.1.34471.2.44.6.";
			_Config["Start PerBeat Measurement"] = "100";
		}

		public DICOMFormat(Dataset ds) : this()
		{
			_DICOMData = ds;
		}

		public bool _ConfigurationWorks()
		{
			try
			{
				int spbm = int.Parse(_Config["Start PerBeat Measurement"], System.Globalization.CultureInfo.InvariantCulture.NumberFormat);

				return (_GenerateSequenceNr != GenerateSequenceNr.Error)
					&& (spbm >= 0)
					&& ((string.Compare(_Config["Mortara Compatibility"], "true", true) == 0)
					||  (string.Compare(_Config["Mortara Compatibility"], "false", true) == 0));
			}
			catch {}

			return false;
		}

		public override int Read(Stream input, int offset)
		{
			if (CheckFormat(_DICOMData))
				return 0;

			try
			{
				input.Seek(offset, SeekOrigin.Begin);
				DcmParser parser = new DcmParser(input);
			
				FileFormat ff = parser.DetectFileFormat();

				if (ff != null)
				{
					parser.DcmHandler = _DICOMData.DcmHandler;

					parser.ParseDcmFile(ff, Tags.PixelData);

					return CheckFormat(_DICOMData) ? 0 : 2;
				}
			}
			catch
			{
			}

			return 1;
		}
		public override int Read(string file, int offset)
		{
			if (CheckFormat(_DICOMData))
				return 0;

			FileStream stream = null;

			try
			{
				stream = new FileStream(file, FileMode.Open);

				return Read(stream, offset);
			}
			catch {}
			finally
			{
				if (stream != null)
				{
					stream.Close();
					stream = null;
				}
			}

			return 1;
		}
		public override int Read(byte[] buffer, int offset)
		{
			if (CheckFormat(_DICOMData))
				return 0;

			System.IO.MemoryStream ms = null;
			
			try
			{
				ms = new MemoryStream(buffer, offset, buffer.Length-offset, false);

				return Read(ms, 0);
			}
			catch {}
			finally
			{
				if (ms != null)
				{
					ms.Close();
					ms = null;
				}
			}

			return 2;
		}
		public override int Write(Stream output)
		{
			try
			{
				FileMetaInfo fmi = _DICOMData.GetFileMetaInfo();

				if (fmi != null)
				{
					DcmEncodeParam param = DcmDecodeParam.ValueOf(fmi.GetString(Tags.TransferSyntaxUID));

					if (_MortaraCompat)
					{
						param = new DcmEncodeParam(org.dicomcs.util.ByteOrder.LITTLE_ENDIAN, false, false, false, true, false, false);

						fmi.PutUI(Tags.TransferSyntaxUID, UIDs.ImplicitVRLittleEndian);
					}

					_DICOMData.WriteFile(output, param);
				}
				else
				{
					_DICOMData.WriteFile(output, org.dicomcs.data.DcmEncodeParam.EVR_LE);
				}

				return 0;
			}
			catch {}

			return 1;
		}
		public override int Write(string file)
		{
			FileStream output = null;

			try
			{
				output = new FileStream(file, FileMode.Create);

				return Write(output);
			}
			catch {}
			finally
			{
				if (output != null)
				{
					output.Close();
					output = null;
				}
			}

			return 1;
		}
		public override int Write(byte[] buffer, int offset)
		{
			System.IO.MemoryStream ms = null;
			
			try
			{
				ms = new MemoryStream(buffer, offset, buffer.Length-offset, true);

				return Write(ms);
			}
			catch {}
			finally
			{
				if (ms != null)
				{
					ms.Close();
					ms = null;
				}
			}

			return 2;
		}
		public bool CheckFormat(Dataset ds)
		{
			if (ds == null)
				return false;

			bool ret = false;

			switch (ds.GetString(Tags.SOPClassUID))
			{
				case UIDs.WaveformStorageTrialRetired:
				case UIDs.HemodynamicWaveformStorage:
				case UIDs.TwelveLeadECGWaveformStorage:
				case UIDs.GeneralECGWaveformStorage:
					ret = true;
					break;
				default:
					break;
			}

			ret = ret &&  (string.Compare(ds.GetString(Tags.Modality), "ECG") == 0);

			if (!ret)
			{
				Empty();
			}

			return ret;
		}
		public override bool CheckFormat(Stream input, int offset)
		{
			try
			{
				input.Seek(offset, SeekOrigin.Begin);

				DcmParser parser = new DcmParser(input);
			
				FileFormat ff = parser.DetectFileFormat();

				if (ff != null)
				{
					parser.DcmHandler = _DICOMData.DcmHandler;

					parser.ParseDcmFile(ff, Tags.PixelData);

					return CheckFormat(_DICOMData);
				}
			}
			catch {}

			return false;
		}
		public override bool CheckFormat(string file, int offset)
		{
			FileStream stream = null;

			try
			{
				stream = new FileStream(file, FileMode.Open);

				return CheckFormat(stream, offset);
			}
			catch {}
			finally
			{
				if (stream != null)
				{
					stream.Close();
					stream = null;
				}
			}

			return false;
		}
		public override bool CheckFormat(byte[] buffer, int offset)
		{
			System.IO.MemoryStream ms = null;
			
			try
			{
				ms = new MemoryStream(buffer, offset, buffer.Length-offset, false);

				return CheckFormat(ms, 0);
			}
			catch {}
			finally
			{
				if (ms != null)
				{
					ms.Close();
					ms = null;
				}
			}

			return false;
		}
		public override IDemographic Demographics
		{
			get
			{
				return this;
			}
		}
		public override IDiagnostic Diagnostics
		{
			get
			{
				return this;
			}
		}
		public override IGlobalMeasurement GlobalMeasurements
		{
			get
			{
				return this;
			}
		}
		public override ISignal Signals
		{
			get
			{
				return this;
			}
		}
		public override void Anonymous(byte type)
		{
			ECGTool.Anonymous(this, (char)type);
		}
		public override int getFileSize()
		{
			return -1;
		}
		public override bool Works()
		{
			return CheckFormat(_DICOMData);
		}
		public override void Empty()
		{
			_DICOMData = new Dataset();
		}

		#region IDisposable Members
		public override void Dispose()
		{
			base.Dispose();

			_DICOMData = null;
		}
		#endregion

		#region ISignal Members

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
			DcmElement waveformElement = _DICOMData.Get(Tags.WaveformSeq);

			try
			{
				if (waveformElement != null
				&&	waveformElement.HasItems())
				{
					Dataset waveformSet = waveformElement.GetItem(0);

					if (string.Compare(waveformSet.GetString(Tags.WaveformOriginality), "ORIGINAL", true) != 0)
						return 2;

					signals.NrLeads = (byte) waveformSet.GetInteger(Tags.NumberOfWaveformChannels);
                    int nrSamples = (int) waveformSet.GetInteger(Tags.NumberOfWaveformSamples);
					signals.RhythmSamplesPerSecond = ParseInt(waveformSet.GetString(Tags.SamplingFrequency));

					int ret = GetWaveform(signals, waveformSet.Get(Tags.ChannelDefInitionSeq), waveformSet.GetInts(Tags.WaveformData), nrSamples, false);

					if (ret != 0)
						return (ret > 0 ? 2 + ret : ret);

					DcmElement element = waveformSet.Get(Tags.WaveformPaddingValue);

					try
					{
						if (element != null)
							signals.TrimSignals((short) element.Int);
					}
					catch {}

					if (waveformElement.vm() == 2)
					{
						waveformSet = waveformElement.GetItem(1);

						if ((string.Compare(waveformSet.GetString(Tags.WaveformOriginality), "DERIVED", true) == 0)
						&&	(signals.NrLeads == waveformSet.GetInteger(Tags.NumberOfWaveformChannels)))
						{
							nrSamples = (int) waveformSet.GetInteger(Tags.NumberOfWaveformSamples);
							signals.MedianSamplesPerSecond = ParseInt(waveformSet.GetString(Tags.SamplingFrequency));
							signals.MedianLength = (ushort) ((1000 * nrSamples) / signals.MedianSamplesPerSecond);

							ret = GetWaveform(signals, waveformSet.Get(Tags.ChannelDefInitionSeq), waveformSet.GetInts(Tags.WaveformData), nrSamples, true);

							if (ret != 0)
								return (ret > 0 ? 2 + ret : ret);
						}
					}

					return 0;
				}
			}
			catch {}
			return 1;
		}

		public int setSignals(Signals signals)
		{
			Signals sigs = signals.CalculateTwelveLeads();

			if (sigs != null)
			{
				int start, end;

				sigs.CalculateStartAndEnd(out start, out end);

				if ((end - start) <= 16384)
				{
					FileMetaInfo fmi = _DICOMData.GetFileMetaInfo();

					if (fmi != null)
						fmi.PutUI(Tags.MediaStorageSOPClassUID, UIDs.TwelveLeadECGWaveformStorage);

					_DICOMData.PutUI(Tags.SOPClassUID, UIDs.TwelveLeadECGWaveformStorage);
				}

				signals = sigs;
			}

			_FiducialPoint = signals.MedianFiducialPoint;
			_QRSZone = signals.QRSZone;

			DcmElement waveformElement = _DICOMData.PutSQ(Tags.WaveformSeq);

			Dataset waveformSet = waveformElement.AddNewItem();

			int ret = SetWaveform(waveformSet, signals, false);

			if (ret != 0)
				return ret;

			if ((signals.MedianAVM != 0.0)
			&&	(signals.MedianSamplesPerSecond != 0)
			&&	(signals.MedianLength != 0))
			{
				waveformSet = waveformElement.AddNewItem();

				return SetWaveform(waveformSet, signals, true);
			}

            return 0;
		}

		#endregion

		#region IDemographic Members
		public void Init()
		{
			Empty();

			try
			{
				FileMetaInfo fmi = new FileMetaInfo();

				fmi.PutOB(Tags.FileMetaInformationVersion, new byte[] {0, 1});
				fmi.PutUI(Tags.MediaStorageSOPClassUID, UIDs.GeneralECGWaveformStorage);
				fmi.PutUI(Tags.MediaStorageSOPInstanceUID, "1.3.6.1.4.1.1.24.04.1985");
				fmi.PutUI(Tags.TransferSyntaxUID, (_MortaraCompat ? UIDs.ImplicitVRLittleEndian : UIDs.ExplicitVRLittleEndian));
				fmi.PutUI(Tags.ImplementationClassUID, "2.24.985.4");
				fmi.PutSH(Tags.ImplementationVersionName, "ECGConversion2");

				_DICOMData.SetFileMetaInfo(fmi);

				_DICOMData.PutDA(Tags.InstanceCreationDate, DateTime.Now);
				_DICOMData.PutTM(Tags.InstanceCreationTime, DateTime.Now);
				_DICOMData.PutUI(Tags.SOPClassUID, UIDs.GeneralECGWaveformStorage);
				_DICOMData.PutUI(Tags.SOPInstanceUID, "1.3.6.1.4.1.1.24.04.1985");
				_DICOMData.PutDA(Tags.StudyDate, "");
				_DICOMData.PutDA(Tags.ContentDate, "");
				_DICOMData.PutDT(Tags.AcquisitionDatetime, "");
				_DICOMData.PutTM(Tags.StudyTime, "");
				_DICOMData.PutTM(Tags.ContentTime, "");
				_DICOMData.PutSH(Tags.AccessionNumber, "");
				_DICOMData.PutCS(Tags.Modality, "ECG");
				_DICOMData.PutLO(Tags.Manufacturer, "");
				_DICOMData.PutPN(Tags.ReferringPhysicianName, "");
				_DICOMData.PutPN(Tags.NameOfPhysicianReadingStudy, "");
				_DICOMData.PutPN(Tags.OperatorName, "");
				_DICOMData.PutLO(Tags.ManufacturerModelName, "");
				_DICOMData.PutPN(Tags.PatientName, "");
				_DICOMData.PutLO(Tags.PatientID, "");
				_DICOMData.PutDA(Tags.PatientBirthDate, "");
				_DICOMData.PutCS(Tags.PatientSex, "");
				_DICOMData.PutLO(Tags.OtherPatientIDs, "");
				_DICOMData.PutAS(Tags.PatientAge, "");
				_DICOMData.PutDS(Tags.PatientSize, "");
				_DICOMData.PutDS(Tags.PatientWeight, "");
				_DICOMData.PutLO(Tags.DeviceSerialNumber, "");
				_DICOMData.PutLO(Tags.SoftwareVersion, "");
				_DICOMData.PutUI(Tags.StudyInstanceUID, "1.1.1.1.1");
				_DICOMData.PutUI(Tags.SeriesInstanceUID, "1.1.1.1.2");
				_DICOMData.PutSH(Tags.StudyID, "");
				_DICOMData.PutIS(Tags.SeriesNumber, "1");
				_DICOMData.PutIS(Tags.InstanceNumber, "1");
				_DICOMData.PutCS(Tags.Laterality, "");
				_DICOMData.PutLO(Tags.CurrentPatientLocation, "");
				_DICOMData.PutLO(Tags.PatientInstitutionResidence, "");
				_DICOMData.PutLT(Tags.VisitComments, "");

				DcmElement temp1 = _DICOMData.PutSQ(Tags.AcquisitionContextSeq);
				Dataset ds = temp1.AddNewItem();

				ds.PutCS(Tags.ValueType, "CODE");

				DcmElement temp2 = ds.PutSQ(Tags.ConceptNameCodeSeq);
				ds = temp2.AddNewItem();

				ds.PutSH(Tags.CodeValue, "5.4.5-33-1");
				ds.PutSH(Tags.CodingSchemeDesignator, "SCPECG");
				ds.PutSH(Tags.CodingSchemeVersion, "1.3");
				ds.PutLO(Tags.CodeMeaning, "Electrode Placement");

				ds = temp1.GetItem(0);
				
				temp2 = ds.PutSQ(Tags.ConceptCodeSeq);
				ds = temp2.AddNewItem();

				ds.PutSH(Tags.CodeValue, "5.4.5-33-1-0");
				ds.PutSH(Tags.CodingSchemeDesignator, "SCPECG");
				ds.PutSH(Tags.CodingSchemeVersion, "1.3");
				ds.PutLO(Tags.CodeMeaning, "Unspecified");

				_DICOMData.PutLO(Tags.ReasonForTheRequestedProcedure, "");
			}
			catch
			{
				int i=0;
				i++;
			}

		}
		private string getName(int pos)
		{
			PersonName pn = new PersonName(_DICOMData.GetString(Tags.PatientName));

			return pn.Get(pos);
		}
		private int setName(string name, int pos)
		{
			PersonName pn = new PersonName(_DICOMData.GetString(Tags.PatientName));

			pn.Set(pos, name);

			_DICOMData.PutPN(Tags.PatientName, pn);

			return 0;
		}
		public string LastName
		{
			get {return getName(PersonName.FAMILY);}
			set {setName(value, PersonName.FAMILY);}
		}
		public string FirstName
		{
			get {return getName(PersonName.GIVEN);}
			set {setName(value, PersonName.GIVEN);}
		}
		public string PatientID
		{
			get {return _DICOMData.GetString(Tags.PatientID);}
			set {if (value != null) _DICOMData.PutLO(Tags.PatientID, value);}
		}
		public string SecondLastName
		{
			get {return getName(PersonName.MIDDLE);}
			set {setName(value, PersonName.MIDDLE);}
		}
		public string PrefixName
		{
			get {return getName(PersonName.PREFIX);}
			set {setName(value, PersonName.PREFIX);}
		}
		public string SuffixName
		{
			get {return getName(PersonName.SUFFIX);}
			set {setName(value, PersonName.SUFFIX);}
		}
		public int getPatientAge(out ushort val, out AgeDefinition def)
		{
			val = 0;
			def = AgeDefinition.Unspecified;

			string temp = _DICOMData.GetString(Tags.PatientAge);

			if (temp != null)
			{
				try
				{
					val = ParseUShort(temp.Substring(0, temp.Length-1));

					switch (temp[temp.Length-1])
					{
						case 'D': case 'd':
							def = AgeDefinition.Days;
							break;
						case 'W': case 'w':
							def = AgeDefinition.Weeks;
							break;
						case 'M': case 'm':
							def = AgeDefinition.Months;
							break;
						case 'Y': case 'y':
							def = AgeDefinition.Years;
							break;
						default:
							val = ParseUShort(temp);
							def = AgeDefinition.Years;
							break;
					}
				}
				catch {}
			}
			
			return def == AgeDefinition.Unspecified ? 1 : 0;
		}
		public int setPatientAge(ushort val, AgeDefinition def)
		{
			string temp = null;;

			try
			{

				switch (def)
				{
					case AgeDefinition.Hours:
						temp = (((val - 1) / 24) + 1).ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + 'D';
						break;
					case AgeDefinition.Days:
						temp = val.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + 'D';
						break;
					case AgeDefinition.Weeks:
						temp = val.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + 'W';
						break;
					case AgeDefinition.Months:
						temp = val.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + 'M';
						break;
					case AgeDefinition.Years:
						temp = val.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + 'Y';
						break;
					default:
						temp = null;
						break;
				}
			}
			catch
			{
				temp = null;
			}

			if (temp != null)
			{
				_DICOMData.PutAS(Tags.PatientAge, temp);

				return 0;
			}

			return 1;
		}
		public Date PatientBirthDate
		{
			get
			{
				DateTime time = _DICOMData.GetDate(Tags.PatientBirthDate);

				if (time.Year > 1000)
					return new Date((ushort) time.Year, (byte) time.Month, (byte) time.Day);

				return null;
			}
			set
			{
				if ((value != null)
				&&	value.isExistingDate())
				{
					_DICOMData.PutDA(Tags.PatientBirthDate, new DateTime(value.Year, value.Month, value.Day));
				}
			}
		}
		public int getPatientHeight(out ushort val, out HeightDefinition def)
		{
			val = 0;
			def = HeightDefinition.Unspecified;

			try
			{
				double val2 = ParseDouble(_DICOMData.GetString(Tags.PatientSize));

				if (val >= 0.1)
				{
					val = (ushort) (val2 * 100);
					def = HeightDefinition.Centimeters;
				}
				else
				{
					val = (ushort) (val2 * 1000);
					def = HeightDefinition.Millimeters;
				}

				return 0;
			}
			catch {}


			return 1;
		}
		public int setPatientHeight(ushort val, HeightDefinition def)
		{
			double val2 = double.MinValue;

			switch (def)
			{
				case HeightDefinition.Centimeters:
					val2 = val * 0.01;
					break;
				case HeightDefinition.Inches:
					val2 = val * 0.0254;
					break;
				case HeightDefinition.Millimeters:
					val2 = val * 0.001;
					break;

					
			}

			if (val2 > 0)
			{
				val2 = Math.Round(val2 * 100.0) * 0.01;

				_DICOMData.PutDS(Tags.PatientSize, val2.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat));

				return 0;
			}

			return 1;
		}
		public int getPatientWeight(out ushort val, out WeightDefinition def)
		{
			val = 0;
			def = WeightDefinition.Unspecified;

			try
			{
				double val2 = ParseDouble(_DICOMData.GetString(Tags.PatientWeight));

				if (val2 >= 1.0)
				{
					val = (ushort) val2;
					def = WeightDefinition.Kilogram;
				}
				else
				{
					val = (ushort) (val2 * 1000.0);
					def = WeightDefinition.Gram;
				}

				return 0;
			}
			catch {}

			return 1;
		}
		public int setPatientWeight(ushort val, WeightDefinition def)
		{
			double val2 = double.MinValue;

			switch (def)
			{
				case WeightDefinition.Gram:
					val2 = val * 0.001;
					break;
				case WeightDefinition.Kilogram:
					val2 = val;
					break;
				case WeightDefinition.Ounce:
					val2 = (val * 0.0283495231);
					break;
				case WeightDefinition.Pound:
					val2 = (val * 0.45359237);
					break;
			}

			if (val2 > 0)
			{
				val2 = Math.Round(val2 * 100.0) * 0.01;

				_DICOMData.PutDS(Tags.PatientWeight, val2.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat));

				return 0;
			}

			return 1;
		}
		public Sex Gender
		{
			get
			{
				string val = _DICOMData.GetString(Tags.PatientSex);
				if (val != null)
				{
					switch (val)
					{
						case "M":
							return Sex.Male;
						case "F":
							return Sex.Female;
						default:
							return Sex.Unspecified;
					}
				}

				return Sex.Null;
			}
			set
			{
				if (value != Sex.Null)
				{
					string sexText = "N";

					switch (value)
					{
						case Sex.Male:
							sexText = "M";
							break;
						case Sex.Female:
							sexText = "F";
							break;
					}


					if (sexText != null)
						_DICOMData.PutCS(Tags.PatientSex, sexText);
				}
			}
		}
		public Race PatientRace
		{
			get {return Race.Null;}
			set {}
		}
		public AcquiringDeviceID AcqMachineID
		{
			get
			{
				AcquiringDeviceID id = new AcquiringDeviceID(true);

				byte map = FilterBitmap;

				if (map != 0)
				{
					if ((map & 0x1) == 0x1)
						id.ACFrequencyEnvironment = 2;
					else if ((map & 0x2) == 0x2)
						id.ACFrequencyEnvironment = 1;
					else
						id.ACFrequencyEnvironment = 0;
				}

				return id;
			}
			set
			{
				_DICOMData.PutLO(Tags.Manufacturer, ((DeviceManufactor)value.ManufactorID).ToString());
				_DICOMData.PutLO(Tags.ManufacturerModelName, BytesTool.readString(value.ModelDescription, 0, value.ModelDescription.Length));
			}
		}
		public AcquiringDeviceID AnalyzingMachineID
		{
			get {return null;}
			set {}
		}
		public DateTime TimeAcquisition
		{
			get
			{
				DateTime time = _DICOMData.GetDate(Tags.AcquisitionDatetime);

				if (time.Year <= 1000)
					time = _DICOMData.GetDate(Tags.AcquisitionDate);

				return time;
			}
			set
			{
				_DICOMData.PutDA(Tags.StudyDate, value);
				_DICOMData.PutDA(Tags.ContentDate, value);
				_DICOMData.PutDT(Tags.AcquisitionDatetime, value);
				_DICOMData.PutTM(Tags.StudyTime, value);
				_DICOMData.PutTM(Tags.ContentTime, value);

				string
					val = "1",
					uid = _UIDPrefix + value.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat) + ".";

				// code to generate a sequence nr if not provided or always
				if ((_GenerateSequenceNr == GenerateSequenceNr.True)
				||	(_GenerateSequenceNr == GenerateSequenceNr.Always))
				{
					Random r = new Random();
					
					val = r.Next(1, 9999).ToString();

					_DICOMData.PutST(Tags.AccessionNumber, val);
				}

				uid += val;
				
				FileMetaInfo fmi = _DICOMData.GetFileMetaInfo();
				if (fmi != null)
					fmi.PutUI(Tags.MediaStorageSOPInstanceUID, uid + ".1");
				
				_DICOMData.PutUI(Tags.SOPInstanceUID, uid + ".1");
				_DICOMData.PutUI(Tags.StudyInstanceUID, uid);
				_DICOMData.PutUI(Tags.SeriesInstanceUID, uid + ".2");
			}
		}
		public ushort BaselineFilter
		{
			get
			{
				try
				{
					if (_HighpassFilter == 0)
					{
						float filter = GetFilter(_DICOMData.Get(Tags.WaveformSeq).GetItem(0), Tags.FilterLowFrequency) * 100.0f;

						if ((filter > 0)
						&&	(filter <= ushort.MaxValue))
							_HighpassFilter = (ushort) filter;
					}

					return _HighpassFilter;
				}
				catch {}
			
				return 0;
			}
			set
			{
				_HighpassFilter = value;
			}
		}
		public ushort LowpassFilter
		{
			get
			{
				try
				{
					if (_LowpassFilter == 0)
					{
						float filter = GetFilter(_DICOMData.Get(Tags.WaveformSeq).GetItem(0), Tags.FilterHighFrequency);

						if ((filter > 0)
						&&	(filter <= ushort.MaxValue))
							_LowpassFilter = (ushort) filter;
					}

					return _LowpassFilter;
				}
				catch {}

				return 0;
			}
			set
			{
				_HighpassFilter = value;
			}
		}
		public byte FilterBitmap
		{
			get
			{
				try
				{
					if (_FilterMap > byte.MaxValue)
					{
						byte map = 0;

						Dataset ds = _DICOMData.Get(Tags.WaveformSeq).GetItem(0);

						float filter = GetFilter(ds, Tags.NotchFilterFrequency);

						if (filter == 60.0f)
							map |= 0x1;
						else if (filter == 50.0f)
							map |= 0x2;

						filter = GetFilter(ds, Tags.FilterHighFrequency) * 100;
					
						if ((filter >= 0)
						&&	(filter <= ushort.MaxValue))
							map |= 0x8;

						_FilterMap = map;
					}

					return _FilterMap;
				}
				catch {}

				return 0;
			}
			set
			{
				_FilterMap = value;
			}
		}
		public string[] FreeTextFields
		{
			get {return _DICOMData.GetStrings(Tags.VisitComments);}
			set {if (value != null) _DICOMData.PutLT(Tags.VisitComments, value);}
		}
		public string SequenceNr
		{
			get
			{
				string temp1 = _DICOMData.GetString(Tags.StudyInstanceUID);

				if (temp1 != null)
				{
					string[] temp2 = temp1.Split('.');

					return temp2[temp2.Length-1];
				}

				return null;
			}
			set
			{
				string
					val = value,
					uid = _UIDPrefix + TimeAcquisition.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat) + ".";

				// code to generate a sequence nr if not provided or always
				if ((_GenerateSequenceNr == GenerateSequenceNr.Always)
				||	((_GenerateSequenceNr == GenerateSequenceNr.True))
				&&	 ((val == null)
				||	  (val.Length == 0)))
				{
					Random r = new Random();

					val = r.Next(1, 9999).ToString();
				}

				if ((val == null)
				||	(val.Length == 0))
					uid += "1";
				else
					uid += val;

				FileMetaInfo fmi = _DICOMData.GetFileMetaInfo();
				if (fmi != null)
					fmi.PutUI(Tags.MediaStorageSOPInstanceUID, uid + ".1");

				_DICOMData.PutUI(Tags.SOPInstanceUID, uid + ".1");
				_DICOMData.PutUI(Tags.StudyInstanceUID, uid);
				_DICOMData.PutUI(Tags.SeriesInstanceUID, uid + ".2");

				if (val != null)
				{
					_DICOMData.PutST(Tags.AccessionNumber, val);
				}
				else if (_DICOMData.Get(Tags.AccessionNumber) != null)
				{
					_DICOMData.Remove(Tags.AccessionNumber);
				}
			}
		}
		public string AcqInstitution
		{
			get {return _DICOMData.GetString(Tags.InstitutionName);}
			set {if (value != null) _DICOMData.PutLO(Tags.InstitutionName, value);}
		}
		public string AnalyzingInstitution
		{
			get {return null;}
			set {}
		}
		public string AcqDepartment
		{
			get {return _DICOMData.GetString(Tags.InstitutionalDepartmentName);}
			set {if (value != null) _DICOMData.PutLO(Tags.InstitutionalDepartmentName, value);}
		}
		public string AnalyzingDepartment
		{
			get {return null;}
			set {}
		}
		public string ReferringPhysician
		{
			get {return _DICOMData.GetString(Tags.ReferringPhysicianName);}
			set {if (value != null) _DICOMData.PutPN(Tags.ReferringPhysicianName, value);}
		}
		public string OverreadingPhysician
		{
			get {return _DICOMData.GetString(Tags.NameOfPhysicianReadingStudy);}
			set {if (value != null) _DICOMData.PutPN(Tags.NameOfPhysicianReadingStudy, value);}
		}
		public string TechnicianDescription
		{
			get {return _DICOMData.GetString(Tags.OperatorName);}
			set {if (value != null) _DICOMData.PutPN(Tags.OperatorName, value);}
		}
		public ushort SystolicBloodPressure
		{
			get {return 0;}
			set {}
		}
		public ushort DiastolicBloodPressure
		{
			get {return 0;}
			set {}
		}
		public Drug[] Drugs
		{
			get {return null;}
			set {}
		}
		public string[] ReferralIndication
		{
			get {return null;}
			set {}
		}
		public string RoomDescription
		{
			get {return null;}
			set {}
		}
		public byte StatCode
		{
			get {return 0xff;}
			set {}
		}
		#endregion

		#region IDiagnostic Members

		public int getDiagnosticStatements(out Statements stat)
		{
			stat = null;

			DcmElement element = _DICOMData.Get(Tags.AnnotationSeq);

			int annotationGroupNumber = -1;
			
			if (element != null)
			{
				ArrayList al = new ArrayList();

				int nr = element.vm();
				for (int i=0;i < nr;i++)
				{
					Dataset ds = element.GetItem(i);

					string line = ds.GetString(Tags.UnformattedTextValue);

					try
					{
						int currentGroupNumber = ds.GetInteger(Tags.AnnotationGroupNumber);

						if ((line != null)
						&&	(annotationGroupNumber < currentGroupNumber))
						{
							annotationGroupNumber = currentGroupNumber;
							al.Clear();
						}

						if (line == null)
							line = "";

						if (annotationGroupNumber == currentGroupNumber)
						{
							al.Add(line);
						}
					}
					catch
					{
					}
				}

				if (al.Count > 0)
				{
					stat = new Statements();

					if (Regex.IsMatch((string) al[al.Count-1], "(UN)?(CONFIRMED REPORT)", RegexOptions.None))
					{
						string temp = (string) al[al.Count-1];

						stat.confirmed = temp.StartsWith("CONF");

						al.RemoveAt(al.Count-1);
					}

					stat.time = _DICOMData.GetDate(stat.confirmed ? Tags.VerificationDateTime : Tags.ObservationDateTime);

					if ((OverreadingPhysician == null)
					||  (stat.time.Year <= 1000))
						stat.time = TimeAcquisition;

					stat.statement = new string[al.Count];

					for (int i=0;i < stat.statement.Length;i++)
						stat.statement[i] = (string) al[i];

					return 0;
				}
			}

			return 1;
		}

		public int setDiagnosticStatements(Statements stat)
		{
			if ((stat != null)
			&&  (stat.time.Year > 1000)
			&&  (stat.statement != null)
			&&  (stat.statement.Length > 0))
			{
				DcmElement element = _DICOMData.Get(Tags.AnnotationSeq);

				int annotationGroupNumber = 0;
			
				if (element == null)
				{
					element = _DICOMData.PutSQ(Tags.AnnotationSeq);
				}
				else
				{
					int nr = element.vm();
					for (int i=0;i < nr;i++)
					{
						Dataset ds = element.GetItem(i);

						try
						{
							int temp = ds.GetInteger(Tags.AnnotationGroupNumber);

							if (annotationGroupNumber <= temp)
								annotationGroupNumber = temp + 1;
						}
						catch
						{
						}
					}
				}

				foreach (string line in stat.statement)
				{
					Dataset ds = element.AddNewItem();

					ds.PutUS(Tags.RefWaveformChannels, s_MeasurementRWC);
					ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
					ds.PutST(Tags.UnformattedTextValue, line);
				}

				if (OverreadingPhysician != null)
				{
					_DICOMData.PutDT(stat.confirmed ? Tags.VerificationDateTime : Tags.ObservationDateTime, stat.time);
				}

				if (stat.confirmed)
				{
					Dataset ds = element.AddNewItem();

					ds.PutUS(Tags.RefWaveformChannels, s_MeasurementRWC);
					ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
					ds.PutST(Tags.UnformattedTextValue, "CONFIRMED REPORT");
				}
				else
				{
					Dataset ds = element.AddNewItem();

					ds.PutUS(Tags.RefWaveformChannels, s_MeasurementRWC);
					ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
					ds.PutST(Tags.UnformattedTextValue, "UNCONFIRMED REPORT");
				}

				return 0;
			}

			return 1;
		}

		#endregion

		#region IGlobalMeasurement Members

		private static int[] s_MeasurementRWC = {1, 0};

		private static string[,] s_AvgRRPPItems = {{"5.10.2.1-3", "5.10.2.1-5", "5.10.2.5-5", "5.10.2.5-1", "5.13.5-7", "5.13.5-9", "5.13.5-11"}, {"RR Interval", "PP Interval", "QTc Interval", "Vent Rate", "PR Interval", "QRS Duration", "QT Interval"}};
		private static string[,] s_AvgRRPPUnits = {{"ms", "ms", "ms", "/min", "ms", "ms", "ms"}, {"milliseconds", "milliseconds", "milliseconds", "heartbeat per minute", "milliseconds", "milliseconds", "milliseconds"}};

		private static string[,] s_MeasurementItems = {{"5.10.3-1", "5.10.3-2", "5.10.3-3", "5.10.3-4", "5.10.3-5", "5.10.3-11", "5.10.3-13", "5.10.3-15"}, {"P onset", "P offset", "QRS onset", "QRS offset", "T offset", "P Axis", "QRS Axis", "T Axis"}};
		private static string[,] s_MeasurementUnits = {{"ms", "ms", "ms", "ms", "ms", "deg", "deg", "deg"}, {"milliseconds", "milliseconds", "milliseconds", "milliseconds", "milliseconds", "degrees", "degrees", "degrees"}};
		private static string[,] s_MeasurementUnitsPoints = {{"POINT", "POINT", "POINT", "POINT", "POINT", "deg", "deg", "deg"}, {null, null, null, null, null, "degrees", "degrees", "degrees"}};

		public int getGlobalMeasurements(out GlobalMeasurements mes)
		{
			mes = null;

			string[,] resultAvgRR_PP = GetValues(_DICOMData.Get(Tags.AnnotationSeq), s_AvgRRPPItems, s_AvgRRPPUnits, s_MeasurementRWC);
			string[,] resultMeasurements = GetValues(_DICOMData.Get(Tags.AnnotationSeq), s_MeasurementItems, s_MeasurementUnits, s_MeasurementRWC, true);

			float factor = 1.0f;

			if (resultAvgRR_PP != null)
			{
				string[,] temp1 = GetValues(_DICOMData.Get(Tags.AnnotationSeq), s_MeasurementItems, s_MeasurementUnitsPoints, s_MeasurementRWC, true);

				if ((temp1 != null)
				&&	((resultMeasurements == null)
				||	 (resultMeasurements.Length < temp1.Length)
				||	 (calcNrOfValues(resultMeasurements) < calcNrOfValues(temp1))))
				{
					DcmElement temp2 = _DICOMData.Get(Tags.WaveformSeq);

					if ((temp2 != null)
					&&	(temp2.vm() >= 1))
					{
						try
						{
							factor = 1000.0f / ParseInt(temp2.GetItem(0).GetString(Tags.SamplingFrequency));
						}
						catch {}
					}

					resultMeasurements = temp1;
				}
			}

			if ((resultAvgRR_PP != null)
			&&	(resultMeasurements != null))
			{
				try
				{
					int spbm = StartPerBeatMeasurement;

					mes = new GlobalMeasurements();

					for (int n=0;n < resultAvgRR_PP.GetLength(0);n++)
					{
						if ((resultAvgRR_PP[n, 7] != null)
						&&	(int.Parse(resultAvgRR_PP[n, 7]) >= spbm))
							continue;

						if (resultAvgRR_PP[n, 0] != null)
							mes.AvgRR = ParseUShort(resultAvgRR_PP[n, 0]);

						if (resultAvgRR_PP[n, 1] != null)
							mes.AvgPP = ParseUShort(resultAvgRR_PP[n, 1]);

						if (resultAvgRR_PP[n, 2] != null)
							mes.QTc = ParseUShort(resultAvgRR_PP[n, 2]);

						if (resultAvgRR_PP[n, 3] != null)
							mes.VentRate = ParseUShort(resultAvgRR_PP[n, 3]);
					}

					int end1 = resultMeasurements.GetLength(0),
						end2 = resultMeasurements.GetLength(1) - 1;
					System.Reflection.FieldInfo[] fi = typeof(GlobalMeasurement).GetFields(System.Reflection.BindingFlags.Public | BindingFlags.Instance);

					if ((end1 > 0)
					&&	(end2 == fi.Length))
					{
						GlobalMeasurement firstMes = ((mes.measurment != null) && (mes.measurment.Length == 1)) ? mes.measurment[0] : null;
						int k = 0;

						mes.measurment = new GlobalMeasurement[end1];

						if (firstMes != null)
							mes.measurment[0] = firstMes;

						for (int i=0;i < end1;i++)
						{
							if ((resultMeasurements[i, end2] != null)
							&&	(int.Parse(resultMeasurements[i, end2]) >= spbm))
								k++;

							if (mes.measurment[k] == null)
								mes.measurment[k] = new GlobalMeasurement();
							
							for (int j=0;j < end2;j++)
							{
								if (resultMeasurements[i,j] != null)
								{
									int temp = ParseInt(resultMeasurements[i, j]);

									if (fi[j].FieldType == typeof(ushort))
									{
										if ((temp >= ushort.MinValue)
										&&	(temp <= ushort.MaxValue))
											fi[j].SetValue(mes.measurment[k], (ushort)(temp * factor));
									}
									else if (fi[j].FieldType == typeof(short))
									{
										if ((temp >= short.MinValue)
										&&	(temp <= short.MaxValue))
											fi[j].SetValue(mes.measurment[k], (short)temp);
									}
									else
									{
										throw new Exception("Error by developer!");
									}
								}
							}
						}

						GlobalMeasurement[] tempMes = mes.measurment;

						if ((tempMes != null)
						&&  ((tempMes.Length != (k+1))
						||   (tempMes[0] == null)))
						{
							if (tempMes[k] != null)
								k++;
							if (tempMes[0] == null)
								k--;

							mes.measurment = new GlobalMeasurement[k];
							for (int i=0,j=0;(i < tempMes.Length)&&(j < k);i++)
							{
								if (tempMes[i] != null)
								{
									mes.measurment[j++] = tempMes[i];
								}
							}
						}
					}

					for (int n=resultAvgRR_PP.GetLength(0)-1;n >= 0;n--)
					{
						if ((resultAvgRR_PP[n, 7] != null)
						&&	(int.Parse(resultAvgRR_PP[n, 7]) >= spbm))
							continue;

						if ((resultAvgRR_PP[n, 4] != null)
						&&	(mes.PRint == GlobalMeasurement.NoValue))
							mes.PRint = ParseUShort(resultAvgRR_PP[n, 4]);

						if ((resultAvgRR_PP[n, 5] != null)
						&&	(mes.QRSdur == GlobalMeasurement.NoValue))
							mes.QRSdur = ParseUShort(resultAvgRR_PP[n, 5]);
						
						if ((resultAvgRR_PP[n, 6] != null)
						&&	(mes.QTdur == GlobalMeasurement.NoValue))
							mes.QTdur = ParseUShort(resultAvgRR_PP[n, 6]);
					}

					return 0;
				}
				catch
				{
					mes = null;
				}
			}

			return 1;
		}

		public int setGlobalMeasurements(GlobalMeasurements mes)
		{
			if ((mes != null)
			&&	(mes.measurment != null))
			{
				DcmElement element = _DICOMData.Get(Tags.AnnotationSeq);

				int annotationGroupNumber = 0;
			
				if (element == null)
				{
					element = _DICOMData.PutSQ(Tags.AnnotationSeq);
				}
				else
				{
					int nr = element.vm();
					for (int i=0;i < nr;i++)
					{
						Dataset ds = element.GetItem(i);

						try
						{
							int temp = ds.GetInteger(Tags.AnnotationGroupNumber);

							if (annotationGroupNumber <= temp)
								annotationGroupNumber = temp + 1;
						}
						catch
						{
						}
					}
				}

				if (mes.AvgRR != GlobalMeasurement.NoValue)
				{
					Dataset ds = element.AddNewItem();

					MakeCodeSequence(ds, Tags.MeasurementUnitsCodeSeq, s_AvgRRPPUnits[0, 0], "UCUM", "1.4", s_AvgRRPPUnits[1, 0]);
					MakeCodeSequence(ds, Tags.ConceptNameCodeSeq, s_AvgRRPPItems[0, 0], "SCPECG", "1.3", s_AvgRRPPItems[1, 0]);

					ds.PutUS(Tags.RefWaveformChannels, s_MeasurementRWC);
					ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
					ds.PutDS(Tags.NumericValue, mes.AvgRR);
				}

				if (mes.AvgPP != GlobalMeasurement.NoValue)
				{
					Dataset ds = element.AddNewItem();

					MakeCodeSequence(ds, Tags.MeasurementUnitsCodeSeq, s_AvgRRPPUnits[0, 1], "UCUM", "1.4", s_AvgRRPPUnits[1, 1]);
					MakeCodeSequence(ds, Tags.ConceptNameCodeSeq, s_AvgRRPPItems[0, 1], "SCPECG", "1.3", s_AvgRRPPItems[1, 1]);

					ds.PutUS(Tags.RefWaveformChannels, s_MeasurementRWC);
					ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
					ds.PutDS(Tags.NumericValue, mes.AvgPP);
				}

				if (mes.PRint != GlobalMeasurement.NoValue)
				{
					Dataset ds = element.AddNewItem();

					MakeCodeSequence(ds, Tags.MeasurementUnitsCodeSeq, s_AvgRRPPUnits[0, 4], "UCUM", "1.4", s_AvgRRPPUnits[1, 4]);
					MakeCodeSequence(ds, Tags.ConceptNameCodeSeq, s_AvgRRPPItems[0, 4], "SCPECG", "1.3", s_AvgRRPPItems[1, 4]);
					
					ds.PutUS(Tags.RefWaveformChannels, s_MeasurementRWC);
					ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
					ds.PutDS(Tags.NumericValue, mes.PRint);
				}

				if (mes.QRSdur != GlobalMeasurement.NoValue)
				{
					Dataset ds = element.AddNewItem();

					MakeCodeSequence(ds, Tags.MeasurementUnitsCodeSeq, s_AvgRRPPUnits[0, 5], "UCUM", "1.4", s_AvgRRPPUnits[1, 5]);
					MakeCodeSequence(ds, Tags.ConceptNameCodeSeq, s_AvgRRPPItems[0, 5], "SCPECG", "1.3", s_AvgRRPPItems[1, 5]);
					
					ds.PutUS(Tags.RefWaveformChannels, s_MeasurementRWC);
					ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
					ds.PutDS(Tags.NumericValue, mes.QRSdur);
				}

				if (mes.QTdur != GlobalMeasurement.NoValue)
				{
					Dataset ds = element.AddNewItem();

					MakeCodeSequence(ds, Tags.MeasurementUnitsCodeSeq, s_AvgRRPPUnits[0, 6], "UCUM", "1.4", s_AvgRRPPUnits[1, 6]);
					MakeCodeSequence(ds, Tags.ConceptNameCodeSeq, s_AvgRRPPItems[0, 6], "SCPECG", "1.3", s_AvgRRPPItems[1, 6]);
					
					ds.PutUS(Tags.RefWaveformChannels, s_MeasurementRWC);
					ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
					ds.PutDS(Tags.NumericValue, mes.QTdur);
				}

				if (mes.QTc != GlobalMeasurement.NoValue)
				{
					Dataset ds = element.AddNewItem();

					MakeCodeSequence(ds, Tags.MeasurementUnitsCodeSeq, "ms", "UCUM", "1.4", "milliseconds");
					MakeCodeSequence(ds, Tags.ConceptNameCodeSeq, "5.10.2.5-5", "SCPECG", "1.3", "QTc Interval");
					
					ds.PutUS(Tags.RefWaveformChannels, s_MeasurementRWC);
					ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
					ds.PutDS(Tags.NumericValue, mes.QTc);
				}

				if (mes.measurment.Length > 0)
				{
					int MortaraCompat = 0;

					try
					{
						MortaraCompat = ParseInt(_DICOMData.Get(Tags.WaveformSeq).GetItem(0).GetString(Tags.SamplingFrequency));
					}
					catch {}
				
					if (MortaraCompat != 0)
					{
						if (mes.measurment[0].Paxis != GlobalMeasurement.NoAxisValue)
						{
							Dataset ds = element.AddNewItem();

							MakeCodeSequence(ds, Tags.MeasurementUnitsCodeSeq, "ms", "UCUM", "1.4", "milliseconds");
							MakeCodeSequence(ds, Tags.ConceptNameCodeSeq, "5.10.3-11", "SCPECG", "1.3", "P Axis");

							ds.PutUS(Tags.RefWaveformChannels, s_MeasurementRWC);
							ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
							ds.PutDS(Tags.NumericValue, mes.measurment[0].Paxis);
						}

						if (mes.measurment[0].QRSaxis != GlobalMeasurement.NoAxisValue)
						{
							Dataset ds = element.AddNewItem();

							MakeCodeSequence(ds, Tags.MeasurementUnitsCodeSeq, "ms", "UCUM", "1.4", "milliseconds");
							MakeCodeSequence(ds, Tags.ConceptNameCodeSeq, "5.10.3-13", "SCPECG", "1.3", "QRS Axis");

							ds.PutUS(Tags.RefWaveformChannels, s_MeasurementRWC);
							ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
							ds.PutDS(Tags.NumericValue, mes.measurment[0].QRSaxis);
						}

						if (mes.measurment[0].Taxis != GlobalMeasurement.NoAxisValue)
						{
							Dataset ds = element.AddNewItem();

							MakeCodeSequence(ds, Tags.MeasurementUnitsCodeSeq, "ms", "UCUM", "1.4", "milliseconds");
							MakeCodeSequence(ds, Tags.ConceptNameCodeSeq, "5.10.3-15", "SCPECG", "1.3", "T Axis");

							ds.PutUS(Tags.RefWaveformChannels, s_MeasurementRWC);
							ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
							ds.PutDS(Tags.NumericValue, mes.measurment[0].Taxis);
						}

						annotationGroupNumber++;

						int[] tempRWC = s_MeasurementRWC;

						if (mes.measurment[0].Ponset != GlobalMeasurement.NoValue)
						{
							Dataset ds = element.AddNewItem();

							MakeCodeSequence(ds, Tags.ConceptNameCodeSeq, s_MeasurementItems[0, 0], "SCPECG", "1.3", s_MeasurementItems[1, 0]);

							ds.PutUS(Tags.RefWaveformChannels, tempRWC);
							ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);

							ds.PutCS(Tags.TemporalRangeType, s_MeasurementUnitsPoints[0, 0]);
							ds.PutUL(Tags.RefSamplePositions, (mes.measurment[0].Ponset * MortaraCompat) / 1000);
						}

						if (mes.measurment[0].Poffset != GlobalMeasurement.NoValue)
						{
							Dataset ds = element.AddNewItem();

							MakeCodeSequence(ds, Tags.ConceptNameCodeSeq, s_MeasurementItems[0, 1], "SCPECG", "1.3", s_MeasurementItems[1, 1]);

							ds.PutUS(Tags.RefWaveformChannels, tempRWC);
							ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
							
							ds.PutCS(Tags.TemporalRangeType, s_MeasurementUnitsPoints[0, 1]);
							ds.PutUL(Tags.RefSamplePositions, (mes.measurment[0].Poffset * MortaraCompat) / 1000);
						}

						if (mes.measurment[0].QRSonset != GlobalMeasurement.NoValue)
						{
							Dataset ds = element.AddNewItem();

							MakeCodeSequence(ds, Tags.ConceptNameCodeSeq, s_MeasurementItems[0, 2], "SCPECG", "1.3", s_MeasurementItems[1, 2]);

							ds.PutUS(Tags.RefWaveformChannels, tempRWC);
							ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
							
							ds.PutCS(Tags.TemporalRangeType, s_MeasurementUnitsPoints[0, 2]);
							ds.PutUL(Tags.RefSamplePositions, (mes.measurment[0].QRSonset * MortaraCompat) / 1000);
						}

						if (_FiducialPoint != 0)
						{
							ushort temp = (ushort) ((mes.measurment[0].QRSonset + mes.measurment[0].QRSoffset) >> 1);

							Dataset ds = element.AddNewItem();

							MakeCodeSequence(ds, Tags.ConceptNameCodeSeq, "5.7.1-3", "SCPECG", "1.3", "Fiducial Point");

							ds.PutUS(Tags.RefWaveformChannels, tempRWC);
							ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
							
							ds.PutCS(Tags.TemporalRangeType, "POINT");
							ds.PutUL(Tags.RefSamplePositions, _FiducialPoint);
						}

						if (mes.measurment[0].QRSoffset != GlobalMeasurement.NoValue)
						{
							Dataset ds = element.AddNewItem();

							MakeCodeSequence(ds, Tags.ConceptNameCodeSeq, s_MeasurementItems[0, 3], "SCPECG", "1.3", s_MeasurementItems[1, 3]);

							ds.PutUS(Tags.RefWaveformChannels, tempRWC);
							ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
							
							ds.PutCS(Tags.TemporalRangeType, s_MeasurementUnitsPoints[0, 3]);
							ds.PutUL(Tags.RefSamplePositions, (mes.measurment[0].QRSoffset * MortaraCompat) / 1000);
						}

						if (mes.measurment[0].Toffset != GlobalMeasurement.NoValue)
						{
							Dataset ds = element.AddNewItem();

							MakeCodeSequence(ds, Tags.ConceptNameCodeSeq, s_MeasurementItems[0, 4], "SCPECG", "1.3", s_MeasurementItems[1, 4]);

							ds.PutUS(Tags.RefWaveformChannels, tempRWC);
							ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
							
							ds.PutCS(Tags.TemporalRangeType, s_MeasurementUnitsPoints[0, 4]);
							ds.PutUL(Tags.RefSamplePositions, (mes.measurment[0].Toffset * MortaraCompat) / 1000);
						}

						annotationGroupNumber = StartPerBeatMeasurement;
					}

					System.Reflection.FieldInfo[] fi = mes.measurment[0].GetType().GetFields(System.Reflection.BindingFlags.Public | BindingFlags.Instance);

                    int end = s_MeasurementItems.GetLength(1);

					if (end != fi.Length)
						return 2;

					string[,] tempMeasurementUnits = (MortaraCompat == 0) ? s_MeasurementUnits : s_MeasurementUnitsPoints;

					for (int i=(MortaraCompat == 0) ? 0 : 1;i < mes.measurment.Length;i++)
					{
						int[] tempRWC = (i == 0) ? new int[]{2, 0} : s_MeasurementRWC;

						for (int j=0;j < end;j++)
						{
							Dataset ds = element.AddNewItem();

                            int val = ECGConversion.ECGGlobalMeasurements.GlobalMeasurement.NoValue;
                            object tempVal = fi[j].GetValue(mes.measurment[i]);

							if (tempVal.GetType() == typeof(ushort))
								val = (ushort)tempVal;
							else if (tempVal.GetType() == typeof(short))
							{
								if (MortaraCompat != 0)
									continue;

								val = (short)tempVal;
							}
							else if (tempVal.GetType() == typeof(int))
								val = (int)tempVal;

							if (val != GlobalMeasurement.NoValue)
							{
								if (tempMeasurementUnits[1, j] != null)
								{
									MakeCodeSequence(ds, Tags.MeasurementUnitsCodeSeq, tempMeasurementUnits[0, j], "UCUM", "1.4", tempMeasurementUnits[1, j]);
									ds.PutDS(Tags.NumericValue, val);
								}
								else
								{
									ds.PutCS(Tags.TemporalRangeType, tempMeasurementUnits[0, j]);
									ds.PutUL(Tags.RefSamplePositions, (MortaraCompat == 0) || (MortaraCompat == 1000) ? val : (val * MortaraCompat) / 1000);
								}

								MakeCodeSequence(ds, Tags.ConceptNameCodeSeq, s_MeasurementItems[0, j], "SCPECG", "1.3", s_MeasurementItems[1, j]);

								ds.PutUS(Tags.RefWaveformChannels, tempRWC);
								ds.PutUS(Tags.AnnotationGroupNumber, annotationGroupNumber);
							}
						}

						annotationGroupNumber++;
					}
				}

				return 0;
			}

			return 1;
		}

		#endregion

		public static int calcNrOfValues(string[,] vals)
		{
			int nr = 0;

			if (vals != null)
			{
				int end1 = vals.GetLength(0),
					end2 = vals.GetLength(1);

				for (int i=0;i < end1;i++)
					for (int j=0;j < end2;j++)
						if (vals[i,j] != null)
							nr++;
			}

			return nr;
		}

		public static string[,] GetValues(DcmElement element, string[,] items, string[,] units, int[] rwc)
		{
			return GetValues(element, items, units, rwc, false);
		}

		public static string[,] GetValues(DcmElement element, string[,] items, string[,] units, int[] rwc, bool special)
		{
			if ((element != null)
			&&	(items != null)
			&&	(units != null)
			&&	(items.Length == units.Length))
			{
				int end = element.vm();

				SortedList sl = new SortedList();

				for (int i=0;i < end;i++)
				{
					Dataset ds = element.GetItem(i);

					string itemName;

					if (IsCodeSequence(ds, Tags.ConceptNameCodeSeq, out itemName, "SCPECG", "1.3"))
					{
						int nr = 0;

						for (;nr < items.GetLength(1);nr++)
							if (string.Compare(items[0, nr], itemName) == 0)
								break;

						if (nr == items.GetLength(1))
							continue;

						string val = null;
						int groupNr = ds.GetInteger(Tags.AnnotationGroupNumber);

						if (units[1, nr] == null)
						{
							if (units[0, nr] == ds.GetString(Tags.TemporalRangeType))
								val = ds.GetString(Tags.RefSamplePositions);
						}
						else if (IsCodeSequence(ds, Tags.MeasurementUnitsCodeSeq, out val, "UCUM", "1.4"))
						{
							val = ds.GetString(Tags.NumericValue);
						}

						int[] tempRWC = ds.GetInts(Tags.RefWaveformChannels);

						if ((val != null)
						&&	(groupNr >= 0)
						&&	(tempRWC.Length == rwc.Length))
						{
							for (int k=0;k < rwc.Length;k++)
								if (tempRWC[k] != rwc[k])
									continue;

							string[] temp = null;

							if (sl.Contains(groupNr))
								temp = (string[]) sl[groupNr];
							else
							{
								temp = new string[items.GetLength(1)];

								sl.Add(groupNr, temp);
							}

							temp[nr] = val;
						}
					}
				}

				if (sl.Count > 0)
				{
					if (special
					&&	(sl.Count > 1)
					&&	(items.GetLength(1) < 64))
					{
						int temp1 = items.GetLength(1);
						ulong a = 0, b = 0; 

						string[] temp2 = (string[]) sl.GetByIndex(0);
						for (int i=0;i < temp1;i++)
							if (temp2[i] != null)
								a |= (1UL << i);

						string[] temp3 = (string[]) sl.GetByIndex(1);
						for (int i=0;i < temp1;i++)
							if (temp3[i] != null)
								b |= (1UL << i);

						if ((a ^ b) == (a + b))
						{
							for (int i=0;i < temp1;i++)
								if (temp2[i] != null)
									temp3[i] = temp2[i];

							sl.RemoveAt(0);
						}
					}

					string[,] ret = new string[sl.Count, items.GetLength(1)+1];

					for (int i=0;i < sl.Count;i++)
					{
						string[] temp = (string[]) sl.GetByIndex(i);
						int j=0;

						for (;j < temp.Length;j++)
							ret[i, j] = temp[j];

						ret[i, j] = sl.GetKey(i).ToString();
					}

					return ret;
				}
			}

			return null;
		}

		public float GetFilter(Dataset ds, uint tag)
		{
			string str = null;

			DcmElement ele = ds.Get(Tags.ChannelDefInitionSeq);

			for (int i=0;i < ele.vm();i++)
			{
				string temp = ele.GetItem(i).GetString(tag);

				if (str == null)
					str = temp;

				if (str == null)
					return float.NaN;;

				if (string.Compare(str, temp, true) != 0)
					return float.NaN;
			}

			return ParseFloat(str);
		}

		public int GetWaveform(Signals sigs, DcmElement chDef, int[] data, int nrSamples, bool median)
		{
			if ((sigs == null)
			||	(chDef == null)
			||	(data == null)
			||	(sigs.NrLeads != chDef.vm()))
				return 1;

			if (nrSamples <= 0)
				return 0;

			byte nrLeads = sigs.NrLeads;

			if ((nrLeads * nrSamples) > data.Length)
				return 2;

			try
			{
				for (int i=0;i < nrLeads;i++)
				{
					Dataset ds = chDef.GetItem(i);

					double val, baseline, skew;
					LeadType type;

					GetChannelInfo(ds, out type, out val, out baseline, out skew);

					if ((val == 0.0)
					&&	(skew != 0.0))
						return 4;

					if (i == 0)
					{
						if (median)
							sigs.MedianAVM = val;
						else
							sigs.RhythmAVM = val;
					}

					if (sigs[i] == null)
					{
						sigs[i] = new Signal();
						sigs[i].Type = type;
					}

					if (sigs[i].Type != type)
						return 5;

					short[] values = new short[nrSamples];
					int pos = i;

					for (int j=0;j < nrSamples;j++,pos+=nrLeads)
						values[j] = (short) data[pos];

					ECGTool.ChangeMultiplier(values, val, (median ? sigs.MedianAVM : sigs.RhythmAVM));

					if (median)
					{
						sigs[i].Median = values;
					}
					else
					{
						sigs[i].Rhythm = values;
						sigs[i].RhythmStart = 0;
						sigs[i].RhythmEnd = nrSamples-1;
					}
				}
			}
			catch
			{
				return 3;
			}

			return 0;
		}

		public int SetWaveform(Dataset ds, Signals sigs, bool median)
		{
			try
			{
				byte nrLeads = sigs.NrLeads;
				int start, end, ret = 0;

				if (median)
				{
					start = 0;
					end = (sigs.MedianLength * sigs.MedianSamplesPerSecond) / 1000;
				}
				else
				{
					sigs.CalculateStartAndEnd(out start, out end);
				}

				int len = end - start;

				if (_MortaraCompat)
				{
					len = (median ? 1200 : 10000); 

					len = ((sigs.RhythmSamplesPerSecond * len) / 1000);
				}
				
				ds.PutDS(Tags.MultiplexGroupTimeOffset, 0);
				ds.PutDS(Tags.TriggerTimeOffset, 0);

				ds.PutCS(Tags.WaveformOriginality, (median ? "DERIVED" : "ORIGINAL"));
				ds.PutUS(Tags.NumberOfWaveformChannels, nrLeads);
				ds.PutUL(Tags.NumberOfWaveformSamples, len);
				ds.PutDS(Tags.SamplingFrequency, median ? sigs.MedianSamplesPerSecond : sigs.RhythmSamplesPerSecond);
				ds.PutSH(Tags.MultiplexGroupLabel, (median ? "MEDIAN BEAT" : "RHYTHM"));

				ret = SetChannelInfo(ds.PutSQ(Tags.ChannelDefInitionSeq), sigs, median);

				if (ret != 0)
					return ret > 0 ? 1 + ret : ret;

				ds.PutUS(Tags.WaveformBitsAllocated, 16);
				ds.PutCS(Tags.WaveformSampleInterpretation, "SS");

				if (median)
					ds.PutUL(Tags.TriggerSamplePosition, sigs.MedianFiducialPoint);

				if (!median)
					ds.PutOW(Tags.WaveformPaddingValue, new short[]{(_MortaraCompat ? (short) 0 : short.MinValue)});

				short[][] temp = new short[nrLeads][];

				if (median)
				{
					for (int n=0;n < nrLeads;n++)
					{
						if (sigs[n].Median.Length == len)
						{
							temp[n] = sigs[n].Median;
						}
						else
						{
							Signal tempSig = sigs[n];

							temp[n] = new short[len];

							for (int i=0;i < len;i++)
							{
								temp[n][i] = (i < tempSig.Median.Length)
									?	tempSig.Median[i]
									:	short.MinValue;
							}
						}
					}
				}
				else
				{
					for (int n=0;n < nrLeads;n++)
					{
						if ((sigs[n].RhythmStart == start)
						&&	(sigs[n].RhythmEnd == end)
						&&	(sigs[n].Rhythm.Length == len))
						{
							temp[n] = sigs[n].Rhythm;
						}
						else
						{
							Signal tempSig = sigs[n];

							temp[n] = new short[len];

							for (int i=0,j=start;(j < tempSig.RhythmEnd) && (j < len);i++,j++)
							{
								temp[n][i] = (j >= tempSig.RhythmStart)
									?	tempSig.Rhythm[j - tempSig.RhythmStart]
									:	short.MinValue;
							}
						}
					}
				}

				ds.PutOW(Tags.WaveformData, temp);
			}
			catch
			{
				return 1;
			}

			return 0;
		}

		public void GetChannelInfo(Dataset ds, out LeadType type, out double val, out double baseline, out double skew)
		{
			val = 1.0;
			type = LeadType.Unknown;
			baseline = 0.0;
			skew = 0.0;

			try
			{
				if (ParseFloat(ds.GetString(Tags.ChannelSensitivityCorrectionFactor)) != 1.0f)
					return;
			}
			catch
			{
				return;
			}

			try
			{
				DcmElement el = ds.Get(Tags.ChannelSensitivityUnitsSeq);

				switch (el.GetItem(0).GetString(Tags.CodeValue))
				{
					case "MV":
						val = 1000000000000.0;
						break;
					case "kV":
						val = 1000000000.0;
						break;
					case "V":
						val = 1000000.0;
						break;
					case "dV":
						val = 100000.0;
						break;
					case "cV":
						val = 10000.0;
						break;
					case "mV":
						val = 1000.0;
						break;
					case "uV":
						val = 1.0;
						break;
				}

				val *= ParseDouble(ds.GetString(Tags.ChannelSensitivity));
			}
			catch
			{
				val = 0.0f;
			}

			try
			{
				baseline = ParseDouble(ds.GetString(Tags.ChannelBaseline));
			} catch {}

			try
			{
				skew = ParseDouble(ds.GetString(Tags.ChannelSampleSkew));
			} catch {}

			try
			{
				Dataset ds2 = ds.Get(Tags.ChannelSourceSeq).GetItem(0);

				switch (ds2.GetString(Tags.CodingSchemeDesignator))
				{
					case "SCPECG":
					{
						string[] temp = ds2.GetString(Tags.CodeValue).Split('-');

						type = (LeadType) ParseInt(temp[temp.Length-1]);
					}
					break;
					case "MDC":
					{
						type = (LeadType) ECGConverter.EnumParse(typeof(LeadTypeVitalRefId), ds2.GetString(Tags.CodeValue), false);
					}
					break;
				}
			} catch {}
		}

		public int SetChannelInfo(DcmElement element, Signals sigs, bool median)
		{
			try
			{
				byte nrLeads = sigs.NrLeads;

				for (int i=0; i < nrLeads;i++)
				{
					Dataset ds = element.AddNewItem();

					MakeCodeSequence(ds, Tags.ChannelSourceSeq, "5.6.3-9-" + ((int)sigs[i].Type).ToString(), "SCPECG", "1.3", "Lead " + sigs[i].Type.ToString());

					ds.PutDS(Tags.ChannelSensitivity, (float) (median ? sigs.MedianAVM : sigs.RhythmAVM));

					MakeCodeSequence(ds, Tags.ChannelSensitivityUnitsSeq, "uV", "UCUM", "1.4", "microvolt");

					ds.PutDS(Tags.ChannelSensitivityCorrectionFactor, 1);
					ds.PutDS(Tags.ChannelBaseline, 0);
					ds.PutDS(Tags.ChannelSampleSkew, 0);
					ds.PutUS(Tags.WaveformBitsStored, 16);

					if (_LowpassFilter != 0)
						ds.PutDS(Tags.FilterHighFrequency, _LowpassFilter);

					if (_HighpassFilter != 0)
						ds.PutDS(Tags.FilterLowFrequency, (_HighpassFilter / 100.0f));

					if ((_FilterMap != 0)
					&&	((_FilterMap & 0x3) != 0x3)
					&&	((_FilterMap & 0x3) != 0x0))
						ds.PutDS(Tags.NotchFilterFrequency, ((_FilterMap & 0x1) == 0x1) ? 60.0f : 50.0f);
				}
			}
			catch
			{
				return 1;
			}

			return 0;
		}

		public static bool IsCodeSequence(Dataset ds, uint tag, out string codeValue, string codingSchemeDesignator, string codingSchemeVersion)
		{
			codeValue = null;

			DcmElement el = ds.Get(tag);

			if ((el != null)
			&&	(el.vm() == 1))
			{
				Dataset ds2 = el.GetItem(0);

				if ((ds2 != null)
				&&	(ds2.GetString(Tags.CodingSchemeDesignator) == codingSchemeDesignator)
				&&	((codingSchemeVersion == null)
				||	 (ds2.GetString(Tags.CodingSchemeVersion) == codingSchemeVersion)))
					codeValue = ds2.GetString(Tags.CodeValue);
			}

			return codeValue != null;
		}

		public static void MakeCodeSequence(Dataset ds, uint tag, string codeValue, string codingSchemeDesignator, string codingSchemeVersion, string codeMeaning)
		{
			DcmElement el =	ds.PutSQ(tag);
			ds = el.AddNewItem();

			ds.PutSH(Tags.CodeValue, codeValue);
			ds.PutSH(Tags.CodingSchemeDesignator, codingSchemeDesignator);
			ds.PutSH(Tags.CodingSchemeVersion, codingSchemeVersion);
			ds.PutLO(Tags.CodeMeaning, codeMeaning);
		}
	}
}