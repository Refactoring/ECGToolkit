/***************************************************************************
Copyright 2019, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands
Copyright 2012-2016, van Ettinger Information Technology, Lopik, The Netherlands
Copyright 2008-2010, Thoraxcentrum, Erasmus MC, Rotterdam, The Netherlands

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
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

using org.dicomcs.data;
using org.dicomcs.dict;

using Communication.IO.Tools;
using ECGConversion;
using ECGConversion.ECGDemographics;
using ECGConversion.ECGDiagnostic;
using ECGConversion.ECGGlobalMeasurements;
using ECGConversion.ECGSignals;
using ECGConversion.ECGLeadMeasurements;

namespace ECGConversion.DICOM
{
    public sealed class DICOMPDFFormat : IECGFormat, ISignal, IDemographic, IDiagnostic, IGlobalMeasurement, ILeadMeasurement
    {
        private const string _PdfFormat = "PDF";
        private const int _MaxPdfSize = 20 * 1024 * 1024;

        private enum GenerateSequenceNr
        {
            // default generate sequence nr values
            False = 0,
            True = 1,
            Always = 2,
            // Other values that are allowed
            No = 0,
            Yes = 1,
            // value that is not allowed
            Error = 0xff,
        }

        [Flags()]
        private enum MortaraDiagCompat
        {
            // default Mortara
            False = 0x0,
            StateOnly = 0x1,
            HistoryOnly = 0x2,
            True = 0x3,
            RoomInStudyDesc = 0x4,

            // alternative values for mortara compat
            No = 0x0,
            Yes = 0x3,

            // value that is not allowed
            Error = 0xff,
        }

        private string _PdfPath
        {
            get
            {
                string ret = _Config["PDF Path"];

                return ret != null ? ret : "";
            }
        }

        private bool _MortaraCompat
        {
            get
            {
                return string.Compare(_Config["Mortara Compatibility"], "true", true) == 0;
            }
        }

        private bool _PutRoomInStudyDescription
        {
            get
            {
                return string.Compare(_Config["Mortara Compatibility"], "RoomInStudyDesc", true) == 0;
            }
        }

        private string _UIDPrefix
        {
            get
            {
                string uidPrefix = _Config["UID Prefix"];

                return uidPrefix == null ? "1.2.826.0.1.34470.2.44.6" : uidPrefix;
            }
        }

        private string _SOPUIDPostfix
        {
            get
            {
                string uidPostfix = _Config["SOP UID Postfix"];

                return uidPostfix == null ? "1" : uidPostfix;
            }
        }

        private string _SeriesUIDPostfix
        {
            get
            {
                string uidPostfix = _Config["Series UID Postfix"];

                return uidPostfix == null ? "2" : uidPostfix;
            }
        }

        private GenerateSequenceNr _GenerateSequenceNr
        {
            get
            {
                string cfg = _Config["Generate SequenceNr"];

                if ((cfg == null)
                || (cfg.Length == 0))
                {
                    return GenerateSequenceNr.False;
                }

                try
                {
                    return (GenerateSequenceNr)ECGConverter.EnumParse(typeof(GenerateSequenceNr), cfg, true);
                }
                catch { }

                return GenerateSequenceNr.Error;
            }
        }

        public Dataset DICOMData
        {
            get
            {
                return _DICOMData;
            }
        }

        public byte[] GetInnerDocument()
        {
            if (Works())
            {
                org.dicomcs.util.ByteBuffer bb = _DICOMData.GetByteBuffer(Tags.EncapsulatedDocument);

                if (bb != null)
                {
                    return bb.ToArray();
                }
            }

            return null;
        }

        private Dataset _DICOMData = null;

        private IECGFormat _InsideFormat = null;

        public DICOMPDFFormat()
        {
            string[] cfgValue = { "PDF Path", "Mortara Compatibility", "UID Prefix", "Series UID Postfix", "SOP UID Postfix", "Generate SequenceNr" };

            Empty();

            if (ECGConversion.ECGConverter.Instance.waitForFormatSupport(_PdfFormat))
            {
                System.Collections.Generic.List<string> listCfg = new System.Collections.Generic.List<string>(cfgValue);

                _InsideFormat = ECGConversion.ECGConverter.Instance.getFormat(_PdfFormat);

                ECGConfig pdfConfig = _InsideFormat.Config;

                for (int i = 0, e = pdfConfig.NrConfigItems; i < e; i++)
                {
                    string name;
                    bool must;

                    pdfConfig.getConfigItem(i, out name, out must);

                    listCfg.Add(name);
                }

                _Config = new ECGConfig(listCfg.ToArray(), 0, new ECGConfig.CheckConfigFunction(this._ConfigurationWorks));

                for (int i = 0, e = pdfConfig.NrConfigItems; i < e; i++)
                {
                    string name;
                    bool must;

                    pdfConfig.getConfigItem(i, out name, out must);

                    _Config[name] = pdfConfig[name];
                }
            }
            else
            {
                _Config = new ECGConfig(cfgValue, 1, new ECGConfig.CheckConfigFunction(this._ConfigurationWorks));
            }

            _Config["PDF Path"] = "";
            _Config["Mortara Compatibility"] = "false";
            _Config["UID Prefix"] = "1.2.826.0.1.34470.2.44.6";
            _Config["Series UID Postfix"] = "0";
            _Config["SOP UID Postfix"] = "1";
        }

        public bool _ConfigurationWorks()
        {
            try
            {
                if (_InsideFormat != null)
                {
                    if ((_PdfPath == null)
                    ||  (_PdfPath.Length == 0))
                    {
                        ECGConfig pdfConfig = _InsideFormat.Config;

                        for (int i = 0, e = pdfConfig.NrConfigItems; i < e; i++)
                        {
                            string name;
                            bool must;

                            pdfConfig.getConfigItem(i, out name, out must);

                            pdfConfig[name] = _Config[name];
                        }

                        return pdfConfig.ConfigurationWorks()
                            && (_GenerateSequenceNr != GenerateSequenceNr.Error);
                    }
                    else if (_PdfPath.StartsWith("http://")
                        ||   _PdfPath.StartsWith("https://"))
                    {
                        return (_GenerateSequenceNr != GenerateSequenceNr.Error);
                    }
                    else
                    {
                        return File.Exists(_PdfPath)
                            && (_GenerateSequenceNr != GenerateSequenceNr.Error);
                    }
                }
                else
                {
                    return File.Exists(_PdfPath)
                        && (_GenerateSequenceNr != GenerateSequenceNr.Error);
                }
            }
            catch { }

            return false;
        }

        public override int Read(Stream input, int offset)
        {
            if (CheckFormat(_DICOMData))
                return 0;

            try
            {
                input.Seek(offset, SeekOrigin.Begin);
                DcmParser parser = new DcmParser(input);

                FileFormat ff = parser.DetectFileFormat();

                if (ff != null)
                {
                    parser.DcmHandler = _DICOMData.DcmHandler;

                    parser.ParseDcmFile(ff, Tags.PixelData);

                    return CheckFormat(_DICOMData) ? 0 : 2;
                }
            }
            catch { }

            return 1;
        }
        public override int Read(string file, int offset)
        {
            if (CheckFormat(_DICOMData))
                return 0;

            FileStream stream = null;

            try
            {
                stream = new FileStream(file, FileMode.Open);

                return Read(stream, offset);
            }
            catch { }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
            }

            return 1;
        }
        public override int Read(byte[] buffer, int offset)
        {
            if (CheckFormat(_DICOMData))
                return 0;

            System.IO.MemoryStream ms = null;

            try
            {
                ms = new MemoryStream(buffer, offset, buffer.Length - offset, false);

                return Read(ms, 0);
            }
            catch { }
            finally
            {
                if (ms != null)
                {
                    ms.Close();
                    ms = null;
                }
            }

            return 2;
        }
        public override int Write(Stream output)
        {
            try
            {
                bool pdfLoaded = false;

                if ((_PdfPath != null)
                &&  (_PdfPath.StartsWith("http://") || _PdfPath.StartsWith("https://")))
                {
                    try
                    {
                        byte[] baTemp = null;
                        int o = 0;

                        using (System.Net.WebResponse rsp = System.Net.WebRequest.Create(_PdfPath).GetResponse())
                        {
                            if (rsp.ContentType.StartsWith("text/html", StringComparison.InvariantCultureIgnoreCase))
                            {
                                string url = rsp.ResponseUri.ToString().Replace(rsp.ResponseUri.Query, "").Replace("DisplayPDFResting.aspx", "PdfTemp.aspx");

                                using (System.Net.WebResponse rsp2 = System.Net.WebRequest.Create(url).GetResponse())
                                {
                                    if (string.Compare(rsp2.ContentType, "application/pdf", true) == 0)
                                    {
                                        baTemp = new byte[_MaxPdfSize];
                                        o = 0;

                                        try
                                        {
                                            using (Stream stream = rsp2.GetResponseStream())
                                            {
                                                for (int r = 1; o < baTemp.Length && r != 0; )
                                                    o += (r = stream.Read(baTemp, o, baTemp.Length - o));
                                            }
                                        }
                                        catch { }
                                    }
                                }
                            }
                            else if (string.Compare(rsp.ContentType, "application/pdf", true) == 0)
                            {
                                baTemp = new byte[_MaxPdfSize];
                                o = 0;

                                using (Stream stream = rsp.GetResponseStream())
                                {
                                    for (int r=1;o < baTemp.Length&&r != 0;)
                                        o += (r = stream.Read(baTemp, o, baTemp.Length - o));
                                }
                            }
                        }

                        if (baTemp != null)
                        {
                            int size = o;

                            byte[] baBuffer = new byte[size];

                            BytesTool.copy(baBuffer, 0, baTemp, 0, size);

                            _DICOMData.PutOB(Tags.EncapsulatedDocument, baBuffer);

                            pdfLoaded = true;
                        }
                    }
                    catch { }
                }


                if (!pdfLoaded
                &&  File.Exists(_PdfPath))
                {
                    _DICOMData.PutOB(Tags.EncapsulatedDocument, File.ReadAllBytes(_PdfPath));

                    pdfLoaded = true;
                }
                else if (!pdfLoaded
                    &&   (_InsideFormat != null)
                    &&   _ConfigurationWorks())
                {
                    byte[] baTemp = new byte[_MaxPdfSize];

                    _InsideFormat.Write(baTemp, 0);

                    int size = LookForPdfEnd(baTemp, 0);

                    byte[] baBuffer = new byte[size];

                    BytesTool.copy(baBuffer, 0, baTemp, 0, size);

                    _DICOMData.PutOB(Tags.EncapsulatedDocument, baBuffer);

                    pdfLoaded = true;
                }

                if (pdfLoaded)
                {
                    FileMetaInfo fmi = _DICOMData.GetFileMetaInfo();

                    if (fmi != null)
                    {
                        DcmEncodeParam param = DcmDecodeParam.ValueOf(fmi.GetString(Tags.TransferSyntaxUID));

                        if (_MortaraCompat)
                        {
                            param = new DcmEncodeParam(org.dicomcs.util.ByteOrder.LITTLE_ENDIAN, false, false, false, true, false, false);

                            fmi.PutUI(Tags.TransferSyntaxUID, UIDs.ImplicitVRLittleEndian);
                        }

                        _DICOMData.WriteFile(output, param);
                    }
                    else
                    {
                        _DICOMData.WriteFile(output, org.dicomcs.data.DcmEncodeParam.EVR_LE);
                    }

                    return 0;
                }

                return 2;
            }
            catch { }

            return 1;
        }
        public override int Write(string file)
        {
            FileStream output = null;

            try
            {
                output = new FileStream(file, FileMode.Create);

                return Write(output);
            }
            catch { }
            finally
            {
                if (output != null)
                {
                    output.Close();
                    output = null;
                }
            }

            return 1;
        }
        public override int Write(byte[] buffer, int offset)
        {
            System.IO.MemoryStream ms = null;

            try
            {
                ms = new MemoryStream(buffer, offset, buffer.Length - offset, true);

                return Write(ms);
            }
            catch { }
            finally
            {
                if (ms != null)
                {
                    ms.Close();
                    ms = null;
                }
            }

            return 2;
        }
        public bool CheckFormat(Dataset ds)
        {
            if (ds == null)
                return false;

            bool ret = false;

            switch (ds.GetString(Tags.SOPClassUID))
            {
                case UIDs.EncapsulatedPDFStorage:
                    ret = true;
                    break;
                default:
                    break;
            }

            ret = ret && (string.Compare(ds.GetString(Tags.Modality), "ECG") == 0);

            if (!ret)
            {
                Empty();
            }

            return ret;
        }
        public override bool CheckFormat(Stream input, int offset)
        {
            try
            {
                input.Seek(offset, SeekOrigin.Begin);

                DcmParser parser = new DcmParser(input);

                FileFormat ff = parser.DetectFileFormat();

                if (ff != null)
                {
                    parser.DcmHandler = _DICOMData.DcmHandler;

                    parser.ParseDcmFile(ff, Tags.PixelData);

                    return CheckFormat(_DICOMData);
                }
            }
            catch { }

            return false;
        }
        public override bool CheckFormat(string file, int offset)
        {
            FileStream stream = null;

            try
            {
                stream = new FileStream(file, FileMode.Open);

                return CheckFormat(stream, offset);
            }
            catch { }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
            }

            return false;
        }
        public override bool CheckFormat(byte[] buffer, int offset)
        {
            System.IO.MemoryStream ms = null;

            try
            {
                ms = new MemoryStream(buffer, offset, buffer.Length - offset, false);

                return CheckFormat(ms, 0);
            }
            catch { }
            finally
            {
                if (ms != null)
                {
                    ms.Close();
                    ms = null;
                }
            }

            return false;
        }
        public override IDemographic Demographics
        {
            get
            {
                return this;
            }
        }
        public override IDiagnostic Diagnostics
        {
            get
            {
                return this;
            }
        }
        public override IGlobalMeasurement GlobalMeasurements
        {
            get
            {
                return this;
            }
        }
        public override ISignal Signals
        {
            get
            {
                return this;
            }
        }
        public override ILeadMeasurement LeadMeasurements
        {
            get
            {
                return this;
            }
        }
        public override void Anonymous(byte type)
        {
            ECGTool.Anonymous(this, (char)type);
        }
        public override int getFileSize()
        {
            return -1;
        }
        public override bool Works()
        {
            return CheckFormat(_DICOMData);
        }
        public override void Empty()
        {
            _DICOMData = new Dataset();
        }

        #region IDisposable Members
        public override void Dispose()
        {
            base.Dispose();

            _DICOMData = null;

            if (_InsideFormat != null)
            {
                _InsideFormat.Dispose();

                _InsideFormat = null;
            }
        }
        #endregion

        #region ISignal Members
        public int getSignals(out Signals signals)
        {
            signals = new Signals();
            int err = getSignalsToObj(signals);
            if (err != 0)
            {
                signals = null;
            }
            return err;
        }

        public int getSignalsToObj(Signals signals)
        {
            return 1;
        }

        public int setSignals(Signals signals)
        {
            if ((_InsideFormat != null)
            &&  (_InsideFormat.Signals != null))
            {
                return _InsideFormat.Signals.setSignals(signals);
            }

            return 0;
        }
        #endregion

        #region IDemographic Members
        public void Init()
        {
            Empty();

            try
            {
                FileMetaInfo fmi = new FileMetaInfo();

                fmi.PutOB(Tags.FileMetaInformationVersion, new byte[] { 0, 1 });
                fmi.PutUI(Tags.MediaStorageSOPClassUID, UIDs.EncapsulatedPDFStorage);
                fmi.PutUI(Tags.MediaStorageSOPInstanceUID, "1.3.6.1.4.1.1.24.04.1985");
                fmi.PutUI(Tags.TransferSyntaxUID, (_MortaraCompat ? UIDs.ImplicitVRLittleEndian : UIDs.ExplicitVRLittleEndian));
                fmi.PutUI(Tags.ImplementationClassUID, "2.24.985.4");
                fmi.PutSH(Tags.ImplementationVersionName, "ECGConversion2");

                _DICOMData.SetFileMetaInfo(fmi);

                _DICOMData.PutDA(Tags.InstanceCreationDate, DateTime.Now);
                _DICOMData.PutTM(Tags.InstanceCreationTime, DateTime.Now);
                _DICOMData.PutUI(Tags.SOPClassUID, UIDs.EncapsulatedPDFStorage);
                _DICOMData.PutUI(Tags.SOPInstanceUID, "1.3.6.1.4.1.1.24.04.1985");
                _DICOMData.PutDA(Tags.StudyDate, "");
                _DICOMData.PutDA(Tags.ContentDate, "");
                _DICOMData.PutDT(Tags.AcquisitionDateTime, "");
                _DICOMData.PutTM(Tags.StudyTime, "");
                _DICOMData.PutTM(Tags.ContentTime, "");
                _DICOMData.PutSH(Tags.AccessionNumber, "");
                _DICOMData.PutCS(Tags.Modality, "ECG");
                _DICOMData.PutLO(Tags.Manufacturer, "");
                _DICOMData.PutPN(Tags.ReferringPhysicianName, "");
                _DICOMData.PutPN(Tags.NameOfPhysiciansReadingStudy, "");
                _DICOMData.PutPN(Tags.OperatorsName, "");
                _DICOMData.PutLO(Tags.ManufacturerModelName, "");
                _DICOMData.PutPN(Tags.PatientName, "");
                _DICOMData.PutLO(Tags.PatientID, "");
                _DICOMData.PutDA(Tags.PatientBirthDate, "");
                _DICOMData.PutCS(Tags.PatientSex, "");
                _DICOMData.PutLO(Tags.OtherPatientIDs, "");
                _DICOMData.PutAS(Tags.PatientAge, "");
                _DICOMData.PutDS(Tags.PatientSize, "");
                _DICOMData.PutDS(Tags.PatientWeight, "");
                _DICOMData.PutLO(Tags.DeviceSerialNumber, "");
                _DICOMData.PutLO(Tags.SoftwareVersions, "");
                _DICOMData.PutUI(Tags.StudyInstanceUID, "1.1.1.1.1");
                _DICOMData.PutUI(Tags.SeriesInstanceUID, "1.1.1.1.2");
                _DICOMData.PutSH(Tags.StudyID, "");
                _DICOMData.PutIS(Tags.SeriesNumber, "1");
                _DICOMData.PutIS(Tags.InstanceNumber, "1");
                _DICOMData.PutCS(Tags.Laterality, "");
                _DICOMData.PutLO(Tags.CurrentPatientLocation, "");
                _DICOMData.PutLO(Tags.PatientInstitutionResidence, "");
                _DICOMData.PutLT(Tags.VisitComments, "");
                _DICOMData.PutLO(Tags.ReasonForTheRequestedProcedure, "");
                _DICOMData.PutLO(Tags.MIMETypeOfEncapsulatedDocument, @"application\pdf");

                if ((_InsideFormat != null)
                &&  (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.Init();
                }
            }
            catch { }

        }
        private string getName(int pos)
        {
            PersonName pn = new PersonName(_DICOMData.GetString(Tags.PatientName));

            return pn.Get(pos);
        }
        private int setName(string name, int pos)
        {
            PersonName pn = new PersonName(_DICOMData.GetString(Tags.PatientName));

            pn.Set(pos, name);

            _DICOMData.PutPN(Tags.PatientName, pn);

            return 0;
        }
        public string LastName
        {
            get { return getName(PersonName.FAMILY); }
            set
            {
                setName(value, PersonName.FAMILY);

                if ((_InsideFormat != null)
                &&  (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.LastName = value;
                }
            }
        }
        public string FirstName
        {
            get { return getName(PersonName.GIVEN); }
            set
            {
                setName(value, PersonName.GIVEN);

                if ((_InsideFormat != null)
                && (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.FirstName = value;
                }
            
            }
        }
        public string PatientID
        {
            get { return _DICOMData.GetString(Tags.PatientID); }
            set
            {
                if (value != null) _DICOMData.PutLO(Tags.PatientID, value);

                if ((_InsideFormat != null)
                && (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.PatientID = value;
                }
            }
        }
        public string SecondLastName
        {
            get { return getName(PersonName.MIDDLE); }
            set
            {
                setName(value, PersonName.MIDDLE);

                if ((_InsideFormat != null)
                && (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.SecondLastName = value;
                }
            }
        }
        public string PrefixName
        {
            get { return getName(PersonName.PREFIX); }
            set
            {
                setName(value, PersonName.PREFIX);

                if ((_InsideFormat != null)
                && (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.PrefixName = value;
                }
            }
        }
        public string SuffixName
        {
            get { return getName(PersonName.SUFFIX); }
            set
            {
                setName(value, PersonName.SUFFIX);

                if ((_InsideFormat != null)
                &&  (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.SuffixName = value;
                }
            }
        }
        public int getPatientAge(out ushort val, out AgeDefinition def)
        {
            val = 0;
            def = AgeDefinition.Unspecified;

            string temp = _DICOMData.GetString(Tags.PatientAge);

            if (temp != null)
            {
                try
                {
                    val = DICOMFormat.ParseUShort(temp.Substring(0, temp.Length - 1));

                    switch (temp[temp.Length - 1])
                    {
                        case 'D':
                        case 'd':
                            def = AgeDefinition.Days;
                            break;
                        case 'W':
                        case 'w':
                            def = AgeDefinition.Weeks;
                            break;
                        case 'M':
                        case 'm':
                            def = AgeDefinition.Months;
                            break;
                        case 'Y':
                        case 'y':
                            def = AgeDefinition.Years;
                            break;
                        default:
                            val = DICOMFormat.ParseUShort(temp);
                            def = AgeDefinition.Years;
                            break;
                    }
                }
                catch { }
            }

            return def == AgeDefinition.Unspecified ? 1 : 0;
        }
        public int setPatientAge(ushort val, AgeDefinition def)
        {
            string temp = null; ;

            try
            {

                switch (def)
                {
                    case AgeDefinition.Hours:
                        temp = (((val - 1) / 24) + 1).ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + 'D';
                        break;
                    case AgeDefinition.Days:
                        temp = val.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + 'D';
                        break;
                    case AgeDefinition.Weeks:
                        temp = val.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + 'W';
                        break;
                    case AgeDefinition.Months:
                        temp = val.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + 'M';
                        break;
                    case AgeDefinition.Years:
                        temp = val.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + 'Y';
                        break;
                    default:
                        temp = null;
                        break;
                }
            }
            catch
            {
                temp = null;
            }

            if ((_InsideFormat != null)
            && (_InsideFormat.Demographics != null))
            {
                _InsideFormat.Demographics.setPatientAge(val, def);
            }

            if (temp != null)
            {
                _DICOMData.PutAS(Tags.PatientAge, temp);

                return 0;
            }

            return 1;
        }
        public Date PatientBirthDate
        {
            get
            {
                DateTime time = _DICOMData.GetDate(Tags.PatientBirthDate);

                if (time.Year > 1000)
                    return new Date((ushort)time.Year, (byte)time.Month, (byte)time.Day);

                return null;
            }
            set
            {
                if ((value != null)
                && value.isExistingDate())
                {
                    _DICOMData.PutDA(Tags.PatientBirthDate, new DateTime(value.Year, value.Month, value.Day));

                    if ((_InsideFormat != null)
                    && (_InsideFormat.Demographics != null))
                    {
                        _InsideFormat.Demographics.PatientBirthDate = value;
                    }
                }
                else
                {
                    if ((_InsideFormat != null)
                    && (_InsideFormat.Demographics != null))
                    {
                        _InsideFormat.Demographics.PatientBirthDate = null;
                    }

                    if (_DICOMData.GetItem(Tags.PatientBirthDate) != null)
                    {
                        _DICOMData.Remove(Tags.PatientBirthDate);
                    }
                }
            }
        }
        public int getPatientHeight(out ushort val, out HeightDefinition def)
        {
            val = 0;
            def = HeightDefinition.Unspecified;

            try
            {
                double val2 = DICOMFormat.ParseDouble(_DICOMData.GetString(Tags.PatientSize));

                if (val >= 0.1)
                {
                    val = (ushort)(val2 * 100);
                    def = HeightDefinition.Centimeters;
                }
                else
                {
                    val = (ushort)(val2 * 1000);
                    def = HeightDefinition.Millimeters;
                }

                return 0;
            }
            catch { }


            return 1;
        }
        public int setPatientHeight(ushort val, HeightDefinition def)
        {
            double val2 = double.MinValue;

            switch (def)
            {
                case HeightDefinition.Centimeters:
                    val2 = val * 0.01;
                    break;
                case HeightDefinition.Inches:
                    val2 = val * 0.0254;
                    break;
                case HeightDefinition.Millimeters:
                    val2 = val * 0.001;
                    break;
            }

            if ((_InsideFormat != null)
            && (_InsideFormat.Demographics != null))
            {
                _InsideFormat.Demographics.setPatientHeight(val, def);
            }

            if (val2 > 0)
            {
                val2 = Math.Round(val2 * 100.0) * 0.01;

                _DICOMData.PutDS(Tags.PatientSize, val2.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat));

                return 0;
            }

            return 1;
        }
        public int getPatientWeight(out ushort val, out WeightDefinition def)
        {
            val = 0;
            def = WeightDefinition.Unspecified;

            try
            {
                double val2 = DICOMFormat.ParseDouble(_DICOMData.GetString(Tags.PatientWeight));

                if (val2 >= 1.0)
                {
                    val = (ushort)val2;
                    def = WeightDefinition.Kilogram;
                }
                else
                {
                    val = (ushort)(val2 * 1000.0);
                    def = WeightDefinition.Gram;
                }

                return 0;
            }
            catch { }

            return 1;
        }
        public int setPatientWeight(ushort val, WeightDefinition def)
        {
            double val2 = double.MinValue;

            switch (def)
            {
                case WeightDefinition.Gram:
                    val2 = val * 0.001;
                    break;
                case WeightDefinition.Kilogram:
                    val2 = val;
                    break;
                case WeightDefinition.Ounce:
                    val2 = (val * 0.0283495231);
                    break;
                case WeightDefinition.Pound:
                    val2 = (val * 0.45359237);
                    break;
            }

            if ((_InsideFormat != null)
            &&  (_InsideFormat.Demographics != null))
            {
                _InsideFormat.Demographics.setPatientWeight(val, def);
            }

            if (val2 > 0)
            {
                val2 = Math.Round(val2 * 100.0) * 0.01;

                _DICOMData.PutDS(Tags.PatientWeight, val2.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat));

                return 0;
            }

            return 1;
        }
        public Sex Gender
        {
            get
            {
                string val = _DICOMData.GetString(Tags.PatientSex);
                if (val != null)
                {
                    switch (val)
                    {
                        case "M":
                            return Sex.Male;
                        case "F":
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
                    string sexText = "N";

                    switch (value)
                    {
                        case Sex.Male:
                            sexText = "M";
                            break;
                        case Sex.Female:
                            sexText = "F";
                            break;
                    }

                    if (sexText != null)
                        _DICOMData.PutCS(Tags.PatientSex, sexText);

                    if ((_InsideFormat != null)
                    &&  (_InsideFormat.Demographics != null))
                    {
                        _InsideFormat.Demographics.Gender = value;
                    }
                }
            }
        }
        public Race PatientRace
        {
            get { return Race.Null; }
            set
            {
                if ((_InsideFormat != null)
                &&  (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.PatientRace = value;
                }
            }
        }
        public AcquiringDeviceID AcqMachineID
        {
            get
            {
                AcquiringDeviceID id = new AcquiringDeviceID(true);

                byte map = FilterBitmap;

                if (map != 0)
                {
                    if ((map & 0x1) == 0x1)
                        id.ACFrequencyEnvironment = 2;
                    else if ((map & 0x2) == 0x2)
                        id.ACFrequencyEnvironment = 1;
                    else
                        id.ACFrequencyEnvironment = 0;
                }

                return id;
            }
            set
            {
                _DICOMData.PutLO(Tags.Manufacturer, ((DeviceManufactor)value.ManufactorID).ToString());
                _DICOMData.PutLO(Tags.ManufacturerModelName, BytesTool.readString(value.ModelDescription, 0, value.ModelDescription.Length));

                if ((_InsideFormat != null)
                &&  (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.AcqMachineID = value;
                }
            }
        }
        public AcquiringDeviceID AnalyzingMachineID
        {
            get { return null; }
            set
            {
                if ((_InsideFormat != null)
                &&  (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.AnalyzingMachineID = value;
                }
            }
        }
        public DateTime TimeAcquisition
        {
            get
            {
                DateTime time = _DICOMData.GetDate(Tags.AcquisitionDateTime);

                if (time.Year <= 1000)
                    time = _DICOMData.GetDate(Tags.AcquisitionDate);

                return time;
            }
            set
            {
                _DICOMData.PutDA(Tags.StudyDate, value);
                _DICOMData.PutDA(Tags.ContentDate, value);
                _DICOMData.PutDT(Tags.AcquisitionDateTime, value);
                _DICOMData.PutTM(Tags.StudyTime, value);
                _DICOMData.PutTM(Tags.ContentTime, value);

                string
                    val = "1",
                    uid = _UIDPrefix + (_UIDPrefix.EndsWith(".") ? "" : ".") + value.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat) + ".";

                // code to generate a sequence nr if not provided or always
                if ((_GenerateSequenceNr == GenerateSequenceNr.True)
                ||  (_GenerateSequenceNr == GenerateSequenceNr.Always))
                {
                    Random r = new Random();

                    val = r.Next(1, 9999).ToString();

                    _DICOMData.PutST(Tags.AccessionNumber, val);
                }

                uid += val;

                FileMetaInfo fmi = _DICOMData.GetFileMetaInfo();
                if (fmi != null)
                    fmi.PutUI(Tags.MediaStorageSOPInstanceUID, uid + (_SOPUIDPostfix.StartsWith(".") ? "" : ".") +_SOPUIDPostfix);

                _DICOMData.PutUI(Tags.SOPInstanceUID, uid + (_SOPUIDPostfix.StartsWith(".") ? "" : ".") +_SOPUIDPostfix);
                _DICOMData.PutUI(Tags.StudyInstanceUID, uid);
                _DICOMData.PutUI(Tags.SeriesInstanceUID, uid + (_SeriesUIDPostfix.StartsWith(".") ? "" : ".") + _SeriesUIDPostfix);

                if (_SOPUIDPostfix.Contains("."))
                {
                    string[] tmp = _SOPUIDPostfix.Split('.');

                    _DICOMData.PutIS(Tags.InstanceNumber, tmp[tmp.Length - 1]);
                }
                else
                {
                    _DICOMData.PutIS(Tags.InstanceNumber, _SOPUIDPostfix);
                }

                if (_SeriesUIDPostfix.Contains("."))
                {
                    string[] tmp = _SeriesUIDPostfix.Split('.');

                    _DICOMData.PutIS(Tags.SeriesNumber, tmp[tmp.Length - 1]);
                }
                else
                {
                    _DICOMData.PutIS(Tags.SeriesNumber, _SeriesUIDPostfix);
                }

                if ((_InsideFormat != null)
                && (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.TimeAcquisition = value;
                }
            }
        }
        public ushort BaselineFilter
        {
            get { return 0; }
            set
            {
                if ((_InsideFormat != null)
                &&  (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.BaselineFilter = value;
                }
            }
        }
        public ushort LowpassFilter
        {
            get { return 0; }
            set
            {
                if ((_InsideFormat != null)
                && (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.LowpassFilter = value;
                }
            }
        }
        public byte FilterBitmap
        {
            get { return 0; }
            set
            {
                if ((_InsideFormat != null)
                && (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.FilterBitmap = value;
                }
            }
        }
        public string[] FreeTextFields
        {
            get { return _DICOMData.GetStrings(Tags.VisitComments); }
            set 
            {
                if (value != null) _DICOMData.PutLT(Tags.VisitComments, value);

                if ((_InsideFormat != null)
                && (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.FreeTextFields = value;
                }
            }
        }
        public string SequenceNr
        {
            get
            {
                string temp1 = _DICOMData.GetString(Tags.StudyInstanceUID);

                if (temp1 != null)
                {
                    string[] temp2 = temp1.Split('.');

                    return temp2[temp2.Length - 1];
                }

                return null;
            }
            set
            {
                string
                    val = value,
                    uid = _UIDPrefix + (_UIDPrefix.EndsWith(".") ? "" : ".") + TimeAcquisition.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat) + ".";

                // code to generate a sequence nr if not provided or always
                if (((_GenerateSequenceNr == GenerateSequenceNr.Always)
                ||   (_GenerateSequenceNr == GenerateSequenceNr.True))
                &&   ((val == null)
                ||    (val.Length == 0)))
                {
                    Random r = new Random();

                    val = r.Next(1, 9999).ToString();
                }

                if ((val == null)
                || (val.Length == 0))
                    uid += "1";
                else
                    uid += val;

                FileMetaInfo fmi = _DICOMData.GetFileMetaInfo();
                if (fmi != null)
                    fmi.PutUI(Tags.MediaStorageSOPInstanceUID, uid + (_SOPUIDPostfix.StartsWith(".") ? "" : ".") + _SOPUIDPostfix);

                _DICOMData.PutUI(Tags.SOPInstanceUID, uid + (_SOPUIDPostfix.StartsWith(".") ? "" : ".") + _SOPUIDPostfix);
                _DICOMData.PutUI(Tags.StudyInstanceUID, uid);
                _DICOMData.PutUI(Tags.SeriesInstanceUID, uid + (_SeriesUIDPostfix.StartsWith(".") ? "" : ".") + _SeriesUIDPostfix);

                if (_SOPUIDPostfix.Contains("."))
                {
                    string[] tmp = _SOPUIDPostfix.Split('.');

                    _DICOMData.PutIS(Tags.InstanceNumber, tmp[tmp.Length - 1]);
                }
                else
                {
                    _DICOMData.PutIS(Tags.InstanceNumber, _SOPUIDPostfix);
                }

                if (_SeriesUIDPostfix.Contains("."))
                {
                    string[] tmp = _SeriesUIDPostfix.Split('.');

                    _DICOMData.PutIS(Tags.SeriesNumber, tmp[tmp.Length - 1]);
                }
                else
                {
                    _DICOMData.PutIS(Tags.SeriesNumber, _SeriesUIDPostfix);
                }

                if (val != null)
                {
                    _DICOMData.PutST(Tags.AccessionNumber, val);
                }
                else if (_DICOMData.Get(Tags.AccessionNumber) != null)
                {
                    _DICOMData.Remove(Tags.AccessionNumber);
                }

                if ((_InsideFormat != null)
                && (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.SequenceNr = value;
                }
            }
        }
        public string AcqInstitution
        {
            get { return _DICOMData.GetString(Tags.InstitutionName); }
            set
            {
                if (value != null) _DICOMData.PutLO(Tags.InstitutionName, value);

                if ((_InsideFormat != null)
                && (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.AcqInstitution = value;
                }
            }
        }
        public string AnalyzingInstitution
        {
            get { return null; }
            set
            {
                if ((_InsideFormat != null)
                && (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.AnalyzingInstitution = value;
                }
            }
        }
        public string AcqDepartment
        {
            get { return _DICOMData.GetString(Tags.InstitutionalDepartmentName); }
            set
            {
                if (value != null) _DICOMData.PutLO(Tags.InstitutionalDepartmentName, value);

                if ((_InsideFormat != null)
                &&  (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.AcqDepartment = value;
                }
            }
        }
        public string AnalyzingDepartment
        {
            get { return null; }
            set
            {
                if ((_InsideFormat != null)
                &&  (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.AnalyzingDepartment= value;
                }
            }
        }
        public string ReferringPhysician
        {
            get { return _DICOMData.GetString(Tags.ReferringPhysicianName); }
            set
            {
                if (value != null) _DICOMData.PutPN(Tags.ReferringPhysicianName, value);

                if ((_InsideFormat != null)
                &&  (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.ReferringPhysician = value;
                }
            }
        }
        public string OverreadingPhysician
        {
            get { return _DICOMData.GetString(Tags.NameOfPhysiciansReadingStudy); }
            set
            {
                if (value != null) _DICOMData.PutPN(Tags.NameOfPhysiciansReadingStudy, value);

                if ((_InsideFormat != null)
                &&  (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.OverreadingPhysician = value;
                }
            }
        }
        public string TechnicianDescription
        {
            get { return _DICOMData.GetString(Tags.OperatorsName); }
            set
            {
                if (value != null) _DICOMData.PutPN(Tags.OperatorsName, value);

                if ((_InsideFormat != null)
                && (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.TechnicianDescription = value;
                }
            }
        }
        public ushort SystolicBloodPressure
        {
            get { return 0; }
            set
            {
                if ((_InsideFormat != null)
                && (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.SystolicBloodPressure = value;
                }
            }
        }
        public ushort DiastolicBloodPressure
        {
            get { return 0; }
            set
            {
                if ((_InsideFormat != null)
                && (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.DiastolicBloodPressure = value;
                }
            }
        }
        public Drug[] Drugs
        {
            get { return null; }
            set
            {
                if ((_InsideFormat != null)
                &&  (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.Drugs = value;
                }
            }
        }
        public string[] ReferralIndication
        {
            get { return null; }
            set
            {
                if ((_InsideFormat != null)
                &&  (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.ReferralIndication = value;
                }
            }
        }
        public string RoomDescription
        {
            get { return null; }
            set
            {
                if ((_InsideFormat != null)
                &&  (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.RoomDescription = value;
                }

                if (value != null)
                {
                    _DICOMData.PutLO(Tags.CurrentPatientLocation, value);

                    if (_PutRoomInStudyDescription)
                    {
                        _DICOMData.PutLO(Tags.StudyDescription, value);
                    }

                    // code to generate add uid using RoomDescription to make a unique id.
                    if ((_GenerateSequenceNr == GenerateSequenceNr.False)
                    && (value != null)
                    && (value.Length > 0)
                    && ((SequenceNr == null)
                    || (string.Compare(SequenceNr, "1") == 0)))
                    {
                        byte[] buff = new byte[2 * (value.Length + 1)];
                        BytesTool.writeString(Encoding.Unicode, value, buff, 0, buff.Length);

                        CRCTool crc = new CRCTool();

                        string
                            val = crc.CalcCRCITT(buff, 0, buff.Length).ToString(),
                            uid = _UIDPrefix + (_UIDPrefix.EndsWith(".") ? "" : ".") + TimeAcquisition.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat) + ".";

                        if ((val == null)
                        || (val.Length == 0))
                            uid += "1";
                        else
                            uid += val;

                        FileMetaInfo fmi = _DICOMData.GetFileMetaInfo();
                        if (fmi != null)
                            fmi.PutUI(Tags.MediaStorageSOPInstanceUID, uid + (_SOPUIDPostfix.StartsWith(".") ? "" : ".") + _SOPUIDPostfix);

                        _DICOMData.PutUI(Tags.SOPInstanceUID, uid + (_SOPUIDPostfix.StartsWith(".") ? "" : ".") + _SOPUIDPostfix);
                        _DICOMData.PutUI(Tags.StudyInstanceUID, uid);
                        _DICOMData.PutUI(Tags.SeriesInstanceUID, uid + (_SeriesUIDPostfix.StartsWith(".") ? "" : ".") + _SeriesUIDPostfix);

                        if (_SOPUIDPostfix.Contains("."))
                        {
                            string[] tmp = _SOPUIDPostfix.Split('.');

                            _DICOMData.PutIS(Tags.InstanceNumber, tmp[tmp.Length - 1]);
                        }
                        else
                        {
                            _DICOMData.PutIS(Tags.InstanceNumber, _SOPUIDPostfix);
                        }
                    }
                }
                else if (_DICOMData.GetItem(Tags.CurrentPatientLocation) != null)
                {
                    _DICOMData.Remove(Tags.CurrentPatientLocation);
                }
            }
        }
        public byte StatCode
        {
            get { return 0xff; }
            set
            {
                if ((_InsideFormat != null)
                &&  (_InsideFormat.Demographics != null))
                {
                    _InsideFormat.Demographics.StatCode = value;
                }
            }
        }
        #endregion

        #region IDiagnostic Members
        public int getDiagnosticStatements(out Statements stat)
        {
            stat = null;

            return 1;
        }

        public int setDiagnosticStatements(Statements stat)
        {
            if ((_InsideFormat != null)
            &&  (_InsideFormat.Diagnostics != null))
            {
                _InsideFormat.Diagnostics.setDiagnosticStatements(stat);
            }

            return 0;
        }
        #endregion

        #region IGlobalMeasurement Members
        public int getGlobalMeasurements(out GlobalMeasurements mes)
        {
            mes = null;

            return 1;
        }

        public int setGlobalMeasurements(GlobalMeasurements mes)
        {
            if ((_InsideFormat != null)
            &&  (_InsideFormat.GlobalMeasurements != null))
            {
                _InsideFormat.GlobalMeasurements.setGlobalMeasurements(mes);
            }

            return 0;
        }
        #endregion

        #region ILeadMeasurement
        public int getLeadMeasurements(out LeadMeasurements mes)
        {
            mes = null;
            return 1;
        }
        
        public int setLeadMeasurements(LeadMeasurements mes)
        {
            if ((_InsideFormat != null)
            &&  (_InsideFormat.LeadMeasurements != null))
            {
                _InsideFormat.LeadMeasurements.setLeadMeasurements(mes);
            }

            return 0;
        }
        #endregion

        /// <summary>
		/// Look for the end of a pdf document.
		/// </summary>
		/// <param name="abBuffer">buffer to look for the end.</param>
		/// <param name="offset">offset to start looking from.</param>
		/// <returns>end of pdf file.</returns>
		private static int LookForPdfEnd(byte[] abBuffer, int offset)
		{
			if (offset >= 0)
			{
				byte[] test = {0x45, 0x4f, 0x46, 0x0A};

				int end = abBuffer.Length - test.Length; 

				for (;offset < end;offset++)
				{
					int i=0;

					for (;i < test.Length;i++)
						if (abBuffer[offset+i] != test[i])
							break;

					if (i == test.Length)
					{
						offset+=test.Length;
						break;
					}
				}
			}

			return offset;
		}

    }
}
