# Localization

Package: `Assets/Packages/Localization` · Namespace: `MyToolz.Localization`

CSV-backed, multi-language text. A `LocalizationDatabaseSO` parses a CSV into a
key → per-language table; a `LocalizationManager` holds the current language and broadcasts
changes; `LocalizationText` components (a `TextMeshProUGUI` subclass) re-localize themselves
automatically. Author keys once, bind UI to them, switch languages at runtime.

## The assets

- **`LocalizationLanguageSO`** (`MyToolz/Localization/Language`) — one per language:
  `displayName`, `code` (stable id used to persist the choice and match the CSV — defaults to
  the asset name), an optional `flag` sprite, and an optional `font` (`TMP_FontAsset`, e.g. a
  CJK-capable face swapped in for that language).
- **`LocalizationDatabaseSO`** (`MyToolz/Localization/Database`) — the table. Assign a CSV
  `TextAsset`, an `orientation` (`KeysAsRows` = header names languages, each row a key;
  `LanguagesAsRows` = the transpose), and an **ordered** `languages` list that **maps by order**
  to the CSV's language columns/rows. Editor buttons: *Reload From CSV*, *Fetch Languages From
  CSV*, *Detect Orientation*. Runtime API: `TryTranslate(key, language, out value)`, `Reload()`,
  `Keys`, `Contains`.
- **`LocalizationBindingSO`** (`MyToolz/Localization/Binding`) — a reusable "this key in this
  database" reference (`database` + `key`, picked via the `[LocalizationKey]` dropdown).
  `Resolve(language)` returns the translation (or the key itself if missing).

## The runtime manager

Add **one** `LocalizationManager` component ("MyToolz/Localization/Localization Manager"):

- Assign `database`; optionally `defaultLanguage` and a `languageSetting` (a `StringSettingSO`
  from **MVP Game Settings**) to **persist** the selected language across sessions.
- API: `Translate(key)` / `TryTranslate(key, out value)`, `SetLanguage(language)`,
  `CurrentLanguage`, `Languages`, `Database`. `[Button] Next Language` cycles in play mode.

> ⚠️ `LocalizationManager` is a **`PublicSingleton<LocalizationManager>`** — a documented
> exception to the house rule that flags `PublicSingleton` (see [singleton.md](singleton.md)).
> It exists because non-DI components (like `LocalizationText`) resolve the current language
> statically. Prefer the event/component surface below over reaching for `.Instance` in new code.

## Changing language & reacting to it (events)

```csharp
using MyToolz.Localization;
using MyToolz.DesignPatterns.EventBus;

// Request a change (the manager persists + broadcasts):
EventBus<ChangeLanguageRequest>.Raise(new ChangeLanguageRequest { Language = french });

// React anywhere (e.g. to re-localize non-text assets):
_binding = new EventBinding<LanguageChanged>(e => Refresh(e.Language));
EventBus<LanguageChanged>.Register(_binding);
```

`SetLanguage` (or a `ChangeLanguageRequest`) updates `CurrentLanguage`, persists the `code` via
the wired `StringSettingSO`, and raises `LanguageChanged`.

## Localizing UI text

Use `LocalizationText` instead of a raw `TextMeshProUGUI`. Assign a `LocalizationBindingSO`; it
registers for `LanguageChanged`, resolves its key on enable/change, and (if `applyLanguageFont`)
swaps to the language's font.

For dynamic values, the resolved string is a **composite format template**:

```csharp
// Binding resolves to e.g. "Current score: {0}"
scoreLabel.SetArguments(score);   // renders "Current score: 42"; re-applied on language change
```

`SetArguments` remembers the arguments, so a later language switch re-formats correctly. A
template with no placeholders (or a malformed one) renders verbatim rather than throwing.

For code that needs a string directly: `LocalizationManager.Instance.Translate("key")` (or
`TryTranslate`).

## Setup checklist

1. Author a CSV of keys × languages; import it as a `TextAsset`.
2. Create a `LocalizationLanguageSO` per language; set each `code` to match its CSV column.
3. Create a `LocalizationDatabaseSO`, assign the CSV, set/detect `orientation`, then *Fetch
   Languages From CSV* (order must line up with the CSV).
4. Add a `LocalizationManager`; assign the database (+ optional `StringSettingSO` to persist).
5. Build UI with `LocalizationText` + a `LocalizationBindingSO` per label; use `SetArguments`
   for dynamic values.

## Do / Don't

- ✅ Route every player-facing string through a key + `LocalizationBindingSO` / `LocalizationText`.
- ✅ Use `SetArguments` for templated values instead of concatenating localized fragments
  (word order differs per language).
- ✅ Persist the choice by wiring a `StringSettingSO` into the manager.
- ✅ Give non-Latin languages a `font` on their `LocalizationLanguageSO`.
- ❌ Don't reorder the database `languages` list out of sync with the CSV columns — they map by
  order.
- ❌ Don't hardcode display text or build sentences by string concatenation.
- ❌ Don't add more `PublicSingleton`s modelled on this one — it's a legacy exception, not the
  pattern to copy.
