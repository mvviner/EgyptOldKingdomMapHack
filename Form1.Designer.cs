namespace EgyptOldKingdomMapHack {
    partial class Form1 {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent() {
            this.TurnLabel = new System.Windows.Forms.Label();
            this.ShowEgyptMapCheckBox = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // TurnLabel
            // 
            this.TurnLabel.AutoSize = true;
            this.TurnLabel.Location = new System.Drawing.Point(12, 9);
            this.TurnLabel.Name = "TurnLabel";
            this.TurnLabel.Size = new System.Drawing.Size(41, 13);
            this.TurnLabel.TabIndex = 7;
            this.TurnLabel.Text = "Turn: 0";
            // 
            // ShowEgyptMapCheckBox
            // 
            this.ShowEgyptMapCheckBox.AutoSize = true;
            this.ShowEgyptMapCheckBox.Location = new System.Drawing.Point(15, 26);
            this.ShowEgyptMapCheckBox.Name = "ShowEgyptMapCheckBox";
            this.ShowEgyptMapCheckBox.Size = new System.Drawing.Size(106, 17);
            this.ShowEgyptMapCheckBox.TabIndex = 8;
            this.ShowEgyptMapCheckBox.Text = "Show Egypt map";
            this.ShowEgyptMapCheckBox.UseVisualStyleBackColor = true;
            this.ShowEgyptMapCheckBox.CheckedChanged += new System.EventHandler(this.ShowEgyptMapCheckBox_CheckedChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.ShowEgyptMapCheckBox);
            this.Controls.Add(this.TurnLabel);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label TurnLabel;
        private System.Windows.Forms.CheckBox ShowEgyptMapCheckBox;
    }
}

