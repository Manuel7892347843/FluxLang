# Types and Variables

A variable stores a value that a program can use and update. Flux declarations use a type followed by a name and, optionally, an initializer.

## Declaring variables

```flux
int score = 0;
double distance = 12.75;
bool active = true;
char letter = 'A';
```

An initializer gives the variable its starting value:

```flux
int lives = 3;
lives = 2;
```

Prefer initializing variables before reading them. The exact behavior of an uninitialized variable can depend on how the compiler allocates its storage.

## Built-in type keywords

The lexer recognizes these type keywords:

| Type | General purpose |
|---|---|
| `int` | Integer values |
| `short` | Short integer values |
| `long` | Long integer values |
| `byte` | Byte-sized values |
| `uint8` | Unsigned 8-bit style values |
| `float` | Single-precision floating-point values |
| `double` | Double-precision floating-point values |
| `char` | Character values |
| `bool` | Boolean values |
| `void` | No return value |
| `ptr` | Pointer-related type keyword |

The parser also supports pointer syntax by placing `*` before a type, such as `int*`. Exact widths and conversion rules should be checked against the target/compiler implementation; do not assume every keyword has identical semantics to C.

## Integer and floating-point literals

Integer literals are written as digits:

```flux
int count = 42;
```

Floating-point literals use a decimal point. An `f` suffix marks a float literal:

```flux
float ratio = 0.5f;
double precise = 0.125;
```

The lexer recognizes decimal numeric forms; use `.` as the decimal separator in source code.

## Boolean and character values

```flux
bool connected = true;
bool finished = false;

char grade = 'A';
```

Boolean literals are `true` and `false`. Character literals are enclosed in single quotes.

## Constants and mutability

The currently visible grammar supports typed variable declarations and assignments, but does not define a dedicated `const` declaration keyword. A normal declared variable can be assigned again:

```flux
int level = 1;
level = level + 1;
```

If a value should remain unchanged, use a naming convention and avoid reassigning it.

## Type aliases

`typedef` introduces an alternate type name:

```flux
typedef int Score;
Score best = 100;
```

Aliases can help make declarations more expressive. See [Structures, Typedefs, and Enums](Structures-Typedefs-and-Enums.md).

## Scope

Variables declared inside a function or block are intended to be local to that region. Use distinct names in separate functions to avoid accidental confusion. Consult the compiler's current scope implementation when relying on nested-block shadowing, because this is an evolving language.
