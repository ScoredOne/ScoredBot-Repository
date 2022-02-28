
namespace ScoredBot {
	partial class TwitchLoginForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.label1 = new System.Windows.Forms.Label();
			this.UsernameTextbox = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.OAuthTextbox = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.SubmitButton = new System.Windows.Forms.Button();
			this.TargetAccTextBox = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft New Tai Lue", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(13, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(76, 19);
			this.label1.TabIndex = 0;
			this.label1.Text = "Username";
			// 
			// UsernameTextbox
			// 
			this.UsernameTextbox.Location = new System.Drawing.Point(16, 33);
			this.UsernameTextbox.Name = "UsernameTextbox";
			this.UsernameTextbox.Size = new System.Drawing.Size(274, 20);
			this.UsernameTextbox.TabIndex = 1;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Font = new System.Drawing.Font("Microsoft New Tai Lue", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label2.Location = new System.Drawing.Point(13, 56);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(51, 19);
			this.label2.TabIndex = 2;
			this.label2.Text = "OAuth";
			// 
			// OAuthTextbox
			// 
			this.OAuthTextbox.Location = new System.Drawing.Point(16, 76);
			this.OAuthTextbox.MaxLength = 36;
			this.OAuthTextbox.Name = "OAuthTextbox";
			this.OAuthTextbox.Size = new System.Drawing.Size(274, 20);
			this.OAuthTextbox.TabIndex = 3;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Font = new System.Drawing.Font("Microsoft New Tai Lue", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label3.Location = new System.Drawing.Point(72, 58);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(218, 15);
			this.label3.TabIndex = 4;
			this.label3.Text = "e.g \"oauth:xxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\"";
			// 
			// SubmitButton
			// 
			this.SubmitButton.Font = new System.Drawing.Font("Microsoft New Tai Lue", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.SubmitButton.Location = new System.Drawing.Point(16, 143);
			this.SubmitButton.Name = "SubmitButton";
			this.SubmitButton.Size = new System.Drawing.Size(274, 23);
			this.SubmitButton.TabIndex = 5;
			this.SubmitButton.Text = "Submit";
			this.SubmitButton.UseVisualStyleBackColor = true;
			this.SubmitButton.Click += new System.EventHandler(this.SubmitButton_Click);
			// 
			// TargetAccTextBox
			// 
			this.TargetAccTextBox.Location = new System.Drawing.Point(16, 117);
			this.TargetAccTextBox.Name = "TargetAccTextBox";
			this.TargetAccTextBox.Size = new System.Drawing.Size(274, 20);
			this.TargetAccTextBox.TabIndex = 7;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font("Microsoft New Tai Lue", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label4.Location = new System.Drawing.Point(13, 97);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(183, 19);
			this.label4.TabIndex = 6;
			this.label4.Text = "Target Account (Optional)";
			// 
			// TwitchLoginForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.ClientSize = new System.Drawing.Size(308, 185);
			this.Controls.Add(this.TargetAccTextBox);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.SubmitButton);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.OAuthTextbox);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.UsernameTextbox);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "TwitchLoginForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Twitch Details";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox UsernameTextbox;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox OAuthTextbox;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button SubmitButton;
		private System.Windows.Forms.TextBox TargetAccTextBox;
		private System.Windows.Forms.Label label4;
	}
}