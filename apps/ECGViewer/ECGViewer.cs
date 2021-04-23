/***************************************************************************
Copyright 2012-2014,2021, van Ettinger Information Technology, Lopik, The Netherlands
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
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using ECGConversion;
using ECGConversion.ECGDiagnostic;
using ECGConversion.ECGGlobalMeasurements;
using ECGConversion.ECGSignals;

namespace ECGViewer
{
	/// <summary>
	/// Summary description for ECGViewer.
	/// </summary>
	public class ECGViewer : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Panel ECGPanel;
		private System.Windows.Forms.MainMenu mainMenu;
		private System.Windows.Forms.MenuItem menuOpen;
		private System.Windows.Forms.MenuItem menuOpenFile;
		private System.Windows.Forms.StatusBar statusBar;
		private System.Windows.Forms.MenuItem menuClose;
		private System.Windows.Forms.OpenFileDialog openECGFileDialog;
		private System.Windows.Forms.Panel InnerECGPanel;
		private System.Windows.Forms.Label labelPatient;
		private System.Windows.Forms.Label labelTime;
		private System.Windows.Forms.TextBox labelDiagnostic;
		private System.Windows.Forms.MenuItem menuSave;
		private System.Windows.Forms.SaveFileDialog saveECGFileDialog;
		private System.Windows.Forms.MenuItem menuSaveFile;
		private System.Windows.Forms.MenuItem menuPlugin;
		private System.Windows.Forms.MenuItem menuAddPluginFile;
		private System.Windows.Forms.MenuItem menuAddPluginDir;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
		private System.Windows.Forms.Label labelPatientSecond;
		private System.Windows.Forms.MenuItem menuView;
		private System.Windows.Forms.MenuItem menuLeadFormat;
		private System.Windows.Forms.MenuItem menuLeadFormatRegular;
		private System.Windows.Forms.MenuItem menuLeadFormatThreeXFour;
		private System.Windows.Forms.MenuItem menuLeadFormatThreeXFourPlusOne;
		private System.Windows.Forms.MenuItem menuLeadFormatThreeXFourPlusThree;
		private System.Windows.Forms.MenuItem menuLeadFormatSixXTwo;
		private System.Windows.Forms.MenuItem menuLeadFormatMedian;
		private System.Windows.Forms.MenuItem menuGain;
		private System.Windows.Forms.MenuItem menuGain4;
		private System.Windows.Forms.MenuItem menuGain3;
		private System.Windows.Forms.MenuItem menuGain2;
		private System.Windows.Forms.MenuItem menuGain1;
		private System.Windows.Forms.MenuItem menuOpenSystems;
        private System.Windows.Forms.MenuItem menuSaveSystems;
        private IContainer components;

		private UnknownECGReader _ECGReader = null;
		private IECGFormat _CurrentECG = null;
		private IECGFormat CurrentECG
		{
			get
			{
				lock (this)
				{
					return _CurrentECG;
				}
			}
			set
			{
				lock (this)
				{
					_Zoom = 1;
					menuZoomIn.Enabled = true;
					menuZoomOut.Enabled = false;

					_OffsetX = 0;
					_OffsetY = 0;

					if ((_CurrentECG != null)
					&&	(_CurrentECG != value))
						_CurrentECG.Dispose();

					if (value == null)
					{
						if (_CurrentECG != null)
							_CurrentECG.Dispose();

						_CurrentECG = null;
						_CurrentSignal = null;
						ECGTimeScrollbar.Visible = false;
						ECGTimeScrollbar.Enabled = false;
					}
					else
					{
						ECGTimeScrollbar.Visible = true;
						ECGTimeScrollbar.Enabled = false;

						Gain = 10f;
						_CurrentECG = value;

						if (_CurrentECG.Signals.getSignals(out _CurrentSignal) != 0)
						{
							this.statusBar.Text = "Failed to get signal!";

							_CurrentECG.Dispose();
							_CurrentECG = null;
						}
						else
						{
							if (_CurrentSignal != null)
							{
								for (int i=0,e=_CurrentSignal.NrLeads;i < e;i++)
								{
									ECGTool.NormalizeSignal(_CurrentSignal[i].Rhythm, _CurrentSignal.RhythmSamplesPerSecond);
								}
							}

							Signals sig = _CurrentSignal.CalculateTwelveLeads();
                            if (sig == null)
                                sig = _CurrentSignal.CalculateFifteenLeads();

							if (sig != null)
								_CurrentSignal = sig;

							if (_CurrentSignal.IsBuffered)
							{
								BufferedSignals bs = _CurrentSignal.AsBufferedSignals;

								bs.LoadSignal(bs.RealRhythmStart, bs.RealRhythmStart + 60 * bs.RhythmSamplesPerSecond);

								ECGTimeScrollbar.Minimum = 0;
								ECGTimeScrollbar.Maximum = bs.RealRhythmEnd - bs.RealRhythmStart;
								ECGTimeScrollbar.Value = 0;
								ECGTimeScrollbar.SmallChange = _CurrentSignal.RhythmSamplesPerSecond;
								ECGTimeScrollbar.LargeChange = _CurrentSignal.RhythmSamplesPerSecond;
							}
							else
							{
								int start, end;

								_CurrentSignal.CalculateStartAndEnd(out start, out end);

								ECGTimeScrollbar.Minimum = 0;
								ECGTimeScrollbar.Maximum = end - start;
								ECGTimeScrollbar.Value = 0;
								ECGTimeScrollbar.SmallChange = _CurrentSignal.RhythmSamplesPerSecond;
								ECGTimeScrollbar.LargeChange = _CurrentSignal.RhythmSamplesPerSecond;
							}
						}

						ECGDraw.ECGDrawType dt = ECGDraw.PossibleDrawTypes(_CurrentSignal);

						menuLeadFormatRegular.Enabled = (dt & ECGDraw.ECGDrawType.Regular) != 0;
						menuLeadFormatThreeXFour.Enabled = (dt & ECGDraw.ECGDrawType.ThreeXFour) != 0;
						menuLeadFormatThreeXFourPlusOne.Enabled = (dt & ECGDraw.ECGDrawType.ThreeXFourPlusOne) != 0;
						menuLeadFormatThreeXFourPlusThree.Enabled = (dt & ECGDraw.ECGDrawType.ThreeXFourPlusThree) != 0;
						menuLeadFormatSixXTwo.Enabled = (dt & ECGDraw.ECGDrawType.SixXTwo) != 0;
						menuLeadFormatMedian.Enabled = (dt & ECGDraw.ECGDrawType.Median) != 0;

						if ((menuLeadFormatThreeXFour.Checked && !menuLeadFormatThreeXFour.Enabled)
						||	(menuLeadFormatThreeXFourPlusOne.Checked && !menuLeadFormatThreeXFourPlusOne.Enabled)
						||	(menuLeadFormatThreeXFourPlusThree.Checked && !menuLeadFormatThreeXFourPlusThree.Enabled)
						||	(menuLeadFormatSixXTwo.Checked && !menuLeadFormatSixXTwo.Enabled)
						||	(menuLeadFormatMedian.Checked && !menuLeadFormatMedian.Enabled))
						{
							CheckLeadFormat(ECGDraw.ECGDrawType.Regular, false);
						}
					}

					_DrawBuffer = null;
				}
			}
		}
		private Signals _CurrentSignal = null;
		private Bitmap DrawBuffer
		{
			get
			{
				lock (this)
				{
					return _DrawBuffer;
				}
			}
			set
			{
				lock (this)
				{
					if (_DrawBuffer != null)
						_DrawBuffer.Dispose();

					_DrawBuffer = value;
				}
			}
		}
		private Bitmap _DrawBuffer = null;
		private ECGDraw.ECGDrawType _DrawType = ECGDraw.ECGDrawType.Regular;
		private System.Windows.Forms.HScrollBar ECGTimeScrollbar;
		private System.Windows.Forms.MenuItem menuAnnonymize;
		private System.Windows.Forms.MenuItem menuDisplayInfo;
		private System.Windows.Forms.MenuItem menuGridType;
		private System.Windows.Forms.MenuItem menuGridFive;
		private System.Windows.Forms.MenuItem menuGridOne;
		private System.Windows.Forms.MenuItem menuGridNone;
		private System.Windows.Forms.MenuItem menuColor;
		private System.Windows.Forms.MenuItem menuColor1;
		private System.Windows.Forms.MenuItem menuColor2;
		private System.Windows.Forms.MenuItem menuColor3;
		private System.Windows.Forms.MenuItem menuColor4;
		private System.Windows.Forms.MenuItem menuZoom;
		private System.Windows.Forms.MenuItem menuZoomOut;
		private System.Windows.Forms.MenuItem menuZoomIn;
		private System.Windows.Forms.MenuItem menuCaliper;
		private System.Windows.Forms.MenuItem menuCaliperOff;
		private System.Windows.Forms.MenuItem menuCaliperDuration;
		private System.Windows.Forms.MenuItem menuCaliperBoth;
        private MenuItem menuFilter;
        private MenuItem menuFilterNone;
        private MenuItem menuFilter40Hz;
        private MenuItem menuFilterMuscle;
        private double _BottomCutoff = double.NaN;
        private double _TopCutoff = double.NaN;

	
		private float Gain
		{
			get
			{
				lock (this)
				{
					return _Gain;
				}
			}
			set
			{
				lock (this)
				{
					if (value == 40f)
					{
						_Gain = value;

						menuGain4.Checked = true;
						menuGain3.Checked = false;
						menuGain2.Checked = false;
						menuGain1.Checked = false;
					}
					else if (value == 20f)
					{
						_Gain = value;

						menuGain4.Checked = false;
						menuGain3.Checked = true;
						menuGain2.Checked = false;
						menuGain1.Checked = false;
					}
					else if (value == 10f)
					{
						_Gain = value;

						menuGain4.Checked = false;
						menuGain3.Checked = false;
						menuGain2.Checked = true;
						menuGain1.Checked = false;
					}
					else if (value == 5f)
					{
						_Gain = value;

						menuGain4.Checked = false;
						menuGain3.Checked = false;
						menuGain2.Checked = false;
						menuGain1.Checked = true;
					}
				}
			}
		}
		private float _Gain = 10f;

		public ECGViewer(string[] args)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			SetColors(-1);

			this.menuGridNone.Checked = ECGDraw.DisplayGrid == ECGDraw.GridType.None;
			this.menuGridOne.Checked = ECGDraw.DisplayGrid == ECGDraw.GridType.OneMillimeters;
			this.menuGridFive.Checked = ECGDraw.DisplayGrid == ECGDraw.GridType.FiveMillimeters;
			CheckVersion.OnAllowNewVersionCheck += new CheckVersion.AllowNewVersionCheckCallback(CheckVersion_OnAllowNewVersionCheck);
			CheckVersion.OnNewVersion += new CheckVersion.NewVersionCallback(CheckVersion_OnNewVersion);

			ECGConverter.Instance.OnNewPlugin += new ECGConverter.NewPluginDelegate(this.LoadECGMS);

			if (ECGConverter.Instance.allPluginsLoaded())
			{
				LoadECGMS(ECGConverter.Instance);
			}

			if (args.Length == 1)
			{
				_ECGReader = new UnknownECGReader();

				IECGFormat format = _ECGReader.Read(args[0]);
 
				if (format != null)
				{
					CurrentECG = format; 
 
					if (CurrentECG != null)
					{
						this.statusBar.Text = "Opened file!";
					}
				}
				else
				{
					CurrentECG = null;
 
					this.statusBar.Text = "Failed to open file!";
				}
 
				this.InnerECGPanel.Refresh();
			}
		}

        [STAThread]
		public static void Main(string[] args)
		{
			Application.Run(new ECGViewer(args));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
                if (_CurrentECG != null)
                {
                    _CurrentECG.Dispose();
                }

				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ECGViewer));
            this.mainMenu = new System.Windows.Forms.MainMenu(this.components);
            this.menuOpen = new System.Windows.Forms.MenuItem();
            this.menuOpenFile = new System.Windows.Forms.MenuItem();
            this.menuOpenSystems = new System.Windows.Forms.MenuItem();
            this.menuView = new System.Windows.Forms.MenuItem();
            this.menuLeadFormat = new System.Windows.Forms.MenuItem();
            this.menuLeadFormatRegular = new System.Windows.Forms.MenuItem();
            this.menuLeadFormatThreeXFour = new System.Windows.Forms.MenuItem();
            this.menuLeadFormatThreeXFourPlusOne = new System.Windows.Forms.MenuItem();
            this.menuLeadFormatThreeXFourPlusThree = new System.Windows.Forms.MenuItem();
            this.menuLeadFormatSixXTwo = new System.Windows.Forms.MenuItem();
            this.menuLeadFormatMedian = new System.Windows.Forms.MenuItem();
            this.menuFilter = new System.Windows.Forms.MenuItem();
            this.menuFilterNone = new System.Windows.Forms.MenuItem();
            this.menuFilter40Hz = new System.Windows.Forms.MenuItem();
            this.menuFilterMuscle = new System.Windows.Forms.MenuItem();
            this.menuGain = new System.Windows.Forms.MenuItem();
            this.menuGain4 = new System.Windows.Forms.MenuItem();
            this.menuGain3 = new System.Windows.Forms.MenuItem();
            this.menuGain2 = new System.Windows.Forms.MenuItem();
            this.menuGain1 = new System.Windows.Forms.MenuItem();
            this.menuGridType = new System.Windows.Forms.MenuItem();
            this.menuGridNone = new System.Windows.Forms.MenuItem();
            this.menuGridOne = new System.Windows.Forms.MenuItem();
            this.menuGridFive = new System.Windows.Forms.MenuItem();
            this.menuColor = new System.Windows.Forms.MenuItem();
            this.menuColor1 = new System.Windows.Forms.MenuItem();
            this.menuColor2 = new System.Windows.Forms.MenuItem();
            this.menuColor3 = new System.Windows.Forms.MenuItem();
            this.menuColor4 = new System.Windows.Forms.MenuItem();
            this.menuCaliper = new System.Windows.Forms.MenuItem();
            this.menuCaliperOff = new System.Windows.Forms.MenuItem();
            this.menuCaliperDuration = new System.Windows.Forms.MenuItem();
            this.menuCaliperBoth = new System.Windows.Forms.MenuItem();
            this.menuZoom = new System.Windows.Forms.MenuItem();
            this.menuZoomOut = new System.Windows.Forms.MenuItem();
            this.menuZoomIn = new System.Windows.Forms.MenuItem();
            this.menuDisplayInfo = new System.Windows.Forms.MenuItem();
            this.menuAnnonymize = new System.Windows.Forms.MenuItem();
            this.menuSave = new System.Windows.Forms.MenuItem();
            this.menuSaveFile = new System.Windows.Forms.MenuItem();
            this.menuSaveSystems = new System.Windows.Forms.MenuItem();
            this.menuClose = new System.Windows.Forms.MenuItem();
            this.menuPlugin = new System.Windows.Forms.MenuItem();
            this.menuAddPluginFile = new System.Windows.Forms.MenuItem();
            this.menuAddPluginDir = new System.Windows.Forms.MenuItem();
            this.ECGPanel = new System.Windows.Forms.Panel();
            this.InnerECGPanel = new System.Windows.Forms.Panel();
            this.labelPatientSecond = new System.Windows.Forms.Label();
            this.labelDiagnostic = new System.Windows.Forms.TextBox();
            this.labelTime = new System.Windows.Forms.Label();
            this.labelPatient = new System.Windows.Forms.Label();
            this.statusBar = new System.Windows.Forms.StatusBar();
            this.openECGFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveECGFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.ECGTimeScrollbar = new System.Windows.Forms.HScrollBar();
            this.ECGPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainMenu
            // 
            this.mainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuOpen,
            this.menuView,
            this.menuSave,
            this.menuClose,
            this.menuPlugin});
            // 
            // menuOpen
            // 
            this.menuOpen.Index = 0;
            this.menuOpen.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuOpenFile,
            this.menuOpenSystems});
            this.menuOpen.Text = "Open";
            // 
            // menuOpenFile
            // 
            this.menuOpenFile.Index = 0;
            this.menuOpenFile.Shortcut = System.Windows.Forms.Shortcut.CtrlO;
            this.menuOpenFile.Text = "File ...";
            this.menuOpenFile.Click += new System.EventHandler(this.menuOpenFile_Click);
            // 
            // menuOpenSystems
            // 
            this.menuOpenSystems.Enabled = false;
            this.menuOpenSystems.Index = 1;
            this.menuOpenSystems.Text = "ECG System";
            // 
            // menuView
            // 
            this.menuView.Enabled = false;
            this.menuView.Index = 1;
            this.menuView.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuLeadFormat,
            this.menuFilter,
            this.menuGain,
            this.menuGridType,
            this.menuColor,
            this.menuCaliper,
            this.menuZoom,
            this.menuDisplayInfo,
            this.menuAnnonymize});
            this.menuView.Text = "View";
            // 
            // menuLeadFormat
            // 
            this.menuLeadFormat.Index = 0;
            this.menuLeadFormat.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuLeadFormatRegular,
            this.menuLeadFormatThreeXFour,
            this.menuLeadFormatThreeXFourPlusOne,
            this.menuLeadFormatThreeXFourPlusThree,
            this.menuLeadFormatSixXTwo,
            this.menuLeadFormatMedian});
            this.menuLeadFormat.Text = "Lead Format";
            // 
            // menuLeadFormatRegular
            // 
            this.menuLeadFormatRegular.Checked = true;
            this.menuLeadFormatRegular.Index = 0;
            this.menuLeadFormatRegular.RadioCheck = true;
            this.menuLeadFormatRegular.Shortcut = System.Windows.Forms.Shortcut.Ctrl1;
            this.menuLeadFormatRegular.Text = "Regular";
            this.menuLeadFormatRegular.Click += new System.EventHandler(this.menuLeadFormatRegular_Click);
            // 
            // menuLeadFormatThreeXFour
            // 
            this.menuLeadFormatThreeXFour.Index = 1;
            this.menuLeadFormatThreeXFour.RadioCheck = true;
            this.menuLeadFormatThreeXFour.Shortcut = System.Windows.Forms.Shortcut.Ctrl2;
            this.menuLeadFormatThreeXFour.Text = "3x4";
            this.menuLeadFormatThreeXFour.Click += new System.EventHandler(this.menuLeadFormatFourXThree_Click);
            // 
            // menuLeadFormatThreeXFourPlusOne
            // 
            this.menuLeadFormatThreeXFourPlusOne.Index = 2;
            this.menuLeadFormatThreeXFourPlusOne.RadioCheck = true;
            this.menuLeadFormatThreeXFourPlusOne.Shortcut = System.Windows.Forms.Shortcut.Ctrl3;
            this.menuLeadFormatThreeXFourPlusOne.Text = "3x4+1";
            this.menuLeadFormatThreeXFourPlusOne.Click += new System.EventHandler(this.menuLeadFormatFourXThreePlusOne_Click);
            // 
            // menuLeadFormatThreeXFourPlusThree
            // 
            this.menuLeadFormatThreeXFourPlusThree.Index = 3;
            this.menuLeadFormatThreeXFourPlusThree.RadioCheck = true;
            this.menuLeadFormatThreeXFourPlusThree.Shortcut = System.Windows.Forms.Shortcut.Ctrl4;
            this.menuLeadFormatThreeXFourPlusThree.Text = "3x4+3";
            this.menuLeadFormatThreeXFourPlusThree.Click += new System.EventHandler(this.menuLeadFormatFourXThreePlusThree_Click);
            // 
            // menuLeadFormatSixXTwo
            // 
            this.menuLeadFormatSixXTwo.Index = 4;
            this.menuLeadFormatSixXTwo.RadioCheck = true;
            this.menuLeadFormatSixXTwo.Shortcut = System.Windows.Forms.Shortcut.Ctrl5;
            this.menuLeadFormatSixXTwo.Text = "6x2";
            this.menuLeadFormatSixXTwo.Click += new System.EventHandler(this.menuLeadFormatSixXTwo_Click);
            // 
            // menuLeadFormatMedian
            // 
            this.menuLeadFormatMedian.Index = 5;
            this.menuLeadFormatMedian.Shortcut = System.Windows.Forms.Shortcut.CtrlM;
            this.menuLeadFormatMedian.Text = "Average Complex";
            this.menuLeadFormatMedian.Click += new System.EventHandler(this.menuLeadFormatMedian_Click);
            // 
            // menuFilter
            // 
            this.menuFilter.Index = 1;
            this.menuFilter.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuFilterNone,
            this.menuFilter40Hz,
            this.menuFilterMuscle});
            this.menuFilter.Text = "Filter";
            // 
            // menuFilterNone
            // 
            this.menuFilterNone.Checked = true;
            this.menuFilterNone.Index = 0;
            this.menuFilterNone.Text = "None";
            this.menuFilterNone.Click += new System.EventHandler(this.menuFilterNone_Click);
            // 
            // menuFilter40Hz
            // 
            this.menuFilter40Hz.Index = 1;
            this.menuFilter40Hz.Text = "40 Hz (0.05-40 Hz)";
            this.menuFilter40Hz.Click += new System.EventHandler(this.menuFilter40Hz_Click);
            // 
            // menuFilterMuscle
            // 
            this.menuFilterMuscle.Index = 2;
            this.menuFilterMuscle.Text = "Muscle (0.05-35 Hz)";
            this.menuFilterMuscle.Click += new System.EventHandler(this.menuFilterMuscle_Click);
            // 
            // menuGain
            // 
            this.menuGain.Index = 2;
            this.menuGain.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuGain4,
            this.menuGain3,
            this.menuGain2,
            this.menuGain1});
            this.menuGain.Text = "Gain";
            // 
            // menuGain4
            // 
            this.menuGain4.Index = 0;
            this.menuGain4.RadioCheck = true;
            this.menuGain4.Text = "40 mm/mV";
            this.menuGain4.Click += new System.EventHandler(this.menuGain4_Click);
            // 
            // menuGain3
            // 
            this.menuGain3.Index = 1;
            this.menuGain3.RadioCheck = true;
            this.menuGain3.Text = "20 mm/mV";
            this.menuGain3.Click += new System.EventHandler(this.menuGain3_Click);
            // 
            // menuGain2
            // 
            this.menuGain2.Checked = true;
            this.menuGain2.Index = 2;
            this.menuGain2.RadioCheck = true;
            this.menuGain2.Text = "10 mm/mV";
            this.menuGain2.Click += new System.EventHandler(this.menuGain2_Click);
            // 
            // menuGain1
            // 
            this.menuGain1.Index = 3;
            this.menuGain1.RadioCheck = true;
            this.menuGain1.Text = "5   mm/mV";
            this.menuGain1.Click += new System.EventHandler(this.menuGain1_Click);
            // 
            // menuGridType
            // 
            this.menuGridType.Index = 3;
            this.menuGridType.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuGridNone,
            this.menuGridOne,
            this.menuGridFive});
            this.menuGridType.Text = "Grid Type";
            // 
            // menuGridNone
            // 
            this.menuGridNone.Index = 0;
            this.menuGridNone.RadioCheck = true;
            this.menuGridNone.Text = "None";
            this.menuGridNone.Click += new System.EventHandler(this.menuGridNone_Click);
            // 
            // menuGridOne
            // 
            this.menuGridOne.Index = 1;
            this.menuGridOne.RadioCheck = true;
            this.menuGridOne.Text = "1 mm";
            this.menuGridOne.Click += new System.EventHandler(this.menuGridOne_Click);
            // 
            // menuGridFive
            // 
            this.menuGridFive.Index = 2;
            this.menuGridFive.RadioCheck = true;
            this.menuGridFive.Text = "5 mm";
            this.menuGridFive.Click += new System.EventHandler(this.menuGridFive_Click);
            // 
            // menuColor
            // 
            this.menuColor.Index = 4;
            this.menuColor.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuColor1,
            this.menuColor2,
            this.menuColor3,
            this.menuColor4});
            this.menuColor.Text = "Color";
            // 
            // menuColor1
            // 
            this.menuColor1.Index = 0;
            this.menuColor1.RadioCheck = true;
            this.menuColor1.Text = "Red / Black";
            this.menuColor1.Click += new System.EventHandler(this.menuColor1_Click);
            // 
            // menuColor2
            // 
            this.menuColor2.Index = 1;
            this.menuColor2.RadioCheck = true;
            this.menuColor2.Text = "Blue / Black";
            this.menuColor2.Click += new System.EventHandler(this.menuColor2_Click);
            // 
            // menuColor3
            // 
            this.menuColor3.Index = 2;
            this.menuColor3.RadioCheck = true;
            this.menuColor3.Text = "Green / Black";
            this.menuColor3.Click += new System.EventHandler(this.menuColor3_Click);
            // 
            // menuColor4
            // 
            this.menuColor4.Index = 3;
            this.menuColor4.RadioCheck = true;
            this.menuColor4.Text = "Gray / Green";
            this.menuColor4.Click += new System.EventHandler(this.menuColor4_Click);
            // 
            // menuCaliper
            // 
            this.menuCaliper.Index = 5;
            this.menuCaliper.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuCaliperOff,
            this.menuCaliperDuration,
            this.menuCaliperBoth});
            this.menuCaliper.Text = "Caliper";
            // 
            // menuCaliperOff
            // 
            this.menuCaliperOff.Checked = true;
            this.menuCaliperOff.Index = 0;
            this.menuCaliperOff.RadioCheck = true;
            this.menuCaliperOff.Shortcut = System.Windows.Forms.Shortcut.CtrlQ;
            this.menuCaliperOff.Text = "Off";
            this.menuCaliperOff.Click += new System.EventHandler(this.menuCaliperOff_Click);
            // 
            // menuCaliperDuration
            // 
            this.menuCaliperDuration.Index = 1;
            this.menuCaliperDuration.RadioCheck = true;
            this.menuCaliperDuration.Shortcut = System.Windows.Forms.Shortcut.CtrlW;
            this.menuCaliperDuration.Text = "Duration";
            this.menuCaliperDuration.Click += new System.EventHandler(this.menuCaliperDuration_Click);
            // 
            // menuCaliperBoth
            // 
            this.menuCaliperBoth.Index = 2;
            this.menuCaliperBoth.RadioCheck = true;
            this.menuCaliperBoth.Shortcut = System.Windows.Forms.Shortcut.CtrlE;
            this.menuCaliperBoth.Text = "Duration + uV";
            this.menuCaliperBoth.Click += new System.EventHandler(this.menuCaliperBoth_Click);
            // 
            // menuZoom
            // 
            this.menuZoom.Index = 6;
            this.menuZoom.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuZoomOut,
            this.menuZoomIn});
            this.menuZoom.Text = "Zoom";
            // 
            // menuZoomOut
            // 
            this.menuZoomOut.Index = 0;
            this.menuZoomOut.Shortcut = System.Windows.Forms.Shortcut.Ctrl9;
            this.menuZoomOut.Text = "Zoom Out";
            this.menuZoomOut.Click += new System.EventHandler(this.menuZoomOut_Click);
            // 
            // menuZoomIn
            // 
            this.menuZoomIn.Index = 1;
            this.menuZoomIn.Shortcut = System.Windows.Forms.Shortcut.Ctrl0;
            this.menuZoomIn.Text = "Zoom In";
            this.menuZoomIn.Click += new System.EventHandler(this.menuZoomIn_Click);
            // 
            // menuDisplayInfo
            // 
            this.menuDisplayInfo.Checked = true;
            this.menuDisplayInfo.Index = 7;
            this.menuDisplayInfo.Shortcut = System.Windows.Forms.Shortcut.CtrlI;
            this.menuDisplayInfo.Text = "Display Info";
            this.menuDisplayInfo.Click += new System.EventHandler(this.menuDisplayInfo_Click);
            // 
            // menuAnnonymize
            // 
            this.menuAnnonymize.Index = 8;
            this.menuAnnonymize.Shortcut = System.Windows.Forms.Shortcut.CtrlA;
            this.menuAnnonymize.Text = "Annonymize";
            this.menuAnnonymize.Click += new System.EventHandler(this.menuAnnonymize_Click);
            // 
            // menuSave
            // 
            this.menuSave.Enabled = false;
            this.menuSave.Index = 2;
            this.menuSave.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuSaveFile,
            this.menuSaveSystems});
            this.menuSave.Text = "Save";
            // 
            // menuSaveFile
            // 
            this.menuSaveFile.Index = 0;
            this.menuSaveFile.Shortcut = System.Windows.Forms.Shortcut.CtrlS;
            this.menuSaveFile.Text = "File ...";
            this.menuSaveFile.Click += new System.EventHandler(this.menuSaveFile_Click);
            // 
            // menuSaveSystems
            // 
            this.menuSaveSystems.Enabled = false;
            this.menuSaveSystems.Index = 1;
            this.menuSaveSystems.Text = "ECG System";
            // 
            // menuClose
            // 
            this.menuClose.Enabled = false;
            this.menuClose.Index = 3;
            this.menuClose.Shortcut = System.Windows.Forms.Shortcut.CtrlL;
            this.menuClose.Text = "Close";
            this.menuClose.Click += new System.EventHandler(this.menuClose_Click);
            // 
            // menuPlugin
            // 
            this.menuPlugin.Index = 4;
            this.menuPlugin.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuAddPluginFile,
            this.menuAddPluginDir});
            this.menuPlugin.Text = "Plugins";
            // 
            // menuAddPluginFile
            // 
            this.menuAddPluginFile.Index = 0;
            this.menuAddPluginFile.Shortcut = System.Windows.Forms.Shortcut.CtrlP;
            this.menuAddPluginFile.Text = "File ...";
            this.menuAddPluginFile.Click += new System.EventHandler(this.menuAddPluginFile_Click);
            // 
            // menuAddPluginDir
            // 
            this.menuAddPluginDir.Index = 1;
            this.menuAddPluginDir.Shortcut = System.Windows.Forms.Shortcut.CtrlShiftP;
            this.menuAddPluginDir.Text = "Dir ...";
            this.menuAddPluginDir.Click += new System.EventHandler(this.menuAddPluginDir_Click);
            // 
            // ECGPanel
            // 
            this.ECGPanel.BackColor = System.Drawing.Color.White;
            this.ECGPanel.Controls.Add(this.InnerECGPanel);
            this.ECGPanel.Controls.Add(this.labelPatientSecond);
            this.ECGPanel.Controls.Add(this.labelDiagnostic);
            this.ECGPanel.Controls.Add(this.labelTime);
            this.ECGPanel.Controls.Add(this.labelPatient);
            this.ECGPanel.Location = new System.Drawing.Point(0, 0);
            this.ECGPanel.Name = "ECGPanel";
            this.ECGPanel.Size = new System.Drawing.Size(684, 449);
            this.ECGPanel.TabIndex = 0;
            // 
            // InnerECGPanel
            // 
            this.InnerECGPanel.BackColor = System.Drawing.Color.Transparent;
            this.InnerECGPanel.Location = new System.Drawing.Point(0, 105);
            this.InnerECGPanel.Name = "InnerECGPanel";
            this.InnerECGPanel.Size = new System.Drawing.Size(175, 90);
            this.InnerECGPanel.TabIndex = 0;
            this.InnerECGPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.InnerECGPanel_Paint);
            this.InnerECGPanel.DoubleClick += new System.EventHandler(this.InnerECGPanel_DoubleClick);
            this.InnerECGPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.InnerECGPanel_MouseDown);
            this.InnerECGPanel.MouseLeave += new System.EventHandler(this.InnerECGPanel_MouseLeave);
            this.InnerECGPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.InnerECGPanel_MouseMove);
            // 
            // labelPatientSecond
            // 
            this.labelPatientSecond.BackColor = System.Drawing.Color.Transparent;
            this.labelPatientSecond.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPatientSecond.ForeColor = System.Drawing.SystemColors.ControlText;
            this.labelPatientSecond.Location = new System.Drawing.Point(222, 29);
            this.labelPatientSecond.Name = "labelPatientSecond";
            this.labelPatientSecond.Size = new System.Drawing.Size(123, 66);
            this.labelPatientSecond.TabIndex = 4;
            // 
            // labelDiagnostic
            // 
            this.labelDiagnostic.BackColor = System.Drawing.Color.White;
            this.labelDiagnostic.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDiagnostic.ForeColor = System.Drawing.SystemColors.ControlText;
            this.labelDiagnostic.Location = new System.Drawing.Point(365, 5);
            this.labelDiagnostic.Multiline = true;
            this.labelDiagnostic.Name = "labelDiagnostic";
            this.labelDiagnostic.ReadOnly = true;
            this.labelDiagnostic.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.labelDiagnostic.Size = new System.Drawing.Size(310, 98);
            this.labelDiagnostic.TabIndex = 3;
            this.labelDiagnostic.Visible = false;
            // 
            // labelTime
            // 
            this.labelTime.BackColor = System.Drawing.Color.Transparent;
            this.labelTime.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTime.ForeColor = System.Drawing.SystemColors.ControlText;
            this.labelTime.Location = new System.Drawing.Point(210, 5);
            this.labelTime.Name = "labelTime";
            this.labelTime.Size = new System.Drawing.Size(145, 15);
            this.labelTime.TabIndex = 2;
            // 
            // labelPatient
            // 
            this.labelPatient.BackColor = System.Drawing.Color.Transparent;
            this.labelPatient.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPatient.ForeColor = System.Drawing.SystemColors.ControlText;
            this.labelPatient.Location = new System.Drawing.Point(5, 5);
            this.labelPatient.Name = "labelPatient";
            this.labelPatient.Size = new System.Drawing.Size(200, 98);
            this.labelPatient.TabIndex = 1;
            // 
            // statusBar
            // 
            this.statusBar.Location = new System.Drawing.Point(0, 485);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(686, 22);
            this.statusBar.TabIndex = 1;
            // 
            // ECGTimeScrollbar
            // 
            this.ECGTimeScrollbar.Enabled = false;
            this.ECGTimeScrollbar.Location = new System.Drawing.Point(0, 449);
            this.ECGTimeScrollbar.Name = "ECGTimeScrollbar";
            this.ECGTimeScrollbar.Size = new System.Drawing.Size(683, 16);
            this.ECGTimeScrollbar.TabIndex = 5;
            this.ECGTimeScrollbar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.ECGTimeScrollbar_Scroll);
            // 
            // ECGViewer
            // 
            this.ClientSize = new System.Drawing.Size(686, 507);
            this.Controls.Add(this.statusBar);
            this.Controls.Add(this.ECGPanel);
            this.Controls.Add(this.ECGTimeScrollbar);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Menu = this.mainMenu;
            this.MinimumSize = new System.Drawing.Size(534, 534);
            this.Name = "ECGViewer";
            this.Text = "ECGViewer";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.ECGViewer_Load);
            this.Resize += new System.EventHandler(this.ECGViewer_Resize);
            this.ECGPanel.ResumeLayout(false);
            this.ECGPanel.PerformLayout();
            this.ResumeLayout(false);

		}
		#endregion

		private void ECGViewer_Load(object sender, System.EventArgs e)
		{
		}

		private void LoadECGMS(ECGConverter instance)
		{
			if (InvokeRequired)
			{
				Invoke(new ECGConverter.NewPluginDelegate(this.LoadECGMS), new object[] {instance});

				return;
			}

			menuOpenSystems.MenuItems.Clear();
			menuSaveSystems.MenuItems.Clear();

			string[] manSysList = instance.getSupportedManagementSystemsList();

			for (int i=0;i < manSysList.Length;i++)
			{
				System.Windows.Forms.MenuItem item = new MenuItem(manSysList[i], new EventHandler(menuECGMSOpen_Click));

				menuOpenSystems.MenuItems.Add(item);

				if (instance.hasECGManagementSystemSaveSupport(i))
				{
					item = new MenuItem(manSysList[i], new EventHandler(menuECGMSSave_Click));

					menuSaveSystems.MenuItems.Add(item);
				}
			}

			if (menuOpenSystems.IsParent)
			{
				menuOpenSystems.Enabled = true;
			}
			else
			{
				menuOpenSystems.Enabled = false;
				menuOpenSystems.MenuItems.Add(new MenuItem("(none)"));
			}

			if (menuSaveSystems.IsParent)
			{
				menuSaveSystems.Enabled = true;
			}
			else
			{
				menuSaveSystems.Enabled = false;
				menuSaveSystems.MenuItems.Add(new MenuItem("(none)"));
			}
		}

		private void ECGViewer_Resize(object sender, EventArgs e)
		{
			lock (this)
			{
				int oldSPS = 0,
					oldPos = ECGTimeScrollbar.Value;

				if (_CurrentSignal != null)
					oldSPS = _CurrentSignal.RhythmSamplesPerSecond;

				_DrawBuffer = null;
				_CurrentSignal = null;

				CurrentECG = _CurrentECG;

				if ((oldSPS != 0)
				&&	(_CurrentSignal != null)
				&&	(_DrawType != ECGDraw.ECGDrawType.Median))
				{
					ECGTimeScrollbar.Value = _CurrentSignal.IsBuffered ? oldPos : (oldPos * _CurrentSignal.RhythmSamplesPerSecond) / oldSPS;
				}

				if (ECGTimeScrollbar.Enabled)
				{
					ECGTimeScrollbar.LargeChange = _CurrentSignal.RhythmSamplesPerSecond;
				}
			}

			this.ECGTimeScrollbar.Width = this.ECGPanel.Width = this.Width - (this.ECGPanel.Left * 2) - 20;
			this.ECGPanel.Height = this.Height - this.ECGPanel.Top - 60 - this.statusBar.Height - this.ECGTimeScrollbar.Height;
			this.ECGTimeScrollbar.Top = this.ECGPanel.Bottom;

			this.InnerECGPanel.Height = this.ECGPanel.Height - this.InnerECGPanel.Top;
			this.InnerECGPanel.Width = this.ECGPanel.Width - this.InnerECGPanel.Left;

			this.labelDiagnostic.Width = this.Width - this.labelDiagnostic.Left - 10;

			this.ECGPanel.Refresh();
		}

		private void menuOpenFile_Click(object sender, System.EventArgs e)
		{
			try
			{
				StringBuilder sb = new StringBuilder();

				sb.Append("Any ECG File (*.*)|*.*");

				int i=0;
				
				System.Collections.ArrayList supportedList = new ArrayList();

				foreach (string format in ECGConverter.Instance.getSupportedFormatsList())
				{
					string extension = ECGConverter.Instance.getExtension(i);

					if (ECGConverter.Instance.hasUnknownReaderSupport(i++))
					{
						supportedList.Add(format);
						
						sb.Append('|');

						sb.Append(format);
						sb.Append(" File");

						if (extension == null)
							extension = "ecg";

						sb.Append(" (*.");
						sb.Append(extension);
						sb.Append(")|*.");
						sb.Append(extension);
					}
				}

				saveECGFileDialog.Filter = sb.ToString();

				openECGFileDialog.Title = "Open ECG";
				openECGFileDialog.Filter = sb.ToString();
				DialogResult dr = this.openECGFileDialog.ShowDialog(this);

				if ((dr == DialogResult.OK)
				&&	File.Exists(this.openECGFileDialog.FileName))
				{
					IECGFormat format = null;
					
					if (openECGFileDialog.FilterIndex > 1)
					{
						string fmt = (string)supportedList[openECGFileDialog.FilterIndex - 2];
						IECGReader reader = ECGConverter.Instance.getReader(fmt);
						ECGConfig cfg = ECGConverter.Instance.getConfig(fmt);
						
						if (cfg != null)
						{
							Config cfgScreen = new Config(fmt, cfg);

							dr = cfgScreen.ShowDialog(this);
							
							if (dr != DialogResult.OK)
								return;
						}
							
						format = reader.Read(this.openECGFileDialog.FileName, 0, cfg);
					}
					else
					{
						if (_ECGReader == null)
							_ECGReader = new UnknownECGReader();

						format = _ECGReader.Read(this.openECGFileDialog.FileName);
					}

					if (format != null)
					{
						CurrentECG = format;
						
						if (CurrentECG != null)
						{
							this.statusBar.Text = "Opened file!";
						}
					}
					else
					{
						CurrentECG = null;

						this.statusBar.Text = "Failed to open file!";
					}
				}
				else
				{
					this.statusBar.Text = "";
				}

				this.InnerECGPanel.Refresh();
			}
			catch (Exception ex)
			{
				CurrentECG = null;

				MessageBox.Show(this, ex.ToString(), ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		
		private void menuSaveFile_Click(object sender, System.EventArgs e)
		{
			try
			{
				StringBuilder sb = new StringBuilder();

				sb.Append("Current Format (*.*)|*.*");

				int i=0;

				string[] supportedList = ECGConverter.Instance.getSupportedFormatsList();

				foreach (string format in supportedList)
				{
					string extension = ECGConverter.Instance.getExtension(i++);

					sb.Append('|');

					sb.Append(format);
					sb.Append(" File");

					if (extension == null)
						extension = "ecg";

					sb.Append(" (*.");
					sb.Append(extension);
					sb.Append(")|*.");
					sb.Append(extension);
				}

				saveECGFileDialog.Title = "Save ECG";
				saveECGFileDialog.Filter = sb.ToString();
				saveECGFileDialog.OverwritePrompt = true;
				DialogResult dr = saveECGFileDialog.ShowDialog(this);

				if (dr == DialogResult.OK)
				{
					int index = saveECGFileDialog.FilterIndex - 2;

					IECGFormat writeFile = CurrentECG;

					if (index >= 0)
					{
						ECGConfig cfg = ECGConverter.Instance.getConfig(index);

						if (cfg != null)
						{
							cfg["Lead Format"] = _DrawType.ToString();
							cfg["Gain"] = Gain.ToString();

                            if (!double.IsNaN(_BottomCutoff))
                                cfg["Filter Bottom Cutoff"] = _BottomCutoff.ToString();
                            if (!double.IsNaN(_TopCutoff))
                                cfg["Filter Top Cutoff"] = _TopCutoff.ToString();

                            if (!double.IsNaN(_BottomCutoff)
                            ||  !double.IsNaN(_TopCutoff))
                                cfg["Filter Number of Sections"] = "2";

							Config cfgScreen = new Config(supportedList[index], cfg);

							dr = cfgScreen.ShowDialog(this);
						}

						if (dr != DialogResult.OK)
							return;

						try
						{
							if (CurrentECG.GetType() != ECGConverter.Instance.getType(index))
							{
								if (((ECGConverter.Instance.Convert(CurrentECG, index, cfg, out writeFile) != 0)
								||	!writeFile.Works())
								&&	(writeFile != null))
								{
									writeFile.Dispose();
									writeFile = null;
								}								
							}
						}
						catch
						{
							if (writeFile != null)
							{
								writeFile.Dispose();
								writeFile = null;
							}
						}

						if (writeFile == null)
						{
							MessageBox.Show(this, "Converting of file has failed!" , "Converting failed!", MessageBoxButtons.OK, MessageBoxIcon.Error);

							return;
						}
					}
				
					ECGWriter.Write(writeFile, saveECGFileDialog.FileName, true);

					if (writeFile != CurrentECG)
						writeFile.Dispose();

					if (ECGWriter.getLastError() != 0)
					{
						MessageBox.Show(this, ECGWriter.getLastErrorMessage(), "Writing of file failed!", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.ToString(), ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void menuECGMSOpen_Click(object sender, System.EventArgs e)
		{
			if (sender.GetType() == typeof(MenuItem))
			{
				System.Windows.Forms.MenuItem temp = (MenuItem) sender;

				if (!ECGConverter.Instance.hasECGManagementSystemSupport(temp.Text))
					return;

				try
				{
					OpenFromECGMS of = new OpenFromECGMS(ECGConverter.Instance.getECGManagementSystem(temp.Text));

					of.ShowDialog(this);

					IECGFormat ecg = of.SelectedECG;

					if (ecg != null)
					{
						CurrentECG = ecg;
					}

					this.InnerECGPanel.Refresh();
				}
				catch (Exception ex)
				{
					CurrentECG = null;

					MessageBox.Show(this, ex.ToString(), ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void menuECGMSSave_Click(object sender, System.EventArgs e)
		{
			if (sender.GetType() == typeof(MenuItem))
			{
				System.Windows.Forms.MenuItem temp = (MenuItem) sender;

				if (!ECGConverter.Instance.hasECGManagementSystemSaveSupport(temp.Text))
					return;

				try
				{
					SaveToECGMS st = new SaveToECGMS(CurrentECG, ECGConverter.Instance.getECGManagementSystem(temp.Text));

					st.ShowDialog(this);
				}
				catch (Exception ex)
				{

					MessageBox.Show(this, ex.ToString(), ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void menuClose_Click(object sender, System.EventArgs e)
		{
			try
			{
				if (CurrentECG != null)
				{
					CurrentECG.Dispose();
					CurrentECG = null;

					this.statusBar.Text = "";

					this.InnerECGPanel.Refresh();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.ToString(), ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void menuAddPluginFile_Click(object sender, System.EventArgs e)
		{
			try
			{
				openECGFileDialog.Title = "Open Plugin";
				openECGFileDialog.Filter = "Assembly file (*.dll)|*.dll";
				DialogResult dr = openECGFileDialog.ShowDialog(this);

				if (dr == DialogResult.OK)
				{
					if (ECGConverter.AddPlugin(openECGFileDialog.FileName) != 0)
						MessageBox.Show(this, "Selected plugin file is not supported!", "Unsupported plugin!", MessageBoxButtons.OK, MessageBoxIcon.Warning);

				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.ToString(), ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void menuAddPluginDir_Click(object sender, System.EventArgs e)
		{
			try
			{
				DialogResult dr = folderBrowserDialog.ShowDialog(this);

				if (dr == DialogResult.OK)
				{
					if (ECGConverter.AddPlugins(folderBrowserDialog.SelectedPath) != 0)
						MessageBox.Show(this, "Couldn't load entire plugin directory!", "Unsupported plugin!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.ToString(), ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void InnerECGPanel_Paint(object sender, PaintEventArgs e)
		{
			lock (this)
			{
				TopInfo(_CurrentECG);

				if (_CurrentECG == null)
				{
					e.Graphics.Clear(this.ECGPanel.BackColor);

					return;
				}

				if (_DrawBuffer == null)
				{
					int w = (int)e.Graphics.VisibleClipBounds.Width,
						h = (int)e.Graphics.VisibleClipBounds.Height;

					int n = 0;
					int[,] s = {{782, 492}, {1042, 657}, {1302, 822}};

					for (;n < s.GetLength(0);n++)
						if ((s[n, 0] > w)
						||	(s[n, 1] > h))
							break;

					n+=2;

					// zoom mode on
					if (_Zoom > 1)
					{
						n *= _Zoom;
						w *= _Zoom;
						h *= _Zoom;

						int start, end;

						if (_DrawType != ECGConversion.ECGDraw.ECGDrawType.Regular)
						{
							start = n * 5 * 33 + 1;
							end = n * 5 * 52 + 1;
						}
						else
						{
							_CurrentSignal.CalculateStartAndEnd(out start, out end);

							end = (((end - start) * 25 * n) / _CurrentSignal.RhythmSamplesPerSecond) + 1 + (n * 5);
							start = int.MaxValue;
						}

						if (w > end)
							w = end;

						if (w < InnerECGPanel.Width)
							w = InnerECGPanel.Width;

						if (h > start)
							h = start;

						if (h < InnerECGPanel.Height)
							h = InnerECGPanel.Height;
					}

					_DrawBuffer = new Bitmap(w, h);

                    ECGConversion.ECGSignals.Signals drawSignal = _CurrentSignal;
                    int nTime = ECGTimeScrollbar.Value;
					ECGDraw.DpiX = ECGDraw.DpiY = 25.4f * n;

                    if (drawSignal.IsBuffered)
                    {
                        float fPixel_Per_s = (float)Math.Round(25.0f * ECGDraw.DpiX * ECGDraw.Inch_Per_mm);

                        ECGConversion.ECGSignals.BufferedSignals bs = drawSignal.AsBufferedSignals;

                        int nrSamplesToLoad = 10 * bs.RealRhythmSamplesPerSecond,
                            value = bs.RealRhythmStart + ECGTimeScrollbar.Value;

                        if (_DrawType == ECGConversion.ECGDraw.ECGDrawType.Regular)
                        {
                            nrSamplesToLoad = (int)((_DrawBuffer.Width * bs.RealRhythmSamplesPerSecond) / fPixel_Per_s);

                            int multiple = (int)Math.Floor(_DrawBuffer.Height / (((5 * bs.NrLeads) + 3) * ECGDraw.DpiX * ECGDraw.Inch_Per_mm * 5));

                            if (multiple > 1) 
                                nrSamplesToLoad *= multiple;
                        }

                        bs.LoadSignal(value, value + nrSamplesToLoad);

                        nTime -= value;

                        drawSignal = drawSignal.GetCopy();
                    }

                    if (drawSignal != null)
                    {
                        if (!double.IsNaN(_BottomCutoff))
                        {
                            if (!double.IsNaN(_TopCutoff))
                            {
                                drawSignal = drawSignal.ApplyBandpassFilter(_BottomCutoff, _TopCutoff);
                            }
                            else
                            {
                                drawSignal = drawSignal.ApplyHighpassFilter(_BottomCutoff);
                            }
                        }
                        else if (!double.IsNaN(_TopCutoff))
                        {
                            drawSignal = drawSignal.ApplyLowpassFilter(_TopCutoff);
                        }
                    }

					int
						oldSPS = _CurrentSignal.RhythmSamplesPerSecond,
						ret = ECGDraw.DrawECG(Graphics.FromImage(_DrawBuffer), drawSignal, _DrawType, nTime, 25.0f, _Gain);

					if (ret < 0)
					{
						Graphics g = Graphics.FromImage(_DrawBuffer);

                        ret = ECGDraw.DrawECG(g, drawSignal, ECGDraw.ECGDrawType.Regular, nTime, 25.0f, _Gain);
					}

					if (_DrawType != ECGDraw.ECGDrawType.Median)
					{
						if (!_CurrentSignal.IsBuffered)
						{
							ECGTimeScrollbar.Minimum = 0;
							ECGTimeScrollbar.SmallChange = _CurrentSignal.RhythmSamplesPerSecond;
							ECGTimeScrollbar.LargeChange = (ECGTimeScrollbar.LargeChange * _CurrentSignal.RhythmSamplesPerSecond) / oldSPS;
							ECGTimeScrollbar.Value = (ECGTimeScrollbar.Value * _CurrentSignal.RhythmSamplesPerSecond) / oldSPS;
							ECGTimeScrollbar.Maximum = (ECGTimeScrollbar.Maximum * _CurrentSignal.RhythmSamplesPerSecond) / oldSPS;
						}

						if ((ret >= 0)
						&&	((ret - ECGTimeScrollbar.Value) < ECGTimeScrollbar.Maximum))
						{
							ECGTimeScrollbar.Enabled = true;
							ECGTimeScrollbar.LargeChange =  Math.Max(ECGTimeScrollbar.LargeChange, ret - ECGTimeScrollbar.Value);
						}
						else
						{
							ECGTimeScrollbar.LargeChange = ECGTimeScrollbar.Maximum;
							ECGTimeScrollbar.Enabled = false;
						}
					}
					else
					{
						ECGTimeScrollbar.Enabled = false;
					}
				}		
	
				if (_OffsetX < 0)
					_OffsetX = 0;
				else if (_OffsetX > (_DrawBuffer.Width - InnerECGPanel.Width))
					_OffsetX = _DrawBuffer.Width - InnerECGPanel.Width;

				if (_OffsetY < 0)
					_OffsetY = 0;
				else if (_OffsetY > (_DrawBuffer.Height - InnerECGPanel.Height))
					_OffsetY = _DrawBuffer.Height - InnerECGPanel.Height;

				e.Graphics.DrawImage(_DrawBuffer, this.InnerECGPanel.DisplayRectangle, _OffsetX, _OffsetY, this.InnerECGPanel.Size.Width, this.InnerECGPanel.Size.Height, GraphicsUnit.Pixel);
			}
		}

		public void TopInfo(IECGFormat format)
		{
			menuClose.Enabled = format != null;
			menuSave.Enabled = format != null;
			menuView.Enabled = format != null;

			if ((format == null)
			||	(format.Demographics == null))
			{
				this.labelPatient.Text = "";
				this.labelPatientSecond.Text = "";
				this.labelTime.Text = "";
				this.labelDiagnostic.Text = "";
				this.labelDiagnostic.Visible = false;
			}
			else
			{
				try
				{
					StringBuilder sb = new StringBuilder();
	
					sb.Append("Name:       ");
	
					if ((format.Demographics.FirstName != null)
					&&	(format.Demographics.FirstName.Length != 0))
					{
						sb.Append(format.Demographics.FirstName);
						sb.Append(" ");
					}
	
					sb.Append(format.Demographics.LastName);
	
					if ((format.Demographics.SecondLastName != null)
					&&	(format.Demographics.SecondLastName.Length != 0))
					{
						sb.Append('-');
						sb.Append(format.Demographics.SecondLastName);
					}
	
					sb.Append('\n');
					sb.Append("Patient ID: ");
					sb.Append(format.Demographics.PatientID);
	
					GlobalMeasurements gms;
	
					if ((format.GlobalMeasurements != null)
					&&	(format.GlobalMeasurements.getGlobalMeasurements(out gms) == 0)
					&&	(gms.measurment != null)
					&&	(gms.measurment.Length > 0)
					&&	(gms.measurment[0] != null))
					{
						int ventRate = (gms.VentRate == GlobalMeasurement.NoValue) ? 0 : (int) gms.VentRate,
							PRint = (gms.PRint == GlobalMeasurement.NoValue) ? 0 : (int) gms.measurment[0].PRint,
							QRSdur = (gms.QRSdur == GlobalMeasurement.NoValue) ? 0 : (int) gms.measurment[0].QRSdur,
							QT = (gms.QTdur == GlobalMeasurement.NoValue) ? 0 : (int) gms.measurment[0].QTdur,
							QTc = (gms.QTc == GlobalMeasurement.NoValue) ? 0 : (int) gms.QTc;
	
						sb.Append("\n\nVent rate:      ");
						PrintValue(sb, ventRate, 3);
						sb.Append(" BPM");
						
						sb.Append("\nPR int:         ");
						PrintValue(sb, PRint, 3);
						sb.Append(" ms");
	
						sb.Append("\nQRS dur:        ");
						PrintValue(sb, QRSdur, 3);
						sb.Append(" ms");
	
						sb.Append("\nQT\\QTc:     ");
						PrintValue(sb, QT, 3);
						sb.Append('/');
						PrintValue(sb, QTc, 3);
						sb.Append(" ms");
	
						sb.Append("\nP-R-T axes: ");
						sb.Append((gms.measurment[0].Paxis != GlobalMeasurement.NoAxisValue) ? gms.measurment[0].Paxis.ToString() : "999");
						sb.Append(' ');
						sb.Append((gms.measurment[0].QRSaxis != GlobalMeasurement.NoAxisValue) ? gms.measurment[0].QRSaxis.ToString() : "999");
						sb.Append(' ');
						sb.Append((gms.measurment[0].Taxis != GlobalMeasurement.NoAxisValue) ? gms.measurment[0].Taxis.ToString() : "999");
					}
	
					this.labelPatient.Text = sb.ToString();
	
					sb = new StringBuilder();
	
					sb.Append("DOB:  ");
	
					ECGConversion.ECGDemographics.Date birthDate = format.Demographics.PatientBirthDate;
					if (birthDate != null)
					{
						sb.Append(birthDate.Day.ToString("00"));
						sb.Append(birthDate.Month.ToString("00"));
						sb.Append(birthDate.Year.ToString("0000"));
					}
	
					sb.Append("\nAge:  ");
	
					ushort ageVal;
					ECGConversion.ECGDemographics.AgeDefinition ad;
	
					if (format.Demographics.getPatientAge(out ageVal, out ad) == 0)
					{
						sb.Append(ageVal);
	
						if (ad != ECGConversion.ECGDemographics.AgeDefinition.Years)
						{
							sb.Append(" ");
							sb.Append(ad.ToString());
						}
					}
					else
						sb.Append("0");
	
					sb.Append("\nGen:  ");
					if (format.Demographics.Gender != ECGConversion.ECGDemographics.Sex.Null)
						sb.Append(format.Demographics.Gender.ToString());
					sb.Append("\nDep:  ");
					sb.Append(format.Demographics.AcqDepartment);
	
					this.labelPatientSecond.Text = sb.ToString();

					DateTime dt = format.Demographics.TimeAcquisition;

					this.labelTime.Text = (dt.Year > 1000) ? dt.ToString("dd/MM/yyyy HH:mm:ss") : "Time Unknown";
	
					Statements stat;
	
					if ((format.Diagnostics != null)
					&&	(format.Diagnostics.getDiagnosticStatements(out stat) == 0))
					{
						if ((stat.statement != null)
                        &&  (stat.statement.Length > 0))
						{
							sb = new StringBuilder();
	
							foreach (string temp in stat.statement)
							{
								sb.Append(temp);
								sb.Append("\r\n");
							}
	
							string temp2 = stat.statement[stat.statement.Length-1];
	
							if ((temp2 != null)
							&&	!temp2.StartsWith("confirmed by", StringComparison.InvariantCultureIgnoreCase)
							&&	!temp2.StartsWith("interpreted by", StringComparison.InvariantCultureIgnoreCase)
							&&	!temp2.StartsWith("reviewed by", StringComparison.InvariantCultureIgnoreCase))
							{
								if ((format.Demographics.OverreadingPhysician != null)
								&&	(format.Demographics.OverreadingPhysician.Length != 0))
								{
									if (stat.confirmed)
										sb.Append("Confirmed by ");
									else if (stat.interpreted)
										sb.Append("Interpreted by ");
									else
										sb.Append("Reviewed by ");
	
									sb.Append(format.Demographics.OverreadingPhysician);
	
								}
								else
									sb.Append("UNCONFIRMED AUTOMATED INTERPRETATION");
							}
	
							this.labelDiagnostic.Text = sb.ToString();
							this.labelDiagnostic.Visible = true;
						}
					}
					else
					{
						this.labelDiagnostic.Text = "";
					}
				}
				catch
				{
					this.labelPatient.Text = "";
					this.labelPatientSecond.Text = "";
					this.labelTime.Text = "";
					this.labelDiagnostic.Text = "";
                    this.menuView.Enabled = false;
                    this.menuSave.Enabled = false;
                    this.menuClose.Enabled = false;

                    this.statusBar.Text = "Open failed (due to an exception)!";
					
					CurrentECG = null;
				}
			}
		}

		private static void PrintValue(StringBuilder sb, int val, int len)
		{
			int temp = sb.Length;
			sb.Append(val.ToString());
			if ((sb.Length - temp) < len)
				sb.Append(' ', len - (sb.Length - temp));
		}

		private void menuLeadFormatRegular_Click(object sender, System.EventArgs e)
		{
			CheckLeadFormat(ECGDraw.ECGDrawType.Regular, true);
		}

		private void menuLeadFormatFourXThree_Click(object sender, System.EventArgs e)
		{
			CheckLeadFormat(ECGDraw.ECGDrawType.ThreeXFour, true);
		}

		private void menuLeadFormatFourXThreePlusOne_Click(object sender, System.EventArgs e)
		{
			CheckLeadFormat(ECGDraw.ECGDrawType.ThreeXFourPlusOne, true);
		}

		private void menuLeadFormatFourXThreePlusThree_Click(object sender, System.EventArgs e)
		{
			CheckLeadFormat(ECGDraw.ECGDrawType.ThreeXFourPlusThree, true);
		}

		private void menuLeadFormatSixXTwo_Click(object sender, System.EventArgs e)
		{
			CheckLeadFormat(ECGDraw.ECGDrawType.SixXTwo, true);
		}

		private void menuLeadFormatMedian_Click(object sender, System.EventArgs e)
		{
			CheckLeadFormat(ECGDraw.ECGDrawType.Median, true);
		}

		private void CheckLeadFormat(ECGDraw.ECGDrawType lt, bool refresh)
		{
			if (lt != _DrawType)
			{
				menuLeadFormatRegular.Checked = false;
				menuLeadFormatSixXTwo.Checked = false;
				menuLeadFormatThreeXFour.Checked = false;
				menuLeadFormatThreeXFourPlusOne.Checked = false;
				menuLeadFormatThreeXFourPlusThree.Checked = false;
				menuLeadFormatMedian.Checked = false;

				switch (lt)
				{
					case ECGDraw.ECGDrawType.Regular:
						menuLeadFormatRegular.Checked = true;
						break;
					case ECGDraw.ECGDrawType.SixXTwo:
						menuLeadFormatSixXTwo.Checked = true;
						break;
					case ECGDraw.ECGDrawType.ThreeXFour:
						menuLeadFormatThreeXFour.Checked = true;
						break;
					case ECGDraw.ECGDrawType.ThreeXFourPlusOne:
						menuLeadFormatThreeXFourPlusOne.Checked = true;
						break;
					case ECGDraw.ECGDrawType.ThreeXFourPlusThree:
						menuLeadFormatThreeXFourPlusThree.Checked = true;
						break;
					case ECGDraw.ECGDrawType.Median:
						menuLeadFormatMedian.Checked = true;
						break;
					default:
						menuLeadFormatRegular.Checked = true;

						lt = ECGDraw.ECGDrawType.Regular;
						break;
				}

				_DrawType = lt;

				if (refresh)
				{
					this.ECGViewer_Resize(null, null);

					DrawBuffer = null;

					Refresh();
				}
			}
		}

		private void menuGain1_Click(object sender, System.EventArgs e)
		{
			if (Gain != 5f)
			{
				Gain = 5f;

				DrawBuffer = null;
			}

			Refresh();
		}

		private void menuGain2_Click(object sender, System.EventArgs e)
		{
			if (Gain != 10f)
			{
				Gain = 10f;

				DrawBuffer = null;
			}

			Refresh();
		}

		private void menuGain3_Click(object sender, System.EventArgs e)
		{
			if (Gain != 20f)
			{
				Gain = 20f;

				DrawBuffer = null;
			}

			Refresh();
		}

		private void menuGain4_Click(object sender, System.EventArgs e)
		{
			if (Gain != 40f)
			{
				Gain = 40f;

				DrawBuffer = null;
			}

			Refresh();
		}

		private void ECGTimeScrollbar_Scroll(object sender, System.Windows.Forms.ScrollEventArgs e)
		{
			lock(this)
			{
				_DrawBuffer = null;	
			}

			Refresh();
		}

		private void menuAnnonymize_Click(object sender, System.EventArgs e)
		{
			lock(this)
			{
				_CurrentECG.Anonymous();
				_DrawBuffer = null;
			}

			Refresh();
		}

		private void menuDisplayInfo_Click(object sender, System.EventArgs e)
		{
			ECGDraw.DisplayInfo = menuDisplayInfo.Checked = !menuDisplayInfo.Checked;

			lock(this)
			{
				_DrawBuffer = null;
			}

			Refresh();
		}

		private void menuGridOne_Click(object sender, System.EventArgs e)
		{
			menuGridNone.Checked = false;
			menuGridOne.Checked = true;
			menuGridFive.Checked = false;

			ECGDraw.DisplayGrid = ECGDraw.GridType.OneMillimeters;

			lock(this)
			{
				_DrawBuffer = null;
			}

			Refresh();
		}

		private void menuGridFive_Click(object sender, System.EventArgs e)
		{
			menuGridNone.Checked = false;
			menuGridOne.Checked = false;
			menuGridFive.Checked = true;

			ECGDraw.DisplayGrid = ECGDraw.GridType.FiveMillimeters;

			lock(this)
			{
				_DrawBuffer = null;
			}

			Refresh();
		}

		private void menuGridNone_Click(object sender, System.EventArgs e)
		{
			menuGridNone.Checked = true;
			menuGridOne.Checked = false;
			menuGridFive.Checked = false;

			ECGDraw.DisplayGrid = ECGDraw.GridType.None;

			lock(this)
			{
				_DrawBuffer = null;
			}

			Refresh();
		}

		private CheckVersion.CheckAllowed CheckVersion_OnAllowNewVersionCheck(string title, string question)
		{
			DialogResult dr = MessageBox.Show(question, title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

			if (dr == DialogResult.Yes)
				return CheckVersion.CheckAllowed.Yes;
			else if (dr == DialogResult.No)
				return CheckVersion.CheckAllowed.No;
			
			return CheckVersion.CheckAllowed.Unknown;
		}

		private void CheckVersion_OnNewVersion(string title, string text, string url)
		{
			DialogResult dr = MessageBox.Show(text + "\n\nDo you wish to download the latest version right now?", title, MessageBoxButtons.YesNo, MessageBoxIcon.Information);

			if (dr == DialogResult.Yes)
			{
				ThreadPool.QueueUserWorkItem(new WaitCallback(this.LoadUrlDelayed), url); 
			}
		}

		private void LoadUrlDelayed(object obj)
		{
			if ((obj != null)
			&&	(obj is string))
			{
				System.Diagnostics.Process process = null;

				try
				{
					Thread.Sleep(1500);

					process = new System.Diagnostics.Process();
					process.StartInfo.FileName = "rundll32.exe";
					process.StartInfo.Arguments = "url.dll,FileProtocolHandler " + (string)obj;
					process.StartInfo.UseShellExecute = true;
					process.Start();
					process.WaitForExit(5000);

				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.ToString(), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				finally
				{
					if (process != null)
						process.Dispose();
				}
			}
		}

		private void SetColors(int kind)
		{
			if (kind < 0)
			{
				kind = 0;
			}

			if (kind == 0)
			{
				this.menuColor1.Checked = true;
				this.menuColor2.Checked = false;
				this.menuColor3.Checked = false;
				this.menuColor4.Checked = false;

				// Might be intressting to add different colors.
				this.ECGPanel.BackColor = Color.White;
				this.labelPatient.BackColor = Color.White;
				this.labelPatient.ForeColor = Color.Black;
				this.labelPatientSecond.BackColor = Color.White;
				this.labelPatientSecond.ForeColor = Color.Black;
				this.labelTime.BackColor = Color.White;
				this.labelTime.ForeColor = Color.Black;
				this.labelDiagnostic.BackColor = Color.White;
				this.labelDiagnostic.ForeColor = Color.Black;

				ECGDraw.BackColor = Color.White;
				ECGDraw.GraphColor = Color.FromArgb(255, 187, 187);
				ECGDraw.GraphSecondColor = Color.FromArgb(255, 229, 229);
				ECGDraw.SignalColor = Color.Black;
				ECGDraw.TextColor = Color.Black;
			}
			else if (kind == 1)
			{
				this.menuColor1.Checked = false;
				this.menuColor2.Checked = true;
				this.menuColor3.Checked = false;
				this.menuColor4.Checked = false;

				// Might be intressting to add different colors.
				this.ECGPanel.BackColor = Color.White;
				this.labelPatient.BackColor = Color.White;
				this.labelPatient.ForeColor = Color.Black;
				this.labelPatientSecond.BackColor = Color.White;
				this.labelPatientSecond.ForeColor = Color.Black;
				this.labelTime.BackColor = Color.White;
				this.labelTime.ForeColor = Color.Black;
				this.labelDiagnostic.BackColor = Color.White;
				this.labelDiagnostic.ForeColor = Color.Black;

				ECGDraw.BackColor = Color.White;
				ECGDraw.GraphColor = Color.FromArgb(187, 187, 255);
				ECGDraw.GraphSecondColor = Color.FromArgb(229, 229, 255);
				ECGDraw.SignalColor = Color.Black;
				ECGDraw.TextColor = Color.Black;
			}
			else if (kind == 2)
			{
				this.menuColor1.Checked = false;
				this.menuColor2.Checked = false;
				this.menuColor3.Checked = true;
				this.menuColor4.Checked = false;

				// Might be intressting to add different colors.
				this.ECGPanel.BackColor = Color.White;
				this.labelPatient.BackColor = Color.White;
				this.labelPatient.ForeColor = Color.Black;
				this.labelPatientSecond.BackColor = Color.White;
				this.labelPatientSecond.ForeColor = Color.Black;
				this.labelTime.BackColor = Color.White;
				this.labelTime.ForeColor = Color.Black;
				this.labelDiagnostic.BackColor = Color.White;
				this.labelDiagnostic.ForeColor = Color.Black;

				ECGDraw.BackColor = Color.White;
				ECGDraw.GraphColor = Color.FromArgb(28, 255, 28);
				ECGDraw.GraphSecondColor = Color.FromArgb(204, 255, 204);
				ECGDraw.SignalColor = Color.Black;
				ECGDraw.TextColor = Color.Black;
			}
			else if (kind == 3)
			{
				this.menuColor1.Checked = false;
				this.menuColor2.Checked = false;
				this.menuColor3.Checked = false;
				this.menuColor4.Checked = true;

				// Might be intressting to add different colors.
				this.ECGPanel.BackColor = Color.Black;
				this.labelPatient.BackColor = Color.Black;
				this.labelPatient.ForeColor = Color.Lime;
				this.labelPatientSecond.BackColor = Color.Black;
				this.labelPatientSecond.ForeColor = Color.Lime;
				this.labelTime.BackColor = Color.Black;
				this.labelTime.ForeColor = Color.Lime;
				this.labelDiagnostic.BackColor = Color.Black;
				this.labelDiagnostic.ForeColor = Color.Lime;

				ECGDraw.BackColor = Color.Black;
				ECGDraw.GraphColor = Color.Gray;
				ECGDraw.GraphSecondColor = Color.FromArgb(96, 96, 96);
				ECGDraw.SignalColor = Color.Lime;
				ECGDraw.TextColor = Color.Lime;
			}

			this.DrawBuffer = null;
			this.InnerECGPanel.Refresh();
		}

		private void menuColor1_Click(object sender, System.EventArgs e)
		{
			SetColors(0);
		}

		private void menuColor2_Click(object sender, System.EventArgs e)
		{
			SetColors(1);
		}

		private void menuColor3_Click(object sender, System.EventArgs e)
		{
			SetColors(2);
		}

		private void menuColor4_Click(object sender, System.EventArgs e)
		{
			SetColors(3);
		}

		private void menuCaliperOff_Click(object sender, System.EventArgs e)
		{
			menuCaliperOff.Checked = true;
			menuCaliperDuration.Checked = false;
			menuCaliperBoth.Checked = false;
		}

		private void menuCaliperDuration_Click(object sender, System.EventArgs e)
		{
			menuCaliperOff.Checked = false;
			menuCaliperDuration.Checked = true;
			menuCaliperBoth.Checked = false;
		}

		private void menuCaliperBoth_Click(object sender, System.EventArgs e)
		{
			menuCaliperOff.Checked = false;
			menuCaliperDuration.Checked = false;
			menuCaliperBoth.Checked = true;
		}

		private int _Zoom = 1;
		private int _OffsetX = 0;
		private int _OffsetY = 0;
		private int _PrevX = 0;
		private int _PrevY = 0;
		private int _LineX1 = int.MinValue;
		private int _LineX2 = int.MinValue;
		private int _LineY1 = int.MinValue;
		private int _LineY2 = int.MinValue;
		private bool _RightMouseButton = false;

		private void InnerECGPanel_MouseMove(object sender, MouseEventArgs e)
		{
			if (_DrawBuffer != null)
			{
				if (e.Button == MouseButtons.Right)
				{
					_OffsetX += (_PrevX - e.X);
					_OffsetY += (_PrevY - e.Y);

					if (_OffsetX < 0)
						_OffsetX = 0;
					else if (_OffsetX > (_DrawBuffer.Width - InnerECGPanel.Width))
						_OffsetX = _DrawBuffer.Width - InnerECGPanel.Width;

					if (_OffsetY < 0)
						_OffsetY = 0;
					else if (_OffsetY > (_DrawBuffer.Height - InnerECGPanel.Height))
						_OffsetY = _DrawBuffer.Height - InnerECGPanel.Height;
				}

				Graphics g = Graphics.FromHwndInternal(this.InnerECGPanel.Handle);

				if ((_LineX1 != int.MinValue)
				&&	(_LineX2 != int.MinValue))
				{
					if (_OffsetX < 0)
						_OffsetX = 0;
					else if (_OffsetX > (_DrawBuffer.Width - InnerECGPanel.Width))
						_OffsetX = _DrawBuffer.Width - InnerECGPanel.Width;

					if (_OffsetY < 0)
						_OffsetY = 0;
					else if (_OffsetY > (_DrawBuffer.Height - InnerECGPanel.Height))
						_OffsetY = _DrawBuffer.Height - InnerECGPanel.Height;

					g.DrawImage(_DrawBuffer, this.InnerECGPanel.DisplayRectangle, _OffsetX, _OffsetY, this.InnerECGPanel.Size.Width, this.InnerECGPanel.Size.Height, GraphicsUnit.Pixel);
				}

				if (e.Button != System.Windows.Forms.MouseButtons.Left)
				{
					_LineX1 = e.X;
					_LineY1 = e.Y;
				}

				_LineX2 = e.X;
				_LineY2 = e.Y;

				if (!menuCaliperOff.Checked)
				{
					Pen pen = new Pen(ECGDraw.TextColor);

					if (menuCaliperDuration.Checked)
					{
						if (_LineX1 != int.MinValue)
							g.DrawLine(pen, _LineX1, 0, _LineX1, InnerECGPanel.Height);

						if (_LineX2 != int.MinValue)
							g.DrawLine(pen, _LineX2, 0, _LineX2, InnerECGPanel.Height);

						this.statusBar.Text = ((_LineX1 == _LineX2) ? "" : Math.Round((Math.Abs(_LineX1 - _LineX2) * 1000 * ECGDraw.mm_Per_Inch) / (ECGDraw.DpiX * 25.0f), 0) + " ms");
					}
					else if (menuCaliperBoth.Checked)
					{
						if ((_LineX2 != int.MinValue)
						&&  (_LineY2 != int.MinValue))
						{
							if ((_LineX1 != int.MinValue)
							&&  (_LineY1 != int.MinValue)
							&&	(_LineX1 != _LineX2)
							&&	(_LineY1 != _LineY2))
							{
								g.DrawRectangle(
									pen,
									Math.Min(_LineX1, _LineX2),
									Math.Min(_LineY1, _LineY2),
									Math.Abs(_LineX1 - _LineX2),
									Math.Abs(_LineY1 - _LineY2));

								this.statusBar.Text = Math.Round((Math.Abs(_LineX1 - _LineX2) * 1000 * ECGDraw.mm_Per_Inch) / (ECGDraw.DpiX * 25.0f), 0) + " ms, "
													+ Math.Round((Math.Abs(_LineY1 - _LineY2) * 1000 * ECGDraw.mm_Per_Inch) / (ECGDraw.DpiY * _Gain), 0) + " uV";
							}
							else
							{
								g.DrawLine(pen, _LineX2, 0, _LineX2, InnerECGPanel.Height-1);
								g.DrawLine(pen, 0, _LineY2, InnerECGPanel.Width-1, _LineY2);

								this.statusBar.Text = "";
							}
						}
					}

					pen.Dispose();
				}
			}

			_PrevX = e.X;
			_PrevY = e.Y;
		}

		private void InnerECGPanel_MouseLeave(object sender, EventArgs e)
		{
			if (_DrawBuffer == null)
				return;

			Graphics g = Graphics.FromHwndInternal(this.InnerECGPanel.Handle);

			if ((_LineX1 != int.MinValue)
			&&	(_LineX2 != int.MinValue))
			{
				if (_OffsetX < 0)
					_OffsetX = 0;
				else if (_OffsetX > (_DrawBuffer.Width - InnerECGPanel.Width))
					_OffsetX = _DrawBuffer.Width - InnerECGPanel.Width;

				if (_OffsetY < 0)
					_OffsetY = 0;
				else if (_OffsetY > (_DrawBuffer.Height - InnerECGPanel.Height))
					_OffsetY = _DrawBuffer.Height - InnerECGPanel.Height;

				g.DrawImage(_DrawBuffer, this.InnerECGPanel.DisplayRectangle, _OffsetX, _OffsetY, this.InnerECGPanel.Size.Width, this.InnerECGPanel.Size.Height, GraphicsUnit.Pixel);

				this.statusBar.Text = "";

				_LineX1 = _LineX2 = int.MinValue;
				_LineY1 = _LineY2 = int.MinValue;
			}
		}

		private void InnerECGPanel_DoubleClick(object sender, EventArgs e)
		{
			if (_RightMouseButton)
			{
				ZoomIn(_PrevX, _PrevY);
			}
		}

		private void InnerECGPanel_MouseDown(object sender, MouseEventArgs e)
		{
			_RightMouseButton = (e.Button & MouseButtons.Right) == MouseButtons.Right;
		}

		private void menuZoomOut_Click(object sender, System.EventArgs e)
		{
			ZoomOut();
		}

		private void menuZoomIn_Click(object sender, System.EventArgs e)
		{
			ZoomIn(
				InnerECGPanel.Width >> 1,
				InnerECGPanel.Height >> 1);
		}

		private void ZoomOut()
		{
			if (_Zoom > 1)
			{
				menuZoomIn.Enabled = true;

				_Zoom >>= 1;
				_OffsetX = ((_OffsetX + (InnerECGPanel.Width >> 1)) >> 1) - (InnerECGPanel.Width >> 1);
				_OffsetY = ((_OffsetY + (InnerECGPanel.Height >> 1)) >> 1) - (InnerECGPanel.Height >> 1);

				if (_Zoom == 1)
					menuZoomOut.Enabled = false;

				DrawBuffer = null;
				Refresh();
			}
		}

		private void ZoomIn(int x, int y)
		{
			if (_Zoom < 4)
			{
				menuZoomOut.Enabled = true;

				_Zoom <<= 1;
				_OffsetX = ((_OffsetX + x) << 1) - (InnerECGPanel.Width >> 1);
				_OffsetY = ((_OffsetY + y) << 1) - (InnerECGPanel.Height >> 1);

				if (_Zoom == 4)
					menuZoomIn.Enabled = false;

				DrawBuffer = null;
				Refresh();
			}
			else
			{
				_Zoom = 1;
				menuZoomIn.Enabled = true;
				menuZoomOut.Enabled = false;

				_OffsetX = 0;
				_OffsetY = 0;

				DrawBuffer = null;
				Refresh();
			}
		}

        private void menuFilterNone_Click(object sender, EventArgs e)
        {
            menuFilterNone.Checked = true;
            menuFilter40Hz.Checked = false;
            menuFilterMuscle.Checked = false;

            _BottomCutoff = double.NaN;
            _TopCutoff = double.NaN;

            DrawBuffer = null;
            Refresh();
        }

        private void menuFilter40Hz_Click(object sender, EventArgs e)
        {
            menuFilterNone.Checked = false;
            menuFilter40Hz.Checked = true;
            menuFilterMuscle.Checked = false;

            _BottomCutoff = 0.05;
            _TopCutoff = 40.0;

            DrawBuffer = null;
            Refresh();
        }

        private void menuFilterMuscle_Click(object sender, EventArgs e)
        {
            menuFilterNone.Checked = false;
            menuFilter40Hz.Checked = false;
            menuFilterMuscle.Checked = true;

            _BottomCutoff = 0.05;
            _TopCutoff = 35.0;

            DrawBuffer = null;
            Refresh();
        }
	}
}
