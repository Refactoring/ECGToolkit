/***************************************************************************
Copyright 2013-2014, van Ettinger Information Technology, Lopik, The Netherlands
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
	/// Class contains section 5 (Reference beat data section).
	/// </summary>
	public class SCPSection5 : SCPSection
	{
		// Defined in SCP.
		private static ushort _SectionID = 5;

		// special variable for setting nr leads before a read.
		private ushort _NrLeads = 0;

		// Part of the stored Data Structure.
		private ushort _AVM = 0;
		private ushort _TimeInterval = 0;
		private byte _Difference = 0;
		private byte _Reserved = 0;
		private ushort[] _DataLength = null;
		private byte[][] _Data = null;
		protected override int _Read(byte[] buffer, int offset)
		{
			if (_NrLeads == 0)
			{
				return 0x1;
			}

			_DataLength = new ushort[_NrLeads];
			_Data = new byte[_NrLeads][];

			int end = offset - Size + Length;
			int startlen = (Marshal.SizeOf(_AVM) + Marshal.SizeOf(_TimeInterval) + Marshal.SizeOf(_Difference) + Marshal.SizeOf(_Reserved));
			startlen += _DataLength.Length * Marshal.SizeOf(_DataLength[0]);
			if ((offset + startlen) > end)
			{
				return 0x2;
			}
			_AVM = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_AVM), true);
			offset += Marshal.SizeOf(_AVM);
			_TimeInterval = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_TimeInterval), true);
			offset += Marshal.SizeOf(_TimeInterval);
			_Difference = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_Difference), true);
			offset += Marshal.SizeOf(_Difference);
			_Reserved = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_Reserved), true);
			offset += Marshal.SizeOf(_Reserved);
			int sum = 0;
			for (int loper=0;loper < _Data.Length;loper++)
			{
				sum += (_DataLength[loper] = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_DataLength[loper]), true));
				offset += Marshal.SizeOf(_DataLength[loper]);
			}
			if ((offset + sum) > end)
			{
				if (_Difference == 0)
				{
					// Begin: special correction for SCP-ECG by corpuls
					int nrBytes = (end - offset);
					int bytesPerLead = nrBytes / _NrLeads;
					
					if ((bytesPerLead < ushort.MaxValue)
					&&	(((bytesPerLead * 1000) / this.getSamplesPerSecond()) >= 1000))
					{
						for (int i=0;i < _NrLeads;i++)
						{
							_DataLength[i] = (ushort) bytesPerLead;
						}
						
						// Here is the trick the data length is missing
						offset -= (_NrLeads << 1);
					}
					else
					{
						return 0x4;
					}
					// End: special correction for SCP-ECG by corpuls
				}
				else
				{
					return 0x4;
				}
			}
			for (int loper=0;loper < _Data.Length;loper++)
			{
				_Data[loper] = new byte[_DataLength[loper]];
				offset += BytesTool.copy(_Data[loper], 0, buffer, offset, _DataLength[loper]);
				if ((_DataLength[loper] & 0x1) == 0x1)
				{
					_DataLength[loper]++;
				}
			}
			return 0x00;
		}
		protected override int _Write(byte[] buffer, int offset)
		{
			BytesTool.writeBytes(_AVM, buffer, offset, Marshal.SizeOf(_AVM), true);
			offset += Marshal.SizeOf(_AVM);
			BytesTool.writeBytes(_TimeInterval, buffer, offset, Marshal.SizeOf(_TimeInterval), true);
			offset += Marshal.SizeOf(_TimeInterval);
			BytesTool.writeBytes(_Difference, buffer, offset, Marshal.SizeOf(_Difference), true);
			offset += Marshal.SizeOf(_Difference);
			BytesTool.writeBytes(_Reserved, buffer, offset, Marshal.SizeOf(_Reserved), true);
			offset += Marshal.SizeOf(_Reserved);

			int offset2 = offset + (_Data.Length * Marshal.SizeOf(_DataLength[0]));

			for (int loper=0;loper < _Data.Length;loper++)
			{
				BytesTool.writeBytes(_DataLength[loper], buffer, offset, Marshal.SizeOf(_DataLength[loper]), true);
				offset += Marshal.SizeOf(_DataLength[loper]);
				BytesTool.copy(buffer, offset2, _Data[loper], 0, _Data[loper].Length);
				offset2 += _DataLength[loper];
			}
			return 0x00;
		}
		protected override void _Empty()
		{
			_AVM = 0;
			_TimeInterval = 0;
			_Difference = 0;
			_Reserved = 0;
			_DataLength = null;
			_Data = null;
		}
		protected override int _getLength()
		{
			if (Works())
			{
				int sum = Marshal.SizeOf(_AVM) + Marshal.SizeOf(_TimeInterval) + Marshal.SizeOf(_Difference) + Marshal.SizeOf(_Reserved);
				sum += (_Data.Length * Marshal.SizeOf(_DataLength[0]));
				for (int loper=0;loper < _Data.Length;loper++)
				{
					sum += _DataLength[loper];
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
			if ((_Data != null)
			&&  (_DataLength != null)
			&&  (_Data.Length == _DataLength.Length)
			&&  (_Data.Length > 0))
			{
				for (int loper=0;loper < _Data.Length;loper++)
				{
					if ((_Data[loper] == null)
					||	(_DataLength[loper] < _Data[loper].Length))
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}
		/// <summary>
		/// Function to set nr of leads used in section (Solution for a tiny problem).
		/// </summary>
		/// <param name="nrleads">nr of leads in section</param>
		public void setNrLeads(ushort nrleads)
		{
			_NrLeads = nrleads;
		}
		/// <summary>
		/// Function to decode data in this section.
		/// </summary>
		/// <param name="tables">Huffman table to use during deconding</param>
		/// <param name="length">nr of samples in encoded data</param>
		/// <returns>decoded leads</returns>
		public short[][] DecodeData(SCPSection2 tables, ushort length)
		{
			if (Works()
			&&	(tables != null))
			{
				// Reset selected table.
				tables.ResetSelect();

				short[][] leads = new short[_Data.Length][];
				for (int loper=0;loper < _Data.Length;loper++)
				{
					leads[loper] = tables.Decode(_Data[loper], 0, _Data[loper].Length, (ushort) ((length * 1000) / _TimeInterval), _Difference); 
					// Check if lead was decoded
					if (leads[loper] == null)
					{
						// return if lead decode failed.
						return null;
					}
				}
				return leads;
			}
			return null;
		}
		/// <summary>
		/// Function to encode data in this section.
		/// </summary>
		/// <param name="data">Rhythm data to encode</param>
		/// <param name="tables">Huffman table to use during enconding</param>
		/// <param name="medianLength">contains length of median data in msec</param>
		/// <param name="difference">difference to use durring decoding</param>
		/// <returns>0 on succes</returns>
		public int EncodeData(short[][] data, SCPSection2 tables, ushort medianLength, byte difference)
		{
			if ((tables != null)
			&&  (data != null))
			{
				ushort nrleads = (ushort) data.Length;
				_Data = new byte[nrleads][];
				_DataLength = new ushort[nrleads];
				for (int loper=0;loper < nrleads;loper++)
				{
					if (data[loper] == null)
					{
						return 2;
					}

					_Difference = difference;
					_Data[loper] = tables.Encode(data[loper], medianLength, 0, _Difference);
					if (_Data[loper] == null)
					{
						_Data = null;
						_DataLength = null;
						return 4;
					}
					_DataLength[loper] = (ushort) _Data[loper].Length;
					if ((_DataLength[loper] & 0x1) == 0x1)
					{
						_DataLength[loper]++;
					}
				}
				return 0;
			}
			return 1;
		}
		/// <summary>
		/// Function to get AVM.
		/// </summary>
		/// <returns>AVM in uV</returns>
		public double getAVM()
		{
			if (_AVM > 0)
			{
				return (((double)_AVM) / 1000.0);
			}
			return -1;
		}
		/// <summary>
		/// Function to set AVM.
		/// </summary>
		/// <param name="avm">AVM in uV</param>
		public void setAVM(double avm)
		{
			if (avm > 0)
			{
				_AVM  = (ushort) (avm * 1000);
			}
		}
		/// <summary>
		/// Function to get samples per second used in data.
		/// </summary>
		/// <returns>samples per second</returns>
		public int getSamplesPerSecond()
		{
			if (_TimeInterval > 0)
			{
				return (1000000 / _TimeInterval);
			}
			return -1;
		}
		/// <summary>
		/// Function to set samples per second used in data.
		/// </summary>
		/// <param name="sps">samples per second</param>
		public void setSamplesPerSecond(int sps)
		{
			if (sps > 0)
			{
				_TimeInterval = (ushort) (1000000 / sps);
			}
		}
	}
}
