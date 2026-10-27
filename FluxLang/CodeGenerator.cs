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
    private readonly Dictionary<string, (int Offset, int Size)> _staticArrays = new Dictionary<string, (int, int)>();
    private int _stackOffset = 0;
    private int _msgCount = 0;
    private int _ifCount = 0;
    private string? _currentFunctionName = null;

    private int _pushDepth = 0;
    private const int STACK_FRAME_SIZE = 4096;

    private string[] ParameterRegisters = { "rdi", "rsi", "rdx", "rcx", "r8", "r9" };

    private int _whileCount = 0;
    private int _doWhileCount = 0;
    private int _forCount = 0;
    private int _switchCount = 0;

    private readonly Stack<string> _breakLabels = new Stack<string>();
    private readonly Stack<string> _continueLabels = new Stack<string>();

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
            ParameterRegisters = new string[] { "rcx", "rdx", "r8", "r9" };
        else
            ParameterRegisters = new string[] { "rdi", "rsi", "rdx", "rcx", "r8", "r9" };

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
        _staticArrays.Clear();
        _stackOffset = 0;
        _pushDepth = 0;
        _whileCount = 0;
        _doWhileCount = 0;
        _forCount = 0;
        _switchCount = 0;
        _breakLabels.Clear();
        _continueLabels.Clear();

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
                GenerateStatement(statement, _p);
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
            finalAsm.AppendLine("extern ___stdinp");
        else
            finalAsm.AppendLine("extern __acrt_iob_func");
        finalAsm.AppendLine();

        finalAsm.AppendLine("section .data");
        finalAsm.AppendLine("   align 16");
        finalAsm.AppendLine("   format_num db \"%lld\", 0");
        finalAsm.AppendLine("   format_num_input db \"%lld\", 0");
        finalAsm.AppendLine("   format_char db \"%c\", 0");
        finalAsm.AppendLine("   format_str db \"%s\", 0");
        finalAsm.AppendLine("   format_double db \"%f\", 0");
        finalAsm.AppendLine("   format_float db \"%f\", 0");
        finalAsm.AppendLine("   align 8");
        finalAsm.AppendLine("   double_zero dq 0x0000000000000000");
        finalAsm.AppendLine("   float_zero dd 0x00000000");
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
            finalAsm.AppendLine($"   sub rsp, {STACK_FRAME_SIZE}");
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

    private void EmitExternalCall(string functionName, string p)
    {
        bool needsAlign = (_pushDepth % 2) != 0;
        if (needsAlign) _asm.AppendLine("   sub rsp, 8");
        _asm.AppendLine($"   call {p}{functionName}");
        if (needsAlign) _asm.AppendLine("   add rsp, 8");
    }

    private void EmitFluxCall(string functionName, string p)
    {
        bool needsAlign = (_pushDepth % 2) != 0;
        if (needsAlign) _asm.AppendLine("   sub rsp, 8");
        _asm.AppendLine($"   call {p}{functionName}");
        if (needsAlign) _asm.AppendLine("   add rsp, 8");
    }

    private void GenerateFunction(FunctionDeclNode func, string p)
    {
        _currentFunctionName = func.Name;
        _stackMap.Clear();
        _typeMap.Clear();
        _staticArrays.Clear();
        _stackOffset = 0;
        _pushDepth = 0;

        string asmName = $"{p}{func.Name}";

        _asmFunctions.AppendLine($"{asmName}:");
        _asmFunctions.AppendLine("   push rbp");
        _asmFunctions.AppendLine("   mov rbp, rsp");
        _asmFunctions.AppendLine($"   sub rsp, {STACK_FRAME_SIZE}");

        for (int i = 0; i < func.Parameters.Count; i++)
        {
            var param = func.Parameters[i];
            _stackOffset -= 8;
            _stackMap[param.Name] = _stackOffset;

            string type = param.Type;
            if (_structs.ContainsKey(type))
                type = "*" + type;

            _typeMap[param.Name] = type;

            if (type == "double" || type == "float")
                _asmFunctions.AppendLine($"   movsd [rbp{_stackOffset}], xmm{i}");
            else
                _asmFunctions.AppendLine($"   mov [rbp{_stackOffset}], {ParameterRegisters[i]}");
        }

        var oldAsm = _asm;
        StringBuilder funcBody = new StringBuilder();
        _asm = funcBody;

        foreach (var stmt in func.Body)
            GenerateStatement(stmt, p);

        _asm = oldAsm;
        _asmFunctions.Append(funcBody.ToString());

        _asmFunctions.AppendLine($"   .exit_{func.Name}:");
        if (func.Name == "main")
            _asmFunctions.AppendLine("   mov rax, 0");
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
        else if (node is TypedefNode || node is EnumNode)
        {
        }
        else if (node is VarDeclNode varDecl)
        {
            string type = varDecl.Type;
            if (_structs.ContainsKey(type))
                type = "*" + type;

            if (varDecl.StaticArraySize > 0)
            {
                int elementSize = GetElementSize(type);
                int totalSize = elementSize * varDecl.StaticArraySize;
                totalSize = ((totalSize + 7) / 8) * 8;
                _stackOffset -= totalSize;
                _stackMap[varDecl.Name] = _stackOffset;
                _typeMap[varDecl.Name] = type;
                _staticArrays[varDecl.Name] = (_stackOffset, varDecl.StaticArraySize);

                if (varDecl.ArrayInitializers != null)
                {
                    for (int i = 0; i < varDecl.ArrayInitializers.Count && i < varDecl.StaticArraySize; i++)
                    {
                        GenerateExpression(varDecl.ArrayInitializers[i], p);
                        int elemOffset = _stackOffset + (i * elementSize);
                        string sign = elemOffset >= 0 ? "+" : "";
                        if (elementSize == 1)
                            _asm.AppendLine($"   mov [rbp{sign}{elemOffset}], al");
                        else if (elementSize == 4)
                            _asm.AppendLine($"   mov [rbp{sign}{elemOffset}], eax");
                        else
                            _asm.AppendLine($"   mov [rbp{sign}{elemOffset}], rax");
                    }
                }
                else
                {
                    _asm.AppendLine($"   lea rdi, [rbp{_stackOffset}]");
                    _asm.AppendLine($"   mov rcx, {totalSize}");
                    _asm.AppendLine("   xor rax, rax");
                    _asm.AppendLine("   rep stosb");
                }
                return;
            }

            if (type == "float" && varDecl.Initializer is FloatLiteralNode fl)
            {
                float fv = (float)fl.Value;
                int fbits = BitConverter.SingleToInt32Bits(fv);
                _stackOffset -= 8;
                _stackMap[varDecl.Name] = _stackOffset;
                _typeMap[varDecl.Name] = type;
                _asm.AppendLine($"   mov dword [rbp{_stackOffset}], 0x{fbits:X8}   ; float literal");
                return;
            }

            GenerateExpression(varDecl.Initializer, p);

            int size = GetSizeOfType(type);
            size = ((size + 7) / 8) * 8;
            _stackOffset -= size;
            _stackMap[varDecl.Name] = _stackOffset;
            _typeMap[varDecl.Name] = type;

            int effSize = GetSizeOfType(type);
            if (effSize == 1)
                _asm.AppendLine($"   mov [rbp{_stackOffset}], al");
            else if (effSize == 4)
                _asm.AppendLine($"   mov [rbp{_stackOffset}], eax");
            else
                _asm.AppendLine($"   mov [rbp{_stackOffset}], rax");
        }
        else if (node is AssignmentNode assignNode)
        {
            if (_stackMap.TryGetValue(assignNode.Name, out int offset))
            {
                string type = _typeMap[assignNode.Name];

                if (assignNode.CompoundOp != null)
                {
                    _asm.AppendLine($"   mov rax, [rbp{offset}]");
                    _asm.AppendLine("   push rax");
                    _pushDepth++;
                    GenerateExpression(assignNode.Expression, p);
                    _asm.AppendLine("   mov rcx, rax");
                    _asm.AppendLine("   pop rax");
                    _pushDepth--;
                    string binOp = assignNode.CompoundOp.Substring(0, 1);
                    EmitBinaryOp(binOp, GetExpressionType(new IdentifierNode(assignNode.Name)) ?? type);
                }
                else
                {
                    GenerateExpression(assignNode.Expression, p);
                }

                if (assignNode.Expression is StringLiteralNode && _typeMap[assignNode.Name] == "char")
                    _typeMap[assignNode.Name] = "*char";

                int sz = GetSizeOfType(_typeMap[assignNode.Name]);
                if (sz == 1)
                    _asm.AppendLine($"   mov [rbp{offset}], al");
                else if (sz == 4)
                    _asm.AppendLine($"   mov [rbp{offset}], eax");
                else
                    _asm.AppendLine($"   mov [rbp{offset}], rax");
            }
            else throw new Exception($"Variable '{assignNode.Name}' not defined");
        }
        else if (node is IncrementNode incNode)
        {
            if (_stackMap.TryGetValue(incNode.VarName, out int offset))
            {
                string op = incNode.IsIncrement ? "add" : "sub";
                _asm.AppendLine($"   mov rax, [rbp{offset}]");
                _asm.AppendLine($"   {op} qword [rbp{offset}], 1");
            }
            else throw new Exception($"Variable '{incNode.VarName}' not defined");
        }
        else if (node is FreeNode freeNode)
        {
            int offset = _stackMap[freeNode.VarName];
            string targetReg = _isWindows ? "rcx" : "rdi";
            _asm.AppendLine($"   mov {targetReg}, [rbp{offset}]");
            EmitExternalCall("free", p);
        }
        else if (node is DerefAssignNode derefNode)
        {
            GenerateExpression(derefNode.Value, p);
            _asm.AppendLine("   push rax");
            _pushDepth++;
            int offset = _stackMap[derefNode.PointerName];
            string type = _typeMap.ContainsKey(derefNode.PointerName) ? _typeMap[derefNode.PointerName] : "ptr";
            int elementSize = GetElementSize(type);
            _asm.AppendLine($"   mov rdx, [rbp{offset}]");
            _asm.AppendLine("   pop rax");
            _pushDepth--;
            if (elementSize == 1) _asm.AppendLine("   mov [rdx], al");
            else if (elementSize == 4) _asm.AppendLine("   mov [rdx], eax");
            else _asm.AppendLine("   mov [rdx], rax");
        }
        else if (node is PrintNode printNode)
        {
            if (!_stackMap.TryGetValue(printNode.VarName, out int offset))
                throw new Exception($"Variable '{printNode.VarName}' not defined");
            string type = _typeMap.ContainsKey(printNode.VarName) ? _typeMap[printNode.VarName] : "int";
            string r1 = _isWindows ? "rcx" : "rdi";
            string r2 = _isWindows ? "rdx" : "rsi";

            if (printNode.IsMemory)
            {
                _asm.AppendLine($"   mov rax, [rbp{offset}]");
                if (type.Contains("*char") || type == "char")
                {
                    _asm.AppendLine($"   movzx {r2}, byte [rax]");
                    _asm.AppendLine($"   lea {r1}, [rel format_char]");
                }
                else
                {
                    _asm.AppendLine($"   mov {r2}, [rax]");
                    _asm.AppendLine($"   lea {r1}, [rel format_num]");
                }
            }
            else
            {
                if (type == "char" || type == "byte" || type == "uint8")
                {
                    _asm.AppendLine($"   movzx {r2}, byte [rbp{offset}]");
                    _asm.AppendLine($"   lea {r1}, [rel format_char]");
                }
                else if (type.Contains("*char"))
                {
                    _asm.AppendLine($"   lea {r1}, [rel format_str]");
                    _asm.AppendLine($"   mov {r2}, [rbp{offset}]");
                }
                else if (type == "float")
                {
                    _asm.AppendLine($"   mov eax, dword [rbp{offset}]");
                    _asm.AppendLine("   movd xmm1, eax");
                    _asm.AppendLine("   cvtss2sd xmm1, xmm1");
                    _asm.AppendLine("   movq rdx, xmm1");
                    _asm.AppendLine($"   lea rcx, [rel format_double]");
                    EmitExternalCall("printf", p);
                    return;
                }
                else if (type == "double")
                {
                    _asm.AppendLine($"   mov rdx, [rbp{offset}]");
                    _asm.AppendLine("   movq xmm1, rdx");
                    _asm.AppendLine("   lea rcx, [rel format_double]");
                    EmitExternalCall("printf", p);
                    return;
                }
                else
                {
                    _asm.AppendLine($"   mov {r2}, [rbp{offset}]");
                    _asm.AppendLine($"   lea {r1}, [rel format_num]");
                }
            }
            _asm.AppendLine("   mov al, 0");
            EmitExternalCall("printf", p);
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
            EmitExternalCall("printf", p);
        }
        else if (node is PrintExprNode printExpr)
        {
            GenerateExpression(printExpr.Expression, p);
            string r1 = _isWindows ? "rcx" : "rdi";
            string r2 = _isWindows ? "rdx" : "rsi";
            string? exprType = GetExpressionType(printExpr.Expression);
            if (exprType == "float" || exprType == "double" || printExpr.Expression is FloatLiteralNode)
            {
                _asm.AppendLine("   movq xmm1, rax");
                _asm.AppendLine("   mov rdx, rax");
                _asm.AppendLine($"   lea rcx, [rel format_double]");
                EmitExternalCall("printf", p);
                return;
            }
            _asm.AppendLine($"   mov {r2}, rax");
            string format = "format_num";
            if (exprType != null)
            {
                if (exprType == "char" || exprType == "byte" || exprType == "uint8") format = "format_char";
                else if (exprType.Contains("*char")) format = "format_str";
            }
            else if (printExpr.Expression is StringLiteralNode) format = "format_str";
            _asm.AppendLine($"   lea {r1}, [rel {format}]");
            _asm.AppendLine("   mov al, 0");
            EmitExternalCall("printf", p);
        }
        else if (node is IfStatementNode ifNode)
        {
            _ifCount++;
            int currentIf = _ifCount;
            string labelElse = $".if_else{currentIf}";
            string labelEnd = $".if_end{currentIf}";
            GenerateCondition(ifNode.Condition, labelElse, true, p);
            foreach (var bodyStatement in ifNode.Body) GenerateStatement(bodyStatement, p);
            _asm.AppendLine($"   jmp {labelEnd}");
            _asm.AppendLine($"{labelElse}:");
            if (ifNode.ElseBody != null)
                foreach (var elseStatement in ifNode.ElseBody) GenerateStatement(elseStatement, p);
            _asm.AppendLine($"{labelEnd}:");
        }
        else if (node is SwitchNode switchNode)
        {
            _switchCount++;
            int currentSwitch = _switchCount;
            string labelEnd = $".switch_end{currentSwitch}";
            string labelDefault = $".switch_default{currentSwitch}";
            bool hasDefault = false;
            GenerateExpression(switchNode.Expression, p);
            _asm.AppendLine("   mov rcx, rax");
            var caseLabels = new List<string>();
            for (int i = 0; i < switchNode.Cases.Count; i++)
            {
                caseLabels.Add($".switch_case{currentSwitch}_{i}");
                if (switchNode.Cases[i].CaseValue == null) hasDefault = true;
            }
            for (int i = 0; i < switchNode.Cases.Count; i++)
            {
                if (switchNode.Cases[i].CaseValue == null) continue;
                _asm.AppendLine("   push rcx");
                _pushDepth++;
                GenerateExpression(switchNode.Cases[i].CaseValue!, p);
                _asm.AppendLine("   mov rdx, rax");
                _asm.AppendLine("   pop rcx");
                _pushDepth--;
                _asm.AppendLine("   cmp rcx, rdx");
                _asm.AppendLine($"   je {caseLabels[i]}");
            }
            _asm.AppendLine(hasDefault ? $"   jmp {labelDefault}" : $"   jmp {labelEnd}");
            _breakLabels.Push(labelEnd);
            for (int i = 0; i < switchNode.Cases.Count; i++)
            {
                if (switchNode.Cases[i].CaseValue != null) _asm.AppendLine($"{caseLabels[i]}:");
                else _asm.AppendLine($"{labelDefault}:");
                foreach (var stmt in switchNode.Cases[i].Body) GenerateStatement(stmt, p);
            }
            _breakLabels.Pop();
            _asm.AppendLine($"{labelEnd}:");
        }
        else if (node is BreakNode)
        {
            if (_breakLabels.Count == 0) throw new Exception("break outside loop/switch");
            _asm.AppendLine($"   jmp {_breakLabels.Peek()}");
        }
        else if (node is ContinueNode)
        {
            if (_continueLabels.Count == 0) throw new Exception("continue outside loop");
            _asm.AppendLine($"   jmp {_continueLabels.Peek()}");
        }
        else if (node is ReturnNode returnNode)
        {
            if (returnNode.Value != null) GenerateExpression(returnNode.Value, p);
            if (_currentFunctionName != null) _asm.AppendLine($"   jmp .exit_{_currentFunctionName}");
            else _asm.AppendLine("   jmp .exit_main");
        }
        else if (node is FunctionCallNode callNode)
        {
            for (int i = 0; i < callNode.Arguments.Count; i++)
            {
                GenerateExpression(callNode.Arguments[i], p);
                _asm.AppendLine($"   mov {ParameterRegisters[i]}, rax");
            }
            EmitFluxCall(callNode.FunctionName, p);
        }
        else if (node is ArrayAssignNode arrayAssign)
        {
            if (_stackMap.TryGetValue(arrayAssign.ArrayName, out int offset))
            {
                string type = _typeMap.ContainsKey(arrayAssign.ArrayName) ? _typeMap[arrayAssign.ArrayName] : "ptr";
                int elementSize = GetElementSize(type);
                GenerateExpression(arrayAssign.Index, p);
                _asm.AppendLine("   push rax");
                _pushDepth++;
                GenerateExpression(arrayAssign.Value, p);
                _asm.AppendLine("   mov rcx, rax");
                _asm.AppendLine("   pop rax");
                _pushDepth--;
                if (_staticArrays.ContainsKey(arrayAssign.ArrayName))
                    _asm.AppendLine($"   lea rdx, [rbp{offset}]");
                else
                    _asm.AppendLine($"   mov rdx, [rbp{offset}]");
                if (elementSize == 8) _asm.AppendLine("   shl rax, 3");
                else if (elementSize == 4) _asm.AppendLine("   shl rax, 2");
                _asm.AppendLine("   add rdx, rax");
                if (elementSize == 1) _asm.AppendLine("   mov [rdx], cl");
                else if (elementSize == 4) _asm.AppendLine("   mov [rdx], ecx");
                else _asm.AppendLine("   mov [rdx], rcx");
                _asm.AppendLine("   mov rax, rcx");
            }
            else throw new Exception($"Array '{arrayAssign.ArrayName}' not defined");
        }
        else if (node is StructAssignNode structAssign)
        {
            GenerateExpression(structAssign.Value, p);
            _asm.AppendLine("   push rax");
            _pushDepth++;
            GenerateExpression(structAssign.StructPtr, p);
            _asm.AppendLine("   mov rdx, rax");
            string? structType = GetExpressionType(structAssign.StructPtr);
            if (structType == null) throw new Exception("Could not determine struct type");
            string structName = structType.TrimStart('*');
            int offset = GetFieldOffset(structName, structAssign.FieldName);
            _asm.AppendLine("   pop rax");
            _pushDepth--;
            _asm.AppendLine($"   mov [rdx + {offset}], rax");
        }
        else if (node is ExitNode exitNode)
        {
            GenerateExpression(exitNode.ExitCode, p);
            string targetReg = _isWindows ? "rcx" : "rdi";
            _asm.AppendLine($"   mov {targetReg}, rax");
            EmitExternalCall("exit", p);
        }
        else if (node is WhileNode whileNode)
        {
            _whileCount++;
            int currentWhile = _whileCount;
            string labelStart = $".while_start{currentWhile}";
            string labelEnd = $".while_end{currentWhile}";
            _breakLabels.Push(labelEnd);
            _continueLabels.Push(labelStart);
            _asm.AppendLine($"{labelStart}:");
            GenerateCondition(whileNode.Condition, labelEnd, true, p);
            foreach (var bodyStatement in whileNode.Body) GenerateStatement(bodyStatement, p);
            _asm.AppendLine($"   jmp {labelStart}");
            _asm.AppendLine($"{labelEnd}:");
            _breakLabels.Pop();
            _continueLabels.Pop();
        }
        else if (node is DoWhileNode doWhileNode)
        {
            _doWhileCount++;
            int currentDoWhile = _doWhileCount;
            string labelStart = $".do_while_start{currentDoWhile}";
            string labelEnd = $".do_while_end{currentDoWhile}";
            string labelCond = $".do_while_cond{currentDoWhile}";
            _breakLabels.Push(labelEnd);
            _continueLabels.Push(labelCond);
            _asm.AppendLine($"{labelStart}:");
            foreach (var bodyStatement in doWhileNode.Body) GenerateStatement(bodyStatement, p);
            _asm.AppendLine($"{labelCond}:");
            GenerateCondition(doWhileNode.Condition, labelEnd, true, p);
            _asm.AppendLine($"   jmp {labelStart}");
            _asm.AppendLine($"{labelEnd}:");
            _breakLabels.Pop();
            _continueLabels.Pop();
        }
        else if (node is ForNode forNode)
        {
            _forCount++;
            int currentFor = _forCount;
            string labelStart = $".for_start{currentFor}";
            string labelEnd = $".for_end{currentFor}";
            string labelIter = $".for_iter{currentFor}";
            _breakLabels.Push(labelEnd);
            _continueLabels.Push(labelIter);
            if (forNode.Initialization != null) GenerateStatement(forNode.Initialization, p);
            _asm.AppendLine($"{labelStart}:");
            if (forNode.Condition != null) GenerateCondition(forNode.Condition, labelEnd, true, p);
            foreach (var bodyStatement in forNode.Body) GenerateStatement(bodyStatement, p);
            _asm.AppendLine($"{labelIter}:");
            if (forNode.Iteration != null)
            {
                if (forNode.Iteration is AssignmentNode || forNode.Iteration is ArrayAssignNode ||
                    forNode.Iteration is FunctionCallNode || forNode.Iteration is IncrementNode)
                    GenerateStatement(forNode.Iteration, p);
                else
                    GenerateExpression(forNode.Iteration, p);
            }
            _asm.AppendLine($"   jmp {labelStart}");
            _asm.AppendLine($"{labelEnd}:");
            _breakLabels.Pop();
            _continueLabels.Pop();
        }
        else if (node is StructNode structNode)
        {
            _structs[structNode.Name] = structNode;
        }
    }

    private void EmitBinaryOp(string op, string type)
    {
        bool isFloat = type == "float" || type == "double";
        if (isFloat)
        {
            _asm.AppendLine("   movq xmm0, rax");
            _asm.AppendLine("   movq xmm1, rcx");
            string suffix = type == "float" ? "ss" : "sd";
            switch (op)
            {
                case "+": _asm.AppendLine($"   add{suffix} xmm0, xmm1"); break;
                case "-": _asm.AppendLine($"   sub{suffix} xmm0, xmm1"); break;
                case "*": _asm.AppendLine($"   mul{suffix} xmm0, xmm1"); break;
                case "/": _asm.AppendLine($"   div{suffix} xmm0, xmm1"); break;
                default: _asm.AppendLine($"   add{suffix} xmm0, xmm1"); break;
            }
            _asm.AppendLine("   movq rax, xmm0");
            return;
        }

        switch (op)
        {
            case "+":
                if (type.StartsWith("*"))
                {
                    string pointedType = type.Substring(1);
                    int size = GetElementSize(pointedType);
                    if (size > 1) _asm.AppendLine($"   imul rcx, {size}");
                }
                _asm.AppendLine("   add rax, rcx");
                break;
            case "-":
                if (type.StartsWith("*"))
                {
                    string pointedType = type.Substring(1);
                    int size = GetElementSize(pointedType);
                    if (size > 1) _asm.AppendLine($"   imul rcx, {size}");
                }
                _asm.AppendLine("   sub rax, rcx");
                break;
            case "*": _asm.AppendLine("   imul rax, rcx"); break;
            case "/":
                _asm.AppendLine("   cqo");
                _asm.AppendLine("   idiv rcx");
                break;
            case "%":
                _asm.AppendLine("   cqo");
                _asm.AppendLine("   idiv rcx");
                _asm.AppendLine("   mov rax, rdx");
                break;
            case "&": _asm.AppendLine("   and rax, rcx"); break;
            case "|": _asm.AppendLine("   or rax, rcx"); break;
            case "^": _asm.AppendLine("   xor rax, rcx"); break;
            case "<<":
                _asm.AppendLine("   mov rdx, rcx");
                _asm.AppendLine("   mov rcx, rdx");
                _asm.AppendLine("   shl rax, cl");
                break;
            case ">>":
                _asm.AppendLine("   mov rdx, rcx");
                _asm.AppendLine("   mov rcx, rdx");
                _asm.AppendLine("   sar rax, cl");
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

    private void GenerateExpression(AstNode node, string p)
    {
        if (node is InputNode)
        {
            string r1 = _isWindows ? "rcx" : "rdi";
            string r2 = _isWindows ? "rdx" : "rsi";
            _asm.AppendLine("   sub rsp, 16");
            _asm.AppendLine("   mov qword [rsp], 0");
            _asm.AppendLine($"   lea {r2}, [rsp]");
            _asm.AppendLine($"   lea {r1}, [rel format_num_input]");
            _asm.AppendLine("   mov al, 0");
            EmitExternalCall("scanf", p);
            _asm.AppendLine("   mov rax, [rsp]");
            _asm.AppendLine("   add rsp, 16");
        }
        else if (node is InputStrNode inputStr)
        {
            GenerateExpression(inputStr.Buffer, p);
            _asm.AppendLine("   push rax");
            _pushDepth++;
            GenerateExpression(inputStr.MaxLen, p);
            _asm.AppendLine("   mov r8, rax");
            _asm.AppendLine("   pop rax");
            _pushDepth--;
            string r1 = _isWindows ? "rcx" : "rdi";
            string r2 = _isWindows ? "rdx" : "rsi";
            string r3 = _isWindows ? "r8" : "rdx";
            _asm.AppendLine($"   mov {r1}, rax");
            _asm.AppendLine($"   mov {r2}, r8");
            if (_isWindows)
            {
                _asm.AppendLine("   mov ecx, 0");
                EmitExternalCall("__acrt_iob_func", p);
                _asm.AppendLine($"   mov {r3}, rax");
            }
            else
            {
                _asm.AppendLine("   mov rdx, [rel ___stdinp]");
                _asm.AppendLine("   mov rdx, [rdx]");
            }
            EmitExternalCall("fgets", p);
        }
        else if (node is StringLiteralNode strLiteral)
        {
            _msgCount++;
            string msgLabel = $"msg{_msgCount}";
            _asmData.AppendLine($"   {msgLabel} db \"{EscapeString(strLiteral.Value)}\", 0");
            _asm.AppendLine($"   lea rax, [rel {msgLabel}]");
        }
        else if (node is FloatLiteralNode floatLit)
        {
            if (floatLit.IsFloat)
            {
                float fv = (float)floatLit.Value;
                int fbits = BitConverter.SingleToInt32Bits(fv);
                _asm.AppendLine($"   mov eax, 0x{fbits:X8}   ; float literal");
            }
            else
            {
                long bits = BitConverter.DoubleToInt64Bits(floatLit.Value);
                _asm.AppendLine($"   mov rax, 0x{bits:X16}   ; double literal");
            }
        }
        else if (node is SqrtNode sqrtNode)
        {
            GenerateExpression(sqrtNode.Value, p);
            _asm.AppendLine("   cvtsi2sd xmm0, rax");
            EmitExternalCall("sqrt", p);
            _asm.AppendLine("   cvttsd2si rax, xmm0");
        }
        else if (node is RandNode)
        {
            _asm.AppendLine("   mov rax, 0");
            EmitExternalCall("rand", p);
        }
        else if (node is TimeNode)
        {
            string targetReg = _isWindows ? "rcx" : "rdi";
            _asm.AppendLine($"   mov {targetReg}, 0");
            _asm.AppendLine("   mov rax, 0");
            EmitExternalCall("time", p);
        }
        else if (node is SleepNode sleepNode)
        {
            GenerateExpression(sleepNode.Duration, p);
            _asm.AppendLine("   imul rax, 1000");
            string targetReg = _isWindows ? "rcx" : "rdi";
            _asm.AppendLine($"   mov {targetReg}, rax");
            _asm.AppendLine("   mov rax, 0");
            EmitExternalCall("usleep", p);
            _asm.AppendLine("   mov rax, 0");
        }
        else if (node is SrandNode srandNode)
        {
            GenerateExpression(srandNode.Seed, p);
            string targetReg = _isWindows ? "rcx" : "rdi";
            _asm.AppendLine($"   mov {targetReg}, rax");
            _asm.AppendLine("   mov rax, 0");
            EmitExternalCall("srand", p);
        }
        else if (node is ArrayAccessNode arrayAccess)
        {
            if (_stackMap.TryGetValue(arrayAccess.ArrayName, out int offset))
            {
                string type = _typeMap.ContainsKey(arrayAccess.ArrayName) ? _typeMap[arrayAccess.ArrayName] : "ptr";
                int elementSize = GetElementSize(type);
                bool isStaticArray = _staticArrays.ContainsKey(arrayAccess.ArrayName);
                GenerateExpression(arrayAccess.Index, p);
                _asm.AppendLine("   mov rcx, rax");
                if (isStaticArray)
                    _asm.AppendLine($"   lea rdx, [rbp{offset}]");
                else
                    _asm.AppendLine($"   mov rdx, [rbp{offset}]");
                if (elementSize == 8) _asm.AppendLine("   shl rcx, 3");
                else if (elementSize == 4) _asm.AppendLine("   shl rcx, 2");
                _asm.AppendLine("   add rdx, rcx");
                if (elementSize == 1) _asm.AppendLine("   movzx rax, byte [rdx]");
                else if (elementSize == 4) _asm.AppendLine("   mov eax, [rdx]");
                else _asm.AppendLine("   mov rax, [rdx]");
            }
            else throw new Exception($"Array '{arrayAccess.ArrayName}' not defined");
        }
        else if (node is StructAccessNode structAccess)
        {
            GenerateExpression(structAccess.StructPtr, p);
            _asm.AppendLine("   mov rdx, rax");
            string? structType = GetExpressionType(structAccess.StructPtr);
            if (structType == null) throw new Exception("Could not determine struct type");
            string structName = structType.TrimStart('*');
            int offset = GetFieldOffset(structName, structAccess.FieldName);
            _asm.AppendLine($"   mov rax, [rdx + {offset}]");
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
            EmitExternalCall("malloc", p);
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
            EmitFluxCall(callNode.FunctionName, p);
        }
        else if (node is CharacterNode charNode)
        {
            _asm.AppendLine($"   mov rax, {(int)charNode.Value}");
        }
        else if (node is AddressOfNode addrOf)
        {
            if (_stackMap.TryGetValue(addrOf.VarName, out int offset))
                _asm.AppendLine($"   lea rax, [rbp{offset}]");
            else throw new Exception($"Variable '{addrOf.VarName}' not defined");
        }
        else if (node is DerefExpressionNode derefExpressionNode)
        {
            if (_stackMap.TryGetValue(derefExpressionNode.PointerName, out int offset))
            {
                string type = _typeMap.ContainsKey(derefExpressionNode.PointerName) ? _typeMap[derefExpressionNode.PointerName] : "ptr";
                int elementSize = GetElementSize(type);
                _asm.AppendLine($"   mov rax, [rbp{offset}]");
                if (elementSize == 1) _asm.AppendLine("   movzx rax, byte [rax]");
                else if (elementSize == 4) _asm.AppendLine("   mov eax, [rax]");
                else _asm.AppendLine("   mov rax, [rax]");
            }
            else throw new Exception($"Variable '{derefExpressionNode.PointerName}' not defined");
        }
        else if (node is IncrementNode incNode)
        {
            if (_stackMap.TryGetValue(incNode.VarName, out int offset))
            {
                string op = incNode.IsIncrement ? "add" : "sub";
                if (!incNode.IsPrefix)
                {
                    _asm.AppendLine($"   mov rax, [rbp{offset}]");
                    _asm.AppendLine($"   {op} qword [rbp{offset}], 1");
                }
                else
                {
                    _asm.AppendLine($"   {op} qword [rbp{offset}], 1");
                    _asm.AppendLine($"   mov rax, [rbp{offset}]");
                }
            }
            else throw new Exception($"Variable '{incNode.VarName}' not defined");
        }
        else if (node is IdentifierNode idNode)
        {
            if (_staticArrays.TryGetValue(idNode.Name, out var arrInfo))
                _asm.AppendLine($"   lea rax, [rbp{arrInfo.Offset}]");
            else if (_stackMap.TryGetValue(idNode.Name, out int offset))
                _asm.AppendLine($"   mov rax, [rbp{offset}]");
            else throw new Exception($"Variable '{idNode.Name}' not defined");
        }
        else if (node is BinaryOpNode opNode)
        {
            string? resultType = GetExpressionType(opNode);
            string? leftType = GetExpressionType(opNode.Left);
            string? rightType = GetExpressionType(opNode.Right);
            bool resultIsFloat = resultType == "float" || resultType == "double";

            GenerateExpression(opNode.Left, p);
            if (resultIsFloat && leftType != "float" && leftType != "double" && leftType != null)
            {
                _asm.AppendLine("   cvtsi2sd xmm0, rax");
                _asm.AppendLine("   movq rax, xmm0");
            }
            _asm.AppendLine("   push rax");
            _pushDepth++;

            GenerateExpression(opNode.Right, p);
            if (resultIsFloat && rightType != "float" && rightType != "double" && rightType != null)
            {
                _asm.AppendLine("   cvtsi2sd xmm0, rax");
                _asm.AppendLine("   movq rax, xmm0");
            }
            _asm.AppendLine("   mov rcx, rax");
            _asm.AppendLine("   pop rax");
            _pushDepth--;

            EmitBinaryOp(opNode.Operator, resultType ?? "int");
        }
        else if (node is UnaryOpNode unaryNode)
        {
            GenerateExpression(unaryNode.Right, p);
            if (unaryNode.Operator == "-")
                _asm.AppendLine("   neg rax");
            else if (unaryNode.Operator == "!")
            {
                _asm.AppendLine("   cmp rax, 0");
                _asm.AppendLine("   sete al");
                _asm.AppendLine("   movzx rax, al");
            }
            else if (unaryNode.Operator == "~")
                _asm.AppendLine("   not rax");
        }
        else if (node is ComparisonNode compNode)
        {
            GenerateExpression(compNode.Left, p);
            _asm.AppendLine("   push rax");
            _pushDepth++;
            GenerateExpression(compNode.Right, p);
            _asm.AppendLine("   mov rcx, rax");
            _asm.AppendLine("   pop rax");
            _pushDepth--;
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
            _pushDepth++;
            GenerateExpression(comp.Right, p);
            _asm.AppendLine("   mov rcx, rax");
            _asm.AppendLine("   pop rax");
            _pushDepth--;
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
            _asm.AppendLine($"   ; {comment.Comment}");
    }

    private int GetSizeOfType(string type)
    {
        if (type.StartsWith("*")) return 8;
        if (type == "int") return 8;
        if (type == "long") return 8;
        if (type == "bool") return 8;
        if (type == "char") return 1;
        if (type == "byte" || type == "uint8") return 1;
        if (type == "short") return 2;
        if (type == "float") return 4;
        if (type == "double") return 8;
        if (_structs.ContainsKey(type)) return GetStructSize(type);
        return 8;
    }

    private int GetElementSize(string type)
    {
        if (type.StartsWith("*"))
        {
            string baseType = type.TrimStart('*');
            if (baseType == "char" || baseType == "byte" || baseType == "uint8") return 1;
            if (baseType == "short") return 2;
            if (baseType == "float") return 4;
            if (baseType == "int" || baseType == "long" || baseType == "double" || baseType == "bool") return 8;
            if (_structs.ContainsKey(baseType)) return GetStructSize(baseType);
            return 8;
        }
        return GetSizeOfType(type);
    }

    private int GetStructSize(string structName)
    {
        if (_structs.TryGetValue(structName, out var structNode))
        {
            int size = 0;
            foreach (var field in structNode.Fields)
                size += GetSizeOfType(field.Type);
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
                if (field.Name == fieldName) return offset;
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
        if (node is FloatLiteralNode fl)
            return fl.IsFloat ? "float" : "double";
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
                if (type.StartsWith("*")) return type.Substring(1);
        }
        if (node is DerefExpressionNode deref)
        {
            if (_typeMap.TryGetValue(deref.PointerName, out string? type))
                if (type.StartsWith("*")) return type.Substring(1);
        }
        if (node is BinaryOpNode bin)
        {
            string? lt = GetExpressionType(bin.Left);
            string? rt = GetExpressionType(bin.Right);
            if (lt == "double" || rt == "double") return "double";
            if (lt == "float" || rt == "float") return "float";
            return lt ?? rt;
        }
        return null;
    }
}