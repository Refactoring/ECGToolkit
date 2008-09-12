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
using System.Runtime.InteropServices;
using Communication.IO.Tools;

namespace ECGConversion.SCP
{
	/// <summary>
	/// SCP Unknown Section. this class is for reading and writing SCP formats without having to implement the sections that aren't needed at all.
	/// </summary>
	public class SCPSectionUnknown : SCPSection
	{
		// Part of the stored Data Structure.
		private byte[] _Data = null;
		public SCPSectionUnknown()
		{}
		protected override int _Read(byte[] buffer, int offset)
		{
			int length = Length - Size;
			if (length <= 0)
			{
				return 0x1;
			}
			_Data = new byte[length];
			offset += BytesTool.copy(_Data, 0, buffer, offset, length);
			return 0x00;
		}
		protected override int _Write(byte[] buffer, int offset)
		{
			offset += BytesTool.copy(buffer, offset, _Data, 0, _Data.Length);
			return 0x00;
		}
		protected override void _Empty()
		{
			_Data = null;
		}
		protected override int _getLength()
		{
			if (Works())
			{
				return _Data.Length;
			}
			return 0;
		}
		public override ushort getSectionID()
		{
			return SectionID;
		}
		public override bool Works()
		{
			if (_Data != null)
			{
				return true;
			}
			return false;
		}
	}
}
