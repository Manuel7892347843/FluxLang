[< Back](../Readme.md)
# Arrays (FluxLang)

In Flux, arrays are lists of items. Every item in an array must be of the same type.
## Declaration:
To create an array, it's necessary to use the memory_allocation function.
```Flux
type array_name = memory_allocation(size);
```

Example:
```Flux
int array = memory_allocation(16);
```

To assign a value to an index in an array, it's necessary to use `[]` operator.  The index must be a number.
Example:
```Flux
array[0] = 1;
array[1] = 3;
```

To access an item in an array, it's necessary to use `[]` operator and the index, which must be a number.
```Flux
print(array[0]); // Output: 1
print(array[1]); // Output: 3
```

# ALERTS:
## Memory Allocation:
When you create an array and allocate a specific amount of memory, the number of items must be the same as the amount of memory allocated.
```Flux
int array = memory_allocation(16); // Allocates 16 bytes of memory, which corrisponds to 2 items.
array[0] = 1;   // In allocated space
array[1] = 3;   // In allocated space
array[2] = 5;   // Out of allocated space, can overwrite other memory locations.
```
With this information, it's important to be aware of the allocated memory size and not exceed it when assigning values to array indices. Exceeding the allocated memory size can lead to undefined behavior and potential data corruption.
## Functions:
When using an array as a function parameter, it's important to remember that the array is passed as an address.  
This means that any changes made in the function will affect the original array. To have the original array, you have to copy it.