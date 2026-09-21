namespace CodeRacer.Server.Models
{
    public class CodeSnippet
    {
        public Guid Id { get; set; }
        public string CodeText { get; set; } = string.Empty;
        public ProgrammingLanguage Language { get; set; }
        public Difficulty Difficulty { get; set; }
        public string ProgrammingConcept { get; set; } = string.Empty;
    }
}