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
using System;

namespace ECGConversion.ECGDemographics
{
	/// <summary>
	/// Interface for manupalation of demograpics information.
	/// </summary>
	public interface IDemographic
	{
		/// <summary>
		/// Function to initialize demographics information.
		/// </summary>
		void Init();
		/// <summary>
		/// Function to get the last name of the patient.
		/// </summary>
		string LastName {get;set;}
		/// <summary>
		/// Function to get the first name of the patient.
		/// </summary>
		string FirstName {get;set;}
		/// <summary>
		/// Function to get the id of the patient.
		/// </summary>
		string PatientID {get;set;}
		/// <summary>
		/// Function to get the second last name of the patient.
		/// </summary>
		string SecondLastName {get;set;}
		/// <summary>
		/// Function to get the prefix of the patient name.
		/// </summary>
		string PrefixName {get;set;}
		/// <summary>
		/// Function to get the suffix of the patient name.
		/// </summary>
		string SuffixName {get;set;}
		/// <summary>
		/// Function to get the age of the patient.
		/// </summary>
		/// <param name="val">age value as defined</param>
		/// <param name="def">definition of the age value</param>
		/// <returns>0 on success</returns>
		int getPatientAge(out ushort val, out AgeDefinition def);
		/// <summary>
		/// Function to set the last name of the patient.
		/// </summary>
		/// <param name="name">last name of patient</param>
		/// <returns>0 on success</returns>
		int setPatientAge(ushort val, AgeDefinition def);
		/// <summary>
		/// Function to get the date of birth of patient.
		/// </summary>
		Date PatientBirthDate {get;set;}
		/// <summary>
		/// Function to get the height of the patient.
		/// </summary>
		/// <param name="val">height value as defined</param>
		/// <param name="def">definition of the height value</param>
		/// <returns>0 on success</returns>
		int getPatientHeight(out ushort val, out HeightDefinition def);
		/// <summary>
		/// Function to set the height of the patient.
		/// </summary>
		/// <param name="val">height value as defined</param>
		/// <param name="def">definition of the height value</param>
		/// <returns>0 on success</returns>
		int setPatientHeight(ushort val, HeightDefinition def);
		/// <summary>
		/// Function to get the weight of the patient.
		/// </summary>
		/// <param name="val">weight value as defined</param>
		/// <param name="def">definition of the weight value</param>
		/// <returns>0 on success</returns>
		int getPatientWeight(out ushort val, out WeightDefinition def);
		/// <summary>
		/// Function to set the weight of the patient.
		/// </summary>
		/// <param name="val">weight value as defined</param>
		/// <param name="def">definition of the weight value</param>
		/// <returns>0 on success</returns>
		int setPatientWeight(ushort val, WeightDefinition def);
		/// <summary>
		/// Function to get sex of patient
		/// </summary>
		Sex Gender  {get;set;}
		/// <summary>
		/// Function to get race of patient.
		/// </summary>
		Race PatientRace {get;set;}
		/// <summary>
		/// Function to get the acquiring machine id.
		/// </summary>
		AcquiringDeviceID AcqMachineID  {get;set;}
		/// <summary>
		/// Function to get the analyzing machine id.
		/// </summary>
		AcquiringDeviceID AnalyzingMachineID {get;set;}
		/// <summary>
		/// Function to get the time of acquisition.
		/// </summary>
		DateTime TimeAcquisition {get;set;}
		/// <summary>
		/// Function to get the high pass baseline filter.
		/// </summary>
		ushort BaselineFilter {get;set;}
		/// <summary>
		/// Function to get the low pass filter.
		/// </summary>
		ushort LowpassFilter {get;set;}
		/// <summary>
		/// Function to get the filter bitmap.
		/// </summary>
		byte FilterBitmap {get;set;}
		/// <summary>
		/// Function to get the free text fields.
		/// </summary>
		string[] FreeTextFields {get;set;}
		/// <summary>
		/// Function to get the sequence number.
		/// </summary>
		string SequenceNr {get;set;}
		/// <summary>
		/// Function to get the acquiring institution.
		/// </summary>
		string AcqInstitution {get;set;}
		/// <summary>
		/// Function to get the analyzing institution.
		/// </summary>
		string AnalyzingInstitution {get;set;}
		/// <summary>
		/// Function to get the acquiring departement.
		/// </summary>
		string AcqDepartment {get;set;}
		/// <summary>
		/// get and set the analyzing departement.
		/// </summary>
		string AnalyzingDepartment {get;set;}
		/// <summary>
		/// Function to get the referring physician.
		/// </summary>
		string ReferringPhysician {get;set;}
		/// <summary>
		/// Function to get the overreading physician.
		/// </summary>
		string OverreadingPhysician {get;set;}
		/// <summary>
		/// Function to get the technician desciption.
		/// </summary>
		string TechnicianDescription {get;set;}
		/// <summary>
		/// Function to get the systolic blood pressure of the patient.
		/// </summary>
		ushort SystolicBloodPressure {get;set;}
		/// <summary>
		/// Function to get the diastolic blood pressure of the patient.
		/// </summary>
		ushort DiastolicBloodPressure {get;set;}
		/// <summary>
		/// Function to get the drugs given to patient.
		/// </summary>
		Drug[] Drugs {get;set;}	// If used at all it probably needs to be implemented better
		/// <summary>
		/// Function to get referral indication.
		/// </summary>
		string[] ReferralIndication {get;set;}
		/// <summary>
		/// Function to get room description.
		/// </summary>
		string RoomDescription {get;set;}
		/// <summary>
		/// Function to get stat code.
		/// </summary>
		byte StatCode {get;set;}
	}
}
