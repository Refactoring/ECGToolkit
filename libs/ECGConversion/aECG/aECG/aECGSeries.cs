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
	/// Summary description for aECGSeries.
	/// </summary>
	public sealed class aECGSeries : aECGElement
	{
		public const string SeriesName = "series";

		public aECGId Id = new aECGId();
		public aECGCode Code = new aECGCode();
		public aECGTime EffectiveTime = new aECGTime();
		public aECGAuthor SeriesAuthor = new aECGAuthor("seriesAuthor");
		public aECGControlVariableHolder[] ControlVariables = new aECGControlVariableHolder[128];
		public aECGSupportingROI SupportingROI = new aECGSupportingROI();
		public aECGSequenceSet SequenceSet = new aECGSequenceSet();
		public aECGAnnotationSet[] Annotation = new aECGAnnotationSet[128];
		public aECGComponent DerivedSet = null;

		public aECGSeries() : base(SeriesName)
		{
			DerivedSet = new aECGComponent("derivation", "derivedSeries");
		}

		public aECGSeries(string name) : base(name)
		{
		}

		public override int Read(XmlReader reader)
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

				int ret = aECGElement.ReadOne(this, reader);

				if (ret != 0)
					return (ret > 0) ? 1 + ret : ret;
			}

			return -1;
		}

		public override int Write(XmlWriter writer)
		{
			writer.WriteStartElement(Name);

			int ret = aECGElement.WriteAll(this, writer);

			if (ret != 0)
				return ret;

			writer.WriteEndElement();

			return 0;
		}

		public override bool Works()
		{
			return Id.Works()
				&& Code.Works()
				&& EffectiveTime.Works()
				&& SequenceSet.Works()
				&& ((DerivedSet == null)
				||	DerivedSet.Works());
		}

		public bool Add(aECGControlVariableHolder var)
		{
			for (int i=0;i < ControlVariables.Length;i++)
			{
				if (ControlVariables[i] == null)
				{
					ControlVariables[i] = var;
					return true;
				}
				else if (ControlVariables[i].Code.Code == var.Code.Code)
				{
					ControlVariables[i] = var;
					return true;
				}
			}

			return false;
		}

		public bool Add(aECGAnnotationSet aset)
		{
			for (int i=0;i < Annotation.Length;i++)
			{
				if (Annotation[i] == null)
				{
					Annotation[i] = aset;
					return true;
				}
				else if (Annotation[i] == aset)
				{
					return true;
				}
			}

			return false;
		}

		public aECGControlVariableHolder getControlVariable(string varName)
		{
			foreach (aECGControlVariableHolder var in ControlVariables)
			{
				if (var == null)
					return null;
				else if ((var.Code != null)
					&&   (var.Code.Code == varName))
					return var;
			}

			return null;
		}

		public aECGAnnotation[] getAnnotations(string code)
		{
			ArrayList al = new ArrayList();

			foreach(aECGAnnotationSet ann in Annotation)
			{
				if (ann == null)
					break;

				aECGAnnotation[] annList = ann.getAnnotations(code);

				if (annList != null)
					al.AddRange(annList);
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

		public aECGAnnotation getAnnotation(string code)
		{
			foreach (aECGAnnotationSet aset in Annotation)
			{
                if (aset == null)
                    return null;

				aECGAnnotation ann = aset[code];

				if (ann != null)
					return ann;
			}

			return null;
		}

		public aECGAnnotationSet getAnnotationSet(string[] code)
		{
			if (code == null)
				return null;

			foreach (string c in code)
			{
				aECGAnnotationSet anns = getAnnotationSet(c);

				if (anns != null)
					return anns;
			}

			return null;
		}

		public aECGAnnotationSet getAnnotationSet(string code)
		{
			foreach (aECGAnnotationSet aset in Annotation)
			{
                if (aset == null)
                    return null;
				if (string.Compare(aset.Code.Code, code) == 0)
					return aset;

				aECGAnnotation ann = aset[code];

				if (ann != null)
					return aset;
			}

			return null;
		}
	}
}
