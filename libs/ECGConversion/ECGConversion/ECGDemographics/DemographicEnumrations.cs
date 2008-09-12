/***************************************************************************
Copyright 2004, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
namespace ECGConversion.ECGDemographics
{
	/// <summary>
	/// An Enumration to determine the sex.
	/// </summary>
	public enum Sex
	{Unspecified = 0, Male, Female, Null = 0xff}
	/// <summary>
	/// An Enumration to determine the race.
	/// </summary>
	public enum Race
	{Unspecified = 0, Caucasian, Black, Oriental, Null = 0xff}
	/// <summary>
	/// An Enumration to determine the age definition.
	/// </summary>
	public enum AgeDefinition
	{Unspecified = 0, Years, Months, Weeks, Days, Hours}
	/// <summary>
	/// An Enumration to determine the height definition.
	/// </summary>
	public enum HeightDefinition
	{Unspecified = 0, Centimeters, Inches, Millimeters}
	/// <summary>
	/// An Enumration to determine the weight definition.
	/// </summary>
	public enum WeightDefinition
	{Unspecified = 0, Kilogram, Gram, Pound, Ounce}
	/// <summary>
	/// An Enumration to determine the device type.
	/// </summary>
	public enum DeviceType
	{Cart = 0, System}
	/// <summary>
	/// An Enumration to determine the device manufactor.
	/// </summary>
	public enum DeviceManufactor
	{Unknown = 0, Burdick, Cambridge, Compumed, Datamed, Fukuda, HewlettPackard,
		MarquetteElectronics, MortaraInstruments, NihonKohden, Okin, Quintin, Siemens,
		SpaceLabs, Telemed, Hellige, ESAOTE, Schiller, PickerSchwarzer, ElettronicTrentina,
		Zwonitz, Other = 100}
	/// <summary>
	/// An Enumration for Electrode Configurations for 12-lead ECG
	/// </summary>
	public enum ElectrodeConfigCodeTwelveLead
	{
		Unspecified = 0,
		StandardTwelveLead,
		MasonLikarAndIndividual,
		MasonLikarAndPadded,
		AllLeadPadded,
		TwelveLeadDerivedXYZ,
		TwelveLeadDerivedNonStandard
	}
	/// <summary>
	/// An Enumration for Electrode Configurations for XYZ ECG
	/// </summary>
	public enum ElectrodeConfigCodeXYZ
	{
		Unspecified = 0,
		Frank,
		McFeeParungao,
		Cube,
		BipolarUncorrected,
		PseudoOrthogonal,
		XYZDerivedTwelveLead
	}
}