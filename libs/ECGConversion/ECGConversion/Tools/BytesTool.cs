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
using System.IO;
using System.Text;

namespace Communication.IO.Tools
{
	public class BytesTool
	{
		/// <summary>
		/// Function to read a given nr of bytes from a Stream.
		/// </summary>
		/// <param name="stream">to read from</param>
		/// <param name="buffer">byte array to write in</param>
		/// <param name="offset">position to start writing in buffer</param>
		/// <param name="count">nr of bytes to read</param>
		/// <returns>nr of bytes read</returns>
		public static int readStream(Stream stream, byte[] buffer, int offset, int count)
		{
			int readBytes = -1;
			int nBytes = -1;
			if (stream.CanRead
			&&	(buffer.Length >= (offset + count)))
			{
				readBytes = 0;
				while (readBytes < count
					&& nBytes != 0)
				{
					nBytes = stream.Read(buffer, offset + readBytes, count - readBytes);
					readBytes += nBytes;
				}
			}
			return readBytes;
		}
		/// <summary>
		/// Function to read a integer from a buffer at an offset.
		/// </summary>
		/// <param name="buffer">source buffer</param>
		/// <param name="offset"></param>
		/// <param name="bytes">length of integer</param>
		/// <param name="littleEndian">little endian or big endian integer</param>
		/// <returns>read integer</returns>
		public static long readBytes(byte[] buffer, int offset, int bytes, bool littleEndian)
		{
			long returnValue = 0;
			if (bytes > 8)
			{
				bytes = 8;
			}
			if (offset + bytes <= buffer.Length)
			{
				for (int read=0;read < bytes;read++)
				{
					returnValue |= (long) buffer[offset + (littleEndian ? read : (bytes-read-1))] << (read << 3);
				}
			}
			return returnValue;
		}
		/// <summary>
		/// Function to write an integer to a buffer at an offset.
		/// </summary>
		/// <param name="values">integer to write</param>
		/// <param name="buffer">buffer to write to</param>
		/// <param name="offset">position to start writing</param>
		/// <param name="bytes">nr bytes to write</param>
		/// <param name="littleEndian">little endian or big endian integer</param>
		/// <returns></returns>
		public static bool writeBytes(long values, byte[] buffer, int offset, int bytes, bool littleEndian)
		{
			if ((buffer != null)
			&&  (offset + bytes) <= buffer.Length
			&&  (bytes <= 8)
			&&  (bytes > 0))
			{
				for (int read=0;read < bytes;read++)
				{
					buffer[offset + (littleEndian ? read : (bytes-read-1))] = (byte) ((values >> (read << 3)) & 0xff);
				}
				return true;
			}
			return false;
		}
		/// <summary>
		///	Function to read a string from a byte array at a given offset
		/// </summary>
		/// <param name="buffer">to read the string from</param>
		/// <param name="offset">position to start reading</param>
		/// <param name="length">max length of string</param>
		/// <returns>a string</returns>
		public static string readString(byte[] buffer, int offset, int length)
		{
			return readString(Encoding.ASCII, buffer, offset, length);
		}
		/// <summary>
		///	Function to read a string from a byte array at a given offset
		/// </summary>
		/// <param name="buffer">to read the string from</param>
		/// <param name="offset">position to start reading</param>
		/// <param name="length">max length of string</param>
		/// <param name="value">value to use as terminator of string</param>
		/// <returns>a string</returns>
		public static string readString(byte[] buffer, int offset, int length, byte value)
		{
			string ret = null;
			length = stringLength(buffer, offset, length, value);
			if (length != 0)
			{
				char[] a = new char[length];
				for (int x=0;x < length;x++)
				{
					a[x] = (char) buffer[offset + x];
				}
				ret = new string(a, 0, length);
			}
			return ret;
		}
		/// <summary>
		///	Function to read a string from a byte array at a given offset
		/// </summary>
		/// <param name="enc">enconding type</param>
		/// <param name="buffer">to read the string from</param>
		/// <param name="offset">position to start reading</param>
		/// <param name="length">max length of string</param>
		/// <returns>a string</returns>
		public static string readString(Encoding enc, byte[] buffer, int offset, int length)
		{
			string ret = null;
			
			length = stringLength(enc, buffer, offset, length);
			if (length != 0)
			{
				ret = enc.GetString(buffer, offset, length);
			}
			return ret;
		}
		/// <summary>
		/// Function to calculate length of string in buffer starting at an offset.
		/// </summary>
		/// <param name="buffer">source buffer</param>
		/// <param name="offset">position to start counting from</param>
		/// <param name="length">max length of string</param>
		/// <returns>length of string</returns>
		public static int stringLength(byte[] buffer, int offset, int length)
		{
			return stringLength(Encoding.ASCII, buffer, offset, length);
		}
		/// <summary>
		/// Function to calculate length of string in buffer starting at an offset.
		/// </summary>
		/// <param name="buffer">source buffer</param>
		/// <param name="offset">position to start counting from</param>
		/// <param name="length">max length of string</param>
		/// <param name="value">value to use as terminator of string</param>
		/// <returns>length of string</returns>
		public static int stringLength(byte[] buffer, int offset, int length, byte value)
		{
			int x=0;

			if (length < 0)
			{
				while ((buffer != null)
					&& ((offset + x) < buffer.Length)
					&& (buffer[offset+x] != value))
				{
					x++;
				}
			}
			else
			{
				while ((buffer != null)
					&& ((offset + x) < buffer.Length)
					&& (x < length)
					&& (buffer[offset+x] != value))
				{
					x++;
				}
			}
			return x;
		}
		/// <summary>
		/// Function to calculate length of string in buffer starting at an offset.
		/// </summary>
		/// <param name="enc">enconding type</param>
		/// <param name="buffer">source buffer</param>
		/// <param name="offset">position to start counting from</param>
		/// <param name="length">max length of string</param>
		/// <returns>length of string</returns>
		public static int stringLength(Encoding enc, byte[] buffer, int offset, int length)
		{
			int len = 0;

			foreach (char temp in enc.GetChars(buffer, offset, (buffer.Length < (offset + length)) ? buffer.Length - offset : length))
			{
				if (temp == '\0')
					break;

				len++;
			}

			return len;
		}
		/// <summary>
		///	Function to write a string too a byte array at a given offset
		/// </summary>
		/// <param name="src">to read from</param>
		/// <param name="buffer">to write the string too</param>
		/// <param name="offset">position to start reading</param>
		/// <param name="length">max length of string</param>
		public static void writeString(string src, byte[] buffer, int offset, int length)
		{
			writeString(Encoding.ASCII, src, buffer, offset, length);
		}
		/// <summary>
		///	Function to write a string too a byte array at a given offset
		/// </summary>
		/// <param name="enc">enconding type</param>
		/// <param name="src">to read from</param>
		/// <param name="buffer">to write the string too</param>
		/// <param name="offset">position to start reading</param>
		/// <param name="length">max length of string</param>
		public static void writeString(Encoding enc, string src, byte[] buffer, int offset, int length)
		{
			if ((src != null)
			&&	(buffer != null))
			{
				int nrChars = enc.GetMaxCharCount((buffer.Length < (offset + length)) ? buffer.Length - offset : length);

                nrChars = (src.Length < nrChars) ? src.Length : nrChars;

				if (nrChars > 0)
			        enc.GetBytes(src, 0, nrChars, buffer, offset);
			}
		}
		/// <summary>
		/// Function to copy content of one buffer to another.
		/// </summary>
		/// <param name="dst">destination buffer</param>
		/// <param name="offdst">offset in destination buffer</param>
		/// <param name="src">source buffer</param>
		/// <param name="offsrc">offset in source buffer</param>
		/// <param name="length">number bytes to copy</param>
		public static int copy(byte[] dst, int offdst, byte[] src, int offsrc, int length)
		{
			int loper=0;
			for (;(loper < length) && ((offdst + loper) < dst.Length) && ((offsrc + loper) < src.Length);loper++)
			{
				dst[offdst + loper] = src[offsrc + loper];
			}
			return loper;
		}
		/// <summary>
		/// Function to empty a buffer to a defined number.
		/// </summary>
		/// <param name="buffer">destination buffer</param>
		/// <param name="offset">offset in buffer</param>
		/// <param name="nrbytes">number byte to empty</param>
		/// <param name="type">number to empty to</param>
		public static void emptyBuffer(byte[] buffer, int offset, int nrbytes, byte type)
		{
			for (int x=0;(x < nrbytes)&&((x + offset) < buffer.Length);x++)
			{
				buffer[offset + x] = type;
			}
		}
	}
}
