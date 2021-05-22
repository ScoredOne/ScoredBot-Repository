using System;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace TwitchBotManager.Code.Classes {
	public static class GlobalFunctions {

		private readonly static YoutubeDL YoutubeDLWorker;

		static GlobalFunctions() {
			// TODO : add optional file path instillation - Disable related functionality if error
			YoutubeDLWorker = new YoutubeDL(100) { // Python restricted
				YoutubeDLPath = Directory.GetCurrentDirectory() + @"\youtube-dl\youtube-dl.exe",
				FFmpegPath = Directory.GetCurrentDirectory() + @"\ffmpeg\ffmpeg.exe"
			};
		}

		public static void CheckAndCreateOutputDirectoryFiles() {
			Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Outputs");
			Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Outputs\ChatHistory");

			if (!File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\LatestFollowSubBits.txt")) {
				File.Create(Directory.GetCurrentDirectory() + @"\Outputs\LatestFollowSubBits.txt").Close();
			}
			if (!File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\CurrentSong.txt")) {
				File.Create(Directory.GetCurrentDirectory() + @"\Outputs\CurrentSong.txt").Close();
			}
			if (!File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\CurrentChatLog.txt")) {
				File.Create(Directory.GetCurrentDirectory() + @"\Outputs\CurrentChatLog.txt").Close();
			}
			if (!File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt")) {
				File.Create(Directory.GetCurrentDirectory() + @"\Outputs\SongRequestData.txt").Close();
			}
			if (!File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\SavedLoginInfo.txt")) {
				File.Create(Directory.GetCurrentDirectory() + @"\Outputs\SavedLoginInfo.txt").Close();
			}
			if (!File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\MediaVolume.txt")) {
				File.Create(Directory.GetCurrentDirectory() + @"\Outputs\MediaVolume.txt").Close();
			}
		}

		public static (string, string, string) LoadLoginFromFile() {
			if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\SavedLoginInfo.txt")) {
				string[] input = File.ReadAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SavedLoginInfo.txt");
				switch (input.Length) {
					case 0:
						break;
					case 2:
						return (input[0], input[1], "");
					case 3:
						return (input[0], input[1], input[2]);
					default:
						File.WriteAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SavedLoginInfo.txt", new string[] { });
						MessageBox.Show("The file containing the login detail seems to have corrupted, the file has been reset.", "Error loading Login Details", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						break;
				}
			}

			return ("", "", "");
		}

		public static int LoadMediaPlayerVolume() {
			if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\MediaVolume.txt")) {
				if (int.TryParse(File.ReadAllText(Directory.GetCurrentDirectory() + @"\Outputs\MediaVolume.txt"), out int value)) {
					if (value > 100) {
						return 100;
					} else if (value < 0) {
						return 0;
					} else {
						return value;
					}
				}
			}

			return 100;
		}

		/// <summary>
		/// Updates song request file and a label associated with song requests
		/// </summary>
		/// <param name="label"></param>
		/// <param name="output"></param>
		public static void UpdateSongRequest(Label label, string output) {
			label.ThreadSafeAction(e => e.Text = output);
			if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\CurrentSong.txt")) {
				File.WriteAllText(Directory.GetCurrentDirectory() + @"\Outputs\CurrentSong.txt", output);
			}
		}

		/// <summary>
		/// Updates song request file
		/// </summary>
		/// <param name="label"></param>
		/// <param name="output"></param>
		public static void UpdateSongRequest(string output) {
			if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\CurrentSong.txt")) {
				File.WriteAllText(Directory.GetCurrentDirectory() + @"\Outputs\CurrentSong.txt", output);
			}
		}

		public async static Task<(string Link, NameValueCollection Details)> RegexYouTubeLink(string link) {
			link = link.Split('&')[0];
			if (!link.Contains("https://")) {
				link = "https://" + link;
			}

			Match Regexmatch = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)").Match(link);
			if (Regexmatch.Success) {

				string youtubeMatch = Regexmatch.Groups[1].Value;

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

				NameValueCollection valueCollection = null;
				if (Youtubedata != null && Youtubedata.Success) {
					valueCollection = new NameValueCollection {
						{ "title", Youtubedata.Data.Title },
						{ "lengthSeconds", Youtubedata.Data.Duration.ToString() }
					};
				} else {
					if (Youtubedata == null) {
						Console.WriteLine("https://www.youtube.com/watch?v=" + youtubeMatch + " Crashed Out");
					} else {
						foreach (string error in Youtubedata.ErrorOutput) {
							Console.WriteLine(error);
						}
					}
				}

				return (link, valueCollection);
			}

			return (null, null);
		}

		/// <summary>
		/// Performs .InvokeRequired check and the Action required of the control in a thread safe way
		/// </summary>
		/// <param name="control"></param>
		/// <param name="action"></param>
		public static void ThreadSafeAction<T>(this T control, Action<T> action) where T : Control {
			if (control.IsDisposed || control.Disposing || MainForm.IsExiting) {
				return;
			}

			if (control.InvokeRequired) {
				try { // TODO : InvalidAsynchronousStateException ... currently 2am lazy fix
					control.Invoke(action, control);
				} catch {
					return;
				}
			} else {
				action.Invoke(control);
			}
		}

		/// <summary>
		/// Function to save space on writing multiple ThreadSafeAction functions.
		/// Makes all controls execute ThreadSafeAction with the same Action.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="controls"></param>
		public static void ExecuteMultipleThreadSafeActions<T>(Action<T> action, params T[] controls) where T : Control {
			foreach (T control in controls) {
				control.ThreadSafeAction(action);
			}
		}
	}
}
