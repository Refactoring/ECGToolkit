/***************************************************************************
Copyright 2008-2009, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using ECGConversion;
using ECGConversion.ECGGlobalMeasurements;
using ECGConversion.ECGSignals;

namespace ECGConversion.CSV
{
	/// <summary>
	/// Summary description for CSVFormat.
	/// </summary>
	public sealed class CSVFormat : IECGFormat, ISignal, IGlobalMeasurement
	{
		public Signals _Sigs =  null;
		public GlobalMeasurements _Mes = null;

        private bool _UseBufferedStream
		{
			get
			{
				return (_Config["Use Buffered Stream"] != null)
					&& (String.Compare(_Config["Use Buffered Stream"], "true", true) == 0);
			}
		}

        private bool _CalculateLeads
        {
            get
            {
                return (_Config["Calculate Leads"] != null)
                    && (String.Compare(_Config["Calculate Leads"], "true", true) == 0);
            }
        }

        private double _FilterBottomCutoff
        {
            get
            {
                double ret = double.NaN;

                if (double.TryParse(_Config["Filter Bottom Cutoff"], out ret))
                    return ret;

                return double.NaN;
            }
        }

        private double _FilterTopCutoff
        {
            get
            {
                double ret = double.NaN;

                if (double.TryParse(_Config["Filter Top Cutoff"], out ret))
                    return ret;

                return double.NaN;
            }
        }

        private int _FilterNumberSections
        {
            get
            {
                int ret = 2;

                if (int.TryParse(_Config["Filter Number of Sections"], out ret))
                    return ret;

                return 2;
            }
        }

		public CSVFormat()
		{
			string[]
                poss = new string[] { "Calculate Leads", "Use Buffered Stream"/*, "Filter Bottom Cutoff", "Filter Top Cutoff", "Filter Number of Sections"*/ };

			_Config = new ECGConfig(null, poss, null);

            _Config["Calculate Leads"] = "false";
			_Config["Use Buffered Stream"] = "false";
            
			Empty();
        }

        /*private bool _ConfigurationWorks()
        {
            try
            {
                string sVal;
                double
                    fValA = double.NaN,
                    fValB = double.NaN;
                int nVal;

                sVal = _Config["Filter Bottom Cutoff"];
                if ((sVal != null)
                && (sVal.Length > 0)
                && (!double.TryParse(sVal, out fValA)
                || (fValA <= 0.0)))
                    return false;

                sVal = _Config["Filter Top Cutoff"];
                if ((sVal != null)
                && (sVal.Length > 0)
                && (!double.TryParse(sVal, out fValB)
                || (fValB <= 0.0)))
                    return false;

                if (!double.IsNaN(fValA)
                && !double.IsNaN(fValB)
                && (fValA >= fValB))
                    return false;

                sVal = _Config["Filter Number of Sections"];
                if ((sVal != null)
                && (sVal.Length > 0)
                && (!int.TryParse(sVal, out nVal)
                || (nVal <= 0)))
                    return false;

                return true;
            }
            catch { }

            return false;
        }*/

		public override bool SupportsBufferedStream
		{
			get
			{
				return _UseBufferedStream;
			}
		}

        /*private Signals _ApplyFilter(Signals sigs, ref DSP.IFilter[] filters)
        {
            if (sigs != null)
            {
                if (!double.IsNaN(_FilterBottomCutoff))
                {
                    if (!double.IsNaN(_FilterTopCutoff))
                    {
                        sigs = sigs.ApplyBandpassFilter(_FilterBottomCutoff, _FilterTopCutoff, _FilterNumberSections, ref filters);
                    }
                    else
                    {
                        sigs = sigs.ApplyHighpassFilter(_FilterBottomCutoff, _FilterNumberSections, ref filters);
                    }
                }
                else if (!double.IsNaN(_FilterTopCutoff))
                {
                    sigs = sigs.ApplyLowpassFilter(_FilterTopCutoff, _FilterNumberSections, ref filters);
                }
            }

            return sigs;
        }*/

		public override int Read(Stream input, int offset)
		{
			return 1;
		}
		public override int Read(string file, int offset)
		{
			return 1;
		}
		public override int Read(byte[] buffer, int offset)
		{
			return 1;
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

			return 1;
		}
		public override int Write(Stream output)
		{
            Signals tempSigs = null;
            Signals calcSigs = null;

            try
            {
                System.IO.StreamWriter sw = new StreamWriter(output);

                if (_CalculateLeads
                && (this._Sigs != null))
                {
                    calcSigs = _Sigs.CalculateFifteenLeads();

                    if (calcSigs == null)
                        calcSigs = _Sigs.CalculateTwelveLeads();

                    if (calcSigs != null)
                    {
                        tempSigs = _Sigs;
                        _Sigs = calcSigs;
                    }
                }

                int ret = ECGConverter.ToExcelTxt(this, sw, '\t', _UseBufferedStream);

                sw.Flush();

                return ret;
            }
            catch {}

            if (tempSigs != null)
            {
                _Sigs = tempSigs;
            }

			return 1;
		}
		public override int Write(byte[] buffer, int offset)
		{
			System.IO.MemoryStream ms = null;
			
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

			return 2;
		}
		public override bool CheckFormat(Stream input, int offset)
		{
			return false;
		}
		public override bool CheckFormat(string file, int offset)
		{
			return false;
		}
		public override bool CheckFormat(byte[] buffer, int offset)
		{
			return false;
		}
		public override ECGConversion.ECGDemographics.IDemographic Demographics
		{
			get
			{
				return null;
			}
		}
		public override ECGConversion.ECGDiagnostic.IDiagnostic Diagnostics
		{
			get
			{
				return null;
			}
		}
		public override ECGConversion.ECGGlobalMeasurements.IGlobalMeasurement GlobalMeasurements
		{
			get
			{
				return this;
			}
		}
		public override ISignal Signals
		{
			get
			{
				return this;
			}
		}
		public override void Anonymous(byte type)
		{
		}
		public override int getFileSize()
		{
			return -1;
		}
		public override bool Works()
		{
			return _Sigs != null;
		}
		public override void Empty()
		{
			_Sigs = null;
		}

		#region IDisposable Members
		public override void Dispose()
		{
			base.Dispose();

			_Sigs = null;
			_Mes = null;
		}
		#endregion

		#region ISignal Members

		public int getSignals(out Signals signals)
		{
			signals = null;

			if (_Sigs != null)
			{
				signals = _Sigs.Clone();

				return 0;
			}

			return 1;
		}

		public int getSignalsToObj(Signals signals)
		{
			return 1;
		}

		public int setSignals(Signals signals)
		{
			_Sigs = signals.Clone();

			return 0;
		}

		#endregion

		#region IGlobalMeasurement Members

		public int getGlobalMeasurements(out GlobalMeasurements mes)
		{
			mes = null;

			if (_Mes != null)
				mes = _Mes.Clone();

			return mes == null ? 1 : 0;
		}

		public int setGlobalMeasurements(GlobalMeasurements mes)
		{
			if (mes != null)
			{
				_Mes = mes;

				return 0;
			}

			return 1;
		}

		#endregion
	}
}