/***************************************************************************
Copyright 2019-2020, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands
Copyright 2012-1014, van Ettinger Information Technology, Lopik, The Netherlands
Copyright 2004,2008, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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

namespace ECGConversion.ECGSignals
{
	/// <summary>
	/// Class containing signals of ECG.
	/// </summary>
	public class Signals
	{
		// Overrall Data.
		public byte NrLeads
		{
			get
			{
				return (byte) (_Lead != null ? _Lead.Length : 0);
			}
			set
			{
				if ((value < byte.MinValue)
				||	(value > byte.MaxValue))
					return;

				_Lead = new Signal[value];
			}
		}

		public virtual bool IsBuffered
		{
			get {return false;}
		}

		public virtual BufferedSignals AsBufferedSignals
		{
			get {return null;}
		}

		// Rhythm Info.
		public double RhythmAVM = 0; // AVM in mV
		public int RhythmSamplesPerSecond = 0;

		// Median Info.
		public double MedianAVM = 0; // AVM in mV
		public ushort MedianLength = 0; // Length in ms
		public int MedianSamplesPerSecond = 0;

		// QRS zones
		public ushort MedianFiducialPoint = 0;
		public QRSZone[] QRSZone = null;

		// Signal Data
		private Signal[] _Lead = null;

		public Signal[] GetLeads()
		{
			return _Lead;
		}

		public void SetLeads(Signal[] leads)
		{
			if (leads.Length > byte.MaxValue)
				return;

			_Lead = leads;
		}

		public Signal this[int i]
		{
			get
			{
				return ((_Lead != null) && (i < _Lead.Length)) ? _Lead[i] : null;
			}
			set
			{
				_Lead[i] = value;
			}
		}

		public Signals()
		{}

		public Signals(byte nrleads)
		{
			NrLeads = nrleads;
		}
		/// <summary>
		/// Function to determine if the first eigth leads are as expected (I, II, V1 - V6).
		/// </summary>
		/// <returns>true if as expected</returns>
		public bool IsNormal()
		{
			return Signal.IsNormal(_Lead);
		}
		/// <summary>
		/// Calculate start and end of signals.
		/// </summary>
		/// <param name="nStart">returns start</param>
		/// <param name="nEnd">returns end</param>
		public void CalculateStartAndEnd(out int nStart, out int nEnd)
		{
			nStart = int.MaxValue;
			nEnd = int.MinValue;

			if (_Lead != null)
			{
				for (int nLead=0;nLead < _Lead.Length;nLead++)
				{
					if (_Lead[nLead].RhythmStart < nStart)
						nStart = _Lead[nLead].RhythmStart;

					if (_Lead[nLead].RhythmEnd > nEnd)
						nEnd = _Lead[nLead].RhythmEnd;
				}
			}
		}

		/// <summary>
		/// Function to determine the number of simultaneosly.
		/// </summary>
		/// <param name="data">signal information.</param>
		/// <returns>true if as expected</returns>
		public int NrSimultaneosly()
		{
			return Signal.NrSimultaneosly(_Lead);
		}
		/// <summary>
		/// Function to sort signal array on lead type.
		/// </summary>
		/// <param name="data">signal array</param>
		public void SortOnType()
		{
			Signal.SortOnType(_Lead);
		}
		/// <summary>
		/// Function to sort signal array on lead type.
		/// </summary>
		/// <param name="first">first value to sort</param>
		/// <param name="last">last value to sort</param>
		public void SortOnType(int first, int last)
		{
			Signal.SortOnType(_Lead, first, last);
		}
		/// <summary>
		/// Function to trim a signals.
		/// </summary>
		/// <param name="val">value to trim on</param>
		public void TrimSignals(short val)
		{
			int start, end;

			CalculateStartAndEnd(out start, out end);

			TrimSignals(val, start, end);
		}
		/// <summary>
		/// Function to trim a signals.
		/// </summary>
		/// <param name="val">value to trim on</param>
		/// <param name="start">start of all signals</param>
		/// <param name="end">end of all signals</param>
		public void TrimSignals(short val, int start, int end)
		{
			foreach (Signal sig in _Lead)
			{
				int	trimBegin = 0,
					trimEnd = sig.Rhythm.Length-1;

				if (sig.RhythmStart == start)
				{
					for (int i=0;i < sig.Rhythm.Length;i++)
					{
						if (sig.Rhythm[i] != val)
						{
							trimBegin = i;
							break;
						}
					}
				}

				if (sig.RhythmEnd == end)
				{
					for (int i=sig.Rhythm.Length-1;i > 0;i--)
					{
						if (sig.Rhythm[i] != val)
						{
							trimEnd = i;
							break;
						}
					}
				}

				if ((trimBegin / RhythmSamplesPerSecond) < 1)
					trimBegin = 0;

				if (((sig.Rhythm.Length-1 - trimEnd) / RhythmSamplesPerSecond) < 1)
					trimEnd = sig.Rhythm.Length-1;

				if ((trimBegin != 0)
				||	(trimEnd != sig.Rhythm.Length-1))
				{
					sig.RhythmStart += trimBegin;
					sig.RhythmEnd -= (sig.Rhythm.Length-1) - trimEnd;

					short[] temp = new short[trimEnd - trimBegin + 1];

					for (int i=0;i < temp.Length;i++)
						temp[i] = sig.Rhythm[i + trimBegin];

					sig.Rhythm = temp;
				}
			}
		}
		/// <summary>
		/// Function to clone a signals object.
		/// </summary>
		/// <returns>cloned signals object</returns>
		public virtual Signals Clone()
		{
			Signals sigs = new Signals();

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

			if (this._Lead != null)
			{
				sigs.NrLeads = this.NrLeads;

				for (int i=0;i < sigs._Lead.Length;i++)
					sigs._Lead[i] = this._Lead[i].Clone();
			}

			return sigs;
		}
        /// <summary>
        /// Function to get a copy of a signals object.
        /// </summary>
        /// <returns>copy to basic signals object</returns>
        public Signals GetCopy()
        {
            Signals sigs = new Signals();

            sigs.RhythmAVM = this.RhythmAVM;
            sigs.RhythmSamplesPerSecond = this.RhythmSamplesPerSecond;

            sigs.MedianAVM = this.MedianAVM;
            sigs.MedianLength = this.MedianLength;
            sigs.MedianSamplesPerSecond = this.MedianSamplesPerSecond;
            sigs.MedianFiducialPoint = this.MedianFiducialPoint;

            if (this.QRSZone != null)
            {
                sigs.QRSZone = new QRSZone[this.QRSZone.Length];

                for (int i = 0; i < sigs.QRSZone.Length; i++)
                    sigs.QRSZone[i] = this.QRSZone[i].Clone();
            }

            if (this._Lead != null)
            {
                sigs.NrLeads = this.NrLeads;

                for (int i = 0; i < sigs._Lead.Length; i++)
                    sigs._Lead[i] = this._Lead[i].Clone();
            }

            return sigs;
        }
        /// <summary>
        /// Apply bandpass filter to Signal object
        /// </summary>
        /// <param name="bottom">bottom frequency of bandpass filter</param>
        /// <param name="top">top frequency of bandpass filter</param>
        /// <returns>a filtered copy of the signal object</returns>
        public Signals ApplyBandpassFilter(double bottom, double top)
        {
            DSP.IFilter[] tmp = null;

            return ApplyBandpassFilter(bottom, top, 2, ref tmp);
        }
        /// <summary>
        /// Apply bandpass filter to Signal object
        /// </summary>
        /// <param name="bottom">bottom frequency of bandpass filter</param>
        /// <param name="top">top frequency of bandpass filter</param>
        /// <param name="numSections">nr of sections to use in filter (default: 2)</param>
        /// <returns>a filtered copy of the signal object</returns>
        public Signals ApplyBandpassFilter(double bottom, double top, int numSections)
        {
            DSP.IFilter[] tmp = null;

            return ApplyBandpassFilter(bottom, top, numSections, ref tmp);
        }
        /// <summary>
        /// Apply bandpass filter to Signal object
        /// </summary>
        /// <param name="bottom">bottom frequency of bandpass filter</param>
        /// <param name="top">top frequency of bandpass filter</param>
        /// <param name="numSections">nr of sections to use in filter (default: 2)</param>
        /// <param name="filters">provide filters that where used</param>
        /// <returns>a filtered copy of the signal object</returns>
        public Signals ApplyBandpassFilter(double bottom, double top, int numSections, ref DSP.IFilter[] filters)
        {
            Signals sigs = new Signals();

            sigs.RhythmAVM = this.RhythmAVM;
            sigs.RhythmSamplesPerSecond = this.RhythmSamplesPerSecond;

            sigs.MedianAVM = this.MedianAVM;
            sigs.MedianLength = this.MedianLength;
            sigs.MedianSamplesPerSecond = this.MedianSamplesPerSecond;
            sigs.MedianFiducialPoint = this.MedianFiducialPoint;

            if (this.QRSZone != null)
            {
                sigs.QRSZone = new QRSZone[this.QRSZone.Length];

                for (int i = 0; i < sigs.QRSZone.Length; i++)
                    sigs.QRSZone[i] = this.QRSZone[i].Clone();
            }

            if (this._Lead != null)
            {
                sigs.NrLeads = this.NrLeads;

                if (filters == null
                ||  filters.Length != this.NrLeads)
                {
                    filters = new DSP.IFilter[this.NrLeads];
                }

                for (int i = 0; i < sigs._Lead.Length; i++)
                {
                    DSP.IFilter
                        rhythmFilter = null,
                        medianFilter = null;

                    if ((_Lead[i].Rhythm != null)
                    &&  (this.RhythmSamplesPerSecond > 0))
                    {
                        if ((filters[i] == null)
                        ||  !(filters[i] is DSP.BandpassFilterButterworthImplementation)) 
                        {
                            filters[i] = new DSP.BandpassFilterButterworthImplementation(bottom, top, numSections, sigs.RhythmSamplesPerSecond);
                        }

                        rhythmFilter = filters[i]; 
                    }

                    if ((_Lead[i].Median != null)
                    &&  (this.MedianSamplesPerSecond > 0))
                    {
                        medianFilter = new DSP.BandpassFilterButterworthImplementation(bottom, top, numSections, sigs.MedianSamplesPerSecond);
                    }

                    sigs._Lead[i] = this._Lead[i].ApplyFilter(rhythmFilter, medianFilter);

                    if (sigs._Lead[i] == null)
                        return null;
                }
            }

            return sigs;
        }
        /// <summary>
        /// Apply lowpass filter to Signal object
        /// </summary>
        /// <param name="cutoff">top frequency of bandpass filter</param>
        /// <returns>a filtered copy of the signal object</returns>
        public Signals ApplyLowpassFilter(double cutoff)
        {
            DSP.IFilter[] tmp = null;

            return ApplyLowpassFilter(cutoff, 2, ref tmp);
        }
        /// <summary>
        /// Apply lowpass filter to Signal object
        /// </summary>
        /// <param name="cutoff">top frequency of bandpass filter</param>
        /// <param name="numSections">nr of sections to use in filter (default: 2)</param>
        /// <returns>a filtered copy of the signal object</returns>
        public Signals ApplyLowpassFilter(double cutoff, int numSections)
        {
            DSP.IFilter[] tmp = null;

            return ApplyLowpassFilter(cutoff, numSections, ref tmp);
        }

        /// <summary>
        /// Apply lowpass filter to Signal object
        /// </summary>
        /// <param name="cutoff">top frequency of bandpass filter</param>
        /// <param name="numSections">nr of sections to use in filter (default: 2)</param>
        /// <param name="filters">provide filters that where used</param>
        /// <returns>a filtered copy of the signal object</returns>
        public Signals ApplyLowpassFilter(double cutoff, int numSections, ref DSP.IFilter[] filters)
        {
            Signals sigs = new Signals();

            sigs.RhythmAVM = this.RhythmAVM;
            sigs.RhythmSamplesPerSecond = this.RhythmSamplesPerSecond;

            sigs.MedianAVM = this.MedianAVM;
            sigs.MedianLength = this.MedianLength;
            sigs.MedianSamplesPerSecond = this.MedianSamplesPerSecond;
            sigs.MedianFiducialPoint = this.MedianFiducialPoint;

            if (this.QRSZone != null)
            {
                sigs.QRSZone = new QRSZone[this.QRSZone.Length];

                for (int i = 0; i < sigs.QRSZone.Length; i++)
                    sigs.QRSZone[i] = this.QRSZone[i].Clone();
            }

            if (this._Lead != null)
            {
                sigs.NrLeads = this.NrLeads;

                if (filters == null
                || filters.Length != this.NrLeads)
                {
                    filters = new DSP.IFilter[this.NrLeads];
                }

                for (int i = 0; i < sigs._Lead.Length; i++)
                {
                    DSP.IFilter
                        rhythmFilter = null,
                        medianFilter = null;

                    if ((_Lead[i].Rhythm != null)
                    && (this.RhythmSamplesPerSecond > 0))
                    {
                        if ((filters[i] == null)
                        ||  !(filters[i] is DSP.LowpassFilterButterworthImplementation))
                        {
                            filters[i] = new DSP.LowpassFilterButterworthImplementation(cutoff, numSections, sigs.RhythmSamplesPerSecond);
                        }

                        rhythmFilter = filters[i];
                    }

                    if ((_Lead[i].Median != null)
                    && (this.MedianSamplesPerSecond > 0))
                    {
                        medianFilter = new DSP.LowpassFilterButterworthImplementation(cutoff, numSections, sigs.MedianSamplesPerSecond);
                    }

                    sigs._Lead[i] = this._Lead[i].ApplyFilter(rhythmFilter, medianFilter);

                    if (sigs._Lead[i] == null)
                        return null;
                }
            }

            return sigs;
        }
        /// <summary>
        /// Apply highpass filter to Signal object
        /// </summary>
        /// <param name="cutoff">top frequency of bandpass filter</param>
        /// <returns>a filtered copy of the signal object</returns>
        public Signals ApplyHighpassFilter(double cutoff)
        {
            DSP.IFilter[] tmp = null;

            return ApplyHighpassFilter(cutoff, 2, ref tmp);
        }
        /// <summary>
        /// Apply highpass filter to Signal object
        /// </summary>
        /// <param name="cutoff">top frequency of bandpass filter</param>
        /// <param name="numSections">nr of sections to use in filter (default: 2)</param>
        /// <returns>a filtered copy of the signal object</returns>
        public Signals ApplyHighpassFilter(double cutoff, int numSections)
        {
            DSP.IFilter[] tmp = null;

            return ApplyHighpassFilter(cutoff, numSections, ref tmp);
        }
        /// <summary>
        /// Apply highpass filter to Signal object
        /// </summary>
        /// <param name="cutoff">top frequency of bandpass filter</param>
        /// <param name="numSections">nr of sections to use in filter (default: 2)</param>
        /// <param name="filters">provide filters that where used</param>
        /// <returns>a filtered copy of the signal object</returns>
        public Signals ApplyHighpassFilter(double cutoff, int numSections, ref DSP.IFilter[] filters)
        {
            Signals sigs = new Signals();

            sigs.RhythmAVM = this.RhythmAVM;
            sigs.RhythmSamplesPerSecond = this.RhythmSamplesPerSecond;

            sigs.MedianAVM = this.MedianAVM;
            sigs.MedianLength = this.MedianLength;
            sigs.MedianSamplesPerSecond = this.MedianSamplesPerSecond;
            sigs.MedianFiducialPoint = this.MedianFiducialPoint;

            if (this.QRSZone != null)
            {
                sigs.QRSZone = new QRSZone[this.QRSZone.Length];

                for (int i = 0; i < sigs.QRSZone.Length; i++)
                    sigs.QRSZone[i] = this.QRSZone[i].Clone();
            }

            if (this._Lead != null)
            {
                sigs.NrLeads = this.NrLeads;

                if (filters == null
                || filters.Length != this.NrLeads)
                {
                    filters = new DSP.IFilter[this.NrLeads];
                }

                for (int i = 0; i < sigs._Lead.Length; i++)
                {
                    DSP.IFilter
                        rhythmFilter = null,
                        medianFilter = null;

                    if ((_Lead[i].Rhythm != null)
                    && (this.RhythmSamplesPerSecond > 0))
                    {
                        if ((filters[i] == null)
                        || !(filters[i] is DSP.HighpassFilterButterworthImplementation))
                        {
                            filters[i] = new DSP.HighpassFilterButterworthImplementation(cutoff, numSections, sigs.RhythmSamplesPerSecond);
                        }

                        rhythmFilter = filters[i];
                    }

                    if ((_Lead[i].Median != null)
                    && (this.MedianSamplesPerSecond > 0))
                    {
                        medianFilter = new DSP.HighpassFilterButterworthImplementation(cutoff, numSections, sigs.MedianSamplesPerSecond);
                    }

                    sigs._Lead[i] = this._Lead[i].ApplyFilter(rhythmFilter, medianFilter);

                    if (sigs._Lead[i] == null)
                        return null;
                }
            }

            return sigs;
        }
		/// <summary>
		/// Function to make leads a certain length.
		/// </summary>
		/// <param name="seconds">length in seconds</param>
		public void MakeSpecificLength(int seconds)
		{
			MakeSpecificLength(seconds, 0);
		}
		/// <summary>
		/// Function to make leads a certain length.
		/// </summary>
		/// <param name="seconds">length (in seconds)</param>
		/// <param name="startPoint">start point in signal (in seconds)</param>
		public void MakeSpecificLength(int seconds, int startPoint)
		{
			int start, end;

			seconds *= this.RhythmSamplesPerSecond;
			startPoint *= this.RhythmSamplesPerSecond;

			CalculateStartAndEnd(out start, out end);

			foreach (Signal sig in _Lead)
			{
				short[] newSig = new short[seconds+1];

				for (int n=0;n <= seconds;n++)
				{
					int pos = (n - startPoint) + start;

					newSig[n] = ((pos >= sig.RhythmStart) && (pos < sig.RhythmEnd)) ? sig.Rhythm[pos - sig.RhythmStart] : (short) 0;
				}

				sig.Rhythm = newSig;
				sig.RhythmStart = 0;
				sig.RhythmEnd = seconds;
			}
		}
		/// <summary>
		/// Function to resample all leads.
		/// </summary>
		/// <param name="samplesPerSecond">samples per second to resample towards</param>
		public void Resample(int samplesPerSecond)
		{
			foreach (Signal sig in this._Lead)
			{
				if ((this.RhythmSamplesPerSecond != 0)
				&&	(this.RhythmAVM != 0)
				&&	(sig.Rhythm != null))
				{
					ECGTool.ResampleLead(sig.Rhythm, this.RhythmSamplesPerSecond, samplesPerSecond, out sig.Rhythm);

					sig.RhythmStart = (int) (((long)sig.RhythmStart * (long)samplesPerSecond) / (long)this.RhythmSamplesPerSecond);
					sig.RhythmEnd = (int) (((long)sig.RhythmEnd * (long)samplesPerSecond) / (long)this.RhythmSamplesPerSecond);
				}

				if ((this.MedianSamplesPerSecond != 0)
				&&	(this.MedianAVM != 0)
				&&	(sig.Median != null))
				{
					ECGTool.ResampleLead(sig.Median, this.MedianSamplesPerSecond, samplesPerSecond, out sig.Median);
				}
			}

			if (this.QRSZone != null)
			{
				foreach (QRSZone zone in this.QRSZone)
				{
					zone.Start = (int) (((long)zone.Start * (long)samplesPerSecond) / (long)this.MedianSamplesPerSecond);
					zone.Fiducial = (int) (((long)zone.Fiducial * (long)samplesPerSecond) / (long)this.MedianSamplesPerSecond);
					zone.End = (int) (((long)zone.End * (long)samplesPerSecond) / (long)this.MedianSamplesPerSecond);
				}
			}

			if ((this.RhythmSamplesPerSecond != 0)
			&&	(this.RhythmAVM != 0))
			{
				this.RhythmSamplesPerSecond = samplesPerSecond;
			}

			if ((this.MedianSamplesPerSecond != 0)
			&&	(this.MedianAVM != 0))
			{
				this.MedianFiducialPoint = (ushort) (((long)this.MedianFiducialPoint * (long)samplesPerSecond) / (long)this.MedianSamplesPerSecond);

				this.MedianSamplesPerSecond = samplesPerSecond;
			}
		}
		/// <summary>
		/// Set AVM for all signals
		/// </summary>
		/// <param name="avm">preferred multiplier</param>
		public void SetAVM(double avm)
		{
			if (avm != 0.0)
			{
				int nrLeads = this.NrLeads;

				for (int i=0;i < nrLeads;i++)
				{
					ECGTool.ChangeMultiplier(this[i].Rhythm, this.RhythmAVM, avm);
					ECGTool.ChangeMultiplier(this[i].Median, this.MedianAVM, avm);
				}

				if (this.RhythmAVM != 0.0)
					this.RhythmAVM = avm;

				if (this.MedianAVM != 0.0)
					this.MedianAVM = avm;
			}
		}
		/// <summary>
		/// Determine whether this is twelve lead signal.
		/// </summary>
		/// <returns>true if twelve lead signal.</returns>
		public bool IsTwelveLeads
		{
			get
			{
				LeadType[] lt = new LeadType[]{	LeadType.I, LeadType.II, LeadType.III,
												  LeadType.aVR, LeadType.aVL, LeadType.aVF,
												  LeadType.V1, LeadType.V2, LeadType.V3,
												  LeadType.V4, LeadType.V5, LeadType.V6};

                

				int nrSim = NrSimultaneosly();			

				if (nrSim != _Lead.Length)
					return false;

				if (nrSim == lt.Length)
				{
					for (int i=0;i < nrSim;i++)
						if (_Lead[i].Type != lt[i])
							return false;

					return true;
				}
                else if (nrSim == 15)
                {
                    int i = 0;

                    for (; i < lt.Length; i++)
                        if (_Lead[i].Type != lt[i])
                            return false;

                    LeadType[][] extra = new LeadType[][] { new LeadType[] { LeadType.V3R, LeadType.V4R, LeadType.V7 }, new LeadType[] { LeadType.V7, LeadType.V8, LeadType.V9 } };
                    bool[] check = new bool[extra.Length];

                    for (int c=0;c<check.Length;c++)
                        check[c] = true;

                    for (int j = 0; i < nrSim; i++, j++)
                        for (int c = 0; c < extra.Length; c++)
                            check[c] &= _Lead[i].Type == extra[c][j];

                    for (i = 0; i < check.Length; i++)
                        if (check[i])
                            return true;
                }

				return false;
			}
		}
        /// <summary>
        /// Determine whether this is fifteen lead signal.
        /// </summary>
        /// <returns>true if twelve lead signal.</returns>
        public bool IsFifteenLeads
        {
            get
            {
                LeadType[][] lts = new LeadType[][] { new LeadType[] { LeadType.I, LeadType.II, LeadType.III,
												  LeadType.aVR, LeadType.aVL, LeadType.aVF,
												  LeadType.V1, LeadType.V2, LeadType.V3,
												  LeadType.V4, LeadType.V5, LeadType.V6, 
                                                  LeadType.V3R, LeadType.V4R, LeadType.V7 },
                                                  new LeadType[] { LeadType.I, LeadType.II, LeadType.III,
												  LeadType.aVR, LeadType.aVL, LeadType.aVF,
												  LeadType.V1, LeadType.V2, LeadType.V3,
												  LeadType.V4, LeadType.V5, LeadType.V6,
                                                  LeadType.V7, LeadType.V8, LeadType.V9 } };

                int nrSim = NrSimultaneosly();

                if (nrSim != _Lead.Length)
                    return false;

                if (nrSim == 15)
                {
                    bool[] check = new bool[lts.Length];

                    for (int c = 0; c < check.Length; c++)
                        check[c] = true;

                    for (int i = 0; i < nrSim; i++)
                        for (int c = 0; c < lts.Length; c++)
                            check[c] &= _Lead[i].Type == lts[c][i];

                    for (int i = 0; i < check.Length; i++)
                        if (check[i])
                            return true;
                }

				return false;
			}
		}
		/// <summary>
		/// Function to make a twelve leads signals object.
		/// </summary>
		/// <returns>returns twelve leads signals object or null</returns>
		public Signals CalculateTwelveLeads()
		{
			LeadType[] lt = new LeadType[]{	LeadType.I, LeadType.II, LeadType.III,
											LeadType.aVR, LeadType.aVL, LeadType.aVF,
											LeadType.V1, LeadType.V2, LeadType.V3,
											LeadType.V4, LeadType.V5, LeadType.V6};

			int nrSim = NrSimultaneosly();

			if (nrSim != _Lead.Length)
				return null;

			Signal[] leads = null;

			if (nrSim == 12)
			{
                ArrayList pos_list = new ArrayList(lt);

				int check_one = 0;
				ArrayList check_two = new ArrayList(lt);
				Signal[] pos = new Signal[12];

				for (int i=0;i < nrSim;i++)
				{
					if (_Lead[i].Type == lt[i])
						check_one++;

					int temp = check_two.IndexOf(_Lead[i].Type);
					if (temp < 0)
						return null;

					check_two.RemoveAt(temp);

					pos[pos_list.IndexOf(_Lead[i].Type)] = _Lead[i];
				}

				if (check_one == 12)
					return this;

				if (check_two.Count == 0)
				{
					for (int i=0;i < pos.Length;i++)
						if (pos[i] != null)
							pos[i] = pos[i].Clone();

					leads = pos;
				}
			}
			else
			{
				short[][]
					tempRhythm = null,
					tempMedian = null;

				Signal[] pos = new Signal[12];

				if (nrSim == 8)
				{
                    ArrayList pos_list = new ArrayList(lt);

					ArrayList check = new ArrayList(
						new LeadType[]{	LeadType.I, LeadType.II,
										LeadType.V1, LeadType.V2, LeadType.V3,
										LeadType.V4, LeadType.V5, LeadType.V6});

					for (int i=0;i < nrSim;i++)
					{
						int temp = check.IndexOf(_Lead[i].Type);
						if (temp < 0)
							return null;

						check.RemoveAt(temp);

                        pos[pos_list.IndexOf(_Lead[i].Type)] = _Lead[i];
					}

					if (check.Count == 0)
					{
						for (int i=0;i < pos.Length;i++)
							if (pos[i] != null)
								pos[i] = pos[i].Clone();

						tempRhythm = new short[2][];
						tempRhythm[0] = pos[0].Rhythm;
						tempRhythm[1] = pos[1].Rhythm;

						tempMedian = new short[2][];
						tempMedian[0] = pos[0].Median;
						tempMedian[1] = pos[1].Median;
					}
				}
				else if (nrSim == 9)
				{
                    ArrayList pos_list = new ArrayList(lt);

					ArrayList check = new ArrayList(
						new LeadType[]{	LeadType.I, LeadType.II, LeadType.III,
										LeadType.V1, LeadType.V2, LeadType.V3,
										LeadType.V4, LeadType.V5, LeadType.V6});

					for (int i=0;i < nrSim;i++)
					{
						int temp = check.IndexOf(_Lead[i].Type);
						if (temp < 0)
							return null;

						check.RemoveAt(temp);

                        pos[pos_list.IndexOf(_Lead[i].Type)] = _Lead[i];
					}

					if (check.Count == 0)
					{
						for (int i=0;i < pos.Length;i++)
							if (pos[i] != null)
								pos[i] = pos[i].Clone();

						tempRhythm = new short[3][];
						tempRhythm[0] = pos[0].Rhythm;
						tempRhythm[1] = pos[1].Rhythm;
						tempRhythm[2] = pos[2].Rhythm;

						tempMedian = new short[3][];
						tempMedian[0] = pos[0].Median;
						tempMedian[1] = pos[1].Median;
						tempMedian[2] = pos[2].Median;
					}
				}

				if ((tempRhythm != null)
				||	(tempMedian != null))
				{
					short[][] calcLeads;

					if ((tempRhythm != null)
					&&	(tempRhythm[0] != null)
					&&	ECGTool.CalculateLeads(tempRhythm, tempRhythm[0].Length, out calcLeads) == 0)
					{
						for (int i=0;i < calcLeads.Length;i++)
						{
							Signal sig = new Signal();
							sig.Type = lt[i + tempRhythm.Length];
							sig.RhythmStart	= pos[0].RhythmStart;
							sig.RhythmEnd	= pos[0].RhythmEnd;
							sig.Rhythm = calcLeads[i];

							pos[i + tempRhythm.Length] = sig; 
						}

						if ((tempMedian != null)
						&&	(tempMedian[0] != null)
						&&	(ECGTool.CalculateLeads(tempMedian, tempMedian[0].Length, out calcLeads) == 0))
						{
							for (int i=0;i < calcLeads.Length;i++)
							{
								pos[i + tempRhythm.Length].Median = calcLeads[i];
							}
						}

						leads = pos;
					}
				}
			}

			if (leads != null)
			{
				Signals sigs = this.Clone();

				sigs.NrLeads = (byte) leads.Length;

				for (int i=0;i < leads.Length;i++)
					sigs._Lead[i] = leads[i];

				return sigs;
			}

			return null;
		}

        /// <summary>
        /// Function to make a fifteen leads signals object.
        /// </summary>
        /// <returns>returns fifteen leads signals object or null</returns>
        public Signals CalculateFifteenLeads()
        {
            LeadType[] lt1 = new LeadType[] { LeadType.I, LeadType.II, LeadType.III,
												  LeadType.aVR, LeadType.aVL, LeadType.aVF,
												  LeadType.V1, LeadType.V2, LeadType.V3,
												  LeadType.V4, LeadType.V5, LeadType.V6, 
                                                  LeadType.V3R, LeadType.V4R, LeadType.V7 };
            LeadType[] lt2 =  new LeadType[] { LeadType.I, LeadType.II, LeadType.III,
												  LeadType.aVR, LeadType.aVL, LeadType.aVF,
												  LeadType.V1, LeadType.V2, LeadType.V3,
												  LeadType.V4, LeadType.V5, LeadType.V6,
                                                  LeadType.V7, LeadType.V8, LeadType.V9 };

            int nrSim = NrSimultaneosly();

            if (nrSim != _Lead.Length)
                return null;

            Signal[] leads = null;

            if (nrSim == lt1.Length)
            {
                ArrayList pos_list1 = new ArrayList(lt1);
                ArrayList pos_list2 = new ArrayList(lt2);

                int check_one1 = 0;
                int check_one2 = 0;
                ArrayList check_two1 = new ArrayList(lt1);
                ArrayList check_two2 = new ArrayList(lt2);
                Signal[] pos1 = new Signal[lt1.Length];
                Signal[] pos2 = new Signal[lt2.Length];

                for (int i = 0; i < nrSim; i++)
                {
                    if (_Lead[i].Type == lt1[i])
                        check_one1++;

                    if (_Lead[i].Type == lt2[i])
                        check_one2++;

                    int temp = check_two1.IndexOf(_Lead[i].Type);
                    if (temp >= 0)
                    {
                        check_two1.RemoveAt(temp);

                        pos1[pos_list1.IndexOf(_Lead[i].Type)] = _Lead[i];
                    }
                    temp = check_two2.IndexOf(_Lead[i].Type);
                    if (temp >= 0)
                    {
                        check_two2.RemoveAt(temp);

                        pos2[pos_list2.IndexOf(_Lead[i].Type)] = _Lead[i];
                    }
                }

                if (check_one1 == lt1.Length)
                    return this;
                if (check_one2 == lt2.Length)
                    return this;

                if (check_two1.Count == 0)
                {
                    for (int i = 0; i < pos1.Length; i++)
                        if (pos1[i] != null)
                            pos1[i] = pos1[i].Clone();

                    leads = pos1;
                }
                else if (check_two2.Count == 0)
                {
                    for (int i = 0; i < pos2.Length; i++)
                        if (pos2[i] != null)
                            pos2[i] = pos2[i].Clone();

                    leads = pos2;
                }
            }
            else
            {
                LeadType[] lt = null;

                short[][]
                    tempRhythm = null,
                    tempMedian = null;

                Signal[] pos = null;

                if (nrSim == 11)
                {
                    Signal[] pos1 = new Signal[lt1.Length];
                    Signal[] pos2 = new Signal[lt2.Length];

                    ArrayList pos_list1 = new ArrayList(lt1);
                    ArrayList pos_list2 = new ArrayList(lt2);

                    ArrayList check1 = new ArrayList(
                        new LeadType[]{	LeadType.I, LeadType.II,
										LeadType.V1, LeadType.V2, LeadType.V3,
										LeadType.V4, LeadType.V5, LeadType.V6,
                                        LeadType.V7, LeadType.V3R, LeadType.V4R});

                    ArrayList check2 = new ArrayList(
                        new LeadType[]{	LeadType.I, LeadType.II,
										LeadType.V1, LeadType.V2, LeadType.V3,
										LeadType.V4, LeadType.V5, LeadType.V6,
                                        LeadType.V7, LeadType.V8, LeadType.V9});

                    for (int i = 0; i < nrSim; i++)
                    {
                        int temp = check1.IndexOf(_Lead[i].Type);
                        if (temp >= 0)
                        {
                            check1.RemoveAt(temp);
    
                            pos1[pos_list1.IndexOf(_Lead[i].Type)] = _Lead[i];
                        }
                        temp = check2.IndexOf(_Lead[i].Type);
                        if (temp >= 0)
                        {
                            check2.RemoveAt(temp);

                            pos2[pos_list2.IndexOf(_Lead[i].Type)] = _Lead[i];
                        }
                    }

                    if (check1.Count == 0)
                    {
                        pos = pos1;
                        lt = lt1;
                    }
                    else if (check2.Count == 0)
                    {
                        pos = pos2;
                        lt = lt2;
                    }

                    if (pos != null)
                    {
                        for (int i = 0; i < pos.Length; i++)
                            if (pos[i] != null)
                                pos[i] = pos[i].Clone();

                        tempRhythm = new short[2][];
                        tempRhythm[0] = pos1[0].Rhythm;
                        tempRhythm[1] = pos1[1].Rhythm;

                        tempMedian = new short[2][];
                        tempMedian[0] = pos1[0].Median;
                        tempMedian[1] = pos1[1].Median;
                    }
                }
                else if (nrSim == 12)
                {
                    Signal[] pos1 = new Signal[lt1.Length];
                    Signal[] pos2 = new Signal[lt2.Length];

                    ArrayList pos_list1 = new ArrayList(lt1);
                    ArrayList pos_list2 = new ArrayList(lt2);

                    ArrayList check1 = new ArrayList(
                        new LeadType[]{	LeadType.I, LeadType.II, LeadType.III,
										LeadType.V1, LeadType.V2, LeadType.V3,
										LeadType.V4, LeadType.V5, LeadType.V6,
                                        LeadType.V7, LeadType.V3R, LeadType.V4R});

                    ArrayList check2 = new ArrayList(
                        new LeadType[]{	LeadType.I, LeadType.II, LeadType.III,
										LeadType.V1, LeadType.V2, LeadType.V3,
										LeadType.V4, LeadType.V5, LeadType.V6,
                                        LeadType.V7, LeadType.V8, LeadType.V9});

                    for (int i = 0; i < nrSim; i++)
                    {
                        int temp = check1.IndexOf(_Lead[i].Type);
                        if (temp >= 0)
                        {
                            check1.RemoveAt(temp);
    
                            pos1[pos_list1.IndexOf(_Lead[i].Type)] = _Lead[i];
                        }
                        temp = check2.IndexOf(_Lead[i].Type);
                        if (temp >= 0)
                        {
                            check2.RemoveAt(temp);

                            pos2[pos_list2.IndexOf(_Lead[i].Type)] = _Lead[i];
                        }
                    }

                    if (check1.Count == 0)
                    {
                        pos = pos1;
                        lt = lt1;
                    }
                    else if (check2.Count == 0)
                    {
                        pos = pos2;
                        lt = lt2;
                    }

                    if (pos != null)
                    {
                        for (int i = 0; i < pos.Length; i++)
                            if (pos[i] != null)
                                pos[i] = pos[i].Clone();

                        tempRhythm = new short[3][];
                        tempRhythm[0] = pos[0].Rhythm;
                        tempRhythm[1] = pos[1].Rhythm;
                        tempRhythm[2] = pos[2].Rhythm;

                        tempMedian = new short[3][];
                        tempMedian[0] = pos[0].Median;
                        tempMedian[1] = pos[1].Median;
                        tempMedian[2] = pos[2].Median;
                    }
                }

                if ((tempRhythm != null)
                ||  (tempMedian != null))
                {
                    short[][] calcLeads;

                    if ((tempRhythm != null)
                    &&  (tempRhythm[0] != null)
                    &&  ECGTool.CalculateLeads(tempRhythm, tempRhythm[0].Length, out calcLeads) == 0)
                    {
                        for (int i = 0; i < calcLeads.Length; i++)
                        {
                            Signal sig = new Signal();
                            sig.Type = lt[i + tempRhythm.Length];
                            sig.RhythmStart = pos[0].RhythmStart;
                            sig.RhythmEnd = pos[0].RhythmEnd;
                            sig.Rhythm = calcLeads[i];

                            pos[i + tempRhythm.Length] = sig;
                        }

                        if ((tempMedian != null)
                        && (tempMedian[0] != null)
                        && (ECGTool.CalculateLeads(tempMedian, tempMedian[0].Length, out calcLeads) == 0))
                        {
                            for (int i = 0; i < calcLeads.Length; i++)
                            {
                                pos[i + tempRhythm.Length].Median = calcLeads[i];
                            }
                        }

                        leads = pos;
                    }
                }
            }

            if (leads != null)
            {
                Signals sigs = this.Clone();

                sigs.NrLeads = (byte)leads.Length;

                for (int i = 0; i < leads.Length; i++)
                    sigs._Lead[i] = leads[i];

                return sigs;
            }

            return null;
        }
	}
}
