using BibSharp;

// https://www.nuget.org/packages/BibSharp


public class BibManager
{
    private readonly TextWriter _output;

    public BibManager(TextWriter output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>
    /// Writes to BibManager's TextWriter a BibTeX formatted output.
    /// 
    /// Example class usage:
    /// 
    /// using var writer = new StreamWriter("references.bib");
    /// BibManager manager = new BibManager(writer);
    /// List<BibEntry> bibEntries =
    /// [
    ///     new BibEntry(EntryType.Article){Key="key1", Title="title1", Journal="Journal1", Year=2006},
    ///     new BibEntry(EntryType.Article){Key="key2", Title="title2", Journal="Journal1", Year=1954},
    ///     new BibEntry(EntryType.Article){Key="key3", Title="title3", Journal="Journal2", Year=2024},
    ///     new BibEntry(EntryType.Article){Key="key4", Title="title4", Journal="Journal2", Year=2014},
    /// ];
    /// manager.GenerateBibTeXFile(bibEntries);
    /// 
    /// </summary>
    /// <param name="bibEntries"></param>
    public void GenerateBibTeXFile(List<BibEntry> bibEntries)
    {
        foreach (BibEntry bibEntry in bibEntries)
        {
            string bibtex = bibEntry.ToBibTeX();
            _output.WriteLine(bibtex);
        }

        _output.Flush();
    }
}
