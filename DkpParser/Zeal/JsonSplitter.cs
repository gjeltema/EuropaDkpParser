// -----------------------------------------------------------------------
// JsonSplitter.cs Copyright 2025 Craig Gjeltema
// -----------------------------------------------------------------------

namespace DkpParser.Zeal;

using System.Text;
using System.Text.RegularExpressions;

internal sealed partial class JsonSplitter
{
    private readonly StringBuilder _buffer = new();

    public IEnumerable<string> SplitJson(string data)
    {
        _buffer.Append(data);

        // Split the accumulated data by '}' followed by one or more whitespace characters and '{'
        string[] jsons = JsonSplitRegex().Split(_buffer.ToString());

        // If the last element of jsons ends with '{', it's an incomplete JSON object
        // Append it to the buffer to wait for the completion in the next read
        if (jsons[^1].EndsWith('{'))
        {
            _buffer.Clear();
            _buffer.Append(jsons[^1]);
        }
        else
        {
            _buffer.Clear();
        }

        foreach (string json in jsons)
        {
            yield return json;
        }
    }

    [GeneratedRegex(@"(?<=\})\s*(?=\{)")]
    private static partial Regex JsonSplitRegex();
}
