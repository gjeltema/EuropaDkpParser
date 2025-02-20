// -----------------------------------------------------------------------
// Clip.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Utility;

using Gjeltema.Logging;

internal static class Clip
{
    private const string LogPrefix = $"{nameof(Clip)}";

    public static void Copy(string text)
    {
        try
        {
            System.Windows.Clipboard.SetText(text);
            return;
        }
        catch (Exception e)
        {
            Log.Warning($"{LogPrefix} Error copying to clipboard: {e.ToLogMessage()}");
        }

        try
        {
            System.Windows.Clipboard.SetText(text);
        }
        catch (Exception e)
        {
            Log.Warning($"{LogPrefix} Error copying to clipboard: {e.ToLogMessage()}");
        }
    }
}
