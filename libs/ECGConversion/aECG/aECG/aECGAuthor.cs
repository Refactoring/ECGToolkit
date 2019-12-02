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
	public sealed class aECGAuthor : aECGElement
	{
		private string _InnerName = null;

		public aECGId Id = new aECGId();
		public aECGDevice Device = null;
		public aECGAssignedAuthorType AssignedAuthorType = null;
		public aECGOrganization Organization = null;

		public aECGAuthor(string name) : base("author")
		{
			_InnerName = name;

			if (string.Compare(name, "seriesAuthor") == 0)
			{
				Device = new aECGDevice();
				Organization = new aECGOrganization();
			}
			else if (string.Compare(name, "assignedEntity") == 0)
			{
				AssignedAuthorType = new aECGAssignedAuthorType();
				Organization = new aECGOrganization("representedAuthoringOrganization");
			}
		}

		public override int Read(System.Xml.XmlReader reader)
		{
			int sequence = 0;

            if ((reader.NodeType == XmlNodeType.Element)
            &&  reader.IsEmptyElement)
                return 0;

			while (reader.Read())
			{
				if ((reader.NodeType == XmlNodeType.Comment)
				||  (reader.NodeType == XmlNodeType.Whitespace))
					continue;

				if (sequence == 0)
				{
					if ((string.Compare(reader.Name, _InnerName) != 0)
					||	(reader.NodeType != XmlNodeType.Element)
					||	(reader.IsEmptyElement))
					{
						if (string.Compare(_InnerName, "seriesAuthor") == 0)
						{
							if ((string.Compare(reader.Name, Name) == 0)					
							&&  (reader.NodeType == XmlNodeType.EndElement))
							{
								_InnerName = null;

								return 0;
							}

							int ret = aECGElement.ReadOne(this, reader);

							if (ret != 0)
								return (ret > 0) ? 2 + ret : ret;

							continue;
						}
						else
						{
							return 1;
						}
					}

					sequence++;
				}
				else if (sequence == 1)
				{
					int ret = aECGElement.ReadOne(this, reader);

					if (ret != 0)
						return (ret > 0) ? 2 + ret : ret;

					if ((string.Compare(reader.Name, _InnerName) == 0)
					&&	(reader.NodeType == XmlNodeType.EndElement))
						sequence++;
				}
				else
				{
					if ((string.Compare(reader.Name, Name) != 0)					
					||  (reader.NodeType != XmlNodeType.EndElement)
					||  reader.IsEmptyElement)
						return 2;

					return 0;
				}
			}

			return -1;
		}

		public override int Write(XmlWriter writer)
		{
			if (!Works())
				return 0;

			writer.WriteStartElement(Name);
			writer.WriteStartElement(_InnerName);

			aECGElement.WriteAll(this, writer);

			writer.WriteEndElement();
			writer.WriteEndElement();

			return 0;
		}

		public override bool Works()
		{
			if ((Device != null)
			&&	!Device.Works())
				return false;

			if ((AssignedAuthorType != null)
			&&	!AssignedAuthorType.Works())
				return false;

			if ((Organization != null)
			&&	!Organization.Works())
				return false;

			return true;
		}

		public void Set(aECGAuthor author)
		{
			if (author == null)
				return;

			if ((author.Device != null)
			&&	(this.Device != null)
			&&	(author.Organization != null)
			&&	(this.Organization != null)
			&&	(author.AssignedAuthorType == null)
			&&	(this.AssignedAuthorType == null))
			{
				Id.Extension = author.Id.Extension;
				Id.Root = author.Id.Root;

				Device.Code.Code = author.Device.Code.Code;
				Device.Code.CodeSystem = author.Device.Code.CodeSystem;
				Device.Code.CodeSystemName = author.Device.Code.CodeSystemName;
				Device.Code.DisplayName = author.Device.Code.DisplayName;

				Device.Id.Extension = author.Device.Id.Extension;
				Device.Id.Root = author.Device.Id.Root;

				Device.manufacturerModelName = author.Device.manufacturerModelName;
				Device.softwareName = author.Device.softwareName;

				Organization.Id.Extension = author.Organization.Id.Extension;
				Organization.Id.Root = author.Organization.Id.Root;

				Organization.name = author.Organization.name;
			}
			else if ((author.Device == null)
				&&	(this.Device == null)
				&&	(author.Organization != null)
				&&	(this.Organization != null)
				&&	(author.AssignedAuthorType != null)
				&&	(this.AssignedAuthorType != null))
			{
				Id.Extension = author.Id.Extension;
				Id.Root = author.Id.Root;

				AssignedAuthorType.Set(author.AssignedAuthorType);

				Organization.Set(author.Organization);
			}
		}
	}
}
