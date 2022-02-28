using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using LibVLCSharp.Shared;

using Newtonsoft.Json;

using ScoredBot.Settings;

using YoutubeDLSharp;

namespace ScoredBot.Code.Classes {

	public class TwitchRequestedSong {
		public SongDataContainer SongData { get; private set; }
		public string Requester { get; private set; }

		public TwitchRequestedSong(SongDataContainer Song, string requester) {
			SongData = Song;
			Requester = requester;
		}

		public string OutputString(bool newLines = false) {
			return SongData.OutputString(Requester, newLines);
		}
	}

	public class SongRequestManager {

		#region ### Variables ###

		private bool Operating;

		private LibVLC _libVLC;

		private string VLCAudioDevice;

		private MediaPlayer VLCPlayer;

		private Media media;

		/// <summary>
		/// Current song playing in the bot
		/// </summary>
		private TwitchRequestedSong CurrentSong;


		/// <summary>
		/// Primary requested song list.
		/// </summary>
		private readonly SongListContainer<TwitchRequestedSong> Songlist;
		public class SongListContainer<TwitchRequestedSong> {
			public readonly LinkedList<TwitchRequestedSong> Requests = new LinkedList<TwitchRequestedSong>();

			private Action EventTrigger;

			public SongListContainer(Action eventtrigger) {
				EventTrigger = eventtrigger;
			}

			public void Clear() {
				Requests.Clear();
				EventTrigger.Invoke();
			}

			public void AddFirst(TwitchRequestedSong item) {
				Requests.AddFirst(item);
				EventTrigger.Invoke();
			}

			public void AddLast(TwitchRequestedSong item) {
				Requests.AddLast(item);
				EventTrigger.Invoke();
			}

			public void RemoveFirst() {
				Requests.RemoveFirst();
				EventTrigger.Invoke();
			}

			public void RemoveLast() {
				Requests.RemoveLast();
				EventTrigger.Invoke();
			}

			public void Remove(TwitchRequestedSong item) {
				Requests.Remove(item);
				EventTrigger.Invoke();
			}
		}

		/// <summary>
		/// Stored and managed backup requests. Info saved to SongRequestData.txt
		/// </summary>
		private readonly Dictionary<SongDataContainer, bool> SecondarySongPlaylist = new Dictionary<SongDataContainer, bool>();

		private bool SecSonglistAvailable => SecondarySongPlaylist.Count != 0;

		private readonly List<SongDataContainer> BrokenLinklist = new List<SongDataContainer>();

		private YoutubeDL YoutubeDLWorker;

		private string OutputDir = Directory.GetCurrentDirectory() + @"\Outputs\CachedSongs\";

		public readonly SongOutputText SongOutputText = new SongOutputText();

		private bool takingSongRequests;
		public bool TakingSongRequests {
			get => takingSongRequests;
			set {
				takingSongRequests = value;
				OnTakingSongRequestsChanged.SafeInvoke(this, true);
			}
		}

		public int SongCacheAmount { get; set; }

		public bool IsLoading { get; private set; } = false;

		public bool IsPlaying { get; private set; } = false;

		public bool IsStopped { get; private set; } = true;

		public bool IsBuffering { get; private set; } = false;

		public bool IsTranstioning { get; private set; } = false;

		public bool IsSecondary { get; private set; } = false;

		public bool PlaylistLoadError { get; private set; } = false;

		/// <summary>
		/// Number of requests to cache.
		/// null = unlimited | 0 = none
		/// </summary>
		public int? RequestsCacheAmount { get; private set; } = 3;

		public string PlayerVolumeTextOutput => VLCPlayer == null ? "Volume: N/A" : "Volume: " + VLCPlayer.Volume;

		public bool PlayerAlive => _libVLC != null && VLCPlayer != null && media != null;

		#region ### Events ###

		public event EventHandler<string> OnSongRequestOutputChanged;

		public event EventHandler OnSecondaryPlaylistUpdated;

		public event EventHandler<bool> OnNextSong;

		public event EventHandler<bool> OnPlay;

		public event EventHandler<bool> OnStopped;

		public event EventHandler<bool> OnPaused;

		public event EventHandler<bool> OnBuffering;

		public event EventHandler<bool> OnTranstioning;

		public event EventHandler<bool> OnTakingSongRequestsChanged;

		public event EventHandler<string> OnError;

		public event EventHandler<string> OnDisposing;

		public event EventHandler<string> OnDisposed;

		public event EventHandler<float> OnProgressbarUpdate;

		public event EventHandler<int> OnVolumeUpdate;

		public event EventHandler<int> OnMaxRequestsUpdate;

		public event EventHandler<(int LiteralPosition, string TranslatedTime)> OnPlayerProgress;

		public event EventHandler OnSongListUpdated;

		#endregion

		#endregion

		/// <summary>
		/// Once Events are populated, call Initialize to start operating
		/// </summary>
		public SongRequestManager() {
			Songlist = new SongListContainer<TwitchRequestedSong>(() => { OnSongListUpdated.Invoke(this, EventArgs.Empty); });
		}

		/// <summary>
		/// Can only be called once, must be called to start operating
		/// </summary>
		/// <returns></returns>
		public bool Initialize() {
			if (Operating) {
				throw new Exception("An attempt to Initialize again was made, this is not allowed.");
			} else {
				try {
					// TODO : add optional file path installation - Disable related functionality if error
					YoutubeDLWorker = new YoutubeDL(10) { // Python restricted to a maximum of ~100
						YoutubeDLPath = Directory.GetCurrentDirectory() + @"\youtube-dl\youtube-dl.exe",
						FFmpegPath = Directory.GetCurrentDirectory() + @"\ffmpeg\ffmpeg.exe",
						OverwriteFiles = true,
						IgnoreDownloadErrors = true,
						OutputFolder = OutputDir,
						OutputFileTemplate = "%(id)s - %(title)s.%(ext)s",
						RestrictFilenames = false
					};

					LoadSecondaryPlaylistFromFile();

					if (PlaylistLoadError) {
						MessageBox.Show("An error occured reading the song list file, Secondary Playlist editing and functions have been disabled to prevent cache loss. To resolve please check songlist file for errors.", "Songlist Read Error");
					}

					EstablishMediaPlayer();

					Operating = true;
				} catch (Exception e) {
					MainForm.StaticPostToDebug(e.Message);

					Operating = false;
				}

				return Operating;
			}
		}

		~SongRequestManager() {
			OnDisposing.SafeInvoke(this, "");

			Operating = false;
			_libVLC?.Dispose();
			VLCPlayer?.Dispose();
			media?.Dispose();
			ClearAllCache(false);

			OnDisposed.SafeInvoke(this, "");
		}

		public void Play() {
			if (!PlayerAlive) {
				EstablishMediaPlayer();
			}

			MediaPlayerPlay();

			OnPlay.SafeInvoke(this, IsPlaying);
		}

		public void Skip() {
			if (!PlayerAlive) {
				EstablishMediaPlayer();
			}
			MediaPlayerSkip();

			OnTranstioning.SafeInvoke(this, IsTranstioning);
		}

		public void Pause() {
			if (!PlayerAlive) {
				EstablishMediaPlayer();
			}
			MediaPlayerPause();

			OnPaused.SafeInvoke(this, !IsPlaying);
		}

		public void Stop() {
			if (!PlayerAlive) {
				IsStopped = true;
				IsPlaying = false;
				return;
			}
			MediaPlayerStop();

			OnStopped.SafeInvoke(this, IsStopped);
		}

		public void RestartPlayer() {
			EstablishMediaPlayer();
		}

		/// <summary>
		/// example input: Directory.GetCurrentDirectory() + @"\Outputs\CachedSongs\"
		/// </summary>
		/// <param name="dir"></param>
		public void UpdateCacheDirectory(string dir) {
			OutputDir = dir;
			if (YoutubeDLWorker != null) {
				YoutubeDLWorker.OutputFolder = dir;
			}
		}

		private void DisposeVideoPlayer() {
			if (VLCPlayer != null) {
				VLCPlayer.Stop();
				VLCPlayer.Dispose();
			}
			if (_libVLC != null) {
				_libVLC.Dispose();
			}

			_libVLC = null;
			VLCPlayer = null;
		}

		private void EstablishMediaPlayer() {
			DisposeVideoPlayer();

			_libVLC = new LibVLC();

			VLCPlayer = new MediaPlayer(_libVLC) {
				FileCaching = 1000,
				NetworkCaching = 1000,
				EnableHardwareDecoding = true
			};

			SetVLCAudioOutput(VLCAudioDevice);

			VLCPlayer.EndReached += Media_EndReached;
			UpdatePlayerVolume();
			VLCPlayer.Buffering += VLCPlayer_Buffering;
			VLCPlayer.Playing += VLCPlayer_Playing;
			VLCPlayer.Paused += VLCPlayer_Paused;
			//VLCPlayer.PositionChanged += VLCPlayer_PositionChanged;
			//VLCPlayer.EncounteredError += VLCPlayer_EncounteredError;
		}

		public void UpdatePlayerVolume() {
			if (VLCPlayer != null) {
				VLCPlayer.Volume = ProgramSettings.AppSettings.AppMusicVolume;
			}
		}

		public IEnumerable<string> GetVLCAudioOutputs() {
			if (VLCPlayer == null) {
				return new string[0];
			} else {
				return VLCPlayer.AudioOutputDeviceEnum.Select(e => e.DeviceIdentifier);
			}
		}

		private bool SetVLCAudioOutput(string device) {
			if (VLCPlayer.AudioOutputDeviceEnum.Any(e => e.DeviceIdentifier.Equals(device))) {
				VLCPlayer.SetAudioOutput(VLCAudioDevice = device);
				MainForm.StaticPostToDebug($"SetVLCAudioOutput : VLC audio set to '{device}'.");
				return true;
			} else {
				MainForm.StaticPostToDebug($"SetVLCAudioOutput : Unable to find device '{device}', VLC audio unchanged.");
				return false;
			}
		}

		public void SetPlayerAudioOutput() {

		}

		private void VLCPlayer_Paused(object sender, EventArgs e) {
			IsPlaying = false;
		}

		private void VLCPlayer_Playing(object sender, EventArgs e) {
			IsBuffering = false;
			IsPlaying = true;
		}

		// Needs testing before keeping event alive on VLCPlayer.EncounteredError
		private void VLCPlayer_EncounteredError(object sender, EventArgs e) {
			OnError.SafeInvoke(this, "VLCPlayer Crashed, Rebuilding player and continuing.");
			EstablishMediaPlayer();
			ThreadPool.QueueUserWorkItem(_ => {
				PlayMedia();
			});
		}

		private void VLCPlayer_Buffering(object sender, MediaPlayerBufferingEventArgs e) {
			IsBuffering = true;
			OnBuffering.SafeInvoke(this, IsBuffering);
		}

		private async void Media_EndReached(object sender, EventArgs e) {
			// https://github.com/ZeBobo5/Vlc.DotNet/wiki/Vlc.DotNet-freezes-(don't-call-Vlc.DotNet-from-a-Vlc.DotNet-callback)

			await ManageCacheAfterMediaEnd();

			// Cant access song, IOExecption cant access file to delete cache

			ThreadPool.QueueUserWorkItem(_ => {
				media.Dispose();
				PlayMedia();
			});
		}

		#region ### MEDIA PLAYER AND SONG LOADER ###

		private bool MediaPlayerPlay() {
			if (!Operating || IsLoading) {
				return false;
			}

			if (VLCPlayer.WillPlay) {
				VLCPlayer.Play();

				OnSongRequestOutputChanged.SafeInvoke(this, SongOutputText.InputString = CurrentSong.OutputString() + " {" + TwitchBot.SongCommandPrefix + "} ");
			} else {
				PlayMedia();
			}

			IsPlaying = true;
			IsStopped = false;

			MainForm.StaticPostToDebug("Song Requests Playing");

			return true;
		}

		private bool MediaPlayerPause() {
			if (!Operating) {
				return false;
			}

			VLCPlayer.Pause();

			OnSongRequestOutputChanged.SafeInvoke(this, SongOutputText.InputString = "Song Requests Paused ");

			IsPlaying = false;

			MainForm.StaticPostToDebug("Song Requests Paused");

			return true;
		}

		private bool MediaPlayerStop() {
			VLCPlayer.Stop();

			OnSongRequestOutputChanged.SafeInvoke(this, SongOutputText.InputString = "Song Requests Stopped ");

			IsStopped = true;
			IsPlaying = false;

			MainForm.StaticPostToDebug("Song Requests Stopped");

			return true;
		}

		private bool MediaPlayerSkip() {
			if (!Operating) {
				return false;
			}
			VLCPlayer.Stop();
			MainForm.StaticPostToDebug("Song Request Skipped");

			PlayMedia();

			return true;
		}

		private Random SecondarySongListRandomNumber;
		private async void PlayMedia(SongDataContainer song = null) {
			if (song == null && Songlist.Requests.Count == 0 && SecondarySongPlaylist.Count == 0) {
				MainForm.StaticPostToDebug("Main playlist and secondary playlist are empty, please request songs to start playing.");
				IsSecondary = false;
				return;
			}

			// Play song
			try {
				OnTranstioning.SafeInvoke(this, IsTranstioning = true);

				if (song != null) {
					CurrentSong = new TwitchRequestedSong(song, song.OriginalRequester);
					IsSecondary = false;
				} else if (Songlist.Requests.Count > 0) {
					CurrentSong = Songlist.Requests.First.Value;
					IsSecondary = false;
				} else if (Songlist.Requests.Count == 0) { // Get from secondary playlist
					if (SecondarySongListRandomNumber == null) { // Flush Random to provide a better Random experiance, Random can usually be repettative if left unflushed
						SecondarySongListRandomNumber = new Random();
						byte[] array = new byte[1000];
						SecondarySongListRandomNumber.NextBytes(array);
						array.ToList().ForEach(e => SecondarySongListRandomNumber.Next(e + 1));
					}

					List<SongDataContainer> SecondaryKeys = SecondarySongPlaylist.Keys.ToList();

					if (SecondarySongPlaylist.All(e => e.Value)) { // If all songs have been played, reset the playlist
						foreach (SongDataContainer x in SecondaryKeys) {
							SecondarySongPlaylist[x] = false;
						}
					}

					int randomNumber = 0;
					do {
						randomNumber = SecondarySongListRandomNumber.Next(SecondarySongPlaylist.Count - 1);
					} while (SecondarySongPlaylist[SecondaryKeys[randomNumber]]); // Random until it finds a song that hasnt been played

					CurrentSong = new TwitchRequestedSong(SecondaryKeys[randomNumber], SecondaryKeys[randomNumber].OriginalRequester);
					SecondarySongPlaylist[SecondaryKeys[randomNumber]] = true;
					IsSecondary = true;
				}

				// Output song details
				OnNextSong.SafeInvoke(this, !SecondarySongPlaylist.Keys.Any(z => z.Link.Equals(CurrentSong.SongData.Link)));
				OnSongRequestOutputChanged.SafeInvoke(this, SongOutputText.InputString = CurrentSong.OutputString() + " {" + TwitchBot.SongCommandPrefix + "} ");

				if (Songlist.Requests.Count > 0 && !IsSecondary) {
					Songlist.RemoveFirst();
				}

				if (CurrentSong.SongData.LocalFile && !CurrentSong.SongData.AudioCached()) {
					throw new ArgumentException($"Local Song: {CurrentSong.SongData.DirLocation} Not found.");
				} else {
					int ESCAPE = 0;
					while (CurrentSong.SongData.DownloadWorking) {
						await Task.Delay(300);
						if (ESCAPE++ > 1000) {
							throw new ArgumentException($"Song : {CurrentSong.SongData.Title} Timed Out while waiting for song to download. (Assumed Downloader encountered an error)");
						}
					}

					if (!CurrentSong.SongData.AudioCached()) {
						await CurrentSong.SongData.GetYouTubeAudioData(YoutubeDLWorker);
						if (!CurrentSong.SongData.AudioCached()) {
							throw new ArgumentException($"Song : {CurrentSong.SongData.Title} Failed to find Cache.");
						}
					}
				}

				VLCPlayer.Media = media = new Media(_libVLC, CurrentSong.SongData.FullDirLocation);
				MediaParsedStatus parsedStatus = await VLCPlayer.Media.Parse(); // TODO : Parse fails on k6x1NVm6Tic ???

				if (parsedStatus == MediaParsedStatus.Done) {
					VLCPlayer.Play();
				} else {
					throw new FileLoadException($"Parse Failed for song request {CurrentSong.SongData.Title}");
				}

				MainForm.StaticPostToDebug("Playing song: " + (string.IsNullOrEmpty(CurrentSong.SongData.Title) ? CurrentSong.SongData.Link : CurrentSong.SongData.Title));

			} catch (Exception exc) {
				OnError.SafeInvoke(this, exc.Message);

				MainForm.StaticPostToDebug("Song Failed to play, recovering and moving on to the next song.");
				MainForm.StaticPostToDebug(exc.Message);

				if (CurrentSong.SongData.LocalFile && !CurrentSong.SongData.AudioCached()) {
					CurrentSong.SongData.LastPingFailed = true;

					SecondarySongPlaylist.Remove(CurrentSong.SongData);

					if (!BrokenLinklist.Contains(CurrentSong.SongData)) {
						BrokenLinklist.Add(CurrentSong.SongData);
					}

					MainForm.StaticPostToDebug("Secondary Playlist Song Data Failed... " + CurrentSong.SongData.DirLocation);
				} else if (!IsSecondary && Songlist.Requests.Count > 0) {
					Songlist.RemoveFirst();
				}
				PlayMedia();
			}

			IsTranstioning = false;
		}

		public void LoadSecondaryPlaylistFromFile() {
			OnSecondaryPlaylistUpdated.SafeInvoke(this, EventArgs.Empty);
			IsLoading = true;

			if (!File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt")) {
				ProgramSettings.CheckAndCreateOutputDirectoryFiles();
				return;
			}

			List<SongDataContainer> songRequestDatas = new List<SongDataContainer>();
			int linecount = 0;
			List<string> errorMessages = new List<string>();
			bool? providedvalueforcache = null;
			File.ReadAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt").ToList().ForEach(e => {
				linecount++;
				if (!string.IsNullOrEmpty(e)) {
					try {
						SongDataContainer requestData = SongDataContainer.ConvertFromJSONData(e, out List<string> missingentires);
						if (requestData != null) {
							songRequestDatas.Add(requestData);

							if (missingentires.Contains("AllowCaching")) {
								if (!providedvalueforcache.HasValue) {
									providedvalueforcache = MessageBox.Show("The cache setting for some songs is missing, would you like to set the bot to cache these songs?", "Cache Value", MessageBoxButtons.YesNo) == DialogResult.Yes;
								}
								if (providedvalueforcache.HasValue) {
									requestData.AllowCaching = providedvalueforcache.Value;
								}
							}
						}
					} catch {
						PlaylistLoadError = true;
						errorMessages.Add($"ERROR READING LINE {linecount}: {e}");
					}
				}
			});

			if (songRequestDatas.Count > 0) {
				MainForm.StaticPostToDebug($"Secondary Playlist Loading with {songRequestDatas.Count} Songs.");

				Task.WhenAll(songRequestDatas.Select(x => GetYouTubeWebData(x)))
					.ContinueWith(x => {
						MainForm.StaticPostToDebug($"Secondary Playlist Data Loaded. All Threads Completed. [{SecondarySongPlaylist.Count} Successful] - [{BrokenLinklist.Count} Failed]");

						MainForm.StaticPostToDebug($"Secondary Playlist Cache Loading with {SecondarySongPlaylist.Count} Songs.");

						Task.WhenAll(SecondarySongPlaylist.TakeWhile(e => e.Key.AllowCaching).Select(e => e.Key.GetYouTubeAudioData(YoutubeDLWorker)))
							.ContinueWith(x => {
								ProcessIDGeneration();

								MainForm.StaticPostToDebug($"Secondary Playlist Songs Loaded. All Threads Completed. " +
									$"[{SecondarySongPlaylist.Sum(y => y.Key.AudioCached() && y.Key.AllowCaching ? 1 : 0)} Successful] - " +
									$"[{SecondarySongPlaylist.Sum(y => !y.Key.AudioCached() && y.Key.AllowCaching ? 1 : 0)} Failed] - " +
									$"[{SecondarySongPlaylist.Sum(y => y.Key.AllowCaching ? 0 : 1)} Skipped]");

								if (!PlaylistLoadError) {
									WriteSongListsToFile(false);
								} else {
									errorMessages.ForEach(e => MainForm.StaticPostToDebug(e));
								}
								IsLoading = false;
								OnSecondaryPlaylistUpdated.SafeInvoke(this, EventArgs.Empty);
							});
					});
			} else {
				MainForm.StaticPostToDebug("Secondary Playlist not loaded, either it is empty or it has been corrupted.");
				IsLoading = false;
			}

		}

		public async Task<bool> PingSecondarySongInfo(int index) {
			if (index >= SecondarySongPlaylist.Count) {
				return false;
			}
			SongDataContainer song = SecondarySongPlaylist.Keys.ElementAt(index);
			if (song.LocalFile) {
				return File.Exists(song.DirLocation);
			} else {
				await GetYouTubeWebData(song);
				return true;
			}
		}

		public async Task PingAllSecondarySongInfo() {
			await Task.WhenAll(SecondarySongPlaylist.Select(e => GetYouTubeWebData(e.Key)));
		}

		public async Task PingAllBrokenSongInfo() {
			await Task.WhenAll(BrokenLinklist.Select(e => GetYouTubeWebData(e)));
		}

		public async Task<bool> PingBrokenSongInfo(int index) {
			if (index >= BrokenLinklist.Count) {
				return false;
			}
			SongDataContainer song = BrokenLinklist.ElementAt(index);
			if (song.LocalFile) {
				return File.Exists(song.DirLocation);
			} else {
				await GetYouTubeWebData(song);
				return true;
			}
		}

		private async Task GetYouTubeWebData(SongDataContainer song) {
			if (SecondarySongPlaylist.Any(e => e.Key == song) || BrokenLinklist.Any(e => e == song)) {
				return;
			}

			if (song.LocalFile) {
				if (song.AudioCached()) {
					BrokenLinklist.Remove(song);

					if (!SecondarySongPlaylist.ContainsKey(song)) {
						SecondarySongPlaylist.Add(song, false);
					}

					MainForm.StaticPostToDebug($"Secondary Playlist Song Data Loaded... {(song.LocalFile ? song.DirLocation : song.Title)}");
				} else {
					SecondarySongPlaylist.Remove(song);

					if (!BrokenLinklist.Contains(song)) {
						BrokenLinklist.Add(song);
					}

					MainForm.StaticPostToDebug($"Secondary Playlist Song Data Failed... {(song.LocalFile ? song.DirLocation : song.Title)}");
				}
			} else {
				await song.GetYouTubeVideoInformation(YoutubeDLWorker);

				if (!song.PingValid && song.AudioCached()) {
					BrokenLinklist.Remove(song);

					if (!SecondarySongPlaylist.ContainsKey(song)) {
						SecondarySongPlaylist.Add(song, false);
					}

					MainForm.StaticPostToDebug($"Secondary Playlist Song Data Loaded... *PING FAILED - LOCAL FILE FOUND* {(song.LocalFile ? song.DirLocation : song.Title)}");
				} else if (!string.IsNullOrEmpty(song.Link) && song.PingValid) {
					BrokenLinklist.Remove(song);

					if (!SecondarySongPlaylist.ContainsKey(song)) {
						SecondarySongPlaylist.Add(song, false);
					}

					MainForm.StaticPostToDebug($"Secondary Playlist Song Data Loaded... {(song.LocalFile ? song.DirLocation : song.Title)}");
				} else {
					SecondarySongPlaylist.Remove(song);

					if (!BrokenLinklist.Contains(song)) {
						BrokenLinklist.Add(song);
					}

					MainForm.StaticPostToDebug($"Secondary Playlist Song Data Failed... {(song.LocalFile ? song.DirLocation : song.Title)}");
				}
			}
		}

		public void WriteSongListsToFile(bool promptRequired) {
			if (SecSonglistAvailable) {
				if (promptRequired) {
					DialogResult prompt = MessageBox.Show("Are you sure you wish to overwrite the saved song list with what is in this App?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

					if (prompt != DialogResult.Yes) {
						MainForm.StaticPostToDebug("Song list write aborted: Prompt denied.");
						return;
					}
				}
				List<string> refreshedList = SecondarySongPlaylist.Select(x => JsonConvert.SerializeObject(x.Key)).ToList();
				refreshedList.AddRange(BrokenLinklist.Select(x => JsonConvert.SerializeObject(x)));

				File.WriteAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt", refreshedList);
				MainForm.StaticPostToDebug("Secondary Song List File successfully updated");
			} else {
				//MessageBox.Show("Confirm", "No songs are currently in the Secondary Playlist.", MessageBoxButtons.OK);
				MainForm.StaticPostToDebug("No songs are currently in the Secondary Playlist.");
			}
		}

		public void WriteSingleSongToFile(SongDataContainer song) {
			string output = JsonConvert.SerializeObject(song) + Environment.NewLine;
			File.AppendAllText(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt", output);
			MainForm.StaticPostToDebug("Secondary Song List successfully updated");
		}

		public async Task<string> SubmitSongRequest(string link, string requester, bool Force = false) {
			if (!Force && !TakingSongRequests) {
				return "Song requests currently disabled.";
			}

			if (GetRequesterRequestAmount(requester) >= ProgramSettings.AppSettings.AppMusicMaxRequests) {
				return $"@{requester} : You have currently requested the maximum amount per person. [{ProgramSettings.AppSettings.AppMusicMaxRequests}]";
			}

			GlobalFunctions.GetYouTubeVideoID(link, out string ID);

			foreach (TwitchRequestedSong song in Songlist.Requests.ToArray()) {
				if (song.SongData.Link.Contains(ID)) {
					Songlist.AddLast(new TwitchRequestedSong(song.SongData, requester));
					return $"@{requester} : {song.SongData.Title} was Successfully Requested.";
				}
			}

			foreach (SongDataContainer song in SecondarySongPlaylist.Keys) {
				if (song.Link.Contains(ID)) {
					Songlist.AddLast(new TwitchRequestedSong(song, requester));
					return $"@{requester} : {song.Title} was Successfully Requested.";
				}
			}

			foreach (SongDataContainer song in BrokenLinklist) {
				if (song.Link.Contains(ID)) {
					return $"@{requester} : {song.Title} Is a saved song but unfortunatly isnt working at the moment, sorry for the inconvenience.";
				}
			}

			bool getvideodata = true;
			if (RequestsCacheAmount.HasValue) {
				getvideodata = Songlist.Requests.Sum(e => {
					if (e.SongData.AudioCached()) {
						return 1;
					}
					return 0;
				}) < RequestsCacheAmount.Value;
			}

			SongDataContainer newsong = await SongDataContainer.CreateNewContainer(link, requester, YoutubeDLWorker, getvideodata);
			ProcessIDGeneration();

			if (newsong.PingValid) {
				Songlist.AddLast(new TwitchRequestedSong(newsong, requester));

				//await Task.WhenAll(Songlist.Take(RequestsCacheAmount > Songlist.Count ? Songlist.Count : RequestsCacheAmount.Value)
				//	.TakeWhile(e => !e.SongData.AudioCached() && !e.SongData.DownloadWorking)
				//	.Select(e => e.SongData.GetYouTubeAudioData(YoutubeDLWorker))
				//	.ToArray());

				return $"@{requester} : {newsong.Title} was Successfully Requested.";
			} else {
				return $"@{requester} : {newsong.Link} Request Failed. {newsong.ErrorMessage}";
			}
		}

		#endregion

		#region ### CACHE FUNCTIONALITY ###

		public void UpdateCacheOutputLocation(string output) {
			YoutubeDLWorker.OutputFolder = output;
		}

		/// <summary>
		/// When a song ends, the cache needs to be updated
		/// </summary>
		private async Task ManageCacheAfterMediaEnd() {
			try {
				media.Dispose();
				if (RequestsCacheAmount.HasValue) {
					int ToCacheValue = RequestsCacheAmount > Songlist.Requests.Count ? Songlist.Requests.Count : RequestsCacheAmount.Value;
					IEnumerable<SongDataContainer> TakenList = Songlist.Requests.Select(e => e.SongData).Take(ToCacheValue);

					if (!TakenList.Contains(CurrentSong.SongData) && !SecondarySongPlaylist.ContainsKey(CurrentSong.SongData)) {
						await CurrentSong.SongData.DeleteCache();
					}

					await Task.WhenAll(TakenList.TakeWhile(e => !e.LocalFile && !e.AudioCached())
						.Select(e => e.GetYouTubeAudioData(YoutubeDLWorker))
						.ToArray());
				} else {
					await Task.WhenAll(Songlist.Requests.Select(e => e.SongData).TakeWhile(e => !e.LocalFile && !e.AudioCached())
						.Select(e => e.GetYouTubeAudioData(YoutubeDLWorker))
						.ToArray());
				}
			} catch (Exception e) {
				MainForm.StaticPostToDebug($"ManageCacheAfterMediaEnd ERROR caught and recovered: {e.Message}");
			}

		}

		/// <summary>
		/// Removes all songs from cache except songs found in the secondary playlist
		/// </summary>
		public void ClearAllCache(bool protectRequests) {
			if (PlaylistLoadError) {
				return;
			}
			HashSet<SongDataContainer> ToProcess = new HashSet<SongDataContainer>();
			foreach (SongDataContainer song in SecondarySongPlaylist.Keys) {
				ToProcess.Add(song);
			}
			foreach (SongDataContainer song in BrokenLinklist) {
				ToProcess.Add(song);
			}
			if (!protectRequests) {
				Stop();
			} else {
				if (CurrentSong != null) {
					ToProcess.Add(CurrentSong.SongData);
				}
				foreach (SongDataContainer song in Songlist.Requests.Select(e => e.SongData)) {
					ToProcess.Add(song);
				}
			}

			foreach (string filename in Directory.GetFiles(OutputDir)) {
				string fileID = Path.GetFileName(filename).Split(' ')[0].Trim();

				if (fileID.Length != 11) { // File not associated with cache so ignore it
					continue;
				}

				if (ToProcess.Any(e => e.Link.Contains(fileID) && e.AllowCaching == true)) {
					continue;
				} else {
					if (File.Exists(filename)) {
						try {
							File.Delete(filename);
						} catch {
							MainForm.StaticPostToDebug($"Attempt to delete file from cache: {filename} Failed.");
						}
					}
				}
				
			}
		}

		#endregion

		public bool CheckCurrentSongIsSaved() =>
			SecondarySongPlaylist.ContainsKey(CurrentSong.SongData) || BrokenLinklist.Contains(CurrentSong.SongData);

		public string GetCurrentSong(bool newlines = false) {
			if (IsStopped) {
				return "Song Requests currently stopped.";
			} else if (CurrentSong == null) {
				return "No song currently playing.";
			}
			return CurrentSong.OutputString(newlines);
		}

		/// <summary>
		/// Returns the requested song data.
		/// </summary>
		/// <returns></returns>
		public List<NameValueCollection> GetRequestedSonglist() => Songlist.Requests.Select(e => e.SongData.OutputDataValues()).ToList();

		/// <summary>
		/// Returns the secondary song data.
		/// </summary>
		/// <returns></returns>
		public List<NameValueCollection> GetSecondaryPlaylist() => SecondarySongPlaylist.Keys.Select(e => e.OutputDataValues()).ToList();

		/// <summary>
		/// Returns the broken song data.
		/// </summary>
		/// <returns></returns>
		public List<NameValueCollection> GetBrokenPlaylist() => BrokenLinklist.Select(e => e.OutputDataValues()).ToList();

		/// <summary>
		/// Returns the ToString of the current song requests and its index in the list.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<string> GetCurrentPlaylist(bool newlines = false) {
			return Songlist.Requests.ToList().Select(e => e.OutputString(newlines));
		}

		public bool GetFromSonglistByIndex(int index, out NameValueCollection data) {
			if (index < Songlist.Requests.Count) {
				data = Songlist.Requests.ElementAt(index).SongData.OutputDataValues();
				return true;
			}
			data = null;
			return false;
		}

		public bool GetFromSecondaryByIndex(int index, out NameValueCollection data) {
			if (index < SecondarySongPlaylist.Count) {
				data = SecondarySongPlaylist.ElementAt(index).Key.OutputDataValues();
				return true;
			}
			data = null;
			return false;
		}

		public bool GetFromBrokenByIndex(int index, out NameValueCollection data) {
			if (index < BrokenLinklist.Count) {
				data = BrokenLinklist[index].OutputDataValues();
				return true;
			}
			data = null;
			return false;
		}

		public void ClearSongRequests() {
			Songlist.Clear();
			ClearAllCache(false);
		}

		public bool SaveCurrentSong() {
			if (CurrentSong != null) {
				if (SecondarySongPlaylist.ContainsKey(CurrentSong.SongData)) {
					return true;
				}
				CurrentSong.SongData.OriginalRequester = CurrentSong.Requester;
				CurrentSong.SongData.AllowCaching = ProgramSettings.AppSettings.CacheNewSongs;

				File.AppendAllText(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt", JsonConvert.SerializeObject(CurrentSong.SongData) + Environment.NewLine);
				MainForm.StaticPostToDebug(CurrentSong.SongData.Title + " Saved to Secondary Playlist.");
				SecondarySongPlaylist.Add(CurrentSong.SongData, false);

				return true;
			}

			return false;
		}

		public bool RemoveCurrentSongFromSeconday() {
			if (CurrentSong != null) {
				if (!SecondarySongPlaylist.ContainsKey(CurrentSong.SongData)) {
					return true;
				}

				if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt")) {
					List<string> songs = File.ReadAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt").ToList();
					songs.RemoveAll(x => x.Contains(CurrentSong.SongData.Link));
					File.WriteAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt", songs);
					MainForm.StaticPostToDebug(CurrentSong.SongData.Title + " Removed from Secondary Playlist.");
				}

				return true;
			}

			return false;
		}

		public bool RemoveSongByGenID(string ID) {
			SongDataContainer songwithID;

			songwithID = SecondarySongPlaylist.Keys.ToList().Find(e => e.UniqueSystemID.Equals(ID));
			if (songwithID != null) {
				SecondarySongPlaylist.Remove(songwithID);
			}
			songwithID = BrokenLinklist.Find(e => e.UniqueSystemID.Equals(ID));
			if (songwithID != null) {
				BrokenLinklist.Remove(songwithID);
			}
			WriteSongListsToFile(false);

			if (songwithID != null) {
				MainForm.StaticPostToDebug($"Song: {songwithID.Title} {(songwithID.LocalFile ? songwithID.FullDirLocation : songwithID.Link)} Successfully removed.");
				songwithID = null;
				return true;
			} else {
				MainForm.StaticPostToDebug($"No song with generated ID: {ID}, found in the system. The song must have already been removed.");
				return false;
			}
		}

		public async Task<bool> AddSecondarySong(string address, string requester, bool local) {
			if (local) {
				if (CheckAddressExistsInSystem(address, local)) {
					MainForm.StaticPostToDebug($"Local Address: {address} : Found, Adding Song Canceled.");
					return true;
				}

				SongDataContainer song = SongDataContainer.CreateNewContainer(address, requester, local: local);
				ProcessIDGeneration();

				if (File.Exists(address)) {
					SecondarySongPlaylist.Add(song, false);
					WriteSingleSongToFile(song);
					return true;
				} else {
					MainForm.StaticPostToDebug($"Local Address: {address} : Invalid. File not found.");
					return false;
				}
			} else {
				if (CheckAddressExistsInSystem(address, local)) {
					MainForm.StaticPostToDebug($"Link: {address} : Found, Adding Song Canceled.");
					return true;
				}

				SongDataContainer song = await SongDataContainer.CreateNewContainer(address, requester, YoutubeDLWorker);

				if (song.PingValid) {
					SecondarySongPlaylist.Add(song, false);
					ProcessIDGeneration();
					WriteSingleSongToFile(song);
					song.AllowCaching = ProgramSettings.AppSettings.CacheNewSongs;
					if (ProgramSettings.AppSettings.CacheNewSongs) {
						await song.GetYouTubeAudioData(YoutubeDLWorker);
					}
					OnSecondaryPlaylistUpdated.Invoke(this, EventArgs.Empty);
					return true;
				} else {
					MainForm.StaticPostToDebug($"YouTube link: {address} : Invalid. Ping returned Errors.");
					return false;
				}
			}
		}

		public bool CheckAddressExistsInSystem(string address, bool local) =>
			local ?
			SecondarySongPlaylist.Keys.Any(e => e.DirLocation.Contains(address)) || BrokenLinklist.Any(e => e.DirLocation.Contains(address)) :
			SecondarySongPlaylist.Keys.Any(e => e.Link.Contains(address)) || BrokenLinklist.Any(e => e.Link.Contains(address));

		public void ClaimAllSongs(string requester) {
			foreach (SongDataContainer song in SecondarySongPlaylist.Keys) {
				song.OriginalRequester = requester;
			}
			foreach (SongDataContainer song in BrokenLinklist) {
				song.OriginalRequester = requester;
			}
			WriteSongListsToFile(true);
		}

		public void ClaimSong(string requester, string ID) {
			SongDataContainer songwithID;

			songwithID = SecondarySongPlaylist.Keys.ToList().Find(e => e.UniqueSystemID.Equals(ID));
			if (songwithID != null) {
				songwithID.OriginalRequester = requester;
				MainForm.StaticPostToDebug($"Song: {songwithID.Title} Successfully claimed by {songwithID.OriginalRequester}.");
			}
			songwithID = BrokenLinklist.Find(e => e.UniqueSystemID.Equals(ID));
			if (songwithID != null) {
				songwithID.OriginalRequester = requester;
				MainForm.StaticPostToDebug($"No song with generated ID: {ID}, found in the system. Claim Song Aborted.");
			}
			WriteSongListsToFile(false);
		}

		public string RemoveLastSongByUser(string requester) {
			if (Songlist.Requests.Any(e => e.Requester.Equals(requester))) {
				TwitchRequestedSong song = Songlist.Requests.TakeWhile(e => e.Requester.Equals(requester)).Last();
				Songlist.Remove(song);
				return $"@{requester} : {song.SongData.Title} Has been removed.";
			} else {
				return $"@{requester} : No songs found.";
			}
		}

		public string RemoveIndexSongByUser(string requester, int index) {
			if (Songlist.Requests.Any(e => e.Requester.Equals(requester))) {
				IEnumerable<TwitchRequestedSong> songs = Songlist.Requests.TakeWhile(e => e.Requester.Equals(requester));
				if (index > songs.Count()) {
					return $"@{requester} : Index value '{index}' not found in requests.";
				} else {
					TwitchRequestedSong song = songs.ElementAt(index - 1);
					Songlist.Remove(song);

					return $"@{requester} : {song.SongData.Title} Has been removed.";
				}
			} else {
				return $"@{requester} : No songs found.";
			}
		}

		public void RemoveSongFromRequestedListSystID(string ID) {
			foreach (TwitchRequestedSong song in Songlist.Requests.ToArray()) {
				if (song.SongData.UniqueSystemID.Equals(ID)) {
					Songlist.Remove(song);
					MainForm.StaticPostToDebug($"Removed Request from {song.Requester} : {song.SongData.Title}");
					return;
				}
			}
			MainForm.StaticPostToDebug($"No song request found with ID: {ID}");
		}

		public string RemoveSongFromRequestsByYTID(string requester, string ytID) {
			string ID = ytID;
			if (ytID.Length != 11) {
				GlobalFunctions.GetYouTubeVideoID(ytID, out ID);
			}
			if (string.IsNullOrEmpty(ID) || ID.Length != 11) {
				return $"@{requester} : Provided song request ID [{ytID}] not recognised.";
			}

			List<TwitchRequestedSong> ToRemove = new List<TwitchRequestedSong>();
			foreach (TwitchRequestedSong song in Songlist.Requests) {
				if (song.SongData.Link.Contains(ID)) {
					ToRemove.Add(song);
				}
			}

			if (ToRemove.Count() > 0) {
				int count = 0;
				foreach (TwitchRequestedSong request in ToRemove) {
					Songlist.Remove(request);
					count++;
				}
				MainForm.StaticPostToDebug($"{count} song requests were removed from the queue with the ID: {ID}");
				return $"@{requester} : {count} song requests were removed from the queue.";
			} else {
				MainForm.StaticPostToDebug($"No song requests found with ID: {ID}");
				return $"@{requester} : No songs found.";
			}
		}

		public string PrintRequesterSongList(string requester) {
			if (Songlist.Requests.Any(e => e.Requester.Equals(requester))) {
				string output = $"@{requester} : | ";
				IEnumerable<TwitchRequestedSong> songs = Songlist.Requests.TakeWhile(e => e.Requester.Equals(requester));
				for (int x = 0; x < songs.Count(); x++) {
					if (string.IsNullOrEmpty(songs.ElementAt(x).SongData.Title)) {
						output += $"{x + 1} : {songs.ElementAt(x).SongData.Link} | ";
					} else {
						output += $"{x + 1} : {songs.ElementAt(x).SongData.Title} | ";
					}
				}

				return output;
			} else {
				return $"@{requester} : No songs found.";
			}
		}

		public int GetRequesterRequestAmount(string requester) {
			return Songlist.Requests.Sum(e => {
				if (e.Requester.Equals(requester)) {
					return 1;
				} else {
					return 0;
				}
			});
		}

		public void ProcessIDGeneration() {
			void ProcessIDs(IEnumerable<SongDataContainer> containers) {
				foreach (SongDataContainer container in containers) {
					if (string.IsNullOrEmpty(container.UniqueSystemID) || CheckCodeIsInSystem(container.UniqueSystemID)) {
						container.UniqueSystemID = GenerateIDCode();
					}
				}
			}
			ProcessIDs(SecondarySongPlaylist.Keys);
			ProcessIDs(BrokenLinklist);
			ProcessIDs(Songlist.Requests.Select(e => e.SongData));
		}

		readonly Random IDRandomEntity = new Random();
		public string GenerateIDCode() {
			const string availableChars = "0123456789qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM";
			const int length = 11;

			string buildID() {
				StringBuilder str_build = new StringBuilder();

				for (int i = 0; i < length; i++) {
					var c = availableChars[IDRandomEntity.Next(0, availableChars.Length)];
					str_build.Append(c);
				}
				return str_build.ToString();
			}

			string ID;

			do {
				ID = buildID();
			} while (CheckCodeIsInSystem(ID));

			return ID;
		}

		public bool CheckCodeIsInSystem(string ID) {
			if (string.IsNullOrEmpty(ID)) {
				return false;
			}
			return SecondarySongPlaylist.Keys.Any(e => !string.IsNullOrEmpty(e.UniqueSystemID) && e.UniqueSystemID.Equals(ID)) ||
				BrokenLinklist.Any(e => !string.IsNullOrEmpty(e.UniqueSystemID) && e.UniqueSystemID.Equals(ID)) ||
				Songlist.Requests.Any(e => !string.IsNullOrEmpty(e.SongData.UniqueSystemID) && e.SongData.UniqueSystemID.Equals(ID));
		}

		public async void CacheSong(string ID, bool cacheValue) {
			SongDataContainer songwithID = SecondarySongPlaylist.Keys.ToList().Find(e => e.UniqueSystemID.Equals(ID));

			if (songwithID != null) {
				songwithID.AllowCaching = cacheValue;
				if (cacheValue) {
					await songwithID.GetYouTubeAudioData(YoutubeDLWorker);
					MainForm.StaticPostToDebug($"{songwithID.Title} Was set to Cache song data.");
				} else {
					await songwithID.DeleteCache();
					MainForm.StaticPostToDebug($"{songwithID.Title} Was set to NOT Cache song data.");
				}
			}

			songwithID = BrokenLinklist.ToList().Find(e => e.UniqueSystemID.Equals(ID));

			if (songwithID != null) {
				songwithID.AllowCaching = cacheValue;
				if (cacheValue) {
					await songwithID.GetYouTubeAudioData(YoutubeDLWorker);
					MainForm.StaticPostToDebug($"{songwithID.Title} Was set to Cache song data.");
				} else {
					MainForm.StaticPostToDebug($"{songwithID.Title} Was set to NOT Cache song data.");
				}
			}

			WriteSongListsToFile(false);
		}

		public void CacheAllSongs() {
			SecondarySongPlaylist.Keys.ToList().ForEach(e => e.AllowCaching = true);
			BrokenLinklist.ForEach(e => e.AllowCaching = true);

			WriteSongListsToFile(false);

			LoadSecondaryPlaylistFromFile();
		}

		public void ClearSongCaches() {
			SecondarySongPlaylist.Keys.ToList().ForEach(e => e.AllowCaching = false);
			BrokenLinklist.ForEach(e => e.AllowCaching = false);

			WriteSongListsToFile(false);

			ClearAllCache(true);
		}
	}
}
