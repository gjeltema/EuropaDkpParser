// -----------------------------------------------------------------------
// DkpServerCharacters.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;

public sealed class DkpServerCharacters
{
    private const char Delimiter = '|';
    private readonly string _dkpCharactersFileName;
    private ICollection<DkpUserCharacter> _userCharacters = [];

    public DkpServerCharacters(string dkpCharactersFileName)
    {
        _dkpCharactersFileName = dkpCharactersFileName;
    }

    public void LoadValues()
    {
        if (!File.Exists(_dkpCharactersFileName))
        {
            SaveValues();
            return;
        }

        string[] fileContents = File.ReadAllLines(_dkpCharactersFileName);

        _userCharacters = fileContents
            .Select(ExtractUserCharacter)
            .ToList();
    }

    public void SaveValues()
        => SaveValues(_userCharacters);

    public void SaveValues(ICollection<DkpUserCharacter> characters)
    {
        try
        {
            _userCharacters = characters;
            IEnumerable<string> lines = GetFileLines(characters);
            File.WriteAllLines(_dkpCharactersFileName, lines);
        }
        catch { }
    }

    private DkpUserCharacter ExtractUserCharacter(string fileLine)
    {
        string[] segments = fileLine.Split(Delimiter);
        return new DkpUserCharacter
        {
            UserId = Convert.ToInt32(segments[0]),
            CharacterId = Convert.ToInt32(segments[1]),
            Name = segments[2],
            Level = Convert.ToInt32(segments[3]),
            ClassName = segments[4]
        };
    }

    private IEnumerable<string> GetFileLines(IEnumerable<DkpUserCharacter> characters)
    {
        foreach (DkpUserCharacter userChar in characters)
        {
            yield return $"{userChar.UserId}{Delimiter}{userChar.CharacterId}{Delimiter}{userChar.Name}{Delimiter}{userChar.Level}{Delimiter}{userChar.ClassName}";
        }
    }
}
