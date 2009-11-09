/***************************************************************************
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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using ECGConversion;
using ECGConversion.ECGDemographics;
using ECGConversion.ECGManagementSystem;
using ECGConversion.ECGSignals;

namespace ECGPrint
{
	public class ECGPrint
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

		private string _InFile;
		private int _InFileOffset;

		private bool _Anonymize;
		private bool _Silence;
		private string _PatientId;
		private SortedList _Config = new SortedList();

		public ECGPrint()
		{
			Init();
		}

		public void Init()
		{
			CheckVersion.OnNewVersion += new ECGConversion.CheckVersion.NewVersionCallback(CheckVersion_OnNewVersion);
			converter = ECGConverter.Instance;

			_NoArgs = true;
			_Error = null;

			_InFile = null;
			_InFileOffset = 0;

			_Anonymize = false;
			_Silence = false;
			_PatientId = null;
			_Config.Clear();
		}

		public void ParseArguments(string[] args)
		{
			_NoArgs = false;

			try
			{
				if (args != null)
				{
					_NoArgs = args.Length == 0;

					if ((args.Length == 1)
					&&	Regex.IsMatch(args[0], "(-h)|(--help)"))
					{
						_NoArgs = true;
					}
					else
					{
						ArrayList al = new ArrayList();

						for (int i=0;i < args.Length;i++)
						{
							if (string.Compare(args[i], "-S") == 0)
							{
								_Silence = true;
							}
							else if (string.Compare(args[i], "-A") == 0)
							{
								_Anonymize = true;
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
										_Error = "Bad Arguments";

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
										_Error = "Bad Arguments";

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
									_Error = "Bad Arguments";
								}
							}
							else
							{
								al.Add(args[i]);
							}
						}

						if (al.Count == 1)
						{
							_InFile = (string) al[0];
							_InFileOffset = 0;
						}
						else if (al.Count == 2)
						{
							_InFile = (string) al[0];
							_InFileOffset = int.Parse((string) al[1]);
						}
						else
						{
							_Error = "Bad Arguments";
						}
					}
				}
			}
			catch
			{
				_Error = "Bad Arguments";
			}
		}

		public void Run()
		{
			try
			{
				if (!converter.waitForFormatSupport("PDF"))
				{
					Console.Error.WriteLine("Error: PDF plug-in is not available!");

					return;
				}

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
					UnknownECGReader reader = new UnknownECGReader();
				
					IECGFormat src = reader.Read(_InFile, _InFileOffset);

					if ((src == null)
					||	!src.Works())
					{
						Console.Error.WriteLine("Error: {0}", reader.getErrorMessage());

						return;
					}

					ECGConfig config = converter.getConfig("PDF");

					for (int i=0;i < _Config.Count;i++)
					{
						if (config != null)
							config[(string) _Config.GetKey(i)] = (string) _Config.GetByIndex(i);
					}

					if ((config != null)
					&&	!config.ConfigurationWorks())
					{
						Console.Error.WriteLine("Error: Bad Configuration for ECG Format!");

						return;
					}

					IECGFormat dst = null;

					converter.Convert(src, "PDF", config, out dst);

					if ((dst == null)
					||	!dst.Works())
					{
						Console.Error.WriteLine("Error: Creating PDF failed!");

						return;
					}

					if (_Anonymize)
						dst.Anonymous();

					if ((_PatientId != null)
					&&  (dst.Demographics != null))
					{
						dst.Demographics.PatientID = _PatientId;
					}

					string outfile = Path.GetTempFileName();

					ECGWriter.Write(dst, outfile, true);

					if (ECGWriter.getLastError() != 0)
					{
						Console.Error.WriteLine("Error: {0}", ECGWriter.getLastErrorMessage());

						return;
					}

					if (!PrintPdf(outfile, _Silence))
					{
						Console.Error.WriteLine("Error: Using acrobat to print failed!");

						return;
					}

					try
					{
						File.Delete(outfile);
					}
					catch
					{
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
				string outputTypes, outputECGMS;

				converter.waitForLoadingAllPlugins();
				ECGConfig cfg = converter.getConfig("PDF");

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

				Console.WriteLine("Usage: ECGPrint [-A] [-S] [-P patid] {0} filein [offset]", ((cfg == null) ? "" : "[-C \"var=val\" [...]]"));
				Console.WriteLine("       ECGPrint -h");
				Console.WriteLine();
				Console.WriteLine("  filein     path to input file");
				Console.WriteLine("  offset     offset in input file (optional)");
				Console.WriteLine("  -A         anonymize output");
				Console.WriteLine("  -S         silently print");
				Console.WriteLine("  -h         provides this help message");
				Console.WriteLine("  -P patid   specifiy a Patient ID for ECG");

				if (cfg != null)
				{
					Console.WriteLine("  -C var=val providing a configuration item");
					Console.WriteLine();
					Console.WriteLine("Exporting type PDF has got the following configuration items:");

					int nrItems = cfg.NrConfigItems;

					for (int i=0;i < nrItems;i++)
					{
						string
							name = cfg[i],
							def = cfg[name];

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
			catch (Exception ex)
			{
				Console.Error.WriteLine("Error: {0}", ex.ToString());
			}
		}

		private static bool PrintPdf(string filePath, bool bSilently)
		{
			DDEClient client = null;

			try
			{
				client = new DDEClient("acroview", "control");

				client.Open();
			}
			catch
			{
			}

			System.Diagnostics.Process p = null;

			if (!client.Opened)
			{
				try
				{
					// try running Adobe Reader
					p = new System.Diagnostics.Process();
					p.StartInfo.FileName = "AcroRd32.exe";
					p.Start();
					p.WaitForInputIdle();
				}
				catch
				{
					return false;
				}

				client.Open();
				
				if (!client.Opened)
				{
					client.Dispose();

					return false;
				}
			}

			bool ret = client.Execute("[DocOpen(\"" + filePath + "\")]", 60000)
					&& client.Execute((bSilently ? "[FilePrintSilent(\"" : "[FilePrint(\"") + filePath + "\")]", 60000)
					&& client.Execute("[DocClose(\"" + filePath + "\")]", 60000);

			if (p != null)
			{
				ret = ret && client.Execute("[AppExit]", 60000);

				p.WaitForExit();
			}

			client.Dispose();

			return ret;
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			ECGPrint tool = new ECGPrint();

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
