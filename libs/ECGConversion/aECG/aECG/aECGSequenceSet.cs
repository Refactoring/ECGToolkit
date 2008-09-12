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
	/// Summary description for aECGSequence.
	/// </summary>
	public sealed class aECGSequenceSet : aECGElement
	{
		private string _InnerName = "sequenceSet";

		private ArrayList _Sets = new ArrayList();
		public aECGControlVariableHolder[] ControlVariables = new aECGControlVariableHolder[128];

		public aECGSequenceSet() : base("component")
		{}

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
					if ((string.Compare(reader.Name, Name) == 0)
					||	(reader.NodeType == XmlNodeType.EndElement))
						return 0;

					if ((string.Compare(reader.Name, _InnerName) != 0)
					||	(reader.NodeType != XmlNodeType.Element)
					||	(reader.IsEmptyElement))
						return 1;

					sequence++;
				}
				else if (sequence == 1)
				{
					if (string.Compare(reader.Name, aECGSequence.SequenceName) == 0)
					{
						aECGSequence seq = new aECGSequence();

						int ret = seq.Read(reader);

						if (ret != 0)
							return (ret > 0) ? 1 + ret : ret;

						_Sets.Add(seq);
					}
					else if ((string.Compare(reader.Name, _InnerName) == 0)
						&&  (reader.NodeType == XmlNodeType.EndElement))
					{
						sequence--;
					}
					else
					{
						int ret = aECGElement.ReadOne(this, reader);

						if (ret != 0)
							return (ret > 0) ? 1 + ret : ret;
					}
				}
			}

			return -1;
		}

		public override int Write(XmlWriter writer)
		{
			if (!Works())
				return 1;

			writer.WriteStartElement(Name);
			writer.WriteStartElement(_InnerName);

			foreach (aECGSequence seq in _Sets)
			{
				int ret = seq.Write(writer);

				if (ret != 0)
					return (ret > 0) ? 1 + ret : ret;
			}

			aECGElement.WriteAll(this, writer);

			writer.WriteEndElement();
			writer.WriteEndElement();

			return 0;
		}

		public override bool Works()
		{
			return _Sets.Count != 0;
		}

		public override void Empty()
		{
			_Sets.Clear();

			base.Empty();
		}

		public int Count
		{
			get
			{
				return _Sets.Count;
			}
		}

		public aECGSequence this[int i]
		{
			get
			{
				if (_Sets.Count == i)
					_Sets.Add(new aECGSequence());
				else if (_Sets.Count <= i)
					return null;

				return (aECGSequence) _Sets[i];
			}
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

		public aECGControlVariableHolder this[string varName]
		{
			get
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
		}
	}
}