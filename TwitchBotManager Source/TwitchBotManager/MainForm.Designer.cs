
namespace TwitchBotManager {
	partial class MainForm {
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.SongRequestVideoView = new LibVLCSharp.WinForms.VideoView();
			this.AddSongToPlayTextBox = new System.Windows.Forms.TextBox();
			this.BotStartStop = new System.Windows.Forms.Button();
			this.PlayPauseButton = new System.Windows.Forms.Button();
			this.StopPlaybackButton = new System.Windows.Forms.Button();
			this.ClearListButton = new System.Windows.Forms.Button();
			this.AddLinkButton = new System.Windows.Forms.Button();
			this.LoadingBar = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openOutputFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ExitToolButton = new System.Windows.Forms.ToolStripMenuItem();
			this.botLoginToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.EnterBotDetailsToolButton = new System.Windows.Forms.ToolStripMenuItem();
			this.CurrentDetailsToolButton = new System.Windows.Forms.ToolStripMenuItem();
			this.UserNameToolLabel = new System.Windows.Forms.ToolStripMenuItem();
			this.OAuthToolLabel = new System.Windows.Forms.ToolStripMenuItem();
			this.TargetFoundLabel = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.scoredToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.httpstwittercomScoredOneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.httpswwwtwitchtvscoredoneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.httpsassetstoreunitycompublishers35238ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.IncreaseVolumeButton = new System.Windows.Forms.Button();
			this.DecreaseVolumeButton = new System.Windows.Forms.Button();
			this.VolumeLabel = new System.Windows.Forms.Label();
			this.RequestsButton = new System.Windows.Forms.Button();
			this.SongSystemButton = new System.Windows.Forms.Button();
			this.SaveLinkButton = new System.Windows.Forms.Button();
			this.SongRequestList = new System.Windows.Forms.ListBox();
			this.MainTabControl = new System.Windows.Forms.TabControl();
			this.SongRequestTab = new System.Windows.Forms.TabPage();
			this.RemoveSongFromSecondaryButton = new System.Windows.Forms.Button();
			this.SkipSongButton = new System.Windows.Forms.Button();
			this.SecondaryPlaylistManagementTab = new System.Windows.Forms.TabPage();
			this.ClaimSongButton = new System.Windows.Forms.Button();
			this.ClaimAllSongsButton = new System.Windows.Forms.Button();
			this.RetryAllBrokenSongButton = new System.Windows.Forms.Button();
			this.RetryBrokenSongButton = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.RemoveBrokenSongButton = new System.Windows.Forms.Button();
			this.SecondaryTextBoxAddField = new System.Windows.Forms.TextBox();
			this.AddSecondarySongButton = new System.Windows.Forms.Button();
			this.RemoveSecondarySongButton = new System.Windows.Forms.Button();
			this.LoadedSongListBoxLabel = new System.Windows.Forms.Label();
			this.BrokenSongsListBox = new System.Windows.Forms.ListBox();
			this.LoadedSongsListBox = new System.Windows.Forms.ListBox();
			this.DebugTab = new System.Windows.Forms.TabPage();
			this.DebugConsoleList = new System.Windows.Forms.ListBox();
			this.ConnectionLabel = new System.Windows.Forms.Label();
			this.CurrentSongRequestLabel = new System.Windows.Forms.Label();
			this.MainProgressBar = new System.Windows.Forms.ProgressBar();
			this.WriteUpdatedSongInfoToFileButton = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.SongRequestVideoView)).BeginInit();
			this.LoadingBar.SuspendLayout();
			this.MainTabControl.SuspendLayout();
			this.SongRequestTab.SuspendLayout();
			this.SecondaryPlaylistManagementTab.SuspendLayout();
			this.DebugTab.SuspendLayout();
			this.SuspendLayout();
			// 
			// SongRequestVideoView
			// 
			this.SongRequestVideoView.BackColor = System.Drawing.Color.Black;
			this.SongRequestVideoView.Location = new System.Drawing.Point(6, 6);
			this.SongRequestVideoView.MediaPlayer = null;
			this.SongRequestVideoView.Name = "SongRequestVideoView";
			this.SongRequestVideoView.Size = new System.Drawing.Size(559, 326);
			this.SongRequestVideoView.TabIndex = 0;
			this.SongRequestVideoView.Text = "VideoPlayer";
			// 
			// AddSongToPlayTextBox
			// 
			this.AddSongToPlayTextBox.Location = new System.Drawing.Point(88, 367);
			this.AddSongToPlayTextBox.Name = "AddSongToPlayTextBox";
			this.AddSongToPlayTextBox.Size = new System.Drawing.Size(477, 20);
			this.AddSongToPlayTextBox.TabIndex = 3;
			// 
			// BotStartStop
			// 
			this.BotStartStop.Location = new System.Drawing.Point(947, -1);
			this.BotStartStop.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
			this.BotStartStop.Name = "BotStartStop";
			this.BotStartStop.Size = new System.Drawing.Size(75, 25);
			this.BotStartStop.TabIndex = 4;
			this.BotStartStop.Text = "Start Bot";
			this.BotStartStop.UseVisualStyleBackColor = true;
			this.BotStartStop.Click += new System.EventHandler(this.BotStartStop_Click);
			// 
			// PlayPauseButton
			// 
			this.PlayPauseButton.Location = new System.Drawing.Point(328, 338);
			this.PlayPauseButton.Name = "PlayPauseButton";
			this.PlayPauseButton.Size = new System.Drawing.Size(75, 23);
			this.PlayPauseButton.TabIndex = 5;
			this.PlayPauseButton.Text = "Play";
			this.PlayPauseButton.UseVisualStyleBackColor = true;
			this.PlayPauseButton.Click += new System.EventHandler(this.PlayPauseButton_Click);
			// 
			// StopPlaybackButton
			// 
			this.StopPlaybackButton.Location = new System.Drawing.Point(490, 338);
			this.StopPlaybackButton.Name = "StopPlaybackButton";
			this.StopPlaybackButton.Size = new System.Drawing.Size(75, 23);
			this.StopPlaybackButton.TabIndex = 6;
			this.StopPlaybackButton.Text = "Stop";
			this.StopPlaybackButton.UseVisualStyleBackColor = true;
			this.StopPlaybackButton.Click += new System.EventHandler(this.StopPlaybackButton_Click);
			// 
			// ClearListButton
			// 
			this.ClearListButton.Location = new System.Drawing.Point(981, 367);
			this.ClearListButton.Name = "ClearListButton";
			this.ClearListButton.Size = new System.Drawing.Size(128, 23);
			this.ClearListButton.TabIndex = 7;
			this.ClearListButton.Text = "Clear SongRequests";
			this.ClearListButton.UseVisualStyleBackColor = true;
			this.ClearListButton.Click += new System.EventHandler(this.ClearListButton_Click);
			// 
			// AddLinkButton
			// 
			this.AddLinkButton.Location = new System.Drawing.Point(7, 365);
			this.AddLinkButton.Name = "AddLinkButton";
			this.AddLinkButton.Size = new System.Drawing.Size(75, 23);
			this.AddLinkButton.TabIndex = 8;
			this.AddLinkButton.Text = "Add Song";
			this.AddLinkButton.UseVisualStyleBackColor = true;
			this.AddLinkButton.Click += new System.EventHandler(this.AddLinkButton_Click);
			// 
			// LoadingBar
			// 
			this.LoadingBar.BackColor = System.Drawing.SystemColors.Control;
			this.LoadingBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.botLoginToolStripMenuItem,
            this.helpToolStripMenuItem});
			this.LoadingBar.Location = new System.Drawing.Point(0, 0);
			this.LoadingBar.Name = "LoadingBar";
			this.LoadingBar.Size = new System.Drawing.Size(1122, 24);
			this.LoadingBar.TabIndex = 9;
			this.LoadingBar.Text = "AppMenuStrip";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openOutputFolderToolStripMenuItem,
            this.ExitToolButton});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "File";
			// 
			// openOutputFolderToolStripMenuItem
			// 
			this.openOutputFolderToolStripMenuItem.Name = "openOutputFolderToolStripMenuItem";
			this.openOutputFolderToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.openOutputFolderToolStripMenuItem.Text = "Open Output Folder";
			this.openOutputFolderToolStripMenuItem.Click += new System.EventHandler(this.openOutputFolderToolStripMenuItem_Click);
			// 
			// ExitToolButton
			// 
			this.ExitToolButton.Name = "ExitToolButton";
			this.ExitToolButton.Size = new System.Drawing.Size(180, 22);
			this.ExitToolButton.Text = "Exit";
			this.ExitToolButton.Click += new System.EventHandler(this.ExitToolButton_Click);
			// 
			// botLoginToolStripMenuItem
			// 
			this.botLoginToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.EnterBotDetailsToolButton,
            this.CurrentDetailsToolButton});
			this.botLoginToolStripMenuItem.Name = "botLoginToolStripMenuItem";
			this.botLoginToolStripMenuItem.Size = new System.Drawing.Size(70, 20);
			this.botLoginToolStripMenuItem.Text = "Bot Login";
			// 
			// EnterBotDetailsToolButton
			// 
			this.EnterBotDetailsToolButton.Name = "EnterBotDetailsToolButton";
			this.EnterBotDetailsToolButton.Size = new System.Drawing.Size(193, 22);
			this.EnterBotDetailsToolButton.Text = "Enter Bot Login Details";
			this.EnterBotDetailsToolButton.Click += new System.EventHandler(this.EnterBotDetailsToolButton_Click);
			// 
			// CurrentDetailsToolButton
			// 
			this.CurrentDetailsToolButton.CheckOnClick = true;
			this.CurrentDetailsToolButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.UserNameToolLabel,
            this.OAuthToolLabel,
            this.TargetFoundLabel});
			this.CurrentDetailsToolButton.Name = "CurrentDetailsToolButton";
			this.CurrentDetailsToolButton.Size = new System.Drawing.Size(193, 22);
			this.CurrentDetailsToolButton.Text = "Current Details";
			this.CurrentDetailsToolButton.CheckedChanged += new System.EventHandler(this.CurrentDetailsToolButton_CheckedChanged);
			// 
			// UserNameToolLabel
			// 
			this.UserNameToolLabel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.UserNameToolLabel.Enabled = false;
			this.UserNameToolLabel.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Italic);
			this.UserNameToolLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
			this.UserNameToolLabel.Name = "UserNameToolLabel";
			this.UserNameToolLabel.Size = new System.Drawing.Size(255, 30);
			this.UserNameToolLabel.Text = "No User Name Found";
			// 
			// OAuthToolLabel
			// 
			this.OAuthToolLabel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.OAuthToolLabel.Enabled = false;
			this.OAuthToolLabel.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Italic);
			this.OAuthToolLabel.ForeColor = System.Drawing.SystemColors.HotTrack;
			this.OAuthToolLabel.Name = "OAuthToolLabel";
			this.OAuthToolLabel.Size = new System.Drawing.Size(255, 30);
			this.OAuthToolLabel.Text = "No OAuth Found";
			// 
			// TargetFoundLabel
			// 
			this.TargetFoundLabel.Enabled = false;
			this.TargetFoundLabel.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Italic);
			this.TargetFoundLabel.Name = "TargetFoundLabel";
			this.TargetFoundLabel.Size = new System.Drawing.Size(255, 30);
			this.TargetFoundLabel.Text = "No Target Found";
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.helpToolStripMenuItem.Text = "Help";
			// 
			// aboutToolStripMenuItem
			// 
			this.aboutToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.scoredToolStripMenuItem});
			this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
			this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
			this.aboutToolStripMenuItem.Text = "About";
			// 
			// scoredToolStripMenuItem
			// 
			this.scoredToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.httpstwittercomScoredOneToolStripMenuItem,
            this.httpswwwtwitchtvscoredoneToolStripMenuItem,
            this.httpsassetstoreunitycompublishers35238ToolStripMenuItem});
			this.scoredToolStripMenuItem.Name = "scoredToolStripMenuItem";
			this.scoredToolStripMenuItem.Size = new System.Drawing.Size(247, 22);
			this.scoredToolStripMenuItem.Text = "ScoredBot created by ScoredOne";
			// 
			// httpstwittercomScoredOneToolStripMenuItem
			// 
			this.httpstwittercomScoredOneToolStripMenuItem.Name = "httpstwittercomScoredOneToolStripMenuItem";
			this.httpstwittercomScoredOneToolStripMenuItem.Size = new System.Drawing.Size(317, 22);
			this.httpstwittercomScoredOneToolStripMenuItem.Text = "https://twitter.com/ScoredOne";
			this.httpstwittercomScoredOneToolStripMenuItem.Click += new System.EventHandler(this.MenuTabOpenURLEvent);
			// 
			// httpswwwtwitchtvscoredoneToolStripMenuItem
			// 
			this.httpswwwtwitchtvscoredoneToolStripMenuItem.Name = "httpswwwtwitchtvscoredoneToolStripMenuItem";
			this.httpswwwtwitchtvscoredoneToolStripMenuItem.Size = new System.Drawing.Size(317, 22);
			this.httpswwwtwitchtvscoredoneToolStripMenuItem.Text = "https://www.twitch.tv/scoredone";
			this.httpswwwtwitchtvscoredoneToolStripMenuItem.Click += new System.EventHandler(this.MenuTabOpenURLEvent);
			// 
			// httpsassetstoreunitycompublishers35238ToolStripMenuItem
			// 
			this.httpsassetstoreunitycompublishers35238ToolStripMenuItem.Name = "httpsassetstoreunitycompublishers35238ToolStripMenuItem";
			this.httpsassetstoreunitycompublishers35238ToolStripMenuItem.Size = new System.Drawing.Size(317, 22);
			this.httpsassetstoreunitycompublishers35238ToolStripMenuItem.Text = "https://assetstore.unity.com/publishers/35238";
			this.httpsassetstoreunitycompublishers35238ToolStripMenuItem.Click += new System.EventHandler(this.MenuTabOpenURLEvent);
			// 
			// IncreaseVolumeButton
			// 
			this.IncreaseVolumeButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.IncreaseVolumeButton.Location = new System.Drawing.Point(297, 337);
			this.IncreaseVolumeButton.Name = "IncreaseVolumeButton";
			this.IncreaseVolumeButton.Size = new System.Drawing.Size(25, 25);
			this.IncreaseVolumeButton.TabIndex = 11;
			this.IncreaseVolumeButton.Text = "+";
			this.IncreaseVolumeButton.UseVisualStyleBackColor = true;
			this.IncreaseVolumeButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.IncreaseVolumeButton_MouseDown);
			this.IncreaseVolumeButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.IncreaseVolumeButton_MouseUp);
			// 
			// DecreaseVolumeButton
			// 
			this.DecreaseVolumeButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.DecreaseVolumeButton.Location = new System.Drawing.Point(266, 337);
			this.DecreaseVolumeButton.Name = "DecreaseVolumeButton";
			this.DecreaseVolumeButton.Size = new System.Drawing.Size(25, 25);
			this.DecreaseVolumeButton.TabIndex = 12;
			this.DecreaseVolumeButton.Text = "-";
			this.DecreaseVolumeButton.UseVisualStyleBackColor = true;
			this.DecreaseVolumeButton.MouseDown += new System.Windows.Forms.MouseEventHandler(this.DecreaseVolumeButton_MouseDown);
			this.DecreaseVolumeButton.MouseUp += new System.Windows.Forms.MouseEventHandler(this.DecreaseVolumeButton_MouseUp);
			// 
			// VolumeLabel
			// 
			this.VolumeLabel.AutoSize = true;
			this.VolumeLabel.Location = new System.Drawing.Point(194, 343);
			this.VolumeLabel.Name = "VolumeLabel";
			this.VolumeLabel.Size = new System.Drawing.Size(66, 13);
			this.VolumeLabel.TabIndex = 13;
			this.VolumeLabel.Text = "Volume: 100";
			// 
			// RequestsButton
			// 
			this.RequestsButton.Location = new System.Drawing.Point(447, 393);
			this.RequestsButton.Name = "RequestsButton";
			this.RequestsButton.Size = new System.Drawing.Size(118, 23);
			this.RequestsButton.TabIndex = 14;
			this.RequestsButton.Text = "Requests OFF";
			this.RequestsButton.UseVisualStyleBackColor = true;
			this.RequestsButton.Click += new System.EventHandler(this.RequestsButton_Click);
			// 
			// SongSystemButton
			// 
			this.SongSystemButton.Location = new System.Drawing.Point(7, 339);
			this.SongSystemButton.Name = "SongSystemButton";
			this.SongSystemButton.Size = new System.Drawing.Size(118, 23);
			this.SongSystemButton.TabIndex = 15;
			this.SongSystemButton.Text = "SongsSystem OFF";
			this.SongSystemButton.UseVisualStyleBackColor = true;
			this.SongSystemButton.Click += new System.EventHandler(this.SongSystemButton_Click);
			// 
			// SaveLinkButton
			// 
			this.SaveLinkButton.Enabled = false;
			this.SaveLinkButton.Location = new System.Drawing.Point(7, 393);
			this.SaveLinkButton.Name = "SaveLinkButton";
			this.SaveLinkButton.Size = new System.Drawing.Size(154, 23);
			this.SaveLinkButton.TabIndex = 16;
			this.SaveLinkButton.Text = "Save Link To Secondary";
			this.SaveLinkButton.UseVisualStyleBackColor = true;
			this.SaveLinkButton.Click += new System.EventHandler(this.SaveLinkButton_Click);
			// 
			// SongRequestList
			// 
			this.SongRequestList.FormattingEnabled = true;
			this.SongRequestList.Location = new System.Drawing.Point(571, 6);
			this.SongRequestList.Name = "SongRequestList";
			this.SongRequestList.SelectionMode = System.Windows.Forms.SelectionMode.None;
			this.SongRequestList.Size = new System.Drawing.Size(538, 355);
			this.SongRequestList.TabIndex = 17;
			// 
			// MainTabControl
			// 
			this.MainTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MainTabControl.Controls.Add(this.SongRequestTab);
			this.MainTabControl.Controls.Add(this.SecondaryPlaylistManagementTab);
			this.MainTabControl.Controls.Add(this.DebugTab);
			this.MainTabControl.Location = new System.Drawing.Point(0, 27);
			this.MainTabControl.Multiline = true;
			this.MainTabControl.Name = "MainTabControl";
			this.MainTabControl.SelectedIndex = 0;
			this.MainTabControl.Size = new System.Drawing.Size(1122, 455);
			this.MainTabControl.TabIndex = 18;
			this.MainTabControl.SelectedIndexChanged += new System.EventHandler(this.MainTabControl_TabIndexChanged);
			// 
			// SongRequestTab
			// 
			this.SongRequestTab.Controls.Add(this.RemoveSongFromSecondaryButton);
			this.SongRequestTab.Controls.Add(this.SkipSongButton);
			this.SongRequestTab.Controls.Add(this.SongRequestList);
			this.SongRequestTab.Controls.Add(this.SongRequestVideoView);
			this.SongRequestTab.Controls.Add(this.SaveLinkButton);
			this.SongRequestTab.Controls.Add(this.AddSongToPlayTextBox);
			this.SongRequestTab.Controls.Add(this.SongSystemButton);
			this.SongRequestTab.Controls.Add(this.RequestsButton);
			this.SongRequestTab.Controls.Add(this.PlayPauseButton);
			this.SongRequestTab.Controls.Add(this.VolumeLabel);
			this.SongRequestTab.Controls.Add(this.StopPlaybackButton);
			this.SongRequestTab.Controls.Add(this.DecreaseVolumeButton);
			this.SongRequestTab.Controls.Add(this.ClearListButton);
			this.SongRequestTab.Controls.Add(this.IncreaseVolumeButton);
			this.SongRequestTab.Controls.Add(this.AddLinkButton);
			this.SongRequestTab.Location = new System.Drawing.Point(4, 22);
			this.SongRequestTab.Name = "SongRequestTab";
			this.SongRequestTab.Padding = new System.Windows.Forms.Padding(3);
			this.SongRequestTab.Size = new System.Drawing.Size(1114, 429);
			this.SongRequestTab.TabIndex = 0;
			this.SongRequestTab.Text = "Song Request Tab";
			this.SongRequestTab.UseVisualStyleBackColor = true;
			// 
			// RemoveSongFromSecondaryButton
			// 
			this.RemoveSongFromSecondaryButton.Enabled = false;
			this.RemoveSongFromSecondaryButton.Location = new System.Drawing.Point(167, 393);
			this.RemoveSongFromSecondaryButton.Name = "RemoveSongFromSecondaryButton";
			this.RemoveSongFromSecondaryButton.Size = new System.Drawing.Size(175, 23);
			this.RemoveSongFromSecondaryButton.TabIndex = 19;
			this.RemoveSongFromSecondaryButton.Text = "Remove Link From Secondary";
			this.RemoveSongFromSecondaryButton.UseVisualStyleBackColor = true;
			this.RemoveSongFromSecondaryButton.Click += new System.EventHandler(this.RemoveSongFromSecondaryButton_Click);
			// 
			// SkipSongButton
			// 
			this.SkipSongButton.Location = new System.Drawing.Point(409, 338);
			this.SkipSongButton.Name = "SkipSongButton";
			this.SkipSongButton.Size = new System.Drawing.Size(75, 23);
			this.SkipSongButton.TabIndex = 18;
			this.SkipSongButton.Text = "Skip";
			this.SkipSongButton.UseVisualStyleBackColor = true;
			this.SkipSongButton.Click += new System.EventHandler(this.SkipSongButton_Click);
			// 
			// SecondaryPlaylistManagementTab
			// 
			this.SecondaryPlaylistManagementTab.Controls.Add(this.WriteUpdatedSongInfoToFileButton);
			this.SecondaryPlaylistManagementTab.Controls.Add(this.ClaimSongButton);
			this.SecondaryPlaylistManagementTab.Controls.Add(this.ClaimAllSongsButton);
			this.SecondaryPlaylistManagementTab.Controls.Add(this.RetryAllBrokenSongButton);
			this.SecondaryPlaylistManagementTab.Controls.Add(this.RetryBrokenSongButton);
			this.SecondaryPlaylistManagementTab.Controls.Add(this.label1);
			this.SecondaryPlaylistManagementTab.Controls.Add(this.RemoveBrokenSongButton);
			this.SecondaryPlaylistManagementTab.Controls.Add(this.SecondaryTextBoxAddField);
			this.SecondaryPlaylistManagementTab.Controls.Add(this.AddSecondarySongButton);
			this.SecondaryPlaylistManagementTab.Controls.Add(this.RemoveSecondarySongButton);
			this.SecondaryPlaylistManagementTab.Controls.Add(this.LoadedSongListBoxLabel);
			this.SecondaryPlaylistManagementTab.Controls.Add(this.BrokenSongsListBox);
			this.SecondaryPlaylistManagementTab.Controls.Add(this.LoadedSongsListBox);
			this.SecondaryPlaylistManagementTab.Location = new System.Drawing.Point(4, 22);
			this.SecondaryPlaylistManagementTab.Name = "SecondaryPlaylistManagementTab";
			this.SecondaryPlaylistManagementTab.Size = new System.Drawing.Size(1114, 429);
			this.SecondaryPlaylistManagementTab.TabIndex = 3;
			this.SecondaryPlaylistManagementTab.Text = "Sec\' Playlist Management";
			this.SecondaryPlaylistManagementTab.UseVisualStyleBackColor = true;
			// 
			// ClaimSongButton
			// 
			this.ClaimSongButton.Location = new System.Drawing.Point(955, 385);
			this.ClaimSongButton.Name = "ClaimSongButton";
			this.ClaimSongButton.Size = new System.Drawing.Size(75, 23);
			this.ClaimSongButton.TabIndex = 11;
			this.ClaimSongButton.Text = "Claim Song";
			this.ClaimSongButton.UseVisualStyleBackColor = true;
			this.ClaimSongButton.Click += new System.EventHandler(this.ClaimSongButton_Click);
			// 
			// ClaimAllSongsButton
			// 
			this.ClaimAllSongsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ClaimAllSongsButton.Location = new System.Drawing.Point(1036, 385);
			this.ClaimAllSongsButton.Name = "ClaimAllSongsButton";
			this.ClaimAllSongsButton.Size = new System.Drawing.Size(75, 23);
			this.ClaimAllSongsButton.TabIndex = 10;
			this.ClaimAllSongsButton.Text = "Claim All";
			this.ClaimAllSongsButton.UseVisualStyleBackColor = true;
			this.ClaimAllSongsButton.Click += new System.EventHandler(this.ClaimAllSongsButton_Click);
			// 
			// RetryAllBrokenSongButton
			// 
			this.RetryAllBrokenSongButton.Location = new System.Drawing.Point(178, 385);
			this.RetryAllBrokenSongButton.Name = "RetryAllBrokenSongButton";
			this.RetryAllBrokenSongButton.Size = new System.Drawing.Size(83, 23);
			this.RetryAllBrokenSongButton.TabIndex = 9;
			this.RetryAllBrokenSongButton.Text = "Retry All";
			this.RetryAllBrokenSongButton.UseVisualStyleBackColor = true;
			this.RetryAllBrokenSongButton.Click += new System.EventHandler(this.RetryAllBrokenSongButton_Click);
			// 
			// RetryBrokenSongButton
			// 
			this.RetryBrokenSongButton.Enabled = false;
			this.RetryBrokenSongButton.Location = new System.Drawing.Point(89, 385);
			this.RetryBrokenSongButton.Name = "RetryBrokenSongButton";
			this.RetryBrokenSongButton.Size = new System.Drawing.Size(83, 23);
			this.RetryBrokenSongButton.TabIndex = 8;
			this.RetryBrokenSongButton.Text = "Retry Song";
			this.RetryBrokenSongButton.UseVisualStyleBackColor = true;
			this.RetryBrokenSongButton.Click += new System.EventHandler(this.RetryBrokenSongButton_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(0, 251);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(168, 17);
			this.label1.TabIndex = 7;
			this.label1.Text = "Loaded Broken Songs";
			// 
			// RemoveBrokenSongButton
			// 
			this.RemoveBrokenSongButton.Enabled = false;
			this.RemoveBrokenSongButton.Location = new System.Drawing.Point(0, 385);
			this.RemoveBrokenSongButton.Name = "RemoveBrokenSongButton";
			this.RemoveBrokenSongButton.Size = new System.Drawing.Size(83, 23);
			this.RemoveBrokenSongButton.TabIndex = 6;
			this.RemoveBrokenSongButton.Text = "Remove Song";
			this.RemoveBrokenSongButton.UseVisualStyleBackColor = true;
			this.RemoveBrokenSongButton.Click += new System.EventHandler(this.RemoveBrokenSongButton_Click);
			// 
			// SecondaryTextBoxAddField
			// 
			this.SecondaryTextBoxAddField.Location = new System.Drawing.Point(182, 227);
			this.SecondaryTextBoxAddField.Name = "SecondaryTextBoxAddField";
			this.SecondaryTextBoxAddField.Size = new System.Drawing.Size(929, 20);
			this.SecondaryTextBoxAddField.TabIndex = 5;
			this.SecondaryTextBoxAddField.TextChanged += new System.EventHandler(this.SecondaryTextBoxAddField_TextChanged);
			// 
			// AddSecondarySongButton
			// 
			this.AddSecondarySongButton.Enabled = false;
			this.AddSecondarySongButton.Location = new System.Drawing.Point(89, 225);
			this.AddSecondarySongButton.Name = "AddSecondarySongButton";
			this.AddSecondarySongButton.Size = new System.Drawing.Size(83, 23);
			this.AddSecondarySongButton.TabIndex = 4;
			this.AddSecondarySongButton.Text = "Add Song";
			this.AddSecondarySongButton.UseVisualStyleBackColor = true;
			this.AddSecondarySongButton.Click += new System.EventHandler(this.AddSecondarySongButton_Click);
			// 
			// RemoveSecondarySongButton
			// 
			this.RemoveSecondarySongButton.Enabled = false;
			this.RemoveSecondarySongButton.Location = new System.Drawing.Point(0, 225);
			this.RemoveSecondarySongButton.Name = "RemoveSecondarySongButton";
			this.RemoveSecondarySongButton.Size = new System.Drawing.Size(83, 23);
			this.RemoveSecondarySongButton.TabIndex = 3;
			this.RemoveSecondarySongButton.Text = "Remove Song";
			this.RemoveSecondarySongButton.UseVisualStyleBackColor = true;
			this.RemoveSecondarySongButton.Click += new System.EventHandler(this.RemoveSecondarySongButton_Click);
			// 
			// LoadedSongListBoxLabel
			// 
			this.LoadedSongListBoxLabel.AutoSize = true;
			this.LoadedSongListBoxLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.LoadedSongListBoxLabel.Location = new System.Drawing.Point(0, 0);
			this.LoadedSongListBoxLabel.Name = "LoadedSongListBoxLabel";
			this.LoadedSongListBoxLabel.Size = new System.Drawing.Size(194, 17);
			this.LoadedSongListBoxLabel.TabIndex = 2;
			this.LoadedSongListBoxLabel.Text = "Loaded Secondary Songs";
			// 
			// BrokenSongsListBox
			// 
			this.BrokenSongsListBox.FormattingEnabled = true;
			this.BrokenSongsListBox.Location = new System.Drawing.Point(0, 271);
			this.BrokenSongsListBox.Name = "BrokenSongsListBox";
			this.BrokenSongsListBox.Size = new System.Drawing.Size(1111, 108);
			this.BrokenSongsListBox.TabIndex = 1;
			this.BrokenSongsListBox.SelectedIndexChanged += new System.EventHandler(this.BrokenSongsListBox_SelectedIndexChanged);
			// 
			// LoadedSongsListBox
			// 
			this.LoadedSongsListBox.FormattingEnabled = true;
			this.LoadedSongsListBox.Location = new System.Drawing.Point(0, 20);
			this.LoadedSongsListBox.Name = "LoadedSongsListBox";
			this.LoadedSongsListBox.Size = new System.Drawing.Size(1111, 199);
			this.LoadedSongsListBox.TabIndex = 0;
			this.LoadedSongsListBox.SelectedIndexChanged += new System.EventHandler(this.LoadedSongsListBox_SelectedIndexChanged);
			// 
			// DebugTab
			// 
			this.DebugTab.Controls.Add(this.DebugConsoleList);
			this.DebugTab.Location = new System.Drawing.Point(4, 22);
			this.DebugTab.Name = "DebugTab";
			this.DebugTab.Size = new System.Drawing.Size(1114, 429);
			this.DebugTab.TabIndex = 2;
			this.DebugTab.Text = "Debug Console";
			this.DebugTab.UseVisualStyleBackColor = true;
			// 
			// DebugConsoleList
			// 
			this.DebugConsoleList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.DebugConsoleList.FormattingEnabled = true;
			this.DebugConsoleList.HorizontalScrollbar = true;
			this.DebugConsoleList.Location = new System.Drawing.Point(0, 0);
			this.DebugConsoleList.Name = "DebugConsoleList";
			this.DebugConsoleList.ScrollAlwaysVisible = true;
			this.DebugConsoleList.Size = new System.Drawing.Size(1114, 433);
			this.DebugConsoleList.TabIndex = 0;
			// 
			// ConnectionLabel
			// 
			this.ConnectionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ConnectionLabel.BackColor = System.Drawing.SystemColors.Control;
			this.ConnectionLabel.Font = new System.Drawing.Font("Consolas", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ConnectionLabel.Location = new System.Drawing.Point(814, 487);
			this.ConnectionLabel.Name = "ConnectionLabel";
			this.ConnectionLabel.Size = new System.Drawing.Size(296, 24);
			this.ConnectionLabel.TabIndex = 19;
			this.ConnectionLabel.Text = "Disconnected";
			this.ConnectionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// CurrentSongRequestLabel
			// 
			this.CurrentSongRequestLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.CurrentSongRequestLabel.AutoSize = true;
			this.CurrentSongRequestLabel.Font = new System.Drawing.Font("Consolas", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CurrentSongRequestLabel.Location = new System.Drawing.Point(12, 489);
			this.CurrentSongRequestLabel.Name = "CurrentSongRequestLabel";
			this.CurrentSongRequestLabel.Size = new System.Drawing.Size(180, 22);
			this.CurrentSongRequestLabel.TabIndex = 20;
			this.CurrentSongRequestLabel.Text = "Song Requests Off";
			// 
			// MainProgressBar
			// 
			this.MainProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.MainProgressBar.Location = new System.Drawing.Point(1022, 0);
			this.MainProgressBar.Margin = new System.Windows.Forms.Padding(0, 3, 3, 3);
			this.MainProgressBar.Name = "MainProgressBar";
			this.MainProgressBar.Size = new System.Drawing.Size(100, 23);
			this.MainProgressBar.TabIndex = 21;
			// 
			// WriteUpdatedSongInfoToFileButton
			// 
			this.WriteUpdatedSongInfoToFileButton.Location = new System.Drawing.Point(847, 385);
			this.WriteUpdatedSongInfoToFileButton.Name = "WriteUpdatedSongInfoToFileButton";
			this.WriteUpdatedSongInfoToFileButton.Size = new System.Drawing.Size(102, 23);
			this.WriteUpdatedSongInfoToFileButton.TabIndex = 12;
			this.WriteUpdatedSongInfoToFileButton.Text = "Update File Data";
			this.WriteUpdatedSongInfoToFileButton.UseVisualStyleBackColor = true;
			this.WriteUpdatedSongInfoToFileButton.Click += new System.EventHandler(this.WriteUpdatedSongInfoToFileButton_Click);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1122, 520);
			this.Controls.Add(this.BotStartStop);
			this.Controls.Add(this.MainProgressBar);
			this.Controls.Add(this.CurrentSongRequestLabel);
			this.Controls.Add(this.ConnectionLabel);
			this.Controls.Add(this.MainTabControl);
			this.Controls.Add(this.LoadingBar);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.LoadingBar;
			this.MinimumSize = new System.Drawing.Size(1138, 559);
			this.Name = "MainForm";
			this.Text = "ScoredBot";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.Load += new System.EventHandler(this.Form1_Load);
			((System.ComponentModel.ISupportInitialize)(this.SongRequestVideoView)).EndInit();
			this.LoadingBar.ResumeLayout(false);
			this.LoadingBar.PerformLayout();
			this.MainTabControl.ResumeLayout(false);
			this.SongRequestTab.ResumeLayout(false);
			this.SongRequestTab.PerformLayout();
			this.SecondaryPlaylistManagementTab.ResumeLayout(false);
			this.SecondaryPlaylistManagementTab.PerformLayout();
			this.DebugTab.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private LibVLCSharp.WinForms.VideoView SongRequestVideoView;
		private System.Windows.Forms.TextBox AddSongToPlayTextBox;
		public System.Windows.Forms.Button BotStartStop;
		public System.Windows.Forms.Button PlayPauseButton;
		public System.Windows.Forms.Button StopPlaybackButton;
		public System.Windows.Forms.Button ClearListButton;
		public System.Windows.Forms.Button AddLinkButton;
		private System.Windows.Forms.MenuStrip LoadingBar;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ExitToolButton;
		private System.Windows.Forms.ToolStripMenuItem botLoginToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem EnterBotDetailsToolButton;
		private System.Windows.Forms.ToolStripMenuItem CurrentDetailsToolButton;
		private System.Windows.Forms.ToolStripMenuItem UserNameToolLabel;
		private System.Windows.Forms.ToolStripMenuItem oAuthToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem OAuthToolLabel;
		private System.Windows.Forms.Button IncreaseVolumeButton;
		private System.Windows.Forms.Button DecreaseVolumeButton;
		private System.Windows.Forms.Label VolumeLabel;
		private System.Windows.Forms.Button RequestsButton;
		private System.Windows.Forms.Button SongSystemButton;
		private System.Windows.Forms.Button SaveLinkButton;
		private System.Windows.Forms.ListBox SongRequestList;
		private System.Windows.Forms.ToolStripMenuItem openOutputFolderToolStripMenuItem;
		private System.Windows.Forms.TabControl MainTabControl;
		private System.Windows.Forms.TabPage SongRequestTab;
		private System.Windows.Forms.Label ConnectionLabel;
		private System.Windows.Forms.TabPage DebugTab;
		public System.Windows.Forms.ListBox DebugConsoleList;
		public System.Windows.Forms.Button SkipSongButton;
		private System.Windows.Forms.Label CurrentSongRequestLabel;
		private System.Windows.Forms.Button RemoveSongFromSecondaryButton;
		private System.Windows.Forms.ToolStripMenuItem TargetFoundLabel;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem scoredToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem httpstwittercomScoredOneToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem httpswwwtwitchtvscoredoneToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem httpsassetstoreunitycompublishers35238ToolStripMenuItem;
		private System.Windows.Forms.ProgressBar MainProgressBar;
		private System.Windows.Forms.TabPage SecondaryPlaylistManagementTab;
		private System.Windows.Forms.Button RemoveBrokenSongButton;
		private System.Windows.Forms.TextBox SecondaryTextBoxAddField;
		private System.Windows.Forms.Button AddSecondarySongButton;
		private System.Windows.Forms.Button RemoveSecondarySongButton;
		private System.Windows.Forms.Label LoadedSongListBoxLabel;
		private System.Windows.Forms.ListBox BrokenSongsListBox;
		private System.Windows.Forms.ListBox LoadedSongsListBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button RetryBrokenSongButton;
		private System.Windows.Forms.Button RetryAllBrokenSongButton;
		private System.Windows.Forms.Button ClaimSongButton;
		private System.Windows.Forms.Button ClaimAllSongsButton;
		private System.Windows.Forms.Button WriteUpdatedSongInfoToFileButton;
	}
}

