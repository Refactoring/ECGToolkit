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
using System.Reflection;
using System.Runtime.InteropServices;

namespace Communication.IO.Tools
{
	/// <summary>
	/// Summary description for a DataSection.
	/// </summary>
	public abstract class DataSection
	{
		public enum StringType
		{
			FIXED_LENGTH = 0x0,
			MAX_LENGTH = 0x1,
			MAX_LENGTH_ZERO_END = 0x2,
//			ONE_BYTE_LENGTH = 0x4,
//			TWO_BYTE_LENGTH = 0x5,
		}

		private int _Pack(int offset, int size)
		{
			if (Pack > 1)
			{
				if (size == 2)
				{
					if ((offset & 0x1) == 0x1)
						offset++; 
				}
				else/* if ((size == 4)
					||	 (size == 8))*/
				{
					int pack = Pack - (offset % Pack);

					if ((pack < Pack)
					&&	(size > pack))
						offset += pack;
				}
			}

			return offset;
		}

		public static int ReadObj(DataSection obj, byte[] buffer, int offset, int length)
		{
			if ((obj == null)
			||	(buffer == null)
			||	((offset + length) > buffer.Length))
				return 1;

			length += offset;

			Type objType = obj.GetType();

			foreach (FieldInfo fieldInfo in objType.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				try
				{
					if (fieldInfo.FieldType == typeof(String))
					{
						offset = obj._Pack(offset, 1);

						FieldInfo fiLen = objType.GetField(fieldInfo.Name + "_Length");

						String val = null;

						if (fiLen != null)
						{
							int size = (int)fiLen.GetValue(obj);

							val = BytesTool.readString(buffer, offset, size);

							if (obj.TypeOfString == StringType.FIXED_LENGTH)
							{
								offset += size;
							}
							else
							{
								offset++;

								if (val != null)
								{
									offset += val.Length;
								}
							}
						}
						else
						{
							val = BytesTool.readString(buffer, offset, length - offset);

							offset++;

							if (val != null)
							{
								offset += val.Length;
							}
						}

						if ((val != null)
						&&	(val.Length == 0))
						{
							val = null;
						}

						fieldInfo.SetValue(obj, val);
					}
					else if (fieldInfo.FieldType.IsArray
						&&	 fieldInfo.FieldType.HasElementType)
					{
						if (fieldInfo.FieldType.GetElementType() == typeof(String))
						{
							offset = obj._Pack(offset, 1);

							FieldInfo fiLen = objType.GetField(fieldInfo.Name + "_Length");
							String[] array = (String[]) fieldInfo.GetValue(obj);

							if (fiLen != null)
							{
								int size = (int)fiLen.GetValue(obj);

								for (int i=0;i < array.Length;i++)
								{
									array[i] = BytesTool.readString(buffer, offset, size);

									if (obj.TypeOfString == StringType.FIXED_LENGTH)
									{
										offset += size;
									}
									else
									{
										offset++;

										if (array[i] != null)
										{
											offset += array[i].Length;
										}
									}

									if ((array[i] != null)
									&&	(array[i].Length == 0))
									{
										array[i] = null;
									}
								}
							}
							else
							{
								for (int i=0;i < array.Length;i++)
								{
									array[i] = BytesTool.readString(buffer, offset, length - offset);

									offset++;

									if (array[i] != null)
									{
										offset += array[i].Length;
								
										if (array[i].Length == 0)
										{
											array[i] = null;
										}
									}
								}
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(UInt64))
						{
							int size = 8;

							UInt64[] array = (UInt64[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								offset = obj._Pack(offset, size);

								for (int i=0;i < array.Length;i++)
								{
									array[i] = (UInt64) BytesTool.readBytes(buffer, offset, size, obj.LittleEndian);
									offset += size;
								}
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(Int64))
						{
							int size = 8;

							Int64[] array = (Int64[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								offset = obj._Pack(offset, size);

								for (int i=0;i < array.Length;i++)
								{
									array[i] = BytesTool.readBytes(buffer, offset, size, obj.LittleEndian);
									offset += size;
								}
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(UInt32))
						{
							int size = 4;

							UInt32[] array = (UInt32[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								offset = obj._Pack(offset, size);

								for (int i=0;i < array.Length;i++)
								{
									array[i] = (UInt32) BytesTool.readBytes(buffer, offset, size, obj.LittleEndian);
									offset += size;
								}
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(Int32))
						{
							int size = 4;

							Int32[] array = (Int32[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								offset = obj._Pack(offset, size);

								for (int i=0;i < array.Length;i++)
								{
									array[i] = (Int32) BytesTool.readBytes(buffer, offset, size, obj.LittleEndian);
									offset += size;
								}
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(UInt16))
						{
							int size = 2;

							UInt16[] array = (UInt16[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								offset = obj._Pack(offset, size);

								for (int i=0;i < array.Length;i++)
								{
									array[i] = (UInt16) BytesTool.readBytes(buffer, offset, size, obj.LittleEndian);
									offset += size;
								}
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(Int16))
						{
							int size = 2;

							Int16[] array = (Int16[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								offset = obj._Pack(offset, size);

								for (int i=0;i < array.Length;i++)
								{
									array[i] = (Int16) BytesTool.readBytes(buffer, offset, size, obj.LittleEndian);
									offset += size;
								}
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(Byte))
						{
							Byte[] array = (Byte[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								offset = obj._Pack(offset, 1);

								for (int i=0;i < array.Length;i++)
									array[i] = buffer[offset++];
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(SByte))
						{
							SByte[] array = (SByte[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								offset = obj._Pack(offset, 1);

								for (int i=0;i < array.Length;i++)
									array[i] = (SByte)buffer[offset++];
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(DataSection))
						{
							// todo
						}
					}
					else if (fieldInfo.FieldType == typeof(Int64))
					{
						int size = 8;

						offset = obj._Pack(offset, size);

						Int64 val = BytesTool.readBytes(buffer, offset, size, obj.LittleEndian);

						fieldInfo.SetValue(obj, val);

						offset += size;
					}
					else if (fieldInfo.FieldType == typeof(UInt64))
					{
						int size = 8;

						offset = obj._Pack(offset, size);

						UInt64 val = (UInt64) BytesTool.readBytes(buffer, offset, size, obj.LittleEndian);

						fieldInfo.SetValue(obj, val);

						offset += size;
					}
					else if (fieldInfo.FieldType == typeof(Int32))
					{
						int size = 4;

						offset = obj._Pack(offset, size);

						Int32 val = (Int32) BytesTool.readBytes(buffer, offset, size, obj.LittleEndian);

						fieldInfo.SetValue(obj, val);

						offset += size;
					}
					else if (fieldInfo.FieldType == typeof(UInt32))
					{
						int size = 4;

						offset = obj._Pack(offset, size);

						UInt32 val = (UInt32) BytesTool.readBytes(buffer, offset, size, obj.LittleEndian);

						fieldInfo.SetValue(obj, val);

						offset += size;
					}
					else if (fieldInfo.FieldType == typeof(Int16))
					{
						int size = 2;

						offset = obj._Pack(offset, size);

						Int16 val = (Int16) BytesTool.readBytes(buffer, offset, size, obj.LittleEndian);

						fieldInfo.SetValue(obj, val);

						offset += size;
					}
					else if (fieldInfo.FieldType == typeof(UInt16))
					{
						int size = 2;

						offset = obj._Pack(offset, size);

						UInt16 val = (UInt16) BytesTool.readBytes(buffer, offset, size, obj.LittleEndian);

						fieldInfo.SetValue(obj, val);

						offset += size;
					}
					else if (fieldInfo.FieldType == typeof(SByte))
					{
						offset = obj._Pack(offset, 1);

						SByte val = (SByte) BytesTool.readBytes(buffer, offset, 1, obj.LittleEndian);

						fieldInfo.SetValue(obj, val);

						offset++;
					}
					else if (fieldInfo.FieldType == typeof(Byte))
					{
						offset = obj._Pack(offset, 1);

						Byte val = (Byte) BytesTool.readBytes(buffer, offset, 1, obj.LittleEndian);

						fieldInfo.SetValue(obj, val);

						offset++;
					}
					else if (fieldInfo.FieldType == typeof(DataSection))
					{
						DataSection innerSection = (DataSection)fieldInfo.GetValue(obj);

						offset = obj._Pack(offset, 4);

						int innerRet = innerSection.Read(buffer, offset, length - offset);

						if (innerRet != 0)
							return innerRet << 1;

						offset += innerSection.Size();
					}
				}
				catch
				{
					return 2;
				}
			}

			return 0;
		}

		public static int WriteObj(DataSection obj, byte[] buffer, int offset, int length)
		{
			if ((obj == null)
			||	(buffer == null)
			||	((offset + length) > buffer.Length))
				return 1;

			length += offset;

			Type objType = obj.GetType();

			foreach (FieldInfo fieldInfo in objType.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				try
				{
					if (fieldInfo.FieldType == typeof(String))
					{
						offset = obj._Pack(offset, 1);

						FieldInfo fiLen = objType.GetField(fieldInfo.Name + "_Length");

						String val = (String) fieldInfo.GetValue(obj);

						if (fiLen != null)
						{
							int size = (int)fiLen.GetValue(obj);

							if (obj.TypeOfString == StringType.FIXED_LENGTH)
							{
								BytesTool.emptyBuffer(buffer, offset, size, 0);

								if (val != null)
									BytesTool.writeString(val, buffer, offset, size);

								offset += size;
							}
							else
							{
								if (val != null)
								{
									BytesTool.writeString(val, buffer, offset, size);

									offset += val.Length < size ? val.Length + 1 : size;

									if (obj.TypeOfString == StringType.MAX_LENGTH_ZERO_END)
										buffer[offset-1] = 0;
								}
								else
								{
									buffer[offset++] = 0;
								}
							}
						}
						else
						{
							if (val != null)
							{
								BytesTool.writeString(val, buffer, offset, length - offset);

								offset += val.Length;
							}
							else
							{
								buffer[0] = 0;
							}

							offset++;
						}
					}
					else if (fieldInfo.FieldType.IsArray
						&&	 fieldInfo.FieldType.HasElementType)
					{
                    	if (fieldInfo.FieldType.GetElementType() == typeof(String))
						{
							offset = obj._Pack(offset, 1);

							FieldInfo fiLen = objType.GetField(fieldInfo.Name + "_Length");
							String[] array = (String[]) fieldInfo.GetValue(obj);

							if (fiLen != null)
							{
								int size = (int)fiLen.GetValue(obj);

								if (obj.TypeOfString == StringType.FIXED_LENGTH)
								{
									for (int i=0;i < array.Length;i++)
									{
										BytesTool.emptyBuffer(buffer, offset, size, 0);

										if (array[i] != null)
											BytesTool.writeString(array[i], buffer, offset, size);

										offset += size;
									}
								}
								else
								{
									for (int i=0;i < array.Length;i++)
									{
										if (array[i] != null)
										{
											BytesTool.writeString(array[i], buffer, offset, size);

											offset += array[i].Length < size ? array[i].Length + 1 : size;

											if (obj.TypeOfString == StringType.MAX_LENGTH_ZERO_END)
												buffer[offset-1] = 0;
										}
										else
										{
											buffer[offset++] = 0;
										}
									}
								}
							}
							else
							{
								for (int i=0;i < array.Length;i++)
								{
									if (array[i] != null)
									{
										BytesTool.writeString(array[i], buffer, offset, length - offset);

										offset += array[i].Length;
									}
									else
									{
										buffer[offset] = 0;
									}

									offset++;
								}
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(UInt64))
						{
							int size = 8;

							UInt64[] array = (UInt64[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								offset = obj._Pack(offset, size);

								for (int i=0;i < array.Length;i++)
								{
									BytesTool.writeBytes((Int64)array[i], buffer, offset, size, obj.LittleEndian);
									offset += size;
								}
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(Int64))
						{
							int size = 8;

							Int64[] array = (Int64[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								offset = obj._Pack(offset, size);

								for (int i=0;i < array.Length;i++)
								{
									BytesTool.writeBytes(array[i], buffer, offset, size, obj.LittleEndian);
									offset += size;
								}
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(UInt32))
						{
							int size = 4;

							UInt32[] array = (UInt32[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								offset = obj._Pack(offset, size);

								for (int i=0;i < array.Length;i++)
								{
									BytesTool.writeBytes(array[i], buffer, offset, size, obj.LittleEndian);
									offset += size;
								}
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(Int32))
						{
							int size = 4;

							Int32[] array = (Int32[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								offset = obj._Pack(offset, size);

								for (int i=0;i < array.Length;i++)
								{
									BytesTool.writeBytes(array[i], buffer, offset, size, obj.LittleEndian);
									offset += size;
								}
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(UInt16))
						{
							int size = 2;

							UInt16[] array = (UInt16[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								offset = obj._Pack(offset, size);

								for (int i=0;i < array.Length;i++)
								{
									BytesTool.writeBytes(array[i], buffer, offset, size, obj.LittleEndian);
									offset += size;
								}
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(Int16))
						{
							int size = 2;

							Int16[] array = (Int16[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								offset = obj._Pack(offset, size);

								for (int i=0;i < array.Length;i++)
								{
									BytesTool.writeBytes(array[i], buffer, offset, size, obj.LittleEndian);
									offset += size;
								}
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(Byte))
						{
							Byte[] array = (Byte[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								offset = obj._Pack(offset, 1);

								for (int i=0;i < array.Length;i++)
									buffer[offset++] = array[i];
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(SByte))
						{
							SByte[] array = (SByte[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								offset = obj._Pack(offset, 1);

								for (int i=0;i < array.Length;i++)
									buffer[offset++] = (Byte) array[i];
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(DataSection))
						{
							// todo
						}
					}
					else if (fieldInfo.FieldType == typeof(Int64))
					{
						int size = 8;

						offset = obj._Pack(offset, size);

						Int64 val = (Int64) fieldInfo.GetValue(obj);

						BytesTool.writeBytes(val, buffer, offset, size, obj.LittleEndian);

						offset += size;
					}
					else if (fieldInfo.FieldType == typeof(UInt64))
					{
						int size = 8;

						offset = obj._Pack(offset, size);

						UInt64 val = (UInt64) fieldInfo.GetValue(obj);

						BytesTool.writeBytes((Int64)val, buffer, offset, size, obj.LittleEndian);

						offset += size;
					}
					else if (fieldInfo.FieldType == typeof(Int32))
					{
						int size = 4;

						offset = obj._Pack(offset, size);

						Int32 val = (Int32) fieldInfo.GetValue(obj);

						BytesTool.writeBytes(val, buffer, offset, size, obj.LittleEndian);

						offset += size;
					}
					else if (fieldInfo.FieldType == typeof(UInt32))
					{
						int size = 4;

						offset = obj._Pack(offset, size);

						UInt32 val = (UInt32) fieldInfo.GetValue(obj);

						BytesTool.writeBytes(val, buffer, offset, size, obj.LittleEndian);

						offset += size;
					}
					else if (fieldInfo.FieldType == typeof(Int16))
					{
						int size = 2;

						offset = obj._Pack(offset, size);

						Int16 val = (Int16) fieldInfo.GetValue(obj);

						BytesTool.writeBytes(val, buffer, offset, size, obj.LittleEndian);

						offset += size;
					}
					else if (fieldInfo.FieldType == typeof(UInt16))
					{
						int size = 2;

						offset = obj._Pack(offset, size);

						UInt16 val = (UInt16) fieldInfo.GetValue(obj);

						BytesTool.writeBytes(val, buffer, offset, size, obj.LittleEndian);

						offset += size;
					}
					else if (fieldInfo.FieldType == typeof(SByte))
					{
						offset = obj._Pack(offset, 1);

						SByte val = (SByte) fieldInfo.GetValue(obj);

						BytesTool.writeBytes(val, buffer, offset, 1, obj.LittleEndian);

						offset++;
					}
					else if (fieldInfo.FieldType == typeof(Byte))
					{
						offset = obj._Pack(offset, 1);

						Byte val = (Byte) fieldInfo.GetValue(obj);

						BytesTool.writeBytes(val, buffer, offset, 1, obj.LittleEndian);

						offset++;
					}
					else if (fieldInfo.FieldType == typeof(DataSection))
					{
						DataSection innerSection = (DataSection)fieldInfo.GetValue(obj);

						offset = obj._Pack(offset, 4);

						int innerRet = innerSection.Write(buffer, offset, length - offset);

						if (innerRet != 0)
							return innerRet << 1;

						offset += innerSection.Size();
					}
				}
				catch
				{
					return 2;
				}
			}

			return 0;
		}

		public static int SizeObj(DataSection obj)
		{
			if (obj == null)
				return -1;

			int ret = 0;

			Type objType = obj.GetType();

			foreach (FieldInfo fieldInfo in objType.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				try
				{
					if (fieldInfo.FieldType == typeof(String))
					{
						ret = obj._Pack(ret, 1);

						FieldInfo fiLen = objType.GetField(fieldInfo.Name + "_Length");

						if (fiLen != null)
						{
							int size = (int)fiLen.GetValue(obj);

							if (obj.TypeOfString == StringType.FIXED_LENGTH)
							{
								ret += size;
							}
							else
							{
								String val = (String) fieldInfo.GetValue(obj);

								if (val != null)
								{
									ret += val.Length < size ? val.Length + 1 : size;
								}
								else
								{
									ret++;
								}
							}
						}
						else
						{
							String val = (String) fieldInfo.GetValue(obj);

							ret += (val == null) ? 1 : val.Length + 1;
						}
					}
					else if (fieldInfo.FieldType.IsArray
						&&	 fieldInfo.FieldType.HasElementType)
					{
						if (fieldInfo.FieldType.GetElementType() == typeof(String))
						{
							ret = obj._Pack(ret, 1);

							FieldInfo fiLen = objType.GetField(fieldInfo.Name + "_Length");
							String[] array = (String[]) fieldInfo.GetValue(obj);

							if (fiLen != null)
							{
								int size = (int)fiLen.GetValue(obj);

								if (obj.TypeOfString == StringType.FIXED_LENGTH)
								{
									ret += array.Length * size;
								}
								else
								{
									for (int i=0;i < array.Length;i++)
									{
										if (array[i] != null)
										{
											ret += array[i].Length < size ? array[i].Length + 1 : size;
										}
										else
										{
											ret++;
										}
									}
								}
							}
							else
							{
								for (int i=0;i < array.Length;i++)
								{
									ret += (array[i] == null) ? 1 : array[i].Length + 1;
								}
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(UInt64))
						{
							int size = 8;

							UInt64[] array = (UInt64[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								ret = obj._Pack(ret, size);
								ret += array.Length * size;
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(Int64))
						{
							int size = 8;

							Int64[] array = (Int64[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								ret = obj._Pack(ret, size);
								ret += array.Length * size;
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(UInt32))
						{
							int size = 4;

							UInt32[] array = (UInt32[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								ret = obj._Pack(ret, size);
								ret += array.Length * size;
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(Int32))
						{
							int size = 4;

							Int32[] array = (Int32[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								ret = obj._Pack(ret, size);
								ret += array.Length * size;
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(UInt16))
						{
							int size = 2;

							UInt16[] array = (UInt16[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								ret = obj._Pack(ret, size);
								ret += array.Length * size;
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(Int16))
						{
							int size = 2;

							Int16[] array = (Int16[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								ret = obj._Pack(ret, size);
								ret += array.Length * size;
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(Byte))
						{
							Byte[] array = (Byte[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								ret = obj._Pack(ret, 1);
								ret += array.Length;
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(SByte))
						{
							SByte[] array = (SByte[]) fieldInfo.GetValue(obj);

							if (array != null)
							{
								ret = obj._Pack(ret, 1);
								ret += array.Length;
							}
						}
						else if (fieldInfo.FieldType.GetElementType() == typeof(DataSection))
						{
							// todo

						}
					}
					else if (fieldInfo.FieldType == typeof(DataSection))
					{
						DataSection innerSection = (DataSection)fieldInfo.GetValue(obj);;

						ret = obj._Pack(ret, 4);
						ret += innerSection.Size();
					}
					else if ((fieldInfo.FieldType == typeof(Int64))
						||	 (fieldInfo.FieldType == typeof(UInt64)))
					{
						ret = obj._Pack(ret, 8);
						ret += 8;
					}
					else if ((fieldInfo.FieldType == typeof(Int32))
						||	 (fieldInfo.FieldType == typeof(UInt32)))
					{
						ret = obj._Pack(ret, 4);
						ret += 4;
					}
					else if ((fieldInfo.FieldType == typeof(Int16))
						||	 (fieldInfo.FieldType == typeof(UInt16)))
					{
						ret = obj._Pack(ret, 2);
						ret += 2;
					}
					else if ((fieldInfo.FieldType == typeof(SByte))
						||	 (fieldInfo.FieldType == typeof(Byte)))
					{
						ret = obj._Pack(ret, 1);
						ret++;
					}
/*					else
					{
						int size = Marshal.SizeOf(fieldInfo.FieldType);

						ret = obj._Pack(ret, size);
						ret += size;
					}*/
				}
				catch {}
			}

			return ret;
		}

		public abstract StringType TypeOfString {get;}
		public abstract bool LittleEndian {get;}
		public virtual int Pack
		{
			get
			{
				return 1;
			}
		}
		public virtual int Read(byte[] buffer, int offset, int length)
		{
			return ReadObj(this, buffer, offset, length);
		}
		public virtual int Write(byte[] buffer, int offset, int length)
		{
			return WriteObj(this, buffer, offset, length);
		}
		public virtual int Size()
		{
			return SizeObj(this);
		}
		public abstract bool Works();
		public abstract void Empty();
	}

}

