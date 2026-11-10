using System.Globalization;
using System.Text;

namespace Autoredi.Generators;

internal static class AutorediNaming
{
    /// <summary>
    /// Unicode category helpers that iterate UTF-16 code units but interpret surrogate
    /// pairs correctly via <see cref="CharUnicodeInfo.GetUnicodeCategory(string, int)"/>.
    /// The generator multi-targets netstandard2.0, so Rune APIs are unavailable.
    /// </summary>
    private static bool IsAlphaNumericAt(string value, int index, out int width)
    {
        var category = CharUnicodeInfo.GetUnicodeCategory(value, index);
        width = char.IsSurrogatePair(value, index) ? 2 : 1;
        return category is UnicodeCategory.UppercaseLetter
            or UnicodeCategory.LowercaseLetter
            or UnicodeCategory.TitlecaseLetter
            or UnicodeCategory.ModifierLetter
            or UnicodeCategory.OtherLetter
            or UnicodeCategory.DecimalDigitNumber
            or UnicodeCategory.LetterNumber
            or UnicodeCategory.OtherNumber;
    }

    private static bool IsLetterStartAt(string value, int index)
    {
        var category = CharUnicodeInfo.GetUnicodeCategory(value, index);
        return category is UnicodeCategory.UppercaseLetter
            or UnicodeCategory.LowercaseLetter
            or UnicodeCategory.TitlecaseLetter
            or UnicodeCategory.ModifierLetter
            or UnicodeCategory.OtherLetter;
    }

    public static string ToIdentifierFragment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Default";
        }

        var builder = new StringBuilder(value.Length + 1);
        var word = new StringBuilder();

        var index = 0;
        while (index < value.Length)
        {
            if (IsAlphaNumericAt(value, index, out var width))
            {
                word.Append(value, index, width);
                index += width;
                continue;
            }

            AppendWord(builder, word);
            index += width;
        }

        AppendWord(builder, word);

        if (builder.Length == 0)
        {
            return "Default";
        }

        if (!IsLetterStartAt(builder.ToString(), 0) && builder[0] != '_' && !char.IsHighSurrogate(builder[0]))
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
    /// The encoding is injective: two different assembly names never produce the same
    /// namespace, so generated types from sibling assemblies can never collide.
    /// A readable prefix is preserved where possible; non-identifier characters are
    /// encoded as <c>_uXXXX_</c> (lowercase hex), <c>_</c> becomes <c>__</c>, a keyword
    /// or otherwise invalid leading segment gets a single <c>_</c> prefix, and an empty
    /// segment becomes <c>_empty</c>.
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
        if (IsLetterStartAt(value, 0))
        {
            expected = char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        return !string.Equals(expected, ToIdentifierFragment(value), StringComparison.Ordinal);
    }

    private static string ToNamespaceSegment(string value)
    {
        if (value.Length == 0)
        {
            return "_empty";
        }

        var builder = new StringBuilder(value.Length * 2);
        var index = 0;
        while (index < value.Length)
        {
            if (IsAlphaNumericAt(value, index, out var width))
            {
                builder.Append(value, index, width);
                index += width;
                continue;
            }

            if (value[index] == '_')
            {
                builder.Append("__");
                index += 1;
                continue;
            }

            builder.Append("_u");
            for (var offset = 0; offset < width; offset++)
            {
                builder.Append(((int)value[index + offset]).ToString("x4", CultureInfo.InvariantCulture));
            }

            builder.Append('_');
            index += width;
        }

        var identifier = builder.ToString();
        if (!IsLetterStartAt(identifier, 0) && identifier[0] != '_')
        {
            identifier = "_" + identifier;
        }

        if (SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None)
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
        builder.Append(char.IsHighSurrogate(first) ? first : char.ToUpperInvariant(first));

        if (word.Length > 1)
        {
            builder.Append(word.ToString(1, word.Length - 1));
        }

        word.Clear();
    }
}
