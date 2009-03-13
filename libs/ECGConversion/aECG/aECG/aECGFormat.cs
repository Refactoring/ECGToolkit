/***************************************************************************
Copyright 2008-2009, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml;
#if !WINCE
using System.Xml.XPath;
using System.Xml.Xsl;
#endif

using Communication.IO.Tools;

using ECGConversion;
using ECGConversion.ECGDemographics;
using ECGConversion.ECGDiagnostic;
using ECGConversion.ECGGlobalMeasurements;
using ECGConversion.ECGSignals;

namespace ECGConversion.aECG
{
	/// <summary>
	/// Summary description for aECGFormat.
	/// </summary>
	public sealed class aECGFormat : IECGFormat, ISignal, IDemographic, IDiagnostic, IGlobalMeasurement
	{
		public System.Text.Encoding Encoding
		{
			get
			{
				try
				{
					return System.Text.Encoding.GetEncoding(_Config["Encoding"]);
				}
				catch {}

				return null;
			}
			set
			{
				_Config["Encoding"] = value.WebName;
			}
		}
		public const string Name = "AnnotatedECG";

		public aECGId Id = new aECGId();
		public aECGCode Code = new aECGCode();
		public string text = null;
		public aECGTime EffectiveTime = new aECGTime();
		public aECGCode ConfidentialityCode = new aECGCode("confidentialityCode", true);
		public aECGCode ReasonCode = new aECGCode("reasonCode");
		public aECGTimepointEvent TimepointEvent = new aECGTimepointEvent();
		public aECGSite TestingSite = new aECGSite("testingSite");
		public aECGControlVariableHolder[] ControlVariables = new aECGControlVariableHolder[128];
		public aECGSubjectFindingComment SubjectFindingComment = new aECGSubjectFindingComment();
		public aECGComponent Component = new aECGComponent();
		public ArrayList UnknownElements = new ArrayList();

		private string _OverreadingPhysician = null;

		public aECGFormat()
		{
			string[] mustValue = {"Encoding"};

			_Config = new ECGConfig(mustValue, null, new ECGConversion.ECGConfig.CheckConfigFunction(this._ConfigurationWorks));
			_Config["Encoding"] = "UTF-8";
		}

		private bool _ConfigurationWorks()
		{
			try
			{
				System.Text.Encoding enc = System.Text.Encoding.GetEncoding(_Config["Encoding"]);

				return enc != null;
			}
			catch {}

			return false;
		}

		public int Read(XmlTextReader reader)
		{
			if (!CheckFormat(reader))
				return 2;

			Encoding = reader.Encoding;

			string schemaLocation = reader.GetAttribute("xsi:schemaLocation");

			if (schemaLocation != null)
			{
				switch (GetFileName(schemaLocation))
				{
#if !WINCE
					case "PORI_MT020001.xsd":
						try
						{
							XmlUrlResolver resolver = new XmlUrlResolver();
							resolver.Credentials = System.Net.CredentialCache.DefaultCredentials; 

							MemoryStream ms = new MemoryStream(5 * 1024 * 1024);
					
#if NET_1_1
							XslTransform xslTrans = new XslTransform();

							XPathDocument xslt = new XPathDocument(Assembly.GetExecutingAssembly().GetManifestResourceStream("ECGConversion.MegaCare.xslt"));
							xslTrans.Load(xslt, resolver, this.GetType().Assembly.Evidence); 

							xslTrans.Transform(new System.Xml.XPath.XPathDocument(reader), null, ms, resolver);
#else
                            XslCompiledTransform xslTrans = new XslCompiledTransform();

                            XmlTextReader xslt = new XmlTextReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("ECGConversion.MegaCare.xslt"));
                            xslTrans.Load(xslt, XsltSettings.TrustedXslt, resolver);

                            xslTrans.Transform(reader, null, ms);
#endif

							ms.Seek(0, SeekOrigin.Begin);

							XmlTextReader temp = new XmlTextReader(ms);

							if (!CheckFormat(temp))
								return 2;

							reader = temp;
						}
						catch {}
						break;
#endif
					default:
						break;
				}
			}

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

			return (ret > 0) ? 3 + ret : ret;
		}

		public override int Read(Stream input, int offset)
		{
			XmlTextReader reader = null;

			try
			{
				input.Seek(offset, SeekOrigin.Begin);
				reader = new XmlTextReader(input);

				return Read(reader);
			}
			catch {}

			return 1;
		}
		public override int Read(string file, int offset)
		{
			FileStream stream = null;

			try
			{
				stream = new FileStream(file, FileMode.Open);

				return Read(stream, offset);
			}
			catch {}
			finally
			{
				if (stream != null)
				{
					stream.Close();
					stream = null;
				}
			}

			return 1;
		}
		public override int Read(byte[] buffer, int offset)
		{
			System.IO.MemoryStream ms = null;
			
			try
			{
				ms = new MemoryStream(buffer, offset, buffer.Length-offset, false);

				return Read(ms, 0);
			}
			catch {}
			finally
			{
				if (ms != null)
				{
					ms.Close();
					ms = null;
				}
			}

			return 2;
		}

		public int Write(XmlWriter writer)
		{
			if (!Works())
				return 1;

			writer.WriteStartDocument();
			writer.WriteStartElement(Name);

			writer.WriteAttributeString("xmlns", "urn:hl7-org:v3");
			writer.WriteAttributeString("xmlns:voc", "urn:hl7-org:v3/voc");
			writer.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");
			writer.WriteAttributeString("xsi:schemaLocation", "urn:hl7-org:v3 ../schema/PORT_MT020001.xsd");
			writer.WriteAttributeString("type", "Observation");

			int ret = aECGElement.WriteAll(this, writer);

			if (ret != 0)
				return (ret > 0) ? 1 + ret : ret;

			writer.WriteEndElement();
			writer.WriteEndDocument();

			writer.Flush();

			return 0;
		}

		public override int Write(string file)
		{
			FileStream output = null;

			try
			{
				output = new FileStream(file, FileMode.Create);

				return Write(output);
			}
			catch {}
			finally
			{
				if (output != null)
				{
					output.Close();
					output = null;
				}
			}

			return 1;
		}
		public override int Write(Stream output)
		{
			XmlTextWriter writer = null;

			try
			{
				writer = new XmlTextWriter(output, Encoding);

				return Write(writer);
			}
			catch {}

			return 1;
		}
		public override int Write(byte[] buffer, int offset)
		{
			System.IO.MemoryStream ms = null;
			
			try
			{
				ms = new MemoryStream(buffer, offset, buffer.Length-offset, true);

				return Write(ms);
			}
			catch {}
			finally
			{
				if (ms != null)
				{
					ms.Close();
					ms = null;
				}
			}

			return 2;
		}
		private bool CheckFormat(XmlReader reader)
		{
			try
			{
				while (reader.Read())
				{
					if ((String.Compare(reader.Name, "AnnotatedECG") == 0)
					&&  (reader.NodeType == XmlNodeType.Element)
					&&	(string.Compare(reader.GetAttribute("type"), "Observation", true) == 0))
					{
						string schemaLocation = reader.GetAttribute("xsi:schemaLocation");

						if (schemaLocation == null)
							return true;

						switch (GetFileName(schemaLocation))
						{
							case "PORT_MT020001.xsd":
#if !WINCE
							case "PORI_MT020001.xsd":
#endif
								return true;
							default:
								break;
						}

						break;
					}
				}
			}
			catch
			{
			}

			return false;
		}
		public override bool CheckFormat(Stream input, int offset)
		{
			if (!input.CanRead)
				return false;

			XmlTextReader reader = null;

			try
			{
				input.Seek(offset, SeekOrigin.Begin);

				reader = new XmlTextReader(input);

				return CheckFormat(reader);
			}
			catch {}

			return false;
		}
		public override bool CheckFormat(string file, int offset)
		{
			FileStream stream = null;

			try
			{
				stream = new FileStream(file, FileMode.Open);

				return CheckFormat(stream, offset);
			}
			catch {}
			finally
			{
				if (stream != null)
				{
					stream.Close();
					stream = null;
				}
			}

			return false;
		}
		public override bool CheckFormat(byte[] buffer, int offset)
		{
			System.IO.MemoryStream ms = null;
			
			try
			{
				ms = new MemoryStream(buffer, offset, buffer.Length-offset, false);

				return CheckFormat(ms, 0);
			}
			catch {}
			finally
			{
				if (ms != null)
				{
					ms.Close();
					ms = null;
				}
			}

			return false;
		}
		public override IDemographic Demographics
		{
			get
			{
				return this;
			}
		}
		public override IDiagnostic Diagnostics
		{
			get
			{
				return this;
			}
		}
		public override IGlobalMeasurement GlobalMeasurements
		{
			get
			{
				return this;
			}
		}
		public override ISignal Signals
		{
			get
			{
				return this;
			}
		}
		public override void Anonymous(byte type)
		{
			ECGTool.Anonymous(this, (char)type);
		}
		public override int getFileSize()
		{
			return -1;
		}
		public override bool Works()
		{
			return Id.Works()
				&& Code.Works()
				&& EffectiveTime.Works()
				&& Component.Works();
		}
		public override void Empty()
		{
			aECGElement.EmptyAll(this);
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

		#region IDisposable Members
		public override void Dispose()
		{
			base.Dispose();

			Empty();
		}
		#endregion

		#region ISignal Members

		public int getSignals(out Signals signals)
		{
			signals = new Signals();
			int err = getSignalsToObj(signals);
			if (err != 0)
			{
				signals = null;
			}
			return err;
		}

		public int getSignalsToObj(Signals signals)
		{
			if (!Works())
				return 1;

			if (Component.Count != 1)
				return 2;

			try
			{
				aECGSeries series = Component[0];

				signals.NrLeads = (byte)(series.SequenceSet.Count-1);

				signals.RhythmSamplesPerSecond = (int) (1.0 / (double) series.SequenceSet[0].Value.Increment.Value);
				signals.RhythmAVM = (double) series.SequenceSet[1].Value.Scale.Value;

				if (series.DerivedSet.Count != 0)
				{
					signals.MedianSamplesPerSecond = (int) (1.0 / (double) series.DerivedSet[0].SequenceSet[0].Value.Increment.Value);
					signals.MedianAVM = (double) series.DerivedSet[0].SequenceSet[1].Value.Scale.Value;
					signals.MedianFiducialPoint = 0;
					signals.MedianLength = (ushort) (series.DerivedSet[0].SequenceSet[1].Value.Digits.Length * ((double) Component[0].DerivedSet[0].SequenceSet[0].Value.Increment.Value * 1000));
				}

				for (int i=0;i< signals.NrLeads;i++)
				{
					signals[i] = new Signal();
					signals[i].Rhythm = (short[]) series.SequenceSet[1+i].Value.Digits.Clone();

					ECGTool.ChangeMultiplier(
						signals[i].Rhythm,
						(double) series.SequenceSet[1+i].Value.Scale.Value,
						signals.RhythmAVM);

					if (signals.MedianLength != 0)
					{
						signals[i].Median = (short[]) series.DerivedSet[0].SequenceSet[1+i].Value.Digits.Clone();

						ECGTool.ChangeMultiplier(
							signals[i].Median,
							(double) series.DerivedSet[0].SequenceSet[1+i].Value.Scale.Value,
							signals.MedianAVM);
					}

					LeadTypeVitalRefId lt = (LeadTypeVitalRefId) ECGConverter.EnumParse(
						typeof(LeadTypeVitalRefId),
						series.SequenceSet[1 + i].Code.Code,
						true);

					if ((signals.MedianSamplesPerSecond != 0)
					&&	(series.SequenceSet[1 + i].Code.Code != series.DerivedSet[0].SequenceSet[1 + i].Code.Code))
						return 3;

					signals[i].Type = (LeadType) lt;
					signals[i].RhythmStart = 0;
					signals[i].RhythmEnd = signals[i].Rhythm.Length;
				}

				signals.TrimSignals(0);
			}
			catch
			{
				return 4;
			}

			return 0;
		}

		public int setSignals(Signals signals)
		{
			if ((signals == null)
			||	(signals.NrLeads == 0)
			||	(signals.RhythmAVM <= 0)
			||	(signals.RhythmSamplesPerSecond <= 0))
			{
				return 1;
			}

			Signals sigs = signals.CalculateTwelveLeads();

			if (sigs != null)
			{
				signals = sigs;
			}

			int start, end;

			signals.CalculateStartAndEnd(out start, out end);

			if ((EffectiveTime.Type == aECGTime.TimeType.Center)
			&&	(start < end)
			&&	(end > 0))
			{
				EffectiveTime.Type = aECGTime.TimeType.LowHigh;
				EffectiveTime.ValTwo = EffectiveTime.ValOne + TimeSpan.FromSeconds(((double)(end - start)) / signals.RhythmSamplesPerSecond);
			}

			Id.Root = ECGConverter.NewGuid().ToString();

			Code.Code = "93000";
			Code.CodeSystem = "2.16.840.1.113883.6.12";
			Code.CodeSystemName="CPT-4";

			aECGSeries series = Component[0];
			aECGSeries derivedSeries = null;
			
			series.Id.Root = ECGConverter.NewGuid().ToString();
			
			series.Code.Code = "RHYTHM";
			series.Code.CodeSystem = "2.16.840.1.113883.5.4";
			series.Code.CodeSystemName = "ActCode";
			series.Code.DisplayName = "Rhythm Waveforms";

			series.EffectiveTime.Type = EffectiveTime.Type;
			series.EffectiveTime.ValOne = EffectiveTime.ValOne;
			series.EffectiveTime.ValTwo = EffectiveTime.ValTwo;

			series.SequenceSet[0].Code.Code = "TIME_RELATIVE";
			series.SequenceSet[0].Code.CodeSystem = "2.16.840.1.113883.5.4";
			series.SequenceSet[0].Code.CodeSystemName = "ActCode";

			series.SequenceSet[0].Value.Type = "GLIST_PQ";

			series.SequenceSet[0].Value.Head.Value = 0.0;
			series.SequenceSet[0].Value.Head.Unit = "s";
			series.SequenceSet[0].Value.Increment.Value = (1.0 / signals.RhythmSamplesPerSecond);
			series.SequenceSet[0].Value.Increment.Unit = "s";

			if (signals.MedianLength != 0)
			{
				derivedSeries = series.DerivedSet[0];

				derivedSeries.Id.Root = ECGConverter.NewGuid().ToString();

				derivedSeries.Code.Code = "REPRESENTATIVE_BEAT";
				derivedSeries.Code.CodeSystem = "2.16.840.1.113883.5.4";
				derivedSeries.Code.CodeSystemName = "ActCode";
				derivedSeries.Code.DisplayName = "Representative Beat Waveforms";

				derivedSeries.EffectiveTime.Type = series.EffectiveTime.Type;
				derivedSeries.EffectiveTime.ValOne = series.EffectiveTime.ValOne;
				derivedSeries.EffectiveTime.ValTwo = series.EffectiveTime.ValTwo;

				derivedSeries.SequenceSet[0].Code.Code = "TIME_RELATIVE";
				derivedSeries.SequenceSet[0].Code.CodeSystem = "2.16.840.1.113883.5.4";
				derivedSeries.SequenceSet[0].Code.CodeSystemName = "ActCode";

				derivedSeries.SeriesAuthor.Set(series.SeriesAuthor);

				derivedSeries.SequenceSet[0].Value.Type = "GLIST_PQ";

				derivedSeries.SequenceSet[0].Value.Head.Value = 0.0;
				derivedSeries.SequenceSet[0].Value.Head.Unit = "s";
				derivedSeries.SequenceSet[0].Value.Increment.Value = (1.0 / signals.RhythmSamplesPerSecond);
				derivedSeries.SequenceSet[0].Value.Increment.Unit = "s";
			}

			for (int i=0,j=1;i < signals.NrLeads;i++,j++)
			{
				if (!Enum.IsDefined(typeof(LeadTypeVitalRefId), ((LeadTypeVitalRefId)signals[i].Type)))
					return 2;

				series.SequenceSet[j].Code.Code = ((LeadTypeVitalRefId)signals[i].Type).ToString();
				series.SequenceSet[j].Code.CodeSystem = "2.16.840.1.113883.6.24";
				series.SequenceSet[j].Code.CodeSystemName = "MDC";

				series.SequenceSet[j].Value.Type = "SLIST_PQ";
				series.SequenceSet[j].Value.Origin.Value = 0.0;
				series.SequenceSet[j].Value.Origin.Unit = "uV";
				series.SequenceSet[j].Value.Scale.Value = signals.RhythmAVM;
				series.SequenceSet[j].Value.Scale.Unit = "uV";

				short[] tempArray = new short[end - start];

				ECGTool.CopySignal(signals[i].Rhythm, 0, tempArray, signals[i].RhythmStart - start, (signals[i].RhythmEnd - signals[i].RhythmStart));
				series.SequenceSet[j].Value.Digits = tempArray;

				if (derivedSeries != null)
				{
					derivedSeries.SequenceSet[j].Code.Code = Component[0].SequenceSet[j].Code.Code;
					derivedSeries.SequenceSet[j].Code.CodeSystem = "2.16.840.1.113883.6.24";
					derivedSeries.SequenceSet[j].Code.CodeSystemName = "MDC";

					derivedSeries.SequenceSet[j].Value.Type = "SLIST_PQ";
					derivedSeries.SequenceSet[j].Value.Origin.Value = 0.0;
					derivedSeries.SequenceSet[j].Value.Origin.Unit = "uV";
					derivedSeries.SequenceSet[j].Value.Scale.Value = signals.MedianAVM;
					derivedSeries.SequenceSet[j].Value.Scale.Unit = "uV";

					tempArray = new short[((signals.MedianLength * signals.MedianSamplesPerSecond) / 1000) + 1];

					ECGTool.CopySignal(signals[i].Median, 0, tempArray, 0, tempArray.Length);
					derivedSeries.SequenceSet[j].Value.Digits = tempArray;
				}
			}

			return 0;
		}

		#endregion

		#region IDemographic Members
		public void Init()
		{
			Empty();
		}
		public string LastName
		{
			get
			{
				return TimepointEvent.SubjectAssignment.Subject.Demographic.PersonName.family;
			}
			set
			{
				TimepointEvent.SubjectAssignment.Subject.Demographic.PersonName.family = value;
			}
		}
		public string FirstName
		{
			get
			{
				return TimepointEvent.SubjectAssignment.Subject.Demographic.PersonName.given;
			}
			set
			{
				TimepointEvent.SubjectAssignment.Subject.Demographic.PersonName.given = value;
			}
		}
		public string PatientID
		{
			get
			{
				return TimepointEvent.SubjectAssignment.Subject.Id.Extension;
			}
			set
			{
				TimepointEvent.SubjectAssignment.Subject.Id.Extension = value;
			}
		}
		public string SecondLastName
		{
			get {return null;}
			set {}
		}
		public string PrefixName
		{
			get
			{
				return TimepointEvent.SubjectAssignment.Subject.Demographic.PersonName.prefix;
			}
			set
			{
				TimepointEvent.SubjectAssignment.Subject.Demographic.PersonName.prefix = value;
			}
		}
		public string SuffixName
		{
			get
			{
				return TimepointEvent.SubjectAssignment.Subject.Demographic.PersonName.suffix;
			}
			set
			{
				TimepointEvent.SubjectAssignment.Subject.Demographic.PersonName.suffix = value;
			}
		}
		public int getPatientAge(out ushort val, out AgeDefinition def)
		{
			val = 0;
			def = AgeDefinition.Unspecified;
			return 1;
		}
		public int setPatientAge(ushort val, AgeDefinition def)
		{
			return 1;
		}
		public Date PatientBirthDate
		{
			get
			{
				try
				{
					DateTime temp = (DateTime) TimepointEvent.SubjectAssignment.Subject.Demographic.BirthTime.Value;

					if (temp.Year > 1000)
					{
						return new Date((ushort) temp.Year, (byte) temp.Month, (byte) temp.Day);
					}
				}
				catch
				{
				}
				
				return null;
			}
			set
			{
				if ((value != null)
					&&	value.isExistingDate())
				{
					TimepointEvent.SubjectAssignment.Subject.Demographic.BirthTime.Value = new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, 0);
				}
			}
		}
		public int getPatientHeight(out ushort val, out HeightDefinition def)
		{
			val = 0;
			def = HeightDefinition.Unspecified;
			return 1;
		}
		public int setPatientHeight(ushort val, HeightDefinition def)
		{
			return 1;
		}
		public int getPatientWeight(out ushort val, out WeightDefinition def)
		{
			val = 0;
			def = WeightDefinition.Unspecified;
			return 1;
		}
		public int setPatientWeight(ushort val, WeightDefinition def)
		{
			return 1;
		}
		public Sex Gender
		{
			get
			{
				if (TimepointEvent.SubjectAssignment.Subject.Demographic.AdministrativeGenderCode.Works())
				{
					switch (TimepointEvent.SubjectAssignment.Subject.Demographic.AdministrativeGenderCode.Code)
					{
						case "M": case "m":
							return Sex.Male;
						case "F": case "f":
							return Sex.Female;
					}

					return Sex.Unspecified;
				}

				return Sex.Null;
			}
			set
			{
				if (value != Sex.Null)
				{
					string code = "UN";

					switch (value)
					{
						case Sex.Male:
							code = "M";
							break;
						case Sex.Female:
							code = "F";
							break;
						default:
							break;
					}

					TimepointEvent.SubjectAssignment.Subject.Demographic.AdministrativeGenderCode.Code = code;
					TimepointEvent.SubjectAssignment.Subject.Demographic.AdministrativeGenderCode.CodeSystem = "2.16.840.1.113883.5.1";
					TimepointEvent.SubjectAssignment.Subject.Demographic.AdministrativeGenderCode.CodeSystemName = "AdministrativeGender";
				}
			}
		}
		public Race PatientRace
		{
			get
			{
				if (TimepointEvent.SubjectAssignment.Subject.Demographic.RaceCode.Works())
				{
					switch (TimepointEvent.SubjectAssignment.Subject.Demographic.RaceCode.Code)
					{
						case "2028-9":
							return Race.Oriental;
						case "2054-5":
							return Race.Black;
						case "2106-3":
							return Race.Caucasian;
					}

					return Race.Unspecified;
				}

				return Race.Null;
			}
			set
			{
				if (value != Race.Null)
				{
					string sRace = null;

					switch (value)
					{
						case Race.Oriental:
							sRace = "2028-9";
							break;
						case Race.Black:
							sRace = "2054-5";
							break;
						case Race.Caucasian:
							sRace = "2106-3";
							break;
						default:
							TimepointEvent.SubjectAssignment.Subject.Demographic.RaceCode.Code = null;
							TimepointEvent.SubjectAssignment.Subject.Demographic.RaceCode.CodeSystem = null;
							TimepointEvent.SubjectAssignment.Subject.Demographic.RaceCode.CodeSystemName = null;
							TimepointEvent.SubjectAssignment.Subject.Demographic.RaceCode.DisplayName = null;
							return;
					}

					TimepointEvent.SubjectAssignment.Subject.Demographic.RaceCode.Code = sRace;
					TimepointEvent.SubjectAssignment.Subject.Demographic.RaceCode.CodeSystem = "2.16.840.1.113883.5.104";
					TimepointEvent.SubjectAssignment.Subject.Demographic.RaceCode.CodeSystemName = "Race";
				}
			}
		}
		public AcquiringDeviceID AcqMachineID
		{
			get
			{
				if (Component.Count != 0)
				{
					AcquiringDeviceID id = new AcquiringDeviceID(true);

					Communication.IO.Tools.BytesTool.writeString(this.Component[0].SeriesAuthor.Device.manufacturerModelName, id.ModelDescription, 0, id.ModelDescription.Length);

					try
					{
						id.ManufactorID = (byte) ((DeviceManufactor) ECGConverter.EnumParse(typeof(DeviceManufactor), this.Component[0].SeriesAuthor.Organization.name, true));
					}
					catch {}

					byte map = FilterBitmap;

					if (map != 0xff)
					{
						if ((map & 0x1) == 0x1)
							id.ACFrequencyEnvironment = 2;
						else if ((map & 0x2) == 0x2)
							id.ACFrequencyEnvironment = 1;
						else
							id.ACFrequencyEnvironment = 0;
					}

					return id;
				}

				return null;
			}
			set
			{
				if (value != null)
				{
					this.Component[0].SeriesAuthor.Device.Id.Extension = "0";
					this.Component[0].SeriesAuthor.Device.Code.Code = "12LEAD_ELECTROCARDIOGRAPH";
					this.Component[0].SeriesAuthor.Device.Code.CodeSystem = "";
				
					this.Component[0].SeriesAuthor.Device.manufacturerModelName = Communication.IO.Tools.BytesTool.readString(value.ModelDescription, 0, value.ModelDescription.Length);
					this.Component[0].SeriesAuthor.Device.softwareName = typeof(ECGConversion.ECGConverter).Namespace;

					this.Component[0].SeriesAuthor.Organization.name =  ((DeviceManufactor)value.ManufactorID).ToString();
				}
			}
		}
		public AcquiringDeviceID AnalyzingMachineID
		{
			get {return null;}
			set {}
		}
		public DateTime TimeAcquisition
		{
			get
			{
				if (EffectiveTime.Works())
				{
					return EffectiveTime.ValOne;
				}
			
				return DateTime.MinValue;
			}
			set
			{
				EffectiveTime.Type = aECGTime.TimeType.Center;
				EffectiveTime.ValOne = value;
			}
		}
		public ushort BaselineFilter
		{
			get
			{
				return 0;
			}
			set {}
		}
		public ushort LowpassFilter
		{
			get
			{
				if (Component.Count > 0)
				{
					aECGControlVariableHolder var0 = Component[0].getControlVariable("MDC_ECG_CTL_VBL_ATTR_FILTER_LOW_PASS");

					if (var0 != null)
					{
						aECGControlVariable var1 = var0.ControlVariable;

						if ((var1 != null)
							&&	(var1.InnerVariables[0].Value.Unit == "Hz"))
						{
							try
							{
								double temp = (double) var1.InnerVariables[0].Value.Value;

								return (ushort) temp;
							}
							catch
							{
							}
						}
					}
				}

				return 0;
			}
			set
			{
				if (value != 0)
				{
					aECGControlVariable var1 = new aECGControlVariable();

					var1.Code.Code = "MDC_ECG_CT_LVBL_ATTR_FILTER_LOW_PASS";
					var1.Code.CodeSystem = "2.16.840.1.113883.6.24";
					var1.Code.CodeSystemName = "MDC";
					var1.Code.DisplayName = "Low Pass Filter";

					aECGControlVariable var2 = new aECGControlVariable("component");
					var2.Code.Code = "MDC_ECG_CTL_VBL_ATTR_FILTER_CUTOFF_FREQ";
					var2.Code.CodeSystem = "2.16.840.1.113883.6.24";
					var2.Code.CodeSystemName = "MDC";
					var2.Code.DisplayName = "Cutoff Frequency";

					var2.Value.Type = "PQ";
					var2.Value.Value = (double) value;
					var2.Value.Unit = "Hz";
					var1.InnerVariables[0] = var2;

					Component[0].Add(new aECGControlVariableHolder(var1));
				}
			}
		}
		public byte FilterBitmap
		{
			get
			{
				if (Component.Count > 0)
				{
					aECGControlVariableHolder var0 = Component[0].getControlVariable("MDC_ECG_CTL_VBL_ATTR_FILTER_NOTCH");

					if ((var0 != null)
						&&  (var0.ControlVariable != null))
					{
						aECGControlVariable var1 = var0.ControlVariable;

						byte map = 0;

						aECGControlVariable var2 = null;

						foreach (aECGControlVariable temp in var1.InnerVariables)
						{
							if ((temp != null)
								&&	(string.Compare(temp.Code.Code, "MDC_ECG_CTL_VBL_ATTR_FILTER_NOTCH_FREQ") == 0))
							{
								var2 = temp;
								break;
							}
						}

						if (var2 != null)
						{
							try
							{
								double val = (double) var2.Value.Value;

								if (val == 60)
									map |= 0x1;
								else if (val == 50)
									map |= 0x2;
							}
							catch
							{
							}
						}

						return map;
					}
				}
			
				return 0;
			}
			set
			{
				if (((value & 0x1) == 0x1)
					||	((value & 0x2) == 0x2))
				{
					aECGControlVariable var1 = new aECGControlVariable();

					var1.Code.Code = "MDC_ECG_CTL_VBL_ATTR_FILTER_NOTCH";
					var1.Code.CodeSystem = "2.16.840.1.113883.6.24";
					var1.Code.CodeSystemName = "MDC";
					var1.Code.DisplayName = "Low Pass Filter";

					aECGControlVariable var2 = new aECGControlVariable("component");
					var2.Code.Code = "MDC_ECG_CTL_VBL_ATTR_FILTER_NOTCH_FREQ";
					var2.Code.CodeSystem = "2.16.840.1.113883.6.24";
					var2.Code.CodeSystemName = "MDC";
					var2.Code.DisplayName = "Notch Frequency";

					var2.Value.Type = "PQ";
					var2.Value.Value = ((value & 0x1) == 0x1) ? 60.0 : 50.0;
					var2.Value.Unit = "Hz";
					var1.InnerVariables[0] = var2;
				
					Component[0].Add(new aECGControlVariableHolder(var1));
				}
			}
		}
		public string[] FreeTextFields
		{
			get
			{
				if (text != null)
					return text.Split(new char[]{'\n', '\r'});

				return null;
			}
			set
			{
				text = null;

				if (value != null)
				{
					System.Text.StringBuilder sb = new System.Text.StringBuilder();

					foreach (string line in value)
					{
						if (sb.Length != 0)
							sb.Append("\n");

						sb.Append(line);
					}

					text = sb.ToString();
				}
			}
		}
		public string SequenceNr
		{
			get {return null;}
			set {}
		}
		public string AcqInstitution
		{
			get {return null;}
			set {}
		}
		public string AnalyzingInstitution
		{
			get {return null;}
			set {}
		}
		public string AcqDepartment
		{
			get {return null;}
			set {}
		}
		public string AnalyzingDepartment
		{
			get {return null;}
			set {}
		}
		public string ReferringPhysician
		{
			get {return null;}
			set {}
		}
		public string OverreadingPhysician
		{
			get
			{
				if ((_OverreadingPhysician == null)
					&&	(Component.Count > 0))
				{
					
					aECGSeries series = Component[0];

					aECGAnnotationSet aset = series.getAnnotationSet("MDC_ECG_INTERPRETATION");

					if ((aset != null)
						&&	(aset.Author.AssignedAuthorType.AssignedPerson != null))
					{
						_OverreadingPhysician = aset.Author.AssignedAuthorType.AssignedPerson.PersonName.family;
					}
				}

				return _OverreadingPhysician;
			}
			set
			{
				_OverreadingPhysician = value;
			}
		}
		public string TechnicianDescription
		{
			get {return null;}
			set {}
		}
		public ushort SystolicBloodPressure
		{
			get {return 0;}
			set {}
		}
		public ushort DiastolicBloodPressure
		{
			get {return 0;}
			set {}
		}
		public Drug[] Drugs
		{
			get {return null;}
			set {}
		}
		public string[] ReferralIndication
		{
			get {return null;}
			set {}
		}
		public string RoomDescription
		{
			get {return null;}
			set {}
		}
		public byte StatCode
		{
			get {return 0xff;}
			set {}
		}
		#endregion

		#region IDiagnostic Members

		public int getDiagnosticStatements(out Statements stat)
		{
			stat = null;

			if (Component.Count > 0)
			{
				aECGAnnotationSet aset = Component[0].getAnnotationSet("MDC_ECG_INTERPRETATION");

				if (aset != null)
				{
					try
					{
						aECGAnnotation ann = aset["MDC_ECG_INTERPRETATION"];

						ArrayList al =  new ArrayList();

						for (int i=0;i < ann.Annotation.Length;i++)
						{
							if (ann.Annotation[i] == null)
								break;
							
							if (string.Compare(ann.Annotation[i].Code.Code, "MDC_ECG_INTERPRETATION_STATEMENT") == 0)
								al.Add(ann.Annotation[i].Value.Value);
						}

						if (al.Count > 0)
						{
							stat = new Statements();

							if (Regex.IsMatch((string) al[al.Count-1], "(UN)?(CONFIRMED REPORT)", RegexOptions.None))
							{
								string temp = (string) al[al.Count-1];

								stat.confirmed = temp.StartsWith("CONF");

								al.RemoveAt(al.Count-1);
							}

							stat.statement = new string[al.Count];

							for (int i=0;i < stat.statement.Length;i++)
								stat.statement[i] = (string) al[i];

							if (stat.confirmed)
								stat.time = (DateTime) aset.ActivityTime.Value;

							return 0;
						}

						return 8;
					}
					catch
					{
						return 4;
					}
				}
				return 2;
			}

			return 1;
		}

		public int setDiagnosticStatements(Statements stat)
		{
			if ((stat != null)
				&&  (stat.time.Year > 1000)
				&&  (stat.statement != null)
				&&  (stat.statement.Length > 0)
				&&	(Component.Count > 0))
			{
				aECGSeries series = Component[0];

				string tempOver = OverreadingPhysician;

				aECGAnnotationSet aset = series.Annotation[0];
				if (stat.confirmed
					||	(tempOver != null)
					||	(aset == null))
				{
					aset = null;

					for (int j=0;j < series.Annotation.Length;j++)
					{
						if (series.Annotation[j] == null)
							break;

						if ((series.Annotation[j].Author.AssignedAuthorType.AssignedPerson != null)
							&&	(series.Annotation[j].Author.AssignedAuthorType.AssignedPerson.PersonName.family == tempOver))
							aset = series.Annotation[j];
					}

					if (aset == null)
					{
						aset = new aECGAnnotationSet();

						if (tempOver != null)
						{
							aset.ActivityTime.Value = stat.time;

							aset.Author.AssignedAuthorType.AssignedPerson = new aECGPerson("assignedPerson");

							aset.Author.AssignedAuthorType.AssignedPerson.PersonName.family = tempOver;
						}
						else
						{
							aset.ActivityTime.Value = this.TimeAcquisition;

							aset.Author.AssignedAuthorType.AssignedDevice = new aECGDevice("assignedDevice");

							aset.Author.AssignedAuthorType.AssignedDevice.Set(series.SeriesAuthor.Device);
							aset.Author.AssignedAuthorType.AssignedDevice.PlayedManufacturedDevice.ManufacturerOrganization.Set(series.SeriesAuthor.Organization);
						}
					}
				}

				aECGAnnotation top = new aECGAnnotation();
				top.Code.Code = "MDC_ECG_INTERPRETATION";
				top.Code.CodeSystem = "2.16.840.1.113883.6.24";

				int i=0;
				for (int j=0;i < stat.statement.Length;i++)
				{
					if (stat.statement[i] == null)
						continue;

					aECGAnnotation temp = new aECGAnnotation();

					temp.Code.Code = "MDC_ECG_INTERPRETATION_STATEMENT";
					temp.Code.CodeSystem = "2.16.840.1.113883.6.24";
					temp.Value.Type = "ST";
					temp.Value.Value = stat.statement[i];

					top.Annotation[j++] = temp;
				}

				if (i > 0)
				{
					aECGAnnotation temp = new aECGAnnotation();

					temp.Code.Code = "MDC_ECG_INTERPRETATION_STATEMENT";
					temp.Code.CodeSystem = "2.16.840.1.113883.6.24";
					temp.Value.Type = "ST";
					temp.Value.Value = stat.confirmed ? "CONFIRMED REPORT" : "UNCONFIRMED REPORT";

					top.Annotation[i] = temp;
				}

				aset.Add(top);

				series.Add(aset);

				return 0;
			}

			return 1;
		}

		#endregion

		#region IGlobalMeasurement Members

		public int getGlobalMeasurements(out GlobalMeasurements mes)
		{
			mes = null;

			if (Component.Count > 0)
			{
				mes = new GlobalMeasurements();

				aECGSeries
					series = Component[0],
					seriesMedian = (series.DerivedSet.Count > 0) ? series.DerivedSet[0] : null;

				string[]
					releventCodes = {"MDC_ECG_WAVC", "MDC_ECG_ANGLE_P_FRONT", "MDC_ECG_ANGLE_QRS_FRONT", "MDC_ECG_ANGLE_T_FRONT", "MDC_ECG_TIME_PD_PP", "MDC_ECG_TIME_PD_RR", "MDC_ECG_HEART_RATE", "MDC_ECG_TIME_PD_QTc"},
					releventValues = {"MDC_ECG_WAVC_PWAVE", "MDC_ECG_WAVC_QRSWAVE", "MDC_ECG_WAVC_TWAVE"};

				aECGAnnotationSet anns, annsMedian;

				anns = series.getAnnotationSet(releventCodes);
				annsMedian = (seriesMedian != null ? seriesMedian.getAnnotationSet(releventCodes) : null);

				if ((anns != null)
				||  (annsMedian != null))
				{
					ArrayList
						alAnnotations = new ArrayList(),
						alMeasurments = new ArrayList();

					if (annsMedian != null)
						alAnnotations.AddRange(annsMedian.Annotation);

					if (anns != null)
						alAnnotations.AddRange(anns.Annotation);

					GlobalMeasurement gm = null;

					for (int i=0;i < alAnnotations.Count;i++)
					{
						aECGAnnotation ann = (aECGAnnotation) alAnnotations[i];

						if (ann == null)
							continue;

						if (string.Compare(ann.Code.Code, "MDC_ECG_BEAT") == 0)
							alAnnotations.AddRange(ann.Annotation);

						int index = IndexOf(releventCodes, ann.Code.Code);

						if (index >= 0)
						{
							object val = null;

							switch (index)
							{
								case -1: break;
								case 0:
									val = ann.Value.Code;
									break;
								case 1: case 2: case 3:
									if (string.Compare(ann.Value.Unit, "deg") == 0)
										val = ann.Value.Value;
									break;
								case 6:
									if (string.Compare(ann.Value.Unit, "bpm") == 0)
										val = ann.Value.Value;
									break;
								default:
									if (string.Compare(ann.Value.Unit, "ms") == 0)
										val = ann.Value.Value;
									break;
							}

							if (val is string
							&&	(ann.SupportingROI.Boundary[0] != null)
							&&	(ann.SupportingROI.Boundary[1] == null))
							{
								aECGValuePair vp1 = null, vp2 = null;

								if (string.Compare(ann.SupportingROI.Boundary[0].Code.Code, "TIME_RELATIVE") == 0)
								{
									switch (IndexOf(releventValues, (string)val))
									{
										case 0:
											vp1 = ann.SupportingROI.Boundary[0].Value["low"];
											vp2 = ann.SupportingROI.Boundary[0].Value["high"];

											if (((vp1 != null)
											||	 (vp2 != null))
											&&	((vp1 == null)
											||	 (string.Compare(vp1.Unit, "ms") == 0))
											&&	((vp2 == null)
											||	 (string.Compare(vp2.Unit, "ms") == 0)))
											{
												if ((gm == null)
												||	(gm.Ponset != GlobalMeasurement.NoValue)
												||	(gm.Ponset != GlobalMeasurement.NoValue))
												{
													gm = new GlobalMeasurement();

													alMeasurments.Add(gm);
												}

												if (vp1 != null)
													gm.Ponset = (ushort) ((double)vp1.Value);

												if (vp2 != null)
													gm.Poffset = (ushort) ((double)vp2.Value);
											}

											break;
										case 1:
											vp1 = ann.SupportingROI.Boundary[0].Value["low"];
											vp2 = ann.SupportingROI.Boundary[0].Value["high"];

											if (((vp1 != null)
											||	 (vp2 != null))
											&&	((vp1 == null)
											||	 (string.Compare(vp1.Unit, "ms") == 0))
											&&	((vp2 == null)
											||	 (string.Compare(vp2.Unit, "ms") == 0)))
											{
												if ((gm == null)
												||	(gm.QRSonset != GlobalMeasurement.NoValue)
												||	(gm.QRSoffset != GlobalMeasurement.NoValue))
												{
													gm = new GlobalMeasurement();

													alMeasurments.Add(gm);
												}

												if (vp1 != null)
													gm.QRSonset = (ushort) ((double)vp1.Value);

												if (vp2 != null)
													gm.QRSoffset = (ushort) ((double)vp2.Value);
											}
											break;
										case 2:
											vp1 = ann.SupportingROI.Boundary[0].Value["high"];

											if ((vp1 != null)
											&&	(string.Compare(vp1.Unit, "ms") == 0))
											{
												if ((gm == null)
													||	(gm.Toffset != GlobalMeasurement.NoValue))
												{
													gm = new GlobalMeasurement();

													alMeasurments.Add(gm);
												}

												gm.Toffset = (ushort) ((double) vp1.Value);
											}
											break;
									}
								}
								else if (string.Compare(ann.SupportingROI.Boundary[0].Code.Code, "TIME_ABSOLUTE") == 0)
								{

								}
							}
							else if (val is double)
							{
								switch (index)
								{
									case 1:
										if ((gm == null)
										||	(gm.Paxis != GlobalMeasurement.NoValue))
										{
											gm = new GlobalMeasurement();

											alMeasurments.Add(gm);
										}

										gm.Paxis = (short) ((double)val);
										break;
									case 2:
										if ((gm == null)
										||	(gm.QRSaxis != GlobalMeasurement.NoValue))
										{
											gm = new GlobalMeasurement();

											alMeasurments.Add(gm);
										}

										gm.QRSaxis = (short) ((double)val);
										break;
									case 3:
										if ((gm == null)
										||	(gm.Taxis != GlobalMeasurement.NoValue))
										{
											gm = new GlobalMeasurement();

											alMeasurments.Add(gm);
										}

										gm.Taxis = (short) ((double)val);
										break;
									case 4:
										mes.AvgPP = (ushort) ((double)val);
										break;
									case 5:
										mes.AvgRR = (ushort) ((double)val);
										break;
									case 6:
										mes.VentRate = (ushort) ((double)val);
										break;
									case 7:
										mes.QTc = (ushort) ((double)val);
										break;
								}
							}
						}
					}

					if (alMeasurments.Count != 0)
					{
						mes.measurment = new GlobalMeasurement[alMeasurments .Count];

						for (int i=0;i < alMeasurments.Count;i++)
						{
							mes.measurment[i] = (GlobalMeasurement) alMeasurments[i];
						}
					}

					return 0;
				}

				return 2;
			}

			return 1;
		}

		public int setGlobalMeasurements(GlobalMeasurements mes)
		{
			if ((mes != null)
			&&	(mes.measurment != null)
			&&	(Component.Count > 0))
			{
				aECGSeries
					series = Component[0],
					seriesMedian = (series.DerivedSet.Count == 1) ? series.DerivedSet[0] : null;

				aECGAnnotationSet
					aset = series.Annotation[0],
					asetMedian = seriesMedian.Annotation[0];

				int n =	1;

				while ((aset != null)
					&& ((DateTime)aset.ActivityTime.Value != this.TimeAcquisition))
					aset = series.Annotation[n++];

				n = 1;

				while ((aset != null)
					&& ((DateTime)aset.ActivityTime.Value != this.TimeAcquisition))
					aset = seriesMedian.Annotation[n++];

				if ((aset == null)
				||	((DateTime)aset.ActivityTime.Value != this.TimeAcquisition))
				{
					aset = new aECGAnnotationSet();

					aset.ActivityTime.Value = this.TimeAcquisition;

					aset.Author.AssignedAuthorType.AssignedDevice = new aECGDevice("assignedDevice");

					aset.Author.AssignedAuthorType.AssignedDevice.Set(series.SeriesAuthor.Device);
					aset.Author.AssignedAuthorType.AssignedDevice.PlayedManufacturedDevice.ManufacturerOrganization.Set(series.SeriesAuthor.Organization);
				}

				if ((asetMedian == null)
				||	((DateTime)asetMedian.ActivityTime.Value != this.TimeAcquisition))
				{
					asetMedian = new aECGAnnotationSet();

					asetMedian.ActivityTime.Value = this.TimeAcquisition;

					asetMedian.Author.AssignedAuthorType.AssignedDevice = new aECGDevice("assignedDevice");

					asetMedian.Author.AssignedAuthorType.AssignedDevice.Set(series.SeriesAuthor.Device);
					asetMedian.Author.AssignedAuthorType.AssignedDevice.PlayedManufacturedDevice.ManufacturerOrganization.Set(series.SeriesAuthor.Organization);
				}

				if (mes.VentRate != GlobalMeasurement.NoValue)
				{
					aECGAnnotation ann = new aECGAnnotation();

					ann.Code.Code = "MDC_ECG_HEART_RATE";
					ann.Code.CodeSystem = "2.16.840.1.113883.6.24";

					ann.Value.Unit = "bpm";
					ann.Value.Value = (double) mes.VentRate;

					aset.Add(ann);
				}

				if (mes.AvgPP != GlobalMeasurement.NoValue)
				{
					aECGAnnotation ann = new aECGAnnotation();

					ann.Code.Code = "MDC_ECG_TIME_PD_PP";
					ann.Code.CodeSystem = "2.16.840.1.113883.6.24";

					ann.Value.Unit = "ms";
					ann.Value.Value = (double) mes.AvgPP;

					aset.Add(ann);
				}

				if (mes.Pdur != GlobalMeasurement.NoValue)
				{
					aECGAnnotation ann = new aECGAnnotation();

					ann.Code.Code = "MDC_ECG_TIME_PD_P";
					ann.Code.CodeSystem = "2.16.840.1.113883.6.24";

					ann.Value.Unit = "ms";
					ann.Value.Value = (double) mes.Pdur;

					aset.Add(ann);
				}

				if (mes.AvgRR != GlobalMeasurement.NoValue)
				{
					aECGAnnotation ann = new aECGAnnotation();

					ann.Code.Code = "MDC_ECG_TIME_PD_RR";
					ann.Code.CodeSystem = "2.16.840.1.113883.6.24";

					ann.Value.Unit = "ms";
					ann.Value.Value = (double) mes.AvgRR;

					aset.Add(ann);
				}

				if (mes.PRint != GlobalMeasurement.NoValue)
				{
					aECGAnnotation ann = new aECGAnnotation();

					ann.Code.Code = "MDC_ECG_TIME_PD_PR";
					ann.Code.CodeSystem = "2.16.840.1.113883.6.24";

					ann.Value.Unit = "ms";
					ann.Value.Value = (double) mes.PRint;

					aset.Add(ann);
				}

				if (mes.QRSdur != GlobalMeasurement.NoValue)
				{
					aECGAnnotation ann = new aECGAnnotation();

					ann.Code.Code = "MDC_ECG_TIME_PD_QRS";
					ann.Code.CodeSystem = "2.16.840.1.113883.6.24";

					ann.Value.Unit = "ms";
					ann.Value.Value = (double) mes.QRSdur;

					aset.Add(ann);
				}

				if (mes.QTdur != GlobalMeasurement.NoValue)
				{
					aECGAnnotation ann = new aECGAnnotation();

					ann.Code.Code = "MDC_ECG_TIME_PD_QT";
					ann.Code.CodeSystem = "2.16.840.1.113883.6.24";

					ann.Value.Unit = "ms";
					ann.Value.Value = (double) mes.QTdur;

					aset.Add(ann);
				}

				if (mes.QTc != GlobalMeasurement.NoValue)
				{
					aECGAnnotation ann = new aECGAnnotation();

					ann.Code.Code = "MDC_ECG_TIME_PD_QTc";
					ann.Code.CodeSystem = "2.16.840.1.113883.6.24";

					ann.Value.Unit = "ms";
					ann.Value.Value = (double) mes.QTc;

					aset.Add(ann);
				}

				for (int i=0;i < mes.measurment.Length;i++)
				{
					IaECGAnnotationHolder tempset = null;

					if ((i == 0)
					&&	(asetMedian != null))
					{
						tempset = asetMedian;
					}
					else
					{
						aECGAnnotation tempAnn = new aECGAnnotation();
						tempAnn.Code.Code = "MDC_ECG_BEAT";
						tempAnn.Code.CodeSystem = "2.16.840.1.113883.6.24";
						tempAnn.Code.CodeSystemName = "MDC";

						tempAnn.Value.Type = "CE";
						tempAnn.Value.Code = "MDC_ECG_BEAT";
						tempAnn.Value.CodeSystem = "2.16.840.1.113883.6.24";
						tempAnn.Value.CodeSystemName = "MDC";

						aset.Add(tempAnn);

						tempset = tempAnn;
					}

					GlobalMeasurement gmes = mes.measurment[i];

					aECGAnnotation ann = new aECGAnnotation();

					ann.Code.Code = "MDC_ECG_WAVC";
					ann.Code.CodeSystem = "2.16.840.1.113883.6.24";

					ann.Value.Type = "CE";
					ann.Value.Code = "MDC_ECG_WAVC_PWAVE";
					ann.Value.CodeSystem = "2.16.840.1.113883.6.24";

					ann.SupportingROI.Code.Code = "ROIPS";
					ann.SupportingROI.Code.CodeSystem = "2.16.840.1.113883.5.4";
					ann.SupportingROI.Code.CodeSystemName = "HL7V3";

					aECGBoundary boundary = new aECGBoundary();
					boundary.Code.Code = "TIME_RELATIVE";
					boundary.Code.CodeSystem = "2.16.840.1.113883.6.24";

					boundary.Value.Type = "IVL_PQ";
					boundary.Value["low"] = new aECGValuePair("low", gmes.Ponset, "ms");
					boundary.Value["high"] = new aECGValuePair("high", gmes.Poffset, "ms");

					ann.SupportingROI.Boundary[0] = boundary;
					tempset.Add(ann);

					ann = new aECGAnnotation();

					ann.Code.Code = "MDC_ECG_WAVC";
					ann.Code.CodeSystem = "2.16.840.1.113883.6.24";

					ann.Value.Type = "CE";
					ann.Value.Code = "MDC_ECG_WAVC_QRSWAVE";
					ann.Value.CodeSystem = "2.16.840.1.113883.6.24";

					ann.SupportingROI.Code.Code = "ROIPS";
					ann.SupportingROI.Code.CodeSystem = "2.16.840.1.113883.5.4";
					ann.SupportingROI.Code.CodeSystemName = "HL7V3";

					boundary = new aECGBoundary();
					boundary.Code.Code = "TIME_RELATIVE";
					boundary.Code.CodeSystem = "2.16.840.1.113883.6.24";

					boundary.Value.Type = "IVL_PQ";
					boundary.Value["low"] = new aECGValuePair("low", gmes.QRSonset, "ms");
					boundary.Value["high"] = new aECGValuePair("high", gmes.QRSoffset, "ms");

					ann.SupportingROI.Boundary[0] = boundary;
					tempset.Add(ann);

					ann = new aECGAnnotation();

					ann.Code.Code = "MDC_ECG_WAVC";
					ann.Code.CodeSystem = "2.16.840.1.113883.6.24";

					ann.Value.Type = "CE";
					ann.Value.Code = "MDC_ECG_WAVC_TWAVE";
					ann.Value.CodeSystem = "2.16.840.1.113883.6.24";

					ann.SupportingROI.Code.Code = "ROIPS";
					ann.SupportingROI.Code.CodeSystem = "2.16.840.1.113883.5.4";
					ann.SupportingROI.Code.CodeSystemName = "HL7V3";

					boundary = new aECGBoundary();
					boundary.Code.Code = "TIME_RELATIVE";
					boundary.Code.CodeSystem = "2.16.840.1.113883.6.24";

					boundary.Value.Type = "IVL_PQ";
					boundary.Value["high"] = new aECGValuePair("high", gmes.Toffset, "ms");

					ann.SupportingROI.Boundary[0] = boundary;
					tempset.Add(ann);

					if (gmes.Paxis != GlobalMeasurement.NoAxisValue)
					{
						ann = new aECGAnnotation();

						ann.Code.Code = "MDC_ECG_ANGLE_P_FRONT";
						ann.Code.CodeSystem = "2.16.840.1.113883.6.24";

						ann.Value.Type = "PQ";
						ann.Value.Value = (double) gmes.Paxis;
						ann.Value.Unit = "deg";
						tempset.Add(ann);
					}

					if (gmes.QRSaxis != GlobalMeasurement.NoAxisValue)
					{
						ann = new aECGAnnotation();

						ann.Code.Code = "MDC_ECG_ANGLE_QRS_FRONT";
						ann.Code.CodeSystem = "2.16.840.1.113883.6.24";

						ann.Value.Type = "PQ";
						ann.Value.Value = (double) gmes.QRSaxis;
						ann.Value.Unit = "deg";
						tempset.Add(ann);
					}

					if (gmes.Taxis != GlobalMeasurement.NoAxisValue)
					{
						ann = new aECGAnnotation();

						ann.Code.Code = "MDC_ECG_ANGLE_T_FRONT";
						ann.Code.CodeSystem = "2.16.840.1.113883.6.24";

						ann.Value.Type = "PQ";
						ann.Value.Value = (double) gmes.Taxis;
						ann.Value.Unit = "deg";
						tempset.Add(ann);
					}
				}

				if (aset.Annotation[0] != null)
					series.Add(aset);

				if (asetMedian.Annotation[0] != null)
					seriesMedian.Add(asetMedian);

				return 0;
			}

			return 1;
		}

		#endregion

		public static int IndexOf(string[] values, string val)
		{
			if ((values == null)
			||	(val == null))
				return -1;

			for (int i=0;i < values.Length;i++)
				if (string.Compare(values[i], val) == 0)
					return i;

			return -1;
		}

		private static string GetFileName(string temp1)
		{
			if (temp1 != null)
			{
				string[] temp2 = temp1.Split(' ');
				temp2 = temp2[temp2.Length-1].Split('\\', '/');
						
				return temp2[temp2.Length-1];
			}

			return string.Empty;
		}
	}
}
