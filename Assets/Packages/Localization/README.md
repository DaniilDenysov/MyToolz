# MyToolz.Localization

ScriptableObject-driven, CSV-backed translation for Unity. Built on the MyToolz **EventBus**, **Singleton**, **DebugUtility**, and **EditorToolz** tools — no Zenject, no external services.

## Pieces

| Type | Kind | Role |
| --- | --- | --- |
| `LocalizationLanguageSO` | asset | One per language. Display name, stable `Code` (for persistence), optional flag sprite, optional TMP font. |
| `LocalizationDatabaseSO` | asset | The single source of truth. References the `.csv` `TextAsset` and the ordered list of languages, parses the file, and maps `key + language → text`. |
| `LocalizationBindingSO` | asset | Binds one CSV key (picked from an always-up-to-date dropdown) to components. Reusable — rename the key once, every user follows. |
| `LocalizationText` | `TextMeshProUGUI` | Renders the binding in the current language and refreshes whenever the language changes. |
| `LocalizationManager` | `PublicSingleton` | Holds the active language, answers `ChangeLanguageRequest`, persists the choice, and raises `LanguageChanged`. |
| `ChangeLanguageRequest` / `LanguageChanged` | `IEvent` | Command in / notification out, both carrying a `LocalizationLanguageSO`. |

`LocalizationDatabaseSO` exists so both the runtime and the editor dropdown read the same parsed data at edit time — a MonoBehaviour manager could not serve the inspector.

## CSV format

First row is the header: the first column is ignored (the key column), the rest are informational language names. Each following row is `key,<value per language...>`. Quote fields that contain commas; escape a quote by doubling it (`""`).

```csv
key,English,Ukrainian,Spanish
play,Play,Грати,Jugar
settings,Settings,Налаштування,Ajustes
greeting,"Hello, friend","Привіт, друже","Hola, amigo"
```

**Column mapping is by order:** `LocalizationDatabaseSO.languages[0]` maps to the first language column, `[1]` to the second, and so on. Keep the list ordered to match the header. The database's **Reload From CSV** button warns when the counts diverge.

### Orientation

The example above is the default, `KeysAsRows`: each **row is a key**, each **column is a language**. The database also reads the transposed layout, `LanguagesAsRows` — each **row is a language**, each **column is a key**:

```csv
language,play,settings,greeting
English,Play,Settings,"Hello, friend"
Ukrainian,Грати,Налаштування,"Привіт, друже"
Spanish,Jugar,Ajustes,"Hola, amigo"
```

Set `LocalizationDatabaseSO.Orientation` to match your file. Everything downstream (lookup, key dropdown, `Fetch Languages`, the manager) is unaffected — parsing normalizes both layouts to the same internal table.

Orientation **auto-detection** (the database's `Detect Orientation` button and the CSV Builder) first reads the header's corner cell — `language`/`languages` means languages-as-rows, `key`/`keys` means keys-as-rows (this is what the Builder writes) — and otherwise falls back to matching cells against known `LocalizationLanguageSO` names. The corner check means detection works even before any language assets exist.

## Authoring CSVs — the CSV Builder

`Tools ▸ MyToolz ▸ Localization ▸ CSV Builder` opens an editor window that builds the CSV from a template so you don't hand-edit commas and quotes:

- **New Template** — starts a grid seeded with the project's `LocalizationLanguageSO` names (or `English`/`Ukrainian`) and a few sample keys.
- **Load / Save** — round-trips a `.csv`. Assign a `Database` and it reads/writes that database's CSV and keeps the database's `Orientation` in sync on save.
- **Swap Orientation** — flips between keys-as-rows and languages-as-rows; the grid is stored orientation-free, so saving simply transposes the file.
- **Create SOs** — one click generates every asset from the CSV: it **re-reads the assigned CSV fresh** (so it never uses a stale grid), then creates/updates the database (referencing the CSV, with orientation and language list filled in), a `LocalizationLanguageSO` per language, and a `LocalizationBindingSO` per key, all cross-wired. Existing matching assets are reused (not duplicated); new ones go under `Languages/` and `Bindings/` in the **Target Folder** (defaults to the CSV's folder).
- **Fetch Languages From Project** — appends any `LocalizationLanguageSO` not already in the grid.
- **Utilities** — Sort Keys, Dedupe Keys, Remove Empty Keys, Fill Empty With Key, Trim All, Clear.

Values are CSV-escaped on save (commas, quotes, and newlines are quoted).

### Encoding & delimiters

- **Delimiter:** parsing auto-detects `,`, `;`, or tab from the header line (Excel writes `;` in many European locales), and a leading BOM is stripped. So a semicolon file is read correctly everywhere, including at runtime.
- **Encoding:** the runtime reads the CSV as a Unity `TextAsset`, which is decoded as **UTF-8** — a file saved as ANSI/Windows-1251 loses its non-ASCII characters (Cyrillic shows as `?`), and this is baked into builds. The **CSV Builder repairs this**: it reads the file's raw bytes (honoring a BOM, and decoding Windows-1251 with a built-in table when the bytes aren't valid UTF-8 — no OS code page needed), so Load recovers the text; **Save** then writes UTF-8 (with BOM) and normalizes the delimiter to `,`.
- **So a 1251 file needs converting once for the runtime to read it:** open it in the Builder, **Load**, **Save** — or in Excel, "Save As ▸ CSV UTF-8". (The Builder's Load/Create SOs read 1251 fine, but the runtime only reads the on-disk file, so it must end up UTF-8.)

## Setup

**Fastest path:** open the **CSV Builder**, assign your `.csv`, press **Load**, then **Create SOs** — that generates the database, languages, and bindings (steps 2–4 below) in one click. Then just place a `LocalizationManager` and `LocalizationText` components (steps 5–6). Manual route:

1. Add your `.csv` to the project (it imports as a `TextAsset`) — or build one with the **CSV Builder** above.
2. Create one `LocalizationLanguageSO` per language (`Create > MyToolz > Localization > Language`).
3. Create a `LocalizationDatabaseSO` (`Create > MyToolz > Localization > Database`); assign the CSV, then press **Fetch Languages From CSV** to populate the language list from the header (each column is matched to a `LocalizationLanguageSO` by `Code`/`DisplayName`/asset name, in column order) — this also back-links each matched language's `database` field to this database. Any unmatched column is logged. **Reload From CSV** just re-parses the keys/values.
4. For each string, create a `LocalizationBindingSO` (`Create > MyToolz > Localization > Binding`); assign the database and pick the key from the dropdown.
5. Put a `LocalizationManager` in the scene, assign the database and a default language. Tick `Dont Destroy On Load` if it should survive scene loads.
6. Add `LocalizationText` to your text objects and assign a binding.

## Changing language

```csharp
// decoupled, via the EventBus
EventBus<ChangeLanguageRequest>.Raise(new ChangeLanguageRequest { Language = ukrainianLanguageSO });

// or directly on the singleton
LocalizationManager.Instance.SetLanguage(ukrainianLanguageSO);
```

Either path updates the manager, persists the selection (PlayerPrefs, keyed by `Code`), and raises `LanguageChanged`; every `LocalizationText` refreshes.

## CSV-backed dropdowns

Two fields are populated from the database's CSV instead of being typed, both re-reading the file each time they open (so they always reflect it) and showing a warning icon when the current value is no longer in the CSV:

- **`LocalizationBindingSO.key`** (`[LocalizationKey]`) — lists the CSV **keys**, sourced from the binding's own `database` field.
- **`LocalizationLanguageSO.code`** (`[LocalizationLanguage]`) — lists the CSV **language names**, so a language's `Code` lines up with a column and `Fetch Languages` / orientation detection match exactly. Sourced from the language's own `database` field; leave that empty to use the single `LocalizationDatabaseSO` in the project.

Both share `LocalizationDropdownDrawer`.
