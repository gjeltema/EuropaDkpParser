// -----------------------------------------------------------------------
// ChannelAnalyzer.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

internal sealed class ChannelAnalyzer
{
    private readonly IDkpParserSettings _settings;

    public ChannelAnalyzer(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public EqChannel GetChannel(string logLine)
    {
        if (logLine.Contains(Constants.RaidYou) || logLine.Contains(Constants.RaidOther))
            return EqChannel.Raid;
        else if (logLine.Contains(Constants.GuildYouSearch) || logLine.Contains(Constants.GuildOther))
            return EqChannel.Guild;
        else if (logLine.Contains(Constants.GuildYou) || logLine.Contains(Constants.OocOther))
            return EqChannel.Ooc;
        else if (logLine.Contains(Constants.AuctionYou) || logLine.Contains(Constants.AuctionOther))
            return EqChannel.Auction;
        else if (logLine.Contains(Constants.GroupYou) || logLine.Contains(Constants.GroupOther))
            return EqChannel.Group;
        else if (logLine.Contains(Constants.ReadyCheckChannel) && logLine.Contains(" tell"))
            return EqChannel.ReadyCheck;

        return EqChannel.None;
    }

    public EqChannel GetValidDkpChannel(ReadOnlySpan<char> logLine)
    {
        if (logLine.Contains(Constants.RaidYou) || logLine.Contains(Constants.RaidOther))
            return EqChannel.Raid;
        else if (_settings.DkpspentGuEnabled && (logLine.Contains(Constants.GuildYou) || logLine.Contains(Constants.GuildOther)))
            return EqChannel.Guild;

        return EqChannel.None;
    }

    public bool IsValidDkpChannel(EqChannel channel)
        => channel switch
        {
            EqChannel.Raid => true,
            EqChannel.Guild => _settings.DkpspentGuEnabled,
            _ => false,
        };
}
