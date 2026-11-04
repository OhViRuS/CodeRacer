using System;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Exceptions;

public class ImportedFileException : Exception
{
    public ImportedFileException(ImportFailureReason reason, string message)
        : base(message)
    {
        Reason = reason;
    }

    public ImportFailureReason Reason { get; }
}
