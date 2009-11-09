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

using ECGConversion;
using ECGConversion.ECGManagementSystem;

namespace ECGStoreSCU
{
	/// <summary>
	/// ECGStoreSCU program.
	/// </summary>
	class ECGStoreSCU
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			try
			{
				CheckVersion.OnNewVersion += new ECGConversion.CheckVersion.NewVersionCallback(CheckVersion_OnNewVersion);

				ECGConverter.Instance.waitForECGManagementSystemSupport("PACS");

				IECGManagementSystem pacs = ECGConverter.Instance.getECGManagementSystem("PACS");
				bool Anonymize = false;
				string patid = null;

				if (pacs == null)
				{
					Console.Error.WriteLine("Error: DICOM plugin not available!");

					return;
				}

				ECGConfig cfg = ECGConverter.Instance.getConfig(pacs.FormatName);

				// A normal parameters list.
				ArrayList al = new ArrayList();

				// first get all the configuration parameters.
				for (int i=0;i < args.Length;i++)
				{
					if (string.Compare(args[i], "-A") == 0)
					{
						// this will anonymize a ECG
						Anonymize = true;
					}
					else if (args[i].StartsWith("-P"))
					{
						// Set the Patient ID of the ECG.
						if (args[i].Length == 2)
						{
							if (args.Length == ++i)
							{
								Console.Error.WriteLine("Error: Bad Arguments!");

								al.Clear();

								break;
							}

							patid = args[i];
						}
						else
						{
							patid = args[i].Substring(2, args[i].Length - 2);
						}
					}
					else if (args[i].StartsWith("-aet"))
					{
						// set AE Title of this SCU.
						if (args[i].Length == 4)
						{
							if (args.Length == ++i)
							{
								Console.Error.WriteLine("Error: Bad Arguments!");

								al.Clear();

								break;
							}

							pacs.Config["AESCU"] = args[i];
						}
						else
						{
							pacs.Config["AESCU"] = args[i].Substring(4, args[i].Length - 4);
						}
					}
					else if (args[i].StartsWith("-aec"))
					{
						// set AE Title of the called SCP.
						if (args[i].Length == 4)
						{
							if (args.Length == ++i)
							{
								Console.Error.WriteLine("Error: Bad Arguments!");

								al.Clear();

								break;
							}

							pacs.Config["AESCP"] = args[i];
						}
						else
						{
							pacs.Config["AESCP"] = args[i].Substring(4, args[i].Length - 4);
						}
					}
					else if (args[i].StartsWith("-C"))
					{
						// Add additional configuration items.
						string[] temp = null;

						if (args[i].Length == 2)
						{
							if (args.Length == ++i)
							{
								Console.Error.WriteLine("Error: Bad Arguments!");

								al.Clear();

								break;
							}

							temp = args[i].Split('=');
						}
						else
						{
							temp = args[i].Substring(2, args[i].Length - 2).Split('=');
						}

						if ((temp == null)
						||  (cfg == null))
						{
							Console.Error.WriteLine("Error: Bad Arguments!");

							al.Clear();

							break;
						}
						else
						{
							cfg[temp[0]] = temp[1];
						}
					}
					else
					{
						// add to the normal parameters list.
						al.Add(args[i]);
					}
				}

				// Three or Four normal parameters are accepted!.
				if ((al.Count == 3)
				||	(al.Count == 4))
				{
					if (!pacs.ConfiguredToSave()
					||	((cfg != null)
					&&	 !cfg.ConfigurationWorks()))
					{
						Console.Error.WriteLine("Error: Bad Configuration!");

						return;
					}

					ECGConversion.IECGFormat src = null;
				
					int offset = 0,
						i = 0;

					string file = (string) al[i++];

					if (al.Count == 4)
					{
						try
						{
							offset = int.Parse((string) al[i++]);
						}
						catch
						{
							Console.Error.WriteLine("Error: incorrect offset!");

							return;
						}
					}

					UnknownECGReader reader = new ECGConversion.UnknownECGReader();
					src = reader.Read(file, offset); 

					if ((src == null)
					||	!src.Works())
					{
						Console.Error.WriteLine("Error: Couldn't open ECG from specified file!");

						return;
					}

					if (Anonymize)
						src.Anonymous();

					pacs.Config["Server"] = (string) al[i++];
					pacs.Config["Port"] = (string) al[i++];

					if (pacs.SaveECG(src, patid, cfg) != 0)
					{
						Console.Error.WriteLine("Storing of ECG failed!");
					}
				}
				else
				{
					// Provide a help message.
					if (al.Count != 0)
					{
						Console.Error.WriteLine("Error: Bad Arguments!");
					}

					Console.WriteLine("Usage: ECGStoreSCU [-A] [-P patid] [-aet name] [-aec name] {0}file [offset] host port", cfg == null ? "" : "[-C var=val] ");
					Console.WriteLine();
					Console.WriteLine("  file       path to input file");
					Console.WriteLine("  offset     offset in input file");
					Console.WriteLine("  server     hostname of DICOM peer");
					Console.WriteLine("  port       tcp/ip port number of peer");
					Console.WriteLine("  -A         anonymize output");
					Console.WriteLine("  -P patid   specifiy a Patient ID for ECG");
					Console.WriteLine("  -aet name  calling AE Title");
					Console.WriteLine("  -aec name  called AE Title");

					if (cfg != null)
					{
						Console.WriteLine("  -C var=val providing a configuration item");
						Console.WriteLine();
						Console.WriteLine("Exporting type {0} has got the following configuration items:", pacs.FormatName);

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
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("Error: {0}", ex.ToString());
			}
		}

		private static void CheckVersion_OnNewVersion(string title, string text, string url)
		{
			Console.WriteLine(title);
			Console.WriteLine();
			Console.WriteLine(text);
			Console.WriteLine(new string('_', 79));
			Console.WriteLine();
		}
	}
}

