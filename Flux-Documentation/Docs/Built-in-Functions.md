# Built-in Functions

Flux recognizes several built-in operations. Their availability is defined by the compiler's lexer, parser, and code generator.

| Built-in | Purpose |
|---|---|
| `print(value)` | Print a value or string |
| `print_memory(pointer)` | Print memory-related content |
| `input()` | Read console input |
| `input_str(buffer, maxLength)` | Read text into a buffer |
| `memory_allocation(size)` | Allocate memory |
| `free_memory(pointer)` | Release allocated memory |
| `sqrt(value)` | Calculate a square root |
| `time()` | Obtain a time value |
| `sleep(duration)` | Pause execution |
| `rand()` | Generate a pseudo-random value |
| `srand(seed)` | Seed the pseudo-random generator |
| `exit(code)` | Terminate the process |
| `setErrorCode(code)` | Set an error code |
| `throw(code)` | Raise an error code |

## Math example

```flux
double value = sqrt(81.0);
print(value);
```

## Random numbers

Seed the pseudo-random generator before generating a sequence when repeatability is not desired:

```flux
srand(time());
int sample = rand();
print(sample);
```

The exact range and quality of `rand()` follow the native runtime. Do not use it for cryptographic purposes.

## Time and sleep

```flux
int started = time();
sleep(1000);
int finished = time();

print(finished - started);
```

The units and return representation of `time()` and `sleep()` depend on the compiler's native implementation. Confirm them in the relevant code before using them for timing-sensitive work.
