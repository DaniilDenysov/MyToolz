# Data Structures

A collection of reusable, framework-agnostic generic data structures.

## Dependencies

None.

## Structure

```
Runtime/
└── BiDictionary.cs   Bidirectional dictionary (TKey <-> TValue)
```

## Usage

`BiDictionary<TKey, TValue>` keeps a forward (`TKey -> TValue`) and reverse
(`TValue -> TKey`) mapping in sync, so you can look up either side in O(1).

```csharp
var map = new BiDictionary<int, string>();
map.TryAdd(1, "one");

map.TryGetValue(1, out string name);   // "one"
map.TryGetValue("one", out int id);    // 1

map.Remove("one");                     // removes both directions
```

Insertion is rejected (`TryAdd` returns `false`) if either the key or the value
is already present, guaranteeing the mapping stays one-to-one.
