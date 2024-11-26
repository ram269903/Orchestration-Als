namespace Common.KeyGen
{
    partial class frmMain
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkFreeText = new System.Windows.Forms.CheckBox();
            this.txtFreeText = new System.Windows.Forms.TextBox();
            this.btnPrivateKey = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.txtPrivateKey = new System.Windows.Forms.TextBox();
            this.chkDateRange = new System.Windows.Forms.CheckBox();
            this.chkTieToPC = new System.Windows.Forms.CheckBox();
            this.dtTo = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.dtFrom = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.btnGenerateKey = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.button1 = new System.Windows.Forms.Button();
            this.btnDecryptKey = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.txtKey = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtUsers = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtBulk = new System.Windows.Forms.TextBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.txtUsers);
            this.groupBox1.Controls.Add(this.chkFreeText);
            this.groupBox1.Controls.Add(this.txtFreeText);
            this.groupBox1.Controls.Add(this.btnPrivateKey);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.txtPrivateKey);
            this.groupBox1.Controls.Add(this.chkDateRange);
            this.groupBox1.Controls.Add(this.chkTieToPC);
            this.groupBox1.Controls.Add(this.dtTo);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.dtFrom);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.txtName);
            this.groupBox1.Location = new System.Drawing.Point(18, 7);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(5);
            this.groupBox1.Size = new System.Drawing.Size(655, 764);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            // 
            // chkFreeText
            // 
            this.chkFreeText.AutoSize = true;
            this.chkFreeText.Location = new System.Drawing.Point(24, 323);
            this.chkFreeText.Margin = new System.Windows.Forms.Padding(4);
            this.chkFreeText.Name = "chkFreeText";
            this.chkFreeText.Size = new System.Drawing.Size(120, 29);
            this.chkFreeText.TabIndex = 15;
            this.chkFreeText.Text = "Modules";
            this.chkFreeText.UseVisualStyleBackColor = true;
            // 
            // txtFreeText
            // 
            this.txtFreeText.Location = new System.Drawing.Point(24, 360);
            this.txtFreeText.Margin = new System.Windows.Forms.Padding(5);
            this.txtFreeText.Multiline = true;
            this.txtFreeText.Name = "txtFreeText";
            this.txtFreeText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtFreeText.Size = new System.Drawing.Size(600, 147);
            this.txtFreeText.TabIndex = 14;
            // 
            // btnPrivateKey
            // 
            this.btnPrivateKey.Location = new System.Drawing.Point(24, 702);
            this.btnPrivateKey.Margin = new System.Windows.Forms.Padding(4);
            this.btnPrivateKey.Name = "btnPrivateKey";
            this.btnPrivateKey.Size = new System.Drawing.Size(215, 43);
            this.btnPrivateKey.TabIndex = 13;
            this.btnPrivateKey.Text = "Generate Private Key";
            this.btnPrivateKey.UseVisualStyleBackColor = true;
            this.btnPrivateKey.Click += new System.EventHandler(this.btnPrivateKey_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(24, 620);
            this.label4.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(122, 25);
            this.label4.TabIndex = 12;
            this.label4.Text = "Private Key";
            // 
            // txtPrivateKey
            // 
            this.txtPrivateKey.Location = new System.Drawing.Point(24, 649);
            this.txtPrivateKey.Margin = new System.Windows.Forms.Padding(5);
            this.txtPrivateKey.Name = "txtPrivateKey";
            this.txtPrivateKey.Size = new System.Drawing.Size(600, 31);
            this.txtPrivateKey.TabIndex = 11;
            // 
            // chkDateRange
            // 
            this.chkDateRange.AutoSize = true;
            this.chkDateRange.Location = new System.Drawing.Point(24, 134);
            this.chkDateRange.Margin = new System.Windows.Forms.Padding(4);
            this.chkDateRange.Name = "chkDateRange";
            this.chkDateRange.Size = new System.Drawing.Size(228, 29);
            this.chkDateRange.TabIndex = 9;
            this.chkDateRange.Text = "Validity Date Range";
            this.chkDateRange.UseVisualStyleBackColor = true;
            // 
            // chkTieToPC
            // 
            this.chkTieToPC.AutoSize = true;
            this.chkTieToPC.Location = new System.Drawing.Point(24, 272);
            this.chkTieToPC.Margin = new System.Windows.Forms.Padding(4);
            this.chkTieToPC.Name = "chkTieToPC";
            this.chkTieToPC.Size = new System.Drawing.Size(127, 29);
            this.chkTieToPC.TabIndex = 8;
            this.chkTieToPC.Text = "Tie to PC";
            this.chkTieToPC.UseVisualStyleBackColor = true;
            // 
            // dtTo
            // 
            this.dtTo.Location = new System.Drawing.Point(127, 215);
            this.dtTo.Margin = new System.Windows.Forms.Padding(4);
            this.dtTo.Name = "dtTo";
            this.dtTo.Size = new System.Drawing.Size(329, 31);
            this.dtTo.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(61, 215);
            this.label3.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(37, 25);
            this.label3.TabIndex = 5;
            this.label3.Text = "To";
            // 
            // dtFrom
            // 
            this.dtFrom.Location = new System.Drawing.Point(127, 170);
            this.dtFrom.Margin = new System.Windows.Forms.Padding(4);
            this.dtFrom.Name = "dtFrom";
            this.dtFrom.Size = new System.Drawing.Size(329, 31);
            this.dtFrom.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(61, 170);
            this.label2.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(61, 25);
            this.label2.TabIndex = 3;
            this.label2.Text = "From";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(24, 32);
            this.label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 25);
            this.label1.TabIndex = 1;
            this.label1.Text = "Name";
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(24, 61);
            this.txtName.Margin = new System.Windows.Forms.Padding(5);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(600, 31);
            this.txtName.TabIndex = 0;
            // 
            // btnGenerateKey
            // 
            this.btnGenerateKey.Location = new System.Drawing.Point(424, 702);
            this.btnGenerateKey.Margin = new System.Windows.Forms.Padding(4);
            this.btnGenerateKey.Name = "btnGenerateKey";
            this.btnGenerateKey.Size = new System.Drawing.Size(186, 43);
            this.btnGenerateKey.TabIndex = 10;
            this.btnGenerateKey.Text = "Generate Key";
            this.btnGenerateKey.UseVisualStyleBackColor = true;
            this.btnGenerateKey.Click += new System.EventHandler(this.btnGenerateKey_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.txtBulk);
            this.groupBox2.Controls.Add(this.button1);
            this.groupBox2.Controls.Add(this.btnDecryptKey);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.txtKey);
            this.groupBox2.Controls.Add(this.btnGenerateKey);
            this.groupBox2.Location = new System.Drawing.Point(683, 7);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(5);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(5);
            this.groupBox2.Size = new System.Drawing.Size(630, 764);
            this.groupBox2.TabIndex = 13;
            this.groupBox2.TabStop = false;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(201, 702);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(234, 43);
            this.button1.TabIndex = 14;
            this.button1.Text = "Generate Bulk Keys";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnDecryptKey
            // 
            this.btnDecryptKey.Location = new System.Drawing.Point(24, 702);
            this.btnDecryptKey.Margin = new System.Windows.Forms.Padding(4);
            this.btnDecryptKey.Name = "btnDecryptKey";
            this.btnDecryptKey.Size = new System.Drawing.Size(186, 43);
            this.btnDecryptKey.TabIndex = 13;
            this.btnDecryptKey.Text = "Decrypt Key";
            this.btnDecryptKey.UseVisualStyleBackColor = true;
            this.btnDecryptKey.Click += new System.EventHandler(this.btnDecryptKey_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(19, 29);
            this.label5.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(49, 25);
            this.label5.TabIndex = 12;
            this.label5.Text = "Key";
            // 
            // txtKey
            // 
            this.txtKey.Location = new System.Drawing.Point(24, 61);
            this.txtKey.Margin = new System.Windows.Forms.Padding(5);
            this.txtKey.Multiline = true;
            this.txtKey.Name = "txtKey";
            this.txtKey.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtKey.Size = new System.Drawing.Size(585, 544);
            this.txtKey.TabIndex = 11;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(19, 532);
            this.label6.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(135, 25);
            this.label6.TabIndex = 18;
            this.label6.Text = "No. Of Users";
            // 
            // txtUsers
            // 
            this.txtUsers.Location = new System.Drawing.Point(19, 561);
            this.txtUsers.Margin = new System.Windows.Forms.Padding(5);
            this.txtUsers.Name = "txtUsers";
            this.txtUsers.Size = new System.Drawing.Size(600, 31);
            this.txtUsers.TabIndex = 17;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(19, 620);
            this.label7.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(149, 25);
            this.label7.TabIndex = 20;
            this.label7.Text = "Bulk Generate";
            // 
            // txtBulk
            // 
            this.txtBulk.Location = new System.Drawing.Point(19, 649);
            this.txtBulk.Margin = new System.Windows.Forms.Padding(5);
            this.txtBulk.Name = "txtBulk";
            this.txtBulk.Size = new System.Drawing.Size(590, 31);
            this.txtBulk.TabIndex = 19;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1337, 787);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Margin = new System.Windows.Forms.Padding(5);
            this.Name = "frmMain";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PB Key Generator";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.DateTimePicker dtTo;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DateTimePicker dtFrom;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Button btnGenerateKey;
        private System.Windows.Forms.CheckBox chkDateRange;
        private System.Windows.Forms.CheckBox chkTieToPC;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox txtKey;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtPrivateKey;
        private System.Windows.Forms.Button btnPrivateKey;
        private System.Windows.Forms.Button btnDecryptKey;
        private System.Windows.Forms.CheckBox chkFreeText;
        private System.Windows.Forms.TextBox txtFreeText;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtUsers;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtBulk;
    }
}

