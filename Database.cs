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

    internal static PropertyInfo[] GetBibTeXProperties()
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

        using (var cmd = c.CreateCommand())
        {
            cmd.CommandText =
                "INSERT INTO users (id, name) VALUES ($id, $name) ON CONFLICT(id) DO UPDATE SET name = $name";
            cmd.Parameters.AddWithValue("$id", user.id.ToString());
            cmd.Parameters.AddWithValue("$name", user.name);

            await cmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }

    // FIXME: Authentication?
    public async Task<User> GetUser(String id)
    {
        using var c = await Conn();

        using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT name FROM users WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);

        return new User(id, (String)(await cmd.ExecuteScalarAsync())!);
    }

    public async Task<List<Reference>> GetReferences(String userId)
    {
        using var c = await Conn();
        using var cmd = c.CreateCommand();

        cmd.CommandText = "SELECT id, entrytype, citekey FROM citations WHERE user = $user";
        cmd.Parameters.AddWithValue("$user", userId);

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
                using var fieldCmd = c.CreateCommand();
                fieldCmd.CommandText = $"SELECT {property.Name} FROM citations WHERE id = $id";
                fieldCmd.Parameters.AddWithValue("$id", reference.id.ToString());

                var res = await fieldCmd.ExecuteScalarAsync();

                if (res == DBNull.Value)
                {
                    continue;
                }

                property.SetValue(reference, res);
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
        using (var cmd = c.CreateCommand())
        {
            cmd.CommandText =
                "INSERT OR REPLACE INTO citations (id, user, entrytype, citekey) VALUES ($id, $user, $entrytype, $citekey)";

            cmd.Parameters.AddWithValue("$id", reference.id.ToString());
            cmd.Parameters.AddWithValue("$user", reference.user);
            cmd.Parameters.AddWithValue("$entrytype", reference.entrytype);
            cmd.Parameters.AddWithValue("$citekey", reference.citekey);

            await cmd.ExecuteNonQueryAsync();
        }

        foreach (var property in Reference.GetBibTeXProperties())
        {
            var value = property.GetValue(reference);
            if (value == null)
            {
                continue;
            }
            using var cmd = c.CreateCommand();
            cmd.CommandText =
                $"UPDATE citations SET {property.Name} = ${property.Name} WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", reference.id.ToString());
            cmd.Parameters.AddWithValue($"${property.Name}", value);

            await cmd.ExecuteNonQueryAsync();
        }

        foreach (var author in reference.authors ?? [])
        {
            using var cmd = c.CreateCommand();
            cmd.CommandText = "INSERT INTO authors (reference, author) VALUES ($id, $author)";
            cmd.Parameters.AddWithValue("$id", reference.id.ToString());
            cmd.Parameters.AddWithValue("$author", author);

            await cmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
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
