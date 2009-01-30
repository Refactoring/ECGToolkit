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
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace ECGConversion.aECG
{
	/// <summary>
	/// Summary description for aECGValue.
	/// </summary>
	public sealed class aECGValue : aECGElement
	{
		private static char[] _splitChars = {' ', '\n', '\t'};

		public string Type;
		public aECGValuePair Head = new aECGValuePair("head");
		public aECGValuePair Increment = new aECGValuePair("increment");
		public aECGValuePair Origin = new aECGValuePair("origin");
		public aECGValuePair Scale = new aECGValuePair("scale");
		public aECGValuePair High = new aECGValuePair("high");
		public aECGValuePair Low = new aECGValuePair("low");

		public short[] Digits;

		public aECGValue() : base("value")
		{
		}

		public static string[] SplitValues(string val, string valPattern)
		{
			string[] ret = null;

			MatchCollection mc = Regex.Matches(val, valPattern, RegexOptions.None);

			if (mc.Count != 0)
			{
				ret = new string[mc.Count];

				for (int i=0;i < mc.Count;i++)
					ret[i] = val.Substring(mc[i].Index, mc[i].Length);
			}

			return ret;
		}

		public override int Read(System.Xml.XmlReader reader)
		{
			if (reader.IsEmptyElement)
				return 1;

			while (reader.Read())
			{
                if ((reader.NodeType == XmlNodeType.Comment)
                ||  (reader.NodeType == XmlNodeType.Whitespace))
                    continue;

				if ((string.Compare(reader.Name, "digits") == 0)
				&&  (reader.NodeType == XmlNodeType.Element)
				&&	!reader.IsEmptyElement)
				{
					reader.Read();

                    int i = 0;

					string[] temp = null;

					try
					{
						temp = SplitValues(reader.Value, "-?[0-9]+");

						Digits = new short[temp.Length];

						for (;i < Digits.Length;i++)
							Digits[i] = short.Parse(temp[i], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
					}
					catch
					{
						return 3;
					}

					reader.Read();
				}
				else if (reader.IsEmptyElement)
				{
					int ret = aECGElement.ReadOne(this, reader);

					if (ret != 0)
						return (ret > 0) ? 3 + ret : ret;
				}
				else if ((string.Compare(reader.Name, Name) == 0)				
					&&   (reader.NodeType == XmlNodeType.EndElement))
				{
					return 0;
				}
				else
				{
					break;
				}
			}

			return 2;
		}

		public override int Write(XmlWriter writer)
		{
			if (!Works())
				return 0;

			writer.WriteStartElement(Name);

			if (Type != null)
				writer.WriteAttributeString("xsi:type", Type);

			aECGElement.WriteAll(this, writer);

			if (Digits != null)
			{
				StringBuilder sb = new StringBuilder();

				foreach (short val in Digits)
				{
					sb.Append(val.ToString());
					sb.Append(" ");
				}

				if (sb.Length > 0)
				{
					writer.WriteStartElement("digits");
					writer.WriteString(sb.ToString());
					writer.WriteEndElement();
				}
			}

			writer.WriteEndElement();

			return 0;
		}

		public override bool Works()
		{
			int i=0;

			if (Head.Works()) i++;
			if (Increment.Works()) i++;
			if (Origin.Works()) i++;
			if (Scale.Works()) i++;
			if (High.Works()) i++;
			if (Low.Works()) i++;

			return (i >= 1);
		}

		public override void Empty()
		{
			Type = null;
			Digits = null;

			base.Empty();
		}
	}
}
