[< Back](../Readme.md)
# Variables (FluxLang)
In Flux the variables must be declared before they can be used and have a specific type. When a variable has a type, it cannot be changed.

| Type    | Decleared Size | Real Size in memory (Stack/Struct) | Memory_Allocation(size) function |
|:--------|:---------------|:-----------------------------------|:---------------------------------|
| int     | 8 byte         | 8 byte                             | specifed size                    |
| char    | 1 byte (logic) | 8 byte (physical)                  | specifed size                    |
| pointer | 8 byte         | 8 byte                             | specifed size                    |

When a variable is declared with the ` memory_allocation`  function, the value of the variable will be the memory address of the allocated memory.  
Without any further declaration, even if the asterisk is used, it will store the memory address of the allocated memory.  
Instead, if the asterisk is used to assign a new value, it will store the assigned value.

## Declaration:
To declare a variable in flux can be done like this:
```Flux
type var_name = value; 
// Or 
type var_name = memory_allocation(size);
*var_name = value;
```  
## Int:
Integer variables are used to store integer values.  
```Flux
int var_name = numeric_value;
// Example:
int a = 10; 

int a = memory_allocation(8); // Value: memory address
*a = 10; // Value: 10
```  
When an int variable is declared, it allocates eight bytes in memory. But it can change based on the size specified in the `memory_allocation` function.

## Char:
Char variables are used to store single characters.
```Flux
char var_name = 'char';
// Example:
char a = 'c'; 

char a = memory_allocation(8);
*a = 'c'; // Value: 'c'
``` 
When a char variable is declared, it allocates eight bytes in memory. But it can change based on the size specified in the `memory_allocation` function.

## Pointer:
Pointer variables are used to store memory addresses and mixed types.
```Flux
pointer var_name = memory_allocation(8);
// Example:
pointer a = memory_allocation(8); // Value: memory address
*a = 10; // Value: 10
*a = 'c'; // Value: 99 -> ASCII value of 'c'
```
When a pointer variable is declared, it allocates eight bytes in memory. But it can change based on the size specified in the `memory_allocation` function.