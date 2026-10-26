[< Back](../Readme.md)
# Functions (FluxLang)
In Flux, functions are blocks of code that can be called as your program needs them.

## Syntax
To define a function, it requires the following things:
- Function type (int, char, ptr, void, etc.)
- Function name (standard flux naming convention is snake_case)
- Function eithers takes no parameters or takes parameters
- Function body

```Flux
type function_name(type param1, type param2, ...){
    // Function body
}
```
Example:
```Flux
void talk(*char message){
    print(message);
}

int add(int a, int b){
    return a + b;
}
```
#### *A function can be defined anywhere in the program, before or after the main function.*
To call the defined function, it requires the following things:
- Function name
- Parameters (if any parameters are being asked)
```Flux
void main(){
    talk("Hello World!"); // Output: Hello World!
}
```

If the function has a return type different from void, it will return the value of the function.
```Flux
void main(){
    int result = add(10, 20);
    print(result); // Output: 30
}
```