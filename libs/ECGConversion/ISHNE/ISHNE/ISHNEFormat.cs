/***************************************************************************
Copyright 2012-2013, van Ettinger Information Technology, Lopik, The Netherlands

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
using System.IO;

using ECGConversion;
using ECGConversion.ECGDemographics;
using ECGConversion.ECGSignals;

using Communication.IO.Tools;

namespace ECGConversion.ISHNE
{
	public class ISHNEFormat : IECGFormat, ISignal, IBufferedSource
	{
		public ISHNEFormat()
		{
			_InputStreamOffset = 0;
			_InputStream = null;
			_Signals = null;
			
			_Config = new ECGConfig(new string[]{"CRC Validation", "AVM Override"}, 1, null);
			_Config["CRC Validation"] = "true";
		}
		
		private bool _CRCValidation
		{
			get
			{
				return string.Compare(_Config["CRC Validation"], "false", true) != 0;
			}
		}
		
		private double _AVMOverride
		{
			get
			{
				double ret = -1.0;
				
				try
				{
					string temp = _Config["AVM Override"];
					
					if ((temp != null)
					&&	(temp.Length > 0))
					{
						ret = double.Parse(temp, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
						
						if (ret <= 0.0)
							ret = -1.0;
					}
				}
				catch {}
				
				return ret;
			}
		}

		~ISHNEFormat()
		{
			if (_InputStream != null)
			{
				try
				{
					_InputStream.Close();
				} catch {}

				_InputStream.Dispose();
				_InputStream = null;
			}
		}

		public override bool CanCloseStream
		{
			get {return false;}
		}

		public override bool SupportsBufferedStream
		{
			get {return true;}
		}

		public override void Dispose ()
		{
			if (_InputStream != null)
			{
				try
				{
					_InputStream.Close();
				} catch {}

				_InputStream.Dispose();
				_InputStream = null;
			}
		}

		// some defines for the ISHNE format
		public const string MAGIC_NUMBER = "ISHNE1.0";
		public const int BYTES_BEFORE_HEADER = 10;
		public const int SHORT_SIZE = 2;
		public const Int16 LEAD_FAULT_VALUE = -32768;

		// keep track of the stream to read from.
		private Int64 _InputStreamOffset;
		private Stream _InputStream;
		// (buffered) signals to read from
		private Signals _Signals;

		// The elements part of the ISHNE format
		private ISHNEHeader _Header = new ISHNEHeader();
		private Byte[] _HeaderAndVarBlock;

		// currently read data (buffer)
		private Int64 _SignalBufferOffset;
		private Byte[] _SignalBuffer;

#region IECGFormat
		public override int Read(Stream input, int offset)
		{
			_Signals = null;

			if ((input != null)
			&&	input.CanRead
			&&	input.CanSeek
			&&	(offset >= 0))
			{
				_InputStreamOffset = input.Position + offset;

				_HeaderAndVarBlock = new byte[_Header.Size() + BYTES_BEFORE_HEADER];

				input.Position += offset;

				if (BytesTool.readStream(input, _HeaderAndVarBlock, 0, _HeaderAndVarBlock.Length) == _HeaderAndVarBlock.Length)
				{
					string magicNumber = BytesTool.readString(_HeaderAndVarBlock, 0, MAGIC_NUMBER.Length);

					if (string.Compare(magicNumber, MAGIC_NUMBER) == 0)
					{
						if ((_Header.Read(_HeaderAndVarBlock, BYTES_BEFORE_HEADER, _HeaderAndVarBlock.Length - BYTES_BEFORE_HEADER) == 0)
						&&	_Header.Works())
						{
							if (_Header.VarBlockSize > 0)
							{
								byte[] buff = new byte[_HeaderAndVarBlock.Length + _Header.VarBlockSize];

								BytesTool.copy(buff, 0, _HeaderAndVarBlock, 0, _HeaderAndVarBlock.Length);

								if (BytesTool.readStream(input, buff, _HeaderAndVarBlock.Length, _Header.VarBlockSize) != _Header.VarBlockSize)
								{
									return 0x10;
								}

								_HeaderAndVarBlock = buff;
							}

							CRCTool tool = new CRCTool();
							tool.Init(CRCTool.CRCCode.CRC_CCITT);

							ushort crc = (ushort) BytesTool.readBytes(_HeaderAndVarBlock, MAGIC_NUMBER.Length, SHORT_SIZE, true);

							if (!_CRCValidation
							||	(crc == tool.CalcCRCITT(_HeaderAndVarBlock, BYTES_BEFORE_HEADER, _HeaderAndVarBlock.Length - BYTES_BEFORE_HEADER)))
							{
                                if (input.CanSeek)
                                {
                                    long nrSamples = (input.Length - _Header.ECGOffset) / 2;

                                    if ((nrSamples > _Header.ECGNrSamples)
                                    &&  (nrSamples <= int.MaxValue))
                                    {
                                        _Header.ECGNrSamples = (int) nrSamples;
                                    }
                                }

								BufferedSignals sigs = new BufferedSignals(this, (byte)_Header.ECGNrLeads);

								// Begin: code that handles overriding of AVM
								double avm = _AVMOverride;
								
								if (avm <= 0.0)
								{
									avm = double.MaxValue;
									
									for (int i=0;i < _Header.ECGNrLeads;i++)
									{
										sigs[i] = new Signal();
										sigs[i].Type = _Header.GetLeadType(i);

										double val = _Header.GetLeadAmplitude(i);

										if (val < avm)
											avm = val;
									}
								}
								else
								{
									for (int i=0;i < _Header.ECGNrLeads;i++)
									{
										sigs[i] = new Signal();
										sigs[i].Type = _Header.GetLeadType(i);
									}
								}
								// End: code that handles overriding of AVM

								sigs.RhythmAVM = avm;
								sigs.RhythmSamplesPerSecond = _Header.ECGSampleRate;
								sigs.RealRhythmSamplesPerSecond = _Header.ECGSampleRate;
								sigs.RealRhythmStart = 0;
								sigs.RealRhythmEnd = _Header.ECGNrSamples / _Header.ECGNrLeads;

								_InputStream = input;
								_Signals = sigs;

								sigs.Init();

								return 0x0;
							}

							return 0x20;
						}

						return 0x8;
					}

					return 0x4;
				}

				return 0x2;
			}

			return 0x1;
		}

		public override int Read(string file, int offset)
		{
			FileStream stream = null;

			try
			{
				stream = new FileStream(file, FileMode.Open);

				return Read(stream, offset);
			}
			catch {}

			return 1;
		}

		public override int Read(byte[] buffer, int offset)
		{
			MemoryStream ms = null;
			
			try
			{
				ms = new MemoryStream(buffer, offset, buffer.Length-offset, false);

				return Read(ms, 0);
			}
			catch {}

			return 2;
		}

		public override int Write(string file)
		{
			FileStream output = null;

			try
			{
				output = new FileStream(file, FileMode.Create);

				return Write(output);
			}
			catch {}
			finally
			{
				if (output != null)
				{
					output.Close();
					output = null;
				}
			}

			return 0x1;
		}

		public override int Write(Stream output)
		{
			try
			{
				if ((output != null)
				&&	output.CanWrite)
				{
					if (Works())
					{
						_Header.VarBlockOffset = _Header.Size() + BYTES_BEFORE_HEADER;
						_Header.VarBlockSize = (_HeaderAndVarBlock != null) && (_HeaderAndVarBlock.Length < _Header.VarBlockOffset) ? _HeaderAndVarBlock.Length - _Header.VarBlockOffset : 0;
						_Header.ECGOffset = _Header.VarBlockOffset + _Header.VarBlockSize;

						if ((_HeaderAndVarBlock == null)
						||	(_HeaderAndVarBlock.Length < _Header.ECGOffset))
						{
							_HeaderAndVarBlock = new byte[_Header.ECGOffset];
						}
						
						// Begin: code that handles overriding of AVM
						double avmOverride = _AVMOverride;
						int nrLeads = _Signals.NrLeads;
						double[] avm = new double[nrLeads];
						
						if (avmOverride <= 0.0)
						{
							for (int i=0;i < nrLeads;i++)
								avm[i] = _Header.GetLeadAmplitude(i);
						}
						else
						{
							for (int i=0;i < nrLeads;i++)
							{
								_Header.ECGLeadResolution[i] = (Int16) (avmOverride * ISHNEHeader.UV_TO_AMPLITUDE);
								avm[i] = avmOverride;
							}
						}
						// End: code that handles overriding of AVM

						BytesTool.writeString(MAGIC_NUMBER, _HeaderAndVarBlock, 0, MAGIC_NUMBER.Length);

						if (_Header.Write(_HeaderAndVarBlock, BYTES_BEFORE_HEADER, _Header.Size()) != 0)
							return 0x4;
						
						CRCTool tool = new CRCTool();
						tool.Init(CRCTool.CRCCode.CRC_CCITT);

						ushort crc = tool.CalcCRCITT(_HeaderAndVarBlock, BYTES_BEFORE_HEADER, _HeaderAndVarBlock.Length - BYTES_BEFORE_HEADER);

						BytesTool.writeBytes(crc, _HeaderAndVarBlock, MAGIC_NUMBER.Length, SHORT_SIZE, true);

						output.Write(_HeaderAndVarBlock, 0, _Header.ECGOffset);

						if (_Signals.IsBuffered)
						{
							BufferedSignals bs = _Signals.AsBufferedSignals;

							int rhythmCur = bs.RealRhythmStart,
								rhythmEnd = bs.RealRhythmEnd,
								stepSize = 60 * bs.RhythmSamplesPerSecond;

							byte[] data = null;

							for (;rhythmCur < rhythmEnd;rhythmCur+=stepSize)
							{
								if (!bs.LoadSignal(rhythmCur, rhythmCur+stepSize))
									return 0x8;

								int buff_size = _WriteSignal(ref data, _Signals, avm);

								if (buff_size < 0)
									return 0x10;

								output.Write(data, 0, buff_size);
							}
						}
						else
						{
							int rhythmStart, rhythmEnd;

							_Signals.CalculateStartAndEnd(out rhythmStart, out rhythmEnd);

							byte[] data = null;

							int buff_size = _WriteSignal(ref data, _Signals, avm);

							if (buff_size < 0)
								return 0x10;

							output.Write(data, 0, buff_size);
						}

						return 0x0;
					}

					return 0x2;
				}
			}
			catch
			{
				return 0x10;
			}

			return 0x1;
		}

		public override int Write(byte[] buffer, int offset)
		{
			MemoryStream ms = null;
			
			try
			{
				ms = new MemoryStream(buffer, offset, buffer.Length-offset, true);

				return Write(ms);
			}
			catch {}
			finally
			{
				if (ms != null)
				{
					ms.Close();
					ms = null;
				}
			}

			return 0x2;
		}

		public override bool CheckFormat(Stream input, int offset)
		{
			if ((input != null)
			&&	input.CanRead
			&&	input.CanSeek
			&&	(offset >= 0))
			{
				byte[] buff = new byte[_Header.Size() + BYTES_BEFORE_HEADER];

				long origin =  input.Position;

				input.Position += offset;

				if (BytesTool.readStream(input, buff, 0, buff.Length) == buff.Length)
				{
					string magicNumber = BytesTool.readString(buff, 0, MAGIC_NUMBER.Length);

					if (string.Compare(magicNumber, MAGIC_NUMBER) == 0)
					{
						ISHNEHeader head = new ISHNEHeader();

						if ((head.Read(buff, BYTES_BEFORE_HEADER, buff.Length - BYTES_BEFORE_HEADER) == 0)
						&&	head.Works())
						{
							if (head.VarBlockSize > 0)
							{
								byte[] buffTwo = new byte[buff.Length + head.VarBlockSize];

								BytesTool.copy(buffTwo, 0, buff, 0, buff.Length);

								if (BytesTool.readStream(input, buffTwo, buff.Length, head.VarBlockSize) != head.VarBlockSize)
								{
									input.Position = origin;

									return false;
								}

								buff = buffTwo;
							}

							input.Position = origin;
							
							if (_CRCValidation)
							{
								CRCTool tool = new CRCTool();
								tool.Init(CRCTool.CRCCode.CRC_CCITT);
	
								ushort crc = (ushort) BytesTool.readBytes(buff, MAGIC_NUMBER.Length, SHORT_SIZE, true);
	
								return crc == tool.CalcCRCITT(buff, BYTES_BEFORE_HEADER, buff.Length - BYTES_BEFORE_HEADER);
							}
							
							return true;
						}
					}
				}

				input.Position = origin;
			}

			return false;
		}

		public override bool CheckFormat(string file, int offset)
		{
			FileStream stream = null;

			try
			{
				stream = new FileStream(file, FileMode.Open);

				return CheckFormat(stream, offset);
			}
			catch {}

			return false;
		}

		public override bool CheckFormat(byte[] buffer, int offset)
		{
			MemoryStream ms = null;
			
			try
			{
				ms = new MemoryStream(buffer, offset, buffer.Length-offset, false);

				return CheckFormat(ms, 0);
			}
			catch {}

			return false;
		}

		public override void Anonymous(byte type)
		{
			ECGTool.Anonymous(Demographics, (char)type);
		}

		public override int getFileSize()
		{
			return _Header.Works() ? _Header.ECGOffset + (_Header.ECGNrSamples << 1): -1;
		}

		public override bool Works()
		{
			return _Header.Works()
				&& (_Signals != null)
				&& (_Signals.NrLeads > 0)
				&& (_Header.ECGNrLeads == _Signals.NrLeads);
		}

		public override void Empty()
		{
			_InputStreamOffset = 0;

			if (_InputStream != null)
			{
				try
				{
					_InputStream.Close();
				} catch {}

				_InputStream.Dispose();
				_InputStream = null;
			}

			_Signals = null;

			_Header.Empty();
			_HeaderAndVarBlock = null;
		}

		public override IDemographic Demographics
		{
			get {return _Header;}
		}

		public override ECGConversion.ECGDiagnostic.IDiagnostic Diagnostics
		{
			get {return null;}
		}

		public override ECGConversion.ECGGlobalMeasurements.IGlobalMeasurement GlobalMeasurements
		{
			get {return null;}
		}

		public override ISignal Signals
		{
			get {return this;}
		}

		public override ECGConversion.ECGLeadMeasurements.ILeadMeasurement LeadMeasurements
		{
			get {return null;}
		}
#endregion

#region ISignal
		public int getSignalsToObj(Signals sigs)
		{
/*			if ((sigs != null)
			&&	(sigs is BufferedSignals))
			{
				sigs.

				return 2;
			}*/

			return 1;
		}

		public int getSignals(out Signals sigs)
		{
			sigs = null;

			if (Works()
			&&	(_Signals != null))
			{
				sigs = _Signals.Clone();

				return 0;
			}

			return 1;
		}

		public int setSignals(Signals sigs)
		{
			if ((sigs != null)
			&&	(sigs.NrLeads > 0)
			&&	(sigs.NrLeads <= ISHNEHeader.MAX_NR_LEADS)
			&&	(sigs.RhythmAVM > 0.0)
			&&	(sigs.RhythmSamplesPerSecond > 0))
			{
				_Header.ECGNrLeads = sigs.NrLeads;
				_Header.ECGSampleRate = (Int16) sigs.RhythmSamplesPerSecond;

				for (int i=0;i < sigs.NrLeads;i++)
				{
					_Header.SetLeadType(i, sigs[i].Type);
					_Header.ECGLeadQuality[i] = 0;
					_Header.ECGLeadResolution[i] = (Int16) (sigs.RhythmAVM * ISHNEHeader.UV_TO_AMPLITUDE);
				}

				if (sigs.IsBuffered)
				{
					BufferedSignals bs = sigs.AsBufferedSignals;

					_Header.ECGNrSamples = (bs.RealRhythmEnd - bs.RealRhythmStart) * _Header.ECGNrLeads;
				}
				else
				{
					int rhythmStart, rhythmEnd;

					sigs.CalculateStartAndEnd(out rhythmStart, out rhythmEnd);

					_Header.ECGNrSamples = (rhythmEnd - rhythmStart) * _Header.ECGNrLeads;
				}

				_Signals = sigs;

				return 0;
			}

			return 1;
		}
#endregion

#region IBufferedSignal
		public bool LoadRhythmSignal(byte leadNr, Signal lead, double avm, int rhythmStart, int rhythmEnd)
		{
			if ((_InputStream != null)
			&&	(_Header != null)
//			&&	_Header.Works()
			&&	(lead != null)
			&&	(avm > 0.0)
			&&	(rhythmStart >= 0)
			&&	(rhythmEnd >= 0)
			&&	(rhythmStart < rhythmEnd))
			{
				leadNr = _Header.GetLeadType(leadNr, lead.Type);

				if ((leadNr >= 0)
				&&	(leadNr < _Header.ECGNrLeads))
				{
					// determine max size
					int max_size = _Header.ECGNrSamples / _Header.ECGNrLeads;

					if (max_size < rhythmEnd)
						rhythmEnd = max_size;

					// determine size and location of buffer
					int buff_size = (rhythmEnd - rhythmStart) * _Header.ECGNrLeads * SHORT_SIZE,
						sigs_size = rhythmEnd - rhythmStart;

					Int64 read_offset = _InputStreamOffset + _Header.ECGOffset + (rhythmStart * SHORT_SIZE * _Header.ECGNrLeads);

					if ((_SignalBuffer == null)
					||	(_SignalBuffer.Length != buff_size)
					||	(_SignalBufferOffset != read_offset))
					{
						// set the new buffer.
						_SignalBufferOffset = read_offset;
						_SignalBuffer = new byte[buff_size];

						_InputStream.Position = read_offset;

						if (BytesTool.readStream(_InputStream, _SignalBuffer, 0, buff_size) != buff_size)
						{
							_SignalBufferOffset = 0;
							_SignalBuffer = null;
						}
					}

					if ((_SignalBuffer != null)
					&&	(_SignalBuffer.Length == buff_size)
					&&	(_SignalBufferOffset == read_offset))
					{
						lead.RhythmStart = rhythmStart;
						lead.RhythmEnd = rhythmEnd;

						if ((lead.Rhythm == null)
						||	(lead.Rhythm.Length != sigs_size))
						{
							lead.Rhythm = new short[sigs_size];
						}

						int pos = leadNr * SHORT_SIZE;

						// Begin: code that handles overriding of AVM
						double leadAVM = _AVMOverride;

						if (leadAVM <= 0.0)
							leadAVM = _Header.GetLeadAmplitude(leadNr);
						// End: code that handles overriding of AVM

						for (int i=0;i < sigs_size;i++)
						{
							short curr = (short)BytesTool.readBytes(_SignalBuffer, pos, SHORT_SIZE, true);

							if (curr != LEAD_FAULT_VALUE)
								lead.Rhythm[i] = (short) ((curr * leadAVM) / avm);
							else
								lead.Rhythm[i] = curr;

							pos += _Header.ECGNrLeads * SHORT_SIZE;
						}

						return true;
					}
				}
			}

			return false;
		}

		public bool LoadTemplateSignal(byte leadNr, Signal lead, double avm, int templateNr)
		{
			return false;
		}

		public void LoadTemplateOccurance(int templateNr, out int templateOccurance, out QRSZone[] templateLocations)
		{
			templateOccurance = 0;
			templateLocations = null;
		}
#endregion

		private static int _WriteSignal(ref byte[] data, Signals sigs, double[] avm)
		{
			if (sigs == null)
				return -1;

			int rhythmStart, rhythmEnd;
			sigs.CalculateStartAndEnd(out rhythmStart, out rhythmEnd);

			int	nrLeads = sigs.NrLeads,
				sigs_size = rhythmEnd - rhythmStart,
				buff_size = sigs_size * nrLeads * SHORT_SIZE;

			if ((sigs_size <= 0)
			||	(avm == null)
			||	(avm.Length != nrLeads))
				return -1;

			if ((data == null)
			||	(data.Length < buff_size))
				data = new byte[buff_size];
						
			for (int pos=0;pos < buff_size;pos+=SHORT_SIZE)
			{
				int pos2 = pos / SHORT_SIZE,
					lead = pos2 % nrLeads,
					sample = (pos2 / nrLeads) + rhythmStart;

				short curr = LEAD_FAULT_VALUE;

				if ((sample >= sigs[lead].RhythmStart)
				&&  (sample < sigs[lead].RhythmEnd))
				{
					sample -= sigs[lead].RhythmStart;

					if ((sample >= 0)
					&&	(sample < sigs[lead].Rhythm.Length))
						curr = sigs[lead].Rhythm[sample];
				}

				if (curr != LEAD_FAULT_VALUE)
					curr = (short) ((curr * sigs.RhythmAVM) / avm[lead]);

				BytesTool.writeBytes(curr, data, pos, SHORT_SIZE, true);
			}

			return buff_size;
		}
	}
}
