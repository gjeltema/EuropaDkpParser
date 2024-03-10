// -----------------------------------------------------------------------
// Strings.cs Copyright 2024 Craig Gjeltema
// -----------------------------------------------------------------------

namespace EuropaDkpParser.Resources;

using System.Windows;

public class Strings
{
    private static readonly ResourceDictionary stringResourceDictionary;

    static Strings()
    {
        stringResourceDictionary = new ResourceDictionary
        {
            Source = new Uri("Resources/StringResources.xaml", UriKind.RelativeOrAbsolute)
        };
    }

    public static string GetString(string resourceKey)
        => (string)stringResourceDictionary[resourceKey];
}
