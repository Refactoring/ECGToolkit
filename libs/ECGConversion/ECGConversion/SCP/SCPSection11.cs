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
	/// Class contains section 11 (contains the Universal ECG interpretive statements section).
	/// </summary>
	public class SCPSection11 : SCPSection
	{
		// Defined in SCP.
		private static ushort _SectionID = 11;

		// Part of the stored Data Structure.
		private byte _Confirmed = 0;
		private SCPDate _Date = null;
		private SCPTime _Time = null;
		private byte _NrStatements = 0;
		private SCPStatement[] _Statements = null;
		protected override int _Read(byte[] buffer, int offset)
		{
			int startsize = Marshal.SizeOf(_Confirmed) + Marshal.SizeOf(typeof(SCPDate)) + Marshal.SizeOf(typeof(SCPTime)) + Marshal.SizeOf(_NrStatements);
			int end = offset - Size + Length;
			if ((offset + startsize) > end)
			{
				return 0x1;
			}

			_Confirmed = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_Confirmed), true);
			offset += Marshal.SizeOf(_Confirmed);
			_Date = new SCPDate();
			_Date.Read(buffer, offset);
			offset += Marshal.SizeOf(_Date);
			_Time = new SCPTime();
			_Time.Read(buffer, offset);
			offset += Marshal.SizeOf(_Time);
			_NrStatements = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_NrStatements), true);
			offset += Marshal.SizeOf(_NrStatements);

			if (_NrStatements > 0)
			{
				_Statements = new SCPStatement[_NrStatements];
				int loper=0;
				for (;loper < _NrStatements;loper++)
				{
					_Statements[loper] = new SCPStatement();
					int err = _Statements[loper].Read(buffer, offset);
					if (err != 0)
					{
						return 0x2;
					}
					offset += _Statements[loper].getLength();
				}
				if (loper != _NrStatements)
				{
					_NrStatements = (byte) loper;
					return 0x4;
				}
			}

			return 0x0;
		}
		protected override int _Write(byte[] buffer, int offset)
		{
			BytesTool.writeBytes(_Confirmed, buffer, offset, Marshal.SizeOf(_Confirmed), true);
			offset += Marshal.SizeOf(_Confirmed);
			_Date.Write(buffer, offset);
			offset += Marshal.SizeOf(_Date);
			_Time.Write(buffer, offset);
			offset += Marshal.SizeOf(_Time);
			BytesTool.writeBytes(_NrStatements, buffer, offset, Marshal.SizeOf(_NrStatements), true);
			offset += Marshal.SizeOf(_NrStatements);
			for (int loper=0;loper < _NrStatements;loper++)
			{
				_Statements[loper].Write(buffer, offset);
				offset += _Statements[loper].getLength();
			}
			return 0x0;
		}
		protected override void _Empty()
		{
			_Confirmed = 0;
			_Date = null;
			_Time = null;
			_NrStatements = 0;
			_Statements = null;
		}
		protected override int _getLength()
		{
			if (Works())
			{
				int sum = Marshal.SizeOf(_Confirmed) + Marshal.SizeOf(_Date) + Marshal.SizeOf(_Time) + Marshal.SizeOf(_NrStatements);
				for (int loper=0;loper < _NrStatements;loper++)
				{
					sum += _Statements[loper].getLength();
				}
				return ((sum % 2) == 0 ? sum : sum + 1);
			}
			return 0;
		}
		public override ushort getSectionID()
		{
			return _SectionID;
		}
		public override bool Works()
		{
			if ((_Date != null)
			&&	(_Time != null)
			&&  ((_NrStatements == 0)
			||	 ((_Statements != null)
			&&	  (_NrStatements <= _Statements.Length))))
			{
				for (int loper=0;loper < _NrStatements;loper++)
				{
					if (_Statements[loper] == null)
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}
		/// <summary>
		/// class for Universal ECG interpretive statements from SCP.
		/// </summary>
		public class SCPStatement
		{
			public byte SequenceNr;
			public ushort Length;
			public byte TypeID;
			public byte[] Field;
			/// <summary>
			/// Constructor to make a SCP statement.
			/// </summary>
			public SCPStatement()
			{}
			/// <summary>
			/// Constructor to make a SCP statement.
			/// </summary>
			/// <param name="seqnr">sequence number of statement</param>
			/// <param name="length">length of byte array</param>
			/// <param name="field">byte array</param>
			public SCPStatement(byte seqnr, ushort length, byte[] field)
			{
				SequenceNr = seqnr;
				Length = length;
				Field = field;
			}
			/// <summary>
			/// Function to read SCP statement from SCP statement.
			/// </summary>
			/// <param name="buffer">byte array</param>
			/// <param name="offset">position to start reading</param>
			/// <returns>0 on success</returns>
			public int Read(byte[] buffer, int offset)
			{
				if ((offset + Marshal.SizeOf(SequenceNr) + Marshal.SizeOf(Length)) > buffer.Length)
				{
					return 0x1;
				}

				SequenceNr = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(SequenceNr), true);
				offset += Marshal.SizeOf(SequenceNr);
				Length = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Length), true);
				offset += Marshal.SizeOf(Length);

				if (Length >= Marshal.SizeOf(TypeID))
				{
					if ((offset + Length) > buffer.Length)
					{
						return 0x2;
					}

					TypeID = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(TypeID), true);
					offset += Marshal.SizeOf(TypeID);

					if (Length > Marshal.SizeOf(TypeID))
					{
						Field = new byte[Length - Marshal.SizeOf(TypeID)];
						offset += BytesTool.copy(Field, 0, buffer, offset, Length - Marshal.SizeOf(TypeID));
					}
				}

				return 0x0;
			}
			/// <summary>
			/// Function to write SCP statement.
			/// </summary>
			/// <param name="buffer">byte array to write into</param>
			/// <param name="offset">position to start writing</param>
			/// <returns>0 on success</returns>
			public int Write(byte[] buffer, int offset)
			{
				
				if ((Field == null)
				||  (Field.Length != Length))
				{
					return 0x1;
				}

				if ((offset + Marshal.SizeOf(SequenceNr) + Marshal.SizeOf(Length)) > buffer.Length)
				{
					return 0x2;
				}

				BytesTool.writeBytes(SequenceNr, buffer, offset, Marshal.SizeOf(SequenceNr), true);
				offset += Marshal.SizeOf(SequenceNr);
				BytesTool.writeBytes(Length, buffer, offset, Marshal.SizeOf(Length), true);
				offset += Marshal.SizeOf(Length);

				if (Length >= Marshal.SizeOf(TypeID))
				{
					if ((offset + Length) > buffer.Length)
					{
						return 0x2;
					}

					BytesTool.writeBytes(TypeID, buffer, offset, Marshal.SizeOf(TypeID), true);
					offset += Marshal.SizeOf(TypeID);

					if (Length > Marshal.SizeOf(TypeID))
					{
						offset += BytesTool.copy(buffer, offset, Field, 0, Length - Marshal.SizeOf(TypeID));
					}
				}

				return 0x0;
			}
			/// <summary>
			/// Function to get length of SCP statement.
			/// </summary>
			/// <returns>length of statement</returns>
			public int getLength()
			{
				int sum = Marshal.SizeOf(SequenceNr) + Marshal.SizeOf(Length);
				if ((Length > 0)
				&&	(Field != null)
				&&  (Length == Field.Length))
				{
					sum += Length;
				}
				return sum;
			}
		}
	}
}
