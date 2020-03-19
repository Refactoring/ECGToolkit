/***************************************************************************
Copyright 2019, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands
Copyright 2013, van Ettinger Information Technology, Lopik, The Netherlands
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
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;

using ECGConversion.ECGDiagnostic;
using ECGConversion.ECGDemographics;
using ECGConversion.ECGGlobalMeasurements;
using ECGConversion.ECGSignals;
using ECGConversion.ECGLeadMeasurements;

namespace ECGConversion
{
	/// <summary>
	/// Class containing all converter functions for ECG files.
	/// </summary>
	public class ECGConverter
	{
		public const string SoftwareName = "ECGConversion";
		public static bool LoadAvailablePlugins = true;
		private static int _DemographicsError = 0;
		private static int _DiagnosticError = 0;
		private static int _GlobalMeasurementsError = 0;
		private static int _SignalError = 0;
		private static int _LeadMeasurementsError = 0;

		private static ECGConverter _Instance = null;
		private static Mutex _Mutex = new Mutex();
		private SortedList _SupportedFormats;
		private SortedList _SupportedECGMS;

		public delegate void NewPluginDelegate(ECGConverter instance);
		public event NewPluginDelegate OnNewPlugin;
#if WINCE
		private int _Loading = 0;
#else
		private volatile int _Loading = 0;
#endif

		/// <summary>
		/// Get the one instance of the converter object.
		/// </summary>
		/// <returns></returns>
		public static ECGConverter Instance
		{
			get
			{
				try
				{
					_Mutex.WaitOne();

					if (_Instance == null)
					{
#if !WINCE
						CheckVersion.CheckForNewVersion();
#endif

						_Instance = new ECGConverter();

						if (LoadAvailablePlugins)
						{
							LoadAvailablePlugins = false;

#if WINCE
							AddPlugins(_Instance, ".");
#else
							AddPlugins(
								_Instance,
								Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
#endif
						}
					}
				}
				finally
				{
					_Mutex.ReleaseMutex();
				}

				return _Instance;
			}
		}

		/// <summary>
		/// constructor for ECGConverter
		/// </summary>
		private ECGConverter()
		{
			_SupportedFormats = new SortedList();
			_SupportedECGMS = new SortedList();

			_SupportedFormats.Add("SCP-ECG", new ECGPlugin("SCP-ECG", "scp", typeof(SCP.SCPFormat), typeof(SCPReader), true, "ToSCP"));
			_SupportedFormats.Add("RAW", new ECGPlugin("RAW", "raw", typeof(RawFormat.RawECGFormat), typeof(RawECGReader), false));
			_SupportedFormats.Add("CSV", new ECGPlugin("CSV", "csv", typeof(CSV.CSVFormat), null, false));
		}

		/// <summary>
		/// destructor for ECGConverter
		/// </summary>
		~ECGConverter()
		{}

		/// <summary>
		/// Function to get number of supported formats.
		/// </summary>
		/// <returns>nr of supported formats</returns>
		public int getNrSupportedFormats()
		{
			lock (_SupportedFormats)
			{
				return _SupportedFormats.Count;
			}
		}	

		/// <summary>
		/// Function to get a list of all supported formats.
		/// </summary>
		/// <returns>array containing the names of all the supported formats.</returns>
		public string[] getSupportedFormatsList()
		{
			string[] ret = null;
			
			lock (_SupportedFormats)
			{
				ret = new string[_SupportedFormats.Count];

				for (int i=0;i < _SupportedFormats.Count;i++)
					ret[i] = ((ECGPlugin) _SupportedFormats.GetByIndex(i)).Name;
			}

			return ret;
		}

		/// <summary>
		/// Function to get number of supported ECG Management Systems.
		/// </summary>
		/// <returns>nr of supported formats</returns>
		public int getNrSupportedECGManagementSystems()
		{
			lock (_SupportedECGMS)
			{
				return _SupportedECGMS.Count;
			}
		}

		/// <summary>
		/// Function to get a list of all supported ECG Management Systems.
		/// </summary>
		/// <returns>array containing the names of all the supported formats.</returns>
		public string[] getSupportedManagementSystemsList()
		{
			string[] ret = null;

			lock (_SupportedECGMS)
			{
				ret = new string[_SupportedECGMS.Count];

				for (int i=0;i < _SupportedECGMS.Count;i++)
					ret[i] = ((ECGManagementSystem.IECGManagementSystem) _SupportedECGMS.GetByIndex(i)).Name;
			}

			return ret;
		}

		/// <summary>
		/// Function to convert ECG file.
		/// </summary>
		/// <param name="src">source format</param>
		/// <param name="toType">convert to</param>
		/// <param name="dst">returns destination format</param>
		/// <returns>
		/// 0 on succes
		/// 1 on unsupported format (probably missing plugin)
		/// </returns>
		public int Convert(IECGFormat src, string toType, out IECGFormat dst)
		{
			return Convert(src, toType, null, out dst);
		}

		/// <summary>
		/// Function to convert ECG file.
		/// </summary>
		/// <param name="src">source format</param>
		/// <param name="toType">convert to</param>
		/// <param name="cfg">configuration to set format to</param>
		/// <param name="dst">returns destination format</param>
		/// <returns>
		/// 0 on succes
		/// 1 on unsupported or bad format (probably missing plugin)
		/// 2 on bad configuration
		/// </returns>
		public int Convert(IECGFormat src, string toType, ECGConfig cfg, out IECGFormat dst)
		{
			dst = null;

			if (toType != null)
			{
				int index = -1;
				
				lock (_SupportedFormats)
				{
					index = _SupportedFormats.IndexOfKey(toType.ToUpper());
				}

				return Convert(src, index, cfg, out dst);
			}

			return 1;
		}

		/// <summary>
		/// Function to check whether format is supported.
		/// </summary>
		/// <param name="type">type of format</param>
		/// <returns>true if supported</returns>
		public bool hasFormatSupport(string type)
		{
			lock (_SupportedFormats)
			{
				return type != null && _SupportedFormats.ContainsKey(type.ToUpper());
			}
		}

		/// <summary>
		/// Function that waits for all loading threads to stop before providing the has Format Support response.
		/// </summary>
		/// <param name="type">type of format</param>
		/// <returns>true if supported</returns>
		public bool waitForFormatSupport(string type)
		{
			while (_Loading > 0)
			{
				if (hasFormatSupport(type))
					return true;

				Thread.Sleep(250);
			}

			return hasFormatSupport(type);
		}

		/// <summary>
		/// Function to check whether ECG Management Systems is supported.
		/// </summary>
		/// <param name="type">type of format</param>
		/// <returns>true if supported</returns>
		public bool hasECGManagementSystemSupport(string type)
		{
			lock (_SupportedECGMS)
			{
				return type != null && _SupportedECGMS.ContainsKey(type.ToUpper());
			}
		}

		/// <summary>
		/// Function that waits for all loading threads to stop before providing the has ECG Management Systems Support response.
		/// </summary>
		/// <param name="type">type of format</param>
		/// <returns>true if supported</returns>
		public bool waitForECGManagementSystemSupport(string type)
		{
			while (_Loading > 0)
			{
				if (hasECGManagementSystemSupport(type))
					return true;

				Thread.Sleep(250);
			}

			return hasECGManagementSystemSupport(type);
		}

		/// <summary>
		/// Waits for the loading of plug-ins to be ready.
		/// </summary>
		public void waitForLoadingAllPlugins()
		{
			while (_Loading > 0)
				Thread.Sleep(250);
		}

		/// <summary>
		/// retrieve whether all plugins are loaded
		/// </summary>
		/// <returns>true if is loaded</returns>
		public bool allPluginsLoaded()
		{
			return _Loading == 0;
		}

		/// <summary>
		/// Function to check whether ECG Management Systems is supported.
		/// </summary>
		/// <param name="type">type of format</param>
		/// <returns>the format</returns>
		public bool hasECGManagementSystemSaveSupport(string type)
		{
			int index = -1;

			lock (_SupportedECGMS)
			{
				index = _SupportedECGMS.IndexOfKey(type.ToUpper());
			}

			return type != null && hasECGManagementSystemSaveSupport(index);
		}

		/// <summary>
		/// Function to get a formats.
		/// </summary>
		/// <param name="type">type of format</param>
		/// <returns>the format</returns>
		public IECGFormat getFormat(string type)
		{
			if (type != null)
			{
				int index = -1;
				
				lock (_SupportedFormats)
				{
					index = _SupportedFormats.IndexOfKey(type.ToUpper());
				}

				return getFormat(index);
			}

			return null;
		}

		/// <summary>
		/// Function to get the extension of a format.
		/// </summary>
		/// <param name="type">type of format</param>
		/// <returns>extension of format</returns>
		public string getExtension(string type)
		{
			if (type != null)
			{
				int index = -1;
				
				lock (_SupportedFormats)
				{
					index = _SupportedFormats.IndexOfKey(type.ToUpper());
				}

				return getExtension(index);
			}

			return null;
		}

		/// <summary>
		/// Function to get a type.
		/// </summary>
		/// <param name="type">type of format</param>
		/// <returns>type</returns>
		public Type getType(string type)
		{
			if (type != null)
			{
				int index = -1;
				
				lock (_SupportedFormats)
				{
					index = _SupportedFormats.IndexOfKey(type.ToUpper());
				}

				return getType(index);
			}

			return null;
		}

		/// <summary>
		/// Function to get a configuration.
		/// </summary>
		/// <param name="type">type of format</param>
		/// <returns>configuration</returns>
		public ECGConfig getConfig(string type)
		{
			if (type != null)
			{
				int index = -1;
				
				lock (_SupportedFormats)
				{
					index = _SupportedFormats.IndexOfKey(type.ToUpper());
				}

				return getConfig(index);
			}

			return null;
		}

		/// <summary>
		/// Function to get a reader.
		/// </summary>
		/// <param name="type">type of reader</param>
		/// <returns>the reader</returns>
		public IECGReader getReader(string type)
		{
			if (type != null)
			{
				int index = -1;
				
				lock (_SupportedFormats)
				{
					index = _SupportedFormats.IndexOfKey(type.ToUpper());
				}

				return getReader(index);
			}

			return null;
		}

		/// <summary>
		/// Function to get an ECG Management System.
		/// </summary>
		/// <param name="type">type of ECG Management System</param>
		/// <returns>ECG Management System</returns>
		public ECGManagementSystem.IECGManagementSystem getECGManagementSystem(string type)
		{
			if (type != null)
			{
				int index = -1;
				
				lock (_SupportedECGMS)
				{
					index = _SupportedECGMS.IndexOfKey(type.ToUpper());
				}

				return getECGManagementSystem(index);
			}

			return null;
		}

		/// <summary>
		/// Convert by supported list.
		/// </summary>
		/// <param name="src">source format</param>
		/// <param name="i">position in list</param>
		/// <param name="cfg">configuration to set format to</param>
		/// <param name="dst">returns destination format</param>
		/// <returns>0 if successful</returns>
		public int Convert(IECGFormat src, int i, out IECGFormat dst)
		{
			return Convert(src, i, null, out dst);
		}

		/// <summary>
		/// Convert by supported list.
		/// </summary>
		/// <param name="src">source format</param>
		/// <param name="i">position in list</param>
		/// <param name="cfg">configuration to set format to</param>
		/// <param name="dst">returns destination format</param>
		/// <returns>0 if successful</returns>
		public int Convert(IECGFormat src, int i, ECGConfig cfg, out IECGFormat dst)
		{
			ECGPlugin plugin = null;

			lock (_SupportedFormats)
			{
				dst = null;

				if ((i < 0)
				||	(i >= _SupportedFormats.Count))
					return 1;

				plugin = (ECGPlugin)_SupportedFormats.GetByIndex(i);
			}

			return plugin.Convert(src, cfg, out dst);
		}

		/// <summary>
		/// Get format from supported list.
		/// </summary>
		/// <param name="i">position in list</param>
		/// <returns>format</returns>
		public IECGFormat getFormat(int i)
		{
			ECGPlugin plugin = null;

			lock (_SupportedFormats)
			{
				if ((i < 0)
				||	(i >= _SupportedFormats.Count))
					return null;

				plugin = (ECGPlugin)_SupportedFormats.GetByIndex(i);
			}

			return plugin.getFormat();
		}

		/// <summary>
		/// Function to get the extension of a format.
		/// </summary>
		/// <param name="i">position in list</param>
		/// <returns>extension of format</returns>
		public string getExtension(int i)
		{
			lock (_SupportedFormats)
			{
				if ((i < 0)
				||	(i >= _SupportedFormats.Count))
					return null;

				return ((ECGPlugin)_SupportedFormats.GetByIndex(i)).Extension;
			}
		}

		/// <summary>
		/// Get format from supported list.
		/// </summary>
		/// <param name="i">position in list</param>
		/// <returns>type</returns>
		public Type getType(int i)
		{
			lock (_SupportedFormats)
			{
				if ((i < 0)
				||	(i >= _SupportedFormats.Count))
					return null;

				return ((ECGPlugin)_SupportedFormats.GetByIndex(i)).getType();
			}
		}

		/// <summary>
		/// Get configuration from supported list.
		/// </summary>
		/// <param name="i">position in list</param>
		/// <returns>configuration</returns>
		public ECGConfig getConfig(int i)
		{
			ECGPlugin plugin = null;

			lock (_SupportedFormats)
			{
				if ((i < 0)
				||	(i >= _SupportedFormats.Count))
					return null;

				plugin = (ECGPlugin)_SupportedFormats.GetByIndex(i);
			}

			IECGFormat format = plugin.getFormat();

			if (format == null)
				return null;

			return format.Config;
		}

		/// <summary>
		/// Get reader from supported list.
		/// </summary>
		/// <param name="i">position in list</param>
		/// <returns>reader</returns>
		public IECGReader getReader(int i)
		{
			ECGPlugin plugin = null;

			lock (_SupportedFormats)
			{
				if ((i < 0)
				||	(i >= _SupportedFormats.Count))
					return null;

				plugin = (ECGPlugin)_SupportedFormats.GetByIndex(i);
			}

			return plugin.getReader();
		}

		/// <summary>
		/// Get ECG Management System from supported list.
		/// </summary>
		/// <param name="i">position in list</param>
		/// <returns>ECG Management System</returns>
		public ECGManagementSystem.IECGManagementSystem getECGManagementSystem(int i)
		{
			lock (_SupportedECGMS)
			{
				if ((i < 0)
				||	(i >= _SupportedECGMS.Count))
					return null;

				return ((ECGManagementSystem.IECGManagementSystem)_SupportedECGMS.GetByIndex(i));
			}
		}

		/// <summary>
		/// Function to check whether plugin entry has got support for unkownreader.
		/// </summary>
		/// <param name="i">position in list</param>
		/// <returns>true if supported</returns>
		public bool hasUnknownReaderSupport(int i)
		{
			lock (_SupportedFormats)
			{
				if ((i < 0)
				||	(i >= _SupportedFormats.Count))
					return false;

				return ((ECGPlugin)_SupportedFormats.GetByIndex(i)).hasUnknownReaderSupport;
			}
		}

		/// <summary>
		/// Function to check whether ECG Management Systems is supported.
		/// </summary>
		/// <param name="i">type of format</param>
		/// <returns>the format</returns>
		public bool hasECGManagementSystemSaveSupport(int i)
		{
			lock (_SupportedECGMS)
			{
				if ((i < 0)
				||	(i >= _SupportedECGMS.Count))
					return false;
			
				return ((ECGManagementSystem.IECGManagementSystem) _SupportedECGMS.GetByIndex(i)).CanSave();
			}
		}

		/// <summary>
		/// Function to get the extra offset.
		/// </summary>
		/// <param name="i">position in list</param>
		/// <returns>extra offset</returns>
		public int getExtraOffset(int i)
		{
			lock (_SupportedFormats)
			{
				if ((i < 0)
				||	(i >= _SupportedFormats.Count))
					return 0;

				return ((ECGPlugin)_SupportedFormats.GetByIndex(i)).ExtraOffset;
			}
		}
		
		/// <summary>
		/// Function that will add plugin to supported list.
		/// </summary>
		/// <param name="plugin">the plugin</param>
		public void AddPlugin(ECGPlugin plugin)
		{
			if ((plugin != null)
			&&	(plugin.Name != null))
			{
				lock (_SupportedFormats)
				{
					string temp = plugin.Name.ToUpper();

					int index = _SupportedFormats.IndexOfKey(temp);

					if (index >= 0)
						_SupportedFormats.SetByIndex(index, plugin);
					else
						_SupportedFormats.Add(temp, plugin);
				}
			}	
		}

		/// <summary>
		/// Function that will add plugin to supported list.
		/// </summary>
		/// <param name="plugin">the plugin</param>
		public void AddPlugin(ECGManagementSystem.IECGManagementSystem manSys)
		{
			if ((manSys != null)
			&&	(manSys.Name != null))
			{
				lock (_SupportedECGMS)
				{
					string temp = manSys.Name.ToUpper();

					int index = _SupportedECGMS.IndexOfKey(temp);

					if (index >= 0)
						_SupportedECGMS.SetByIndex(index, manSys);
					else
						_SupportedECGMS.Add(temp, manSys);
				}
			}	
		}

        /// <summary>
        /// Function to add all plugins in a directory.
        /// </summary>
        /// <param name="dir">directory to detect plugins from</param>
        /// <returns>returns 0 if successful</returns>
        public static int AddPlugins(string dir)
        {
            return AddPlugins(Instance, dir);
        }

        /// <summary>
        /// Function to add all plugins in a directory.
        /// </summary>
        /// <param name="converter">instance of the converter singleton</param>
        /// <param name="dir">directory to detect plugins from</param>
        /// <returns></returns>
        public static int AddPlugins(ECGConverter converter, string dir)
        {
            if (converter == null)
                return 1;

            try
            {
				converter._Loading++;

				System.Threading.ThreadPool.QueueUserWorkItem(
					new WaitCallback(LoadPlugins),
					new object[]{converter, dir});
            }
            catch (Exception)
            {
                return 3;
            }

            return 0;
        }

		/// <summary>
		/// Load plugins Async
		/// </summary>
		private static void LoadPlugins(object obj)
		{
			if ((obj != null)
			&&	(obj is object[]))
			{
				object[] temp = (object[])obj;

				if ((temp.Length == 2)
				&&	(temp[0] != null)
				&&	(temp[0] is ECGConverter))
				{
					ECGConverter instance = (ECGConverter) temp[0];
					
					try
					{
						string dir = (string)temp[1];

						if ((dir == null)
						||	(dir.Length == 0))
							return;

						string[] asPlugin = Directory.GetFiles(dir, "*.dll");

#if WINCE
						string currentDll = Assembly.GetExecutingAssembly().FullName.Split(',')[0] + ".dll";
#else
						string currentDll = System.IO.Path.GetFileName(Assembly.GetExecutingAssembly().Location);
#endif

						foreach (string sPlugin in asPlugin)
						{
							if (String.Compare(System.IO.Path.GetFileName(sPlugin), currentDll, true) != 0)
							{
								LoadPlugin(new object[]{ instance, sPlugin, true});
							}
						}
					}
					finally
					{
						if (instance.OnNewPlugin != null)
							instance.OnNewPlugin(instance);

						instance._Loading--;
					}
				}
			}
		}


		/// <summary>
		/// Function that will add plugins from a certain dll to the supported list.
		/// </summary>
		/// <param name="dllfile">path to dll</param>
		/// <returns>returns 0 if successful</returns>
		public static int AddPlugin(string dllfile)
		{
			return AddPlugin(Instance, dllfile);
		}

		/// <summary>
		/// Function that will add plugins from a certain dll to the supported list.
		/// </summary>
		/// <param name="converter">the object to add the plugin to</param>
		/// <param name="dllfile">path to dll</param>
		/// <returns>returns 0 if successful</returns>
		private static int AddPlugin(ECGConverter converter, string dllfile)
		{
			if (converter == null)
				return 1;

			try
			{
				converter._Loading++;

				System.Threading.ThreadPool.QueueUserWorkItem(
					new WaitCallback(LoadPlugin),
					new object[]{converter, dllfile});
			}
			catch (Exception)
			{
				return 2;
			}

			return 0;
		}

		/// <summary>
		/// Load plugin Async
		/// </summary>
		private static void LoadPlugin(object obj)
		{
			if ((obj != null)
			&&	(obj is object[]))
			{
				object[] temp = (object[])obj;

				if (((temp.Length == 2)
				||	 (temp.Length == 3))
				&&	(temp[0] != null)
				&&	(temp[0] is ECGConverter))
				{
					ECGConverter instance = (ECGConverter) temp[0];

					try
					{
						string dllfile = (string) temp[1];

						Assembly assembly = Assembly.LoadFrom(dllfile);
						Type type = assembly.GetType("ECGConversion.ECGLoad");

						if (type == null)
							return;

						MethodInfo methodInfo = type.GetMethod("LoadPlugin");

						if (methodInfo != null)
						{
							foreach (ECGPlugin plugin in (Array) methodInfo.Invoke(null, null))
								if (plugin != null)
									instance.AddPlugin(plugin);
						}

						methodInfo = type.GetMethod("LoadECGMS");

						if (methodInfo != null)
						{
							foreach (ECGManagementSystem.IECGManagementSystem ecgms in (Array) methodInfo.Invoke(null, null))
								if (ecgms != null)
									instance.AddPlugin(ecgms);
						}
					}
					catch {}
					finally
					{
						if (temp.Length == 2)
						{
							if (instance.OnNewPlugin != null)
								instance.OnNewPlugin(instance);

							instance._Loading--;
						}
					}
				}
			}
		}

		/// <summary>
		/// Function to write an ECG to Txt file that can be read with Excel.
		/// </summary>
		/// <param name="src">an ECG file to convert</param>
		/// <param name="output">stream to write Txt in.</param>
		/// <param name="hSeperator">Horizontal seperator to use</param>
		/// <returns>0 on success</returns>
		public static int ToExcelTxt(IECGFormat src, TextWriter output, char hSeperator)
		{
            return ToExcelTxt(src, output, hSeperator, false);
        }

        /// <summary>
        /// Function to write an ECG to Txt file that can be read with Excel.
        /// </summary>
        /// <param name="src">an ECG file to convert</param>
        /// <param name="output">stream to write Txt in.</param>
        /// <param name="hSeperator">Horizontal seperator to use</param>
        /// <param name="useBufferedStream">true if entire buffered stream must be writen</param>
        /// <returns>0 on success</returns>
        public static int ToExcelTxt(IECGFormat src, TextWriter output, char hSeperator, bool useBufferedStream)
        {
            if ((src != null)
            && (output != null))
            {
                ISignal sigread = src.Signals;
                if (sigread != null)
                {
                    ECGGlobalMeasurements.GlobalMeasurements mes = null;
                    if (src.GlobalMeasurements != null)
                        src.GlobalMeasurements.getGlobalMeasurements(out mes);

                    BufferedSignals bs = null;
                    int rhythmPos = 0,
                        rhythmEnd = 0,
                        stepSize = 0;

                    Signals data;
                    sigread.getSignals(out data);


                    if (useBufferedStream
                    && (data != null)
                    && (data.AsBufferedSignals != null))
                    {
                        bs = data.AsBufferedSignals;
                        rhythmPos = bs.RealRhythmStart;
                        rhythmEnd = bs.RealRhythmEnd;
                        stepSize = 60 * bs.RealRhythmSamplesPerSecond;
                    }


                    if ((data != null)
                    && (data.NrLeads != 0))
                    {
                        bool bFirst = true;

                        do
                        {
                            int sampleOffset = rhythmPos;

                            if (bs != null)
                            {
                                if (!bs.LoadSignal(rhythmPos, rhythmPos + stepSize))
                                    return 8;

                                rhythmPos += stepSize;
                            }

                            // Determine minimum start and maximum end.
                            int minstart = int.MaxValue;
                            int maxend = int.MinValue;

                            data.CalculateStartAndEnd(out minstart, out maxend);

                            if (bFirst)
                            {
                                output.Write("samplenr");
                                for (int lead = 0; lead < data.NrLeads; lead++)
                                {
                                    if (data[lead] != null)
                                    {
                                        output.Write("{0}{1}", hSeperator, data[lead].Type);
                                    }
                                    else
                                    {
                                        output.Write("{0}?", hSeperator);
                                    }
                                }
                                if ((mes != null)
                                && (mes.measurment != null))
                                    output.Write("{0}measurement", hSeperator);

                                output.WriteLine();
                                bFirst = false;
                            }

                            if (minstart == sampleOffset)
                                sampleOffset = 0;
                            else if ((minstart > 0)
                            && (minstart < sampleOffset))
                                sampleOffset -= minstart;

                            for (int sample = minstart; sample < maxend; sample++)
                            {
                                output.Write("{0}", sample + sampleOffset);
                                for (int lead = 0; lead < data.NrLeads; lead++)
                                {
                                    if ((data[lead] != null)
                                    && (data[lead].Rhythm != null)
                                    && (data[lead].Rhythm.Length >= (data[lead].RhythmEnd - data[lead].RhythmEnd))
                                    && (sample >= data[lead].RhythmStart)
                                    && (sample < data[lead].RhythmEnd))
                                    {
                                        output.Write("{0}{1}", hSeperator, (data[lead].Rhythm[sample - data[lead].RhythmStart] * data.RhythmAVM).ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat));
                                    }
                                    else
                                    {
                                        output.Write("{0}0", hSeperator);
                                    }
                                }

                                if ((mes != null)
                                && (mes.measurment != null))
                                {
                                    bool bWrite = false;

                                    for (int i = (data.MedianLength == 0 ? 0 : 1); i < mes.measurment.Length; i++)
                                    {
                                        bWrite = false;

                                        if ((mes.measurment[i].Ponset != GlobalMeasurement.NoValue)
                                            && (mes.measurment[i].Ponset == (sample + sampleOffset)))
                                        {
                                            output.Write("{0}1000{0}P+{1}", hSeperator, i);
                                            break;
                                        }
                                        else if ((mes.measurment[i].Poffset != GlobalMeasurement.NoValue)
                                            && (mes.measurment[i].Poffset == (sample + sampleOffset)))
                                        {
                                            output.Write("{0}-1000{0}P-{1}", hSeperator, i);
                                            break;
                                        }
                                        else if ((mes.measurment[i].QRSonset != GlobalMeasurement.NoValue)
                                            && (mes.measurment[i].QRSonset == (sample + sampleOffset)))
                                        {
                                            output.Write("{0}1500{0}QRS+{1}", hSeperator, i);
                                            break;
                                        }
                                        else if ((mes.measurment[i].QRSoffset != GlobalMeasurement.NoValue)
                                            && (mes.measurment[i].QRSoffset == (sample + sampleOffset)))
                                        {
                                            output.Write("{0}-1500{0}QRS-{1}", hSeperator, i);
                                            break;
                                        }
                                        else if ((mes.measurment[i].Toffset != GlobalMeasurement.NoValue)
                                            && (mes.measurment[i].Toffset == (sample + sampleOffset)))
                                        {
                                            output.Write("{0}-1250{0}T-{1}", hSeperator, i);
                                            break;
                                        }

                                        bWrite = true;
                                    }

                                    if (bWrite)
                                    {
                                        output.Write("{0}0", hSeperator);
                                    }
                                }

                                output.WriteLine();
                            }
                        } while (rhythmPos < rhythmEnd);

                        return 0;
                    }
                    return 4;
                }
                return 2;
            }
            return 1;
        }
		/// <summary>
		/// Function to write an ECG to Txt file that can be read with MatLab.
		/// </summary>   
		/// <param name="src">an ECG file to convert</param>
		/// <param name="output">stream to write Txt in.</param>
		/// <returns>0 on success</returns>
		public static int ToMatlabTxt(IECGFormat src, TextWriter output)
		{
			if ((src != null)
			&&	(output != null))
			{
				ISignal sigread = src.Signals;
				if (sigread != null)
				{
					Signals data;
					sigread.getSignals(out data);

					if ((data != null)
					&&	(data.NrLeads != 0)
					&&	(data.RhythmSamplesPerSecond != 0))
					{
						LeadType[] neededLeads = {LeadType.I, LeadType.II, LeadType.III, LeadType.aVR, LeadType.aVL, LeadType.aVF, LeadType.V1, LeadType.V2, LeadType.V3, LeadType.V4, LeadType.V5, LeadType.V6};
						output.Write("ECGStandardLeads = [");
						for (int find=0;find < neededLeads.Length;find++)
						{
							int p = 0;
							for (;p < data.NrLeads;p++)
							{
								if ((data[p] != null)
								&&	(data[p].Type == neededLeads[find])
								&&	(data[p].Rhythm != null))
								{
									break;
								}
							}
							if (p == data.NrLeads)
							{
								p = -1;
							}
							output.Write("{0} ", p+1);
						}
						output.WriteLine("];");
						output.WriteLine();

						if (data.MedianSamplesPerSecond != 0)
						{
							if (data.QRSZone != null)
							{
								output.Write("ECGSubtraction = [");
								int nr = 0;
								for (int loper=0;loper < data.QRSZone.Length;loper++)
								{
									if ((data.QRSZone[loper] != null)
									&&	(data.QRSZone[loper].Type == 0))
									{
										nr++;
										output.Write("{0} {1} {2} {3};",
										    data.QRSZone[loper].Start + 1,
										    data.MedianFiducialPoint + (data.QRSZone[loper].Start - data.QRSZone[loper].Fiducial) + 1,
										    data.QRSZone[loper].End,
										    data.MedianFiducialPoint + (data.QRSZone[loper].End - data.QRSZone[loper].Fiducial));
									}
								}
								output.WriteLine("];");
								output.WriteLine();
								output.WriteLine("ECGSubtractionNr = {0};", nr);
								output.WriteLine();
							}

							output.WriteLine("ECGMedianLength = {0};", (data.MedianLength * data.MedianSamplesPerSecond) / 1000);
							output.WriteLine();
						
					    	output.Write("ECGMedian = [");
							for (int sample=0;sample < (data.MedianLength * data.MedianSamplesPerSecond) / 1000;sample++)
							{
								for (int lead=0;lead < data.NrLeads;lead++)
								{
									if ((data[lead] != null)
									&&	(data[lead].Median != null)
									&&	(sample < data[lead].Median.Length))
									{
										output.Write(" {0}", data[lead].Median[sample] * data.MedianAVM);
									}
									else
									{
										output.Write(" 0");
									}
								}
								output.Write(";");
							}
							output.WriteLine("];");
							output.WriteLine();
							
							if (data.MedianSamplesPerSecond != data.RhythmSamplesPerSecond)
							{
								for (int lead=0;lead < data.NrLeads;lead++)
								{
									ECGTool.ResampleLead(data[lead].Rhythm, data.RhythmSamplesPerSecond, data.MedianSamplesPerSecond, out data[lead].Rhythm);
									data[lead].RhythmStart = (data[lead].RhythmStart * data.MedianSamplesPerSecond) / data.RhythmSamplesPerSecond;
									data[lead].RhythmEnd = (data[lead].RhythmEnd * data.MedianSamplesPerSecond) / data.RhythmSamplesPerSecond;
								}
							}
						}

						// Determine minimum start and maximum end.
						int minstart = int.MaxValue;
						int maxend = int.MinValue;
						for (int lead=0;lead < data.NrLeads;lead++)
						{
							if (data[lead] != null)
							{
								minstart = Math.Min(minstart, data[lead].RhythmStart);
								maxend = Math.Max(maxend, data[lead].RhythmEnd);
							}
						}

						output.WriteLine("ECGNrLeads = {0};", data.NrLeads);
						output.WriteLine();
						output.WriteLine("ECGNrSamples = {0};", maxend - minstart);
						output.WriteLine();

						output.Write("ECGRhythm = [");
						for (int sample=minstart;sample < maxend;sample++)
						{
							for (int lead=0;lead < data.NrLeads;lead++)
							{
								if ((data[lead] != null)
								&&	(data[lead].Rhythm != null)
								&&	(data[lead].Rhythm.Length >= (data[lead].RhythmEnd - data[lead].RhythmEnd))
								&&	(sample >= data[lead].RhythmStart)
								&&	(sample < data[lead].RhythmEnd))
								{
									output.Write(" {0}", data[lead].Rhythm[sample - data[lead].RhythmStart] * data.RhythmAVM);
								}
								else
								{
									output.Write(" 0");
								}
							}
							output.Write(";");
						}
						output.WriteLine("];");

						return 0;
					}
					return 4;
				}
				return 2;
			}
			return 1;
		}
		/// <summary>
		/// Enumeration to determine the part of converting that must be done.
		/// </summary>
		public enum ConvertWork
		{
			DoDemographics			= 0x01,
			DoSignal				= 0x02,
			DoDiagnostic			= 0x04,
			DoGlobalMeasurements	= 0x08,
			DoLeadMeasurements		= 0x10,
			DoAll					= 0x1f
		}
		/// <summary>
		/// Function to copy one ECG file to an other. (uses a couple of interfaces)
		/// </summary>
		/// <param name="src">source ECG file</param>
		/// <param name="dst">destination ECG file</param>
		/// <returns>0 on success</returns>
		public static int Convert(IECGFormat src, IECGFormat dst)
		{
			return Convert(src, dst, ConvertWork.DoAll);
		}
		/// <summary>
		/// Function to copy one ECG file to an other. (uses a couple of interfaces)
		/// </summary>
		/// <param name="src">source ECG file</param>
		/// <param name="dst">destination ECG file</param>
		/// <returns>0 on success</returns>
		public static int Convert(IECGFormat src, IECGFormat dst, ConvertWork cw)
		{
			_DemographicsError = 0;
			_DiagnosticError = 0;
			_GlobalMeasurementsError = 0;
			_SignalError = 0;
			_LeadMeasurementsError = 0;

			if (src == null)
			{
				return 1;
			}
			if (dst == null)
			{
				return 2;
			}

			if ((cw & ConvertWork.DoDemographics) != 0)
			{
				IDemographic demsrc = src.Demographics;
				IDemographic demdst = dst.Demographics;
				if ((demsrc != null)
				&&  (demdst != null))
				{
					demdst.Init();
					_DemographicsError = DemographicCopy(demsrc, demdst);
				}
			}

			if ((cw & ConvertWork.DoSignal) != 0)
			{
				ISignal sigsrc = src.Signals;
				ISignal sigdst = dst.Signals;
				if ((sigsrc != null)
				&&	(sigdst != null))
				{
					_SignalError = SignalCopy(sigsrc, sigdst);
				}
			}

			if ((cw & ConvertWork.DoDiagnostic) != 0)
			{
				IDiagnostic diasrc = src.Diagnostics;
				IDiagnostic diadst = dst.Diagnostics;
				if ((diasrc != null)
				&&  (diadst != null))
				{
					_DiagnosticError = DiagnosticCopy(diasrc, diadst);
				}
			}

			if ((cw & ConvertWork.DoGlobalMeasurements) != 0)
			{
				IGlobalMeasurement messrc = src.GlobalMeasurements;
				IGlobalMeasurement mesdst = dst.GlobalMeasurements;
				if ((messrc != null)
				&&  (mesdst != null))
				{
					_GlobalMeasurementsError = GlobalMeasurementCopy(messrc, mesdst);
				}
			}

			if ((cw & ConvertWork.DoLeadMeasurements) != 0)
			{
				ILeadMeasurement leadsrc = src.LeadMeasurements;
				ILeadMeasurement leaddst = dst.LeadMeasurements;
				if ((leadsrc != null)
				&&	(leaddst != null))
				{
					_LeadMeasurementsError = LeadMeasurementCopy(leadsrc, leaddst);
				}
			}

			return 0;
		}
		/// <summary>
		/// Function to copy demographics from one to another.
		/// </summary>
		/// <param name="src">source</param>
		/// <param name="dst">destination</param>
		/// <returns>complex</returns>
		public static int DemographicCopy(IDemographic src, IDemographic dst)
		{
			// Check for correct input.
			if ((src == null)
			||  (dst == null))
			{
				return 1;
			}

			// Do copy of all Demographics.
			int err = 0;

			dst.LastName = src.LastName;
			if (dst.LastName == null)
				err |= (0x1 << 1);

			dst.FirstName = src.FirstName;
			if (dst.FirstName == null)
				err |= (0x1 << 2);

			dst.PatientID = src.PatientID;

			dst.SecondLastName = src.SecondLastName;
			if (dst.SecondLastName == null)
				err |= (0x1 << 3);

			dst.PrefixName = src.PrefixName;
			if (dst.PrefixName == null)
				err |= (0x1 << 29);
			
			dst.SuffixName = src.SuffixName;
			if (dst.SuffixName == null)
				err |= (0x1 << 30);
			
			ushort val;
			AgeDefinition age;
			if (src.getPatientAge(out val, out age) == 0)
			{
				if (dst.setPatientAge(val, age) != 0)
				{
					err |= (0x1 << 4);
				}
			}
			else
			{
				err |= (0x1 << 4);
			}

			dst.PatientBirthDate = src.PatientBirthDate;
			if (dst.PatientBirthDate == null)
				err |= (0x1 << 5);

			HeightDefinition height;
			if (src.getPatientHeight(out val, out height) == 0)
			{
				if (dst.setPatientHeight(val, height) != 0)
				{
					err |= (0x1 << 6);
				}
			}
			else
			{
				err |= (0x1 << 6);
			}

			WeightDefinition weight;
			if (src.getPatientWeight(out val, out weight) == 0)
			{
				if (dst.setPatientWeight(val, weight) != 0)
				{
					err |= (0x1 << 7);
				}
			}
			else
			{
				err |= (0x1 << 7);
			}

			dst.Gender = src.Gender;
			if (src.Gender == Sex.Null)
				err |= (0x1 << 8);
			
			dst.PatientRace = src.PatientRace;
			if (dst.PatientRace == Race.Null)
				err |= (0x1 << 9);

			dst.AcqMachineID = src.AcqMachineID;

			dst.AnalyzingMachineID = src.AnalyzingMachineID;
			if (dst.AnalyzingMachineID == null)
				err |= (0x1 << 10);

			dst.TimeAcquisition = src.TimeAcquisition;

			dst.BaselineFilter = src.BaselineFilter;
			if (dst.BaselineFilter == 0)
				err |= (0x1 << 11);

			dst.LowpassFilter = src.LowpassFilter;
			if (dst.LowpassFilter == 0)
				err |= (0x1 << 12);
			
			dst.FilterBitmap = src.FilterBitmap;
			if (dst.FilterBitmap == 0)
				err |= (0x1 << 13);

			dst.FreeTextFields = src.FreeTextFields;
			if (dst.FreeTextFields == null)
				err |= (0x1 << 14);

			dst.SequenceNr = src.SequenceNr;
			if (dst.SequenceNr != null)
				err |= (0x1 << 15);

			dst.AcqInstitution = src.AcqInstitution;
			if (dst.AcqInstitution == null)
				err |= (0x1 << 16);

			dst.AnalyzingInstitution = src.AnalyzingInstitution;
			if (dst.AnalyzingInstitution == null)
				err |= (0x1 << 17);

			dst.AcqDepartment = src.AcqDepartment;
			if (dst.AcqDepartment == null)
				err |= (0x1 << 18);

			dst.AnalyzingDepartment = src.AnalyzingDepartment;
			if (dst.AnalyzingDepartment == null)
				err |= (0x1 << 19);

			dst.ReferringPhysician = src.ReferringPhysician;
			if (dst.ReferringPhysician == null)
				err |= (0x1 << 20);

			dst.OverreadingPhysician = src.OverreadingPhysician;
			if (src.OverreadingPhysician == null)
				err |= (0x1 << 21);

			dst.TechnicianDescription = src.TechnicianDescription;
			if (dst.TechnicianDescription == null)
				err |= (0x1 << 22);

			dst.SystolicBloodPressure = src.SystolicBloodPressure;
			if (dst.SystolicBloodPressure != 0)
				err |= (0x1 << 23);

			dst.DiastolicBloodPressure = src.DiastolicBloodPressure;
			if (dst.DiastolicBloodPressure != 0)
				err |= (0x1 << 24);

			dst.Drugs = src.Drugs;
			if (dst.Drugs != null)
				err |= (0x1 << 25);

			dst.ReferralIndication = src.ReferralIndication;
			if (dst.ReferralIndication == null)
				err |= (0x1 << 26);

			dst.RoomDescription = src.RoomDescription;
			if (dst.RoomDescription == null)
				err |= (0x1 << 27);

			dst.StatCode = src.StatCode;
			if (dst.StatCode == 0xff)
				err |= (0x1 << 28);
			
			if ((dst.PatientID == null)
			||	(dst.AcqMachineID == null)
			||	(dst.TimeAcquisition.Year <= 1000))
				return -1;

			return err;
		}
		/// <summary>
		/// Function to copy diagnostic statements from one to anohter
		/// </summary>
		/// <param name="src">source</param>
		/// <param name="dst">destination</param>
		/// <returns>0 on success</returns>
		public static int DiagnosticCopy(IDiagnostic src, IDiagnostic dst)
		{
			// Check for correct input.
			if (src == null)
			{
				return 1;
			}
			if (dst == null)
			{
				return 2;
			}

			// Do copy of diagnostic
			Statements stat;
			if (src.getDiagnosticStatements(out stat) == 0)
			{
				if (dst.setDiagnosticStatements(stat) != 0)
				{
					return 8;
				}
			}
			else
			{
				return 4;
			}
			return 0;
		}
		/// <summary>
		/// Function to copy global measurments from one to another.
		/// </summary>
		/// <param name="src">source</param>
		/// <param name="dst">destination</param>
		/// <returns>0 on success</returns>
		public static int GlobalMeasurementCopy(IGlobalMeasurement src, IGlobalMeasurement dst)
		{
			// Check for correct input.
			if (src == null)
			{
				return 1;
			}
			if (dst == null)
			{
				return 2;
			}

			// Do copy of measurements
			GlobalMeasurements mes;
			if (src.getGlobalMeasurements(out mes) == 0)
			{
				if (dst.setGlobalMeasurements(mes) != 0)
				{
					return 8;
				}
			}
			else
			{
				return 4;
			}
			return 0;
		}
		/// <summary>
		/// Function to copy signals from one to anohter
		/// </summary>
		/// <param name="src">source</param>
		/// <param name="dst">destination</param>
		/// <returns>0 on success</returns>
		public static int SignalCopy(ISignal src, ISignal dst)
		{
			// Check for correct input.
			if (src == null)
			{
				return 1;
			}
			if (dst == null)
			{
				return 2;
			}

			// Do copy of signals
			Signals signals;
			if (src.getSignals(out signals) == 0)
			{
				if (dst.setSignals(signals) != 0)
				{
					return 8;
				}
			}
			else
			{
				return 4;
			}

			return 0;
		}
		/// <summary>
		/// Function to copy lead measurments from one to another.
		/// </summary>
		/// <param name="src">source</param>
		/// <param name="dst">destination</param>
		/// <returns>0 on success</returns>
		public static int LeadMeasurementCopy(ILeadMeasurement src, ILeadMeasurement dst)
		{
			// Check for correct input.
			if (src == null)
			{
				return 1;
			}
			if (dst == null)
			{
				return 2;
			}

			// Do copy of measurements
			LeadMeasurements mes;
			if (src.getLeadMeasurements(out mes) == 0)
			{
				if (dst.setLeadMeasurements(mes) != 0)
				{
					return 8;
				}
			}
			else
			{
				return 4;
			}
			return 0;
		}
		/// <summary>
		/// Function to get error during demographics conversion.
		/// </summary>
		/// <returns>-1 on error</returns>
		public static int getDemographicsError()
		{
			return _DemographicsError;
		}
		/// <summary>
		/// Function to get error during demographics conversion.
		/// </summary>
		/// <returns>error message or null</returns>
		public static string getDemographicsErrorMessage()
		{
			string ret = null;
			if (_DemographicsError == -1)
			{
				ret = "Failed to convert one of the major fields";
			}
			return ret;
		}
		/// <summary>
		/// Function to get error during diagnostic conversion.
		/// </summary>
		/// <returns>0 on success</returns>
		public static int getDiagnosticError()
		{
			return _DiagnosticError;
		}
		/// <summary>
		/// Function to get error during diagnostic conversion.
		/// </summary>
		/// <returns>error message or null</returns>
		public static string getDiagnosticErrorMessage()
		{
			string ret = null;
			switch (_DiagnosticError)
			{
				case 1:
					ret = "No Source ECG format";
					break;
				case 2:
					ret = "No Destination ECG format";
					break;
				case 4:
					ret = "Getting diagnostic from source ECG format failed";
					break;
				case 8:
					ret = "Setting diagnostic of destination ECG format failed";
					break;
			}
			return ret;
		}
		/// <summary>
		/// Function to get error during global measurements conversion.
		/// </summary>
		/// <returns>0 on success</returns>
		public static int getGlobalMeasurementsError()
		{
			return _GlobalMeasurementsError;
		}
		/// <summary>
		/// Function to get error during global measurements conversion.
		/// </summary>
		/// <returns>error message or null</returns>
		public static string getGlobalMeasurementsErrorMessage()
		{
			string ret = null;
			switch (_GlobalMeasurementsError)
			{
				case 1:
					ret = "No Source ECG format";
				break;
				case 2:
					ret = "No Destination ECG format";
				break;
				case 4:
					ret = "Getting global measurements from source ECG format failed";
				break;
				case 8:
					ret = "Setting global measurements of destination ECG format failed";
				break;
			}
			return ret;
		}
		/// <summary>
		/// Function to get error during signal conversion.
		/// </summary>
		/// <returns>0 on success</returns>
		public static int getSignalError()
		{
			return _SignalError;
		}
		/// <summary>
		/// Function to get error during signal conversion.
		/// </summary>
		/// <returns>error message or null</returns>
		public static string getSignalErrorMessage()
		{
			string ret = null;
			switch (_SignalError)
			{
				case 1:
					ret = "No Source ECG format";
				break;
				case 2:
					ret = "No Destination ECG format";
				break;
				case 4:
					ret = "Deconding signal from source ECG format failed";
				break;
				case 8:
					ret = "Encoding signal to destination ECG format failed";
				break;
			}
			return ret;
		}
		/// <summary>
		/// Function to get error during lead measurements conversion.
		/// </summary>
		/// <returns>0 on success</returns>
		public static int getLeadMeasurementsError()
		{
			return _LeadMeasurementsError;
		}
		/// <summary>
		/// Function to get error during lead measurements conversion.
		/// </summary>
		/// <returns>error message or null</returns>
		public static string getLeadMeasurementsErrorMessage()
		{
			string ret = null;
			switch (_LeadMeasurementsError)
			{
				case 1:
					ret = "No Source ECG format";
					break;
				case 2:
					ret = "No Destination ECG format";
					break;
				case 4:
					ret = "Getting lead measurements from source ECG format failed";
					break;
				case 8:
					ret = "Setting lead measurements of destination ECG format failed";
					break;
			}
			return ret;
		}
		
		public static object EnumParse(Type enumType, string str, bool ignoreCase)
		{
#if WINCE
			if (enumType.BaseType != typeof(Enum))
				throw new Exception("EnumParse: enumType isn't an enum!");

			if (ignoreCase)
			{
				foreach (FieldInfo fi in enumType.GetFields())
				{
					if (string.Compare(fi.Name, str, ignoreCase) == 0)
						return fi.GetValue(null);
				}
			}
			else
			{
				FieldInfo fi = enumType.GetField(str);

				if (fi != null)
					return fi.GetValue(null);
			}

				throw new Exception("EnumParse: str value not found in enum!");
#else
			return Enum.Parse(enumType, str, ignoreCase);
#endif
		}

		public static Guid NewGuid()
		{
#if WINCE
			Random randGen = new Random();
			byte[] randBytes = new byte[16];

			randGen.NextBytes(randBytes);

			return new Guid(randBytes);
#else
			return Guid.NewGuid();
#endif
		}
	}
}
