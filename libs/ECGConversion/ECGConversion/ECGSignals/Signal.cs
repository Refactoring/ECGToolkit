/***************************************************************************
Copyright 2020, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands
Copyright 2012, van Ettinger Information Technology, Lopik, The Netherlands
Copyright 2004-2005,2008, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
	/// Class Containing data of one signal.
	/// </summary>
	public class Signal
	{
		public LeadType Type = LeadType.Unknown;
		public int RhythmStart = 0;
		public int RhythmEnd = 0;
		public short[] Rhythm = null;
		public short[] Median = null;
		/// <summary>
		/// Function to make a deep copy of this object.
		/// </summary>
		/// <returns>copy of object</returns>
		public Signal Clone()
		{
			Signal sig = new Signal();

			sig.Type = this.Type;
			sig.RhythmStart = this.RhythmStart;
			sig.RhythmEnd = this.RhythmEnd;

			if (this.Rhythm != null)
			{
				sig.Rhythm = new short[this.Rhythm.Length];
				for (int i=0;i < sig.Rhythm.Length;i++)
					sig.Rhythm[i] = this.Rhythm[i];
			}

			if (this.Median != null)
			{
				sig.Median = new short[this.Median.Length];
				for (int i=0;i < sig.Median.Length;i++)
					sig.Median[i] = this.Median[i];
			}

			return sig;
		}
        /// <summary>
        /// Function to apply a bandpass filter on Signal object
        /// </summary>
        /// <param name="rhythmFilter">Provide filter for rhythm data</param>
        /// <param name="medianFilter">Provide filter for median data</param>
        /// <returns>a filtered copy of object</returns>
        public Signal ApplyFilter(DSP.IFilter rhythmFilter, DSP.IFilter medianFilter)
        {
            Signal sig = new Signal();

            sig.Type = this.Type;
            sig.RhythmStart = this.RhythmStart;
            sig.RhythmEnd = this.RhythmEnd;

            if (this.Rhythm != null)
            {
                if (rhythmFilter == null)
                    return null;

                rhythmFilter.compute(this.Rhythm[0]);
                rhythmFilter.compute(this.Rhythm[0]);

                sig.Rhythm = new short[this.Rhythm.Length];
                for (int i = 0; i < sig.Rhythm.Length; i++)
                    sig.Rhythm[i] = (short) Math.Round(rhythmFilter.compute(this.Rhythm[i]));
            }

            if (this.Median != null)
            {
                if (medianFilter == null)
                    return null;

                medianFilter.compute(this.Median[0]);
                medianFilter.compute(this.Median[0]);

                sig.Median = new short[this.Median.Length];
                for (int i = 0; i < sig.Median.Length; i++)
                    sig.Median[i] = (short) Math.Round(medianFilter.compute(this.Median[i]));
            }

            return sig;
        }
		/// <summary>
		/// Function to determine if the first eigth leads are as expected (I, II, V1 - V6).
		/// </summary>
		/// <param name="data">signal information.</param>
		/// <returns>true if as expected</returns>
		public static bool IsNormal(Signal[] data)
		{
			if ((data != null)
			&&	(data.Length >= 8))
			{
				for (int loper=0;loper < 8;loper++)
				{
					if ((data[loper] == null)
					||	(data[loper].Type != (LeadType) (1 + loper)))
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}
		/// <summary>
		/// Function to determine the number of simultaneosly.
		/// </summary>
		/// <param name="data">signal information.</param>
		/// <returns>true if as expected</returns>
		public static int NrSimultaneosly(Signal[] data)
		{
			if ((data != null)
			&&  (data.Length > 1)
			&&	(data[0] != null))
			{
				int Nr = 1,
                    allowedDiff = 5;
				for (;Nr < data.Length;Nr++)
				{
					if (data[Nr] == null)
					{
						return 0;
					}
					if ((Math.Abs(data[0].RhythmStart - data[Nr].RhythmStart) > allowedDiff)
					||	(Math.Abs(data[0].RhythmEnd - data[Nr].RhythmEnd) > allowedDiff))
					{
						break;
					}
				}
				return Nr;
			}
			return 0;
		}
		/// <summary>
		/// Function to sort signal array on lead type.
		/// </summary>
		/// <param name="data">signal array</param>
		public static void SortOnType(Signal[] data)
		{
			if (data != null)
			{
				SortOnType(data, 0, data.Length-1);
			}
		}
		/// <summary>
		/// Function to sort signal array on lead type.
		/// </summary>
		/// <param name="data">signal array</param>
		/// <param name="first"></param>
		/// <param name="last"></param>
		public static void SortOnType(Signal[] data, int first, int last)
		{
			if ((data != null)
			&&	(first < last))
			{
				int p = _PartitionOnType(data, first, last);

				SortOnType(data, first, p - 1);
				SortOnType(data, p + 1, last);
			}
		}
		private static int _PartitionOnType(Signal[] data, int first, int last)
		{
			Signal pivot, t;
			int i, m, p;

			m = (first + last) / 2;

			pivot = data[m];
			data[m] = data[first];
			data[first] = pivot;

			p = first;

			for (i=first+1;i <= last;i++)
			{
				if ((data == null)
				||	(data[i].Type < pivot.Type))
				{
					t = data[++p];
					data[p] = data[i];
					data[i] = t;
				}
			}

			t = data[first];
			data[first] = data[p];
			data[p] = t;

			return p;
		}
	}
}
