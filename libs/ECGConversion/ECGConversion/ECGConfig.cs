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
using System.Collections;

namespace ECGConversion
{
	/// <summary>
	/// Configuration class for the ECGConversion Toolkit.
	/// </summary>
	public sealed class ECGConfig
	{
		public delegate bool CheckConfigFunction();

		/// <summary>
		/// Constructor for an ECGConversion Configuration 
		/// </summary>
		/// <param name="mustConf">values that must be set</param>
		/// <param name="posConf">values that can be set</param>
		public ECGConfig(string[] mustConf, string[] posConf, CheckConfigFunction ccf)
		{
			_CheckConfig = ccf;
			_PossibleConfigs = new string[(mustConf != null ? mustConf.Length : 0) + (posConf != null ? posConf.Length : 0)];

			int i=0;

			if (mustConf != null)
			{
				for (;i < mustConf.Length;i++)
					_PossibleConfigs[i] = mustConf[i];
			}

			_MustValue = i;

			if (posConf != null)
			{
				for (int j=0;j < posConf.Length;i++,j++)
					_PossibleConfigs[i] = posConf[j];
			}
		}
		/// <summary>
		/// Constructor for an ECGConversion Configuration 
		/// </summary>
		/// <param name="posConf">all config values that can be set</param>
		/// <param name="bMust">true if all values must be set</param>
		public ECGConfig(string[] posConf, bool bMust, CheckConfigFunction ccf)
		{
			_CheckConfig = ccf;
			_PossibleConfigs = (string[]) posConf.Clone();

			_MustValue = bMust ? _PossibleConfigs.Length : 0;
		}
		/// <summary>
		/// Constructor for an ECGConversion Configuration 
		/// </summary>
		/// <param name="posConf">all config values that can be set</param>
		/// <param name="mustValue">till which possible config value must be set.</param>
		public ECGConfig(string[] posConf, int mustValue, CheckConfigFunction ccf)
		{
			_CheckConfig = ccf;
			_PossibleConfigs = (string[]) posConf.Clone();

			_MustValue = mustValue;
		}
		private int _MustValue;
		private string[] _PossibleConfigs;
		private SortedList _Configs = new SortedList();
		private CheckConfigFunction _CheckConfig;
		/// <summary>
		/// to get and set configuration parts.
		/// </summary>
		public string this[int index]
		{
			get
			{
				if ((index >= 0)
				&&	(index < _PossibleConfigs.Length))
					return _PossibleConfigs[index];

				return null;
			}
		}
		/// <summary>
		/// to get and set configuration parts.
		/// </summary>
		public string this[string val]
		{
			get
			{
				if (_Configs.ContainsKey(val))
					return (string) _Configs[val];

				return null;
			}
			set
			{
				if (IsPartOfConfig(val))
				{
					if ((value == null)
					||	(value.Length == 0))
					{
						if (_Configs.ContainsKey(val))
							_Configs.Remove(val);
					}
					else
					{
						if (_Configs.ContainsKey(val))
							_Configs[val] = value;
						else
							_Configs.Add(val, value);
					}
				}
			}
		}
		/// <summary>
		/// Attribute to count nr of config items.
		/// </summary>
		public int NrConfigItems
		{
			get
			{
				return (_PossibleConfigs == null) ? 0 : _PossibleConfigs.Length;
			}
		}
		/// <summary>
		/// Get a configuration item.
		/// </summary>
		/// <param name="n">nr of configuration item to get.</param>
		/// <param name="name">returns name of configuration item.</param>
		/// <param name="must">returns whether field is mandatory.</param>
		public void getConfigItem(int n, out string name, out bool must)
		{
			name = null;
			must = false;

			if ((_PossibleConfigs != null)
			&&	(n >= 0)
			&&	(n < _PossibleConfigs.Length))
			{
				must = n < _MustValue;
				name = _PossibleConfigs[n];
			}
		}
		/// <summary>
		/// Check whether an configaration value is set.
		/// </summary>
		/// <param name="val">configuration value to check</param>
		/// <returns>true if set</returns>
		public bool IsPartOfConfig(string val)
		{
			for (int i=0;i < _PossibleConfigs.Length;i++)
				if (string.Compare(_PossibleConfigs[i], val) == 0)
					return true;

			return false;
		}
		/// <summary>
		/// Function to check whether configuration works.
		/// </summary>
		/// <returns>true if configuration has got all configuration values that must be set.</returns>
		public bool ConfigurationWorks()
		{
			for (int i=0;i < _MustValue;i++)
				if (!_Configs.ContainsKey(_PossibleConfigs[i]))
					return false;

			return (_CheckConfig == null) || _CheckConfig();
		}
		/// <summary>
		/// Clone configuration 
		/// </summary>
		/// <param name="bFull">to do a deep copy.</param>
		/// <returns>an configuration</returns>
		public ECGConfig Clone(bool bFull)
		{
			ECGConfig ret = new ECGConfig(_PossibleConfigs, _MustValue, _CheckConfig);

			if (bFull)
				ret._Configs = new SortedList(_Configs);

			return ret;
		}

		/// <summary>
		/// Function to set configuration based on configration of same kind
		/// </summary>
		/// <param name="conf">configuration to set current to</param>
		/// <returns>true if successful</returns>
		public bool Set(ECGConfig conf)
		{
			if (conf == this)
				return true;

			if ((conf != null)
			&&	(conf._Configs != null)
			&&	(_Configs != null)
			&&	(conf._PossibleConfigs.Length == _PossibleConfigs.Length)
			&&	(conf._MustValue == _MustValue))
			{
				for (int i=0;i < _PossibleConfigs.Length;i++)
					if (string.Compare(conf._PossibleConfigs[i], _PossibleConfigs[i]) != 0)
						return false;

				_Configs.Clear();

				for (int i=0;i < conf._Configs.Count;i++)
					_Configs.Add(conf._Configs.GetKey(i), conf._Configs.GetByIndex(i));

				return true;
			}

			return false;
		}
	}
}
