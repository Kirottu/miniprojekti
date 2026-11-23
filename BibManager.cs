using BibSharp;

// https://www.nuget.org/packages/BibSharp

public class BibManager
{
    private readonly TextWriter _output;

    public BibManager(TextWriter output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    // järjestelmässä olevista viitteistä pitää saada generoitua LaTeX-dokumenttiin sopiva BibTeX-muotoinen tiedosto
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
