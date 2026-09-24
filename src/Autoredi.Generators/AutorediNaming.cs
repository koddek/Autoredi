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

    /// <summary>
    /// Returns the method suffix used for an assembly-wide registration method.
    /// </summary>
    public static string ToAssemblyMethodSuffix(string value)
    {
        var fragment = ToIdentifierFragment(value);
        return string.Equals(fragment, "All", StringComparison.Ordinal)
            ? "AllAssembly"
            : fragment;
    }

    /// <summary>
    /// Converts an assembly name into a namespace-safe sequence of identifier segments.
    /// </summary>
    public static string ToNamespace(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "_";
        }

        var segments = value.Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            segments[i] = ToNamespaceSegment(segments[i]);
        }

        return string.Join(".", segments);
    }

    /// <summary>
    /// Escapes text before embedding it in generated XML documentation.
    /// </summary>
    public static string EscapeXmlText(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '&':
                    builder.Append("&amp;");
                    break;
                case '<':
                    builder.Append("&lt;");
                    break;
                case '>':
                    builder.Append("&gt;");
                    break;
                case '"':
                    builder.Append("&quot;");
                    break;
                case '\'':
                    builder.Append("&apos;");
                    break;
                default:
                    builder.Append(char.IsControl(ch) ? ' ' : ch);
                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Returns whether a group name needs more than the documented PascalCase conversion.
    /// </summary>
    public static bool RequiresSanitization(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var expected = value;
        if (char.IsLetter(value[0]))
        {
            expected = char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        return !string.Equals(expected, ToIdentifierFragment(value), StringComparison.Ordinal);
    }

    private static string ToNamespaceSegment(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch) || ch == '_')
            {
                builder.Append(ch);
            }
        }

        var identifier = builder.ToString();
        if (identifier.Length == 0)
        {
            return "_";
        }

        if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
        {
            identifier = "_" + identifier;
        }

        if (!SyntaxFacts.IsValidIdentifier(identifier) || SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None)
        {
            identifier = "_" + identifier;
        }

        return identifier;
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
