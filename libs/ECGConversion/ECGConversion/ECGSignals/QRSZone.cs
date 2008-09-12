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
	/// Class containing a QRS zone.
	/// </summary>
	public class QRSZone
	{
		public ushort Type = ushort.MaxValue;
		public int Start = 0;
		public int Fiducial = 0;
		public int End = 0;
		public QRSZone()
		{}
		public QRSZone(ushort type, int start, int fiducial, int end)
		{
			Type = type;
			Start = start;
			Fiducial = fiducial;
			End = end;
		}
		public QRSZone Clone()
		{
			return new QRSZone(this.Type, this.Start, this.Fiducial, this.End);
		}
	}
}
