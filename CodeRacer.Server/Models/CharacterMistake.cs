namespace CodeRacer.Server.Models
{
    public struct CharacterMistake
    {
        public int PositionIndex { get; set; }
        public char ExpectedCharacter { get; set; }
        public char TypedCharacter { get; set; }
    }
}