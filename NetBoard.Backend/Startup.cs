using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using NetBoard.ExtensionMethods;
using NetBoard.Middleware;
using NetBoard.Model.Data;
using System.IO;
using System.Security.Claims;

namespace NetBoard {
	public class Startup {
		public readonly IConfiguration _configuration;
		public readonly IWebHostEnvironment _environment;

		public Startup(IWebHostEnvironment environment) {
			_configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", false, true).Build();
			_environment = environment;
		}

		public void ConfigureServices(IServiceCollection services) {
			services.AddDbContext<ApplicationDbContext>(options => {
				switch (_configuration["DatabaseType"]) {
					case "PostgreSQL":
						options.UseNpgsql(_configuration.GetConnectionString("PostgreSQL"), b => b.MigrationsAssembly("NetBoard.Backend"));
						break;
					case "SqlServer":
						options.UseSqlServer(_configuration.GetConnectionString("SqlServer"), b => b.MigrationsAssembly("NetBoard.Backend"));
						break;
					case "MySQL":
						options.UseMySQL(_configuration.GetConnectionString("MySQL"), b => b.MigrationsAssembly("NetBoard.Backend"));
						break;
					default:
						break;
				}
			});

			// Identity
			services.AddIdentity<ApplicationUser, IdentityRole<int>>(o => {
				o.SignIn.RequireConfirmedAccount = true;
				o.Password.RequireDigit = false;
				o.Password.RequireNonAlphanumeric = false;
				o.Password.RequireLowercase = false;
				o.Password.RequireUppercase = false;
				o.Password.RequiredUniqueChars = 0;
				o.Password.RequiredLength = 0;
				o.User.RequireUniqueEmail = false;
				o.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier;
			})
				.AddEntityFrameworkStores<ApplicationDbContext>()
				.AddDefaultTokenProviders();

			services.AddCors(options => {
				options.AddPolicy(name: "public_API",
					b => {
						b.AllowAnyOrigin();
						b.AllowAnyHeader();
					});
			});

			services.AddIdentityServer()
				.AddInMemoryClients(Clients.Get())
				.AddInMemoryIdentityResources(Resources.GetIdentityResources())
				.AddInMemoryApiResources(Resources.GetApiResources())
				.AddInMemoryApiScopes(Resources.GetApiScopes())
				.AddAspNetIdentity<ApplicationUser>()
				.AddDeveloperSigningCredential();

			services.AddControllers().AddJsonOptions(o => {
				o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
				o.JsonSerializerOptions.IgnoreReadOnlyProperties = true;
				o.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault;
			});

			//// only look for pages in "Pages/Controller/Action" format
			//services.Configure<RazorViewEngineOptions>(o => {
			//		o.ViewLocationFormats.Clear();
			//		o.ViewLocationFormats.Add("/Pages/{1}/{0}" + RazorViewEngine.ViewExtension);
			//});
			//services.AddRazorPages();

			services.AddMemoryCache();
			services.AddDataProtection()
				.SetApplicationName("NetBoard")
				.PersistKeysToFileSystem(new DirectoryInfo(@"/var/dpkeys"));			

			// allow to use Configuration in any class
			services.AddSingleton(_configuration);

			// ip limiter
			AddIPLimiter(services);
			// image upload tweaks
			services.Configure<FormOptions>(o => {
				o.ValueLengthLimit = int.MaxValue;
				o.MultipartBodyLengthLimit = int.MaxValue;
				o.MemoryBufferThreshold = int.MaxValue;
			});

			services.AddRouting(o => {
				o.LowercaseUrls = true;
			});


			services.AddMvc(o => {
				o.EnableEndpointRouting = false;
			});

			services.AddAuthentication("Bearer")
				//.AddIdentityServerAuthentication("Identity", iOptions => {
				//	iOptions.ApiName = "admin";
				//	iOptions.Authority = _environment.IsDevelopment() 
				//						? _configuration["IS4:AuthorityDevelopment"] 
				//						: _configuration["IS4:AuthorityProduction"];
				//	iOptions.SaveToken = true;
				//})
				.AddJwtBearer("Bearer", jOptions => {
					jOptions.Audience = "admin";
					jOptions.Authority = _environment.IsDevelopment()
										? _configuration["IS4:AuthorityDevelopment"]
										: _configuration["IS4:AuthorityProduction"];
					jOptions.SaveToken = true;
				});


			services.ConfigureHangfire(_configuration);
		}

		private void AddIPLimiter(IServiceCollection services) {
			// get config from appsettings.json
			services.Configure<IpRateLimitOptions>(_configuration.GetSection("IpRateLimiting"));
			services.Configure<IpRateLimitPolicies>(_configuration.GetSection("IpRateLimitPolicies"));
			// inject said config
			services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
			services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

			// https://github.com/aspnet/Hosting/issues/793
			services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

			// configuration (resolvers, counter key builders)
			services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
			if (env.IsDevelopment()) {
				app.UseDeveloperExceptionPage();
				IdentityModelEventSource.ShowPII = true;
			} else {
				app.UseExceptionHandler("/error.html");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseStaticFiles();
			app.UseDefaultFiles();

			// tell reverse proxy to use put in provided by X-Forwarder-For header
			app.UseForwardedHeaders(new ForwardedHeadersOptions {
				ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
			});

			// banned IPs get 404 on all requests
			app.UseIPFilter();

			app.UseRouting();

			app.UseIdentityServer();

			app.UseAuthentication();
			app.UseCors("public_API");
			app.UseAuthorization();

			app.UseIpRateLimiting();

			app.UseEndpoints(endpoints => {
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{controller}/{action=GetThreads}/{id?}");
				//endpoints.MapRazorPages();
				endpoints.MapControllers();
			});

			HangfireExtensions.RunHangfireJobs();
		}		
	}
}
