[< Back](../Readme.md)
# Structs (FluxLang)

In Flux, structs are used to define custom types. They are useful to define complex types of data and organize related data together.
## Declaration
A struct is declared using the `struct` keyword followed by the name of the struct and a list of fields enclosed in curly braces.

```Flux
struct struct_name{
    // Struct fields
}
```

Example:
```Flux
struct Point{
    int x;
    int y;
}
```

To create an instance of a struct, use the `memory_allocation` function, with the size of the struct as the first argument.
```Flux
Point p = memory_allocation(Point);
```

To access a struct field, use the `->` operator, and select the field needed. With this operation you can read and assign values to the fields.
```Flux
p->x = 10;
p->y = 20;
```