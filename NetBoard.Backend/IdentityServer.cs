using IdentityServer4;
using IdentityServer4.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace NetBoard {
	internal class Clients {
		public static IEnumerable<Client> Get() {
			var configuration = new ConfigurationBuilder()
								.SetBasePath(Directory.GetCurrentDirectory())
								.AddJsonFile("appsettings.json")
								.Build();
			return new List<Client> { 
				new Client {
					ClientId = "netchan_admin",
					ClientName = "Admin admin",
					AllowedGrantTypes = GrantTypes.Code,
					ClientSecrets = new List<Secret> {new Secret(configuration["IS4:ClientSecrets:Admin"].Sha256())},
					RedirectUris = new List<string> {"http://localhost:8080/signin-oidc"},
					AllowedScopes = new List<string> {
						IdentityServerConstants.StandardScopes.OpenId,
						IdentityServerConstants.StandardScopes.Profile,
						IdentityServerConstants.StandardScopes.Email,
						"administrator",
						"moderator",
						"janitor",
						"role"
					},
					AllowedCorsOrigins = new List<string> {
						"http://localhost:8080",
						"https://localhost:8080"
					},
					RequirePkce = true,
					RequireClientSecret = false,
					AllowPlainTextPkce = false
				},
				new Client {
					ClientId = "netchan_moderator",
					ClientName = "Admin moderator",
					AllowedGrantTypes = GrantTypes.Code,
					ClientSecrets = new List<Secret> {new Secret(configuration["IS4:ClientSecrets:Moderator"].Sha256())},
					RedirectUris = new List<string> {"https://localhost:5934/signin-oidc"},
					AllowedScopes = new List<string> {
						IdentityServerConstants.StandardScopes.OpenId,
						IdentityServerConstants.StandardScopes.Profile,
						IdentityServerConstants.StandardScopes.Email,
						"moderator",
						"janitor",
						"role"
					},
					AllowedCorsOrigins = new List<string> {
						"http://localhost:8080",
						"https://localhost:8080"
					},
					RequirePkce = true,
					RequireClientSecret = false,
					AllowPlainTextPkce = false
				},
				new Client {
					ClientId = "netchan_janitor",
					ClientName = "Admin janitor",
					AllowedGrantTypes = GrantTypes.Code,
					ClientSecrets = new List<Secret> {new Secret(configuration["IS4:ClientSecrets:Janitor"].Sha256())},
					RedirectUris = new List<string> {"https://localhost:5934/signin-oidc"},
					AllowedScopes = new List<string> {
						IdentityServerConstants.StandardScopes.OpenId,
						IdentityServerConstants.StandardScopes.Profile,
						IdentityServerConstants.StandardScopes.Email,
						"janitor",
						"role"
					},
					AllowedCorsOrigins = new List<string> {
						"http://localhost:8080",
						"https://localhost:8080"
					},
					RequirePkce = true,
					RequireClientSecret = false,
					AllowPlainTextPkce = false
				}
			};
		}
	}

	internal class Resources {
		public static IEnumerable<IdentityResource> GetIdentityResources() {
			return new[] {
				new IdentityResources.OpenId(),
				new IdentityResources.Profile(),
				new IdentityResources.Email(),
				new IdentityResource {
					Name = "role",
					UserClaims = new List<string> {"role"}
				}
			};
		}

		public static IEnumerable<ApiResource> GetApiResources() {
			var configuration = new ConfigurationBuilder()
								.SetBasePath(Directory.GetCurrentDirectory())
								.AddJsonFile("appsettings.json")
								.Build();
			return new[] {
				new ApiResource {
					Name = "admin",
					DisplayName = "Administration API",
					Description = "Allows you to access administrative endpoints.",
					Scopes = new List<string> {"admin", "moderator", "janitor"},
					ApiSecrets = new List<Secret> {new Secret(configuration["IS4:ScopeSecrets:Admin"].Sha256())},
					UserClaims = new List<string> {"role"}
				}
				//new ApiResource {
				//	Name = "moderator",
				//	DisplayName = "Moderator API",
				//	Description = "Allows you to access moderator endpoints.",
				//	Scopes = new List<string> { "moderator"},
				//	ApiSecrets = new List<Secret> {new Secret(configuration["IS4:ScopeSecrets:Moderator"].Sha256())},
				//	UserClaims = new List<string> {"role"}
				//},
				//new ApiResource {
				//	Name = "janitor",
				//	DisplayName = "Janitor API",
				//	Description = "Allows you to access janitor endpoints.",
				//	Scopes = new List<string> { "janitor"},
				//	ApiSecrets = new List<Secret> {new Secret(configuration["IS4:ScopeSecrets:Janitor"].Sha256())},
				//	UserClaims = new List<string> {"role"}
				//}
			};
		}

		public static IEnumerable<ApiScope> GetApiScopes() {
			return new[] {
				new ApiScope("administrator", "Access to Admin API"),
				new ApiScope("moderator", "Access to Moderator API"),
				new ApiScope("janitor", "Access to Janitor API"),
			};
		}
	}
}