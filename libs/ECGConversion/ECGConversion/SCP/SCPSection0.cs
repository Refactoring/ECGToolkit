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
	/// Class contains section 0 (Pointer section).
	/// </summary>
	public class SCPSection0 : SCPSection
	{
		// Defined in SCP.
		private static byte[] _Reserved = {(byte)'S', (byte)'C', (byte)'P', (byte)'E', (byte)'C', (byte)'G'};
		private static ushort _SectionID = 0;
		private static int _NrMandatory = 12;

		// Part of the stored Data Structure.
		private SCPPointer[] _MandatoryPointers = new SCPPointer[_NrMandatory];
		private SCPPointer[] _OptionalPointers = null;
		protected override int _Read(byte[] buffer, int offset)
		{
			/* Very stange, but most SCP files I got didn't seem to be live up to this law.
			 * the check will now only be doen when the Protocol Version Nr is greator equal
			 * to 14. This value might need changes in future.
			 * 
			 *	Reference 5.3.1 of the "Standard Communications protocol for computer-assisted
			 *	 electrocardiography".
			 */ 
			if ((ProtocolVersionNr >= 13)
			&&	((Reserved[0] != _Reserved[0])
			||	(Reserved[1] != _Reserved[1])
			||	(Reserved[2] != _Reserved[2])
			||	(Reserved[3] != _Reserved[3])
			||	(Reserved[4] != _Reserved[4])
			||	(Reserved[5] != _Reserved[5])))
			{
				return 0x1;
			}

			int nrPointers = (Length - Size) / Marshal.SizeOf(typeof(SCPPointer));
			if (nrPointers < 12)
			{
				return 0x2;
			}
			for (int loper=0;loper < _NrMandatory;loper++)
			{
				_MandatoryPointers[loper] = new SCPPointer();
				int err = _MandatoryPointers[loper].Read(buffer, offset);
				if (err != 0)
				{
					return err << 2;
				}
				offset += Marshal.SizeOf(_MandatoryPointers[loper]);
			}
			nrPointers -= _NrMandatory;
			if (nrPointers > 0)
			{
				_OptionalPointers = new SCPPointer[nrPointers];
				for (int loper=0;loper < nrPointers;loper++)
				{
					_OptionalPointers[loper] = new SCPPointer();
					int err = _OptionalPointers[loper].Read(buffer, offset);
					if (err != 0)
					{
						return err << 3;
					}
					offset += Marshal.SizeOf(_OptionalPointers[loper]);
				}
			}
			return 0x0;
		}
		protected override int _Write(byte[] buffer, int offset)
		{
			for (int loper=0;loper < _NrMandatory;loper++)
			{
				int err = _MandatoryPointers[loper].Write(buffer, offset);
				if (err != 0)
				{
					return err;
				}
				offset += Marshal.SizeOf(_MandatoryPointers[loper]);
			}
			if (_OptionalPointers != null)
			{
				for (int loper=0;loper < _OptionalPointers.Length;loper++)
				{
					int err = _OptionalPointers[loper].Write(buffer, offset);
					if (err != 0)
					{
						return err << 1;
					}
					offset += Marshal.SizeOf(_OptionalPointers[loper]);
				}
			}
			return 0x0;
		}
		protected override void _Empty()
		{
			BytesTool.copy(Reserved, 0, _Reserved, 0, 6);
			if ((this._MandatoryPointers != null)
			&&	(_MandatoryPointers.Length == _NrMandatory))
			{
				for (int loper=0;loper < _NrMandatory;loper++)
				{
					_MandatoryPointers[loper] = null;
				}
			}
			else
			{
				_MandatoryPointers = new SCPPointer[_NrMandatory];
			}
			_OptionalPointers = null;
		}
		public override ushort getSectionID()
		{
			return _SectionID;
		}
		protected override int _getLength()
		{
			int sum = Marshal.SizeOf(typeof(SCPPointer)) * getNrPointers();
			return ((sum % 2) == 0 ? sum : sum + 1);
		}
		public override bool Works()
		{
			if ((_MandatoryPointers != null)
			&&  (_MandatoryPointers.Length == _NrMandatory))
			{
				for (int loper=0;loper < _NrMandatory;loper++)
				{
					if (_MandatoryPointers[loper] == null)
					{
						return false;
					}
				}
				if (_OptionalPointers != null)
				{
					for (int loper=0;loper < _OptionalPointers.Length;loper++)
					{
						if (_OptionalPointers[loper] == null)
						{
							return false;
						}
					}
				}
				return true;
			}
			return false;
		}
		/// <summary>
		/// Function to get number of pointers contained in pointer section.
		/// </summary>
		/// <returns>number of pointers</returns>
		public int getNrPointers()
		{
			return _NrMandatory + (_OptionalPointers != null ? _OptionalPointers.Length : 0);
		}
		/// <summary>
		/// Function to set number of pointers contained in pointer section.
		/// </summary>
		/// <param name="nr">number of pointers</param>
		public void setNrPointers(int nr)
		{
			int current = _NrMandatory + (_OptionalPointers != null ? _OptionalPointers.Length : 0);
			if ((nr != current)
			&&  (nr >= _NrMandatory))
			{
				_MandatoryPointers = new SCPPointer[_NrMandatory];
				if (nr > _NrMandatory)
				{
					_OptionalPointers = new SCPPointer[nr - _NrMandatory];
				}
			}
		}
		/// <summary>
		/// Function to get section id of a pointer.
		/// </summary>
		/// <param name="nr">pointer number to get section id from</param>
		/// <returns>id of section</returns>
		public ushort getSectionID(int nr)
		{
			if ((nr >= 0)
			&&  (nr < getNrPointers()))
			{
				if (nr < _NrMandatory)
				{
					return _MandatoryPointers[nr].Nr;
				}
				return _OptionalPointers[nr - _NrMandatory].Nr;
			}
			return 0;
		}
		/// <summary>
		/// Function to set section id of a pointer.
		/// </summary>
		/// <param name="nr">number of pointer to set</param>
		/// <param name="ID">id of section</param>
		public void setSectionID(int nr, ushort ID)
		{
			nr -= _NrMandatory;
			if ((_OptionalPointers != null)
			&&	(nr >= 0)
			&&  (nr < _OptionalPointers.Length)
			&&  (_OptionalPointers[nr] != null))
			{
				_OptionalPointers[nr].Nr = ID;
			}
		}
		/// <summary>
		/// Function to get index of a pointer.
		/// </summary>
		/// <param name="nr">number of pointer to get from</param>
		/// <returns>0 on success</returns>
		public int getIndex(int nr)
		{
			if ((nr >= 0)
			&&  (nr < getNrPointers()))
			{
				if (nr < _NrMandatory)
				{
					return _MandatoryPointers[nr].Index;
				}
				return _OptionalPointers[nr - _NrMandatory].Index;
			}
			return 0;
		}
		/// <summary>
		/// Function to set index of a pointer.
		/// </summary>
		/// <param name="nr">number of pointer to set</param>
		/// <param name="index">index to set pointer to</param>
		public void setIndex(int nr, int index)
		{
			if ((nr >= 0)
			&&  (nr < getNrPointers()))
			{
				if (nr < _NrMandatory)
				{
					_MandatoryPointers[nr].Index = index;
				}
				else
				{
					_OptionalPointers[nr - _NrMandatory].Index = index;
				}
			}
		}
		/// <summary>
		/// Function to get length of section from pointer.
		/// </summary>
		/// <param name="nr">number of pointer to get from</param>
		/// <returns>length of section</returns>
		public int getLength(int nr)
		{
			if ((nr >= 0)
			&&  (nr < getNrPointers()))
			{
				if (nr < _NrMandatory)
				{
					return _MandatoryPointers[nr].Length;
				}
				return _OptionalPointers[nr - _NrMandatory].Length;
			}
			return 0;
		}
		/// <summary>
		/// Function to get length of section from pointer.
		/// </summary>
		/// <param name="nr">number of pointer to set</param>
		/// <param name="length">length of section</param>
		public void setLength(int nr, int length)
		{
			if ((nr >= 0)
			&&  (nr < getNrPointers()))
			{
				if (nr < _NrMandatory)
				{
					_MandatoryPointers[nr].Length = length;
				}
				else
				{
					_OptionalPointers[nr - _NrMandatory].Length = length;
				}
			}
		}
		/// <summary>
		/// Function to get all values of a pointer.
		/// </summary>
		/// <param name="nr">number of pointer to read from</param>
		/// <param name="id">id of section</param>
		/// <param name="length">length of section</param>
		/// <param name="index">index of section</param>
		public void getPointer(int nr, out ushort id, out int length, out int index)
		{
			id = 0;length = 0;index =0;
			if ((nr >= 0)
			&&  (_MandatoryPointers != null)
			&&	(_MandatoryPointers.Length == _NrMandatory))
			{
				if (nr < _NrMandatory)
				{
					if (_MandatoryPointers[nr] != null)
					{
						id = _MandatoryPointers[nr].Nr;
						length = _MandatoryPointers[nr].Length;
						index = _MandatoryPointers[nr].Index;
					}
				}
				else if ((_OptionalPointers != null)
					&&	 ((nr - _NrMandatory) < _OptionalPointers.Length))
				{
					nr -= _NrMandatory;
					if (_OptionalPointers[nr] == null)
					{
						id = _OptionalPointers[nr].Nr;
						length = _OptionalPointers[nr].Length;
						index = _OptionalPointers[nr].Index;
					}
				}
			}
		}
		/// <summary>
		/// Function to set all values of a pointer
		/// </summary>
		/// <param name="nr">number of pointer to set</param>
		/// <param name="id">id of section</param>
		/// <param name="length">length of section</param>
		/// <param name="index">index of section</param>
		public void setPointer(int nr, ushort id, int length, int index)
		{
			if (nr >= 0)
			{
				if (nr < _NrMandatory)
				{
					if (_MandatoryPointers[nr] == null)
					{
						_MandatoryPointers[nr] = new SCPPointer();
					}
					_MandatoryPointers[nr].Nr = id;
					_MandatoryPointers[nr].Length = length;
					_MandatoryPointers[nr].Index = index;
				}
				else if ((_OptionalPointers != null)
					&&	 ((nr - _NrMandatory) < _OptionalPointers.Length))
				{
					nr -= _NrMandatory;
					if (_OptionalPointers[nr] == null)
					{
						_OptionalPointers[nr] = new SCPPointer();
					}
					_OptionalPointers[nr].Nr = id;
					_OptionalPointers[nr].Length = length;
					_OptionalPointers[nr].Index = index;
				}
			}
		}
		/// <summary>
		/// Class containing a SCP pointer.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)]
		public class SCPPointer
		{
			public ushort Nr;
			public int Length;
			public int Index;
			/// <summary>
			/// Function to read SCP pointer.
			/// </summary>
			/// <param name="buffer">byte array to read from</param>
			/// <param name="offset">position to start reading</param>
			/// <returns>0 on success</returns>
			public int Read(byte[] buffer, int offset)
			{
				if ((offset + Marshal.SizeOf(this)) > buffer.Length)
				{
					return 0x1;
				}

				Nr = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Nr), true);
				offset += Marshal.SizeOf(Nr);
				Length = (int) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Length), true);
				offset += Marshal.SizeOf(Length);
				Index = (int) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Index), true);
				offset += Marshal.SizeOf(Index);

				return 0x0;
			}
			/// <summary>
			/// Function to write SCP pointer.
			/// </summary>
			/// <param name="buffer">byte array to write section into</param>
			/// <param name="offset">position to start writing</param>
			/// <returns>0 on success</returns>
			public int Write(byte[] buffer, int offset)
			{
				if ((offset + Marshal.SizeOf(this)) > buffer.Length)
				{
					return 0x1;
				}

				BytesTool.writeBytes(Nr, buffer, offset, Marshal.SizeOf(Nr), true);
				offset += Marshal.SizeOf(Nr);
				BytesTool.writeBytes(Length, buffer, offset, Marshal.SizeOf(Length), true);
				offset += Marshal.SizeOf(Length);
				BytesTool.writeBytes(Index, buffer, offset, Marshal.SizeOf(Index), true);
				offset += Marshal.SizeOf(Index);

				return 0x0;
			}
		}
	}
}