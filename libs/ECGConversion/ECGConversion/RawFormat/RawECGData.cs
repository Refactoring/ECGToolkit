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

Written by Marcel de Wijs.

****************************************************************************/
using System;
using System.Runtime.InteropServices;
using Communication.IO.Tools;
using ECGConversion.ECGDemographics;
using ECGConversion.ECGSignals;

namespace ECGConversion.RawFormat
{
    /// <summary>
    /// Summary description for RawECGData.
    /// </summary>
    public class RawECGData
    {
        short[][] _Data = null;
        public RawECGData()
        {
            //
            // TODO: Add constructor logic here
            //
        }
        /// <summary>
        /// Function to read data segment from buffer.
        /// </summary>
        /// <param name="buffer">buffer to read from</param>
        /// <param name="offset">position to start reading</param>
        /// <param name="littleEndian">true if little endian used</param>
        /// <param name="nrleads">number of leads in section</param>
        /// <param name="nrsamples">number of samples for each lead</param>
        /// <returns>0 on success</returns>
        public int Read(byte[] buffer, int offset, bool littleEndian, int nrleads, uint nrsamples, bool bIsADCFormat)
        {
            if ((buffer == null)
			||	(nrleads <= 0))
            {
                return 1;
            }

            _Data = new short[nrleads][];
            for (int lead=0;lead < nrleads;lead++)
            {
                _Data[lead] = new short[nrsamples];
            }

            if (bIsADCFormat)
            {
                for (int sample=0;sample < nrsamples;sample++)
                {
                    if ((offset + Marshal.SizeOf(typeof(short))) > buffer.Length)
                    {
                        break;
                    }

                    for (int lead=0;lead < nrleads;lead++)
                    {
                        if ((offset + Marshal.SizeOf(_Data[lead][sample])) > buffer.Length)
                        {
                            break;
                        }
                        _Data[lead][sample] = (short) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_Data[lead][sample]), littleEndian);
                        offset += Marshal.SizeOf(_Data[lead][sample]);
                    }
                }
            }
            else
            {
                for (int lead=0;lead < nrleads;lead++)
                {
                    if ((offset + Marshal.SizeOf(typeof(short))) > buffer.Length)
                    {
                        break;
                    }

                    for (int sample=0;sample < nrsamples;sample++)
                    {
                        if ((offset + Marshal.SizeOf(_Data[lead][sample])) > buffer.Length)
                        {
                            break;
                        }
                        _Data[lead][sample] = (short) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(_Data[lead][sample]), littleEndian);
                        offset += Marshal.SizeOf(_Data[lead][sample]);
                    }
                }
            }
            return 0;
        }
        /// <summary>
        /// Function to write data section to buffer.
        /// </summary>
        /// <param name="buffer">buffer to write in</param>
        /// <param name="littleEndian">true if little endian used</param>
        /// <returns>0 on success</returns>
        public int Write(out byte[] buffer, bool littleEndian, bool bIsADCFormat)
        {
            buffer = new byte[getLength()];
            int err = Write(buffer, 0, littleEndian);
            if (err != 0)
            {
                buffer = null;
            }
            return err;
        }
        /// <summary>
        /// Function to write data section to buffer.
        /// </summary>
        /// <param name="buffer">buffer to write in</param>
        /// <param name="offset">position to start writing</param>
        /// <param name="littleEndian">true if little endian used</param>
        /// <returns>0 on success</returns>
        public int Write(byte[] buffer, int offset, bool littleEndian)
        {
            if (!Works())
            {
                return 1;
            }

            if ((buffer == null)
			||	((offset + (_Data.Length * _Data[0].Length * Marshal.SizeOf(typeof(short)))) > buffer.Length))
            {
                return 2;
            }

            for (int sample=0;sample < _Data[0].Length;sample++)
            {
                for (int lead=0;lead < _Data.Length;lead++)
                {
                    BytesTool.writeBytes(_Data[lead][sample], buffer, offset, Marshal.SizeOf(_Data[lead][sample]), littleEndian);
                    offset += Marshal.SizeOf(_Data[lead][sample]);
                }
            }
            return 0;
        }
        /// <summary>
        /// Function to get size of section.
        /// </summary>
        /// <returns>size of section</returns>
        public int getLength()
        {
            if (Works())
            {
                return (_Data.Length * _Data[0].Length * Marshal.SizeOf(typeof(short)));
            }
            return 0;
        }
        /// <summary>
        /// Function to get number of leads in section.
        /// </summary>
        /// <returns>number of leads</returns>
        public int getNrLeads()
        {
            if (Works())
            {
                return _Data.Length;
            }
            return 0;
        }
        /// <summary>
        /// Function to get number o samples for each lead.
        /// </summary>
        /// <returns>number of samples for each lead</returns>
        public int getNrSamplesPerLead()
        {
            if (Works())
            {
                return _Data[0].Length;
            }
            return 0;
        }
        /// <summary>
        /// Function to empty a section.
        /// </summary>
        public void Empty()
        {
            _Data = null;
        }
        /// <summary>
        /// Function to encode data in section.
        /// </summary>
        /// <param name="data">data to encode</param>
        /// <param name="nrsamples">number of samples in each lead</param>
        /// <returns>0 on success.</returns>
        public int Encode(short[][] data, int nrsamples)
        {
            if ((data != null)
			&&	(data.Length > 0)
			&&	(data[0] != null)
			&&	(data[0].Length > 0)
			&&	(nrsamples <= data[0].Length))
            {
                _Data = new short[data.Length][];
                int length = data[0].Length;
                for (int lead=0;lead < _Data.Length;lead++)
                {
                    if (length != data[lead].Length)
                    {
                        _Data = null;
                        return 2;
                    }
                    _Data[lead] = new short[nrsamples];
                    for (int sample=0;sample < _Data[lead].Length;sample++)
                    {
                        _Data[lead][sample] = data[lead][sample];
                    }
                }
                return 0;
            }
            return 1;
        }
        /// <summary>
        /// Function to Decode data in section
        /// </summary>
        /// <param name="data">array to write data in</param>
        /// <returns>0 on success</returns>
        public int Decode(out short[][] data)
        {
            data = null;
            if (Works())
            {
                data = new short[_Data.Length][];
                for (int lead=0;lead < _Data.Length;lead++)
                {
                    data[lead] = new short[_Data[0].Length];
                    for (int sample=0;sample < data[lead].Length;sample++)
                    {
                        data[lead][sample] = _Data[lead][sample];
                    }
                }
                return 0;
            }
            return 1;
        }
        /// <summary>
        /// Function to get completeness of section
        /// </summary>
        /// <returns>true if works</returns>
        public bool Works()
        {
            if ((_Data != null)
			&&	(_Data.Length > 0)
			&&	(_Data[0] != null))
            {
                int lastLength = _Data[0].Length;
                for (int loper=1;loper < _Data.Length;loper++)
                {
                    if ((_Data[loper] == null)
                        ||	(_Data[loper].Length != lastLength))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}
