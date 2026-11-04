namespace CodeRacer.Server.Models;

public enum ImportFailureReason
{
    NoFile,
    EmptyFile,
    UnsupportedExtension,
    FileTooLarge,
    NotTextFile
}
