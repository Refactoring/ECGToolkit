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
	/// class containing time in SCP format.
	/// </summary>
	public class SCPTime
	{
		public const int Size = 3;

		public byte Hour = 0;
		public byte Min = 0;
		public byte Sec = 0;
		/// <summary>
		/// Constructor of a SCP time.
		/// </summary>
		public SCPTime()
		{}
		/// <summary>
		/// Constructor of a SCP time.
		/// </summary>
		/// <param name="hour">number of hour</param>
		/// <param name="min">number of minute</param>
		/// <param name="sec">number of second</param>
		public SCPTime(int hour, int min, int sec)
		{
			Hour = (byte) hour;
			Min = (byte) min;
			Sec = (byte) sec;
		}
		/// <summary>
		/// Function to read an SCP time.
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

			Hour = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Hour), true);
			offset += Marshal.SizeOf(Hour);
			Min = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Min), true);
			offset += Marshal.SizeOf(Min);
			Sec = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Sec), true);
			offset += Marshal.SizeOf(Sec);

			return 0x0;
		}
		/// <summary>
		/// Function to write SCP time.
		/// </summary>
		/// <param name="buffer">byte array to write into</param>
		/// <param name="offset">position to start writing</param>
		/// <returns>0 on success</returns>
		public int Write(byte[] buffer, int offset)
		{
			if ((offset + Size) > buffer.Length)
			{
				return 0x1;
			}

			BytesTool.writeBytes(Hour, buffer, offset, Marshal.SizeOf(Hour), true);
			offset += Marshal.SizeOf(Hour);
			BytesTool.writeBytes(Min, buffer, offset, Marshal.SizeOf(Min), true);
			offset += Marshal.SizeOf(Min);
			BytesTool.writeBytes(Sec, buffer, offset, Marshal.SizeOf(Sec), true);
			offset += Marshal.SizeOf(Sec);

			return 0x0;
		}
	}
}
