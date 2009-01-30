/***************************************************************************
Copyright 2004,2008-2009, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

Written by Marcel de Wijs. Changed by Maarten van Ettinger.

****************************************************************************/
using System;
using System.Runtime.InteropServices;
using Communication.IO.Tools;
using ECGConversion.ECGDemographics;
using ECGConversion.ECGSignals;

namespace ECGConversion.RawFormat
{
    /// <summary>
    /// Summary description for RawECGHeader.
    /// </summary>
	public class RawECGHeader : IDemographic, ISignal
	{
		// static variables to read header
		public static byte ValueOfP = 0x50;
		public static byte ValueOfK = 0x4b;
		public static int HeaderSize = 512;
		//        private static int _HeadHeadPadding = 15;
		// format of header
		private byte _P;
		private byte _K;
		//        private byte _FileVersion;
		//        private byte _FileType;
		private short _HeaderSize;
		private uint _TotalFileSize;
		private byte _Day;
		private byte _Month;
		private short _Year;
		private byte _Hour;
		private byte _Min;
		private byte _Sec;
		private RawECGPatientId     _PatId      = new RawECGPatientId();
		private RawECGIdRecordInfo  _IdRecInfo  = new RawECGIdRecordInfo();
		private RawECGInfo          _ECGInfo    = new RawECGInfo();
		private RawECGRecordInfo    _RecInfo    = new RawECGRecordInfo();
        
		private LeadType[]          LeadConfiguration;
		/// <summary>
		/// Function to read header from buffer.
		/// </summary>
		/// <param name="buffer">buffer to read from</param>
		/// <param name="offset">position to start reading</param>
		/// <param name="littleEndian">true if little endian is used</param>
		/// <returns>0 on success</returns>
		public int Read(byte[] buffer, int offset, bool littleEndian)
		{
			if ((buffer == null)
			||	((offset + HeaderSize) > buffer.Length))
			{
				return 0x1;
			}
            
			if ((_P != ValueOfP)
			||	(_K != ValueOfK))
			{
				return 0x2;
			}

          
			_Day    =   (byte)System.DateTime.Now.Date.Day; 
			_Month  =   (byte)System.DateTime.Now.Date.Month;
			_Year   =   (short)System.DateTime.Now.Date.Year;
			_Hour   =   (byte)System.DateTime.Now.Date.Hour;
			_Min    =   (byte)System.DateTime.Now.Date.Minute;
			_Sec    =   (byte)System.DateTime.Now.Date.Second;

			_PatId.Read(buffer, offset, littleEndian); 
			_IdRecInfo.Read(buffer, offset, littleEndian); 
			_ECGInfo.Read(buffer, offset, littleEndian); 
			_RecInfo.Read(buffer, offset, littleEndian);

			return 0x0;
		}
		/// <summary>
		/// Function to write header into a buffer.
		/// </summary>
		/// <param name="buffer">buffer to write in</param>
		/// <param name="littleEndian">true if little endian is used</param>
		/// <returns>0 on success</returns>
		public int Write(out byte[] buffer, bool littleEndian)
		{
			buffer = new byte[_HeaderSize];
			int err = Write(buffer, 0, littleEndian);
			if (err != 0)
			{
				buffer = null;
			}
			return err;
		}
		/// <summary>
		/// Function to write header into a buffer.
		/// </summary>
		/// <param name="buffer">buffer to write in</param>
		/// <param name="offset">position to start writing</param>
		/// <param name="littleEndian">true if little endian is used</param>
		/// <returns>0 on success</returns>
		public int Write(byte[] buffer, int offset, bool littleEndian)
		{
			if (!Works())
			{
				return 0x1;
			}

			if ((buffer == null)
			||	((offset + _HeaderSize) > buffer.Length))
			{
				return 0x2;
			}

			_HeaderSize = (short) HeaderSize;
			_TotalFileSize = (uint) (((_ECGInfo.NrOfLeads * _ECGInfo.NrECGSamples) << 1) );

			return 0;
		}
		/// <summary>
		/// Function to anonymous the header.
		/// </summary>
		/// <param name="type">character to use for anonymous</param>
		public void Anonymous(byte type)
		{
			// Empty id, lastname and firstname.
			BytesTool.emptyBuffer(_PatId.Id, 0, BytesTool.stringLength(_PatId.Id, 0, RawECGPatientId.MaxIdLen), type);
			BytesTool.emptyBuffer(_PatId.LastName, 0, BytesTool.stringLength(_PatId.LastName, 0, RawECGPatientId.MaxNameLen), type);
			BytesTool.emptyBuffer(_PatId.FirstName, 0, BytesTool.stringLength(_PatId.FirstName, 0, RawECGPatientId.MaxNameLen), type);
			// Setting date to first of january.
			_PatId.BirthDate[4] = (byte) '0';
			_PatId.BirthDate[5] = (byte) '1';
			_PatId.BirthDate[6] = (byte) '0';
			_PatId.BirthDate[7] = (byte) '1';
			_PatId.BirthDate[8] = 0;
		}
		/// <summary>
		/// Function to empty header.
		/// </summary>
		public void Empty()
		{
			_P = ValueOfP;
			_K = ValueOfK;
			//            _FileVersion = 0x16;
			//            _FileType = 12;
			_HeaderSize = (short) HeaderSize;
			_TotalFileSize = 0;
			_Day = 0;
			_Min = 0;
			_Year = 0;
			_Hour = 0;
			_Min = 0;
			_Sec = 0;
			_PatId.Empty();
			_IdRecInfo.Empty();
			_RecInfo.Empty();
			_ECGInfo.Empty();
			if ( LeadConfiguration == null)
			{
				LeadConfiguration = new LeadType[8];
				LeadConfiguration[0] = LeadType.I;
				LeadConfiguration[1] = LeadType.II;
				LeadConfiguration[2] = LeadType.V1;
				LeadConfiguration[3] = LeadType.V2;
				LeadConfiguration[4] = LeadType.V3;
				LeadConfiguration[5] = LeadType.V4;
				LeadConfiguration[6] = LeadType.V5;
				LeadConfiguration[7] = LeadType.V6;
			}
		}
		/// <summary>
		/// Function to check if header makes sense.
		/// </summary>
		/// <returns></returns>
		public bool Works()
		{
			return ((_P == ValueOfP)
				&&	(_K == ValueOfK)
				&&	(_HeaderSize == HeaderSize)
				&&	(_PatId.Id != null)
				&&	(_PatId.Id[0] != 0x00)
				&&	(_ECGInfo.ECGLSBPerMV > 0)
				&&	(_ECGInfo.ECGSampFreq > 0)
				&&	(_ECGInfo.NrECGSamples > 0)
				&&	(_ECGInfo.NrOfLeads > 0)
				&&	(_ECGInfo.NrOfLeads <= RawECGInfo.MaxNrLeads));
		}
		/// <summary>
		/// Function to get number of leads.
		/// </summary>
		/// <returns>number of leads</returns>
		public int getNrLeads()
		{
			if (Works())
			{
				return _ECGInfo.NrOfLeads;
			}
			return 0;
		}
		/// <summary>
		/// Function to get number of samples per lead.
		/// </summary>
		/// <returns>number of samples per lead</returns>
		public uint getNrSamplesPerLead()
		{
			if (Works())
			{
				return _ECGInfo.NrECGSamples;
			}
			return 0;
		}

		public short getLSBPerMV()
		{
			if (Works())
			{
				return _ECGInfo.ECGLSBPerMV;
			}
			return 0;
		}

		public void setNrLeads(int nNrLeads)
		{
			_ECGInfo.NrOfLeads = (short)nNrLeads;
		}
		/// <summary>
		/// Function to get number of samples per lead.
		/// </summary>
		/// <returns>number of samples per lead</returns>
		public void setNrSamplesPerLead(int nNrSamplesPerLead)
		{
			_ECGInfo.NrECGSamples = (uint)nNrSamplesPerLead;
		}

		public short getSampleRate()
		{
			if (Works())
			{
				return _ECGInfo.ECGSampFreq;
			}
			return 0;
		}
		public void setSampleRate(int nECGSampeFreq)
		{
			_ECGInfo.ECGSampFreq = (short)nECGSampeFreq;
		}

		public void setLSBPerMV(int nLSBPerMV)
		{
			_ECGInfo.ECGLSBPerMV = (short)nLSBPerMV;
		}

		public void setLeadConfiguration(string[] myLeadConfig)
		{
			if (myLeadConfig.Length != LeadConfiguration.Length)
			{
				LeadConfiguration = new LeadType[myLeadConfig.Length];
			}

			for (int i=0; i<myLeadConfig.Length; i++)
			{
				LeadConfiguration[i] = (LeadType)ECGConverter.EnumParse(typeof(LeadType), myLeadConfig[i], true);
			}
		}

		/// <summary>
		/// Function to get the length of the ecg data according to the Header.
		/// </summary>
		/// <returns>length of ecg data</returns>
		public uint getDataLength()
		{
			if (Works())
			{
				return (uint) (_ECGInfo.NrECGSamples * _ECGInfo.NrOfLeads * Marshal.SizeOf(typeof(short)));
			}
			return 0;
		}
		/// <summary>
		/// Function to get if data is stored little endian or not.
		/// </summary>
		/// <returns>true if little endian</returns>
		public bool isLittleEndian()
		{
			return true;
		}
		/// <summary>
		/// Class containing patient Id.
		/// </summary>
		public class RawECGPatientId
		{
			// static variables for definition of structure.
			public static int Size = 80;
			public static int Padding = 8;
			public static int MaxIdLen = 21;
			public static int MaxNameLen = 23;
			public static int MaxDateLen = 9;
			// structure of data.
			public short IdFormat;
			public byte[] Id = new byte[MaxIdLen];
			public byte[] LastName = new byte[MaxNameLen];
			public byte[] FirstName = new byte[MaxNameLen];
			public byte[] BirthDate = new byte[MaxDateLen];
			public byte Sex;
			public byte Race;
			/// <summary>
			/// Function to read patient id from buffer.
			/// </summary>
			/// <param name="buffer">buffer to read from</param>
			/// <param name="offset">position to start reading</param>
			/// <param name="littleEndian">true if little endian is used</param>
			/// <returns>0 on success</returns>
			public int Read(byte[] buffer, int offset, bool littleEndian)
			{
				string name;

				IdFormat=0;

				string id = "1234567";
				BytesTool.writeString(id,Id, 0, RawECGPatientId.MaxNameLen);

				name = "unknown";
				BytesTool.writeString(name, LastName, 0, RawECGPatientId.MaxNameLen);
				BytesTool.writeString(name, FirstName, 0, RawECGPatientId.MaxNameLen);

				Date myDate = new Date(1901,1,1); 
				BytesTool.writeString(myDate.Year.ToString("d4"), BirthDate, 0, 5);
				BytesTool.writeString(myDate.Month.ToString("d2"), BirthDate, 4, 3);
				BytesTool.writeString(myDate.Day.ToString("d2"), BirthDate, 6, 3);

				Sex = (byte)0;

				Race = (byte)0;

				return 0;
			}
			/// <summary>
			/// Function to write patient id into a buffer.
			/// </summary>
			/// <param name="buffer">buffer to write in</param>
			/// <param name="offset">position to start writing</param>
			/// <param name="littleEndian">true if little endian is used</param>
			/// <returns>0 on success</returns>
			public int Write(byte[] buffer, int offset, bool littleEndian)
			{
				// empty, shoudl nog be called
				throw new SystemException();
			}
			/// <summary>
			/// Function to empty header.
			/// </summary>
			public void Empty()
			{
				IdFormat = 0;
				Id = new byte[MaxIdLen];
				Id = System.Text.ASCIIEncoding.ASCII.GetBytes("1234567");
				LastName = new byte[MaxNameLen];
				FirstName = new byte[MaxNameLen];
				BirthDate = new byte[MaxDateLen];
				for (int loper=0;loper < MaxDateLen-1;loper++)
				{
					BirthDate[loper] = (byte) '0';
				}
				Sex = 0;
				Race = 0;
			}
		}
		/// <summary>
		/// Class containing Id record info.
		/// </summary>
		public class RawECGIdRecordInfo
		{
			// static variables for definition of structure.
			public static int Size = 22;
			public static int Padding = 10;
			public static int MaxNrDrugs = 2;
			public static int MaxNrClinClass = 2;
			// structure of data.
			public short AgeYears;
			public short AgeMonths;
			public short AgeDays;
			public short[] Drugs = new short[MaxNrDrugs];
			public short[] ClinClass = new short[MaxNrClinClass];
			public short Height;
			public short Weight;
			public short SysBP;
			public short DiaBP;
			/// <summary>
			/// Function to read id record info from buffer.
			/// </summary>
			/// <param name="buffer">buffer to read from</param>
			/// <param name="offset">position to start reading</param>
			/// <param name="littleEndian">true if little endian is used</param>
			/// <returns>0 on success</returns>
			public int Read(byte[] buffer, int offset, bool littleEndian)
			{
				// default applied
				AgeYears = 0;
				AgeMonths = 0;
				AgeDays = 0;

				for (int loper=0;loper < MaxNrDrugs;loper++)
				{
					Drugs[loper] = (short)0;
				}

				for (int loper=0;loper < MaxNrClinClass;loper++)
				{
					ClinClass[loper] = (short)0;
				}

				Height = (short) 0;
				Weight = (short) 0;
				SysBP = (short) 0;
				DiaBP = (short) 0;

				return 0;
			}
			/// <summary>
			/// Function to write id record into a buffer.
			/// </summary>
			/// <param name="buffer">buffer to write in</param>
			/// <param name="offset">position to start writing</param>
			/// <param name="littleEndian">true if little endian is used</param>
			/// <returns>0 on success</returns>
			public int Write(byte[] buffer, int offset, bool littleEndian)
			{
				// empty, shoudl nog be called
				throw new SystemException();
			}
			/// <summary>
			/// Function to empty header.
			/// </summary>
			public void Empty()
			{
				AgeYears = 0;
				AgeMonths = 0;
				AgeDays = 0;
				Drugs = new short[MaxNrDrugs];
				ClinClass = new short[MaxNrClinClass];
				Height = 0;
				Weight = 0;
				SysBP = 0;
				DiaBP = 0;
			}
		}
		/// <summary>
		/// Class containing ecg info.
		/// </summary>
		public class RawECGInfo
		{
			// static variables for definition of structure.
			public static int Size = 42;
			public static int Padding = 6;
			public static int MaxNrLeads = 12;
			// structure of data.
			public short ECGSampFreq;
			public short ECGLSBPerMV;
			public short ElecSetting;
			public short AMType;
			public short TremorType;
			public short NrOfLeads;
			public short[] LeadConfig = new short[MaxNrLeads];
			public short ECGType = 1;
			public uint NrECGSamples;
			/// <summary>
			/// Function to read ecg info from buffer.
			/// </summary>
			/// <param name="buffer">buffer to read from</param>
			/// <param name="offset">position to start reading</param>
			/// <param name="littleEndian">true if little endian is used</param>
			/// <returns>0 on success</returns>
			public int Read(byte[] buffer, int offset, bool littleEndian)
			{
				if (ECGSampFreq == 0)  
				{
					ECGSampFreq = 500;  
				}
				ElecSetting = 0;
				AMType      = 0;
				TremorType  = 0;

				for (int loper=0;loper < MaxNrLeads;loper++)
				{
					LeadConfig[loper] = loper < NrOfLeads? (short)loper : (short)-1;
				}

				ECGType = 1;
                
				return 0;
			}
			/// <summary>
			/// Function to write ecg info into a buffer.
			/// </summary>
			/// <param name="buffer">buffer to write in</param>
			/// <param name="offset">position to start writing</param>
			/// <param name="littleEndian">true if little endian is used</param>
			/// <returns>0 on success</returns>
			public int Write(byte[] buffer, int offset, bool littleEndian)
			{
				// empty, should nog be called
				throw new SystemException();
			}
			/// <summary>
			/// Function to empty header.
			/// </summary>
			public void Empty()
			{
				ECGSampFreq = 0;
				ECGLSBPerMV = 0;
				ElecSetting = 0;
				AMType = 0;
				TremorType = 0;
				NrOfLeads = 0;
				LeadConfig = new short[MaxNrLeads];
				for (int loper=0;loper < MaxNrLeads;loper++)
				{
					LeadConfig[loper] = -1;
				}
				ECGType = 1;
				NrECGSamples = 0;
			}
		}
		/// <summary>
		/// Class containing record info.
		/// </summary>
		public class RawECGRecordInfo
		{
			// static variables for definition of structure.
			public static int Size = 92;
			public static int Padding = 28;
			public static int MaxECGIdLen = 19;
			public static int MaxRecDateLen = 9;
			public static int MaxRecTimeLen = 7;
			public static int MaxCartIdLen = 6;
			public static int MaxLocationLen = 11;
			public static int MaxSiteLen = 11;
			public static int MaxProgramVersionLen = 26;
			// structure of data.
			public short CartType = -1;
			public byte[] ECGId = new byte[MaxECGIdLen];
			public byte[] RecDate = new byte[MaxRecDateLen];
			public byte[] RecTime = new byte[MaxRecTimeLen];
			public byte[] CartId = new byte[MaxCartIdLen];
			public byte[] Location = new byte[MaxLocationLen];
			public byte[] Site = new byte[MaxSiteLen];
			public byte[] ProgramVersion = new byte[MaxProgramVersionLen];
			public byte ByteOrder = (byte) 'M';
			/// <summary>
			/// Function to read record id from buffer.
			/// </summary>
			/// <param name="buffer">buffer to read from</param>
			/// <param name="offset">position to start reading</param>
			/// <param name="littleEndian">true if little endian is used</param>
			/// <returns>0 on success</returns>
			public int Read(byte[] buffer, int offset, bool littleEndian)
			{
				CartType        = (short) -1;
				ECGId[0]        = 0;
                
				Date myDate = new Date(1901,1,1);
				BytesTool.writeString(myDate.Year.ToString("d4"), RecDate, 0, 5);
				BytesTool.writeString(myDate.Month.ToString("d2"),RecDate, 4, 3);
				BytesTool.writeString(myDate.Day.ToString("d2"), RecDate, 6, 3);

				BytesTool.writeString("01", RecTime, 0, 3);
				BytesTool.writeString("01", RecTime, 2, 3);
				BytesTool.writeString("01", RecTime, 4, 3);
         
				AcquiringDeviceID id = new AcquiringDeviceID();
				BytesTool.writeString(id.DeviceID.ToString("d5"), CartId, 0, RawECGRecordInfo.MaxCartIdLen);
				BytesTool.writeString("ErasmusMC", Location, 0, RawECGRecordInfo.MaxLocationLen);
				BytesTool.writeString(((DeviceManufactor)id.ManufactorID).ToString(), Site, 0, RawECGRecordInfo.MaxSiteLen);
				BytesTool.writeString("ECGConversion 1.1", ProgramVersion, 0, RawECGRecordInfo.MaxProgramVersionLen);

				ByteOrder = (byte) 'I';
				return 0;
			}
			/// <summary>
			/// Function to write record id into a buffer.
			/// </summary>
			/// <param name="buffer">buffer to write in</param>
			/// <param name="offset">position to start writing</param>
			/// <param name="littleEndian">true if little endian is used</param>
			/// <returns>0 on success</returns>
			public int Write(byte[] buffer, int offset, bool littleEndian)
			{
				// empty, shoudl nog be called
				throw new SystemException();
			}
			/// <summary>
			/// Function to empty header.
			/// </summary>
			public void Empty()
			{
				CartType = -1;
				ECGId = new byte[MaxECGIdLen];
				RecDate = new byte[MaxRecDateLen];
				RecTime = new byte[MaxRecTimeLen];
				CartId = new byte[MaxCartIdLen];
				Location = new byte[MaxLocationLen];
				Site = new byte[MaxSiteLen];
				ProgramVersion = new byte[MaxProgramVersionLen];
				ByteOrder = (byte) 'M';
			}
		}
		/// <summary>
		/// A class to store GRI setup.
		/// </summary>
		public class Analys2000GRISetup
		{
			// static variables for definition of structure.
			public static int Size = 4;
			public static int Padding = 188;
			// structure of data.
			public short BradyLimit = 60;
			public short TachyLimit = 100;
			/// <summary>
			/// Function to read GRI Setup from buffer.
			/// </summary>
			/// <param name="buffer">buffer to read from</param>
			/// <param name="offset">position to start reading</param>
			/// <param name="littleEndian">true if little endian is used</param>
			/// <returns>0 on success</returns>
			public int Read(byte[] buffer, int offset, bool littleEndian)
			{
				BradyLimit = (short) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(BradyLimit), littleEndian);
				offset += Marshal.SizeOf(BradyLimit);

				TachyLimit = (short) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(TachyLimit), littleEndian);
				offset += Marshal.SizeOf(TachyLimit);

				return 0;
			}
			/// <summary>
			/// Function to write GRI Setup into a buffer.
			/// </summary>
			/// <param name="buffer">buffer to write in</param>
			/// <param name="offset">position to start writing</param>
			/// <param name="littleEndian">true if little endian is used</param>
			/// <returns>0 on success</returns>
			public int Write(byte[] buffer, int offset, bool littleEndian)
			{
				BytesTool.writeBytes(BradyLimit, buffer, offset, Marshal.SizeOf(BradyLimit), littleEndian);
				offset += Marshal.SizeOf(BradyLimit);

				BytesTool.writeBytes(TachyLimit, buffer, offset, Marshal.SizeOf(TachyLimit), littleEndian);
				offset += Marshal.SizeOf(TachyLimit);

				return 0;
			}
			/// <summary>
			/// Function to empty header.
			/// </summary>
			public void Empty()
			{
				BradyLimit = 60;
				TachyLimit = 100;
			}
		}
		public void Init()
		{
			Empty();
		}
		public string LastName
		{
			get
			{
				if (_PatId.LastName[0] != 0)
				{
					return BytesTool.readString(_PatId.LastName, 0, RawECGPatientId.MaxNameLen);
				}
				return null;
			}
			set
			{
				if (value != null)
				{
					BytesTool.writeString(value, _PatId.LastName, 0, RawECGPatientId.MaxNameLen);
				}
			}
		}
		public string FirstName
		{
			get
			{
				if (_PatId.FirstName[0] != 0)
				{
					return BytesTool.readString(_PatId.FirstName, 0, RawECGPatientId.MaxNameLen);
				}
				return null;
			}
			set
			{
				if (value != null)
				{
					BytesTool.writeString(value, _PatId.FirstName, 0, RawECGPatientId.MaxNameLen);
				}
			}
		}
		public string PatientID
		{
			get
			{
				if (_PatId.Id[0] != 0)
				{
					return BytesTool.readString(_PatId.Id, 0, RawECGPatientId.MaxIdLen);;
				}
				return null;
			}
			set
			{
				if (value != null)
				{
					BytesTool.writeString(value, _PatId.Id, 0, RawECGPatientId.MaxNameLen);
					_PatId.IdFormat = 0;
				}
			}
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
			if (_IdRecInfo.AgeYears != 0)
			{
				val = (ushort) _IdRecInfo.AgeYears;
				def = AgeDefinition.Years;
				return 0;
			}
			else if (_IdRecInfo.AgeMonths != 0)
			{
				val = (ushort) _IdRecInfo.AgeMonths;
				def = AgeDefinition.Months;
				return 0;
			}
			else if (_IdRecInfo.AgeDays != 0)
			{
				val = (ushort) _IdRecInfo.AgeDays;
				def = AgeDefinition.Days;
				return 0;
			}
			return 1;
		}
		public int setPatientAge(ushort val, AgeDefinition def)
		{
			if ((val != 0)
				&&	(def != AgeDefinition.Unspecified))
			{
				switch (def)
				{
					case AgeDefinition.Years:
						_IdRecInfo.AgeYears = (short) val;
						break;
					case AgeDefinition.Months:
						_IdRecInfo.AgeMonths = (short) val;
						break;
					case AgeDefinition.Weeks:
						_IdRecInfo.AgeDays = (short) (val * 7);
						break;
					case AgeDefinition.Days:
						_IdRecInfo.AgeDays = (short) val;
						break;
					case AgeDefinition.Hours:
						_IdRecInfo.AgeDays = (short) (val / 24);
						break;
				}
				return 0;
			}
			return 1;
		}
		public Date PatientBirthDate
		{
			get
			{
				if (_PatId.BirthDate[0] != 0)
				{
					try
					{
						Date date = new Date();
						string year = BytesTool.readString(_PatId.BirthDate, 0, 4);
						string month = BytesTool.readString(_PatId.BirthDate, 4, 2);
						string day = BytesTool.readString(_PatId.BirthDate, 6, 2);
						date.Year = ushort.Parse(year);
						date.Month = byte.Parse(month);
						date.Day = byte.Parse(day);

						return date;
					}
					catch (Exception)
					{
					}
				}
				return null;
			}
			set
			{
				if (value != null)
				{
					BytesTool.writeString(value.Year.ToString("d4"), _PatId.BirthDate, 0, 5);
					BytesTool.writeString(value.Month.ToString("d2"), _PatId.BirthDate, 4, 3);
					BytesTool.writeString(value.Day.ToString("d2"), _PatId.BirthDate, 6, 3);
				}
			}
		}
		public int getPatientHeight(out ushort val, out HeightDefinition def)
		{
			val = 0;
			def = HeightDefinition.Unspecified;
			if (_IdRecInfo.Weight != 0)
			{
				val = (ushort) _IdRecInfo.Height;
				def = HeightDefinition.Centimeters;
			}
			return 1;
		}
		public int setPatientHeight(ushort val, HeightDefinition def)
		{
			if ((val != 0)
			&&	(def != HeightDefinition.Unspecified))
			{
				switch (def)
				{
					case HeightDefinition.Centimeters:
						_IdRecInfo.Height = (short) val;
						break;
					case HeightDefinition.Millimeters:
						_IdRecInfo.Height = (short) (val / 10);
						break;
					case HeightDefinition.Inches:
						_IdRecInfo.Height = (short) ((val * 254) / 100);
						break;
				}
				return 0;
			}
			return 1;
		}
		public int getPatientWeight(out ushort val, out WeightDefinition def)
		{
			val = 0;
			def = WeightDefinition.Unspecified;
			if (_IdRecInfo.Height != 0)
			{
				val = (ushort) _IdRecInfo.Height;
				def = WeightDefinition.Kilogram;
				return 0;
			}
			return 1;
		}
		public int setPatientWeight(ushort val, WeightDefinition def)
		{
			if ((val != 0)
			&&	(def != WeightDefinition.Unspecified))
			{
				switch (def)
				{
					case WeightDefinition.Kilogram:
						_IdRecInfo.Height = (short) val;
						break;
					case WeightDefinition.Gram:
						_IdRecInfo.Height = (short) (val / 1000);
						break;
					case WeightDefinition.Ounce:
						_IdRecInfo.Height = (short) ((val * 10000) / 283);
						break;
					case WeightDefinition.Pound:
						_IdRecInfo.Height = (short) ((val * 10000) / 4536);
						break;

				}
				return 0;
			}
			return 1;
		}
		public Sex Gender
		{
			get
			{
				switch ((char) _PatId.Sex)
				{
					case 'F':
						return Sex.Female;
					case 'M':
						return Sex.Male;
				}
				return Sex.Null;
			}
			set
			{
				switch (value)
				{
					case Sex.Female:
						_PatId.Sex = (byte) 'F';
						break;
					case Sex.Male:
						_PatId.Sex = (byte) 'M';
						break;
					default:
						_PatId.Sex = 0;
						break;
				}
			}
		}
		public Race PatientRace
		{
			get
			{
				switch ((char) _PatId.Race)
				{
					case 'C':
						return Race.Caucasian;
					case 'M':
						return Race.Oriental;
					case 'N':
						return Race.Black;
				}
				return Race.Null;
			}
			set
			{
				switch(value)
				{
					case Race.Caucasian:
						_PatId.Race = (byte) 'C';
						break;
					case Race.Oriental:
						_PatId.Race = (byte) 'M';
						break;
					case Race.Black:
						_PatId.Race = (byte) 'N';
						break;
					default:
						_PatId.Race = 0;
						break;
				}
			}
		}
		public AcquiringDeviceID AcqMachineID
		{
			get
			{
				AcquiringDeviceID ret = new AcquiringDeviceID(true);
				ret.DeviceCapabilities = 0x88;
				return ret;
			}
			set
			{
				if (value != null)
				{
					BytesTool.writeString(value.DeviceID.ToString("d5"), _RecInfo.CartId, 0, RawECGRecordInfo.MaxCartIdLen);
					BytesTool.writeString("ErasmusMC", _RecInfo.Location, 0, RawECGRecordInfo.MaxLocationLen);
					BytesTool.writeString(((DeviceManufactor)value.ManufactorID).ToString(), _RecInfo.Site, 0, RawECGRecordInfo.MaxSiteLen);
					BytesTool.writeString("ECGConversion 2.0", _RecInfo.ProgramVersion, 0, RawECGRecordInfo.MaxProgramVersionLen);
				}
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
				try
				{
					return new DateTime(_Year, _Month, _Day, _Hour, _Min, _Sec);
				}
				catch {}

				return DateTime.MinValue;
			}
			set
			{
				if (value.Year > 1000)
				{
					_Year = (short) value.Year;
					_Month = (byte) value.Month;
					_Day = (byte) value.Day;
					_Hour = (byte) value.Hour;
					_Min = (byte) value.Minute;
					_Sec = (byte) value.Second;

					BytesTool.writeString(value.Year.ToString("d4"), _RecInfo.RecDate, 0, 5);
					BytesTool.writeString(value.Month.ToString("d2"), _RecInfo.RecDate, 4, 3);
					BytesTool.writeString(value.Day.ToString("d2"), _RecInfo.RecDate, 6, 3);

					BytesTool.writeString(value.Hour.ToString("d2"), _RecInfo.RecTime, 0, 3);
					BytesTool.writeString(value.Minute.ToString("d2"), _RecInfo.RecTime, 2, 3);
					BytesTool.writeString(value.Second.ToString("d2"), _RecInfo.RecTime, 4, 3);
				}
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
			set {}
		}
		public string SequenceNr
		{
			get
			{
				if (_RecInfo.ECGId[0] != 0)
				{
					return BytesTool.readString(_RecInfo.ECGId, 0, RawECGRecordInfo.MaxECGIdLen);
				}
				return null;
			}
			set
			{
				if (value != null)
				{
					BytesTool.writeString(value, _RecInfo.ECGId, 0, RawECGRecordInfo.MaxECGIdLen);
				}
			}
		}
		public string AcqInstitution
		{
			get {return null;}
			set {}
		}
		public string AnalyzingInstitution
		{
			get {return null;}
			set {}
		}
		public string AcqDepartment
		{
			get {return null;}
			set {}
		}
		public string AnalyzingDepartment
		{
			get {return null;}
			set {}
		}
		public string ReferringPhysician
		{
			get {return null;}
			set {}
		}
		public string OverreadingPhysician
		{
			get {return null;}
			set {}
		}
		public string TechnicianDescription
		{
			get {return null;}
			set {}
		}
		public ushort SystolicBloodPressure
		{
			get
			{
				return (ushort) _IdRecInfo.SysBP;
			}
			set
			{
				_IdRecInfo.SysBP = (short) value;
			}
		}
		public ushort DiastolicBloodPressure
		{
			get
			{
				return (ushort) _IdRecInfo.DiaBP;
			}
			set
			{
				_IdRecInfo.DiaBP = (short) value;
			}
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
        public int getSignals(out Signals signals)
        {
            signals = new Signals();
            int err = getSignalsToObj(signals);
            if (err != 0)
            {
                signals = null;
            }
            return 0;
        }
        public int getSignalsToObj(Signals signals)
        {
            if ((Works())
			&&	(signals != null))
            {
                signals.NrLeads = (byte) _ECGInfo.NrOfLeads;

                signals.RhythmAVM = (1000.0 / (double) _ECGInfo.ECGLSBPerMV);
                signals.RhythmSamplesPerSecond = _ECGInfo.ECGSampFreq;

                for (int loper=0;loper < _ECGInfo.NrOfLeads;loper++)
                {
                    signals[loper] = new Signal();
                    signals[loper].RhythmStart = 0;
                    signals[loper].RhythmEnd = (int) _ECGInfo.NrECGSamples;

                    switch (_ECGInfo.LeadConfig[loper])
                    {
                        case 0:
                            signals[loper].Type = LeadConfiguration[0];
                            break;
                        case 1:
                            signals[loper].Type = LeadConfiguration[1];
                            break;
                        case 2:
                            signals[loper].Type = LeadConfiguration[2];
                            break;
                        case 3:
                            signals[loper].Type = LeadConfiguration[3];
                            break;
                        case 4:
                            signals[loper].Type = LeadConfiguration[4];
                            break;
                        case 5:
                            signals[loper].Type = LeadConfiguration[5];
                            break;
                        case 6:
                            signals[loper].Type = LeadConfiguration[6];
                            break;
                        case 7:
                            signals[loper].Type = LeadConfiguration[7];
                            break;
                        case 8:
                            signals[loper].Type = LeadConfiguration[8];
                            break;
                        case 9:
                            signals[loper].Type = LeadConfiguration[9];
                            break;
                        case 10:
                            signals[loper].Type = LeadConfiguration[10];
                            break;
                        case 11:
                            signals[loper].Type = LeadConfiguration[11];
                            break;
                        default:
                            signals[loper].Type = LeadType.Unknown;
                            break;
                    }
                }
                return 0;
            }
            return 1;
        }
        public int setSignals(Signals signals)
        {
            if ((signals != null)
			&&	(signals.NrLeads != 0)
			&&	(signals.RhythmAVM > 0)
			&&	(signals.RhythmSamplesPerSecond > 0))
            {
                _ECGInfo.NrOfLeads = (short) RawECGFormat.NeededLeads.Length;

                _ECGInfo.ECGLSBPerMV = 400;
                _ECGInfo.ECGSampFreq = 500;

                _ECGInfo.TremorType = 0;

                for (int loper=0;loper < _ECGInfo.NrOfLeads;loper++)
                {
                    _ECGInfo.LeadConfig[loper] = (short) RawECGFormat.NeededLeads[loper];// signals[loper].Type;
                }

                int minstart = int.MaxValue;
                int maxend = int.MinValue;

                for (int loper=0;loper < signals.NrLeads;loper++)
                {
                    if (signals[loper] != null)
                    {
                        minstart = Math.Min(minstart, signals[loper].RhythmStart);
                        maxend = Math.Max(maxend, signals[loper].RhythmEnd);
                    }
                }

                if ((minstart != int.MaxValue)
				&&	(maxend != int.MinValue)
				&&	(minstart < maxend))
                {
                    _ECGInfo.NrECGSamples = (uint) (((maxend - minstart) * 500) / signals.RhythmSamplesPerSecond);
                }

                return 0;
            }
            return 1;
        }
        
        public void WriteInfo(System.IO.StreamWriter output)
        {   //Write leadinfo
            for (int loper=0;loper < _ECGInfo.NrOfLeads;loper++)
            {
                output.Write(" ");
                output.Write(((LeadType)_ECGInfo.LeadConfig[loper]).ToString());
            }
            output.Write("\n");
        }
    }
}

