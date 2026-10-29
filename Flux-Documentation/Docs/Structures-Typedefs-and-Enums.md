# Structures, Typedefs, and Enums

Flux provides structure declarations, type aliases, and enumerated constants to organize data and make code easier to understand.

## Structures

A `struct` groups fields under one type:

```flux
struct Player {
    int health;
    int score;
};
```

Structure values are accessed through the pointer-style member operator `->` in the parser:

```flux
Player* player = memory_allocation(16);
// Initialize fields only after a valid Player-sized region is available.
player->health = 100;
player->score = 0;
```

The exact structure size depends on its fields and native layout. Do not hard-code a size unless you have verified the compiler's layout rules.

## `typedef`

`typedef` defines an alternate name for a type:

```flux
typedef int Health;
Health currentHealth = 100;
```

This can communicate intent without introducing a new runtime representation.

## `enum`

An enum associates names with integer values:

```flux
enum Direction {
    North,
    East,
    South,
    West
};
```

By default, enum values are assigned in sequence beginning at zero. A member can be assigned a specific integer value:

```flux
enum Status {
    Ready = 1,
    Running,
    Finished = 10
};
```

The parser and code generator collect enum member constants for use in expressions.

## When to use each feature

- Use a **struct** when several related values belong together.
- Use a **typedef** to make a type's purpose clearer.
- Use an **enum** when a value represents one choice from a finite set.
