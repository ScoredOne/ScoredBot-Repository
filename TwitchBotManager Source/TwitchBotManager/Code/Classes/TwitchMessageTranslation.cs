using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TwitchLib.Client.Models;

namespace ScoredBot.Code.Classes {

	public class TwitchMessageTranslation {

		public enum MessageLabels {
			Me,
			Broadcaster,
			Staff,
			Mod,
			Vip,
			Highlighted,
			Turbo,
			Partner,
			SkipedSubOnly
		}

		public string TimeStamp;
		public string UserName;
		public List<MessageLabels> Labels = new List<MessageLabels>();
		public string Message;
		public string BitValue;
		public string SubAmount;

		public TwitchMessageTranslation(ChatMessage baseMessage) {
			TimeStamp = baseMessage.TmiSentTs;
			UserName = baseMessage.Username;

			if (baseMessage.IsMe) {
				Labels.Add(MessageLabels.Me);
			}
			if (baseMessage.IsBroadcaster) {
				Labels.Add(MessageLabels.Broadcaster);
			}
			if (baseMessage.IsStaff) {
				Labels.Add(MessageLabels.Staff);
			}
			if (baseMessage.IsModerator) {
				Labels.Add(MessageLabels.Mod);
			}
			if (baseMessage.IsVip) {
				Labels.Add(MessageLabels.Vip);
			}
			if (baseMessage.IsHighlighted) {
				Labels.Add(MessageLabels.Highlighted);
			}
			if (baseMessage.IsTurbo) {
				Labels.Add(MessageLabels.Turbo);
			}
			if (baseMessage.IsPartner) {
				Labels.Add(MessageLabels.Partner);
			}
			if (baseMessage.IsSkippingSubMode) {
				Labels.Add(MessageLabels.SkipedSubOnly);
			}

			Message = baseMessage.Message;
			BitValue = baseMessage.Bits.ToString();
			SubAmount = baseMessage.SubscribedMonthCount.ToString();
		}

		public override string ToString() {
			return TimeStamp + " || " +
				TranslateLabels(Labels, '|') +
				UserName + " /' " + Message + " '/";
		}

		private string TranslateLabels(List<MessageLabels> list, char seperatingChar) {
			string output = "";

			list.ForEach(e => {
				output += e.ToString() + seperatingChar;
			});

			return output;
		}

	}
}
