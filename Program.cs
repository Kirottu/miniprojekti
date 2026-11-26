using System.IdentityModel.Tokens.Jwt;
using dotenv.net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using miniprojekti.Components;

class Miniprojekti
{
    public static async Task Main(string[] args)
    {
        DotEnv.Load();
        var envVars = DotEnv.Read();

        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();

        // Add the database as a singleton so it is accessible ASP.NET via dependency injection
        builder.Services.AddSingleton<Database>();

        builder.Services.AddControllers();

        builder
            .Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(options =>
            {
                var oidcConfig = builder.Configuration.GetSection("OpenIDConnectSettings");

                options.Authority = oidcConfig["Authority"];
                options.ClientId = oidcConfig["ClientId"];
                options.ClientSecret = envVars["CLIENT_SECRET"];

                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ResponseType = OpenIdConnectResponseType.Code;

                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;

                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                options.TokenValidationParameters.RoleClaimType = "roles";
            });

        var requireAuthPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

        builder.Services.AddAuthorizationBuilder().SetFallbackPolicy(requireAuthPolicy);

        var dbString = builder.Configuration.GetConnectionString("Database")!;
        var path = dbString.Split('=')[1];

        // Init DB if it doesn't exist
        if (!File.Exists(path))
        {
            Console.WriteLine("Initializing DB...");
            using var c = new SqliteConnection(dbString);
            c.Open();
            using (var p = c.CreateCommand())
            {
                p.CommandText = File.ReadAllText("sql/schema.sql");
                p.ExecuteNonQuery();
            }
        }

        var app = builder.Build();

        // Add temporary test references if the database is empty.
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Database>();

            var references = await db.GetReferences("test");

            if (references.Count == 0)
            {
                await db.SaveUser(new User("test", "test"));

                await db.SaveReference(
                    new Reference(
                        Guid.Parse("11111111-1111-7111-8111-111111111111"),
                        "test",
                        "inproceedings",
                        "VPL11"
                    )
                    {
                        authors = ["Vihavainen, Arto", "Paksula, Matti", "Luukkainen, Matti"],
                        title =
                            "Extreme Apprenticeship Method in Teaching Programming for Beginners.",
                        year = "2011",
                    }
                );

                await db.SaveReference(
                    new Reference(
                        Guid.Parse("22222222-2222-7222-8222-222222222222"),
                        "test",
                        "article",
                        "CBH91"
                    )
                    {
                        authors = ["Allan Collins", "John Seely Brown", "Ann Holum"],
                        title = "Cognitive apprenticeship: making thinking visible",
                        year = "1991",
                    }
                );

                await db.SaveReference(
                    new Reference(
                        Guid.Parse("33333333-3333-7333-8333-333333333333"),
                        "test",
                        "book",
                        "Martin09"
                    )
                    {
                        authors = ["Martin, Robert"],
                        title = "Clean Code: A Handbook of Agile Software Craftsmanship",
                        year = "2008",
                    }
                );

                Console.WriteLine("Test references added to database.");
            }
        }

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        app.MapControllers();

        app.Run();
    }
}
