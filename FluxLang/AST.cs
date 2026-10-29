using System.Collections.Generic;

namespace FluxLang;

public abstract class AstNode {}

public class ProgramNode : AstNode
{
    public List<AstNode> Functions { get; set; }
    public List<AstNode> Statements { get; set; }
    public List<AstNode> Comments { get; set; }

    public ProgramNode()
    {
        Functions = new List<AstNode>();
        Statements = new List<AstNode>();
        Comments = new List<AstNode>();
    }
}

public class ParameterNode : AstNode
{
    public string Type { get; }
    public string Name { get; }
    public ParameterNode(string type, string name) { Type = type; Name = name; }
}

public class FunctionDeclNode : AstNode
{
    public string ReturnType { get; }
    public string Name { get; }
    public List<ParameterNode> Parameters { get; }
    public List<AstNode> Body { get; }
    public FunctionDeclNode(string returnType, string name, List<ParameterNode> parameters, List<AstNode> body)
    { ReturnType = returnType; Name = name; Parameters = parameters; Body = body; }
}

public class ReturnNode : AstNode
{
    public AstNode? Value { get; }
    public ReturnNode(AstNode? value) { Value = value; }
}

public class FunctionCallNode : AstNode
{
    public string FunctionName { get; }
    public List<AstNode> Arguments { get; }
    public FunctionCallNode(string functionName, List<AstNode> arguments)
    { FunctionName = functionName; Arguments = arguments; }
}

public class VarDeclNode : AstNode
{
    public string Type { get; set; }
    public string Name { get; set; }
    public AstNode Initializer { get; set; }
    public int StaticArraySize { get; set; } = 0;
    public List<AstNode>? ArrayInitializers { get; set; }
    public VarDeclNode(string type, string name, AstNode initializer)
    { Type = type; Name = name; Initializer = initializer; }
}

public class AllocNode : AstNode
{
    public AstNode Size { get; set; }
    public AllocNode(AstNode size) { Size = size; }
}

public class FreeNode : AstNode
{
    public string VarName { get; set; }
    public FreeNode(string varName) { VarName = varName; }
}

public class NumberNode : AstNode
{
    public int Value { get; }
    public NumberNode(int value) { Value = value; }
}

public class FloatLiteralNode : AstNode
{
    public double Value { get; }
    public bool IsFloat { get; }
    public FloatLiteralNode(double value, bool isFloat = false) { Value = value; IsFloat = isFloat; }
}

public class IdentifierNode : AstNode
{
    public string Name { get; }
    public IdentifierNode(string name) { Name = name; }
}

public class PrintNode : AstNode
{
    public string VarName { get; }
    public bool IsMemory { get; }
    public PrintNode(string varName, bool isMemory = false) { VarName = varName; IsMemory = isMemory; }
}

public class DerefAssignNode : AstNode
{
    public string PointerName { get; }
    public AstNode Value { get; }
    public DerefAssignNode(string pointerName, AstNode value) { PointerName = pointerName; Value = value; }
}

public class DerefExpressionNode : AstNode
{
    public string PointerName { get; }
    public DerefExpressionNode(string pointerName) { PointerName = pointerName; }
}

public class AddressOfNode : AstNode
{
    public string VarName { get; }
    public AddressOfNode(string varName) { VarName = varName; }
}

public class PrintStrNode : AstNode
{
    public string Text { get; }
    public PrintStrNode(string text) { Text = text; }
}

public class BinaryOpNode : AstNode
{
    public AstNode Left { get; }
    public string Operator { get; }
    public AstNode Right { get; }
    public BinaryOpNode(AstNode left, string op, AstNode right) { Left = left; Operator = op; Right = right; }
}

public class UnaryOpNode : AstNode
{
    public string Operator { get; }
    public AstNode Right { get; }
    public UnaryOpNode(string op, AstNode right) { Operator = op; Right = right; }
}

public class IncrementNode : AstNode
{
    public string VarName { get; }
    public bool IsIncrement { get; }
    public bool IsPrefix { get; }
    public IncrementNode(string varName, bool isIncrement, bool isPrefix)
    { VarName = varName; IsIncrement = isIncrement; IsPrefix = isPrefix; }
}

public class ComparisonNode : AstNode
{
    public AstNode Left { get; }
    public string Operator { get; }
    public AstNode Right { get; }
    public ComparisonNode(AstNode left, string op, AstNode right) { Left = left; Operator = op; Right = right; }
}

public class IfStatementNode : AstNode
{
    public AstNode Condition { get; }
    public List<AstNode> Body { get; }
    public List<AstNode> ElseBody { get; set; }
    public IfStatementNode(AstNode condition, List<AstNode> body, List<AstNode> elseBody)
    { Condition = condition; Body = body; ElseBody = elseBody; }
}

public class SwitchCaseNode : AstNode
{
    public AstNode? CaseValue { get; }
    public List<AstNode> Body { get; }
    public SwitchCaseNode(AstNode? caseValue, List<AstNode> body) { CaseValue = caseValue; Body = body; }
}

public class SwitchNode : AstNode
{
    public AstNode Expression { get; }
    public List<SwitchCaseNode> Cases { get; }
    public SwitchNode(AstNode expression, List<SwitchCaseNode> cases) { Expression = expression; Cases = cases; }
}

public class BreakNode : AstNode { }
public class ContinueNode : AstNode { }

public class CharacterNode : AstNode
{
    public char Value { get; }
    public CharacterNode(char value) { Value = value; }
}

public class ExitNode : AstNode
{
    public AstNode ExitCode { get; }
    public ExitNode(AstNode exitCode) { ExitCode = exitCode; }
}

public class SqrtNode : AstNode
{
    public AstNode Value { get; }
    public SqrtNode(AstNode value) { Value = value; }
}

public class TimeNode : AstNode { public TimeNode() { } }

public class SleepNode : AstNode
{
    public AstNode Duration { get; }
    public SleepNode(AstNode duration) { Duration = duration; }
}

public class RandNode : AstNode { public RandNode() { } }

public class SrandNode : AstNode
{
    public AstNode Seed { get; }
    public SrandNode(AstNode seed) { Seed = seed; }
}

public class AssignmentNode : AstNode
{
    public string Name { get; }
    public AstNode Expression { get; }
    public string? CompoundOp { get; set; }
    public AssignmentNode(string name, AstNode expression) { Name = name; Expression = expression; }
}

public class WhileNode : AstNode
{
    public AstNode Condition { get; }
    public List<AstNode> Body { get; }
    public WhileNode(AstNode condition, List<AstNode> body) { Condition = condition; Body = body; }
}

public class DoWhileNode : AstNode
{
    public AstNode Condition { get; }
    public List<AstNode> Body { get; }
    public DoWhileNode(AstNode condition, List<AstNode> body) { Condition = condition; Body = body; }
}

public class ForNode : AstNode
{
    public AstNode? Initialization { get; }
    public AstNode? Condition { get; }
    public AstNode? Iteration { get; }
    public List<AstNode> Body { get; }
    public ForNode(AstNode? initialization, AstNode? condition, AstNode? iteration, List<AstNode> body)
    { Initialization = initialization; Condition = condition; Iteration = iteration; Body = body; }
}

public class DoubleSlashNode : AstNode
{
    public string Comment { get; }
    public DoubleSlashNode(string comment) { Comment = comment; }
}

public class InputNode : AstNode { }

public class InputStrNode : AstNode
{
    public AstNode Buffer { get; }
    public AstNode MaxLen { get; }
    public InputStrNode(AstNode buffer, AstNode maxLen) { Buffer = buffer; MaxLen = maxLen; }
}

public class ArrayAccessNode : AstNode
{
    public string ArrayName { get; }
    public AstNode Index { get; }
    public ArrayAccessNode(string arrayName, AstNode index) { ArrayName = arrayName; Index = index; }
}

public class ArrayAssignNode : AstNode
{
    public string ArrayName { get; }
    public AstNode Index { get; }
    public AstNode Value { get; }
    public ArrayAssignNode(string arrayName, AstNode index, AstNode value)
    { ArrayName = arrayName; Index = index; Value = value; }
}

public class StringLiteralNode : AstNode
{
    public string Value { get; }
    public StringLiteralNode(string value) { Value = value; }
}

public class PrintExprNode : AstNode
{
    public AstNode Expression { get; }
    public PrintExprNode(AstNode expression) { Expression = expression; }
}

public class StructNode : AstNode
{
    public string Name { get; }
    public List<VarDeclNode> Fields { get; }
    public StructNode(string name, List<VarDeclNode> fields) { Name = name; Fields = fields; }
}

public class StructAccessNode : AstNode
{
    public AstNode StructPtr { get; }
    public string FieldName { get; }
    public StructAccessNode(AstNode structPtr, string fieldName) { StructPtr = structPtr; FieldName = fieldName; }
}

public class StructAssignNode : AstNode
{
    public AstNode StructPtr { get; }
    public string FieldName { get; }
    public AstNode Value { get; }
    public StructAssignNode(AstNode structPtr, string fieldName, AstNode value)
    { StructPtr = structPtr; FieldName = fieldName; Value = value; }
}

public class BoolLiteralNode : AstNode
{
    public bool Value { get; }
    public BoolLiteralNode(bool value) { Value = value; }
}

public class TypedefNode : AstNode
{
    public string OldType { get; }
    public string NewType { get; }
    public TypedefNode(string oldType, string newType) { OldType = oldType; NewType = newType; }
}

public class EnumNode : AstNode
{
    public string Name { get; }
    public List<(string MemberName, int Value)> Members { get; }
    public EnumNode(string name, List<(string, int)> members) { Name = name; Members = members; }
}

public class TryCatchNode : AstNode
{
    public List<AstNode> TryBody { get; }
    public string CatchExceptionVar { get; }
    public List<AstNode> CatchBody { get; }
    public TryCatchNode(List<AstNode> tryBody, string catchExceptionVar, List<AstNode> catchBody)
    { TryBody = tryBody; CatchExceptionVar = catchExceptionVar; CatchBody = catchBody; }
}

public class ThrowNode : AstNode
{
    public AstNode ErrorCodeExpr { get; }
    public ThrowNode(AstNode errorCodeExpr) { ErrorCodeExpr = errorCodeExpr; }
}

public class SetErrorCodeNode : AstNode
{
    public AstNode ErrorCodeExpr { get; }
    public SetErrorCodeNode(AstNode errorCodeExpr) { ErrorCodeExpr = errorCodeExpr; }
}