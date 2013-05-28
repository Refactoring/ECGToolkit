/***************************************************************************
Copyright 2013, van Ettinger Information Technology, Lopik, The Netherlands
Copyright 2004-2005,2009, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
	/// Class contains section 6 (Rhythm data section).
	/// </summary>
	public class SCPSection6 : SCPSection
	{
		// Defined in SCP.
		private static ushort _SectionID = 6;

		// special variable for setting nr leads before a read.
		private ushort _NrLeads = 0;

		// Part of the stored Data Structure.
		private ushort _AVM = 0;
		private ushort _TimeInterval = 0;
		private byte _Difference = 0;
		private byte _Bimodal = 0;
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
			int startlen = (Marshal.SizeOf(_AVM) + Marshal.SizeOf(_TimeInterval) + Marshal.SizeOf(_Difference) + Marshal.SizeOf(_Bimodal));
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
			_Bimodal = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_Bimodal), true);
			offset += Marshal.SizeOf(_Bimodal);
			int sum = 0;
			for (int loper=0;loper < _Data.Length;loper++)
			{
				sum += (_DataLength[loper] = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_DataLength[loper]), true));
				offset += Marshal.SizeOf(_DataLength[loper]);
			}
			if ((offset + sum) > end)
			{
				return 0x4;
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
			BytesTool.writeBytes(_Bimodal, buffer, offset, Marshal.SizeOf(_Bimodal), true);
			offset += Marshal.SizeOf(_Bimodal);

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
			_Bimodal = 0;
			_DataLength = null;
			_Data = null;
		}
		protected override int _getLength()
		{
			if (Works())
			{
				int sum = Marshal.SizeOf(_AVM) + Marshal.SizeOf(_TimeInterval) + Marshal.SizeOf(_Difference) + Marshal.SizeOf(_Bimodal);
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
			&&  (_Data.Length == _DataLength.Length))
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
		/// <param name="leadDefinition"></param>
		/// <returns>decoded leads</returns>
		public short[][] DecodeData(SCPSection2 tables, SCPSection3 leadDefinition, SCPSection4 qrsLocations, int medianFreq)
		{
			int localFreq = getSamplesPerSecond();

			if (Works()
			&&	(tables != null)
			&&  (tables.Works())
			&&  (leadDefinition != null)
			&&  (leadDefinition.Works())
			&&  (leadDefinition.getNrLeads() == _Data.Length)
			&&  (localFreq > 0))
			{
				if ((medianFreq <= 0)
				||  (medianFreq == localFreq))
				{
					medianFreq = 1;
					localFreq = 1;
				}

				if ((medianFreq < localFreq)
				&&	!leadDefinition.isMediansUsed()
				&&	(_Bimodal == 0x0))
				{
					medianFreq = localFreq;
				}
				
				if (((medianFreq % localFreq) != 0)
				||	((medianFreq / localFreq) < 1)
				||	((medianFreq / localFreq) > 4))
				{
					return null;
				}

				if ((_Bimodal == 0x1)
				&&  (qrsLocations == null))
				{
					return null;
				}

				// Reset selected table.
				tables.ResetSelect();

				short[][] leads = new short[_Data.Length][];
				for (int loper=0;loper < _Data.Length;loper++)
				{
					int time = (leadDefinition.getLeadLength(loper) * localFreq) / medianFreq;

					// Bimodal part might be buggy unable to test.
					if ((localFreq != medianFreq)
					&&	(_Bimodal == 0x1))
					{
						int rate = medianFreq / localFreq;

						// Calculate nr of samples stored in section.
						time = 0;

						int nrzones = qrsLocations.getNrProtectedZones();
						for (int zone=0;zone < nrzones;zone++)
						{
							int begin = (qrsLocations.getProtectedStart(zone) >= leadDefinition.getLeadStart(loper) ? qrsLocations.getProtectedStart(zone) : leadDefinition.getLeadStart(loper));
							int end = (qrsLocations.getProtectedEnd(zone) <= leadDefinition.getLeadEnd(loper) ? qrsLocations.getProtectedEnd(zone) : leadDefinition.getLeadEnd(loper));
							
							begin = (end > begin ? end - begin + 1 : 0);

							time += begin + (rate - (begin % rate));
						}

						time += ((leadDefinition.getLeadLength(loper) - time) * localFreq) / medianFreq;
					}

					leads[loper] = tables.Decode(_Data[loper], 0, _Data[loper].Length, time, _Difference); 
					// Check if lead was decoded
					if (leads[loper] == null)
					{
						// return if lead decode failed.
						return null;
					}

					if (localFreq != medianFreq)
					{
						int rate = medianFreq / localFreq;
						if (_Bimodal == 0x1)
						{
							int beginNonProtected = 0;
							int endNonProtected = qrsLocations.getProtectedStart(0);
							// Restructure bimodal data to length set in Section3.
							short[] temp = new short[leadDefinition.getLeadLength(loper)];

							int time1Offset = leadDefinition.getLeadStart(loper);
							int time1=0;
							int time2=0;

							int zone = 0;

							while ((time1 < temp.Length)
								&& (time2 < leads[loper].Length))
							{
								if (((time1 + time1Offset) >= beginNonProtected)
								&&	((time1 + time1Offset) < endNonProtected))
								{
									for (int begin=0;begin < (rate >> 1);begin++)
									{
										temp[time1++] = leads[loper][time2];
									}

									if ((time2 + ((endNonProtected - (time1 + time1Offset)) / rate)) >= leads[loper].Length)
									{
										endNonProtected -= ((time2 + ((endNonProtected - (time1 + time1Offset)) / rate)) - leads[loper].Length) * rate;
									}

									endNonProtected -= rate + (rate >> 1);

									while ((time1 + time1Offset) < endNonProtected)
									{
										for (int i=0;(i < rate) && (time1 < temp.Length);i++)
										{
											temp[time1++] = (short) (((leads[loper][time2+1] - leads[loper][time2]) / rate) * i + leads[loper][time2]);
										}
										time2++;
									}

									endNonProtected += rate + (rate >> 1);

									for (int end=0;end < (rate >> 1);end++)
									{
										temp[time1++] = leads[loper][time2];
									}

									time2++;

									beginNonProtected = (zone == qrsLocations.getNrProtectedZones() ? temp.Length : qrsLocations.getProtectedEnd(zone));
								}
								else
								{
									// This should never happen!!
									if (zone == qrsLocations.getNrProtectedZones())
									{
										break;
									}

									while (((time1 + time1Offset) < qrsLocations.getProtectedEnd(zone))
										&& (time1 < temp.Length)
										&& (time2 < leads[loper].Length))
									{
										temp[time1++] = leads[loper][time2++];
									}
									zone++;
									endNonProtected = (zone == qrsLocations.getNrProtectedZones() ? temp.Length : qrsLocations.getProtectedStart(zone));
								}
							}

							leads[loper] = temp;
						}
						else
						{
							ECGTool.ResampleLead(leads[loper], localFreq, medianFreq, out leads[loper]);
						}
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
		/// <param name="leadDefinition">Lead Definitions to use for encoding</param>
		/// <param name="difference">difference to use durring decoding</param>
		/// <returns>0 on succes</returns>
		public int EncodeData(short[][] data, SCPSection2 tables, SCPSection3 leadDefinition, SCPSection4 qrsLocations, int medianFreq, byte difference)
		{
			int localFreq = getSamplesPerSecond();

			if ((data != null)
			&&	(tables != null)
			&&	(leadDefinition != null)
			&&  (localFreq > 0))
			{
				if ((medianFreq <= 0)
				||  (medianFreq == localFreq))
				{
					medianFreq = 1;
					localFreq = 1;
				}

				if ((_Bimodal == 0x1)
				&&  (qrsLocations == null))
				{
					return 2;
				}

				ushort nrleads = leadDefinition.getNrLeads();
				_Data = new byte[nrleads][];
				_DataLength = new ushort[nrleads];
				for (int loper=0;loper < nrleads;loper++)
				{
					if (data[loper] == null)
					{
						return 4;
					}

					short[] temp = data[loper];

					int time = (leadDefinition.getLeadLength(loper) * localFreq) / medianFreq;
					if (localFreq != medianFreq)
					{
						int rate = (medianFreq / localFreq);
						// Bimodal part might be buggy unable to test.
						if ((_Bimodal == 0x1)
						&&	((medianFreq % localFreq) == 0)
						&&	(rate > 0)
						&&	(rate < 5))
						{
							// Calculate nr of samples stored in section.
							time = 0;

							int nrzones = qrsLocations.getNrProtectedZones();
							for (int zone=0;zone < nrzones;zone++)
							{
								int begin = (qrsLocations.getProtectedStart(zone) >= leadDefinition.getLeadStart(loper) ? qrsLocations.getProtectedStart(zone) : leadDefinition.getLeadStart(loper));
								int end = (qrsLocations.getProtectedEnd(zone) <= leadDefinition.getLeadEnd(loper) ? qrsLocations.getProtectedEnd(zone) : leadDefinition.getLeadEnd(loper));
							
								begin = (end > begin ? end - begin + 1 : 0);

								time += begin + (rate - (begin % rate));
							}

							time += ((leadDefinition.getLeadLength(loper) - time) * localFreq) / medianFreq;

							int leadLength = leadDefinition.getLeadLength(loper);

							time += ((leadLength - time) * localFreq) / medianFreq;

							// Restructure bimodal data to length set in Section3.
							temp = new short[time];

							int time2Offset = leadDefinition.getLeadStart(loper);
							int time1=0;
							int time2=0;

							while ((time1 < temp.Length)
								&& (time2 <= leadLength)
								&& (time2 < data[loper].Length))
							{
								int zone=0;
								int end = qrsLocations.getNrProtectedZones();
								for (;zone < end;zone++)
								{
									if ((qrsLocations.getProtectedLength(zone) > 0)
									&&	(time2 + time2Offset >= qrsLocations.getProtectedStart(zone))
									&&  (time2 + time2Offset <= qrsLocations.getProtectedEnd(zone)))
									{
										break;
									}
								}

								if (zone < end)
								{
									temp[time1] = data[loper][time2++];
								}
								else
								{
									int Sum = 0;
									for (int sumLoper=0;sumLoper < rate;sumLoper++)
									{
										Sum += data[loper][time2 + sumLoper];
									}
									temp[time1] = (short) (Sum / rate);
									time2 += rate;
								}

								time1++;
							}
						}
						else
						{
							_Bimodal = 0;
							ECGTool.ResampleLead(temp, medianFreq, localFreq, out temp);
						}
					}

					_Difference = difference;
					_Data[loper] = tables.Encode(temp, time, 0, _Difference);
					if (_Data[loper] == null)
					{
						_Data = null;
						_DataLength = null;
						return 8;
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
		/// <summary>
		/// Function to get bimodal settings.
		/// </summary>
		/// <returns>true if bimodal used</returns>
		public bool getBimodal()
		{
			return _Bimodal == 1;
		}
		/// <summary>
		/// Function to set bimodal settings.
		/// </summary>
		/// <param name="bimodal">true if bimodal used</param>
		public void setBimodal(bool bimodal)
		{
			_Bimodal = (byte) (bimodal ? 1 : 0);
		}
	}
}
