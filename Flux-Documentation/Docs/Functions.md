# Functions

Functions package reusable operations behind a name. A function declaration includes a return type, function name, parameter list, and body.

## Declaring a function

```flux
int multiply(int left, int right) {
    return left * right;
}
```

- `int` is the return type.
- `multiply` is the function name.
- `left` and `right` are typed parameters.
- `return` sends a value back to the caller.

## Calling functions

```flux
void main() {
    int result = multiply(6, 7);
    print(result);
}
```

Arguments are supplied in the same order as the parameters.

## Functions without a return value

Use `void` when a function performs an action but does not return a value:

```flux
void greet() {
    print("Hello!");
}

void main() {
    greet();
}
```

## Returning early

```flux
int absoluteValue(int number) {
    if (number < 0) {
        return 0 - number;
    }
    return number;
}
```

A return statement ends the current function. Make sure every path in a non-void function returns a value.

## Recursion

A function can call itself:

```flux
int factorial(int n) {
    if (n <= 1) {
        return 1;
    }
    return n * factorial(n - 1);
}
```

Recursion uses stack space for each active call. Avoid unbounded recursion, especially when working with large inputs.

## Function design tips

- Give functions one clear responsibility.
- Use descriptive parameter names.
- Prefer small functions that can be tested independently.
- Keep return types and argument types explicit.
