/***************************************************************************
Copyright 2012, van Ettinger Information Technology, Lopik, The Netherlands
Copyright 2004-2009, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
	/// Class contains section 2 (HuffmanTables).
	/// </summary>
	/// <remarks>
	/// SCP uses a diffrent way to store codes, then UNIPRO. because I preffer the way UNIPRO
	/// stores its codes. I store the SCP codes the same way as UNIPRO, but when I read/write
	/// them from/to a buffer I reverse the code. This solution keeps the working of the SCP 
	/// and UNIPRO decode/encode very simalar, which is always a positive thing.
	/// </remarks>
	public class SCPSection2 : SCPSection
	{
		// Defined in SCP.
		private static ushort _SectionID = 2;
		private static ushort _DefaultTable = 19999;

		// currently selected table.
		private int _Selected = 0;

		// Part of the stored Data Structure.
		private ushort _NrTables = 0;
		private SCPHuffmanStruct[][] _Tables = null;
		protected override int _Read(byte[] buffer, int offset)
		{
			int end = offset - Size + Length;
			if ((offset + Marshal.SizeOf(_NrTables)) > end)
			{
				return 0x1;
			}
			_NrTables = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_NrTables), true);
			offset += Marshal.SizeOf(_NrTables);
			if (_NrTables < _DefaultTable)
			{
				_Tables = new SCPHuffmanStruct[_NrTables][];
				for (int table=0;table < _NrTables;table++)
				{
					if ((offset + Marshal.SizeOf(_NrTables)) > end)
					{
						_Empty();
						return 0x2;
					}
					_Tables[table] = new SCPHuffmanStruct[BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_NrTables), true)];
					offset += Marshal.SizeOf(_NrTables);
					if ((offset + (_Tables[table].Length * SCPHuffmanStruct.Size)) > end)
					{
						_Empty();
						return 0x4;
					}
					for (int loper=0;loper < _Tables[table].Length;loper++)
					{
						_Tables[table][loper] = new SCPHuffmanStruct();
						int err = _Tables[table][loper].Read(buffer, offset);
						if (err != 0)
						{
							return err << 3 + table;
						}
						offset += SCPHuffmanStruct.Size;
					}
				}
			}
			return 0x0;
		}
		protected override int _Write(byte[] buffer, int offset)
		{
			BytesTool.writeBytes(_NrTables, buffer, offset, Marshal.SizeOf(_NrTables), true);
			offset += Marshal.SizeOf(_NrTables);
			if (_NrTables < _DefaultTable)
			{
				for (int table=0;table < _NrTables;table++)
				{
					BytesTool.writeBytes(_Tables[table].Length, buffer, offset, Marshal.SizeOf(_NrTables), true);
					offset += Marshal.SizeOf(_NrTables);
					for (int loper=0;loper < _Tables[table].Length;loper++)
					{
						int err = _Tables[table][loper].Write(buffer, offset);
						if (err != 0)
						{
							return err << table;
						}
						offset += SCPHuffmanStruct.Size;
					}
				}
			}
			return 0x0;
		}
		protected override void _Empty()
		{
			_Tables = null;
			_NrTables = 0;
		}
		protected override int _getLength()
		{
			if (Works())
			{
				int sum = Marshal.SizeOf(_NrTables);
				if (_NrTables != _DefaultTable)
				{
					for (int table=0;table < _NrTables;table++)
					{
						sum += Marshal.SizeOf(_NrTables) + (_Tables[table].Length * SCPHuffmanStruct.Size);
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
			if ((_Tables != null)
			&&  (_NrTables == _Tables.Length))
			{
				for (int table=0;table < _Tables.Length;table++)
				{
					if (_Tables[table] == null)
					{
						return false;
					}
					for (int loper=0;loper < _Tables[table].Length;loper++)
					{
						if (_Tables[table][loper] == null)
						{
							return false;
						}
					}
				}
				return true;
			}
			else if ((_Tables == null)
				&&	 ((_NrTables == _DefaultTable)
				||	  (_NrTables == 0)))
			{
				return true;
			}
			return false;
		}
		/// <summary>
		/// Function to decode encoded data.
		/// </summary>
		/// <param name="buffer">buffer to read in</param>
		/// <param name="offset">position to start reading</param>
		/// <param name="nrbytes">nrbytes of encoded bytes in buffer</param>
		/// <param name="length">length of signal in samples</param>
		/// <param name="difference">difference to use durring decoding</param>
		/// <returns>short array containing decoded data</returns>
		public short[] Decode(byte[] buffer, int offset, int nrbytes, int length, byte difference)
		{
			if (Works() || (_NrTables == 0))
			{
				if (_NrTables == _DefaultTable)
				{
					return InhouseDecode(buffer, offset, nrbytes, length, difference);
				}
				else if (_NrTables == 0)
				{
					return NoDecode(buffer, offset, nrbytes, length, difference);
				}
				else
				{
					return HuffmanTableDecode(buffer, offset, nrbytes, length, difference);
				}
			}
			return null;
		}
		/// <summary>
		/// Function to do huffman decode of encoded data. (using SCP default huffmantable)
		/// </summary>
		/// <param name="buffer">buffer to read in</param>
		/// <param name="offset">position to start reading</param>
		/// <param name="nrbytes">nrbytes of encoded bytes in buffer</param>
		/// <param name="length">length of signal in samples</param>
		/// <param name="difference">difference to use durring decoding</param>
		/// <returns>short array containing decoded data</returns>
		public static short[] InhouseDecode(byte[] buffer, int offset, int nrbytes, int length, byte difference)
		{
			// This safes us some calculations.
			nrbytes += offset;

			// Check if input data makes sense.
			if ((buffer != null)
			&&  (nrbytes <= buffer.Length))
			{
				// Setting up the variables for decode.
				short[] leadData = new short[length];
				int currentTime = 0;
				int currentBit = (offset << 3);
				int max = 9;

				while (((currentBit >> 3) < nrbytes)
					&& ((currentTime) < length))
				{
					int count = currentBit;
					int cmax = currentBit + max;
					// Read in bits till 0 bit or defined maximum.
					for (;(currentBit < cmax) && ((currentBit >> 3) < nrbytes) && (((buffer[currentBit >> 3] >> (0x7 - (currentBit & 0x7))) & 0x1) != 0);currentBit++);

					// determine number of bits
					count = currentBit - count;

					// Increase current bit one more time
					if (count != max)
					{
						currentBit++;
					}

					// If it doesn't fit stop
					if ((currentBit >> 3) >= nrbytes)
					{
						break;
					}

					if (count >= max)
					{
						// Read in last bit
						bool tmp = (((buffer[currentBit >> 3] >> (0x7 - (currentBit & 0x7))) & 0x1) == 0);
						currentBit++;
						// store starting point of additional bits.
						int start = currentBit;
						// If last bit 0 read in 8 additional bits else 16 additional bits.
						int stop = currentBit + (tmp ? 8 : 16);

						// If it doesn't fit return with error
						if ((stop >> 3) >= nrbytes)
						{
							break;
						}

						// Reading in number of extra  bits.
						for (count=0;currentBit < stop;currentBit++)
						{
							count <<= 1;
							count |= ((buffer[currentBit >> 3] >> (0x7 - (currentBit & 0x7))) & 0x1);
							if ((start == currentBit)
								&&  (count != 0))
							{
								count = -1;
							}
						}
					}
					else if (count != 0)
					{
						// If it doesn't fit stop
						if ((currentBit >> 3) >= nrbytes)
						{
							break;
						}
						// if last bit is one value is negative.
						if (((buffer[currentBit >> 3] >> (0x7 - (currentBit & 0x7))) & 0x1) != 0)
						{
							count = -count;
						}
						currentBit++;
					}

					// Decode Differences.
					switch (difference)
					{
						case 0:
							leadData[currentTime] = ((short) count);
							break;
						case 1:
							leadData[currentTime] = ((currentTime == 0) ? (short) count : (short) (count + leadData[currentTime - 1]));
							break;
						case 2:
							leadData[currentTime] = ((currentTime < 2) ? (short) count : (short) (count + (leadData[currentTime - 1] << 1) - leadData[currentTime - 2]));
							break;
						default:
							// Undefined difference used exit empty.
							return null;
					}
					// Increment time by one.
					currentTime++;
				}
				return leadData;
			}
			return null;
		}
		/// <summary>
		/// Function to do huffman decode of encoded data.
		/// </summary>
		/// <param name="buffer">buffer to read in</param>
		/// <param name="offset">position to start reading</param>
		/// <param name="nrbytes">nrbytes of encoded bytes in buffer</param>
		/// <param name="length">length of signal in samples</param>
		/// <param name="difference">difference to use durring decoding</param>
		/// <returns>short array containing decoded data</returns>
		private short[] HuffmanTableDecode(byte[] buffer, int offset, int nrbytes, int length, byte difference)
		{
			// This safes us some calculations.
			nrbytes += offset;

			// Check if input data makes sense.
			if ((buffer != null)
			&&  (nrbytes <= buffer.Length))
			{
				// Setting up the variables for decode.
				short[] leadData = new short[length];
				int currentTime = 0;
				int currentBit = (offset << 3);

				while (((currentBit >> 3) < nrbytes)
					&& ((currentTime) < length))
				{
					// Search for a hit.
					SCPHuffmanStruct h = InterpettingData(buffer, currentBit);
					// Exit if there was no hit.
					if (h == null)
					{
						return null;
					}

					// Check if hit fits.
					if (((currentBit + h.entire) >> 3) >= nrbytes)
					{
						break;
					}

					// If table mode is 0 do switch.
					if (h.tablemode == 0)
					{
						_Selected = h.value - 1;
						continue;
					}

					short code = 0;
					// read extra data behind hit if available.
					for (int count=0, start=(currentBit + h.prefix);count < (h.entire - h.prefix);count++)
					{
						code <<= 1;
						code += (short) ((buffer[(start + count) >> 3] >> (0x7 - ((start + count) & 0x7))) & 0x1);
						if ((count == 0)
						&&  (code != 0))
						{
							code = -1;
						}
					}
					// add up a the value of the hit.
					code += h.value;

					// Decode Differences.
					switch (difference)
					{
						case 0:
							leadData[currentTime] = code;
							break;
						case 1:
							leadData[currentTime] = ((currentTime == 0) ? code : (short) (code + leadData[currentTime - 1]));
							break;
						case 2:
							leadData[currentTime] = ((currentTime < 2) ? code : (short) (code + (leadData[currentTime - 1] << 1) - leadData[currentTime - 2]));
							break;
						default:
							// Undefined difference used exit empty.
							return null;
					}

					// Increment current bit
					currentBit += h.entire;

					// Increment time by one.
					currentTime++;
				}
				return leadData;
			}
			return null;
		}
		/// <summary>
		/// Find next hit in byte array starting at an offset in bits.
		/// </summary>
		/// <param name="buffer">byte array containing encoded data</param>
		/// <param name="offset">position (in bits) to start searching for a hit</param>
		/// <returns>Info of hit.</returns>
		private SCPHuffmanStruct InterpettingData(byte[] buffer, int offset)
		{
			if ((_Tables[_Selected] != null)
			&&  (buffer != null))
			{
				uint bitBuffer = 0;
				int readMax = _Tables[_Selected][_Tables[_Selected].Length-1].prefix;
				for (int read=0;read < readMax&&((read + offset) >> 3) < buffer.Length;read++)
				{
					bitBuffer <<= 1;
					bitBuffer |= (uint) ((buffer[(offset + read) >> 3] >> (0x7 - ((offset + read) & 0x7))) & 0x1);

					for (int table = 0;table < _Tables[_Selected].Length;table++)
					{
						if ((bitBuffer == _Tables[_Selected][table].code)
						&&  ((read + 1) == _Tables[_Selected][table].prefix))
						{
							return _Tables[_Selected][table];
						}
					}
				}
			}
			return null;
		}
		/// <summary>
		/// Function to do interpetting of unencoded data.
		/// </summary>
		/// <param name="buffer">buffer to read in</param>
		/// <param name="offset">position to start reading</param>
		/// <param name="nrbytes">nrbytes of encoded bytes in buffer</param>
		/// <param name="length">length of signal in samples</param>
		/// <param name="difference">difference to use durring decoding</param>
		/// <returns>short array containing decoded data</returns>
		public static short[] NoDecode(byte[] buffer, int offset, int nrbytes, int length, byte difference)
		{
			// Check if input data makes sense.
			if ((buffer != null)
			&&  ((offset + nrbytes) <= buffer.Length)
			&&  ((length * Marshal.SizeOf(typeof(short))) <= nrbytes))
			{
				short[] leadData = new short[length];
				for (int currentTime=0;currentTime < length;currentTime++)
				{
					short code = (short) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(typeof(short)), true);
					offset += Marshal.SizeOf(typeof(short));

					// Decode Differences.
					switch (difference)
					{
						case 0:
							leadData[currentTime] = code;
							break;
						case 1:
							leadData[currentTime] = ((currentTime == 0) ? code : (short) (code + leadData[currentTime - 1]));
							break;
						case 2:
							leadData[currentTime] = ((currentTime < 2) ? code : (short) (code + (leadData[currentTime - 1] << 1) - leadData[currentTime - 2]));
							break;
						default:
							// Undefined difference used exit empty.
							return null;
					}
				}
				return leadData;
			}
			return null;
		}
		/// <summary>
		/// Function to encode data.
		/// </summary>
		/// <param name="data">signal to read from</param>
		/// <param name="time">number of samples to use</param>
		/// <param name="usedTable">table to use for encoding</param>
		/// <param name="difference">difference to use durring decoding</param>
		/// <returns>byte array containing encoded data</returns>
		public byte[] Encode(short[] data, int time, short usedTable, byte difference)
		{
			if (Works() || _NrTables == 0)
			{
				if (_NrTables == _DefaultTable)
				{
					return InhouseEncode(data, time, difference);
				}
				else if (_NrTables == 0)
				{
					return NoEncode(data, time, difference);
				}
				else
				{
					return HuffmanTableEncode(data, time, usedTable, difference);
				}
			}
			return null;
		}
		/// <summary>
		/// Function to encode signal using the default huffman table (using optimized code).
		/// </summary>
		/// <param name="data">signal to read from</param>
		/// <param name="time">number of samples to use</param>
		/// <param name="difference">difference to use durring decoding</param>
		/// <returns>byte array containing encoded data</returns>
		public static byte[] InhouseEncode(short[] data, int time, byte difference)
		{
			byte[] ret = null;

			// Check if input makes sense
			if ((data != null)
			&&	(time <= data.Length))
			{
				// Initialize some handy variables
				int currentBit = 0;

				// Make buffer for worst case.
				byte[] buffer = new byte[((time * 26) >> 3) + 1];

				// For each sample do encode.
				for (int currentTime=0;currentTime < time;currentTime++)
				{
					short code = 0;

					// Encode Differences.
					switch (difference)
					{
						case 0:
							code = data[currentTime];
						break;
						case 1:
							code = (short) ((currentTime < 1) ? data[currentTime] : data[currentTime] - data[currentTime - 1]);
						break;
						case 2:
							code = (short) ((currentTime < 2) ? data[currentTime] : data[currentTime] - (data[currentTime - 1] << 1) + data[currentTime - 2]);
						break;
						default:
							// Undefined difference used exit empty.
							return null;
					}

					// Do inhouse encode
                    //System.Diagnostics.Debug.WriteLine("Begin inhouse!"+code.ToString());
                    if ( code == -32768) code = 32767;
                    if (Math.Abs(code) <= 8)
					{
                      //  System.Diagnostics.Debug.WriteLine("Begin !");
						// if code 0 then add one 0 bit.
						if (code == 0)
						{
							buffer[currentBit >> 3] <<= 1;
							currentBit++;
						}
						else
						{
							// add absolute number of 1 bits.
							int codeAbs = Math.Abs(code);
							for (int loper=0;loper < codeAbs;loper++)
							{
								buffer[currentBit >> 3] <<= 1;
								buffer[currentBit >> 3] |= 1;
								currentBit++;
							}

							// add one 0 bit.
							buffer[currentBit >> 3] <<= 1;
							currentBit++;

							// add one more bit for positive of negative
							buffer[currentBit >> 3] <<= 1;
							if (code < 0)
							{
								buffer[currentBit >> 3] |= 1;
							}
							currentBit++;
						}
					}
					else
					{
                        // Code doesn't fit in normal table do special.
						// First add nine 1 bits.
						for (int loper=0;loper < 9;loper++)
						{
							buffer[currentBit >> 3] <<= 1;
							buffer[currentBit >> 3] |= 1;
							currentBit++;
						}

						// Add one more bit depending on size of code
						buffer[currentBit >> 3] <<= 1;
						int extraLength = 8;
						if (!((code <= 127) && (code >= -128)))
						{
							buffer[currentBit >> 3] |= 1;
							extraLength = 16;
						}
						currentBit++;

						// Add bits for extra code.
						for (extraLength--;extraLength >= 0;extraLength--)
						{
							buffer[currentBit >> 3] <<= 1;
							buffer[currentBit >> 3] |= (byte)((code >> extraLength) & 0x1);
							currentBit++;
                            int currentBitShift = currentBit >> 3;
						}
                    }
                }

				// Shift end to right position.
				if ((currentBit & 0x7) != 0x0)
				{
					buffer[(currentBit >> 3)] <<= (0x8 - (currentBit & 0x7));
					currentBit += (0x8 - (currentBit & 0x7));
				}
				else
				{
					// seems to solve a small encoding bug.
					currentBit += 8;
				}

				// Allocate a fitting buffer
				ret = new byte[(currentBit >> 3)];

				// Copy worst case buffer in fitting buffer.
				for (int loper = 0;loper < ret.Length;loper++)
				{
					ret[loper] = buffer[loper];
				}
			}
			return ret;
		}
		/// <summary>
		/// Function to encode signal using the huffman table.
		/// </summary>
		/// <param name="data">signal to read from</param>
		/// <param name="time">number of samples to use</param>
		/// <param name="quanta">sample distance in signal</param>
		/// <param name="usedTable">table to use for encoding</param>
		/// <param name="difference">difference to use durring decoding</param>
		/// <returns>byte array containing encoded data</returns>
		private byte[] HuffmanTableEncode(short[] data, int time, short usedTable, byte difference)
		{
			byte[] ret = null;

			// Check if input makes sense
			if ((data != null)
			&&	(time <= data.Length))
			{
				// Initialize some handy variables
				int currentBit = 0;

				// Make buffer for worst case.
				byte[] buffer = null;

				if ((usedTable >= 0)
				&&  (usedTable < _Tables.Length)
				&&  (usedTable != _Selected))
				{
					uint code = 0;
					int len = 0;

					// get TableSwap position in HuffmanTable.
					int p = getTableSwap(usedTable);

					// Check if table swap is possible in this table.
					if (p >= 0)
					{
						// Store needed data from swap HuffmanStruct.
						code = _Tables[_Selected][p].code;
						len = _Tables[_Selected][p].entire;

						// set currently selected table.
						_Selected = usedTable;
					}

					// allocate buffer for worstcase.
					buffer = new byte[((len + (time * getWorstCase())) >> 3) + 1];

					// add table swap.
					for (len--;len >= 0;len--)
					{
						buffer[currentBit >> 3] <<= 1;
						buffer[currentBit >> 3] |= (byte)((code >> len) & 0x1);
						currentBit++;
					}
				}
				else
				{
					// No tables swap, so only space needed for worst case.
					buffer = new byte[((time * getWorstCase()) >> 3) + 1];
				}

				// For each sample do encode.
				for (int currentTime=0;currentTime < time;currentTime++)
				{
					short code = 0;

					// Encode Differences.
					switch (difference)
					{
						case 0:
							code = data[currentTime];
							break;
						case 1:
							code = (short) ((currentTime < 1) ? data[currentTime] : data[currentTime] - data[currentTime - 1]);
							break;
						case 2:
							code = (short) ((currentTime < 2) ? data[currentTime] : data[currentTime] - (data[currentTime - 1] << 1) + data[currentTime - 2]);
							break;
						default:
							// Undefined difference used exit empty.
							return null;
					}

					// Call Interpetting data to get an hit.
					SCPHuffmanStruct h = InterpettingData(code);
					if (h == null)
					{
						// not hit table or data must be wrong.
						return null;
					}

					// Push in the code.
					for (int loper=(h.prefix-1);loper >= 0;loper--)
					{
						buffer[currentBit >> 3] <<= 1;
						buffer[currentBit >> 3] |= (byte)((h.code >> loper) & 0x1);
						currentBit++;
					}

					// Push in the extra code, for special case.
					uint now = (uint) (code - h.value);
					for (int loper=(h.entire - h.prefix - 1);loper >= 0;loper--)
					{
						buffer[currentBit >> 3] <<= 1;
						buffer[currentBit >> 3] |= (byte)((code >> loper) & 0x1);
						currentBit++;
					}
				}

				// Shift end to right position.
				if ((currentBit & 0x7) != 0x0)
				{
					buffer[(currentBit >> 3)] <<= (0x8 - (currentBit & 0x7));
					currentBit += (0x8 - (currentBit & 0x7));
				}
				else
				{
					// seems to solve a small encoding bug.
					currentBit += 8;
				}

				// Allocate a fitting buffer
				ret = new byte[(currentBit >> 3)];

				// Copy worst case buffer in fitting buffer.
				for (int loper = 0;loper < ret.Length;loper++)
				{
					ret[loper] = buffer[loper];
				}
			}
			return ret;
		}
		/// <summary>
		/// Function to find corresponding HuffmanStruct with value.
		/// </summary>
		/// <param name="value">value to search</param>
		/// <returns>corresponding HuffmanStruct</returns>
		private SCPHuffmanStruct InterpettingData(short value)
		{
			// Check if selected Table exists
			if ((_Tables != null)
			&&	(_Tables[_Selected] != null))
			{
				// Search in structs of table.
				for (int loper=0;loper < _Tables[_Selected].Length;loper++)
				{
					SCPHuffmanStruct h = _Tables[_Selected][loper];
					// -1, because it can be positive and negative
					int extra = (h.entire - h.prefix - 1);

					// Check if value is equal to struct.
					if ((h.value == value)
					&&  (h.tablemode != 0))
					{
						return h;
					}
					// Check if value fits in special case.
					else if ((extra > 0)
						&&	 ((value - h.value) < (0x1 << extra))
						&&   ((value - h.value) >= -(0x1 << extra))
						&&	 (h.tablemode != 0))
					{
						return h;
					}
				}
			}
			return null;
		}
		/// <summary>
		/// Function to store signal using no compression.
		/// </summary>
		/// <param name="data">signal to read from</param>
		/// <param name="time">number of samples to use</param>
		/// <param name="quanta">sample distance in signal</param>
		/// <param name="difference">difference to use durring decoding</param>
		/// <returns>byte array containing encoded data</returns>
		public static byte[] NoEncode(short[] data, int time, byte difference)
		{
			// Check if input data makes sense.
			if ((data != null)
			&&	(time <= data.Length))
			{
				// Initializing some handy variables
				int offset = 0;
				int sizeOfSample = Marshal.SizeOf(typeof(short));

				// Make buffer to contain samples
				byte[] ret = new byte[time * sizeOfSample];

				// For each sample do encode.
				for (int currentTime=0;currentTime < time;currentTime++)
				{
					short code = 0;

					// Encode Differences.
					switch (difference)
					{
						case 0:
							code = data[currentTime];
							break;
						case 1:
							code = (short) ((currentTime < 1) ? data[currentTime] : data[currentTime] - data[currentTime - 1]);
							break;
						case 2:
							code = (short) ((currentTime < 2) ? data[currentTime] : data[currentTime] - (data[currentTime - 1] << 1) + data[currentTime - 2]);
							break;
						default:
							// Undefined difference used exit empty.
							return null;
					}

					// Write data in buffer.
					BytesTool.writeBytes(code, ret, offset, sizeOfSample, true);
					offset += sizeOfSample;
				}
				return ret;
			}
			return null;
		}
		/// <summary>
		/// Resets the current selected HuffmanTable
		/// </summary>
		public void ResetSelect()
		{
			if (Works())
			{
				_Selected = 0;
			}
		}
		/// <summary>
		/// Function to get position of table swap.
		/// </summary>
		/// <param name="table">prefered table</param>
		/// <returns>position in current table</returns>
		private int getTableSwap(int table)
		{
			if (Works()
			&&  (table < _Tables.Length)
			&&	(_Selected < _Tables.Length))
			{
				for (int loper=0;loper < _Tables[_Selected].Length;loper++)
				{
					if (_Tables[_Selected][loper].tablemode == 0
					&&  _Tables[_Selected][loper].value == (table + 1))
					{
						return loper;
					}
				}
			}
			return -1;
		}
		/// <summary>
		/// Function to get binary length of worst case of selected table.
		/// </summary>
		/// <returns>length of worst case</returns>
		private int getWorstCase()
		{
			int worst = -1;
			if (Works())
			{
				if (this._NrTables == _DefaultTable)
				{
					worst = 26;
				}
				else if (_NrTables == 0)
				{
					worst = 16;
				}
				else
				{
					for (int loper=0;loper < _Tables[_Selected].Length;loper++)
					{
						if (_Tables[_Selected][loper].entire > worst)
						{
							worst = _Tables[_Selected][loper].entire;
						}
					}
				}
			}
			return worst;
		}
		/// <summary>
		/// Function to set standard huffman table.
		/// </summary>
		public void UseStandard()
		{
			_NrTables = _DefaultTable;
		}
		/// <summary>
		/// Function to set no huffman encoding.
		/// </summary>
		public void UseNoHuffman()
		{
			_NrTables = 0;
		}
		/// <summary>
		/// Class that contains a SCP Huffman struct
		/// </summary>
		public class SCPHuffmanStruct
		{
			public const int Size = 9;

			public byte prefix;
			public byte entire;
			public byte tablemode;
			public short value;
			public uint code;
			public SCPHuffmanStruct()
			{}
			public SCPHuffmanStruct(byte prefix, byte entire, byte tablemode, short value, uint code)
			{
				this.prefix = prefix;
				this.entire = entire;
				this.tablemode = tablemode;
				this.value = value;
				this.code = code;
			}
			/// <summary>
			/// Function to read an SCP huffman struct.
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

				prefix = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(prefix), true);
				offset += Marshal.SizeOf(prefix);
				entire = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(entire), true);
				offset += Marshal.SizeOf(entire);
				tablemode = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(tablemode), true);
				offset += Marshal.SizeOf(tablemode);
				value = (short) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(value), true);
				offset += Marshal.SizeOf(value);
				uint tempCode = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(code), true);
				offset += Marshal.SizeOf(code);

				// Have to reverse the code, because SCP stores it that way.
				code = 0;
				for (int loper=prefix;loper > 0;loper--)
				{
					code <<= 1;
					code |= (tempCode & 0x1);
					tempCode >>= 1;
				}

				return 0x0;
			}
			/// <summary>
			/// Function to write SCP huffman struct.
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

				BytesTool.writeBytes(prefix, buffer, offset, Marshal.SizeOf(prefix), true);
				offset += Marshal.SizeOf(prefix);
				BytesTool.writeBytes(entire, buffer, offset, Marshal.SizeOf(entire), true);
				offset += Marshal.SizeOf(entire);
				BytesTool.writeBytes(tablemode, buffer, offset, Marshal.SizeOf(tablemode), true);
				offset += Marshal.SizeOf(tablemode);
				BytesTool.writeBytes(value, buffer, offset, Marshal.SizeOf(value), true);
				offset += Marshal.SizeOf(value);

				// Have to reverse the code, becaus SCP stores it that way.
				uint tempCode1 = code;
				uint tempCode2 = 0;
				for (int loper=prefix;loper > 0;loper--)
				{
					tempCode2 <<= 1;
					tempCode2 |= (tempCode1 & 0x1);
					tempCode1 >>= 1;
				}

				BytesTool.writeBytes((int)tempCode2, buffer, offset, Marshal.SizeOf(code), true);
				offset += Marshal.SizeOf(code);

				return 0x0;
			}
		}
	}
}
