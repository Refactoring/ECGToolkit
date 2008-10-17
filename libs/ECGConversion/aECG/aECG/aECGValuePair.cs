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
	public sealed class aECGValuePair : aECGElement
	{
		public string Type;
		public object Value
		{
			get
			{
				if (_Time.Year > 1000)
					return _Time;
				if (_InnerValue != null)
					return _InnerValue;
				return _Value;
			}
			set
			{
				if (value.GetType() == typeof(string))
				{
					_InnerValue = (string) value;
					_Time = DateTime.MinValue;
					_Value = Double.NaN;
				}
				else if (value.GetType() == typeof(DateTime))
				{
					_InnerValue = null;
					_Time = (DateTime) value;
					_Value = Double.NaN;
				}
				else if (value.GetType() == typeof(double))
				{
					_InnerValue = null;
					_Time = DateTime.MinValue;
					_Value = (double) value;
				}
				else if (value.GetType() == typeof(float))
				{
					_InnerValue = null;
					_Time = DateTime.MinValue;
					_Value = (float) value;
				}
			}
		}
		public string Unit;

		public string Code;
		public string CodeSystem;
		public string CodeSystemName;
		public string DisplayName;

        private DateTime _Time;
		private double _Value;
		private string _InnerValue;

		private System.Collections.SortedList _InnerList = new System.Collections.SortedList();

		public aECGValuePair this[string name]
		{
			get
			{
				if (_InnerList.ContainsKey(name))
					return (aECGValuePair) _InnerList[name];
				return null;
			}
			set
			{
				if (_InnerList.ContainsKey(name))
					_InnerList[name] = value;
				else
					_InnerList.Add(name, value);
			}
		}

		public aECGValuePair(string name) : base(name)
		{
			Empty();
		}

		public aECGValuePair(string name, double val, string unit) : base(name)
		{
			Empty();

			_Value = val;
			Unit = unit;
		}

		public override int Read(XmlReader reader)
		{
			Type = reader.GetAttribute("xsi:type");

			Unit = reader.GetAttribute("unit");

			string val = reader.GetAttribute("value");

			if (val != null)
			{
				if (Unit != null)
				{
					try
					{
						_Value = (val.Length == 0) ? 0.0 : double.Parse(val, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
					}
					catch
					{
						return 2;
					}
				}
				else
				{
					try
					{
						_Time = aECGTime.ParseDate(val);
					}
					catch
					{
						return 2;
					}
				}
			}
			else
			{
				Code = reader.GetAttribute("code");
				CodeSystem = reader.GetAttribute("codeSystem");
				CodeSystemName = reader.GetAttribute("codeSystemName");
				DisplayName = reader.GetAttribute("displayName");
			}

			if (!reader.IsEmptyElement)
			{
				reader.Read();

				if ((reader.Name == Name)
				&&  (reader.NodeType == XmlNodeType.EndElement))
					return 0;
				else if (reader.NodeType != XmlNodeType.Element)
				{
					_InnerValue = reader.Value;

					reader.Read();
				}

				if (reader.NodeType == XmlNodeType.Element)
				{
					do
					{
						if ((reader.NodeType == XmlNodeType.Comment)
						||  (reader.NodeType == XmlNodeType.Whitespace))
							continue;

						if ((string.Compare(reader.Name, Name) == 0)
						&&  (reader.NodeType == XmlNodeType.EndElement))
							break;

						if (reader.NodeType == XmlNodeType.Element)
						{
							aECGValuePair temp = new aECGValuePair(reader.Name);

							int ret = temp.Read(reader);

							if (ret != 0)
								return ret > 0 ? 3 + ret : ret;

							_InnerList.Add(temp.Name, temp);
						}
					} while (reader.Read());

					if (_InnerList.Count != 0)
						_InnerValue = null;
				}
				else if ((reader.Name != Name)
					||	 (reader.NodeType != XmlNodeType.EndElement))
					return 1;
			}

			return 0;
		}

		public override int Write(XmlWriter writer)
		{
			if (!Works())
				return 0;

			writer.WriteStartElement(Name);

			if (Type != null)
				writer.WriteAttributeString("xsi:type", Type);

			if (_InnerValue != null)
			{
				writer.WriteString(_InnerValue);
			}
			else
			{
				if (Unit != null)
				{
					writer.WriteAttributeString("value", _Value.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat));
					writer.WriteAttributeString("unit", Unit);
				}
				else if (_Time.Year > 1000)
				{
					writer.WriteAttributeString("value", aECGTime.WriteDate(_Time));
				}
				else if ((Code != null)
					&&	 (CodeSystem != null))
				{
					writer.WriteAttributeString("code", Code);
					writer.WriteAttributeString("codeSystem", CodeSystem);

					if (CodeSystemName != null)
						writer.WriteAttributeString("codeSystemName", CodeSystemName);

					if (DisplayName != null)
						writer.WriteAttributeString("displayName", DisplayName);
				}
			}

			foreach (aECGValuePair vp in _InnerList.Values)
				vp.Write(writer);

			writer.WriteEndElement();

			return 0;
		}

		public override bool Works()
		{
			foreach (aECGValuePair vp in _InnerList.Values)
				if (!vp.Works())
					return false;

			return	((!double.IsNaN(_Value))
				&&	 (Unit != null))
                ||  (_Time.Year >= 1000)
				||	(_InnerValue != null)
				||	((Code != null)
				&&	 (CodeSystem != null))
				||	(_InnerList.Count > 0);
		}

		public override void Empty()
		{
			Type = null;
			_Value = double.NaN;
			Unit = null;
            _Time = DateTime.MinValue;
			_InnerValue = null;

			base.Empty();
		}

		public void Set(aECGValuePair vp)
		{
			this.Type = vp.Type;
			this.Unit = vp.Unit;
			this.Value = vp.Value;
			this.Code = vp.Code;
			this.CodeSystem = vp.CodeSystem;
			this.CodeSystemName = vp.CodeSystemName;
			this.DisplayName = vp.DisplayName;
		}
	}
}
