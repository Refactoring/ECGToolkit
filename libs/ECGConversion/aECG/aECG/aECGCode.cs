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
	public sealed class aECGCode : aECGElement
	{
		public string Code;
		public string CodeSystem;
		public string CodeSystemName;
		public string DisplayName;

		private bool _Must = false;

		public aECGCode() : base("code")
		{}

		public aECGCode(bool must) : base("code")
		{
			_Must = must;
		}

		public aECGCode(string name) : base(name)
		{}

		public aECGCode(string name, bool must) : base(name)
		{
			_Must = must;
		}

		public override int Read(XmlReader reader)
		{
			if (!reader.IsEmptyElement)
				return 1;

			Code = reader.GetAttribute("code");
			CodeSystem = reader.GetAttribute("codeSystem");
			CodeSystemName = reader.GetAttribute("codeSystemName");
			DisplayName = reader.GetAttribute("displayName");

			return 0;
		}

		public override int Write(XmlWriter writer)
		{
			if (!Works())
				return 0;

			writer.WriteStartElement(Name);

			if (Code != null)
				writer.WriteAttributeString("code", Code);

            if (CodeSystem != null)
			    writer.WriteAttributeString("codeSystem", CodeSystem);

			if (CodeSystemName != null)
				writer.WriteAttributeString("codeSystemName", CodeSystemName);

			if (DisplayName != null)
				writer.WriteAttributeString("displayName", DisplayName);

			writer.WriteEndElement();

			return 0;
		}

		public override bool Works()
		{
			return	(Code != null)
				||	_Must;
		}

		public override void Empty()
		{
			Code = null;
			CodeSystem = null;
			CodeSystemName = null;
			DisplayName = null;
		}
	}
}