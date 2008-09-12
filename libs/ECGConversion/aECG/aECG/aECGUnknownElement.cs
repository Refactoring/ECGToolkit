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
	public sealed class aECGUnknownElement : aECGElement
	{
		public SortedList Attributes = new SortedList();
		public string Value = null;

		public aECGUnknownElement(string name) : base(name)
		{
			Empty();
		}

		public override int Read(XmlReader reader)
		{
            int depth = reader.Depth;

			bool isEmpty = reader.IsEmptyElement;

			if (reader.HasAttributes)
			{
				try
				{
					for (int i=0;i < reader.AttributeCount;i++)
					{
						reader.MoveToAttribute(i);

						Attributes.Add(reader.Name, reader.Value);
					}
				}
				catch
				{
					return -2;
				}
			}

			if (isEmpty)
				return 0;

			while (reader.Read())
			{
				if ((reader.NodeType == XmlNodeType.Comment)
				||  (reader.NodeType == XmlNodeType.Whitespace))
					continue;

				if (reader.NodeType == XmlNodeType.Text)
				{
					Value = reader.Value;

					continue;
				}

				if ((reader.Depth == depth)
				&&	(String.Compare(reader.Name, Name) == 0))
				{
					if (reader.NodeType == XmlNodeType.EndElement)
						return 0;
					else
						return 3;
				}

				if (Value != null)
					return 4;

				int ret = aECGElement.ReadOne(this, reader);

				if (ret != 0)
					return (ret > 0 ? 4 + ret : ret);
			}

			return -1;
		}

		public override int Write(XmlWriter writer)
		{
			if (!Works())
				return 0;

			writer.WriteStartElement(Name);

			for (int i=0;i < Attributes.Count;i++)
				writer.WriteAttributeString((string) Attributes.GetKey(i), (string) Attributes.GetByIndex(i));

			if (Value != null)
				writer.WriteString(Value);
			else
				aECGElement.WriteAll(this, writer);

			writer.WriteEndElement();

			return 0;
		}

		public override bool Works()
		{
			return true;
		}

		public override void Empty()
		{
			Attributes.Clear();
			Value = null;

			base.Empty();
		}
	}
}