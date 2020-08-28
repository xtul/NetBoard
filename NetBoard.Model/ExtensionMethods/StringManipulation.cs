using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetBoard.Model.ExtensionMethods {
	public static class StringManipulation {
		public static bool IsNullOrEmptyWithTrim(this string value) {
			bool isEmpty = string.IsNullOrEmpty(value);
			if (isEmpty) {
				return true;
			}
			return value.Trim().Length == 0;
		}

		public static string ToTitleCase(this string text, char seperator = ' ') =>
			string.Join(seperator, text.Split(seperator).Select(word => new string(
				word.Select((letter, i) => i == 0 ? char.ToUpper(letter) : char.ToLower(letter)).ToArray())));

		public static string ReduceLength(this string str, int desiredLength, string cutoffText) {
			if (str.Length > desiredLength) {
				return str.Substring(0, desiredLength) + cutoffText;
			}
			return str;
		}
	}
}
