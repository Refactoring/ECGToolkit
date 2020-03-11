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
namespace ECGConversion.ECGDemographics
{
	/// <summary>
	/// Class containing a date (format is equal to SCP).
	/// </summary>
	public class Date
	{
		// Static information for check.
		private static byte[] _DaysInMonth = {0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
		private static int _LeapMonth = 2;
		private static byte _DaysInLeapMonth = 29;

		// Content of date class.
		public ushort Year = 0;
		public byte Month = 0;
		public byte Day = 0;
		/// <summary>
		/// Constructor to make a date.
		/// </summary>
		public Date()
		{}
		/// <summary>
		/// Constructor to make a date.
		/// </summary>
		/// <param name="year">number of year</param>
		/// <param name="month">number of month</param>
		/// <param name="day">number of day</param>
		public Date(ushort year, byte month, byte day)
		{
			Year = year;
			Month = month;
			Day = day;
		}
		/// <summary>
		/// Check if date is likely to be an existing date.
		/// </summary>
		/// <returns>true: is an existing date.
		/// false: is an non existing date.</returns>
		public bool isExistingDate()
		{
			// The following check will most likely work for another 7000 years at least.
			if ((Month > 0)
			&&	(Month <= 12)
            &&  (Year > 0))
			{
				if ((Month == _LeapMonth)
				&&  ((Year % 4) == 0)
				&&  (((Year % 100) != 0)
				||   ((Year % 400)) == 0))
				{
					return ((Day > 0) && (Day <= _DaysInLeapMonth));
				}
				else
				{
					return ((Day > 0) && (Day <= _DaysInMonth[Month]));
				}
			}
			return false;
		}
	}
}
