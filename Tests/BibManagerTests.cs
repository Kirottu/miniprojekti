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
        manager.GenerateBibTeXFile(bibEntries);
        string result = writer.ToString();

        string expected = 
            """
            @article{key1,
              title = {title1},
              journal = {Journal1},
              year = {2006},
            }

            @article{key2,
              title = {title2},
              journal = {Journal1},
              year = {1954},
            }


            """;

        Assert.Equal(expected, result);
    }
}