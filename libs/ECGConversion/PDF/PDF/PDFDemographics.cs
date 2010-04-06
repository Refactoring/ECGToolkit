/***************************************************************************
Copyright 2008, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
		private Date _PatientBirthDate;
		private Sex _Gender;
		private Race _PatientRace;
		private ushort _BaselineFilter;
		private ushort _LowpassFilter;
		private byte _FilterBitmap;
		private string _AcqInstitution;
		private string _AcqDepartment;

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
			_PatientBirthDate = null;
			_Gender = Sex.Null;
			_PatientRace = Race.Null;

			_BaselineFilter = 0;
			_LowpassFilter = 0;
			_FilterBitmap = 0;

			_AcqInstitution = null;
			_AcqDepartment = null;
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
			get {return _AcqInstitution;}
			set {_AcqInstitution = value;}
		}

		public string AnalyzingInstitution
		{
			get {return null;}
			set {}
		}

		public string AcqDepartment
		{
			get {return _AcqDepartment;}
			set {_AcqDepartment = value;}
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
			get {return 0xff;}
			set {}
		}

		#endregion
	}
}
