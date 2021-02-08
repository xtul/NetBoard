using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static NetBoard.Model.Data.Post;

namespace NetBoard.Controllers.Helpers {
	public static class PosterExtensions {
		#region Tools
		public static AdministrativeLevel GetPosterLevel(ClaimsPrincipal principal) {
			AdministrativeLevel adminLevel = AdministrativeLevel.Anon;
			if (principal.IsInRole("admin")) adminLevel = AdministrativeLevel.Admin;
			if (principal.IsInRole("moderator")) adminLevel = AdministrativeLevel.Mod;
			if (principal.IsInRole("janitor")) adminLevel = AdministrativeLevel.Janitor;
			return adminLevel;
		}
		#endregion Tools
	}

    // https://stackoverflow.com/a/61104735
    public static class UnixtimeExtensions {
        public static readonly DateTime UNIXTIME_ZERO_POINT = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Converts a Unix timestamp (UTC timezone by definition) into a DateTime object
        /// </summary>
        /// <param name="value">An input of Unix timestamp in seconds or milliseconds format</param>
        /// <param name="localize">should output be localized or remain in UTC timezone?</param>
        /// <param name="isInMilliseconds">Is input in milliseconds or seconds?</param>
        /// <returns></returns>
        public static DateTime FromUnixtime(this long value, bool localize = false, bool isInMilliseconds = true) {
            DateTime result;

            if (isInMilliseconds) {
                result = UNIXTIME_ZERO_POINT.AddMilliseconds(value);
            } else {
                result = UNIXTIME_ZERO_POINT.AddSeconds(value);
            }

            if (localize)
                return result.ToLocalTime();
            else
                return result;
        }

        /// <summary>
        /// Converts a DateTime object into a Unix time stamp
        /// </summary>
        /// <param name="value">any DateTime object as input</param>
        /// <param name="isInMilliseconds">Should output be in milliseconds or seconds?</param>
        /// <returns></returns>
        public static long ToUnixtime(this DateTime value, bool isInMilliseconds = true) {
            if (isInMilliseconds) {
                return (long)value.ToUniversalTime().Subtract(UNIXTIME_ZERO_POINT).TotalMilliseconds;
            } else {
                return (long)value.ToUniversalTime().Subtract(UNIXTIME_ZERO_POINT).TotalSeconds;
            }
        }
    }
}
