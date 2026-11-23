using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Xunit;

/// Base class that creates a real SQLite database file from the project schema
/// and gives every test its own isolated database.
public abstract class DatabaseTestBase : IDisposable
{
    protected readonly Database Database;
    protected readonly string ConnectionString;
    private readonly string _path;

    protected DatabaseTestBase()
    {
        _path = Path.Combine(Path.GetTempPath(), $"miniprojekti-test-{Guid.NewGuid():N}.db");
        var connectionString = $"DataSource={_path}";
        ConnectionString = connectionString;

        using (var conn = new SqliteConnection(connectionString))
        {
            conn.Open();
            var schema = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "sql", "schema.sql"));
            foreach (var statement in schema.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                using var cmd = new SqliteCommand(statement, conn);
                cmd.ExecuteNonQuery();
            }
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = connectionString,
            })
            .Build();

        Database = new Database(config);
    }

    public void Dispose()
    {
        string[] suffixes = [".db", ".db-wal", ".db-shm"];
        foreach (var suffix in suffixes)
        {
            var p = _path + suffix;
            if (File.Exists(p))
            {
                File.Delete(p);
            }
        }
    }

    /// Executes a raw SELECT that is expected to return a single value, bypassing
    /// the Database class entirely to inspect the raw database state.
    protected async Task<string?> Select(string sql, params (string, object?)[] parameters)
    {
        using var c = new SqliteConnection(ConnectionString);
        await c.OpenAsync();
        using var cmd = c.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value);
        }
        var result = await cmd.ExecuteScalarAsync();
        return result is DBNull ? null : result?.ToString();
    }

    protected async Task<int> Count(string sql)
    {
        return int.Parse((await Select(sql))!);
    }
}

public class SaveUserTests : DatabaseTestBase
{
    [Fact]
    public async Task SaveUser_StoresAndRetrievesUser()
    {
        var user = new User("user-1", "Alice");
        await Database.SaveUser(user);

        var fetched = await Database.GetUser(user.id);
        Assert.Equal(user.name, fetched.name);

        // Raw database state
        Assert.Equal(1, await Count("SELECT COUNT(*) FROM users"));
        Assert.Equal("user-1", await Select("SELECT id FROM users WHERE name = 'Alice'"));
    }

    [Fact]
    public async Task SaveUser_SavingExistingUserAgain_UpdatesName()
    {
        var user = new User("user-1", "Alice");
        await Database.SaveUser(user);
        await Database.SaveUser(new User("user-1", "Alice Cooper"));

        // The upsert must not create a second row
        Assert.Equal(1, await Count("SELECT COUNT(*) FROM users"));
        Assert.Equal("Alice Cooper", await Select("SELECT name FROM users WHERE id = 'user-1'"));
        Assert.Equal("Alice Cooper", (await Database.GetUser("user-1")).name);
    }
}

public class SaveReferenceTests : DatabaseTestBase
{
    [Fact]
    public async Task SaveReference_StoresCoreFieldsAndAuthors()
    {
        var reference = new Reference(Guid.CreateVersion7(), "user-1", "article", "cite-1");
        reference.authors = ["Smith, John", "Doe, Jane"];
        await Database.SaveUser(new User("user-1", "Alice"));
        await Database.SaveReference(reference);

        // Raw database state
        Assert.Equal(1, await Count("SELECT COUNT(*) FROM citations"));
        Assert.Equal("article", await Select("SELECT entrytype FROM citations WHERE id = $id", ("$id", reference.id.ToString())));
        Assert.Equal("cite-1", await Select("SELECT citekey FROM citations WHERE id = $id", ("$id", reference.id.ToString())));
        Assert.Equal("user-1", await Select("SELECT user FROM citations WHERE id = $id", ("$id", reference.id.ToString())));

        Assert.Equal(2, await Count("SELECT COUNT(*) FROM authors"));
        Assert.Equal(1, await Count("SELECT COUNT(*) FROM authors WHERE author = 'Smith, John'"));
        Assert.Equal(1, await Count("SELECT COUNT(*) FROM authors WHERE author = 'Doe, Jane'"));
    }

    private static Reference FullReference()
    {
        var reference = new Reference(Guid.CreateVersion7(), "user-1", "article", "cite-1");
        reference.authors = ["Smith, John"];
        reference.address = "Helsinki";
        reference.annote = "An annotation";
        reference.booktitle = "A Book";
        reference.chapter = "3.14";
        reference.edition = "2nd";
        reference.howpublished = "Online";
        reference.institution = "JYU";
        reference.journal = "Journal of Testing";
        reference.month = "01";
        reference.note = "A note";
        reference.number = "12";
        reference.organization = "Org";
        reference.pages = "10-20";
        reference.publisher = "Publisher";
        reference.school = "JYU";
        reference.series = "Series A";
        reference.title = "A title";
        reference.type = "Article";
        reference.volume = "5";
        reference.year = "2026";
        reference.doi = "10.1234/example";
        reference.issn = "1234-5678";
        reference.isbn = "978-0-0000-0000-0";
        reference.url = "https://example.com";
        return reference;
    }

    [Fact]
    public async Task SaveReference_StoresAllBibTeXFields()
    {
        var reference = FullReference();
        await Database.SaveUser(new User("user-1", "Alice"));
        await Database.SaveReference(reference);

        // Raw database state: every field must be present with its value
        Assert.Equal("A title", await Select("SELECT title FROM citations WHERE id = $id", ("$id", reference.id.ToString())));
        foreach (var property in Reference.GetBibTeXProperties())
        {
            var expected = property.GetValue(reference)!;
            Assert.Equal(expected, await Select($"SELECT {property.Name} FROM citations WHERE id = $id", ("$id", reference.id.ToString())));
        }
    }

    [Fact]
    public async Task SaveReference_LeavesUnsetFieldsNull()
    {
        var reference = new Reference(Guid.CreateVersion7(), "user-1", "article", "cite-1");
        await Database.SaveUser(new User("user-1", "Alice"));
        await Database.SaveReference(reference);

        // Raw database state: no optional field may be set
        foreach (var property in Reference.GetBibTeXProperties())
        {
            Assert.Null(await Select($"SELECT {property.Name} FROM citations WHERE id = $id", ("$id", reference.id.ToString())));
        }
        Assert.Equal(0, await Count("SELECT COUNT(*) FROM authors"));
    }

    [Fact]
    public async Task SaveReference_WithoutUser_FailsAndRollsBack()
    {
        var reference = new Reference(Guid.CreateVersion7(), "ghost", "article", "cite-1");

        await Assert.ThrowsAsync<SqliteException>(() => Database.SaveReference(reference));

        // The whole transaction must have been rolled back
        Assert.Equal(0, await Count("SELECT COUNT(*) FROM citations"));
        Assert.Equal(0, await Count("SELECT COUNT(*) FROM authors"));
    }

    [Fact]
    public async Task SaveReference_SavingTheSameIdAgain_UpdatesExistingRow()
    {
        var id = Guid.CreateVersion7();
        var first = new Reference(id, "user-1", "article", "cite-1");
        first.authors = ["Smith, John"];
        first.title = "Old title";
        await Database.SaveUser(new User("user-1", "Alice"));
        await Database.SaveReference(first);

        var second = new Reference(id, "user-1", "article", "new-citekey");
        second.authors = ["Doe, Jane"];
        second.title = "New title";
        await Database.SaveReference(second);

        // Raw database state: row is replaced, not duplicated
        Assert.Equal(1, await Count("SELECT COUNT(*) FROM citations"));
        Assert.Equal("new-citekey", await Select("SELECT citekey FROM citations WHERE id = $id", ("$id", id.ToString())));
        Assert.Equal("New title", await Select("SELECT title FROM citations WHERE id = $id", ("$id", id.ToString())));
        Assert.Equal(1, await Count("SELECT COUNT(*) FROM authors"));
        Assert.Equal("Doe, Jane", await Select("SELECT author FROM authors WHERE reference = $id", ("$id", id.ToString())));
    }
}

public class GetReferencesTests : DatabaseTestBase
{
    private static Reference MakeReference(string user, string citekey, string title)
    {
        var reference = new Reference(Guid.CreateVersion7(), user, "article", citekey);
        reference.title = title;
        reference.year = "2026";
        reference.authors = ["Smith, John"];
        return reference;
    }

    [Fact]
    public async Task GetReferences_ReturnsOnlyTheUsersReferences()
    {
        await Database.SaveUser(new User("user-1", "Alice"));
        await Database.SaveUser(new User("user-2", "Bob"));
        await Database.SaveReference(MakeReference("user-1", "cite-a", "Title A"));
        await Database.SaveReference(MakeReference("user-1", "cite-b", "Title B"));
        await Database.SaveReference(MakeReference("user-2", "cite-c", "Title C"));

        var references = await Database.GetReferences("user-1");

        Assert.Equal(2, references.Count);
        Assert.All(references, r => Assert.Equal("user-1", r.user));
        Assert.Contains(references, r => r.citekey == "cite-a");
        Assert.Contains(references, r => r.citekey == "cite-b");
        Assert.DoesNotContain(references, r => r.citekey == "cite-c");
    }

    [Fact]
    public async Task GetReferences_FillsStoredBibTeXFields()
    {
        var reference = MakeReference("user-1", "cite-a", "Title A");
        await Database.SaveUser(new User("user-1", "Alice"));
        await Database.SaveReference(reference);

        var references = await Database.GetReferences("user-1");

        var fetched = Assert.Single(references);
        Assert.Equal(reference.id, fetched.id);
        Assert.Equal("article", fetched.entrytype);
        Assert.Equal("Title A", fetched.title);
        Assert.Equal("2026", fetched.year);
        // Unset optional fields stay null
        Assert.Null(fetched.journal);
    }

    [Fact]
    public async Task GetReferences_UnknownUser_ReturnsEmptyList()
    {
        Assert.Empty(await Database.GetReferences("ghost"));
    }
}

public class GetUserTests : DatabaseTestBase
{
    [Fact]
    public async Task GetUser_UnknownUser_HasNullName()
    {
        var user = await Database.GetUser("ghost");
        Assert.Null(user.name);
    }
}
