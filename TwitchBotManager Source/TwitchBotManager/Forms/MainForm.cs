using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using LibVLCSharp.Shared;

using Microsoft.VisualBasic;

using TwitchBotManager.Code;
using TwitchBotManager.Code.Classes;

using Timer = System.Windows.Forms.Timer; // System.Threading; conflict

//LibVLC.Windows.Light
//https://t.co/nrXDbX722c?amp=1 //https://t.co/cAeSkJRNGn?amp=1

// Option to load songs on run
// Option to load songs raw (link only)

/* Song Request Work:
- Resort list into new catigories, (Pings work + song cached) - (Pings dont work + song cached) - (local songs) - (pings dont work + song not cached)
- Combine first 3 lists ^ into a playlist to play from
*/

/* TODO:
- Local songs secondary playlist entries
- Remove song from requested list (Needs testing)
- User Blacklist for songs
- Twitch chat message toggling
- Follower only request mode
- User chat tracker
- Whisper functionality for mods
- 
- 
- 
- 
- 
- 
*/

namespace TwitchBotManager {

	public partial class MainForm : Form {

		public static bool IsExiting { get; private set; } = false;

		private Timer UpdateHandler;

		private bool Initialized;
		private bool AppWorking;

		private TwitchBot twitchBot;
		private TwitchAPIInterfaceObject twitchAPI;

		public (string UserName, string OAuth, string Secret, string Target) TwitchBotLoginDetails { get; private set; } // UserName, oauth:xxxxxxxxxxxxxxxxxxxxxxxxxxxxx, TargetChannel

		private SongRequestManager songRequestManager;

		private static List<string> StaticDebugOutQueue = new List<string>();
		public static void StaticPostToDebug(string message) {
			Console.WriteLine(message);
			StaticDebugOutQueue.Add(message);
		}

		public Action<string> PostToDebug => e => {
			if (DebugConsoleList != null && Initialized) {
				DebugConsoleList.Items.Add($"{DateTime.Now:yyyy-MM-dd / HH-mm-ss} :: {e}");
				DebugConsoleList.TopIndex = DebugConsoleList.Items.Count - 1;
			}
		};

		public MainForm() {
			InitializeComponent();

			Core.Initialize();

			KeyPreview = true;

			Application.ApplicationExit += Application_ApplicationExit;

			PostToDebug.Invoke("ScoredBot Application Opened and Loaded");

			// Output Directory Data
			GlobalFunctions.CheckAndCreateOutputDirectoryFiles();

			songRequestManager = new SongRequestManager();
			songRequestManager.OnBuffering += SongRequestManager_OnBuffering;
			songRequestManager.OnError += SongRequestManager_OnError;
			songRequestManager.OnVolumeUpdate += SongRequestManager_OnVolumeUpdate;
			songRequestManager.OnSecondaryPlaylistUpdated += SongRequestManager_OnSecondaryPlaylistUpdated;
			songRequestManager.OnProgressbarUpdate += SongRequestManager_OnProgressbarUpdate;
			songRequestManager.OnSongRequestOutputChanged += SongRequestManager_OnSongRequestOutputChanged;
			songRequestManager.OnStopped += SongRequestManager_OnStopped;
			songRequestManager.OnNextSong += SongRequestManager_OnNextSong;
			songRequestManager.Initialize();

			VolumeLabel.Text = songRequestManager.PlayerVolumeTextOutput;

			TwitchBotLoginDetails = GlobalFunctions.LoadLoginFromFile();

			twitchAPI = new TwitchAPIInterfaceObject(TwitchBotLoginDetails.OAuth, TwitchBotLoginDetails.Secret); // Need more info to set this up

			Show();

			Initialized = true;
		}

		private void Form1_Load(object sender, EventArgs e) {
			UpdateHandler = new Timer {
				Interval = 250 // 0.25 sec
			};
			UpdateHandler.Tick += new EventHandler(Update);
			UpdateHandler.Start();

			if (File.Exists(Directory.GetCurrentDirectory() + @"\Nothing.wav")) { // Tricks windows to add app to advanced audio settings on app load before song requests starts
				SoundPlayer soundPlayer = new SoundPlayer(Directory.GetCurrentDirectory() + @"\Nothing.wav"); // Works as long as an empty wav file exists
				soundPlayer.PlaySync();
				soundPlayer.Dispose();
			}

			twitchAPI.InitialiseAccess();
		}

		#region ##### Main Form Functions and Events	#####

		private void Application_ApplicationExit(object sender, EventArgs e) {
			IsExiting = true;
			if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\CurrentChatLog.txt") && !string.IsNullOrEmpty(File.ReadAllText(Directory.GetCurrentDirectory() + @"\Outputs\CurrentChatLog.txt"))) {
				File.Move(Directory.GetCurrentDirectory() + @"\Outputs\CurrentChatLog.txt", Directory.GetCurrentDirectory() + @"\Outputs\ChatHistory\ChatLog(" + DateTime.Now.ToString("yyyy-MM-dd_hh-mmtt") + ").txt");
			}
			GlobalFunctions.UpdateSongRequest("");
		}

		private void Update(object sender, EventArgs e) {
			//refresh here...
			// TODO : Clean up all this...

			if (twitchBot != null) {
				if (twitchBot.IsActive) {
					if (twitchBot.IsConnected) {
						ConnectionLabel.Text = "Connected";
						ConnectionLabel.ForeColor = Color.Green;

						BotStartStop.Text = "Stop Bot";
					} else {
						ConnectionLabel.Text = "Disconnected";
						ConnectionLabel.ForeColor = Color.Red;

						BotStartStop.Text = "Start Bot";
					}

					BotStartStop.Text = twitchBot.IsConnected ? "Stop Bot" : "Start Bot";
				} else {
					ConnectionLabel.Text = "Attempting to connect";
					BotStartStop.Text = "Working";

					songRequestManager.TakingSongRequests = false;
				}

				RequestsButton.Enabled = twitchBot.IsActive;

				outputProcessingMessageToolStripMenuItem.Enabled = true;
			} else {
				BotStartStop.Enabled = !AppWorking && !(string.IsNullOrEmpty(TwitchBotLoginDetails.UserName) || string.IsNullOrEmpty(TwitchBotLoginDetails.OAuth));

				ConnectionLabel.Text = "Disconnected";
				ConnectionLabel.ForeColor = Color.Red;

				BotStartStop.Text = "Start Bot";

				RequestsButton.Enabled = false;
				songRequestManager.TakingSongRequests = false;

				outputProcessingMessageToolStripMenuItem.Enabled = false;
			}

			if (songRequestManager != null) {

				PlayPauseButton.Text = songRequestManager.IsPlaying ? "Pause" : "Play";

				AddLinkButton.Enabled = true;

				CurrentSongDefaultLabel.ThreadSafeAction(e => e.Text = songRequestManager.GetCurrentSong());
				CurrentSongRequestLabel.ThreadSafeAction(e => e.Text = songRequestManager.SongOutputText.OutputString);

				if (songRequestManager.IsStopped) {
					PlayPauseButton.Enabled = true;
					SkipSongButton.Enabled = StopPlaybackButton.Enabled = false;
				} else {
					PlayPauseButton.Enabled = SkipSongButton.Enabled = StopPlaybackButton.Enabled = !songRequestManager.IsLoading;
				}

			} else {
				PlayPauseButton.Enabled = false;
				StopPlaybackButton.Enabled = false;
				SkipSongButton.Enabled = false;

				AddLinkButton.Enabled = false;
				RequestsButton.Enabled = songRequestManager.TakingSongRequests = false;

				CurrentSongDefaultLabel.ThreadSafeAction(e => e.Text = "SongRequestManager Error");
			}

			UserNameToolLabel.Text = string.IsNullOrEmpty(TwitchBotLoginDetails.UserName) ? "No User Name Found" : TwitchBotLoginDetails.UserName;
			OAuthToolLabel.Text = string.IsNullOrEmpty(TwitchBotLoginDetails.OAuth) ? "No ClientID Found" : TwitchBotLoginDetails.OAuth;
			TargetFoundLabel.Text = string.IsNullOrEmpty(TwitchBotLoginDetails.Target) ? "No Target Found" : TwitchBotLoginDetails.Target;

			RequestsButton.Text = songRequestManager.TakingSongRequests ? "Requests ON" : "Requests OFF";

			// TODO : Change
			RetryAllBrokenSongButton.Enabled = songRequestManager.GetBrokenPlaylist().Count > 0;

			// StaticPostToDebug
			if (StaticDebugOutQueue.Count > 0) {
				List<string> outList = new List<string>(StaticDebugOutQueue);
				StaticDebugOutQueue.Clear();
				outList.ForEach(x => PostToDebug.Invoke(x));
			}
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {
			IsExiting = true;
		}

		private void ExitToolButton_Click(object sender, EventArgs e) {
			Application.Exit();
		}

		private void EnterBotDetailsToolButton_Click(object sender, EventArgs e) {
			TwitchLoginForm twitchLoginForm = new TwitchLoginForm(this);
			PostToDebug.Invoke("TwitchLoginForm Dialog Opened");
			twitchLoginForm.ShowDialog();

		}

		private void openOutputFolderToolStripMenuItem_Click(object sender, EventArgs e) {
			if (Directory.Exists(Directory.GetCurrentDirectory() + @"\Outputs")) {
				ProcessStartInfo startInfo = new ProcessStartInfo {
					Arguments = Directory.GetCurrentDirectory() + @"\Outputs",
					FileName = "explorer.exe"
				};

				Process.Start(startInfo);
			}
		}

		private void BotStartStop_Click(object sender, EventArgs e) {
			BotStartStop.Enabled = false;

			if (twitchBot == null) {
				twitchBot = new TwitchBot(TwitchBotLoginDetails.UserName, TwitchBotLoginDetails.OAuth, TwitchBotLoginDetails.Target);
				twitchBot.OnCurrentSong += TwitchBot_OnCurrentSong;
				twitchBot.OnAddSong += TwitchBot_OnAddSong;
				twitchBot.OnClearSongRequests += TwitchBot_OnClearSongRequests;
				twitchBot.OnPauseSongRequests += TwitchBot_OnPauseSongRequests;
				twitchBot.OnPlaySongRequests += TwitchBot_OnPlaySongRequests;
				twitchBot.OnPrintSongList += TwitchBot_OnPrintSongList;
				twitchBot.OnPrintUserSongRequests += TwitchBot_OnPrintUserSongRequests;
				twitchBot.OnRemoveAllSongs += TwitchBot_OnRemoveAllSongs;
				twitchBot.OnRemoveSong += TwitchBot_OnRemoveSong;
				twitchBot.OnSkipSong += TwitchBot_OnSkipSong;
				twitchBot.OnMessageReceived += TwitchBot_OnMessageReceived;

				twitchBot.OnConnectionError += TwitchBot_OnConnectionError;

				outputProcessingMessageToolStripMenuItem.Checked = twitchBot.OutputProcessingCommandMessage;

				PostToDebug.Invoke("Bot Started");
			} else if (twitchBot.IsActive) {
				if (twitchBot.IsConnected) {
					twitchBot.DisconnectFromChat();
					TwitchBot_OnClearSongRequests(null, null);
					songRequestManager.TakingSongRequests = false;

					PostToDebug.Invoke("Bot Stopped");
				} else {
					twitchBot.ConnectToChat();
					PostToDebug.Invoke("Bot Started");
				}
			}

			BotStartStop.Enabled = true;
		}

		private void TwitchBot_OnConnectionError(object sender, TwitchLib.Client.Events.OnConnectionErrorArgs e) {
			CancellationTokenSource cancellationToken = new CancellationTokenSource(60000);
			Task thread = new Task(async () => {
				AppWorking = true;
				do {
					twitchBot.ReconnectToChat();
					Console.WriteLine("Attempting to reconnect bot.");
					await Task.Delay(1000);
				} while (!twitchBot.IsConnected && !cancellationToken.IsCancellationRequested);

				if (cancellationToken.IsCancellationRequested && !twitchBot.IsConnected) {
					Console.WriteLine("Bot reconnection failed.");
					MessageBox.Show("The bot failed to reconnect to Twitch.", "Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}

				AppWorking = false;
			});
			thread.Start();
		}

		public void UpdateSongListOutput() {
			SongRequestList.ThreadSafeAction(e => e.Items.Clear());

			Dictionary<int, string> currentlist = songRequestManager.GetCurrentPlaylist(true);
			foreach (int index in currentlist.Keys.OrderBy(e => e)) {
				if (currentlist.TryGetValue(index, out string value)) {
					SongRequestList.ThreadSafeAction(e => e.Items.Add($"#{index + 1} :: {value}"));
					SongRequestList.ThreadSafeAction(e => e.Items.Add(""));
				}
			}
		}
		private void CurrentDetailsToolButton_CheckedChanged(object sender, EventArgs e) {
			UserNameToolLabel.Visible =
			OAuthToolLabel.Visible =
			TargetFoundLabel.Visible = !CurrentDetailsToolButton.Checked;

			CurrentDetailsToolButton.Text = CurrentDetailsToolButton.Checked ? "*Details Hidden*" : "Current Details";
		}

		private void MenuTabOpenURLEvent(object sender, EventArgs e) {
			ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
			Process process = Process.Start(menuItem.Text);
			PostToDebug.Invoke(menuItem.Text + " :: About link opened.");
		}

		private void MainTabControl_TabIndexChanged(object sender, EventArgs e) {
			MainTabControl.ThreadSafeAction(tabs => {
				if (tabs.SelectedTab == DebugTab) {
					DebugConsoleList.TopIndex = DebugConsoleList.Items.Count - 1;
					//LoadDebugMessages();
				} else if (tabs.SelectedTab == SecondaryPlaylistManagementTab) {
					UpdateSecPlaylistTabLists();
				}
			});
		}

		public void SetTwitchBotLoginDetails(string username, string clientid, string secret, string target) {
			TwitchBotLoginDetails = (username, clientid, secret, target);

			File.WriteAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SavedLoginInfo.txt", new string[] { username, clientid, secret, target });

			PostToDebug.Invoke("TwitchBotLoginDetails Set and saved");
		}

		private void reloadSecondarySongListToolStripMenuItem_Click(object sender, EventArgs e) {
			// TODO : Update reloadSecondarySongListToolStripMenuItem_Click

			GlobalFunctions.ExecuteThreadSafeActionToMultiple<Control>(x => x.Enabled = false,
				RemoveBrokenSongButton,
				RetryBrokenSongButton,
				AddSecondarySongButton,
				RemoveSecondarySongButton,
				ClaimSongButton,
				ClaimAllSongsButton,
				WriteUpdatedSongInfoToFileButton,
				LoadedSongsListBox,
				BrokenSongsListBox);

			songRequestManager.LoadSecondaryPlaylistFromFile();

			GlobalFunctions.ExecuteThreadSafeActionToMultiple<Control>(x => x.Enabled = true,
				ClaimAllSongsButton,
				WriteUpdatedSongInfoToFileButton,
				LoadedSongsListBox,
				BrokenSongsListBox);
		}

		private void outputProcessingMessageToolStripMenuItem_Click(object sender, EventArgs e) {
			bool value = outputProcessingMessageToolStripMenuItem.Checked = !outputProcessingMessageToolStripMenuItem.Checked;

			if (twitchBot != null) {
				twitchBot.OutputProcessingCommandMessage = value;
			}
		}

		#endregion

		#region ##### Song Request Tabs and Logic	#####

		#region ### TWITCH EVENT HANDLERS ###

		private void TwitchBot_OnCurrentSong(object sender, BotCommandContainer e) {
			string output = songRequestManager.GetCurrentSong();
			if (!string.IsNullOrEmpty(output)) {
				twitchBot.SendMessageToTwitchChat($"Current Song :: {output}");
			} else {
				twitchBot.SendMessageToTwitchChat("Sorry an error occured getting the current song.");
			}
		}

		private async void TwitchBot_OnAddSong(object sender, BotCommandContainer e) {
			string output = await songRequestManager.SubmitSongRequest(e.Command, e.User);

			twitchBot.SendMessageToTwitchChat(output);

			UpdateSongListOutput();
		}

		private void TwitchBot_OnPauseSongRequests(object sender, BotCommandContainer e) {
			songRequestManager.Pause();
		}

		private void TwitchBot_OnPlaySongRequests(object sender, BotCommandContainer e) {
			songRequestManager.Play();
		}

		private void TwitchBot_OnPrintSongList(object sender, BotCommandContainer e) {
			Dictionary<int, string> songlistout = songRequestManager.GetCurrentPlaylist();

			string Output = songlistout.Count > 4 ? "Song List, Next 5 songs :: " : $"Song List, Next {songlistout.Count} songs :: ";

			for (int x = 0; x < 5 && x < songlistout.Count; x++) {
				if (songlistout.TryGetValue(x, out string value)) {
					Output += $"#{x} -- {value} || ";
				}
			}

			twitchBot.SendMessageToTwitchChat(Output);
		}

		private void TwitchBot_OnPrintUserSongRequests(object sender, BotCommandContainer e) {
			twitchBot.SendMessageToTwitchChat(songRequestManager.PrintRequesterSongList(e.User));
		}

		private void TwitchBot_OnClearSongRequests(object sender, BotCommandContainer e) {
			songRequestManager.ClearSongRequests();
			UpdateSongListOutput();
		}

		private void TwitchBot_OnRemoveAllSongs(object sender, BotCommandContainer e) {
			songRequestManager.ClearSongRequests();

			twitchBot.SendMessageToTwitchChat(e.User + " Has cleared the song list.");
			UpdateSongListOutput();
		}

		private void TwitchBot_OnRemoveSong(object sender, BotCommandContainer e) {
			if (string.IsNullOrEmpty(e.Command)) {
				twitchBot.SendMessageToTwitchChat(songRequestManager.RemoveLastSongByUser(e.User));
			} else if (int.TryParse(e.Command, out int result)) {
				twitchBot.SendMessageToTwitchChat(songRequestManager.RemoveIndexSongByUser(e.User, result));
			} else {
				twitchBot.SendMessageToTwitchChat($"@{e.User} Sorry that command wasnt recognised.");
			}
			UpdateSongListOutput();
		}

		private void TwitchBot_OnSkipSong(object sender, BotCommandContainer e) {
			songRequestManager.Skip();
			if (twitchBot != null) {
				twitchBot.SendMessageToTwitchChat(e.User + " Has skipped the song.");
			}
			PostToDebug.Invoke("Song Skipped by " + e.User);
		}

		private void TwitchBot_OnMessageReceived(object sender, string e) {
			if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\CurrentChatLog.txt")) {
				File.AppendAllText(Directory.GetCurrentDirectory() + @"\Outputs\CurrentChatLog.txt", e + Environment.NewLine);
			}
		}

		#endregion

		#region ### VOLUME BUTTONS ###

		private Timer VolumeLooper;

		private void IncreaseVolumeButton_MouseDown(object sender, MouseEventArgs e) {
			void IncreaseVolume() {
				if (songRequestManager != null && songRequestManager.CurrentVolume < 100) {
					songRequestManager.CurrentVolume++;
					VolumeLabel.Text = songRequestManager.PlayerVolumeTextOutput;
				}
			}

			IncreaseVolume();

			VolumeLooper = new Timer {
				Interval = 100 // 0.1 sec
			};
			VolumeLooper.Tick += new EventHandler((s, e) => {
				IncreaseVolume();
			});
			VolumeLooper.Start();
		}

		private void DecreaseVolumeButton_MouseDown(object sender, MouseEventArgs e) {
			void DecreaseVolume() {
				if (songRequestManager != null && songRequestManager.CurrentVolume > 0) {
					songRequestManager.CurrentVolume--;
					VolumeLabel.Text = songRequestManager.PlayerVolumeTextOutput;
				}
			}

			DecreaseVolume();

			VolumeLooper = new Timer {
				Interval = 100 // 0.1 sec
			};
			VolumeLooper.Tick += new EventHandler((s, e) => {
				DecreaseVolume();
			});
			VolumeLooper.Start();
		}

		private void VolumeButton_MouseUp(object sender, MouseEventArgs e) {
			VolumeLooper.Stop();
			VolumeLooper.Dispose();
			VolumeLooper = null;

			if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\MediaVolume.txt")) {
				File.WriteAllText(Directory.GetCurrentDirectory() + @"\Outputs\MediaVolume.txt", songRequestManager.CurrentVolume.ToString());
			}

			PostToDebug.Invoke("Volume set to " + songRequestManager.CurrentVolume.ToString());
		}

		#endregion

		#region ### EVENTS ###

		private void PlayPauseButton_Click(object sender, EventArgs e) {
			if (songRequestManager.IsPlaying) {
				songRequestManager.Pause();
			} else {
				songRequestManager.Play();
			}
		}

		private void StopPlaybackButton_Click(object sender, EventArgs e) {
			songRequestManager.Stop();

			PostToDebug.Invoke("Song Requests Stopped");
		}

		private void ClearListButton_Click(object sender, EventArgs e) {
			songRequestManager.ClearSongRequests();

			UpdateSongListOutput();
			PostToDebug.Invoke("Song Requests list cleared");
		}

		private async void AddLinkButton_Click(object sender, EventArgs e) {
			// Currently just adds a song to the song list, need a button or change this to do one for host queue
			if (twitchBot != null && twitchBot.IsActive && songRequestManager != null) {
				await songRequestManager.SubmitSongRequest(TwitchBotLoginDetails.UserName, AddSongToPlayTextBox.Text, true);
				PostToDebug.Invoke(AddSongToPlayTextBox.Text + " Link added to current requests");
			}
		}

		private void RequestsButton_Click(object sender, EventArgs e) {
			songRequestManager.TakingSongRequests = !songRequestManager.TakingSongRequests;

			PostToDebug.Invoke(songRequestManager.TakingSongRequests ? "Song System set to taking requests" : "Song System set to not taking requests");
		}

		private void SaveLinkButton_Click(object sender, EventArgs e) {
			if (songRequestManager.SaveCurrentSong()) {
				SaveLinkButton.Enabled = false;
				RemoveSongFromSecondaryButton.Enabled = true;
			}
		}

		private void RemoveSongFromSecondaryButton_Click(object sender, EventArgs e) {
			if (songRequestManager.RemoveCurrentSongFromSeconday()) {
				SaveLinkButton.Enabled = true;
				RemoveSongFromSecondaryButton.Enabled = false;
			}
		}

		private void SkipSongButton_Click(object sender, EventArgs e) {
			TwitchBot_OnSkipSong(null, new BotCommandContainer(SongRequestCommandType.SkipSong, TwitchBotLoginDetails.UserName, null));
		}

		#endregion

		#region ### SECONDARY SONG MANAGER TAB ###

		public void UpdateSecPlaylistTabLists() {
			Dictionary<int, string> Secondary = songRequestManager.GetSecondaryPlaylist();
			Dictionary<int, string> Broken = songRequestManager.GetBrokenPlaylist();

			if (Secondary.Count > 0) {
				LoadedSongsListBox.ThreadSafeAction(e => {
					e.DataSource = Secondary.Select(f => $"{f.Key + 1}. {f.Value}").ToList();
				});
			}

			if (Broken.Count > 0) {
				BrokenSongsListBox.ThreadSafeAction(e => {
					e.DataSource = Broken.Select(f => $"{f.Key + 1}. {f.Value}").ToList();
				});
			}
		}

		private void RemoveSecondarySongButton_Click(object sender, EventArgs e) {
			if (songRequestManager.RemoveSecondaryAtIndex(LoadedSongsListBox.SelectedIndex, out string output)) {
				UpdateSecPlaylistTabLists();
			}
			PostToDebug.Invoke(output);
		}

		private async void AddSecondarySongButton_Click(object sender, EventArgs e) {
			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = false, RemoveBrokenSongButton, RetryBrokenSongButton, AddSecondarySongButton, RemoveSecondarySongButton, ClaimSongButton, ClaimAllSongsButton, WriteUpdatedSongInfoToFileButton);

			string link = SecondaryTextBoxAddField.Text.Trim();
			string requester = TwitchBotLoginDetails.UserName;

			if (string.IsNullOrEmpty(requester)) {
				requester = Interaction.InputBox("Twitch Login currently empty, please insert a requester name.", "Requester field needed.", "#NOT PROVIDED#");
			}

			Match match = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)").Match(link);
			if (match.Success) {
				// TODO : Support for local files
				await songRequestManager.AddSecondarySong(link, requester, false);
			} else {
				MessageBox.Show("Link does not appear to be a YouTube link, please check link and try again.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = true, RemoveBrokenSongButton, RetryBrokenSongButton, AddSecondarySongButton, RemoveSecondarySongButton, ClaimSongButton, ClaimAllSongsButton, WriteUpdatedSongInfoToFileButton);
		}

		private void RemoveBrokenSongButton_Click(object sender, EventArgs e) {
			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = false, RemoveBrokenSongButton, RetryBrokenSongButton, AddSecondarySongButton, RemoveSecondarySongButton, ClaimSongButton, ClaimAllSongsButton, WriteUpdatedSongInfoToFileButton);

			if (songRequestManager.RemoveBrokenAtIndex(LoadedSongsListBox.SelectedIndex, out string output)) {
				UpdateSecPlaylistTabLists();
			}
			PostToDebug.Invoke(output);

			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = true, RemoveBrokenSongButton, RetryBrokenSongButton, AddSecondarySongButton, RemoveSecondarySongButton, ClaimSongButton, ClaimAllSongsButton, WriteUpdatedSongInfoToFileButton);
		}

		private async void RetryBrokenSongButton_Click(object sender, EventArgs e) {
			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = false, BrokenSongsListBox, LoadedSongsListBox);

			await songRequestManager.PingBrokenSongInfo(BrokenSongsListBox.SelectedIndex).ContinueWith((t) => {
				UpdateSecPlaylistTabLists();

				GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = true, BrokenSongsListBox, LoadedSongsListBox);
			});
		}

		private async void RetryAllBrokenSongButton_Click(object sender, EventArgs e) {
			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = false, BrokenSongsListBox, LoadedSongsListBox);

			await songRequestManager.PingAllBrokenSongInfo();

			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = true, BrokenSongsListBox, LoadedSongsListBox);
		}

		private void LoadedSongsListBox_SelectedIndexChanged(object sender, EventArgs e) {
			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = LoadedSongsListBox.SelectedIndex != -1, RemoveSecondarySongButton, ClaimSongButton);
			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = false, RemoveBrokenSongButton, RetryBrokenSongButton, AddSecondarySongButton);
			BrokenSongsListBox.ThreadSafeAction(x => x.SelectedIndex = -1);
			if (LoadedSongsListBox.SelectedIndex != -1 && songRequestManager.GetFromSecondaryByIndex(LoadedSongsListBox.SelectedIndex, out NameValueCollection data)) {
				SecondaryTextBoxAddField.Text = data["Link"];
			}
		}

		private void BrokenSongsListBox_SelectedIndexChanged(object sender, EventArgs e) {
			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = false, RemoveSecondarySongButton, AddSecondarySongButton);
			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = BrokenSongsListBox.SelectedIndex != -1, RemoveBrokenSongButton, RetryBrokenSongButton, ClaimSongButton);
			LoadedSongsListBox.ThreadSafeAction(x => x.SelectedIndex = -1);
			if (BrokenSongsListBox.SelectedIndex != -1 && songRequestManager.GetFromBrokenByIndex(BrokenSongsListBox.SelectedIndex, out NameValueCollection data)) {
				SecondaryTextBoxAddField.Text = data["Link"];
			}
		}

		private void ClaimSongButton_Click(object sender, EventArgs e) {
			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = false, BrokenSongsListBox, LoadedSongsListBox);

			string requester = string.IsNullOrEmpty(TwitchBotLoginDetails.UserName) ?
				Interaction.InputBox("Twitch Login currently empty, please insert a requester name.", "Requester field needed.", "#NOT PROVIDED#") :
				TwitchBotLoginDetails.UserName;

			if (LoadedSongsListBox.SelectedIndex != -1) {
				songRequestManager.ClaimSong(requester, false, LoadedSongsListBox.SelectedIndex);
			} else if (BrokenSongsListBox.SelectedIndex != -1) {
				songRequestManager.ClaimSong(requester, true, BrokenSongsListBox.SelectedIndex);
			}

			UpdateSecPlaylistTabLists();
			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = true, BrokenSongsListBox, LoadedSongsListBox);
		}

		private void ClaimAllSongsButton_Click(object sender, EventArgs e) {
			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = false, BrokenSongsListBox, LoadedSongsListBox);

			string requester = string.IsNullOrEmpty(TwitchBotLoginDetails.UserName) ? 
				Interaction.InputBox("Twitch Login currently empty, please insert a requester name.", "Requester field needed.", "#NOT PROVIDED#") : 
				TwitchBotLoginDetails.UserName;

			songRequestManager.ClaimAllSongs(requester);
			UpdateSecPlaylistTabLists();
			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = true, BrokenSongsListBox, LoadedSongsListBox);
		}

		private void SecondaryTextBoxAddField_TextChanged(object sender, EventArgs e) {
			if (GlobalFunctions.GetYouTubeVideoID(SecondaryTextBoxAddField.Text, out string ID)) {
				// TODO : Support for local files
				bool value = songRequestManager.CheckAddressExistsInSystem(SecondaryTextBoxAddField.Text.Trim(), false);
				AddSecondarySongButton.ThreadSafeAction(x => x.Enabled = !value);
				RemoveSecondarySongButton.ThreadSafeAction(x => x.Enabled = value);

			} else {
				GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = false, AddSecondarySongButton, RemoveSecondarySongButton);
			}

		}

		private void WriteUpdatedSongInfoToFileButton_Click(object sender, EventArgs e) {
			songRequestManager.WriteSongListsToFile(true);
		}

		#endregion

		#region ### SONGREQUESTMANAGER EVENTS ###	

		private void SongRequestManager_OnSongRequestOutputChanged(object sender, string e) {
			GlobalFunctions.UpdateSongRequest(e);
			if (songRequestManager.CheckCurrentSongIsSaved()) {
				SaveLinkButton.ThreadSafeAction(e => e.Enabled = false);
				RemoveSongFromSecondaryButton.ThreadSafeAction(e => e.Enabled = true);
			} else {
				SaveLinkButton.ThreadSafeAction(e => e.Enabled = true);
				RemoveSongFromSecondaryButton.ThreadSafeAction(e => e.Enabled = false);
			}
		}

		private void SongRequestManager_OnProgressbarUpdate(object sender, float e) {
			
		}

		private void SongRequestManager_OnSecondaryPlaylistUpdated(object sender, EventArgs e) {
			UpdateSecPlaylistTabLists();
		}

		private void SongRequestManager_OnVolumeUpdate(object sender, int e) {
			VolumeLabel.ThreadSafeAction(e => e.Text = "Volume: " + e.ToString());
		}

		private void SongRequestManager_OnError(object sender, string e) {
			PostToDebug.Invoke(e);
		}

		private void SongRequestManager_OnBuffering(object sender, bool e) {
			
		}

		private void SongRequestManager_OnStopped(object sender, bool e) {
			GlobalFunctions.UpdateSongRequest(CurrentSongRequestLabel, "Song Requests Stopped");
		}

		private void SongRequestManager_OnNextSong(object sender, bool e) {
			UpdateSongListOutput();
		}

		#endregion

		#endregion

	}
}