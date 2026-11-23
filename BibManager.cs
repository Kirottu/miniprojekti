using BibSharp;

// https://www.nuget.org/packages/BibSharp

public class BibManager()
{

    // järjestelmässä olevista viitteistä pitää saada generoitua LaTeX-dokumenttiin sopiva BibTeX-muotoinen tiedosto
    public void GenerateBibTeXFile(List<BibEntry> bibEntries)
    {
        using (StreamWriter outputFile = new StreamWriter("references.bib"))
        {
            foreach (BibEntry bibEntry in bibEntries)
            {
                string bibtex = bibEntry.ToBibTeX();
                outputFile.WriteLine(bibtex);
            }
        }
    }
    
}

