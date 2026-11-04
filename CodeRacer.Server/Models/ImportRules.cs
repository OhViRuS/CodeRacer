using System.Collections.Generic;

namespace CodeRacer.Server.Models;

/// <summary>
/// The limits applied to imported files. Defined only on the server; clients read them from the API.
/// </summary>
public sealed record ImportRules(IReadOnlyList<string> SupportedExtensions, long MaxFileSizeBytes);
