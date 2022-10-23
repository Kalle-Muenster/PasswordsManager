<<<<<<< HEAD
namespace Passwords.ICO
=======
﻿namespace Passwords.ICO
>>>>>>> refs/remotes/fork/main
{
    partial class DasBedienFeld
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing )
        {
            if( disposing && ( components != null ) ) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btn_TheGUI = new System.Windows.Forms.Button();
            this.btn_Start = new System.Windows.Forms.Button();
            this.btn_Stop = new System.Windows.Forms.Button();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // btn_TheGUI
            // 
            this.btn_TheGUI.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(34)))), ((int)(((byte)(34)))), ((int)(((byte)(34)))));
            this.btn_TheGUI.FlatAppearance.BorderColor = System.Drawing.Color.Wheat;
            this.btn_TheGUI.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_TheGUI.ForeColor = System.Drawing.Color.Wheat;
            this.btn_TheGUI.Location = new System.Drawing.Point(12, 331);
            this.btn_TheGUI.Name = "btn_TheGUI";
            this.btn_TheGUI.Size = new System.Drawing.Size(258, 37);
            this.btn_TheGUI.TabIndex = 0;
            this.btn_TheGUI.Tag = "Gui";
            this.btn_TheGUI.Text = "ThePasswords - TheGUI";
            this.btn_TheGUI.UseVisualStyleBackColor = false;
            this.btn_TheGUI.Click += new System.EventHandler(this.BedienfeldButton_TheGUIClick);
            this.btn_TheGUI.MouseEnter += new System.EventHandler(this.BedienfeldButton_OnHover);
            this.btn_TheGUI.MouseLeave += new System.EventHandler(this.BedienfeldButton_OnHover);
            // 
            // btn_Start
            // 
            this.btn_Start.ForeColor = System.Drawing.Color.Wheat;
            this.btn_Start.Location = new System.Drawing.Point(12, 96);
            this.btn_Start.Name = "btn_Start";
            this.btn_Start.Size = new System.Drawing.Size(91, 23);
            this.btn_Start.TabIndex = 0;
            this.btn_Start.Text = "Start Server";
            this.btn_Start.Click += new System.EventHandler(this.BedienfeldButton_StartClick);
            this.btn_Start.MouseEnter += new System.EventHandler(this.BedienfeldButton_OnHover);
            this.btn_Start.MouseLeave += new System.EventHandler(this.BedienfeldButton_OnHover);
            // 
            // btn_Stop
            // 
            this.btn_Stop.ForeColor = System.Drawing.Color.Wheat;
            this.btn_Stop.Location = new System.Drawing.Point(12, 145);
            this.btn_Stop.Name = "btn_Stop";
            this.btn_Stop.Size = new System.Drawing.Size(91, 23);
            this.btn_Stop.TabIndex = 0;
            this.btn_Stop.Text = "Stop Server";
            this.btn_Stop.Click += new System.EventHandler(this.BedienfeldButton_StopClick);
            this.btn_Stop.MouseEnter += new System.EventHandler(this.BedienfeldButton_OnHover);
            this.btn_Stop.MouseLeave += new System.EventHandler(this.BedienfeldButton_OnHover);
            // 
            // checkBox1
            // 
            this.checkBox1.Location = new System.Drawing.Point(149, 251);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(104, 24);
            this.checkBox1.TabIndex = 0;
            this.checkBox1.Text = "OptionX";
            // 
            // checkBox2
            // 
            this.checkBox2.Location = new System.Drawing.Point(12, 251);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(104, 24);
            this.checkBox2.TabIndex = 0;
            this.checkBox2.Text = "OptionA";
            // 
            // comboBox1
            // 
            this.comboBox1.Location = new System.Drawing.Point(149, 281);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(121, 23);
            this.comboBox1.TabIndex = 0;
            // 
            // DasBedienFeld
            // 
            this.ClientSize = new System.Drawing.Size(363, 453);
            this.Controls.Add(this.btn_TheGUI);
            this.Controls.Add(this.btn_Start);
            this.Controls.Add(this.btn_Stop);
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.checkBox2);
            this.Controls.Add(this.comboBox1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "DasBedienFeld";
            this.ResumeLayout(false);

        }

        #endregion

        private Button btn_TheGUI;
        private Button btn_Start;
        private Button btn_Stop;
        private CheckBox checkBox1;
        private CheckBox checkBox2;
        private ComboBox comboBox1;
    }
}