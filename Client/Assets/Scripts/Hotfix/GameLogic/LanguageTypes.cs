using System.Collections.Generic;

/// <summary>
/// AutoGenerate
/// </summary>
public static class LanguageTypes
{
    public const string ChineseSimplified = "ChineseSimplified";
    public const string English = "English";
    public const string Japanese = "Japanese";
    public const string Russian = "Russian";

    public static readonly IReadOnlyList<string> Languages = new List<string>
    {
        "ChineseSimplified",
        "English",
        "Japanese",
        "Russian",
    };

    public static string IndexToString(int index)
    {
        if (index < 0 || index >= Languages.Count) return "Unknown";
        return Languages[index];
    }

    public static int StringToIndex(string s)
    {
        int index = -1;
        for (int i = 0; i < Languages.Count; i++)
        {
            if (Languages[i] == s)
            {
                index = i;
                break;
            }
        }

        return index;
    }
}
