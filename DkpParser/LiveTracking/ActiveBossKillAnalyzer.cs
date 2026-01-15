// -----------------------------------------------------------------------
// ActiveBossKillAnalyzer.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.LiveTracking;

internal sealed class ActiveBossKillAnalyzer
{
    private const string DruzzilGuild = "Druzzil Ro tells the guild, '";
    private const string Expires = " that expires in ";
    private const string HasKilled = " has killed ";
    private const string In = " in ";
    private const string Lockout = "You have incurred a lockout for ";
    private const string Slain = " has been slain by ";
    private readonly IRaidValues _raidValues;

    public ActiveBossKillAnalyzer(IRaidValues raidValues)
    {
        _raidValues = raidValues;
    }

    public string GetBossKillName(string logLine)
    {
        if (logLine.Contains(Lockout))
        {
            int indexOfEndOfLockout = logLine.IndexOf(Lockout) + Lockout.Length + 1;
            int indexOfExpires = logLine.IndexOf(Expires);

            string bossName = logLine[indexOfEndOfLockout..indexOfExpires].Trim();

            if (_raidValues.BossesWithNoDruzzilMessage.Contains(bossName))
                return bossName;
        }
        else if (logLine.Contains(Slain))
        {
            string[] split = logLine.Split(Slain);
            if (split.Length != 2)
                return null;

            string bossName = split[0].Trim();

            if (_raidValues.BossesWithNoDruzzilMessage.Contains(bossName))
                return bossName;
        }
        else if (logLine.Contains(DruzzilGuild))
        {
            int inIndex = logLine.IndexOf(In);
            int killedIndex = logLine.IndexOf(HasKilled) + HasKilled.Length;

            return logLine[killedIndex..inIndex];
        }

        return null;
    }
}
