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

namespace ECGConversion.ECGGlobalMeasurements
{
	/// <summary>
	/// Class containing one spike (SCP defined).
	/// </summary>
	public class Spike
	{
		public ushort Time = GlobalMeasurement.NoValue;
		public short Amplitude = GlobalMeasurement.NoAxisValue;
	}
}
