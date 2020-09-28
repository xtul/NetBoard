using NetBoard.Model.ExtensionMethods;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;
using System.Security.Cryptography;

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
	}
}