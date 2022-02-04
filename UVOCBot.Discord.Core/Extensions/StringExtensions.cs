namespace System;

public static class StringExtensions
{
    /// <summary>
    /// Removes HTML tags from a string.
    /// </summary>
    /// <param name="input">The input string to sanitise.</param>
    /// <param name="maxLength">The maximum length of the output string.</param>
    /// <returns>The sanitised string.</returns>
    public static string RemoveHtml(this string input, int maxLength = -1)
    {
        string result = string.Empty;

        foreach (string c in input.Split('<', StringSplitOptions.RemoveEmptyEntries))
        {
            int closeIndex = c.IndexOf('>');

            if (closeIndex == -1)
                result += c;
            else
                result += c[++closeIndex..];

            if (result.Length > maxLength)
                return result[..maxLength];
        }

        return result;
    }
}
