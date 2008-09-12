/***************************************************************************
Copyright 2004-2005, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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

namespace ECGConversion
{
	/// <summary>
	/// ECGWriter class. ECG Writer contains static function to write any ECG Format.
	/// </summary>
	public class ECGWriter
	{
		private static int _LastError = 0;
		/// <summary>
		/// Function to write ECG file of any kind to a file.
		/// </summary>
		/// <param name="format">ECG file to write</param>
		/// <param name="file">path to write to</param>
		/// <param name="overwrite">true if you want to overwrite existing files</param>
		/// <returns>error:
		/// 0x0) success</returns>
		public static void Write(IECGFormat format, string file, bool overwrite)
		{
			Stream output = null;

			try
			{
				output = new FileStream(file, (overwrite ? FileMode.Create : FileMode.CreateNew));
				Write(format, output); 
			}
			catch
			{
				_LastError = 1;
			}
			finally
			{
				if (output != null)
					output.Close();
			}
		}
		/// <summary>
		/// unction to write ECG file of any kind to a stream
		/// </summary>
		/// <param name="format">ECG file to write</param>
		/// <param name="output">stream to write to</param>
		/// <returns>error:
		/// 0x0) success</returns>
		public static void Write(IECGFormat format, Stream output)
		{
			if (format != null)
			{
				_LastError = (format.Write(output) << 2);
			}
			else
			{
				_LastError = 2;
			}
		}
		/// <summary>
		/// Function to write ECG file of any kind to byte array.
		/// </summary>
		/// <param name="format">ECG file to write</param>
		/// <param name="buffer">byte array to write to</param>
		/// <param name="offset"></param>
		/// <returns>error:
		/// 0x0) success</returns>
		public static void Write(IECGFormat format, byte[] buffer, int offset)
		{
			if (format != null)
			{
				_LastError = (format.Write(buffer, offset) << 2);
			}
			else
			{
				_LastError = 2;
			}
		}
		/// <summary>
		/// get last error
		/// </summary>
		/// <returns>error as an integer</returns>
		public static int getLastError()
		{
			return _LastError;
		}
		/// <summary>
		/// get last error message 
		/// </summary>
		/// <returns>string of error message</returns>
		public static string getLastErrorMessage()
		{
			string ret = null;
			if (_LastError == 1)
			{
				ret = "Can't open file";
			}
			else if (_LastError == 2)
			{
				ret = "No ECG format to write";
			}
			else if (_LastError != 0)
			{
				ret = "ECG format seems incompelete";
			}
			return ret;
		}
	}
}
