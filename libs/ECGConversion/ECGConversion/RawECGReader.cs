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
using System.IO;
using ECGConversion.RawFormat;

namespace ECGConversion
{
	/// <summary>
	/// Summary description for RawECGReader.
	/// </summary>
	public class RawECGReader : IECGReader
	{
        private int _ECGLSBperMV = 25;
        private int _nrleads = -1;
        private int _nrsamplesperlead = -1;
        private int _samplerate = 500;
        private bool _littleEndian = true;
        private int _bytespersample = 2;
        private bool _bIsADCFormat = true;
        private string[] _theLeadConfig;

        

        public RawECGReader()
		{
			//
			// TODO: Add constructor logic here
			//
		}

        #region properties
        public int NrLeads
        {
            get{return _nrleads;}
            set{_nrleads = value;}
        }
        public int NrSamples
        {
            get{return _nrsamplesperlead;}
            set{_nrsamplesperlead = value;}
        }
        public int SampleRate
        {
            get{return _samplerate ;}
            set{_samplerate = value;}
        }
        public bool LitteEndian
        {
            get{return _littleEndian  ;}
            set{_littleEndian  = value;}
        }
        public int BytesPerSample
        {
            get{return _bytespersample;}
            set{_bytespersample= value;}
        }
        public bool bIsADCFormat
        {
            get{return _bIsADCFormat;}
            set{_bIsADCFormat= value;}
        }
        
        #endregion

        public IECGFormat Read(string sFile, int iNrOfLeads, int nNrOfSamplesPerLead)
        {
            _nrleads = iNrOfLeads;
            _nrsamplesperlead = nNrOfSamplesPerLead;
            return Read(sFile);
        }

        public IECGFormat Read(string sFile, int iNrOfLeads, int nNrOfSamplesPerLead, int iSampleRate)
        {
            _nrleads = iNrOfLeads;
            _nrsamplesperlead = nNrOfSamplesPerLead;
            _samplerate = iSampleRate;
            return Read(sFile);
        }

        public IECGFormat Read(string sFile, int iNrOfLeads, int nNrOfSamplesPerLead, int iSampleRate, string sEndian, int nECGLSBperMv, bool IsADC)
        {
            _nrleads = iNrOfLeads;
            _nrsamplesperlead = nNrOfSamplesPerLead;
            _samplerate = iSampleRate;
            if (sEndian=="ïeee-be")
            {
                _littleEndian = false;
            }
            _ECGLSBperMV = nECGLSBperMv;
            _bIsADCFormat = IsADC;
            return Read(sFile);
        }

        public IECGFormat Read(string sFile, int iNrOfLeads, int nNrOfSamplesPerLead, int iSampleRate, string sEndian, int nECGLSBperMv, bool IsADC, string[] MyLeadConfig)
        {
            _nrleads = iNrOfLeads;
            _nrsamplesperlead = nNrOfSamplesPerLead;
            _samplerate = iSampleRate;
            if (sEndian=="ïeee-be")
            {
                _littleEndian = false;
            }
            _ECGLSBperMV = nECGLSBperMv;
            _bIsADCFormat = IsADC;
            _theLeadConfig = MyLeadConfig;
            return Read(sFile);
        }

        public override IECGFormat Read(Stream input, int offset, ECGConfig cfg)
        {
            LastError = 0;
            IECGFormat ret = null;
            if (   (input != null)
                &&	input.CanRead 
                && (_nrleads != -1) 
                && (_nrsamplesperlead != -1))
            {
                RawECGFormat MyFormat = new RawECGFormat();
                MyFormat.setNrLeads(_nrleads);
                MyFormat.setNrOfSamplePerLead(_nrsamplesperlead);
                MyFormat.setLitteEndian(_littleEndian);
                MyFormat.setSampleRate(_samplerate);
                MyFormat.setECGLSBperMV(_ECGLSBperMV);
                MyFormat.setIsADCFormat(_bIsADCFormat);
                MyFormat.setLeadConfiguration(_theLeadConfig);

				if (ret.Config != null)
				{
					ret.Config.Set(cfg);

					if (!ret.Config.ConfigurationWorks())
					{
						LastError = 3;

						return null;
					}
				}

                ret = (IECGFormat)MyFormat;
                if (ret.CheckFormat(input, offset))
                {
                    LastError = (ret.Read(input, offset) << 2);
                }

                if (!ret.Works())
                {
                    LastError = 2;
                    ret = null;
                }
            }
            else
            {
                LastError = 1;
            }
            return ret;
        }
        
        public override IECGFormat Read(string file, int offset, ECGConfig cfg)
        {
            LastError = 0;
            IECGFormat ret = null;
            if (file != null)
            {
                try
                {
                    Stream input = new FileStream(file, FileMode.Open);
                    ret = Read(input, offset, cfg);
                    input.Close();
                }
                catch
                {
                    LastError = 1;
                }
            }
            return ret;
        }
        
        public override IECGFormat Read(byte[] buffer, int offset, ECGConfig cfg)
        {
            LastError = 0;
            IECGFormat ret = null;
            if (buffer != null)
            {
                ret = new RawECGFormat();

				if (ret.Config != null)
				{
					ret.Config.Set(cfg);

					if (!ret.Config.ConfigurationWorks())
					{
						LastError = 3;

						return null;
					}
				}

                if (ret.CheckFormat(buffer, offset))
                {
                    LastError = (ret.Read(buffer, offset) << 2);
                }

                if (!ret.Works())
                {
                    LastError = 2;
                    ret = null;
                }
            }
            else
            {
                LastError = 1;
            }
            return ret;
        }

        public override string getErrorMessage()
        {
            string message = null;
            switch (LastError)
            {
                case 0:
                    break;
                case 1:
                    message = "No file found";
                    break;
                case 2:
                    message = "Unknown error";
                    break;
				case 3:
					message = "Incorrect format configuration";
					break;
                default:
                    message = "ECG file type specific error";
                    break;
            }
            return message;
        }
    }
}
