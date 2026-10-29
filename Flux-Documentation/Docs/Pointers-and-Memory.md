# Pointers and Memory

Flux exposes pointers and manual memory management. These features are powerful, but incorrect pointer use can cause invalid memory access, corrupted data, or program crashes.

## Address-of and dereference

The unary `&` operator obtains the address of a variable. The unary `*` operator dereferences a pointer:

```flux
int value = 42;
int* pointer = &value;

*pointer = 50;
print(value);
```

The address must refer to valid storage of the appropriate type. Never dereference an uninitialized, null, freed, or otherwise invalid pointer.

## Dynamic allocation

Flux provides `memory_allocation(size)` for allocating memory and `free_memory(pointer)` for releasing it.

```flux
int* data = memory_allocation(4 * 8);

// Use the allocated region only after checking that allocation succeeded
// according to the behavior of the compiler/runtime in your build.

free_memory(data);
```

The requested size is in bytes. Calculate it carefully based on the number and size of elements. The compiler's current code generator uses native allocation functions, so allocation failure and platform details should be handled conservatively.

## Ownership and lifetime

Manual memory management means the programmer is responsible for the lifetime of allocated memory.

A practical checklist:

1. Allocate enough bytes for the intended data.
2. Keep track of which pointer owns the allocation.
3. Do not access memory after it has been freed.
4. Release each owned allocation once.
5. Avoid losing the only pointer to an allocation.
6. Do not free memory that was not dynamically allocated by the matching allocator.

## Common memory errors

| Error | What can happen |
|---|---|
| Memory leak | Allocated memory is never released |
| Use-after-free | Accessing memory after release |
| Double free | Releasing the same allocation more than once |
| Buffer overflow | Writing beyond the allocated region |
| Invalid dereference | Reading or writing through an invalid pointer |

## Arrays and pointers

Array indexing and pointer arithmetic are low-level operations. Keep the element count and allocated byte count consistent. The current language implementation should not be assumed to provide automatic bounds checks or garbage collection.

## No automatic garbage collection

Flux's documented memory facilities are explicit. Do not assume that dynamically allocated memory will be reclaimed automatically when a pointer goes out of scope.
