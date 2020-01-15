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
using System.Drawing;
using iTextSharp.text.pdf;

namespace ECGConversion
{
	/// <summary>
	/// PDFTool hold function for drawing of ECG in PDF.
	/// </summary>
	public class PDFTool
	{
		public const float PdfDocumentDpi = 72.0f;
		public const float mm_Per_Inch = ECGConversion.ECGDraw.mm_Per_Inch;
		/// <summary>
		/// Function to make a nice rectangle for a grid.
		/// </summary>
		/// <param name="totalWidth">width of paper</param>
		/// <param name="totalHeight">height of paper</param>
		/// <param name="width">width of grid</param>
		/// <param name="height">height of grid</param>
		/// <param name="maxHeight">max height of grid</param>
		/// <returns>rectangle describing the grid</returns>
		public static RectangleF CreateRectangle(float totalWidth, float totalHeight, float width, float height, float maxHeight)
		{
			if (height > maxHeight)
				height = maxHeight;

			return new RectangleF((totalWidth - width) / 2f, (totalHeight - maxHeight) - 5f, width, height);
		}
		/// <summary>
		/// Function to draw a nice grid.
		/// </summary>
		/// <param name="cb">content byte to manipulate the a part of a pdf document</param>
		/// <param name="mm_Per_Line">mm between each line</param>
		/// <param name="location">location (rectangle in mm) to draw grid in</param>
		public static void DrawGrid(PdfContentByte cb, float mm_Per_Line, RectangleF location)
		{
			float
				fMinX = location.X,
				fMinY = location.Y,
				fMaxX = location.Right,
				fMaxY = location.Bottom;

			// draw vertical lines.
			for (float fI=fMinX;fI <= fMaxX;fI+=mm_Per_Line)
			{
				cb.MoveTo((fI * PdfDocumentDpi) / mm_Per_Inch, -(fMinY * PdfDocumentDpi) / mm_Per_Inch);
				cb.LineTo((fI * PdfDocumentDpi) / mm_Per_Inch, -(fMaxY * PdfDocumentDpi) / mm_Per_Inch);
			}

			// draw horizontal lines.
			for (float fI=fMinY;fI <= fMaxY;fI+=mm_Per_Line)
			{
				cb.MoveTo((fMinX * PdfDocumentDpi) / mm_Per_Inch, -(fI * PdfDocumentDpi) / mm_Per_Inch);
				cb.LineTo((fMaxX * PdfDocumentDpi) / mm_Per_Inch, -(fI * PdfDocumentDpi) / mm_Per_Inch);
			}

			cb.Stroke();
		}
		/// <summary>
		/// Draw signal of a lead
		/// </summary>
		/// <param name="cb">content byte to manipulate the a part of a pdf document</param>
		/// <param name="point">point (in mm) to start drawing at</param>
		/// <param name="fWidth">width (in mm) for drawing of signal</param>
		/// <param name="mm_Per_s">mm per second</param>
		/// <param name="mm_Per_mV">mm per milliVolt</param>
		/// <param name="signals">the signals object</param>
		/// <param name="nLead">lead number to draw (nLead &lt; 0 is Median data with -1 as lead 0)</param>
		/// <param name="nSample">sample number to start drawing at.</param>
		/// <param name="fCalibrationWidth">width (in mm) of calibration pulse </param>
		/// <param name="bLimitLines">true if need to draw limit lines</param>
		/// <returns>returns last drawn sample nr.</returns>
		public static int DrawSignal(PdfContentByte cb, PointF point, float fWidth, float mm_Per_s, float mm_Per_mV, ECGConversion.ECGSignals.Signals signals, int nLead, int nSample, float fCalibrationWidth, bool bLimitLines)
		{
			float
				fX = point.X,
				fY = -point.Y,
				fOrignalY = fY,
				fMinX = fX,
				fMaxX = fX + fWidth,
				fSpecial = bLimitLines ? 11.0f : 7.0f;

			if (fCalibrationWidth > 2.5f)
				fMinX += DrawCalibrationPulse(cb, point, fCalibrationWidth, mm_Per_mV);

			bool bMedian = (nLead < 0);

			if (bMedian)
				nLead = Math.Abs(nLead) - 1;

			try
			{
				string sType = (signals[nLead].Type != ECGConversion.ECGSignals.LeadType.Unknown)
					?	signals[nLead].Type.ToString()
					:	"Channel " + (nLead + 1);

				cb.BeginText();
				cb.ShowTextAligned(
					PdfContentByte.ALIGN_LEFT,
					sType,
					((fMinX + 1.0f) * PdfDocumentDpi) / mm_Per_Inch,
					((fY + fSpecial) * PdfDocumentDpi) / mm_Per_Inch,
					0);
				cb.EndText();
			}
			catch {}

			int RS, RE;

			if (bMedian)
			{
				RS = 0;
				RE = (int) (signals.MedianLength * signals.MedianSamplesPerSecond * 0.001f) + 1;
			}
			else
			{
				RS = signals[nLead].RhythmStart;
				RE = signals[nLead].RhythmEnd;
			}

			short[] tempSignal = bMedian ? signals[nLead].Median : signals[nLead].Rhythm;

			for (int nOldSample = nSample;(nSample < RE) && (fX < fMaxX);nSample++)
			{
				fX = fMinX + ((nSample - nOldSample) * mm_Per_s) / signals.RhythmSamplesPerSecond;
				fY = fOrignalY;

				if ((nSample >= RS)
				&&	(nSample < RE)
				&&	(nSample >= RS)
				&&	((nSample - RS) < tempSignal.Length))
					fY += (float) (tempSignal[nSample - RS] * signals.RhythmAVM * mm_Per_mV * 0.001f);
				else
					fY = float.NaN;

				if (float.IsNaN(fY))
				{
					cb.MoveTo((fX * PdfDocumentDpi) / mm_Per_Inch, (fOrignalY * PdfDocumentDpi) / mm_Per_Inch);

					nOldSample = nSample + 1;
				}
				else if (nSample == nOldSample)
					cb.MoveTo((fX * PdfDocumentDpi) / mm_Per_Inch, (fY * PdfDocumentDpi) / mm_Per_Inch);
				else
					cb.LineTo((fX * PdfDocumentDpi) / mm_Per_Inch, (fY * PdfDocumentDpi) / mm_Per_Inch);
			}

			cb.MoveTo((fMinX * PdfDocumentDpi) / mm_Per_Inch, ((fOrignalY + fSpecial) * PdfDocumentDpi) / mm_Per_Inch);
			cb.LineTo((fMinX * PdfDocumentDpi) / mm_Per_Inch, ((fOrignalY + fSpecial + 2.5f) * PdfDocumentDpi) / mm_Per_Inch);

			if (bLimitLines)
			{
				if (bMedian)
				{
					cb.MoveTo((fX * PdfDocumentDpi) / mm_Per_Inch, ((fOrignalY + fSpecial) * PdfDocumentDpi) / mm_Per_Inch);
					cb.LineTo((fX * PdfDocumentDpi) / mm_Per_Inch, ((fOrignalY + fSpecial + 2.5f) * PdfDocumentDpi) / mm_Per_Inch);
				}
				else
				{
					cb.MoveTo((fMaxX * PdfDocumentDpi) / mm_Per_Inch, ((fOrignalY + fSpecial) * PdfDocumentDpi) / mm_Per_Inch);
					cb.LineTo((fMaxX * PdfDocumentDpi) / mm_Per_Inch, ((fOrignalY + fSpecial + 2.5f) * PdfDocumentDpi) / mm_Per_Inch);
				}
			}

			cb.Stroke();

			return nSample - 1;
		}
		/// <summary>
		/// Draw a calibration pulse.
		/// </summary>
		/// <param name="cb">content byte to manipulate the a part of a pdf document</param>
		/// <param name="point">point (in mm) to start pulse at</param>
		/// <param name="fWidth">width (in mm) of pulse </param>
		/// <param name="mm_Per_mV">mm Per milliVolt</param>
		/// <returns>returns a the width of the calibration pulse.</returns>
		public static float DrawCalibrationPulse(PdfContentByte cb, PointF point, float fWidth, float mm_Per_mV)
		{
			float
				fX = point.X,
				fY = -point.Y;

			if (fWidth > 5.0f)
			{
				cb.MoveTo((fX * PdfDocumentDpi) / mm_Per_Inch, (fY * PdfDocumentDpi) / mm_Per_Inch);
				fX += fWidth * 0.2f;
				cb.LineTo((fX * PdfDocumentDpi) / mm_Per_Inch, (fY * PdfDocumentDpi) / mm_Per_Inch);
				fY += mm_Per_mV;
				cb.LineTo((fX * PdfDocumentDpi) / mm_Per_Inch, (fY * PdfDocumentDpi) / mm_Per_Inch);
				fX += fWidth * 0.6f;
				cb.LineTo((fX * PdfDocumentDpi) / mm_Per_Inch, (fY * PdfDocumentDpi) / mm_Per_Inch);
				fY -= mm_Per_mV;
				cb.LineTo((fX * PdfDocumentDpi) / mm_Per_Inch, (fY * PdfDocumentDpi) / mm_Per_Inch);
				fX += fWidth * 0.2f;
				cb.LineTo((fX * PdfDocumentDpi) / mm_Per_Inch, (fY * PdfDocumentDpi) / mm_Per_Inch);
			}
			else
			{
				cb.MoveTo((fX * PdfDocumentDpi) / mm_Per_Inch, (fY * PdfDocumentDpi) / mm_Per_Inch);
				fX += fWidth * 0.4f;
				cb.LineTo((fX * PdfDocumentDpi) / mm_Per_Inch, (fY * PdfDocumentDpi) / mm_Per_Inch);
				fY += mm_Per_mV;
				cb.LineTo((fX * PdfDocumentDpi) / mm_Per_Inch, (fY * PdfDocumentDpi) / mm_Per_Inch);
				fX += fWidth * 0.6f;
				cb.LineTo((fX * PdfDocumentDpi) / mm_Per_Inch, (fY * PdfDocumentDpi) / mm_Per_Inch);
				fY -= mm_Per_mV;
				cb.LineTo((fX * PdfDocumentDpi) / mm_Per_Inch, (fY * PdfDocumentDpi) / mm_Per_Inch);
			}

			cb.Stroke();

			return fWidth;
		}
		/// <summary>
		/// Draw the header of the grid.
		/// </summary>
		/// <param name="cb">content byte to manipulate the a part of a pdf document</param>
		/// <param name="location">location (rectangle in mm) of the grid</param>
		/// <param name="dtTimeStamp">Time stamp of the beginning of the signal</param>
		/// <param name="mm_Per_s">mm per second</param>
		/// <param name="mm_Per_mV">mm per milliVolt</param>
        public static void DrawGridHeader(PdfContentByte cb, RectangleF location, DateTime dtTimeStamp, float mm_Per_s, float mm_Per_mV, double bottomCutoff, double topCutoff)
        {
            try
            {
                float
                    fX = (location.X * PdfDocumentDpi) / mm_Per_Inch,
                    fY = (-location.Y * PdfDocumentDpi) / mm_Per_Inch;

                string sText = dtTimeStamp.ToLongTimeString();

                cb.BeginText();

                if (dtTimeStamp.Year >= 1500)
                    sText = dtTimeStamp.ToShortDateString() + " " + sText;

                cb.ShowTextAligned(
                    PdfContentByte.ALIGN_LEFT,
                    sText,
                    fX,
                    fY,
                    0);

                fX = (location.Right * PdfDocumentDpi) / mm_Per_Inch;

                System.Text.StringBuilder sbText = new System.Text.StringBuilder();

                if (!double.IsNaN(bottomCutoff))
                {
                    if (!double.IsNaN(topCutoff))
                    {
                        sbText.AppendFormat("{0}-{1} Hz, ", bottomCutoff, topCutoff);
                    }
                    else
                    {
                        sbText.AppendFormat("{0}-inf Hz, ", bottomCutoff);
                    }
                }
                else if (!double.IsNaN(topCutoff))
                {
                    sbText.AppendFormat("0-{0} Hz, ", topCutoff);
                }

                sbText.AppendFormat("{0:0} mm/s, {1:0} mm/mV ", mm_Per_s, mm_Per_mV);

                sText = sbText.ToString();

                cb.ShowTextAligned(
                    PdfContentByte.ALIGN_RIGHT,
                    sText,
                    fX,
                    fY,
                    0);

                cb.EndText();
            }
            catch { }
        }
		/// <summary>
		/// Draw the header of the grid.
		/// </summary>
		/// <param name="cb">content byte to manipulate the a part of a pdf document</param>
		/// <param name="location">location (rectangle in mm) of the grid</param>
		/// <param name="mm_Per_s">mm per second</param>
		/// <param name="mm_Per_mV">mm per milliVolt</param>
        /// <param name="bottomCuttoff">bottom cutoff of used filter</param>
        /// <param name="topCutoff">top butoff of used filter</param>
        public static void DrawGridHeader(PdfContentByte cb, RectangleF location, float mm_Per_s, float mm_Per_mV, double bottomCutoff, double topCutoff)
        {
            try
            {
                cb.BeginText();

                System.Text.StringBuilder sbText = new System.Text.StringBuilder();

                if (!double.IsNaN(bottomCutoff))
                {
                    if (!double.IsNaN(topCutoff))
                    {
                        sbText.AppendFormat("{0}-{1} Hz, ", bottomCutoff, topCutoff);
                    }
                    else
                    {
                        sbText.AppendFormat("{0}-inf Hz, ", bottomCutoff);
                    }
                }
                else if (!double.IsNaN(topCutoff))
                {
                    sbText.AppendFormat("0-{0} Hz, ", topCutoff);
                }

                sbText.AppendFormat("{0:0} mm/s, {1:0} mm/mV ", mm_Per_s, mm_Per_mV);

                string sText = sbText.ToString();

                cb.ShowTextAligned(
                    PdfContentByte.ALIGN_RIGHT,
                    sText,
                    (location.Right * PdfDocumentDpi) / mm_Per_Inch,
                    (-(location.Bottom + 2.0f) * PdfDocumentDpi) / mm_Per_Inch,
                    0);

                cb.EndText();
            }
            catch { }
        }
		/// <summary>
		/// Look for the end of a pdf document.
		/// </summary>
		/// <param name="abBuffer">buffer to look for the end.</param>
		/// <param name="offset">offset to start looking from.</param>
		/// <returns>end of pdf file.</returns>
		public static int LookForPdfEnd(byte[] abBuffer, int offset)
		{
			if (offset >= 0)
			{
				byte[] test = {0x45, 0x4f, 0x46, 0x0A};

				int end = abBuffer.Length - test.Length; 

				for (;offset < end;offset++)
				{
					int i=0;

					for (;i < test.Length;i++)
						if (abBuffer[offset+i] != test[i])
							break;

					if (i == test.Length)
					{
						offset+=test.Length;
						break;
					}
				}
			}

			return offset;
		}
	}
}
