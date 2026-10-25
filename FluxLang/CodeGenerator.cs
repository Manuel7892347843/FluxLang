using System;
using System.Collections.Generic;
using System.Text;

namespace FluxLang;

public class CodeGenerator
{
    private readonly ProgramNode _program;
    private StringBuilder _asm = new StringBuilder();
    private readonly StringBuilder _asmData = new StringBuilder(); // Dynamic data generator for custom print strings
    private readonly Dictionary<string, int> _stackMap = new Dictionary<string, int>();
    private readonly Dictionary<string, string> _typeMap = new Dictionary<string, string>();
    private int _stackOffset = 0;
    private int _msgCount = 0; // Counter to generate unique labels for print strings
    private int _ifCount = 0;
    private string? _currentFunctionName = null;
    private static readonly string[] ParameterRegisters = { "rdi", "rsi", "rdx", "rcx", "r8", "r9" };
    private int _whileCount = 0;
    private int _doWhileCount = 0;
    private int _forCount = 0;

    public CodeGenerator(ProgramNode program)
    {
        _program = program;
    }

    public string Generate()
    {
        return RealGenerate();
    }

    private readonly StringBuilder _asmFunctions = new StringBuilder();

    private string RealGenerate()
    {
        // 1. Reset per la scoperta
        _asm.Clear();
        _asmData.Clear();
        _asmFunctions.Clear();
        _msgCount = 0;
        _ifCount = 0;
        _stackMap.Clear();
        _typeMap.Clear();
        _stackOffset = 0;
        _whileCount = 0;
        _doWhileCount = 0;
        _forCount = 0;

        // 2. Genera funzioni
        bool hasMain = false;
        foreach (var func in _program.Functions)
        {
            var funcDecl = (FunctionDeclNode)func;
            if (funcDecl.Name == "main") hasMain = true;
            GenerateFunction(funcDecl);
        }

        // 3. Genera corpo principale (se presenti istruzioni top-level)
        if (_program.Statements.Count > 0)
        {
            foreach (var statement in _program.Statements)
            {
                GenerateStatement(statement);
            }
        }

        var finalAsm = new StringBuilder();
        
        finalAsm.AppendLine("global _main");
        finalAsm.AppendLine("extern _malloc");
        finalAsm.AppendLine("extern _free");
        finalAsm.AppendLine("extern _printf");
        finalAsm.AppendLine("extern _scanf");
        finalAsm.AppendLine("extern _exit");
        finalAsm.AppendLine("extern _sqrt");
        finalAsm.AppendLine("extern _time");
        finalAsm.AppendLine("extern _usleep");
        finalAsm.AppendLine();
        
        finalAsm.AppendLine("section .data");
        finalAsm.AppendLine("   align 16");
        finalAsm.AppendLine("   format_num db \"%lld\", 10, 0");
        finalAsm.AppendLine("   format_num_input db \"%lld\", 0");
        finalAsm.AppendLine("   format_char db \"%c\", 10, 0");
        finalAsm.AppendLine("   format_str db \"%s\", 10, 0");
        finalAsm.Append(_asmData.ToString()); 
        finalAsm.AppendLine();
        
        finalAsm.AppendLine("section .text");
        finalAsm.AppendLine("   align 16");
        
        finalAsm.Append(_asmFunctions.ToString());
        finalAsm.AppendLine();

        if (!hasMain)
        {
            finalAsm.AppendLine("_main:");
            finalAsm.AppendLine("   push rbp");
            finalAsm.AppendLine("   mov rbp, rsp");
            finalAsm.AppendLine("   sub rsp, 128");
            finalAsm.AppendLine();

            finalAsm.Append(_asm.ToString()); 

            finalAsm.AppendLine();
            finalAsm.AppendLine(".exit_main:");
            finalAsm.AppendLine("   mov rax, 0");
            finalAsm.AppendLine("   leave");
            finalAsm.AppendLine("   ret");
        }
        
        return finalAsm.ToString();
    }

    private void GenerateFunction(FunctionDeclNode func)
    {
        _currentFunctionName = func.Name;
        _stackMap.Clear();
        _typeMap.Clear();
        _stackOffset = 0;

        string asmName = func.Name == "main" ? "_main" : $"_{func.Name}";

        _asmFunctions.AppendLine($"{asmName}:");
        _asmFunctions.AppendLine("   push rbp");
        _asmFunctions.AppendLine("   mov rbp, rsp");
        _asmFunctions.AppendLine("   sub rsp, 128    ; Reserve more stack space");

        for (int i = 0; i < func.Parameters.Count; i++)
        {
            var param = func.Parameters[i];
            _stackOffset -= 8;
            _stackMap[param.Name] = _stackOffset;
            _typeMap[param.Name] = param.Type;
            _asmFunctions.AppendLine($"   mov [rbp{_stackOffset}], {ParameterRegisters[i]}  ; Store parameter {param.Name}");
        }

        var oldAsm = _asm;
        // Questo è un po' un trucco, ma usiamo un StringBuilder locale per il corpo della funzione
        StringBuilder funcBody = new StringBuilder();
        // Reindirizza _asm a funcBody
        _asm = funcBody;

        foreach (var stmt in func.Body)
        {
            GenerateStatement(stmt);
        }

        _asm = oldAsm;
        _asmFunctions.Append(funcBody.ToString());

        _asmFunctions.AppendLine($"   .exit_{func.Name}:");
        if (func.Name == "main")
        {
            _asmFunctions.AppendLine("   mov rax, 0");
        }
        _asmFunctions.AppendLine("   leave");
        _asmFunctions.AppendLine("   ret");
        _asmFunctions.AppendLine();
        
        _currentFunctionName = null;
    }

    private void GenerateStatement(AstNode node)
    {
        if (node is DoubleSlashNode doubSlashNode)
        {
            GenerateComment(doubSlashNode);
        }
        else if (node is VarDeclNode varDecl)
        {
            GenerateExpression(varDecl.Initializer);
            _stackOffset -= 8;
            _stackMap[varDecl.Name] = _stackOffset;
            _typeMap[varDecl.Name] = varDecl.Type;
            _asm.AppendLine($"   mov [rbp{_stackOffset}], rax   ; Save the variable '{varDecl.Name}' of type {varDecl.Type}");
        }
        else if (node is AssignmentNode assignNode)
        {
            GenerateExpression(assignNode.Expression);
            if (_stackMap.TryGetValue(assignNode.Name, out int offset))
            {
                if (assignNode.Expression is StringLiteralNode && _typeMap[assignNode.Name] == "char")
                {
                    _typeMap[assignNode.Name] = "*char";
                }
                
                _asm.AppendLine($"   mov [rbp{offset}], rax   ; Update variable '{assignNode.Name}'");
            }
            else
            {
                throw new Exception($"Variable '{assignNode.Name}' not defined");
            }
        }
        else if (node is FreeNode freeNode)
        {
            int offset = _stackMap[freeNode.VarName];
            _asm.AppendLine($"   mov rdi, [rbp{offset}]  ; Recovers the address to free");
            _asm.AppendLine("   call _free  ; Frees the memory");
        }
        else if (node is DerefAssignNode derefNode)
        {
            GenerateExpression(derefNode.Value);
            _asm.AppendLine("   push rax    ; Save value to deref-assign");
            
            int offset = _stackMap[derefNode.PointerName];
            string type = _typeMap.ContainsKey(derefNode.PointerName) ? _typeMap[derefNode.PointerName] : "ptr";
            int elementSize = type.Contains("*char") || type == "char" ? 1 : 8;

            _asm.AppendLine($"   mov rdi, [rbp{offset}]    ; Recovers the pointer {derefNode.PointerName} address");
            _asm.AppendLine("   pop rax     ; Restore value to write");
            
            if (elementSize == 1)
            {
                _asm.AppendLine("   mov [rdi], al      ; Writes the byte value inside the memory cell");
            }
            else
            {
                _asm.AppendLine("   mov [rdi], rax      ; Writes the real value inside the memory cell of the heap");
            }
        }
        else if (node is PrintNode printNode)
        {
            if (!_stackMap.TryGetValue(printNode.VarName, out int offset))
            {
                throw new Exception($"Variable '{printNode.VarName}' not defined");
            }
            string type = _typeMap.ContainsKey(printNode.VarName) ? _typeMap[printNode.VarName] : "int";

            if (printNode.IsMemory)
            {
                _asm.AppendLine($"   mov rdi, [rbp{offset}]    ; Recovers the address of {printNode.VarName}");
                
                if (type.Contains("*char") || type == "char")
                {
                    _asm.AppendLine("   movzx rsi, byte [rdi] ; Reads the real byte value from heap");
                    _asm.AppendLine("   lea rdi, [rel format_char]");
                }
                else
                {
                    _asm.AppendLine("   mov rsi, [rdi]      ; Reads the real value from heap");
                    _asm.AppendLine("   lea rdi, [rel format_num]");
                }
            }
            else
            {
                if (type == "char")
                {
                    _asm.AppendLine($"   mov rsi, [rbp{offset}]    ; Load char value to RSI");
                    _asm.AppendLine("   lea rdi, [rel format_char]");
                }
                else if (type.Contains("*char"))
                {
                    _asm.AppendLine("   lea rdi, [rel format_str]");
                    _asm.AppendLine($"   mov rsi, [rbp{offset}]    ; Load string pointer to RSI");
                }
                else 
                {
                    _asm.AppendLine($"   mov rsi, [rbp{offset}]    ; Load int value to RSI");
                    _asm.AppendLine("   lea rdi, [rel format_num]");
                }
            }
            
            _asm.AppendLine("   mov al, 0");
            _asm.AppendLine("   call _printf    ; Calls the system printf");
        }
        else if (node is PrintStrNode strNode)
        {
            _msgCount++;
            string msgLabel = $"msg_{_msgCount}";
            _asmData.AppendLine($"   {msgLabel} db \"{strNode.Text}\", 0");
            _asm.AppendLine("   lea rdi, [rel format_str]");
            _asm.AppendLine($"   lea rsi, [rel {msgLabel}]");
            _asm.AppendLine("   mov al, 0");
            _asm.AppendLine("   call _printf");
        }
        else if (node is PrintExprNode printExpr)
        {
            GenerateExpression(printExpr.Expression);
            _asm.AppendLine("   mov rsi, rax");
            
            // Try to determine the type if it's a simple array access or variable
            string format = "format_num";
            if (printExpr.Expression is ArrayAccessNode aa)
            {
                if (_typeMap.TryGetValue(aa.ArrayName, out string? t) && (t.Contains("*char") || t == "char"))
                    format = "format_char";
            }
            else if (printExpr.Expression is DerefExpressionNode derefExpr)
            {
                if (_typeMap.TryGetValue(derefExpr.PointerName, out string? t) && (t.Contains("*char") || t == "char"))
                    format = "format_char";
            }
            else if (printExpr.Expression is IdentifierNode id)
            {
                if (_typeMap.TryGetValue(id.Name, out string? t))
                {
                    if (t == "char") format = "format_char";
                    else if (t.Contains("*char")) format = "format_str";
                }
            }
            else if (printExpr.Expression is StringLiteralNode)
            {
                format = "format_str";
            }

            _asm.AppendLine($"   lea rdi, [rel {format}]");
            _asm.AppendLine("   mov al, 0");
            _asm.AppendLine("   call _printf");
        }
        else if (node is IfStatementNode ifNode)
        {
            _ifCount++;

            string labelElse = $".if_else_{_ifCount}";
            string labelEnd = $".if_end_{_ifCount}";

            GenerateCondition(ifNode.Condition, labelElse, true);

            foreach (var bodyStatement in ifNode.Body)
            {
                GenerateStatement(bodyStatement);
            }

            _asm.AppendLine($"   jmp {labelEnd}    ; Skip else block after true branch");

            _asm.AppendLine($"{labelElse}:        ; Else block");

            if (ifNode.ElseBody != null)
            {
                foreach (var elseStatement in ifNode.ElseBody)
                {
                    GenerateStatement(elseStatement);
                }
            }

            _asm.AppendLine($"{labelEnd}:        ; End of the if/else block");
        }
        else if (node is ReturnNode returnNode)
        {
            if (returnNode.Value != null)
            {
                GenerateExpression(returnNode.Value);
            }
            if (_currentFunctionName != null)
            {
                _asm.AppendLine($"   jmp .exit_{_currentFunctionName}");
            }
            else
            {
                _asm.AppendLine("   jmp .exit_main");
            }
        }
        else if (node is FunctionCallNode callNode)
        {
            for (int i = 0; i < callNode.Arguments.Count; i++)
            {
                GenerateExpression(callNode.Arguments[i]);
                _asm.AppendLine($"   mov {ParameterRegisters[i]}, rax");
            }
            _asm.AppendLine($"   call _{callNode.FunctionName}");
        }
        else if (node is ArrayAssignNode arrayAssign)
        {
            if (_stackMap.TryGetValue(arrayAssign.ArrayName, out int offset))
            {
                string type = _typeMap.ContainsKey(arrayAssign.ArrayName) ? _typeMap[arrayAssign.ArrayName] : "ptr";
                int elementSize = type.Contains("*char") || type == "char" ? 1 : 8;

                GenerateExpression(arrayAssign.Index);
                _asm.AppendLine("   push rax        ; Save index");
                GenerateExpression(arrayAssign.Value);
                _asm.AppendLine("   mov rcx, rax    ; Value to write in RCX");
                _asm.AppendLine("   pop rax         ; Restore index in RAX");
                
                _asm.AppendLine($"   mov rdx, [rbp{offset}]  ; Load array base address");
                
                if (elementSize == 8)
                {
                    _asm.AppendLine("   shl rax, 3      ; Index * 8");
                }
                else if (elementSize == 4)
                {
                    _asm.AppendLine("   shl rax, 2      ; Index * 4");
                }

                _asm.AppendLine("   add rdx, rax    ; Address = base + index * size");
                
                if (elementSize == 1)
                {
                    _asm.AppendLine("   mov [rdx], cl   ; Store byte value from RCX");
                }
                else
                {
                    _asm.AppendLine("   mov [rdx], rcx  ; Store value from RCX");
                }
                _asm.AppendLine("   mov rax, rcx     ; Array assignment expression value is the assigned value");
            }
            else
            {
                throw new Exception($"Array '{arrayAssign.ArrayName}' not defined");
            }
        }
        else if (node is ExitNode exitNode)
        {
            GenerateExpression(exitNode.ExitCode);
            _asm.AppendLine("   mov rdi, rax");
            _asm.AppendLine("   call _exit");
        }
        else if (node is WhileNode whileNode)
        {
            _whileCount++;
            string labelStart = $".while_start_{_whileCount}";
            string labelEnd = $".while_end_{_whileCount}";

            _asm.AppendLine($"{labelStart}:");

            GenerateCondition(whileNode.Condition, labelEnd, true);

            foreach (var bodyStatement in whileNode.Body)
            {
                GenerateStatement(bodyStatement);
            }

            _asm.AppendLine($"   jmp {labelStart}   ; Jump back to the start of the while loop");
            _asm.AppendLine($"{labelEnd}:        ; End of the while loop");
        }
        else if (node is DoWhileNode doWhileNode)
        {
            _doWhileCount++;
            string labelStart = $".do_while_start_{_doWhileCount}";
            string labelEnd = $".do_while_end_{_doWhileCount}";

            _asm.AppendLine($"{labelStart}:");

            foreach (var bodyStatement in doWhileNode.Body)
            {
                GenerateStatement(bodyStatement);
            }

            GenerateCondition(doWhileNode.Condition, labelEnd, true);
            _asm.AppendLine($"   jmp {labelStart}   ; Jump back to the start of the do-while loop");
            _asm.AppendLine($"{labelEnd}:        ; End of the do-while loop");
        }
        else if (node is ForNode forNode)
        {
            _forCount++;
            string labelStart = $".for_start_{_forCount}";
            string labelEnd = $".for_end_{_forCount}";
            string labelIter = $".for_iter_{_forCount}";

            // 1. Initialization
            if (forNode.Initialization != null)
            {
                GenerateStatement(forNode.Initialization);
            }

            // 2. Loop start label
            _asm.AppendLine($"{labelStart}:");

            // 3. Condition
            if (forNode.Condition != null)
            {
                GenerateCondition(forNode.Condition, labelEnd, true);
            }

            // 4. Body
            foreach (var bodyStatement in forNode.Body)
            {
                GenerateStatement(bodyStatement);
            }

            // 5. Iteration label and iteration expression
            _asm.AppendLine($"{labelIter}:");
            if (forNode.Iteration != null)
            {
                // Iteration is an expression, but we use it as a statement here.
                if (forNode.Iteration is AssignmentNode || forNode.Iteration is ArrayAssignNode || forNode.Iteration is FunctionCallNode)
                {
                    GenerateStatement(forNode.Iteration);
                }
                else
                {
                    GenerateExpression(forNode.Iteration);
                }
            }

            // 6. Jump back to condition
            _asm.AppendLine($"   jmp {labelStart}");
            _asm.AppendLine($"{labelEnd}:");
        }
    }

    private void GenerateExpression(AstNode node)
    {
        if (node is InputNode)
        {
            _asm.AppendLine("   sub rsp, 16     ; Space for input value and alignment");
            _asm.AppendLine("   mov qword [rsp], 0 ; Clear the memory");
            _asm.AppendLine("   lea rsi, [rsp]  ; Pass the address to RSI");
            _asm.AppendLine("   lea rdi, [rel format_num_input]");
            _asm.AppendLine("   mov al, 0");
            _asm.AppendLine("   call _scanf");
            _asm.AppendLine("   mov rax, [rsp]  ; Load the input value to RAX");
            _asm.AppendLine("   add rsp, 16     ; Restore stack pointer");
        }
        else if (node is StringLiteralNode strLiteral)
        {
            _msgCount++;
            string msgLabel = $"msg_{_msgCount}";
            _asmData.AppendLine($"   {msgLabel} db \"{strLiteral.Value}\", 0");
            _asm.AppendLine($"   lea rax, [rel {msgLabel}]");
        }
        else if (node is SqrtNode sqrtNode)
        {
            GenerateExpression(sqrtNode.Value);
            _asm.AppendLine("   cvtsi2sd xmm0, rax    ; Convert integer to double");
            _asm.AppendLine("   call _sqrt           ; Call math sqrt(double)");
            _asm.AppendLine("   cvttsd2si rax, xmm0   ; Convert double back to integer (truncate)");
        }
        else if (node is TimeNode)
        {
            _asm.AppendLine("   mov rdi, 0           ; NULL pointer");
            _asm.AppendLine("   mov rax, 0");
            _asm.AppendLine("   call _time           ; Get current time");
        }
        else if (node is SleepNode sleepNode)
        {
            GenerateExpression(sleepNode.Duration);
            _asm.AppendLine("   imul rax, 1000       ; Convert milliseconds to microseconds");
            _asm.AppendLine("   mov rdi, rax         ; usleep takes microseconds");
            _asm.AppendLine("   mov rax, 0");
            _asm.AppendLine("   call _usleep");
            _asm.AppendLine("   mov rax, 0           ; sleep returns void-like 0");
        }
        else if (node is ArrayAccessNode arrayAccess)
        {
            if (_stackMap.TryGetValue(arrayAccess.ArrayName, out int offset))
            {
                string type = _typeMap.ContainsKey(arrayAccess.ArrayName) ? _typeMap[arrayAccess.ArrayName] : "ptr";
                int elementSize = type.Contains("*char") || type == "char" ? 1 : 8;

                GenerateExpression(arrayAccess.Index);
                _asm.AppendLine("   mov rcx, rax    ; Index to RCX");
                _asm.AppendLine($"   mov rdx, [rbp{offset}]  ; Load array base address");
                
                if (elementSize == 8)
                {
                    _asm.AppendLine("   shl rcx, 3      ; Index * 8");
                }
                else if (elementSize == 4)
                {
                    _asm.AppendLine("   shl rcx, 2      ; Index * 4");
                }
                
                _asm.AppendLine("   add rdx, rcx    ; Address = base + index * size");
                
                if (elementSize == 1)
                {
                    _asm.AppendLine("   movzx rax, byte [rdx] ; Load byte value");
                }
                else
                {
                    _asm.AppendLine("   mov rax, [rdx]  ; Load value from address");
                }
            }
            else
            {
                throw new Exception($"Array '{arrayAccess.ArrayName}' not defined");
            }
        }
        else if (node is AllocNode allocNode)
        {
            GenerateExpression(allocNode.Size);
            _asm.AppendLine("   mov rdi, rax    ; Gives the calculated size to the RDI");
            _asm.AppendLine("   call _malloc    ; Allocates the memory (the address goes in RAX)");
        }
        else if (node is NumberNode numNode)
        {
            _asm.AppendLine($"   mov rax, {numNode.Value}");
        }
        else if (node is FunctionCallNode callNode)
        {
            for (int i = 0; i < callNode.Arguments.Count; i++)
            {
                GenerateExpression(callNode.Arguments[i]);
                _asm.AppendLine($"   mov {ParameterRegisters[i]}, rax");
            }
            _asm.AppendLine($"   call _{callNode.FunctionName}");
        }
        else if (node is CharacterNode charNode)
        {
            _asm.AppendLine($"   mov rax, {(int)charNode.Value}");
        }
        else if (node is DerefExpressionNode derefExpressionNode)
        {
            if (_stackMap.TryGetValue(derefExpressionNode.PointerName, out int offset))
            {
                string type = _typeMap.ContainsKey(derefExpressionNode.PointerName) ? _typeMap[derefExpressionNode.PointerName] : "ptr";
                int elementSize = type.Contains("*char") || type == "char" ? 1 : 8;

                _asm.AppendLine($"   mov rax, [rbp{offset}]  ; Reads pointer '{derefExpressionNode.PointerName}'");
                if (elementSize == 1)
                {
                    _asm.AppendLine("   movzx rax, byte [rax]   ; Reads byte value pointed by the pointer");
                }
                else
                {
                    _asm.AppendLine("   mov rax, [rax]          ; Reads value pointed by the pointer");
                }
            }
            else
            {
                throw new Exception($"Variable '{derefExpressionNode.PointerName}' not defined");
            }
        }
        else if (node is IdentifierNode idNode)
        {
            if (_stackMap.TryGetValue(idNode.Name, out int offset))
            {
                _asm.AppendLine($"   mov rax, [rbp{offset}]  ; Reads the value of the variable '{idNode.Name}'");
            }
            else
            {
                throw new Exception($"Variable '{idNode.Name}' not defined");
            }
        }
        else if (node is BinaryOpNode opNode)
        {
            GenerateExpression(opNode.Left);
            _asm.AppendLine("   push rax    ; Push left side onto the stack");
            GenerateExpression(opNode.Right);
            _asm.AppendLine("   mov rcx, rax    ; Move right side to RCX");
            _asm.AppendLine("   pop rax       ; Pop left side back to RAX");
            
            // Pointer arithmetic handling
            string? leftType = GetExpressionType(opNode.Left);
            if (opNode.Operator == "+" || opNode.Operator == "-")
            {
                if (leftType != null && leftType.StartsWith("*"))
                {
                    string pointedType = leftType.Substring(1);
                    int size = (pointedType == "char" || pointedType.StartsWith("*char")) ? 1 : 8;
                    if (size > 1)
                    {
                        // Use a register for size to avoid issues with imul imm8/16/32
                        _asm.AppendLine($"   imul rcx, {size}  ; Scale offset for pointer arithmetic");
                    }
                }
            }

            switch (opNode.Operator)
            {
                case "+":
                    _asm.AppendLine("   add rax, rcx    ; RAX = RAX + RCX");
                    break;
                case "-":
                    _asm.AppendLine("   sub rax, rcx    ; RAX = RAX - RCX");
                    break;
                case "*":
                    _asm.AppendLine("   imul rax, rcx    ; RAX = RAX * RCX");
                    break;
                case "/":
                    _asm.AppendLine("   cqo     ; Sign-extend RAX into RDX (required for 64-bit division)");
                    _asm.AppendLine("   idiv rcx    ; RAX = RAX / RCX");
                    break;
                case "&&":
                    _asm.AppendLine("   cmp rax, 0");
                    _asm.AppendLine("   setne al");
                    _asm.AppendLine("   movzx rax, al");
                    _asm.AppendLine("   cmp rcx, 0");
                    _asm.AppendLine("   setne cl");
                    _asm.AppendLine("   movzx rcx, cl");
                    _asm.AppendLine("   and rax, rcx");
                    break;
                case "||":
                    _asm.AppendLine("   or rax, rcx");
                    _asm.AppendLine("   cmp rax, 0");
                    _asm.AppendLine("   setne al");
                    _asm.AppendLine("   movzx rax, al");
                    break;
            }
        }
        else if (node is UnaryOpNode unaryNode)
        {
            GenerateExpression(unaryNode.Right);
            if (unaryNode.Operator == "-")
            {
                _asm.AppendLine("   neg rax");
            }
            else if (unaryNode.Operator == "!")
            {
                _asm.AppendLine("   cmp rax, 0");
                _asm.AppendLine("   sete al");
                _asm.AppendLine("   movzx rax, al");
            }
        }
        else if (node is ComparisonNode compNode)
        {
            GenerateExpression(compNode.Left);
            _asm.AppendLine("   push rax");
            GenerateExpression(compNode.Right);
            _asm.AppendLine("   mov rcx, rax");
            _asm.AppendLine("   pop rax");
            _asm.AppendLine("   cmp rax, rcx");
            
            string setOp = compNode.Operator switch
            {
                "==" => "sete",
                "!=" => "setne",
                "<" => "setl",
                ">" => "setg",
                "<=" => "setle",
                ">=" => "setge",
                _ => throw new Exception($"Unknown comparison operator: {compNode.Operator}")
            };
            
            _asm.AppendLine($"   {setOp} al");
            _asm.AppendLine("   movzx rax, al");
        }
    }

    private void GenerateCondition(AstNode condition, string targetLabel, bool jumpIfFalse)
    {
        if (condition is ComparisonNode comp)
        {
            GenerateExpression(comp.Left);
            _asm.AppendLine("   push rax");
            GenerateExpression(comp.Right);
            _asm.AppendLine("   mov rcx, rax");
            _asm.AppendLine("   pop rax");
            _asm.AppendLine("   cmp rax, rcx");

            string jumpOp = comp.Operator switch
            {
                "==" => jumpIfFalse ? "jne" : "je",
                "!=" => jumpIfFalse ? "je" : "jne",
                "<" => jumpIfFalse ? "jge" : "jl",
                ">" => jumpIfFalse ? "jle" : "jg",
                "<=" => jumpIfFalse ? "jg" : "jle",
                ">=" => jumpIfFalse ? "jl" : "jge",
                _ => throw new Exception("Unknown operator")
            };
            _asm.AppendLine($"   {jumpOp} {targetLabel}");
        }
        else if (condition is BinaryOpNode bin && bin.Operator == "&&")
        {
            if (jumpIfFalse)
            {
                GenerateCondition(bin.Left, targetLabel, true);
                GenerateCondition(bin.Right, targetLabel, true);
            }
            else
            {
                string labelEnd = $".and_end_{Guid.NewGuid().ToString("N")}";
                GenerateCondition(bin.Left, labelEnd, true);
                GenerateCondition(bin.Right, targetLabel, false);
                _asm.AppendLine($"{labelEnd}:");
            }
        }
        else if (condition is BinaryOpNode binOr && binOr.Operator == "||")
        {
            if (jumpIfFalse)
            {
                string labelTrue = $".or_true_{Guid.NewGuid().ToString("N")}";
                GenerateCondition(binOr.Left, labelTrue, false);
                GenerateCondition(binOr.Right, targetLabel, true);
                _asm.AppendLine($"{labelTrue}:");
            }
            else
            {
                GenerateCondition(binOr.Left, targetLabel, false);
                GenerateCondition(binOr.Right, targetLabel, false);
            }
        }
        else if (condition is UnaryOpNode unary && unary.Operator == "!")
        {
            GenerateCondition(unary.Right, targetLabel, !jumpIfFalse);
        }
        else
        {
            GenerateExpression(condition);
            _asm.AppendLine("   cmp rax, 0");
            _asm.AppendLine(jumpIfFalse ? $"   je {targetLabel}" : $"   jne {targetLabel}");
        }
    }

    private void GenerateComment(AstNode node)
    {
        if(node is DoubleSlashNode comment)
        {
            _asm.AppendLine($"   ; {comment.Comment}");
        }
    }
    private string? GetExpressionType(AstNode node)
    {
        if (node is IdentifierNode id)
        {
            _typeMap.TryGetValue(id.Name, out string? type);
            return type;
        }
        if (node is ArrayAccessNode aa)
        {
            if (_typeMap.TryGetValue(aa.ArrayName, out string? type))
            {
                if (type.StartsWith("*")) return type.Substring(1);
            }
        }
        if (node is DerefExpressionNode deref)
        {
            if (_typeMap.TryGetValue(deref.PointerName, out string? type))
            {
                if (type.StartsWith("*")) return type.Substring(1);
            }
        }
        return null;
    }
}