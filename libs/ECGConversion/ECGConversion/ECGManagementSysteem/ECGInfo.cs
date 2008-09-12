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

namespace ECGConversion.ECGManagementSystem
{
	/// <summary>
	/// ECG Info provided by Management System.
	/// </summary>
	public class ECGInfo
	{
		public ECGInfo(object ui, string patid, string name, DateTime acqtime)
		{
			_UniqueIdentifier = ui;
			_PatientID = patid;
			_PatientName = name;
			_AcquisitionTime = acqtime;
		}

		public ECGInfo(object ui, string patid, string name, DateTime acqtime, Sex gender)
		{
			_UniqueIdentifier = ui;
			_PatientID = patid;
			_PatientName = name;
			_AcquisitionTime = acqtime;
			_Gender = gender;
		}

		public object UniqueIdentifier
		{
			get
			{
				return _UniqueIdentifier;
			}
		}
		private object _UniqueIdentifier;

		public string PatientID
		{
			get
			{
				return _PatientID;
			}
		}
		private string _PatientID;
		
		public string PatientName
		{
			get
			{
				return _PatientName;
			}
		}
		private string _PatientName;

		public DateTime AcquisitionTime
		{
			get
			{
				return _AcquisitionTime;
			}
		}

		private DateTime _AcquisitionTime;

		public Sex Gender
		{
			get
			{
				return _Gender;
			}
		}

		private Sex _Gender = Sex.Null;
	}
}
