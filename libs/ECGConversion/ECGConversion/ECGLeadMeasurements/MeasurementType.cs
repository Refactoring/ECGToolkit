/***************************************************************************
Copyright 2008,2021, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
namespace ECGConversion.ECGLeadMeasurements
{
	/// <summary>
	/// Enumration MeasurementType for lead measurements.
	/// </summary>
	public enum MeasurementType
	{
		None = -1,
		Pdur = 0,
		PRint,
		QRSdur,
		QTint,
		Qdur,
		Rdur,
		Sdur,
		RRdur,
		SSdur,
        RRRdur,
		Qamp,
		Ramp,
		Samp,
		RRamp,
		SSamp,
        RRRamp,
		Jamp,
		Pamp_pos,
		Pamp_min,
		Tamp_pos,
		Tamp_min,
		STslope,
		Pmorphology,
		Tmorphology,
		IsoElectricQRSonset,
		IsoElectricQRSend,
		IntrinsicoidDeflection,
		QualityCode,
		STampJ20,
		STampJ60,
		STampJ80,
		STamp1_16RR,
		STamp1_8RR,
        QRSonset,
        QRSoffset,
        Qoffset,
        Roffset,
        Soffset,
        RRoffset,
        SSoffset,
        RRRoffset,
        Toffset,
        Pnotch,
        Rnotch,

        Ronset = Qoffset,
        Sonset =  Roffset,
        RRonset = Soffset,
        SSonset = RRoffset,
        RRRonset = SSoffset, 
	}
}
