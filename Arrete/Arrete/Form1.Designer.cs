namespace Arrete
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.panel1 = new System.Windows.Forms.Panel();
            this.groupBox13 = new System.Windows.Forms.GroupBox();
            this.label16 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panelError = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.labError = new System.Windows.Forms.Label();
            this.panelAuth = new System.Windows.Forms.Panel();
            this.inputKey = new System.Windows.Forms.TextBox();
            this.inputPass = new System.Windows.Forms.TextBox();
            this.InputLogin = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnNext = new System.Windows.Forms.Button();
            this.btnBack = new System.Windows.Forms.Button();
            this.btnValid = new System.Windows.Forms.Button();
            this.loader = new System.Windows.Forms.PictureBox();
            this.panelConfirm = new System.Windows.Forms.Panel();
            this.label7 = new System.Windows.Forms.Label();
            this.labAmount = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.workerAuth = new System.ComponentModel.BackgroundWorker();
            this.workerConfirm = new System.ComponentModel.BackgroundWorker();
            this.timerStop = new System.Windows.Forms.Timer(this.components);
            this.panel1.SuspendLayout();
            this.groupBox13.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panelError.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.panelAuth.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.loader)).BeginInit();
            this.panelConfirm.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.groupBox13);
            this.panel1.Location = new System.Drawing.Point(1, 2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(808, 128);
            this.panel1.TabIndex = 0;
            // 
            // groupBox13
            // 
            this.groupBox13.Controls.Add(this.label16);
            this.groupBox13.Font = new System.Drawing.Font("Microsoft Sans Serif", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox13.Location = new System.Drawing.Point(0, 3);
            this.groupBox13.Name = "groupBox13";
            this.groupBox13.Size = new System.Drawing.Size(805, 122);
            this.groupBox13.TabIndex = 52;
            this.groupBox13.TabStop = false;
            this.groupBox13.Text = "                Arrete Caisse";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.ForeColor = System.Drawing.Color.DarkBlue;
            this.label16.Location = new System.Drawing.Point(227, 62);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(311, 31);
            this.label16.TabIndex = 0;
            this.label16.Text = "PLateforme Arrêt Caisse";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.panelError);
            this.panel2.Location = new System.Drawing.Point(1, 136);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(808, 100);
            this.panel2.TabIndex = 1;
            // 
            // panelError
            // 
            this.panelError.Controls.Add(this.pictureBox1);
            this.panelError.Controls.Add(this.labError);
            this.panelError.ForeColor = System.Drawing.Color.Red;
            this.panelError.Location = new System.Drawing.Point(11, 3);
            this.panelError.Name = "panelError";
            this.panelError.Size = new System.Drawing.Size(743, 95);
            this.panelError.TabIndex = 13;
            this.panelError.TabStop = false;
            this.panelError.Text = "Alerte";
            this.panelError.Visible = false;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(43, 15);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(74, 67);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // labError
            // 
            this.labError.AutoSize = true;
            this.labError.Font = new System.Drawing.Font("Microsoft Sans Serif", 26.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labError.Location = new System.Drawing.Point(215, 28);
            this.labError.Name = "labError";
            this.labError.Size = new System.Drawing.Size(428, 39);
            this.labError.TabIndex = 0;
            this.labError.Text = "Numéro téléphone invalide";
            // 
            // panelAuth
            // 
            this.panelAuth.Controls.Add(this.inputKey);
            this.panelAuth.Controls.Add(this.inputPass);
            this.panelAuth.Controls.Add(this.InputLogin);
            this.panelAuth.Controls.Add(this.label4);
            this.panelAuth.Controls.Add(this.label3);
            this.panelAuth.Controls.Add(this.label2);
            this.panelAuth.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.panelAuth.Location = new System.Drawing.Point(12, 242);
            this.panelAuth.Name = "panelAuth";
            this.panelAuth.Size = new System.Drawing.Size(787, 272);
            this.panelAuth.TabIndex = 2;
            // 
            // inputKey
            // 
            this.inputKey.Location = new System.Drawing.Point(264, 185);
            this.inputKey.Name = "inputKey";
            this.inputKey.PasswordChar = '*';
            this.inputKey.Size = new System.Drawing.Size(380, 38);
            this.inputKey.TabIndex = 5;
            // 
            // inputPass
            // 
            this.inputPass.Location = new System.Drawing.Point(264, 118);
            this.inputPass.Name = "inputPass";
            this.inputPass.PasswordChar = '*';
            this.inputPass.Size = new System.Drawing.Size(380, 38);
            this.inputPass.TabIndex = 4;
            // 
            // InputLogin
            // 
            this.InputLogin.DropDownStyle = System.Windows.Forms.ComboBoxStyle.Simple;
            this.InputLogin.FormattingEnabled = true;
            this.InputLogin.Location = new System.Drawing.Point(264, 52);
            this.InputLogin.Name = "InputLogin";
            this.InputLogin.Size = new System.Drawing.Size(380, 51);
            this.InputLogin.TabIndex = 3;
            this.InputLogin.SelectedIndexChanged += new System.EventHandler(this.InputLogin_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(46, 192);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(151, 31);
            this.label4.TabIndex = 2;
            this.label4.Text = "Cle Arrete :";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(46, 121);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(142, 31);
            this.label3.TabIndex = 1;
            this.label3.Text = "Password:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(46, 70);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(88, 31);
            this.label2.TabIndex = 0;
            this.label2.Text = "Login:";
            // 
            // btnCancel
            // 
            this.btnCancel.AutoEllipsis = true;
            this.btnCancel.BackColor = System.Drawing.Color.White;
            this.btnCancel.FlatAppearance.BorderSize = 0;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCancel.Font = new System.Drawing.Font("Cambria", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancel.Image = ((System.Drawing.Image)(resources.GetObject("btnCancel.Image")));
            this.btnCancel.Location = new System.Drawing.Point(82, 527);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnCancel.Size = new System.Drawing.Size(152, 185);
            this.btnCancel.TabIndex = 60;
            this.btnCancel.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnCancel.UseVisualStyleBackColor = false;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnNext
            // 
            this.btnNext.AutoEllipsis = true;
            this.btnNext.BackColor = System.Drawing.Color.White;
            this.btnNext.FlatAppearance.BorderSize = 0;
            this.btnNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNext.Font = new System.Drawing.Font("Cambria", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnNext.Image = ((System.Drawing.Image)(resources.GetObject("btnNext.Image")));
            this.btnNext.Location = new System.Drawing.Point(601, 524);
            this.btnNext.Name = "btnNext";
            this.btnNext.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnNext.Size = new System.Drawing.Size(154, 184);
            this.btnNext.TabIndex = 59;
            this.btnNext.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnNext.UseVisualStyleBackColor = false;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // btnBack
            // 
            this.btnBack.AutoEllipsis = true;
            this.btnBack.BackColor = System.Drawing.Color.White;
            this.btnBack.FlatAppearance.BorderSize = 0;
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.Font = new System.Drawing.Font("Cambria", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBack.Image = ((System.Drawing.Image)(resources.GetObject("btnBack.Image")));
            this.btnBack.Location = new System.Drawing.Point(80, 532);
            this.btnBack.Name = "btnBack";
            this.btnBack.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnBack.Size = new System.Drawing.Size(154, 180);
            this.btnBack.TabIndex = 61;
            this.btnBack.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnBack.UseVisualStyleBackColor = false;
            this.btnBack.Visible = false;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // btnValid
            // 
            this.btnValid.AutoEllipsis = true;
            this.btnValid.BackColor = System.Drawing.Color.White;
            this.btnValid.FlatAppearance.BorderSize = 0;
            this.btnValid.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnValid.Font = new System.Drawing.Font("Cambria", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnValid.Image = ((System.Drawing.Image)(resources.GetObject("btnValid.Image")));
            this.btnValid.Location = new System.Drawing.Point(601, 524);
            this.btnValid.Name = "btnValid";
            this.btnValid.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnValid.Size = new System.Drawing.Size(154, 176);
            this.btnValid.TabIndex = 62;
            this.btnValid.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnValid.UseVisualStyleBackColor = false;
            this.btnValid.Visible = false;
            this.btnValid.Click += new System.EventHandler(this.btnValid_Click);
            // 
            // loader
            // 
            this.loader.Image = ((System.Drawing.Image)(resources.GetObject("loader.Image")));
            this.loader.Location = new System.Drawing.Point(350, 545);
            this.loader.Name = "loader";
            this.loader.Size = new System.Drawing.Size(100, 103);
            this.loader.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.loader.TabIndex = 63;
            this.loader.TabStop = false;
            this.loader.Visible = false;
            // 
            // panelConfirm
            // 
            this.panelConfirm.Controls.Add(this.label7);
            this.panelConfirm.Controls.Add(this.labAmount);
            this.panelConfirm.Controls.Add(this.label5);
            this.panelConfirm.Location = new System.Drawing.Point(9, 242);
            this.panelConfirm.Name = "panelConfirm";
            this.panelConfirm.Size = new System.Drawing.Size(787, 276);
            this.panelConfirm.TabIndex = 6;
            this.panelConfirm.Visible = false;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(670, 125);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(86, 31);
            this.label7.TabIndex = 2;
            this.label7.Text = "FCFA";
            // 
            // labAmount
            // 
            this.labAmount.AutoSize = true;
            this.labAmount.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labAmount.ForeColor = System.Drawing.Color.CornflowerBlue;
            this.labAmount.Location = new System.Drawing.Point(410, 121);
            this.labAmount.Name = "labAmount";
            this.labAmount.Size = new System.Drawing.Size(29, 31);
            this.labAmount.TabIndex = 1;
            this.labAmount.Text = "0";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(6, 121);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(326, 31);
            this.label5.TabIndex = 0;
            this.label5.Text = "Montant Total En Caisse :";
            // 
            // workerAuth
            // 
            this.workerAuth.DoWork += new System.ComponentModel.DoWorkEventHandler(this.workerAuth_DoWork);
            this.workerAuth.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.workerAuth_RunWorkerCompleted);
            // 
            // workerConfirm
            // 
            this.workerConfirm.DoWork += new System.ComponentModel.DoWorkEventHandler(this.workerConfirm_DoWork);
            this.workerConfirm.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.workerConfirm_RunWorkerCompleted);
            // 
            // timerStop
            // 
            this.timerStop.Interval = 10000;
            this.timerStop.Tick += new System.EventHandler(this.timerStop_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(788, 1291);
            this.Controls.Add(this.panelConfirm);
            this.Controls.Add(this.loader);
            this.Controls.Add(this.btnValid);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnNext);
            this.Controls.Add(this.panelAuth);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Form1";
            this.panel1.ResumeLayout(false);
            this.groupBox13.ResumeLayout(false);
            this.groupBox13.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panelError.ResumeLayout(false);
            this.panelError.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.panelAuth.ResumeLayout(false);
            this.panelAuth.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.loader)).EndInit();
            this.panelConfirm.ResumeLayout(false);
            this.panelConfirm.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.GroupBox panelError;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label labError;
        private System.Windows.Forms.Panel panelAuth;
        private System.Windows.Forms.TextBox inputKey;
        private System.Windows.Forms.TextBox inputPass;
        private System.Windows.Forms.ComboBox InputLogin;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Button btnValid;
        private System.Windows.Forms.PictureBox loader;
        private System.Windows.Forms.Panel panelConfirm;
        private System.Windows.Forms.Label label5;
        private System.ComponentModel.BackgroundWorker workerAuth;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label labAmount;
        private System.ComponentModel.BackgroundWorker workerConfirm;
        private System.Windows.Forms.GroupBox groupBox13;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Timer timerStop;
    }
}

