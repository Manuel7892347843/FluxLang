# Input and Output

Flux includes facilities for printing values and reading input from the console.

## Printing text

```flux
print("Hello from Flux!");
```

String literals use double quotes. Common escaped characters include:

- `\n` — newline
- `\r` — carriage return
- `\t` — tab
- `\\` — backslash
- `\"` — double quote

## Printing values

```flux
int score = 125;
print(score);
```

The parser also supports expression-printing syntax in the current AST. Prefer the forms that your compiler build accepts and test output for the types you use.

`print_memory` is recognized as a print-related keyword in the lexer. Its behavior is associated with printing memory/pointer content, so use it carefully and only with valid addresses.

## Reading a numeric value

The `input()` expression reads a value from standard input:

```flux
int value = input();
print(value);
```

The runtime implementation determines how input is parsed and what happens when the entered text is not a valid number.

## Reading text

`input_str(buffer, maxLength)` reads text into a supplied buffer:

```flux
char* buffer = memory_allocation(64);
input_str(buffer, 64);

// Work with the buffer while it remains valid.

free_memory(buffer);
```

Provide a valid writable buffer and a maximum length that does not exceed its capacity. Follow the implementation's expected termination convention.

## Exit status

`exit(code)` terminates the process with an exit code:

```flux
exit(0);
```

Conventionally, `0` indicates success and nonzero values indicate an error. The exact native behavior follows the compiler/runtime.
