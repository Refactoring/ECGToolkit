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
	public sealed class aECGLocation : aECGElement
	{
		public string name;
		public aECGAddress Address = new aECGAddress();

		public aECGLocation() : base("location")
		{}

		public override int Read(XmlReader reader)
		{
			if (reader.IsEmptyElement)
				return 0;

			int ret = 0;

			while (reader.Read())
			{
				if ((reader.NodeType == XmlNodeType.Comment)
				||  (reader.NodeType == XmlNodeType.Whitespace))
					continue;

				if (String.Compare(reader.Name, Name) == 0)
				{
					if (reader.NodeType == XmlNodeType.EndElement)
						break;
					else
						return 3;
				}

				ret = aECGElement.ReadOne(this, reader);

				if (ret != 0)
					break;
			}

			return ret;
		}

		public override int Write(XmlWriter writer)
		{
			if (!Works())
				return 0;

			writer.WriteStartElement(Name);

			aECGElement.WriteAll(this, writer);

			writer.WriteEndElement();

			return 0;
		}

		public override bool Works()
		{
			return	(name != null)
				&&	(Address.Works());
		}
	}
}