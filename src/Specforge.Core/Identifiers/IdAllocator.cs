using System.Text.RegularExpressions;

using Specforge.Core.Configuration;
using Specforge.Core.Exceptions;

namespace Specforge.Core.Identifiers;

/// <summary>
/// Default, non-tombstone-aware <see cref="IIdAllocator"/> (DEC-004). Scans the package's ledger file
/// for the kind, returns <c>max + 1</c>. ITEM-010 layers tombstone-gap awareness on top via a decorator.
/// </summary>
public sealed partial class IdAllocator(ILedgerReader ledgerReader, SessionState session) : IIdAllocator
{
    /// <summary>The 3-digit ceiling (DEC-004).</summary>
    public const int MaxNumber = 999;

    [GeneratedRegex("^CMT-([0-9]{3})$")]
    private static partial Regex CommitNumber();

    [GeneratedRegex("^REV-[A-Z]{2,6}-[0-9]{3}-([0-9]{3})$")]
    private static partial Regex ReviewSequence();

    public async Task<int> NextNumberAsync(string kind, string packageName, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(kind);

        string ledgerDirectory = ResolveLedgerDirectory(packageName);
        (string fileName, Regex extractor) = SelectionFor(kind);
        string path = Path.Combine(ledgerDirectory, fileName);

        IReadOnlyList<string> ids = await ledgerReader.ReadLedgerIdsAsync(path, ct).ConfigureAwait(false);

        int highest = 0;
        foreach (string id in ids)
        {
            Match match = extractor.Match(id);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int number) && number > highest)
            {
                highest = number;
            }
        }

        int next = highest + 1;
        if (next > MaxNumber)
        {
            throw new SpecforgeKindExhaustedException(kind, packageName);
        }

        return next;
    }

    private static (string FileName, Regex Extractor) SelectionFor(string kind) => kind switch
    {
        "CMT" => ("commits.md", CommitNumber()),
        "REV" => ("reviews.md", ReviewSequence()),
        // ART-ITEM-* mirroring rows live in items.md (ITEM-008 routing); everything else in artifacts.md.
        "ITEM" => ("items.md", MirrorNumber("ITEM")),
        _ => ("artifacts.md", MirrorNumber(kind)),
    };

    // Mirroring artifact rows take the form ART-<KIND>-<NNN>; the kind is interpolated at runtime
    // (extra kinds are unknown at compile time), so this regex cannot be source-generated.
    private static Regex MirrorNumber(string kind) =>
        new($"^ART-{Regex.Escape(kind)}-([0-9]{{3}})$", RegexOptions.CultureInvariant);

    private string ResolveLedgerDirectory(string packageName)
    {
        SpecforgeConfig config = session.Config ?? throw new InvalidOperationException("Configuration is not loaded.");
        SpecforgePackageConfig package = config.Packages.FirstOrDefault(p => string.Equals(p.Name, packageName, StringComparison.Ordinal))
            ?? throw new SpecforgeUnknownPackageException(packageName, config.Packages);

        string configDirectory = Path.GetDirectoryName(config.ConfigPath) ?? ".";
        return Path.Combine(configDirectory, package.Path, "ledger");
    }
}
