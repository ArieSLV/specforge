using System.Text;
using System.Text.RegularExpressions;

using Specforge.Core.Exceptions;

namespace Specforge.Core.Identifiers;

/// <summary>
/// Validates and normalizes filename title slugs per DEC-004 §"File-Naming Convention":
/// <c>^[A-Z][A-Z0-9-]*[A-Z0-9]$</c>, max 60 chars, no leading/trailing/consecutive hyphens.
/// </summary>
public static partial class TitleSlug
{
    /// <summary>Maximum slug length (DEC-004).</summary>
    public const int MaxLength = 60;

    private const string PatternText = "^[A-Z][A-Z0-9-]*[A-Z0-9]$ (max 60 chars, no consecutive hyphens)";

    [GeneratedRegex("^[A-Z][A-Z0-9-]*[A-Z0-9]$")]
    private static partial Regex SlugShape();

    /// <summary>Throws <see cref="SpecforgeInvalidIdentifierException"/> if <paramref name="slug"/> violates the rules.</summary>
    public static void Validate(string slug)
    {
        if (string.IsNullOrEmpty(slug)
            || slug.Length > MaxLength
            || slug.Contains("--", StringComparison.Ordinal)
            || !SlugShape().IsMatch(slug))
        {
            throw new SpecforgeInvalidIdentifierException(slug ?? string.Empty, [PatternText]);
        }
    }

    /// <summary>
    /// Normalizes a plain title (<c>"Distribution and Transport"</c>) into a slug
    /// (<c>"DISTRIBUTION-AND-TRANSPORT"</c>). Throws if the result is empty after normalization.
    /// </summary>
    public static string FromTitle(string title)
    {
        ArgumentNullException.ThrowIfNull(title);

        StringBuilder builder = new();
        bool lastWasHyphen = false;
        foreach (char ch in title.ToUpperInvariant())
        {
            if (ch is (>= 'A' and <= 'Z') or (>= '0' and <= '9'))
            {
                builder.Append(ch);
                lastWasHyphen = false;
            }
            else if (builder.Length > 0 && !lastWasHyphen)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        string slug = builder.ToString().Trim('-');
        if (slug.Length > MaxLength)
        {
            slug = slug[..MaxLength].Trim('-');
        }

        if (slug.Length == 0)
        {
            throw new SpecforgeInvalidIdentifierException(title, [PatternText]);
        }

        Validate(slug);
        return slug;
    }
}
