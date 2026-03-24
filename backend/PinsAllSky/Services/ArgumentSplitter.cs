using System.Text;

namespace NINA.PINS.AllSky.Services;

public static class ArgumentSplitter
{
    public static IReadOnlyList<string> Split(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        var parts = new List<string>();
        var current = new StringBuilder();
        var quote = '\0';

        foreach (var character in input)
        {
            if (quote == '\0' && char.IsWhiteSpace(character))
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            if ((character == '"' || character == '\'') && (quote == '\0' || quote == character))
            {
                quote = quote == '\0' ? character : '\0';
                continue;
            }

            current.Append(character);
        }

        if (current.Length > 0)
        {
            parts.Add(current.ToString());
        }

        return parts;
    }
}
