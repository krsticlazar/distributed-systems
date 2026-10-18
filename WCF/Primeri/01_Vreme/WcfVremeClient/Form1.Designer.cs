namespace WcfVremeClient
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
            this.btnVreme = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.btnVremeA = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnVreme
            // 
            this.btnVreme.Location = new System.Drawing.Point(12, 12);
            this.btnVreme.Name = "btnVreme";
            this.btnVreme.Size = new System.Drawing.Size(126, 23);
            this.btnVreme.TabIndex = 0;
            this.btnVreme.Text = "GetVreme";
            this.btnVreme.UseVisualStyleBackColor = true;
            this.btnVreme.Click += new System.EventHandler(this.btnVreme_Click);
            // 
            // textBox1
            // 
            this.textBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox1.Location = new System.Drawing.Point(12, 41);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(260, 208);
            this.textBox1.TabIndex = 1;
            // 
            // btnVremeA
            // 
            this.btnVremeA.Location = new System.Drawing.Point(144, 12);
            this.btnVremeA.Name = "btnVremeA";
            this.btnVremeA.Size = new System.Drawing.Size(126, 23);
            this.btnVremeA.TabIndex = 0;
            this.btnVremeA.Text = "GetVreme Async";
            this.btnVremeA.UseVisualStyleBackColor = true;
            this.btnVremeA.Click += new System.EventHandler(this.btnVremeA_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.btnVremeA);
            this.Controls.Add(this.btnVreme);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnVreme;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button btnVremeA;
    }
}

