using System;
using System.Collections.Generic;
using System.Text;

namespace FluxLang;

public class CodeGenerator
{
    private readonly ProgramNode _program;
    private StringBuilder _asm = new StringBuilder();
    private readonly StringBuilder _asmData = new StringBuilder();
    private readonly Dictionary<string, int> _stackMap = new Dictionary<string, int>();
    private readonly Dictionary<string, string> _typeMap = new Dictionary<string, string>();
    private int _stackOffset = 0;
    private int _msgCount = 0;
    private int _ifCount = 0;
    private string? _currentFunctionName = null;

    private string[] ParameterRegisters = { "rdi", "rsi", "rdx", "rcx", "r8", "r9" };

    private int _whileCount = 0;
    private int _doWhileCount = 0;
    private int _forCount = 0;

    private string _p = "";
    private bool _isWindows = false;

    private readonly Dictionary<string, StructNode> _structs = new Dictionary<string, StructNode>();

    public CodeGenerator(ProgramNode program)
    {
        _program = program;
    }

    public string Generate(string os)
    {
        string activeOs = string.IsNullOrEmpty(os) ? "win64" : os;
        _isWindows = activeOs.Contains("win");
        _p = _isWindows ? "" : "_";

        if (_isWindows)
        {
            ParameterRegisters = new string[] { "rcx", "rdx", "r8", "r9" };
        }
        else
        {
            ParameterRegisters = new string[] { "rdi", "rsi", "rdx", "rcx", "r8", "r9" };
        }

        return RealGenerate(os);
    }

    private readonly StringBuilder _asmFunctions = new StringBuilder();

    private string RealGenerate(string os)
    {
        _structs.Clear();
        foreach (var stmt in _program.Statements)
        {
            if (stmt is StructNode sn) _structs[sn.Name] = sn;
        }

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
        // _structs.Clear(); // Don't clear here, already cleared at the beginning of RealGenerate

        bool hasMain = false;
        foreach (var func in _program.Functions)
        {
            var funcDecl = (FunctionDeclNode)func;
            if (funcDecl.Name == "main") hasMain = true;
            GenerateFunction(funcDecl, _p);
        }

        if (_program.Statements.Count > 0)
        {
            foreach (var statement in _program.Statements)
            {
                GenerateStatement(statement, _p);
            }
        }

        var finalAsm = new StringBuilder();

        finalAsm.AppendLine($"global {_p}main");
        finalAsm.AppendLine($"extern {_p}malloc");
        finalAsm.AppendLine($"extern {_p}free");
        finalAsm.AppendLine($"extern {_p}printf");
        finalAsm.AppendLine($"extern {_p}scanf");
        finalAsm.AppendLine($"extern {_p}exit");
        finalAsm.AppendLine($"extern {_p}sqrt");
        finalAsm.AppendLine($"extern {_p}time");
        finalAsm.AppendLine($"extern {_p}usleep");
        finalAsm.AppendLine($"extern {_p}rand");
        finalAsm.AppendLine($"extern {_p}srand");
        finalAsm.AppendLine($"extern {_p}fgets");

        if (!_isWindows)
        {
            finalAsm.AppendLine("extern ___stdinp");
        }
        else
        {
            finalAsm.AppendLine("extern __acrt_iob_func");
        }
        finalAsm.AppendLine();

        finalAsm.AppendLine("section .data");
        finalAsm.AppendLine("   align 16");
        finalAsm.AppendLine("   format_num db \"%lld\", 0");
        finalAsm.AppendLine("   format_num_input db \"%lld\", 0");
        finalAsm.AppendLine("   format_char db \"%c\", 0");
        finalAsm.AppendLine("   format_str db \"%s\", 0");
        finalAsm.Append(_asmData.ToString());
        finalAsm.AppendLine();

        finalAsm.AppendLine("section .text");
        finalAsm.AppendLine("   align 16");

        finalAsm.Append(_asmFunctions.ToString());
        finalAsm.AppendLine();

        if (!hasMain)
        {
            finalAsm.AppendLine($"{_p}main:");
            finalAsm.AppendLine("   push rbp");
            finalAsm.AppendLine("   mov rbp, rsp");
            finalAsm.AppendLine("   sub rsp, 144 ; Riserva spazio locale multi-16 + 32 byte obbligatori di Shadow Space per Windows");
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

    private string EscapeString(string input)
    {
        return input.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\", 10, \"")
                    .Replace("\r", "\", 13, \"")
                    .Replace("\t", "\", 9, \"");
    }

    private void GenerateFunction(FunctionDeclNode func, string p)
    {
        _currentFunctionName = func.Name;
        _stackMap.Clear();
        _typeMap.Clear();
        _stackOffset = 0;

        string asmName = $"{p}{func.Name}";

        _asmFunctions.AppendLine($"{asmName}:");
        _asmFunctions.AppendLine("   push rbp");
        _asmFunctions.AppendLine("   mov rbp, rsp");
        _asmFunctions.AppendLine("   sub rsp, 144    ; Alloca stack sufficiente includendo lo Shadow Space");

        for (int i = 0; i < func.Parameters.Count; i++)
        {
            var param = func.Parameters[i];
            _stackOffset -= 8;
            _stackMap[param.Name] = _stackOffset;
            
            string type = param.Type;
            if (_structs.ContainsKey(type))
            {
                type = "*" + type;
            }
            
            _typeMap[param.Name] = type;
            _asmFunctions.AppendLine($"   mov [rbp{_stackOffset}], {ParameterRegisters[i]}  ; Store parameter {param.Name} (type {type})");
        }

        var oldAsm = _asm;
        StringBuilder funcBody = new StringBuilder();
        _asm = funcBody;

        foreach (var stmt in func.Body)
        {
            GenerateStatement(stmt, p);
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

    private void GenerateStatement(AstNode node, string p)
    {
        if (node is DoubleSlashNode doubSlashNode)
        {
            GenerateComment(doubSlashNode);
        }
        else if (node is VarDeclNode varDecl)
        {
            GenerateExpression(varDecl.Initializer, p);
            
            string type = varDecl.Type;
            if (_structs.ContainsKey(type))
            {
                type = "*" + type;
            }
            
            int size = GetSizeOfType(type);
            _stackOffset -= size;
            _stackMap[varDecl.Name] = _stackOffset;
            _typeMap[varDecl.Name] = type;

            if (size == 1)
            {
                _asm.AppendLine($"   mov [rbp{_stackOffset}], al   ; Save the variable '{varDecl.Name}' of type {type}");
            }
            else
            {
                _asm.AppendLine($"   mov [rbp{_stackOffset}], rax   ; Save the variable '{varDecl.Name}' of type {type}");
            }
        }
        else if (node is AssignmentNode assignNode)
        {
            GenerateExpression(assignNode.Expression, p);
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
            
            string targetReg = _isWindows ? "rcx" : "rdi";
            _asm.AppendLine($"   mov {targetReg}, [rbp{offset}]  ; Recovers the address to free");
            _asm.AppendLine($"   call {p}free  ; Frees the memory");
        }
        else if (node is DerefAssignNode derefNode)
        {
            GenerateExpression(derefNode.Value, p);
            _asm.AppendLine("   push rax    ; Save value to deref-assign");

            int offset = _stackMap[derefNode.PointerName];
            string type = _typeMap.ContainsKey(derefNode.PointerName) ? _typeMap[derefNode.PointerName] : "ptr";
            int elementSize = type.Contains("*char") || type == "char" ? 1 : 8;

            _asm.AppendLine($"   mov rdx, [rbp{offset}]    ; Recovers the pointer {derefNode.PointerName} address");
            _asm.AppendLine("   pop rax     ; Restore value to write");

            if (elementSize == 1)
            {
                _asm.AppendLine("   mov [rdx], al      ; Writes the byte value inside the memory cell");
            }
            else
            {
                _asm.AppendLine("   mov [rdx], rax      ; Writes the real value inside the memory cell of the heap");
            }
        }
        else if (node is PrintNode printNode)
        {
            if (!_stackMap.TryGetValue(printNode.VarName, out int offset))
            {
                throw new Exception($"Variable '{printNode.VarName}' not defined");
            }
            string type = _typeMap.ContainsKey(printNode.VarName) ? _typeMap[printNode.VarName] : "int";

            string r1 = _isWindows ? "rcx" : "rdi";
            string r2 = _isWindows ? "rdx" : "rsi";

            if (printNode.IsMemory)
            {
                _asm.AppendLine($"   mov rax, [rbp{offset}]    ; Recovers the address of {printNode.VarName}");

                if (type.Contains("*char") || type == "char")
                {
                    _asm.AppendLine($"   movzx {r2}, byte [rax] ; Reads the real byte value from heap");
                    _asm.AppendLine($"   lea {r1}, [rel format_char]");
                }
                else
                {
                    _asm.AppendLine($"   mov {r2}, [rax]      ; Reads the real value from heap");
                    _asm.AppendLine($"   lea {r1}, [rel format_num]");
                }
            }
            else
            {
                if (type == "char")
                {
                    _asm.AppendLine($"   mov {r2}, [rbp{offset}]    ; Load char value to second parameter register");
                    _asm.AppendLine($"   lea {r1}, [rel format_char]");
                }
                else if (type.Contains("*char"))
                {
                    _asm.AppendLine($"   lea {r1}, [rel format_str]");
                    _asm.AppendLine($"   mov {r2}, [rbp{offset}]    ; Load string pointer to second parameter register");
                }
                else
                {
                    _asm.AppendLine($"   mov {r2}, [rbp{offset}]    ; Load int value to second parameter register");
                    _asm.AppendLine($"   lea {r1}, [rel format_num]");
                }
            }
            _asm.AppendLine("   mov al, 0");
            _asm.AppendLine($"   call {p}printf    ; Calls the system printf");
        }
        else if (node is PrintStrNode strNode)
        {
            _msgCount++;
            string msgLabel = $"msg{_msgCount}";
            _asmData.AppendLine($"   {msgLabel} db \"{EscapeString(strNode.Text)}\", 0");
            string r1 = _isWindows ? "rcx" : "rdi";
            string r2 = _isWindows ? "rdx" : "rsi";
            _asm.AppendLine($"   lea {r1}, [rel format_str]");
            _asm.AppendLine($"   lea {r2}, [rel {msgLabel}]");
            _asm.AppendLine("   mov al, 0");
            _asm.AppendLine($"   call {p}printf");
        }
        else if (node is PrintExprNode printExpr)
        {
            GenerateExpression(printExpr.Expression, p);
            string r1 = _isWindows ? "rcx" : "rdi";
            string r2 = _isWindows ? "rdx" : "rsi";
            _asm.AppendLine($"   mov {r2}, rax");
            string format = "format_num";
            
            string? exprType = GetExpressionType(printExpr.Expression);
            if (exprType != null)
            {
                if (exprType == "char") format = "format_char";
                else if (exprType.Contains("*char")) format = "format_str";
            }
            else if (printExpr.Expression is StringLiteralNode)
            {
                format = "format_str";
            }
            
            _asm.AppendLine($"   lea {r1}, [rel {format}]");
            _asm.AppendLine("   mov al, 0");
            _asm.AppendLine($"   call {p}printf");
        }
        else if (node is IfStatementNode ifNode)
        {
            _ifCount++;
            int currentIf = _ifCount;
            string labelElse = $".if_else{currentIf}";
            string labelEnd = $".if_end{currentIf}";
            GenerateCondition(ifNode.Condition, labelElse, true, p);
            foreach (var bodyStatement in ifNode.Body)
            {
                GenerateStatement(bodyStatement, p);
            }
            _asm.AppendLine($"   jmp {labelEnd}");
            _asm.AppendLine($"{labelElse}:");
            if (ifNode.ElseBody != null)
            {
                foreach (var elseStatement in ifNode.ElseBody)
                {
                    GenerateStatement(elseStatement, p);
                }
            }
            _asm.AppendLine($"{labelEnd}:");
        }
        else if (node is ReturnNode returnNode)
        {
            if (returnNode.Value != null)
            {
                GenerateExpression(returnNode.Value, p);
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
                GenerateExpression(callNode.Arguments[i], p);
                _asm.AppendLine($"   mov {ParameterRegisters[i]}, rax");
            }
            _asm.AppendLine($"   call {p}{callNode.FunctionName}");
        }
        else if (node is ArrayAssignNode arrayAssign)
        {
            if (_stackMap.TryGetValue(arrayAssign.ArrayName, out int offset))
            {
                string type = _typeMap.ContainsKey(arrayAssign.ArrayName) ? _typeMap[arrayAssign.ArrayName] : "ptr";
                int elementSize = type.Contains("*char") || type == "char" ? 1 : 8;
                GenerateExpression(arrayAssign.Index, p);
                _asm.AppendLine("   push rax        ; Save index");
                GenerateExpression(arrayAssign.Value, p);
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
                _asm.AppendLine("   mov rax, rcx");
            }
            else
            {
                throw new Exception($"Array '{arrayAssign.ArrayName}' not defined");
            }
        }
        else if (node is StructAssignNode structAssign)
        {
            GenerateExpression(structAssign.Value, p);
            _asm.AppendLine("   push rax        ; Save value to assign to struct field");

            GenerateExpression(structAssign.StructPtr, p);
            _asm.AppendLine("   mov rdx, rax    ; RDX = struct base pointer");
            
            string? structType = GetExpressionType(structAssign.StructPtr);
            if (structType == null) throw new Exception("Could not determine struct type");
            
            string structName = structType.TrimStart('*');
            int offset = GetFieldOffset(structName, structAssign.FieldName);
            
            _asm.AppendLine("   pop rax         ; RAX = value to assign");
            _asm.AppendLine($"   mov [rdx + {offset}], rax ; Assign value to field {structAssign.FieldName} at offset {offset}");
        }
        else if (node is ExitNode exitNode)
        {
            GenerateExpression(exitNode.ExitCode, p);
            string targetReg = _isWindows ? "rcx" : "rdi";
            _asm.AppendLine($"   mov {targetReg}, rax");
            _asm.AppendLine($"   call {p}exit");
        }
        else if (node is WhileNode whileNode)
        {
            _whileCount++;
            int currentWhile = _whileCount;
            string labelStart = $".while_start{currentWhile}";
            string labelEnd = $".while_end{currentWhile}";
            _asm.AppendLine($"{labelStart}:");
            GenerateCondition(whileNode.Condition, labelEnd, true, p);
            foreach (var bodyStatement in whileNode.Body)
            {
                GenerateStatement(bodyStatement, p);
            }
            _asm.AppendLine($"   jmp {labelStart}");
            _asm.AppendLine($"{labelEnd}:");
        }
        else if (node is DoWhileNode doWhileNode)
        {
            _doWhileCount++;
            int currentDoWhile = _doWhileCount;
            string labelStart = $".do_while_start{currentDoWhile}";
            string labelEnd = $".do_while_end{currentDoWhile}";
            _asm.AppendLine($"{labelStart}:");
            foreach (var bodyStatement in doWhileNode.Body)
            {
                GenerateStatement(bodyStatement, p);
            }
            GenerateCondition(doWhileNode.Condition, labelEnd, true, p);
            _asm.AppendLine($"   jmp {labelStart}");
            _asm.AppendLine($"{labelEnd}:");
        }
        else if (node is ForNode forNode)
        {
            _forCount++;
            int currentFor = _forCount;
            string labelStart = $".for_start{currentFor}";
            string labelEnd = $".for_end{currentFor}";
            string labelIter = $".for_iter{currentFor}";
            if (forNode.Initialization != null)
            {
                GenerateStatement(forNode.Initialization, p);
            }
            _asm.AppendLine($"{labelStart}:");
            if (forNode.Condition != null)
            {
                GenerateCondition(forNode.Condition, labelEnd, true, p);
            }
            foreach (var bodyStatement in forNode.Body)
            {
                GenerateStatement(bodyStatement, p);
            }
            _asm.AppendLine($"{labelIter}:");
            if (forNode.Iteration != null)
            {
                if (forNode.Iteration is AssignmentNode || forNode.Iteration is ArrayAssignNode || forNode.Iteration is FunctionCallNode)
                {
                    GenerateStatement(forNode.Iteration, p);
                }
                else
                {
                    GenerateExpression(forNode.Iteration, p);
                }
            }
            _asm.AppendLine($"   jmp {labelStart}");
            _asm.AppendLine($"{labelEnd}:");
        }
        else if (node is StructNode structNode)
        {
            _structs[structNode.Name] = structNode;
        }
    }

    private void GenerateExpression(AstNode node, string p)
    {
        if (node is InputNode)
        {
            string r1 = _isWindows ? "rcx" : "rdi";
            string r2 = _isWindows ? "rdx" : "rsi";
            _asm.AppendLine("   sub rsp, 16");
            _asm.AppendLine("   mov qword [rsp], 0");
            _asm.AppendLine($"   lea {r2}, [rsp]  ; Secondo parametro dell'indirizzo");
            _asm.AppendLine($"   lea {r1}, [rel format_num_input] ; Primo parametro formato");
            _asm.AppendLine("   mov al, 0");
            _asm.AppendLine($"   call {p}scanf");
            _asm.AppendLine("   mov rax, [rsp]");
            _asm.AppendLine("   add rsp, 16");
        }
        else if (node is InputStrNode inputStr)
        {
            GenerateExpression(inputStr.Buffer, p);
            _asm.AppendLine("   push rax        ; Salva il puntatore del buffer");
            GenerateExpression(inputStr.MaxLen, p);
            _asm.AppendLine("   mov r8, rax     ; Sposta temporaneamente MaxLen in R8");
            _asm.AppendLine("   pop rax         ; Recupera l'indirizzo del buffer");
            
            string r1 = _isWindows ? "rcx" : "rdi";
            string r2 = _isWindows ? "rdx" : "rsi";
            string r3 = _isWindows ? "r8"  : "rdx";
            
            _asm.AppendLine($"   mov {r1}, rax");
            _asm.AppendLine($"   mov {r2}, r8");
            
            if (_isWindows)
            {
                _asm.AppendLine("   mov ecx, 0      ; 0 = stdin identifier");
                _asm.AppendLine("   call __acrt_iob_func");
                _asm.AppendLine($"   mov {r3}, rax   ; Sposta il FILE* restituito nel 3° registro parametro");
            }
            else
            {
                _asm.AppendLine("   mov rdx, [rel ___stdinp]");
                _asm.AppendLine("   mov rdx, [rdx]");
            }
            _asm.AppendLine($"   call {p}fgets");
        }
        else if (node is StringLiteralNode strLiteral)
        {
            _msgCount++;
            string msgLabel = $"msg{_msgCount}";
            _asmData.AppendLine($"   {msgLabel} db \"{EscapeString(strLiteral.Value)}\", 0");
            _asm.AppendLine($"   lea rax, [rel {msgLabel}]");
        }
        else if (node is SqrtNode sqrtNode)
        {
            GenerateExpression(sqrtNode.Value, p);
            _asm.AppendLine("   cvtsi2sd xmm0, rax");
            _asm.AppendLine($"   call {p}sqrt");
            _asm.AppendLine("   cvttsd2si rax, xmm0");
        }
        else if (node is RandNode)
        {
            _asm.AppendLine("   mov rax, 0");
            _asm.AppendLine($"   call {p}rand");
        }
        else if (node is TimeNode)
        {
            string targetReg = _isWindows ? "rcx" : "rdi";
            _asm.AppendLine($"   mov {targetReg}, 0");
            _asm.AppendLine("   mov rax, 0");
            _asm.AppendLine($"   call {p}time");
        }
        else if (node is SleepNode sleepNode)
        {
            GenerateExpression(sleepNode.Duration, p);
            _asm.AppendLine("   imul rax, 1000");
            string targetReg = _isWindows ? "rcx" : "rdi";
            _asm.AppendLine($"   mov {targetReg}, rax");
            _asm.AppendLine("   mov rax, 0");
            _asm.AppendLine($"   call {p}usleep");
            _asm.AppendLine("   mov rax, 0");
        }
        else if (node is SrandNode srandNode)
        {
            GenerateExpression(srandNode.Seed, p);
            string targetReg = _isWindows ? "rcx" : "rdi";
            _asm.AppendLine($"   mov {targetReg}, rax");
            _asm.AppendLine("   mov rax, 0");
            _asm.AppendLine($"   call {p}srand");
        }
        else if (node is ArrayAccessNode arrayAccess)
        {
            if (_stackMap.TryGetValue(arrayAccess.ArrayName, out int offset))
            {
                string type = _typeMap.ContainsKey(arrayAccess.ArrayName) ? _typeMap[arrayAccess.ArrayName] : "ptr";
                int elementSize = type.Contains("*char") || type == "char" ? 1 : 8;
                GenerateExpression(arrayAccess.Index, p);
                _asm.AppendLine("   mov rcx, rax");
                _asm.AppendLine($"   mov rdx, [rbp{offset}]");
                if (elementSize == 8)
                {
                    _asm.AppendLine("   shl rcx, 3");
                }
                else if (elementSize == 4)
                {
                    _asm.AppendLine("   shl rcx, 2");
                }
                _asm.AppendLine("   add rdx, rcx");
                if (elementSize == 1)
                {
                    _asm.AppendLine("   movzx rax, byte [rdx]");
                }
                else
                {
                    _asm.AppendLine("   mov rax, [rdx]");
                }
            }
            else
            {
                throw new Exception($"Array '{arrayAccess.ArrayName}' not defined");
            }
        }
        else if (node is StructAccessNode structAccess)
        {
            GenerateExpression(structAccess.StructPtr, p);
            _asm.AppendLine("   mov rdx, rax    ; RDX = struct base pointer");
            
            string? structType = GetExpressionType(structAccess.StructPtr);
            if (structType == null) throw new Exception("Could not determine struct type");
            
            string structName = structType.TrimStart('*');
            int offset = GetFieldOffset(structName, structAccess.FieldName);
            
            _asm.AppendLine($"   mov rax, [rdx + {offset}] ; Access field {structAccess.FieldName} at offset {offset}");
        }
        else if (node is AllocNode allocNode)
        {
            if (allocNode.Size is IdentifierNode id && _structs.ContainsKey(id.Name))
            {
                int size = GetStructSize(id.Name);
                _asm.AppendLine($"   mov rax, {size}");
            }
            else
            {
                GenerateExpression(allocNode.Size, p);
            }
            string targetReg = _isWindows ? "rcx" : "rdi";
            _asm.AppendLine($"   mov {targetReg}, rax");
            _asm.AppendLine($"   call {p}malloc");
        }
        else if (node is NumberNode numNode)
        {
            _asm.AppendLine($"   mov rax, {numNode.Value}");
        }
        else if (node is FunctionCallNode callNode)
        {
            for (int i = 0; i < callNode.Arguments.Count; i++)
            {
                GenerateExpression(callNode.Arguments[i], p);
                _asm.AppendLine($"   mov {ParameterRegisters[i]}, rax");
            }
            _asm.AppendLine($"   call {p}{callNode.FunctionName}");
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
                _asm.AppendLine($"   mov rax, [rbp{offset}]");
                if (elementSize == 1)
                {
                    _asm.AppendLine("   movzx rax, byte [rax]");
                }
                else
                {
                    _asm.AppendLine("   mov rax, [rax]");
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
                _asm.AppendLine($"   mov rax, [rbp{offset}]");
            }
            else
            {
                throw new Exception($"Variable '{idNode.Name}' not defined");
            }
        }
        else if (node is BinaryOpNode opNode)
        {
            GenerateExpression(opNode.Left, p);
            _asm.AppendLine("   push rax");
            GenerateExpression(opNode.Right, p);
            _asm.AppendLine("   mov rcx, rax");
            _asm.AppendLine("   pop rax");
            string? leftType = GetExpressionType(opNode.Left);
            if (opNode.Operator == "+" || opNode.Operator == "-")
            {
                if (leftType != null && leftType.StartsWith("*"))
                {
                    string pointedType = leftType.Substring(1);
                    int size = (pointedType == "char" || pointedType.StartsWith("*char")) ? 1 : 8;
                    if (size > 1)
                    {
                        _asm.AppendLine($"   imul rcx, {size}");
                    }
                }
            }
            switch (opNode.Operator)
            {
                case "+":
                    _asm.AppendLine("   add rax, rcx");
                    break;
                case "-":
                    _asm.AppendLine("   sub rax, rcx");
                    break;
                case "*":
                    _asm.AppendLine("   imul rax, rcx");
                    break;
                case "/":
                    _asm.AppendLine("   cqo");
                    _asm.AppendLine("   idiv rcx");
                    break;
                case "%":
                    _asm.AppendLine("   cqo");
                    _asm.AppendLine("   idiv rcx");
                    _asm.AppendLine("   mov rax, rdx");
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
            GenerateExpression(unaryNode.Right, p);
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
            GenerateExpression(compNode.Left, p);
            _asm.AppendLine("   push rax");
            GenerateExpression(compNode.Right, p);
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
        else if (node is BoolLiteralNode boolNode)
        {
            _asm.AppendLine($"  mov rax, {(boolNode.Value ? 1 : 0)}");
        }
    }

    private void GenerateCondition(AstNode condition, string targetLabel, bool jumpIfFalse, string p)
    {
        if (condition is ComparisonNode comp)
        {
            GenerateExpression(comp.Left, p);
            _asm.AppendLine("   push rax");
            GenerateExpression(comp.Right, p);
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
                GenerateCondition(bin.Left, targetLabel, true, p);
                GenerateCondition(bin.Right, targetLabel, true, p);
            }
            else
            {
                string labelEnd = $".and_end{Guid.NewGuid().ToString("N")}";
                GenerateCondition(bin.Left, labelEnd, true, p);
                GenerateCondition(bin.Right, targetLabel, false, p);
                _asm.AppendLine($"{labelEnd}:");
            }
        }
        else if (condition is BinaryOpNode binOr && binOr.Operator == "||")
        {
            if (jumpIfFalse)
            {
                string labelTrue = $".or_true{Guid.NewGuid().ToString("N")}";
                GenerateCondition(binOr.Left, labelTrue, false, p);
                GenerateCondition(binOr.Right, targetLabel, true, p);
                _asm.AppendLine($"{labelTrue}:");
            }
            else
            {
                GenerateCondition(binOr.Left, targetLabel, false, p);
                GenerateCondition(binOr.Right, targetLabel, false, p);
            }
        }
        else if (condition is UnaryOpNode unary && unary.Operator == "!")
        {
            GenerateCondition(unary.Right, targetLabel, !jumpIfFalse, p);
        }
        else
        {
            GenerateExpression(condition, p);
            _asm.AppendLine("   cmp rax, 0");
            _asm.AppendLine(jumpIfFalse ? $"   je {targetLabel}" : $"   jne {targetLabel}");
        }
    }

    private void GenerateCondition(AstNode condition, string targetLabel, bool jumpIfFalse)
    {
        GenerateCondition(condition, targetLabel, jumpIfFalse, _p);
    }

    private void GenerateComment(AstNode node)
    {
        if (node is DoubleSlashNode comment)
        {
            _asm.AppendLine($"   ; {comment.Comment}");
        }
    }

    private int GetSizeOfType(string type)
    {
        if (type.StartsWith("*")) return 8;
        if (type == "int") return 8;
        if (type == "bool") return 8;
        if (type == "char") return 1;
        if (_structs.ContainsKey(type)) return GetStructSize(type);
        return 8; // Default
    }

    private int GetStructSize(string structName)
    {
        if (_structs.TryGetValue(structName, out var structNode))
        {
            int size = 0;
            foreach (var field in structNode.Fields)
            {
                size += GetSizeOfType(field.Type);
            }
            return size;
        }
        throw new Exception($"Struct '{structName}' not defined");
    }

    private int GetFieldOffset(string structName, string fieldName)
    {
        if (_structs.TryGetValue(structName, out var structNode))
        {
            int offset = 0;
            foreach (var field in structNode.Fields)
            {
                if (field.Name == fieldName)
                {
                    return offset;
                }
                offset += GetSizeOfType(field.Type);
            }
            throw new Exception($"Field '{fieldName}' not found in struct '{structName}'");
        }
        throw new Exception($"Struct '{structName}' not defined");
    }

    private string? GetExpressionType(AstNode node)
    {
        if (node is IdentifierNode id)
        {
            _typeMap.TryGetValue(id.Name, out string? type);
            return type;
        }
        if (node is StructAccessNode sa)
        {
            string? structType = GetExpressionType(sa.StructPtr);
            if (structType != null)
            {
                string structName = structType.TrimStart('*');
                if (_structs.TryGetValue(structName, out var structNode))
                {
                    var field = structNode.Fields.Find(f => f.Name == sa.FieldName);
                    return field?.Type;
                }
            }
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
