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
    public class FIRFilterImplementation
    {
        protected double[] z;
        public FIRFilterImplementation(int order)
        {
            this.z = new double[order];
        }

        public double compute(double input, double[] a)
        {
            // computes y(t) = a0*x(t) + a1*x(t-1) + a2*x(t-2) + ... an*x(t-n)
            double result = 0;

            for (int t = a.Length - 1; t >= 0; t--)
            {
                if (t > 0)
                {
                    this.z[t] = this.z[t - 1];
                }
                else
                {
                    this.z[t] = input;
                }
                result += a[t] * this.z[t];
            }
            return result;
        }
    }
}