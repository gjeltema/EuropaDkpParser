// -----------------------------------------------------------------------
// Clip.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Utility;

internal static class Clip
{
    public static void Copy(string text)
    {
        try
        {
            System.Windows.Clipboard.SetText(text);
            return;
        }
        catch
        { }

        try
        {
            System.Windows.Clipboard.SetText(text);
        }
        catch
        { }
    }
}
