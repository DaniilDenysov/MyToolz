# Adapter

A lightweight adapter design pattern implementation for Unity. Provides a clean way to access non-serializable references through the Unity Inspector by wrapping them behind a serializable adapter interface.

## Dependencies

None.

## Usage

Implement `IAdapter<T>` on a MonoBehaviour or ScriptableObject to expose a non-serializable reference through an inspector-friendly wrapper. The adapter acts as a bridge between Unity's serialization system and types that cannot be directly serialized.
