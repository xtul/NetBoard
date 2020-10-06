using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
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
