namespace FluxLang;

public class Lexer
{
    private readonly string _source;
    private int _position;

    public Lexer(string source)
    {
        _source = source;
        _position = 0;
    }
    
    private char Current => _position >= _source.Length ? '\0' : _source[_position];
    private void Advance() => _position++;

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (_position < _source.Length)
        {
            if (char.IsWhiteSpace(Current))
            {
                Advance();
                continue;
            }

            if (char.IsDigit(Current))
            {
                string number = "";
                while (char.IsDigit(Current))
                {
                    number += Current;
                    Advance();
                }
                
                tokens.Add(new Token(TokenType.Number, number));
                continue;
            }

            if (char.IsLetter(Current) || Current == '_')
            {
                string ident = "";
                while (char.IsLetterOrDigit(Current) || Current == '_')
                {
                    ident += Current;
                    Advance();
                }

                switch (ident)
                {
                    case "int":
                        tokens.Add(new Token(TokenType.TypeInt, ident));
                        break;
                    case "ptr":
                        tokens.Add(new Token(TokenType.TypePtr, ident));
                        break;
                    case "char":
                        tokens.Add(new Token(TokenType.TypeChar, ident));
                        break;
                    case "void":
                        tokens.Add(new Token(TokenType.TypeVoid, ident));
                        break;
                    case "bool":
                        tokens.Add(new Token(TokenType.TypeBool, ident));
                        break;
                    case "return":
                        tokens.Add(new Token(TokenType.Return, ident));
                        break;
                    case "memory_allocation":
                        tokens.Add(new Token(TokenType.Alloc, ident));
                        break;
                    case "free_memory":
                        tokens.Add(new Token(TokenType.Free, ident));
                        break;
                    case "print_memory":
                        tokens.Add(new Token(TokenType.Print, ident));
                        break;
                    case "print":
                        tokens.Add(new Token(TokenType.Print, ident));
                        break;
                    case "if":
                        tokens.Add(new Token(TokenType.If, ident));
                        break;
                    case "else":
                        tokens.Add(new Token(TokenType.Else, ident));
                        break;
                    case "exit":
                        tokens.Add(new Token(TokenType.Exit, ident));
                        break;
                    case "input":
                        tokens.Add(new Token(TokenType.Input, ident));
                        break;
                    case "input_str":
                        tokens.Add(new Token(TokenType.InputStr, ident));
                        break;
                    case "while":
                        tokens.Add(new Token(TokenType.While, ident));
                        break;
                    case "for":
                        tokens.Add(new Token(TokenType.For, ident));
                        break;
                    case "do":
                        tokens.Add(new Token(TokenType.Do, ident));
                        break;
                    case "import":
                        tokens.Add(new Token(TokenType.Import, ident));
                        break;
                    case "sqrt":
                        tokens.Add(new Token(TokenType.Sqrt, ident));
                        break;
                    case "time":
                        tokens.Add(new Token(TokenType.Time, ident));
                        break;
                    case "sleep":
                        tokens.Add(new Token(TokenType.Sleep, ident));
                        break;
                    case "rand":
                        tokens.Add(new Token(TokenType.Rand, ident));
                        break;
                    case "srand":
                        tokens.Add(new Token(TokenType.Srand, ident));
                        break;
                    case "struct":
                        tokens.Add(new Token(TokenType.Struct, ident));
                        break;
                    case "true":
                        tokens.Add(new Token(TokenType.True, ident));
                        break;
                    case "false":
                        tokens.Add(new Token(TokenType.False, ident));
                        break;
                    default:
                        tokens.Add(new Token(TokenType.Identifier, ident));
                        break;
                }
                
                continue;   
            }

            switch (Current)
            {
                case '=':
                    Advance();
                    if (Current == '=')
                    {
                        tokens.Add(new Token(TokenType.EqualEqual, "=="));
                        Advance();
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Assign, "="));
                    }
                    break;
                case '!':
                    Advance();
                    if (Current == '=')
                    {
                        tokens.Add(new Token(TokenType.NotEqual, "!="));
                        Advance();
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Not, "!"));
                    }
                    break;
                case '<':
                    Advance();
                    if (Current == '=')
                    {
                        tokens.Add(new Token(TokenType.LessEqual, "<="));
                        Advance();
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Less, "<"));
                    }
                    break;
                case '>':
                    Advance();
                    if (Current == '=')
                    {
                        tokens.Add(new Token(TokenType.GreaterEqual, ">="));
                        Advance();
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Greater, ">"));
                    }
                    break;
                case '&':
                    Advance();
                    if (Current == '&')
                    {
                        tokens.Add(new Token(TokenType.And, "&&"));
                        Advance();
                    }
                    else
                    {
                        throw new Exception("Unexpected character: & (expected &&)");
                    }
                    break;
                case '|':
                    Advance();
                    if (Current == '|')
                    {
                        tokens.Add(new Token(TokenType.Or, "||"));
                        Advance();
                    }
                    else
                    {
                        throw new Exception("Unexpected character: | (expected ||)");
                    }
                    break;
                case '(':
                    tokens.Add(new Token(TokenType.OpenParenthesis, "("));
                    Advance();
                    break;
                case ')':
                    tokens.Add(new Token(TokenType.CloseParenthesis, ")"));
                    Advance();
                    break;
                case ';':
                    tokens.Add(new Token(TokenType.Semicolon, ";"));
                    Advance();
                    break;
                case '*':
                    tokens.Add(new Token(TokenType.Asterisk, "*"));
                    Advance();
                    break;
                case '"':
                    Advance();
                    string text = "";
                    while (Current != '"')
                    {
                        if (Current == '\\')
                        {
                            Advance();
                            switch (Current)
                            {
                                case 'n':
                                    text += '\n';
                                    break;
                                case 'r':
                                    text += '\r';
                                    break;
                                case '\t':
                                    text += '\t';
                                    break;
                                case '\\':
                                    text += '\\';
                                    break;
                                case '"':
                                    text += '"';
                                    break;
                                default:
                                    text += '\\';
                                    text += Current;
                                    break;
                            }
                        }
                        else
                            text += Current;
                        Advance();
                    }

                    if (Current == '\0')
                    {
                        throw new Exception("Syntax error: Missing closing ' in the string");
                    }

                    Advance();
                    tokens.Add(new Token(TokenType.StringLiteral, text));
                    break;
                case '-':
                    Advance();
                    if (Current == '>')
                    {
                        tokens.Add(new Token(TokenType.Arrow, "->"));
                        Advance();
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Minus, "-"));
                    }
                    break;
                case '+':
                    tokens.Add(new Token(TokenType.Plus, "+"));
                    Advance();
                    break;
                case '%':
                    tokens.Add(new Token(TokenType.Percent, "%"));
                    Advance();
                    break;
                case '/':
                    Advance();
                    if (Current == '/')
                    {
                        Advance(); // Skip second /
                        string comment = "";
                        while (Current != '\n' && Current != '\0')
                        {
                            comment += Current;
                            Advance();
                        }
                        tokens.Add(new Token(TokenType.SlashSlash, comment.Trim()));
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Slash, "/"));
                    }
                    break;
                case ',':
                    tokens.Add(new Token(TokenType.Comma, ","));
                    Advance();
                    break;
                case '[':
                    tokens.Add(new Token(TokenType.OpenBracket, "["));
                    Advance();
                    break;
                case ']':
                    tokens.Add(new Token(TokenType.CloseBracket, "]"));
                    Advance();
                    break;
                case '{':
                    tokens.Add(new Token(TokenType.OpenBrace, "{"));                    
                    Advance();
                    break;
                case '}':
                    tokens.Add(new Token(TokenType.CloseBrace, "}"));
                    Advance();
                    break;
                case '\'':
                    Advance();
                    char c = Current;
                    Advance();
                    if (Current != '\'')
                    {
                        throw new Exception("Syntax error: Missing closing ' after character");
                    }
                    Advance();
                    tokens.Add(new Token(TokenType.CharLiteral, c.ToString()));
                    break;
                default:
                    throw new Exception($"Unexpected character: {Current}");
            }
        }
        
        tokens.Add(new Token(TokenType.EOF, ""));
        return tokens;
    }
}