# FluxLang (Flx-1)

> A small C-style imperative language that compiles to NASM assembly and links with Clang.

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![NASM](https://img.shields.io/badge/NASM-3.02%2B-orange)](https://nasm.us/)
[![Clang](https://img.shields.io/badge/Clang-23.1.4%2B-blue)](https://llvm.org/)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)

Flux is a C-style, imperative language that compiles to x86-64 assembly via [NASM](https://nasm.us/) and is linked with [Clang](https://llvm.org/) (LLVM).
It is intentionally small: a hand-written lexer, a recursive-descent parser, an AST, and a straightforward code generator.

---

## Requirements

In order to build and use Flux you need:

1. **NASM** (v3.02+) installed and in your **PATH**
2. **Clang** (LLVM-23.1.4+) installed and in your **PATH**

Verify with:

```bash
nasm -v
clang --version
```
- Supported targets:

  - Windows win64 / win32

  - macOS macho64

> Linux ELF is not a first-class target yet — see Documentation/Troubleshooting.md for notes.

# Quick start
```bash
# 1. build the compiler
dotnet build -c Release

# 2. write a program
cat > hello.flx <<'EOF'
void main(){
    print("Hello, World!");
}
EOF

# 3. compile and run
FluxLang -os win64 -file hello.flx -exe hello -dev true
```
Output:

```
[1/4] File .../out/hello.asm generated.
[2/4] NASM: Object file .../out/hello.o generated.
[3/4] Clang: Linking completed. Executable .../exe/hello.exe ready.
[4/4] Execution of your program started...

--------------------------------------------
Hello, World!
--------------------------------------------
```

Command-line flags:

| Flag | Meaning | Default |
| :--- | :--- | :--- |
| `-file <path>` | Source file to compile (required) | — |
| `-exe <name>` | Name of the produced executable | `program` |
| `-os <target>` | Target platform: `win64`, `win32`, `macho64` | `win64` |
| `-dev <bool>` | Verbose progress output (`true` / `false`) | `false` |

## Documentation
Full documentation is available in the official [Docs](https://fluxlang.lyvexstudio.com)