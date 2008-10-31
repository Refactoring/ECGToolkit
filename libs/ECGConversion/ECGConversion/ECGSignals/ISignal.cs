/***************************************************************************
Copyright 2004, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
	/// Interface for manupalation of the signals.
	/// </summary>
	public interface ISignal
	{
		/// <summary>
		/// Function to get the signal of an ECG.
		/// </summary>
		/// <param name="signals">signals</param>
		/// <returns>0 on succes</returns>
		int getSignals(out Signals signals);
		/// <summary>
		/// Function to get the signals of an ECG and set a given Signals object.
		/// </summary>
		/// <param name="signals">signals</param>
		/// <returns>0 on success</returns>
		int getSignalsToObj(Signals signals);
		/// <summary>
		/// Function to set the signals of an ECG.
		/// </summary>
		/// <param name="signals">signals</param>
		/// <returns>0 on success</returns>
		int setSignals(Signals signals);
	}
}
