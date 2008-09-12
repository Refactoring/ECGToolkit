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
	public sealed class aECGControlVariable : aECGElement
	{
		public const string ControlVariableName = "controlVariable";

		private string _InnerName = null;

		public aECGCode Code = new aECGCode();
		public aECGControlVariable[] InnerVariables;
		public aECGValuePair Value = null;

		public aECGControlVariable() : base(ControlVariableName)
		{
			_InnerName = "component";

			InnerVariables = new aECGControlVariable[2];
		}

		public aECGControlVariable(string name) : base(name)
		{
			_InnerName = ControlVariableName;

			Value = new aECGValuePair("value");
		}

		public override int Read(System.Xml.XmlReader reader)
		{
			while (reader.Read())
			{
				if ((reader.NodeType == XmlNodeType.Comment)
				||  (reader.NodeType == XmlNodeType.Whitespace))
					continue;

				if ((string.Compare(reader.Name, Name) == 0)
				&&  (reader.NodeType == XmlNodeType.EndElement))
					return 0;

				int ret = 0;

				if ((InnerVariables != null)
				&&	(string.Compare(reader.Name, _InnerName) == 0))
				{
					aECGControlVariable var = new aECGControlVariable(_InnerName);

					ret = var.Read(reader);

					if (ret == 0)
					{
						for (int i=0;i < InnerVariables.Length;i++)
						{
							if (InnerVariables[i] == null)
							{
								InnerVariables[i] = var;
								break;
							}
						}
					}
				}
				else
				{
					ret = aECGElement.ReadOne(this, reader);
				}

				if (ret != 0)
					return (ret > 0) ? 2 + ret : ret;
			}

			return -1;
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
			return Code.Works();
		}
	}
}
