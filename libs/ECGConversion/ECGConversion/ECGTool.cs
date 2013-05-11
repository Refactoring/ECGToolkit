/***************************************************************************
Copyright 2012-2013, van Ettinger Information Technology, Lopik, The Netherlands
Copyright 2004,2008-2010, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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

namespace ECGConversion
{
	/// <summary>
	/// a tool for calculating some leads.
	/// </summary>
	public class ECGTool
	{
		// const limit for the holter resample algorithm
		public const int MAX_HOLTER_SPS = 25;
		public const int MIN_HOLTER_SPS = 10;

		/// <summary>
		///  Interval to use for Resampling with polynomial in msec.
		/// </summary>
		public static int ResampleInterval = 20;
		public static double[][] Fast = null;
		
		public static bool LeadSubtract(short[] leadA, short[] leadB)
		{
			if ((leadA != null)
			&&  (leadB != null)
			&&	(leadA.Length == leadB.Length))
			{
				_LeadSubtract(leadA, leadB, leadA.Length);
				
				return true;
			}
			
			return false;
		}
		
		private unsafe static void _LeadSubtract(short[] leadA, short[] leadB, int length)
		{
			fixed (short* pLeadA = leadA, pLeadB = leadB)
			{
				for (int loper=0;loper < length;loper++)
				{
					pLeadA[loper] -= pLeadB[loper];
				}
			}
		}
		
		public static bool LeadAdd(short[] leadA, short[] leadB)
		{
			if ((leadA != null)
			&&  (leadB != null)
			&&	(leadA.Length == leadB.Length))
			{
				_LeadAdd(leadA, leadB, leadA.Length);
				
				return true;
			}
			
			return false;
		}
		
		private unsafe static void _LeadAdd(short[] leadA, short[] leadB, int length)
		{
			fixed (short* pLeadA = leadA, pLeadB = leadB)
			{
				for (int loper=0;loper < length;loper++)
				{
					pLeadA[loper] += pLeadB[loper];
				}
			}
		}

		/// <summary>
		/// Calculate four leads from lead I, II and (optionaly) III.
		/// </summary>
		/// <returns>error:
		/// 0x00) succes
		/// 0x01) given leads are wrong
		/// 0x02) error during caculation lead III
		/// 0x04) error during caculation lead aVR
		/// 0x08) error during caculation lead aVL
		/// 0x10) error during caculation lead aVF</returns>
		public static int CalculateLeads(short[][] in_leads, int TotalLength, out short[][] out_leads)
		{
			out_leads = null;

			if (in_leads != null)
			{
				if (in_leads.Length == 2)
				{
					return CalculateLeads(in_leads[0], in_leads[1], TotalLength, out out_leads);
				}
				else if (in_leads.Length == 3)
				{
					return CalculateLeads(in_leads[0], in_leads[1], in_leads[2], TotalLength, out out_leads);
				}
			}

			return 1;
		}
		/// <summary>
		/// Calculate four leads from lead I and II.
		/// </summary>
		/// <returns>error:
		/// 0x00) succes
		/// 0x01) given leads are wrong
		/// 0x02) error during caculation lead III
		/// 0x04) error during caculation lead aVR
		/// 0x08) error during caculation lead aVL
		/// 0x10) error during caculation lead aVF</returns>
		public static int CalculateLeads(short[] leadI, short[] leadII, int TotalLength, out short[][] leads)
		{
			return CalculateLeads(leadI, 0, leadI.Length, leadII, 0, leadII.Length, TotalLength, out leads);
		}
		/// <summary>
		/// Calculate four leads from lead I, II and III.
		/// </summary>
		/// <returns>error:
		/// 0x00) succes
		/// 0x01) given leads are wrong
		/// 0x04) error during caculation lead aVR
		/// 0x08) error during caculation lead aVL
		/// 0x10) error during caculation lead aVF</returns>
		public static int CalculateLeads(short[] leadI, short[] leadII, short[] leadIII, int TotalLength, out short[][] leads)
		{
			return CalculateLeads(leadI, 0, leadI.Length, leadII, 0, leadII.Length, leadIII, 0, leadIII.Length, TotalLength, out leads);
		}
		/// <summary>
		/// Calculate four leads from lead I and II.
		/// </summary>
		/// <returns>error:
		/// 0x00) succes
		/// 0x01) given leads are wrong
		/// 0x02) error during caculation lead III
		/// 0x04) error during caculation lead aVR
		/// 0x08) error during caculation lead aVL
		/// 0x10) error during caculation lead aVF</returns>
		public static int CalculateLeads(short[] leadI, int beginI, int lengthI, short[] leadII, int beginII, int lengthII, int totalLength, out short[][] leads)
		{
			leads = null;
			if ((leadI != null)
			&&  (leadII != null)
			&&  (beginI >= 0)
			&&  (beginII >= 0)
			&&	(lengthI > 0)
			&&	(lengthII > 0)
			&&  (lengthI <= leadI.Length)
			&&  (lengthII <= leadII.Length)
			&&  ((beginI + lengthI) <= totalLength)
			&&  ((beginII + lengthII) <= totalLength))
			{
				leads = new short[4][];
				leads[0] = _CalculateLeadIII(leadI, beginI, lengthI, leadII, beginII, lengthII, totalLength);
				if (leads[0] == null)
				{
					return 0x2;
				}
				leads[1] = _CalculateLeadaVR(leadI, beginI, lengthI, leadII, beginII, lengthII, totalLength);
				if (leads[1] == null)
				{
					return 0x4;
				}
				leads[2] = _CalculateLeadaVL(leadI, beginI, lengthI, leadII, beginII, lengthII, totalLength, false);
				if (leads[2] == null)
				{
					return 0x8;
				}
				leads[3] = _CalculateLeadaVF(leadI, beginI, lengthI, leadII, beginII, lengthII, totalLength, false);
				if (leads[3] == null)
				{
					return 0x10;
				}
				return 0x0;
			}
			return 0x1;
		}
		/// <summary>
		/// Calculate four leads from lead I and II.
		/// </summary>
		/// <returns>error:
		/// 0x00) succes
		/// 0x01) given leads are wrong
		/// 0x02) error during caculation lead III
		/// 0x04) error during caculation lead aVR
		/// 0x08) error during caculation lead aVL
		/// 0x10) error during caculation lead aVF</returns>
		public static int CalculateLeads(short[] leadI, int beginI, int lengthI, short[] leadII, int beginII, int lengthII, short[] leadIII, int beginIII, int lengthIII, int totalLength, out short[][] leads)
		{
			leads = null;
			if ((leadI != null)
			&&  (leadII != null)
			&&  (beginI >= 0)
			&&  (beginII >= 0)
			&&	(beginIII >= 0)
			&&	(lengthI > 0)
			&&	(lengthII > 0)
			&&	(lengthIII > 0)
			&&  (lengthI <= leadI.Length)
			&&  (lengthII <= leadII.Length)
			&&	(lengthIII <= leadIII.Length)
			&&  ((beginI + lengthI) <= totalLength)
			&&  ((beginII + lengthII) <= totalLength)
			&&	((beginIII + lengthIII) <= totalLength))
			{
				leads = new short[3][];
				leads[0] = _CalculateLeadaVR(leadI, beginI, lengthI, leadII, beginII, lengthII, totalLength);
				if (leads[0] == null)
				{
					return 0x4;
				}
				leads[1] = _CalculateLeadaVL(leadI, beginI, lengthI, leadIII, beginIII, lengthIII, totalLength, true);
				if (leads[1] == null)
				{
					return 0x8;
				}
				leads[2] = _CalculateLeadaVF(leadIII, beginIII, lengthIII, leadII, beginII, lengthII, totalLength, true);
				if (leads[2] == null)
				{
					return 0x10;
				}
				return 0x0;
			}
			return 0x1;
		}
		/// <summary>
		/// Calculate lead III from lead I and II.
		/// </summary>
		public static short[] CalculateLeadIII(short[] leadI, int beginI, int lengthI, short[] leadII, int beginII, int lengthII, int totalLength)
		{
			if ((leadI != null)
			&&  (leadII != null)
			&&  (beginI >= 0)
			&&  (beginII >= 0)
			&&	(lengthI > 0)
			&&	(lengthII > 0)
			&&  (lengthI <= leadI.Length)
			&&  (lengthII <= leadII.Length)
			&&  ((beginI + lengthI) <= totalLength)
			&&  ((beginII + lengthII) <= totalLength))
			{
				return _CalculateLeadIII(leadI, beginI, lengthI, leadII, beginII, lengthII, totalLength);
			}
			return null;
		}
		/// <summary>
		/// Hidden function to calculate III (input must be checked before using this function).
		/// </summary>
		private unsafe static short[] _CalculateLeadIII(short[] leadI, int beginI, int lengthI, short[] leadII, int beginII, int lengthII, int totalLength)
		{
			short[] ret = new short[totalLength];

			fixed (short* pLeadI = leadI, pLeadII = leadII, pLeadIII = ret)
			{
				short dataI, dataII;

				for (int loper=0;loper < totalLength;loper++)
				{
					dataI = ((loper >= beginI) && (loper < (beginI + lengthI)) ? pLeadI[loper - beginI] : (short)0);
					dataII = ((loper >= beginII) && (loper < (beginII + lengthII)) ? pLeadII[loper - beginII] : (short)0);

					pLeadIII[loper] = (short) (dataII - dataI);
				}
			}

			return ret;
		}
		/// <summary>
		/// Calculate lead aVR from lead I and II.
		/// </summary>
		public static short[] CalculateLeadaVR(short[] leadI, int beginI, int lengthI, short[] leadII, int beginII, int lengthII, int totalLength)
		{
			if ((leadI != null)
			&&  (leadII != null)
			&&  (beginI >= 0)
			&&  (beginII >= 0)
			&&	(lengthI > 0)
			&&	(lengthII > 0)
			&&  (lengthI <= leadI.Length)
			&&  (lengthII <= leadII.Length)
			&&  ((beginI + lengthI) <= totalLength)
			&&  ((beginII + lengthII) <= totalLength))
			{
				return _CalculateLeadaVR(leadI, beginI, lengthI, leadII, beginII, lengthII, totalLength);
			}

			return null;
		}
		/// <summary>
		/// Hidden function to calculate aVR (input must be checked before using this function).
		/// </summary>
		private unsafe static short[] _CalculateLeadaVR(short[] leadI, int beginI, int lengthI, short[] leadII, int beginII, int lengthII, int totalLength)
		{
			short[] ret = new short[totalLength];

			fixed (short* pLeadI = leadI, pLeadII = leadII, pLeadaVR = ret)
			{
				short dataI, dataII;

				for (int loper=0;loper < totalLength;loper++)
				{
					dataI = ((loper >= beginI) && (loper < (beginI + lengthI)) ? pLeadI[loper - beginI] : (short)0);
					dataII = ((loper >= beginII) && (loper < (beginII + lengthII)) ? pLeadII[loper - beginII] : (short)0);

					pLeadaVR[loper] = (short) -((dataI + dataII) >> 1);
				}
			}

			return ret;
		}
		/// <summary>
		/// Calculate lead aVL from lead I and II/III.
		/// </summary>
		public static short[] CalculateLeadaVL(short[] leadI, int beginI, int lengthI, short[] leadX, int beginX, int lengthX, int totalLength, bool threeLeads)
		{
			if ((leadI != null)
			&&  (leadX != null)
			&&  (beginI >= 0)
			&&  (beginX >= 0)
			&&	(lengthI > 0)
			&&	(lengthX > 0)
			&&  (lengthI <= leadI.Length)
			&&  (lengthX <= leadX.Length)
			&&  ((beginI + lengthI) <= totalLength)
			&&  ((beginX + lengthX) <= totalLength))
			{
				return _CalculateLeadaVL(leadI, beginI, lengthI, leadX, beginX, lengthX, totalLength, threeLeads);
			}
			return null;
		}
		/// <summary>
		/// Hidden function to calculate aVL (input must be checked before using this function).
		/// </summary>
		private unsafe static short[] _CalculateLeadaVL(short[] leadI, int beginI, int lengthI, short[] leadX, int beginX, int lengthX, int totalLength, bool threeLead)
		{
			short[] ret = new short[totalLength];

			fixed (short* pLeadI = leadI, pLeadX = leadX, pLeadaVL = ret)
			{
				if (threeLead)
				{
					short dataI, dataIII;

					for (int loper=0;loper < totalLength;loper++)
					{
						dataI	= ((loper >= beginI) && (loper < (beginI + lengthI)) ? pLeadI[loper - beginI] : (short)0);
						dataIII = ((loper >= beginX) && (loper < (beginX + lengthX)) ? pLeadX[loper - beginX] : (short)0);

						pLeadaVL[loper] = (short) ((dataI - dataIII) >> 1);
					}
				}
				else
				{
					short dataI, dataII;

					for (int loper=0;loper < totalLength;loper++)
					{
						dataI = ((loper >= beginI) && (loper < (beginI + lengthI)) ? pLeadI[loper - beginI] : (short)0);
						dataII = ((loper >= beginX) && (loper < (beginX + lengthX)) ? pLeadX[loper - beginX] : (short)0);

						pLeadaVL[loper] = (short) (((dataI << 1) - dataII) >> 1);
					}
				}
			}

			return ret;
		}
		/// <summary>
		/// Calculate lead aVF from lead I/III and II.
		/// </summary>
		public static short[] CalculateLeadaVF(short[] leadX, int beginX, int lengthX, short[] leadII, int beginII, int lengthII, int totalLength, bool threeLead)
		{
			if ((leadX != null)
			&&  (leadII != null)
			&&  (beginX >= 0)
			&&  (beginII >= 0)
			&&  (lengthX <= leadX.Length)
			&&  (lengthII <= leadII.Length)
			&&  ((beginX + lengthX) <= totalLength)
			&&  ((beginII + lengthII) <= totalLength))
			{
				return _CalculateLeadaVF(leadX, beginX, lengthX, leadII, beginII, lengthII, totalLength, threeLead);
			}
			return null;
		}
		/// <summary>
		/// Hidden function to calculate aVF (input must be checked before using this function).
		/// </summary>
		private unsafe static short[] _CalculateLeadaVF(short[] leadX, int beginX, int lengthX, short[] leadII, int beginII, int lengthII, int totalLength, bool threeLead)
		{
			short[] ret = new short[totalLength];

			fixed (short* pLeadX = leadX, pLeadII = leadII, pLeadaVF = ret)
			{
				if (threeLead)
				{
					short dataIII, dataII;

					for (int loper=0;loper < totalLength;loper++)
					{
						dataIII = ((loper >= beginX) && (loper < (beginX + lengthX)) ? pLeadX[loper - beginX] : (short)0);
						dataII = ((loper >= beginII) && (loper < (beginII + lengthII)) ? pLeadII[loper - beginII] : (short)0);

						pLeadaVF[loper] = (short) ((dataII + dataIII) >> 1);
					}
				}
				else
				{
					short dataI, dataII;

					for (int loper=0;loper < totalLength;loper++)
					{
						dataI = ((loper >= beginX) && (loper < (beginX + lengthX)) ? pLeadX[loper - beginX] : (short)0);
						dataII = ((loper >= beginII) && (loper < (beginII + lengthII)) ? pLeadII[loper - beginII] : (short)0);

						pLeadaVF[loper] = (short) (((dataII << 1) - dataI) >> 1);
					}
				}
			}

			return ret;
		}
		/// <summary>
		/// Function to resample a signal.
		/// </summary>
		/// <param name="src">signal to resample</param>
		/// <param name="srcFreq">sample rate of signal</param>
		/// <param name="dstFreq">destination sample rate</param>
		/// <param name="dst">resampled signals</param>
		/// <returns>0 on success</returns>
		public static int ResampleSignal(short[][] src, int srcFreq, int dstFreq, out short[][] dst)
		{
			dst = null;
			if ((src != null)
			&&  (src.Length > 0)
			&&  (srcFreq > 0)
			&&  (dstFreq > 0))
			{
				if (srcFreq == dstFreq)
				{
					dst = src;
					return 0;
				}
				dst = new short[src.Length][];

				// Do resampling for each lead.
				for (int loper=0;loper < dst.Length;loper++)
				{
					if (ResampleLead(src[loper], srcFreq, dstFreq, out dst[loper]) != 0)
					{
						dst = null;
						return (0x2 << loper);
					}
				}
				return 0;
			}
			return 1;
		}
		/// <summary>
		/// Function to resample a signal.
		/// </summary>
		/// <param name="src">signal to resample</param>
		/// <param name="nrsamples">nr of samples to resample</param>
		/// <param name="srcFreq">sample rate of signal</param>
		/// <param name="dstFreq">destination sample rate</param>
		/// <param name="dst">resampled signals</param>
		/// <returns>0 on success</returns>
		public static int ResampleSignal(short[][] src, int nrsamples, int srcFreq, int dstFreq, out short[][] dst)
		{
			return ResampleSignal(src, 0, nrsamples, srcFreq, dstFreq, out dst);
		}
		/// <summary>
		/// Function to resample a signal.
		/// </summary>
		/// <param name="src">signal to resample</param>
		/// <param name="startsample">sample number to start at.</param>
		/// <param name="nrsamples">nr of samples to resample</param>
		/// <param name="srcFreq">sample rate of signal</param>
		/// <param name="dstFreq">destination sample rate</param>
		/// <param name="dst">resampled signals</param>
		/// <returns>0 on success</returns>
		public static int ResampleSignal(short[][] src, int startsample, int nrsamples, int srcFreq, int dstFreq, out short[][] dst)
		{
			dst = null;
			if ((src != null)
			&&  (src.Length > 0)
			&&  (srcFreq > 0)
			&&  (dstFreq > 0)
			&&	(startsample >= 0)
			&&	(nrsamples > 0))
			{
				if (srcFreq == dstFreq)
				{
					dst = src;
					return 0;
				}
				dst = new short[src.Length][];
				// Do resampling for each lead.
				for (int loper=0;loper < dst.Length;loper++)
				{
					if (ResampleLead(src[loper], startsample, nrsamples, srcFreq, dstFreq, out dst[loper]) != 0)
					{
						dst = null;
						return (0x2 << loper);
					}
				}
				return 0;
			}
			return 1;
		}
		/// <summary>
		/// Function to resample one lead of a signal.
		/// </summary>
		/// <param name="src">lead of signal to resample</param>
		/// <param name="srcFreq">sample rate of signal</param>
		/// <param name="dstFreq">destination sample rate</param>
		/// <param name="dst">resampled signals</param>
		/// <returns>0 on success</returns>
		public static int ResampleLead(short[] src, int srcFreq, int dstFreq, out short[] dst)
		{
			dst = null;
			if (src != null)
			{
				return ResampleLead(src, 0, src.Length, srcFreq, dstFreq, out dst);
			}
			return 1;
		}
		/// <summary>
		/// Function to resample one lead of a signal.
		/// </summary>
		/// <param name="src">lead of signal to resample</param>
		/// <param name="nrsamples">nr of samples in source</param>
		/// <param name="srcFreq">sample rate of signal</param>
		/// <param name="dstFreq">destination sample rate</param>
		/// <param name="dst">resampled signals</param>
		/// <returns>0 on success</returns>
		public static int ResampleLead(short[] src, int nrsamples, int srcFreq, int dstFreq, out short[] dst)
		{
			dst = null;
			if (src != null)
			{
				return ResampleLead(src, 0, src.Length, srcFreq, dstFreq, out dst);
			}
			return 1;
		}
		/// <summary>
		/// Function to resample one lead of a signal.
		/// </summary>
		/// <param name="src">lead of signal to resample</param>
		/// <param name="startsample">samplenr to start resampleing</param>
		/// <param name="nrsamples">nr of samples in source</param>
		/// <param name="srcFreq">sample rate of signal</param>
		/// <param name="dstFreq">destination sample rate</param>
		/// <param name="dst">resampled signals</param>
		/// <returns>0 on success</returns>
		public static int ResampleLead(short[] src, int startsample, int nrsamples, int srcFreq, int dstFreq, out short[] dst)
		{
			int n = ((ResampleInterval * srcFreq) / 1000); // n= number of samples for a 20 ms (resample)interval
			dst = null;

			// Make n a even number and larger then or equal to 2
			n>>=1;
			if (n<=0) 
			{n=1;}
			n<<=1;

			MakeFastTable(n, srcFreq, dstFreq);

			if ((src != null)
			&&  (n > 1)
			&&  (srcFreq > 0)
			&&  (dstFreq > 0)
			&&	(startsample >= 0)
			&&	(nrsamples > 0)
			&&  ((startsample + nrsamples) <= src.Length))
			{
				int err = 0;

				unsafe
				{
					if (srcFreq == dstFreq)
					{
						dst = new short[nrsamples];

						fixed (short* pSrc = src, pDst = dst)
						{
							short* pd = pDst;
							short* pdend = pd + nrsamples;
							short* ps = pSrc + startsample;

							while (pd < pdend)
								*(pd++) = *(ps++);
						}

						return 0;
					}
	
					dst = new short[(nrsamples * dstFreq) / srcFreq + 1];
					int tussenFreq = KGV(srcFreq, dstFreq);
					int srcAdd = tussenFreq / srcFreq;
					int dstAdd = tussenFreq / dstFreq;

					int start = (startsample * srcAdd);
					int end = (startsample + nrsamples) * srcAdd;

					// Allocate two arrays for calculations
					double[] c = new double[n+1];
					double[] d = new double[n+1];

					fixed (short* pSrc = src, pDst = dst)
					{
						for (int tussenLoper=start;tussenLoper < end;tussenLoper+=dstAdd)
						{
							// If sample matches precisly a sample of source do no calculations.
							if ((tussenLoper % srcAdd) == 0)
							{
								pDst[(tussenLoper - start) / dstAdd] = pSrc[tussenLoper / srcAdd];
							}
							else
							{
								// Determine first sample for polynoom.
								int first = tussenLoper / srcAdd - (n >> 1);

								// determine used N (for n at begin and end of data).
								int usedN = n;
								// if first is smaller then 0 make N smaller.
								if (first < -1)
								{
									usedN -= ((-1 - first) << 1);
									first = -1;
								}
								// if last is greater or equal then nrsamples make N smaller.
								if (first + usedN >= nrsamples)
								{
									usedN -= (((first + usedN) - nrsamples) << 1);
									first = nrsamples - usedN - 1;
								}

								if (((dstFreq / srcFreq) == 2)
								&&	((dstFreq % srcFreq) == 0))
								{
									int p = ((usedN >> 1)-1);
									double result = 0;

									for (int loper=0;loper < usedN;loper++)
									{
										result += (pSrc[first+1+loper] * Fast[p][loper]);
									}

									pDst[(tussenLoper - start) / dstAdd] = (short) result;
								}
								else
								{
									double den = 0;
									int ns = 1;
									int dif = Math.Abs(tussenLoper - ((first + 1) * srcAdd));
									// Fill arrays with source samples.
									for (int loper=1;loper <= usedN;loper++)
									{
										int dift;
										if ((dift = Math.Abs(tussenLoper - ((first + loper) * srcAdd))) < dif)
										{
											ns = loper;
											dif = dift;
										}
										c[loper] = pSrc[first + loper];
										d[loper] = pSrc[first + loper];
									}

									// The initial approximation
									double y = pSrc[first + ns--];

									for (int loper1=1;loper1 < usedN;loper1++)
									{
										for (int loper2=1;loper2 <= (usedN - loper1);loper2++)
										{
											int ho = ((first + loper2) * srcAdd) - tussenLoper;
											int hp = ((first + loper2 + loper1) * srcAdd) - tussenLoper;
											double w = c[loper2 + 1] - d[loper2];
											if ((den = ho - hp) == 0)
											{
												// Error when no difference (dividing by zero is impossible)
												err |= 0x2;
											}
											den = w / den;
											d[loper2] = hp * den;
											c[loper2] = ho * den;
										}
										// Change approxiamation.
										y += ((ns << 1) < (usedN - loper1) ? c[ns + 1] : d[ns--]);
									}

									// set value destination with approxiamation.
									pDst[(tussenLoper - start) / dstAdd] = (short) y; 
								}
							}
						}
					}
				}

				return err;
			}
			return 1;
		}
		/// <summary>
		/// Make the fasttable.
		/// </summary>
		/// <param name="n">nr of samples resambles 20ms</param>
		/// <param name="srcFreq">sample rate of signal</param>
		/// <param name="dstFreq">destination sample rate</param>
		private static void MakeFastTable(int n, int srcFreq, int dstFreq)
		{
			// Make a table when fast calculation is possible
			if (((dstFreq / srcFreq) == 2)
			&&	((dstFreq % srcFreq) == 0))
			{
				// Only make a new table if needed.
				if ((Fast == null)
				||	(Fast.Length < (n >> 1)))
				{
					double[][] temp = Fast;
					Fast = new double[n >> 1][];

					for (int x=0;x < Fast.Length;x++)
					{
						// If fast table previously available, don't calculate again.
						if ((temp != null)
						&&	(temp.Length > x))
						{
							Fast[x] = temp[x];
						}
						else
						{
							Fast[x] = new double[(x+1) << 1];
							for (int y=0;y < Fast[x].Length;y++)
							{
								Fast[x][y] = 1;
								for (int z=0, c=0;z < Fast[x].Length-1;z++, c+=2)
								{
									if ((y << 1) == c)
									{c += 2;}
									Fast[x][y] *= (double)(((x<<1)+1) - c) / (double)((y << 1) - c);
								}
							}
						}
					}
				}
			}
		}
		/// <summary>
		/// Function to set an other multiplier (if this function is improperly used data will be lost).
		/// </summary>
		/// <param name="src">signal to change multiplier</param>
		/// <param name="srcmulti">orignal multiplier</param>
		/// <param name="dstmulti">preferred multiplier</param>
		/// <returns>0 on success</returns>
		public static int ChangeMultiplier(short[][] src, double srcmulti, double dstmulti)
		{
			if ((src != null)
			&&	(srcmulti == dstmulti))
			{
				return 0;
			}
			else if ((src != null)
				&&   (srcmulti > 0)
				&&   (dstmulti > 0))
			{
				for (int loper=0;loper < src.Length;loper++)
				{
					if (ChangeMultiplier(src[loper], srcmulti, dstmulti) != 0)
					{
						return (0x2 << loper);
					}
				}
				return 0;
			}
			return 1;
		}
		/// <summary>
		/// Function to set an other multiplier (if this function is improperly used data will be lost).
		/// </summary>
		/// <param name="src">Lead to change multiplier</param>
		/// <param name="srcmulti">orignal multiplier</param>
		/// <param name="dstmulti">preferred multiplier</param>
		/// <returns>0 on success</returns>
		public static int ChangeMultiplier(short[] src, double srcmulti, double dstmulti)
		{
			if ((src != null)
			&&	(srcmulti == dstmulti))
			{
				return 0;
			}
			else if ((src != null)
				&&   (srcmulti > 0)
				&&   (dstmulti > 0))
			{
				unsafe
				{
					fixed (short* pSrc = src)
					{
						short* ps = pSrc;
						short* psend = ps + src.Length;

						while (ps < psend)
						{
							*ps = (short) ((*ps * srcmulti) / dstmulti);
							ps++;
						}
					}
				}
				return 0;
			}
			return 1;
		}
		/// <summary>
		/// Function to copy a signal.
		/// </summary>
		/// <param name="src">source array of signal</param>
		/// <param name="src_offset">offset in source array to start copy at</param>
		/// <param name="dst">destination array for signal</param>
		/// <param name="dst_offset">offset in destination array to start copy at</param>
		/// <param name="len">length of the copy</param>
		/// <returns>0 if succesfull or 1 if invalid arguments</returns>
		public static int CopySignal(short[] src, int src_offset, short[] dst, int dst_offset, int len)
		{
			if ((src != null)
			&&	(dst != null)
			&&	(src_offset >= 0)
			&&	(dst_offset >= 0)
			&&	(len > 0)
			&&	((dst_offset + len) <= dst.Length))
			{
				if ((src_offset + len) > src.Length)
					len = src.Length - src_offset;

				unsafe
				{
					fixed (short* pSrc = src, pDst = dst)
					{
						short* ps = pSrc + src_offset;
						short* pd = pDst + dst_offset;
						short* pdend = pd + len;

						while (pd < pdend)
							*(pd++) = *(ps++);
					}
				}

				return 0;
			}

			return 1;
		}
		/// <summary>
		/// Function to normalize a signal.
		/// </summary>
		/// <param name="src">the source signal</param>
		/// <param name="sps">nr of samples per second of the source signal</param>
		/// <returns>0 if successfull</returns>
		public static int NormalizeSignal(short[] src, int sps)
		{
			return ((src != null) && (sps > 0)) ? ShiftSignal(src, MedianValue(src, sps)) : 1;
		}
		/// <summary>
		/// Function to shift signal.
		/// </summary>
		/// <param name="src">the source signal</param>
		/// <param name="median">value to shift signal</param>
		/// <returns>0 if successfull</returns>
		public static int ShiftSignal(short[] src, short shift)
		{
			if (src != null)
			{
				if (shift != 0)
				{
					for (int i=0;i < src.Length;i++)
						src[i] -= shift;
				}

				return 0;
			}

			return 1;
		}
		/// <summary>
		/// Function to resample one lead of a signal to holter overview signal.
		/// </summary>
		/// <param name="src">lead of signal to resample</param>
		/// <param name="srcFreq">sample rate of signal</param>
		/// <param name="dstFreq">destination sample rate</param>
		/// <param name="dst">resampled signals</param>
		/// <returns>0 on success</returns>
		public static int MakeHolterSignal(short[] src, int srcFreq, int dstFreq, out short[] dst)
		{
			return MakeHolterSignal(src, 0, src.Length, srcFreq, dstFreq, out dst);
		}
		/// <summary>
		/// Function to resample one lead of a signal to holter overview signal.
		/// </summary>
		/// <param name="src">lead of signal to resample</param>
		/// <param name="startsample">samplenr to start resampleing</param>
		/// <param name="srcFreq">sample rate of signal</param>
		/// <param name="dstFreq">destination sample rate</param>
		/// <param name="dst">resampled signals</param>
		/// <returns>0 on success</returns>
		public static int MakeHolterSignal(short[] src, int startsample, int srcFreq, int dstFreq, out short[] dst)
		{
			return MakeHolterSignal(src, startsample, src.Length - startsample, srcFreq, dstFreq, out dst);
		}
		/// <summary>
		/// Function to resample one lead of a signal to holter overview signal.
		/// </summary>
		/// <param name="src">lead of signal to resample</param>
		/// <param name="startsample">samplenr to start resampleing</param>
		/// <param name="nrsamples">nr of samples in source</param>
		/// <param name="srcFreq">sample rate of signal</param>
		/// <param name="dstFreq">destination sample rate</param>
		/// <param name="dst">resampled signals</param>
		/// <returns>0 on success</returns>
		public static int MakeHolterSignal(short[] src, int startsample, int nrsamples, int srcFreq, int dstFreq, out short[] dst)
		{
			dst = null;
			
			if ((src != null)
			    &&  (srcFreq > MAX_HOLTER_SPS)
			    &&  (dstFreq >= MIN_HOLTER_SPS)
			    &&	(dstFreq <= MAX_HOLTER_SPS)
			    &&	(startsample >= 0)
			    &&	(nrsamples > 0)
			    &&  ((startsample + nrsamples) <= src.Length))
			{
				int dstSize = (nrsamples * dstFreq) / srcFreq;
				
				dst = new short[dstSize];
				
				short
					min = short.MaxValue,
					max = short.MinValue,
					prev = 0;
				
				int prev_j = 0;
				
				for (int i=0;(i < nrsamples)&&(prev_j < dstSize);i++)
				{
					int j = (i * dstFreq) / srcFreq;
					
					short val = src[startsample + i];
					
					if (j == prev_j)
					{
						if (val < min)
							min = val;
						
						if (val > max)
							max = val;
					}
					else
					{
						prev = dst[prev_j] = (Math.Abs(prev - min) > Math.Abs(prev - max))? min : max;
						
						prev_j = j;
						
						min = max = val;
					}
				}
				
				if (prev_j < dstSize)
				{
					prev = dst[prev_j] = (Math.Abs(min - prev) > Math.Abs(max - prev))? min : max;
				}
				
				return 0;
			}
			
			return 1;
		}
		/// <summary>
		/// Get the median value of a signal.
		/// </summary>
		/// <param name="src">the source signal</param>
		/// <param name="sps">nr of samples per second of the source signal</param>
		/// <returns>the median value</returns>
		private static short MedianValue(short[] src, int sps)
		{
			if (src != null)
			{
				if (sps > 8)
					sps >>= 3;

				short[] temp1 = new short[(src.Length / sps) + 1];

				int j = 0;
				for (int i=0;i < src.Length;i+=sps,j++)
				{
					short temp2 = src[i];

					for (int k=0;k < j;k++)
					{
						if (temp2 < temp1[k])
						{
							short temp3 = temp2;
							temp2 = temp1[k];
							temp1[k] = temp3;
						}
					}

					temp1[j] = temp2;
				}

				return temp1[temp1.Length >> 1];
			}

			return 0;
		}
		/// <summary>
		/// Function to determine the "grootste gemene deler"
		/// </summary>
		/// <param name="x1">value 1</param>
		/// <param name="x2">value 2</param>
		/// <returns>"grootste gemene deler"</returns>
		private static int GGD(int x1, int x2)
		{
			if ((x1 == 0)
			||  (x2 == 0))
			{
				return 0;
			}

			if (x1 >= x2)
			{
				if ((x1 % x2) == 0)
				{
					return x2;
				}
				return GGD(x2, x1 % x2);
			}
			return GGD(x2, x1);
		}
		/// <summary>
		/// Function to determine the "kleinst gemene veelvoud"
		/// </summary>
		/// <param name="x1">value 1</param>
		/// <param name="x2">value 2</param>
		/// <returns>"kleinst gemene veelvoud"</returns>
		private static int KGV(int x1, int x2)
		{
			int ggd = GGD(x1, x2);
			return (ggd == 0 ? 0 : (x1 * x2) / ggd);
		}

		/// <summary>
		/// Function to anonymous an instance of IDemographics.
		/// </summary>
		/// <param name="demo">instance to anonymous</param>
		/// <param name="type">char to use</param>
		public static void Anonymous(ECGDemographics.IDemographic demo, char type)
		{
			string temp = demo.LastName;

			if (temp != null)
				demo.LastName = new string(type, temp.Length);

			temp = demo.FirstName;
			if (temp != null)
				demo.FirstName = new string(type, temp.Length);

			temp = demo.PatientID;
			if (temp != null)
				demo.PatientID = new string(type, temp.Length);

			temp = demo.SecondLastName;
			if (temp != null)
				demo.SecondLastName = new string(type, temp.Length);

			temp = demo.PrefixName;
			if (temp != null)
				demo.PrefixName = new string(type, temp.Length);

			temp = demo.SuffixName;
			if (temp != null)
				demo.SuffixName = new string(type, temp.Length);

			ECGDemographics.Date date = demo.PatientBirthDate;

			if (date != null)
			{
				date.Day = 1;
				date.Month = 1;

				demo.PatientBirthDate = date;
			}

			temp = demo.SequenceNr;

			if (temp != null)
				demo.SequenceNr = "1";

		}

		private ECGTool(){}
	}
}
