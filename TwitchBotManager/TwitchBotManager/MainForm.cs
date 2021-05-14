﻿using System;
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

using Newtonsoft.Json;

using TwitchBotManager.Code;
using TwitchBotManager.Code.Classes;

using Timer = System.Windows.Forms.Timer; // System.Threading; conflict

namespace TwitchBotManager {

	public partial class MainForm : Form {

		public static bool IsExiting { get; private set; } = false;

		private Timer UpdateTimer;

		private bool Initialized;
		private bool SecSongListInitalized;

		private TwitchBot twitchBot;
		private TwitchAPICode twitchAPI;

		public (string UserName, string OAuth, string Target) TwitchBotLoginDetails { get; private set; } // UserName, oauth:xxxxxxxxxxxxxxxxxxxxxxxxxxxxx, TargetChannel

		public LibVLC _libVLC;
		public MediaPlayer _mp;
		public Media media;

		public bool isFullscreen = false;
		public bool isPlaying = false;
		public bool isSkipping = false;
		public Size oldVideoSize;
		public Size oldFormSize;
		public Point oldVideoLocation;

		public SongRequestData CurrentSong;
		public LinkedList<SongRequestData> Songlist = new LinkedList<SongRequestData>();
		public List<(SongRequestData SongData, bool Played)> SecondarySonglist = new List<(SongRequestData, bool)>();
		public List<SongRequestData> BrokenLinklist = new List<SongRequestData>();

		public bool SongRequestsSystem = false;
		public bool TakingSongRequests = false;

		public SongOutputText SongOutputText = new SongOutputText();

		private LinkedList<string> DebugListOut = new LinkedList<string>();
		public Action<string> PostToDebug => e => {
			if (DebugConsoleList != null && Initialized) {
				DebugListOut.AddFirst(DateTime.Now.ToString("yyyy-MM-dd / HH-mm-ss") + " :: " + e);
				LoadDebugMessages();
			}
		};

		private void LoadDebugMessages() {
			MainTabControl.ThreadSafeAction(e => {
				if (e.SelectedTab == DebugTab) {
					DebugConsoleList.ThreadSafeAction(e => {
						e.Items.Clear();
						try { // can be null for some unknown reason sometimes so this stops unexpected exceptions
							ListBox.ObjectCollection objectCollection = new ListBox.ObjectCollection(e, DebugListOut.ToArray()); // Currently the only way I know how to get to new posts to the top method...
						} catch { }
					});
				}
			});
		}

		public MainForm() {
			InitializeComponent();
			Initialized = true;

			//twitchAPI = new TwitchAPICode(); // Need more info to set this up

			Core.Initialize();

			KeyPreview = true;
			oldVideoSize = SongRequestVideoView.Size;
			oldFormSize = Size;
			oldVideoLocation = SongRequestVideoView.Location;

			//VLC stuff
			_libVLC = new LibVLC();
			_mp = new MediaPlayer(_libVLC);
			SongRequestVideoView.MediaPlayer = _mp;
			_mp.EndReached += Media_EndReached;

			Application.ApplicationExit += Application_ApplicationExit;

			PostToDebug.Invoke("ScoredBot Application Opened and Loaded");

			// Output Directory Data
			GlobalFunctions.CheckAndCreateOutputDirectoryFiles();
			LoadSecondaryPlaylist();
			TwitchBotLoginDetails = GlobalFunctions.LoadLoginFromFile();
			_mp.Volume = GlobalFunctions.LoadMediaPlayerVolume();
			VolumeLabel.Text = "Volume: " + _mp.Volume;

			Show();
		}

		private void Form1_Load(object sender, EventArgs e) {
			UpdateTimer = new Timer {
				Interval = 250 // 0.25 sec
			};
			UpdateTimer.Tick += new EventHandler(Update);
			UpdateTimer.Start();

			if (File.Exists(Directory.GetCurrentDirectory() + @"\Nothing.wav")) { // Tricks windows to add app to advanced audio settings on app load before song requests starts
				SoundPlayer soundPlayer = new SoundPlayer(Directory.GetCurrentDirectory() + @"\Nothing.wav"); // Works as long as an empty wav file exists
				soundPlayer.PlaySync();
				soundPlayer.Dispose();
			}
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

			PlayPauseButton.Text = _mp.IsPlaying ? "Pause" : "Play";

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

					SongRequestsSystem = false;
					TakingSongRequests = false;
				}

				SongSystemButton.Enabled = twitchBot.IsActive;
				RequestsButton.Enabled = twitchBot.IsActive;
			} else {
				BotStartStop.Enabled = !(string.IsNullOrEmpty(TwitchBotLoginDetails.UserName) || string.IsNullOrEmpty(TwitchBotLoginDetails.OAuth));

				ConnectionLabel.Text = "Disconnected";
				ConnectionLabel.ForeColor = Color.Red;

				BotStartStop.Text = "Start Bot";

				SongSystemButton.Enabled = false;
				RequestsButton.Enabled = false;
				SongRequestsSystem = false;
				TakingSongRequests = false;
			}

			if (SongRequestsSystem) {
				PlayPauseButton.Enabled = !isSkipping;
				StopPlaybackButton.Enabled = true;
				SkipSongButton.Enabled = _mp.Media != null;

				AddLinkButton.Enabled = true;
				RequestsButton.Enabled = true;

				if (string.IsNullOrEmpty(SongOutputText.OutputString)) {
					CurrentSongRequestLabel.ThreadSafeAction(e => e.Text = SongOutputText.InputString = "Song Requests Off");
				} else {
					CurrentSongRequestLabel.ThreadSafeAction(e => e.Text = SongOutputText.OutputString);
				}
			} else {
				PlayPauseButton.Enabled = false;
				StopPlaybackButton.Enabled = false;
				SkipSongButton.Enabled = false;

				AddLinkButton.Enabled = false;
				RequestsButton.Enabled = TakingSongRequests = false;

				CurrentSongRequestLabel.ThreadSafeAction(e => e.Text = SongOutputText.InputString = "Song Requests Off");
			}

			UserNameToolLabel.Text = string.IsNullOrEmpty(TwitchBotLoginDetails.UserName) ? "No User Name Found" : TwitchBotLoginDetails.UserName;
			OAuthToolLabel.Text = string.IsNullOrEmpty(TwitchBotLoginDetails.OAuth) ? "No OAuth Found" : TwitchBotLoginDetails.OAuth;
			TargetFoundLabel.Text = string.IsNullOrEmpty(TwitchBotLoginDetails.Target) ? "No Target Found" : TwitchBotLoginDetails.Target;

			SongSystemButton.Text = SongRequestsSystem ? "SongsSystem ON" : "SongsSystem OFF";
			RequestsButton.Text = TakingSongRequests ? "Requests ON" : "Requests OFF";

			RetryAllBrokenSongButton.Enabled = BrokenLinklist.Count > 0;
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
				twitchBot.OnPrintSongList += TwitchBot_OnPrintSongList;
				twitchBot.OnPrintUserSongRequest += TwitchBot_OnPrintUserSongRequest;
				twitchBot.OnRemoveAllSongs += TwitchBot_OnRemoveAllSongs;
				twitchBot.OnRemoveSong += TwitchBot_OnRemoveSong;
				twitchBot.OnSkipSong += TwitchBot_OnSkipSong;
				twitchBot.OnMessageReceived += TwitchBot_OnMessageReceived;

				PostToDebug.Invoke("Bot Started");
			} else if (twitchBot.IsActive) {
				if (twitchBot.IsConnected) {
					twitchBot.DisconnectFromChat();
					TwitchBot_OnClearSongRequests(null, null);
					SongRequestsSystem = TakingSongRequests = false;

					CurrentSongRequestLabel.ThreadSafeAction(e => e.Text = "Song Requests Off");
					PostToDebug.Invoke("Bot Stopped");
				} else {
					twitchBot.ConnectToChat();
					PostToDebug.Invoke("Bot Started");
				}
			}

			BotStartStop.Enabled = true;
		}

		public void UpdateSongListOutput() {
			SongRequestList.ThreadSafeAction(e => e.Items.Clear());
			SongRequestData[] requestData = Songlist.ToArray();
			for (int x = 0; x < Songlist.Count; x++) {
				string lengthstring = "";
				if (requestData[x].LengthSec.HasValue) {
					if (requestData[x].LengthSec.Value < 60) {
						lengthstring = requestData[x].LengthSec.Value + " Seconds long.";
					} else {
						lengthstring = requestData[x].LengthSec.Value / 60 + " Minutes - " + requestData[x].LengthSec.Value % 60 + " Seconds long.";
					}
				}

				SongRequestList.ThreadSafeAction(e => e.Items.Add("#" + (x + 1) + " :: Requester : " + requestData[x].Requester));
				SongRequestList.ThreadSafeAction(e => e.Items.Add("Requested: " + (string.IsNullOrEmpty(requestData[x].Title) ? requestData[x].Link : requestData[x].Title)));
				SongRequestList.ThreadSafeAction(e => e.Items.Add(string.IsNullOrEmpty(lengthstring) ? "" : " : Length = " + lengthstring));
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
					LoadDebugMessages();
				} else if (tabs.SelectedTab == SecondaryPlaylistManagementTab) {
					UpdateSecPlaylistTabLists();
				}
			});
		}

		public void SetTwitchBotLoginDetails(string username, string oauth, string target) {
			TwitchBotLoginDetails = (username, oauth, target);

			File.WriteAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SavedLoginInfo.txt", new string[] { username, oauth, target });

			PostToDebug.Invoke("TwitchBotLoginDetails Set and saved");
		}

		#endregion

		#region ##### Song Request Tabs and Logic	#####

		#region ### TWITCH EVENT HANDLERS ###

		private void TwitchBot_OnCurrentSong(object sender, BotCommandContainer e) {
			if (!SongRequestsSystem) {
				twitchBot.SendMessageToTwitchChat("Song requests currently disabled.");
				return;
			}

			if (string.IsNullOrEmpty(Songlist.First.Value.Title)) {
				twitchBot.SendMessageToTwitchChat("Current Song :: " + Songlist.First.Value.Requester + " with " + Songlist.First.Value.Link);
			} else {
				string lengthstring = "";
				if (Songlist.First.Value.LengthSec.HasValue) {
					if (Songlist.First.Value.LengthSec.Value < 60) {
						lengthstring = " at " + Songlist.First.Value.LengthSec.Value + " Seconds long.";
					} else {
						lengthstring = " at " + Songlist.First.Value.LengthSec.Value / 60 + " Minutes - " + Songlist.First.Value.LengthSec.Value % 60 + " Seconds long.";
					}
				}

				twitchBot.SendMessageToTwitchChat("Current Song :: " + Songlist.First.Value.Requester + " with " + Songlist.First.Value.Title + " {" + Songlist.First.Value.Link + "}" + lengthstring);
			}
		}

		private void TwitchBot_OnAddSong(object sender, BotCommandContainer e) {
			if (!SongRequestsSystem) {
				twitchBot.SendMessageToTwitchChat("Song requests currently disabled.");
				return;
			} else if (!TakingSongRequests && !e.User.Equals(TwitchBotLoginDetails.UserName)) {
				twitchBot.SendMessageToTwitchChat("Sorry, user requests are currently diabled.");
				return;
			}

			if (e.Additional != null && e.Additional is NameValueCollection collection && collection["title"] != null && collection["lengthSeconds"] != null) {
				int songlength = int.Parse(collection["lengthSeconds"]);

				Songlist.AddLast(new SongRequestData(e.Command, e.User, collection["title"], songlength));
				twitchBot.SendMessageToTwitchChat(e.User + " Has Requested " + collection["title"] + (songlength < 60 ? " at " + songlength + " Seconds long." : (" at " + songlength / 60 + " Minutes - " + songlength % 60 + " Seconds long.")));
			} else {
				twitchBot.SendMessageToTwitchChat(e.User + " Sorry, the request " + e.Command + " was a bad link and not added to the song requests.");
			}


			if (!_mp.IsPlaying && Songlist.Count > 0) {
				PlayMedia();
			}
			UpdateSongListOutput();
		}

		private void TwitchBot_OnClearSongRequests(object sender, BotCommandContainer e) {
			_mp.Stop();
			if (_mp.Media != null) {
				_mp.Media.Dispose();
			}
			if (twitchBot != null && File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\CurrentSong.txt")) {
				File.WriteAllText(Directory.GetCurrentDirectory() + @"\Outputs\CurrentSong.txt", "");
			}
			Songlist.Clear();
		}

		private void TwitchBot_OnPauseSongRequests(object sender, BotCommandContainer e) {
			_mp.Pause();
		}

		private void TwitchBot_OnPrintSongList(object sender, BotCommandContainer e) {
			if (!SongRequestsSystem) {
				twitchBot.SendMessageToTwitchChat("Song requests currently disabled.");
				return;
			} else if (Songlist.Count < 2) {
				twitchBot.SendMessageToTwitchChat("Song request list is currently empty.");
				return;
			}

			SongRequestData[] songRequests = Songlist.ToArray();

			string Output = songRequests.Length > 4 ? "Song List, Next 5 songs :: " : "Song List, Next " + songRequests.Length + " songs :: ";

			for (int x = 1; x < 6 && x < songRequests.Length; x++) {
				if (string.IsNullOrEmpty(songRequests[x].Title)) {
					Output += "#" + x + " -- " + songRequests[x].Requester + " with " + songRequests[x].Link + " || ";
				} else {
					Output += "#" + x + " -- " + songRequests[x].Requester + " with " + songRequests[x].Title + " || ";
				}
			}

			twitchBot.SendMessageToTwitchChat(Output);
		}

		private void TwitchBot_OnPrintUserSongRequest(object sender, BotCommandContainer e) {
			if (!SongRequestsSystem) {
				twitchBot.SendMessageToTwitchChat("Song requests currently disabled.");
				return;
			} else if (Songlist.Count < 2) {
				twitchBot.SendMessageToTwitchChat("Song request list is currently empty.");
				return;
			}

			string Output = e.User + "'s song requests :: ";
			SongRequestData[] songRequests = Songlist.TakeWhile(x => x.Requester.Equals(e.User)).ToArray();

			for (int x = 0; x < songRequests.Length; x++) {
				if (string.IsNullOrEmpty(songRequests[x].Title)) {
					Output += "#" + x + " -- " + songRequests[x].Link + " ::";
				} else {
					Output += "#" + x + " -- " + songRequests[x].Title + " ::";
				}
			}

			twitchBot.SendMessageToTwitchChat(Output);
		}

		private void TwitchBot_OnRemoveAllSongs(object sender, BotCommandContainer e) {
			Songlist.Clear();

			twitchBot.SendMessageToTwitchChat(e.User + " Has cleared the song list.");
			UpdateSongListOutput();
		}

		private void TwitchBot_OnRemoveSong(object sender, BotCommandContainer e) {
			LinkedList<SongRequestData> requestData = new LinkedList<SongRequestData>(Songlist.TakeWhile(x => x.Requester.Equals(e.User)).ToList());

			if (requestData == null || requestData.Count == 0) {
				twitchBot.SendMessageToTwitchChat(e.User + " You don't have any songs to remove.");
			} else {
				if (string.IsNullOrEmpty(e.Command)) {
					Songlist.Remove(requestData.Last.Value);

					if (string.IsNullOrEmpty(requestData.Last.Value.Title)) {
						twitchBot.SendMessageToTwitchChat(e.User + " removed song: " + requestData.Last.Value.Link);
						PostToDebug.Invoke("Song Removed " + requestData.Last.Value.Link + " by requester " + requestData.Last.Value.Requester);
					} else {
						twitchBot.SendMessageToTwitchChat(e.User + " removed song: " + requestData.Last.Value.Title);
						PostToDebug.Invoke("Song Removed " + requestData.Last.Value.Title + " by requester " + requestData.Last.Value.Requester);
					}
				} else {
					if (e.Command.ToCharArray().All(x => char.IsDigit(x)) && int.TryParse(e.Command, out int result)) {
						if (result == 0) {
							twitchBot.SendMessageToTwitchChat(e.User + " remove value cant be 0.");
						} else {
							result--;
							if (result <= requestData.Count) {
								Songlist.Remove(requestData.ToArray()[result]);

								if (string.IsNullOrEmpty(requestData.ToArray()[result].Title)) {
									twitchBot.SendMessageToTwitchChat(e.User + " removed song: " + requestData.ToArray()[result].Link);
								} else {
									twitchBot.SendMessageToTwitchChat(e.User + " removed song: " + requestData.ToArray()[result].Title);
								}
							}
						}
					} else {
						twitchBot.SendMessageToTwitchChat(e.User + " remove value was not valid.");
					}
				}
			}

			UpdateSongListOutput();
		}

		private void TwitchBot_OnSkipSong(object sender, BotCommandContainer e) {
			_mp.Position = 1;
			twitchBot.SendMessageToTwitchChat(e.User + " Has skipped the song.");
			PostToDebug.Invoke("Song Skipped by " + e.User);
		}

		private void TwitchBot_OnMessageReceived(object sender, string e) {
			if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\CurrentChatLog.txt")) {
				File.AppendAllText(Directory.GetCurrentDirectory() + @"\Outputs\CurrentChatLog.txt", e + Environment.NewLine);
			}
		}

		#endregion

		#region ### VOLUME BUTTONS ###

		Timer IncreaseVolumeLooper;
		private void IncreaseVolumeButton_MouseDown(object sender, MouseEventArgs e) {
			IncreaseVolumeLooper = new Timer {
				Interval = 50 // 0.01 sec
			};
			IncreaseVolumeLooper.Tick += new EventHandler((s, e) => {
				if (_mp.Volume < 100) {
					_mp.Volume++;
					VolumeLabel.Text = "Volume: " + _mp.Volume;
				}
			});
			IncreaseVolumeLooper.Start();
		}

		private void IncreaseVolumeButton_MouseUp(object sender, MouseEventArgs e) {
			IncreaseVolumeLooper.Stop();
			IncreaseVolumeLooper.Dispose();
			IncreaseVolumeLooper = null;

			if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\MediaVolume.txt")) {
				File.WriteAllText(Directory.GetCurrentDirectory() + @"\Outputs\MediaVolume.txt", _mp.Volume.ToString());
			}
		}

		Timer DecreaseVolumeLooper;
		private void DecreaseVolumeButton_MouseDown(object sender, MouseEventArgs e) {
			DecreaseVolumeLooper = new Timer {
				Interval = 50 // 0.01 sec
			};
			DecreaseVolumeLooper.Tick += new EventHandler((s, e) => {
				if (_mp.Volume > 0) {
					_mp.Volume--;
					VolumeLabel.Text = "Volume: " + _mp.Volume;
				}
			});
			DecreaseVolumeLooper.Start();
		}

		private void DecreaseVolumeButton_MouseUp(object sender, MouseEventArgs e) {
			DecreaseVolumeLooper.Stop();
			DecreaseVolumeLooper.Dispose();
			DecreaseVolumeLooper = null;

			if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\MediaVolume.txt")) {
				File.WriteAllText(Directory.GetCurrentDirectory() + @"\Outputs\MediaVolume.txt", _mp.Volume.ToString());
			}

			PostToDebug.Invoke("Volume set to " + _mp.Volume.ToString());
		}

		#endregion

		#region ### MEDIA PLAYER AND SONG LOADER ###

		Random SecondarySongListRandomNumber;
		private async void PlayMedia() {
			if (Songlist.Count == 0 && SecondarySonglist.Count == 0) {
				PostToDebug.Invoke("Main playlist and secondary playlist are empty, please request some songs to start playing.");
				return;
			}

			// Get Current Song

			if (Songlist.Count > 0) {
				CurrentSong = Songlist.First.Value;
				Songlist.RemoveFirst();
			} else { // Get from secondary playlist
				if (SecondarySongListRandomNumber == null) { // Flush Random to provide a better Random experiance, Random can usually be repettative if left unflushed
					SecondarySongListRandomNumber = new Random();
					byte[] array = new byte[1000];
					SecondarySongListRandomNumber.NextBytes(array);
					array.ToList().ForEach(e => SecondarySongListRandomNumber.Next(e + 1));
				}

				if (SecondarySonglist.All(e => e.Played)) { // If all songs have been played, reset the playlist
					List<(SongRequestData, bool)> templist = new List<(SongRequestData, bool)>();
					SecondarySonglist.ForEach(e => templist.Add((e.SongData, false)));
					SecondarySonglist = templist;
				}

				int randomNumber = 0;
				do {
					randomNumber = SecondarySongListRandomNumber.Next(SecondarySonglist.Count - 1);
				} while (SecondarySonglist[randomNumber].Played); // Random until it finds a song that hasnt been played

				CurrentSong = SecondarySonglist[randomNumber].SongData;
				SecondarySonglist[randomNumber] = (SecondarySonglist[randomNumber].SongData, true);
			}

			SaveLinkButton.ThreadSafeAction(e => {
				e.Enabled = !SecondarySonglist.Any(x => x.SongData.Link.Equals(CurrentSong.Link));
				RemoveSongFromSecondaryButton.ThreadSafeAction(new Action<Control>(e => e.Enabled = !SaveLinkButton.Enabled)); // Ensures previous line was executed before working on this thread
			});

			// Output song details
			if (twitchBot != null) {
				GlobalFunctions.UpdateSongRequest(CurrentSongRequestLabel, SongOutputText.InputString = CurrentSong.ToString() + " ||" + twitchBot.SongCommandPrefix + "|| ");
			}

			// Play song
			try {
				var media = new Media(_libVLC, CurrentSong.Link, FromType.FromLocation);
				await media.Parse(MediaParseOptions.ParseNetwork);

				_mp.Media = media.SubItems.First();
				_mp.Play();

				PostToDebug.Invoke("Playing song: " + (string.IsNullOrEmpty(CurrentSong.Title) ? CurrentSong.Link : CurrentSong.Title));

			} catch (Exception exc) {
				Console.WriteLine(exc.Message);
				PostToDebug.Invoke(exc.Message);
				PostToDebug.Invoke("Song Failed to play, recovering and moving on to the next song.");
				PlayMedia();
			}

			isSkipping = false;
			UpdateSongListOutput();
		}

		private void LoadSecondaryPlaylist() {
			SecSongListInitalized = false;
			MainProgressBar.Value = 0;

			if (!File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt")) {
				GlobalFunctions.CheckAndCreateOutputDirectoryFiles();
				return;
			}

			List<SongRequestData> songRequestDatas = new List<SongRequestData>();
			File.ReadAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt").ToList().ForEach(e => {
				if (!string.IsNullOrEmpty(e)) {
					SongRequestData requestData = SongRequestData.ConvertJSONData(e);
					if (requestData != null) {
						songRequestDatas.Add(requestData);
					}
				}
			});

			List<Thread> ThreadList = new List<Thread>();
			SecondarySonglist.Capacity = songRequestDatas.Count;
			songRequestDatas.ForEach(e => {
				ThreadList.Add(new Thread(() => GetYouTubeWebData(e)));
			});

			if (songRequestDatas.Count > 0) {
				PostToDebug.Invoke("Secondary Playlist Loading with " + songRequestDatas.Count + " Songs.");

				ThreadList.ForEach(e => {
					e.Start();
				});

				Thread completechecker = new Thread(() => {
					// Progress Bar and completion checker
					while (ThreadList.Any(e => e.IsAlive)) {
						double alive = ThreadList.TakeWhile(e => !e.IsAlive).Count();
						double all = ThreadList.Count;

						int value = (int)Math.Round((alive / all) * 100d);

						MainProgressBar.ThreadSafeAction(f => f.Value = value);
					}
					PostToDebug.Invoke("Secondary Playlist Loaded. All Threads Completed.");
					SecSongListInitalized = true;
					UpdateSecPlaylistTabLists();

					MainProgressBar.ThreadSafeAction(f => f.Value = 0);

					// Write the updated details to the file
					WriteSongListsToFile();
				});
				completechecker.Start();

			} else {
				SecSongListInitalized = true;
				PostToDebug.Invoke("Secondary Playlist not loaded, either it is empty or it has been corrupted.");
			}
		}

		public void GetYouTubeWebData(SongRequestData song) {
			(string Link, NameValueCollection Details) RegexData = GlobalFunctions.RegexYouTubeLink(song.Link);

			if (!string.IsNullOrEmpty(RegexData.Link) && RegexData.Details != null) {
				if (RegexData.Details["title"] != null && RegexData.Details["lengthSeconds"] != null) {
					int songlength = int.Parse(RegexData.Details["lengthSeconds"]);

					SecondarySonglist.Add((new SongRequestData(RegexData.Link, song.Requester, RegexData.Details["title"], songlength), false));

					PostToDebug.Invoke("Secondary Playlist Song Loaded... " + RegexData.Details["title"]);
				}
			} else {
				BrokenLinklist.Add(song);
			}
		}

		public void WriteSongListsToFile() {
			List<string> refreshedList = new List<string>();
			SecondarySonglist.ForEach(e => {
				refreshedList.Add(JsonConvert.SerializeObject(e.SongData));
			});
			BrokenLinklist.ForEach(e => {
				refreshedList.Add(JsonConvert.SerializeObject(e));
			});
			refreshedList.Add(Environment.NewLine);
			File.WriteAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt", refreshedList);
			PostToDebug.Invoke("Secondary Song List successfully updated");
		}

		#endregion

		#region ### EVENTS ###

		private void Media_EndReached(object sender, EventArgs e) {
			_mp.Media.Dispose();
			PlayMedia();
		}

		private void PlayPauseButton_Click(object sender, EventArgs e) {
			if (_mp.IsPlaying) {
				if (_mp.CanPause) {
					_mp.Pause();

					GlobalFunctions.UpdateSongRequest(CurrentSongRequestLabel, SongOutputText.InputString = "Song Requests Paused");

					PostToDebug.Invoke("Song Requests Paused");
				}
			} else {
				if (_mp.WillPlay) {
					_mp.Play();

					GlobalFunctions.UpdateSongRequest(CurrentSongRequestLabel, SongOutputText.InputString = CurrentSong.ToString() + " ||" + twitchBot.SongCommandPrefix + "|| ");
				} else {
					PlayMedia();
				}

				PostToDebug.Invoke("Song Requests Playing");
			}
		}

		private void StopPlaybackButton_Click(object sender, EventArgs e) {
			_mp.Stop();

			GlobalFunctions.UpdateSongRequest(CurrentSongRequestLabel, SongOutputText.InputString = "Song Requests Stopped");

			PostToDebug.Invoke("Song Requests Stopped");
		}

		private void ClearListButton_Click(object sender, EventArgs e) {
			Songlist.Clear();
			UpdateSongListOutput();
			PostToDebug.Invoke("Song Requests list cleared");
		}

		private void AddLinkButton_Click(object sender, EventArgs e) {
			// Currently just adds a song to the song list, need a button or change this to do one for host queue
			if (twitchBot != null && twitchBot.IsActive && SongRequestsSystem) {
				twitchBot.ReceiveSongRequest(TwitchBotLoginDetails.UserName, AddSongToPlayTextBox.Text);
				PostToDebug.Invoke(AddSongToPlayTextBox.Text + " Link added to current requests");
			}
		}

		private void SongSystemButton_Click(object sender, EventArgs e) {
			SongRequestsSystem = !SongRequestsSystem;
			if (SongRequestsSystem) {

				PostToDebug.Invoke("SongRequestsSystem turned On");
			} else {
				TwitchBot_OnClearSongRequests(null, null);

				GlobalFunctions.UpdateSongRequest(CurrentSongRequestLabel, SongOutputText.InputString = "Song Requests Off");

				PostToDebug.Invoke("SongRequestsSystem turned Off");
			}
		}

		private void RequestsButton_Click(object sender, EventArgs e) {
			TakingSongRequests = !TakingSongRequests;

			PostToDebug.Invoke(TakingSongRequests ? "Song System set to taking requests" : "Song System set to not taking requests");
		}

		private void SaveLinkButton_Click(object sender, EventArgs e) {
			if (CurrentSong != null) {
				File.AppendAllText(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt", JsonConvert.SerializeObject(CurrentSong) + Environment.NewLine);
				PostToDebug.Invoke(CurrentSong.Title + " Saved to Secondary Playlist.");

				SaveLinkButton.Enabled = false;
				RemoveSongFromSecondaryButton.Enabled = true;
			}
		}

		private void RemoveSongFromSecondaryButton_Click(object sender, EventArgs e) {
			if (CurrentSong != null) {
				if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt")) {
					List<string> songs = File.ReadAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt").ToList();
					songs.RemoveAll(e => e.Contains(CurrentSong.Link));
					File.WriteAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt", songs);
					PostToDebug.Invoke(CurrentSong.Title + " Removed from Secondary Playlist.");

					SaveLinkButton.Enabled = true;
					RemoveSongFromSecondaryButton.Enabled = false;
				}
			}
		}

		private void SkipSongButton_Click(object sender, EventArgs e) {
			isSkipping = true;
			TwitchBot_OnSkipSong(null, new BotCommandContainer(SongRequestCommandType.SkipSong, TwitchBotLoginDetails.UserName, null));
		}

		#endregion

		#region ### SECONDARY SONG MANAGER TAB ###

		public void UpdateSecPlaylistTabLists() {
			if (SecSongListInitalized) {
				LoadedSongsListBox.ThreadSafeAction(e => {
					e.Items.Clear();
					List<string> songdataString = new List<string>();
					for (int x = 0; x < SecondarySonglist.Count; x++) {
						songdataString.Add(x + ". " + SecondarySonglist[x].SongData.ToString());
					}

					try { // Can be null for some unknown reason sometimes so this stops unexpected exceptions
						ListBox.ObjectCollection objectCollection = new ListBox.ObjectCollection(e, songdataString.ToArray());
					} catch { };
				});

				BrokenSongsListBox.ThreadSafeAction(e => {
					e.Items.Clear();
					List<string> songdataString = new List<string>();
					for (int x = 0; x < BrokenLinklist.Count; x++) {
						songdataString.Add(x + ". " + BrokenLinklist[x].ToString());
					}

					try { // Can be null for some unknown reason sometimes so this stops unexpected exceptions
						ListBox.ObjectCollection objectCollection = new ListBox.ObjectCollection(e, songdataString.ToArray());
					} catch { };
				});
			}
		}

		private void RemoveSecondarySongButton_Click(object sender, EventArgs e) {
			if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt")) {
				List<string> songs = File.ReadAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt").ToList();

				PostToDebug.Invoke(SecondarySonglist[LoadedSongsListBox.SelectedIndex].SongData.Title + " Removed from Secondary Playlist.");

				songs.RemoveAll(e => e.Contains(SecondarySonglist[LoadedSongsListBox.SelectedIndex].SongData.Link)); // Link persists compared to data changes
				File.WriteAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt", songs);

				SecondarySonglist.RemoveAll(e => e.SongData.Link.Contains(SecondarySonglist[LoadedSongsListBox.SelectedIndex].SongData.Link));

				UpdateSecPlaylistTabLists();
			}
		}

		private void AddSecondarySongButton_Click(object sender, EventArgs e) {
			string link = SecondaryTextBoxAddField.Text.Trim();
			string requester = TwitchBotLoginDetails.UserName;

			if (string.IsNullOrEmpty(requester)) {
				requester = Interaction.InputBox("Twitch Login currently empty, please insert a requester name.", "Requester field needed.", "#NOT PROVIDED#");
			}

			Match match = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)").Match(link);
			if (match.Success) {
				if (SecondarySonglist.Any(x => x.SongData.Link.Contains(match.Value)) || BrokenLinklist.Any(x => x.Link.Contains(match.Value))) {
					MessageBox.Show("Link appears to be already found in the secondary requests.", "Song Already Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
				} else {
					Task task = new Task(() => GetYouTubeWebData(new SongRequestData(link, requester)));
					task.ContinueWith((e) => {
						PostToDebug.Invoke(link + " Link added to Secondary Song Requests.");
						UpdateSecPlaylistTabLists(); 
						WriteSongListsToFile();
					});
					task.Start();
				}
			} else {
				MessageBox.Show("Link does not appear to be a YouTube link, please check link and try again.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			GlobalFunctions.ExecuteMultipleThreadSafeActions(e => e.Enabled = false, RemoveBrokenSongButton, RetryBrokenSongButton, AddSecondarySongButton, RemoveSecondarySongButton, ClaimSongButton);
		}

		private void RemoveBrokenSongButton_Click(object sender, EventArgs e) {
			if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt")) {
				List<string> songs = File.ReadAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt").ToList();

				songs.RemoveAll(e => e.Contains(BrokenLinklist[BrokenSongsListBox.SelectedIndex].Link)); // Link persists compared to data changes

				File.WriteAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt", songs);

				PostToDebug.Invoke(BrokenLinklist[BrokenSongsListBox.SelectedIndex].Title + " Removed from Secondary Broken Playlist.");
				BrokenLinklist.RemoveAt(BrokenSongsListBox.SelectedIndex);

				UpdateSecPlaylistTabLists();

				GlobalFunctions.ExecuteMultipleThreadSafeActions(e => e.Enabled = false, RemoveBrokenSongButton, RetryBrokenSongButton, AddSecondarySongButton, RemoveSecondarySongButton, ClaimSongButton);
			}
		}

		private void RetryBrokenSongButton_Click(object sender, EventArgs e) {
			SongRequestData songRequest = BrokenLinklist[BrokenSongsListBox.SelectedIndex];
			BrokenLinklist.RemoveAt(BrokenSongsListBox.SelectedIndex);

			Task task = new Task(() => GetYouTubeWebData(songRequest));
			task.ContinueWith((e) => UpdateSecPlaylistTabLists());
			task.Start();
		}

		private void RetryAllBrokenSongButton_Click(object sender, EventArgs e) {
			List<SongRequestData> songRequests = new List<SongRequestData>();
			BrokenLinklist.ForEach(x => songRequests.Add(x));
			BrokenLinklist.Clear();

			List<Thread> ThreadList = new List<Thread>();
			songRequests.ForEach(x => {
				ThreadList.Add(new Thread(() => GetYouTubeWebData(x)));
			});
			ThreadList.ForEach(x => x.Start());

			Thread completechecker = new Thread(() => {
				// Progress Bar and completion checker
				while (ThreadList.Any(e => e.IsAlive)) {
					double alive = ThreadList.TakeWhile(e => !e.IsAlive).Count();
					double all = ThreadList.Count;

					int value = (int)Math.Round((alive / all) * 100d);

					MainProgressBar.ThreadSafeAction(f => f.Value = value);
				}
				PostToDebug.Invoke("Secondary Playlist Loaded. All Threads Completed.");
				UpdateSecPlaylistTabLists();

				MainProgressBar.ThreadSafeAction(f => f.Value = 0);

				// Write the updated details to the file
				WriteSongListsToFile();
			});
			completechecker.Start();
		}

		private void LoadedSongsListBox_SelectedIndexChanged(object sender, EventArgs e) {
			GlobalFunctions.ExecuteMultipleThreadSafeActions(e => e.Enabled = LoadedSongsListBox.SelectedIndex != -1, RemoveSecondarySongButton, ClaimSongButton);
			GlobalFunctions.ExecuteMultipleThreadSafeActions(e => e.Enabled = false, RemoveBrokenSongButton, RetryBrokenSongButton, AddSecondarySongButton);
			if (LoadedSongsListBox.SelectedIndex != -1) {
				SecondaryTextBoxAddField.Text = SecondarySonglist[LoadedSongsListBox.SelectedIndex].SongData.Link;
			}
		}

		private void BrokenSongsListBox_SelectedIndexChanged(object sender, EventArgs e) {
			GlobalFunctions.ExecuteMultipleThreadSafeActions(e => e.Enabled = false, RemoveSecondarySongButton, AddSecondarySongButton);
			GlobalFunctions.ExecuteMultipleThreadSafeActions(e => e.Enabled = BrokenSongsListBox.SelectedIndex != -1, RemoveBrokenSongButton, RetryBrokenSongButton, ClaimSongButton);
			if (BrokenSongsListBox.SelectedIndex != -1) {
				SecondaryTextBoxAddField.Text = BrokenLinklist[BrokenSongsListBox.SelectedIndex].Link;
			}
		}

		private void ClaimSongButton_Click(object sender, EventArgs e) {
			if (LoadedSongsListBox.SelectedIndex != -1) {
				SecondarySonglist[LoadedSongsListBox.SelectedIndex].SongData.Requester = TwitchBotLoginDetails.UserName;
			} else if (BrokenSongsListBox.SelectedIndex != -1) {
				BrokenLinklist[BrokenSongsListBox.SelectedIndex].Requester = TwitchBotLoginDetails.UserName;
			}
			UpdateSecPlaylistTabLists();
			WriteSongListsToFile();
		}

		private void ClaimAllSongsButton_Click(object sender, EventArgs e) {
			SecondarySonglist.ForEach(e => {
				e.SongData.Requester = TwitchBotLoginDetails.UserName;
			});
			BrokenLinklist.ForEach(e => {
				e.Requester = TwitchBotLoginDetails.UserName;
			});
			UpdateSecPlaylistTabLists();
			WriteSongListsToFile();
		}

		private void SecondaryTextBoxAddField_TextChanged(object sender, EventArgs e) {
			AddSecondarySongButton.ThreadSafeAction(x => x.Enabled = true);
		}

		#endregion

		#endregion
	}
}