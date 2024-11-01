/***************************************************************************
Copyright 2020, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

Licensed under the Code Project Open License, Version 1.02 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	https://www.codeproject.com/info/cpol10.aspx

Origannly published on CodeProject.com:

    https://www.codeproject.com/Tips/5070936/Lowpass-Highpass-and-Bandpass-Butterworth-Filters

Edited by Maarten JB van Ettinger.

****************************************************************************/
using System;
using System.Collections.Generic;
using System.Web;

namespace ECGConversion.DSP
{
    public class BandpassFilterButterworthImplementation : IFilter
    {
        protected LowpassFilterButterworthImplementation lowpassFilter;
        protected HighpassFilterButterworthImplementation highpassFilter;

        public BandpassFilterButterworthImplementation
           (double bottomFrequencyHz, double topFrequencyHz, int numSections, double Fs)
        {
            this.lowpassFilter = new LowpassFilterButterworthImplementation
                                 (topFrequencyHz, numSections, Fs);
            this.highpassFilter = new HighpassFilterButterworthImplementation
                                  (bottomFrequencyHz, numSections, Fs);
        }

        public double compute(double input)
        {
            // compute the result as the cascade of the highpass and lowpass filters
            return this.highpassFilter.compute(this.lowpassFilter.compute(input));
        }
    }
}