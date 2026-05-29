using Specforge.Core.Exceptions;

namespace Specforge.Core.Identifiers;

/// <summary>
/// Orchestrates identifier validation: parse → kind-registry membership → qualified-package
/// resolution. Pure (no I/O); consumes a pre-built <see cref="KindRegistry"/> and
/// <see cref="ValidationContext"/>. Throws <see cref="SpecforgeInvalidIdentifierException"/> on any failure.
/// </summary>
public static class IdValidator
{
    /// <summary>Human-readable patterns reported in the <c>expectedPatterns</c> envelope field.</summary>
    public static IReadOnlyList<string> ExpectedPatterns { get; } =
    [
        "^[A-Z]{2,6}-[0-9]{3}$",
        "^REV-[A-Z]{2,6}-[0-9]{3}-[0-9]{3}$",
        "^ART-[A-Z][A-Z0-9-]*[A-Z0-9]$",
        "<package>/<bare-identifier>",
    ];

    /// <summary>Validates <paramref name="input"/>, throwing if it is malformed, names an unknown kind, or references an unknown package.</summary>
    public static void Validate(string input, KindRegistry registry, ValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(context);

        if (!IdParser.TryParse(input, out ParsedIdentifier? parsed) || parsed is null)
        {
            throw new SpecforgeInvalidIdentifierException(input ?? string.Empty, ExpectedPatterns);
        }

        if (parsed is QualifiedIdentifier qualified)
        {
            if (!context.KnownPackages.Contains(qualified.Package))
            {
                throw new SpecforgeInvalidIdentifierException(input, ExpectedPatterns);
            }

            ValidateKind(input, qualified.Inner, registry);
            return;
        }

        ValidateKind(input, parsed, registry);
    }

    private static void ValidateKind(string input, ParsedIdentifier parsed, KindRegistry registry)
    {
        bool ok = parsed switch
        {
            AtomicIdentifier atomic => registry.IsKnownKind(atomic.Kind),
            CompositeReviewIdentifier review => registry.IsKnownKind(review.Target.Kind),
            DescriptiveArtIdentifier => true, // ART is always a core kind
            _ => false,
        };

        if (!ok)
        {
            throw new SpecforgeInvalidIdentifierException(input, ExpectedPatterns);
        }
    }
}
