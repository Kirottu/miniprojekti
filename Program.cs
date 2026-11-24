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
