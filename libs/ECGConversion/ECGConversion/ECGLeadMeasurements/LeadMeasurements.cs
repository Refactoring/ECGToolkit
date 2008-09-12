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
using System.Collections;

namespace ECGConversion.ECGLeadMeasurements
{
	public class LeadMeasurements
	{
		public LeadMeasurements()
		{}

		public LeadMeasurements(int nr)
		{
			Measurements = new LeadMeasurement[nr];

			for (int i=0;i < Measurements.Length;i++)
				Measurements[i] = new LeadMeasurement();
		}

		public LeadMeasurement[] Measurements;
	}
}
