/***************************************************************************
Copyright 2013, van Ettinger Information Technology, Lopik, The Netherlands
Copyright 2008-2009, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

using ECGConversion;
using ECGConversion.ECGDemographics;
using ECGConversion.ECGManagementSystem;
using ECGConversion.ECGSignals;

namespace ECGTool
{
	public class ECGTool
	{
		private ECGConverter converter;

		private bool _NoArgs;
		private bool _BadArgs
		{
			get
			{
				return _Error != null;
			}
		}
		private string _Error;

		private string _InType;
		private string _InFile;
		private int _InFileOffset;
		private string _OutType;
		private string _OutFile;

		private bool _Anonymize;
		private string _PatientId;
		private SortedList _Config = new SortedList();
		
		private int _BufferedSecondsToLoad;
		private int _BufferedOffsetToLoad;

		public ECGTool()
		{
			CheckVersion.OnNewVersion += new ECGConversion.CheckVersion.NewVersionCallback(CheckVersion_OnNewVersion);

			converter = ECGConverter.Instance;

			Init();
		}

		public void Init()
		{
			_NoArgs = true;
			_Error = null;

			_InType = null;
			_InFile = null;
			_InFileOffset = 0;
			_OutType = null;
			_OutFile = null;

			_Anonymize = false;
			_PatientId = null;
			_Config.Clear();
			
			_BufferedSecondsToLoad = 0;
			_BufferedOffsetToLoad = 0;
		}

		public void ParseArguments(string[] args)
		{
			_NoArgs = false;

			try
			{

				if (args != null)
				{
					_NoArgs = args.Length == 0;

					if (((args.Length == 1)
					||	 (args.Length == 2))
					&&	Regex.IsMatch(args[0], "(-h)|(--help)"))
					{
						if (args.Length == 2)
							_OutType = args[1];

						_NoArgs = true;
					}
					else
					{
						ArrayList al = new ArrayList();

						for (int i=0;i < args.Length;i++)
						{
							if (string.Compare(args[i], "-A") == 0)
							{
								_Anonymize = true;
							}
							else if (args[i].StartsWith("-I"))
							{
								if (args[i].Length == 2)
								{
									if (args.Length > ++i)
									{
										_InType = args[i];
									}
									else
									{
										_Error = "Bad Arguments!";

										return;
									}
								}
								else
								{
									_InType = args[i].Substring(2, args[i].Length-2);
								}
							}
							else if (args[i].StartsWith("-P"))
							{
								if (args[i].Length == 2)
								{
									if (args.Length > ++i)
									{
										_PatientId = args[i];
									}
									else
									{
										_Error = "Bad Arguments!";

										return;
									}
								}
								else
								{
									_PatientId = args[i].Substring(2, args[i].Length-2);
								}
							}
							else if (args[i].StartsWith("-C"))
							{
								string[] temp = null;

								if (args[i].Length == 2)
								{
									if (args.Length > ++i)
									{
										temp = args[i].Split('=');
									}
									else
									{
										_Error = "Bad Arguments!";

										return;
									}
								}
								else
								{
									temp = args[i].Substring(2, args[i].Length-2).Split('=');
								}

								if ((temp != null)
								&&	(temp.Length == 2))
								{
									_Config[temp[0]] = temp[1];
								}
								else
								{
									_Error = "Bad Arguments!";
									
									return;
								}
							}
							else if (args[i].StartsWith("-bsl"))
							{
								string val = null;
								
								// set the nr of seconds to load for buffered signals.
								if (args[i].Length == 4)
								{
									if (args.Length == ++i)
									{
										_Error = "Bad Arguments!";
		
										return;
									}
		
									val = args[i];
								}
								else
								{
									val = args[i].Substring(4, args[i].Length - 4);
								}
								
								if (val != null)
								{
									if (string.Compare(val, "all", true) == 0)
									{
										_BufferedSecondsToLoad = int.MaxValue;
									}
									else
									{
										try
										{
											char B = val[val.Length-1];
											
											int nA = 0,
												nB = 1;
											
											if (char.ToLower(B) == 's')
											{
												nB = 1;
												val = val.Substring(0, val.Length-1);
											}
											else if (char.ToLower(B) == 'm')
											{
												nB = 60;
												val = val.Substring(0, val.Length-1);
											}
											else if (char.ToLower(B) == 'h')
											{
												nB = 3600;
												val = val.Substring(0, val.Length-1);
											}
											else if (char.ToLower(B) == 'd')
											{
												nB = 86400;
												val = val.Substring(0, val.Length-1);
											}
											else if (char.ToLower(B) == 'w')
											{
												nB = 604800;
												val = val.Substring(0, val.Length-1);
											}
											
											nA = int.Parse(val);
										
											_BufferedSecondsToLoad = nA * nB;
										}
										catch
										{
											_Error = "Bad Arguments!";
		
											return;
										}
									}
								}
							}
							else if (args[i].StartsWith("-bol"))
							{
								string val = null;
								
								// set the nr of seconds to load for buffered signals.
								if (args[i].Length == 4)
								{
									if (args.Length == ++i)
									{
										_Error = "Bad Arguments!";
		
										return;
									}
		
									val = args[i];
								}
								else
								{
									val = args[i].Substring(4, args[i].Length - 4);
								}
								
								if (val != null)
								{
									try
									{
										char B = val[val.Length-1];
		
										int nA = 0,
											nB = 1;
		
										if (char.ToLower(B) == 's')
										{
											nB = 1;
											val = val.Substring(0, val.Length-1);
										}
										else if (char.ToLower(B) == 'm')
										{
											nB = 60;
											val = val.Substring(0, val.Length-1);
										}
										else if (char.ToLower(B) == 'h')
										{
											nB = 3600;
											val = val.Substring(0, val.Length-1);
										}
										else if (char.ToLower(B) == 'd')
										{
											nB = 86400;
											val = val.Substring(0, val.Length-1);
										}
										else if (char.ToLower(B) == 'w')
										{
											nB = 604800;
											val = val.Substring(0, val.Length-1);
										}
											
										nA = int.Parse(val);
										
										_BufferedOffsetToLoad = nA * nB;
									}
									catch
									{
										_Error = "Bad Arguments!";
										return;
									}
								}
							}
							else
							{
								al.Add(args[i]);
							}
						}

						if (al.Count == 2)
						{
							_InFile = (string) al[0];
							_InFileOffset = 0;
							_OutType = (string) al[1];
							_OutFile = null;
						}
						if (al.Count == 3)
						{
							_InFile = (string) al[0];

							if (ECGConverter.Instance.hasECGManagementSystemSaveSupport((string) al[2]))
							{
								_InFileOffset = int.Parse((string) al[1]);
								_OutType = (string) al[2];
								_OutFile = null;
							}
							else
							{
								_InFileOffset = 0;
								_OutType = (string) al[1];
								_OutFile = (string) al[2];
							}
						}
						else if (al.Count == 4)
						{
							_InFile = (string) al[0];
							_InFileOffset = int.Parse((string) al[1]);
							_OutType = (string) al[2];
							_OutFile = (string) al[3];
						}
						else
						{
							_Error = "Bad Arguments!";
						}
					}
				}
			}
			catch
			{
				_Error = "Bad Arguments!";
			}
		}

		public void Run()
		{
			try
			{
				if (_NoArgs)
				{
					Help();
				}
				else if (_BadArgs)
				{
					Error();
					Help();
				}
				else 
				{
					if (_InType != null)
						converter.waitForLoadingAllPlugins();

					IECGReader reader = _InType == null ? new UnknownECGReader() : converter.getReader(_InType);

					if (reader == null)
					{
						Console.Error.WriteLine("Error: no reader provided by input type!");

						return;
					}

					ECGConfig config1 = ECGConverter.Instance.getConfig(_InType);

					if (config1 != null)
					{
						for (int i=0;i < _Config.Count;i++)
						{
							config1[(string) _Config.GetKey(i)] = (string) _Config.GetByIndex(i);
						}
					}	
				
					IECGFormat src = reader.Read(_InFile, _InFileOffset, config1);

					if (src == null
					||	!src.Works())
					{
						Console.Error.WriteLine("Error: {0}", reader.getErrorMessage());

						return;
					}
					
					if ((_BufferedSecondsToLoad > 0)
					||	(_BufferedOffsetToLoad > 0))
					{
						ECGConversion.ECGSignals.Signals sigs = null;
						
						if ((src.Signals.getSignals(out sigs) == 0)
						&&	sigs.IsBuffered)
						{
							ECGConversion.ECGSignals.BufferedSignals bs = (ECGConversion.ECGSignals.BufferedSignals)sigs;
							
							int start = 0,
								end = 0;
								
							start = bs.RealRhythmStart + (_BufferedOffsetToLoad * bs.RhythmSamplesPerSecond);
							end = (_BufferedSecondsToLoad == int.MaxValue) ? _BufferedSecondsToLoad : start + (_BufferedSecondsToLoad * bs.RhythmSamplesPerSecond);
							
							if (start > bs.RealRhythmEnd)
								start = bs.RealRhythmEnd;
							
							if (end > bs.RealRhythmEnd)
								end = bs.RealRhythmEnd;
								
							if (start < end)
							{
								bs.LoadSignal(start, end);
								
								src.Signals.setSignals(bs);
							}
						}
					}

					IECGManagementSystem manSys = _OutFile == null ? null : converter.getECGManagementSystem(_OutType);

					config1 = ECGConverter.Instance.getConfig(manSys == null ? _OutType : manSys.FormatName);

					ECGConfig config2 = manSys == null ? null : manSys.Config;

					for (int i=0;i < _Config.Count;i++)
					{
						if (config1 != null)
							config1[(string) _Config.GetKey(i)] = (string) _Config.GetByIndex(i);

						if (config2 != null)
							config2[(string) _Config.GetKey(i)] = (string) _Config.GetByIndex(i);
					}

					if ((config1 != null)
					&&	!config1.ConfigurationWorks())
					{
						Console.Error.WriteLine("Error: Bad Configuration for ECG Format!");

						return;
					}

					if ((config2 != null)
					&&	!config2.ConfigurationWorks())
					{
						Console.Error.WriteLine("Error: Bad Configuration for ECG Management System!");

						return;
					}

					if (manSys == null)
					{
						IECGFormat dst = src.GetType() == ECGConverter.Instance.getType(_OutType) ? src : null;

						if (dst == null)
						{
							if ((src.Demographics != null)
							&&	(src.Demographics.PatientID == null))
								src.Demographics.PatientID = _PatientId;

							ECGConverter.Instance.Convert(src, _OutType, config1, out dst);
						}

						if ((dst == null)
						||	!dst.Works())
						{
							Console.Error.WriteLine("Error: Conversion Failed!");

							return;
						}

						if (_Anonymize)
							dst.Anonymous();

						if ((_PatientId != null)
						&&  (dst.Demographics != null))
						{
							dst.Demographics.PatientID = _PatientId;
						}

						ECGWriter.Write(dst, _OutFile, true);

						if (ECGWriter.getLastError() != 0)
							Console.Error.WriteLine("Error: {0}", ECGWriter.getLastErrorMessage());
					}
					else
					{
						if (manSys.ConfiguredToSave())
						{
							if (_Anonymize)
							{
								src.Anonymous();
							}

							manSys.SaveECG(src, _PatientId, config1);
						}
						else
						{
							Console.Error.WriteLine("Error: Not configured to store!");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Error: {0}", ex.ToString());
			}
		}

		private void Error()
		{
			Console.Error.WriteLine("Error: {0}", _Error);
		}

		private void Help()
		{
			try
			{
				converter.waitForLoadingAllPlugins();

				bool bHelp = true;

				if ((_OutType != null)
				&&	!_BadArgs)
				{
					ECGConfig
						cfg1 = null,
						cfg2 = null;

					IECGFormat format = converter.getFormat(_OutType);

					if (format != null)
					{
						bHelp = false;

						cfg1 = format.Config;
					}
					else
					{
						IECGManagementSystem manSys = converter.getECGManagementSystem(_OutType);

						if (manSys != null)
						{
							bHelp = false;

							cfg1 = manSys.Config;
							cfg2 = converter.getFormat(manSys.FormatName).Config;
						}
					}

					if (bHelp)
					{
						_Error = "Unknown type!";
						Error();
					}
					else
					{
						int nrItems = cfg1 != null ? cfg1.NrConfigItems : 0;

						if (cfg2 != null)
							nrItems += cfg1.NrConfigItems;

						if (nrItems == 0)
						{
							Console.WriteLine("Exporting type {0} has got zero configuration items!", _OutType);
							Console.WriteLine();
						}
						else
						{
							Console.WriteLine("Exporting type {0} has got the following configuration items:", _OutType);

							nrItems = cfg1 != null ? cfg1.NrConfigItems : 0;

							for (int i=0;i < nrItems;i++)
							{
								string
									name = cfg1[i],
									def = cfg1[name];

								Console.Write("  {0}", name);
								if (def != null)
								{
									Console.Write(" (default value: \"");
									Console.Write(def);
									Console.Write("\")");
								}
								Console.WriteLine();
							}

							nrItems = cfg2 != null ? cfg2.NrConfigItems : 0;

							for (int i=0;i < nrItems;i++)
							{
								string
									name = cfg2[i],
									def = cfg2[name];

								Console.Write("  {0}", name);
								if (def != null)
								{
									Console.Write(" (default value: \"");
									Console.Write(def);
									Console.Write("\")");
								}
								Console.WriteLine();
							}
						}
					}
				}

				if (bHelp)
				{
					string outputTypes, outputECGMS;

					StringBuilder sb = new StringBuilder();

					foreach (string str in converter.getSupportedFormatsList())
					{
						if (sb.Length != 0)
							sb.Append(", ");

						sb.Append(str);
					}

					outputTypes = sb.ToString();

					sb = new StringBuilder();

					foreach (string str in converter.getSupportedManagementSystemsList())
					{
						if (converter.hasECGManagementSystemSaveSupport(str))
						{
							if (sb.Length != 0)
								sb.Append(", ");

							sb.Append(str);
						}
					}

					outputECGMS = sb.Length == 0 ? "(none)" : sb.ToString();

					Console.WriteLine("Usage: ECGTool [-A] [-P patid] [-I intype] [-bsl nrsec] [-bol nrsec] [-C \"var=val\" [...]] filein [offset] outtype fileout");
					Console.WriteLine("       ECGTool [-A] [-P patid] [-I intype] [-bsl nrsec] [-bol nrsec] [-C \"var=val\" [...]] filein [offset] outecgms");
					Console.WriteLine("       ECGTool -h [outtype | outecgms | intype]");
					Console.WriteLine();
					Console.WriteLine("  filein     path to input file");
					Console.WriteLine("  offset     offset in input file");
					Console.WriteLine("  outtype    type of ouput file: {0}", outputTypes);
					Console.WriteLine("  fileout    path for output file");
					Console.WriteLine("  outecgms   type of output ECG Management System: {0}", outputECGMS);
					Console.WriteLine("  -A         anonymize output");
					Console.WriteLine("  -P patid   specifiy a Patient ID for ECG");
					Console.WriteLine("  -I intype  specify an input type");
					Console.WriteLine("  -bsl nrsec buffered seconds to load (add to value: s, m, h, d or w) or use: all");
					Console.WriteLine("  -bol nrsec buffered seconds offset to load (add to value: s, m, h, d or w)");
					Console.WriteLine("  -C var=val providing a configuration item");
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Error: {0}", ex.ToString());
			}
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			ECGTool tool = new ECGTool();

			tool.ParseArguments(args);

			tool.Run();
		}

		private void CheckVersion_OnNewVersion(string title, string text, string url)
		{
			Console.WriteLine(title);
			Console.WriteLine();
			Console.WriteLine(text);
			Console.WriteLine(new string('_', 79));
			Console.WriteLine();
		}
	}
}
