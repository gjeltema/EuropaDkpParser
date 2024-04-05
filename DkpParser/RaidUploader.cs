// -----------------------------------------------------------------------
// FileOutputGenerator.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class RaidUploader : IRaidUpload
{
    private readonly DkpParserSettings _settings;

    public RaidUploader(DkpParserSettings settings)
    {
        _settings = settings;
    }

    public RaidUploadResults UploadRaid(RaidEntries raidEntries)
    {

        return null;
    }
}

public sealed class RaidUploadResults
{

}

public interface IRaidUpload
{
    RaidUploadResults UploadRaid(RaidEntries raidEntries);
}
