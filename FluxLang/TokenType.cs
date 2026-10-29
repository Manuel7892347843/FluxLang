namespace FluxLang;

public enum TokenType
{
    TypeInt, TypePtr, TypeChar, TypeVoid, TypeBool, TypeFloat, TypeDouble,
    TypeShort, TypeLong, TypeByte, TypeUint8,
    Return, Alloc, Free, Print, Asterisk, Comma,
    StringLiteral, CharLiteral,
    Plus, Minus, Slash, SlashSlash, Percent,
    If, Else, OpenBrace, CloseBrace,
    EqualEqual, NotEqual, Less, Greater, LessEqual, GreaterEqual,
    And, Or, Not,
    While, For, Do, Switch, Case, Default, Break, Continue,
    Identifier, Number, FloatLiteral,
    OpenParenthesis, CloseParenthesis, Assign, Semicolon,
    Exit, Sqrt, Time, Sleep, Rand, Srand,
    Input, InputStr,
    OpenBracket, CloseBracket, Arrow, Import,
    Struct, Typedef, Enum, True, False,
    BitAnd, BitOr, BitXor, BitNot, ShiftLeft, ShiftRight,
    PlusAssign, MinusAssign, StarAssign, SlashAssign,
    PlusPlus, MinusMinus, Ampersand, Colon,
    Try, Catch, Throw, SetErrorCode,
    EOF
}

public class Token
{
    public TokenType Type { get; set; }
    public string Value { get; set; }

    public Token(TokenType type, string value)
    {
        Type = type;
        Value = value;
    }
    public override string ToString() => $"{Type} {Value}";
}