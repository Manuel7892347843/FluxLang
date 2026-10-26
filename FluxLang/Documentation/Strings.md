[< Back](../Readme.md)
# Strings (FluxLang)

In Flux, strings don't have a specific type, like in C. They are represented as sequences of characters.  
For this reason, to create a string in Flux, you can use: 
```Flux
char str_name[] = "string";
```
Now, with this string you can access its characters through the `str_name[i]` syntax.
```Flux
int i = 0;
char str[] = "string";
while(str[i] != 0) {
    print(str[i]);
    i++;
}
// Output: 
//  s
//  t
//  r
//  i
//  n
//  g
```

In the while loop, it can be seen that the condition, in the while loop, is `str[i] != 0`. The `0` means the end of the string, because it's checking the numeric value of every character in the string. This method is `null-termination`, meaning that the string is terminated by a null character, which has a numeric value of 0.

## Strings as functions arguments
If you want to pass a string as a function argument, in Flux you will have to use the `*char` type.

Example:
```Flux
void welcome(*char name){
    print("Welcome, ");
    print(name);
    print("!");
}

// In main():
// welcome("John");
// Output:
// Welcome, 
// John
// !
```
And, `*char` can be used as a return type.  
Example:
```Flux
*char shift_by_one(*char msg){
    int i = 0;
    while(msg[i] != 0){
    msg[i] = msg[i] + 1;
        i = i + 1;
    }
    return msg;
}

// In main():
// shift_by_one("Hello World!");
// Output: Ifmmp!Xpsme"
```