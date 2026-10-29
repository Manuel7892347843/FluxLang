# Language Fundamentals

Flux is a **statically typed, imperative language with C-style syntax**.

## Programming model

An imperative program describes operations to perform in sequence. Flux statements can declare variables, assign values, call functions, print output, or control execution using conditions and loops.

```flux
void main() {
    int counter = 0;
    counter = counter + 1;
    print(counter);
}
```

## Static typing

A variable declaration specifies a type. For example, `int` declares an integer variable and `bool` declares a Boolean variable.

```flux
int age = 18;
float temperature = 21.5f;
bool isReady = true;
char initial = 'F';
```

Types communicate how values should be represented and handled by generated code. Flux is not a dynamically typed language where a variable freely changes type at runtime.

## Statements and blocks

A statement generally ends with `;`. Braces `{}` group statements into a block.

```flux
if (age >= 18) {
    print("Adult");
}
```

A comment begins with `//` and continues until the end of that line:

```flux
// This is a comment.
int total = 5;
```

## Identifiers

Names for variables, functions, and other declarations can contain letters, digits, and underscores. The lexer requires the first character of an identifier to be a letter or `_`.

```flux
int player_score = 100;
int _temporary = 0;
```

Names are case-sensitive in the language implementation: `score` and `Score` should be treated as distinct names.

## Program structure

A common program has a `main` function:

```flux
void main() {
    // Program starts here.
}
```

Flux also supports top-level statements. When no `main` function is declared, the compiler can emit an entry-point wrapper for top-level statements.

## Design perspective

Flux exposes several lower-level concepts that are often hidden in higher-level languages:

- Explicit variable types.
- Pointer and address operations.
- Manual allocation and release of memory.
- Native compilation through assembly and a system linker.

These features are useful for learning how programs interact with memory and the native platform. They also mean that correctness and memory safety require attention from the programmer.
