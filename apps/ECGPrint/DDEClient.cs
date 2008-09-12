/***************************************************************************
Copyright 2008, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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

Based on article:

1..2..3 ways of integrating MATLAB with the .NET
By Emanuele Ruffaldi 
Available at: http://www.codeproject.com/KB/dotnet/matlabeng.aspx

****************************************************************************/
using System;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace ECGPrint
{
	enum TransactionType
	{
		XTYP_REGISTER = 0x00A0 |0x8000|0x0002, 
		XTYP_UNREGISTER = 0x00D0 |0x8000|0x0002,
		XTYP_ADVDATA = 0x0010|0x4000,
		XTYP_XACT_COMPLETE = 0x0080|0x8000,
		XTYP_DISCONNECT = 0x00C0|0x8000|0x0002,
		XTYP_EXECUTE = 0x0050|0x4000,
		XTYP_REQUEST = 0x00B0|0x2000,
		XTYP_POKE = 0x0090|0x4000
	}

	enum DDEErrors
	{
		NOTPROCESSED = 0x4009,
		NOERROR = 0,
		BUSY = 0x4001,
		EXECACKTIMEOUT = 0x4005,
		POKEACKTIMEOUT = 0x400b,
		DATAACKTIMEOUT = 0x4002
	}

	enum ClipboardFormats
	{
		CF_NONE = 0,
		CF_TEXT = 1,
		CF_BITMAP = 2,
		CF_METAFILEPICT = 3,
		CF_UNICODETEXT = 13
	}

	/// <summary>
	/// Summary description for DDEClient.
	/// </summary>
	public class DDEClient : IDisposable
	{
		#region DDE
		[DllImport("user32.dll")]
		private static extern int DdeInitializeW(ref int id, DDECallback cb, int afcmd, int ulres);

		[DllImport("user32.dll")]
		private static extern int DdeUninitialize(int id);

		[DllImport("user32.dll")]
		private static extern DDEErrors DdeGetLastError(int idInst);
			
		[DllImport("user32.dll")]
		private static extern IntPtr DdeConnect(int idInst, IntPtr hszService, IntPtr hszTopic, IntPtr pCC);

		[DllImport("user32.dll")]
		private static extern int DdeDisconnect(IntPtr hc);

		[DllImport("user32.dll", EntryPoint = "DdeClientTransaction", CharSet = CharSet.Auto)]
		private static extern IntPtr DdeClientTransactionString(string pData, int cbData, IntPtr hConv, IntPtr hszItem, ClipboardFormats wFmt, TransactionType wType, int dwTimeout, int pdwResult);

		[DllImport("user32.dll")]
		private static extern void DdeFreeDataHandle(IntPtr data);

		[DllImport("user32.dll", CharSet=CharSet.Unicode)]
		private static extern IntPtr DdeCreateStringHandleW(int idInst, string psz, int iCodePage);

		[DllImport("user32.dll")]
		private static extern int DdeFreeStringHandle(int idInst, IntPtr hsz);

		private delegate IntPtr DDECallback(TransactionType uType, int uFmt, IntPtr hConv, IntPtr hsz1, IntPtr hsz2, IntPtr hdata, IntPtr data1, IntPtr data2);

		// Additional function available
		//[DllImport("user32.dll")]
		//private static extern IntPtr DdeClientTransaction(IntPtr pData, int cbData, IntPtr hConv, IntPtr hszItem, ClipboardFormats wFmt, TransactionType wType, int dwTimeout, int pdwResult);

		//[DllImport("user32.dll", EntryPoint = "DdeClientTransaction", CharSet = CharSet.Ansi)]
		//private static extern IntPtr DdeClientTransactionStringA(string pData, int cbData, IntPtr hConv, IntPtr hszItem, ClipboardFormats wFmt, TransactionType wType, int dwTimeout, int pdwResult);

		//[DllImport("user32.dll", EntryPoint = "DdeClientTransaction", CharSet = CharSet.Ansi)]
		//private static extern IntPtr DdeClientTransactionXL(byte [] data, int cbData, IntPtr hConv, IntPtr hszItem, int wFmt, TransactionType wType, int dwTimeout, int pdwResult);

		//[DllImport("user32.dll")]
		//private static extern IntPtr DdeAccessData(IntPtr p, out int datasize);

		//[DllImport("user32.dll", EntryPoint = "DdeAccessData", CharSet = CharSet.Ansi )]
		//private static extern string DdeAccessDataString(IntPtr p, out int datasize);

		//[DllImport("user32.dll", CharSet=CharSet.Ansi,EntryPoint="DdeCreateDataHandle")]
		//private static extern IntPtr DdeCreateDataHandleString(int id, string data, int len, int off, IntPtr hszitem, ClipboardFormats wFmt, int flags);

		//[DllImport("user32.dll")]
		//private static extern IntPtr DdeCreateDataHandle(int id, IntPtr data, int len, int off, IntPtr hszitem, ClipboardFormats wFmt, int flags);

		//[DllImport("user32.dll", CharSet=CharSet.Unicode  )]
		//private static extern int DdeQueryString(int idInst, IntPtr hsz, StringBuilder text, int length, int cp);
		
		//[DllImport("user32.dll")]
		//private static extern int RegisterClipboardFormat(string lpszFormat);

		#endregion

		private int _InstanceID = 0;
		private string _Service = null;
		private string _Topic = null;
		private IntPtr _ChannelID = IntPtr.Zero;
		bool _LastTimeout;

		public DDEClient(string service, string topic)
		{
			DdeInitializeW(ref _InstanceID, new DDECallback(this.UselessDDECallback), 0x00008000|0x003c0000|0x00000010,0);

			if (_InstanceID == 0)
				throw new Exception("DdeInitialize failed!");

			_Service = service;
			_Topic = topic;
		}

		public bool Opened
		{
			get
			{
				return _ChannelID != IntPtr.Zero;
			}
		}

		public bool LastTimeout
		{
			get
			{
				return _LastTimeout;
			}
		}

		public bool Open()
		{
			if ((_Service == null)
			||	(_Topic == null))
				return false;

			IntPtr ipService = DdeCreateStringHandleW(_InstanceID, _Service, 1200);
			IntPtr ipTopic = DdeCreateStringHandleW(_InstanceID, _Topic, 1200);

			_ChannelID = DdeConnect(_InstanceID, ipService , ipTopic, new IntPtr(0));

			DdeFreeStringHandle(_InstanceID, ipService);
			DdeFreeStringHandle(_InstanceID, ipTopic);

			return _ChannelID != IntPtr.Zero;
		}

		public bool Execute(string command, int timeout)
		{
			if (!Opened)
				return false;

			_LastTimeout = false;
			do
			{
				IntPtr da = DdeClientTransactionString(command, command.Length*2+2, _ChannelID, IntPtr.Zero, 0, TransactionType.XTYP_EXECUTE, timeout, 0);
				if(da == IntPtr.Zero ) 
				{				
					DDEErrors e = DdeGetLastError(_InstanceID);
					if(e == DDEErrors.BUSY)
					{
						Thread.Sleep(100);
						continue;
					}
					else if(e == DDEErrors.EXECACKTIMEOUT)
					{
						_LastTimeout = true;
						break;
					}
					else
						break;
				}
				else
				{
					DdeFreeDataHandle(da);
					return true;
				}						
			} while(true);

			return false;
		}

		private IntPtr UselessDDECallback (TransactionType uType, int uFmt, IntPtr hConv, IntPtr hsz1, IntPtr hsz2, IntPtr hdata, IntPtr data1, IntPtr data2)
		{
			return IntPtr.Zero;			
		}

		public void Close()
		{
			if(_ChannelID != IntPtr.Zero)
			{
				DdeDisconnect(_ChannelID);
				_ChannelID = IntPtr.Zero;
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			Close();

			if(_InstanceID != 0)
			{
				DdeUninitialize(_InstanceID);
				_InstanceID = 0;				
			}
		}

		#endregion
	}
}
