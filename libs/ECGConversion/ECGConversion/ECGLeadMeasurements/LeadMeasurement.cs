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

using ECGConversion.ECGSignals;

namespace ECGConversion.ECGLeadMeasurements
{
	public class LeadMeasurement
	{
		public static short NoValue = 29999;

		public LeadMeasurement()
		{}

		public LeadMeasurement(LeadType lt)
		{
			LeadType = lt;
		}

		public LeadType LeadType = LeadType.Unknown;

		private SortedList _List = new SortedList();

		public short this[MeasurementType mt]
		{
			get
			{
				int index = _List.IndexOfKey(mt);

				return index >= 0 ? (short) _List.GetByIndex(index) : NoValue;
			}
			set
			{
				int index = _List.IndexOfKey(mt);

				if (value == NoValue)
				{
					if (index >= 0)
						_List.RemoveAt(index);
				}
				else
				{
					if (index >= 0)
						_List.SetByIndex(index, value);
					else
						_List.Add(mt, value);
				}
			}
		}

		public int Count
		{
			get
			{
				return _List.Count;
			}
		}

		public short getValueByIndex(int index)
		{
			return (index >= 0) && (index < _List.Count) ? (short) _List.GetByIndex(index) : NoValue;
		}

		public MeasurementType getKeyByIndex(int index)
		{
			return (index >= 0) && (index < _List.Count) ? (MeasurementType) _List.GetKey(index) : MeasurementType.None;
		}
	}
}
