/***************************************************************************
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
using System.Runtime.InteropServices;
using Communication.IO.Tools;
using ECGConversion.ECGSignals;
using ECGConversion.ECGGlobalMeasurements;

namespace ECGConversion.SCP
{
	/// <summary>
	/// Class contains section 4 (QRS locations).
	/// </summary>
	public class SCPSection4 : SCPSection, ISignal
	{
		// Defined in SCP.
		private static ushort _SectionID = 4;

		// Part of the stored Data Structure.
		private ushort _MedianDataLength = 0;
		private ushort _FirstFiducial = 0;
		private ushort _NrQRS = 0xffff;
		private SCPQRSSubtraction[] _Subtraction = null;
		private SCPQRSProtected[] _Protected = null;
		protected override int _Read(byte[] buffer, int offset)
		{
			int end = offset - Size + Length;
			if ((offset + Marshal.SizeOf(_MedianDataLength) + Marshal.SizeOf(_FirstFiducial) + Marshal.SizeOf(_NrQRS)) > end)
			{
				return 0x1;
			}
			_MedianDataLength = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_MedianDataLength), true);
			offset += Marshal.SizeOf(_MedianDataLength);
			_FirstFiducial = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_FirstFiducial), true);
			offset += Marshal.SizeOf(_FirstFiducial);
			_NrQRS = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_NrQRS), true);
			offset += Marshal.SizeOf(_NrQRS);
			if ((offset + (_NrQRS * Marshal.SizeOf(typeof(SCPQRSSubtraction)))) > end)
			{
				return 0x2;
			}
			_Subtraction = new SCPQRSSubtraction[_NrQRS];
			for (int loper=0;loper < _NrQRS;loper++)
			{
				_Subtraction[loper] = new SCPQRSSubtraction();
				int err = _Subtraction[loper].Read(buffer, offset);
				if (err != 0)
				{
					return err << (2 + loper);
				}
				offset += Marshal.SizeOf(_Subtraction[loper]);
			}

			if ((offset + (_NrQRS * Marshal.SizeOf(typeof(SCPQRSProtected)))) > end)
			{
				return 0x0;
			}

			_Protected = new SCPQRSProtected[_NrQRS];
			for (int loper=0;loper < _NrQRS;loper++)
			{
				_Protected[loper] = new SCPQRSProtected();
				int err = _Protected[loper].Read(buffer, offset);
				if (err != 0)
				{
					return err << (2 + loper);
				}
				offset += Marshal.SizeOf(_Protected[loper]);
			}
			return 0x0;
		}
		protected override int _Write(byte[] buffer, int offset)
		{
			BytesTool.writeBytes(_MedianDataLength, buffer, offset, Marshal.SizeOf(_MedianDataLength), true);
			offset += Marshal.SizeOf(_MedianDataLength);
			BytesTool.writeBytes(_FirstFiducial, buffer, offset, Marshal.SizeOf(_FirstFiducial), true);
			offset += Marshal.SizeOf(_FirstFiducial);
			BytesTool.writeBytes(_NrQRS, buffer, offset, Marshal.SizeOf(_NrQRS), true);
			offset += Marshal.SizeOf(_NrQRS);
			for (int loper=0;loper < _NrQRS;loper++)
			{
				int err = _Subtraction[loper].Write(buffer, offset);
				if (err != 0)
				{
					return err << loper;
				}
				offset += Marshal.SizeOf(_Subtraction[loper]);
			}

			if (_Protected != null)
			{
				for (int loper=0;loper < _NrQRS;loper++)
				{
					int err = _Protected[loper].Write(buffer, offset);
					if (err != 0)
					{
						return err << loper;
					}
					offset += Marshal.SizeOf(_Protected[loper]);
				}
			}
			return 0x0;
		}
		protected override void _Empty()
		{
			_MedianDataLength = 0;
			_FirstFiducial = 0;
			_NrQRS = 0xffff;
			_Subtraction = null;
			_Protected = null;
		}
		protected override int _getLength()
		{
			if (Works())
			{
				int sum = (Marshal.SizeOf(_MedianDataLength) + Marshal.SizeOf(_FirstFiducial) + Marshal.SizeOf(_NrQRS));
				sum += (_NrQRS * Marshal.SizeOf(typeof(SCPQRSSubtraction)));
				if (_Protected != null)
				{
					sum += (_NrQRS * Marshal.SizeOf(typeof(SCPQRSProtected)));
				}
				return ((sum % 2) == 0 ? sum : sum + 1);
			}
			return 0;
		}
		public override ushort getSectionID()
		{
			return _SectionID;
		}
		public override bool Works()
		{
			if ((_Subtraction != null)
			&&  (_Protected != null)
			&&  (_NrQRS == _Subtraction.Length)
			&&  (_NrQRS == _Protected.Length)
			||  (_NrQRS == 0))
			{
				if ((_Protected != null)
				&&  (_Protected.Length != _NrQRS))
				{
					return false;
				}

				for (int loper=0;loper < _NrQRS;loper++)
				{
					if ((_Subtraction[loper] == null)
					||  ((_Protected != null)
					&&	 (_Protected[loper] == null)))
					{
						return false;
					}
				}
				return true;
			}
			return false;
		}
		/// <summary>
		/// Function to get length of median.
		/// </summary>
		/// <returns>length of median data.</returns>
		public ushort getMedianLength()
		{
			if (Works())
			{
				return _MedianDataLength;
			}
			return 0;
		}
		/// <summary>
		/// Function to add median data to residual data.
		/// </summary>
		/// <remarks>both signals must have the same sample rate and AVM</remarks>
		/// <param name="definition">Data structure containing information about length of residual data.</param>
		/// <param name="residual">2D array containing residual data for each lead. On succes will contain rhythm data.</param>
		/// <param name="median">2D array containing median data for each lead.</param>
		/// <returns>error:
		/// 0x001) given data makes no sense.
		/// 0x002) Fault in Lead nr 0.
		/// 0x004) Fault in Lead nr 1.
		/// 0x008) Fault in Lead nr 2.
		/// 0x010) Fault in Lead nr 3.
		/// 0x020) Fault in Lead nr 4.
		/// 0x040) Fault in Lead nr 5.
		/// 0x080) Fault in Lead nr 6.
		/// 0x100) Fault in Lead nr 7.
		/// ...</returns>
		public int AddMedians(SCPSection3 definition, short[][] residual, short[][] median)
		{
			// Check if given data makes sense
			if (Works()
			&&  (definition != null)
			&&	(residual != null)
			&&  (median != null)
			&&  (definition.Works())
			&&  (median.Length == definition.getNrLeads())
			&&  (residual.Length == median.Length))
			{
				int err = 0;
				for (int qrsnr=0;qrsnr < _NrQRS;qrsnr++)
				{
					if ((_Subtraction[qrsnr].Type != 0))
						continue;

					for (int channel=0;channel < median.Length;channel++)
					{
						if ((residual[channel] == null)
						||  (median[channel] == null)
						||  (residual[channel].Length < definition.getLeadLength(channel)))
						{
							err |= (0x2 << channel);
							continue;
						}

						int loperResidual = _Subtraction[qrsnr].Start - definition.getLeadStart(channel);
						int loperMedian = (_FirstFiducial - _Subtraction[qrsnr].Fiducial + _Subtraction[qrsnr].Start - 1);
						int endResidual = _Subtraction[qrsnr].End - definition.getLeadStart(channel);

						if ((loperResidual >= 0)
						&&  (loperMedian >= 0))
						{
							while ((loperResidual <= endResidual)
								&& (loperMedian < median[channel].Length))
							{
								residual[channel][loperResidual++] += median[channel][loperMedian++];
							}
						}
					}
				}
				return err;
			}
			return -1;
		}
		/// <summary>
		/// Function to subtract median data to residual data.
		/// </summary>
		/// <remarks>both signals must have the same sample rate and AVM</remarks>
		/// <param name="definition">Data structure containing information about length of rhythm data.</param>
		/// <param name="rhythm">2D array containing rhythm data for each lead. On succes will contain residual data.</param>
		/// <param name="median">2D array containing median data for each lead.</param>
		/// <returns>error:
		/// 0x001) given data makes no sense.
		/// 0x002) Fault in Lead nr 0.
		/// 0x004) Fault in Lead nr 1.
		/// 0x008) Fault in Lead nr 2.
		/// 0x010) Fault in Lead nr 3.
		/// 0x020) Fault in Lead nr 4.
		/// 0x040) Fault in Lead nr 5.
		/// 0x080) Fault in Lead nr 6.
		/// 0x100) Fault in Lead nr 7.
		/// ...</returns>
		public int SubtractMedians(SCPSection3 definition, short[][] rhythm, short[][] median)
		{
			// Check if given data makes sense
			if (Works()
			&&  (definition != null)
			&	(rhythm != null)
			&&  (median != null)
			&&  (definition.Works())
			&&  (median.Length == definition.getNrLeads())
			&&  (rhythm.Length == median.Length))
			{
				int err = 0;
				for (int qrsnr=0;qrsnr < _NrQRS;qrsnr++)
				{
					if ((_Subtraction[qrsnr].Type != 0))
						continue;

					for (int channel=0;channel < median.Length;channel++)
					{
						if ((rhythm[channel] == null)
						||  (median[channel] == null)
						||  (rhythm[channel].Length < definition.getLeadLength(channel)))
						{
							err |= (0x2 << channel);
							continue;
						}

						int loperResidual = _Subtraction[qrsnr].Start - definition.getLeadStart(channel);
						int loperMedian = (_FirstFiducial - _Subtraction[qrsnr].Fiducial + _Subtraction[qrsnr].Start - 1);
						int endResidual = _Subtraction[qrsnr].End - definition.getLeadStart(channel);

						if ((loperResidual >= 0)
						&&  (loperMedian >= 0))
						{
							while ((loperResidual <= endResidual)
								&& (loperMedian < median[channel].Length))
							{
								rhythm[channel][loperResidual++] -= median[channel][loperMedian++];
							}
						}
					}
				}
				return err;
			}
			return -1;
		}
		/// <summary>
		/// Function to get nr of protected zones.
		/// </summary>
		/// <returns>nr of protected zones</returns>
		public int getNrProtectedZones()
		{
			return (_Protected != null ? _Protected.Length : 0);
		}
		/// <summary>
		/// Function to get start of protected zone.
		/// </summary>
		/// <param name="nr">nr of protected zone</param>
		/// <returns>start sample nr of protected zone</returns>
		public int getProtectedStart(int nr)
		{
			if ((_Protected != null)
			&&  (nr >= 0)
			&&	(nr < _Protected.Length))
			{
				return _Protected[nr].Start;
			}
			return -1;
		}
		/// <summary>
		/// Function to get end of protected zone.
		/// </summary>
		/// <param name="nr">nr of protected zone</param>
		/// <returns>end sample nr of protected zone</returns>
		public int getProtectedEnd(int nr)
		{
			if ((_Protected != null)
			&&  (nr >= 0)
			&&	(nr < _Protected.Length))
			{
				return _Protected[nr].End;
			}
			return -1;
		}
		/// <summary>
		/// Function to get length of protected zone.
		/// </summary>
		/// <param name="nr">nr of protected zone</param>
		/// <returns>length of protected zone in samples</returns>
		public int getProtectedLength(int nr)
		{
			if ((_Protected != null)
			&&  (nr >= 0)
			&&	(nr < _Protected.Length))
			{
				if (_Protected[nr] == null)
				{
					return 0;
				}

				int templen = _Protected[nr].End - _Protected[nr].Start;
				return (templen > 0 ? templen + 1 : 0);
			}
			return -1;
		}
		/// <summary>
		/// Function to set protected zones using global measurements.
		/// </summary>
		/// <param name="global">global measurments</param>
		/// <param name="medianFreq">Samples per Second of median</param>
		/// <param name="rate">Bimodal compression rate</param>
		/// <param name="minbegin">Begin of all leads in samples</param>
		/// <param name="maxend">End of all leads in samples</param>
		public void setProtected(GlobalMeasurements global, int medianFreq, int rate, int minbegin, int maxend)
		{
			if ((global != null)
			&&	(global.measurment != null)
			&&	(_Subtraction != null)
			&&	(_Protected != null)
			&&	(medianFreq != 0))
			{
				// If global measurements are per beat use them.
				if (global.measurment.Length == (_Protected.Length + 1))
				{
					for (int loper=0;loper < _Protected.Length;loper++)
					{
						_Protected[loper].Start = _Subtraction[loper].Fiducial + (short) ((global.measurment[loper + 1].QRSonset - (_FirstFiducial * 1000)) / medianFreq);
						_Protected[loper].End = _Subtraction[loper].Fiducial + (short) ((global.measurment[loper + 1].QRSoffset - (_FirstFiducial * 1000)) / medianFreq);

						// Make protected zones work properly
						_Protected[loper].Start -= ((_Protected[loper].Start % rate) == 0 ? rate : (_Protected[loper].Start % rate));
						_Protected[loper].Start++;
						_Protected[loper].End += (rate - (_Protected[loper].End % rate));

						// Keep it all between boundaries of ECG.
						if (_Protected[loper].Start < minbegin)
						{
							_Protected[loper].Start = minbegin;
						}
						if (_Protected[loper].End > maxend)
						{
							_Protected[loper].End = maxend;
						}
					}
				}
				else if (global.measurment.Length > 0)
				{
					for (int loper=0;loper < _Protected.Length;loper++)
					{
						if (_Subtraction[loper].Type == 0)
						{
							_Protected[loper].Start = _Subtraction[loper].Fiducial + (short) ((global.measurment[0].QRSonset * medianFreq) / 1000) - _FirstFiducial;
							_Protected[loper].End = _Subtraction[loper].Fiducial + (short) ((global.measurment[0].QRSoffset * medianFreq) / 1000) - _FirstFiducial;
						}

						// Make protected zones work properly
						_Protected[loper].Start -= ((_Protected[loper].Start % rate) == 0 ? rate : (_Protected[loper].Start % rate));
						_Protected[loper].Start++;
						_Protected[loper].End += (rate - (_Protected[loper].End % rate));

						// Keep it all between boundaries of ECG.
						if (_Protected[loper].Start < minbegin)
						{
							_Protected[loper].Start = minbegin;
						}
						if (_Protected[loper].End > maxend)
						{
							_Protected[loper].End = maxend;
						}
					}
				}
			}
		}
		// Signal Manupalations
		public int getSignals(out Signals signals)
		{
			signals = new Signals();
			int err = getSignalsToObj(signals);
			if (err != 0)
			{
				signals = null;
			}
			return err;
		}
		public int getSignalsToObj(Signals signals)
		{
			if (signals != null
			&&  Works())
			{
				signals.MedianLength = _MedianDataLength;
				signals.MedianFiducialPoint = _FirstFiducial;

				if (_NrQRS == 0)
				{
					signals.QRSZone = null;
				}
				else
				{
					signals.QRSZone = new QRSZone[_NrQRS];
					for (int loper = 0;loper < _NrQRS;loper++)
					{
						signals.QRSZone[loper] = new QRSZone();
						if (_Subtraction[loper] != null)
						{
							signals.QRSZone[loper].Type = _Subtraction[loper].Type;
							signals.QRSZone[loper].Start = _Subtraction[loper].Start - 1;
							signals.QRSZone[loper].Fiducial = _Subtraction[loper].Fiducial;
							signals.QRSZone[loper].End = _Subtraction[loper].End;
						}
					}
				}

				return 0;
			}
			return 1;
		}
		public int setSignals(Signals signals)
		{
			if ((signals != null)
			&&  (signals.NrLeads != 0))
			{
				_MedianDataLength = signals.MedianLength;
				_FirstFiducial = signals.MedianFiducialPoint;

				if (signals.QRSZone == null)
				{
					_NrQRS = 0;
					_Subtraction = null;
					_Protected = null;
				}
				else
				{
					_NrQRS = (ushort) signals.QRSZone.Length;
					_Subtraction = new SCPQRSSubtraction[_NrQRS];
					_Protected = new SCPQRSProtected[_NrQRS];

					for (int loper=0;loper < _NrQRS;loper++)
					{
						_Subtraction[loper] = new SCPQRSSubtraction();
						_Protected[loper] = new SCPQRSProtected();

						if (signals.QRSZone[loper] != null)
						{
							_Subtraction[loper].Type = signals.QRSZone[loper].Type;
							_Subtraction[loper].Fiducial = signals.QRSZone[loper].Fiducial;

							if (_Subtraction[loper].Type == 0)
							{
								_Subtraction[loper].Start = signals.QRSZone[loper].Start + 1;
								_Subtraction[loper].End = signals.QRSZone[loper].End;

								if (((_Subtraction[loper].End - _Subtraction[loper].Fiducial) + _FirstFiducial)
								>=	((signals.MedianLength * signals.MedianSamplesPerSecond) / 1000))
								{
									_Subtraction[loper].End = (int) (((((signals.MedianLength * signals.MedianSamplesPerSecond) / 1000) - _FirstFiducial) + _Subtraction[loper].Fiducial - 2) & 0xfffffffe);
								}

								_Protected[loper].Start = _Subtraction[loper].Start;
								_Protected[loper].End = _Subtraction[loper].End;
							}
							else
							{
							}
						}
					}
				}

				return 0;
			}
			return 1;
		}
		/// <summary>
		/// Class containing SCP QRS subtraction zone.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)]
		public class SCPQRSSubtraction
		{
			public ushort Type = ushort.MaxValue;
			public int Start = 0;
			public int Fiducial = 0;
			public int End = 0;
			/// <summary>
			/// Constructor for an QRS Subtraction zone.
			/// </summary>
			public SCPQRSSubtraction()
			{}
			/// <summary>
			/// Constructor for an QRS Subtraction zone.
			/// </summary>
			/// <param name="type">type of subtraction</param>
			/// <param name="start">starting point of subtraction zone</param>
			/// <param name="fiducial">fiducial point in subtraction zone</param>
			/// <param name="end">ending point of subtraction zone</param>
			public SCPQRSSubtraction(byte type, int start, int fiducial, int end)
			{
				Type = type;
				Start = start;
				Fiducial = fiducial;
				End = end;
			}
			/// <summary>
			/// Function to read QRS Subtraction.
			/// </summary>
			/// <param name="buffer">byte array to read QRS subtraction.</param>
			/// <param name="offset">position to start reading</param>
			/// <returns>0 on success</returns>
			public int Read(byte[] buffer, int offset)
			{
				if (offset + Marshal.SizeOf(this) > buffer.Length)
				{
					return 0x1;
				}

				Type = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Type), true);
				offset += Marshal.SizeOf(Type);
				Start = (int) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Start), true);
				offset += Marshal.SizeOf(Start);
				Fiducial = (int) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Fiducial), true);
				offset += Marshal.SizeOf(Fiducial);
				End = (int) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(End), true);
				offset += Marshal.SizeOf(End);

				return 0x0;
			}
			/// <summary>
			/// Function to write QRS Subtraction zone.
			/// </summary>
			/// <param name="buffer">byte array</param>
			/// <param name="offset">position to start writing</param>
			/// <returns>0 on success</returns>
			public int Write(byte[] buffer, int offset)
			{
				if (offset + Marshal.SizeOf(this) > buffer.Length)
				{
					return 0x1;
				}

				BytesTool.writeBytes(Type, buffer, offset, Marshal.SizeOf(Type), true);
				offset += Marshal.SizeOf(Type);
				BytesTool.writeBytes(Start, buffer, offset, Marshal.SizeOf(Start), true);
				offset += Marshal.SizeOf(Start);
				BytesTool.writeBytes(Fiducial, buffer, offset, Marshal.SizeOf(Fiducial), true);
				offset += Marshal.SizeOf(Fiducial);
				BytesTool.writeBytes(End, buffer, offset, Marshal.SizeOf(End), true);
				offset += Marshal.SizeOf(End);

				return 0x0;
			}
		}
		/// <summary>
		/// Class containing QRS protected zones.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Ansi)]
		public class SCPQRSProtected
		{
			public int Start = 0;
			public int End = 0;
			/// <summary>
			/// Constructor to create an QRS protected zone.
			/// </summary>
			public SCPQRSProtected()
			{}
			/// <summary>
			/// Constructor to create an QRS protected zone.
			/// </summary>
			/// <param name="start">start sample of zone</param>
			/// <param name="end">end sample of zone</param>
			public SCPQRSProtected(int start, int end)
			{
				Start = start;
				End = end;
			}
			/// <summary>
			/// Function to read QRS protected zone.
			/// </summary>
			/// <param name="buffer">byte array to read from</param>
			/// <param name="offset">position to start reading</param>
			/// <returns>0 on success</returns>
			public int Read(byte[] buffer, int offset)
			{
				if (offset + Marshal.SizeOf(this) > buffer.Length)
				{
					return 0x1;
				}

				Start = (int) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(Start), true);
				offset += Marshal.SizeOf(Start);
				End = (int) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(End), true);
				offset += Marshal.SizeOf(End);

				return 0x0;
			}
			/// <summary>
			/// Function to write QRS protected zone.
			/// </summary>
			/// <param name="buffer">byte array to write to</param>
			/// <param name="offset">position to start writing</param>
			/// <returns>0 on success</returns>
			public int Write(byte[] buffer, int offset)
			{
				if (offset + Marshal.SizeOf(this) > buffer.Length)
				{
					return 0x1;
				}

				BytesTool.writeBytes(Start, buffer, offset, Marshal.SizeOf(Start), true);
				offset += Marshal.SizeOf(Start);
				BytesTool.writeBytes(End, buffer, offset, Marshal.SizeOf(End), true);
				offset += Marshal.SizeOf(End);

				return 0x0;
			}
		}
	}
}