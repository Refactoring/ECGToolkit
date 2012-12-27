/***************************************************************************
Copyright 2012, van Ettinger Information Technology, Lopik, The Netherlands
Copyright 2009, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
#if !WINCE
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Xml.Serialization;

namespace ECGConversion
{
	/// <summary>
	/// Summary description for CheckVersion
	/// </summary>
	public class CheckVersion
	{
		// Strings to notify about a new release
		private const string ReleaseTitle = "C# ECGToolkit: {0} Released!";
		private const string ReleaseText = "Latest version of the C# ECGToolkit is available at: " + ReleaseUrl;
		private const string ReleaseUrl = "http://ecgtoolkit-cs.sourceforge.net";

		// Strings to ask about new version check
		public const string AllowCheckTitle = "Allow new version notification for C# ECGToolkit?";
		public const string AllowCheckText = "The C# ECG Toolkit can automatically notify you about a new version.\n\nDo you wish to be notified about a new release?";

		// Strings to identify the version part of the feed
		private const string ReleaseTextStart = "ecgtoolkit-cs-";
		private const string ReleaseTextEnd = " released";

		// Should redirect to: @"http://sourceforge.net/export/rss2_projfiles.php?group_id=238719";
		private const string CheckUrl = @"http://ecgtoolkit-cs.sourceforge.net/check_version.php";

		/// <summary>
		/// Function to fetch version number from a text
		/// </summary>
		/// <param name="str">text to fetch version number from</param>
		/// <param name="spr">seperator of version text</param>
		/// <returns>version number as an integer</returns>
		private static int VersionFromString(string str, char spr)
		{
			int ret = -1;

			try
			{
				string[] versionArray = str.Split(spr);

				if (versionArray.Length >= 2)
					ret = (int.Parse(versionArray[0]) << 8) + (int.Parse(versionArray[1]) & 0xff);
			}
			catch {}

			return ret;
		}

		/// <summary>
		/// Function to fetch version number from assembly
		/// </summary>
		/// <returns>version number as an integer</returns>
		private static int GetAssemblyVersion()
		{
			try
			{
				Assembly assembly = Assembly.GetExecutingAssembly();
				Version version = assembly.GetName().Version;

				return (version.Major << 8) + (version.Minor & 0xff);
			} 
			catch {}

			return -1;
		}

		public delegate void NewVersionCallback(string title, string text, string url);

		/// <summary>
		/// event handler to handle a new version.
		/// </summary>
		public static event NewVersionCallback OnNewVersion;

		public delegate CheckAllowed AllowNewVersionCheckCallback(string title, string question);

		/// <summary>
		/// event handler to handle a request to allow new version check.
		/// </summary>
		public static event AllowNewVersionCheckCallback OnAllowNewVersionCheck;

		/// <summary>
		/// Enumartion describing the check allowed possibilities.
		/// </summary>
		public enum CheckAllowed
		{
			Unknown,
			No,
			Yes,
			AlreadyChecked,
		}

		/// <summary>
		/// Get/Set whether new version check is allowed
		/// </summary>
		public static CheckAllowed CheckNewVersionAllowed
		{
			get
			{
				string path = Path.Combine(
#if DEBUG				
					Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
					"CheckVersion.log");
#else
					Path.GetTempPath(),
					"ECGConversionCheckVersion.log");
#endif

				FileInfo fi = new FileInfo(path);

				if (fi.Exists)
				{
					if (Math.Abs((DateTime.Now.ToUniversalTime() - fi.LastWriteTimeUtc).TotalMinutes) <= 15.0)
						return CheckAllowed.AlreadyChecked;

					StreamReader sw = new StreamReader(path);
					try
					{
						return (string.Compare(sw.ReadLine(), CheckAllowed.Yes.ToString()) == 0) ? CheckAllowed.Yes : CheckAllowed.No;
					}
					catch {}
					finally
					{
						sw.Close();
					}
				}

				return CheckAllowed.Unknown;
			}
			set
			{
				string path = Path.Combine(
#if DEBUG				
					Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
					"CheckVersion.log");
#else
					Path.GetTempPath(),
					"ECGConversionCheckVersion.log");
#endif

				if (value == CheckAllowed.Unknown)
				{
					File.Delete(path);
				}
				else if (value != CheckAllowed.AlreadyChecked)
				{
					StreamWriter sw = new StreamWriter(path, false);

					sw.WriteLine(value.ToString());
					sw.Flush();
					sw.Close();
				}
			}
		}

		/// <summary>
		/// Perform check for new version.
		/// </summary>
		public static void CheckForNewVersion()
		{
			if (OnNewVersion == null)
				return;

			// perform check whether new version check is allowed or necessary.
			try
			{
				switch (CheckNewVersionAllowed)
				{
						// Unknown whether check is allowed so ask when handled.
					case CheckAllowed.Unknown:		
						if (OnAllowNewVersionCheck == null)
							return;

						CheckAllowed ca = OnAllowNewVersionCheck(AllowCheckTitle, AllowCheckText);

						if (ca == CheckAllowed.No)
						{
							CheckNewVersionAllowed = CheckAllowed.No;
							return;
						}
						else if (ca == CheckAllowed.Unknown)
						{
							return;
						}

						CheckNewVersionAllowed = CheckAllowed.Yes;
						break;
					case CheckAllowed.No:	
					case CheckAllowed.AlreadyChecked:
						return;
				}
			}
			catch
			{
				return;
			}

			int version = GetAssemblyVersion();

			WebRequest wReq = null;
			WebResponse wRes = null;

			try
			{
				wReq = HttpWebRequest.Create(CheckUrl);
				wRes = wReq.GetResponse();

				if (wRes is HttpWebResponse)
				{
					HttpWebResponse hwRes = (HttpWebResponse) wRes;
					Stream stream = hwRes.GetResponseStream();

					XmlSerializer serializer = new XmlSerializer(typeof(RssFeed));
					object obj = serializer.Deserialize(stream);

					if (obj is RssFeed)
					{
						RssFeed rss = (RssFeed)obj;

						foreach (RssItem item in rss.channel.item)
						{
							int
								indexStart = item.title.IndexOf(ReleaseTextStart),
								indexEnd = item.title.IndexOf(ReleaseTextEnd);

							if (indexStart < 0)
								indexStart = 0;
							else
								indexStart += ReleaseTextStart.Length;

							if (indexEnd > 0)
							{
								string versionText = item.title.Substring(indexStart, indexEnd - indexStart);
								int latestVersion = VersionFromString(versionText, '_');

								if (version < latestVersion)
								{
									OnNewVersion(
										ReleaseTitle.Replace("{0}", (latestVersion >> 8) + "." + (latestVersion & 0xff)),
										ReleaseText,
										ReleaseUrl);

									CheckNewVersionAllowed = CheckAllowed.Yes;

									return;
								}
							}
						}
					}
				}
			}
			catch {}
			finally
			{
				if (wRes != null)
					wRes.Close();
			}
		}

		[XmlRoot("rss")]
		public class RssFeed
		{
			[XmlAttribute()]
			public string version;

			[XmlElement()]
			public RssChannel channel;
		}

		public class RssChannel
		{
			[XmlElement()]
			public string title;

			[XmlElement()]
			public string link;

			[XmlElement()]
			public string description;

			[XmlElement()]
			public string copyright;

			[XmlElement()]
			public string lastBuildDate;

			[XmlElement()]
			public string generator;

			[XmlElement()]
			public RssImage image;

			[XmlElement()]
			public RssItem[] item;
		}

		public class RssImage
		{
			[XmlElement()]
			public string title;

			[XmlElement()]
			public string url;

			[XmlElement()]
			public string link;

			[XmlElement()]
			public string width;

			[XmlElement()]
			public string height;

			[XmlElement()]
			public string description;
		}

		public class RssItem
		{
			[XmlElement()]
			public string title;

			[XmlElement()]
			public string description;

			[XmlElement()]
			public string author;

			[XmlElement()]
			public string link;

			[XmlElement()]
			public RssGuid guid;

			[XmlElement()]
			public string pubDate;

			[XmlElement()]
			public string comments;
		}

		public class RssGuid
		{
			[XmlAttribute()]
			public bool isPermaLink;

			[XmlText()]
			public string value;
		}
	}
}
#endif