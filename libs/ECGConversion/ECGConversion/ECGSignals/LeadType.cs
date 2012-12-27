/***************************************************************************
Copyright 2012, van Ettinger Information Technology, Lopik, The Netherlands
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

namespace ECGConversion.ECGSignals
{
    /// <summary>
    /// Enumaration for lead types.
    /// </summary>
    public enum LeadType
    {
        Unknown = 0, I, II, V1, V2, V3, V4, V5, V6, V7, V2R, V3R, V4R, V5R, V6R, V7R, X, Y, Z,
		CC5, CM5, LA, RA, LL, fI, fE, fC, fA, fM, fF, fH, dI, dII, dV1, dV2, dV3, dV4, dV5, dV6,
		dV7, dV2R, dV3R, dV4R, dV5R, dV6R, dV7R, dX, dY, dZ, dCC5, dCM5, dLA, dRA, dLL, dfI, dfE,
		dfC, dfA, dfM, dfF, dfH, III, aVR, aVL, aVF, aVRneg, V8, V9, V8R, V9R, D, A, J, Defib, 
        Extern, A1, A2, A3, A4, dV8, dV9, dV8R, dV9R, dD, dA, dJ, Chest, V, VR, VL, VF, MCL, MCL1,
		MCL2, MCL3, MCL4, MCL5, MCL6, CC, CC1, CC2, CC3, CC4, CC6, CC7, CM, CM1, CM2, CM3, CM4, CM6,
		dIII, daVR, daVL, daVF, daVRneg, dChest, dV, dVR, dVL, dVF, CM7, CH5, CS5, CB5, CR5, ML, AB1,
		AB2, AB3, AB4, ES, AS, AI, S, dDefib, dExtern, dA1, dA2, dA3, dA4, dMCL1, dMCL2, dMCL3,
		dMCL4, dMCL5, dMCL6, RL, CV5RL, CV6LL, CV6LU, V10, dMCL, dCC, dCC1, dCC2, dCC3, dCC4, dCC6,
		dCC7,  dCM, dCM1, dCM2, dCM3, dCM4, dCM6, dCM7, dCH5, dCS5, dCB5, dCR5, dML, dAB1, dAB2, dAB3,
		dAB4, dES, dAS, dAI, dS, dRL, dCV5RL, dCV6LL, dCV6LU, dV10
	}

	public enum LeadTypeVitalRefId
	{
		MDC_ECG_LEAD_CONFIG = 0,
		MDC_ECG_LEAD_I,
		MDC_ECG_LEAD_II,
		MDC_ECG_LEAD_V1,
		MDC_ECG_LEAD_V2,
		MDC_ECG_LEAD_V3,
		MDC_ECG_LEAD_V4,
		MDC_ECG_LEAD_V5,
		MDC_ECG_LEAD_V6,
		MDC_ECG_LEAD_V7,
		MDC_ECG_LEAD_V2R,
		MDC_ECG_LEAD_V3R,
		MDC_ECG_LEAD_V4R,
		MDC_ECG_LEAD_V5R,
		MDC_ECG_LEAD_V6R,
		MDC_ECG_LEAD_V7R,
		MDC_ECG_LEAD_X,
		MDC_ECG_LEAD_Y,
		MDC_ECG_LEAD_Z,
		MDC_ECG_LEAD_CC5,
		MDC_ECG_LEAD_CM5,
		MDC_ECG_LEAD_LA,
		MDC_ECG_LEAD_RA,
		MDC_ECG_LEAD_LL,
		MDC_ECG_LEAD_fI,
		MDC_ECG_LEAD_fE,
		MDC_ECG_LEAD_fC,
		MDC_ECG_LEAD_fA, 
		MDC_ECG_LEAD_fM, 
		MDC_ECG_LEAD_fF,
		MDC_ECG_LEAD_fH,
		MDC_ECG_LEAD_dI,
		MDC_ECG_LEAD_dII,
		MDC_ECG_LEAD_dV1,
		MDC_ECG_LEAD_dV2,
		MDC_ECG_LEAD_dV3,
		MDC_ECG_LEAD_dV4,
		MDC_ECG_LEAD_dV5,
		MDC_ECG_LEAD_dV6,
		MDC_ECG_LEAD_III = 61,
		MDC_ECG_LEAD_aVR,
		MDC_ECG_LEAD_aVL,
		MDC_ECG_LEAD_aVF,
		MDC_ECG_LEAD_aVRneg,
		MDC_ECG_LEAD_V8,
		MDC_ECG_LEAD_V9,
		MDC_ECG_LEAD_V8R,
		MDC_ECG_LEAD_V9R,
		MDC_ECG_LEAD_D,
		MDC_ECG_LEAD_A,
		MDC_ECG_LEAD_J,
		MDC_ECG_LEAD_Defib, 
		MDC_ECG_LEAD_Extern,
		MDC_ECG_LEAD_A1,
		MDC_ECG_LEAD_A2,
		MDC_ECG_LEAD_A3,
		MDC_ECG_LEAD_A4,
		MDC_ECG_LEAD_C = 86,
		MDC_ECG_LEAD_V,
		MDC_ECG_LEAD_VR,
		MDC_ECG_LEAD_VL,
		MDC_ECG_LEAD_VF,
		MDC_ECG_LEAD_MCL,
		MDC_ECG_LEAD_MCL1,
		MDC_ECG_LEAD_MCL2,
		MDC_ECG_LEAD_MCL3,
		MDC_ECG_LEAD_MCL4, 
		MDC_ECG_LEAD_MCL5,
		MDC_ECG_LEAD_MCL6,
		MDC_ECG_LEAD_CC,
		MDC_ECG_LEAD_CC1,
		MDC_ECG_LEAD_CC2,
		MDC_ECG_LEAD_CC3,
		MDC_ECG_LEAD_CC4,
		MDC_ECG_LEAD_CC6,
		MDC_ECG_LEAD_CC7,
		MDC_ECG_LEAD_CM,
		MDC_ECG_LEAD_CM1,
		MDC_ECG_LEAD_CM2,
		MDC_ECG_LEAD_CM3,
		MDC_ECG_LEAD_CM4,
		MDC_ECG_LEAD_CM6,
		MDC_ECG_LEAD_dIII, 
		MDC_ECG_LEAD_daVR,
		MDC_ECG_LEAD_daVL,
		MDC_ECG_LEAD_daVF,
		MDC_ECG_LEAD_CM7 = 121,
		MDC_ECG_LEAD_CH5,
		MDC_ECG_LEAD_CS5,
		MDC_ECG_LEAD_CB5,
		MDC_ECG_LEAD_CR5,
		MDC_ECG_LEAD_ML,
		MDC_ECG_LEAD_AB1,
		MDC_ECG_LEAD_AB2,
		MDC_ECG_LEAD_AB3,
		MDC_ECG_LEAD_AB4,
		MDC_ECG_LEAD_ES,
		MDC_ECG_LEAD_AS,
		MDC_ECG_LEAD_AI,
		MDC_ECG_LEAD_S,
		MDC_ECG_LEAD_RL = 147,
		MDC_ECG_LEAD_CV5RL,
		MDC_ECG_LEAD_CV6LL,
		MDC_ECG_LEAD_CV6LU,
		MDC_ECG_LEAD_V10
	}
}