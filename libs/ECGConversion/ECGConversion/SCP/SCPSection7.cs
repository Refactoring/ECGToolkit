/***************************************************************************
Copyright 2013, van Ettinger Information Technology, Lopik, The Netherlands
Copyright 2004,2009, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
using System.Runtime.InteropServices;
using Communication.IO.Tools;
using ECGConversion.ECGGlobalMeasurements;

namespace ECGConversion.SCP
{
	/// <summary>
	/// Class contains section 7 (contains the global measurements section).
	/// </summary>
	public class SCPSection7 : SCPSection, IGlobalMeasurement
	{
		// Defined in SCP.
		private static ushort _SectionID = 7;

		// Special variables for this section.
		private bool _AfterSpikes = false;
		private bool _AfterSpikesInfo = false;
		private bool _AfterQRSType = false;

		// Part of the stored Data Structure.
		private byte _NrRefTypeQRS = 0;
		private byte _NrSpikes = 0;
		private ushort _AvgRRInterval = 0;
		private ushort _AvgPPInterval = 0;
		private SCPMeasurement[] _Measurements = null;
		private SCPSpike[] _Spikes = null;
		private SCPSpikeInfo[] _SpikesInfo = null;
		private ushort _NrQRS = 0;
		private byte[] _QRSType = null;
		private SCPExtraMeasurements _ExtraMeasurements = new SCPExtraMeasurements();

		// Manufactor specific block (Not implemented, because UNIPRO doesn't store this kind of info).
		private byte[] _Rest = null;

		protected override int _Read(byte[] buffer, int offset)
		{
			_AfterSpikes = true;
			_AfterSpikesInfo = true;
			_AfterQRSType = true;
			int end = offset - Size + Length;
			int frontsize = Marshal.SizeOf(_NrRefTypeQRS) + Marshal.SizeOf(_NrSpikes) + Marshal.SizeOf(_AvgRRInterval) + Marshal.SizeOf(_AvgPPInterval);
			if ((offset + frontsize) > end)
			{
				return 0x1;
			}

			_NrRefTypeQRS = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_NrRefTypeQRS), true);
			offset += Marshal.SizeOf(_NrRefTypeQRS);
			_NrSpikes = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_NrSpikes), true);
			offset += Marshal.SizeOf(_NrSpikes);
			_AvgRRInterval = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_AvgRRInterval), true);
			offset += Marshal.SizeOf(_AvgRRInterval);
			_AvgPPInterval = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_AvgPPInterval), true);
			offset += Marshal.SizeOf(_AvgPPInterval);

			if (_NrRefTypeQRS > 0)
			{
				if (((offset + (_NrRefTypeQRS * SCPMeasurement.Size)) > end)
				&&	(_NrSpikes == 0))
				{
					_NrRefTypeQRS = (byte) ((end - offset) / SCPMeasurement.Size);
				}
				
				if ((offset + (_NrRefTypeQRS * SCPMeasurement.Size)) > end)
				{
					return 0x2;
				}
				else
				{
					_Measurements = new SCPMeasurement[_NrRefTypeQRS];
					for (int loper=0;loper < _NrRefTypeQRS;loper++)
					{
						_Measurements[loper] = new SCPMeasurement();
						_Measurements[loper].Read(buffer, offset);
						offset += SCPMeasurement.Size;
					}
				}
			}
			if (_NrSpikes > 0)
			{
				if ((offset + (_NrSpikes * SCPSpike.Size)) > end)
				{
					return 0x4;
				}
				_Spikes = new SCPSpike[_NrSpikes];
				for (int loper=0;loper < _NrSpikes;loper++)
				{
					_Spikes[loper] = new SCPSpike();
					_Spikes[loper].Read(buffer, offset);
					offset += SCPSpike.Size;
				}
				if (offset + (_NrSpikes * SCPSpikeInfo.Size) > end)
				{
					_AfterSpikes = false;
					_AfterSpikesInfo = false;
					_AfterQRSType = false;
					return 0x0;
				}
				_SpikesInfo = new SCPSpikeInfo[_NrSpikes];
				for (int loper=0;loper < _NrSpikes;loper++)
				{
					_SpikesInfo[loper] = new SCPSpikeInfo();
					_SpikesInfo[loper].Read(buffer, offset);
					offset += SCPSpikeInfo.Size;
				}
			}
			
			if ((offset + Marshal.SizeOf(_NrQRS)) > end)
			{
				_AfterSpikesInfo = false;
				_AfterQRSType = false;
				return 0x0;
			}

			_NrQRS = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_NrQRS), true);
			offset += Marshal.SizeOf(_NrQRS);

			if (_NrQRS > 0)
			{
				if ((offset + (_NrQRS * Marshal.SizeOf(typeof(byte)))) > end)
				{
					return 0x10;
				}
				_QRSType = new byte[_NrQRS];
				for (int loper=0;loper < _NrQRS;loper++)
				{
					_QRSType[loper] = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_QRSType[loper]), true);
					offset += Marshal.SizeOf(_QRSType[loper]);
				}
			}

			int err = _ExtraMeasurements.Read(buffer, offset);
			offset += _ExtraMeasurements.getLength();
			
			// added an extra byte in check to prevent some errors
			if ((err != 0)
			||  (offset > end+1))
			{
				_AfterQRSType = false;
				_ExtraMeasurements.Empty();
				return 0x20;
			}

			if ((end - offset) > 0)
			{
				_Rest = new byte[end - offset];
				offset += BytesTool.copy(_Rest, 0, buffer, offset, _Rest.Length);
			}

			return 0x0;
		}
		protected override int _Write(byte[] buffer, int offset)
		{
			BytesTool.writeBytes(_NrRefTypeQRS, buffer, offset, Marshal.SizeOf(_NrRefTypeQRS), true);
			offset += Marshal.SizeOf(_NrRefTypeQRS);
			BytesTool.writeBytes(_NrSpikes, buffer, offset, Marshal.SizeOf(_NrSpikes), true);
			offset += Marshal.SizeOf(_NrSpikes);
			BytesTool.writeBytes(_AvgRRInterval, buffer, offset, Marshal.SizeOf(_AvgRRInterval), true);
			offset += Marshal.SizeOf(_AvgRRInterval);
			BytesTool.writeBytes(_AvgPPInterval, buffer, offset, Marshal.SizeOf(_AvgPPInterval), true);
			offset += Marshal.SizeOf(_AvgPPInterval);

			if (_NrRefTypeQRS > 0)
			{
				for (int loper=0;loper < _NrRefTypeQRS;loper++)
				{
					int err = _Measurements[loper].Write(buffer, offset);
					if (err != 0)
					{
						return 0x1;
					}
					offset += SCPMeasurement.Size;
				}
			}
			if (_NrSpikes > 0)
			{
				for (int loper=0;loper < _NrSpikes;loper++)
				{
					int err = _Spikes[loper].Write(buffer, offset);
					if (err != 0)
					{
						return 0x2;
					}
					offset += SCPSpike.Size;
				}
				if (!_AfterSpikes)
				{
					return 0;
				}
				for (int loper=0;loper < _NrSpikes;loper++)
				{
					int err = _SpikesInfo[loper].Write(buffer, offset);
					if (err != 0)
					{
						return 0x4;
					}
					offset += SCPSpikeInfo.Size;
				}
			}

			if (!_AfterSpikesInfo)
			{
				return 0;
			}

			BytesTool.writeBytes(_NrQRS, buffer, offset, Marshal.SizeOf(_NrQRS), true);
			offset += Marshal.SizeOf(_NrQRS);

			if ((_NrQRS > 0)
			&&  (_QRSType != null))
			{
				for (int loper=0;loper < _NrQRS;loper++)
				{
					BytesTool.writeBytes(_QRSType[loper], buffer, offset, Marshal.SizeOf(_QRSType[loper]), true);
					offset += Marshal.SizeOf(_QRSType[loper]);
				}
			}

			if (!_AfterQRSType)
			{
				return 0;
			}

			if (_ExtraMeasurements != null)
			{
				int err = _ExtraMeasurements.Write(buffer, offset);
				if (err != 0)
				{
					return 0x8;
				}
				offset += _ExtraMeasurements.getLength();
			}

			if ((_Rest != null)
			&&  ((offset + _Rest.Length) < buffer.Length))
			{
				offset += BytesTool.copy(_Rest, 0, buffer, offset, _Rest.Length);
			}
			return 0x0;
		}
		protected override void _Empty()
		{
			_NrRefTypeQRS = 0;
			_NrSpikes = 0;
			_AvgRRInterval = 0;
			_AvgPPInterval = 0;
			_Measurements = null;
			_Spikes = null;
			_SpikesInfo = null;
			_AfterSpikes = false;
			_NrQRS = 0;
			_AfterQRSType = false;
			_QRSType = null;
			_ExtraMeasurements.Empty();
			_Rest = null;
		}
		protected override int _getLength()
		{
			if (Works())
			{
				int sum = Marshal.SizeOf(_NrRefTypeQRS) + Marshal.SizeOf(_NrSpikes) + Marshal.SizeOf(_AvgPPInterval) + Marshal.SizeOf(_AvgRRInterval);
				sum += _NrRefTypeQRS * SCPMeasurement.Size;
				sum += (_NrSpikes * (SCPSpike.Size + SCPSpikeInfo.Size));
				if (_AfterSpikes)
				{
					sum += Marshal.SizeOf(_NrQRS) + (_NrQRS * Marshal.SizeOf(typeof(byte)));
					if (_AfterQRSType)
					{
						sum += _ExtraMeasurements.getLength();
						if (_Rest != null)
						{
							sum += _Rest.Length;
						}
					}
				}
				return sum;
			}
			return 0;
		}
		public override ushort getSectionID()
		{
			return _SectionID;
		}
		public override bool Works()
		{
			if ((_NrRefTypeQRS == 0)
			||	((_Measurements != null)
			&&	 (_Measurements.Length == _NrRefTypeQRS)))
			{
				if ((_NrSpikes == 0)
				||  ((_Spikes != null)
				&&	 (_Spikes.Length == _NrSpikes)))
				{
					if (!_AfterSpikes)
					{
						return ((_NrRefTypeQRS != 0) || (_NrSpikes != 0));
					}

					if ((_SpikesInfo != null)
					&&  (_SpikesInfo.Length == _NrSpikes))
					{
						if (!_AfterSpikesInfo)
						{
							return ((_NrRefTypeQRS != 0) || (_NrSpikes != 0));
						}

						if ((_NrQRS == 0)
						||  ((_QRSType != null)
						&&	 (_QRSType.Length == _NrQRS)))
						{
							return (!_AfterQRSType
								||	(_ExtraMeasurements != null));
						}
					}
				}
				return true;
			}
			return false;
		}
		public int getGlobalMeasurements(out GlobalMeasurements mes)
		{
			mes = null;
			if (Works())
			{
				mes = new GlobalMeasurements();
				mes.AvgRR = _AvgRRInterval;
				mes.AvgPP = _AvgPPInterval;
				mes.measurment = new GlobalMeasurement[_NrRefTypeQRS];
				for (int loper=0;loper < _NrRefTypeQRS;loper++)
				{
					mes.measurment[loper] = new GlobalMeasurement();
					if (_Measurements[loper] != null)
					{
						mes.measurment[loper].Ponset = _Measurements[loper].Ponset;
						mes.measurment[loper].Poffset = _Measurements[loper].Poffset;
						mes.measurment[loper].QRSonset = _Measurements[loper].QRSonset;
						mes.measurment[loper].QRSoffset = _Measurements[loper].QRSoffset;
						mes.measurment[loper].Toffset = _Measurements[loper].Toffset;
						mes.measurment[loper].Paxis = _Measurements[loper].Paxis;
						mes.measurment[loper].QRSaxis= _Measurements[loper].QRSaxis;
						mes.measurment[loper].Taxis = _Measurements[loper].Taxis;
					}
				}

				if (_NrSpikes > 0)
				{
					mes.spike = new Spike[_NrSpikes];
					for (int loper=0;loper < _NrSpikes;loper++)
					{
						mes.spike[loper] = new Spike();
						mes.spike[loper].Time = _Spikes[loper].Time;
						mes.spike[loper].Amplitude = _Spikes[loper].Amplitude;
					}
				}

				if (_AfterQRSType)
				{
					mes.VentRate = _ExtraMeasurements.VentRate;

					switch (_ExtraMeasurements.FormulaType)
					{
						case 1: case 2:
							mes.QTcType = (GlobalMeasurements.QTcCalcType) (_ExtraMeasurements.FormulaType - 1);
							break;
						default:
							mes.QTc = _ExtraMeasurements.QTc;
							break;
					}
				}

				return 0;
			}
			return 1;
		}
		public int setGlobalMeasurements(GlobalMeasurements mes)
		{
			if ((mes != null)
			&&  (mes.measurment != null))
			{
				Empty();

				_AvgRRInterval = mes.AvgRR;
				_AvgPPInterval = mes.AvgPP;

				_NrRefTypeQRS = (byte) mes.measurment.Length;
				_Measurements = new SCPMeasurement[_NrRefTypeQRS];
				for (int loper=0;loper < _NrRefTypeQRS;loper++)
				{
					_Measurements[loper] = new SCPMeasurement();
					if (mes.measurment[loper] != null)
					{
						_Measurements[loper].Ponset = mes.measurment[loper].Ponset;
						_Measurements[loper].Poffset = mes.measurment[loper].Poffset;
						_Measurements[loper].QRSonset = mes.measurment[loper].QRSonset;
						_Measurements[loper].QRSoffset = mes.measurment[loper].QRSoffset;
						_Measurements[loper].Toffset = mes.measurment[loper].Toffset;
						_Measurements[loper].Paxis = mes.measurment[loper].Paxis;
						_Measurements[loper].QRSaxis = mes.measurment[loper].QRSaxis;
						_Measurements[loper].Taxis = mes.measurment[loper].Taxis;
					}
				}

				_NrSpikes = 0;

				if (mes.spike != null)
				{
					_NrSpikes = (byte) mes.spike.Length;
					_Spikes = new SCPSpike[_NrSpikes];
					_SpikesInfo = new SCPSpikeInfo[_NrSpikes];
					for (int loper=0;loper < _NrSpikes;loper++)
					{
						_Spikes[loper] = new SCPSpike();
						_SpikesInfo[loper] = new SCPSpikeInfo();
						if (mes.spike[loper] != null)
						{
							_Spikes[loper].Time = mes.spike[loper].Time;
							_Spikes[loper].Amplitude = mes.spike[loper].Amplitude;
						}
					}
				}
				
				_AfterSpikes = true;

				_AfterSpikesInfo = true;

				_NrQRS = 0;
				_QRSType = null;

				_AfterQRSType = true;

				_ExtraMeasurements = new SCPExtraMeasurements();
				_ExtraMeasurements.VentRate = mes.VentRate;
				_ExtraMeasurements.QTc = mes.QTc;

				byte temp = (byte) (mes.QTcType + 1);

				if (temp > 2)
					temp = 0;

				_ExtraMeasurements.FormulaType = temp;

				return 0;
			}
			return 1;
		}
		/// <summary>
		/// Class containing measurements for SCP.
		/// </summary>
		public class SCPMeasurement
		{
			public const int Size = 16;

			public ushort Ponset = GlobalMeasurement.NoValue;
			public ushort Poffset = GlobalMeasurement.NoValue;
			public ushort QRSonset = GlobalMeasurement.NoValue;
			public ushort QRSoffset = GlobalMeasurement.NoValue;
			public ushort Toffset = GlobalMeasurement.NoValue;
			public short Paxis = GlobalMeasurement.NoAxisValue;
			public short QRSaxis = GlobalMeasurement.NoAxisValue;
			public short Taxis = GlobalMeasurement.NoAxisValue;
			/// <summary>
			/// Function to read measurements.
			/// </summary>
			/// <param name="buffer">byte array to read from</param>
			/// <param name="offset">position to start reading</param>
			/// <returns>0 on success</returns>
			public int Read(byte[] buffer, int offset)
			{
				if ((offset + Size) > buffer.Length)
				{
					return 0x1;
				}

				Ponset = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Ponset), true);
				offset += Marshal.SizeOf(Ponset);
				Poffset = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Poffset), true);
				offset += Marshal.SizeOf(Poffset);
				QRSonset = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(QRSonset), true);
				offset += Marshal.SizeOf(QRSonset);
				QRSoffset = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(QRSoffset), true);
				offset += Marshal.SizeOf(QRSoffset);
				Toffset = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Toffset), true);
				offset += Marshal.SizeOf(Toffset);
				Paxis= (short) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Paxis), true);
				offset += Marshal.SizeOf(Paxis);
				QRSaxis = (short) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(QRSaxis), true);
				offset += Marshal.SizeOf(QRSaxis);
				Taxis = (short) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Taxis), true);
				offset += Marshal.SizeOf(Taxis);

				return 0x0;
			}
			/// <summary>
			/// Function to write measurements.
			/// </summary>
			/// <param name="buffer">byte array to write into</param>
			/// <param name="offset">position to start writing</param>
			/// <returns>0 on success</returns>
			public int Write(byte[] buffer, int offset)
			{
				if ((offset + Size) > buffer.Length)
				{
					return 0x1;
				}

				BytesTool.writeBytes(Ponset, buffer, offset, Marshal.SizeOf(Ponset), true);
				offset += Marshal.SizeOf(Ponset);
				BytesTool.writeBytes(Poffset, buffer, offset, Marshal.SizeOf(Poffset), true);
				offset += Marshal.SizeOf(Poffset);
				BytesTool.writeBytes(QRSonset, buffer, offset, Marshal.SizeOf(QRSonset), true);
				offset += Marshal.SizeOf(QRSonset);
				BytesTool.writeBytes(QRSoffset, buffer, offset, Marshal.SizeOf(QRSoffset), true);
				offset += Marshal.SizeOf(QRSoffset);
				BytesTool.writeBytes(Toffset, buffer, offset, Marshal.SizeOf(Toffset), true);
				offset += Marshal.SizeOf(Toffset);
				BytesTool.writeBytes(Paxis, buffer, offset, Marshal.SizeOf(Paxis), true);
				offset += Marshal.SizeOf(Paxis);
				BytesTool.writeBytes(QRSaxis, buffer, offset, Marshal.SizeOf(QRSaxis), true);
				offset += Marshal.SizeOf(QRSaxis);
				BytesTool.writeBytes(Taxis, buffer, offset, Marshal.SizeOf(Taxis), true);
				offset += Marshal.SizeOf(Taxis);

				return 0x0;
			}
		}
		/// <summary>
		/// Class containing SCP spikes.
		/// </summary>
		public class SCPSpike
		{
			public const int Size = 4;

			public ushort Time = GlobalMeasurement.NoValue;
			public short Amplitude = GlobalMeasurement.NoAxisValue;
			/// <summary>
			/// Function to read a SCP spike.
			/// </summary>
			/// <param name="buffer">byte array to read from</param>
			/// <param name="offset">position to start reading</param>
			/// <returns>0 on success</returns>
			public int Read(byte[] buffer, int offset)
			{
				if ((offset + Size) > buffer.Length)
				{
					return 0x1;
				}

				Time = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Time), true);
				offset += Marshal.SizeOf(Time);
				Amplitude = (short) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Amplitude), true);
				offset += Marshal.SizeOf(Amplitude);

				return 0x0;
			}
			/// <summary>
			/// Function to write SCP spike.
			/// </summary>
			/// <param name="buffer">byte array to write into</param>
			/// <param name="offset">position to start writing</param>
			/// <returns>0 on success</returns>
			public int Write(byte[] buffer, int offset)
			{
				if ((offset + Size) > buffer.Length)
				{
					return 0x1;
				}

				BytesTool.writeBytes(Time, buffer, offset, Marshal.SizeOf(Time), true);
				offset += Marshal.SizeOf(Time);
				BytesTool.writeBytes(Amplitude, buffer, offset, Marshal.SizeOf(Amplitude), true);
				offset += Marshal.SizeOf(Amplitude);

				return 0x0;
			}
		}
		/// <summary>
		/// Class containing SCP Spike info.
		/// </summary>
		public class SCPSpikeInfo
		{
			public const int Size = 6;

			public byte Type = 255;
			public byte Source = 0;
			public ushort TriggerIndex = GlobalMeasurement.NoValue;
			public ushort PulseWidth = GlobalMeasurement.NoValue;
			/// <summary>
			/// Function to read SCP spike info.
			/// </summary>
			/// <param name="buffer">byte array to read from</param>
			/// <param name="offset">position to start reading</param>
			/// <returns>0 on success</returns>
			public int Read(byte[] buffer, int offset)
			{
				if ((offset + Size) > buffer.Length)
				{
					return 0x1;
				}

				Type = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Type), true);
				offset += Marshal.SizeOf(Type);
				Source = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Source), true);
				offset += Marshal.SizeOf(Source);
				TriggerIndex = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(TriggerIndex), true);
				offset += Marshal.SizeOf(TriggerIndex);
				PulseWidth = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(PulseWidth), true);
				offset += Marshal.SizeOf(PulseWidth);

				return 0x0;
			}
			/// <summary>
			/// Function to write SCP Spike info.
			/// </summary>
			/// <param name="buffer">byte array to write into</param>
			/// <param name="offset">position to start writing</param>
			/// <returns>0 on success</returns>
			public int Write(byte[] buffer, int offset)
			{
				if ((offset + Size) > buffer.Length)
				{
					return 0x1;
				}

				BytesTool.writeBytes(Type, buffer, offset, Marshal.SizeOf(Type), true);
				offset += Marshal.SizeOf(Type);
				BytesTool.writeBytes(Source, buffer, offset, Marshal.SizeOf(Source), true);
				offset += Marshal.SizeOf(Source);
				BytesTool.writeBytes(TriggerIndex, buffer, offset, Marshal.SizeOf(TriggerIndex), true);
				offset += Marshal.SizeOf(TriggerIndex);
				BytesTool.writeBytes(PulseWidth, buffer, offset, Marshal.SizeOf(PulseWidth), true);
				offset += Marshal.SizeOf(PulseWidth);

				return 0x0;
			}
		}
		/// <summary>
		/// Class containing SCP extra measurements. (no compatability with UNIPRO at all)
		/// </summary>
		public class SCPExtraMeasurements
		{
			public ushort VentRate = GlobalMeasurement.NoValue;
			public ushort AtrialRate = GlobalMeasurement.NoValue;
			public ushort QTc = GlobalMeasurement.NoValue;
			public byte FormulaType = 0;
			public ushort TaggedFieldSize = 0;
			// Tagged Field are not implemented.
			public byte[] TaggedFields = null;
			/// <summary>
			/// Function to read SCP extra measurements.
			/// </summary>
			/// <param name="buffer">byte array to read from</param>
			/// <param name="offset">position to start reading</param>
			/// <returns>0 on success</returns>
			public int Read(byte[] buffer, int offset)
			{
				Empty();
				int frontsize = Marshal.SizeOf(VentRate) + Marshal.SizeOf(AtrialRate) + Marshal.SizeOf(QTc) + Marshal.SizeOf(FormulaType) + Marshal.SizeOf(TaggedFieldSize);
				if ((offset + frontsize) > buffer.Length)
				{
					return 0x1;
				}

				VentRate = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(VentRate), true);
				offset += Marshal.SizeOf(VentRate);
				AtrialRate = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(AtrialRate), true);
				offset += Marshal.SizeOf(AtrialRate);
				QTc = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(QTc), true);
				offset += Marshal.SizeOf(QTc);
				FormulaType = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(FormulaType), true);
				offset += Marshal.SizeOf(FormulaType);
				TaggedFieldSize = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(TaggedFieldSize), true);
				offset += Marshal.SizeOf(TaggedFieldSize);

				if (TaggedFieldSize > 0)
				{
					if ((offset + TaggedFieldSize) > buffer.Length)
					{
						return 0x2;
					}

					TaggedFields = new byte[TaggedFieldSize];
					offset += BytesTool.copy(TaggedFields, 0, buffer, offset, TaggedFieldSize);
				}

				return 0x0;
			}
			/// <summary>
			/// Function to write SCP extra measurements.
			/// </summary>
			/// <param name="buffer">byte array to write into</param>
			/// <param name="offset">position to start writing</param>
			/// <returns>0 on success</returns>
			public int Write(byte[] buffer, int offset)
			{
				BytesTool.writeBytes(VentRate, buffer, offset, Marshal.SizeOf(VentRate), true);
				offset += Marshal.SizeOf(VentRate);
				BytesTool.writeBytes(AtrialRate, buffer, offset, Marshal.SizeOf(AtrialRate), true);
				offset += Marshal.SizeOf(AtrialRate);
				BytesTool.writeBytes(QTc, buffer, offset, Marshal.SizeOf(QTc), true);
				offset += Marshal.SizeOf(QTc);
				BytesTool.writeBytes(FormulaType, buffer, offset, Marshal.SizeOf(FormulaType), true);
				offset += Marshal.SizeOf(FormulaType);
				BytesTool.writeBytes(TaggedFieldSize, buffer, offset, Marshal.SizeOf(TaggedFieldSize), true);
				offset += Marshal.SizeOf(TaggedFieldSize);

				if (TaggedFields != null)
				{
					offset += BytesTool.copy(buffer, offset, TaggedFields, 0, TaggedFieldSize);
				}

				return 0x0;
			}
			/// <summary>
			/// Function to empty this extra measurements.
			/// </summary>
			public void Empty()
			{
				VentRate = 0;
				AtrialRate = 0;
				QTc = 0;
				FormulaType = 0;
				TaggedFieldSize = 0;
				TaggedFields = null;
			}
			/// <summary>
			/// Function to get length of extra measurements.
			/// </summary>
			/// <returns>length of extra measurements</returns>
			public int getLength()
			{
				if (Works())
				{
					int sum = Marshal.SizeOf(VentRate) + Marshal.SizeOf(AtrialRate);
					sum += Marshal.SizeOf(QTc) + Marshal.SizeOf(FormulaType) + Marshal.SizeOf(TaggedFieldSize);
					sum += TaggedFieldSize;
					return ((sum % 2) == 0 ? sum : sum + 1);
				}
				return 0;
			}
			/// <summary>
			/// Function to check if extra measurements works
			/// </summary>
			/// <returns>if writeable is true</returns>
			public bool Works()
			{
				if (TaggedFieldSize == 0)
				{
					return true;
				}
				else if ((TaggedFields != null)
					&&   (TaggedFields.Length >= TaggedFieldSize))
				{
					return true;
				}
				return false;
			}
		}
	}
}
