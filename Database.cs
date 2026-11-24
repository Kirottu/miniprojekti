using System.Reflection;
using Microsoft.Data.Sqlite;

// FIXME: Authentication?
public record User(String id, String name);

/// An attribute used to mark the optional BibTeX fields for some good ol' C# reflection
[AttributeUsage(AttributeTargets.Property)]
class BibTeXOptionalAttribute : Attribute { }

public record Reference(
    /// The unique ID of this specific reference
    Guid id,
    /// The ID of the user that owns this reference
    String user,
    // BibTeX "declaration"
    String entrytype,
    String citekey
)
{
    // BibTeX fields
    public List<String>? authors { get; set; }

    [BibTeXOptionalAttribute]
    public String? address { get; set; }

    [BibTeXOptionalAttribute]
    public String? annote { get; set; }

    [BibTeXOptionalAttribute]
    public String? booktitle { get; set; }

    [BibTeXOptionalAttribute]
    public String? chapter { get; set; }

    [BibTeXOptionalAttribute]
    public String? edition { get; set; }

    [BibTeXOptionalAttribute]
    public String? howpublished { get; set; }

    [BibTeXOptionalAttribute]
    public String? institution { get; set; }

    [BibTeXOptionalAttribute]
    public String? journal { get; set; }

    [BibTeXOptionalAttribute]
    public String? month { get; set; }

    [BibTeXOptionalAttribute]
    public String? note { get; set; }

    [BibTeXOptionalAttribute]
    public String? number { get; set; }

    [BibTeXOptionalAttribute]
    public String? organization { get; set; }

    [BibTeXOptionalAttribute]
    public String? pages { get; set; }

    [BibTeXOptionalAttribute]
    public String? publisher { get; set; }

    [BibTeXOptionalAttribute]
    public String? school { get; set; }

    [BibTeXOptionalAttribute]
    public String? series { get; set; }

    [BibTeXOptionalAttribute]
    public String? title { get; set; }

    [BibTeXOptionalAttribute]
    public String? type { get; set; }

    [BibTeXOptionalAttribute]
    public String? volume { get; set; }

    [BibTeXOptionalAttribute]
    public String? year { get; set; }

    // BibTeX non-standard fields
    [BibTeXOptionalAttribute]
    public String? doi { get; set; }

    [BibTeXOptionalAttribute]
    public String? issn { get; set; }

    [BibTeXOptionalAttribute]
    public String? isbn { get; set; }

    [BibTeXOptionalAttribute]
    public String? url { get; set; }

    public static PropertyInfo[] GetBibTeXProperties()
    {
        return typeof(Reference)
            .GetProperties()
            .Where(prop => Attribute.IsDefined(prop, typeof(BibTeXOptionalAttribute)))
            .ToArray();
    }
}

public class Database
{
    private readonly string dbString;

    public Database(IConfiguration config)
    {
        this.dbString = config.GetConnectionString("Database")!;
    }

    public async Task SaveUser(User user)
    {
        using var c = await Conn();
        using var tx = await c.BeginTransactionAsync();

        await ExecuteAsync(
            c,
            "INSERT INTO users (id, name) VALUES ($id, $name) ON CONFLICT(id) DO UPDATE SET name = $name",
            ("$id", user.id.ToString()),
            ("$name", user.name)
        );

        await tx.CommitAsync();
    }

    public async Task DeleteUser(String id)
    {
        using var c = await Conn();
        using var tx = await c.BeginTransactionAsync();

        await ExecuteAsync(c, "DELETE FROM users WHERE id = $id", ("$id", id));

        await tx.CommitAsync();
    }

    // FIXME: Authentication?
    public async Task<User> GetUser(String id)
    {
        using var c = await Conn();

        var name = await ScalarAsync(c, "SELECT name FROM users WHERE id = $id", ("$id", id));

        return new User(id, name!);
    }

    public async Task<List<Reference>> GetReferences(String userId)
    {
        using var c = await Conn();
        using var cmd = Cmd(
            c,
            "SELECT id, entrytype, citekey FROM citations WHERE user = $user",
            ("$user", userId)
        );

        using var reader = await cmd.ExecuteReaderAsync();

        List<Reference> references = [];

        while (await reader.ReadAsync())
        {
            var reference = new Reference(
                Guid.Parse(reader.GetString(0)),
                userId,
                reader.GetString(1),
                reader.GetString(2)
            );
            foreach (var property in Reference.GetBibTeXProperties())
            {
                var res = await ScalarAsync(
                    c,
                    $"SELECT {property.Name} FROM citations WHERE id = $id",
                    ("$id", reference.id.ToString())
                );

                if (res != null)
                {
                    property.SetValue(reference, res);
                }
            }

            using var authorCmd = Cmd(
                c,
                "SELECT author FROM authors WHERE reference = $id",
                ("$id", reference.id.ToString())
            );

            using var authorReader = await authorCmd.ExecuteReaderAsync();

            reference.authors = [];

            while (await authorReader.ReadAsync())
            {
                reference.authors.Add(authorReader.GetString(0));
            }

            references.Add(reference);
        }

        return references;
    }

    /// Save a reference to the database
    public async Task SaveReference(Reference reference)
    {
        using var c = await Conn();

        using var tx = await c.BeginTransactionAsync();

        // Insert the always required fields first
        await ExecuteAsync(
            c,
            "INSERT OR REPLACE INTO citations (id, user, entrytype, citekey) VALUES ($id, $user, $entrytype, $citekey)",
            ("$id", reference.id.ToString()),
            ("$user", reference.user),
            ("$entrytype", reference.entrytype),
            ("$citekey", reference.citekey)
        );

        foreach (var property in Reference.GetBibTeXProperties())
        {
            var value = property.GetValue(reference);
            if (value is null)
            {
                continue;
            }
            await ExecuteAsync(
                c,
                $"UPDATE citations SET {property.Name} = ${property.Name} WHERE id = $id",
                ("$id", reference.id.ToString()),
                ("$" + property.Name, value)
            );
        }

        foreach (var author in reference.authors ?? [])
        {
            await ExecuteAsync(
                c,
                "INSERT INTO authors (reference, author) VALUES ($id, $author)",
                ("$id", reference.id.ToString()),
                ("$author", author)
            );
        }

        await tx.CommitAsync();
    }

    public async Task DeleteReference(String id)
    {
        using var c = await Conn();
        using var tx = await c.BeginTransactionAsync();

        await ExecuteAsync(c, "DELETE FROM citations WHERE id = $id", ("$id", id));

        await tx.CommitAsync();
    }

    /// Builds a parameterized command on the given connection without executing it.
    private SqliteCommand Cmd(SqliteConnection c, string sql, params (string, object?)[] parameters)
    {
        var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value);
        }
        return cmd;
    }

    /// Executes a non-query statement and returns nothing.
    private async Task ExecuteAsync(
        SqliteConnection c,
        string sql,
        params (string, object?)[] parameters
    )
    {
        using var cmd = Cmd(c, sql, parameters);
        await cmd.ExecuteNonQueryAsync();
    }

    /// Executes a query expected to return a single value, or null when no row matches.
    private async Task<string?> ScalarAsync(
        SqliteConnection c,
        string sql,
        params (string, object?)[] parameters
    )
    {
        using var cmd = Cmd(c, sql, parameters);
        var result = await cmd.ExecuteScalarAsync();
        return result is DBNull ? null : result?.ToString();
    }

    private async Task<SqliteConnection> Conn()
    {
        var c = new SqliteConnection(this.dbString);
        await c.OpenAsync();
        using (var cmd = c.CreateCommand()) // per-connection pragma
        {
            cmd.CommandText = "PRAGMA busy_timeout = 5000;"; // wait up to 5s on lock
            cmd.ExecuteNonQuery();
        }
        using (var cmd = c.CreateCommand())
        {
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();
        }

        return c;
    }
}
