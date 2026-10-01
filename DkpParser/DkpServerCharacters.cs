// -----------------------------------------------------------------------
// DkpServerCharacters.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

using System.IO;
using Gjeltema.Logging;

public sealed class DkpServerCharacters
{
    private const char Delimiter = '|';
    private const string LogPrefix = $"[{nameof(DkpServerCharacters)}]";
    private readonly string _dkpCharactersFileName;
    private ICollection<DkpUserCharacter> _userCharacters = [];

    public DkpServerCharacters(string dkpCharactersFileName)
    {
        _dkpCharactersFileName = dkpCharactersFileName;
    }

    public IEnumerable<DkpUserCharacter> AllUserCharacters
        => _userCharacters;

    public bool CharacterConfirmedNotOnDkpServer(string characterName)
        => _userCharacters.Count > 0 && !_userCharacters.Any(x => x.Name == characterName);

    public DkpUserCharacter GetUserCharacter(string characterName)
        => _userCharacters.FirstOrDefault(x => x.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase));

    public bool IsRelatedCharacterInCollection(PlayerCharacter character, IEnumerable<PlayerCharacter> characters)
    {
        DkpUserCharacter dkpCharacter = _userCharacters.FirstOrDefault(x => x.Name.Equals(character.CharacterName, StringComparison.OrdinalIgnoreCase));
        if (dkpCharacter == null)
            return false;

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
            Log.Error($"{LogPrefix} {_dkpCharactersFileName} does not exist.");
            return;
        }

        string[] fileContents = File.ReadAllLines(_dkpCharactersFileName);

        _userCharacters = fileContents
            .Select(ExtractUserCharacter)
            .ToList();
    }

    public void SaveValues()
        => SaveValues(_userCharacters);

    public void SaveValues(IEnumerable<DkpUserCharacter> characters)
    {
        try
        {
            _userCharacters = characters.OrderBy(x => x.UserId).ThenBy(x => x.CharacterId).ToList();
            IEnumerable<string> lines = GetFileLines(_userCharacters);
            Task.Run(() => File.WriteAllLines(_dkpCharactersFileName, lines));
        }
        catch (Exception ex)
        {
            Log.Error($"{LogPrefix} Unable to write character info to file: {_dkpCharactersFileName}: {ex.ToLogMessage()}");
        }
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
