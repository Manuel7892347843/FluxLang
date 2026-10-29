# Errors and Exceptions

Flux supports `try`, `catch`, `throw`, and `setErrorCode` syntax for handling exceptional conditions and error codes.

## `try` and `catch`

Place code that may fail inside a `try` block. A `catch` block receives the exception code through its named variable:

```flux
try {
    // Operation that may raise an error.
} catch (error) {
    print(error);
}
```

The caught value is an error code rather than a rich exception object in the current AST representation.

## Throwing an error

```flux
throw(10);
```

The `throw` expression supplies an error code. The code generator searches for an active handler. If no handler exists, it emits an unhandled-exception message and exits through the native runtime path.

## Setting an error code

`setErrorCode` is also recognized by the lexer and parser:

```flux
setErrorCode(5);
```

Use it according to the error-flow convention implemented by your compiler version.

## Error-handling recommendations

- Use meaningful, documented error codes.
- Catch errors near the layer that can recover from them.
- Avoid silently ignoring failures.
- Keep `try` blocks focused.
- Ensure cleanup is performed when an error can bypass normal control flow.

## Important implementation note

Flux's exception handling is implemented by the compiler's generated native code and handler records. It is not automatically identical to C++, C#, or another language's exception semantics. Verify interactions with nested handlers, function calls, and allocated memory in your current build.
