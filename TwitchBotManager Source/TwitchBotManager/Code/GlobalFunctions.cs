using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ScoredBot.Code.Classes {
	public static class GlobalFunctions {

		static GlobalFunctions() { }

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
			string linkCopy;
			Match Regexmatch;
			if (link.Length > 11) {
				linkCopy = link.Split('&')[0];

				Regexmatch = new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)").Match(linkCopy);

				if (Regexmatch.Success) {
					ID = Regexmatch.Groups[1].Value;
					return true;
				} else {
					ID = null;
					return false;
				}
			} else if (link.Length == 11) {
				Regexmatch = new Regex(@"([a-zA-Z0-9-_]+)").Match(link);

				if (Regexmatch.Success) {
					ID = link;
					return true;
				} else {
					ID = null;
					return false;
				}
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

		public static NameValueCollection ParseDictionaryToNVC(Dictionary<string, string> dic) {
			NameValueCollection valueCollection = new NameValueCollection(dic.Count);
			foreach (KeyValuePair<string, string> pair in dic) {
				valueCollection.Add(pair.Key, pair.Value);
			}
			return valueCollection;
		}

		public static string BoolToWordEnabled(bool value) => value ? "Enabled" : "Diabled";
	}
}
