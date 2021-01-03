using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetBoard.Model.ExtensionMethods {
	public static class StringManipulation {
		/// <summary>
		/// Checks if this string is null or empty, while treating spaces as an empty string.
		/// </summary>
		public static bool IsNullOrEmptyWithTrim(this string value) {
			bool isEmpty = string.IsNullOrEmpty(value);
			if (isEmpty) {
				return true;
			}
			return value.Trim().Length == 0;
		}

		/// <summary>
		/// Converts string to Title Case.
		/// </summary>
		public static string ToTitleCase(this string text, char seperator = ' ') =>
			string.Join(seperator, text.Split(seperator).Select(word => new string(
				word.Select((letter, i) => i == 0 ? char.ToUpper(letter) : char.ToLower(letter)).ToArray())));

		/// <summary>
		/// Reduces <paramref name="str"/>'s length to <paramref name="desiredLength"/> and adds <paramref name="cutoffText"/> at the end.
		/// </summary>
		public static string ReduceLength(this string str, int desiredLength, string cutoffText) {
			if (str.Length > desiredLength) {
				return str.Substring(0, desiredLength) + cutoffText;
			}
			return str;
		}
	}
}
