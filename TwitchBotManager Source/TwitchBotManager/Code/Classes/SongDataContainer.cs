using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
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
		/// Users currently requesting the song 
		/// </summary>
		[JsonIgnore]
		public readonly LinkedList<string> CurrentRequesters = new LinkedList<string>();

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

		public bool AudioCached() => File.Exists(DirLocation);

		public TimeSpan LengthInTime => TimeSpan.FromSeconds(LengthSec);

		private SongDataContainer() { }

		/// <summary>
		/// Song Constructor for local/secondary playlist files
		/// </summary>
		public static SongDataContainer CreateNewContainer(string dirLocation, string requester, string link = "", DateTime lastpingdate = new DateTime(), string title = "", int length = 0, bool failedping = false, bool local = false) {
			return new SongDataContainer() {
				Link = link,
				OriginalRequester = requester,
				Title = title,
				LengthSec = length,
				DirLocation = dirLocation,
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
			container.CurrentRequesters.AddFirst(requester);

			if (youtubeDL == null) {
				return container;
			}

			await container.GetYouTubeVideoInformation(youtubeDL);

			if (getaudiodata) {
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
		public string OutputString(int Requester, bool newLines = false) {
			string output = "";

			output += $"{(newLines ? Environment.NewLine : string.Empty)}|# Title #| : {(string.IsNullOrEmpty(Title) ? "#TITLE MISSING#" : Title)} ";

			output += $"{(newLines ? Environment.NewLine : string.Empty)}|# Link #| : {(string.IsNullOrEmpty(Link) ? "#LINK MISSING#" : Link)} ";

			if (Requester == -1) {
				output += $"{(newLines ? Environment.NewLine : string.Empty)}|# Requester #| : {(string.IsNullOrEmpty(OriginalRequester) ? "#REQUESTER MISSING#" : OriginalRequester)} ";
			} else if (Requester < CurrentRequesters.Count) {
				output += $"{(newLines ? Environment.NewLine : string.Empty)}|# Requester #| : {CurrentRequesters.ElementAt(Requester)} ";
			} else {
				throw new ArgumentOutOfRangeException("Requester Value Exceeds CurrentRequesters.Count");
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
				{ nameof(CurrentRequesters), CurrentRequesters.MergeList('/') },
				{ nameof(Title), Title },
				{ nameof(LengthSec), LengthSec.ToString() },
				{ nameof(DirLocation), DirLocation },
				{ nameof(LastValidPing), LastValidPing.ToString() },
				{ nameof(LastPingFailed), LastPingFailed.ToString() },
				{ nameof(PingValid), PingValid.ToString() },
				{ nameof(LocalFile), LocalFile.ToString() },
				{ nameof(AudioCached), AudioCached().ToString() }
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

		public async Task<bool> GetYouTubeVideoInformation(YoutubeDL YoutubeDLWorker, bool Force = false) {
			if (YoutubeDLWorker == null) {
				throw new NullReferenceException("GetYouTubeVideoInformation: YoutubeDLWorker was provided null");
			}

			if (LocalFile) {
				MainForm.StaticPostToDebug($"GetYouTubeVideoInformation: Skipped {(string.IsNullOrEmpty(Title) ? Link : Title)} data, Song is Local.");
				return true;
			}

			if (!Force && PingValid) {
				MainForm.StaticPostToDebug($"GetYouTubeVideoInformation: {(string.IsNullOrEmpty(Title) ? Link : Title)} is still within valid period, download canceled.");
				return true;
			} else if (Force && PingValid) {
				MainForm.StaticPostToDebug($"GetYouTubeVideoInformation: Forced download of {(string.IsNullOrEmpty(Title) ? Link : Title)} data.");
			}

			if (GlobalFunctions.GetYouTubeVideoID(Link, out string youtubeMatch)) {
				RunResult<VideoData> Youtubedata = await YoutubeDLWorker.RunVideoDataFetch("https://www.youtube.com/watch?v=" + youtubeMatch
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

				if (Youtubedata != null && Youtubedata.Success) {
					Title = Youtubedata.Data.Title;
					LengthSec = (int)Youtubedata.Data.Duration;
					LastValidPing = DateTime.Now;
					LastPingFailed = false;
					MainForm.StaticPostToDebug($"Secondary Song Info Downloaded... {Title}");
				} else {
					if (Youtubedata == null) {
						MainForm.StaticPostToDebug("https://www.youtube.com/watch?v=" + youtubeMatch + " Crashed Out");
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
				string errorMessage = $"GetYouTubeVideoInformation using link: {Link} Failed, link was not recognised by Regex.";
				MainForm.StaticPostToDebug(errorMessage);
				LastPingFailed = true;
			}

			return PingValid;
		}

		public async Task<bool> GetYouTubeAudioData(YoutubeDL YoutubeDLWorker, YoutubeDLSharp.Options.AudioConversionFormat type = YoutubeDLSharp.Options.AudioConversionFormat.Mp3, bool Force = false) {
			if (YoutubeDLWorker == null) {
				throw new NullReferenceException("GetYouTubeAudioData: YoutubeDLWorker was provided null");
			}

			if (LocalFile) {
				MainForm.StaticPostToDebug($"GetYouTubeVideoInformation: Skipped {(string.IsNullOrEmpty(Title) ? Link : Title)} download, Song is Local.");
				return true;
			}

			if (string.IsNullOrEmpty(Title)) {
				await GetYouTubeVideoInformation(YoutubeDLWorker);
			}

			if (!Force && AudioCached()) {
				MainForm.StaticPostToDebug($"GetYouTubeAudioData: {(string.IsNullOrEmpty(Title) ? Link : Title)} Audio found, download canceled.");
				return true;
			} else if (Force) {
				MainForm.StaticPostToDebug($"GetYouTubeVideoInformation: Forced download of {(string.IsNullOrEmpty(Title) ? Link : Title)} Audio");
			}

			if (GlobalFunctions.GetYouTubeVideoID(Link, out string ID)) {
				if (AudioCached()) {
					File.Delete(DirLocation);
				}
				RunResult<string> Youtubedata = await YoutubeDLWorker.RunAudioDownload("https://www.youtube.com/watch?v=" + ID, type);

				if (Youtubedata.Success) {
					string extension = $".{type.ToString().ToLower()}";
					string filenameTitle = Title;
					char[] invalidchars = Path.GetInvalidFileNameChars();
					foreach (char invalid in invalidchars) {
						filenameTitle = filenameTitle.Replace(invalid, 'X');
					}
					string newFilename = $"{Path.GetDirectoryName(Youtubedata.Data)}\\{ID} - {filenameTitle}{extension}";

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
					} while (check);

					string successMessage = $"Audio Download of {Link} Successfull: {newFilename}";

					MainForm.StaticPostToDebug(successMessage);

					//Find filename by ID then rename that entry to new one, need to change special characters before that too

					DirLocation = newFilename;
					LastValidPing = DateTime.Now;
					LastPingFailed = false;
				} else {
					string errorMessage = $"GetYouTubeAudioData using link: {Link} Failed, Downloader encountered errors.";
					MainForm.StaticPostToDebug(errorMessage);

					foreach (string errors in Youtubedata.ErrorOutput) {
						MainForm.StaticPostToDebug(errors);
					}

					LastPingFailed = true;
				}

			} else {
				string errorMessage = $"GetYouTubeAudioData using link: {Link} Failed, link was not recognised by Regex.";
				MainForm.StaticPostToDebug(errorMessage);
				Console.WriteLine(errorMessage);
				LastPingFailed = true;
			}

			return AudioCached();
		}

		public async Task DeleteCache() {
			if (AudioCached()) {
				bool pass = false;
				do {
					try {
						File.Delete(DirLocation);
						DirLocation = "";
						pass = true;
					} catch {
						await Task.Delay(500);
					}
				} while (!pass);
			}
		}
	}
}
