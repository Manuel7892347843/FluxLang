# Getting Started

This guide walks through setting up the Flux toolchain and compiling a first program.

## 1. Install the toolchain

Flux uses three main tools:

- **.NET SDK 10** to build and run the compiler.
- **NASM** to assemble generated x86-64 assembly.
- **Clang** to link the object file into a native executable.

Check that each command is available:

```bash
dotnet --version
nasm -v
clang --version
```

If a command is not found, install the tool and add its executable directory to your system `PATH`.

## 2. Build the compiler

From the Flux repository directory:

```bash
dotnet build -c Release
```

Use the resulting compiler executable or run it through the appropriate .NET command for your project.

## 3. Create a source file

Create `hello.flx`:

```flux
void main() {
    print("Hello, World!");
}
```

The `main` function is the program entry point when it is declared. Flux can also generate a wrapper entry point for top-level statements if no `main` function is present.

## 4. Compile and execute

```bash
FluxLang -os win64 -file hello.flx -exe hello -dev true
```

The compiler runs the stages of code generation, assembly, linking, and execution. The `-dev true` option prints progress information.

### Output locations

For a source file in a directory, Flux creates:

- `out/<source-name>.asm` — generated NASM assembly.
- `out/<source-name>.o` — assembled object file.
- `exe/<source-name>.exe` on Windows, or an executable without `.exe` for the macOS target.

The `-exe` option is accepted by the driver as the requested executable name; inspect your compiler build if you need to customize output naming further.

## 5. Try a slightly larger program

```flux
int square(int number) {
    return number * number;
}

void main() {
    int value = 7;
    int result = square(value);

    print("Square:");
    print(result);
}
```

## Common command mistakes

- The `-file` option is required.
- The source path must exist.
- `-os` must be one of the targets supported by the compiler driver.
- NASM and Clang must be installed and discoverable by the process.
- Use `-dev true` to see which compilation stage is being reached.
