/***************************************************************************
Copyright 2004, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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

namespace ECGConversion.ECGDemographics
{
	/// <summary>
	/// Device information that can be imported and exported in both SCP and UNIPRO.
	/// </summary>
	public class AcquiringDeviceID
	{
		public AcquiringDeviceID()
		{
		}
		public AcquiringDeviceID(bool bNoDeviceId)
		{
			if (bNoDeviceId)
			{
				InstitutionNr = 0;
				DepartmentNr = 11;
				DeviceID = 51;
				DeviceType = (byte) ECGConversion.ECGDemographics.DeviceType.System;
				ManufactorID = (byte) DeviceManufactor.Unknown;
				DeviceCapabilities = 0x8;
				ACFrequencyEnvironment = 1;
				Communication.IO.Tools.BytesTool.writeString("MCONV", ModelDescription, 0, ModelDescription.Length);
			}
		}

		private static int _ModelDescriptionLen = 6;
		public ushort InstitutionNr = 0;
		public ushort DepartmentNr = 0;
		public ushort DeviceID = 0;
		public byte DeviceType = 0;
		public byte ManufactorID = (byte) DeviceManufactor.Unknown;
		public byte DeviceCapabilities = 0; // Is defined in SCP Section1 tag 14 byte 18.
		public byte ACFrequencyEnvironment = 0; // Is defined in SCP Section1 tag 14 byte 19.
		public byte[] ModelDescription = new byte[_ModelDescriptionLen];
	}
}
