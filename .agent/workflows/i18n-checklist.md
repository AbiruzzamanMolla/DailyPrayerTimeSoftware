---
description: Localization checklist for Daily Prayer Timer. Run this EVERY TIME a new feature, label, or UI string is added.
---

# 🌐 i18n Localization Checklist

> **MANDATORY**: Every UI string, notification, label, or tooltip added to this app MUST be localized.
> Never hardcode display text in XAML or C#. This app supports multiple languages (English, Bangla, Arabic, and future languages).

---

## 📋 Quick Rule: Any visible text = must be in JSON

| Location         | Rule                                                                 |
|------------------|----------------------------------------------------------------------|
| XAML TextBlock   | `Text="{DynamicResource MyKey}"` — key must exist in ALL json files  |
| XAML ToolTip     | `ToolTip="{DynamicResource MyKey}"`                                  |
| C# code-behind   | `LocalizationManager.Instance.GetString("MyKey")`                    |
| Notification     | `GetString("Notify_MyKey")` — never a raw string                     |
| MessageBox       | `GetString("Msg_MyTitle")` and `GetString("Msg_MyBody")`             |
| Tray menu items  | `GetString("Tray_MyItem")`                                           |
| Date / Time      | Use `GetLocalizedDate()`, `GetLocalizedDayName()`, `FormatCountdown()` helpers |

---

## ✅ Step-by-Step Checklist

### Step 1 — Identify all new text

Before writing any code, list every user-visible string the feature needs:

```
[ ] Button label
[ ] Section header
[ ] Error/success message
[ ] Tooltip
[ ] Notification title
[ ] Notification body
[ ] Placeholder text
[ ] Dynamic runtime string (e.g. countdown, prayer name)
```

### Step 2 — Add keys to `en.json` first

- File: `DailyPrayerTime.Native/i18n/en.json`
- Use the correct naming convention:

| Category        | Key prefix example          |
|-----------------|-----------------------------|
| Buttons         | `Btn_Submit`                |
| Labels          | `Label_TimeLeft`            |
| Sections        | `Section_Nafal`             |
| Notifications   | `Notify_PrayerStarted`      |
| Prayer names    | `Prayer_Fajr`               |
| Messages/Errors | `Msg_SearchFailed`          |
| Titles          | `Title_Error`               |
| Checks/Toggles  | `Check_AutoStart`           |
| Hijri months    | `Month_Hijri_1..12`         |
| Gregorian month | `Month_Gregorian_1..12`     |
| Day names       | `Day_0..6` (0=Sunday)       |
| Status labels   | `Status_Completed`          |
| Unit abbrevs    | `Unit_Hour_Short`           |
| App header text | `Header_*`                  |

### Step 3 — Sync ALL language files

After adding to `en.json`, **immediately** add the same key to:
- `bn.json` — Bangla translation
- Any other language files in `i18n/`

> ⚠️ The app will silently fall back to the raw key string if a translation is missing.
> This causes broken UI — always keep all files in sync.

### Step 4 — Use in XAML or C#

**XAML (DynamicResource — auto-updates on language change):**
```xml
<TextBlock Text="{DynamicResource MyKey}" />
<Button ToolTip="{DynamicResource MyKey_Tooltip}" />
```

**C# (for dynamic/runtime strings):**
```csharp
LocalizationManager.Instance.GetString("MyKey")

// For formatted strings:
string.Format(LocalizationManager.Instance.GetString("Notify_PrayerStarted"), prayerName)
```

**Built-in helpers (use these, don't reinvent):**
```csharp
FormatCountdown(hours, minutes, seconds)  // → "7ঘ 18মি 4স" / "7h 18m 4s"
GetLocalizedDate(DateTime.Now)            // → "10 এপ্রিল 2026" / "10 April 2026"
GetLocalizedDayName(DateTime.Now)         // → "শুক্রবার" / "Friday"
```

### Step 5 — Verify both languages

Run the app, switch to Bangla, and verify:
1. No English text appears where Bangla should show
2. No raw key strings appear (e.g. `Prayer_Fajr` instead of `ফজর`)
3. Layout is not broken by longer translated strings
4. Countdown/date/day shows in correct script

---

## 🔑 LocalizationManager — How it works

Keys are stored in both forms in the ResourceDictionary:
- `Prayer_Fajr` → for XAML `{DynamicResource Prayer_Fajr}` bindings
- `i18n_Prayer_Fajr` → for C# `GetString("Prayer_Fajr")` calls

Both forms are registered automatically when `SetLanguage()` is called.
You only ever define the key **without** the `i18n_` prefix in JSON files.

---

## 🆕 Adding a New Language

See `i18n/CONTRIBUTING_I18N.md` for the full contributor guide.

Short version:
1. Copy `i18n/en.json` → `i18n/xx.json` (use ISO 639-1 code)
2. Translate all values (keep keys exactly the same)
3. Add the language code to the language selector in `SettingsWindow.xaml`
4. Handle any RTL layout adjustments if needed (Arabic, Urdu, etc.)

---

## ❌ Common Mistakes

| Wrong ❌ | Correct ✅ |
|----------|-----------|
| `<TextBlock Text="Fajr"/>` | `<TextBlock Text="{DynamicResource Prayer_Fajr}"/>` |
| `MessageBox.Show("Error!")` | `MessageBox.Show(GetString("Title_Error"))` |
| `DateTime.Now.ToString("dddd")` | `GetLocalizedDayName(DateTime.Now)` |
| `$"{h}h {m}m {s}s"` | `FormatCountdown(h, m, s)` |
| Adding key only to `en.json` | Add to ALL language files simultaneously |
| Using prefix in JSON: `"i18n_Prayer_Fajr"` | Just use `"Prayer_Fajr"` — prefix is auto-added |
