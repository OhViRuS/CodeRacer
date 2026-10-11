namespace CodeRacer.Server.Models;

public readonly struct CharacterMistake
{
    public int PositionIndex { get; }
    public char ExpectedCharacter { get; }
    public char TypedCharacter { get; }

    public CharacterMistake(int positionIndex, char expectedCharacter, char typedCharacter)
    {
        PositionIndex = positionIndex;
        ExpectedCharacter = expectedCharacter;
        TypedCharacter = typedCharacter;
    }
}