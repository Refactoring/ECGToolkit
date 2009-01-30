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
using System.Xml;
using System.Reflection;

namespace ECGConversion.aECG
{
	/// <summary>
	/// Summary description for aECGElement.
	/// </summary>
	public abstract class aECGElement
	{
		public static int ReadOne(object obj, XmlReader reader)
		{
			if (obj == null)
				return -2;

            if (reader.NodeType == XmlNodeType.EndElement)
                return 0;

			int ret = 0;

			foreach (FieldInfo fieldInfo in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				try
				{
					if (fieldInfo.FieldType.IsSubclassOf(typeof(aECGElement)))
					{
						aECGElement temp = (aECGElement) fieldInfo.GetValue(obj);

						if ((temp != null)
						&&	(String.Compare(reader.Name, temp.Name) == 0))
						{
							ret = temp.Read(reader);

							return ret;
						}
					}
					else if ((fieldInfo.FieldType == typeof(string))
						&&	 char.IsLower(fieldInfo.Name[0]))
					{
						if (String.Compare(reader.Name, fieldInfo.Name) == 0)
						{
							if (reader.IsEmptyElement)
								return 0;

							reader.Read();

							if ((reader.Name == fieldInfo.Name)
							&&  (reader.NodeType == XmlNodeType.EndElement))
								return 0;

							fieldInfo.SetValue(obj, reader.Value);

							reader.Read();

							if ((reader.Name != fieldInfo.Name)
							||	(reader.NodeType != XmlNodeType.EndElement))
								return 1;

							return 0;
						}
					}
					else if (fieldInfo.FieldType.IsArray
						&&	 fieldInfo.FieldType.HasElementType
                        &&   (fieldInfo.FieldType.GetElementType().IsSubclassOf(typeof(aECGElement))))
					{
                        Array array = (Array)fieldInfo.GetValue(obj);

						if (array != null)
						{
							Type type = fieldInfo.FieldType.GetElementType();

							aECGElement temp = (aECGElement) Activator.CreateInstance(type);

							if (String.Compare(reader.Name, temp.Name) == 0)
							{
								ret = temp.Read(reader);

								if (ret == 0)
								{
									for (int i=0;i < array.Length;i++)
									{
										if (array.GetValue(i) == null)
										{
											array.SetValue(temp, i);
											break;
										}
									}
								}

								return ret;
							}
						}
					}
				}
				catch
				{
				}
			}

			aECGUnknownElement element = new aECGUnknownElement(reader.Name);

			ret = element.Read(reader);

			if (obj.GetType().IsSubclassOf(typeof(aECGElement)))
			{
				((aECGElement)obj).UnknownElements.Add(element);
			}
			else
			{
				try
				{
					FieldInfo fieldInfo = obj.GetType().GetField("UnknownElements");
					
					((ArrayList) fieldInfo.GetValue(obj)).Add(element);
				}
				catch 
				{
					return -3;
				}
			}

			return ret;
		}

		public static int WriteAll(object obj, XmlWriter writer)
		{
			if (obj == null)
				return -2;

			int ret = 0;
			
			foreach (FieldInfo fieldInfo in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				try
				{
					if (fieldInfo.FieldType.IsSubclassOf(typeof(aECGElement)))
					{
						aECGElement temp = (aECGElement) fieldInfo.GetValue(obj);

                        if (temp != null)
                        {
                            ret = temp.Write(writer);

                            if (ret != 0)
                                return ret;
                        }
					}
					else if ((fieldInfo.FieldType == typeof(string))
						&&	 char.IsLower(fieldInfo.Name[0]))
					{
						string val = (string) fieldInfo.GetValue(obj);

						if (val != null)
						{
							writer.WriteStartElement(fieldInfo.Name);
							writer.WriteString(val);
							writer.WriteEndElement();
						}
					}
					else if (fieldInfo.FieldType.IsArray
						&&	 fieldInfo.FieldType.HasElementType
						&&   (fieldInfo.FieldType.GetElementType().IsSubclassOf(typeof(aECGElement))))
					{
						Array array = (Array)fieldInfo.GetValue(obj);

						if (array != null)
						{
							foreach (aECGElement element in array)
							{
								if (element == null)
									break;

								element.Write(writer);
							}
						}
					}
				}
				catch
				{
				}
			}

			if (obj.GetType().IsSubclassOf(typeof(aECGElement)))
			{
				foreach(aECGUnknownElement element in ((aECGElement)obj).UnknownElements)
				{
					ret = element.Write(writer);

					if (ret != 0)
						return ret; 
				}
			}
			else
			{
				try
				{
					FieldInfo fieldInfo = obj.GetType().GetField("UnknownElements");

					foreach(aECGUnknownElement element in (ArrayList) fieldInfo.GetValue(obj))
					{
						ret = element.Write(writer);

						if (ret != 0)
							return ret; 
					}
				}
				catch 
				{
					return -3;
				}
			}

			return ret;
		}

		public static int EmptyAll(object obj)
		{
			if (obj == null)
				return -2;
		
			foreach (FieldInfo fieldInfo in obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				try
				{
					if (fieldInfo.FieldType.IsSubclassOf(typeof(aECGElement)))
					{
						aECGElement temp = (aECGElement) fieldInfo.GetValue(obj);

						temp.Empty();
					}
					else if ((fieldInfo.FieldType == typeof(string))
						&&	 char.IsLower(fieldInfo.Name[0]))
					{
						fieldInfo.SetValue(obj, null);
					}
                    else if (fieldInfo.FieldType.IsArray
                        &&   fieldInfo.FieldType.HasElementType
                        &&   (fieldInfo.FieldType.GetElementType().IsSubclassOf(typeof(aECGElement))))
                    {
                        Array array = (Array)fieldInfo.GetValue(obj);

						if (array != null)
						{
							for (int i = 0; i < array.Length; i++)
								array.SetValue(null, i);

							fieldInfo.SetValue(obj, null);
						}
                    }
				}
				catch
				{
				}
			}

			if (obj.GetType().IsSubclassOf(typeof(aECGElement)))
			{
				((aECGElement)obj).UnknownElements.Clear();
			}
			else
			{
				try
				{
					FieldInfo fieldInfo = obj.GetType().GetField("UnknownElements");
					
					((ArrayList) fieldInfo.GetValue(obj)).Clear();
				}
				catch 
				{
					return -3;
				}
			}

			return 0;
		}

		public aECGElement(string name)
		{
			_Name = name;
		}

		public string Name
		{
			get
			{
				return _Name;
			}
		}

		private string _Name;

		public ArrayList UnknownElements = new ArrayList();

		public abstract int Read(XmlReader reader);
		public abstract int Write(XmlWriter writer);
		public abstract bool Works();
		public virtual void Empty()
		{
			EmptyAll(this);
		}
	}
}
