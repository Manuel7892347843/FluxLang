# FluxLang (Flx-1)

> **Flux** is a compact, C-style imperative programming language that compiles to x86-64 assembly using NASM and links native executables with Clang.

Flux is designed to make low-level programming concepts approachable while keeping a direct path from source code to machine code. Its compiler is built around a hand-written lexer, a recursive-descent parser, an Abstract Syntax Tree (AST), and an assembly code generator.

## Contents

- [What is Flux?](#what-is-flux)
- [Requirements](#requirements)
- [Build and run Flux](#build-and-run-flux)
- [Your first Flux program](#your-first-flux-program)
- [Language overview](#language-overview)
- [Documentation](#documentation)
- [Compiler pipeline](#compiler-pipeline)
- [Current target platforms](#current-target-platforms)

## What is Flux?

Flux is an **imperative, statically typed, C-like language**. Programs are made from statements, expressions, functions, and explicitly typed variables. The language includes control flow, arrays, structures, pointers, manual memory allocation, and basic exception-style error handling.

Flux source files use the `.flx` extension in the examples below. Flux is compiled: the compiler translates the source into NASM assembly, NASM assembles it into an object file, and Clang links the object file into a native executable.

> **Implementation note:** Flux is a developing language. This documentation describes the syntax and facilities represented by the current compiler source. Some advanced behavior can depend on the target platform and compiler implementation.

## Requirements

To build and run Flux, install the following tools and make sure they are available in your `PATH`:

- **.NET SDK 10** — builds the Flux compiler.
- **NASM 3.02 or newer** — assembles generated assembly.
- **Clang / LLVM** — links the generated object file.

Verify the native tools:

```bash
nasm -v
clang --version
dotnet --version
```

## Build and run Flux

Build the compiler from the repository root:

```bash
dotnet build -c Release
```

Create a source file named `hello.flx`:

```flux
void main() {
    print("Hello, Flux!");
}
```

Compile and run it (the executable name shown here is the compiler command in your environment):

```bash
FluxLang -os win64 -file hello.flx -exe hello -dev true
```

The compiler creates intermediate files in an `out` directory beside the source file and places the executable in an `exe` directory.

### Command-line options

| Option | Description | Default |
|---|---|---|
| `-file <path>` | Source file to compile. Required. | — |
| `-exe <name>` | Output executable name. | `program` |
| `-os <target>` | Target: `win64`, `win32`, or `macho64`. | `win64` |
| `-dev <bool>` | Prints compilation progress when set to `true`. | `false` |

Examples:

```bash
# Windows 64-bit
FluxLang -os win64 -file hello.flx -exe hello -dev true

# Windows 32-bit
FluxLang -os win32 -file hello.flx -exe hello

# macOS x86-64
FluxLang -os macho64 -file hello.flx -exe hello
```

## Your first Flux program

```flux
// A small program demonstrating variables, a function, and output.
int add(int left, int right) {
    return left + right;
}

void main() {
    int answer = add(20, 22);
    print("The answer is:");
    print(answer);
}
```

Flux uses braces to group blocks and semicolons to terminate most statements. Functions declare a return type, a name, parameters, and a body. `void` indicates that a function does not return a value.

## Language overview

| Feature | Example |
|---|---|
| Typed variables | `int score = 10;` |
| Arithmetic | `score = score + 1;` |
| Branching | `if (score > 10) { ... } else { ... }` |
| Loops | `while`, `do ... while`, `for` |
| Functions | `int sum(int a, int b) { return a + b; }` |
| Arrays | `int values[3] = {10, 20, 30};` |
| Structures | `struct Player { int health; };` |
| Pointers | `int* address = &score;` |
| Manual memory | `memory_allocation(size)` and `free_memory(pointer);` |
| Error handling | `try { ... } catch (error) { ... }` |
| Modules | `import "path/to/module.flx";` |

See the individual guides for detailed explanations and examples.

## Documentation

All additional Markdown guides are stored in [`Docs/`](Docs/).

- [Getting Started](Flux-Documentation/Docs/Getting-Started.md)
- [Language Fundamentals](Flux-Documentation/Docs/Language-Fundamentals.md)
- [Types and Variables](Flux-Documentation/Docs/Types-and-Variables.md)
- [Operators and Expressions](Flux-Documentation/Docs/Operators-and-Expressions.md)
- [Control Flow](Flux-Documentation/Docs/Control-Flow.md)
- [Functions](Flux-Documentation/Docs/Functions.md)
- [Arrays and Strings](Flux-Documentation/Docs/Arrays-and-Strings.md)
- [Structures, Typedefs, and Enums](Flux-Documentation/Docs/Structures-Typedefs-and-Enums.md)
- [Pointers and Memory](Flux-Documentation/Docs/Pointers-and-Memory.md)
- [Modules and Libraries](Flux-Documentation/Docs/Modules-and-Libraries.md)
- [Input and Output](Flux-Documentation/Docs/Input-and-Output.md)
- [Errors and Exceptions](Flux-Documentation/Docs/Errors-and-Exceptions.md)
- [Built-in Functions](Flux-Documentation/Docs/Built-in-Functions.md)
- [Compiler and Troubleshooting](Flux-Documentation/Docs/Compiler-and-Troubleshooting.md)

## Compiler pipeline

Flux compilation follows these stages:

1. **Lexing** — the lexer converts source text into tokens.
2. **Parsing** — the parser checks grammar and builds an AST.
3. **Code generation** — the AST is translated into NASM assembly.
4. **Assembly** — NASM creates an object file.
5. **Linking** — Clang links the object file and required runtime functions.
6. **Execution** — the resulting native program runs.

The compiler implementation is written in C# under the `FluxLang` namespace.

## Current target platforms

The compiler currently contains target handling for:

- Windows x64 (`win64`)
- Windows x86 (`win32`)
- macOS x86-64 (`macho64`)

Linux ELF is not listed as a supported target by the current driver. The macOS target shown here is x86-64; it should not be assumed to produce an Apple Silicon-native binary.

## Contributing and license

Bug reports, examples, documentation improvements, and compiler contributions are welcome. Please include a minimal source program and target platform when reporting a problem.

Flux's repository README currently identifies the project as MIT-licensed. See the repository's `LICENSE` file for the complete terms.
