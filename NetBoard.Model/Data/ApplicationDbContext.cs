using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetBoard.Model.Data.Objects;
using Newtonsoft.Json.Linq;

namespace NetBoard.Model.Data {
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int> {
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

		/// <summary>
		/// Used to make generic board queries.
		/// </summary>
		public DbSet<Post> Posts { get; set; }

		// board dbsets will be obsolete in the future
		// public DbSet<G> GPosts { get; set; }
		// public DbSet<Meta> MetaPosts { get; set; }
		// public DbSet<Diy> DiyPosts { get; set; }
		public DbSet<Report> Reports { get; set; }
		public DbSet<Sage> Sages { get; set; }
		public DbSet<Ban> Bans { get; set; }
		public DbSet<MarkedForDeletion> MarkedForDeletion { get; set; }
		public DbSet<ImageQueue> ImageQueue { get; set; }
		public DbSet<FrontpageData> FrontpageData { get; set; }

		protected override void OnModelCreating(ModelBuilder b) {
			base.OnModelCreating(b);
			b.HasDefaultSchema("netboard");

			// configure all boards
			// https://stackoverflow.com/a/49677172/11365088
			var boards = b.Model.GetEntityTypes().Where(t => t.ClrType.IsSubclassOf(typeof(Post)));
			var configureMethod = GetType().GetTypeInfo().DeclaredMethods.Single(m => m.Name == nameof(ConfigureEntity));
			var args = new object[] { b };
			foreach (var entityType in boards) {
				configureMethod.MakeGenericMethod(entityType.ClrType).Invoke(null, args);
			}

			b.Entity<Post>().ToView("posts").HasNoKey();

			b.Entity<Report>().HasKey(x => x.Id);
			b.Entity<Sage>().HasKey(x => x.Id);
			b.Entity<Ban>().HasKey(x => x.Id);
			b.Entity<MarkedForDeletion>().HasKey(x => x.Id);
			b.Entity<FrontpageData>().HasKey(x => x.Id);
			b.Entity<ImageQueue>().HasKey(x => x.Id);

			// equalize table names
			string prefix = "net_";
			foreach (var entityType in b.Model.GetEntityTypes()) {
				var tableName = entityType.GetTableName();
				if (tableName.StartsWith("AspNet")) {
					tableName = tableName.Replace("AspNet", prefix);
					entityType.SetTableName(tableName.ToLower());
				} else {
					if (tableName == "markedfordeletion") {
						entityType.SetTableName($"{prefix}deletion_list");
						continue;
					}
					if (tableName == "frontpagedata") {
						entityType.SetTableName($"{prefix}frontpage_data");
						continue;
					}
					tableName = prefix + tableName.ToLower();
					entityType.SetTableName(tableName);
				}
			}			
		}

		static void ConfigureEntity<T>(ModelBuilder builder) where T : Post {
			var e = builder.Entity<T>();

			if (typeof(T).Name != "poststructure") {
				e.ToTable(typeof(T).Name + "_posts");
			}
			e.Property(p => p.Content).IsRequired();
			e.Property(p => p.Name).HasDefaultValue("Anonymous");
			e.Property(p => p.SpoilerImage).HasDefaultValue(false);
			e.Property(p => p.Archived).HasDefaultValue(false);
			e.Property(p => p.Sticky).HasDefaultValue(false);
		}

		protected override void OnConfiguring(DbContextOptionsBuilder o) {
			base.OnConfiguring(o);
			o.UseSnakeCaseNamingConvention();
		}
	}
}
