using NetBoard.Model.ExtensionMethods;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Net;

namespace NetBoard.Model.Data {
	public class PostStructure {
		[Key]
		public int Id { get; set; }

		private string _image;
		public string Image {
			get { return _image; }
			set {
				if (value.IsNullOrEmptyWithTrim()) {
					_image = null;
					return;
				}
				_image = value;
			}
		}
		[StringLength(5)]
		[NotMapped]
		public string ImageExt { get; set; }
		[StringLength(4000)]
		public string Content { get; set; }

		[NotMapped]
		public string ContentShort {
			get {
				if (Content.Length > 255) {
					return Content.Substring(0, 251) + "..."; 
				}
				return Content;
			}
		}

		private string _name;
		[StringLength(32)]
		public string Name { 
			get { return _name; } 
			set {
				if (value.IsNullOrEmptyWithTrim()) {
					_name = "Anonymous";
					return;
				}
				_name = value;
			}
		}
		[StringLength(64)]
		public string Password { get; set; }
		public DateTime PostedOn { get; set; }
		public bool? SpoilerImage { get; set; }

		private string _subject;
		[StringLength(64)]
		public string Subject {
			get { return _subject; }
			set {
				if (value.IsNullOrEmptyWithTrim()) {
					_subject = null;
					return;
				}
				_subject = value;
			}
		}
		[DefaultValue(false)]
		public bool Archived { get; set; }
		public AdministrativeLevel PosterLevel { get; set; }
		public int? Thread { get; set; }
		[DefaultValue(false)]
		public bool Sticky { get; set; }
		public DateTime LastPostDate { get; set; }
		public string PosterIP { get; set; }
		public bool? ShadowBanned { get; set; }
		[NotMapped]
		public List<PostStructure> Responses { get; set; }
		[NotMapped]
		public int? ResponseCount { get; set; }
		[NotMapped]
		public int? ImageCount { get; set; }
		[NotMapped]
		public int? UniquePosterCount { get; set; }
		[NotMapped]
		[StringLength(32)]
		public string Options { get; set; }
		/// <summary>
		/// Used in data transfer to determine whether given post came from GETting IP
		/// </summary>
		[NotMapped]
		public bool You { get; set; } 
		[NotMapped]
		public string CaptchaCode { get; set; }
		public bool? PastLimits { get; set; }


		public enum AdministrativeLevel {
			Anon,
			Janitor,
			Mod,
			Admin,
		}

		#region Password

		protected static readonly int HashLength = 24;
		protected static readonly int SaltLength = 16;
		protected static readonly int Iterations = 9175;

		// https://medium.com/@mehanix/lets-talk-security-salted-password-hashing-in-c-5460be5c3aae
		/// <summary>
		/// Encrypts provided password and fills Password parameter.
		/// </summary>
		/// <param name="pw">A password to store.</param>
		public void SetPassword(string pw) {
			// generate salt
			byte[] salt;
			new RNGCryptoServiceProvider().GetBytes(salt = new byte[SaltLength]);

			// generate hash
			var hash = new Rfc2898DeriveBytes(pw, salt, Iterations).GetBytes(HashLength);

			// generate complete string for storage
			var completeHash = new byte[HashLength + SaltLength];
			Array.Copy(salt, 0, completeHash, 0, SaltLength);
			Array.Copy(hash, 0, completeHash, SaltLength, HashLength);

			Password = Convert.ToBase64String(completeHash);
		}

		/// <summary>
		/// Checks if password of this post is correct.
		/// </summary>
		/// <param name="pw">Password to try out.</param>
		/// <returns>True if correct.</returns>
		public bool TestPassword(string pw) {
			var encryptedPassword = Convert.FromBase64String(Password);
			// extract salt
			var salt = new byte[SaltLength];
			Array.Copy(encryptedPassword, 0, salt, 0, SaltLength);

			// generate hash from provided password
			var hash = new Rfc2898DeriveBytes(pw, salt, Iterations).GetBytes(HashLength);

			// check if generated hash is the same as the one stored
			bool result = true;
			for (int i = 0; i < HashLength; i++) {
				if (encryptedPassword[i + SaltLength] != hash[i]) {
					result = false;
				}
			}

			return result;
		}

		#endregion Password

		#region Shadowbanned

		public bool IsShadowbanned() {
			return ShadowBanned.HasValue && ShadowBanned.Value == true;
		}

		/// <summary>
		/// Determines whether this shadowbanned post should be displayed. Shadowbanned posts are only displayed to the person that posted it.
		/// </summary>
		/// <param name="connectingIp"></param>
		/// <returns></returns>
		public bool ShouldDisplayShadowbanned(IPAddress connectingIp) {
			if (!IsShadowbanned()) {
				return true; // it isn't shadowbanned, display it
			}
			// display only if poster IP is equal to IP requesting this post
			return IPAddress.Parse(PosterIP).Equals(connectingIp);
		}

		#endregion Shadowbanned

		#region Utilities
		/// <summary>
		/// Determines whether this post was made by connecting IP.
		/// </summary>
		public bool IsYou(IPAddress clientIP) {
			var ip = PosterIP ?? "127.0.0.1";

			return IPAddress.Parse(ip).Equals(clientIP);
		}

		/// <summary>
		/// Removes sensitive/useless data from this post, allowing it to be used in data transfer (eg. API response).
		/// Make sure you don't save this post to database! When in doubt, use <see cref="CloneAsDTO"/>.
		/// </summary>
		public void AsDTO(int contentPreviewLength, string contentCutoffText, IPAddress clientIp, int? threadId = null, bool isOP = false) {
			Password = null;
			You = IsYou(clientIp);
			PosterIP = null;
			ShadowBanned = null;
			if (Image == null) SpoilerImage = null;

			if (!isOP) {
				Subject = null; // responses don't have subjects, silly
			}

			Content = Content.ReduceLength(contentPreviewLength, contentCutoffText);
			if (threadId.HasValue) Thread = threadId.Value;
		}

		/// <summary>
		/// Removes sensitive/useless data from this post, allowing it to be used in data transfer (eg. API response).
		/// Make sure you don't save this post to database! When in doubt, use <see cref="CloneAsDTO"/>.
		/// Skips content reduction.
		/// </summary>
		public void AsDTO(IPAddress clientIp, int? threadId = null, bool isOP = false) {
			AsDTO(4000, "", clientIp, threadId, isOP);
		}

		/// <summary>
		/// Creates a new <see cref="PostStructure"/> based on this one in a version that can be used in data transfer (eg. API response).
		/// </summary>
		/// <returns>A new <see cref="PostStructure"/> as DTO.</returns>
		public PostStructure CloneAsDTO(int contentPreviewLength, string contentCutoffText, IPAddress posterIp, int? threadId = null) {
			var clone = this;
			clone.AsDTO(contentPreviewLength, contentCutoffText, posterIp, threadId);

			return clone;
		}
		#endregion Utilities
	}
}