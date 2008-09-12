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

namespace ECGViewer
{
	/// <summary>
	/// Summary description for Config.
	/// </summary>
	public class Config : System.Windows.Forms.Form
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private ECGConversion.ECGConfig _Config;
		private ECGConversion.ECGConfig _OldConfig;
		private ECGConversion.ECGConfig.CheckConfigFunction _CheckConfig;

		public Config(string name, ECGConversion.ECGConfig config)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			this.Text += name;

			_Config = config;
		}

		public Config(string name, ECGConversion.ECGConfig config, ECGConversion.ECGConfig.CheckConfigFunction ccf) : this(name, config)
		{
			_CheckConfig = ccf;
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
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// buttonOK
			// 
			this.buttonOK.Location = new System.Drawing.Point(198, 242);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.TabIndex = 0;
			this.buttonOK.Text = "OK";
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.Location = new System.Drawing.Point(278, 242);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			// 
			// Config
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(359, 272);
			this.ControlBox = false;
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "Config";
			this.Text = "Setup ";
			this.Load += new System.EventHandler(this.Config_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void Config_Load(object sender, System.EventArgs e)
		{
			int nrItems = (_Config == null) ? 0 : _Config.NrConfigItems;

			if (nrItems <= 0)
			{
				Close();

				return;
			}

			_OldConfig = _Config.Clone(true);

			int pos = 0;

			int i=0;

			for (;i < nrItems;i++)
			{
				string name;
				bool must;

				_Config.getConfigItem(i, out name, out must);

				if (name != null)
				{
					TextBox tb = new TextBox();
					tb.Name = name;

					tb.Text = _Config[name];
					tb.TextChanged += new EventHandler(tb_TextChanged);
					tb.TabIndex = i;

					pos += (tb.Height >> 1);
					tb.Top = pos;
					tb.Left = (Width >> 1);
					tb.Width = (Width >> 1) - 10;

					Label label = new Label();

					label.AutoSize = true;
					label.TextAlign = ContentAlignment.MiddleLeft;
					label.Name = "label" + name;
					label.Text = name;

					if (must)
						label.Text += " *";

					label.Top = pos;
					label.Left = 5;
					label.Height = tb.Height;

					this.Controls.Add(tb);
					this.Controls.Add(label);

					pos += (tb.Height);
				}
			}

			pos += (buttonOK.Height >> 1);

			buttonCancel.Top = pos;
			buttonCancel.TabIndex = i++;

			buttonOK.Top = pos;
			buttonOK.TabIndex = i++;

			this.Height = buttonOK.Bottom + (buttonOK.Height >> 1) + 20;

			buttonOK.Enabled = _CheckConfig == null ? _Config.ConfigurationWorks() : _CheckConfig();
		}

		private void buttonCancel_Click(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.Cancel;

			_Config.Set(_OldConfig);

			Close();
		}

		private void buttonOK_Click(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.OK;

			Close();
		}

		private void tb_TextChanged(object sender, EventArgs e)
		{
			if (sender.GetType() == typeof(TextBox))
			{
				TextBox temp = (TextBox) sender;

				_Config[temp.Name] = temp.Text;

				buttonOK.Enabled = _CheckConfig == null ? _Config.ConfigurationWorks() : _CheckConfig();
			}
		}
	}
}
