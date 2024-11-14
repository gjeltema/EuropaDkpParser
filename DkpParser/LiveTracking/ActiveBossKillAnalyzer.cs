// -----------------------------------------------------------------------
// ActiveBossKillAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.LiveTracking;

internal sealed class ActiveBossKillAnalyzer
{
    private const string DruzzilGuild = "Druzzil Ro tells the guild, '";
    private const string HasKilled = " has killed ";
    private const string In = " in ";

    public string GetBossKillName(string logLine)
    {
        if (!logLine.Contains(DruzzilGuild))
            return null;

        int inIndex = logLine.IndexOf(In);
        int killedIndex = logLine.IndexOf(HasKilled) + HasKilled.Length;

        return logLine[killedIndex..inIndex];
    }
}
