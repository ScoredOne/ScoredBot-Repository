using System;
using System.Windows.Forms;

namespace TwitchBotManager {
	public partial class TwitchLoginForm : Form {
		private readonly MainForm ParentObject;

		public TwitchLoginForm(MainForm parent) {
			InitializeComponent();
			ParentObject = parent;
		}

		private void SubmitButton_Click(object sender, EventArgs e) {
			string username = UsernameTextbox.Text.ToLower().Trim();
			string oauth = OAuthTextbox.Text.ToLower().Trim();
			string target = TargetAccTextBox.Text.ToLower().Trim();

			if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(oauth)) {
				MessageBox.Show("Username and OAuth can't be blank, please input these two values.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			if (!oauth.Contains("oauth:")) {
				MessageBox.Show("oauth: was not detected at the start of the input, will attempt to add and continue.", "string error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				oauth = "oauth:" + oauth;
			}
			if (oauth.Length != 36) {
				MessageBox.Show("OAuth is a set 36 character pattern, please input the one provided here (https://twitchapps.com/tmi/)", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// TODO : Secret in twitch login details, also rename oauth to clientid
			ParentObject.SetTwitchBotLoginDetails(username, oauth, "", target);

			Close();
		}
	}
}
