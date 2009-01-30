/***************************************************************************
Copyright (c) 2008, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
#if WINCE
using System;

namespace ECGConversion
{
	/// <summary>
	/// SortedList for .NET Compact.
	/// </summary>
	public class SortedList
	{
		private class ItemHolder
		{
			public ItemHolder(IComparable  k, object o)
			{
				key = k;
				obj = o;
			}

			public IComparable key;
			public object obj;

			public ItemHolder Clone()
			{
				return new ItemHolder(key, obj);
			}
		}

		private static int _AutoResize = 16;
		private ItemHolder[] _Items;
		private int _Count;

		public SortedList()
		{
			_Items = new ItemHolder[_AutoResize];
			_Count = 0;
		}

		public SortedList(SortedList obj)
		{
			_Items = new ItemHolder[obj._Items.Length];
			_Count = obj._Count;

			for (int i=0;i < _Count;i++)
				_Items[i] = obj._Items[i].Clone();
		}

		public void Add(IComparable  key, object obj)
		{
			int index = _IndexOfKey(key);

			if ((index < _Count)
			&&	(key.CompareTo(_Items[index].key) == 0))
			{
				_Items[index].obj = obj;
			}
			else if (_Count == _Items.Length)
			{
				ItemHolder[] items = new ItemHolder[_Items.Length + _AutoResize];

				for (int i=0;i < index;i++)
					items[i] = _Items[i];

				items[index] = new ItemHolder(key, obj);

				for (;index < _Count;index++)
					items[index+1] = _Items[index];

				_Items = items;

				_Count++;
			}
			else
			{
				for (int i=_Count-1;i >= index;i--)
					_Items[i+1] = _Items[i];

				_Items[index] = new ItemHolder(key, obj);

				_Count++;
			}
		}

		public bool ContainsKey(IComparable key)
		{
			int index = _IndexOfKey(key);

			return index < _Count && key.CompareTo(_Items[index].key) == 0;
		}

		public int Count
		{
			get
			{
				return _Count;
			}
		}

		public void Clear()
		{
			for (int i=0;i < _Count;i++)
				_Items[i] = null;

			_Count = 0;
		}

		public object GetByIndex(int index)
		{
			return index >= 0 && index < _Count ? _Items[index].obj : null;
		}

		public IComparable GetKey(int index)
		{
			return index >= 0 && index < _Count ? _Items[index].key : null;
		}

		public int IndexOfKey(IComparable  key)
		{
			int index = _IndexOfKey(key);

			return index < _Count && key.CompareTo(_Items[index].key) == 0 ? index : -1;
		}

		private int _IndexOfKey(IComparable  key)
		{
			int l = 0,
				h = _Count;

			while (l < h)
			{
				int m = (l + h) >> 1;
				int cmp = key.CompareTo(_Items[m].key);

				if (cmp == 0)
					return m;
				else if (cmp < 0)
					h = m;
				else
					l = m + 1;
			}

			return l;
		}

		public void Remove(IComparable  key)
		{
			int index = _IndexOfKey(key);

			if ((index < _Count)
			&&	(key.CompareTo(_Items[index].key) == 0))
				RemoveAt(index);
		}

		public void RemoveAt(int index)
		{
			if ((index >= 0)
			&&	(index < _Count))
			{
				_Count--;

				for (;index < _Count;index++)
					_Items[index] = _Items[index+1];

				_Items[_Count] = null;
			}
		}

		public void SetByIndex(int index, object obj)
		{
			if ((index >= 0)
			&&	(index < _Count))
				_Items[index].obj = obj;
		}

		public object this[IComparable  key]
		{
			get
			{
				int index = _IndexOfKey(key);

				return index < _Count && key.CompareTo(_Items[index].key) == 0 ? _Items[index].obj : null;
			}
			set
			{
				int index = _IndexOfKey(key);

				if ((index < _Count)
				&&	(key.CompareTo(_Items[index].key) == 0))
				{
					_Items[index].obj = value;
				}
				else
				{
					throw new Exception("Key isn't in the list!");
				}
			}
		}

		public object[] Values
		{
			get
			{
				object[] ret = new object[_Count];

				for (int i=0;i < _Count;i++)
				{
					ret[i] = _Items[i].obj;
				}

				return ret;
			}
		}
	}
}
#endif