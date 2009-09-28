namespace MusicPlugin.Views
{
    partial class TextBoxCustom
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.CustomButton = new System.Windows.Forms.Button();
            this.CustomTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // CustomButton
            // 
            this.CustomButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.CustomButton.Location = new System.Drawing.Point(206, 4);
            this.CustomButton.Name = "CustomButton";
            this.CustomButton.Size = new System.Drawing.Size(25, 20);
            this.CustomButton.TabIndex = 0;
            this.CustomButton.Text = "...";
            this.CustomButton.UseVisualStyleBackColor = true;
            this.CustomButton.Click += new System.EventHandler(this.CustomButton_Click);
            // 
            // CustomTextBox
            // 
            this.CustomTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.CustomTextBox.Location = new System.Drawing.Point(0, 4);
            this.CustomTextBox.Name = "CustomTextBox";
            this.CustomTextBox.Size = new System.Drawing.Size(200, 20);
            this.CustomTextBox.TabIndex = 1;
            // 
            // TextBoxCustom
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.CustomTextBox);
            this.Controls.Add(this.CustomButton);
            this.Name = "TextBoxCustom";
            this.Size = new System.Drawing.Size(236, 29);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button CustomButton;
        public System.Windows.Forms.TextBox CustomTextBox;
    }
}
