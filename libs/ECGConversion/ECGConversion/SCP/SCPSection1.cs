/***************************************************************************
Copyright 2013-2014, van Ettinger Information Technology, Lopik, The Netherlands
Copyright 2004-2005,2008-2009, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
using System.Runtime.InteropServices;
using Communication.IO.Tools;
using ECGConversion.ECGDemographics;

namespace ECGConversion.SCP
{
	/// <summary>
	/// Class contains section 1 (Header Information).
	/// </summary>
	public class SCPSection1 : SCPSection, IDemographic
	{
		// Fields that must be made empty for anonymous. (must be sorted from low to high)
		private static byte[] _AnonymousFields = {0, 1, 2, 3, 5, 30, 31};
		// Defined in SCP.
		private static byte[] _MustBePresent = {2, 14, 25, 26}; // defined in paragraph 5.4.3.1 of SCP
		private static byte[] _MultipleInstanceFields = {10, 13, 30, 32, 35}; // Must be sorted
		private static ushort _MaximumFieldLength = 64;
		private static byte[] _MaximumLengthExceptions = {13, 14, 15, 30, 35}; // Must be sorted
		private static ushort _ExceptionsMaximumLength = 256; // should be 80, but some scp file doen't use this maximum. apparantly 128 wasn't enough as well
		private static byte _ManufactorField = 0xc8;
		private static ushort _SectionID = 1;
		private static byte _DemographicTerminator = 0xff;

		// ResizeSpeed for the array to store the Fields.
		private static int _ResizeSpeed = 8;

		// Part of the stored Data Structure.
		private int _NrFields = 0;
		private SCPHeaderField[] _Fields = null;
		protected override int _Read(byte[] buffer, int offset)
		{
			Init();
			int end = offset - Size + Length;
			while (offset < end)
			{
				SCPHeaderField field = new SCPHeaderField();
				field.Tag = (byte) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(field.Tag), true);
				offset += Marshal.SizeOf(field.Tag);
				if (field.Tag == _DemographicTerminator)
				{
					break;
				}
				else if ((offset + 2) > end)
				{
					_Empty();
					return 0x1;
				}
				field.Length = (ushort) BytesTool.readBytes(buffer, offset, Marshal.SizeOf(field.Length), true);
				offset += Marshal.SizeOf(field.Length);
				if ((offset + field.Length) > end)
				{
					_Empty();
					return 0x2;
				}
				field.Value = new byte[field.Length];
				offset += BytesTool.copy(field.Value, 0, buffer, offset, field.Length);
				Insert(field);
			}
			return 0x0;
		}
		protected override int _Write(byte[] buffer, int offset)
		{
			for (int loper=0;loper < _NrFields;loper++)
			{
				BytesTool.writeBytes(_Fields[loper].Tag , buffer, offset, Marshal.SizeOf(_Fields[loper].Tag), true);
				offset += Marshal.SizeOf(_Fields[loper].Tag);
				BytesTool.writeBytes(_Fields[loper].Length , buffer, offset, Marshal.SizeOf(_Fields[loper].Length), true);
				offset += Marshal.SizeOf(_Fields[loper].Length);
				offset += BytesTool.copy(buffer, offset, _Fields[loper].Value, 0, _Fields[loper].Length);
			}
			return 0x0;
		}
		protected override void _Empty()
		{
			_NrFields = 0;
			_Fields = null;
		}
		public override ushort getSectionID()
		{
			return _SectionID;
		}
		protected override int _getLength()
		{
			if (Works())
			{
				int sum = 0;
				for (int loper=0;loper < _NrFields;loper++)
				{
					sum += (Marshal.SizeOf(_Fields[loper].Tag) + Marshal.SizeOf(_Fields[loper].Length) + _Fields[loper].Length);
				}
				return ((sum % 2) == 0 ? sum : sum + 1);
			}
			return 0;
		}
		public override bool Works()
		{
			if (CheckInstances())
			{
				for (int loper=0;loper < _NrFields;loper++)
				{
					if ((_Fields[loper] == null)
					||  ((_Fields[loper].Value == null) && (_Fields[loper].Length != 0))
					||  ((_Fields[loper].Value != null) && (_Fields[loper].Value.Length != _Fields[loper].Length))
					||  ((_Fields[loper].Length > _MaximumFieldLength) && (_Fields[loper].Tag < _ManufactorField) && (!isException(_MaximumLengthExceptions, _Fields[loper].Tag) || (_Fields[loper].Length > _ExceptionsMaximumLength))))
					{
						return false;
					}
				}
				for (int loper=0;loper < _MustBePresent.Length;loper++)
				{
					if (_SearchField(_MustBePresent[loper]) < 0)
					{
						return false;
					}
				}
				return (_Fields[_NrFields - 1].Tag == 0xff) &&  (_Fields[_NrFields - 1].Length == 0);
			}
			return false;
		}
		/// <summary>
		/// Function to initialize a section 1. Only needed when not reading from buffer.
		/// </summary>
		public void Init()
		{
			_Empty();
			_Fields = new SCPHeaderField[_ResizeSpeed];
			_Fields[_NrFields++] = new SCPHeaderField(_DemographicTerminator, 0, null);
		}
		/// <summary>
		/// Function to insert a field into section.
		/// </summary>
		/// <param name="field">field to insert</param>
		/// <returns>0 on success</returns>
		public int Insert(SCPHeaderField field)
		{
			if ((field != null)
			&&  (field.Tag != _DemographicTerminator)
			&&	(_Fields != null)
			&&  (_NrFields <= _Fields.Length)
			&&  (_Fields[_NrFields - 1].Tag == _DemographicTerminator))
			{
				if ((field.Length == 0)
				||	((field.Value != null)
				&&	 (field.Length <= field.Value.Length)))
				{
					int p1 = _SearchField(field.Tag);
					// If field exist must override or can be an multiple instances.
					if (p1 >= 0)
					{
						// If multiple instaces field, add field as last of this kind of field.
						if (isException(_MultipleInstanceFields, field.Tag))
						{
							// Resize if space is needed.
							if (_NrFields == _Fields.Length)
							{
								Resize();
							}
							// Find last of this kind.
							for (;(p1 < _NrFields) && (_Fields[p1].Tag == field.Tag);p1++);
							// Make space in array for field.
							for (int loper = _NrFields;loper > p1;loper--)
							{
								_Fields[loper] = _Fields[loper - 1];
							}
							_Fields[p1] = field;
							_NrFields++;
						}
						else
						{
							// Overwrite existing field.
							_Fields[p1] = field;
						}
					}
					else
					{
						// Resize if space is needed
						if (_NrFields == _Fields.Length)
						{
							Resize();
						}
						int p2 = _InsertSearch(field.Tag);
						// Make space to insert.
						for (int loper = _NrFields;loper > p2;loper--)
						{
							_Fields[loper] = _Fields[loper - 1];
						}
						_Fields[p2] = field;
						_NrFields++;
					}
					return 0x0;
				}
				return 0x2;
			}
			return 0x1;
		}
		/// <summary>
		/// Function to remove a certain field from section.
		/// </summary>
		/// <param name="tag">tag of field.</param>
		/// <returns>0 on success</returns>
		public int Remove(byte tag)
		{
			if ((tag != _DemographicTerminator)
			&&  (_Fields != null)
			&&  (_NrFields <= _Fields.Length))
			{
				int p = _SearchField(tag);
				if (p >= 0)
				{
					_NrFields--;
					for (;p < _NrFields;p++)
					{
						_Fields[p] = _Fields[p+1];
					}
					return 0x0;
				}
				return 0x2;
			}
			return 0x1;
		}
		/// <summary>
		/// Function to resize the space for header fields.
		/// </summary>
		public void Resize()
		{
			SCPHeaderField[] temp = new SCPHeaderField[_NrFields + _ResizeSpeed];
			for (int loper=0;loper < _NrFields;loper++)
			{
				temp[loper] = _Fields[loper];
			}
			_Fields = temp;
		}
		/// <summary>
		/// Function to get a field from this section
		/// </summary>
		/// <param name="tag">tag to search for</param>
		/// <returns></returns>
		public SCPHeaderField GetField(byte tag)
		{			
			int pos = _SearchField(tag);
			
			if ((_Fields != null)
			&&	(pos >= 0)
			&&	(pos < _Fields.Length))
			{
				return _Fields[pos];
			}
			
			return null;
		}
		/// <summary>
		/// Function to search for a field with a certain tag.
		/// </summary>
		/// <param name="tag">tag to search for</param>
		/// <returns>position of this field</returns>
		private int _SearchField(byte tag)
		{
			int l = 0;
			int h = _NrFields-1;
			int m = (h >> 1);
			while (l <= h && _Fields[m].Tag != tag) 
			{
				if (tag > _Fields[m].Tag)
				{
					l = m + 1;
				} 
				else 
				{
					h = m - 1;
				}
				m = ((l + h) >> 1);
			}
			if ((m >= 0) && (m < _NrFields) && (_Fields[m].Tag == tag))
			{
				return m;
			}
			return -1;
		}
		/// <summary>
		/// Function to find position to insert a field with a certain tag.
		/// </summary>
		/// <param name="tag">tag to search on.</param>
		/// <returns>position to insert</returns>
		private int _InsertSearch(byte tag)
		{
			int l = 0, h = _NrFields;
			while (l < h)
			{
				int m = (l + h) / 2;
				if (_Fields[m].Tag < tag)
					l = m + 1;
				else
					h = m;
			}
			return l;
		}
		/// <summary>
		/// Function to check wheter the used fields are indeed sorted.
		/// </summary>
		private bool CheckInstances()
		{
			if ((_Fields != null)
			&&  (_NrFields > 0)
			&&  (_NrFields <= _Fields.Length)
			&&  (_Fields[0] != null))
			{
				byte prev = _Fields[0].Tag;
				for (int loper=1;loper < _NrFields;loper++)
				{
					if ((prev == _Fields[loper].Tag)
					&&  !isException(_MultipleInstanceFields, prev))
					{
						return false;
					}
					prev = _Fields[loper].Tag;
				}
				return true;
			}
			return false;
		}
		/// <summary>
		/// Function to check for exception case.
		/// </summary>
		/// <param name="condition">condition</param>
		/// <param name="tag">value of tag</param>
		/// <returns>is exception then true</returns>
		private static bool isException(byte[] condition, byte tag)
		{
			if (condition == null)
			{
				return false;
			}
			int l = 0;
			int h = condition.Length - 1;
			int m = (h >> 1);
			while (l <= h && condition[m] != tag) 
			{
				if (tag > condition[m])
				{
					l = m + 1;
				} 
				else 
				{
					h = m - 1;
				}
				m = ((l + h) >> 1);
			}
			return (m >= 0) && (m < condition.Length) && (condition[m] == tag);	
		}
		/// <summary>
		/// Function to anonymous this section.
		/// </summary>
		/// <param name="type">value to empty with</param>
		public void Anonymous(byte type)
		{
			for (int loper=0;loper < _NrFields;loper++)
			{
				if (isException(_AnonymousFields, _Fields[loper].Tag)
				&&  (_Fields[loper].Value != null)
				&&  (_Fields[loper].Length <= _Fields[loper].Value.Length))
				{
					if (_Fields[loper].Tag == 5)
					{
						SCPDate date2 = new SCPDate();
						date2.Read(_Fields[loper].Value, 0);
						date2.Day = 1;
						date2.Month = 1;
						date2.Write(_Fields[loper].Value, 0);
					}
					else
					{
						BytesTool.emptyBuffer(_Fields[loper].Value, 0, _Fields[loper].Length-1, type);
					}
				}
			}
		}
		/// <summary>
		/// Get encoding for text from language support code.
		/// </summary>
		/// <returns>used encoding</returns>
		public System.Text.Encoding getLanguageSupportCode()
		{
			System.Text.Encoding enc;

			getLanguageSupportCode(out enc);

			return enc;
		}
		/// <summary>
		/// Get encoding for text from language support code.
		/// </summary>
		/// <param name="enc">used encoding</param>
		/// <returns>0 if successfull</returns>
		public int getLanguageSupportCode(out System.Text.Encoding enc)
		{
			enc = System.Text.Encoding.ASCII;

			int p = _SearchField(14);
			if ((p >= 0)
			&&	(_Fields[p] != null)
			&&  (_Fields[p].Value != null)
			&&  (_Fields[p].Length <= _Fields[p].Value.Length)
			&&  (_Fields[p].Length > 16))
			{
				byte lsc = _Fields[p].Value[16];

				if ((lsc & 0x1) == 0x0)
				{
					return 0;
				}
				else if ((lsc & 0x3) == 0x1)
				{
					enc = System.Text.Encoding.GetEncoding("ISO-8859-1");

					return 0;
				}
				else
				{
					string encName = null;

					switch (lsc)
					{
						case 0x03: encName = "ISO-8859-2"; break;
						case 0x0b: encName = "ISO-8859-4"; break;
						case 0x13: encName = "ISO-8859-5"; break;
						case 0x1b: encName = "ISO-8859-6"; break;
						case 0x23: encName = "ISO-8859-7"; break;
						case 0x2b: encName = "ISO-8859-8"; break;
						case 0x33: encName = "ISO-8859-11"; break;
						case 0x3b: encName = "ISO-8859-15"; break;
						case 0x07: encName = "utf-16"; break; //case 0x07: encName = "ISO-60646"; break;
						case 0x0f: //encName = "JIS X0201-1976";
						case 0x17: //encName = "JIS X0208-1997";
						case 0x1f: //encName = "JIS X0212-1990";
							encName = "EUC-JP";
							break;
						case 0x27: encName = "gb2312"; break;
						case 0x2f: encName = "ks_c_5601-1987"; break;
						default: break;
					}

					if (encName != null)
					{
						enc = System.Text.Encoding.GetEncoding(encName);

						return 0;
					}
				}
			}
			return 1;
		}
		/// <summary>
		/// Set language support code based on encoding.
		/// </summary>
		/// <param name="enc">encoding to set lsc with.</param>
		/// <returns>0 if successfull</returns>
		public int setLanguageSupportCode(System.Text.Encoding enc)
		{
			int ret = 0;
			byte lsc = 0;

			switch (enc.CodePage)
			{
				case 20127: break;
				case 28591: lsc = 0x01; break;
				case 28592: lsc = 0x03; break;
				case 28594: lsc = 0x0b; break;
				case 28595: lsc = 0x13; break;
				case 28596: lsc = 0x1b; break;
				case 28597: lsc = 0x23; break;
				case 28598: lsc = 0x2b; break;
				case 28603: lsc = 0x33; break;
				case 28605: lsc = 0x3b; break;
				case  1200: lsc = 0x07; break;
				case 20932: lsc = 0x1f; break;
				case 20936: lsc = 0x27; break;
				case   949: lsc = 0x2f; break;
				default: ret = 1; break;
			}

			int p = _SearchField(14);
			if ((p >= 0)
			&&	(_Fields[p] != null)
			&&  (_Fields[p].Value != null)
			&&  (_Fields[p].Length <= _Fields[p].Value.Length)
			&&  (_Fields[p].Length > 16))
			{
				_Fields[p].Value[16] = lsc;
			}
			else if (ret != 1)
			{
				ret = 2;
			}

			p = _SearchField(15);
			if ((p >= 0)
			&&	(_Fields[p] != null)
			&&  (_Fields[p].Value != null)
			&&  (_Fields[p].Length <= _Fields[p].Value.Length)
			&&  (_Fields[p].Length > 16))
			{
				_Fields[p].Value[16] = lsc;
			}


			return ret;
		}
		/// <summary>
		/// Function to get Protocol Compatability Level.
		/// </summary>
		/// <param name="pc">Protocol Compatability Level</param>
		/// <returns>0 on succes</returns>
		public int getProtocolCompatibilityLevel(out ProtocolCompatibility pc)
		{
			pc = 0;
			int p = _SearchField(14);
			if ((p >= 0)
			&&	(_Fields[p] != null)
			&&  (_Fields[p].Value != null)
			&&  (_Fields[p].Length <= _Fields[p].Value.Length)
			&&  (_Fields[p].Length > 15))
			{
				pc = (ProtocolCompatibility) _Fields[p].Value[15];
				return 0;
			}
			return 1;
		}
		/// <summary>
		/// Function to set Protocol Compatability Level.
		/// </summary>
		/// <param name="pc">Protocol Compatability Level</param>
		/// <returns>0 on succes</returns>
		public int setProtocolCompatibilityLevel(ProtocolCompatibility pc)
		{
			int p = _SearchField(14);
			if ((p >= 0)
			&&	(_Fields[p] != null)
			&&  (_Fields[p].Value != null)
			&&  (_Fields[p].Length <= _Fields[p].Value.Length)
			&&  (_Fields[p].Length > 15))
			{
				_Fields[p].Value[15] = (byte) pc;
				return 0;
			}
			return 1;
		}
		/// <summary>
		/// Function to get a text from a certain tag.
		/// </summary>
		/// <param name="tag">id of tag</param>
		/// <param name="text">a string</param>
		/// <returns>0 on success</returns>
		public string getText(byte tag)
		{
			int p = _SearchField(tag);
			if ((p >= 0)
			&&  (_Fields[p].Value != null)
			&&  (_Fields[p].Length <= _Fields[p].Value.Length))
			{
				return BytesTool.readString(_Encoding, _Fields[p].Value, 0, _Fields[p].Length);;
			}
			return null;
		}
		/// <summary>
		/// Function to set a text from a cetain tag.
		/// </summary>
		/// <param name="tag">id of tag</param>
		/// <param name="text">a string</param>
		/// <returns>0 on success</returns>
		public int setText(byte tag, string text)
		{
			if (text != null)
			{
				SCPHeaderField field = new SCPHeaderField();
				field.Tag = tag;
				field.Length = (ushort) (text.Length >= _MaximumFieldLength ? _MaximumFieldLength :  text.Length + 1);
				field.Value = new byte[field.Length];
				BytesTool.writeString(_Encoding, text, field.Value, 0, field.Length);
				return Insert(field) << 1;
			}
			return 1;
		}
		// Getting Demographics information
		public string LastName
		{
			get {return getText(0);}
			set {setText(0, value);}
		}
		public string FirstName
		{
			get {return getText(1);}
			set {setText(1, value);}
		}
		public string PatientID
		{
			get {return getText(2);}
			set {setText(2, value);}
		}
		public string SecondLastName
		{
			get {return getText(3);}
			set {setText(3, value);}
		}
		public string PrefixName
		{
			get {return null;}
			set {}
		}
		public string SuffixName
		{
			get {return null;}
			set {}
		}
		public int getPatientAge(out ushort val, out AgeDefinition def)
		{
			val = 0; def = AgeDefinition.Unspecified;
			int p = _SearchField(4);
			if ((p >= 0)
			&&  (_Fields[p].Length == 3)
			&&  (_Fields[p].Value != null)
			&&  (_Fields[p].Length <= _Fields[p].Value.Length))
			{
				val = (ushort) BytesTool.readBytes(_Fields[p].Value, 0, Marshal.SizeOf(val), true);
				switch (BytesTool.readBytes(_Fields[p].Value, Marshal.SizeOf(val), 1, true))
				{
					case 1:
						def = AgeDefinition.Years;
					break;
					case 2:
						def = AgeDefinition.Months;
					break;
					case 3:
						def = AgeDefinition.Weeks;
					break;
					case 4:
						def = AgeDefinition.Days;
					break;
					case 5:
						def = AgeDefinition.Hours;
					break;
					default:
						def = AgeDefinition.Unspecified;
					break;
				}
				return (val == 0) && (def == AgeDefinition.Unspecified) ? 2 : 0;
			}
			return 1;
		}
		public int setPatientAge(ushort val, AgeDefinition def)
		{
			SCPHeaderField field = new SCPHeaderField();
			field.Tag = 4;
			field.Length = (ushort) (Marshal.SizeOf(val) + Marshal.SizeOf(typeof(byte)));
			field.Value = new byte[field.Length];
			BytesTool.writeBytes(val, field.Value, 0, Marshal.SizeOf(val), true);
			BytesTool.writeBytes((byte)def, field.Value, Marshal.SizeOf(val), Marshal.SizeOf(typeof(byte)), true);
			return Insert(field) << 1;
		}
		public Date PatientBirthDate
		{
			get
			{
				int p = _SearchField(5);
				if ((p >= 0)
				&&  (_Fields[p].Length == 4)
				&&  (_Fields[p].Value != null)
				&&  (_Fields[p].Length <= _Fields[p].Value.Length))
				{
					SCPDate scpdate = new SCPDate();
					scpdate.Read(_Fields[p].Value, 0);
					Date date = new Date();
					date.Year = scpdate.Year;
					date.Month = scpdate.Month;
					date.Day = scpdate.Day;
					return date;
				}
				return null;
			}
			set
			{
				if ((value != null)
				&&  (value.isExistingDate()))
				{
					SCPHeaderField field = new SCPHeaderField();
					field.Tag = 5;
					field.Length = (ushort) SCPDate.Size;
					field.Value = new byte[field.Length];
					SCPDate scpdate = new SCPDate();
					scpdate.Year = value.Year;
					scpdate.Month = value.Month;
					scpdate.Day = value.Day;
					scpdate.Write(field.Value, 0);

					Insert(field);
				}
			}
		}
		public int getPatientHeight(out ushort val, out HeightDefinition def)
		{
			val = 0; def = HeightDefinition.Unspecified;
			int p = _SearchField(6);
			if ((p >= 0)
			&&  (_Fields[p].Length == 3)
			&&  (_Fields[p].Value != null)
			&&  (_Fields[p].Length <= _Fields[p].Value.Length))
			{
				val = (ushort) BytesTool.readBytes(_Fields[p].Value, 0, Marshal.SizeOf(val), true);
				switch (BytesTool.readBytes(_Fields[p].Value, Marshal.SizeOf(val), 1, true))
				{
					case 1:
						def = HeightDefinition.Centimeters;
					break;
					case 2:
						def = HeightDefinition.Inches;
					break;
					case 3:
						def = HeightDefinition.Millimeters;
					break;
					default:
						def = HeightDefinition.Unspecified;
					break;
				}
				return (val == 0) && (def == HeightDefinition.Unspecified) ? 2 : 0;
			}
			return 1;
		}
		public int setPatientHeight(ushort val, HeightDefinition def)
		{
			SCPHeaderField field = new SCPHeaderField();
			field.Tag = 6;
			field.Length = (ushort) (Marshal.SizeOf(val) + Marshal.SizeOf(typeof(byte)));
			field.Value = new byte[field.Length];
			BytesTool.writeBytes(val, field.Value, 0, Marshal.SizeOf(val), true);
			BytesTool.writeBytes((byte)def, field.Value, Marshal.SizeOf(val), 1, true);
			return Insert(field) << 1;
		}
		public int getPatientWeight(out ushort val, out WeightDefinition def)
		{
			val = 0; def = WeightDefinition.Unspecified;
			int p = _SearchField(7);
			if ((p >= 0)
			&&  (_Fields[p].Length == 3)
			&&  (_Fields[p].Value != null)
			&&  (_Fields[p].Length <= _Fields[p].Value.Length))
			{
				val = (ushort) BytesTool.readBytes(_Fields[p].Value, 0, Marshal.SizeOf(val), true);
				switch (BytesTool.readBytes(_Fields[p].Value, Marshal.SizeOf(val), 1, true))
				{
					case 1:
						def = WeightDefinition.Kilogram;
					break;
					case 2:
						def = WeightDefinition.Gram;
					break;
					case 3:
						def = WeightDefinition.Pound;
					break;
					case 4:
						def = WeightDefinition.Ounce;
					break;
					default:
						def = WeightDefinition.Unspecified;
					break;
				}
				return (val == 0) && (def == WeightDefinition.Unspecified) ? 2 : 0;
			}
			return 1;
		}
		public int setPatientWeight(ushort val, WeightDefinition def)
		{
			SCPHeaderField field = new SCPHeaderField();
			field.Tag = 7;
			field.Length = (ushort) (Marshal.SizeOf(val) + Marshal.SizeOf(typeof(byte)));
			field.Value = new byte[field.Length];
			BytesTool.writeBytes(val, field.Value, 0, Marshal.SizeOf(val), true);
			BytesTool.writeBytes((byte)def, field.Value, Marshal.SizeOf(val), Marshal.SizeOf(typeof(byte)), true);
			return Insert(field) << 1;
		}
		public Sex Gender
		{
			get
			{
				int p = _SearchField(8);
				if ((p >= 0)
				&&  (_Fields[p].Length == 1)
				&&  (_Fields[p].Value != null)
				&&  (_Fields[p].Length <= _Fields[p].Value.Length))
				{
					switch (BytesTool.readBytes(_Fields[p].Value, 0, 1, true))
					{
						case 1:
							return Sex.Male;
						case 2:
							return Sex.Female;
						default:
							return Sex.Unspecified;
					}
				}
				return Sex.Null;
			}
			set
			{
				if (value != Sex.Null)
				{
					SCPHeaderField field = new SCPHeaderField();
					field.Tag = 8;
					field.Length = (ushort) Marshal.SizeOf(typeof(byte));
					field.Value = new byte[field.Length];
					BytesTool.writeBytes((byte)value, field.Value, 0, Marshal.SizeOf(typeof(byte)), true);
					Insert(field);
				}
			}
		}
		public Race PatientRace
		{
			get
			{
				int p = _SearchField(9);
				if ((p >= 0)
				&&  (_Fields[p].Length == 1)
				&&  (_Fields[p].Value != null)
				&&  (_Fields[p].Length <= _Fields[p].Value.Length))
				{
					switch (BytesTool.readBytes(_Fields[p].Value, 0, 1, true))
					{
						case 1:
							return Race.Caucasian;
						case 2:
							return Race.Black;
						case 3:
							return Race.Oriental;
						default:
							return Race.Unspecified;
					}
				}
				return Race.Null;
			}
			set
			{
				if (value != Race.Null)
				{
					SCPHeaderField field = new SCPHeaderField();
					field.Tag = 9;
					field.Length = (ushort) Marshal.SizeOf(typeof(byte));
					field.Value = new byte[field.Length];
					BytesTool.writeBytes((byte)value, field.Value, 0, Marshal.SizeOf(typeof(byte)), true);
					Insert(field);
				}
			}
		}
		public AcquiringDeviceID AcqMachineID
		{
			get
			{
				int p = _SearchField(14);
				if ((p >= 0)
				&&  (_Fields[p].Length >= 36)
				&&  (_Fields[p].Value != null)
				&&  (_Fields[p].Length <= _Fields[p].Value.Length))
				{
					AcquiringDeviceID id = new AcquiringDeviceID();
					int offset = 0;
					id.InstitutionNr = (ushort) BytesTool.readBytes(_Fields[p].Value, offset, Marshal.SizeOf(id.InstitutionNr), true);
					offset += Marshal.SizeOf(id.InstitutionNr);
					id.DepartmentNr = (ushort) BytesTool.readBytes(_Fields[p].Value, offset, Marshal.SizeOf(id.DepartmentNr), true);
					offset += Marshal.SizeOf(id.DepartmentNr);
					id.DeviceID = (ushort) BytesTool.readBytes(_Fields[p].Value, offset, Marshal.SizeOf(id.DeviceID), true);
					offset += Marshal.SizeOf(id.DeviceID);
					id.DeviceType = (byte) BytesTool.readBytes(_Fields[p].Value, offset, Marshal.SizeOf(id.DeviceType), true);
					offset += Marshal.SizeOf(id.DeviceType);
					id.ManufactorID = (byte) BytesTool.readBytes(_Fields[p].Value, offset, Marshal.SizeOf(id.ManufactorID), true);
					offset += Marshal.SizeOf(id.ManufactorID);
					offset += BytesTool.copy(id.ModelDescription, 0, _Fields[p].Value, offset, id.ModelDescription.Length);

					// Skip some not needed info.
					offset += 3;

					id.DeviceCapabilities = (byte) BytesTool.readBytes(_Fields[p].Value, offset, Marshal.SizeOf(id.DeviceCapabilities), true);
					offset += Marshal.SizeOf(id.DeviceCapabilities);
					id.ACFrequencyEnvironment = (byte) BytesTool.readBytes(_Fields[p].Value, offset, Marshal.SizeOf(id.ACFrequencyEnvironment), true);
					offset += Marshal.SizeOf(id.ACFrequencyEnvironment);

					return id;
				}
				return null;
			}
			set
			{
				AcquiringDeviceID id = value;

				if ((id != null)
				&&  (id.ModelDescription != null))
				{
					SCPHeaderField field = new SCPHeaderField();
					string deviceManufactor = (id.ManufactorID == 0 ? ECGConverter.SoftwareName : ((DeviceManufactor)id.ManufactorID).ToString());
					string unknown = "unknown";
					field.Tag = 14;
					field.Length = (ushort) (41 + (ECGConverter.SoftwareName.Length > 24 ? 24 : ECGConverter.SoftwareName.Length) + deviceManufactor.Length + (3 * unknown.Length));
					field.Value = new byte[field.Length];
					int offset = 0;
					BytesTool.writeBytes(id.InstitutionNr, field.Value, offset, Marshal.SizeOf(id.InstitutionNr), true);
					offset += Marshal.SizeOf(id.InstitutionNr);
					BytesTool.writeBytes(id.DepartmentNr, field.Value, offset, Marshal.SizeOf(id.DepartmentNr), true);
					offset += Marshal.SizeOf(id.DepartmentNr);
					BytesTool.writeBytes(id.DeviceID, field.Value, offset, Marshal.SizeOf(id.DeviceID), true);
					offset += Marshal.SizeOf(id.DeviceID);
					BytesTool.writeBytes(id.DeviceType, field.Value, offset, Marshal.SizeOf(id.DeviceType), true);
					offset += Marshal.SizeOf(id.DeviceType);
					BytesTool.writeBytes((id.ManufactorID == 0 ? (byte) 0xff : id.ManufactorID), field.Value, offset, Marshal.SizeOf(id.ManufactorID), true);
					offset += Marshal.SizeOf(id.ManufactorID);
					offset += BytesTool.copy(field.Value, offset, id.ModelDescription, 0, id.ModelDescription.Length);
					field.Value[offset++] = ProtocolVersionNr;
					field.Value[offset++] = 0x00;
					field.Value[offset++] = 0x00;
					field.Value[offset++] = (id.DeviceCapabilities == 0 ? (byte) 0x8 : id.DeviceCapabilities);
					field.Value[offset++] = id.ACFrequencyEnvironment;

					// Skip Reserved for Future field
					offset += 16;

					field.Value[offset++] = (byte) (unknown.Length + 1);

					BytesTool.writeString(_Encoding, unknown, field.Value, offset, unknown.Length + 1);
					offset+= unknown.Length + 1;

					BytesTool.writeString(_Encoding, unknown, field.Value, offset, unknown.Length + 1);
					offset+= unknown.Length + 1;

					BytesTool.writeString(_Encoding, unknown, field.Value, offset, unknown.Length + 1);
					offset+= unknown.Length + 1;

					BytesTool.writeString(_Encoding, ECGConverter.SoftwareName, field.Value, offset, (ECGConverter.SoftwareName.Length > 24 ? 24 : ECGConverter.SoftwareName.Length) + 1);
					offset+= (ECGConverter.SoftwareName.Length > 24 ? 24 : ECGConverter.SoftwareName.Length) + 1;

					BytesTool.writeString(_Encoding, deviceManufactor, field.Value, offset, deviceManufactor.Length + 1);
					offset+= deviceManufactor.Length + 1;

					int ret = Insert(field);

					if (ret == 0)
						ret = setLanguageSupportCode(_Encoding);
				}
			}
		}
		public AcquiringDeviceID AnalyzingMachineID
		{
			get
			{
				int p = _SearchField(15);
				if ((p >= 0)
				&&  (_Fields[p].Length >= 36)
				&&  (_Fields[p].Value != null)
				&&  (_Fields[p].Length <= _Fields[p].Value.Length))
				{
					AcquiringDeviceID id = new AcquiringDeviceID();
					int offset = 0;
					id.InstitutionNr = (ushort) BytesTool.readBytes(_Fields[p].Value, offset, Marshal.SizeOf(id.InstitutionNr), true);
					offset += Marshal.SizeOf(id.InstitutionNr);
					id.DepartmentNr = (ushort) BytesTool.readBytes(_Fields[p].Value, offset, Marshal.SizeOf(id.DepartmentNr), true);
					offset += Marshal.SizeOf(id.DepartmentNr);
					id.DeviceID = (ushort) BytesTool.readBytes(_Fields[p].Value, offset, Marshal.SizeOf(id.DeviceID), true);
					offset += Marshal.SizeOf(id.DeviceID);
					id.DeviceType = (byte) BytesTool.readBytes(_Fields[p].Value, offset, Marshal.SizeOf(id.DeviceType), true);
					offset += Marshal.SizeOf(id.DeviceType);
					id.ManufactorID = (byte) BytesTool.readBytes(_Fields[p].Value, offset, Marshal.SizeOf(id.ManufactorID), true);
					offset += Marshal.SizeOf(id.ManufactorID);
					offset += BytesTool.copy(id.ModelDescription, 0, _Fields[p].Value, offset, id.ModelDescription.Length);

					// Skip some not needed info.
					offset += 3;

					id.DeviceCapabilities = (byte) BytesTool.readBytes(_Fields[p].Value, offset, Marshal.SizeOf(id.DeviceCapabilities), true);
					offset += Marshal.SizeOf(id.DeviceCapabilities);
					id.ACFrequencyEnvironment = (byte) BytesTool.readBytes(_Fields[p].Value, offset, Marshal.SizeOf(id.ACFrequencyEnvironment), true);
					offset += Marshal.SizeOf(id.ACFrequencyEnvironment);

					return id;
				}
				return null;
			}
			set
			{
				AcquiringDeviceID id = value;

				if ((id != null)
				&&  (id.ModelDescription != null))
				{
					SCPHeaderField field = new SCPHeaderField();
					string deviceManufactor = (id.ManufactorID == 0 ? ECGConverter.SoftwareName : ((DeviceManufactor)id.ManufactorID).ToString());
					string unknown = "unknown";
					field.Tag = 15;
					field.Length = (ushort) (41 + (ECGConverter.SoftwareName.Length > 24 ? 24 : ECGConverter.SoftwareName.Length) + deviceManufactor.Length + (3 * unknown.Length));
					field.Value = new byte[field.Length];
					int offset = 0;
					BytesTool.writeBytes(id.InstitutionNr, field.Value, offset, Marshal.SizeOf(id.InstitutionNr), true);
					offset += Marshal.SizeOf(id.InstitutionNr);
					BytesTool.writeBytes(id.DepartmentNr, field.Value, offset, Marshal.SizeOf(id.DepartmentNr), true);
					offset += Marshal.SizeOf(id.DepartmentNr);
					BytesTool.writeBytes(id.DeviceID, field.Value, offset, Marshal.SizeOf(id.DeviceID), true);
					offset += Marshal.SizeOf(id.DeviceID);
					BytesTool.writeBytes(id.DeviceType, field.Value, offset, Marshal.SizeOf(id.DeviceType), true);
					offset += Marshal.SizeOf(id.DeviceType);
					BytesTool.writeBytes(id.ManufactorID == 0 ? (byte)0xff : id.ManufactorID, field.Value, offset, Marshal.SizeOf(id.ManufactorID), true);
					offset += Marshal.SizeOf(id.ManufactorID);
					offset += BytesTool.copy(field.Value, offset, id.ModelDescription, 0, id.ModelDescription.Length);
					field.Value[offset++] = ProtocolVersionNr;
					field.Value[offset++] = 0x00;
					field.Value[offset++] = 0x00;
					field.Value[offset++] = (id.DeviceCapabilities == 0 ? (byte) 0x8 : id.DeviceCapabilities);
					field.Value[offset++] = id.ACFrequencyEnvironment;

					// Skip Reserved for Future field
					offset += 16;

					field.Value[offset++] = (byte) (unknown.Length + 1);

					BytesTool.writeString(_Encoding, unknown, field.Value, offset, unknown.Length + 1);
					offset+= unknown.Length + 1;

					BytesTool.writeString(_Encoding, unknown, field.Value, offset, unknown.Length + 1);
					offset+= unknown.Length + 1;

					BytesTool.writeString(_Encoding, unknown, field.Value, offset, unknown.Length + 1);
					offset+= unknown.Length + 1;

					BytesTool.writeString(_Encoding, ECGConverter.SoftwareName, field.Value, offset, (ECGConverter.SoftwareName.Length > 24 ? 24 : ECGConverter.SoftwareName.Length) + 1);
					offset+= (ECGConverter.SoftwareName.Length > 24 ? 24 : ECGConverter.SoftwareName.Length) + 1;

					BytesTool.writeString(_Encoding, deviceManufactor, field.Value, offset, deviceManufactor.Length + 1);
					offset+= deviceManufactor.Length + 1;

					int ret = Insert(field);

					if (ret == 0)
						ret = setLanguageSupportCode(_Encoding);
				}
			}
		}
		public DateTime TimeAcquisition
		{
			get
			{
				int p = _SearchField(25);
				if ((p >= 0)
				&&  (_Fields[p].Length == 4)
				&&  (_Fields[p].Value != null)
				&&  (_Fields[p].Length <= _Fields[p].Value.Length))
				{
					SCPDate scpdate = new SCPDate();
					scpdate.Read(_Fields[p].Value, 0);
					DateTime time = new DateTime(scpdate.Year, scpdate.Month, scpdate.Day);

					p = _SearchField(26);
					if ((p >= 0)
					&&  (_Fields[p].Length == 3)
					&&  (_Fields[p].Value != null)
					&&  (_Fields[p].Length <= _Fields[p].Value.Length))
					{
						SCPTime scptime = new SCPTime();
						scptime.Read(_Fields[p].Value, 0);

						time = time.AddHours(scptime.Hour).AddMinutes(scptime.Min).AddSeconds(scptime.Sec);
					}

					return time;
				}
				return DateTime.MinValue;
			}
			set
			{
				DateTime time = value;

				if (time.Year > 1000)
				{
					SCPHeaderField field = new SCPHeaderField();
					field.Tag = 25;
					field.Length = (ushort) SCPDate.Size;
					field.Value = new byte[field.Length];
					SCPDate scpdate = new SCPDate();
					scpdate.Year = (ushort) time.Year;
					scpdate.Month = (byte) time.Month;
					scpdate.Day = (byte) time.Day;
					scpdate.Write(field.Value, 0);
					int ret = Insert(field);

					if (ret != 0)
						return;

					field = new SCPHeaderField();
					field.Tag = 26;
					field.Length = (ushort) SCPTime.Size;
					field.Value = new byte[field.Length];
					SCPTime scptime = new SCPTime();
					scptime.Hour = (byte) time.Hour;
					scptime.Min = (byte) time.Minute;
					scptime.Sec = (byte) time.Second;
					scptime.Write(field.Value, 0);

					Insert(field);
				}
			}
		}
		public ushort BaselineFilter
		{
			get
			{
				int p = _SearchField(27);
				if ((p >= 0)
				&&  (_Fields[p].Length == 2)
				&&  (_Fields[p].Value != null)
				&&  (_Fields[p].Length <= _Fields[p].Value.Length))
				{
					return (ushort) BytesTool.readBytes(_Fields[p].Value, 0, Marshal.SizeOf(typeof(ushort)), true);
				}
				return 0;
			}
			set
			{
				SCPHeaderField field = new SCPHeaderField();
				field.Tag = 27;
				field.Length = (ushort) Marshal.SizeOf(value);
				field.Value = new byte[field.Length];
				BytesTool.writeBytes(value, field.Value, 0, Marshal.SizeOf(value), true);
				Insert(field);
			}
		}
		public ushort LowpassFilter
		{
			get
			{
				int p = _SearchField(28);
				if ((p >= 0)
				&&  (_Fields[p].Length == 2)
				&&  (_Fields[p].Value != null)
				&&  (_Fields[p].Length <= _Fields[p].Value.Length))
				{
					return (ushort) BytesTool.readBytes(_Fields[p].Value, 0, Marshal.SizeOf(typeof(ushort)), true);
				}
				return 0;
			}
			set
			{
				SCPHeaderField field = new SCPHeaderField();
				field.Tag = 28;
				field.Length = (ushort) Marshal.SizeOf(value);
				field.Value = new byte[field.Length];
				BytesTool.writeBytes(value, field.Value, 0, Marshal.SizeOf(value), true);
				Insert(field);
			}
		}
		public byte FilterBitmap
		{
			get
			{
				int p = _SearchField(29);
				if ((p >= 0)
				&&  (_Fields[p].Length == 1)
				&&  (_Fields[p].Value != null)
				&&  (_Fields[p].Length <= _Fields[p].Value.Length))
				{
					return (byte) BytesTool.readBytes(_Fields[p].Value, 0, Marshal.SizeOf(typeof(byte)), true);
				}
				return 0;
			}
			set
			{
				if (value != 0)
				{
					SCPHeaderField field = new SCPHeaderField();
					field.Tag = 29;
					field.Length = (ushort) Marshal.SizeOf(value);
					field.Value = new byte[field.Length];
					BytesTool.writeBytes(value, field.Value, 0, Marshal.SizeOf(value), true);
					Insert(field);
				}
			}
		}
		public string[] FreeTextFields
		{
			get
			{
				int p = _SearchField(30);
				if (p >= 0)
				{
					for (;(p > 0) && (_Fields[p-1].Tag == 30);p--){}
					int len=0;
					for (;((p + len) < _NrFields) && (_Fields[p+len].Tag == 30);len++){}

					if (len > 0)
					{
						string []text = new string[len];
						for (int loper=0;loper < len;loper++)
						{
							if ((_Fields[p + loper].Value != null)
							&&  (_Fields[p + loper].Length <= _Fields[p + loper].Value.Length))
							{
								text[loper] = BytesTool.readString(_Encoding, _Fields[p + loper].Value, 0, _Fields[p + loper].Length);
							}
						}
						return text;
					}
				}
				return null;
			}
			set
			{
				if (value != null)
				{
					for (int loper=0;loper < value.Length;loper++)
					{
						if (value[loper] != null)
						{
							SCPHeaderField field = new SCPHeaderField();
							field.Tag = 30;
							field.Length = (ushort) (value[loper].Length >= _ExceptionsMaximumLength ? _ExceptionsMaximumLength : value[loper].Length + 1);
							field.Value = new byte[field.Length];
							BytesTool.writeString(_Encoding, value[loper], field.Value, 0, field.Length);
							Insert(field);
						}
					}
				}
			}
		}
		public string SequenceNr
		{
			get {return getText(31);}
			set {setText(31, value);}
		}
		public string AcqInstitution
		{
			get {return getText(16);}
			set {setText(16, value);}
		}
		public string AnalyzingInstitution
		{
			get {return getText(17);}
			set {setText(17, value);}
		}
		public string AcqDepartment
		{
			get {return getText(18);}
			set {setText(18, value);}
		}
		public string AnalyzingDepartment
		{
			get {return getText(19);}
			set {setText(19, value);}
		}
		public string ReferringPhysician
		{
			get {return getText(20);}
			set {setText(20, value);}
		}
		public string OverreadingPhysician
		{
			get {return getText(21);}
			set {setText(21, value);}
		}
		public string TechnicianDescription
		{
			get {return getText(22);}
			set {setText(22, value);}
		}
		public ushort SystolicBloodPressure
		{
			get
			{
				int p = _SearchField(11);
				if (p >= 0)
				{
					return (ushort) BytesTool.readBytes(_Fields[p].Value, 0, Marshal.SizeOf(typeof(ushort)), true);
				}
				return 0;
			}
			set
			{
				if (value != 0)
				{
					SCPHeaderField field = new SCPHeaderField();
					field.Tag = 11;
					field.Length = (ushort) Marshal.SizeOf(typeof(ushort));
					field.Value = new byte[field.Length];
					BytesTool.writeBytes(value, field.Value, 0, field.Length, true);
					Insert(field);
				}
			}
		}
		public ushort DiastolicBloodPressure
		{
			get
			{
				int p = _SearchField(12);
				if (p >= 0)
				{
					return (ushort) BytesTool.readBytes(_Fields[p].Value, 0, Marshal.SizeOf(typeof(ushort)), true);
				}
				return 1;
			}
			set
			{
				if (value != 0)
				{
					SCPHeaderField field = new SCPHeaderField();
					field.Tag = 12;
					field.Length = (ushort) Marshal.SizeOf(typeof(ushort));
					field.Value = new byte[field.Length];
					BytesTool.writeBytes(value, field.Value, 0, field.Length, true);
					Insert(field);
				}
			}
		}
		public Drug[] Drugs
		{
			get
			{
				int p = _SearchField(10);
				if (p >= 0)
				{
					for (;(p > 0) && (_Fields[p-1].Tag == 10);p--){}
					int len=0;
					for (;((p + len) < _NrFields) && (_Fields[p+len].Tag == 10);len++){}

					if (len > 0)
					{
						Drug[] drugs = new Drug[len];
						for (int loper=0;loper < len;loper++)
						{
							if ((_Fields[p + loper].Length > 3)
							&&  (_Fields[p + loper].Value != null)
							&&  (_Fields[p + loper].Length <= _Fields[p + loper].Value.Length))
							{
								drugs[loper] = new Drug();
								drugs[loper].DrugClass = _Fields[p + loper].Value[1];
								drugs[loper].ClassCode = _Fields[p + loper].Value[2];
								drugs[loper].TextDesciption = BytesTool.readString(_Encoding, _Fields[p + loper].Value, 3, _Fields[p + loper].Length - 3);
							}
						}
						return drugs;
					}
				}
				return null;
			}
			set
			{
				if (value != null)
				{
					for (int loper=0;loper < value.Length;loper++)
					{
						if (value[loper] != null)
						{
							SCPHeaderField field = new SCPHeaderField();
							field.Tag = 10;
							field.Length = (ushort) (4 + (value[loper].TextDesciption != null ? value[loper].TextDesciption.Length : 0));
							field.Value = new byte[field.Length];
							field.Value[0] = 0;
							field.Value[1] = value[loper].DrugClass;
							field.Value[2] = value[loper].ClassCode;
							BytesTool.writeString(_Encoding, value[loper].TextDesciption, field.Value, 3, field.Length - 3);

							Insert(field);
						}
					}
				}
			}
		}
		public string[] ReferralIndication
		{
			get
			{
				int p = _SearchField(13);
				if (p >= 0)
				{
					for (;(p > 0) && (_Fields[p-1].Tag == 13);p--){}
					int len=0;
					for (;((p + len) < _NrFields) && (_Fields[p+len].Tag == 13);len++){}
					if (len < 0)
					{
						string[] text = new string[len];
						for (int loper=0;loper < len;loper++)
						{
							if ((_Fields[p + loper].Value != null)
								&&  (_Fields[p + loper].Length <= _Fields[p + loper].Value.Length))
							{
								text[loper] = BytesTool.readString(_Encoding, _Fields[p + loper].Value, 0, _Fields[p + loper].Length);
							}
						}
						return text;
					}
				}
				return null;
			}
			set
			{
				if (value != null)
				{
					for (int loper=0;loper < value.Length;loper++)
					{
						if (value[loper] != null)
						{
							SCPHeaderField field = new SCPHeaderField();
							field.Tag = 13;
							field.Length = (ushort) (value[loper].Length >= _ExceptionsMaximumLength ? _ExceptionsMaximumLength : value[loper].Length + 1);
							field.Value = new byte[field.Length];
							BytesTool.writeString(_Encoding, value[loper], field.Value, 0, field.Length);
							Insert(field);
						}
					}
				}
			}
		}
		public string RoomDescription
		{
			get {return getText(23);}
			set {setText(23, value);}
		}
		public byte StatCode
		{
			get
			{
				int p = _SearchField(24);
				if ((p >= 0)
				&&	(_Fields[p].Value != null)
				&&	(_Fields[p].Length == Marshal.SizeOf(typeof(byte))))
				{
					return (byte) BytesTool.readBytes(_Fields[p].Value, 0, Marshal.SizeOf(typeof(byte)), true);
				}
				return 0xff;
			}
			set
			{
				if (value != 0xff)
				{
					SCPHeaderField field = new SCPHeaderField();
					field.Tag = 24;
					field.Length = (ushort) Marshal.SizeOf(value);
					field.Value = new byte[field.Length];
					BytesTool.writeBytes(value, field.Value, 0, Marshal.SizeOf(value), true);
					Insert(field);
				}
			}
		}
		/// <summary>
		/// Class for a header field.
		/// </summary>
		public class SCPHeaderField
		{
			public byte Tag = 0;
			public ushort Length = 0;
			public byte[] Value = null;
			/// <summary>
			/// Constructor to make an SCP header field.
			/// </summary>
			public SCPHeaderField()
			{}
			/// <summary>
			/// Constructor to make an SCP header field.
			/// </summary>
			/// <param name="tag">tag to use</param>
			/// <param name="length">length to use</param>
			/// <param name="value">array to use</param>
			public SCPHeaderField(byte tag, ushort length, byte[] value)
			{
				Tag = tag;
				Length = (value == null ? (ushort)0 : length);
				Value = value;
			}
		}
		public enum ProtocolCompatibility
		{CatI = 0x90, CatII = 0xa0, CatIII = 0xb0, CatIV = 0xc0}
	}
}
