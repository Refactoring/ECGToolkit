/***************************************************************************
Copyright 2004-2005,2008, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
	/// IECGReader abstract class for Reading a certain ECG.
	/// </summary>
	public abstract class IECGReader
	{
		// Used to get error if something is wrong.
		protected int LastError = 0;
		/// <summary>
		/// Function to read ECG file.
		/// </summary>
		/// <param name="file">file path to read from</param>
		/// <returns>ECG file</returns>
		public IECGFormat Read(string file)
		{
			return Read(file, 0);
		}
		/// <summary>
		/// Function to read ECG file.
		/// </summary>
		/// <param name="input">stream to read from</param>
		/// <returns>ECG file</returns>
		public IECGFormat Read(Stream input)
		{
			return Read(input, 0);
		}
		/// <summary>
		/// Function to read ECG file.
		/// </summary>
		/// <param name="buffer">byte array to read from</param>
		/// <returns>ECG file</returns>
		public IECGFormat Read(byte[] buffer)
		{
			return Read(buffer, 0);
		}
		/// <summary>
		/// Function to read ECG file.
		/// </summary>
		/// <param name="file">file path to read from</param>
		/// <param name="offset">position to start reading</param>
		/// <returns>ECG file</returns>
		public IECGFormat Read(string file, int offset)
		{
			return Read(file, offset, null);
		}
		/// <summary>
		/// Function to read ECG file.
		/// </summary>
		/// <param name="file">file path to read from</param>
		/// <param name="offset">position to start reading</param>
		/// <param name="cfg">configuration for reading type</param>
		/// <returns>ECG file</returns>
		public abstract IECGFormat Read(string file, int offset, ECGConfig cfg);
		/// <summary>
		/// Function to read ECG file.
		/// </summary>
		/// <param name="file">file path to read from</param>
		/// <param name="offset">position to start reading</param>
		/// <returns>ECG file</returns>
		public IECGFormat Read(Stream input, int offset)
		{
			return Read(input, offset, null);
		}
		/// <summary>
		/// Function to read ECG file.
		/// </summary>
		/// <param name="input">stream to read from</param>
		/// <param name="offset">position to start reading</param>
		/// <param name="cfg">configuration for reading type</param>
		/// <returns>ECG file</returns>
		public abstract IECGFormat Read(Stream input, int offset, ECGConfig cfg);
		/// <summary>
		/// Function to read ECG file.
		/// </summary>
		/// <param name="file">file path to read from</param>
		/// <param name="offset">position to start reading</param>
		/// <returns>ECG file</returns>
		public IECGFormat Read(byte[] buffer, int offset)
		{
			return Read(buffer, offset, null);
		}
		/// <summary>
		/// Function to read ECG file.
		/// </summary>
		/// <param name="buffer">buffer to read from</param>
		/// <param name="offset">position to start reading</param>
		/// <param name="cfg">configuration for reading type</param>
		/// <returns>ECG file</returns>
		public abstract IECGFormat Read(byte[] buffer, int offset, ECGConfig cfg);
		/// <summary>
		/// Function to get Error code.
		/// </summary>
		/// <returns>error code: 0 is success</returns>
		public int getError()
		{
			return LastError;
		}
		/// <summary>
		/// Function to get Error Message.
		/// </summary>
		/// <returns>error message</returns>
		public abstract string getErrorMessage();
	}
}
