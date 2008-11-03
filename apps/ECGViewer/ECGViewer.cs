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
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Text;
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
		private System.Windows.Forms.Label labelDiagnostic;
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
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private UnknownECGReader _ECGReader = new UnknownECGReader();
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
					if ((_CurrentECG != null)
					&&	(_CurrentECG != value))
						_CurrentECG.Dispose();

					if (value == null)
					{
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

							_CurrentECG = null;
						}
						else
						{
							Signals sig = _CurrentSignal.CalculateTwelveLeads();

							if (sig != null)
								_CurrentSignal = sig;

							int start, end;

							_CurrentSignal.CalculateStartAndEnd(out start, out end);

							ECGTimeScrollbar.Minimum = 0;
							ECGTimeScrollbar.Maximum = end - start;
							ECGTimeScrollbar.Value = 0;
							ECGTimeScrollbar.SmallChange = _CurrentSignal.RhythmSamplesPerSecond;
							ECGTimeScrollbar.LargeChange = _CurrentSignal.RhythmSamplesPerSecond;
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

		public ECGViewer()
		{
/*			// Might be intressting to add different colors.
			ECGDraw.BackColor = Color.Black;
			ECGDraw.GraphColor = Color.Gray;
			ECGDraw.GraphSecondColor = Color.FromArgb(96, 96, 96);
			ECGDraw.SignalColor = Color.Lime;
			ECGDraw.TextColor = Color.Lime;*/

			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		public static void Main(string[] args)
		{
			Application.Run(new ECGViewer());
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(ECGViewer));
			this.mainMenu = new System.Windows.Forms.MainMenu();
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
			this.menuGain = new System.Windows.Forms.MenuItem();
			this.menuGain4 = new System.Windows.Forms.MenuItem();
			this.menuGain3 = new System.Windows.Forms.MenuItem();
			this.menuGain2 = new System.Windows.Forms.MenuItem();
			this.menuGain1 = new System.Windows.Forms.MenuItem();
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
			this.labelDiagnostic = new System.Windows.Forms.Label();
			this.labelTime = new System.Windows.Forms.Label();
			this.labelPatient = new System.Windows.Forms.Label();
			this.statusBar = new System.Windows.Forms.StatusBar();
			this.openECGFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.saveECGFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
			this.ECGTimeScrollbar = new System.Windows.Forms.HScrollBar();
			this.menuGridType = new System.Windows.Forms.MenuItem();
			this.menuGridFive = new System.Windows.Forms.MenuItem();
			this.menuGridOne = new System.Windows.Forms.MenuItem();
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
																					 this.menuGain,
																					 this.menuGridType,
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
			this.menuLeadFormatRegular.Text = "Regular";
			this.menuLeadFormatRegular.Click += new System.EventHandler(this.menuLeadFormatRegular_Click);
			// 
			// menuLeadFormatThreeXFour
			// 
			this.menuLeadFormatThreeXFour.Index = 1;
			this.menuLeadFormatThreeXFour.RadioCheck = true;
			this.menuLeadFormatThreeXFour.Text = "3x4";
			this.menuLeadFormatThreeXFour.Click += new System.EventHandler(this.menuLeadFormatFourXThree_Click);
			// 
			// menuLeadFormatThreeXFourPlusOne
			// 
			this.menuLeadFormatThreeXFourPlusOne.Index = 2;
			this.menuLeadFormatThreeXFourPlusOne.RadioCheck = true;
			this.menuLeadFormatThreeXFourPlusOne.Text = "3x4+1";
			this.menuLeadFormatThreeXFourPlusOne.Click += new System.EventHandler(this.menuLeadFormatFourXThreePlusOne_Click);
			// 
			// menuLeadFormatThreeXFourPlusThree
			// 
			this.menuLeadFormatThreeXFourPlusThree.Index = 3;
			this.menuLeadFormatThreeXFourPlusThree.RadioCheck = true;
			this.menuLeadFormatThreeXFourPlusThree.Text = "3x4+3";
			this.menuLeadFormatThreeXFourPlusThree.Click += new System.EventHandler(this.menuLeadFormatFourXThreePlusThree_Click);
			// 
			// menuLeadFormatSixXTwo
			// 
			this.menuLeadFormatSixXTwo.Index = 4;
			this.menuLeadFormatSixXTwo.RadioCheck = true;
			this.menuLeadFormatSixXTwo.Text = "6x2";
			this.menuLeadFormatSixXTwo.Click += new System.EventHandler(this.menuLeadFormatSixXTwo_Click);
			// 
			// menuLeadFormatMedian
			// 
			this.menuLeadFormatMedian.Index = 5;
			this.menuLeadFormatMedian.Text = "Average Complex";
			this.menuLeadFormatMedian.Click += new System.EventHandler(this.menuLeadFormatMedian_Click);
			// 
			// menuGain
			// 
			this.menuGain.Index = 1;
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
			// menuDisplayInfo
			// 
			this.menuDisplayInfo.Checked = true;
			this.menuDisplayInfo.Index = 3;
			this.menuDisplayInfo.Text = "Display Info";
			this.menuDisplayInfo.Click += new System.EventHandler(this.menuDisplayInfo_Click);
			// 
			// menuAnnonymize
			// 
			this.menuAnnonymize.Index = 4;
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
			this.menuAddPluginFile.Text = "File ...";
			this.menuAddPluginFile.Click += new System.EventHandler(this.menuAddPluginFile_Click);
			// 
			// menuAddPluginDir
			// 
			this.menuAddPluginDir.Index = 1;
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
			this.ECGPanel.Location = new System.Drawing.Point(1, 1);
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
			// 
			// labelPatientSecond
			// 
			this.labelPatientSecond.BackColor = System.Drawing.Color.Transparent;
			this.labelPatientSecond.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.labelPatientSecond.ForeColor = System.Drawing.SystemColors.ControlText;
			this.labelPatientSecond.Location = new System.Drawing.Point(222, 29);
			this.labelPatientSecond.Name = "labelPatientSecond";
			this.labelPatientSecond.Size = new System.Drawing.Size(123, 66);
			this.labelPatientSecond.TabIndex = 4;
			// 
			// labelDiagnostic
			// 
			this.labelDiagnostic.BackColor = System.Drawing.Color.Transparent;
			this.labelDiagnostic.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.labelDiagnostic.ForeColor = System.Drawing.SystemColors.ControlText;
			this.labelDiagnostic.Location = new System.Drawing.Point(365, 5);
			this.labelDiagnostic.Name = "labelDiagnostic";
			this.labelDiagnostic.Size = new System.Drawing.Size(310, 427);
			this.labelDiagnostic.TabIndex = 3;
			// 
			// labelTime
			// 
			this.labelTime.BackColor = System.Drawing.Color.Transparent;
			this.labelTime.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.labelTime.ForeColor = System.Drawing.SystemColors.ControlText;
			this.labelTime.Location = new System.Drawing.Point(210, 5);
			this.labelTime.Name = "labelTime";
			this.labelTime.Size = new System.Drawing.Size(145, 15);
			this.labelTime.TabIndex = 2;
			// 
			// labelPatient
			// 
			this.labelPatient.BackColor = System.Drawing.Color.Transparent;
			this.labelPatient.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
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
			// menuGridType
			// 
			this.menuGridType.Index = 2;
			this.menuGridType.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																						 this.menuGridOne,
																						 this.menuGridFive});
			this.menuGridType.Text = "Grid Type";
			// 
			// menuGridFive
			// 
			this.menuGridFive.Index = 1;
			this.menuGridFive.RadioCheck = true;
			this.menuGridFive.Checked = ECGDraw.DisplayGrid == ECGDraw.GridType.FiveMillimeters;
			this.menuGridFive.Text = "5 mm";
			this.menuGridFive.Click += new System.EventHandler(this.menuGridFive_Click);
			// 
			// menuGridOne
			// 
			this.menuGridOne.Index = 0;
			this.menuGridOne.RadioCheck = true;
			this.menuGridOne.Checked = ECGDraw.DisplayGrid == ECGDraw.GridType.OneMillimeters;
			this.menuGridOne.Text = "1 mm";
			this.menuGridOne.Click += new System.EventHandler(this.menuGridOne_Click);
			// 
			// ECGViewer
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
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
			this.Resize += new System.EventHandler(this.ECGViewer_Resize);
			this.Load += new System.EventHandler(this.ECGViewer_Load);
			this.ECGPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		private void ECGViewer_Load(object sender, System.EventArgs e)
		{
			menuOpenSystems.MenuItems.Clear();
			menuSaveSystems.MenuItems.Clear();

			string[] manSysList = ECGConverter.Instance.getSupportedManagementSystemsList();

			for (int i=0;i < manSysList.Length;i++)
			{
				System.Windows.Forms.MenuItem item = new MenuItem(manSysList[i], new EventHandler(menuECGMSOpen_Click));

				menuOpenSystems.MenuItems.Add(item);

				if (ECGConverter.Instance.hasECGManagementSystemSaveSupport(i))
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
					ECGTimeScrollbar.Value = (oldPos * _CurrentSignal.RhythmSamplesPerSecond) / oldSPS;
				}

				if (ECGTimeScrollbar.Enabled)
				{
					ECGTimeScrollbar.LargeChange = _CurrentSignal.RhythmSamplesPerSecond;
				}
			}

			this.ECGTimeScrollbar.Width = this.ECGPanel.Width = this.Width - (this.ECGPanel.Left * 2) - 10;
			this.ECGPanel.Height = this.Height - this.ECGPanel.Top - 50 - this.statusBar.Height - this.ECGTimeScrollbar.Height;
			this.ECGTimeScrollbar.Top = this.ECGPanel.Bottom;

			this.InnerECGPanel.Height = this.ECGPanel.Height - this.InnerECGPanel.Top;
			this.InnerECGPanel.Width = this.ECGPanel.Width - this.InnerECGPanel.Left;

			this.labelDiagnostic.Width = this.Width - this.labelDiagnostic.Left - 5;

			this.ECGPanel.Refresh();
		}

		private void menuOpenFile_Click(object sender, System.EventArgs e)
		{
			try
			{
				StringBuilder sb = new StringBuilder();

				sb.Append("Any ECG File (*.*)|*.*");

				int i=0;

				foreach (string format in ECGConverter.Instance.getSupportedFormatsList())
				{
					string extension = ECGConverter.Instance.getExtension(i);

					if (ECGConverter.Instance.hasUnknownReaderSupport(i++))
					{
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
					IECGFormat format = _ECGReader.Read(this.openECGFileDialog.FileName);

					if (format != null)
					{
						CurrentECG = format;	

						this.statusBar.Text = "Opened file!";
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

							Config cfgScreen = new Config(supportedList[index], cfg);

							dr = cfgScreen.ShowDialog(this);
						}

						if (dr != DialogResult.OK)
							return;

						try
						{
							if (CurrentECG.GetType() != ECGConverter.Instance.getType(index))
							{
								if ((ECGConverter.Instance.Convert(CurrentECG, index, cfg, out writeFile) != 0)
								||	!writeFile.Works())
									writeFile = null;
							}
						}
						catch
						{
							writeFile = null;
						}

						if (writeFile == null)
						{
							MessageBox.Show(this, "Converting of file has failed!" , "Converting failed!", MessageBoxButtons.OK, MessageBoxIcon.Error);

							return;
						}
					}
				
					ECGWriter.Write(writeFile, saveECGFileDialog.FileName, true);

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

					ECGViewer_Load(sender, e);
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

					ECGViewer_Load(sender, e);
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

					_DrawBuffer = new Bitmap(w, h);

					int n = 0;
					int[,] s = {{782, 492}, {1042, 657}, {1302, 822}};

					for (;n < s.GetLength(0);n++)
						if ((s[n, 0] > w)
							||	(s[n, 1] > h))
							break;

					n+=2;
				
					ECGConversion.ECGDraw.DpiX = ECGConversion.ECGDraw.DpiY = 25.4f * n;

					int
						oldSPS = _CurrentSignal.RhythmSamplesPerSecond,
						ret = ECGDraw.DrawECG(Graphics.FromImage(_DrawBuffer), _CurrentSignal, _DrawType, ECGTimeScrollbar.Value, 25.0f, _Gain, true);

					if (ret < 0)
					{
						ret = ECGDraw.DrawECG(Graphics.FromImage(_DrawBuffer), _CurrentSignal, ECGDraw.ECGDrawType.Regular, ECGTimeScrollbar.Value, 25.0f, _Gain, true);
					}

					if (_DrawType != ECGDraw.ECGDrawType.Median)
					{
						if (oldSPS != _CurrentSignal.RhythmSamplesPerSecond)
						{
							ECGTimeScrollbar.Minimum = 0;
							ECGTimeScrollbar.SmallChange = _CurrentSignal.RhythmSamplesPerSecond;
							ECGTimeScrollbar.LargeChange = (ECGTimeScrollbar.LargeChange * _CurrentSignal.RhythmSamplesPerSecond) / oldSPS;
							ECGTimeScrollbar.Value= (ECGTimeScrollbar.Value * _CurrentSignal.RhythmSamplesPerSecond) / oldSPS;
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

				e.Graphics.DrawImage(_DrawBuffer, 0, 0, _DrawBuffer.Width, _DrawBuffer.Height);
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
			}
			else
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
					if (stat.statement != null)
					{
						sb = new StringBuilder();

						foreach (string temp in stat.statement)
						{
							sb.Append(temp);
							sb.Append('\n');
						}

						string temp2 = stat.statement[stat.statement.Length-1];

						if ((temp2 != null)
						&&	!temp2.StartsWith("Confirmed by")
						&&	!temp2.StartsWith("Interpreted by")
						&&	!temp2.StartsWith("Reviewed by"))
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
					}
				}
				else
				{
					this.labelDiagnostic.Text = "";
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
			menuGridFive.Checked = false;
			menuGridOne.Checked = true;
		
			ECGDraw.DisplayGrid = ECGDraw.GridType.OneMillimeters;

			lock(this)
			{
				_DrawBuffer = null;
			}

			Refresh();
		}

		private void menuGridFive_Click(object sender, System.EventArgs e)
		{
			menuGridFive.Checked = true;
			menuGridOne.Checked = false;

			ECGDraw.DisplayGrid = ECGDraw.GridType.FiveMillimeters;

			lock(this)
			{
				_DrawBuffer = null;
			}

			Refresh();
		}
	}
}
