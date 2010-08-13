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
using System.Data.SqlClient;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;

using ECGConversion;
using ECGConversion.ECGManagementSystem;

namespace ECGViewer
{
	/// <summary>
	/// Summary description for OpenFromPACS.
	/// </summary>
	public class OpenFromECGMS : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.TextBox textPatientID;
		private System.Windows.Forms.ListBox listECG;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private IECGManagementSystem _ManSys;
		private IECGFormat _SelectedECG;
		private System.Windows.Forms.Button buttonSetup;
		private System.Windows.Forms.Label labelPatientID;
		private ECGInfo[] _ECGList;

		public IECGFormat SelectedECG
		{
			get
			{
				return _SelectedECG;
			}
		}

		public OpenFromECGMS(IECGManagementSystem ms)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			_ManSys = ms;
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
			this.listECG = new System.Windows.Forms.ListBox();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.textPatientID = new System.Windows.Forms.TextBox();
			this.buttonSetup = new System.Windows.Forms.Button();
			this.labelPatientID = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// listECG
			// 
			this.listECG.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.listECG.ItemHeight = 14;
			this.listECG.Location = new System.Drawing.Point(112, 8);
			this.listECG.Name = "listECG";
			this.listECG.Size = new System.Drawing.Size(504, 340);
			this.listECG.TabIndex = 0;
			this.listECG.SelectedIndexChanged += new System.EventHandler(this.listECG_SelectedIndexChanged);
			// 
			// buttonCancel
			// 
			this.buttonCancel.Location = new System.Drawing.Point(552, 352);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(64, 23);
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// buttonOK
			// 
			this.buttonOK.Enabled = false;
			this.buttonOK.Location = new System.Drawing.Point(472, 352);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(72, 23);
			this.buttonOK.TabIndex = 2;
			this.buttonOK.Text = "OK";
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// textPatientID
			// 
			this.textPatientID.Location = new System.Drawing.Point(8, 24);
			this.textPatientID.Name = "textPatientID";
			this.textPatientID.Size = new System.Drawing.Size(96, 20);
			this.textPatientID.TabIndex = 4;
			this.textPatientID.Text = "";
			this.textPatientID.TextChanged += new System.EventHandler(this.TextChange);
			// 
			// buttonSetup
			// 
			this.buttonSetup.Location = new System.Drawing.Point(112, 352);
			this.buttonSetup.Name = "buttonSetup";
			this.buttonSetup.Size = new System.Drawing.Size(72, 23);
			this.buttonSetup.TabIndex = 5;
			this.buttonSetup.Text = "Setup";
			this.buttonSetup.Click += new System.EventHandler(this.buttonSetup_Click);
			// 
			// labelPatientID
			// 
			this.labelPatientID.Location = new System.Drawing.Point(8, 8);
			this.labelPatientID.Name = "labelPatientID";
			this.labelPatientID.Size = new System.Drawing.Size(96, 16);
			this.labelPatientID.TabIndex = 6;
			this.labelPatientID.Text = "Patient ID:";
			// 
			// OpenFromECGMS
			// 
			this.ClientSize = new System.Drawing.Size(626, 382);
			this.ControlBox = false;
			this.Controls.Add(this.labelPatientID);
			this.Controls.Add(this.buttonSetup);
			this.Controls.Add(this.textPatientID);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.listECG);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "OpenFromECGMS";
			this.ShowInTaskbar = false;
			this.Text = "Open from ";
			this.Load += new System.EventHandler(this.OpenFromPACS_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void buttonCancel_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		private void buttonOK_Click(object sender, System.EventArgs e)
		{
			try
			{
				if (listECG.SelectedIndex >= 0)
				{
					_SelectedECG = _ManSys.getECG(_ECGList[listECG.SelectedIndex]);

					if ((_SelectedECG != null)
					&&	_SelectedECG.Works())
					{
						Close();
					}
					else
					{
						MessageBox.Show(this, "Opening of ECG from Management System failed!", "Opening ECG failed!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.ToString(), ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OpenFromPACS_Load(object sender, System.EventArgs e)
		{
			if ((_ManSys == null)
			||	!_ManSys.Works())
			{
				Close();

				return;
			}

			this.Text += _ManSys.Name;

			this.buttonSetup.Enabled = (_ManSys.Config != null) && (_ManSys.Config.NrConfigItems > 0);

			TextChange(sender, e);
		}

		private void TextChange(object sender, System.EventArgs e)
		{
			try
			{
				listECG.Items.Clear();
				_ECGList = null;
				buttonOK.Enabled = false;

				if (textPatientID.Text.Length < 5)
					return;

				Refresh();

				_ECGList = _ManSys.getECGList(textPatientID.Text);

				if (_ECGList == null)
					return;

				foreach (ECGInfo info in _ECGList)
				{
					StringBuilder sb = new StringBuilder();

					sb.Append(info.AcquisitionTime.ToString());
					sb.Append(' ');
					sb.Append(info.PatientID);
					sb.Append(' ');
					sb.Append(info.PatientName);
					sb.Append(' ');

					switch (info.Gender)
					{
						case ECGConversion.ECGDemographics.Sex.Male:
							sb.Append('M');
							break;
						case ECGConversion.ECGDemographics.Sex.Female:
							sb.Append('F');
							break;
						default:
							sb.Append('U');
							break;
					}

					listECG.Items.Add(sb.ToString());
				}
			}
			catch
			{
			}
		}

		private void listECG_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			buttonOK.Enabled = listECG.SelectedIndex >= 0;
		}

		private void buttonSetup_Click(object sender, System.EventArgs e)
		{
			Config cfg = new Config(_ManSys.Name, _ManSys.Config);

			cfg.ShowDialog(this);

			TextChange(sender, e);
		}
	}
}
