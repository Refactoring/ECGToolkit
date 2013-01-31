/***************************************************************************
Copyright 2013, van Ettinger Information Technology, Lopik, The Netherlands
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
using System;
using System.Runtime.InteropServices;

using ECGConversion.ECGSignals;
using ECGConversion.ECGLeadMeasurements;

using Communication.IO.Tools;

namespace ECGConversion.SCP
{
	/// <summary>
	/// Class contains section 10 (contains the Lead Measurements Result section).
	/// </summary>
	public class SCPSection10 : SCPSection, ILeadMeasurement
	{
		// Defined in SCP.
		private static ushort _SectionID = 10;

		// Part of the stored Data Structure.
		private ushort _NrLeads
		{
			get
			{
				return (ushort) (_LeadMeasurements == null ? 0 : _LeadMeasurements.Length);
			}
			set
			{
				_LeadMeasurements = value == 0 ? null : new SCPLeadMeasurements[value];
			}
		}
		private ushort _ManufactorSpecific;
		private SCPLeadMeasurements[] _LeadMeasurements;

		public SCPSection10()
		{
			Empty();
		}

		protected override int _Read(byte[] buffer, int offset)
		{
			int startsize = Marshal.SizeOf(_NrLeads) + Marshal.SizeOf(_ManufactorSpecific);
			int end = offset - Size + Length;

			if ((offset + startsize) > end)
				return 0x1;

			int fieldSize = Marshal.SizeOf(_NrLeads);
			_NrLeads = (ushort) BytesTool.readBytes(buffer, offset, fieldSize, true);
			offset += fieldSize;

			fieldSize = Marshal.SizeOf(_ManufactorSpecific);
			_ManufactorSpecific = (ushort) BytesTool.readBytes(buffer, offset, fieldSize, true);
			offset += fieldSize;

			if (_LeadMeasurements == null)
				return 0x0;

			for (int i=0;i < _LeadMeasurements.Length;i++)
			{
				_LeadMeasurements[i] = new SCPLeadMeasurements();
				int ret = _LeadMeasurements[i].Read(buffer, offset);

				if (ret != 0)
				{
					_LeadMeasurements[i] = null;

					return ret;
				}
				
				offset += _LeadMeasurements[i].getLength();
			}

			return 0x0;
		}
		protected override int _Write(byte[] buffer, int offset)
		{
			if (!Works())
				return 0x1;

			if ((offset + _getLength()) > buffer.Length)
				return 0x2;

			if (_NrLeads  == 0)
				return 0;

			int fieldSize = Marshal.SizeOf(_NrLeads);
			BytesTool.writeBytes(_NrLeads, buffer, offset, fieldSize, true);
			offset += fieldSize;

			fieldSize = Marshal.SizeOf(_ManufactorSpecific);
			BytesTool.writeBytes(_ManufactorSpecific, buffer, offset, fieldSize, true);
			offset += fieldSize;

			if (_NrLeads == 0)
				return 0x0;

			for (int loper=0;loper < _LeadMeasurements.Length;loper++)
			{
				int ret = _LeadMeasurements[loper].Write(buffer, offset);

				if (ret != 0)
					return 0x1;

				offset += _LeadMeasurements[loper].getLength();
			}
			return 0x0;
		}
		protected override void _Empty()
		{
			_NrLeads = 0;
			_ManufactorSpecific = 0;
		}
		protected override int _getLength()
		{
			if (Works())
			{
				if (_NrLeads == 0)
					return 0;

				int sum = Marshal.SizeOf(_NrLeads) + Marshal.SizeOf(_ManufactorSpecific);

				if (_NrLeads != 0)
				{
					for (int loper=0;loper < _LeadMeasurements.Length;loper++)
					{
						sum += _LeadMeasurements[loper].getLength();
					}
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
			if (_NrLeads == 0)
				return true;

			for (int loper=0;loper < _LeadMeasurements.Length;loper++)
			{
				if (_LeadMeasurements[loper] == null
				||	!_LeadMeasurements[loper].Works())
				{
					return false;
				}
			}
			return true;
		}
		/// <summary>
		/// class for Lead Measurements from SCP.
		/// </summary>
		public class SCPLeadMeasurements
		{
			public LeadType LeadId
			{
				get
				{
					return (LeadType) _LeadId;
				}
				set
				{
					_LeadId = (ushort) value;
				}
			}
			public int Count
			{
				get
				{
					return (_Measurements == null ? 0 : _Measurements.Length);
				}
				set
				{
					int temp = (value < 50) ? 50 : (value << 1);

					if ((temp <= ushort.MaxValue)
					&&	(temp >= ushort.MinValue))
						_LeadLength = (ushort) temp;
				}
			}
			public short this[MeasurementType mt]
			{
				get
				{
					return this[(int)mt];
				}
				set
				{
					this[(int)mt] = value;
				}
			}
			public short this[int id]
			{
				get
				{
					if ((_Measurements == null)
					||	(id < 0)
					||	(id >= _Measurements.Length))
						return LeadMeasurement.NoValue;

					return _Measurements[id];
				}
				set
				{
					if ((_Measurements != null)
					&&	(id >= 0)
					&&	(id < _Measurements.Length))
					{
						_Measurements[id] = value;
					}
				}
			}
			private ushort _LeadId;
			private ushort _LeadLength
			{
				get
				{
					return (ushort) (_Measurements == null ? 0 : (_Measurements.Length << 1));
				}
				set
				{
					if (value >> 1 < 50)
						value = 50 << 1;

					_Measurements = value == 0 && ((value & 0x1) != 0x1) ? null : new short[value >> 1];

					if (_Measurements != null)
					{
						for (int i=0;i < _Measurements.Length;i++)
						{
							MeasurementType mt = (MeasurementType) i;

							bool bZero =	(mt == MeasurementType.Pmorphology)
										||	(mt == MeasurementType.Tmorphology)
										||	(mt == MeasurementType.QualityCode)
										||  (mt > MeasurementType.STamp1_8RR);

							_Measurements[i] = bZero ? (short) 0 : LeadMeasurement.NoValue;
						}
					}
				}
			}
			private short[] _Measurements;
			/// <summary>
			/// Constructor to make a SCP statement.
			/// </summary>
			public SCPLeadMeasurements()
			{}
			/// <summary>
			/// Constructor to make a SCP statement.
			/// </summary>
			public SCPLeadMeasurements(LeadType lt, ushort nrMes)
			{
				LeadId = lt;
				Count = nrMes;
			}
			/// <summary>
			/// Function to read SCP statement from SCP statement.
			/// </summary>
			/// <param name="buffer">byte array</param>
			/// <param name="offset">position to start reading</param>
			/// <returns>0 on success</returns>
			public int Read(byte[] buffer, int offset)
			{
				if ((offset + Marshal.SizeOf(_LeadId) + Marshal.SizeOf(_LeadLength)) > buffer.Length)
					return 0x1;

				int fieldSize = Marshal.SizeOf(_LeadId);
				_LeadId = (ushort) BytesTool.readBytes(buffer, offset, fieldSize, true);
				offset += fieldSize;
				
				fieldSize = Marshal.SizeOf(_LeadLength);
				_LeadLength = (ushort) BytesTool.readBytes(buffer, offset, fieldSize, true);
				offset += fieldSize;

				if (_Measurements != null)
				{
					if ((offset + _LeadLength) > buffer.Length)
						return 0x2;

					fieldSize = Marshal.SizeOf(typeof(short));

					for (int i=0;i < _Measurements.Length;i++)
					{
						_Measurements[i] = (short) BytesTool.readBytes(buffer, offset, fieldSize, true);
						offset += fieldSize;
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
				if (!Works())
					return 0x1;

				if ((offset + getLength()) > buffer.Length)
					return 0x2;

				int fieldSize = Marshal.SizeOf(_LeadId);
				BytesTool.writeBytes(_LeadId, buffer, offset, fieldSize, true);
				offset += fieldSize;
				
				fieldSize = Marshal.SizeOf(_LeadLength);
				BytesTool.writeBytes(_LeadLength, buffer, offset, fieldSize, true);
				offset += fieldSize;

				if (_LeadLength != 0)
				{
					fieldSize = Marshal.SizeOf(typeof(short));

					for (int i=0;i < _Measurements.Length;i++)
					{
						BytesTool.writeBytes(_Measurements[i], buffer, offset, fieldSize, true);
						offset += fieldSize;
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
				if (Works())
				{
					int sum = Marshal.SizeOf(_LeadId) + Marshal.SizeOf(_LeadLength);

					if (_Measurements != null)
						sum += _LeadLength;

					return sum;
				}
				return 0;
			}
			public bool Works()
			{
				return _Measurements != null;
			}
		}
		#region ILeadMeasurement Members
		public int getLeadMeasurements(out LeadMeasurements mes)
		{
			mes = null;

			if (_NrLeads != 0)
			{
				int nrLeads = _NrLeads;

				mes = new LeadMeasurements(nrLeads);

				for (int i=0;i < nrLeads;i++)
				{
					mes.Measurements[i].LeadType = _LeadMeasurements[i].LeadId;

					int len = _LeadMeasurements[i].Count;

					for (int j=0;j < len;j++)
						mes.Measurements[i][(MeasurementType) j] = _LeadMeasurements[i][j];
				}

				return 0;
			}
			
			return 1;
		}
		public int setLeadMeasurements(LeadMeasurements mes)
		{
			if (mes != null)
			{
				int nrLeads = mes.Measurements.Length;

				_NrLeads = (ushort) nrLeads;

				for (int i=0;i < nrLeads;i++)
				{
					int nrValues = mes.Measurements[i].Count;

					nrValues = (nrLeads > 0) ? ((int) mes.Measurements[i].getKeyByIndex(nrValues-1))+1 : 0;

					_LeadMeasurements[i] = new SCPLeadMeasurements();
					_LeadMeasurements[i].LeadId = mes.Measurements[i].LeadType;
					_LeadMeasurements[i].Count = nrValues;

					nrValues = mes.Measurements[i].Count;

					for (int j=0;j < nrValues;j++)
						_LeadMeasurements[i][mes.Measurements[i].getKeyByIndex(j)] = mes.Measurements[i].getValueByIndex(j);
				}

				return 0;
			}

			return 1;
		}
		#endregion
	}
}
