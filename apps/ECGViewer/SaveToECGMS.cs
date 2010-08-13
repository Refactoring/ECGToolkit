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
using System.Windows.Forms;

using ECGConversion;

namespace ECGViewer
{
	/// <summary>
	/// Summary description for SaveToPACS.
	/// </summary>
	public class SaveToECGMS : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.TextBox textPatientId;
		private System.Windows.Forms.Label labelPatientId;
		private ECGConversion.ECGManagementSystem.IECGManagementSystem _ManSys;
		private IECGFormat _Source;
		private System.Windows.Forms.Button buttonSetup;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public SaveToECGMS(IECGFormat src, ECGConversion.ECGManagementSystem.IECGManagementSystem manSys)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			_Source = src;
			_ManSys = manSys;
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
			this.buttonCancel = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.textPatientId = new System.Windows.Forms.TextBox();
			this.labelPatientId = new System.Windows.Forms.Label();
			this.buttonSetup = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// buttonCancel
			// 
			this.buttonCancel.Location = new System.Drawing.Point(208, 40);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.TabIndex = 0;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// buttonOK
			// 
			this.buttonOK.Location = new System.Drawing.Point(128, 40);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.TabIndex = 1;
			this.buttonOK.Text = "OK";
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// textPatientId
			// 
			this.textPatientId.Location = new System.Drawing.Point(136, 8);
			this.textPatientId.Name = "textPatientId";
			this.textPatientId.Size = new System.Drawing.Size(144, 20);
			this.textPatientId.TabIndex = 10;
			this.textPatientId.Text = "";
			// 
			// labelPatientId
			// 
			this.labelPatientId.Location = new System.Drawing.Point(8, 8);
			this.labelPatientId.Name = "labelPatientId";
			this.labelPatientId.Size = new System.Drawing.Size(120, 24);
			this.labelPatientId.TabIndex = 11;
			this.labelPatientId.Text = "Patient Id (optional):";
			this.labelPatientId.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// buttonSetup
			// 
			this.buttonSetup.Location = new System.Drawing.Point(8, 40);
			this.buttonSetup.Name = "buttonSetup";
			this.buttonSetup.TabIndex = 12;
			this.buttonSetup.Text = "Setup";
			this.buttonSetup.Click += new System.EventHandler(this.buttonSetup_Click);
			// 
			// SaveToECGMS
			// 
			this.ClientSize = new System.Drawing.Size(288, 70);
			this.ControlBox = false;
			this.Controls.Add(this.buttonSetup);
			this.Controls.Add(this.labelPatientId);
			this.Controls.Add(this.textPatientId);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.buttonCancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SaveToECGMS";
			this.ShowInTaskbar = false;
			this.Text = "Save to";
			this.Load += new System.EventHandler(this.SaveToECGMS_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void SaveToECGMS_Load(object sender, System.EventArgs e)
		{
			if ((_ManSys == null)
			||	(_Source == null))
				Close();

			buttonOK.Enabled = _ManSys.ConfiguredToSave();
			buttonSetup.Enabled = (_ManSys.Config != null) && (_ManSys.Config.NrConfigItems > 0);
		}

		private void buttonCancel_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		private void buttonOK_Click(object sender, System.EventArgs e)
		{
			try
			{
				string patid = textPatientId.Text;

				if (_Source.GetType() != ECGConverter.Instance.getType(_ManSys.FormatName))
				{
					ECGConfig cfg = ECGConverter.Instance.getConfig(_ManSys.FormatName);

					if (cfg != null)
					{
						Config cfgScreen = new Config(_ManSys.FormatName, cfg);

						if (cfgScreen.ShowDialog(this) != DialogResult.OK)
							return;
					}
				}

				if ((patid != null)
				&&	(patid.Length == 0))
					patid = null;

				int result = _ManSys.SaveECG(_Source, patid);

				if (result == 0)
				{
					MessageBox.Show(this, "Sending of ECG to PACS was successfully completed.", "ECG send to PACS!", MessageBoxButtons.OK, MessageBoxIcon.Information);

					Close();
				}
				else
				{
					string text = "Sending of ECG failed due to unknown reason!";

					switch (result)
					{
						case 1:
							text = "Sending of ECG failed due to bad configuration!";
							break;
						case 2:
							text = "Sending of ECG failed because converting to DICOM failed!";
							break;
						case 3:
							text = "Sending of ECG failed because of failure of connection!";
							break;
						case -1:
							text = "Sending of ECG is not supported!";
							break;
						default:
							text = "Sending of ECG failed for unknown reason!";
							break;
					}

					MessageBox.Show(this, text, "Sending ECG failed", MessageBoxButtons.OK, MessageBoxIcon.Error);

				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.ToString(), ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void buttonSetup_Click(object sender, System.EventArgs e)
		{
			Config cfg = new Config(_ManSys.Name, _ManSys.Config, new ECGConfig.CheckConfigFunction(_ManSys.ConfiguredToSave));

			cfg.ShowDialog(this);

			buttonOK.Enabled = _ManSys.ConfiguredToSave();
		}
	}
}
