/***************************************************************************
Copyright 2012, van Ettinger Information Technology, Lopik, The Netherlands
Copyright 2004,2010, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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

namespace ECGConversion.ECGGlobalMeasurements
{
	/// <summary>
	/// Class containing one wave measurement (SCP an UNIPRO defined).
	/// </summary>
	public class GlobalMeasurement
	{
		public static ushort NoValue = 29999;
		public static short NoAxisValue = 29999;
		public ushort Ponset = NoValue;
		public ushort Poffset = NoValue;
		public ushort QRSonset = NoValue;
		public ushort QRSoffset = NoValue;
		public ushort Toffset = NoValue;
		public short Paxis = NoAxisValue;
		public short QRSaxis = NoAxisValue;
		public short Taxis = NoAxisValue;

		public ushort Pdur
		{
			get {return (ushort) ((Poffset != NoValue) && (Ponset != NoValue) && (Ponset < Poffset) ? (Poffset - Ponset) : NoValue);}
			set
			{
				if ((value > 0)
				&&	(value != NoValue))
				{
                    if (Ponset == NoValue)
						Ponset = 100;

					Poffset = (ushort) (value + Ponset);
				}
				else
				{
					Ponset = NoValue;
					Poffset = NoValue;
				}
			}
		}
		public ushort PRint
		{
			get {return (ushort) ((QRSonset != NoValue) && (Ponset != NoValue) ? (QRSonset - Ponset) : NoValue);}
			set
			{
				if ((value > 0)
				&&	(value != NoValue))
				{
					if (Ponset == NoValue)
					{
						Ponset = 100;
						Poffset = NoValue;
					}

					QRSonset = (ushort) (value + Ponset);
				}
			}
		}
		public ushort QRSdur
		{
			get {return (ushort) ((QRSoffset != NoValue) && (QRSonset != NoValue) ? (QRSoffset - QRSonset) : NoValue);}
			set
			{
				if ((value != NoValue)
				&&	(value != 0))
				{
					if ((QRSonset == NoValue)
					||	(QRSonset == 0))
					{
						Ponset = NoValue;
						Poffset = NoValue;

						QRSonset = 400;
					}

					QRSoffset = (ushort) (value + QRSonset);
				}
			}
		}
		public ushort Tdur
		{
			get {return (ushort) ((Toffset != NoValue) && (Ponset != NoValue) ? (Toffset - QRSoffset) : NoValue);}
/*			set
			{
				if ((value != NoValue)
				&&	(value != 0))
				{
					if ((Toffset != NoValue)
					&&	(Toffset != 0))
					{
						Tonset = Toffset - value;
					}
					else
					{
						throw new Exception("You should set QTdur(ation) or Toffset before setting Tdur(ation)! ");
					}
				}
				else
				{
					Tonset = NoValue;
					Toffset = NoValue;
				}
			}*/
		}
		public ushort QTdur
		{
			get {return (ushort) ((Toffset != NoValue) && (QRSonset != NoValue) ? (Toffset - QRSonset) : NoValue);}
			set
			{
				if ((value != NoValue)
				&&	(value != 0)
				&&	(QRSonset != NoValue)
				&&	(QRSonset != 0))
				{
					Toffset = (ushort) (QRSonset + value);
				}
				else
				{
					Toffset = NoValue;
				}
			}
		}
		public ushort calcQTc(ushort AvgRR, ushort HR, GlobalMeasurements.QTcCalcType calcType)
		{
			if ((AvgRR == 0)
			||	(AvgRR == NoValue)
			||	(QTdur == NoValue))
				return NoValue;

			ushort ret = NoValue;

			switch (calcType)
			{
				case GlobalMeasurements.QTcCalcType.Bazett:
					ret = (ushort) (QTdur / Math.Sqrt(AvgRR * 0.001));
					break;
				case GlobalMeasurements.QTcCalcType.Fridericia:
					ret = (ushort) (QTdur / Math.Pow(AvgRR * 0.001, 1.0/3.0));
					break;
				case GlobalMeasurements.QTcCalcType.Framingham:
					ret = (ushort) (QTdur + (154 * (1 - (AvgRR * 0.001))));
					break;
				case GlobalMeasurements.QTcCalcType.Hodges:
					ret = (ushort) (QTdur + (1.75 * (HR - 60)));
					break;
				default:break;
			}

			return ret;
		}
	}
}
