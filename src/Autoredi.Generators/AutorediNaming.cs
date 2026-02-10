using System.Text;

namespace Autoredi.Generators;

internal static class AutorediNaming
{
    public static string ToIdentifierFragment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Default";
        }

        var builder = new StringBuilder(value.Length + 1);
        var word = new StringBuilder();

        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                word.Append(ch);
                continue;
            }

            AppendWord(builder, word);
        }

        AppendWord(builder, word);

        if (builder.Length == 0)
        {
            return "Default";
        }

        if (!char.IsLetter(builder[0]) && builder[0] != '_')
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }

    private static void AppendWord(StringBuilder builder, StringBuilder word)
    {
        if (word.Length == 0)
        {
            return;
        }

        var first = word[0];
        builder.Append(char.ToUpperInvariant(first));

        if (word.Length > 1)
        {
            builder.Append(word.ToString(1, word.Length - 1));
        }

        word.Clear();
    }
}
