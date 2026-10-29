# Operators and Expressions

Expressions combine literals, variables, function calls, and operators to produce values.

## Arithmetic

| Operator | Meaning | Example |
|---|---|---|
| `+` | Addition | `a + b` |
| `-` | Subtraction | `a - b` |
| `*` | Multiplication | `a * b` |
| `/` | Division | `a / b` |
| `%` | Remainder | `a % b` |

```flux
int a = 17;
int b = 5;

int sum = a + b;
int difference = a - b;
int product = a * b;
int remainder = a % b;
```

Be mindful of integer division and division by zero. Numeric behavior can depend on the types involved and the generated native instructions.

## Assignment

```flux
int value = 10;
value = 15;

value += 2;
value -= 1;
value *= 3;
value /= 2;
```

The compiler recognizes compound assignment operators for addition, subtraction, multiplication, and division.

## Increment and decrement

```flux
int index = 0;
index++;
++index;
index--;
--index;
```

Both prefix and postfix forms are recognized. In larger expressions, prefer a separate statement when the precise result of the increment expression matters.

## Comparisons

| Operator | Meaning |
|---|---|
| `==` | Equal |
| `!=` | Not equal |
| `<` | Less than |
| `>` | Greater than |
| `<=` | Less than or equal |
| `>=` | Greater than or equal |

```flux
if (score >= 100) {
    print("Complete");
}
```

## Logical operators

- `&&` — logical AND
- `||` — logical OR
- `!` — logical NOT

```flux
bool canEnter = age >= 18 && hasTicket;
if (!canEnter) {
    print("Access denied");
}
```

## Bitwise operators

Flux recognizes bitwise operators:

| Operator | Meaning |
|---|---|
| `&` | Bitwise AND (also used for address-of in unary position) |
| `|` | Bitwise OR |
| `^` | Bitwise XOR |
| `~` | Bitwise NOT |
| `<<` | Left shift |
| `>>` | Right shift |

```flux
int flags = 1 | 4;
int shifted = flags << 1;
```

The parser distinguishes unary `&variable` (address-of) from binary `left & right` (bitwise AND).

## Operator precedence

The parser groups expressions by precedence, from tighter to looser in broad terms:

1. Primary expressions and function calls.
2. Unary operators.
3. Multiplication, division, and remainder.
4. Addition and subtraction.
5. Comparisons.
6. Logical AND, then logical OR.
7. Bitwise AND, XOR, and OR, plus shifts as handled by the parser.

When an expression is hard to read, use parentheses:

```flux
int result = (a + b) * c;
```

> The parser is hand-written. For expressions involving several operator families, parentheses are the clearest way to state the intended grouping.
