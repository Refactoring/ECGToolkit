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

namespace ECGConversion.ECGGlobalMeasurements
{
	/// <summary>
	/// Class containing measurements of ECG.
	/// </summary>
	public class GlobalMeasurements
	{
		public enum QTcCalcType
		{
			Unknown = -1,
			Bazett = 0,
			Hodges = 1,
			Fridericia = 2,
			Framingham = 3,
			
		}

		private ushort _QTc = GlobalMeasurement.NoValue;
		private ushort _VentRate = GlobalMeasurement.NoValue;

		public ushort AvgRR = GlobalMeasurement.NoValue;
		public ushort AvgPP = GlobalMeasurement.NoValue;
		public GlobalMeasurement[] measurment = null;
		public Spike[] spike = null;

		public ushort VentRate
		{
			get
			{
				if (_VentRate < GlobalMeasurement.NoValue)
					return _VentRate;

				return (ushort) ((AvgRR == 0) || (AvgRR == GlobalMeasurement.NoValue) ? 0 : (60000 / AvgRR));
			}
			set
			{
				_VentRate = value < GlobalMeasurement.NoValue ? value : GlobalMeasurement.NoValue;
			}
		}
		public ushort Pdur
		{
			get
			{
				if ((measurment != null)
				&&	(measurment.Length > 0)
				&&	(measurment[0] != null))
					return measurment[0].Pdur;

				return GlobalMeasurement.NoValue;
			}
		}
		public ushort PRint
		{
			get
			{
				if ((measurment != null)
				&&	(measurment.Length > 0)
				&&	(measurment[0] != null))
					return measurment[0].PRint;

				return GlobalMeasurement.NoValue;
			}
		}
		public ushort QRSdur
		{
			get
			{
				if ((measurment != null)
				&&	(measurment.Length > 0)
				&&	(measurment[0] != null))
					return measurment[0].QRSdur;

				return GlobalMeasurement.NoValue;
			}
		}
		public ushort QTdur
		{
			get
			{
				if ((measurment != null)
				&&	(measurment.Length > 0)
				&&	(measurment[0] != null))
					return measurment[0].QTdur;

				return GlobalMeasurement.NoValue;
			}
		}
		public ushort QTc
		{
			get
			{
				if (_QTc < GlobalMeasurement.NoValue)
					return _QTc;

				if ((measurment != null)
				&&	(measurment.Length > 0)
				&&	(measurment[0] != null)
				&&	(AvgRR != GlobalMeasurement.NoValue))
					return measurment[0].calcQTc(AvgRR, VentRate, QTcType);

				return GlobalMeasurement.NoValue;
			}
			set
			{
				_QTc = value;
			}
		}
		public QTcCalcType QTcType
		{
			get
			{
				if (_QTc >= GlobalMeasurement.NoValue)
					return (QTcCalcType) _QTc - GlobalMeasurement.NoValue;

				return QTcCalcType.Unknown;
			}
			set
			{
				if (value != QTcCalcType.Unknown)
					_QTc = (ushort) (GlobalMeasurement.NoValue + value);
				else if (_QTc >= GlobalMeasurement.NoValue)
					_QTc = 0;
			}
		}
	}
}
