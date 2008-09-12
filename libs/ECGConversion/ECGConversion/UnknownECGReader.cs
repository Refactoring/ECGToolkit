/***************************************************************************
Copyright (c) 2004-2007,2008, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
using ECGConversion.SCP;
using System.Reflection;

namespace ECGConversion
{
	/// <summary>
	/// UnknownECGReader class. class to read all supported formats.
	/// </summary>
	public class UnknownECGReader : IECGReader
	{
		public UnknownECGReader()
		{}

		public override IECGFormat Read(string file, int offset, ECGConfig cfg)
		{
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
			IECGFormat ret = null;
			LastError = 0;

			if ((input != null)
			&&	input.CanRead)
			{
				int i = 0;

				ECGConverter converter = ECGConverter.Instance;
				for (;i < converter.getNrSupportedFormats();i++)
				{
					if (converter.hasUnknownReaderSupport(i))
					{
						try
						{
							ret = converter.getFormat(i);

							if ((ret != null)
							&&	ret.CheckFormat(input, offset + converter.getExtraOffset(i)))
							{
								ret.Read(input, offset + converter.getExtraOffset(i));
								if (ret.Works())
								{
									break;
								}
							}
						}
						catch (Exception ex)
						{
							Console.Error.WriteLine(ex.ToString());
						}

						ret = null;
					}
				}

				if (i == converter.getNrSupportedFormats())
				{
					LastError = 2;
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
			IECGFormat ret = null;
			LastError = 0;

			if (buffer != null)
			{
				int i = 0;

				ECGConverter converter = ECGConverter.Instance;
				for (;i < converter.getNrSupportedFormats();i++)
				{
					if (converter.hasUnknownReaderSupport(i))
					{
						try
						{
							ret = converter.getFormat(i);

							if ((ret != null)
							&&	ret.CheckFormat(buffer, offset + converter.getExtraOffset(i)))
							{
								ret.Read(buffer, offset + converter.getExtraOffset(i));
								if (ret.Works())
								{
									break;
								}
							}
						}
						catch (Exception ex)
						{
							Console.Error.WriteLine(ex.ToString());
						}

						ret = null;
					}
				}

				if (i == converter.getNrSupportedFormats())
				{
					LastError = 2;
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
					message = "Not any of the supported ECG file types";
				break;
				default:
					message = "ECG file type specific error";
				break;
			}
			return message;
		}
	}
}
