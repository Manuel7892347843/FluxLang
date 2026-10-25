namespace FluxLang;

public enum TokenType
{
    TypeInt,            // int
    TypePtr,            // ptr
    TypeChar,           // char
    TypeVoid,           // void
    
    Return,             // return
    
    Alloc,              // memory_allocation
    Free,               // free_memory
    
    Print,
    Asterisk,
    Comma,
    
    StringLiteral,
    CharLiteral,
    
    Plus,
    Minus,
    Slash,
    SlashSlash,
    
    If,
    Else,
    OpenBrace,
    CloseBrace,
    EqualEqual,
    NotEqual,
    Less,
    Greater,
    LessEqual,
    GreaterEqual,
    
    And,
    Or,
    Not,
    
    While,
    For,
    Do,
    
    Identifier,
    Number,
    OpenParenthesis,
    CloseParenthesis,
    Assign,
    Semicolon,
    
    Exit,
    Sqrt,
    Time,
    Sleep,
    
    Input,
    OpenBracket,
    CloseBracket,
    
    Import,
    
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