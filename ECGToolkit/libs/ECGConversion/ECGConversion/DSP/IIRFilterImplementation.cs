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
    public class IIRFilterImplementation
    {
        protected double[] z;
        public IIRFilterImplementation(int order)
        {
            this.z = new double[order];
        }

        public double compute(double input, double[] a)
        {
            // computes y(t) = x(t) + a1*y(t-1) + a2*y(t-2) + ... an*y(t-n)
            // z-transform: H(z) = 1 / (1 - sum(1 to n) [an * y(t-n)])
            // a0 is assumed to be 1
            // y(t) is not stored, so y(t-1) is stored at z[0], 
            // and a1 is stored as coefficient[0]

            double result = input;

            for (int t = 0; t < a.Length; t++)
            {
                result += a[t] * this.z[t];
            }
            for (int t = a.Length - 1; t >= 0; t--)
            {
                if (t > 0)
                {
                    this.z[t] = this.z[t - 1];
                }
                else
                {
                    this.z[t] = result;
                }
            }
            return result;
        }
    }
}