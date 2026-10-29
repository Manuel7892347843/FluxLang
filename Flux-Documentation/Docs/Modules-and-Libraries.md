# Modules and Libraries

Flux supports an `import` statement for including source modules. The parser tracks imported files to avoid repeatedly processing the same file during an import traversal.

## Importing a module

The import keyword is recognized by the lexer. In the current parser implementation, imports are resolved relative to the importing source file's path.

A typical layout might look like this:

```text
project/
├── main.flx
└── Lib/
    └── math.flx
```

A source file can import another Flux file using the import syntax accepted by the parser, for example:

```flux
import "Lib/math.flx";
```

> **Syntax caveat:** The exact import grammar and path handling are determined by the current parser implementation. If this example is rejected by your compiler build, inspect `ParseStatement`'s import branch and follow its exact delimiter/terminator rules.

## Writing a reusable module

A module can expose functions that another source file can call:

```flux
// Lib/math.flx
int add(int a, int b) {
    return a + b;
}
```

Then the main source can import it and call the function:

```flux
// main.flx
import "Lib/math.flx";

void main() {
    print(add(4, 5));
}
```

Ensure imported declarations are visible to the parser/compiler in the way your build expects.

## Source modules versus binary libraries

The import feature is source-file oriented. It should not be confused with a package manager or a general-purpose binary-library loader.

The compiler's generated assembly links against selected native runtime functions through Clang. A Flux source import does not, by itself, mean that an arbitrary external `.lib`, `.dll`, `.so`, or `.dylib` can be imported directly.

## Organizing a project

Suggested conventions:

- Keep reusable functions in focused module files.
- Use relative paths within the project.
- Avoid circular imports.
- Use descriptive module filenames.
- Keep examples and module interfaces documented.

## Import cache

The parser includes an imported-file cache and a method to clear it before a new parse. This prevents repeated inclusion during a compilation run. If you are embedding the parser in another host application, reset the cache at the start of a fresh compilation when appropriate.
