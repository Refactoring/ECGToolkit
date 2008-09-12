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
using ECGConversion.DICOM;

namespace ECGConversion
{
	/// <summary>
	/// Class that will load in all supported formats added by this plugin.
	/// </summary>
	public class ECGLoad
	{
		/// <summary>
		/// Function that returns a list of all plugins formats in this plugin.
		/// </summary>
		/// <returns>list of all plugin formats</returns>
		public static ECGPlugin[] LoadPlugin()
		{
			return new ECGPlugin[] {new ECGPlugin("DICOM", "dcm", typeof(DICOMFormat), typeof(DICOMReader), true)};
		}

		/// <summary>
		/// Function that returns a list of all ECG Management Systems in this plugin.
		/// </summary>
		/// <returns>list of all ECG Management Systems</returns>
		public static ECGManagementSystem.IECGManagementSystem[] LoadECGMS()
		{
			return new ECGManagementSystem.IECGManagementSystem[] {new PACS()};
		}
	}
}