// -----------------------------------------------------------------------
// Clip.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Utility;

using System.Runtime.InteropServices;
using Gjeltema.Logging;

internal static class Clip
{
    private const uint ClipboardCantOpenErrorCode = 0x800401D0;
    private const string LogPrefix = $"[{nameof(Clip)}]";
    private const int NumberOfRetries = 3;

    public static void Copy(string text)
    {
        for (int i = 0; i < NumberOfRetries; i++)
        {
            try
            {
                System.Windows.Clipboard.SetDataObject(text, true);
                return;
            }
            catch (COMException e)
            {
                bool clipboardCantOpen = ((uint)e.ErrorCode) == ClipboardCantOpenErrorCode;
                Log.Warning($"{LogPrefix} COM error copying to clipboard - Clipboard cant open:{clipboardCantOpen}: {e.ToLogMessage()}");
            }
            catch (Exception ex)
            {
                Log.Error($"{LogPrefix} Error copying to clipboard: {ex.ToLogMessage()}");
            }

            Thread.Sleep(1);
        }
    }
}
