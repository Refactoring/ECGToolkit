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
    public class LowpassFilterButterworthImplementation : IFilter
    {
        protected LowpassFilterButterworthSection[] section;

        public LowpassFilterButterworthImplementation
               (double cutoffFrequencyHz, int numSections, double Fs)
        {
            this.section = new LowpassFilterButterworthSection[numSections];
            for (int i = 0; i < numSections; i++)
            {
                this.section[i] = new LowpassFilterButterworthSection
                                  (cutoffFrequencyHz, i + 1, numSections * 2, Fs);
            }
        }
        public double compute(double input)
        {
            double output = input;
            for (int i = 0; i < this.section.Length; i++)
            {
                output = this.section[i].compute(output);
            }
            return output;
        }
    }
}