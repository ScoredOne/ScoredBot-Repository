namespace TwitchBotManager.Code.Classes {
	public class SongOutputText {

		private string _inputstring;
		public string InputString {
			private get => _inputstring;
			set {
				_inputstring = value;
				Reset();
			}
		}

		private readonly static int OutputLength = 95;

		/// <summary>
		/// Outputs the string to the length of 75, progresses 1 to the right with each call.
		/// </summary>
		public string OutputString { get => Progress(); }

		private int lowerValue = 0;
		private int upperValue = OutputLength;

		private void Reset() {
			lowerValue = 0;
			upperValue = OutputLength;
		}

		private string Progress() {
			if (string.IsNullOrEmpty(InputString) || InputString.Length < upperValue) {
				return InputString;
			}

			if (lowerValue >= InputString.Length) {
				lowerValue = 0;
			}
			if (upperValue >= InputString.Length) {
				upperValue = 0;
			}

			char[] array = InputString.ToCharArray();

			string output = "";

			// Because substring broke... why... dont know, said out of array but it wasnt so this is the backup...
			if (lowerValue >= upperValue) {
				int count = 0;
				for (int x = lowerValue; x < array.Length; x++, count++) {
					output += array[x];
				}
				for (int x = 0; x < upperValue; x++) {
					output += array[x];
					if (++count >= OutputLength) {
						break;
					}
				}
			} else {
				for (int x = lowerValue; x < upperValue; x++) {
					output += array[x];
				}
			}

			lowerValue++;
			upperValue++;

			return output;
		}

		public SongOutputText() { }
	}
}
