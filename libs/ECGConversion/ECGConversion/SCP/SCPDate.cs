/***************************************************************************
Copyright 2004,2009, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
using System.Runtime.InteropServices;
using Communication.IO.Tools;

namespace ECGConversion.SCP
{
	/// <summary>
	/// class containing date in SCP format.
	/// </summary>
	public class SCPDate
	{
		public const int Size = 4;

		// data structure of SCP date.
		public ushort Year = 0;
		public byte Month = 0;
		public byte Day = 0;
		/// <summary>
		/// Constructor of a SCP date.
		/// </summary>
		public SCPDate()
		{}
		/// <summary>
		/// Constructor of a SCP date.
		/// </summary>
		/// <param name="year">number of year</param>
		/// <param name="month">number of month</param>
		/// <param name="day">number of day</param>
		public SCPDate(int year, int month, int day)
		{
			Year = (ushort) year;
			Month = (byte) month;
			Day = (byte) day;
		}
		/// <summary>
		/// Function to read an SCP date from byte array.
		/// </summary>
		/// <param name="buffer">byte array to read from</param>
		/// <param name="offset">position to start reading</param>
		/// <returns>0 on success</returns>
		public int Read(byte[] buffer, int offset)
		{
			if ((offset + Size) > buffer.Length)
			{
				return 0x1;
			}

			Year = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Year), true);
			offset += Marshal.SizeOf(Year);
			Month = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Month), true);
			offset += Marshal.SizeOf(Month);
			Day = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Day), true);
			offset += Marshal.SizeOf(Day);

			return 0x0;
		}
		/// <summary>
		/// Function to write an SCP date to a byte array.
		/// </summary>
		/// <param name="buffer">byte array to write in</param>
		/// <param name="offset">position to start writing</param>
		/// <returns></returns>
		public int Write(byte[] buffer, int offset)
		{
			if ((offset + Size) > buffer.Length)
			{
				return 0x1;
			}

			BytesTool.writeBytes(Year, buffer, offset, Marshal.SizeOf(Year), true);
			offset += Marshal.SizeOf(Year);
			BytesTool.writeBytes(Month, buffer, offset, Marshal.SizeOf(Month), true);
			offset += Marshal.SizeOf(Month);
			BytesTool.writeBytes(Day, buffer, offset, Marshal.SizeOf(Day), true);
			offset += Marshal.SizeOf(Day);

			return 0x0;
		}
	}
}
