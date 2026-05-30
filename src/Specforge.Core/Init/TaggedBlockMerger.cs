using Specforge.Core.Exceptions;

namespace Specforge.Core.Init;

/// <summary>
/// Merges the specforge tagged block into <c>CLAUDE.md</c> / <c>AGENTS.md</c> (DEC-008): replace
/// content between the delimiters if present, prepend if absent, create if the file is missing. User
/// content outside the block is preserved byte-for-byte. Imbalanced delimiters are a hard error.
/// </summary>
public sealed class TaggedBlockMerger
{
    /// <summary>Exact opening delimiter (public contract, DEC-008).</summary>
    public const string StartDelimiter = "<!-- specforge:start -->";

    /// <summary>Exact closing delimiter (public contract, DEC-008).</summary>
    public const string EndDelimiter = "<!-- specforge:end -->";

    /// <summary>
    /// Returns the new file content. <paramref name="existing"/> is the current file text, or
    /// <see langword="null"/> when the file does not exist. Throws
    /// <see cref="SpecforgeInvalidArgumentException"/> for an imbalanced block in <paramref name="filePathForError"/>.
    /// </summary>
    public string Merge(string? existing, string blockBody, string filePathForError)
    {
        string block = $"{StartDelimiter}\n{blockBody}\n{EndDelimiter}";

        if (string.IsNullOrEmpty(existing))
        {
            return block + "\n";
        }

        int start = existing.IndexOf(StartDelimiter, StringComparison.Ordinal);
        int end = existing.IndexOf(EndDelimiter, StringComparison.Ordinal);

        if (start < 0 && end < 0)
        {
            // No block yet — prepend with a blank line of separation.
            return block + "\n\n" + existing;
        }

        if (start < 0 || end < 0 || end < start)
        {
            throw new SpecforgeInvalidArgumentException(
                filePathForError,
                "balanced <!-- specforge:start --> ... <!-- specforge:end --> block",
                "remove the partial block or run init with behavioralFiles=none");
        }

        int endFull = end + EndDelimiter.Length;
        return existing[..start] + block + existing[endFull..];
    }
}
