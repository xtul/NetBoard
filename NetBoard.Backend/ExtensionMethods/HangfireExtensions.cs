using Hangfire;
using Hangfire.MySql;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace NetBoard.ExtensionMethods {
	public static class HangfireExtensions {
		public static void ConfigureHangfire(this IServiceCollection services, IConfiguration configuration) {
			services.AddScoped<HangfireJobs>();
			services.AddHangfire((provider, config) => {
				switch (configuration.GetSection("DatabaseType").Value) {
					case "PostgreSQL":
						config.UsePostgreSqlStorage(configuration.GetConnectionString("PostgreSQL"));
						break;
					case "SqlServer":
						config.UseSqlServerStorage(configuration.GetConnectionString("SqlServer"));
						break;
					case "MySQL":
						config.UseStorage(new MySqlStorage(configuration.GetConnectionString("MySQL"), null));
						break;
					default:
						break;
				}
				GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { Attempts = 0 });
			});
			services.AddHangfireServer(options => {
				// do only one job at a time - we often depend on things running in certain order
				options.WorkerCount = 1;
			});
		}

		public static void RunHangfireJobs() {
			BackgroundJob.Enqueue<HangfireJobs>(j => j.SeedDB()); // always run on startup - in case of empty database
			RecurringJob.AddOrUpdate<HangfireJobs>(j => j.CleanupImageQueue(), "*/5 * * * *");
		}
	}
}
