using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;

namespace TwitchBotManager.Code.Classes {
	public static class GlobalFunctions {

		static GlobalFunctions() {}

		public static void CheckAndCreateOutputDirectoryFiles() {
			Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Outputs");
			Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Outputs\ChatHistory");
			Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Outputs\CachedSongs");

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
			if (!File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\Settings.txt")) {
				File.Create(Directory.GetCurrentDirectory() + @"\Outputs\Settings.txt").Close();
			}

		}

		public static (string, string, string, string) LoadLoginFromFile() {
			if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\SavedLoginInfo.txt")) {
				string[] input = File.ReadAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SavedLoginInfo.txt");
				switch (input.Length) {
					case 0:
						break;
					case 2:
						return (input[0], input[1], "", "");
					case 3:
						return (input[0], input[1], "", input[2]);
					default:
						File.WriteAllLines(Directory.GetCurrentDirectory() + @"\Outputs\SavedLoginInfo.txt", new string[] { });
						MessageBox.Show("The file containing the login detail seems to have corrupted, the file has been reset.", "Error loading Login Details", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						break;
				}
			}

			return ("", "", "", "");
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

		public static bool GetYouTubeVideoID(string link, out string ID) {
			string linkCopy = link.Split('&')[0];

			Match Regexmatch = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)").Match(linkCopy);

			if (Regexmatch.Success) {
				ID = Regexmatch.Groups[1].Value;
				return true;
			} else {
				ID = null;
				return false;
			}
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
		public static void ExecuteThreadSafeActionToMultiple<T>(Action<T> action, params T[] controls) where T : Control {
			foreach (T control in controls) {
				control.ThreadSafeAction(action);
			}
		}

		public static void SafeInvoke<T>(this EventHandler<T> handler, object self, T paramater) {
			if (handler == null) {
				return;
			} else {
				handler.Invoke(self, paramater);
			}
		}

		public static void SafeInvoke(this EventHandler handler, object self, EventArgs paramater) {
			if (handler == null) {
				return;
			} else {
				handler.Invoke(self, paramater);
			}
		}

		public static string MergeList(this IEnumerable<string> list, char conjoiningChar) {
			int count = list.Count();
			if (count == 0) {
				return "";
			} else if (count == 1) {
				return list.First();
			}
			string output = "";
			for (int x = 0; x < count; x++) {
				output += list.ElementAt(x);
				if (x < count - 1) {
					output += conjoiningChar;
				}
			}
			return output;
		}

		public static NameValueCollection ParseDictionaryToNVC(Dictionary<string,string> dic) {
			NameValueCollection valueCollection = new NameValueCollection(dic.Count);
			foreach (KeyValuePair<string, string> pair in dic) {
				valueCollection.Add(pair.Key, pair.Value);
			}
			return valueCollection;
		}
	}
}
