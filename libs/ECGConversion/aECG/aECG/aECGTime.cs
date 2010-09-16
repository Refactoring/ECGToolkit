/***************************************************************************
Copyright 2008, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
using System.Xml;

namespace ECGConversion.aECG
{
	/// <summary>
	/// Summary description for aECGTime.
	/// </summary>
	public sealed class aECGTime : aECGElement
	{
		private static string[] _DateFormats = {"yyyyMMddHHmmss.fff", "yyyyMMddHHmmss", "yyyyMMddHHmm", "yyyyMMdd", "yyyyMM", "yyyy"};
		public TimeType Type;
		public DateTime ValOne; // low or center
		public DateTime ValTwo; // high or empty
		public aECGValuePair Width = new aECGValuePair("width");

		public static DateTime ParseDate(string val)
		{
			if ((val != null)
			&&	(val.Trim().Length != 0))
			{
				if (val.Length == 8)
				{
					if (val.EndsWith("0000"))
						return new DateTime(int.Parse(val.Substring(0, 4)), 1, 1);
				}

				return DateTime.ParseExact(val, _DateFormats, System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat, System.Globalization.DateTimeStyles.None);
			}

			return DateTime.MinValue;
		}

		public static string WriteDate(DateTime dt)
		{
			if ((dt.Hour == 0)
			&&	(dt.Minute == 0)
			&&	(dt.Second == 0)
			&&	(dt.Millisecond == 0)
			&&	(dt.Ticks == 0))
				return dt.ToString(_DateFormats[3], System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat);

			return dt.ToString(_DateFormats[0], System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat);
		}

		public aECGTime() : base("effectiveTime")
		{
			Empty();
		}

		public aECGTime(string name) : base(name)
		{
			Empty();
		}

		public override int Read(XmlReader reader)
		{
			if (reader.IsEmptyElement)
			{
				Type = TimeType.Empty;
				return 0;
			}

			while (reader.Read())
			{
				if ((reader.NodeType == XmlNodeType.Comment)
				||  (reader.NodeType == XmlNodeType.Whitespace))
					continue;

				if (String.Compare(reader.Name, Name) == 0)
				{
					if (!Works())
					{
						Empty();

						return 1;
					}

					if (reader.NodeType == XmlNodeType.EndElement)
						return 0;

					return 2;
				}

				if (String.Compare(reader.Name, "center") == 0)
				{
					if (Type == TimeType.LowHigh)
						return 3;

					Type = TimeType.Center;

                    try
                    {
                        ValOne = ParseDate(reader.GetAttribute("value"));
                    }
                    catch
                    {
						return 3;
                    }

					if (!reader.IsEmptyElement)
					{
						int depth = reader.Depth;

						while (reader.Read())
						{
							if ((depth == reader.Depth)
							&&	(string.Compare(reader.Name, "center") == 0)
							&&	(reader.NodeType == XmlNodeType.EndElement))
								break;
						}
					}
				}
				else if (String.Compare(reader.Name, "low") == 0)
				{
					if (Type == TimeType.Center)
						return 3;

					Type = TimeType.LowHigh;

                    try
                    {
                        ValOne = ParseDate(reader.GetAttribute("value"));
                    }
                    catch
                    {
						return 3;
                    }

					if (!reader.IsEmptyElement)
					{
						int depth = reader.Depth;

						while (reader.Read())
						{
							if ((depth == reader.Depth)
							&&	(string.Compare(reader.Name, "low") == 0)
							&&	(reader.NodeType == XmlNodeType.EndElement))
								break;
						}
					}
				}
				else if (String.Compare(reader.Name, "high") == 0)
				{
					if (Type == TimeType.Center)
						return 3;

					Type = TimeType.LowHigh;

                    try
                    {
                        ValTwo = ParseDate(reader.GetAttribute("value"));
                    }
                    catch
                    {
						return 3;
                    }

					if (!reader.IsEmptyElement)
					{
						int depth = reader.Depth;

						while (reader.Read())
						{
							if ((depth == reader.Depth)
							&&	(string.Compare(reader.Name, "high") == 0)
							&&	(reader.NodeType == XmlNodeType.EndElement))
								break;
						}
					}
				}
			}

			return -1;
		}

		public override int Write(XmlWriter writer)
		{
			if (!Works())
				return 0;

			writer.WriteStartElement(Name);

			switch (Type)
			{
				case TimeType.LowHigh:
					writer.WriteStartElement("low");
					writer.WriteAttributeString("value", WriteDate(ValOne));
					writer.WriteAttributeString("inclusive", "true");
					writer.WriteEndElement();

					writer.WriteStartElement("high");
					writer.WriteAttributeString("value", WriteDate(ValTwo));
					writer.WriteAttributeString("inclusive", "false");
					writer.WriteEndElement();
					break;
				case TimeType.Center:
					writer.WriteStartElement("center");
					writer.WriteAttributeString("value", WriteDate(ValOne));
					writer.WriteEndElement();
					break;
			}

			writer.WriteEndElement();

			return 0;
		}


		public override void Empty()
		{
			Type = TimeType.Empty;
			ValOne = DateTime.MinValue;
			ValTwo = DateTime.MinValue;

			base.Empty();
		}

		public override bool Works()
		{
			switch (Type)
			{
				case TimeType.Center:
					return (ValOne.Year > 1000);
				case TimeType.LowHigh:
					return (ValTwo.Year > 1000) && (ValOne.Year > 1000);
				default:
					break;
			}

			return false;
		}
		
		public enum TimeType
		{
			LowHigh,
			Center,
			Empty
		}
	}
}
