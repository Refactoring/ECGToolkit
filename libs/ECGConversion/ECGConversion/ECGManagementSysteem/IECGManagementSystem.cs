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
	public abstract class IECGManagementSystem
	{
		/// <summary>
		/// Function to get whether configuration works.
		/// </summary>
		/// <returns>true if configuration is correct.</returns>
		public bool Works()
		{
			return Config == null || Config.ConfigurationWorks();
		}
		/// <summary>
		/// Function to get Name of ECG Management System.
		/// </summary>
		public abstract string Name {get;}
		/// <summary>
		/// Function to get Name of ECG Management System.
		/// </summary>
		public abstract string FormatName {get;}
		/// <summary>
		/// Function to get configuration
		/// </summary>
		public abstract ECGConfig Config {get;}
		/// <summary>
		/// Function to get a list of ECG
		/// </summary>
		/// <param name="patid">Patient ID</param>
		/// <returns>array of all ECGs</returns>
		public abstract ECGInfo[] getECGList(string patid);
		/// <summary>
		/// Get an ECG by info.
		/// </summary>
		/// <param name="ecgInfo">the ECG info</param>
		/// <returns>an ECG file</returns>
		public virtual IECGFormat getECG(ECGInfo ecgInfo)
		{
			if ((ecgInfo != null)
			&&	(ecgInfo.UniqueIdentifier != null))
				return getECGByUI(ecgInfo.UniqueIdentifier);

			return null;
		}
		/// <summary>
		/// Get an ECG by Unique Identifier
		/// </summary>
		/// <param name="ui">the Unique Identifier</param>
		/// <returns>an ECG file</returns>
		public abstract IECGFormat getECGByUI(object ui);
		/// <summary>
		/// Attribute whether ECG Management System can save ECGs
		/// </summary>
		/// <returns>true if</returns>
		public bool CanSave()
		{
			try
			{
				return SaveECG(null) != -1;
			}
			catch {}

			return false;
		}
		/// <summary>
		/// Attribute whether ECG Management System can save ECGs when configured.
		/// </summary>
		/// <returns>true if configured to save</returns>
		public bool ConfiguredToSave()
		{
			try
			{
				int result = SaveECG(null);

				return result != 1 && result != -1;
			}
			catch {}

			return false;
		}
		/// <summary>
		/// Function to save an ECG to an ECG Management System.
		/// </summary>
		/// <param name="ecg">ECG file to save</param>
		/// <returns>
		/// -1 if not supported
		/// 0 if successfull
		/// 1 if improperly configured
		/// 2 if can't convert to correct format
		/// 3 if saving has failed
		/// > 3 if specific failure.
		/// </returns>
		public int SaveECG(IECGFormat ecg)
		{
			return SaveECG(ecg, null, null);
		}
		/// <summary>
		/// Function to save an ECG to an ECG Management System.
		/// </summary>
		/// <param name="ecg">ECG file to save</param>
		/// <param name="patid">patient id to store to</param>
		/// <returns>
		/// -1 if not supported
		/// 0 if successfull
		/// 1 if improperly configured
		/// 2 if can't convert to correct format
		/// 3 if saving has failed
		/// > 3 if specific failure.
		/// </returns>
		public int SaveECG(IECGFormat ecg, string patid)
		{
			return SaveECG(ecg, patid, null);
		}
		/// <summary>
		/// Function to save an ECG to an ECG Management System.
		/// </summary>
		/// <param name="ecg">ECG file to save</param>
		/// <param name="patid">patient id to store to</param>
		/// <param name="cfg">configuration to set towards</param>
		/// <returns>
		/// -1 if not supported
		/// 0 if successfull
		/// 1 if improperly configured
		/// 2 if can't convert to correct format
		/// 3 if saving has failed
		/// > 3 if specific failure.
		/// </returns>
		public abstract int SaveECG(IECGFormat ecg, string patid, ECGConfig cfg);
	}
}
