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
	public sealed class aECGId : aECGElement
	{
		public string Root;
		public string Extension;
		private bool _Must = false;

		public aECGId() : base("id")
		{}

		public aECGId(bool must) : base("id")
		{
			_Must = must;
		}

		public override int Read(XmlReader reader)
		{
			Root = reader.GetAttribute("root");
			Extension = reader.GetAttribute("extension");

			if (!reader.IsEmptyElement)
			{
				int depth = reader.Depth;

				while (reader.Read())
				{
					if ((depth == reader.Depth)
					&&	(string.Compare(reader.Name, Name) == 0)
					&&	(reader.NodeType == XmlNodeType.EndElement))
						break;
				}
			}

			return 0;
		}

		public override int Write(XmlWriter writer)
		{
			if (!Works())
				return 0;

			writer.WriteStartElement(Name);

			if (Root != null)
				writer.WriteAttributeString("root", Root);

			if (Extension != null)
				writer.WriteAttributeString("extension", Extension);

			writer.WriteEndElement();

			return 0;
		}

		public override bool Works()
		{
            return (Root != null)
				|| (Extension != null)
				|| _Must;
		}
		
		public override void Empty()
		{
			Root = null;
			Extension = null;

			base.Empty();
		}

	}
}
