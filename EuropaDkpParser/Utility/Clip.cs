// -----------------------------------------------------------------------
// Clip.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Utility;

internal static class Clip
{
    public static bool Copy(string text)
    {
        try
        {
            System.Windows.Clipboard.SetText(text);
        }
        catch
        {
            return false;
        }
        return true;
    }
}
