using System.Globalization;

namespace Specforge.Core.Identifiers;

/// <summary>
/// Pure (no I/O, no DI) parser turning an identifier string into a <see cref="ParsedIdentifier"/>.
/// Recognizes the four DEC-004 forms; rejects anything else (including lowercase and wrong widths).
/// </summary>
public static class IdParser
{
    /// <summary>Tries to parse <paramref name="input"/>; returns <see langword="false"/> (and null) on any non-match.</summary>
    public static bool TryParse(string? input, out ParsedIdentifier? id)
    {
        id = null;
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        int slash = input.IndexOf('/', StringComparison.Ordinal);
        if (slash >= 0)
        {
            if (input.IndexOf('/', slash + 1) >= 0)
            {
                return false; // package names never contain '/'
            }

            string package = input[..slash];
            string rest = input[(slash + 1)..];
            if (!IdRegexes.PackageName().IsMatch(package) || !TryParseBare(rest, out ParsedIdentifier? inner) || inner is null)
            {
                return false;
            }

            id = new QualifiedIdentifier(package, inner);
            return true;
        }

        return TryParseBare(input, out id);
    }

    private static bool TryParseBare(string input, out ParsedIdentifier? id)
    {
        id = null;

        if (IdRegexes.CompositeReview().IsMatch(input))
        {
            // REV-<KIND>-<NNN>-<SEQ>
            string[] parts = input.Split('-');
            AtomicIdentifier target = new(parts[1], ParseNumber(parts[2]));
            id = new CompositeReviewIdentifier(target, ParseNumber(parts[3]));
            return true;
        }

        if (IdRegexes.Atomic().IsMatch(input))
        {
            int dash = input.LastIndexOf('-');
            id = new AtomicIdentifier(input[..dash], ParseNumber(input[(dash + 1)..]));
            return true;
        }

        if (IdRegexes.DescriptiveArt().IsMatch(input))
        {
            id = new DescriptiveArtIdentifier(input["ART-".Length..]);
            return true;
        }

        return false;
    }

    private static int ParseNumber(string digits) => int.Parse(digits, CultureInfo.InvariantCulture);
}
