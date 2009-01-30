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

		public CSVFormat()
		{
			Empty();
		}

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
			try
			{
				System.IO.StreamWriter sw = new StreamWriter(output);

				int ret = ECGConverter.ToExcelTxt(this, sw, '\t');

				sw.Flush();

				return ret;
			}
			catch
			{
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