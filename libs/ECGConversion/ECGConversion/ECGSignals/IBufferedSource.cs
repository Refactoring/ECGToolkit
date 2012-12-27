/***************************************************************************
Copyright 2012, van Ettinger Information Technology, Lopik, The Netherlands

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
	public interface IBufferedSource
	{
		/// <summary>
		/// Loads the rhythm data for buffered signal.
		/// </summary>
		/// <param name='leadNr'>nr of the lead</param>
		/// <param name='lead'>the lead to load signal into</param>
		/// <param name='avm'>the AVM used by this rhythms data</param>
		/// <param name='rhythmStart'>start of section to load (0 is beginning of signal in file)</param>
		/// <param name='rhythmEnd'>end of section to load (0 is beginning of signal in file)</param>
		/// <returns>
		/// returns true if signal is loaded succesfully
		/// </returns>
		bool LoadRhythmSignal(byte leadNr, Signal lead, double avm, int rhythmStart, int rhythmEnd);

		/// <summary>
		/// Loads the Median template data for buffered signal.
		/// </summary>
		/// <param name='leadNr'>nr of the lead</param>
		/// <param name='lead'>the lead to load signal into</param>
		/// <param name='avm'>the AVM used by this rhythms data</param>
		/// <param name='templateNr'>template nr to load signal for</param>
		/// <returns>
		/// returns true if signal is loaded succesfully
		/// </returns>
		bool LoadTemplateSignal(byte leadNr, Signal lead, double avm, int templateNr);

		/// <summary>
		/// Loads the template occurance.
		/// </summary>
		/// <param name='templateNr'>template nr to load occurance info for</param>
		/// <param name='templateOccurance'>nr of occurance of template</param>
		/// <param name='templateLocations'>locations associated to this template</param>
		void LoadTemplateOccurance(int templateNr, out int templateOccurance, out QRSZone[] templateLocations);
	}
}

