// -----------------------------------------------------------------------
// EqClasses.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public static class EqClasses
{
    private static readonly Dictionary<string, string> _classNameAlias = new()
    {
        { "Minstrel", "Bard" },
        { "Troubadour", "Bard" },
        { "Virtuoso", "Bard" },
        { "Maestro", "Bard" },
        { "Primalist", "Beastlord" },
        { "Animist", "Beastlord" },
        { "Savage Lord", "Beastlord" },
        { "Feral Lord", "Beastlord" },
        { "Vicar", "Cleric" },
        { "Templar", "Cleric" },
        { "High Priest", "Cleric" },
        { "Archon", "Cleric" },
        { "Wanderer", "Druid" },
        { "Preserver", "Druid" },
        { "Hierophant", "Druid" },
        { "Storm Warden", "Druid" },
        { "Illusionist", "Enchanter" },
        { "Beguiler", "Enchanter" },
        { "Phantasmist", "Enchanter" },
        { "Coercer", "Enchanter" },
        { "Elementalist", "Magician" },
        { "Conjurer", "Magician" },
        { "Arch Mage", "Magician" },
        { "Arch Convoker", "Magician" },
        { "Disciple", "Monk" },
        { "Master", "Monk" },
        { "Grandmaster", "Monk" },
        { "Transcendent", "Monk" },
        { "Heretic", "Necromancer" },
        { "Defiler", "Necromancer" },
        { "Warlock", "Necromancer" },
        { "Arch Lich", "Necromancer" },
        { "Cavalier", "Paladin" },
        { "Knight", "Paladin" },
        { "Crusader", "Paladin" },
        { "Lord Protector", "Paladin" },
        { "Pathfinder", "Ranger" },
        { "Outrider", "Ranger" },
        { "Warder", "Ranger" },
        { "Forest Stalker", "Ranger" },
        { "Rake", "Rogue" },
        { "Blackguard", "Rogue" },
        { "Assassin", "Rogue" },
        { "Deceiver", "Rogue" },
        { "Reaver", "Shadow Knight" },
        { "Revenant", "Shadow Knight" },
        { "Grave Lord", "Shadow Knight" },
        { "Dread Lord", "Shadow Knight" },
        { "Mystic", "Shaman" },
        { "Luminary", "Shaman" },
        { "Oracle", "Shaman" },
        { "Prophet", "Shaman" },
        { "Champion", "Warrior" },
        { "Myrmidon", "Warrior" },
        { "Warlord", "Warrior" },
        { "Overlord", "Warrior" },
        { "Channeler", "Wizard" },
        { "Evoker", "Wizard" },
        { "Sorceror", "Wizard" },
        { "Arcanist", "Wizard" },
    };

    public static string GetClassFromTitle(string title)
    {
        if (_classNameAlias.TryGetValue(title, out string className))
            return className;

        return title;
    }
}
