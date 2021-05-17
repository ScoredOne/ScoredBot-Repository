using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace TwitchBotManager.Code.Classes {

	public class SongRequestData {
		public string Link;
		public string Requester;
		public string Title;
		public int? LengthSec;

		public SongRequestData(string link, string requester, string title = null, int? length = null) {
			Link = link;
			Requester = requester;
			Title = title;
			LengthSec = length;
		}

		public override string ToString() {
			string output = "";
			if (!string.IsNullOrEmpty(Title)) {
				output += "|# Title #| : " + Title;
			} else {
				output += "|# Title #| : " + "#TITLE MISSING#";
			}
			if (!string.IsNullOrEmpty(Link)) {
				output += " |# Link #| : " + Link;
			} else {
				output += " |# Link #| : " + "#LINK MISSING#";
			}
			if (!string.IsNullOrEmpty(Requester)) {
				output += " |# Requester #| : " + Requester;
			} else {
				output += " |# Requester #| : " + "#REQUESTER MISSING#";
			}
			return output;
		}

		public static SongRequestData ConvertJSONData(string value) {
			Dictionary<string, string> valuePairs = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(value);

			if (valuePairs == null) {
				return null;
			}

			if (int.TryParse(valuePairs["LengthSec"], out int intvalue)) {
				return new SongRequestData(valuePairs["Link"], valuePairs["Requester"], valuePairs["Title"], intvalue);
			}

			return new SongRequestData(valuePairs["Link"], valuePairs["Requester"], "", 0);
		}
	}
}
