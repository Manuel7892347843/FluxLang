# Compiler Internals and Troubleshooting

This page gives a high-level view of the compiler and lists common setup problems.

## Compiler architecture

Flux's compiler is implemented in C# and uses a straightforward pipeline:

```text
Flux source
    |
    v
Lexer ------> Tokens
    |
    v
Parser -----> Abstract Syntax Tree (AST)
    |
    v
CodeGenerator -> NASM assembly (.asm)
    |
    v
NASM -------> Object file (.o)
    |
    v
Clang ------> Native executable
```

### Lexer

The lexer reads source characters and recognizes tokens such as keywords, identifiers, numeric literals, operators, strings, and punctuation.

### Parser

The parser consumes tokens and builds AST nodes. It uses recursive-descent parsing for statements and expressions.

### Abstract Syntax Tree

The AST represents the structure of the program: declarations, expressions, loops, function calls, arrays, structures, and error-handling blocks.

### Code generator

The code generator translates AST nodes into NASM assembly. It manages stack slots, labels, function calls, data sections, and target-specific calling conventions.

### Native tools

The driver invokes NASM to assemble the generated file and Clang to link the object file into an executable.

## Troubleshooting

### `No file specified`

Pass a source path with `-file`:

```bash
FluxLang -os win64 -file app.flx
```

### `File not found`

Check the spelling and working directory. An absolute path can help identify path issues.

### `nasm` or `clang` cannot be started

Install the tool and ensure its executable is available in `PATH`. Open a new terminal after changing environment variables.

### Assembly or linking fails

- Confirm that the selected `-os` matches the intended target.
- Check the generated `.asm` file in `out/`.
- Verify that NASM and Clang support the target and architecture.
- Keep the complete tool error output when reporting a compiler issue.

### Unexpected syntax error

- Check semicolons and braces.
- Check matching parentheses and brackets.
- Verify type names and literal syntax.
- Review the exact syntax accepted by the current parser.

### Program crashes or prints unexpected values

Pointer misuse, invalid array indices, incorrect allocation sizes, or assumptions about native type widths can cause runtime problems. Reduce the program to a minimal example and check each memory operation.

## Reporting a bug

Include:

1. Flux compiler version or commit.
2. Operating system and target (`win64`, `win32`, or `macho64`).
3. A minimal `.flx` reproduction.
4. The exact command used.
5. The full error output or observed behavior.
