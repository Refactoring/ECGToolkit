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

****************************************************************************/
using System;
using System.Collections;
using System.Data.SqlClient;
using System.Text;

using org.dicomcs.data;
using org.dicomcs.dict;
using org.dicomcs.net;

namespace ECGConversion
{
	using ECGManagementSystem;
	using DICOM;

	public class PACS : IECGManagementSystem
	{
		class PACSUID
		{
			public PACSUID(string studyUID)
			{
				StudyInstanceUID = studyUID;
				SeriesInstanceUID = null;
				SOPInstanceUID = null;
			}

			public PACSUID(string studyUID, string seriesUID)
			{
				StudyInstanceUID = studyUID;
				SeriesInstanceUID = seriesUID;
				SOPInstanceUID = null;
			}

			public PACSUID(string studyUID, string seriesUID, string instanceUID)
			{
				StudyInstanceUID = studyUID;
				SeriesInstanceUID = seriesUID;
				SOPInstanceUID = instanceUID;
			}

			public readonly string StudyInstanceUID;
			public readonly string SeriesInstanceUID;
			public readonly string SOPInstanceUID;
		}

		public PACS()
		{
			string[]
				must = {"Server", "AESCU", "AESCP", "Port"},
				poss = {"WADO URL"};

			_Config = new ECGConfig(must, poss, new ECGConfig.CheckConfigFunction(this._Works));

			_Config["Server"] = "127.0.0.1";
			_Config["AESCU"] = "STORESCU";
			_Config["AESCP"] = "STORESCP";
			_Config["Port"] = "104";
		}

		protected ECGConfig _Config;

		public override ECGConfig Config
		{
			get {return _Config;}
		}

		public override string Name
		{
			get {return "PACS";}
		}

		public override string FormatName
		{
			get {return "DICOM";}
		}

		public override ECGInfo[] getECGList(string patid)
		{
			if (!Works())
				return null;

			ECGInfo[] ret = null;

			try
			{
				BasicSCU scu = new BasicSCU(_Config["AESCU"], _Config["AESCP"], _Config["Server"], int.Parse(_Config["Port"]), 1000);

				string[] modalities = {"ECG"};

				Dataset[] dsa = scu.CFindStudy(patid, modalities);

				if (dsa != null)
				{
					ArrayList alResult = new ArrayList();

					foreach (Dataset ds in dsa)
					{
						string genderText = ds.GetString(Tags.PatientSex);

						ECGDemographics.Sex gender = ECGDemographics.Sex.Unspecified;

						switch (genderText)
						{
							case "M": case "m":
								gender = ECGDemographics.Sex.Male;
								break;
							case "F": case "f":
								gender = ECGDemographics.Sex.Female;
								break;
						}

						alResult.Add(new ECGInfo(
							new PACSUID(ds.GetString(Tags.StudyInstanceUID)),
							ds.GetString(Tags.PatientID),
							ds.GetString(Tags.PatientName),
							ds.GetDateTime(Tags.StudyDate, Tags.StudyTime),
							gender));
					}

					if (alResult.Count > 0)
					{
						ret = new ECGInfo[alResult.Count];

						for (int i=0;i < ret.Length;i++)
							ret[i] = (ECGInfo) alResult[i];
					}
				}	
			}
			catch {}

			return ret;
		}

		public override IECGFormat getECGByUI(object ui)
		{
			if (Works()
			&&	(ui != null)
			&&	(ui.GetType() == typeof(PACSUID)))
			{
				BasicSCU scu = new BasicSCU(_Config["AESCU"], _Config["AESCP"], _Config["Server"], int.Parse(_Config["Port"]), 5000);

				PACSUID uid = (PACSUID) ui;

				if (Config["WADO URL"] == null)
				{
					Dataset[] ds = scu.CGet(uid.StudyInstanceUID, uid.SeriesInstanceUID, uid.SOPInstanceUID);

					if ((ds != null)
					&&	(ds.Length > 0))
					{
						IECGFormat ret = new DICOMFormat(ds[0]);

						if (ret.Works())
							return ret;
					}
				}
				else
				{
					Dataset ds = scu.WADOGet(Config["WADO URL"], uid.StudyInstanceUID, uid.SeriesInstanceUID, uid.SOPInstanceUID);

					if (ds != null)
					{
						IECGFormat ret = new DICOMFormat(ds);

						if (ret.Works())
							return ret;
					}
				}
			}

			return null;
		}

		public override int SaveECG(IECGFormat ecg, string patid, ECGConfig cfg)
		{
			if (Works()
			&&	(_Config["AESCU"] != null)
			&&	(_Config["AESCP"] != null)
			&&	(_Config["Port"] != null))
			{
				if ((ecg != null)
				&&	ecg.Works()
				&&	(ecg.GetType() != typeof(DICOM.DICOMFormat)))
				{
					IECGFormat dst = null;

					int ret = ECGConverter.Instance.Convert(ecg, FormatName, cfg, out dst);

					if (ret != 0)
						return 2;

					if ((dst != null)
					&&	dst.Works())
						ecg = dst;
				}

				if ((ecg != null)
				&&	ecg.Works()
				&&	(ecg.GetType() == typeof(DICOM.DICOMFormat)))
				{
					if (patid != null)
						ecg.Demographics.PatientID = patid;

					try
					{
						DICOM.DICOMFormat dcm = (DICOM.DICOMFormat) ecg;

						BasicSCU scu = new BasicSCU(_Config["AESCU"], _Config["AESCP"], _Config["Server"], int.Parse(_Config["Port"]), 5000);

						scu.CStore(dcm.DICOMData);

						return 0;
					}
					catch {}

					return 3;
				}

				return 2;
			}

			return 1;
		}

		public bool _Works()
		{
			string temp = Config["Port"];

			return temp == null
				|| System.Text.RegularExpressions.Regex.IsMatch(temp, "^[0-9]+$");
		}

	}
}