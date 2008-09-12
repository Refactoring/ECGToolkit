/***************************************************************************
Copyright 2007-2008, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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

namespace ECGConversion
{
	/// <summary>
	/// class describing an ECGConverter entry for each plugin.
	/// </summary>
	public sealed class ECGPlugin
	{
		private string _Name = ""; // case insensitive
		private string _Extension = null;
		private Type _FormatType = null;
		private Type _ReaderType = null;
		private bool _UnkownReaderSupport = false;
		private string _ConvertFunction = "";
		private int _ExtraOffset = 0;

		/// <summary>
		/// constructor for plugin entry.
		/// </summary>
		public ECGPlugin()
		{}

		/// <summary>
		/// constructor to make a simple plugin entry.
		/// </summary>
		/// <param name="name">name of plugin (case insensitive)</param>
		/// <param name="format">format object</param>
		/// <param name="reader">reader object</param>
		/// <param name="unkownReaderSupport">support for unkown reader</param>
		public ECGPlugin(string name, string ext, Type formatType, Type readerType, bool unkownReaderSupport)
		{
			SetPlugin(name, ext, formatType, readerType, unkownReaderSupport);
		}

		/// <summary>
		/// constructor to make a simple plugin entry.
		/// </summary>
		/// <param name="name">name of plugin (case insensitive)</param>
		/// <param name="format">format object</param>
		/// <param name="reader">reader object</param>
		/// <param name="unkownReaderSupport">support for unkown reader</param>
		public ECGPlugin(string name, string ext, Type formatType, Type readerType, bool unkownReaderSupport, int extraOffset)
		{
			SetPlugin(name, ext, formatType, readerType, unkownReaderSupport, extraOffset);
		}

		/// <summary>
		/// constructor to make a simple plugin entry.
		/// </summary>
		/// <param name="name">name of plugin (case insensitive)</param>
		/// <param name="format">format object</param>
		/// <param name="reader">reader object</param>
		/// <param name="unkownReaderSupport">support for unkown reader</param>
		/// <param name="convertFunction">possible static function to call for converting</param>
		public ECGPlugin(string name, string ext, Type formatType, Type readerType, bool unkownReaderSupport, string convertFunction)
		{
			SetPlugin(name, ext, formatType, readerType, unkownReaderSupport, convertFunction);
		}

		/// <summary>
		/// constructor to make a simple plugin entry.
		/// </summary>
		/// <param name="name">name of plugin (case insensitive)</param>
		/// <param name="format">format object</param>
		/// <param name="reader">reader object</param>
		/// <param name="unkownReaderSupport">support for unkown reader</param>
		/// <param name="convertFunction">possible static function to call for converting</param>
		public ECGPlugin(string name, string ext, Type formatType, Type readerType, bool unkownReaderSupport, string convertFunction, int extraOffset)
		{
			SetPlugin(name, ext, formatType, readerType, unkownReaderSupport, convertFunction, extraOffset);
		}

		/// <summary>
		/// Set the values of the plugin entry
		/// </summary>
		/// <param name="name">name of plugin (case insensitive)</param>
		/// <param name="format">format object</param>
		/// <param name="reader">reader object</param>
		/// <param name="unkownReaderSupport">support for unkown reader</param>
		/// <param name="convertFunction">possible static function to call for converting</param>
		/// <param name="extraOffset">extra offset for a file of this kind</param>
		/// <returns>0 on successful</returns>
		public int SetPlugin(string name, string ext, Type formatType, Type readerType, bool unkownReaderSupport, string convertFunction, int extraOffset)
		{
			int ret = SetPlugin(name, ext, formatType, readerType, unkownReaderSupport);

			if (ret == 0)
			{
				_ConvertFunction = convertFunction;
				_ExtraOffset = extraOffset;
			}

			return ret;
		}

		/// <summary>
		/// Set the values of the plugin entry
		/// </summary>
		/// <param name="name">name of plugin (case insensitive)</param>
		/// <param name="format">format object</param>
		/// <param name="reader">reader object</param>
		/// <param name="unkownReaderSupport">support for unkown reader</param>
		/// <param name="convertFunction">possible static function to call for converting</param>
		/// <returns>0 on successful</returns>
		public int SetPlugin(string name, string ext, Type formatType, Type readerType, bool unkownReaderSupport, string convertFunction)
		{
			int ret = SetPlugin(name, ext, formatType, readerType, unkownReaderSupport);

			if (ret == 0)
				_ConvertFunction = convertFunction;

			return ret;
		}

		/// <summary>
		/// Set the values of the plugin entry
		/// </summary>
		/// <param name="name">name of plugin (case insensitive)</param>
		/// <param name="format">format object</param>
		/// <param name="reader">reader object</param>
		/// <param name="unkownReaderSupport">support for unkown reader</param>
		/// <param name="convertFunction">possible static function to call for converting</param>
		/// <param name="extraOffset">extra offset for a file of this kind</param>
		/// <returns>0 on successful</returns>
		public int SetPlugin(string name, string ext, Type formatType, Type readerType, bool unkownReaderSupport, int extraOffset)
		{
			int ret = SetPlugin(name, ext, formatType, readerType, unkownReaderSupport);

			if (ret == 0)
				_ExtraOffset = extraOffset;

			return ret;
		}

		/// <summary>
		/// Set the values of the plugin entry.
		/// </summary>
		/// <param name="name">name of plugin (case insensitive)</param>
		/// <param name="format">format object</param>
		/// <param name="reader">reader object</param>
		/// <param name="unkownReaderSupport">support for unkown reader</param>
		/// <returns>0 on successful</returns>
		public int SetPlugin(string name, string ext, Type formatType, Type readerType, bool unkownReaderSupport)
		{
			if ((name.Length == 0)
			||	(formatType == null)
			||	!formatType.IsSubclassOf(typeof(IECGFormat)))
				return 1;

			if ((readerType != null)
			&&	!readerType.IsSubclassOf(typeof(IECGReader)))
				return 2;

			_Name = name;
			_Extension = ext;
			_FormatType = formatType;
			_ReaderType = readerType;
			_UnkownReaderSupport = unkownReaderSupport;
			_ConvertFunction = "";

			return 0;
		}

		/// <summary>
		/// Function get name of supported plugin.
		/// </summary>
		public string Name
		{
			get
			{
				return _Name;
			}
		}

		/// <summary>
		/// Extension of the supported plugin
		/// </summary>
		public string Extension
		{
			get
			{
				return _Extension;
			}
		}

		/// <summary>
		/// Conver to this supported format.
		/// </summary>
		/// <param name="src">source format.</param>
		/// <param name="cfg">configuration to set format to</param>
		/// <param name="dst">output of destination format.</param>
		/// <returns>0 on successful</returns>
		public int Convert(IECGFormat src, ECGConfig cfg, out IECGFormat dst)
		{
			dst = null;

			if (_FormatType == null)
				return 1;

			if (_ConvertFunction.Length == 0)
			{
				dst = (IECGFormat) Activator.CreateInstance(_FormatType);

				if ((cfg != null)
				&&	((dst.Config == null)
				||	 !dst.Config.Set(cfg)))
					return 2;

				return ECGConverter.Convert(src, dst) << 2;
			}

			try
			{
				object[] args = new object[] {src, cfg, null};

				int ret = (int) _FormatType.GetMethod(_ConvertFunction).Invoke(null, args);

				if (args[2] != null)
					dst = (IECGFormat) args[2];

				return ret << 2;
			}
			catch
			{
			}

			return 1;
		}

		/// <summary>
		/// Get a format object of supported format
		/// </summary>
		/// <returns>a format object</returns>
		public IECGFormat getFormat()
		{
			return (IECGFormat) Activator.CreateInstance(_FormatType);
		}

		/// <summary>
		/// Get a type of supported format
		/// </summary>
		/// <returns>a format object</returns>
		public Type getType()
		{
			return _FormatType;
		}

		/// <summary>
		/// Get a reader object associated with supported format.
		/// </summary>
		/// <returns>a reader object</returns>
		public IECGReader getReader()
		{

			return (_ReaderType != null)
				?	(IECGReader) Activator.CreateInstance(_ReaderType)
				:	null;
		}

		/// <summary>
		/// Property to check whether plugin entry has got support for unknownreader.
		/// </summary>
		public bool hasUnknownReaderSupport
		{
			get
			{
				return _UnkownReaderSupport;
			}
		}

		/// <summary>
		/// Property for extra offset when detecting
		/// </summary>
		public int ExtraOffset
		{
			get
			{
				return _ExtraOffset;
			}
		}
	}
}
