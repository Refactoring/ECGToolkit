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
	interface IaECGAnnotationHolder
	{
		aECGAnnotation[] getAnnotations(string code);
		bool Add(aECGAnnotation ann);
		aECGAnnotation this[string code] {get;}
	}

	public sealed class aECGAnnotationSet : aECGElement, IaECGAnnotationHolder
	{
		private string _InnerName = null;

		public aECGCode Code = new aECGCode();
		public aECGValuePair ActivityTime = new aECGValuePair("activityTime");
		public aECGCode MethodCode = new aECGCode("methodCode");
		public aECGAuthor Author = new aECGAuthor("assignedEntity");
		public aECGControlVariable[] ControlVariable = new aECGControlVariable[128];
		public aECGAnnotation[] Annotation = new aECGAnnotation[128];

		public aECGAnnotationSet() : base("subjectOf")
		{
			_InnerName = "annotationSet";
		}

		public override int Read(System.Xml.XmlReader reader)
		{
			int sequence = 0;

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
						return 1;

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
			return Code.Works()
				|| ActivityTime.Works()
				|| MethodCode.Works()
				|| ((Annotation[0] != null)
				&&	(Annotation[0].Works()));
		}

		public aECGAnnotation[] getAnnotations(string code)
		{
			ArrayList al = new ArrayList();

			foreach(aECGAnnotation ann in Annotation)
			{
				if (ann == null)
					break;

				if (string.Compare(code, ann.Code.Code) == 0)
					al.Add(ann);
				else
				{
					aECGAnnotation[] annList = ann.getAnnotations(code);

					if (annList != null)
						al.AddRange(annList);
				}
			}

			if (al.Count > 0)
			{
				aECGAnnotation[] ret = new aECGAnnotation[al.Count];

				for (int i=0;i < ret.Length;i++)
					ret[i] = (aECGAnnotation) al[i];

				return ret;
			}

			return null;
		}

		public bool Add(aECGAnnotation ann)
		{
			for (int i=0;i < Annotation.Length;i++)
			{
				if (Annotation[i] == null)
				{
					Annotation[i] = ann;

					return true;
				}
				else if (Annotation[i] == ann)
				{
					return true;
				}
			}

			return false;
		}

		public aECGAnnotation this[string code]
		{
			get
			{
				for (int i=0;i < Annotation.Length;i++)
				{
					if (Annotation[i] == null)
						break;

					if (string.Compare(code, Annotation[i].Code.Code) == 0)
						return Annotation[i];
					else
					{
						aECGAnnotation ann = Annotation[i][code];

						if (ann != null)
							return ann;
					}
				}

				return null;
			}
		}
	}
}
