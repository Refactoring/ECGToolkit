/***************************************************************************
Copyright 2019, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands
Copyright 2012, van Ettinger Information Technology, Lopik, The Netherlands
Copyright 2008-2010, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;

using ECGConversion.ECGDiagnostic;
using ECGConversion.ECGGlobalMeasurements;
using ECGConversion.ECGSignals;

using iTextSharp.text;
using iTextSharp.text.pdf;

namespace ECGConversion.PDF
{
	/// <summary>
	/// Class containing the PDF format.
	/// </summary>
	public class PDFFormat : IECGFormat, ISignal, IGlobalMeasurement, IDiagnostic
	{
		public enum SupportedPaper
		{
			A4,
			LETTER,
			A4_Portrait,
			LETTER_Portrait
		}

		private PDFDemographics _Demographics;
		private Signals _Signals;
		private GlobalMeasurements _GlobalMeasurements;
		private Statements _Diagnostic;

		private bool _DrawGrid
		{
			get
			{
				try
				{
					return bool.Parse(_Config["Grid"]);
				}
				catch {}

				return true;
			}
		}

		private float _Gain
		{
			get
			{
				try
				{
					return float.Parse(_Config["Gain"], System.Globalization.CultureInfo.CurrentUICulture);
				}
				catch {}

				return 10f;
			}
		}

		private float _DrawSpeed
		{
			get
			{
				try
				{
					return float.Parse(_Config["Speed"], System.Globalization.CultureInfo.CurrentUICulture);
				} 
				catch {}

				return 25.0f;
			}
		}

		private ECGDraw.ECGDrawType _DrawType
		{
			get
			{
				try
				{
					return (ECGDraw.ECGDrawType) ECGConverter.EnumParse(typeof(ECGDraw.ECGDrawType), _Config["Lead Format"], true);
				}
				catch {}

				return ECGDraw.ECGDrawType.None;
			}
		}

		private SupportedPaper _PaperType
		{
			get
			{
				try
				{
					return (SupportedPaper) ECGConverter.EnumParse(typeof(SupportedPaper), _Config["Paper Type"], true);
				}
				catch {}

				return SupportedPaper.A4;
			}
		}

		private iTextSharp.text.Rectangle _PageSize
		{
			get
			{
				iTextSharp.text.Rectangle ret = new RectangleReadOnly(595,842); // new iTextSharp.text.Rectangle(8.5, 11.0);

				switch (_PaperType)
				{
					case SupportedPaper.LETTER:
					case SupportedPaper.LETTER_Portrait:
//						ret = PageSize.LETTER;
						ret = new RectangleReadOnly(612,792);
						break;
					default:
						break;
				}

				return ret;
			}
		}

		private LeadType[] _ExtraLeads
		{
			get
			{
				LeadType[] ret = null;

				string cfg = _Config["Extra Leads"];

				if (cfg != null)
				{
					string[] cfgs = cfg.Split(',', ';');

					ret = new LeadType[cfgs.Length];

					for (int i=0;i < cfgs.Length;i++)
					{
						ret[i] = (LeadType) ECGConverter.EnumParse(typeof(LeadType), cfgs[i], true);
					}
				}

				return ret;
			}
		}

		private int _SignalLength
		{
			get
			{
				if (_Signals != null)
				{
					int start, end;

					_Signals.CalculateStartAndEnd(out start, out end);

					return (int) Math.Round((end - start) / (float)_Signals.RhythmSamplesPerSecond);
				}

				return 0;
			}
		}

		private string _DocumentTitle
		{
			get
			{
				string temp = _Config["Document Title"];

				if (temp == null)
					temp = "ECG - " + _Demographics.PatientID + " - " + _Demographics.TimeAcquisition.ToString();

				return temp;
			}
		}

		private string _DocumentCreator
		{
			get
			{
				string temp = _Config["Document Creator"];

				if (temp == null)
					temp = "ECGConversion Toolkit";

				return temp;
			}
		}

		private string _DocumentAuthor
		{
			get
			{
				string temp = _Config["Document Author"];

				if (temp == null)
					temp = "ECGConversion Toolkit";

				return temp;
			}
		}

		private string _HeaderImagePath
		{
			get
			{
				string temp = _Config["Header Image"];

				if ((temp != null)
				&&	(temp.Length == 0))
					return null;

				return temp;
			}
		}
		
		internal class FreeTextSpec
		{
			private FreeTextSpec(float x, float y, float fontSize, string text)
			{
				_Point = new PointF(x, y);
				_FontSize = fontSize;				
				_Text = text;
			}
			
			internal static FreeTextSpec[] ParseSyntax(string config, SupportedPaper sp)
			{
				if (config != null)
				{
					// keep track of all provided free text segments.
					ArrayList temp = new ArrayList();
					
					// walk through all provided segments
					foreach (string segment in config.Split('|'))
					{
						if (segment.Length > 0)
						{			
							// index of the start of the text field
							int index = segment.IndexOf(':');
						
							// check that the ':' is found! 
							if (index > 0)
							{
								// split the begin of a free textfield description
								string[] spec = segment.Substring(0, index).Split(' ', ',');
								// get the free text that must be displayed
								string text = segment.Substring(index+1);
								
								// values that are provided to specify the free text to be dispayed.
								int fontSize = 8;
								double x = 0, y = 0;
								
								// if 3 values are provided
								if (spec.Length == 3)
								{
									// parse the font size
									fontSize = int.Parse(spec[0], CultureInfo.InvariantCulture.NumberFormat);
									
									// parse x and y coördinate
									x = double.Parse(spec[1], CultureInfo.InvariantCulture.NumberFormat);
									y = double.Parse(spec[2], CultureInfo.InvariantCulture.NumberFormat);
								}
								else if (spec.Length == 2)
								{
									// parse x and y coördinate
									x = double.Parse(spec[0], CultureInfo.InvariantCulture.NumberFormat);
									y = double.Parse(spec[1], CultureInfo.InvariantCulture.NumberFormat);
								}
								
								// check whether the values make any sense
								if ((x > 0f)
								&&	(y > 0f)
								&&	(text != null)
								&&	(text.Length > 0))
								{
									if ((sp == SupportedPaper.LETTER)
									||	(sp == SupportedPaper.LETTER_Portrait))
									{
										x = x / PDFTool.mm_Per_Inch;
										y = y / PDFTool.mm_Per_Inch;
									}
									
									temp.Add(new FreeTextSpec((float)x, (float)y, fontSize, text));
								}
								else
								{
									throw new Exception("Syntax of free text field is bad!");
								}
							}
							else
							{
								throw new Exception("Syntax of free text field is bad!");
							}
						}
					}
					
					// check that there is any free text provided.
					if ((temp != null)
					&&	(temp.Count > 0))
					{
						FreeTextSpec[] ret = new FreeTextSpec[temp.Count];
						
						for (int i=0;i < ret.Length;i++)
						{
							ret[i] = (FreeTextSpec) temp[i];
						}
						
						return ret;
					}
				}
				
				return null;
			}
			
			private PointF _Point;
			private float _FontSize;
			private string _Text;
			
			public PointF Point
			{
				get {return _Point;}
			}
			
			public float FontSize
			{
				get {return _FontSize;}
			}
			
			public string Text
			{
				get {return _Text;}
			}
		}
		
		internal FreeTextSpec[] _FreeText
		{
			get
			{
				return FreeTextSpec.ParseSyntax(_Config["Free Text"], _PaperType);
			}
		}

		private bool _UseBufferedStream
		{
			get
			{
				return (_Config["Use Buffered Stream"] != null)
					&& (String.Compare(_Config["Use Buffered Stream"], "true", true) == 0);
			}
		}

		public PDFFormat()
		{
			string[]
				must = new string[]{"Lead Format", "Paper Type", "Gain", "Speed", "Grid"},
				poss = new string[]{"Extra Leads", "Document Title", "Document Creator", "Document Author", "Header Image", "Free Text", "Use Buffered Stream"};

			_Config = new ECGConfig(must, poss, new ECGConfig.CheckConfigFunction(this._ConfigurationWorks));
			
			_Config["Lead Format"] = ECGDraw.ECGDrawType.Regular.ToString();
			_Config["Paper Type"] = "A4";
			_Config["Gain"] = "10";
			_Config["Speed"] = "25";
			_Config["Grid"] = "true";
			_Config["Use Buffered Stream"] = "false";

			Empty();
		}

		public override bool SupportsBufferedStream
		{
			get
			{
				return _UseBufferedStream;
			}
		}

		private bool _ConfigurationWorks()
		{
			try
			{	
				ECGDraw.ECGDrawType dt = (ECGDraw.ECGDrawType) ECGConverter.EnumParse(typeof(ECGDraw.ECGDrawType), _Config["Lead Format"], true);
				float.Parse(_Config["Gain"], System.Globalization.CultureInfo.CurrentUICulture);
				SupportedPaper sp = (SupportedPaper) ECGConverter.EnumParse(typeof(SupportedPaper), _Config["Paper Type"], true);
				
				FreeTextSpec[] fts = _FreeText;

				if ((_DrawType == ECGDraw.ECGDrawType.ThreeXFourPlusOne)
				&&	(_ExtraLeads != null)
				&&	(_ExtraLeads.Length != 1))
					return false;
				else if ((_DrawType == ECGDraw.ECGDrawType.ThreeXFourPlusThree)
					&&	 (_ExtraLeads != null)
					&&	 (_ExtraLeads.Length != 3))
					return false;

				return!(((fts == null)
				    ||	 (fts.Length > 0))
				    &&  (dt != ECGDraw.ECGDrawType.Regular)
					&&	((sp == SupportedPaper.A4_Portrait)
					||	 (sp == SupportedPaper.LETTER_Portrait)));
			}
			catch {}

			return false;
		}

		#region IECGFormat Members
		public override int Read(Stream input, int offset)
		{
			return 0x1;
		}
		public override int Read(string file, int offset)
		{
			return 0x1;
		}
		public override int Read(byte[] buffer, int offset)
		{
			return 0x1;
		}
		public override int Write(string file)
		{
			if (file != null)
			{
				Stream output = null;
					
				try
				{
					// open stream to write to.
					output = new FileStream(file, FileMode.Create);

					// use write function for streams.
					return Write(output);
				}
				catch {}
				finally
				{
					if (output != null)
						output.Close();
				}
			}
			return 0x1;
		}
		public override int Write(Stream output)
		{
			if ((output != null)
			&&  (output.CanWrite)
			&&	Works())
			{
				Signals sigs = _Signals.CalculateTwelveLeads();
                if (sigs == null)
                    sigs =_Signals.CalculateFifteenLeads();

				BufferedSignals bs = null;
				int nrSecs = 7,
					signalPos = 0,
                    signalEnd = 0,
                    signalMulti = 1;

				int[] extra = FindExtraLeads(sigs, _ExtraLeads);

				bool bTwelveLeadECG = sigs != null;

				if (sigs == null)
					sigs = _Signals;

				if (_UseBufferedStream)
					bs = sigs.AsBufferedSignals;

				if (bs != null)
				{
					if (bTwelveLeadECG)
					{
						nrSecs = 10;

						if ((_DrawType == ECGDraw.ECGDrawType.Regular)
						&&	((_PaperType == SupportedPaper.A4_Portrait)
						||	 (_PaperType == SupportedPaper.LETTER_Portrait)))
							nrSecs = 8;
					}

					if (_DrawType == ECGDraw.ECGDrawType.Median)
					{
						if (bs.TemplateNrMedian > 0)
						{
							if (!bs.LoadTemplate(signalPos++))
								return 4;

                            signalEnd = bs.TemplateNrMedian;
						}
					}
					else
					{
                        if (_DrawType == ECGDraw.ECGDrawType.Regular)
                        {
                            if (bs.NrLeads == 1)
                                signalMulti = 6;
                            else if (bs.NrLeads == 2)
                                signalMulti = 5;
                            else if (bs.NrLeads == 3)
                                signalMulti = 3;
                            else if (bs.NrLeads <= 6)
                                signalMulti = 2;
                        }

						signalPos = bs.RealRhythmStart;

						if (!bs.LoadSignal(signalPos, signalPos + (nrSecs * sigs.RhythmSamplesPerSecond * signalMulti)))
							return 4;

                        signalPos += (nrSecs * sigs.RhythmSamplesPerSecond * signalMulti);
                        signalEnd = bs.RealRhythmEnd;
					}
					}

				if ((_DrawType & ECGDraw.PossibleDrawTypes(sigs)) != 0)
				{
					int sigLength = _SignalLength;

					bool bRotated = sigs.IsTwelveLeads
								&&	((_DrawType != ECGDraw.ECGDrawType.Regular)
								||	 (sigLength == 9)
								||	 (sigLength == 10));

					if ((_PaperType == SupportedPaper.A4_Portrait)
					||	(_PaperType == SupportedPaper.LETTER_Portrait))
						bRotated = false;
					else if (_DrawSpeed != 25.0f)
						bRotated = false;

					Document document = new Document(bRotated ? _PageSize.Rotate() : _PageSize);
					
					float
						width = (float) Math.Round((document.PageSize.Width * PDFTool.mm_Per_Inch) / PDFTool.PdfDocumentDpi),
						height = (float) Math.Round((document.PageSize.Height * PDFTool.mm_Per_Inch) / PDFTool.PdfDocumentDpi);

					try
					{
						PdfWriter writer = PdfWriter.GetInstance(document, output);
                        bool bFirstA = true;

						document.AddTitle(_DocumentTitle);
						document.AddCreator(_DocumentCreator);
						document.AddAuthor(_DocumentAuthor);

						document.Open();

                        do
                        {
                            PdfContentByte cb = null;
                            if (bFirstA)
                            {
                                bFirstA = false;
                            }
                            else
                            {
                                document.NewPage();

                                if (_DrawType == ECGDraw.ECGDrawType.Median)
                                {
                                    if (!bs.LoadTemplate(signalPos++))
                                        return 4;
                                }
                                else
                                {
                                    if (!bs.LoadSignal(signalPos, signalPos + (nrSecs * sigs.RhythmSamplesPerSecond * signalMulti)))
                                        return 4;

                                    signalPos += (nrSecs * sigs.RhythmSamplesPerSecond * signalMulti);
                                }
                            }

                            cb = writer.DirectContent;

						cb.Transform(new System.Drawing.Drawing2D.Matrix(1, 0, 0, 1, 0, writer.PageSize.Height));

						cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.COURIER, BaseFont.CP1252, false), 8.0f);

						if (bRotated)
						{
							RectangleF gridRect = PDFTool.CreateRectangle(width, height, 260.0f, 160.0f, 165.0f);

							cb.SetLineWidth(0.5f);
							cb.SetRGBColorStroke(0, 0, 0);

							DrawPageHeader(cb, new RectangleF(gridRect.X, 5.0f, gridRect.Width, gridRect.Y - 5.0f), 3.5f);

							if (_DrawGrid)
							{
								cb.SetLineWidth(0.25f);
								cb.SetRGBColorStroke(0xff, 0xdd, 0xdd);
								PDFTool.DrawGrid(cb, 1.0f, gridRect);

								cb.SetLineWidth(0.5f);
								cb.SetRGBColorStroke(0xee, 0xbb, 0xbb);
								PDFTool.DrawGrid(cb, 5.0f, gridRect);
							}

							PointF point = new PointF(gridRect.X, gridRect.Y);

							cb.SetLineWidth(0.5f);
							cb.SetRGBColorStroke(0, 0, 0);

							cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.COURIER, BaseFont.CP1252, false), 8.0f);
							PDFTool.DrawGridHeader(cb, gridRect, 25.0f, _Gain);

							switch (_DrawType)
							{
								case ECGDraw.ECGDrawType.Regular:
								{
									point.Y += 15.0f;
                                            float fJump = sigs.IsFifteenLeads ? 10.0f : 12.5f;

                                            for (int i = 0,e = sigs.IsFifteenLeads ? 15 : 12; i < e; i++)
									{
										int nrSamples = 0;

										nrSamples = PDFTool.DrawSignal(cb, point, gridRect.Width, 25.0f, _Gain, sigs, i, nrSamples, 10.0f, true);

                                                point.Y += fJump;
									}
								}
								break;
								case ECGDraw.ECGDrawType.ThreeXFour:
								{
									point.Y += 25.0f;

                                            if (sigs.IsFifteenLeads)
                                            {
                                                PointF
                                                    point2 = new PointF(point.X + 60.0f, point.Y),
                                                    point3 = new PointF(point.X + 110.0f, point.Y),
                                                    point4 = new PointF(point.X + 160.0f, point.Y),
                                                    point5 = new PointF(point.X + 210.0f, point.Y);

                                                for (int i = 0; i < 3; i++)
                                                {
                                                    int nrSample = 0;

                                                    nrSample = PDFTool.DrawSignal(cb, point, (point2.X - point.X), 25.0f, _Gain, sigs, i, nrSample, 10.0f, true);
                                                    nrSample = PDFTool.DrawSignal(cb, point2, (point3.X - point2.X), 25.0f, _Gain, sigs, i + 3, nrSample, 0, true);
                                                    nrSample = PDFTool.DrawSignal(cb, point3, (point4.X - point3.X), 25.0f, _Gain, sigs, i + 6, nrSample, 0, true);
                                                    nrSample = PDFTool.DrawSignal(cb, point4, gridRect.Width - (point4.X - gridRect.X), 25.0f, _Gain, sigs, i + 9, nrSample, 0, true);
                                                    nrSample = PDFTool.DrawSignal(cb, point5, gridRect.Width - (point5.X - gridRect.X), 25.0f, _Gain, sigs, i + 12, nrSample, 0, true);

                                                    point.Y += 55.0f;
                                                    point2.Y += 55.0f;
                                                    point3.Y += 55.0f;
                                                    point4.Y += 55.0f;
                                                    point5.Y += 55.0f;
                                                }
                                            }
                                            else
                                            {
									PointF
										point2 = new PointF(point.X +  72.5f, point.Y),
										point3 = new PointF(point.X + 135.0f, point.Y),
										point4 = new PointF(point.X + 197.5f, point.Y);

									for (int i=0;i < 3;i++)
									{
										int nrSample = 0;

										nrSample = PDFTool.DrawSignal(cb, point, (point2.X - point.X), 25.0f, _Gain, sigs, i, nrSample, 10.0f, true);
										nrSample = PDFTool.DrawSignal(cb, point2, (point3.X - point2.X), 25.0f, _Gain, sigs, i + 3, nrSample, 0, true);
										nrSample = PDFTool.DrawSignal(cb, point3, (point4.X - point3.X), 25.0f, _Gain, sigs, i + 6, nrSample, 0, true);
										nrSample = PDFTool.DrawSignal(cb, point4, gridRect.Width - (point4.X - gridRect.X), 25.0f, _Gain, sigs, i + 9, nrSample, 0, true);

										point.Y += 55.0f;
										point2.Y += 55.0f;
										point3.Y += 55.0f;
										point4.Y += 55.0f;
									}
								}
                                        }
								break;
								case ECGDraw.ECGDrawType.ThreeXFourPlusOne:
								{
									point.Y += 20.0f;

                                            if (sigs.IsFifteenLeads)
                                            {
                                                PointF
                                                    point2 = new PointF(point.X + 60.0f, point.Y),
                                                    point3 = new PointF(point.X + 110.0f, point.Y),
                                                    point4 = new PointF(point.X + 160.0f, point.Y),
                                                    point5 = new PointF(point.X + 210.0f, point.Y);

                                                for (int i = 0; i < 3; i++)
                                                {
                                                    int nrSample = 0;

                                                    nrSample = PDFTool.DrawSignal(cb, point, (point2.X - point.X), 25.0f, _Gain, sigs, i, nrSample, 10.0f, true);
                                                    nrSample = PDFTool.DrawSignal(cb, point2, (point3.X - point2.X), 25.0f, _Gain, sigs, i + 3, nrSample, 0, true);
                                                    nrSample = PDFTool.DrawSignal(cb, point3, (point4.X - point3.X), 25.0f, _Gain, sigs, i + 6, nrSample, 0, true);
                                                    nrSample = PDFTool.DrawSignal(cb, point4, gridRect.Width - (point4.X - gridRect.X), 25.0f, _Gain, sigs, i + 9, nrSample, 0, true);
                                                    nrSample = PDFTool.DrawSignal(cb, point5, gridRect.Width - (point5.X - gridRect.X), 25.0f, _Gain, sigs, i + 12, nrSample, 0, true);

                                                    point.Y += 40.0f;
                                                    point2.Y += 40.0f;
                                                    point3.Y += 40.0f;
                                                    point4.Y += 40.0f;
                                                    point5.Y += 40.0f;
                                                }
                                            }
                                            else
                                            {
									PointF
										point2 = new PointF(point.X +  72.5f, point.Y),
										point3 = new PointF(point.X + 135.0f, point.Y),
										point4 = new PointF(point.X + 197.5f, point.Y);

									for (int i=0;i < 3;i++)
									{
										int nrSample = 0;

										nrSample = PDFTool.DrawSignal(cb, point, (point2.X - point.X), 25.0f, _Gain, sigs, i, nrSample, 10.0f, true);
										nrSample = PDFTool.DrawSignal(cb, point2, (point3.X - point2.X), 25.0f, _Gain, sigs, i + 3, nrSample, 0, true);
										nrSample = PDFTool.DrawSignal(cb, point3, (point4.X - point3.X), 25.0f, _Gain, sigs, i + 6, nrSample, 0, true);
										nrSample = PDFTool.DrawSignal(cb, point4, gridRect.Width - (point4.X - gridRect.X), 25.0f, _Gain, sigs, i + 9, nrSample, 0, true);

										point.Y += 40.0f;
										point2.Y += 40.0f;
										point3.Y += 40.0f;
										point4.Y += 40.0f;
									}
                                            }

									PDFTool.DrawSignal(cb, point, gridRect.Width, 25.0f, _Gain, sigs, extra != null ? extra[0] : 1, 0, 10.0f, true);
								}
								break;
								case ECGDraw.ECGDrawType.ThreeXFourPlusThree:
								{
									point.Y += 12.5f;

                                            if (sigs.IsFifteenLeads)
                                            {
                                                PointF
                                                    point2 = new PointF(point.X + 60.0f, point.Y),
                                                    point3 = new PointF(point.X + 110.0f, point.Y),
                                                    point4 = new PointF(point.X + 160.0f, point.Y),
                                                    point5 = new PointF(point.X + 210.0f, point.Y);

                                                for (int i = 0; i < 3; i++)
                                                {
                                                    int nrSample = 0;

                                                    nrSample = PDFTool.DrawSignal(cb, point, (point2.X - point.X), 25.0f, _Gain, sigs, i, nrSample, 10.0f, true);
                                                    nrSample = PDFTool.DrawSignal(cb, point2, (point3.X - point2.X), 25.0f, _Gain, sigs, i + 3, nrSample, 0, true);
                                                    nrSample = PDFTool.DrawSignal(cb, point3, (point4.X - point3.X), 25.0f, _Gain, sigs, i + 6, nrSample, 0, true);
                                                    nrSample = PDFTool.DrawSignal(cb, point4, gridRect.Width - (point4.X - gridRect.X), 25.0f, _Gain, sigs, i + 9, nrSample, 0, true);
                                                    nrSample = PDFTool.DrawSignal(cb, point5, gridRect.Width - (point5.X - gridRect.X), 25.0f, _Gain, sigs, i + 12, nrSample, 0, true);

                                                    point.Y += 27.5f;
                                                    point2.Y += 27.5f;
                                                    point3.Y += 27.5f;
                                                    point4.Y += 27.5f;
                                                    point5.Y += 27.5f;
                                                }
                                            }
                                            else
                                            {
									PointF
										point2 = new PointF(point.X +  72.5f, point.Y),
										point3 = new PointF(point.X + 135.0f, point.Y),
										point4 = new PointF(point.X + 197.5f, point.Y);

									for (int i=0;i < 3;i++)
									{
										int nrSample = 0;

										nrSample = PDFTool.DrawSignal(cb, point, (point2.X - point.X), 25.0f, _Gain, sigs, i, nrSample, 10.0f, true);
										nrSample = PDFTool.DrawSignal(cb, point2, (point3.X - point2.X), 25.0f, _Gain, sigs, i + 3, nrSample, 0, true);
										nrSample = PDFTool.DrawSignal(cb, point3, (point4.X - point3.X), 25.0f, _Gain, sigs, i + 6, nrSample, 0, true);
										nrSample = PDFTool.DrawSignal(cb, point4, gridRect.Width - (point4.X - gridRect.X), 25.0f, _Gain, sigs, i + 9, nrSample, 0, true);

										point.Y += 27.5f;
										point2.Y += 27.5f;
										point3.Y += 27.5f;
										point4.Y += 27.5f;
									}
                                            }

									PDFTool.DrawSignal(cb, point, gridRect.Width, 25.0f, _Gain, sigs, extra != null ? extra[0] : 1, 0, 10.0f, true);
									point.Y += 25.0f;
									PDFTool.DrawSignal(cb, point, gridRect.Width, 25.0f, _Gain, sigs, extra != null ? extra[1] : 7, 0, 10.0f, true);
									point.Y += 27.5f;
									PDFTool.DrawSignal(cb, point, gridRect.Width, 25.0f, _Gain, sigs, extra != null ? extra[2] : 10, 0, 10.0f, true);
								}
								break;
								case ECGDraw.ECGDrawType.SixXTwo:
								{
									point.Y += 12.5f;

									PointF point2 = new PointF(point.X + (gridRect.Width / 2.0f) + 5.0f, point.Y);

									for (int i=0;i < 6;i++)
									{
										int nrSample = 0;

										nrSample = PDFTool.DrawSignal(cb, point, (point2.X - point.X), 25.0f, _Gain, sigs, i, nrSample, 10.0f, true);
										nrSample = PDFTool.DrawSignal(cb, point2, gridRect.Width - (point2.X - gridRect.X), 25.0f, _Gain, sigs, i + 6, nrSample, 0, true);

										point.Y += 27.5f;
										point2.Y += 27.5f;
									}

								}
								break;
								case ECGDraw.ECGDrawType.Median:
								{
									point.X += 20.0f;
									point.Y += 25.0f;

                                            if (sigs.IsFifteenLeads)
                                            {
                                                PointF
                                                    point2 = new PointF(point.X + 50f, point.Y),
                                                    point3 = new PointF(point.X + 100f, point.Y),
                                                    point4 = new PointF(point.X + 150f, point.Y),
                                                    point5 = new PointF(point.X + 200f, point.Y);

                                                for (int i = -1; i > -4; i--)
                                                {
                                                    point.X -= 20.0f;

                                                    PDFTool.DrawCalibrationPulse(cb, point, 10.0f, _Gain);

                                                    point.X += 20.0f;

                                                    PDFTool.DrawSignal(cb, point, (point2.X - point.X), 25.0f, _Gain, sigs, i, 0, 0, true);
                                                    PDFTool.DrawSignal(cb, point2, (point3.X - point2.X), 25.0f, _Gain, sigs, i - 3, 0, 0, true);
                                                    PDFTool.DrawSignal(cb, point3, (point4.X - point3.X), 25.0f, _Gain, sigs, i - 6, 0, 0, true);
                                                    PDFTool.DrawSignal(cb, point4, (point5.X - point4.X), 25.0f, _Gain, sigs, i - 9, 0, 0, true);
                                                    PDFTool.DrawSignal(cb, point5, gridRect.Width - (point5.X - gridRect.X), 25.0f, _Gain, sigs, i - 12, 0, 0, true);

                                                    point.Y += 55.0f;
                                                    point2.Y += 55.0f;
                                                    point3.Y += 55.0f;
                                                    point4.Y += 55.0f;
                                                    point5.Y += 55.0f;
                                                }
                                            }
                                            else
                                            {
									PointF
										point2 = new PointF(point.X +  65f, point.Y),
										point3 = new PointF(point.X + 130f, point.Y),
										point4 = new PointF(point.X + 195f, point.Y);

									for (int i=-1;i > -4;i--)
									{
										point.X -= 20.0f;

										PDFTool.DrawCalibrationPulse(cb, point, 10.0f, _Gain);

										point.X += 20.0f;

										PDFTool.DrawSignal(cb, point, (point2.X - point.X), 25.0f, _Gain, sigs, i, 0, 0, true);
										PDFTool.DrawSignal(cb, point2, (point3.X - point2.X), 25.0f, _Gain, sigs, i - 3, 0, 0, true);
										PDFTool.DrawSignal(cb, point3, (point4.X - point3.X), 25.0f, _Gain, sigs, i - 6, 0, 0, true);
										PDFTool.DrawSignal(cb, point4, gridRect.Width - (point4.X - gridRect.X), 25.0f, _Gain, sigs, i - 9, 0, 0, true);

										point.Y += 55.0f;
										point2.Y += 55.0f;
										point3.Y += 55.0f;
										point4.Y += 55.0f;
									}
								}
                                        }
								break;
								default:
									return 2;
							}
						}
						else if (((sigLength <= 8)
							||	 (_PaperType == SupportedPaper.A4_Portrait)
							||	 (_PaperType == SupportedPaper.LETTER_Portrait))
							&&	 sigs.IsTwelveLeads
							&&	 (_DrawSpeed == 25.0f))
						{
							RectangleF gridRect = ((_PaperType == SupportedPaper.LETTER) || (_PaperType == SupportedPaper.LETTER_Portrait)) ? PDFTool.CreateRectangle(width, height, 200.0f, (15.0f * sigs.NrLeads) + 20.0f, 230.0f) : PDFTool.CreateRectangle(width, height, 200.0f, (20.0f * sigs.NrLeads) + 15.0f, 257.5f); 

							cb.SetLineWidth(0.5f);
							cb.SetRGBColorStroke(0, 0, 0);

							DrawPageHeader(cb, new RectangleF(gridRect.X, 5.0f, gridRect.Width, gridRect.Y - 5.0f), 3.5f);

							if (_DrawGrid)
							{
								cb.SetLineWidth(0.25f);
								cb.SetRGBColorStroke(0xff, 0xdd, 0xdd);
								PDFTool.DrawGrid(cb, 1.0f, gridRect);

								cb.SetLineWidth(0.5f);
								cb.SetRGBColorStroke(0xee, 0xbb, 0xbb);
								PDFTool.DrawGrid(cb, 5.0f, gridRect);
							}

							PointF point = new PointF(gridRect.X, gridRect.Y + 15.0f);

							cb.SetLineWidth(0.5f);
							cb.SetRGBColorStroke(0, 0, 0);

							cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.COURIER, BaseFont.CP1252, false), 8.0f);
							PDFTool.DrawGridHeader(cb, gridRect, 25.0f, _Gain);

                                float fJump = sigs.IsFifteenLeads ? 16f : 20f;

                                if ((_PaperType == SupportedPaper.LETTER)
                                || (_PaperType == SupportedPaper.LETTER_Portrait))
                                    fJump = sigs.IsFifteenLeads ? 12f : 15f;

							for (int i=0;i < sigs.NrLeads;i++)
							{
								PDFTool.DrawSignal(cb, point, gridRect.Width, 25.0f, _Gain, sigs, i, 0, 0.0f, false);

                                    point.Y += fJump;
							}

							point.X = gridRect.X + 5.0f;
							point.Y = gridRect.Bottom;

							PDFTool.DrawCalibrationPulse(cb, point, 10.0f, _Gain);
						}
						else
						{
							int start, end, prev = -1;
                                bool bFirstB = true;

							sigs.CalculateStartAndEnd(out start, out end);

//							if (start > 0)
//								end -= start;
//							start = 0;
							end--;

							RectangleF gridRect = new RectangleF(0, 0, width, height);

							DateTime dtStart = _Demographics.TimeAcquisition;

							float
								fStartY = 15.0f,
								fIncrement = 20.0f;

							if (sigs.NrLeads > 12)
							{
								float fMaxHeight = (((_PaperType == SupportedPaper.LETTER) || (_PaperType == SupportedPaper.LETTER_Portrait)) ? 240.0f : 255.0f) - 20.0f;

								fStartY = 15.0f;

								fIncrement = (fMaxHeight / (sigs.NrLeads-1));
							}

							while ((start < end)
								&& (start != prev))
							{
								if (gridRect.Bottom > (height - 5.0f))
								{
                                        if (bFirstB)
                                            bFirstB = false;
									else
									{
										document.NewPage();

										cb = writer.DirectContent;

										cb.Transform(new System.Drawing.Drawing2D.Matrix(1, 0, 0, 1, 0, writer.PageSize.Height));

										cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.COURIER, BaseFont.CP1252, false), 8.0f);
									}
									
									cb.SetLineWidth(0.5f);
									cb.SetRGBColorStroke(0, 0, 0);

									gridRect = ((_PaperType == SupportedPaper.LETTER) || (_PaperType == SupportedPaper.LETTER_Portrait)) ? PDFTool.CreateRectangle(width, height, 205.0f, (20.0f * sigs.NrLeads), 240.0f) : PDFTool.CreateRectangle(width, height, 180.0f, (20.0f * sigs.NrLeads) + 5.0f, 255.0f); 

									DrawPageHeader(cb, new RectangleF(gridRect.X, 5.0f, gridRect.Width, gridRect.Y - 5.0f), 3.5f);
								}

								if (_DrawGrid)
								{
									cb.SetLineWidth(0.25f);
									cb.SetRGBColorStroke(0xff, 0xdd, 0xdd);
									PDFTool.DrawGrid(cb, 1.0f, gridRect);

									cb.SetLineWidth(0.5f);
									cb.SetRGBColorStroke(0xee, 0xbb, 0xbb);
									PDFTool.DrawGrid(cb, 5.0f, gridRect);
								}

								PointF point = new PointF(gridRect.X, gridRect.Y + fStartY);

								cb.SetLineWidth(0.5f);
								cb.SetRGBColorStroke(0, 0, 0);

								cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.COURIER, BaseFont.CP1252, false), 8.0f);
								PDFTool.DrawGridHeader(cb, gridRect, dtStart.AddMilliseconds((start * 1000.0) / sigs.RhythmSamplesPerSecond), _DrawSpeed, _Gain);

								int temp = 0;

								for (int i=0;i < sigs.NrLeads;i++)
								{
									temp = Math.Max(temp, PDFTool.DrawSignal(cb, point, gridRect.Width, _DrawSpeed, _Gain, sigs, i, start, 5.0f, false));

									point.Y += fIncrement;
								}

								prev = start;
								start = temp;
								gridRect.Y += gridRect.Height + 5.0f;
							}
						}
                        }
                        while (signalPos < signalEnd);

						return 0;
					}
					catch(DocumentException de) 
					{
						Console.Error.WriteLine(de.ToString());
					}
					catch(IOException ioe) 
					{
						Console.Error.WriteLine(ioe.ToString());
					}
					catch (Exception ex)
					{
						Console.Error.WriteLine(ex.ToString());
					}
					finally
					{
						document.Close();
					}
				}
			}

			return 0x1;
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

			return 0x1;
		}
		public override bool CheckFormat(Stream input, int offset)
		{
			return false;
		}
		public override bool CheckFormat(string file, int offset)
		{
			return false;
		}
		public override bool CheckFormat(byte[] buffer, int offset)
		{
			return false;
		}
		public override void Anonymous(byte type)
		{
			ECGTool.Anonymous(_Demographics, (char) type);
		}
		public override int getFileSize()
		{
			return -1;
		}
		private static int[] FindExtraLeads(Signals sigs, LeadType[] lta)
		{
			int[] ret = null;

			if ((sigs != null)
			&&	(lta != null))
			{
				ret = new int[lta.Length];

				for (int i=0;i < lta.Length;i++)
				{
					ret[i] = int.MaxValue;
					for (int j=0;j < sigs.NrLeads;j++)
					{
						if (lta[i] == sigs[j].Type)
						{
							ret[i] = j;
							break;
						}
					}
				}
			}

			return ret;
		}
		private void DrawPageHeader(PdfContentByte cb, RectangleF headerRect, float fLineHeight)
		{
			if (_HeaderImagePath != null)
			{
				try
				{
					iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(_HeaderImagePath);

					if (image != null)
					{
						image.ScaleToFit((30f * PDFTool.PdfDocumentDpi) / PDFTool.mm_Per_Inch, (40f * PDFTool.PdfDocumentDpi) / PDFTool.mm_Per_Inch);

						image.SetAbsolutePosition(
							((headerRect.Right * PDFTool.PdfDocumentDpi) / PDFTool.mm_Per_Inch) - image.ScaledWidth,
							(-headerRect.Bottom * PDFTool.PdfDocumentDpi) / PDFTool.mm_Per_Inch);
						
						cb.AddImage(image);
					}
				}
				catch {}
			}

			cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.COURIER, BaseFont.CP1252, false), 8.0f);

			StringBuilder sb = new StringBuilder();

			PointF point = new PointF(headerRect.X + 1.25f, headerRect.Y);
			float fTempY = point.Y;

			sb.Append("Name:       ").Append(_Demographics.LastName);
			sb.Append("\nPatient ID: ").Append(_Demographics.PatientID);

			GlobalMeasurements gms = _GlobalMeasurements;

			if ((gms != null)
			&&	(gms.measurment != null)
			&&	(gms.measurment.Length > 0)
			&&	(gms.measurment[0] != null))
			{
				int ventRate = (gms.VentRate == GlobalMeasurement.NoValue) ? 0 : (int) gms.VentRate,
					PRint = (gms.PRint == GlobalMeasurement.NoValue) ? 0 : (int) gms.measurment[0].PRint,
					QRSdur = (gms.QRSdur == GlobalMeasurement.NoValue) ? 0 : (int) gms.measurment[0].QRSdur,
					QT = (gms.QTdur == GlobalMeasurement.NoValue) ? 0 : (int) gms.measurment[0].QTdur,
					QTc = (gms.QTc == GlobalMeasurement.NoValue) ? 0 : (int) gms.QTc;
				
				sb.Append("\n\nVent rate:      ");
				PrintValue(sb, ventRate, 3);
				sb.Append(" BPM");
					
				sb.Append("\nPR int:         ");
				PrintValue(sb, PRint, 3);
				sb.Append(" ms");

				sb.Append("\nQRS dur:        ");
				PrintValue(sb, QRSdur, 3);
				sb.Append(" ms");

				sb.Append("\nQT/QTc:     ");
				PrintValue(sb, QT, 3);
				sb.Append('/');
				PrintValue(sb, QTc, 3);
				sb.Append(" ms");

				sb.Append("\nP-R-T axes: ");
				sb.Append((gms.measurment[0].Paxis != GlobalMeasurement.NoAxisValue) ? gms.measurment[0].Paxis.ToString() : "999");
				sb.Append(' ');
				sb.Append((gms.measurment[0].QRSaxis != GlobalMeasurement.NoAxisValue) ? gms.measurment[0].QRSaxis.ToString() : "999");
				sb.Append(' ');
				sb.Append((gms.measurment[0].Taxis != GlobalMeasurement.NoAxisValue) ? gms.measurment[0].Taxis.ToString() : "999");
			}

			DrawText(cb, point, sb, fLineHeight, 50.0f);

			point.X += 52.5f;

			DateTime dt = _Demographics.TimeAcquisition;

			DrawText(cb, point,	new string[] {(dt.Year > 1000) ? dt.ToString("dd/MM/yyyy HH:mm:ss") : "Time Unknown"}, fLineHeight, 35.0f);

			point.X += 2.5f;
			point.Y += 2 * fLineHeight;

			sb = new StringBuilder();

			sb.Append("DOB:  ");

			ECGConversion.ECGDemographics.Date birthDate = _Demographics.PatientBirthDate;
			if (birthDate != null)
			{
				sb.Append(birthDate.Day.ToString("00"));
				sb.Append(birthDate.Month.ToString("00"));
				sb.Append(birthDate.Year.ToString("0000"));
			}

			sb.Append("\nAge:  ");

			ushort ageVal;
			ECGConversion.ECGDemographics.AgeDefinition ad;

			if (_Demographics.getPatientAge(out ageVal, out ad) == 0)
			{
				sb.Append(ageVal);

				if (ad != ECGConversion.ECGDemographics.AgeDefinition.Years)
				{
					sb.Append(" ");
					sb.Append(ad.ToString());
				}
			}
			else
				sb.Append("0");

			sb.Append("\nGen:  ");
			if (_Demographics.Gender != ECGConversion.ECGDemographics.Sex.Null)
				sb.Append(_Demographics.Gender.ToString());
			sb.Append("\nDep:  ");
			sb.Append(_Demographics.AcqDepartment);

			DrawText(cb, point, sb, fLineHeight, 32.0f);

			point.X += 35.0f;
			point.Y = fTempY;

			if ((_Diagnostic != null)
			&&	(_Diagnostic.statement != null)
            &&  (_Diagnostic.statement.Length > 0))
			{
				sb = new StringBuilder();

				foreach (string temp in _Diagnostic.statement)
				{
					sb.Append(temp);
					sb.Append('\n');
				}

				string temp2 = _Diagnostic.statement[_Diagnostic.statement.Length-1];

				if (temp2 != null)
				{
					temp2 = temp2.ToLower();

					if (!temp2.StartsWith("confirmed by")
					&&	!temp2.StartsWith("interpreted by")
					&&	!temp2.StartsWith("reviewed by"))
					{
						if ((_Demographics.OverreadingPhysician != null)
						&&	(_Demographics.OverreadingPhysician.Length != 0))
						{
							if (_Diagnostic.confirmed)
								sb.Append("\nConfirmed by ");
							else if (_Diagnostic.interpreted)
								sb.Append("\nInterpreted by ");
							else
								sb.Append("\nReviewed by ");

							sb.Append(_Demographics.OverreadingPhysician);

						}
						else
							sb.Append("\nUNCONFIRMED AUTOMATED INTERPRETATION");
					}
				}

				DrawText(cb, point, sb, fLineHeight, headerRect.Right - point.X);
			}

			FreeTextSpec[] fts = _FreeText;

			if (fts != null)
			{
				foreach (FreeTextSpec ft in fts)
				{
					cb.SetFontAndSize(BaseFont.CreateFont(BaseFont.COURIER, BaseFont.CP1252, false), ft.FontSize);

					DrawText(cb, ft.Point, new StringBuilder(ft.Text), (fLineHeight * ft.FontSize) / 8.0f, headerRect.Width);
				}
			}
		}
		private static void PrintValue(StringBuilder sb, int val, int len)
		{
			int temp = sb.Length;
			sb.Append(val.ToString());
			if ((sb.Length - temp) < len)
				sb.Append(' ', len - (sb.Length - temp));
		}
		public static void DrawText(PdfContentByte cb, PointF point, StringBuilder sb, float fLineHeight, float fMaxWidth)
		{
			if ((cb != null)
			&&	(sb != null)
			&&	(sb.Length > 0))
				DrawText(cb, point, sb.ToString().Split('\n'), fLineHeight, fMaxWidth);
		}
		public static void DrawText(PdfContentByte cb, PointF point, string[] strs, float fLineHeight, float fMaxWidth)
		{
			fMaxWidth = (fMaxWidth * PDFTool.PdfDocumentDpi) / PDFTool.mm_Per_Inch;

			if ((strs != null)
			&&	(strs.Length != 0))
			{
				cb.BeginText();

				ArrayList allLines = null;

				if ((Math.Abs(cb.GetEffectiveStringWidth("i", false) - cb.GetEffectiveStringWidth("w", false)) < 0.01f )
				&&	(fMaxWidth > 0.1f))
				{
					float c = cb.GetEffectiveStringWidth("i", false);

					allLines = new ArrayList();

					foreach (string line in strs)
					{
						if ((line != null)
						&&	(line.Length != 0)
						&&	((c * line.Length) > fMaxWidth))
						{
							int maxLineSize = (int) Math.Floor(fMaxWidth / c);

							StringBuilder sb = new StringBuilder(line);

							while (sb.Length > 0)
							{
								if (maxLineSize >= sb.Length)
								{
									allLines.Add(sb.ToString());
									sb.Remove(0, sb.Length);
								}
								else
								{
									int i=maxLineSize-1;

									for (;i > 0;i--)
										if (sb[i] == ' ')
										{
											break;
										}

									if (i > 0)
									{
										allLines.Add(sb.ToString(0, i));
										sb.Remove(0, i+1);
									}
									else
									{
										allLines.Add(sb.ToString(0, maxLineSize));
										sb.Remove(0, maxLineSize);
									}
								}
							}
						}
						else
						{
							allLines.Add(line);
						}
					}
				}
				else
				{
					allLines = new ArrayList(strs);
				}

				foreach (string line in allLines)
				{
					point.Y += fLineHeight;

					if ((line == null)
					||	(line.Length == 0))
						continue;

					cb.ShowTextAligned(
						PdfContentByte.ALIGN_LEFT,
						line,
						(point.X * PDFTool.PdfDocumentDpi) / PDFTool.mm_Per_Inch,
						(-point.Y * PDFTool.PdfDocumentDpi) / PDFTool.mm_Per_Inch,
						0);
				}

				cb.EndText();
			}
		}
		public override ECGConversion.ECGDemographics.IDemographic Demographics
		{
			get
			{
				return _Demographics;
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
		public override bool Works()
		{
			return _Demographics.Works()
				&& (_Signals != null);
		}
		public override void Empty()
		{
			_Demographics = new PDFDemographics();
			_Signals = null;
			_GlobalMeasurements = null;
			_Diagnostic = null;
		}
		#endregion

		#region ISignal Members

		public int getSignalsToObj(Signals signals)
		{
			return 1;
		}

		public int getSignals(out Signals signals)
		{
			signals = null;
			return 1;
		}

		public int setSignals(Signals signals)
		{
			if (signals != null)
			{
				_Signals =  signals;

				return 0;
			}

			return 1;
		}

		#endregion

		#region IGlobalMeasurement Members

		public int getGlobalMeasurements(out GlobalMeasurements mes)
		{
			mes = null;
			return 1;
		}

		public int setGlobalMeasurements(GlobalMeasurements mes)
		{
			if (mes != null)
			{
				_GlobalMeasurements = mes;

				return 0;
			}

			return 1;
		}

		#endregion

		#region IDiagnostic Members

		public int getDiagnosticStatements(out Statements stat)
		{
			stat = null;
			return 1;
		}

		public int setDiagnosticStatements(Statements stat)
		{
			if (stat != null)
			{
				_Diagnostic = stat;

				return 0;
			}

			return 1;
		}

		#endregion

		#region IDisposable Members
		public override void Dispose()
		{
			base.Dispose();

			_Demographics = null;
			_Signals = null;
			_GlobalMeasurements = null;
			_Diagnostic = null;
		}
		#endregion
	}
}
