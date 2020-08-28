using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetBoard.Model.Data;
using Microsoft.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace NetBoard {
	public class Program {
		public static void Main(string[] args) {
            CreateHostBuilder(args).Build().Run();
        }

		public static IHostBuilder CreateHostBuilder(string[] args) {
			var assemblyDir = Path.GetDirectoryName(typeof(Startup).Assembly.Location);
			return Host.CreateDefaultBuilder(args)
					.ConfigureWebHostDefaults(w => {
						w.UseKestrel();
						w.UseContentRoot(assemblyDir);
						w.UseStartup<Startup>();
					});
		}
	}
}
