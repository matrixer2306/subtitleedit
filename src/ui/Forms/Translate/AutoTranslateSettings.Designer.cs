﻿namespace Nikse.SubtitleEdit.Forms.Translate
{
    sealed partial class AutoTranslateSettings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelParagraphHandling = new System.Windows.Forms.Label();
            this.labelMaxBytes = new System.Windows.Forms.Label();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOk = new System.Windows.Forms.Button();
            this.labelPrompt = new System.Windows.Forms.Label();
            this.labelDelay = new System.Windows.Forms.Label();
            this.labelTemperature = new System.Windows.Forms.Label();
            this.nikseUpDownTemperature = new Nikse.SubtitleEdit.Controls.NikseUpDown();
            this.nikseUpDownDelay = new Nikse.SubtitleEdit.Controls.NikseUpDown();
            this.nikseTextBoxPrompt = new Nikse.SubtitleEdit.Controls.NikseTextBox();
            this.nikseUpDownMaxBytes = new Nikse.SubtitleEdit.Controls.NikseUpDown();
            this.comboBoxParagraphHandling = new Nikse.SubtitleEdit.Controls.NikseComboBox();
            this.SuspendLayout();
            // 
            // labelParagraphHandling
            // 
            this.labelParagraphHandling.AutoSize = true;
            this.labelParagraphHandling.Location = new System.Drawing.Point(19, 22);
            this.labelParagraphHandling.Name = "labelParagraphHandling";
            this.labelParagraphHandling.Size = new System.Drawing.Size(87, 13);
            this.labelParagraphHandling.TabIndex = 91;
            this.labelParagraphHandling.Text = "Lines merge/split";
            // 
            // labelMaxBytes
            // 
            this.labelMaxBytes.AutoSize = true;
            this.labelMaxBytes.Location = new System.Drawing.Point(18, 96);
            this.labelMaxBytes.Name = "labelMaxBytes";
            this.labelMaxBytes.Size = new System.Drawing.Size(183, 13);
            this.labelMaxBytes.TabIndex = 94;
            this.labelMaxBytes.Text = "Max byte size for each server request";
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(716, 431);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 101;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            this.buttonCancel.KeyDown += new System.Windows.Forms.KeyEventHandler(this.buttonCancel_KeyDown);
            // 
            // buttonOk
            // 
            this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOk.Location = new System.Drawing.Point(635, 431);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(75, 23);
            this.buttonOk.TabIndex = 100;
            this.buttonOk.Text = "OK";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
            // 
            // labelPrompt
            // 
            this.labelPrompt.AutoSize = true;
            this.labelPrompt.Location = new System.Drawing.Point(18, 184);
            this.labelPrompt.Name = "labelPrompt";
            this.labelPrompt.Size = new System.Drawing.Size(102, 13);
            this.labelPrompt.TabIndex = 98;
            this.labelPrompt.Text = "Prompt for ChatGPT";
            // 
            // labelDelay
            // 
            this.labelDelay.AutoSize = true;
            this.labelDelay.Location = new System.Drawing.Point(19, 62);
            this.labelDelay.Name = "labelDelay";
            this.labelDelay.Size = new System.Drawing.Size(179, 13);
            this.labelDelay.TabIndex = 101;
            this.labelDelay.Text = "Delay seconds after each server call";
            // 
            // labelTemperature
            // 
            this.labelTemperature.AutoSize = true;
            this.labelTemperature.Location = new System.Drawing.Point(18, 131);
            this.labelTemperature.Name = "labelTemperature";
            this.labelTemperature.Size = new System.Drawing.Size(67, 13);
            this.labelTemperature.TabIndex = 103;
            this.labelTemperature.Text = "Temperature";
            // 
            // nikseUpDownTemperature
            // 
            this.nikseUpDownTemperature.BackColor = System.Drawing.SystemColors.Window;
            this.nikseUpDownTemperature.BackColorDisabled = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.nikseUpDownTemperature.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(171)))), ((int)(((byte)(173)))), ((int)(((byte)(179)))));
            this.nikseUpDownTemperature.BorderColorDisabled = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.nikseUpDownTemperature.ButtonForeColor = System.Drawing.SystemColors.ControlText;
            this.nikseUpDownTemperature.ButtonForeColorDown = System.Drawing.Color.Orange;
            this.nikseUpDownTemperature.ButtonForeColorOver = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.nikseUpDownTemperature.DecimalPlaces = 1;
            this.nikseUpDownTemperature.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.nikseUpDownTemperature.Location = new System.Drawing.Point(217, 127);
            this.nikseUpDownTemperature.Maximum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.nikseUpDownTemperature.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.nikseUpDownTemperature.Name = "nikseUpDownTemperature";
            this.nikseUpDownTemperature.Size = new System.Drawing.Size(75, 23);
            this.nikseUpDownTemperature.TabIndex = 102;
            this.nikseUpDownTemperature.TabStop = false;
            this.nikseUpDownTemperature.Text = "nikseUpDownTemperature";
            this.nikseUpDownTemperature.ThousandsSeparator = false;
            this.nikseUpDownTemperature.Value = new decimal(new int[] {
            0,
            0,
            0,
            0});
            // 
            // nikseUpDownDelay
            // 
            this.nikseUpDownDelay.BackColor = System.Drawing.SystemColors.Window;
            this.nikseUpDownDelay.BackColorDisabled = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.nikseUpDownDelay.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(171)))), ((int)(((byte)(173)))), ((int)(((byte)(179)))));
            this.nikseUpDownDelay.BorderColorDisabled = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.nikseUpDownDelay.ButtonForeColor = System.Drawing.SystemColors.ControlText;
            this.nikseUpDownDelay.ButtonForeColorDown = System.Drawing.Color.Orange;
            this.nikseUpDownDelay.ButtonForeColorOver = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.nikseUpDownDelay.DecimalPlaces = 0;
            this.nikseUpDownDelay.Increment = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nikseUpDownDelay.Location = new System.Drawing.Point(222, 57);
            this.nikseUpDownDelay.Maximum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.nikseUpDownDelay.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.nikseUpDownDelay.Name = "nikseUpDownDelay";
            this.nikseUpDownDelay.Size = new System.Drawing.Size(75, 23);
            this.nikseUpDownDelay.TabIndex = 20;
            this.nikseUpDownDelay.TabStop = false;
            this.nikseUpDownDelay.Text = "nikseUpDown2";
            this.nikseUpDownDelay.ThousandsSeparator = false;
            this.nikseUpDownDelay.Value = new decimal(new int[] {
            0,
            0,
            0,
            0});
            // 
            // nikseTextBoxPrompt
            // 
            this.nikseTextBoxPrompt.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.nikseTextBoxPrompt.FocusedColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.nikseTextBoxPrompt.Location = new System.Drawing.Point(22, 202);
            this.nikseTextBoxPrompt.Multiline = true;
            this.nikseTextBoxPrompt.Name = "nikseTextBoxPrompt";
            this.nikseTextBoxPrompt.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.nikseTextBoxPrompt.Size = new System.Drawing.Size(769, 213);
            this.nikseTextBoxPrompt.TabIndex = 40;
            // 
            // nikseUpDownMaxBytes
            // 
            this.nikseUpDownMaxBytes.BackColor = System.Drawing.SystemColors.Window;
            this.nikseUpDownMaxBytes.BackColorDisabled = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.nikseUpDownMaxBytes.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(171)))), ((int)(((byte)(173)))), ((int)(((byte)(179)))));
            this.nikseUpDownMaxBytes.BorderColorDisabled = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.nikseUpDownMaxBytes.ButtonForeColor = System.Drawing.SystemColors.ControlText;
            this.nikseUpDownMaxBytes.ButtonForeColorDown = System.Drawing.Color.Orange;
            this.nikseUpDownMaxBytes.ButtonForeColorOver = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.nikseUpDownMaxBytes.DecimalPlaces = 0;
            this.nikseUpDownMaxBytes.Increment = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nikseUpDownMaxBytes.Location = new System.Drawing.Point(217, 92);
            this.nikseUpDownMaxBytes.Maximum = new decimal(new int[] {
            128000,
            0,
            0,
            0});
            this.nikseUpDownMaxBytes.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.nikseUpDownMaxBytes.Name = "nikseUpDownMaxBytes";
            this.nikseUpDownMaxBytes.Size = new System.Drawing.Size(75, 23);
            this.nikseUpDownMaxBytes.TabIndex = 30;
            this.nikseUpDownMaxBytes.TabStop = false;
            this.nikseUpDownMaxBytes.Text = "nikseUpDown1";
            this.nikseUpDownMaxBytes.ThousandsSeparator = false;
            this.nikseUpDownMaxBytes.Value = new decimal(new int[] {
            0,
            0,
            0,
            0});
            // 
            // comboBoxParagraphHandling
            // 
            this.comboBoxParagraphHandling.BackColor = System.Drawing.SystemColors.Window;
            this.comboBoxParagraphHandling.BackColorDisabled = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.comboBoxParagraphHandling.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(171)))), ((int)(((byte)(173)))), ((int)(((byte)(179)))));
            this.comboBoxParagraphHandling.BorderColorDisabled = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(120)))), ((int)(((byte)(120)))));
            this.comboBoxParagraphHandling.ButtonForeColor = System.Drawing.SystemColors.ControlText;
            this.comboBoxParagraphHandling.ButtonForeColorDown = System.Drawing.Color.Orange;
            this.comboBoxParagraphHandling.ButtonForeColorOver = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.comboBoxParagraphHandling.DropDownHeight = 400;
            this.comboBoxParagraphHandling.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxParagraphHandling.DropDownWidth = 165;
            this.comboBoxParagraphHandling.FormattingEnabled = true;
            this.comboBoxParagraphHandling.Location = new System.Drawing.Point(112, 20);
            this.comboBoxParagraphHandling.MaxLength = 32767;
            this.comboBoxParagraphHandling.Name = "comboBoxParagraphHandling";
            this.comboBoxParagraphHandling.SelectedIndex = -1;
            this.comboBoxParagraphHandling.SelectedItem = null;
            this.comboBoxParagraphHandling.SelectedText = "";
            this.comboBoxParagraphHandling.Size = new System.Drawing.Size(272, 21);
            this.comboBoxParagraphHandling.TabIndex = 10;
            this.comboBoxParagraphHandling.UsePopupWindow = false;
            // 
            // AutoTranslateSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(803, 466);
            this.Controls.Add(this.nikseUpDownTemperature);
            this.Controls.Add(this.labelTemperature);
            this.Controls.Add(this.labelDelay);
            this.Controls.Add(this.nikseUpDownDelay);
            this.Controls.Add(this.nikseTextBoxPrompt);
            this.Controls.Add(this.labelPrompt);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.nikseUpDownMaxBytes);
            this.Controls.Add(this.labelMaxBytes);
            this.Controls.Add(this.comboBoxParagraphHandling);
            this.Controls.Add(this.labelParagraphHandling);
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(520, 340);
            this.Name = "AutoTranslateSettings";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "AutoTranslateSettings";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.AutoTranslateSettings_KeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Controls.NikseComboBox comboBoxParagraphHandling;
        private System.Windows.Forms.Label labelParagraphHandling;
        private System.Windows.Forms.Label labelMaxBytes;
        private Controls.NikseUpDown nikseUpDownMaxBytes;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.Label labelPrompt;
        private Controls.NikseTextBox nikseTextBoxPrompt;
        private Controls.NikseUpDown nikseUpDownDelay;
        private System.Windows.Forms.Label labelDelay;
        private Controls.NikseUpDown nikseUpDownTemperature;
        private System.Windows.Forms.Label labelTemperature;
    }
}