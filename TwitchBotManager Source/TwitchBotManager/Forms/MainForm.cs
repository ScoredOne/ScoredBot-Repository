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

using ScoredBot.Code;
using ScoredBot.Code.Classes;
using ScoredBot.Settings;

using Timer = System.Windows.Forms.Timer; // System.Threading; conflict

//LibVLC.Windows.Light
//https://t.co/nrXDbX722c?amp=1 //https://t.co/cAeSkJRNGn?amp=1

// Option to load songs on run
// Option to load songs raw (link only)

/*
 - Song check if in secondary currently requires exact link instead of cleaning then checking (eg &t=12 [time stamp] makes it think the song isnt in the store)
 */

/* TODO:
- Local songs secondary playlist entries
- User Blacklist for songs
- Twitch chat message toggling
- Follower only request mode
- User chat tracker
- Whisper functionality for mods
- Timed actions
- 
- 
- 
- https://stackoverflow.com/questions/453161/how-can-i-save-application-settings-in-a-windows-forms-application
*/

namespace ScoredBot {

	public partial class MainForm : Form {

		public static bool IsExiting { get; private set; } = false;

		private Timer UpdateHandler;

		private bool Initialized;
		private bool AppWorking;

		private TwitchBot twitchBot;
		//private TwitchAPIInterfaceObject twitchAPI;

		private SongRequestManager songRequestManager;

		private List<NameValueCollection> RequestedSongs;
		private List<NameValueCollection> SecondarySongs;
		private List<NameValueCollection> BrokenSongs;

		private List<TwitchMessageTranslation> twitchMessages = new List<TwitchMessageTranslation>();

		private static List<string> StaticDebugOutQueue = new List<string>();
		public static void StaticPostToDebug(string message) {
			Console.WriteLine(message);
			StaticDebugOutQueue.Add(message);
		}

		public Action<string> PostToDebug => e => {
			if (DebugConsoleList != null && Initialized) {
				DebugConsoleList.ThreadSafeAction(f => {
					f.Items.Add($"{DateTime.Now:yyyy-MM-dd / HH-mm-ss} :: {e}");
					f.TopIndex = DebugConsoleList.Items.Count - 1;
				});
			}
		};

		public MainForm() {
			InitializeComponent();

			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			Core.Initialize();

			KeyPreview = true;

			Application.ApplicationExit += Application_ApplicationExit;

			PostToDebug.Invoke("ScoredBot Application Opened and Loaded");

			// Output Directory Data
			ProgramSettings.CheckAndCreateOutputDirectoryFiles();

			songRequestManager = new SongRequestManager();
			songRequestManager.OnBuffering += SongRequestManager_OnBuffering;
			songRequestManager.OnError += SongRequestManager_OnError;
			songRequestManager.OnVolumeUpdate += SongRequestManager_OnVolumeUpdate;
			songRequestManager.OnMaxRequestsUpdate += SongRequestManager_OnMaxRequestsUpdate;
			songRequestManager.OnSecondaryPlaylistUpdated += SongRequestManager_OnSecondaryPlaylistUpdated;
			songRequestManager.OnProgressbarUpdate += SongRequestManager_OnProgressbarUpdate;
			songRequestManager.OnSongRequestOutputChanged += SongRequestManager_OnSongRequestOutputChanged;
			songRequestManager.OnStopped += SongRequestManager_OnStopped;
			songRequestManager.OnNextSong += SongRequestManager_OnNextSong;
			songRequestManager.OnSongListUpdated += SongRequestManager_OnSongListUpdated;
			songRequestManager.Initialize();

			VolumeLabel.Text = songRequestManager.PlayerVolumeTextOutput;

			MaxRequestsLabel.Text = $"Request Limit: {ProgramSettings.AppSettings.AppMusicMaxRequests}";

			CacheSongsRadioBut.Checked = ProgramSettings.AppSettings.CacheNewSongs;

			//twitchAPI = new TwitchAPIInterfaceObject(ProgramSettings.AppSettings.OAuth,
			//	ProgramSettings.AppSettings.Secret); // Need more info to set this up

			Show();

			Initialized = true;
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
			Exception crash = (Exception)e.ExceptionObject;
			string output = $"{crash.Message}{Environment.NewLine}{Environment.NewLine}{crash.StackTrace}{Environment.NewLine}{Environment.NewLine}{crash.InnerException}{Environment.NewLine}{Environment.NewLine}{crash.Data}";
			File.WriteAllText(Directory.GetCurrentDirectory() + @"\Outputs\CRASH(" + DateTime.Now.ToString("yyyy-MM-dd_hh-mmtt") + ").txt", output);
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

			//twitchAPI.InitialiseAccess();
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

				AddLinkButton.Enabled =
				RequestsButton.Enabled = twitchBot.IsActive;

				outputProcessingMessageToolStripMenuItem.Enabled = true;
			} else {
				BotStartStop.Enabled = !AppWorking && !(string.IsNullOrEmpty(ProgramSettings.AppSettings.UserName) ||
					string.IsNullOrEmpty(ProgramSettings.AppSettings.OAuth));

				ConnectionLabel.Text = "Disconnected";
				ConnectionLabel.ForeColor = Color.Red;

				BotStartStop.Text = "Start Bot";

				AddLinkButton.Enabled =
				RequestsButton.Enabled = false;
				songRequestManager.TakingSongRequests = false;

				outputProcessingMessageToolStripMenuItem.Enabled = false;
			}

			if (songRequestManager != null) {

				PlayPauseButton.Text = songRequestManager.IsPlaying ? "Pause" : "Play";

				if (songRequestManager.PlaylistLoadError) {
					SaveLinkButton.Enabled =
					RequestsButton.Enabled =
					RemoveSongFromSecondaryButton.Enabled =
					ClaimAllSongsButton.Enabled =
					ClaimSongButton.Enabled =
					RemoveSecondarySongButton.Enabled =
					AddSecondarySongButton.Enabled =
					WriteUpdatedSongInfoToFileButton.Enabled =
					songRequestManager.TakingSongRequests =
					ClearALLCacheButton.Enabled =
					CacheALLSongsButton.Enabled =
					ClaimAllSongsButton.Enabled =
					ClaimSongButton.Enabled =
					CacheSongButton.Enabled = false;
				} 

				CurrentSongDefaultLabel.ThreadSafeAction(e => e.Text = songRequestManager.GetCurrentSong());
				CurrentSongRequestLabel.ThreadSafeAction(e => e.Text = songRequestManager.SongOutputText.OutputString);

				if (songRequestManager.IsLoading) {
					songRequestManager.Stop();
					PlayPauseButton.Enabled =
					SkipSongButton.Enabled =
					RemoveSecondarySongButton.Enabled =
					AddSecondarySongButton.Enabled =
					ClearALLCacheButton.Enabled =
					CacheALLSongsButton.Enabled =
					ClaimAllSongsButton.Enabled =
					ClaimSongButton.Enabled =
					CacheSongButton.Enabled =
					StopPlaybackButton.Enabled = false;
				} else if (songRequestManager.IsStopped) {
					PlayPauseButton.Enabled =
					ClearALLCacheButton.Enabled =
					CacheALLSongsButton.Enabled =
					ClaimAllSongsButton.Enabled =
					ClaimSongButton.Enabled =
					CacheSongButton.Enabled = true;
					SkipSongButton.Enabled = StopPlaybackButton.Enabled = false;
				} else {
					PlayPauseButton.Enabled =
					SkipSongButton.Enabled =
					StopPlaybackButton.Enabled =
					ClearALLCacheButton.Enabled =
					CacheALLSongsButton.Enabled =
					ClaimAllSongsButton.Enabled =
					ClaimSongButton.Enabled =
					CacheSongButton.Enabled = true;
				}
			} else {
				PlayPauseButton.Enabled =
				StopPlaybackButton.Enabled =
				SkipSongButton.Enabled =

				AddLinkButton.Enabled =
				RequestsButton.Enabled =
				songRequestManager.TakingSongRequests = false;

				CurrentSongDefaultLabel.ThreadSafeAction(e => e.Text = "SongRequestManager Error");
			}

			UserNameToolLabel.Text = string.IsNullOrEmpty(ProgramSettings.AppSettings.UserName) ? "No User Name Found" : ProgramSettings.AppSettings.UserName;
			OAuthToolLabel.Text = string.IsNullOrEmpty(ProgramSettings.AppSettings.OAuth) ? "No ClientID Found" : ProgramSettings.AppSettings.OAuth;
			TargetFoundLabel.Text = string.IsNullOrEmpty(ProgramSettings.AppSettings.Target) ? "No Target Found" : ProgramSettings.AppSettings.Target;

			RequestsButton.Text = songRequestManager.TakingSongRequests ? "Requests ON" : "Requests OFF";

			// TODO : Change
			RetryAllBrokenSongButton.Enabled = songRequestManager.GetBrokenPlaylist().Count() > 0;

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
				twitchBot = new TwitchBot(ProgramSettings.AppSettings.UserName,
					ProgramSettings.AppSettings.OAuth,
					ProgramSettings.AppSettings.Target);
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
				twitchBot.OnMODRemoveSong += TwitchBot_OnMODRemoveSong;

				twitchBot.OnConnectionError += TwitchBot_OnConnectionError;

				outputProcessingMessageToolStripMenuItem.Checked = twitchBot.OutputProcessingCommandMessage;

				PostToDebug.Invoke("Bot Started");
			} else if (twitchBot.IsActive) {
				if (twitchBot.IsConnected) {
					twitchBot.DisconnectFromChat();
					songRequestManager.TakingSongRequests = false;

					PostToDebug.Invoke("Bot Stopped");
				} else {
					twitchBot.ConnectToChat();
					PostToDebug.Invoke("Bot Started");
				}
			}

			BotStartStop.Enabled = true;
		}

		private void TwitchBot_OnConnectionError(object sender, EventArgs e) {
			CancellationTokenSource cancellationToken = new CancellationTokenSource(60000);
			Thread thread = new Thread(async () => {
				AppWorking = true;
				try {
					do {
						await Task.Delay(1000);
						twitchBot.ReconnectToChat();
						StaticPostToDebug("Attempting to reconnect bot.");
					} while (!twitchBot.IsConnected && !cancellationToken.IsCancellationRequested);

					if (cancellationToken.IsCancellationRequested && !twitchBot.IsConnected) {
						StaticPostToDebug("Bot reconnection failed.");
						MessageBox.Show("The bot failed to reconnect to Twitch.", "Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				} catch { }
				AppWorking = false;
			});
			thread.Start();
		}

		public void UpdateSongListOutput() {
			List<string> UpdatedList = new List<string>();
			int count = 0;

			foreach (string song in songRequestManager.GetCurrentPlaylist(true)) {
				UpdatedList.Add($"#{++count} :: {song}");
			}

			SongRequestList.ThreadSafeAction(e => e.DataSource = UpdatedList);
			if (UpdatedList.Count > 0) {
				SongRequestList.ThreadSafeAction(e => e.SelectedIndex = -1);
			}
			RemoveSongRequestButton.ThreadSafeAction(e => e.Enabled = UpdatedList.Count > 0);
			RequestedSongs = songRequestManager.GetRequestedSonglist();
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
			ProgramSettings.AppSettings.UserName = username;
			ProgramSettings.AppSettings.OAuth = clientid;
			ProgramSettings.AppSettings.Secret = secret;
			ProgramSettings.AppSettings.Target = target;

			File.WriteAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SavedLoginInfo.txt", new string[] { username, clientid, secret, target });

			PostToDebug.Invoke("TwitchBotLoginDetails Set and saved");
		}

		private void reloadSecondarySongListToolStripMenuItem_Click(object sender, EventArgs e) {
			// TODO : Update reloadSecondarySongListToolStripMenuItem_Click

			GlobalFunctions.ExecuteThreadSafeActionToMultiple<Control>(x => x.Enabled = false,
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
			if (e.Commands != null && e.Commands.Count > 0) {
				string command = e.Commands[0];

				string output = await songRequestManager.SubmitSongRequest(command, e.User);

				twitchBot.SendMessageToTwitchChat(output);
			} else {
				StaticPostToDebug("ERROR : TwitchBot_OnAddSong called without a song link attached.");
			}

		}

		private void TwitchBot_OnPauseSongRequests(object sender, BotCommandContainer e) {
			songRequestManager.Pause();
			twitchBot.SendMessageToTwitchChat($"@{e.User} : Set to Pause.");
		}

		private void TwitchBot_OnPlaySongRequests(object sender, BotCommandContainer e) {
			songRequestManager.Play();
			twitchBot.SendMessageToTwitchChat($"@{e.User} : Set to Play.");
		}

		private void TwitchBot_OnPrintSongList(object sender, BotCommandContainer e) {
			IEnumerable<string> songlistout = songRequestManager.GetCurrentPlaylist();

			if (songlistout.Count() == 0) {
				twitchBot.SendMessageToTwitchChat($"@{e.User} Song List is currently empty.");
			} else {
				int count = songlistout.Count() > 4 ? 5 : songlistout.Count();
				string Output = $"@{e.User} Song List, Next {count} songs :: ";

				for (int x = 0; x < count; x++) {
					Output += $"|| #{x + 1}. {songlistout.ElementAt(x)} ";
				}

				twitchBot.SendMessageToTwitchChat(Output);
			}
		}

		private void TwitchBot_OnPrintUserSongRequests(object sender, BotCommandContainer e) {
			twitchBot.SendMessageToTwitchChat(songRequestManager.PrintRequesterSongList(e.User));
		}

		private void TwitchBot_OnClearSongRequests(object sender, BotCommandContainer e) {
			songRequestManager.ClearSongRequests();

			twitchBot.SendMessageToTwitchChat(e.User + " Has cleared the song list.");
		}

		private void TwitchBot_OnRemoveAllSongs(object sender, BotCommandContainer e) {
			songRequestManager.ClearSongRequests();

			twitchBot.SendMessageToTwitchChat(e.User + " Has cleared the song list.");
		}

		private void TwitchBot_OnRemoveSong(object sender, BotCommandContainer e) {
			if (e.Commands == null) {
				twitchBot.SendMessageToTwitchChat(songRequestManager.RemoveLastSongByUser(e.User));
			} else if (e.Commands.Count > 0) {
				string command = e.Commands[0];
				if (int.TryParse(command, out int result)) {
					string message = songRequestManager.RemoveIndexSongByUser(e.User, result);
					PostToDebug.Invoke(message);
					twitchBot.SendMessageToTwitchChat(message);
				} else {
					twitchBot.SendMessageToTwitchChat($"@{e.User} Sorry that command wasnt recognised.");
				}
			}
		}

		private void TwitchBot_OnSkipSong(object sender, BotCommandContainer e) {
			songRequestManager.Skip();
			if (twitchBot != null) {
				twitchBot.SendMessageToTwitchChat(e.User + " Has skipped the song.");
				PostToDebug.Invoke("Song Skipped by " + e.User);
			}
		}

		private void TwitchBot_OnMessageReceived(object sender, TwitchMessageTranslation e) {
			File.AppendAllText(Directory.GetCurrentDirectory() + @"\Outputs\CurrentChatLog.txt", e + Environment.NewLine);
			twitchMessages.Add(e);
		}

		private void TwitchBot_OnMODRemoveSong(object sender, BotCommandContainer e) {
			if (e.Commands == null || e.Commands.Count == 0) {
				twitchBot.SendMessageToTwitchChat($"@{e.User} Sorry that command for 'modremovesong' wasnt recognised.");
			} else {
				string message = songRequestManager.RemoveSongFromRequestsByYTID(e.User, e.Commands[0]);
				PostToDebug.Invoke(message);
				twitchBot.SendMessageToTwitchChat(message);
			}
		}

		#endregion

		#region ### VOLUME BUTTONS ###

		private Timer VolumeLooper;

		private void IncreaseVolumeButton_MouseDown(object sender, MouseEventArgs e) {
			void IncreaseVolume() {
				if (songRequestManager != null && ProgramSettings.AppSettings.AppMusicVolume < 100) {
					ProgramSettings.AppSettings.AppMusicVolume++;
					songRequestManager.UpdatePlayerVolume();
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
				if (songRequestManager != null && ProgramSettings.AppSettings.AppMusicVolume > 0) {
					ProgramSettings.AppSettings.AppMusicVolume--;
					songRequestManager.UpdatePlayerVolume();
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
			PostToDebug.Invoke("Volume set to " + ProgramSettings.AppSettings.AppMusicVolume.ToString());
		}

		#endregion

		#region ### MAX REQUEST BUTTONS ###
		private void DecreaseMaxRequestsButton_Click(object sender, EventArgs e) {
			if (ProgramSettings.AppSettings.AppMusicMaxRequests > 1) {
				ProgramSettings.AppSettings.AppMusicMaxRequests--;
			} else {
				ProgramSettings.AppSettings.AppMusicMaxRequests = 1;
			}
			MaxRequestsLabel.ThreadSafeAction(e => e.Text = $"Request Limit: {ProgramSettings.AppSettings.AppMusicMaxRequests}");
			PostToDebug.Invoke("Max User Requests set to " + ProgramSettings.AppSettings.AppMusicMaxRequests.ToString());
		}

		private void IncreaseMaxRequestsButton_Click(object sender, EventArgs e) {
			if (ProgramSettings.AppSettings.AppMusicMaxRequests < 100) {
				ProgramSettings.AppSettings.AppMusicMaxRequests++;
			} else {
				ProgramSettings.AppSettings.AppMusicMaxRequests = 100;
			}
			MaxRequestsLabel.ThreadSafeAction(e => e.Text = $"Request Limit: {ProgramSettings.AppSettings.AppMusicMaxRequests}");
			PostToDebug.Invoke("Max User Requests set to " + ProgramSettings.AppSettings.AppMusicMaxRequests.ToString());
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

			PostToDebug.Invoke("Song Requests list cleared");
		}

		private void AddLinkButton_Click(object sender, EventArgs e) {
			// Currently just adds a song to the song list, need a button or change this to do one for host queue
			if (twitchBot != null && twitchBot.IsActive && songRequestManager != null && GlobalFunctions.GetYouTubeVideoID(AddSongToPlayTextBox.Text, out _)) {
				Task.Run(() => songRequestManager.SubmitSongRequest(AddSongToPlayTextBox.Text, ProgramSettings.AppSettings.UserName, true));
				PostToDebug.Invoke(AddSongToPlayTextBox.Text + " Link added to current requests");
				AddSongToPlayTextBox.Text = "";
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
			TwitchBot_OnSkipSong(null, new BotCommandContainer(SongRequestCommandType.SkipSong, ProgramSettings.AppSettings.UserName, null));
		}

		private void RemoveSongRequestButton_Click(object sender, EventArgs e) {
			if (SongRequestList.SelectedIndex != -1) {
				NameValueCollection nameValueCollection = RequestedSongs.ElementAt(SongRequestList.SelectedIndex);
				if (nameValueCollection != null) {
					songRequestManager.RemoveSongFromRequestedListSystID(nameValueCollection["UniqueSystemID"]);
				}
			}
		}

		#endregion

		#region ### SECONDARY SONG MANAGER TAB ###

		public void UpdateSecPlaylistTabLists() {
			if (!songRequestManager.IsLoading) {
				if (!string.IsNullOrEmpty(SearchInputBox.Text) && (SongTitleCheckBox.Checked || YTCheckBox.Checked || RequesterCheckBox.Checked)) {
					SecondarySongs = songRequestManager.GetSecondaryPlaylist().Where(e => {
						if (SongTitleCheckBox.Checked) {
							return e.Get(nameof(SongDataContainer.Title)).ToLower().Contains(SearchInputBox.Text.ToLower());
						}
						if (YTCheckBox.Checked) {
							return e.Get(nameof(SongDataContainer.Link)).Contains(SearchInputBox.Text);
						}
						if (RequesterCheckBox.Checked) {
							return e.Get(nameof(SongDataContainer.OriginalRequester)).ToLower().Contains(SearchInputBox.Text.ToLower());
						}
						return false;
					}).ToList();
					BrokenSongs = songRequestManager.GetBrokenPlaylist().Where(e => {
						if (SongTitleCheckBox.Checked) {
							return e.Get(nameof(SongDataContainer.Title)).ToLower().Contains(SearchInputBox.Text.ToLower());
						}
						if (YTCheckBox.Checked) {
							return e.Get(nameof(SongDataContainer.Link)).Contains(SearchInputBox.Text);
						}
						if (RequesterCheckBox.Checked) {
							return e.Get(nameof(SongDataContainer.OriginalRequester)).ToLower().Contains(SearchInputBox.Text.ToLower());
						}
						return false;
					}).ToList();
				} else {
					SecondarySongs = songRequestManager.GetSecondaryPlaylist();
					BrokenSongs = songRequestManager.GetBrokenPlaylist();
				}

				int count = 0;
				if (SecondarySongs.Count > 0) {
					LoadedSongsListBox.ThreadSafeAction(e => {
						e.DataSource = SecondarySongs.Select(f => $"#{++count}. {f.Get(nameof(SongDataContainer.Title))}" +
						$"{(!string.IsNullOrEmpty(f.Get(nameof(SongDataContainer.LastPingFailed))) && bool.Parse(f.Get(nameof(SongDataContainer.LastPingFailed))) ? " - #PING FAILED / CACHE INTACT#" : "")}").ToList();
					});
				} else {
					LoadedSongsListBox.ThreadSafeAction(e => {
						e.DataSource = new List<string>();
					});
				}

				count = 0;
				if (BrokenSongs.Count > 0) {
					BrokenSongsListBox.ThreadSafeAction(e => {
						e.DataSource = BrokenSongs.Select(f => $"#{++count}. {f.Get(nameof(SongDataContainer.Title))}").ToList();
					});
				} else {
					BrokenSongsListBox.ThreadSafeAction(e => {
						e.DataSource = new List<string>();
					});
				}
			} else {
				LoadedSongsListBox.ThreadSafeAction(e => {
					e.DataSource = new List<string>();
				});
				BrokenSongsListBox.ThreadSafeAction(e => {
					e.DataSource = new List<string>();
				});
			}

			GlobalFunctions.ExecuteThreadSafeActionToMultiple(e => e.SelectedIndex = -1, LoadedSongsListBox, BrokenSongsListBox);
		}

		private void RemoveSecondarySongButton_Click(object sender, EventArgs e) {
			switch (SongListTabs.SelectedIndex) {
				case 0:
					if (LoadedSongsListBox.SelectedIndex < SecondarySongs.Count && LoadedSongsListBox.SelectedIndex != -1) {
						string value = SecondarySongs[LoadedSongsListBox.SelectedIndex].Get(nameof(SongDataContainer.UniqueSystemID));
						if (!string.IsNullOrEmpty(value)) {
							songRequestManager.RemoveSongByGenID(value);
							UpdateSecPlaylistTabLists();
						}
					}
					break;
				case 1:
					if (BrokenSongsListBox.SelectedIndex < BrokenSongs.Count && BrokenSongsListBox.SelectedIndex != -1) {
						string value = BrokenSongs[BrokenSongsListBox.SelectedIndex].Get(nameof(SongDataContainer.UniqueSystemID));
						if (!string.IsNullOrEmpty(value)) {
							songRequestManager.RemoveSongByGenID(value);
							UpdateSecPlaylistTabLists();
						}
					}
					break;
			}

		}

		private async void AddSecondarySongButton_Click(object sender, EventArgs e) {
			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = false, RetryBrokenSongButton, AddSecondarySongButton, RemoveSecondarySongButton, ClaimSongButton, ClaimAllSongsButton, WriteUpdatedSongInfoToFileButton);

			string link = SecondaryTextBoxAddField.Text.Trim();
			string requester = ProgramSettings.AppSettings.UserName;

			if (string.IsNullOrEmpty(requester)) {
				requester = Interaction.InputBox("Twitch Login currently empty, please insert a requester name for the song.", "Requester field required.", "#NOT PROVIDED#");
			}

			Match match = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)").Match(link);
			if (match.Success) {
				// TODO : Support for local files
				await songRequestManager.AddSecondarySong(link, requester, false);
			} else {
				MessageBox.Show("Link does not appear to be a YouTube link, please check link and try again.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = true, RetryBrokenSongButton, RemoveSecondarySongButton, ClaimSongButton, ClaimAllSongsButton, WriteUpdatedSongInfoToFileButton);
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
			ClaimSongButton.ThreadSafeAction(e => e.Enabled = LoadedSongsListBox.SelectedIndex != -1);
			BrokenSongsListBox.ThreadSafeAction(x => x.SelectedIndex = -1);
			if (LoadedSongsListBox.SelectedIndex != -1 && songRequestManager.GetFromSecondaryByIndex(LoadedSongsListBox.SelectedIndex, out NameValueCollection data)) {
				SecondaryTextBoxAddField.Text = data.Get(nameof(SongDataContainer.Link));
				SongDetailsListBox.DataSource = new List<string>() {
					$"Link:\t\t{data.Get(nameof(SongDataContainer.Link))}",
					$"Title:\t\t{data.Get(nameof(SongDataContainer.Title))}",
					$"Original Requester:\t{data.Get(nameof(SongDataContainer.OriginalRequester))}",
					$"Length:\t\t{data.Get(nameof(SongDataContainer.LengthInTime))}",
					$"Ping Valid:\t{data.Get(nameof(SongDataContainer.PingValid))}",
					$"Last Valid Ping:\t{data.Get(nameof(SongDataContainer.LastValidPing))}",
					$"Caching Enabled:\t{data.Get(nameof(SongDataContainer.AllowCaching))}",
				};
			} else if (LoadedSongsListBox.SelectedIndex == -1) {
				SecondaryTextBoxAddField.Text = "";
				SongDetailsListBox.DataSource = new List<string>() { };
			}
		}

		private void BrokenSongsListBox_SelectedIndexChanged(object sender, EventArgs e) {
			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = BrokenSongsListBox.SelectedIndex != -1, RetryBrokenSongButton, ClaimSongButton);
			LoadedSongsListBox.ThreadSafeAction(x => x.SelectedIndex = -1);
			if (BrokenSongsListBox.SelectedIndex != -1 && songRequestManager.GetFromBrokenByIndex(BrokenSongsListBox.SelectedIndex, out NameValueCollection data)) {
				SecondaryTextBoxAddField.Text = data.Get(nameof(SongDataContainer.Link));
				SongDetailsListBox.DataSource = new List<string>() {
					$"Link:\t\t{data.Get(nameof(SongDataContainer.Link))}",
					$"Title:\t\t{data.Get(nameof(SongDataContainer.Title))}",
					$"Original Requester:\t{data.Get(nameof(SongDataContainer.OriginalRequester))}",
					$"Length:\t\t{data.Get(nameof(SongDataContainer.LengthInTime))}",
					$"Ping Valid:\t{data.Get(nameof(SongDataContainer.PingValid))}",
					$"Last Valid Ping:\t{data.Get(nameof(SongDataContainer.LastValidPing))}",
					$"Caching Enabled:\t{data.Get(nameof(SongDataContainer.AllowCaching))}",
				};
			} else if (BrokenSongsListBox.SelectedIndex == -1) {
				SecondaryTextBoxAddField.Text = "";
				SongDetailsListBox.DataSource = new List<string>() { };
			}
		}

		private void ClaimSongButton_Click(object sender, EventArgs e) {
			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = false, BrokenSongsListBox, LoadedSongsListBox);

			string requester = string.IsNullOrEmpty(ProgramSettings.AppSettings.UserName) ?
				Interaction.InputBox("Twitch Login currently empty, please insert a requester name.", "Requester field needed.", "#NOT PROVIDED#") :
				ProgramSettings.AppSettings.UserName;

			switch (SongListTabs.SelectedIndex) {
				case 0:
					if (LoadedSongsListBox.SelectedIndex < SecondarySongs.Count && LoadedSongsListBox.SelectedIndex != -1) {
						string value = SecondarySongs[LoadedSongsListBox.SelectedIndex].Get(nameof(SongDataContainer.UniqueSystemID));
						if (!string.IsNullOrEmpty(value)) {
							songRequestManager.ClaimSong(requester, value);
						}
					}
					break;
				case 1:
					if (BrokenSongsListBox.SelectedIndex < BrokenSongs.Count && BrokenSongsListBox.SelectedIndex != -1) {
						string value = BrokenSongs[BrokenSongsListBox.SelectedIndex].Get(nameof(SongDataContainer.UniqueSystemID));
						if (!string.IsNullOrEmpty(value)) {
							songRequestManager.ClaimSong(requester, value);
						}
					}
					break;
			}

			UpdateSecPlaylistTabLists();
			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = true, BrokenSongsListBox, LoadedSongsListBox);
		}

		private void ClaimAllSongsButton_Click(object sender, EventArgs e) {
			GlobalFunctions.ExecuteThreadSafeActionToMultiple(x => x.Enabled = false, BrokenSongsListBox, LoadedSongsListBox);

			string requester = string.IsNullOrEmpty(ProgramSettings.AppSettings.UserName) ?
				Interaction.InputBox("Twitch Login currently empty, please insert a requester name.", "Requester field needed.", "#NOT PROVIDED#") :
				ProgramSettings.AppSettings.UserName;

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

		private void CacheSongsRadioBut_CheckedChanged(object sender, EventArgs e) {
			ProgramSettings.AppSettings.CacheNewSongs = CacheSongsRadioBut.Checked;
		}

		private void CacheSongButton_Click(object sender, EventArgs e) {
			switch (SongListTabs.SelectedIndex) {
				case 0:
					if (LoadedSongsListBox.SelectedIndex < SecondarySongs.Count && LoadedSongsListBox.SelectedIndex != -1) {
						string value = SecondarySongs[LoadedSongsListBox.SelectedIndex].Get(nameof(SongDataContainer.UniqueSystemID));
						if (!string.IsNullOrEmpty(value) && bool.TryParse(SecondarySongs[LoadedSongsListBox.SelectedIndex].Get(nameof(SongDataContainer.AllowCaching)), out bool tocache)) {
							songRequestManager.CacheSong(value, !tocache);
						}
					}
					break;
				case 1:
					if (BrokenSongsListBox.SelectedIndex < BrokenSongs.Count && BrokenSongsListBox.SelectedIndex != -1) {
						string value = BrokenSongs[BrokenSongsListBox.SelectedIndex].Get(nameof(SongDataContainer.UniqueSystemID));
						if (!string.IsNullOrEmpty(value) && bool.TryParse(SecondarySongs[LoadedSongsListBox.SelectedIndex].Get(nameof(SongDataContainer.AllowCaching)), out bool tocache)) {
							songRequestManager.CacheSong(value, !tocache);
						}
					}
					break;
			}
			UpdateSecPlaylistTabLists();
		}

		private void CacheALLSongsButton_Click(object sender, EventArgs e) {
			if (MessageBox.Show("This action will set all songs in the secondary playlist to cache, the app will immediatly start to download all songs caches. Is this ok", "Cache All Songs?", MessageBoxButtons.OKCancel) == DialogResult.OK) {
				songRequestManager.CacheAllSongs();
			}
		}

		private void ClearALLCacheButton_Click(object sender, EventArgs e) {
			if (MessageBox.Show("This action will set all songs in the secondary playlist to NOT cache, the app will immediatly delete all downloaded songs caches. Is this ok", "Clear All Song Caches?", MessageBoxButtons.OKCancel) == DialogResult.OK) {
				songRequestManager.ClearSongCaches();
			}
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
			UpdateSongListOutput();
		}

		private void SongRequestManager_OnProgressbarUpdate(object sender, float e) {

		}

		private void SongRequestManager_OnSecondaryPlaylistUpdated(object sender, EventArgs e) {
			UpdateSecPlaylistTabLists();
		}

		private void SongRequestManager_OnVolumeUpdate(object sender, int e) {
			VolumeLabel.ThreadSafeAction(v => v.Text = $"Volume: {e}");
		}

		private void SongRequestManager_OnMaxRequestsUpdate(object sender, int e) {
			MaxRequestsLabel.ThreadSafeAction(v => v.Text = $"Request Limit: {e}");
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

		}
		private void SearchInputBox_TextChanged(object sender, EventArgs e) {
			UpdateSecPlaylistTabLists();
		}

		private void SongTitleCheckBox_CheckedChanged(object sender, EventArgs e) {
			UpdateSecPlaylistTabLists();
		}

		private void YTCheckBox_CheckedChanged(object sender, EventArgs e) {
			UpdateSecPlaylistTabLists();
		}

		private void RequesterCheckBox_CheckedChanged(object sender, EventArgs e) {
			UpdateSecPlaylistTabLists();
		}

		private void SongRequestManager_OnSongListUpdated(object sender, EventArgs e) {
			UpdateSongListOutput();
		}

		private void SongRequestList_SelectedIndexChanged(object sender, EventArgs e) {
			RemoveSongRequestButton.Enabled = SongRequestList.SelectedItem != null;
		}

		#endregion

		#endregion

	}
}