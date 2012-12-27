/***************************************************************************
Copyright 2012, van Ettinger Information Technology, Lopik, The Netherlands

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

using ECGConversion.ISHNE;

namespace ECGConversion
{
	/// <summary>
	/// ISHNEReader class. class to read MUSEXML format.
	/// </summary>
	public class ISHNEReader : IECGReader
	{
		public ISHNEReader()
		{}
		public override IECGFormat Read(string file, int offset, ECGConfig cfg)
		{
			LastError = 0;
			IECGFormat ret = null;
			if (file != null)
			{
				try
				{
					Stream input = new FileStream(file, FileMode.Open, FileAccess.Read);
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
		public override IECGFormat Read(Stream input, int offset, ECGConfig cfg)
		{
			LastError = 0;
			IECGFormat ret = null;
			if ((input != null)
			&&	(input.CanRead))
			{
				ret = new ISHNEFormat();

				if (ret.Config != null)
				{
					ret.Config.Set(cfg);

					if (!ret.Config.ConfigurationWorks())
					{
						LastError = 3;

						return null;
					}
				}

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
		public override IECGFormat Read(byte[] buffer, int offset, ECGConfig cfg)
		{
			LastError = 0;
			IECGFormat ret = null;
			if (buffer != null)
			{
				ret = new ISHNEFormat();

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
					message = "Not a HL7 aECG file";
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
