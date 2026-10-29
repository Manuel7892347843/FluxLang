# Control Flow

Control-flow statements decide which statements execute and how often.

## `if` and `else`

```flux
int temperature = 25;

if (temperature > 30) {
    print("Hot");
} else {
    print("Comfortable");
}
```

An `if` statement evaluates a condition and executes its body when the condition is true. `else` provides an alternative block.

## `while`

A `while` loop checks its condition before each iteration:

```flux
int count = 0;

while (count < 3) {
    print(count);
    count++;
}
```

If the condition is initially false, the body does not execute.

## `do ... while`

A `do ... while` loop executes its body first and checks the condition afterward:

```flux
int attempts = 0;

do {
    attempts++;
} while (attempts < 3);
```

This form always executes the body at least once.

## `for`

A `for` loop groups initialization, condition, and iteration:

```flux
for (int i = 0; i < 5; i++) {
    print(i);
}
```

Conceptually, initialization runs once, the condition is checked before each iteration, and the iteration expression runs after the body.

## `switch`, `case`, and `default`

```flux
int option = 2;

switch (option) {
    case 1:
        print("One");
        break;
    case 2:
        print("Two");
        break;
    default:
        print("Other");
        break;
}
```

`break` exits the current loop or switch. Use `default` for values not handled by an explicit case. Confirm fall-through behavior in the compiler version you use; include `break` explicitly to make intent clear.

## `continue`

`continue` skips the remainder of the current loop body and proceeds to the next iteration:

```flux
for (int i = 0; i < 10; i++) {
    if (i == 5) {
        continue;
    }
    print(i);
}
```

Use `break` and `continue` only where a loop or switch context is active.
