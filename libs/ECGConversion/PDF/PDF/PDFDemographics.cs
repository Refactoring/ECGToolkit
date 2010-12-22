/***************************************************************************
Copyright 2008,2010, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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

namespace ECGConversion.PDF
{
	/// <summary>
	/// Summary description for PDFDemographics.
	/// </summary>
	public class PDFDemographics : IDemographic
	{
		private string _PatientID;
		private DateTime _AcqTime = DateTime.MinValue;
		private string _LastName;
		private string _FirstName;
		private string _SecondLastName;
		private string _PrefixName;
		private string _SuffixName;

		private ushort _PatientAgeVal;
		private AgeDefinition _PatientAgeDef;

		private ushort _PatientHeightVal;
		private HeightDefinition _PatientHeightDef;

		private ushort _PatientWeightVal;
		private WeightDefinition _PatientWeightDef;

		private Date _PatientBirthDate;
		private Sex _Gender;
		private Race _PatientRace;
		private ushort _BaselineFilter;
		private ushort _LowpassFilter;
		private byte _FilterBitmap;

		private string _AcqInstitution;
		private string _AcqDepartment;
		private string _AnalyzingDepartment;
		private string _AnalyzingInstitution;

		private ushort _DiastolicBloodPressure = ECGConversion.ECGGlobalMeasurements.GlobalMeasurement.NoValue;
		private ushort _SystolicBloodPressure = ECGConversion.ECGGlobalMeasurements.GlobalMeasurement.NoValue;

		private string[] _ReferralIndication;
		private string _ReferringPhysician;
		private string _OverreadingPhysician;
		private string _RoomDescription;
		private string _SequenceNr;
		private string _TechnicianDescription;

		private string[] _FreeTextFields;
		private Drug[] _Drugs;
		private byte _StatCode = 0xff;

		public PDFDemographics()
		{
		}

		public bool Works()
		{
			return _PatientID != null;
		}

		#region IDemographic Members

		public void Init()
		{
			_PatientID = null;
			_AcqTime = DateTime.MinValue;
			_LastName = null;
			_FirstName = null;
			_SecondLastName = null;
			_PrefixName = null;
			_SuffixName = null;

			_PatientAgeVal = 0;
			_PatientAgeDef = AgeDefinition.Unspecified;

			_PatientHeightVal = 0;
			_PatientHeightDef = HeightDefinition.Unspecified;

			_PatientWeightVal = 0;
			_PatientWeightDef = WeightDefinition.Unspecified;

			_PatientBirthDate = null;
			_Gender = Sex.Null;
			_PatientRace = Race.Null;

			_BaselineFilter = 0;
			_LowpassFilter = 0;
			_FilterBitmap = 0;

			_AcqInstitution = null;
			_AcqDepartment = null;

			 _AnalyzingDepartment = null;
			_AnalyzingInstitution = null;

			_DiastolicBloodPressure = ECGConversion.ECGGlobalMeasurements.GlobalMeasurement.NoValue;
			_SystolicBloodPressure = ECGConversion.ECGGlobalMeasurements.GlobalMeasurement.NoValue;

			_ReferralIndication = null;
			_ReferringPhysician = null;
			_OverreadingPhysician = null;
			_RoomDescription = null;
			_SequenceNr = null;
			_TechnicianDescription = null;

			_FreeTextFields = null;
			_Drugs = null;
			_StatCode = 0xff;
		}

		public string LastName
		{
			get {return _LastName;}
			set {_LastName = value;}
		}

		public string FirstName
		{
			get {return _FirstName;}
			set {_FirstName = value;}
		}

		public string PatientID
		{
			get {return _PatientID;}
			set {_PatientID = value;}
		}

		public string SecondLastName
		{
			get {return _SecondLastName;}
			set {_SecondLastName = value;}
		}

		public string PrefixName
		{
			get {return _PrefixName;}
			set {_PrefixName = value;}
		}

		public string SuffixName
		{
			get {return _SuffixName;}
			set {_SuffixName = value;}
		}

		public int getPatientAge(out ushort val, out AgeDefinition def)
		{
			val = _PatientAgeVal;
			def = _PatientAgeDef;

			return def == AgeDefinition.Unspecified ? 1 : 0;
		}

		public int setPatientAge(ushort val, AgeDefinition def)
		{
			_PatientAgeVal = val;
			_PatientAgeDef = def;

			return 0;
		}

		public Date PatientBirthDate
		{
			get {return _PatientBirthDate;}
			set {_PatientBirthDate = value;}
		}

		public int getPatientHeight(out ushort val, out HeightDefinition def)
		{
			val = _PatientHeightVal;
			def = _PatientHeightDef;

			return 0;
		}

		public int setPatientHeight(ushort val, HeightDefinition def)
		{
			_PatientHeightVal = val;
			_PatientHeightDef = def;

			return 0;
		}

		public int getPatientWeight(out ushort val, out WeightDefinition def)
		{
			val = _PatientWeightVal;
			def = _PatientWeightDef;

			return 0;
		}

		public int setPatientWeight(ushort val, WeightDefinition def)
		{
			_PatientWeightVal = val;
			_PatientWeightDef = def;

			return 0;
		}

		public Sex Gender
		{
			get {return _Gender;}
			set {_Gender = value;}
		}

		public Race PatientRace
		{
			get {return _PatientRace;}
			set {_PatientRace = value;}
		}

		public AcquiringDeviceID AcqMachineID
		{
			get {return new AcquiringDeviceID();}
			set {}
		}

		public AcquiringDeviceID AnalyzingMachineID
		{
			get {return null;}
			set {}
		}

		public DateTime TimeAcquisition
		{
			get {return _AcqTime;}
			set {_AcqTime = value;}
		}

		public ushort BaselineFilter
		{
			get {return _BaselineFilter;}
			set {_BaselineFilter = value;}
		}

		public ushort LowpassFilter
		{
			get {return _LowpassFilter;}
			set {_LowpassFilter = value;}
		}

		public byte FilterBitmap
		{
			get {return _FilterBitmap;}
			set {_FilterBitmap = value;}
		}

		public string[] FreeTextFields
		{
			get {return _FreeTextFields;}
			set {_FreeTextFields = value;}
		}

		public string SequenceNr
		{
			get {return _SequenceNr;}
			set {_SequenceNr = value;}
		}

		public string AcqInstitution
		{
			get {return _AcqInstitution;}
			set {_AcqInstitution = value;}
		}

		public string AnalyzingInstitution
		{
			get {return _AnalyzingInstitution;}
			set {_AnalyzingInstitution = value;}
		}

		public string AcqDepartment
		{
			get {return _AcqDepartment;}
			set {_AcqDepartment = value;}
		}

		public string AnalyzingDepartment
		{
			get {return _AnalyzingDepartment;}
			set {_AnalyzingDepartment = value;}
		}

		public string ReferringPhysician
		{
			get {return _ReferringPhysician;}
			set {_ReferringPhysician = value;}
		}

		public string OverreadingPhysician
		{
			get {return _OverreadingPhysician;}
			set {_OverreadingPhysician = value;}
		}

		public string TechnicianDescription
		{
			get {return _TechnicianDescription;}
			set {_TechnicianDescription = value;}
		}

		public ushort SystolicBloodPressure
		{
			get {return _SystolicBloodPressure;}
			set {_SystolicBloodPressure = value;}
		}

		public ushort DiastolicBloodPressure
		{
			get {return _DiastolicBloodPressure;}
			set {_DiastolicBloodPressure = value;}
		}

		public Drug[] Drugs
		{
			get {return _Drugs;}
			set {_Drugs = value;}
		}

		public string[] ReferralIndication
		{
			get {return _ReferralIndication;}
			set {_ReferralIndication = value;}
		}

		public string RoomDescription
		{
			get {return _RoomDescription;}
			set {_RoomDescription = value;}
		}

		public byte StatCode
		{
			get {return _StatCode;}
			set {_StatCode = value;}
		}

		#endregion
	}
}
