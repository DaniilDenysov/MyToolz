# Prototype

A simple prototype (clone) design pattern interface for Unity.

## Dependencies

None.

## Structure

```
Runtime/
└── IPrototype.cs   Interface requiring a Clone() method for deep-copying objects
```

## Usage

Implement `IPrototype<T>` on any class that needs to produce independent copies of itself.
