using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using TwitchBotManager.Code.Classes;

using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;

namespace TwitchBotManager.Code {

	public enum SongRequestCommandType {
		CurrentSong,
		AddSong,
		RemoveSong,
		RemoveAllSongs,
		PrintSongList,
		PrintUserSongRequest,
		SkipSong,
		PauseSongRequests,
		PlaySongRequests,
		ClearSongRequests
	}

	public class BotCommandContainer : EventArgs {
		public SongRequestCommandType CommandType { get; private set; }
		public string User { get; private set; }
		public string Command { get; private set; }
		public object Additional { get; private set; }

		public BotCommandContainer(SongRequestCommandType commandType, string user, string command, object additional = null) {
			CommandType = commandType;
			User = user;
			Command = command;
			Additional = additional;
		}
	}

	public class TwitchBot {
		private Timer UpdateTimer;

		private readonly TwitchClient client;

		public static readonly string SongCommandPrefix = "!SR";

		public string TwitchUsername;
		public string TwitchOAuth;
		public string TargetChannel;

		public static string AboutText => "This is ScoredBot, to use !SR," +
						" insert a link or a command. For example type !SR help for a list of all commands." +
						" ScoredBot, created and maintained by ScoredOne. Download: https://github.com/ScoredOne/ScoredBot-Repository";


		public string[] BlacklistedWords = new string[] {

		};

		public event EventHandler<string> OnMessageReceived;

		public event EventHandler<BotCommandContainer> OnCurrentSong;
		public event EventHandler<BotCommandContainer> OnAddSong;
		public event EventHandler<BotCommandContainer> OnRemoveSong;
		public event EventHandler<BotCommandContainer> OnRemoveAllSongs;
		public event EventHandler<BotCommandContainer> OnPrintSongList;
		public event EventHandler<BotCommandContainer> OnPrintUserSongRequest;
		public event EventHandler<BotCommandContainer> OnSkipSong;
		public event EventHandler<BotCommandContainer> OnPauseSongRequests;
		public event EventHandler<BotCommandContainer> OnPlaySongRequests;
		public event EventHandler<BotCommandContainer> OnClearSongRequests;

		public event EventHandler<OnConnectionErrorArgs> OnConnectionError;

		public bool IsConnected => client.IsConnected;

		public bool IsActive { get; private set; } = false;

		// Removes large if chain from message processing, initialised in a private blank constructor
		private readonly Dictionary<string, Action<OnMessageReceivedArgs, List<string>>> CommandDictionary;

		private TwitchBot() {
			CommandDictionary = new Dictionary<string, Action<OnMessageReceivedArgs, List<string>>>(StringComparer.CurrentCultureIgnoreCase) {
				{"song", (e, f) => {
					OnCurrentSong?.Invoke(null, new BotCommandContainer(SongRequestCommandType.CurrentSong, e.ChatMessage.Username, null));
				}},
				{"remove", (e, f) => {
					if (f.Count > 2) {
						OnRemoveSong.Invoke(null, new BotCommandContainer(SongRequestCommandType.RemoveSong, e.ChatMessage.Username, f[2])); // Remove index
					} else {
						OnRemoveSong.Invoke(null, new BotCommandContainer(SongRequestCommandType.RemoveSong, e.ChatMessage.Username, null)); // Remove first
					}
				}},
				{"removeall", (e, f) => {
					OnRemoveAllSongs.Invoke(null, new BotCommandContainer(SongRequestCommandType.RemoveAllSongs, e.ChatMessage.Username, null)); // Remove all
				}},
				{"showlist", (e, f) => {
					OnPrintSongList.Invoke(null, new BotCommandContainer(SongRequestCommandType.PrintSongList, e.ChatMessage.Username, null));
				}},
				{"showmylist", (e, f) => {
					OnPrintUserSongRequest.Invoke(null, new BotCommandContainer(SongRequestCommandType.PrintUserSongRequest, e.ChatMessage.Username, null));
				}},
				{"skip", (e, f) => {
					if (e.ChatMessage.IsModerator) {
						OnSkipSong.Invoke(null, new BotCommandContainer(SongRequestCommandType.SkipSong, e.ChatMessage.Username, null));
					} else {
						client.SendMessage(TwitchUsername, e.ChatMessage.Username + " : You do not have permission to use this command.");
					}
				}},
				{"pause", (e, f) => {
					if (e.ChatMessage.IsModerator) {
						OnPauseSongRequests.Invoke(null, new BotCommandContainer(SongRequestCommandType.PauseSongRequests, e.ChatMessage.Username, null));
					} else {
						client.SendMessage(TwitchUsername, e.ChatMessage.Username + " : You do not have permission to use this command.");
					}
				}},
				{"play", (e, f) => {
					if (e.ChatMessage.IsModerator) {
						OnPlaySongRequests.Invoke(null, new BotCommandContainer(SongRequestCommandType.PlaySongRequests, e.ChatMessage.Username, null));
					} else {
						client.SendMessage(TwitchUsername, e.ChatMessage.Username + " : You do not have permission to use this command.");
					}
				}},
				{"clear", (e, f) => {
					if (e.ChatMessage.IsModerator) {
						OnClearSongRequests.Invoke(null, new BotCommandContainer(SongRequestCommandType.ClearSongRequests, e.ChatMessage.Username, null));
					} else {
						client.SendMessage(TwitchUsername, e.ChatMessage.Username + " : You do not have permission to use this command.");
					}
				}},
				{"help", (e, f) => {
					client.SendMessage(TwitchUsername, e.ChatMessage.Username +
							" : !SR commands: *media link* = Adds song to the song list ||" +
								" remove = Remove your latest song ||" +
								" showlist = Shows the next 5 songs in the song list ||" +
								" showmylist = Shows the songs you have added to the list ||" +
								" remove # = Remove song at index starting with earliest ||" +
								" removeall = Remove all songs you have added ||" +
								" (MOD) skip = Skips the current song ||" +
								" (MOD) pause = Pauses the current song ||" +
								" (MOD) clear = Wipes the current song list");
				}},
				{"", (e, f) => {
					client.SendMessage(TwitchUsername, AboutText); // Help text or bot info 
				}}
			};
		}

		public TwitchBot(string twitchusername, string twitchoauth, string targetchannel = null) : this() {
			TwitchUsername = twitchusername;
			TwitchOAuth = twitchoauth;
			TargetChannel = targetchannel;

			ConnectionCredentials credentials = new ConnectionCredentials(twitchusername, twitchoauth);
			ClientOptions clientOptions = new ClientOptions {
				MessagesAllowedInPeriod = 999999,
				ThrottlingPeriod = TimeSpan.FromSeconds(1),
				ReconnectionPolicy = new ReconnectionPolicy(1, 10)
			};

			WebSocketClient customClient = new WebSocketClient(clientOptions);
			client = new TwitchClient(customClient);

			//UpdateTimer = new Timer {
			//	Interval = 250 // 0.25 sec
			//};
			//UpdateTimer.Tick += new EventHandler(Update);
			//UpdateTimer.Start();

			Task.Run(() => { // App hangs without task
				client.Initialize(credentials, string.IsNullOrEmpty(targetchannel) ? twitchusername : targetchannel);

				client.OnLog += Client_OnLog;
				client.OnJoinedChannel += Client_OnJoinedChannel;
				client.OnMessageReceived += Client_OnMessageReceived;
				client.OnWhisperReceived += Client_OnWhisperReceived;

				client.OnNewSubscriber += Client_OnNewSubscriber;
				client.OnCommunitySubscription += Client_OnCommunitySubscription;
				client.OnContinuedGiftedSubscription += Client_OnContinuedGiftedSubscription;
				client.OnGiftedSubscription += Client_OnGiftedSubscription;
				client.OnReSubscriber += Client_OnReSubscriber;

				client.OnConnected += Client_OnConnected;
				client.OnReconnected += Client_OnReconnected;
				client.OnChatCleared += Client_OnChatCleared;
				client.OnConnectionError += Client_OnConnectionError;
				client.OnDisconnected += Client_OnDisconnected;
				client.OnError += Client_OnError;
				client.OnIncorrectLogin += Client_OnIncorrectLogin;

				client.OnBeingHosted += Client_OnBeingHosted;
				client.OnRaidNotification += Client_OnRaidNotification;

				client.AutoReListenOnException = true;

				client.Connect();

				IsActive = true;
			});
		}

		private void Update(object sender, EventArgs e) {

		}

		public void ConnectToChat() {
			if (!client.IsConnected && IsActive) {
				client.Connect();
			}
		}

		public void DisconnectFromChat() {
			if (client.IsConnected && IsActive) {
				client.Disconnect();
			}
		}

		public void ReconnectToChat() {
			if (client.IsConnected && IsActive) {
				client.Reconnect();
			}
		}

		private void Client_OnLog(object sender, OnLogArgs e) {
			Console.WriteLine($"{e.DateTime}: {e.BotUsername} - {e.Data}");
		}

		private void Client_OnConnected(object sender, OnConnectedArgs e) {
			IsActive = true;
			Console.WriteLine($"Connected to {e.AutoJoinChannel}");
		}

		private void Client_OnReconnected(object sender, OnReconnectedEventArgs e) {
			IsActive = true;
		}

		private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e) {
			Console.WriteLine("ScoredBot, successfully connected.");
			client.SendMessage(e.Channel, "ScoredBot, successfully connected.");
		}

		private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e) {
			string messagelog = DateTime.Now.ToString("MM/dd/yy - H:mm:ss zzz") + " || " +
				(e.ChatMessage.Username.Equals(TwitchUsername) ? "#ME > " : "") +
				e.ChatMessage.Username +
				" => ¬[ " + e.ChatMessage.Message + " ]¬" +
				(e.ChatMessage.IsStaff ? " | STAFF" : "") +
				(e.ChatMessage.IsModerator ? " | MOD" : "") +
				(e.ChatMessage.IsSubscriber ? " | SUB " + e.ChatMessage.SubscribedMonthCount : "") +
				(e.ChatMessage.IsVip ? " | VIP" : "");

			OnMessageReceived.Invoke(null, messagelog);

			List<string> MessageText = e.ChatMessage.Message.Split(' ').ToList();
			MessageText.RemoveAll(x => string.IsNullOrEmpty(x));

			if (MessageText[0].Equals(SongCommandPrefix, StringComparison.CurrentCultureIgnoreCase)) {
				if (MessageText.Count > 1) {
					if (CommandDictionary.ContainsKey(MessageText[1])) {
						CommandDictionary[MessageText[1]].Invoke(e, MessageText);
					} else {
						// Song request link
						ReceiveSongRequest(e.ChatMessage.Username, MessageText[1]);
					}
				} else {
					client.SendMessage(TwitchUsername, AboutText); // Help text or bot info 
				}
			}

			if (BlacklistedWords.Any(x => e.ChatMessage.Message.Contains(x))) {
				client.TimeoutUser(e.ChatMessage.Channel, e.ChatMessage.Username, TimeSpan.FromMinutes(30), "YOU HAD TO GO THERE! 30 minute timeout!");
			}
		}

		public async void ReceiveSongRequest(string user, string commandlink) {
			(string link, NameValueCollection SongData) = await GlobalFunctions.RegexYouTubeLink(commandlink);

			if (!string.IsNullOrEmpty(link) && SongData != null) {
				if (SongData["errors"] == null) {
					OnAddSong.Invoke(null, new BotCommandContainer(SongRequestCommandType.AddSong, user, link, SongData));
				} else {
					MainForm.StaticPostToDebug("Song request failed... " + link + " :: " + SongData["errors"]);
				}
			} else {
				client.SendMessage(TwitchUsername, user + " Your link wasn't recognised, please use links from YouTube.com to add song requests.");
			}
		}

		// TODO : Configure to receive song requests and help requests
		private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e) {
			if (e.WhisperMessage.Username == "my_friend") {
				client.SendWhisper(e.WhisperMessage.Username, "Hey! Whispers are so cool!!");
			}
		}

		#region ###Subscriber Notifications###

		private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e) {
			//if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime) {
			//    client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points! So kind of you to use your Twitch Prime on this channel!");
			//} else {
			//    client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points!");
			//}
		}

		private void Client_OnReSubscriber(object sender, OnReSubscriberArgs e) {

		}

		private void Client_OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e) {

		}

		private void Client_OnContinuedGiftedSubscription(object sender, OnContinuedGiftedSubscriptionArgs e) {

		}

		private void Client_OnCommunitySubscription(object sender, OnCommunitySubscriptionArgs e) {

		}

		#endregion

		private void Client_OnIncorrectLogin(object sender, OnIncorrectLoginArgs e) {
			// TODO : Message MainForm the login was invalid
		}

		private void Client_OnError(object sender, OnErrorEventArgs e) {
			// TODO : Message MainForm there was an error
		}

		private void Client_OnDisconnected(object sender, OnDisconnectedEventArgs e) {

		}

		private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e) {
			OnConnectionError.Invoke(sender, e);
			MessageBox.Show("A connection error has occured, please check your login details and try again.", "Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void Client_OnChatCleared(object sender, OnChatClearedArgs e) {

		}

		private void Client_OnRaidNotification(object sender, OnRaidNotificationArgs e) {
			// TODO : Message chat with a thank you and link to there Twitch
		}

		private void Client_OnBeingHosted(object sender, OnBeingHostedArgs e) {
			// TODO : Message chat with a thank you and link to there Twitch
		}

		public void SendMessageToTwitchChat(string message) {
			client.SendMessage(TwitchUsername, message);
		}

		private void PrintChatToLog() {

		}
	}
}
