/***************************************************************************
Copyright 2004,2008, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
	/// abstract class describing basic form of a section.
	/// </summary>
	public abstract class SCPSection
	{
		public static int Size = 16;
		private static int _ReservedLength = 6;

		// encoding of scp file
		protected System.Text.Encoding _Encoding = System.Text.Encoding.ASCII;
		
		// Content of Header of section.
		protected ushort CRC;
		protected ushort SectionID = 0;
		protected int Length;
		protected byte SectionVersionNr;
		protected byte ProtocolVersionNr;
		protected byte[] Reserved = new byte[_ReservedLength];
		/// <summary>
		/// Constructor for a SCP Section.
		/// </summary>
		public SCPSection()
		{
			SectionID = getSectionID();
			Empty();
		}
		/// <summary>
		/// Set encoding used for section.
		/// </summary>
		/// <param name="enc">encoding to use in section.</param>
		public void SetEncoding(System.Text.Encoding enc)
		{
			_Encoding = enc;
		}
		/// <summary>
		/// Function to read an SCP Section.
		/// </summary>
		/// <param name="buffer">buffer to read from</param>
		/// <param name="offset">position on buffer to start reading</param>
		/// <returns>error:
		/// 0x00) succes
		/// 0x01) no buffer provided or buffer to small for header
		/// 0x02) Section ID doesn't seem to be right
		/// 0x04) buffer not big enough for entire section
		/// 0x08) CRC Check Failed
		/// rest) Section specific error </returns>
		public int Read(byte[] buffer, int offset, int length)
		{
			Empty();
			if ((buffer != null)
			&&	(offset + Size) <= buffer.Length)
			{
				int crcoffset = offset;
				CRC = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(CRC), true);
				offset += Marshal.SizeOf(CRC);
				SectionID = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(SectionID), true);
				if (SectionID != getSectionID())
				{
					return 0x2;
				}
				offset += Marshal.SizeOf(SectionID);
				Length = (int) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Length), true);
				if (((length != Length)
				&&	(SectionID != 0))
				||	(crcoffset + Length) > buffer.Length)
				{
					return 0x4;
				}
				offset += Marshal.SizeOf(Length);
				CRCTool crc = new CRCTool();
				crc.Init(CRCTool.CRCCode.CRC_CCITT);
				if (CRC != crc.CalcCRCITT(buffer, crcoffset + Marshal.SizeOf(CRC), Length - Marshal.SizeOf(CRC)))
				{
					return 0x8;
				}
				SectionVersionNr = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(SectionVersionNr), true);
				offset += Marshal.SizeOf(SectionVersionNr);
				ProtocolVersionNr = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(ProtocolVersionNr ), true);
				offset += Marshal.SizeOf(ProtocolVersionNr);
				offset += BytesTool.copy(Reserved, 0, buffer, offset, _ReservedLength);

				return _Read(buffer, offset) << 4;
			}
			return 0x1;
		}
		/// <summary>
		/// Function to write an SCP Section.
		/// </summary>
		/// <param name="buffer">buffer allocated to write in</param>
		/// <returns>error:
		/// 0x00) succes
		/// 0x01) section incorrect
		/// 0x02) no buffer provided or buffer to small for header
		/// rest) Section specific error </returns>
		public int Write(out byte[] buffer)
		{
			buffer = null;
			if (Works())
			{
				buffer = new byte[_getLength() + Size];

				if (buffer.Length <= Size)
				{
					buffer = null;

					return 0;
				}

				int err = Write(buffer, 0);
				if (err != 0)
				{
					buffer = null;
				}
				return err;
			}
			return 0x1;
		}
		/// <summary>
		/// Function to write an SCP Section.
		/// </summary>
		/// <param name="buffer">buffer to write to</param>
		/// <param name="offset">position on buffer to start writing</param>
		/// <returns>error:
		/// 0x00) succes
		/// 0x01) section incorrect
		/// 0x02) no buffer provided or buffer to small for header
		/// rest) Section specific error </returns>
		public int Write(byte[] buffer, int offset)
		{
			Length = _getLength() + Size;

			if (Length == Size)
				return 0;

			if (Works())
			{
				if ((buffer != null)
				&&	((offset + Length) <= buffer.Length))
				{
					int crcoffset = offset;
					offset += Marshal.SizeOf(CRC);
					SectionID = getSectionID();
					BytesTool.writeBytes(SectionID, buffer, offset, Marshal.SizeOf(SectionID), true);
					offset += Marshal.SizeOf(SectionID);
					BytesTool.writeBytes(Length, buffer, offset, Marshal.SizeOf(Length), true);
					offset += Marshal.SizeOf(Length);
					BytesTool.writeBytes(SectionVersionNr, buffer, offset, Marshal.SizeOf(SectionVersionNr), true);
					offset += Marshal.SizeOf(SectionVersionNr);
					BytesTool.writeBytes(ProtocolVersionNr, buffer, offset, Marshal.SizeOf(ProtocolVersionNr), true);
					offset += Marshal.SizeOf(ProtocolVersionNr);
					offset += BytesTool.copy(buffer, offset, Reserved, 0, _ReservedLength);

					int err = _Write(buffer, offset);
					if (err == 0)
					{
						CRCTool crc = new CRCTool();
						crc.Init(CRCTool.CRCCode.CRC_CCITT);
						CRC = crc.CalcCRCITT(buffer, crcoffset + Marshal.SizeOf(CRC), Length - Marshal.SizeOf(CRC));
						BytesTool.writeBytes(CRC, buffer, crcoffset, Marshal.SizeOf(CRC), true);
					}
					return err << 2;
				}
				return 0x2;
			}
			return 0x1;
		}
		/// <summary>
		/// Function to empty a SCP section.
		/// </summary>
		public void Empty()
		{
			CRC = 0;
			SectionID = getSectionID();
			Length = 0;
			SectionVersionNr = SCPFormat.DefaultSectionVersion;
			ProtocolVersionNr = SCPFormat.DefaultProtocolVersion;
			Reserved[0] = 0; Reserved[1] = 0; Reserved[2] = 0;
			Reserved[3] = 0; Reserved[4] = 0; Reserved[5] = 0;
			_Empty();
		}
		/// <summary>
		/// Function to get the length of a SCP section.
		/// </summary>
		/// <returns>length of section</returns>
		public int getLength()
		{
			int len = _getLength();
			return (len == 0 ? 0 : len + Size);
		}
		/// <summary>
		/// Hidden read function is called by Read().
		/// </summary>
		/// <param name="buffer">byte array to read from</param>
		/// <param name="offset">position to start reading</param>
		/// <returns>0 on success</returns>
		protected abstract int _Read(byte[] buffer, int offset);
		/// <summary>
		/// Hidden write function is called by Write().
		/// </summary>
		/// <param name="buffer">byte array to write into</param>
		/// <param name="offset">position to start writing</param>
		/// <returns>0 on success</returns>
		protected abstract int _Write(byte[] buffer, int offset);
		/// <summary>
		/// Hidden empty function is called by Empty(). 
		/// </summary>
		protected abstract void _Empty();
		/// <summary>
		/// Hidden length function is called getLength().
		/// </summary>
		/// <returns></returns>
		protected abstract int _getLength();
		/// <summary>
		/// Function to get section ID of section.
		/// </summary>
		/// <returns>section id</returns>
		public abstract ushort getSectionID();
		/// <summary>
		/// Function to check working of section.
		/// </summary>
		/// <returns>true: working
		/// false not working</returns>
		public abstract bool Works();
	}
}
