namespace Specforge.Core.Identifiers;

/// <summary>
/// The parsed form of a specforge identifier (DEC-004). A closed sum type with four variants:
/// atomic, composite review, descriptive artifact, and qualified (cross-package).
/// </summary>
public abstract record ParsedIdentifier;

/// <summary><c>&lt;KIND&gt;-&lt;NNN&gt;</c>, e.g. <c>DEC-001</c>.</summary>
public sealed record AtomicIdentifier(string Kind, int Number) : ParsedIdentifier;

/// <summary><c>REV-&lt;KIND&gt;-&lt;NNN&gt;-&lt;SEQ&gt;</c>, e.g. <c>REV-DEC-001-002</c> — a per-target review event.</summary>
public sealed record CompositeReviewIdentifier(AtomicIdentifier Target, int Sequence) : ParsedIdentifier;

/// <summary><c>ART-&lt;SLUG&gt;</c>, e.g. <c>ART-WORK-PLAN</c> (also matches mirroring <c>ART-DEC-001</c> forms).</summary>
public sealed record DescriptiveArtIdentifier(string Slug) : ParsedIdentifier;

/// <summary><c>&lt;package&gt;/&lt;bare-identifier&gt;</c>, e.g. <c>specforge-mvp/DEC-001</c>.</summary>
public sealed record QualifiedIdentifier(string Package, ParsedIdentifier Inner) : ParsedIdentifier;
