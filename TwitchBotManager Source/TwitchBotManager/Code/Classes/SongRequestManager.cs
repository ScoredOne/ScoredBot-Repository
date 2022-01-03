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

using YoutubeDLSharp;

namespace TwitchBotManager.Code.Classes {

	public class SongRequestManager {

		#region ### Variables ###

		private bool Operating;

		private LibVLC _libVLC;

		private MediaPlayer VLCPlayer;

		private Media media;

		/// <summary>
		/// Current song playing in the bot
		/// </summary>
		private SongDataContainer CurrentSong;

		/// <summary>
		/// Primary requested song list.
		/// </summary>
		private readonly LinkedList<SongDataContainer> Songlist = new LinkedList<SongDataContainer>();

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

		public uint SongCacheAmount { get; set; }

		private int currentVolume;
		public int CurrentVolume {
			get => currentVolume;
			set {
				currentVolume = value;
				OnVolumeUpdate.SafeInvoke(this, value);
				if (VLCPlayer != null) {
					VLCPlayer.Volume = value;
				}
			}
		}

		public bool IsLoading { get; private set; } = false;

		public bool IsPlaying { get; private set; } = false;

		public bool IsStopped { get; private set; } = true;

		public bool IsBuffering { get; private set; } = false;

		public bool IsTranstioning { get; private set; } = false;

		public bool IsSecondary { get; private set; } = false;

		/// <summary>
		/// Number of requests to cache.
		/// null = unlimited | 0 = none
		/// </summary>
		public int? RequestsCacheAmount { get; private set; } = 3;

		public int ToCacheVale { get; private set; } = 0;

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

		public event EventHandler<(int LiteralPosition, string TranslatedTime)> OnPlayerProgress;

		#endregion

		#endregion

		/// <summary>
		/// Once Events are populated, call Initialize to start operating
		/// </summary>
		public SongRequestManager() { }

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

					CurrentVolume = GlobalFunctions.LoadMediaPlayerVolume();

					LoadSecondaryPlaylistFromFile();

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
			ClearAllCache();

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

			VLCPlayer.EndReached += Media_EndReached;
			VLCPlayer.Volume = CurrentVolume;
			VLCPlayer.Buffering += VLCPlayer_Buffering;
			VLCPlayer.Playing += VLCPlayer_Playing;
			VLCPlayer.Paused += VLCPlayer_Paused;
			//VLCPlayer.PositionChanged += VLCPlayer_PositionChanged;
			//VLCPlayer.EncounteredError += VLCPlayer_EncounteredError;
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

			if (CurrentSong.CurrentRequesters.Count > 0) {
				CurrentSong.CurrentRequesters.RemoveFirst();
			}

			await ManageCacheAfterMediaEnd();

			// Cant access song, IOExecption cant access file to delete cache

			ThreadPool.QueueUserWorkItem(_ => {
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

				OnSongRequestOutputChanged.SafeInvoke(this, SongOutputText.InputString = CurrentSong.OutputString(IsSecondary ? -1 : 0) + " ||" + TwitchBot.SongCommandPrefix + "|");
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

			OnSongRequestOutputChanged.SafeInvoke(this, SongOutputText.InputString = "Song Requests Paused");

			IsPlaying = false;

			MainForm.StaticPostToDebug("Song Requests Paused");

			return true;
		}

		private bool MediaPlayerStop() {
			VLCPlayer.Stop();

			OnSongRequestOutputChanged.SafeInvoke(this, SongOutputText.InputString = "Song Requests Stopped");

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

			if (VLCPlayer.WillPlay) {
				VLCPlayer.Play();
			} else {
				PlayMedia();
			}

			MainForm.StaticPostToDebug("Song Request Skipped");

			return true;
		}

		private Random SecondarySongListRandomNumber;
		private async void PlayMedia(SongDataContainer song = null) {
			if (song == null) {
				if (Songlist.Count == 0 && SecondarySongPlaylist.Count == 0) {
					MainForm.StaticPostToDebug("Main playlist and secondary playlist are empty, please request songs to start playing.");
					IsSecondary = false;
					return;
				}

				IsTranstioning = true;
				// Get Current Song

				if (Songlist.Count > 0) {
					CurrentSong = Songlist.First.Value;
					Songlist.RemoveFirst();
					IsSecondary = false;
				} else { // Get from secondary playlist
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

					CurrentSong = SecondaryKeys[randomNumber];
					SecondarySongPlaylist[SecondaryKeys[randomNumber]] = true;
					IsSecondary = true;
				}
			} else {
				CurrentSong = song;
				IsSecondary = false;
			}

			// Output song details
			OnNextSong.SafeInvoke(this, !SecondarySongPlaylist.Keys.Any(z => z.Link.Equals(CurrentSong.Link)));
			OnSongRequestOutputChanged.SafeInvoke(this, SongOutputText.InputString = CurrentSong.OutputString(IsSecondary ? -1 : 0) + " ||" + TwitchBot.SongCommandPrefix + "|");

			// Play song
			try {
				OnTranstioning.SafeInvoke(this, true);

				if (CurrentSong.LocalFile && !CurrentSong.AudioCached()) {
					throw new Exception($"Local file: {CurrentSong.DirLocation} Not found.");
				} else if (!CurrentSong.AudioCached() && !CurrentSong.DownloadWorking) {
					await CurrentSong.GetYouTubeAudioData(YoutubeDLWorker);
					if (!CurrentSong.AudioCached()) {
						throw new Exception($"Song : {CurrentSong.Title} Failed to find Cache.");
					}
				}

				VLCPlayer.Media = media = new Media(_libVLC, CurrentSong.FullDirLocation);
				await VLCPlayer.Media.Parse();

				VLCPlayer.Play();

				MainForm.StaticPostToDebug("Playing song: " + (string.IsNullOrEmpty(CurrentSong.Title) ? CurrentSong.Link : CurrentSong.Title));

			} catch (Exception exc) {
				OnError.SafeInvoke(this, exc.Message);

				MainForm.StaticPostToDebug("Song Failed to play, recovering and moving on to the next song.");
				MainForm.StaticPostToDebug(exc.Message);

				if (CurrentSong.LocalFile && !CurrentSong.AudioCached()) {
					CurrentSong.LastPingFailed = true;

					SecondarySongPlaylist.Remove(CurrentSong);

					if (!BrokenLinklist.Contains(CurrentSong)) {
						BrokenLinklist.Add(CurrentSong);
					}

					MainForm.StaticPostToDebug("Secondary Playlist Song Data Failed... " + CurrentSong.DirLocation);
				}
				PlayMedia();
			}

			IsTranstioning = false; // Doesnt work very well
									//UpdateSongListOutput();
		}

		public void LoadSecondaryPlaylistFromFile() {
			OnSecondaryPlaylistUpdated.SafeInvoke(this, EventArgs.Empty);
			IsLoading = true;

			if (!File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt")) {
				GlobalFunctions.CheckAndCreateOutputDirectoryFiles();
				return;
			}

			List<SongDataContainer> songRequestDatas = new List<SongDataContainer>();
			File.ReadAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt").ToList().ForEach(e => {
				if (!string.IsNullOrEmpty(e)) {
					SongDataContainer requestData = SongDataContainer.ConvertFromJSONData(e);
					if (requestData != null) {
						songRequestDatas.Add(requestData);
					}
				}
			});

			if (songRequestDatas.Count > 0) {
				MainForm.StaticPostToDebug($"Secondary Playlist Loading with {songRequestDatas.Count} Songs.");

				Task.WhenAll(songRequestDatas.Select(x => GetYouTubeWebData(x)))
					.ContinueWith(x => {
						MainForm.StaticPostToDebug($"Secondary Playlist Data Loaded. All Threads Completed. [{SecondarySongPlaylist.Count} Successful] - [{BrokenLinklist.Count} Failed]");

						MainForm.StaticPostToDebug($"Secondary Playlist Cache Loading with {SecondarySongPlaylist.Count} Songs.");

						Task.WhenAll(SecondarySongPlaylist.Select(e => e.Key.GetYouTubeAudioData(YoutubeDLWorker)))
							.ContinueWith(x => {
								ProcessIDGeneration();

								MainForm.StaticPostToDebug($"Secondary Playlist Songs Loaded. All Threads Completed. [{SecondarySongPlaylist.Sum(y => y.Key.AudioCached() ? 1 : 0)} Successful] - [{SecondarySongPlaylist.Sum(y => y.Key.AudioCached() ? 0 : 1)} Failed]");

								WriteSongListsToFile(false);
								IsLoading = false;
							});
					});
			} else {
				MainForm.StaticPostToDebug("Secondary Playlist not loaded, either it is empty or it has been corrupted.");
				IsLoading = false;
			}

			OnSecondaryPlaylistUpdated.SafeInvoke(this, EventArgs.Empty);
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
			if (!song.LocalFile) {
				await song.GetYouTubeVideoInformation(YoutubeDLWorker);
			}

			if ((song.LocalFile && song.AudioCached()) || (!string.IsNullOrEmpty(song.Link) && song.PingValid)) {
				BrokenLinklist.Remove(song);

				if (!SecondarySongPlaylist.ContainsKey(song)) {
					SecondarySongPlaylist.Add(song, false);
				}

				MainForm.StaticPostToDebug($"Secondary Playlist Song Data Loaded... {(song.LocalFile ? song.DirLocation : song.Title)}");
			} else if (!song.PingValid && song.AudioCached()) {
				BrokenLinklist.Remove(song);

				if (!SecondarySongPlaylist.ContainsKey(song)) {
					SecondarySongPlaylist.Add(song, false);
				}

				MainForm.StaticPostToDebug($"Secondary Playlist Song Data Loaded... *PING FAILED - LOCAL FILE FOUND* {(song.LocalFile ? song.DirLocation : song.Title)}");
			} else {
				SecondarySongPlaylist.Remove(song);

				if (!BrokenLinklist.Contains(song)) {
					BrokenLinklist.Add(song);
				}

				MainForm.StaticPostToDebug($"Secondary Playlist Song Data Failed... {(song.LocalFile ? song.DirLocation : song.Title)}");
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

			string output = "";
			SongDataContainer newsong = null;

			attempt: try {
				GlobalFunctions.GetYouTubeVideoID(link, out string ID);

				foreach (SongDataContainer song in Songlist.ToArray()) {
					if (song.Link.Contains(ID)) {
						song.CurrentRequesters.AddLast(requester);
						Songlist.AddLast(song);
						output= $"@{requester} : {song.Title} was Successfully Requested.";
						break;
					}
				}

				foreach (SongDataContainer song in SecondarySongPlaylist.Keys) {
					if (song.Link.Contains(ID)) {
						song.CurrentRequesters.AddLast(requester);
						Songlist.AddLast(song);
						output= $"@{requester} : {song.Title} was Successfully Requested.";
						break;
					}
				}

				foreach (SongDataContainer song in BrokenLinklist) {
					if (song.Link.Contains(ID)) {
						output= $"@{requester} : {song.Title} Is a saved song but unfortunatly isnt working at the moment, sorry for the inconvenience.";
						break;
					}
				}

				if (newsong == null) {
					newsong = await SongDataContainer.CreateNewContainer(link, requester, YoutubeDLWorker);
				}

				if (newsong.PingValid) {
					Songlist.AddLast(newsong);

					foreach (SongDataContainer songData in Songlist.Take(RequestsCacheAmount > Songlist.Count ? Songlist.Count : RequestsCacheAmount.Value)) {
						if (!songData.AudioCached()) {
							await songData.GetYouTubeAudioData(YoutubeDLWorker);
						}
					}

					output= $"@{requester} : {newsong.Title} was Successfully Requested.";
				} else {
					output= $"@{requester} : {newsong.Link} Request Failed. Ping to YouTube Failed.";
				}
			} catch {
				await Task.Delay(500);
				goto attempt;
			}
			return output;
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
				if (RequestsCacheAmount.HasValue) {
					ToCacheVale = RequestsCacheAmount > Songlist.Count ? Songlist.Count : RequestsCacheAmount.Value;
					IEnumerable<SongDataContainer> TakenList = Songlist.Take(ToCacheVale);

					if (!TakenList.Contains(CurrentSong) && !SecondarySongPlaylist.ContainsKey(CurrentSong)) {
						media.Dispose();
						await CurrentSong.DeleteCache();
					}

					foreach (SongDataContainer songData in TakenList) {
						if (!songData.AudioCached()) {
							await songData.GetYouTubeAudioData(YoutubeDLWorker);
						}
					}
				} else {
					foreach (SongDataContainer songData in Songlist) {
						if (!songData.AudioCached()) {
							await songData.GetYouTubeAudioData(YoutubeDLWorker);
						}
					}
				}
			} catch (Exception e) {
				MainForm.StaticPostToDebug($"ManageCacheAfterMediaEnd ERROR caught and recovered: {e.Message}");
			}

		}

		/// <summary>
		/// Removes all songs from cache except songs found in the secondary playlist
		/// </summary>
		public void ClearAllCache() {
			foreach (string filename in Directory.GetFiles(OutputDir)) {
				string fileID = Path.GetFileName(filename).Split(' ')[0].Trim();

				if (fileID.Length != 11) { // File not associated with cache so ignore it
					continue;
				}

				if (SecondarySongPlaylist.Keys.Any(e => e.Link.Contains(fileID)) || BrokenLinklist.Any(e => e.Link.Contains(fileID))) {
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
			SecondarySongPlaylist.ContainsKey(CurrentSong) || BrokenLinklist.Contains(CurrentSong);

		public string GetCurrentSong(bool newlines = false) {
			if (IsStopped) {
				return "Song Requests currently stopped.";
			} else if (CurrentSong == null) {
				return "No song currently playing.";
			}
			return CurrentSong.OutputString(CurrentSong.CurrentRequesters.Count > 0 ? 0 : -1, newlines);
		}

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
		public Dictionary<int, string> GetCurrentPlaylist(bool newlines = false) {
			Dictionary<int, string> output = new Dictionary<int, string>();
			SongDataContainer[] keys = Songlist.ToArray();
			Dictionary<SongDataContainer, int> processed = new Dictionary<SongDataContainer, int>();

			for (int x = 0; x < keys.Length; x++) {
				if (processed.ContainsKey(keys[x])) {
					processed[keys[x]]++;
				} else {
					processed.Add(keys[x], 0);
				}

				output.Add(x, keys[x].OutputString(processed[keys[x]], newlines));
			}

			return output;
		}

		public bool GetFromSonglistByIndex(int index, out NameValueCollection data) {
			if (index < Songlist.Count) {
				data = Songlist.ElementAt(index).OutputDataValues();
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
			foreach (SongDataContainer song in Songlist) {
				if (!SecondarySongPlaylist.ContainsKey(song) || !BrokenLinklist.Contains(song)) {
					song.DeleteCache();
				}
			}
			Songlist.Clear();
			OnSecondaryPlaylistUpdated.SafeInvoke(this, EventArgs.Empty);
		}

		public bool SaveCurrentSong() {
			if (CurrentSong != null) {
				if (SecondarySongPlaylist.ContainsKey(CurrentSong)) {
					return true;
				}

				File.AppendAllText(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt", JsonConvert.SerializeObject(CurrentSong) + Environment.NewLine);
				MainForm.StaticPostToDebug(CurrentSong.Title + " Saved to Secondary Playlist.");
				SecondarySongPlaylist.Add(CurrentSong, false);

				return true;
			}

			return false;
		}

		public bool RemoveCurrentSongFromSeconday() {
			if (CurrentSong != null) {
				if (!SecondarySongPlaylist.ContainsKey(CurrentSong)) {
					return true;
				}

				if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt")) {
					List<string> songs = File.ReadAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt").ToList();
					songs.RemoveAll(x => x.Contains(CurrentSong.Link));
					File.WriteAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt", songs);
					MainForm.StaticPostToDebug(CurrentSong.Title + " Removed from Secondary Playlist.");
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
				song.UniqueSystemID = GenerateIDCode();
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

				SongDataContainer song = await SongDataContainer.CreateNewContainer(address, requester, YoutubeDLWorker, true);
				song.UniqueSystemID = GenerateIDCode();
				if (song.PingValid) {
					SecondarySongPlaylist.Add(song, false);
					WriteSingleSongToFile(song);
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

		public bool ClaimSong(string requester, bool brokenlist, int index) {
			if (brokenlist) {
				if (BrokenLinklist.Count > index) {
					BrokenLinklist.ElementAt(index).OriginalRequester = requester;
					WriteSongListsToFile(true);
					return true;
				}
				return false;
			} else {
				if (SecondarySongPlaylist.Count > index) {
					SecondarySongPlaylist.Keys.ElementAt(index).OriginalRequester = requester;
					WriteSongListsToFile(true);
					return true;
				}
				return false;
			}
		}

		public string RemoveLastSongByUser(string requester) {
			if (!Songlist.Any(e => e.CurrentRequesters.First.Equals(requester))) {
				return $"@{requester} : No songs found.";
			} else {
				SongDataContainer song = Songlist.TakeWhile(e => e.CurrentRequesters.First.Equals(requester)).Last();
				Songlist.Remove(song);
				return $"@{requester} : {song.Title} Has been removed.";
			}
		}


		public string RemoveIndexSongByUser(string requester, int index) {
			if (!Songlist.Any(e => e.CurrentRequesters.First.Equals(requester))) {
				return $"@{requester} : No songs found.";
			} else {
				IEnumerable<SongDataContainer> songs = Songlist.TakeWhile(e => e.CurrentRequesters.First.Equals(requester));
				if (songs.Count() > index) {
					return $"@{requester} : Index value '{index}' not found in requests.";
				} else {
					SongDataContainer song = Songlist.TakeWhile(e => e.CurrentRequesters.First.Equals(requester)).ElementAt(index);
					Songlist.Remove(song);

					return $"@{requester} : {song.Title} Has been removed.";
				}
			}
		}

		public string PrintRequesterSongList(string requester) {
			if (!Songlist.Any(e => e.CurrentRequesters.First.Equals(requester))) {
				return $"@{requester} : No songs found.";
			} else {
				string output = $"@{requester} : | ";
				IEnumerable<SongDataContainer> songs = Songlist.TakeWhile(e => e.CurrentRequesters.First.Equals(requester));
				for (int x = 0; x < songs.Count(); x++) {
					if (string.IsNullOrEmpty(songs.ElementAt(x).Title)) {
						output += $"{x + 1} : {songs.ElementAt(x).Link} | ";
					} else {
						output += $"{x + 1} : {songs.ElementAt(x).Title} | ";
					}
				}

				return output;
			}
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

			string ID = "";

			do {
				ID = buildID();
			} while (CheckCodeIsInSystem(ID));

			return ID;
		}

		public bool CheckCodeIsInSystem(string ID) {
			if (string.IsNullOrEmpty(ID)) {
				return false;
			}
			return SecondarySongPlaylist.Keys.Any(e => !string.IsNullOrEmpty(e.UniqueSystemID) && e.UniqueSystemID.Equals(ID)) || BrokenLinklist.Any(e => !string.IsNullOrEmpty(e.UniqueSystemID) && e.UniqueSystemID.Equals(ID));
		}
	}
}
