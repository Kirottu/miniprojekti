using Xunit;
using BibSharp;

public class BibManagerTests
{
    [Fact]
    public void GenerateBibTeXFile_ValidReferences_WritesFormattedBibTeX()
    {
        using var writer = new StringWriter();
        BibManager manager = new BibManager(writer);
        List<BibEntry> bibEntries =
        [
            new BibEntry(EntryType.Article){Key="key1", Title="title1", Journal="Journal1", Year=2006},
            new BibEntry(EntryType.Article){Key="key2", Title="title2", Journal="Journal1", Year=1954},
        ];
        bibEntries[0].AddAuthor(new BibSharp.Models.Author("Smith", "Jack", "T."));
        bibEntries[0].AddAuthor(new BibSharp.Models.Author("Gomez", "Carter", "S."));
        bibEntries[1].AddAuthor(new BibSharp.Models.Author("Phillips", "Ryan", "E."));
        bibEntries[1].AddAuthor(new BibSharp.Models.Author("Rogers", "Jody", "Y."));
        manager.GenerateBibTeXFile(bibEntries);
        string result = writer.ToString();

        string expected = 
            """
            @article{key1,
              title = {title1},
              journal = {Journal1},
              year = {2006},
              author = {Smith, Jack T. and Gomez, Carter S.},
            }

            @article{key2,
              title = {title2},
              journal = {Journal1},
              year = {1954},
              author = {Phillips, Ryan E. and Rogers, Jody Y.},
            }


            """;

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task FromDOI_ValidURL_CreatesBibEntry()
    { 
        using var writer = new StringWriter();
        BibManager manager = new BibManager(writer);

        var entry = await manager.FromDOI("10.1145/2380552.2380613");

        Assert.NotNull(entry);
        Assert.Equal("Three years of design-based research to reform a software engineering curriculum", entry.Title);
        Assert.Equal(2012, entry.Year);
        Assert.Equal(EntryType.InProceedings, entry.EntryType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("---")]
    public async Task FromDOI_InvalidURL_ReturnsNull(string url)
    { 
        using var writer = new StringWriter();
        BibManager manager = new BibManager(writer);

        var entry = await manager.FromDOI(url);

        Assert.Null(entry);
    }
}