# Contributing Translations

Thank you for your interest in making the **Daily Prayer Timer** accessible to more people! This guide will help you add a new language or improve existing translations.

## Adding a New Language

1.  **Locate the Translation Directory**:
    Navigate to `DailyPrayerTime.Native/i18n/`.

2.  **Create a New JSON File**:
    Copy `en.json` and rename it using the [ISO 639-1 language code](https://en.wikipedia.org/wiki/List_of_ISO_639-1_codes) (e.g., `fr.json` for French, `ar.json` for Arabic).

3.  **Translate the Values**:
    Translate only the **Values** in the JSON pairs. **DO NOT** change the Keys.
    
    *Example:*
    ```json
    "Settings": "Paramètres",
    "Prayer_Fajr": "Fajr",
    ```

4.  **Special Placeholders**:
    Some strings contain placeholders like `{0}` or `{1}`. These are used by the app to insert dynamic data (like prayer names or minutes). Ensure these remain in your translation.
    
    *Example:*
    `"Adhan_Title": "Time for {0}"` -> `"Adhan_Title": "Heure de {0}"`

5.  **Arabic Support**:
    If your language uses a right-to-left (RTL) script, the UI will currently attempt to render it, but please report any layout issues.

6.  **Submit a Pull Request**:
    Once your translation is ready, submit a Pull Request to the repository.

## Coding Standards for Localization

If you are a developer adding new UI elements:

- **Always use DynamicResource**: Never hardcode strings in XAML.
- **Prefix Keys**: Use the `i18n_` prefix in XAML (e.g., `{DynamicResource i18n_MyNewKey}`).
- **Key Naming**: Use `Section_` for headers, `Label_` for text labels, `Btn_` for buttons, and `Msg_` for status/error messages.
- **LocalizationManager**: In C# code, use `LocalizationManager.Instance.GetString("Key")`.

## Verification

Before submitting, run the app and switch to your new language in the **Settings -> Interface Language** dropdown to ensure everything fits within the UI and renders correctly.
