// -----------------------------------------------------------------------
// DkpLogGenerationSessionSettings.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser;

public sealed class DkpLogGenerationSessionSettings
{
    public DateTime EndTime { get; init; }

    public IEnumerable<string> FilesToParse { get; init; }

    public string GeneratedFile { get; init; }

    public string SourceDirectory { get; init; }

    public DateTime StartTime { get; init; }
}
