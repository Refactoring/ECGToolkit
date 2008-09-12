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
using System.Collections;
using System.Xml;

namespace ECGConversion.aECG
{
	/// <summary>
	/// Summary description for aECGComponent.
	/// </summary>
	public sealed class aECGComponent : aECGElement
	{
		private ArrayList _Series = new ArrayList();
		private string _InnerName = null;

		public aECGComponent() : base("component")
		{
		}

		public aECGComponent(string Name, string innerName) : base(Name)
		{
            _InnerName = innerName;
		}

		public override int Read(System.Xml.XmlReader reader)
		{
			while (reader.Read())
			{
				if ((reader.NodeType == XmlNodeType.Comment)
				||  (reader.NodeType == XmlNodeType.Whitespace))
					continue;

				if (String.Compare(reader.Name, Name) == 0)
				{
					if (reader.NodeType == XmlNodeType.EndElement)
						return 0;
					else
						return 1;
				}

				if (_InnerName == null)
				{
					if (String.Compare(reader.Name, aECGSeries.SeriesName) == 0)
					{
						aECGSeries series = new aECGSeries();

						int ret = series.Read(reader);

						if (ret != 0)
							return (ret > 0) ? 1 + ret : ret;

						_Series.Add(series);
					}
				}
				else
				{
					if (String.Compare(reader.Name, _InnerName) == 0)
					{
						aECGSeries series = new aECGSeries(_InnerName);

						int ret = series.Read(reader);

						if (ret != 0)
							return (ret > 0) ? 1 + ret : ret;

						_Series.Add(series);
					}
				}
			}

			return -1;
		}

		public override int Write(System.Xml.XmlWriter writer)
		{
			if (_Series.Count != 0)
			{
				writer.WriteStartElement(Name);

				foreach (aECGSeries series in _Series)
				{
					int ret = series.Write(writer);

					if (ret != 0)
						return ret;
				}

				writer.WriteEndElement();
			}

			return 0;
		}

		public override bool Works()
		{
			foreach (aECGSeries series in _Series)
				if (!series.Works())
					return false;

			return true;
		}

		public override void Empty()
		{
			_Series.Clear();

			base.Empty();
		}

		public int Count
		{
			get
			{
				return _Series.Count;
			}
		}

		public aECGSeries this[int i]
		{
			get
			{
				if (_Series.Count == i)
				{
					if (_InnerName == null)
						_Series.Add(new aECGSeries());
					else
						_Series.Add(new aECGSeries(_InnerName));
				}
				else if (_Series.Count <= i)
					return null;

				return (aECGSeries) _Series[i];
			}
		}
	}
}
