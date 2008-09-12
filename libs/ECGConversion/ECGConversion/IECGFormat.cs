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
using System.IO;
using ECGConversion.ECGDemographics;
using ECGConversion.ECGDiagnostic;
using ECGConversion.ECGGlobalMeasurements;
using ECGConversion.ECGLeadMeasurements;
using ECGConversion.ECGSignals;

namespace ECGConversion
{
	public abstract class IECGFormat : IDisposable
	{
		/// <summary>
		/// Standard anonymous single byte char.
		/// </summary>
		private static byte _StandardAnonymous = (byte) '*';

		/// <summary>
		/// Configuration object of the format.
		/// </summary>
		protected ECGConfig _Config;

		/// <summary>
		/// Configuration for format
		/// </summary>
		public ECGConfig Config
		{
			get
			{
				return _Config;
			}
		}

		/// <summary>
		/// Check whether configuration works.
		/// </summary>
		/// <returns>return true if configuration is correct.</returns>
		public bool ConfigrationWorks()
		{
			return Config == null
				|| Config.ConfigurationWorks();
		}

		/// <summary>
		/// Function to read an ECG file.
		/// </summary>
		/// <param name="input">stream to read from</param>
		/// <returns>0 on success</returns>
		public int Read(Stream input)
		{
			return Read(input, 0);
		}
		/// <summary>
		/// Function to read an ECG file.
		/// </summary>
		/// <param name="file">file path to read from</param>
		/// <returns>0 on success</returns>
		public int Read(string file)
		{
			return Read(file, 0);
		}
		/// <summary>
		/// Function to read an ECG file.
		/// </summary>
		/// <param name="buffer">byte array to read from</param>
		/// <returns>0 on success</returns>
		public int Read(byte[] buffer)
		{
			return Read(buffer, 0);
		}
		/// <summary>
		/// Function to check if is this format.
		/// </summary>
		/// <param name="input">stream to check</param>
		/// <returns>true if likely to be format</returns>
		public bool CheckFormat(Stream input)
		{
			return CheckFormat(input, 0);
		}
		/// <summary>
		/// Function to check if is this format.
		/// </summary>
		/// <param name="file">file path to check</param>
		/// <returns>true if likely to be format</returns>
		public bool CheckFormat(string file)
		{
			return CheckFormat(file, 0);
		}
		/// <summary>
		/// Function to check if is this format.
		/// </summary>
		/// <param name="buffer">byte array to check</param>
		/// <returns>true if likely to be format</returns>
		public bool CheckFormat(byte[] buffer)
		{
			return CheckFormat(buffer, 0);
		}
		/// <summary>
		/// Function to anonymous the ECG file.
		/// </summary>
		public void Anonymous()
		{
			Anonymous(_StandardAnonymous);
		}
		/// <summary>
		/// Function to read an ECG file.
		/// </summary>
		/// <param name="input">stream to read from</param>
		/// <param name="offset">position to start reading</param>
		/// <returns>0 on success</returns>
		public abstract int Read(Stream input, int offset);
		/// <summary>
		/// Function to read an ECG file.
		/// </summary>
		/// <param name="file">file path to read from</param>
		/// <param name="offset">position to start reading</param>
		/// <returns>0 on success</returns>
		public abstract int Read(string file, int offset);
		/// <summary>
		/// Function to read an ECG file.
		/// </summary>
		/// <param name="buffer">byte array to read from</param>
		/// <param name="offset">position to start reading</param>
		/// <returns>0 on success</returns>
		public abstract int Read(byte[] buffer, int offset);
		/// <summary>
		/// Function to write ECG format to a file
		/// </summary>
		/// <param name="file">path to file</param>
		/// <returns>0 on success</returns>
		public abstract int Write(string file);
		/// <summary>
		/// Function to write ECG format to a stream.
		/// </summary>
		/// <param name="output">stream to write to</param>
		/// <returns>0 on success</returns>
		public abstract int Write(Stream output);
		/// <summary>
		/// Function to write ECG format to byte array.
		/// </summary>
		/// <param name="buffer">byte array to write to</param>
		/// <param name="offset">position to start writing</param>
		/// <returns>0 on success</returns>
		public abstract int Write(byte[] buffer, int offset);
		/// <summary>
		/// Function to check if is this format.
		/// </summary>
		/// <param name="input">stream to check</param>
		/// <param name="offset">position to start check</param>
		/// <returns>0 on success</returns>
		public abstract bool CheckFormat(Stream input, int offset);
		/// <summary>
		/// Function to check if is this format.
		/// </summary>
		/// <param name="file">file path to check</param>
		/// <param name="offset">position to start check</param>
		/// <returns>0 on success</returns>
		public abstract bool CheckFormat(string file, int offset);
		/// <summary>
		/// Function to check if is this format.
		/// </summary>
		/// <param name="buffer">byte array to check</param>
		/// <param name="offset">position to start check</param>
		/// <returns>0 on success</returns>
		public abstract bool CheckFormat(byte[] buffer, int offset);
		/// <summary>
		/// Function to get IDemographic class.
		/// </summary>
		/// <returns>IDemographic object</returns>
		public abstract IDemographic Demographics {get;}
		/// <summary>
		/// Function to get IDiagnostic class.
		/// </summary>
		/// <returns>IDiagnostic object</returns>
		public abstract IDiagnostic Diagnostics {get;}
		/// <summary>
		/// Function to get IGlobalMeasurement class.
		/// </summary>
		/// <returns>IGlobalMeasurement object</returns>
		public abstract IGlobalMeasurement GlobalMeasurements  {get;}
		/// <summary>
		/// Function to get ISignal class.
		/// </summary>
		/// <returns></returns>
		public abstract ISignal Signals  {get;}
		/// <summary>
		/// Function to get IMeasurement class.
		/// </summary>
		/// <returns>ILeadMeasurement object</returns>
		public virtual ILeadMeasurement LeadMeasurements
		{get{return null;}}
		/// <summary>
		/// Function to anonymous the ECG file.
		/// </summary>
		/// <param name="type">type to anonymous with</param>
		public abstract void Anonymous(byte type);
		/// <summary>
		/// Function to determine size of file.
		/// </summary>
		/// <returns>size of file</returns>
		public abstract int getFileSize();
		/// <summary>
		/// Function to check if format works.
		/// </summary>
		/// <returns>true if format works</returns>
		public abstract bool Works();
		/// <summary>
		/// Function to empty a ECG file.
		/// </summary>
		public abstract void Empty();
		#region IDisposable Members
		public virtual void Dispose()
		{
			_Config = null;
		}
		#endregion
	} 
}
