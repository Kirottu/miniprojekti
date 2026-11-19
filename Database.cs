using Microsoft.Data.Sqlite;

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
    List<String>? authors { get; set; }
    String? address { get; set; }
    String? annote { get; set; }
    String? booktitle { get; set; }
    String? chapter { get; set; }
    String? edition { get; set; }
    String? howpublished { get; set; }
    String? institution { get; set; }
    String? journal { get; set; }
    String? month { get; set; }
    String? note { get; set; }
    String? number { get; set; }
    String? organization { get; set; }
    String? pages { get; set; }
    String? publisher { get; set; }
    String? school { get; set; }
    String? series { get; set; }
    String? title { get; set; }
    String? type { get; set; }
    String? volume { get; set; }
    String? year { get; set; }

    // BibTeX non-standard fields
    String? doi { get; set; }
    String? issn { get; set; }
    String? isbn { get; set; }
    String? url { get; set; }
}

public class Database
{
    private readonly string dbString;

    public Database(IConfiguration config)
    {
        this.dbString = config.GetConnectionString("Database")!;
    }

    private async Task<SqliteConnection> Conn()
    {
        var c = new SqliteConnection(this.dbString);
        await c.OpenAsync();
        using (var p = c.CreateCommand()) // per-connection pragma
        {
            p.CommandText = "PRAGMA busy_timeout = 5000;"; // wait up to 5s on lock
            p.ExecuteNonQuery();
        }
        using (var p = c.CreateCommand())
        {
            p.CommandText = "PRAGMA foreign_keys = ON;";
            p.ExecuteNonQuery();
        }

        return c;
    }
}
