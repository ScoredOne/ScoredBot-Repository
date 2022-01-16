using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

using Newtonsoft.Json;

using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace TwitchBotManager.Code.Classes {

	public class SongDataContainer {

		/// <summary>
		/// Youtube Link
		/// </summary>
		public string Link { get; private set; }

		/// <summary>
		/// User who requested song to the bot originally
		/// </summary>
		public string OriginalRequester { get; set; }

		/// <summary>
		/// Song Title
		/// </summary>
		public string Title { get; private set; }

		/// <summary>
		/// Song Lenght in seconds
		/// </summary>
		public int LengthSec { get; private set; }

		/// <summary>
		/// Cached Location
		/// </summary>
		public string DirLocation { get; private set; }

		/// <summary>
		/// Full Cached Location including program directory
		/// </summary>
		[JsonIgnore]
		public string FullDirLocation => Directory.GetCurrentDirectory() + DirLocation;

		/// <summary>
		/// Last ping to youtube
		/// </summary>
		public DateTime LastValidPing { get; private set; }

		/// <summary>
		/// Did the most recent ping fail?
		/// </summary>
		public bool LastPingFailed { get; set; }

		/// <summary>
		/// Was the last ping was successful
		/// </summary>
		[JsonIgnore]
		public bool PingValid => DateTime.Now < new DateTime(LastValidPing.Ticks).AddDays(14);

		/// <summary>
		/// If the file is Local so pings to the internet and redownloads are not required.
		/// Disables requirements for other values such as Link and Title, but relies on AudioCached always being true.
		/// </summary>
		public bool LocalFile { get; private set; }

		/// <summary>
		/// ID generated for the system
		/// </summary>
		[JsonIgnore]
		public string UniqueSystemID { get; set; }

		public bool AudioCached() => File.Exists(FullDirLocation);

		public TimeSpan LengthInTime => TimeSpan.FromSeconds(LengthSec);

		[JsonIgnore]
		public Task<RunResult<VideoData>> InformationAquireTask { get; private set; }

		[JsonIgnore]
		public Task<RunResult<string>> VideoAquireTask { get; private set; }

		private SongDataContainer() { }

		[JsonIgnore]
		public bool DownloadWorking;

		[JsonIgnore]
		public string ErrorMessage { get; private set; } = "";

		/// <summary>
		/// Song Constructor for local/secondary playlist files
		/// </summary>
		public static SongDataContainer CreateNewContainer(string dirLocation, string requester, string link = "", DateTime lastpingdate = new DateTime(), string title = "", int length = 0, bool failedping = false, bool local = false) {
			return new SongDataContainer() {
				Link = link,
				OriginalRequester = requester,
				Title = title,
				LengthSec = length,
				DirLocation = dirLocation.Replace(Directory.GetCurrentDirectory(), ""),
				LastValidPing = lastpingdate,
				LastPingFailed = failedping,
				LocalFile = local
			};
		}

		/// <summary>
		/// Song Constructor to download files
		/// </summary>
		/// <param name="link"></param>
		/// <param name="requester"></param>
		/// <param name="youtubeDL"></param>
		/// <param name="getaudiodata"></param>
		public async static Task<SongDataContainer> CreateNewContainer(string link, string requester, YoutubeDL youtubeDL, bool getaudiodata = false) {
			SongDataContainer container = new SongDataContainer() {
				Link = link,
				OriginalRequester = requester,
				LocalFile = false
			};

			if (youtubeDL == null) {
				return container;
			}

			await container.GetYouTubeVideoInformation(youtubeDL);

			if (getaudiodata && container.PingValid) {
				await container.GetYouTubeAudioData(youtubeDL);
			}

			return container;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Requester"> -1 = Original Requester, 0+ gets Current Requester at index </param>
		/// <param name="newLines"></param>
		/// <returns></returns>
		public string OutputString(string Requester = "", bool newLines = false) {
			string output = "";

			output += $"{(newLines ? Environment.NewLine : string.Empty)}|# Title #| : {(string.IsNullOrEmpty(Title) ? "#TITLE MISSING#" : Title)} ";

			output += $"{(newLines ? Environment.NewLine : string.Empty)}|# Link #| : {(string.IsNullOrEmpty(Link) ? "#LINK MISSING#" : Link)} ";

			if (string.IsNullOrEmpty(Requester)) {
				output += $"{(newLines ? Environment.NewLine : string.Empty)}|# Requester #| : {(string.IsNullOrEmpty(OriginalRequester) ? "#REQUESTER MISSING#" : OriginalRequester)} ";
			} else {
				output += $"{(newLines ? Environment.NewLine : string.Empty)}|# Requester #| : {Requester} ";
			}
			
			if (LengthSec > 0) {
				output += $"{(newLines ? Environment.NewLine : string.Empty)}|# Duration #| : {LengthInTime} ";
			}
			return output;
		}

		public NameValueCollection OutputDataValues() {
			GlobalFunctions.GetYouTubeVideoID(Link, out string ID);
			return new NameValueCollection {
				{ nameof(Link), Link },
				{ nameof(ID), ID },
				{ nameof(OriginalRequester), OriginalRequester },
				{ nameof(Title), Title },
				{ nameof(LengthSec), LengthSec.ToString() },
				{ nameof(LengthInTime), LengthInTime.ToString() },
				{ nameof(DirLocation), DirLocation },
				{ nameof(LastValidPing), LastValidPing.ToString() },
				{ nameof(LastPingFailed), LastPingFailed.ToString() },
				{ nameof(PingValid), PingValid.ToString() },
				{ nameof(LocalFile), LocalFile.ToString() },
				{ nameof(AudioCached), AudioCached().ToString() },
				{ nameof(UniqueSystemID), UniqueSystemID }
			};
		}

		/// <summary>
		/// Creates a SongDataContainer from a Serialized JSON string
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static SongDataContainer ConvertFromJSONData(string value) {
			NameValueCollection valuePairs = GlobalFunctions.ParseDictionaryToNVC(new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(value));

			if (bool.TryParse(valuePairs["LocalFile"], out bool boolvalue) && boolvalue) { // Local
				if (valuePairs["DirLocation"] == null || !File.Exists(valuePairs["DirLocation"])) {
					throw new ArgumentException("DirLocation: location for local file not found or valid");
				} else {
					return CreateNewContainer(valuePairs["DirLocation"],
						string.IsNullOrEmpty(valuePairs["OriginalRequester"]) ? "#DATA MISSING#" : valuePairs["OriginalRequester"],
						string.IsNullOrEmpty(valuePairs["Link"]) ? "#DATA MISSING#" : valuePairs["Link"],
						DateTime.TryParse(valuePairs["LastValidPing"], out DateTime date) ? date : new DateTime(),
						string.IsNullOrEmpty(valuePairs["Title"]) ? "#DATA MISSING#" : valuePairs["Title"],
						int.TryParse(valuePairs["LengthSec"], out int length) ? length : 0,
						bool.TryParse(valuePairs["LastPingFailed"], out bool failping) && failping,
						boolvalue);
				}
			} else {
				if (valuePairs["Link"] == null) {
					throw new ArgumentException("Link: Link for secondary song not found or valid");
				} else {
					return CreateNewContainer(string.IsNullOrEmpty(valuePairs["DirLocation"]) ? "#DATA MISSING#" : valuePairs["DirLocation"],
						string.IsNullOrEmpty(valuePairs["OriginalRequester"]) ? "#DATA MISSING#" : valuePairs["OriginalRequester"],
						valuePairs["Link"],
						DateTime.TryParse(valuePairs["LastValidPing"], out DateTime date) ? date : new DateTime(),
						string.IsNullOrEmpty(valuePairs["Title"]) ? "#DATA MISSING#" : valuePairs["Title"],
						int.TryParse(valuePairs["LengthSec"], out int length) ? length : 0,
						bool.TryParse(valuePairs["LastPingFailed"], out bool failping) && failping,
						boolvalue);
				}
			}
		}

		public void ReadJSONData(string value) {
			NameValueCollection valuePairs = GlobalFunctions.ParseDictionaryToNVC(new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(value));

			Link = valuePairs["Link"];

			OriginalRequester = valuePairs["OriginalRequester"];

			Title = valuePairs["Title"];

			if (int.TryParse(valuePairs["LengthSec"], out int intvalue)) {
				LengthSec = intvalue;
			}

			DirLocation = valuePairs["DirLocation"];

			if (DateTime.TryParse(valuePairs["LastValidPing"], out DateTime datevalue)) {
				LastValidPing = datevalue;
			}

			if (bool.TryParse(valuePairs["LastPingFailed"], out bool boolping)) {
				LastPingFailed = boolping;
			} else {
				LastPingFailed = false;
			}

			if (bool.TryParse(valuePairs["LocalFile"], out bool boolvalue)) {
				LocalFile = boolvalue;
			} else {
				LocalFile = false;
			}
		}

		public async Task GetYouTubeVideoInformation(YoutubeDL YoutubeDLWorker, bool Force = false) {
			if (YoutubeDLWorker == null) {
				throw new NullReferenceException("GetYouTubeVideoInformation: YoutubeDLWorker was provided null");
			}

			if (InformationAquireTask != null) {
				await InformationAquireTask;
				return;
			} else if (LocalFile) {
				MainForm.StaticPostToDebug($"GetYouTubeVideoInformation: Skipped {(string.IsNullOrEmpty(Title) ? Link : Title)} data, Song is Local.");
				return;
			} else if (!Force && PingValid) {
				MainForm.StaticPostToDebug($"GetYouTubeVideoInformation: {(string.IsNullOrEmpty(Title) ? Link : Title)} is still within valid period, download canceled.");
				return;
			} else if (Force && PingValid) {
				MainForm.StaticPostToDebug($"GetYouTubeVideoInformation: Forced download of {(string.IsNullOrEmpty(Title) ? Link : Title)} data.");
			}

			DownloadWorking = true;
			if (GlobalFunctions.GetYouTubeVideoID(Link, out string youtubeMatch)) {
				RunResult<VideoData> Youtubedata = null;
				Exception exception = null;

				try {
					MainForm.StaticPostToDebug($"GetYouTubeVideoInformation: Download of {(string.IsNullOrEmpty(Title) ? Link : Title)} started.");
					InformationAquireTask = YoutubeDLWorker.RunVideoDataFetch("https://www.youtube.com/watch?v=" + youtubeMatch
					, overrideOptions: new YoutubeDLSharp.Options.OptionSet() {
						DumpJson = true,
						DumpSingleJson = true,
						HlsPreferNative = true,
						IgnoreConfig = true,
						NoPlaylist = true,
						SkipDownload = true,
						GetThumbnail = false,
						ListThumbnails = false,
						WriteAllThumbnails = false,
						WriteThumbnail = false
					});
					Youtubedata = await InformationAquireTask;
				} catch (Exception e) {
					exception = e;
				}

				if (exception != null) {
					MainForm.StaticPostToDebug($"{Link} : Error attempting to download song information. : {ErrorMessage = exception.Message}");
					LastPingFailed = true;
				} else if (Youtubedata != null && Youtubedata.Success) {
					Title = Youtubedata.Data.Title;
					LengthSec = (int)Youtubedata.Data.Duration;
					if (LengthSec > 900) {
						LastPingFailed = true;
						ErrorMessage = "Video Length exceeds set limit of 15 Mins.";

						MainForm.StaticPostToDebug($"Video Length exceeds set limit of 15 Mins.... {Title}");
					} else {
						LastValidPing = DateTime.Now;
						LastPingFailed = false;

						MainForm.StaticPostToDebug($"Secondary Song Info Downloaded... {Title}");
					}
				} else {
					if (Youtubedata == null) {
						MainForm.StaticPostToDebug($"https://www.youtube.com/watch?v={youtubeMatch} : YoutubeDLWorker Crashed Out");
					} else {
						string errors = "";
						foreach (string error in Youtubedata.ErrorOutput) {
							errors += error + " :: ";
						}
						//This video is not available
						if (errors.Contains("This video is not available")) {
							ErrorMessage = "Video not available, it may not be available at streamers location.";
						}
						MainForm.StaticPostToDebug(errors);
					}
					LastPingFailed = true;
				}

			} else {
				ErrorMessage = $"{Link} Failed, link was not recognised by Regex.";
				MainForm.StaticPostToDebug($"GetYouTubeVideoInformation using link: {Link} Failed, link was not recognised by Regex.");
				LastPingFailed = true;
			}

			DownloadWorking = false;
			InformationAquireTask = null;
			return;
		}

		public async Task GetYouTubeAudioData(YoutubeDL YoutubeDLWorker, YoutubeDLSharp.Options.AudioConversionFormat type = YoutubeDLSharp.Options.AudioConversionFormat.Mp3, bool Force = false) {
			if (YoutubeDLWorker == null) {
				throw new NullReferenceException("GetYouTubeAudioData: YoutubeDLWorker was provided null");
			}

			if (VideoAquireTask != null) {
				await VideoAquireTask;
				return;
			}

			if (LocalFile) {
				MainForm.StaticPostToDebug($"GetYouTubeVideoInformation: Skipped {(string.IsNullOrEmpty(Title) ? Link : Title)} download, Song is Local.");
				return;
			}

			if (string.IsNullOrEmpty(Title)) {
				if (InformationAquireTask != null) {
					await InformationAquireTask;
				} else {
					await GetYouTubeVideoInformation(YoutubeDLWorker);
				}
			}

			if (!Force && AudioCached()) {
				MainForm.StaticPostToDebug($"GetYouTubeAudioData: {(string.IsNullOrEmpty(Title) ? Link : Title)} Audio found, download canceled.");
				return;
			} else if (Force) {
				MainForm.StaticPostToDebug($"GetYouTubeVideoInformation: Forced download of {(string.IsNullOrEmpty(Title) ? Link : Title)} Audio");
			}
			DownloadWorking = true;

			if (GlobalFunctions.GetYouTubeVideoID(Link, out string ID)) {
				if (AudioCached()) {
					File.Delete(FullDirLocation);
				}
				RunResult<string> Youtubedata = null;
				Exception exception = null;

				try {
					VideoAquireTask = YoutubeDLWorker.RunAudioDownload("https://www.youtube.com/watch?v=" + ID, type);
					Youtubedata = await VideoAquireTask;
				} catch (Exception e) {
					exception = e;
				}

				if (exception != null) {
					MainForm.StaticPostToDebug($"{Title} : Error attempting to download song data. : {ErrorMessage = exception.Message}");
					LastPingFailed = true;
				} else if (Youtubedata != null && Youtubedata.Success) {
					string extension = $".{type.ToString().ToLower()}";
					string filenameTitle = Title;
					char[] invalidchars = Path.GetInvalidFileNameChars();
					foreach (char invalid in invalidchars) {
						filenameTitle = filenameTitle.Replace(invalid, 'X');
					}
					string newFilename = $"{Path.GetDirectoryName(Youtubedata.Data)}\\{ID} - {filenameTitle}{extension}";

					try {
						bool check;
						do { // Youtube-dl might still be processing the file so wait until it is created then rename
							check = true;
							IEnumerable<string> directories = Directory.GetFiles($"{Path.GetDirectoryName(Youtubedata.Data)}\\");
							foreach (string dir in directories) {
								if (dir.Contains(ID) && dir.Contains(extension)) {
									File.Move(dir, newFilename);
									check = false;
									break;
								}
							}
							await Task.Delay(1000);
						} while (check);
					} catch (Exception e) {
						MainForm.StaticPostToDebug($"Rename function failed on {Title} : {e.Message}");
					}

					string successMessage = $"Audio Download of {Link} Successfull: {newFilename}";

					MainForm.StaticPostToDebug(successMessage);

					//Find filename by ID then rename that entry to new one, need to change special characters before that too

					DirLocation = newFilename.Replace(Directory.GetCurrentDirectory(), "");
					LastValidPing = DateTime.Now;
					LastPingFailed = false;
				} else {
					if (Youtubedata == null) {
						MainForm.StaticPostToDebug($"GetYouTubeAudioData using link: {Link} Failed, Downloader encountered errors.");
					} else {
						string errors = "";
						foreach (string error in Youtubedata.ErrorOutput) {
							errors += error + " :: ";
						}
						MainForm.StaticPostToDebug(errors);
					}

					LastPingFailed = true;
				}

			} else {
				ErrorMessage = $"{Link} Failed, link was not recognised by Regex.";
				MainForm.StaticPostToDebug($"GetYouTubeAudioData using link: {Link} Failed, link was not recognised by Regex.");
				LastPingFailed = true;
			}

			VideoAquireTask = null;
			DownloadWorking = false;
		}

		public async Task DeleteCache() {
			if (AudioCached()) {
				bool pass = false;
				int attempts = 0;
				do {
					try {
						File.Delete(FullDirLocation);
						DirLocation = "";
						pass = true;
					} catch {
						await Task.Delay(500);
					}
				} while (!pass && ++attempts < 100);
			}
		}
	}
}
