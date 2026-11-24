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
            "@article{key1,\r\n  title = {title1},\r\n  journal = {Journal1},\r\n  year = {2006},\r\n}\r\n\r\n@article{key2,\r\n  title = {title2},\r\n  journal = {Journal1},\r\n  year = {1954},\r\n}\r\n\r\n";

        Assert.Equal(expected, result);
    }
}