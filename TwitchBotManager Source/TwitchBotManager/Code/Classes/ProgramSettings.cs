using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using Newtonsoft.Json;

namespace ScoredBot.Settings {
	public class SettingsOutputStructure {

		public readonly Action OnDataWriten;

		public SettingsOutputStructure(Action datawritenaction) {
			OnDataWriten = datawritenaction;
		}

		private string _username;
		public string UserName {
			get => _username;
			set {
				if (_username != value) {
					_username = value;
					OnDataWriten.Invoke();
				}
			}
		}

		private string _oauth;
		public string OAuth {
			get => _oauth;
			set {
				if (_oauth != value) {
					_oauth = value;
					OnDataWriten.Invoke();
				}
			}
		}

		private string _secret;
		public string Secret {
			get => _secret;
			set {
				if (_secret != value) {
					_secret = value;
					OnDataWriten.Invoke();
				}
			}
		}

		private string _target;
		public string Target {
			get => _target;
			set {
				if (_target != value) {
					_target = value;
					OnDataWriten.Invoke();
				}
			}
		}

		private int _appMusicVolume;
		public int AppMusicVolume {
			get => _appMusicVolume;
			set {
				if (value > 100) {
					value = 100;
				} else if (value < 0) {
					value = 0;
				}
				if (_appMusicVolume != value) {
					_appMusicVolume = value;
					OnDataWriten.Invoke();
				}
			}
		}

		private int _appSoundEffectsVolume;
		public int AppSoundEffectsVolume {
			get => _appSoundEffectsVolume;
			set {
				if (value > 100) {
					value = 100;
				} else if (value < 0) {
					value = 0;
				}
				if (_appSoundEffectsVolume != value) {
					_appSoundEffectsVolume = value;
					OnDataWriten.Invoke();
				}
			}
		}

		private int _appMusicMaxRequests;
		public int AppMusicMaxRequests {
			get => _appMusicMaxRequests;
			set {
				if (value > 100) {
					value = 100;
				} else if (value < 1) {
					value = 1;
				}
				if (_appMusicMaxRequests != value) {
					_appMusicMaxRequests = value;
					OnDataWriten.Invoke();
				}
			}
		}

		private bool _cacheNewSongs;
		public bool CacheNewSongs {
			get => _cacheNewSongs;
			set {
				if (_cacheNewSongs != value) {
					_cacheNewSongs = value;
					OnDataWriten.Invoke();
				}
			}
		}

		private ObservableCollection<string> _other = new ObservableCollection<string>();
		public ObservableCollection<string> Other {
			get => _other;
			set {
				if (_other != value) {
					_other = value;
					OnDataWriten.Invoke();
				}
			}
		}

		protected class SettingsData {
			public string UserName;
			public string OAuth;
			public string Secret;
			public string Target;
			public int AppMusicVolume;
			public int AppSoundEffectsVolume;
			public int AppMusicMaxRequests;
			public bool CacheNewSongs;
			public List<string> Other;

			public SettingsData(string username,
				string oauth,
				string secret,
				string target,
				int appmusicvolume,
				int appsoundeffectsvolume,
				int appmusicmaxrequests,
				bool cachenewsongs,
				List<string> other) {
				UserName = username;
				OAuth = oauth;
				Secret = secret;
				Target = target;
				AppMusicVolume = appmusicvolume;
				AppSoundEffectsVolume = appsoundeffectsvolume;
				AppMusicMaxRequests = appmusicmaxrequests;
				CacheNewSongs = cachenewsongs;
				Other = other;
			}
		}

		public void InputDataJSON(string JSON) {
			SettingsData data = JsonConvert.DeserializeObject<SettingsData>(JSON);
			if (data != null) {
				_username = data.UserName;
				_oauth = data.OAuth;
				_secret = data.Secret;
				_target = data.Target;
				_appMusicVolume = data.AppMusicVolume;
				_appSoundEffectsVolume = data.AppSoundEffectsVolume;
				_appMusicMaxRequests = data.AppMusicMaxRequests;
				_cacheNewSongs = data.CacheNewSongs;
				_other = new ObservableCollection<string>(data.Other == null ? new List<string>() : data.Other);
				_other.CollectionChanged += (o, e) => OnDataWriten.Invoke();
			}
		}

		public string ConvertToJSON() {
			return JsonConvert.SerializeObject(new SettingsData(UserName,
				OAuth,
				Secret,
				Target,
				AppMusicVolume,
				AppSoundEffectsVolume,
				AppMusicMaxRequests,
				CacheNewSongs,
				Other.ToList()
				), Formatting.Indented);
		}
	}

	public static class ProgramSettings {

		public static readonly SettingsOutputStructure AppSettings;

		public static EventHandler OnAppSettingsLoaded;

		static ProgramSettings() {
			CheckAndCreateOutputDirectoryFiles();
			AppSettings = new SettingsOutputStructure(() => SaveSettingsToFile());
			ReadSettingFromFile();
		}

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
			if (!File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\Settings.txt")) {
				File.Create(Directory.GetCurrentDirectory() + @"\Outputs\Settings.txt").Close();
			}
		}

		public static void SaveSettingsToFile() {
			if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\Settings.txt")) {
				File.WriteAllText(Directory.GetCurrentDirectory() + @"\Outputs\Settings.txt", AppSettings.ConvertToJSON());
			}
		}

		public static void ReadSettingFromFile() {
			if (File.Exists(Directory.GetCurrentDirectory() + @"\Outputs\Settings.txt")) {
				string input = File.ReadAllText(Directory.GetCurrentDirectory() + @"\Outputs\Settings.txt");
				if (!string.IsNullOrEmpty(input)) {
					AppSettings.InputDataJSON(input);
					OnAppSettingsLoaded?.Invoke(new object(), EventArgs.Empty);
				} else {
					goto writenewsettings;
				}
				return;
			}
			writenewsettings: SaveSettingsToFile();
		}
	}
}
