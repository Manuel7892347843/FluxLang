[< Back](../Readme.md)
# Memory (FluxLang)

In Flux, memory can be managed with the `memory_allocation` and `free_memory` functions, because it doesn't have a garbage collector.
These functions allow you to allocate and deallocate memory as you need.
```Flux
ptr p = memory_allocation(10); // Allocate 10 bytes of memory
free_memory(p); // Frees the 10 bytes of memory allocated
```
In the language, these functions can be used to create Pointers, [Array](Arrays.md), [Structs](Structs.md) and every other type of [Variables](Variables.md).