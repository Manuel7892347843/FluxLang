using System;
using System.Collections.Generic;

namespace FluxLang;

public class Parser
{
    private readonly List<Token> _tokens;
    private int _position = 0;
    private readonly string _currentFilePath;
    private static readonly HashSet<string> _importedFiles = new HashSet<string>();

    public Parser(List<Token> tokens, string currentFilePath = "")
    {
        _tokens = tokens;
        _currentFilePath = !string.IsNullOrEmpty(currentFilePath) ? Path.GetFullPath(currentFilePath) : "";
    }

    public static void ClearImportCache()
    {
        _importedFiles.Clear();
    }
    
    private Token Current => _position >= _tokens.Count ? _tokens[^1] : _tokens[_position];

    private Token Advance()
    {
        var current = Current;
        _position++;
        return current;
    }

    private Token Match(TokenType type, string errorMessage)
    {
        if (Current.Type == type)
            return Advance();
        throw new Exception($"{errorMessage}. Find insted: {Current}");
    }

    private AstNode ParseComment()
    {
        if (Current.Type == TokenType.SlashSlash)
        {
            var comment = Advance().Value;
            return new DoubleSlashNode(comment);
        }
        throw new Exception($"Expected comment. Find insted: {Current}");
    }

    private AstNode ParseExpression()
    {
        return ParseLogicalOr();
    }

    private AstNode ParseLogicalOr()
    {
        AstNode left = ParseLogicalAnd();
        while (Current.Type == TokenType.Or)
        {
            string op = Advance().Value;
            AstNode right = ParseLogicalAnd();
            left = new BinaryOpNode(left, op, right);
        }
        return left;
    }

    private AstNode ParseLogicalAnd()
    {
        AstNode left = ParseComparisonExpr();
        while (Current.Type == TokenType.And)
        {
            string op = Advance().Value;
            AstNode right = ParseComparisonExpr();
            left = new BinaryOpNode(left, op, right);
        }
        return left;
    }

    private AstNode ParseComparisonExpr()
    {
        AstNode left = ParseAdditive();
        if (IsComparisonOperator(Current.Type))
        {
            string op = Advance().Value;
            AstNode right = ParseAdditive();
            return new ComparisonNode(left, op, right);
        }
        return left;
    }

    private bool IsComparisonOperator(TokenType type)
    {
        return type == TokenType.EqualEqual || type == TokenType.NotEqual ||
               type == TokenType.Less || type == TokenType.Greater ||
               type == TokenType.LessEqual || type == TokenType.GreaterEqual;
    }

    private AstNode ParseAdditive()
    {
        AstNode left = ParseMultiplicative();
        while (Current.Type == TokenType.Plus || Current.Type == TokenType.Minus)
        {
            string op = Advance().Value;
            AstNode right = ParseMultiplicative();
            left = new BinaryOpNode(left, op, right);
        }
        return left;
    }

    private AstNode ParseMultiplicative()
    {
        AstNode left = ParseUnary();
        while (Current.Type == TokenType.Asterisk || Current.Type == TokenType.Slash || Current.Type == TokenType.Percent)
        {
            string op = Advance().Value;
            AstNode right = ParseUnary();
            left = new BinaryOpNode(left, op, right);
        }
        return left;
    }

    private AstNode ParseUnary()
    {
        if (Current.Type == TokenType.Minus || Current.Type == TokenType.Not)
        {
            string op = Advance().Value;
            AstNode right = ParseUnary();
            return new UnaryOpNode(op, right);
        }
        return ParsePrimary();
    }

    private AstNode ParsePrimary()
    {
        if (Current.Type == TokenType.Input)
        {
            Advance();
            Match(TokenType.OpenParenthesis, "Missing ( after input");
            Match(TokenType.CloseParenthesis, "Missing ) after input(");
            return new InputNode();
        }

        if (Current.Type == TokenType.InputStr)
        {
            Advance();
            Match(TokenType.OpenParenthesis, "Missing ( after input_str");
            AstNode buffer = ParseExpression();
            Match(TokenType.Comma, "Missing , after buffer in input_str");
            AstNode maxLen = ParseExpression();
            Match(TokenType.CloseParenthesis, "Missing ) after input_str(...)");
            return new InputStrNode(buffer, maxLen);
        }

        if (Current.Type == TokenType.Alloc)
        {
            Advance();
            Match(TokenType.OpenParenthesis, "Missing ( after memory_allocation");
            AstNode size = ParseExpression();
            Match(TokenType.CloseParenthesis, "Missing ) after memory_allocation)");
            return new AllocNode(size);
        }

        if (Current.Type == TokenType.Sqrt)
        {
            Advance();
            Match(TokenType.OpenParenthesis, "Missing ( after sqrt");
            AstNode value = ParseExpression();
            Match(TokenType.CloseParenthesis, "Missing ) after sqrt)");
            return new SqrtNode(value);
        }

        if (Current.Type == TokenType.Time)
        {
            Advance();
            Match(TokenType.OpenParenthesis, "Missing ( after time");
            Match(TokenType.CloseParenthesis, "Missing ) after time()");
            return new TimeNode();
        }

        if (Current.Type == TokenType.Sleep)
        {
            Advance();
            Match(TokenType.OpenParenthesis, "Missing ( after sleep");
            AstNode duration = ParseExpression();
            Match(TokenType.CloseParenthesis, "Missing ) after sleep()");
            return new SleepNode(duration);
        }

        if (Current.Type == TokenType.Rand)
        {
            Advance();
            Match(TokenType.OpenParenthesis, "Missing ( after rand");
            Match(TokenType.CloseParenthesis, "Missing ) after rand()");
            return new RandNode();
        }

        if (Current.Type == TokenType.Srand)
        {
            Advance();
            Match(TokenType.OpenParenthesis, "Missing ( after srand");
            AstNode seed = ParseExpression();
            Match(TokenType.CloseParenthesis, "Missing ) after srand()");
            return new SrandNode(seed);
        }

        if (Current.Type == TokenType.Asterisk)
        {
            Advance();
            string pointerName = Match(TokenType.Identifier, "Expected pointer name after *").Value;
            return new DerefExpressionNode(pointerName);
        }

        if (Current.Type == TokenType.Number)
        {
            return new NumberNode(int.Parse(Advance().Value));
        }

        if (Current.Type == TokenType.Identifier)
        {
            string name = Advance().Value;
            AstNode node;
            if (Current.Type == TokenType.OpenParenthesis)
            {
                Match(TokenType.OpenParenthesis, "Missing (");
                var arguments = new List<AstNode>();
                if (Current.Type != TokenType.CloseParenthesis)
                {
                    arguments.Add(ParseExpression());
                    while (Current.Type == TokenType.Comma)
                    {
                        Advance();
                        arguments.Add(ParseExpression());
                    }
                }
                Match(TokenType.CloseParenthesis, "Missing ) after arguments");
                node = new FunctionCallNode(name, arguments);
            }
            else if (Current.Type == TokenType.OpenBracket)
            {
                Advance();
                AstNode index = ParseExpression();
                Match(TokenType.CloseBracket, "Missing ] after array index");
                node = new ArrayAccessNode(name, index);
            }
            else
            {
                node = new IdentifierNode(name);
            }

            // Handle member access: node->field1->field2
            while (Current.Type == TokenType.Arrow)
            {
                Advance();
                string fieldName = Match(TokenType.Identifier, "Expected field name after ->").Value;
                node = new StructAccessNode(node, fieldName);
            }
            return node;
        }

        if (Current.Type == TokenType.StringLiteral)
        {
            return new StringLiteralNode(Advance().Value);
        }

        if (Current.Type == TokenType.CharLiteral)
        {
            return new CharacterNode(Advance().Value[0]);
        }

        if (Current.Type == TokenType.OpenParenthesis)
        {
            Advance();
            AstNode expression = ParseExpression();
            Match(TokenType.CloseParenthesis, "Missing ) after expression");
            return expression;
        }

        if (Current.Type == TokenType.True)
        {
            Advance();
            return new BoolLiteralNode(true);
        }
        
        if (Current.Type == TokenType.False)
        {
            Advance();
            return new BoolLiteralNode(false);
        }

        throw new Exception($"Expression not valid: {Current.Value}");
    }

    private string ParseType()
    {
        string type = "";
        while (Current.Type == TokenType.Asterisk)
        {
            type += Advance().Value;
        }
        
        if (IsTypeToken(Current.Type))
        {
            type += Advance().Value;
        }
        else
        {
            throw new Exception($"Expected type. Found instead: {Current}");
        }
        return type;
    }

    private AstNode ParseStatement()
    {
        if (Current.Type == TokenType.SlashSlash)
        {
            return ParseComment();
        }

        if (IsTypeStartToken(Current.Type))
        {
            if (IsVariableDeclaration())
            {
                string type = ParseType();
                
                string name = Match(TokenType.Identifier, "Expected the name of the variable").Value;
                
                // Check for char var[size] = ... or int var[size] = ...
                if (Current.Type == TokenType.OpenBracket)
                {
                    Advance();
                    AstNode? size = null;
                    if (Current.Type != TokenType.CloseBracket)
                    {
                        size = ParseExpression();
                    }
                    Match(TokenType.CloseBracket, "Expected ] after array index in declaration");
                    
                    if (type == "char") type = "*char";
                    else if (type == "int") type = "*int";
                    else if (type == "bool") type = "*bool";
                    else if (type == "ptr") type = "*ptr";
                    else if (type == "void") type = "*void";
                }

                AstNode? initializer = null;
                if (Current.Type == TokenType.Assign)
                {
                    Advance();
                    initializer = ParseExpression();

                    // Automatic type adjustment for char name = "string"
                    if (type == "char" && initializer is StringLiteralNode)
                    {
                        type = "*char";
                    }
                }

                Match(TokenType.Semicolon, "Missing ; at the end of the line");
                return new VarDeclNode(type, name, initializer ?? new NumberNode(0));
            }
        }

        if (Current.Type == TokenType.Free)
        {
            Advance();
            Match(TokenType.OpenParenthesis, "Expected ( after free_memory");
            string varName = Match(TokenType.Identifier, "Expected a variable as a paramiter in free_memory").Value;
            Match(TokenType.CloseParenthesis, "Missing ) after the variable");
            Match(TokenType.Semicolon, "Missing ; at the end of the line");
            return new FreeNode(varName);
        }

        if (Current.Type == TokenType.Asterisk)
        {
            Advance();
            string name = Match(TokenType.Identifier, "Expected the name of the pointer").Value;
            Match(TokenType.Assign, "Expected = after the name of the pointer");
            AstNode value = ParseExpression();
            Match(TokenType.Semicolon, "Missing ; at the end of the line");
            return new DerefAssignNode(name, value);
        }

        if (Current.Type == TokenType.Print)
        {
            bool isMemory = Current.Value == "print_memory";
            Advance();
            Match(TokenType.OpenParenthesis, "Missing (");
            if (Current.Type == TokenType.StringLiteral)
            {
                string text = Advance().Value;
                Match(TokenType.CloseParenthesis, "Missing ) after the string message");
                Match(TokenType.Semicolon, "Missing ; at the end of the line");
                return new PrintStrNode(text);
            }
            else
            {
                AstNode expr = ParseExpression();
                
                Match(TokenType.CloseParenthesis, "Missing ) after print argument");
                Match(TokenType.Semicolon, "Missing ; at the end of the line");

                if (expr is IdentifierNode idNode)
                {
                    return new PrintNode(idNode.Name, isMemory);
                }
                
                if (isMemory)
                {
                    throw new Exception("print_memory expects an identifier");
                }
                
                return new PrintExprNode(expr);
            }
        }

        if (Current.Type == TokenType.If)
        {
            Advance();
            Match(TokenType.OpenParenthesis, "Missing ( after if");
            AstNode condition = ParseExpression();
            Match(TokenType.CloseParenthesis, "Missing ) before the body of the if");
            
            List<AstNode> body = new List<AstNode>();
            if (Current.Type == TokenType.OpenBrace)
            {
                Match(TokenType.OpenBrace, "Missing { to start the if body");
                while (Current.Type != TokenType.CloseBrace && Current.Type != TokenType.EOF)
                {
                    body.Add(ParseStatement());
                }
                Match(TokenType.CloseBrace, "Missing } to end the if body");
            }
            else
            {
                body.Add(ParseStatement());
            }

            List<AstNode>? elseBody = null;
            if (Current.Type == TokenType.Else)
            {
                Advance();
                if (Current.Type == TokenType.OpenBrace)
                {
                    Match(TokenType.OpenBrace, "Missing { to start the else body");
                    elseBody = new List<AstNode>();
                    while (Current.Type != TokenType.CloseBrace && Current.Type != TokenType.EOF)
                    {
                        elseBody.Add(ParseStatement());
                    }
                    Match(TokenType.CloseBrace, "Missing } to end the else body");
                }
                else
                {
                    elseBody = new List<AstNode>();
                    elseBody.Add(ParseStatement());
                }
            }
            return new IfStatementNode(condition, body, elseBody);
        }
        
        if (Current.Type == TokenType.Return)
        {
            Advance();
            AstNode? value = null;
            if (Current.Type != TokenType.Semicolon)
            {
                value = ParseExpression();
            }
            Match(TokenType.Semicolon, "Missing ; after return");
            return new ReturnNode(value);
        }

        if (Current.Type == TokenType.Identifier)
        {
            int startPos = _position;
            string name = Advance().Value;
            
            AstNode targetNode = new IdentifierNode(name);
            while (Current.Type == TokenType.Arrow)
            {
                Advance();
                string fieldName = Match(TokenType.Identifier, "Expected field name after ->").Value;
                targetNode = new StructAccessNode(targetNode, fieldName);
            }

            if (Current.Type == TokenType.OpenParenthesis && targetNode is IdentifierNode)
            {
                Match(TokenType.OpenParenthesis, "Missing (");
                var arguments = new List<AstNode>();
                if (Current.Type != TokenType.CloseParenthesis)
                {
                    arguments.Add(ParseExpression());
                    while (Current.Type == TokenType.Comma)
                    {
                        Advance();
                        arguments.Add(ParseExpression());
                    }
                }
                Match(TokenType.CloseParenthesis, "Missing ) after arguments");
                Match(TokenType.Semicolon, "Missing ; after function call");
                return new FunctionCallNode(name, arguments);
            }
            
            if (Current.Type == TokenType.Assign)
            {
                Advance();
                AstNode expression = ParseExpression();
                Match(TokenType.Semicolon, "Missing ; after assignment");
                
                if (targetNode is IdentifierNode id)
                {
                    return new AssignmentNode(id.Name, expression);
                }
                else if (targetNode is StructAccessNode sa)
                {
                    return new StructAssignNode(sa.StructPtr, sa.FieldName, expression);
                }
            }

            if (Current.Type == TokenType.OpenBracket && targetNode is IdentifierNode)
            {
                Advance();
                AstNode index = ParseExpression();
                Match(TokenType.CloseBracket, "Missing ] after array index");
                Match(TokenType.Assign, "Expected = after array index");
                AstNode value = ParseExpression();
                Match(TokenType.Semicolon, "Missing ; after array assignment");
                return new ArrayAssignNode(name, index, value);
            }
            
            _position = startPos; 
        }

        if (Current.Type == TokenType.Exit)
        {
            Advance();
            Match(TokenType.OpenParenthesis, "Missing ( before the parameter of exit");
            AstNode exitCode = ParseExpression();
            Match(TokenType.CloseParenthesis, "Missing ) after exit parameter");
            Match(TokenType.Semicolon, "Missing ; after exit");
            return new ExitNode(exitCode);
        }

        if (Current.Type == TokenType.Rand)
        {
            Advance();
            Match(TokenType.OpenParenthesis, "Missing ( after rand");
            Match(TokenType.CloseParenthesis, "Missing ) after rand()");
            Match(TokenType.Semicolon, "Missing ; after rand()");
            return new RandNode();
        }

        if (Current.Type == TokenType.Srand)
        {
            Advance();
            Match(TokenType.OpenParenthesis, "Missing ( after srand");
            AstNode seed = ParseExpression();
            Match(TokenType.CloseParenthesis, "Missing ) after srand()");
            Match(TokenType.Semicolon, "Missing ; after srand()");
            return new SrandNode(seed);
        }

        if (Current.Type == TokenType.Sleep)
        {
            Advance();
            Match(TokenType.OpenParenthesis, "Missing ( after sleep");
            AstNode duration = ParseExpression();
            Match(TokenType.CloseParenthesis, "Missing ) after sleep()");
            Match(TokenType.Semicolon, "Missing ; after sleep()");
            return new SleepNode(duration);
        }

        if (Current.Type == TokenType.While)
        {
            Advance();
            Match(TokenType.OpenParenthesis, "Missing ( before the condition");
            AstNode condition = ParseExpression();
            Match(TokenType.CloseParenthesis, "Missing ) after the condition");
            Match(TokenType.OpenBrace, "Missing { to start the while body");
            var body = new List<AstNode>();
            while (Current.Type != TokenType.CloseBrace && Current.Type != TokenType.EOF)
            {
                body.Add(ParseStatement());
            }
            Match(TokenType.CloseBrace, "Missing } to end the while body");
            return new WhileNode(condition, body);
        }

        if (Current.Type == TokenType.Do)
        {
            Advance();
            Match(TokenType.OpenBrace, "Missing { to start the do-while body");
            var body = new List<AstNode>();
            while (Current.Type != TokenType.CloseBrace && Current.Type != TokenType.EOF)
            {
                body.Add(ParseStatement());
            }
            Match(TokenType.CloseBrace, "Missing } to end the do-while body");
            Match(TokenType.While, "Expected while after do body");
            Match(TokenType.OpenParenthesis, "Missing ( before the condition");
            AstNode condition = ParseExpression();
            Match(TokenType.CloseParenthesis, "Missing ) after the condition");
            Match(TokenType.Semicolon, "Missing ; after do-while statement");
            return new DoWhileNode(condition, body);
        }

        if (Current.Type == TokenType.For)
        {
            Advance();
            Match(TokenType.OpenParenthesis, "Missing ( after for");
            
            AstNode? initialization = null;
            if (Current.Type != TokenType.Semicolon)
            {
                // In C, for init can be a declaration or an expression.
                // Our ParseStatement handles declarations and assignments, but it consumes the semicolon.
                // We need something slightly different here or adjust ParseStatement.
                // Let's try to parse a statement but without the semicolon requirement if possible, 
                // OR just use ParseStatement and handle it.
                // However, ParseStatement expects a semicolon at the end for most things.
                
                // Let's check if it's a declaration.
                if (IsTypeStartToken(Current.Type))
                {
                    // This is tricky because ParseStatement will consume the semicolon.
                    initialization = ParseStatement();
                }
                else
                {
                    // It might be an assignment or expression.
                    initialization = ParseStatement();
                }
            }
            else
            {
                Advance(); // Skip semicolon
            }

            AstNode? condition = null;
            if (Current.Type != TokenType.Semicolon)
            {
                condition = ParseExpression();
            }
            Match(TokenType.Semicolon, "Missing ; after for condition");

            AstNode? iteration = null;
            if (Current.Type != TokenType.CloseParenthesis)
            {
                // Iteration is usually an assignment or function call, without semicolon.
                // We need to be careful here. 
                // Let's implement a small ParseForIteration to avoid semicolon issue.
                iteration = ParseForIteration();
            }
            Match(TokenType.CloseParenthesis, "Missing ) after for iteration");
            
            Match(TokenType.OpenBrace, "Missing { to start the for body");
            var body = new List<AstNode>();
            while (Current.Type != TokenType.CloseBrace && Current.Type != TokenType.EOF)
            {
                body.Add(ParseStatement());
            }
            Match(TokenType.CloseBrace, "Missing } to end the for body");
            
            return new ForNode(initialization, condition, iteration, body);
        }

        if (Current.Type == TokenType.Struct)
        {
            Advance();
            string name = Match(TokenType.Identifier, "Expected struct name").Value;
            Match(TokenType.OpenBrace, "Missing { to start the struct body");
            var fields = new List<VarDeclNode>();
            while (Current.Type != TokenType.CloseBrace && Current.Type != TokenType.EOF)
            {
                var stmt = ParseStatement();
                if (stmt is VarDeclNode varDecl)
                {
                    fields.Add(varDecl);
                }
                else if (stmt is DoubleSlashNode)
                {
                    // Ignore comments inside struct
                }
                else
                {
                    throw new Exception("Only variable declarations are allowed inside a struct");
                }
            }
            Match(TokenType.CloseBrace, "Missing } to end the struct body");
            Match(TokenType.Semicolon, "Missing ; after struct declaration");
            return new StructNode(name, fields);
        }
        
        throw new Exception($"Unrecognized instruction: {Current.Value}");
    }

    private AstNode ParseForIteration()
    {
        // Similar to ParseStatement but without Semicolon
        if (Current.Type == TokenType.Identifier)
        {
            string name = Advance().Value;
            if (Current.Type == TokenType.OpenParenthesis)
            {
                Match(TokenType.OpenParenthesis, "Missing (");
                var arguments = new List<AstNode>();
                if (Current.Type != TokenType.CloseParenthesis)
                {
                    arguments.Add(ParseExpression());
                    while (Current.Type == TokenType.Comma)
                    {
                        Advance();
                        arguments.Add(ParseExpression());
                    }
                }
                Match(TokenType.CloseParenthesis, "Missing ) after arguments");
                return new FunctionCallNode(name, arguments);
            }
            
            if (Current.Type == TokenType.Assign)
            {
                Advance();
                AstNode expression = ParseExpression();
                return new AssignmentNode(name, expression);
            }

            if (Current.Type == TokenType.OpenBracket)
            {
                Advance();
                AstNode index = ParseExpression();
                Match(TokenType.CloseBracket, "Missing ] after array index");
                Match(TokenType.Assign, "Expected = after array index");
                AstNode value = ParseExpression();
                return new ArrayAssignNode(name, index, value);
            }
            _position--;
        }
        
        return ParseExpression();
    }

    private FunctionDeclNode ParseFunction()
    {
        string returnType = ParseType();
        string name = Match(TokenType.Identifier, "Expected function name").Value;
        Match(TokenType.OpenParenthesis, "Expected ( after function name");
        var parameters = new List<ParameterNode>();
        if (Current.Type != TokenType.CloseParenthesis)
        {
            string pType = ParseType();
            string pName = Match(TokenType.Identifier, "Expected parameter name").Value;
            parameters.Add(new ParameterNode(pType, pName));
            while (Current.Type == TokenType.Comma)
            {
                Advance();
                string nextPType = ParseType();
                string nextPName = Match(TokenType.Identifier, "Expected parameter name").Value;
                parameters.Add(new ParameterNode(nextPType, nextPName));
            }
        }
        Match(TokenType.CloseParenthesis, "Expected ) after parameters");
        Match(TokenType.OpenBrace, "Expected { to start function body");
        var body = new List<AstNode>();
        while (Current.Type != TokenType.CloseBrace && Current.Type != TokenType.EOF)
        {
            body.Add(ParseStatement());
        }
        Match(TokenType.CloseBrace, "Expected } to end function body");
        return new FunctionDeclNode(returnType, name, parameters, body);
    }

    public ProgramNode Parse()
    {
        var program = new ProgramNode();
        while (Current.Type != TokenType.EOF)
        {
            if (Current.Type == TokenType.SlashSlash)
            {
                var comment = ParseComment();
                program.Comments.Add(comment);
                program.Statements.Add(comment);
            }
            else if (Current.Type == TokenType.Import)
            {
                Advance();
                string importPath = Match(TokenType.StringLiteral, "Expected string literal after import").Value;
                Match(TokenType.Semicolon, "Expected ; after import path");

                string fullPath;
                if (!string.IsNullOrEmpty(_currentFilePath))
                {
                    string directory = Path.GetDirectoryName(_currentFilePath) ?? "";
                    fullPath = Path.GetFullPath(Path.Combine(directory, importPath));
                }
                else
                {
                    fullPath = Path.GetFullPath(importPath);
                }

                if (File.Exists(fullPath))
                {
                    if (!_importedFiles.Contains(fullPath))
                    {
                        _importedFiles.Add(fullPath);
                        string importedCode = File.ReadAllText(fullPath);
                        Lexer importedLexer = new Lexer(importedCode);
                        List<Token> importedTokens = importedLexer.Tokenize();
                        Parser importedParser = new Parser(importedTokens, fullPath);
                        ProgramNode importedAst = importedParser.Parse();

                        program.Functions.AddRange(importedAst.Functions);
                        program.Statements.AddRange(importedAst.Statements);
                        program.Comments.AddRange(importedAst.Comments);
                    }
                }
                else
                {
                    throw new Exception($"Imported file not found: {fullPath}");
                }
            }
            else if (IsFunctionDeclaration())
            {
                program.Functions.Add(ParseFunction());
            }
            else
            {
                program.Statements.Add(ParseStatement());
            }
        }
        return program;
    }

    private bool SkipType()
    {
        int p = _position;
        while (p < _tokens.Count && _tokens[p].Type == TokenType.Asterisk) p++;
        if (p < _tokens.Count && IsTypeToken(_tokens[p].Type))
        {
            return true;
        }
        return false;
    }

    private bool IsVariableDeclaration()
    {
        int pos = _position;
        
        while (pos < _tokens.Count && _tokens[pos].Type == TokenType.Asterisk) pos++;
        if (pos < _tokens.Count && IsTypeToken(_tokens[pos].Type))
        {
            pos++;
            
            // Case 1: Type name = ...
            if (pos < _tokens.Count && _tokens[pos].Type == TokenType.Identifier && 
                pos + 1 < _tokens.Count && _tokens[pos + 1].Type == TokenType.Assign)
            {
                return true;
            }

            // Case 2: Type name; (Declaration without initializer)
            if (pos < _tokens.Count && _tokens[pos].Type == TokenType.Identifier && 
                pos + 1 < _tokens.Count && _tokens[pos + 1].Type == TokenType.Semicolon)
            {
                return true;
            }

            // Case 3: Type name[size] = ...
            if (pos < _tokens.Count && _tokens[pos].Type == TokenType.Identifier && 
                pos + 1 < _tokens.Count && _tokens[pos + 1].Type == TokenType.OpenBracket)
            {
                int tempPos = pos + 2;
                // Skip expression inside brackets
                int bracketCount = 1;
                while (tempPos < _tokens.Count && bracketCount > 0)
                {
                    if (_tokens[tempPos].Type == TokenType.OpenBracket) bracketCount++;
                    else if (_tokens[tempPos].Type == TokenType.CloseBracket) bracketCount--;
                    tempPos++;
                }
                
                if (tempPos < _tokens.Count && (_tokens[tempPos].Type == TokenType.Assign || _tokens[tempPos].Type == TokenType.Semicolon))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsFunctionDeclaration()
    {
        int pos = _position;
        
        while (pos < _tokens.Count && _tokens[pos].Type == TokenType.Asterisk) pos++;
        if (pos < _tokens.Count && IsTypeToken(_tokens[pos].Type))
        {
            pos++;
            if (pos < _tokens.Count && _tokens[pos].Type == TokenType.Identifier && 
                pos + 1 < _tokens.Count && _tokens[pos + 1].Type == TokenType.OpenParenthesis)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsTypeStartToken(TokenType type)
    {
        return type == TokenType.Asterisk || IsTypeToken(type);
    }

    private bool IsTypeToken(TokenType type)
    {
        return type == TokenType.TypeInt || type == TokenType.TypePtr || type == TokenType.TypeChar || type == TokenType.TypeVoid || type == TokenType.TypeBool || type == TokenType.Identifier;
    }

    private Token LookAhead(int distance)
    {
        int pos = _position + distance;
        if (pos >= _tokens.Count) return _tokens[^1];
        return _tokens[pos];
    }
}