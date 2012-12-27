/***************************************************************************
Copyright 2012, van Ettinger Information Technology, Lopik, The Netherlands

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

using ECGConversion.ECGDemographics;

namespace ECGConversion.ISHNE
{
	public class ISHNEHeader : Communication.IO.Tools.DataSection, IDemographic
	{
		public override StringType TypeOfString
		{
			get {return StringType.FIXED_LENGTH;}
		}

		public override bool LittleEndian
		{
			get {return true;}
		}

		public override int Pack
		{
			get {return 1;}
		}

		public override int Size ()
		{
			return 512;
		}

		// ISHNE defines
		public const int DATE_LEN = 3;
		public const int TIME_LEN = 3;
		public const int MAX_NR_LEADS = 12;
		public const double AMPLITUDE_TO_UV = 0.001;
		public const double UV_TO_AMPLITUDE = 1000;

		// empty values
		public const Int16 EMPTY_VAL = -9;

		// String lengths
		public const int PatientFirstName_Length	= 40;
		public const int PatientLastName_Length		= 40;
		public const int PatientId_Length			= 20;
		public const int ECGRecorder_Length			= 40;
		public const int ECGProprietary_Length		= 80;
		public const int ECGCopyright_Length		= 80;
		public const int Reserved_Length			= 88;

		// the data structure
		public Int32 VarBlockSize;
		public Int32 ECGNrSamples;
		public Int32 VarBlockOffset;
		public Int32 ECGOffset;
		public Int16 FileVersion;
		public String PatientFirstName;
		public String PatientLastName;
		public String PatientId;
		public Int16 PatientSex;
		public Int16 PatientRaceCode;
		public Int16[] PatientBirthdate = new Int16[DATE_LEN];
		public Int16[] ECGRecordDate = new Int16[DATE_LEN];
		public Int16[] ECGFileDate = new Int16[DATE_LEN];
		public Int16[] ECGRecordTime = new Int16[TIME_LEN];
		public Int16 ECGNrLeads;
		public Int16[] ECGLeadSpecification = new Int16[MAX_NR_LEADS];
		public Int16[] ECGLeadQuality = new Int16[MAX_NR_LEADS];
		public Int16[] ECGLeadResolution = new Int16[MAX_NR_LEADS];
		public Int16 Pacemaker;
		public String ECGRecorder;
		public Int16 ECGSampleRate;
		public String ECGProprietary;
		public String ECGCopyright;
		public String Reserved;

		public ISHNEHeader()
		{
			Empty();
		}

		public override void Empty()
		{
			VarBlockSize = EMPTY_VAL;
			ECGNrSamples = EMPTY_VAL;
			VarBlockOffset = EMPTY_VAL;
			ECGOffset = EMPTY_VAL;
			FileVersion = EMPTY_VAL;
			PatientFirstName = null;
			PatientLastName = null;
			PatientId = null;
			PatientSex = EMPTY_VAL;
			PatientRaceCode = EMPTY_VAL;

			for (int i=0;i < DATE_LEN;i++)
			{
				PatientBirthdate[i] = EMPTY_VAL;
				ECGRecordDate[i] = EMPTY_VAL;
				ECGFileDate[i] = EMPTY_VAL;
				ECGRecordTime[i] = EMPTY_VAL;
			}

			ECGNrLeads = EMPTY_VAL;

			for (int i=0;i < MAX_NR_LEADS;i++)
			{
				ECGLeadSpecification[i] = EMPTY_VAL;
				ECGLeadQuality[i] = EMPTY_VAL;
				ECGLeadResolution[i] = EMPTY_VAL;
			}

			Pacemaker = EMPTY_VAL;
			ECGRecorder = null;
			ECGSampleRate = EMPTY_VAL;
			ECGProprietary = null;
			ECGCopyright = null;
			Reserved = null;
		}

		public override bool Works()
		{
			return (VarBlockSize >= 0)
				&& (ECGNrSamples > 0)
				&& (VarBlockOffset == (Size() + ISHNEFormat.BYTES_BEFORE_HEADER))
				&& (ECGOffset == (VarBlockSize + VarBlockOffset))
				&& (FileVersion != EMPTY_VAL)
				&& (PatientLastName != null)
				&& (PatientLastName.Length > 0)
				&& (PatientId != null)
				&& (PatientId.Length > 0)
				&& (PatientSex >= 0)
				&& (PatientRace >= 0)
				&& (PatientBirthdate.Length == DATE_LEN)
				&& (ECGRecordDate.Length == DATE_LEN)
				&& (ECGRecordDate[0] != EMPTY_VAL)
				&& (ECGRecordDate[1] != EMPTY_VAL)
				&& (ECGRecordDate[2] != EMPTY_VAL)
				&& (ECGFileDate.Length == DATE_LEN)
				&& (ECGRecordTime.Length == TIME_LEN)
				&& (ECGRecordTime[0] != EMPTY_VAL)
				&& (ECGRecordTime[1] != EMPTY_VAL)
				&& (ECGRecordTime[2] != EMPTY_VAL)
				&& CheckLeads()
				&& (ECGSampleRate > 0);
		}

		public bool CheckLeads()
		{
			if ((ECGNrLeads > 0)
			&&  (ECGNrLeads <= MAX_NR_LEADS))
			{
				for (int i=0;i < ECGNrLeads;i++)
				{
					if ((ECGLeadSpecification[i] < 0)
					||	(ECGLeadSpecification[i] > 19)
					||	(ECGLeadQuality[i] < 0)
					||	(ECGLeadQuality[i] > 5)
					||	(ECGLeadResolution[i] <= 0))
					{
						return false;
					}
				}
				return true;
			}

			return false;
		}

		public ECGSignals.LeadType GetLeadType(int i)
		{
			ECGSignals.LeadType ret = ECGSignals.LeadType.Unknown;

			if ((i >= 0)
			&&	(i < ECGNrLeads)
			&&	(i < MAX_NR_LEADS))
			{
				switch (ECGLeadSpecification[i])
				{
					case 2: ret = ECGConversion.ECGSignals.LeadType.X; break;
					case 3: ret = ECGConversion.ECGSignals.LeadType.Y; break;
					case 4: ret = ECGConversion.ECGSignals.LeadType.Z; break;
					case 5: ret = ECGConversion.ECGSignals.LeadType.I; break;
					case 6: ret = ECGConversion.ECGSignals.LeadType.II; break;
					case 7: ret = ECGConversion.ECGSignals.LeadType.III; break;
					case 8: ret = ECGConversion.ECGSignals.LeadType.aVR; break;
					case 9: ret = ECGConversion.ECGSignals.LeadType.aVL; break;
					case 10: ret = ECGConversion.ECGSignals.LeadType.aVF; break;
					case 11: ret = ECGConversion.ECGSignals.LeadType.V1; break;
					case 12: ret = ECGConversion.ECGSignals.LeadType.V2; break;
					case 13: ret = ECGConversion.ECGSignals.LeadType.V3; break;
					case 14: ret = ECGConversion.ECGSignals.LeadType.V4; break;
					case 15: ret = ECGConversion.ECGSignals.LeadType.V5; break;
					case 16: ret = ECGConversion.ECGSignals.LeadType.V6; break;
					case 17: ret = ECGConversion.ECGSignals.LeadType.ES; break;
					case 18: ret = ECGConversion.ECGSignals.LeadType.AS; break;
					case 19: ret = ECGConversion.ECGSignals.LeadType.AI; break;
				}
			}

			return ret;
		}

		public byte GetLeadType(byte leadNr, ECGSignals.LeadType leadType)
		{
			int i=ECGNrLeads;

			if (leadType != ECGSignals.LeadType.Unknown)
			{
				for (i=0;i < ECGNrLeads;i++)
					if (leadType == GetLeadType(i))
						break;
			}

			return i < ECGNrLeads ? (byte)i : leadNr;
		}

		public void SetLeadType(int leadNr, ECGSignals.LeadType leadType)
		{
			if ((leadNr >= 0)
			&&	(leadNr < ECGNrLeads))
			{
			Int16 val = 0;

				switch (leadType)
				{
					case ECGConversion.ECGSignals.LeadType.X: val = 2; break;
					case ECGConversion.ECGSignals.LeadType.Y: val = 3; break;
					case ECGConversion.ECGSignals.LeadType.Z: val = 4; break;
					case ECGConversion.ECGSignals.LeadType.I: val = 5; break;
					case ECGConversion.ECGSignals.LeadType.II: val = 6; break;
					case ECGConversion.ECGSignals.LeadType.III: val = 7; break;
					case ECGConversion.ECGSignals.LeadType.aVR: val = 8; break;
					case ECGConversion.ECGSignals.LeadType.aVL: val = 9; break;
					case ECGConversion.ECGSignals.LeadType.aVF: val = 10; break;
					case ECGConversion.ECGSignals.LeadType.V1: val = 11; break;
					case ECGConversion.ECGSignals.LeadType.V2: val = 12; break;
					case ECGConversion.ECGSignals.LeadType.V3: val = 13; break;
					case ECGConversion.ECGSignals.LeadType.V4: val = 14; break;
					case ECGConversion.ECGSignals.LeadType.V5: val = 15; break;
					case ECGConversion.ECGSignals.LeadType.V6: val = 16; break;
					case ECGConversion.ECGSignals.LeadType.ES: val = 17; break;
					case ECGConversion.ECGSignals.LeadType.AS: val = 18; break;
					case ECGConversion.ECGSignals.LeadType.AI: val = 19; break;
				}
				ECGLeadSpecification[leadNr] = val;
			}
		}

		public double GetLeadAmplitude(int i)
		{
			double ret = double.NaN;

			if ((i >= 0)
			&&	(i < ECGNrLeads)
			&&	(i < MAX_NR_LEADS)
			&&	(ECGLeadResolution[i] > 0))
			{
				ret = ECGLeadResolution[i] * AMPLITUDE_TO_UV;
			}

			return ret;
		}

#region IDemographic
		public void Init()
		{
			Empty();

			VarBlockOffset = Size() + ISHNEFormat.BYTES_BEFORE_HEADER;
			VarBlockSize = 0;
			ECGOffset = VarBlockOffset + VarBlockSize;

			FileVersion = 1;

			PatientSex = 0;
			PatientRace = 0;

			DateTime now = DateTime.Now;

			ECGFileDate[0] = (Int16)now.Day;
			ECGFileDate[1] = (Int16)now.Month;
			ECGFileDate[2] = (Int16)now.Year;
		}

		public string LastName
		{
			get {return PatientLastName;}
			set {PatientLastName = value;}
		}

		public string FirstName
		{
			get {return PatientLastName;}
			set {PatientLastName = value;}
		}
		
		public string PatientID
		{
			get {return PatientId;}
			set {PatientId = value;}
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
			get
			{
				Date ret = null;

				try
				{
					if ((PatientBirthdate[0] != EMPTY_VAL)
					&&	(PatientBirthdate[1] != EMPTY_VAL)
					&&	(PatientBirthdate[2] != EMPTY_VAL))
					{
						ret = new Date((ushort)PatientBirthdate[2], (byte)PatientBirthdate[1], (byte)PatientBirthdate[0]);
					}
				} catch {}

				return ret;
			}
			set
			{
				if (value != null)
				{
					PatientBirthdate[0] = (Int16)value.Day;
					PatientBirthdate[1] = (Int16)value.Month;
					PatientBirthdate[2] = (Int16)value.Year;
				}
				else
				{
					PatientBirthdate[0] = EMPTY_VAL;
					PatientBirthdate[1] = EMPTY_VAL;
					PatientBirthdate[2] = EMPTY_VAL;
				}
			}
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
			get
			{
				Sex ret = Sex.Null;

				if (PatientSex >= 0)
					ret = (Sex) PatientSex;

				return ret;
			}
			set
			{
				if (value == Sex.Null)
					PatientSex = EMPTY_VAL;
				else
					PatientSex = (Int16)value;
			}
		}

		public Race PatientRace
		{
			get
			{
				Race ret = Race.Null;

				if (PatientRaceCode >= 0)
					ret = (Race) PatientRaceCode;

				return ret;
			}
			set
			{
				if (value == Race.Null)
					PatientRaceCode = EMPTY_VAL;
				else
					PatientRaceCode = (Int16)value;

			}
		}

		public AcquiringDeviceID AcqMachineID 
		{
			get
			{
				AcquiringDeviceID ret = new AcquiringDeviceID(true);

				if (ECGRecorder != null)
				{
					Communication.IO.Tools.BytesTool.writeString(ECGRecorder, ret.ModelDescription, 0, ret.ModelDescription.Length);
				}

				return ret;
			}
			set
			{
				if (value != null)
				{
					ECGRecorder = Communication.IO.Tools.BytesTool.readString(value.ModelDescription, 0, value.ModelDescription.Length);
				}
				else
				{
					ECGRecorder = null;
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
				DateTime ret = DateTime.MinValue;

				try
				{
					ret = new DateTime(ECGRecordDate[2], ECGRecordDate[1], ECGRecordDate[0], ECGRecordTime[0], ECGRecordTime[1], ECGRecordTime[2]);
				} catch {}

				return ret;
			}
			set
			{
				if (value != DateTime.MinValue)
				{
					ECGRecordDate[0] = (Int16)value.Day;
					ECGRecordDate[1] = (Int16)value.Month;
					ECGRecordDate[2] = (Int16)value.Year;

					ECGRecordTime[0] = (Int16)value.Hour;
					ECGRecordTime[1] = (Int16)value.Minute;
					ECGRecordTime[2] = (Int16)value.Second;
				}
				else
				{
					ECGRecordDate[0] = EMPTY_VAL;
					ECGRecordDate[1] = EMPTY_VAL;
					ECGRecordDate[2] = EMPTY_VAL;

					ECGRecordTime[0] = EMPTY_VAL;
					ECGRecordTime[1] = EMPTY_VAL;
					ECGRecordTime[2] = EMPTY_VAL;
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
			get {return 0xff;}
			set {}
		}

		public string[] FreeTextFields
		{
			get {return null;}
			set {}
		}

		public string SequenceNr
		{
			get {return null;}
			set {}
		}

		public string AcqInstitution
		{
			get
			{
				return ECGProprietary;
			}
			set
			{
				ECGProprietary = value;
			}
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
			get {return 0;}
			set {}
		}
#endregion
	}
}

