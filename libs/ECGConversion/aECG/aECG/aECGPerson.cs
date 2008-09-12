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
	public sealed class aECGPerson : aECGElement
	{
		public aECGName PersonName = new aECGName();
		public aECGCode AdministrativeGenderCode = new aECGCode("administrativeGenderCode", true);
		public aECGValuePair BirthTime = new aECGValuePair("birthTime");
		public aECGCode RaceCode = new aECGCode("raceCode", true);

		public aECGPerson(string name) : base(name)
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
			return PersonName.Works()
				|| AdministrativeGenderCode.Works()
				|| BirthTime.Works()
				|| RaceCode.Works();
		}

		public void Set(aECGPerson pers)
		{
			this.PersonName.family = pers.PersonName.family;
			this.PersonName.given = pers.PersonName.given;
			this.PersonName.prefix = pers.PersonName.prefix;
			this.PersonName.suffix = pers.PersonName.suffix;

			this.AdministrativeGenderCode.Code = pers.AdministrativeGenderCode.Code;
			this.AdministrativeGenderCode.CodeSystem = pers.AdministrativeGenderCode.CodeSystem;
			this.AdministrativeGenderCode.CodeSystemName = pers.AdministrativeGenderCode.CodeSystemName;
			this.AdministrativeGenderCode.DisplayName = pers.AdministrativeGenderCode.DisplayName;

			this.BirthTime.Set(pers.BirthTime);

			this.RaceCode.Code = pers.RaceCode.Code;
			this.RaceCode.CodeSystem = pers.RaceCode.CodeSystem;
			this.RaceCode.CodeSystemName = pers.RaceCode.CodeSystemName;
			this.RaceCode.DisplayName = pers.RaceCode.DisplayName;
		}
	}
}