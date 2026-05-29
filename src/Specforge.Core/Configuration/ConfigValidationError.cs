namespace Specforge.Core.Configuration;

/// <summary>
/// One shape-validation failure, located by a JSON Pointer (RFC 6901) into the config DOM.
/// </summary>
/// <param name="Pointer">JSON Pointer to the offending node, e.g. <c>/packages/0/name</c> (empty string = document root).</param>
/// <param name="Message">Human/AI-readable description of the problem.</param>
public sealed record ConfigValidationError(string Pointer, string Message);
