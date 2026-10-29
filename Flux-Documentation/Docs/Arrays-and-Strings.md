]# Arrays and Strings

Arrays store a sequence of elements of a declared type. Flux supports fixed-size array declarations, indexed access, and indexed assignment.

## Declaring an array

```flux
int numbers[5];
```

An array can be initialized with values:

```flux
int numbers[3] = {10, 20, 30};
```

The declared size is fixed for this form. Initializer values should fit within the array's declared capacity.

## Reading and writing elements

Array indices are written in square brackets:

```flux
int numbers[3] = {10, 20, 30};

int first = numbers[0];
numbers[1] = 25;

print(numbers[1]);
```

Array indexing is conventionally zero-based: the first element is at index `0`. Access only indices within the valid range (`0` through `size - 1`); do not assume runtime bounds checking is provided.

## Iterating through an array

```flux
int values[4] = {2, 4, 6, 8};

for (int i = 0; i < 4; i++) {
    print(values[i]);
}
```

Keep the loop limit consistent with the declared array length.

## Strings

Flux recognizes string literals enclosed in double quotes:

```flux
print("Flux strings are written like this.");
```

The lexer handles common escapes such as `\n`, `\r`, `\t`, `\\`, and `\"`.

The compiler represents strings through its native code-generation path. Treat string storage and mutability carefully: a string literal is not necessarily a writable character buffer.

## Character arrays and input buffers

For reading text into memory, use an allocated or otherwise valid buffer and the `input_str(buffer, maxLength)` facility. The buffer must have enough space for the requested input and any terminator required by the native implementation.

```flux
// Illustrative only: allocate enough bytes for the input buffer.
char* buffer = memory_allocation(128);
input_str(buffer, 128);
print_memory(buffer);
free_memory(buffer);
```

This example illustrates the API shape. Verify pointer type and string-printing behavior with your current Flux build before relying on it in production.
