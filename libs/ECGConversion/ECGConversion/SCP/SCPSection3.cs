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
using ECGConversion.ECGSignals;

namespace ECGConversion.SCP
{
	/// <summary>
	/// Class contains section 3 (Lead definition section).
	/// </summary>
	public class SCPSection3 : SCPSection, ISignal
	{
		// Defined in SCP.
		private static ushort _SectionID = 3;

		// Part of the stored Data Structure.
		private byte _NrLeads = 0;
		private byte _Flags = 0;
		private SCPLead[] _Leads = null;
		protected override int _Read(byte[] buffer, int offset)
		{
			int end = offset - Size + Length;
			if ((offset + Marshal.SizeOf(_NrLeads) + Marshal.SizeOf(_Flags)) > end)
			{
				return 0x1;
			}
			_NrLeads = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_NrLeads), true);
			offset += Marshal.SizeOf(_NrLeads);
			_Flags = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_Flags), true);
			offset += Marshal.SizeOf(_Flags);
			if (offset + (_NrLeads * Marshal.SizeOf(typeof(SCPLead))) > end)
			{
				_Empty();
				return 0x2;
			}

			// BEGIN DIRTY SOLUTION!!!
			// this solution is for a bug in some CCW files.
			if (((end - offset) / Marshal.SizeOf(typeof(SCPLead))) > _NrLeads)
			{
				_NrLeads = (byte) ((end - offset) / Marshal.SizeOf(typeof(SCPLead)));
			}
			// END DIRTY SOLUTION!!!

			_Leads = new SCPLead[_NrLeads];
			for (int loper=0;loper < _NrLeads;loper++)
			{
				_Leads[loper] = new SCPLead();
				int err = _Leads[loper].Read(buffer, offset);
				if (err != 0)
				{
					return err << 2 + loper;
				}
				offset += Marshal.SizeOf(_Leads[loper]);
			}
			return 0x0;
		}
		protected override int _Write(byte[] buffer, int offset)
		{
			BytesTool.writeBytes(_NrLeads, buffer, offset, Marshal.SizeOf(_NrLeads), true);
			offset += Marshal.SizeOf(_NrLeads);
			BytesTool.writeBytes(_Flags, buffer, offset, Marshal.SizeOf(_Flags), true);
			offset += Marshal.SizeOf(_Flags);
			for (int loper=0;loper < _NrLeads;loper++)
			{
				int err = _Leads[loper].Write(buffer, offset);
				if (err != 0)
				{
					return err << loper;
				}
				offset += Marshal.SizeOf(_Leads[loper]);
			}
			return 0x0;
		}
		protected override void _Empty()
		{
			_NrLeads = 0;
			_Flags = 0;
		}
		protected override int _getLength()
		{
			if (Works())
			{
				int sum = (Marshal.SizeOf(_NrLeads) + Marshal.SizeOf(this._Flags));
				sum += (_NrLeads * Marshal.SizeOf(typeof(SCPLead)));
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
			if ((_Leads != null) && (_NrLeads == _Leads.Length))
			{
				for (int loper=0;loper < _NrLeads;loper++)
				{
					if (_Leads[loper] == null)
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}
		/// <summary>
		/// Function to get number of leads.
		/// </summary>
		/// <returns>number of leads.</returns>
		public ushort getNrLeads()
		{
			return _NrLeads;
		}
		/// <summary>
		/// Function to get beginning of lead.
		/// </summary>
		/// <param name="nr">number of lead</param>
		/// <returns>begin of lead</returns>
		public int getLeadStart(int nr)
		{
			if ((_Leads != null)
			&&  (nr >= 0)
			&&  (nr < _NrLeads))
			{
				return _Leads[nr].Start;
			}
			return Int32.MaxValue;
		}
		/// <summary>
		/// Function to get lead end.
		/// </summary>
		/// <param name="nr">number of lead</param>
		/// <returns>end of lead</returns>
		public int getLeadEnd(int nr)
		{
			if ((_Leads != null)
				&&  (nr >= 0)
				&&  (nr < _NrLeads))
			{
				return _Leads[nr].End;
			}
			return Int32.MinValue;
		}
		/// <summary>
		/// Function to get id of lead.
		/// </summary>
		/// <param name="nr">number of lead</param>
		/// <returns>id of lead</returns>
		public int getLeadId(int nr)
		{
			if ((_Leads != null)
				&&  (nr >= 0)
				&&  (nr < _NrLeads))
			{
				return _Leads[nr].ID;
			}
			return -1;
		}
		/// <summary>
		/// Function to get length of lead
		/// </summary>
		/// <param name="nr">number of lead</param>
		/// <returns>length of lead</returns>
		public int getLeadLength(int nr)
		{
			int length = -1;
			if ((_Leads != null)
			&&  (nr >= 0)
			&&  (nr < _NrLeads))
			{
				length = _Leads[nr].End - _Leads[nr].Start + 1;
			}
			return length;
		}
		/// <summary>
		/// Function of minimum beginning of leads.
		/// </summary>
		/// <returns>min begin</returns>
		public int getMinBegin()
		{
			int min = Int32.MaxValue;
			if (Works())
			{
				for (int x=0;x < _NrLeads;x++)
				{
					min = (min <= getLeadStart(x) ? min : getLeadStart(x));
				}
			}
			return min;
		}
		/// <summary>
		/// Function of maximum ending of leads
		/// </summary>
		/// <returns>max end</returns>
		public int getMaxEnd()
		{
			int max = Int32.MinValue;
			if (Works())
			{
				for (int x=0;x < _NrLeads;x++)
				{
					max = (max >= getLeadEnd(x) ? max : getLeadEnd(x));
				}
			}
			return max;
		}
		/// <summary>
		/// Function to get total length of all the leads.
		/// </summary>
		/// <returns>total length</returns>
		public int getTotalLength()
		{
			return getMaxEnd() - getMinBegin();
		}
		/// <summary>
		/// Function to determine if medians are used.
		/// </summary>
		/// <returns>true if medians are used</returns>
		public bool isMediansUsed()
		{
			return ((_Flags & 0x1) == 0x1);
		}
		/// <summary>
		/// Function to set use of median subtraction.
		/// </summary>
		/// <param name="used">true if medians used</param>
		public void setMediansUsed(bool used)
		{
			if (used)
			{
				_Flags |= 0x01;
			}
			else
			{
				_Flags &= 0xfe;
			}
		}
		/// <summary>
		/// Function to determine if all leads are simultaneously.
		/// </summary>
		/// <returns>true if leads are simultaneous</returns>
		public bool isSimultaneously()
		{
			return ((_Flags & 0x4) == 0x4);
		}
		/// <summary>
		/// Function to determine if all leads are simultaneously.
		/// </summary>
		/// <returns>true if leads are simultaneous</returns>
		private bool _isSimultaneously()
		{
			if ((_Leads != null)
			&&	(_NrLeads > 1)
			&&  (_NrLeads <= _Leads.Length)
			&&  (_Leads[0] != null))
			{
				int loper=1;
				for (;loper < _NrLeads;loper++)
				{
					if ((_Leads[loper] == null)
					||	(_Leads[0].Start != _Leads[loper].Start)
					||  (_Leads[0].End != _Leads[loper].End))
					{
						break;
					}
				}
				_Flags |= (byte) (loper << 3);
				return (loper == _NrLeads);
			}
			if ((_Leads != null) && (_NrLeads == 1))
			{
				_Flags = 0x8;
			}
			return (_Leads != null) && (_NrLeads == 1);
		}
		// Signal Manupalations
		public int getSignals(out Signals signals)
		{
			signals = new Signals();
			int err = getSignals(signals);
			if (err != 0)
			{
				signals = null;
			}
			return err;
		}
		public int getSignals(Signals signals)
		{
			if ((signals != null)
			&&  (Works()))
			{
				signals.NrLeads = _NrLeads;

				for (int loper=0;loper < _NrLeads;loper++)
				{
					signals[loper] = new Signal();
					signals[loper].Type = (LeadType) _Leads[loper].ID;
					signals[loper].RhythmStart = _Leads[loper].Start - 1;
					signals[loper].RhythmEnd = _Leads[loper].End;
				}

				return 0;
			}
			return 1;
		}
		public int setSignals(Signals signals)
		{
			if ((signals != null)
			&&  (signals.NrLeads > 0)
			&&  (signals.RhythmSamplesPerSecond != 0))
			{
				_NrLeads = (byte) signals.NrLeads;
				_Leads = new SCPLead[_NrLeads];
				_Flags = 0;
				for (int loper=0;loper< _NrLeads;loper++)
				{
					if (signals[loper] == null)
					{
						return 2;
					}

					_Leads[loper] = new SCPLead();
					if (signals.MedianSamplesPerSecond != 0)
					{
						_Leads[loper].Start = (signals[loper].RhythmStart * signals.MedianSamplesPerSecond) / signals.RhythmSamplesPerSecond + 1;
						_Leads[loper].End = (signals[loper].RhythmEnd * signals.MedianSamplesPerSecond) / signals.RhythmSamplesPerSecond;
					}
					else
					{
						_Leads[loper].Start = signals[loper].RhythmStart + 1;
						_Leads[loper].End = signals[loper].RhythmEnd;
					}
					_Leads[loper].ID = (byte) signals[loper].Type;
				}

				if (_isSimultaneously())
				{
					_Flags |= 0x4;
				}
				return 0;
			}
			return 1;
		}
		/// <summary>
		/// Class containing SCP lead information.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)]
		public class SCPLead
		{
			public int Start;
			public int End;
			public byte ID;
			/// <summary>
			/// Constructor of SCP lead.
			/// </summary>
			public SCPLead()
			{}
			/// <summary>
			/// Constructor of SCP lead.
			/// </summary>
			/// <param name="start">start sample of lead</param>
			/// <param name="end">end sample of lead</param>
			/// <param name="id">id of lead</param>
			public SCPLead(int start, int end, byte id)
			{
				Start = start;
				End = end;
				ID = id;
			}
			/// <summary>
			/// Function to read SCP lead information.
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
				Start = (int) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Start), true);
				offset += Marshal.SizeOf(Start);
				End = (int) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(End), true);
				offset += Marshal.SizeOf(End);
				ID = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(ID), true);
				offset += Marshal.SizeOf(ID);

				return 0x0;
			}
			/// <summary>
			/// Function to write SCP lead information.
			/// </summary>
			/// <param name="buffer">byte array to write into</param>
			/// <param name="offset">position to write to</param>
			/// <returns>0 on success</returns>
			public int Write(byte[] buffer, int offset)
			{
				if ((offset + Marshal.SizeOf(this)) > buffer.Length)
				{
					return 0x1;
				}

				BytesTool.writeBytes(Start, buffer, offset, Marshal.SizeOf(Start), true);
				offset += Marshal.SizeOf(Start);
				BytesTool.writeBytes(End, buffer, offset, Marshal.SizeOf(End), true);
				offset += Marshal.SizeOf(End);
				BytesTool.writeBytes(ID, buffer, offset, Marshal.SizeOf(ID), true);
				offset += Marshal.SizeOf(ID);

				return 0x0;
			}
		}
	}
}
