using Microsoft.Data.Sqlite;
using miniprojekti.Components;

class Miniprojekti
{
    public static void Main(String[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();

        // Add the database as a singleton so it is accessible ASP.NET via dependency injection
        builder.Services.AddSingleton<Database>();

        builder.Services.AddControllers();

        // Set WAL mode in Sqlite so that the DB is more resillient
        {
            using var c = new SqliteConnection(
                builder.Configuration.GetConnectionString("Database")!
            );
            c.Open();
            using (var p = c.CreateCommand())
            {
                p.CommandText = "PRAGMA journal_mode=WAL;";
                p.ExecuteNonQuery();
            }
        }

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        app.MapControllers();

        app.Run();
    }
}
