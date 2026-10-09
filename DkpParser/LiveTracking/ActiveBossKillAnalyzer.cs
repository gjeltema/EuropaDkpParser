// -----------------------------------------------------------------------
// ActiveBossKillAnalyzer.cs Copyright 2026 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.LiveTracking;

using Gjeltema.Logging;

internal sealed class ActiveBossKillAnalyzer
{
    private const string Expires = " that expires in ";
    private const string HasKilled = " has killed ";
    private const string In = " in ";
    private const string LogPrefix = $"[{nameof(ActiveBossKillAnalyzer)}]";
    private readonly IRaidValues _raidValues;

    public ActiveBossKillAnalyzer(IRaidValues raidValues)
    {
        _raidValues = raidValues;
    }

    public string GetBossKillName(string logLine)
    {
        if (logLine.Contains(Constants.Lockout))
        {
            // [Wed Jan 14 23:41:07 2026] You have incurred a lockout for Va Xi Aten Ha Ra that expires in 6 Days and 18 Hours.
            Log.Debug($"{LogPrefix} Lockout message: {logLine}");
            int indexOfEndOfLockout = logLine.IndexOf(Constants.Lockout) + Constants.Lockout.Length;
            int indexOfExpires = logLine.IndexOf(Expires);

            string bossName = logLine[indexOfEndOfLockout..indexOfExpires].Trim();
            if (_raidValues.BossesWithNoDruzzilMessage.Contains(bossName))
            {
                Log.Debug($"{LogPrefix} Returning boss name: {bossName}");
                return bossName;
            }
        }
        else if (logLine.Contains(Constants.Slain))
        {
            // [Wed Jan 14 23:41:07 2026] Va Xi Aten Ha Ra has been slain by Motanz!
            Log.Debug($"{LogPrefix} Slain message: {logLine}");
            string[] split = logLine.Split(Constants.Slain);
            if (split.Length != 2)
                return null;

            string bossName = split[0].Trim();
            if (_raidValues.BossesWithNoDruzzilMessage.Contains(bossName))
            {
                Log.Debug($"{LogPrefix} Returning boss name: {bossName}");
                return bossName;
            }
        }
        else if (logLine.Contains(Constants.DruzzilGuild) && logLine.Contains(HasKilled))
        {
            // [Wed Jan 14 23:41:07 2026] Druzzil Ro tells the guild, 'Brydda of <Europa> has killed Va Xi Aten Ha Ra in Vex Thal!'
            // [Thu Oct 01 22:45:40 2026] Druzzil Ro tells the guild, 'A ripple passes through your guild's thread of time. Kassandra has asked the Timekeeper of Druzzil Ro to meddle with its weave.
            //     Beware! Time now moves slowly within your Planar Thread, and those walking it have been drawn back to Tranquility.'
            // [Fri Oct 02 00:29:01 2026] Druzzil Ro tells the guild, 'A ripple passes through your guild's thread of time. Tarscales has asked the Timekeeper of Druzzil Ro to set its weave right.
            //     Beware! Time has returned to its natural pace, and those walking it have been drawn back to Tranquility.'
            Log.Debug($"{LogPrefix} Druzzil message: {logLine}");
            int inIndex = logLine.IndexOf(In);
            int killedIndex = logLine.IndexOf(HasKilled) + HasKilled.Length;
            if (inIndex <= killedIndex)
                return null;

            string bossName = logLine[killedIndex..inIndex].Trim();
            Log.Debug($"{LogPrefix} Returning boss name: {bossName}");
            return bossName;
        }

        return null;
    }
}
