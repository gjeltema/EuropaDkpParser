// -----------------------------------------------------------------------
// DkpServerCharacters.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.Diagnostics;
using System.IO;

public sealed class DkpServerCharacters
{
    private const char Delimiter = '|';
    private readonly string _dkpCharactersFileName;
    private ICollection<DkpUserCharacter> _userCharacters = [];

    public IEnumerable<DkpUserCharacter> AllUserCharacters
        => _userCharacters;

    public DkpServerCharacters(string dkpCharactersFileName)
    {
        _dkpCharactersFileName = dkpCharactersFileName;
    }

    public bool CharacterConfirmedExistsOnDkpServer(string characterName)
        => _userCharacters.Count > 0 && _userCharacters.Any(x => x.Name == characterName);

    public bool CharacterConfirmedNotOnDkpServer(string characterName)
        => _userCharacters.Count > 0 && !_userCharacters.Any(x => x.Name == characterName);

    public IEnumerable<DkpUserCharacter> GetAllRelatedCharacters(DkpUserCharacter userCharacter)
    {
        if (_userCharacters.Count == 0)
            return [];

        return _userCharacters.Where(x => x.UserId == userCharacter.UserId);
    }

    public IEnumerable<MutipleCharactersOnAccount> GetMultipleCharactersOnAccount(IEnumerable<PlayerCharacter> characters)
    {
        List<PlayerCharacter> playerCharacters = new(characters);
        List<MutipleCharactersOnAccount> multipleChars = [];
        for (int i = 0; i < playerCharacters.Count; i++)
        {
            PlayerCharacter currentChar = playerCharacters[i];
            DkpUserCharacter dkpCharacter = _userCharacters.FirstOrDefault(x => x.Name.Equals(currentChar.CharacterName, StringComparison.OrdinalIgnoreCase));
            if (dkpCharacter == null)
                continue;

            if (multipleChars.Any(x => x.Contains(dkpCharacter)))
                continue;

            List<DkpUserCharacter> associatedCharacters = _userCharacters
                .Where(x => x.UserId == dkpCharacter.UserId && x.Name != dkpCharacter.Name)
                .ToList();

            if (associatedCharacters.Count < 2)
                continue;

            for (int j = i + 1; j < playerCharacters.Count; j++)
            {
                PlayerCharacter comparingChar = playerCharacters[j];
                DkpUserCharacter matchingDkpChar = associatedCharacters.FirstOrDefault(x => x.Name.Equals(comparingChar.CharacterName, StringComparison.OrdinalIgnoreCase));
                if (matchingDkpChar != null)
                {
                    MutipleCharactersOnAccount multipleDkpCharMatch = new()
                    {
                        FirstCharacter = dkpCharacter,
                        SecondCharacter = matchingDkpChar,
                    };
                    multipleChars.Add(multipleDkpCharMatch);
                }
            }
        }

        return multipleChars;
    }

    public DkpUserCharacter GetUserCharacter(string characterName)
        => _userCharacters.FirstOrDefault(x => x.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase));

    public bool IsRelatedCharacterInCollection(PlayerCharacter character, IEnumerable<PlayerCharacter> characters)
    {
        DkpUserCharacter dkpCharacter = _userCharacters.FirstOrDefault(x => x.Name.Equals(character.CharacterName, StringComparison.OrdinalIgnoreCase));
        List<DkpUserCharacter> associatedCharacters = _userCharacters
                .Where(x => x.UserId == dkpCharacter.UserId && x.Name != dkpCharacter.Name)
                .ToList();

        if (associatedCharacters.Count == 0)
            return false;

        foreach (PlayerCharacter characterInList in characters)
        {
            if (associatedCharacters.Any(x => x.Name.Equals(characterInList.CharacterName, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
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

[DebuggerDisplay("{DebugText,nq}")]
public sealed class MutipleCharactersOnAccount
{
    public DkpUserCharacter FirstCharacter { get; init; }

    public DkpUserCharacter SecondCharacter { get; init; }

    private string DebugText
        => $"{FirstCharacter.Name} {SecondCharacter.Name}";

    public bool Contains(DkpUserCharacter character)
    {
        if (character == null)
            return false;

        else if (character.CharacterId == FirstCharacter.CharacterId)
            return true;

        else if (character.CharacterId == SecondCharacter.CharacterId)
            return true;

        return false;
    }
}
