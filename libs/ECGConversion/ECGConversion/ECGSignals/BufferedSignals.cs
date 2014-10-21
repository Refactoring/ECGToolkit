/***************************************************************************
Copyright 2012-1024, van Ettinger Information Technology, Lopik, The Netherlands

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

namespace ECGConversion.ECGSignals
{
	/// <summary>
	/// Buffered signals. Provides an extension of the signals class this will allow longer files.
	/// </summary>
	public class BufferedSignals : Signals
	{
		public const int NR_SECS_LOADED_ON_INIT = 10;
		public static bool LoadSignalOnInit = true;

		private IBufferedSource _Source;

		public override bool IsBuffered
		{
			get
			{
				return _Source != null;
			}
		}

		// this is the size of the underlying signals
		public int RealRhythmSamplesPerSecond;
		public int RealRhythmStart;
		public int RealRhythmEnd;

		// info about the templates
		public int TemplateNrMedian;
		public int TemplateMedianSamplesPerSecond;
		public int TemplateOccurance;

		public override BufferedSignals AsBufferedSignals
		{
			get
			{
				return _Source != null ? this : null;
			}
		}

		public override Signals Clone()
		{
			BufferedSignals sigs = new BufferedSignals(this._Source, this.NrLeads);

			// copy of BufferedSignals items
			sigs.RealRhythmSamplesPerSecond = this.RealRhythmSamplesPerSecond;
			sigs.RealRhythmStart = this.RealRhythmStart;
			sigs.RealRhythmEnd = this.RealRhythmEnd;
			sigs.TemplateMedianSamplesPerSecond = this.TemplateMedianSamplesPerSecond;
			sigs.TemplateNrMedian = this.TemplateNrMedian;
			sigs.TemplateOccurance = this.TemplateOccurance;

			// copy of Signals items
			sigs.RhythmAVM = this.RhythmAVM;
			sigs.RhythmSamplesPerSecond = this.RhythmSamplesPerSecond;

			sigs.MedianAVM = this.MedianAVM;
			sigs.MedianLength = this.MedianLength;
			sigs.MedianSamplesPerSecond = this.MedianSamplesPerSecond;
			sigs.MedianFiducialPoint = this.MedianFiducialPoint;

			if (this.QRSZone != null)
			{
				sigs.QRSZone = new QRSZone[this.QRSZone.Length];

				for (int i=0;i < sigs.QRSZone.Length;i++)
					sigs.QRSZone[i] = this.QRSZone[i].Clone();
			}

			for (byte i=0;i < sigs.NrLeads;i++)
			{
				sigs[i] = this[i].Clone();
			}

			return sigs;
		}

		public BufferedSignals(IBufferedSource src)
#if !NET_1_1
		:	base()
		{
			_Source = src;
			
			RealRhythmSamplesPerSecond = 0;
			RealRhythmStart = 0;
			RealRhythmEnd = 0;
			TemplateMedianSamplesPerSecond = 0;
			TemplateNrMedian = 0;
			TemplateOccurance = 0;
		}
#else
		{
			base();

			_Source = src;
			
			RealRhythmSamplesPerSecond = 0;
			RealRhythmStart = 0;
			RealRhythmEnd = 0;
			TemplateMedianSamplesPerSecond = 0;
			TemplateNrMedian = 0;
			TemplateOccurance = 0;
		}
#endif
			

		public BufferedSignals(IBufferedSource src, byte nrleads)
#if !NET_1_1
		:	base(nrleads)
		{
			_Source = src;
			
			RealRhythmSamplesPerSecond = 0;
			RealRhythmStart = 0;
			RealRhythmEnd = 0;
			TemplateMedianSamplesPerSecond = 0;
			TemplateNrMedian = 0;
			TemplateOccurance = 0;
		}
#else
		{
			base(nrleads);

			_Source = src;
			
			RealRhythmSamplesPerSecond = 0;
			RealRhythmStart = 0;
			RealRhythmEnd = 0;
			TemplateMedianSamplesPerSecond = 0;
			TemplateNrMedian = 0;
			TemplateOccurance = 0;
		}
#endif

		public void Init()
		{
			if (LoadSignalOnInit)
			{
				LoadSignal(RealRhythmStart, NR_SECS_LOADED_ON_INIT * RealRhythmSamplesPerSecond);
			}
		}

		/// <summary>
		/// Loads a part of the rhythm signal.
		/// </summary>
		/// <param name='rhythmStart'>position to start loading.</param>
		/// <param name='rhythmEnd'>load until this end</param>
		/// <returns>
		/// True if loading of rhythm signal is succesfull
		/// </returns>
		public bool LoadSignal(int rhythmStart, int rhythmEnd)
		{
			if ((_Source != null)
			&&	(RealRhythmSamplesPerSecond > 0)
			&&	(RealRhythmStart >= 0)
			&&	(RealRhythmEnd >= 0)
			&&	(RealRhythmStart < RealRhythmEnd)
			&&	(rhythmStart >= 0)
			&&	(rhythmEnd >= 0)
			&&	(rhythmStart < rhythmEnd))
			{
				Signal
					sigI = null,
					sigII = null,
					sigIII = null;

				// iterate through all leads
				for (byte i=0;i < NrLeads;i++)
				{
					// load rhythm for lead
					bool load = _Source.LoadRhythmSignal(i, this[i], this.RhythmAVM, rhythmStart - RealRhythmStart, rhythmEnd - RealRhythmStart);

					if (load)
					{
						this[i].RhythmStart += RealRhythmStart;
						this[i].RhythmEnd += RealRhythmStart;

						if (RealRhythmSamplesPerSecond != RhythmSamplesPerSecond)
						{
							short[] temp = null;
								
							ECGTool.ResampleLead(this[i].Rhythm, RealRhythmSamplesPerSecond, RhythmSamplesPerSecond, out temp);
							
							long overflow_prevent = (long)this[i].RhythmStart;
							overflow_prevent *= (long)RhythmSamplesPerSecond;
							overflow_prevent /= (long)RealRhythmSamplesPerSecond;

							this[i].RhythmStart = (int) overflow_prevent;
							this[i].RhythmEnd = this[i].RhythmStart + temp.Length;
							this[i].Rhythm = temp;
						}

						// keep track of leads I, II and III for possible caclulating of leads
						if (this[i].Type == LeadType.I)
							sigI = this[i];
						else if (this[i].Type == LeadType.II)
							sigII = this[i];
						else if (this[i].Type == LeadType.III)
							sigIII = this[i];
					}
					else
					{
						// check that at least I and II are available to calculate leads
						if ((sigI != null)
						&&	(sigII != null))
						{
							// calculate the lead if it can't load
							if (this[i].Type == LeadType.III)
							{
								this[i].Rhythm = ECGTool.CalculateLeadIII(sigI.Rhythm, 0, sigI.Rhythm.Length, sigII.Rhythm, 0, sigII.Rhythm.Length, rhythmEnd - rhythmStart);
								this[i].RhythmStart = sigI.RhythmStart;
								this[i].RhythmEnd = sigI.RhythmEnd;
							}
							else if (this[i].Type == LeadType.aVR)
							{
								this[i].Rhythm = ECGTool.CalculateLeadaVR(sigI.Rhythm, 0, sigI.Rhythm.Length, sigII.Rhythm, 0, sigII.Rhythm.Length, rhythmEnd - rhythmStart);
								this[i].RhythmStart = sigI.RhythmStart;
								this[i].RhythmEnd = sigI.RhythmEnd;
							}
							else if (this[i].Type == LeadType.aVL)
							{
								if (sigIII != null)
									this[i].Rhythm = ECGTool.CalculateLeadaVL(sigI.Rhythm, 0, sigI.Rhythm.Length, sigIII.Rhythm, 0, sigIII.Rhythm.Length, rhythmEnd - rhythmStart, true);
								else
									this[i].Rhythm = ECGTool.CalculateLeadaVL(sigI.Rhythm, 0, sigI.Rhythm.Length, sigII.Rhythm, 0, sigII.Rhythm.Length, rhythmEnd - rhythmStart, false);

								this[i].RhythmStart = sigI.RhythmStart;
								this[i].RhythmEnd = sigI.RhythmEnd;
							}
							else if (this[i].Type == LeadType.aVF)
							{
								if (sigIII != null)
									this[i].Rhythm = ECGTool.CalculateLeadaVF(sigIII.Rhythm, 0, sigIII.Rhythm.Length, sigII.Rhythm, 0, sigII.Rhythm.Length, rhythmEnd - rhythmStart, true);
								else
									this[i].Rhythm = ECGTool.CalculateLeadaVF(sigI.Rhythm, 0, sigI.Rhythm.Length, sigII.Rhythm, 0, sigII.Rhythm.Length, rhythmEnd - rhythmStart, false);

								this[i].RhythmStart = sigI.RhythmStart;
								this[i].RhythmEnd = sigI.RhythmEnd;
							}
							else
							{
								// fail on load failed: might be undesirable
								return false;
							}
						}
						else
						{
							// fail on load failed: might be undesirable
							return false;
						}
					}
				}

				return true;
			}
		
			return false;
		}

		/// <summary>
		/// Loads the template.
		/// </summary>
		/// <param name='templateNr'>Template nr to load</param>
		/// <returns>
		/// True if loading of rhythm signal is succesfull
		/// </returns>
		public bool LoadTemplate(int templateNr)
		{
			if ((_Source != null)
			&&	(TemplateMedianSamplesPerSecond > 0)
			&&	(TemplateNrMedian > 0)
			&&	(templateNr > 0)
			&&	(templateNr < TemplateNrMedian))
			{
				Signal
					sigI = null,
					sigII = null,
					sigIII = null;
				
				// iterate through all leads
				for (byte i=0;i < NrLeads;i++)
				{
					// load rhythm for lead
					bool load = _Source.LoadTemplateSignal(i, this[i], this.MedianAVM, templateNr);
					
					if (load)
					{	
						if (TemplateMedianSamplesPerSecond != MedianSamplesPerSecond)
						{
							short[] temp = null;
							
							ECGTool.ResampleLead(this[i].Median, TemplateMedianSamplesPerSecond, MedianSamplesPerSecond, out temp);
							
							this[i].Median = temp;
						}
						
						// keep track of leads I, II and III for possible caclulating of leads
						if (this[i].Type == LeadType.I)
							sigI = this[i];
						else if (this[i].Type == LeadType.II)
							sigII = this[i];
						else if (this[i].Type == LeadType.III)
							sigIII = this[i];
					}
					else
					{
						// check that at least I and II are available to calculate leads
						if ((sigI != null)
						&&	(sigII != null))
						{
							// calculate the lead if it can't load
							if (this[i].Type == LeadType.III)
							{
								this[i].Median = ECGTool.CalculateLeadIII(sigI.Median, 0, sigI.Median.Length, sigII.Median, 0, sigII.Median.Length, sigI.Median.Length);
							}
							else if (this[i].Type == LeadType.aVR)
							{
								this[i].Median = ECGTool.CalculateLeadaVR(sigI.Median, 0, sigI.Median.Length, sigII.Median, 0, sigII.Median.Length, sigI.Median.Length);
							}
							else if (this[i].Type == LeadType.aVL)
							{
								if (sigIII != null)
									this[i].Median = ECGTool.CalculateLeadaVL(sigI.Median, 0, sigI.Median.Length, sigIII.Median, 0, sigIII.Median.Length, sigI.Median.Length, true);
								else
									this[i].Median = ECGTool.CalculateLeadaVL(sigI.Median, 0, sigI.Median.Length, sigII.Median, 0, sigII.Median.Length, sigI.Median.Length, false);
							}
							else if (this[i].Type == LeadType.aVF)
							{
								if (sigIII != null)
									this[i].Median = ECGTool.CalculateLeadaVF(sigIII.Median, 0, sigIII.Median.Length, sigII.Median, 0, sigII.Median.Length, sigII.Median.Length, true);
								else
									this[i].Median = ECGTool.CalculateLeadaVF(sigI.Median, 0, sigI.Median.Length, sigII.Median, 0, sigII.Median.Length, sigI.Median.Length, false);
							}
							else
							{
								TemplateOccurance = 0;
								
								return false;
							}
						}
						else
						{
							TemplateOccurance = 0;
							
							return false;
						}
					}
				}

				// load the template occurance
				_Source.LoadTemplateOccurance(templateNr, out TemplateOccurance, out QRSZone);
				
				return true;
			}
			
			TemplateOccurance = 0;
			
			return false;
		}
	}
}

