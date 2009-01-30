/***************************************************************************
Copyright 2004,2008-2009, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

	http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

Written by Marcel de Wijs. Changed by Maarten van Ettinger.

****************************************************************************/
using System;
using System.IO;
using System.Runtime.InteropServices;
using Communication.IO.Tools;
using ECGConversion;
using ECGConversion.ECGDemographics;
using ECGConversion.ECGDiagnostic;
using ECGConversion.ECGGlobalMeasurements;
using ECGConversion.ECGSignals;
namespace ECGConversion.RawFormat
{
	/// <summary>
	/// Summary description for RawECGFormat.
	/// </summary>
	public class RawECGFormat : IECGFormat, ISignal
	{
        // Static variable for needed leads for other programs support.
        public static LeadType[] NeededLeads = {LeadType.V1, LeadType.V2, LeadType.V3, LeadType.V4, LeadType.V5, LeadType.V6, LeadType.I, LeadType.II, LeadType.III};
        public static int ExtraBytes = 12;
        // Data parts.
        private RawECGHeader    _DummyHeader    = new RawECGHeader();
        private RawECGData      _Data           = new RawECGData();

        private bool _bIsADCFormat = true;
        private int _ECGLSBperMV = 25;
        private int _nrleads = 1;
        private int _nrsamples = -1;
        private int _samplerate = 1000;
        private bool _littleEndian = false;
        private string[] _theLeadConfig;

        public void setIsADCFormat(bool bIsADCFormat)
        {
            _bIsADCFormat = bIsADCFormat;
        }

        public void setNrLeads(int NrOfLeads)
        {
            _nrleads = NrOfLeads;
        }

        public void setNrOfSamplePerLead(int iNrOfSamplePerLead)
        {
            _nrsamples = iNrOfSamplePerLead;
        }

        public void setSampleRate(int iSampleRate)
        {
            _samplerate = iSampleRate;
        }

        public void setLitteEndian(bool isLitteEndian)
        {
            _littleEndian = isLitteEndian;
        }

        public void setECGLSBperMV(int LSBPerMV)
        {
            _ECGLSBperMV = LSBPerMV;
        }

        public void setLeadConfiguration(string[] myLeadConfig)
        {
            _theLeadConfig = myLeadConfig;
        }

        #region IECGFormat Members
        public override int Read(Stream input, int offset)
        {
            byte[] buffer = new byte[input.Length - offset];
            input.Seek(offset, SeekOrigin.Begin);
            if (BytesTool.readStream(input, buffer, 0, buffer.Length) != buffer.Length)
            {
                return 0x1;
            }

            return Read(buffer, 0) << 1;
        }
        public override int Read(string file, int offset)
        {
            if (file != null)
            {
                Stream read = new FileStream(file, FileMode.Open);
                int err = Read(read, offset);
                read.Close();
                return err << 1;
            }
            return 0x1;
        }
        public override int Read(byte[] buffer, int offset)
        {	
            _DummyHeader.Init();
            bool littleEndian = true;

            _DummyHeader.setNrLeads(_nrleads);
            _DummyHeader.setNrSamplesPerLead(_nrsamples);
            _DummyHeader.setSampleRate(_samplerate);
            _DummyHeader.setLSBPerMV(_ECGLSBperMV);

            if (_theLeadConfig !=null)
            {
                _DummyHeader.setLeadConfiguration(_theLeadConfig);
            }
            if (_DummyHeader.Read(buffer, offset, littleEndian) != 0)
            {
                return 0x2;
            }

            if (_Data.Read(buffer, offset, littleEndian, _DummyHeader.getNrLeads(), _DummyHeader.getNrSamplesPerLead(), _bIsADCFormat) != 0)
            {
                return 0x20;
            }

            return 0;
        }
        public override int Write(string file)
        {
            if (file != null)
            {
                Stream output = new FileStream(file, FileMode.Create);
                int ret = Write(output);
                output.Close();

                // next mus be done after the former write has been done, all parameters have been set.
                System.IO.StreamWriter outputData = System.IO.File.CreateText("RawInfo.dat");
                WriteInfo(outputData);
                outputData.Close();

                return ret;
            }
            return 1;
        }
        public override int Write(Stream output)
        {
            if (output != null
                &&  output.CanWrite
                &&	Works())
            {
                byte[] buffer = new byte[getFileSize()];
                int ret = (Write(buffer, 0) << 1);
                if (ret != 0)
                {
                    return ret;
                }
                output.Write(buffer, 0, buffer.Length);

                // next mus be done after the former write has been done, all parameters have been set.
                System.IO.StreamWriter outputData = System.IO.File.CreateText("RawInfo.dat");
                WriteInfo(outputData);
                outputData.Close();
                return 0;
            }
            return 1;
        }
        public override int Write(byte[] buffer, int offset)
        {
            if (Works()
                &&	(buffer != null)
                &&	(offset + getFileSize()) <= buffer.Length)
            {
                if (_Data.Write(buffer, offset, _DummyHeader.isLittleEndian()) != 0)
                {
                    return 4;
                }

                return 0;
            }
            return 1;
        }

        public override bool CheckFormat(Stream input, int offset)
        {
            if ((input != null)
                &&  input.CanRead)
            {
                return true;
            }
            return false;
        }
        public override bool CheckFormat(string file, int offset)
        {
            if (file != null)
            {
                Stream read = new FileStream(file, FileMode.Open);
                bool ret = CheckFormat(read, offset);
                read.Close();
                return ret;
            }
            return false;
        }
        public override bool CheckFormat(byte[] buffer, int offset)
        {
            if ((offset + RawECGHeader.HeaderSize) <= buffer.Length)
            {
                return ((buffer[offset] == RawECGHeader.ValueOfP)
                    &&	(buffer[offset + 1] == RawECGHeader.ValueOfK)
                    &&	((((int) BytesTool.readBytes(buffer, 4, 2, true)) == RawECGHeader.HeaderSize)
                    ||	 (((int) BytesTool.readBytes(buffer, 4, 2, false)) == RawECGHeader.HeaderSize)));
            }
            return false;
        }
        public override IDemographic Demographics
        {
			get
			{
				return (IDemographic) _DummyHeader;
			}
        }
        public override IDiagnostic Diagnostics
        {
			get
			{
				return null;
			}
        }
        public override IGlobalMeasurement GlobalMeasurements
        {
			get
			{
				return null;
			}
        }
        public override ISignal Signals
        {
			get
			{
				return (ISignal) this;
			}
        }
        public override void Anonymous(byte type)
        {
            _DummyHeader.Anonymous(type);
        }
        public override int getFileSize()
        {
            if (Works())
            {
                return _Data.getLength();
            }
            return 0;
        }
        public override bool Works()
        {
            return (_DummyHeader.Works()
                &&	_Data.Works()
                &&	(_DummyHeader.getNrSamplesPerLead() == _Data.getNrSamplesPerLead())
                &&	(_DummyHeader.getNrLeads() == _Data.getNrLeads()));
        }
        public override void Empty()
        {
            _DummyHeader.Empty();
            _Data.Empty();
        }
        #endregion
        #region ISignal Members
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
            if ((Works())
			&&	(signals != null))
            {
                if (_DummyHeader.getSignalsToObj(signals) != 0)
                {
                    return 2;
                }

                short[][] data;
                if (_Data.Decode(out data) != 0)
                {
                    return 4;
                }

                if (data.Length != _DummyHeader.getNrLeads())
                {
                    return 8;
                }

                for (int loper=0;loper < _DummyHeader.getNrLeads();loper++)
                {
                    if (data[loper] == null)
                    {
                        return 16;
                    }
                    signals[loper].Rhythm = data[loper];
                }

                return 0;
            }
            return 1;
        }
        public int setSignals(Signals signals)
        {
            if ((signals != null)
			&&	(signals.NrLeads != 0)
			&&	(signals.RhythmAVM > 0)
			&&	(signals.RhythmSamplesPerSecond > 0))
            {
                if (_DummyHeader.setSignals(signals) != 0)
                {
                    return 2;
                }

                // Determine minimum start.
                int minstart = int.MaxValue;
                for (int lead=0;lead < signals.NrLeads;lead++)
                {
                    if (signals[lead] != null)
                    {
                        minstart = Math.Min(minstart, signals[lead].RhythmStart);
                    }
                }

                int samplesBeforeResample = (int) (_DummyHeader.getNrSamplesPerLead() * signals.RhythmSamplesPerSecond) / 500;

                short[] position = new short[NeededLeads.Length];
                for (int find=0;find < NeededLeads.Length;find++)
                {
                    int p = 0;
                    for (;p < signals.NrLeads;p++)
                    {
                        if ((signals[p] != null)
						&&	(signals[p].Type == NeededLeads[find])
						&&	(signals[p].Rhythm != null))
                        {
                            break;
                        }
                    }
                    if (p == signals.NrLeads)
                    {
                        p = -1;
                    }
                    position[find] = (short) p;
                }

                short[][] data = new short[_DummyHeader.getNrLeads()][];
                for (int lead=0;lead < _DummyHeader.getNrLeads();lead++)
                {
                    if (position[lead] != -1)
                    {
                        data[lead] = new short[samplesBeforeResample];
                        for (int sample=signals[position[lead]].RhythmStart;sample < signals[position[lead]].RhythmEnd;sample++)
                        {
                            data[lead][sample - minstart] = signals[position[lead]].Rhythm[sample - signals[position[lead]].RhythmStart];
                        }
                    }
                }


                for (int lead=0;lead < _DummyHeader.getNrLeads();lead++)
                {
                    if (data[lead] == null)
                    {
                        if ((NeededLeads[lead] == LeadType.III)
						&&	(data[6] != null)
						&&	(data[7] != null))
                        {
                            data[lead] = ECGTool.CalculateLeadIII(data[6], 0, data[6].Length, data[7], 0, data[7].Length, data[6].Length);
                        }
                        else
                        {
                            return 4;
                        }
                    }
                }

                ECGTool.ResampleSignal(data, samplesBeforeResample, signals.RhythmSamplesPerSecond, 500, out data);
                ECGTool.ChangeMultiplier(data, signals.RhythmAVM, 2.5);

                if (_Data.Encode(data, (int) _DummyHeader.getNrSamplesPerLead()) != 0)
                {
                    return 8;
                }

                return 0;
            }
            return 1;
        }
        #endregion
        #region IDisposable Members
        public override void Dispose()
        {
			base.Dispose();

            _DummyHeader = null;
            _Data = null;
			_theLeadConfig = null;
        }
        #endregion

        public void WriteInfo(System.IO.StreamWriter output)
        {
            output.WriteLine("NrOfleads: {0} ",_Data.getNrLeads());
            output.WriteLine("NrSamples per lead: {0} ",_Data.getNrSamplesPerLead());
            output.WriteLine("LitteEndian?: {0}",_DummyHeader.isLittleEndian());
            output.WriteLine("PatientID: {0} ",_DummyHeader.PatientID);
            output.WriteLine("LSBPerMv:{0} ",_DummyHeader.getLSBPerMV());
            output.WriteLine("Samplerate:{0} ",_DummyHeader.getSampleRate());
            
            _DummyHeader.WriteInfo(output);
        }
    }
}
