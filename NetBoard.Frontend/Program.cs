using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Blazored.LocalStorage;

namespace NetBoard.Frontend {
	public static class Program {
		public static async Task Main(string[] args) {
			var builder = WebAssemblyHostBuilder.CreateDefault(args);
			builder.RootComponents.Add<App>("#app");

			builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
			builder.Services.AddMudBlazorDialog();
			builder.Services.AddMudBlazorSnackbar();
			builder.Services.AddMudBlazorResizeListener();

			builder.Services.AddTransient(sp => new HttpClient {
				BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
			})
				.AddBlazoredLocalStorage();

			builder.Services.AddOidcAuthentication(options => {
				// Configure your authentication provider options here.
				// For more information, see https://aka.ms/blazor-standalone-auth
				builder.Configuration.Bind("Local", options.ProviderOptions);
			});

			await builder.Build().RunAsync();
		}
	}
}
