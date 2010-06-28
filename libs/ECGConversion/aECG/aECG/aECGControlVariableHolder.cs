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
using System.Reflection;

namespace ECGConversion.aECG
{
	public sealed class aECGControlVariableHolder : aECGElement
	{
		public const string ControlVariableHolderName = "controlVariable";

		public aECGControlVariable ControlVariable;
		public aECGRelatedObservation RelatedObservation;
		public aECGTransactionType TransactionType;

		public aECGCode Code
		{
			get
			{
				if (!Works())
					return null;

				if (ControlVariable != null)
					return ControlVariable.Code;
				else if (RelatedObservation != null)
					return RelatedObservation.Code;

				return null;
			}
		}

		public aECGControlVariableHolder() : base(ControlVariableHolderName)
		{
		}

		public aECGControlVariableHolder(aECGControlVariable var) : base(ControlVariableHolderName)
		{
			ControlVariable = var;
		}

		public aECGControlVariableHolder(aECGRelatedObservation robs) : base(ControlVariableHolderName)
		{
			RelatedObservation = robs;
		}

		public aECGControlVariableHolder(aECGTransactionType tt) : base(ControlVariableHolderName)
		{
			TransactionType = tt;
		}

		public override int Read(System.Xml.XmlReader reader)
		{
            int depth = reader.Depth;

			while (reader.Read())
			{
				int ret = 0;

				if ((reader.NodeType == XmlNodeType.Comment)
				||  (reader.NodeType == XmlNodeType.Whitespace))
					continue;

				if ((string.Compare(reader.Name, Name) == 0)
				&&	(reader.NodeType == XmlNodeType.EndElement)
				&&	(reader.Depth == depth))
				{
					return 0;
				}
				else if (string.Compare(reader.Name, aECGControlVariable.ControlVariableName) == 0)
				{
					aECGControlVariable var = new aECGControlVariable();

					ret = var.Read(reader);

					if (ret == 0)
						ControlVariable = var;
				}
				else if (string.Compare(reader.Name, aECGRelatedObservation.RelatedObservationName) == 0)
				{
					aECGRelatedObservation robs = new aECGRelatedObservation();

					ret = robs.Read(reader);

					if (ret == 0)
						RelatedObservation = robs;
				}
				else if (string.Compare(reader.Name, aECGRelatedObservation.RelatedObservationName) == 0)
				{
					aECGTransactionType tt = new aECGTransactionType();

					ret = tt.Read(reader);

					if (ret == 0)
						TransactionType = tt;
				}
				else
				{
					ret = aECGElement.ReadOne(this, reader);
				}

				if (ret != 0)
					return ret > 0 ? 1 + ret : ret;
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
			int check=0;

			foreach (FieldInfo fieldInfo in this.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				try
				{
					if (fieldInfo.FieldType.IsSubclassOf(typeof(aECGElement)))
					{
						aECGElement temp = (aECGElement) fieldInfo.GetValue(this);

						if (temp != null)
							check++;
					}
				}
				catch {}
			}
				
			return check == 1;
		}
	}
}
