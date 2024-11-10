// -----------------------------------------------------------------------
// ChannelAnalyzer.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

internal sealed class ChannelAnalyzer
{
    private readonly IDkpParserSettings _settings;

    public ChannelAnalyzer(IDkpParserSettings settings)
    {
        _settings = settings;
    }

    public EqChannel GetValidDkpChannel(string logLine)
    {
        if (logLine.Contains(Constants.RaidYouSearch) || logLine.Contains(Constants.RaidOther))
            return EqChannel.Raid;
        else if (_settings.DkpspentGuEnabled && (logLine.Contains(Constants.GuildYouSearch) || logLine.Contains(Constants.GuildOther)))
            return EqChannel.Guild;
        else if (_settings.DkpspentOocEnabled && (logLine.Contains(Constants.OocYouSearch) || logLine.Contains(Constants.OocOther)))
            return EqChannel.Ooc;
        else if (_settings.DkpspentAucEnabled && (logLine.Contains(Constants.AuctionYouSearch) || logLine.Contains(Constants.AuctionOther)))
            return EqChannel.Auction;

        return EqChannel.None;
    }
}
