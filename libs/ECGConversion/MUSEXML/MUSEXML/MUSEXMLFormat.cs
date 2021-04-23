/***************************************************************************
Copyright 2012-2013, van Ettinger Information Technology, Lopik, The Netherlands

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
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

using ECGConversion.ECGDemographics;
using ECGConversion.ECGDiagnostic;
using ECGConversion.ECGGlobalMeasurements;
using ECGConversion.ECGSignals;

namespace ECGConversion.MUSEXML
{
	public class MUSEXMLFormat : IECGFormat, IDemographic, ISignal, IGlobalMeasurement, IDiagnostic
	{
		private const String _ConstTimeFormat = "HH:mm:ss";
		private const String _ConstDateFormat = "MM/dd/yyyy";
		private const double _MinAVM = 0.001;
		private const int _MinSPS = 5;
		private const int _MinLength = 1;

		/// <summary>
		/// Constructor for the MUSEXML format.
		/// </summary>
		public MUSEXMLFormat()
		{
			_Config = new ECGConfig(new string[]{"Excluded Leads", "Default AVM", "Default SPS", "Default Length", "Default Site", "Time Format", "Date Format"}, 4, new ECGConfig.CheckConfigFunction(this._ConfigurationWorks));

			_Config["Excluded Leads"] = "III, aVF, aVL, aVR";
			_Config["Default AVM"] = "4.88";
			_Config["Default SPS"] = "500";
			_Config["Default Length"] = "10";
			_Config["Default Site"] = "1";
			
			_Config["Time Format"] = _ConstTimeFormat;
			_Config["Date Format"] = _ConstDateFormat;
		}

		public bool _ConfigurationWorks()
		{
			LeadType[] lta = null;

			try
			{
				lta = _ExcludedLeads;
			}
			catch
			{
				return false;
			}

			return ((lta == null)
				||  (lta.Length > 0))
				&& (System.Text.RegularExpressions.Regex.IsMatch(_Config["Default AVM"], "[0-9]+(.[0-9]+)?"))
				&& (System.Text.RegularExpressions.Regex.IsMatch(_Config["Default SPS"], "[0-9]+"))
				&& (System.Text.RegularExpressions.Regex.IsMatch(_Config["Default Length"], "[0-9]+"));
		}

		private LeadType[] _ExcludedLeads
		{
			get
			{
				LeadType[] ret = null;

				string sEL = _Config["Excluded Leads"];

				if ((sEL != null)
				&&	(sEL.Length > 0))
				{
					string[] aEL = sEL.Split(',', ';', ':', '-');

					ret = new LeadType[aEL.Length];

					for (int i=0;i < ret.Length;i++)
					{
						ret[i] = (LeadType)(-1);

						if (aEL[i] != null)
						{
							aEL[i] = aEL[i].Trim ();

							if (aEL[i].Length > 0)
							{
								ret[i] = (LeadType) ECGConversion.ECGConverter.EnumParse(typeof(LeadType), aEL[i], true);
							}
						}
					}
				}

				return ret;
			}
		}

		private double _DefaultAVM
		{
			get
			{
				double ret = 0.0;

				try
				{
					ret = Double.Parse(_Config["Default AVM"], CultureInfo.InvariantCulture.NumberFormat);
				} catch {}

				return ret;
			}
		}

		private int _DefaultSPS
		{
			get
			{
				int ret = 0;

				try
				{
					ret = Int32.Parse(_Config["Default SPS"], CultureInfo.InvariantCulture.NumberFormat);
				} catch {}

				return ret;
			}
		}

		private int _DefaultLength
		{
			get
			{
				int ret = 0;

				try
				{
					ret = Int32.Parse(_Config["Default Length"], CultureInfo.InvariantCulture.NumberFormat);
				} catch {}

				return ret;
			}
		}

		private string _DefaultSite
		{
			get
			{
				return _Config["Default Site"];
			}
		}
		
		private string _TimeFormat
		{
			get
			{
				string ret = null;
				
				ret = _Config["Time Format"];
				
				if ((ret == null)
				||	(ret.Length == 0))
					ret = _ConstTimeFormat;
				
				return ret;
			}
		}
		
		private string _DateFormat
		{
			get
			{
				string ret = null;
				
				ret = _Config["Date Format"];
				
				if ((ret == null)
				||	(ret.Length == 0))
					ret = _ConstDateFormat;
				
				return ret;
			}
		}
		
		private DateTime ParseDateTime(string str)
		{
			string[] formats = new string[]{
				_TimeFormat + " " + _DateFormat,
				_ConstTimeFormat + " " + _ConstDateFormat,
				CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern + " " + CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern,
				CultureInfo.CurrentUICulture.DateTimeFormat.LongTimePattern + " " + CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern};
			
			return DateTime.ParseExact(str, formats, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None);
		}
		
		private DateTime ParseDate(string str)
		{
			string[] formats = new string[]{
				_DateFormat,
				_ConstDateFormat,
				CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern,
				CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern};
			
			return DateTime.ParseExact(str, formats, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.None);
		}
		
		/// <summary>
		/// The data contained in the MUSEXML file.
		/// </summary>
		private Schemas.RestingECG _Root;
		
		public Schemas.RestingECG Data
		{
			get
			{
				return _Root;
			}
			set
			{
				_Root = value;
			}
		}
		
		private Schemas.Waveform[] _Waveform
		{
			get
			{
				Schemas.Waveform[] ret = null;
				ArrayList list = null;
				
				if ((_Root != null)
				&&	(_Root.Items != null)
				&&	(_Root.Items.Length > 0))
				{
					list = new ArrayList(_Root.Items.Length);
					
					foreach (object wf in _Root.Items)
					{
						if ((wf != null)
						&&	(wf is Schemas.Waveform))
							list.Add((Schemas.Waveform) wf);
					}
				}
				
				if ((list != null)
				&&	(list.Count > 0))
				{
					ret = new Schemas.Waveform[list.Count];
					
					for (int i=0;i < ret.Length;i++)
						ret[i] = (Schemas.Waveform) list[i];
					
				}
				
				return ret;
			}
		}
		
		public override int Read(Stream input, int offset)
		{
			_Root = null;

			if (input != null)
			{	
				if (offset != input.Position)
				{
					if (!input.CanSeek)
						return 0x4;

					input.Seek(offset, SeekOrigin.Begin);
				}

				try
				{
					XmlSerializer serializer = new XmlSerializer(typeof(Schemas.RestingECG));

					object obj = serializer.Deserialize(input);
					
					if ((obj != null)
					&&	(obj is Schemas.RestingECG))
					{	
						_Root = (Schemas.RestingECG)obj;

						return 0;
					}

				}
				catch {}

				return 0x2;
			}
			
			
			return 0x1;
		}
		public override int Read(string file, int offset)
		{
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
		public override int Write(Stream output)
		{
			if ((output != null)
			&&  (output.CanWrite))
			{
				XmlTextWriter xw = null;

				try
				{
					Encoding enc = Encoding.GetEncoding("iso-8859-1");

					if (enc == null)
						enc = Encoding.UTF8;

					xw = new XmlTextWriter(output, enc);

					xw.Formatting = Formatting.Indented;

					xw.WriteStartDocument();
					xw.WriteDocType("RestingECG", null, "restecg.dtd", "");

					XmlSerializer serializer = new XmlSerializer(typeof(Schemas.RestingECG));
					XmlSerializerNamespaces emptyNs = new XmlSerializerNamespaces(new XmlQualifiedName[] { XmlQualifiedName.Empty });

					serializer.Serialize(xw, _Root, emptyNs);

					xw.WriteEndDocument();
				}
				catch
				{
					return 0x2;
				}
				finally
				{
					if (xw != null)
					{
						xw.Close();
					}
				}

				return 0x0;
			}
			return 0x1;
		}
		public override int Write(byte[] buffer, int offset)
		{
			MemoryStream ms = null;
			
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
		private bool CheckFormat(XmlReader reader)
		{
			try
			{
				while (true)
				{
					try
					{
						if (!reader.Read())
							break;
					} 
					catch (System.IO.FileNotFoundException) {}

					if (reader.NodeType == XmlNodeType.Element)
					{
						return (String.Compare(reader.Name, "RestingECG") == 0);
					}
				}
			}
			catch
			{
			}

			return false;
		}
		public override bool CheckFormat(Stream input, int offset)
		{
			if (!input.CanRead)
				return false;

			XmlTextReader reader = null;

			try
			{
				input.Seek(offset, SeekOrigin.Begin);

				reader = new XmlTextReader(input);

				return CheckFormat(reader);
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
		public override void Anonymous(byte type)
		{
			ECGTool.Anonymous(Demographics, (char) type);
		}
		public override int getFileSize()
		{
			return -1;
		}
		public override bool Works()
		{	
			if ((_Root != null)
			&&	(_Root.PatientDemographics != null)
			&&	(_Root.TestDemographics != null)
			&&	(_Root.Items != null)
			&&	(_Root.Items.Length > 0))
			{
				foreach (object wf in _Root.Items)
				{
					if ((wf == null)
					||  !(wf is Schemas.Waveform)
					||	!CheckWaveform((Schemas.Waveform)wf))
					{	
						return false;
					}
				}

				return true;
			}
			
			return false;
		}
		
		public static bool CheckWaveform (Schemas.Waveform wf)
		{
			try
			{
				if ((wf != null)
				&&	(wf.LeadData != null)) 
				{
					for (int i=0; i < wf.LeadData.Length; i++)
					{
						if (!CheckCRC(wf.LeadData[i]))
						{
							return false;
						}
					}

					return true;
				}
			}
			catch {}

			return false;
		}
		
		public static bool CheckCRC(Schemas.LeadData ld)
		{
			return CheckCRC(ld, null);
		}
				
		public static bool CheckCRC(Schemas.LeadData ld, byte[] wfd)
		{
			return GetCRC(ld, wfd) == ld.LeadDataCRC32;
		}

		public static uint GetCRC(Schemas.LeadData ld)
		{
			return GetCRC(ld, null);
		}
		
		public static uint GetCRC(Schemas.LeadData ld, byte[] wfd)
		{
			Communication.IO.Tools.CRCTool crc = new Communication.IO.Tools.CRCTool();

			crc.Init(Communication.IO.Tools.CRCTool.CRCCode.CRC32);
			
			if (wfd == null)
				wfd = GetData(ld.WaveFormData);
			
			return (uint) crc.crctablefast(wfd, 0, wfd.Length);
		}

		public void SetCRC(Schemas.LeadData ld)
		{
			SetCRC(ld, null);
		}
		
		public void SetCRC(Schemas.LeadData ld, byte[] wfd)
		{
			ld.LeadDataCRC32 = GetCRC(ld, wfd);
		}
		
		public static byte[] GetData(string wfData)
		{
			return Convert.FromBase64String(wfData);
		}
		
		public static string SetData(byte[] wfData)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(Convert.ToBase64String(wfData, 0, wfData.Length));

			for (int i=0;i < sb.Length;i+=84)
			{
				sb.Insert(i, "\r\n");
				i+=2;
			}

			sb.Append("\r\n         ");	
			
			return sb.ToString();
		}
		
		public override void Empty()
		{
			_Root = null;
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
		
		#region IDemographic Members

		public string ReferringPhysician
		{
			get
			{
				return ((_Root.TestDemographics.ReferringMDLastName != null)
					&&	(_Root.TestDemographics.ReferringMDLastName.Length != 0))
					?	_Root.TestDemographics.ReferringMDLastName
					:	null;
			}
			set
			{
				if (value != null)
					_Root.TestDemographics.ReferringMDLastName = value;
			}
		}

		public string PrefixName
		{
			get {return null;}
			set {}
		}

		public int setPatientWeight (ushort val, WeightDefinition def)
		{
			// TODO: improve?
			this._Root.PatientDemographics.Item1ElementName = Schemas.Item1ChoiceType.WeightKG;
			this._Root.PatientDemographics.Item1 = 0;

			if ((val != 0)
			&& (def != WeightDefinition.Unspecified))
			{
				switch (def)
				{
					case WeightDefinition.Gram:
						_Root.PatientDemographics.Item1ElementName = Schemas.Item1ChoiceType.WeightKG;
						_Root.PatientDemographics.Item1 = new Decimal(val / 1000.0);
						break;
					case WeightDefinition.Kilogram:
						_Root.PatientDemographics.Item1ElementName = Schemas.Item1ChoiceType.WeightKG;
						_Root.PatientDemographics.Item1 = val;
						break;
					case WeightDefinition.Ounce:
						_Root.PatientDemographics.Item1ElementName = Schemas.Item1ChoiceType.WeightLBS;
						_Root.PatientDemographics.Item1 = new decimal(val / 16.0);
						break;
					case WeightDefinition.Pound:
						_Root.PatientDemographics.Item1ElementName = Schemas.Item1ChoiceType.WeightLBS;
						_Root.PatientDemographics.Item1 = val;
						break;
				}
				
				return 0;
			}

			return 1;
		}

		public Drug[] Drugs
		{
			get {return null;}
			set {}
		}

		public string PatientID
		{
			get
			{
				return _Root.PatientDemographics.PatientID;
			}
			set
			{
				_Root.PatientDemographics.PatientID = (value != null) ? value : "";
			}
		}

		public string AnalyzingInstitution
		{
			get {return null;}
			set {}
		}

		public string OverreadingPhysician
		{
			get
			{
				return ((_Root.TestDemographics.OverreaderLastName != null)
					&&	(_Root.TestDemographics.OverreaderLastName.Length != 0))
					?	_Root.TestDemographics.OverreaderLastName
					:	null;
			}
			set
			{
				if (value != null)
					_Root.TestDemographics.OverreaderLastName = value;
			}
		}

		public string TechnicianDescription
		{
			get
			{
				return ((_Root.TestDemographics.AcquisitionTechID != null)
					&&	(_Root.TestDemographics.AcquisitionTechID.Length != 0))
					?	_Root.TestDemographics.AcquisitionTechID
					:	null;
			}
			set
			{
				if (value != null)
					_Root.TestDemographics.AcquisitionTechID = value;
			}
		}

		public int setPatientAge(ushort val, ECGConversion.ECGDemographics.AgeDefinition def)
		{
			return 1;
		}

		public string LastName
		{
			get
			{
				return ((_Root.PatientDemographics.PatientLastName != null)
					&&	(_Root.PatientDemographics.PatientLastName.Length != 0))
					?	_Root.PatientDemographics.PatientLastName
					:	null;
			}
			set
			{
				if (value != null)
					_Root.PatientDemographics.PatientLastName = value;
			}
		}

		public DateTime TimeAcquisition
		{
			get
			{	
				return ParseDateTime(_Root.TestDemographics.AcquisitionTime + " " + _Root.TestDemographics.AcquisitionDate);
			}
			set
			{
				_Root.TestDemographics.AcquisitionTime = value.ToString(_TimeFormat, CultureInfo.InvariantCulture.DateTimeFormat);
				_Root.TestDemographics.AcquisitionDate = value.ToString(_DateFormat, CultureInfo.InvariantCulture.DateTimeFormat);
			}
		}

		public AcquiringDeviceID AnalyzingMachineID
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public string RoomDescription
		{
			get
			{
				return ((_Root.TestDemographics.RoomID != null)
					&&	(_Root.TestDemographics.RoomID.Length != 0))
					?	_Root.TestDemographics.RoomID
					:	null;
			}
			set
			{
				if (value != null)
					_Root.TestDemographics.RoomID = value;
			}
		}

		public Date PatientBirthDate
		{
			get
			{
				Date ret = null;
				
				if (_Root.PatientDemographics.DateofBirth != null)
				{
					try
					{
						DateTime dt = ParseDate(_Root.PatientDemographics.DateofBirth);
						
						ret = new Date();
						ret.Year = (ushort) dt.Year;
						ret.Month = (byte) dt.Month;
						ret.Day = (byte) dt.Day;
						
						if (!ret.isExistingDate())
							ret = null;
					}
					catch
					{
						ret = null;
					}
				}
				
				return ret;
			}
			set
			{
				_Root.PatientDemographics.DateofBirth = "";
				
				if ((value != null)
				&&	(value.isExistingDate()))
				{
					try
					{
						DateTime dt = new DateTime(value.Year, value.Month, value.Day);
						
						_Root.PatientDemographics.DateofBirth = dt.ToString(_DateFormat, CultureInfo.InvariantCulture.DateTimeFormat);
					}
					catch {}
				}
			}
		}

		public ECGConversion.ECGDemographics.Sex Gender
		{
			get
			{
				ECGConversion.ECGDemographics.Sex ret = Sex.Null;
				
				if (_Root.PatientDemographics.Gender != null)
				{
					if (_Root.PatientDemographics.Gender.Length == 0)
						ret = ECGConversion.ECGDemographics.Sex.Unspecified;
					else
						ret = (ECGConversion.ECGDemographics.Sex) Enum.Parse(typeof(ECGConversion.ECGDemographics.Sex), _Root.PatientDemographics.Gender, true);	
				}
				
				return ret;
			}
			set
			{
				if (value == Sex.Null)
					_Root.PatientDemographics.Gender = null;
				else if (value == Sex.Unspecified)
					_Root.PatientDemographics.Gender = "";
				else
					_Root.PatientDemographics.Gender = value.ToString();
			}
		}

		public int setPatientHeight(ushort val, HeightDefinition def)
		{
			// TODO: improve?
			this._Root.PatientDemographics.ItemElementName = Schemas.ItemChoiceType.HeightCM;
			this._Root.PatientDemographics.Item = 0;

			if ((val != 0)
			&&	(def != HeightDefinition.Unspecified))
			{
				switch (def)
				{
					case HeightDefinition.Centimeters:
						_Root.PatientDemographics.ItemElementName = Schemas.ItemChoiceType.HeightCM;
						_Root.PatientDemographics.Item= val;
						break;
					case HeightDefinition.Millimeters:
						_Root.PatientDemographics.ItemElementName = Schemas.ItemChoiceType.HeightCM;
						_Root.PatientDemographics.Item = new Decimal(val / 10.0);
						break;
					case HeightDefinition.Inches:
						_Root.PatientDemographics.ItemElementName = Schemas.ItemChoiceType.HeightIN;
						_Root.PatientDemographics.Item = val;
						break;
				}
				
				return 0;
			}

			return 1;
		}

		public ushort DiastolicBloodPressure
		{
			get
			{
				if ((_Root.RestingECGMeasurements != null)
				&&  (_Root.RestingECGMeasurements.DiastolicBP != null)
				&&	(_Root.RestingECGMeasurements.DiastolicBP.Length > 0))
				{
					try
					{
						return UInt16.Parse(_Root.RestingECGMeasurements.DiastolicBP, CultureInfo.InvariantCulture.NumberFormat);
					}
					catch {}
				}

				return 0;
			}
			set
			{
				if (_Root != null)
				{
					if ((value != ECGGlobalMeasurements.GlobalMeasurement.NoAxisValue)
					&&	(value != 0))
					{
						if (_Root.RestingECGMeasurements == null)
							_Root.RestingECGMeasurements = new Schemas.RestingECGMeasurements();

						_Root.RestingECGMeasurements.DiastolicBP = value.ToString(CultureInfo.InvariantCulture.NumberFormat);
					}
					else if (_Root.RestingECGMeasurements != null)
					{
						_Root.RestingECGMeasurements.DiastolicBP = "";
					}
				}
			}
		}

		public string FirstName
		{
			get
			{
				return ((_Root.PatientDemographics.PatientFirstName != null)
					&&	(_Root.PatientDemographics.PatientFirstName.Length != 0))
					?	_Root.PatientDemographics.PatientFirstName
					:	null;
			}
			set
			{
				if (value != null)
					_Root.PatientDemographics.PatientFirstName = value;
			}
		}

		public void Init()
		{
			if (_Root == null)
			{
				_Root = new Schemas.RestingECG();

				_Root.PatientDemographics = new Schemas.PatientDemographics();
				_Root.PatientDemographics.PatientID = "";
				_Root.PatientDemographics.Race = "";
				_Root.PatientDemographics.PatientLastName = "";
				_Root.PatientDemographics.PatientFirstName = "";

				_Root.TestDemographics = new Schemas.TestDemographics();
				_Root.TestDemographics.DataType = "Resting";
				_Root.TestDemographics.Site = _DefaultSite;
				_Root.TestDemographics.AcquisitionDevice = "ECGToolkit-cs";
				_Root.TestDemographics.Status = "Unconfirmed";
				_Root.TestDemographics.Priority = "Normal";
				_Root.TestDemographics.Location = "1";
				_Root.TestDemographics.RoomID = "";

				DateTime acq = DateTime.Now;
				_Root.TestDemographics.AcquisitionTime = acq.ToString(_TimeFormat);
				_Root.TestDemographics.AcquisitionDate = acq.ToString(_DateFormat);
				_Root.TestDemographics.CartNumber = "";

				_Root.TestDemographics.AcquisitionSoftwareVersion = "0A";
				_Root.TestDemographics.AcquisitionTechID = "";
				_Root.TestDemographics.OrderingMDLastName = "";
				_Root.TestDemographics.ReferringMDLastName = "";
				_Root.TestDemographics.OverreaderLastName = "";
				_Root.TestDemographics.SecondaryID = "";
				_Root.TestDemographics.XMLSourceVersion = "MAC5000 v1.0";

				_Root.Items = new object[1];
				Schemas.Waveform wvf = new Schemas.Waveform();
				_Root.Items[0] = wvf;
				wvf.ACFilter = new String[] {""};
			}
		}

		public string AcqInstitution
		{
			get {return null;}
			set {}
		}

		public byte FilterBitmap
		{
			get
			{
				// TODO:  Add MUSEFormat.FilterBitmap getter implementation
				return 0;
			}
			set
			{
				// TODO:  Add MUSEFormat.FilterBitmap setter implementation
			}
		}

		public ushort LowpassFilter
		{
			get
			{
				Schemas.Waveform[] wfs = _Waveform;
				
				if ((wfs != null)
				&&	(wfs.Length > 0)
				&&	(wfs[0].LowPassFilter != null))
				{
					try
					{
						return UInt16.Parse(wfs[0].LowPassFilter, CultureInfo.InvariantCulture.NumberFormat);
					} catch {}
				}
				
				return 0;
			}
			set
			{
				Schemas.Waveform[] wfs = _Waveform;
				
				if ((wfs != null)
				&&	(wfs.Length > 0))
				{
					wfs[0].LowPassFilter = value != 0 ? value.ToString(CultureInfo.InvariantCulture.NumberFormat) : null;
				}
			}
		}

		public string[] ReferralIndication
		{
			get {return null;}
			set {}
		}

		public string SuffixName
		{
			get {return null;}
			set {}
		}

		public ushort SystolicBloodPressure
		{
			get
			{
				if ((_Root.RestingECGMeasurements != null)
				&&  (_Root.RestingECGMeasurements.SystolicBP != null)
				&&	(_Root.RestingECGMeasurements.SystolicBP.Length > 0))
				{
					try
					{
						return UInt16.Parse(_Root.RestingECGMeasurements.SystolicBP, CultureInfo.InvariantCulture.NumberFormat);
					}
					catch {}
				}

				return 0;
			}
			set
			{
				if (_Root != null)
				{
					if ((value != ECGGlobalMeasurements.GlobalMeasurement.NoAxisValue)
					&&	(value != 0))
					{
						if (_Root.RestingECGMeasurements == null)
							_Root.RestingECGMeasurements = new Schemas.RestingECGMeasurements();

						_Root.RestingECGMeasurements.SystolicBP = value.ToString(CultureInfo.InvariantCulture.NumberFormat);
					}
					else if (_Root.RestingECGMeasurements != null)
					{
						_Root.RestingECGMeasurements.SystolicBP = "";
					}
				}
			}
		}

		public string SecondLastName
		{
			get {return null;}
			set {}
		}

		public int getPatientWeight(out ushort val, out WeightDefinition def)
		{
			// TODO: improve?
			val = 0;
			def = WeightDefinition.Unspecified;

			if ((_Root != null)
			&&	(_Root.PatientDemographics != null))
			{
				if (_Root.PatientDemographics.Item1 > 0)
				{
					val = (ushort)_Root.PatientDemographics.Item1;
					
					switch (_Root.PatientDemographics.Item1ElementName)
					{
						case Schemas.Item1ChoiceType.WeightKG:
							def = WeightDefinition.Kilogram;
						break;
						case Schemas.Item1ChoiceType.WeightLBS:
							def = WeightDefinition.Pound;
						break;
					}

					return 0;
				}
			}

			return 1;
		}

		public ushort BaselineFilter
		{
			get
			{
				Schemas.Waveform[] wfs = _Waveform;
				
				if ((wfs != null)
				&&	(wfs.Length > 0)
				&&	(wfs[0].HighPassFilter != null))
				{
					try
					{
						return UInt16.Parse(wfs[0].HighPassFilter, CultureInfo.InvariantCulture.NumberFormat);
					} catch {}
				}
				
				return 0;
			}
			set
			{
				Schemas.Waveform[] wfs = _Waveform;
				
				if ((wfs != null)
				&&	(wfs.Length > 0))
				{
					wfs[0].HighPassFilter = value != 0 ? value.ToString(CultureInfo.InvariantCulture.NumberFormat) : null;
				}
			}
		}

		public ECGConversion.ECGDemographics.Race PatientRace
		{
			get
			{
				try
				{
					if ((_Root.PatientDemographics.Race == null)
					||	(_Root.PatientDemographics.Race.Length == 0))
						return Race.Unspecified;

					return (ECGConversion.ECGDemographics.Race) ECGConverter.EnumParse(typeof(ECGConversion.ECGDemographics.Race), _Root.PatientDemographics.Race, true);
				}
				catch {}

				return Race.Null;
			}
			set
			{
				_Root.PatientDemographics.Race = (value != ECGDemographics.Race.Null) && (value != Race.Unspecified) ? value.ToString() : "";
			}
		}

		public byte StatCode
		{
			get {return 0;}
			set {}
		}

		public string AcqDepartment
		{
			get
			{
				return ((_Root.TestDemographics.Location != null)
					&&	(_Root.TestDemographics.Location.Length != 0))
					?	_Root.TestDemographics.Location
					:	null;
			}
			set
			{
				if (value != null)
					_Root.TestDemographics.Location = value;
			}
		}

		public int getPatientAge(out ushort val, out ECGConversion.ECGDemographics.AgeDefinition def)
		{
			val = 0;
			def = ECGConversion.ECGDemographics.AgeDefinition.Unspecified;

			return 1;
		}

		public string[] FreeTextFields
		{
			get {return null;}
			set {}
		}

		public int getPatientHeight(out ushort val, out HeightDefinition def)
		{
			// TODO: improve?
			val = 0;
			def = HeightDefinition.Unspecified;

			if ((_Root != null)
			&&	(_Root.PatientDemographics != null))
			{
				if (_Root.PatientDemographics.Item > 0)
				{
					val = (ushort)_Root.PatientDemographics.Item;
					
					switch (_Root.PatientDemographics.ItemElementName)
					{
						case Schemas.ItemChoiceType.HeightCM:
							def = HeightDefinition.Centimeters;
						break;
						case Schemas.ItemChoiceType.HeightIN:
							def = HeightDefinition.Inches;
						break;
					}

					return 0;
				}
			}

			return 1;
		}

		public string AnalyzingDepartment
		{
			get {return null;}
			set {}
		}

		public string SequenceNr
		{
			get {return null;}
			set {}
		}

		public AcquiringDeviceID AcqMachineID
		{
			get
			{
				AcquiringDeviceID ret = new AcquiringDeviceID(true);

				try
				{
					ret.InstitutionNr = UInt16.Parse(_Root.TestDemographics.Site, CultureInfo.InvariantCulture.NumberFormat);
				} catch {}
				try
				{
					ret.DepartmentNr = UInt16.Parse(_Root.TestDemographics.Location, CultureInfo.InvariantCulture.NumberFormat);
				} catch {}

				Communication.IO.Tools.BytesTool.writeString(_Root.TestDemographics.AcquisitionDevice, ret.ModelDescription, 0, ret.ModelDescription.Length);
				
				return ret;
			}
			set
			{
				if ((_DefaultSite == null)
				||	(_DefaultSite.Length == 0))
					_Root.TestDemographics.Site = (value.InstitutionNr != ECGGlobalMeasurements.GlobalMeasurement.NoValue) ? value.InstitutionNr.ToString(CultureInfo.InvariantCulture.NumberFormat) : "";

				_Root.TestDemographics.Location = (value.DepartmentNr != ECGGlobalMeasurements.GlobalMeasurement.NoValue) ? value.DepartmentNr.ToString(CultureInfo.InvariantCulture.NumberFormat) : "";
/*				string acqDevice = Communication.IO.Tools.BytesTool.readString(value.ModelDescription, 0, value.ModelDescription.Length);

				if ((acqDevice != null)
				&&	(acqDevice.Length != 0))
					_Root.TestDemographics.AcquisitionDevice = acqDevice;*/
			}
		}

		#endregion

		#region ISignal Members

		public int setSignals(Signals signals)
		{
			// TODO: improve?
			if ((signals != null)
			&&	(signals.NrLeads > 0)
			&&	(_Root != null)
			&&	(_Root.Items != null)
			&&	(_Root.Items.Length > 0)
			&&	(_Root.Items[0] is Schemas.Waveform))
			{
				LeadType[] aEL = _ExcludedLeads;

				if ((aEL != null)
				&&	(aEL.Length > 0))
				{
					int nrLeads = 0;
					for (int i=0;i < signals.NrLeads;i++)
					{
						if (signals[i] != null)
						{
							LeadType lt = signals[i].Type;

							int j=0;

							for (;j < aEL.Length;j++)
								if (lt == aEL[j])
									break;

							if (j == aEL.Length)
								nrLeads++;
						}
					}


					Signals eight = new Signals((byte)nrLeads);
					eight.RhythmAVM = signals.RhythmAVM;
					eight.RhythmSamplesPerSecond = signals.RhythmSamplesPerSecond;

					eight.MedianAVM = signals.MedianAVM;
					eight.MedianSamplesPerSecond = signals.MedianSamplesPerSecond;
					eight.MedianLength = signals.MedianLength;
					eight.MedianFiducialPoint = signals.MedianFiducialPoint;

					eight.QRSZone = signals.QRSZone;

					for (int i=0,j=0;j < signals.NrLeads;j++)
					{
						LeadType lt = signals[j].Type;

						int k = 0;
						for (;k < aEL.Length;k++)
							if (lt == aEL[k])
								break;

						if (k < aEL.Length)
							continue;

						eight[i++] = signals[j];
						nrLeads--;
					}

					if (nrLeads == 0)
					{
						signals = eight;
					}
				}

				// make sure the file is a certain length
				if (_DefaultLength > _MinLength)
				{
					signals.MakeSpecificLength(_DefaultLength);
				}
				
				Schemas.Waveform wf = (Schemas.Waveform)_Root.Items[0];
				
				// bad code should not be part of final code.
//				signals.MakeSpecificLength(10);

				double avm = _DefaultAVM;
				int sps = _DefaultSPS;

				if (avm < _MinAVM)
					avm = signals.RhythmAVM;

				if (sps < _MinSPS)
					sps = signals.RhythmSamplesPerSecond;

				wf.WaveformType = "Rhythm";
				wf.WaveformStartTime = "0";
				wf.NumberofLeads = signals.NrLeads;
				wf.SampleType = "CONTINUOUS_SAMPLES";
				wf.SampleBase = sps;
				wf.SampleExponent = 0;

				wf.LeadData = new Schemas.LeadData[wf.NumberofLeads];

				for (int i=0;i < wf.LeadData.Length;i++)
				{
					wf.LeadData[i] = new Schemas.LeadData();
					wf.LeadData[i].LeadID = signals[i].Type.ToString();

					wf.LeadData[i].LeadTimeOffset = 0;

					wf.LeadData[i].LeadAmplitudeUnitsPerBit = new Decimal(avm);
					wf.LeadData[i].LeadAmplitudeUnits = "MICROVOLTS";

					wf.LeadData[i].LeadOffsetFirstSample = (signals[i].RhythmStart * sps) / signals.RhythmSamplesPerSecond;
					wf.LeadData[i].FirstSampleBaseline = 0;
					wf.LeadData[i].LeadSampleSize = 2;

					wf.LeadData[i].LeadSampleCountTotal = ((signals[i].RhythmEnd - signals[i].RhythmStart) * sps) / signals.RhythmSamplesPerSecond;
					int smallCorrect = (wf.LeadData[i].LeadSampleCountTotal % wf.SampleBase);
					if (smallCorrect < 10)
						wf.LeadData[i].LeadSampleCountTotal -= smallCorrect;

					wf.LeadData[i].LeadByteCountTotal = wf.LeadData[i].LeadSampleSize * wf.LeadData[i].LeadSampleCountTotal;

					wf.LeadData[i].LeadHighLimit = 2147483647;
					wf.LeadData[i].LeadLowLimit = 268435456;
					
					wf.LeadData[i].LeadOff = "FALSE";
					wf.LeadData[i].BaselineSway = "FALSE";

					short[] tempSig = null;
					ECGTool.ResampleLead(signals[i].Rhythm, signals.RhythmSamplesPerSecond, sps, out tempSig);
					ECGTool.ChangeMultiplier(tempSig, signals.RhythmAVM, avm);

					byte[] tempData = new byte[wf.LeadData[i].LeadByteCountTotal];
					for (int j=0,offset=0;j < wf.LeadData[i].LeadSampleCountTotal;j++)
					{
						Communication.IO.Tools.BytesTool.writeBytes(tempSig[j], tempData, offset, 2, true);
						offset += 2;
					}

					wf.LeadData[i].WaveFormData = SetData(tempData);
					SetCRC(wf.LeadData[i], tempData);
				}

				return 0;
			}

			return 1;
		}

		public int getSignals(out Signals signals)
		{
			signals = new Signals();

			int ret = getSignalsToObj(signals);

			if (ret != 0)
				signals = null;

			return ret;
		}

		public int getSignalsToObj(Signals signals)
		{
			// TODO: improve?
			Schemas.Waveform[] wfs = _Waveform;
			
			if (Works()
			&&	(wfs.Length > 0))
            {
                if ((string.Compare(wfs[0].WaveformType, "Rhythm", true) == 0)
                && (wfs[0].LeadData[0].LeadAmplitudeUnitsPerBit > 0)
                && (wfs[0].SampleBase != 0))
                {
                    signals.NrLeads = ((byte)wfs[0].LeadData.Length);

                    signals.RhythmAVM = Decimal.ToDouble(wfs[0].LeadData[0].LeadAmplitudeUnitsPerBit);
                    signals.RhythmSamplesPerSecond = wfs[0].SampleBase;

                    for (int i = 0; i < signals.NrLeads; i++)
                    {
                        signals[i] = new Signal();

                        signals[i].Type = LeadType.Unknown;
                        try
                        {
                            signals[i].Type = (LeadType)Enum.Parse(typeof(LeadType), wfs[0].LeadData[i].LeadID);
                        }
                        catch { }
                        signals[i].Rhythm = new short[wfs[0].LeadData[i].LeadSampleCountTotal];
                        signals[i].RhythmStart = wfs[0].LeadData[i].LeadOffsetFirstSample;
                        signals[i].RhythmEnd = wfs[0].LeadData[i].LeadOffsetFirstSample + signals[i].Rhythm.Length;

                        byte[] wfd = GetData(wfs[0].LeadData[i].WaveFormData);
                        short sampleSize = wfs[0].LeadData[i].LeadSampleSize;

                        for (int j = 0, offset = 0; j < signals[i].Rhythm.Length; j++)
                        {
                            signals[i].Rhythm[j] = (short)Communication.IO.Tools.BytesTool.readBytes(wfd, offset, sampleSize, true);

                            offset += wfs[0].LeadData[i].LeadSampleSize;
                        }

                        ECGTool.ChangeMultiplier(signals[i].Rhythm, Decimal.ToDouble(wfs[0].LeadData[i].LeadAmplitudeUnitsPerBit), signals.RhythmAVM);
                    }

                    return 0;
                }
                else if ((wfs.Length > 1)
                    &&   (string.Compare(wfs[0].WaveformType, "Median", true) == 0)
                    &&   (string.Compare(wfs[1].WaveformType, "Rhythm", true) == 0)
                    &&   (wfs[0].LeadData[0].LeadAmplitudeUnitsPerBit > 0)
                    &&   (wfs[1].LeadData[0].LeadAmplitudeUnitsPerBit > 0)
                    &&   (wfs[0].SampleBase != 0)
                    &&   (wfs[1].SampleBase != 0))
                {
                    signals.NrLeads = ((byte)wfs[1].LeadData.Length);

                    signals.RhythmAVM = Decimal.ToDouble(wfs[1].LeadData[0].LeadAmplitudeUnitsPerBit);
                    signals.RhythmSamplesPerSecond = wfs[1].SampleBase;

                    if (wfs[0].LeadData.Length == wfs[1].LeadData.Length)
                    {
                        signals.MedianAVM = Decimal.ToDouble(wfs[0].LeadData[0].LeadAmplitudeUnitsPerBit);
                        signals.MedianSamplesPerSecond = wfs[0].SampleBase;
                        signals.MedianLength = (ushort) ((wfs[0].LeadData[0].LeadSampleCountTotal * 1000) / signals.MedianSamplesPerSecond);
                    }

                    for (int i = 0; i < signals.NrLeads; i++)
                    {
                        signals[i] = new Signal();

                        signals[i].Type = LeadType.Unknown;
                        try
                        {
                            signals[i].Type = (LeadType)Enum.Parse(typeof(LeadType), wfs[1].LeadData[i].LeadID);
                        }
                        catch { }
                        signals[i].Rhythm = new short[wfs[1].LeadData[i].LeadSampleCountTotal];
                        signals[i].RhythmStart = wfs[1].LeadData[i].LeadOffsetFirstSample;
                        signals[i].RhythmEnd = wfs[1].LeadData[i].LeadOffsetFirstSample + signals[i].Rhythm.Length;

                        byte[] wfd = GetData(wfs[1].LeadData[i].WaveFormData);
                        short sampleSize = wfs[1].LeadData[i].LeadSampleSize;

                        for (int j = 0, offset = 0; j < signals[i].Rhythm.Length; j++)
                        {
                            signals[i].Rhythm[j] = (short)Communication.IO.Tools.BytesTool.readBytes(wfd, offset, sampleSize, true);

                            offset += sampleSize;
                        }

                        if ((wfs[0].LeadData.Length == wfs[1].LeadData.Length)
                        &&  (string.Compare(wfs[0].LeadData[i].LeadID, wfs[1].LeadData[i].LeadID, true) == 0))
                        {
                            wfd = GetData(wfs[0].LeadData[i].WaveFormData);
                            sampleSize = wfs[0].LeadData[i].LeadSampleSize;

                            signals[i].Median = new short[wfs[0].LeadData[i].LeadSampleCountTotal];

                            for (int j = 0, offset = 0; j < signals[i].Median.Length; j++)
                            {
                                signals[i].Median[j] = (short)Communication.IO.Tools.BytesTool.readBytes(wfd, offset, sampleSize, true);

                                offset += sampleSize;
                            }
                        }

                        ECGTool.ChangeMultiplier(signals[i].Rhythm, Decimal.ToDouble(wfs[1].LeadData[i].LeadAmplitudeUnitsPerBit), signals.RhythmAVM);
                        ECGTool.ChangeMultiplier(signals[i].Median, Decimal.ToDouble(wfs[0].LeadData[i].LeadAmplitudeUnitsPerBit), signals.MedianAVM);
                    }

                    return 0;
                }
			}
			
			return 1;
		}

		#endregion
		
		#region IGlobalMeasurement Members
		
		private static int _ParseGlobalMeasurement(string sVal, bool allowNegative)
		{
			int nVal = GlobalMeasurement.NoAxisValue;
			
			if ((sVal != null)
			&&	(sVal.Length > 0))
			{
				nVal = Int32.Parse(sVal, CultureInfo.InvariantCulture.NumberFormat);
				
				if (!allowNegative
				&&	(nVal < 0))
				{
					nVal = GlobalMeasurement.NoAxisValue;
				}
			}
			
			return nVal;
		}
		
		private static int _ParseGlobalMeasurement(string sVal, int sampleBase, bool allowNegative)
		{
			int nVal = GlobalMeasurement.NoAxisValue;
			
			if ((sVal != null)
			&&	(sVal.Length > 0))
			{
				nVal = Int32.Parse(sVal, CultureInfo.InvariantCulture.NumberFormat);
				
				if (!allowNegative
				&&	(nVal < 0))
				{
					nVal = GlobalMeasurement.NoAxisValue;
				}
				else
				{
					nVal = (nVal * 1000) / sampleBase;
				}
			}
			
			return nVal;
		}
		
		private static string _ToStringGlobalMeasurements(int nVal, bool allowNegative)
		{
			string sVal = "";
			
			if ((nVal != GlobalMeasurement.NoAxisValue)
			&&	(allowNegative
			||	 (nVal > 0)))
			{
				sVal = nVal.ToString(CultureInfo.InvariantCulture.NumberFormat);
			}
			
			return sVal;
		}
		
		private static string _ToStringGlobalMeasurements(int nVal, int sampleBase, bool allowNegative)
		{
			string sVal = "";
			
			if ((nVal != GlobalMeasurement.NoAxisValue)
			&&	(allowNegative
			||	 (nVal > 0)))
			{
				nVal = (nVal * sampleBase) / 1000;
				
				sVal = nVal.ToString(CultureInfo.InvariantCulture.NumberFormat);
			}
			
			return sVal;
		}

		public int getGlobalMeasurements(out ECGConversion.ECGGlobalMeasurements.GlobalMeasurements mes)
		{
			mes = null;

			if ((_Root != null)
			&&	(_Root.RestingECGMeasurements != null))
			{
				try
				{
					mes = new GlobalMeasurements();
					mes.measurment = new ECGConversion.ECGGlobalMeasurements.GlobalMeasurement[1];
					mes.measurment[0] = new ECGConversion.ECGGlobalMeasurements.GlobalMeasurement();
					
					int sampleBase = _Root.RestingECGMeasurements.ECGSampleBase;

					mes.measurment[0].Ponset   = (ushort) _ParseGlobalMeasurement(_Root.RestingECGMeasurements.POnset, sampleBase, false);
					mes.measurment[0].Poffset  = (ushort) _ParseGlobalMeasurement(_Root.RestingECGMeasurements.POffset, sampleBase, false);
					mes.measurment[0].QRSonset = (ushort) _ParseGlobalMeasurement(_Root.RestingECGMeasurements.QOnset, sampleBase, false);
					mes.measurment[0].QRSoffset = (ushort) _ParseGlobalMeasurement(_Root.RestingECGMeasurements.QOffset, sampleBase, false);
					mes.measurment[0].Toffset = (ushort) _ParseGlobalMeasurement(_Root.RestingECGMeasurements.TOffset, sampleBase, false);
									
					ushort qrsDur = GlobalMeasurement.NoValue;
					
					qrsDur = (ushort) _ParseGlobalMeasurement(_Root.RestingECGMeasurements.QRSDuration, false);
					
					if (mes.measurment[0].QRSonset != GlobalMeasurement.NoValue)
					{
						if ((qrsDur != GlobalMeasurement.NoValue)
						&&	(qrsDur > 0))
							mes.measurment[0].QRSoffset = (ushort) (mes.measurment[0].QRSonset + qrsDur);
						else
							mes.measurment[0].QRSoffset = GlobalMeasurement.NoValue;
					
						qrsDur = (ushort) _ParseGlobalMeasurement(_Root.RestingECGMeasurements.PRInterval, false);
						if (qrsDur != GlobalMeasurement.NoValue)
							mes.measurment[0].Ponset = (ushort) (mes.measurment[0].QRSonset - qrsDur);
					
						qrsDur = (ushort) _ParseGlobalMeasurement(_Root.RestingECGMeasurements.QTInterval, false);
						if (qrsDur != GlobalMeasurement.NoValue)
							mes.measurment[0].Toffset = (ushort) (mes.measurment[0].QRSonset + qrsDur);
						else
							qrsDur = mes.QTdur;
					}
					else
					{
						qrsDur = mes.QTdur;
					}

					mes.measurment[0].Paxis = (short) _ParseGlobalMeasurement(_Root.RestingECGMeasurements.PAxis, true);
					mes.measurment[0].QRSaxis = (short) _ParseGlobalMeasurement(_Root.RestingECGMeasurements.RAxis, true);
					mes.measurment[0].Taxis = (short) _ParseGlobalMeasurement(_Root.RestingECGMeasurements.TAxis, true);
					
					ushort val = (ushort) _ParseGlobalMeasurement(_Root.RestingECGMeasurements.QTcFrederica, false);
					
					if (val != GlobalMeasurement.NoValue)
					{
						if (qrsDur != GlobalMeasurement.NoValue)
						{
							Decimal d = qrsDur;
							d /= val;
								
							d = d * d * d * 1000;
								
							mes.AvgRR = Decimal.ToUInt16(d);
							mes.QTcType = ECGConversion.ECGGlobalMeasurements.GlobalMeasurements.QTcCalcType.Fridericia;
						}
						else
						{
							mes.QTc = val;
						}
					}
					else
					{
						val = (ushort) _ParseGlobalMeasurement(_Root.RestingECGMeasurements.QTCorrected, false);
						
						if (val != GlobalMeasurement.NoValue)
						{
							if (qrsDur != GlobalMeasurement.NoValue)
							{	
								Decimal d = qrsDur;
								d /= val;
								
								d = d * d * 1000;
								
								mes.AvgRR = Decimal.ToUInt16(d);
								mes.QTcType = ECGConversion.ECGGlobalMeasurements.GlobalMeasurements.QTcCalcType.Bazett;
							}
							else
							{
								mes.QTc = val;
							}
						}
					}
					
					qrsDur = (ushort) _ParseGlobalMeasurement(_Root.RestingECGMeasurements.VentricularRate, false);
					if ((qrsDur != GlobalMeasurement.NoValue)
					&&	(qrsDur != mes.VentRate))
						mes.VentRate = qrsDur;
					
					return 0;
				}
				catch
				{
					mes = null;
				}

				return 2;
			}

			return 1;
		}

		public int setGlobalMeasurements(ECGConversion.ECGGlobalMeasurements.GlobalMeasurements mes)
		{
			if ((mes != null)
			&&	(mes.measurment != null)
			&&	(mes.measurment.Length > 0)
			&&	(_Root != null))
			{
				Schemas.Waveform[] wfs = _Waveform;
				
				if (wfs != null)
				{
					int sampleBase = wfs[0].SampleBase;
					
					if (_Root.RestingECGMeasurements == null)
						_Root.RestingECGMeasurements = new Schemas.RestingECGMeasurements();
					
					_Root.RestingECGMeasurements.ECGSampleBase =  sampleBase;
					_Root.RestingECGMeasurements.ECGSampleExponent = wfs[0].SampleExponent;

					_Root.RestingECGMeasurements.VentricularRate = _ToStringGlobalMeasurements(mes.VentRate, false);
					_Root.RestingECGMeasurements.PRInterval = _ToStringGlobalMeasurements(mes.PRint, false);
					_Root.RestingECGMeasurements.QRSDuration = _ToStringGlobalMeasurements(mes.QRSdur, false);
					_Root.RestingECGMeasurements.QTInterval = _ToStringGlobalMeasurements(mes.QTdur, false);
					
					if (mes.QTcType == ECGConversion.ECGGlobalMeasurements.GlobalMeasurements.QTcCalcType.Fridericia)
					{
						_Root.RestingECGMeasurements.QTCorrected = null;
						_Root.RestingECGMeasurements.QTcFrederica = _ToStringGlobalMeasurements(mes.QTc, false);
					}
					else
					{
						_Root.RestingECGMeasurements.QTCorrected = _ToStringGlobalMeasurements(mes.QTc, false);
						_Root.RestingECGMeasurements.QTcFrederica = null;
					}

					_Root.RestingECGMeasurements.PAxis = _ToStringGlobalMeasurements(mes.measurment[0].Paxis, true);
					_Root.RestingECGMeasurements.RAxis = _ToStringGlobalMeasurements(mes.measurment[0].QRSaxis, true);
					_Root.RestingECGMeasurements.TAxis = _ToStringGlobalMeasurements(mes.measurment[0].Taxis, true);

					_Root.RestingECGMeasurements.POnset = _ToStringGlobalMeasurements(mes.measurment[0].Ponset, sampleBase, false);
					_Root.RestingECGMeasurements.POffset = _ToStringGlobalMeasurements(mes.measurment[0].Poffset, sampleBase, false);
					_Root.RestingECGMeasurements.QOnset = _ToStringGlobalMeasurements(mes.measurment[0].QRSonset, sampleBase, false);
					_Root.RestingECGMeasurements.QOffset = _ToStringGlobalMeasurements(mes.measurment[0].QRSoffset, sampleBase, false);
					_Root.RestingECGMeasurements.TOffset = _ToStringGlobalMeasurements(mes.measurment[0].Toffset, sampleBase, false);

					return 0;
				}
				return 2;
			}
			return 1;
		}

		#endregion

		#region IDiagnostic Members

		public int getDiagnosticStatements(out Statements stat)
		{
			stat = null;

			if ((_Root != null)
			&&	(_Root.PatientDemographics != null)
			&&	(_Root.Diagnosis != null)
			&&	(string.Compare(_Root.Diagnosis.Modality, "Resting", true) == 0)
			&&	(_Root.Diagnosis.DiagnosisStatement != null)
			&&	(_Root.Diagnosis.DiagnosisStatement.Length > 0))
			{
				stat = new Statements();
				
				ArrayList al = new ArrayList();

				int nrStatements = 0;

				foreach (Schemas.DiagnosisStatement ds in _Root.Diagnosis.DiagnosisStatement)
				{
					string text = ds.StmtText;

					if (text == null)
						text = "";

					if (al.Count < nrStatements)
						al[nrStatements] = ((string)al[nrStatements]) + text;
					else
						al.Add(text);

					if (ds.StmtFlag != null)
					{
						foreach (string flag in ds.StmtFlag)
						{
							if (string.Compare(flag, "ENDSLINE", true) == 0)
							{
								nrStatements++;
								break;
							}
						}
					}
				}

				stat.statement = new string[al.Count];

				for (int i=0;i < stat.statement.Length;i++)
					stat.statement[i] = (string)al[i];

				stat.confirmed = string.Compare(_Root.TestDemographics.Status, "confirmed", true) == 0;
				stat.time = this.TimeAcquisition;
				
				if ((_Root.TestDemographics.EditTime != null)
				&&	(_Root.TestDemographics.EditTime.Length > 0)
				&&	(_Root.TestDemographics.EditDate != null)
				&&	(_Root.TestDemographics.EditDate.Length > 0))
				{
					stat.interpreted = (_Root.TestDemographics.OverreaderLastName != null) && (_Root.TestDemographics.OverreaderLastName.Length > 0);
					
					if (stat.interpreted)
					{
						stat.time = ParseDateTime(_Root.TestDemographics.EditTime + " " + _Root.TestDemographics.EditDate);
					}
				}

				return 0;
			}
			
			return 1;
		}

		public int setDiagnosticStatements(Statements stat)
		{
			if ((stat != null)
			&&	(stat.statement != null)
			&&	(stat.statement.Length > 0)
			&&	(_Root != null)
			&&	(_Root.TestDemographics != null))
			{
				_Root.Diagnosis = new Schemas.Diagnosis();

				_Root.Diagnosis.Modality = "Resting";

				if (stat.confirmed)
				{
					_Root.TestDemographics.EditTime = stat.time.ToString(_TimeFormat, CultureInfo.InvariantCulture.DateTimeFormat);
					_Root.TestDemographics.EditDate = stat.time.ToString(_DateFormat, CultureInfo.InvariantCulture.DateTimeFormat);
					_Root.TestDemographics.Status = "Confirmed";
				}
				else if (stat.interpreted)
				{
					_Root.TestDemographics.EditTime = stat.time.ToString(_TimeFormat, CultureInfo.InvariantCulture.DateTimeFormat);
					_Root.TestDemographics.EditDate = stat.time.ToString(_DateFormat, CultureInfo.InvariantCulture.DateTimeFormat);
					_Root.TestDemographics.Status = "Interpreted";
				}
				else
				{
					_Root.TestDemographics.Status = "Unconfirmed";
				}

				_Root.Diagnosis.DiagnosisStatement = new Schemas.DiagnosisStatement[stat.statement.Length];
				
				for (int i=0;i < stat.statement.Length;i++)
				{
					_Root.Diagnosis.DiagnosisStatement[i] = new Schemas.DiagnosisStatement();

					string text = stat.statement[i];

					if (text == null)
						text = "";

					_Root.Diagnosis.DiagnosisStatement[i].StmtText = text;
					_Root.Diagnosis.DiagnosisStatement[i].StmtFlag = new string[] {"ENDSLINE"};
				}

				return 0;
			}

			return 1;
		}

		#endregion
	}
}

