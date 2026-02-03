namespace NodeFlow.Server.Nodes.Common.Extension;

public static class StringExtension
{
    public static int ConvertTimeToMilliseconds(this string timeName, int value)
    {
        var delayMilliseconds = timeName.ToLowerInvariant() switch
        {
            "millisecond" => value,
            "second" => value * 1000,
            "minute" => value * 60 * 1000,
            _ => throw new InvalidOperationException($"Invalid Time: {timeName}")
        };

        return delayMilliseconds;
    }

    // public static string ToTitleCase(this string input)
    // {
    // }

    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var words = input.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (!string.IsNullOrEmpty(words[i]))
            {
                words[i] = char.ToUpper(words[i][0]) + words[i][1..].ToLower();
            }
        }

        return string.Join(" ", words);
    }

    public static string Truncate(this string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return input.Length <= maxLength ? input : input[..maxLength] + "...";
    }

    public static string RemoveSpecialCharacters(this string input)
    {
        return string.IsNullOrEmpty(input)
            ? input
            : new string(input.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());
    }
}