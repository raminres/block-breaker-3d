using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Arcade.Core
{
    public enum GameLanguage
    {
        English = 0,
        Turkish = 1
    }

    /// <summary>
    /// Centralized localization manager driven by Unity Localization Tables (com.unity.localization).
    /// Retrieves strings dynamically from ArcadeTable StringTables for English and Turkish,
    /// enabling direct editing via Unity's Localization Tables window and inspector.
    /// Persists language preference in PlayerPrefs and triggers OnLanguageChanged.
    /// </summary>
    public static class LocalizationManager
    {
        private const string PrefKey = "Arcade_Language";
        private static GameLanguage currentLanguage = GameLanguage.English;
        private static bool isInitialized = false;

        private static StringTable enTable;
        private static StringTable trTable;
        private static bool tablesLoaded = false;

        public static event Action OnLanguageChanged;

        public static GameLanguage CurrentLanguage
        {
            get
            {
                EnsureInitialized();
                return currentLanguage;
            }
            set
            {
                SetLanguage(value);
            }
        }

        public static StringTable EnglishTable
        {
            get
            {
                EnsureInitialized();
                return enTable;
            }
        }

        public static StringTable TurkishTable
        {
            get
            {
                EnsureInitialized();
                return trTable;
            }
        }

        public const string TableCollectionName = "ArcadeTable";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeAutoInit()
        {
            EnsureInitialized();
        }

        public static void LoadTables()
        {
            try
            {
                var initOp = LocalizationSettings.InitializationOperation;
                if (initOp.IsValid() && !initOp.IsDone)
                {
                    initOp.WaitForCompletion();
                }

                if (LocalizationSettings.AvailableLocales != null && LocalizationSettings.StringDatabase != null)
                {
                    var enLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
                    var trLocale = LocalizationSettings.AvailableLocales.GetLocale("tr");

                    if (enLocale != null)
                    {
                        enTable = LocalizationSettings.StringDatabase.GetTable(TableCollectionName, enLocale);
                    }

                    if (trLocale != null)
                    {
                        trTable = LocalizationSettings.StringDatabase.GetTable(TableCollectionName, trLocale);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalizationManager] Runtime StringDatabase table load: {ex.Message}");
            }

#if UNITY_EDITOR
            if (enTable == null)
                enTable = UnityEditor.AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/Tables/ArcadeTable_en.asset");
            if (trTable == null)
                trTable = UnityEditor.AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/Tables/ArcadeTable_tr.asset");
#endif

            tablesLoaded = enTable != null && trTable != null;
        }

        public static void SetTables(StringTable englishTable, StringTable turkishTable)
        {
            enTable = englishTable;
            trTable = turkishTable;
            tablesLoaded = enTable != null && trTable != null;
        }

        private static void EnsureInitialized()
        {
            if (!tablesLoaded)
            {
                LoadTables();
            }

            if (isInitialized) return;

            string savedLang = PlayerPrefs.GetString(PrefKey, "English");
            if (Enum.TryParse<GameLanguage>(savedLang, true, out var parsed))
            {
                currentLanguage = parsed;
            }
            else
            {
                currentLanguage = Application.systemLanguage == SystemLanguage.Turkish 
                    ? GameLanguage.Turkish 
                    : GameLanguage.English;
            }

            UpdateSelectedLocale(currentLanguage);

            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;

            isInitialized = true;
        }

        private static void OnSelectedLocaleChanged(Locale locale)
        {
            if (locale == null) return;
            GameLanguage newLang = locale.Identifier.Code.StartsWith("tr", StringComparison.OrdinalIgnoreCase) 
                ? GameLanguage.Turkish 
                : GameLanguage.English;

            if (currentLanguage != newLang)
            {
                currentLanguage = newLang;
                PlayerPrefs.SetString(PrefKey, newLang.ToString());
                PlayerPrefs.Save();
                OnLanguageChanged?.Invoke();
            }
        }

        public static void SetLanguage(GameLanguage language)
        {
            EnsureInitialized();
            if (currentLanguage == language && isInitialized) return;
            currentLanguage = language;
            PlayerPrefs.SetString(PrefKey, language.ToString());
            PlayerPrefs.Save();

            UpdateSelectedLocale(language);

            OnLanguageChanged?.Invoke();
        }

        private static void UpdateSelectedLocale(GameLanguage language)
        {
            try
            {
                string code = language == GameLanguage.Turkish ? "tr" : "en";
                Locale targetLocale = null;

                if (LocalizationSettings.AvailableLocales != null)
                {
                    targetLocale = LocalizationSettings.AvailableLocales.GetLocale(code);
                }

#if UNITY_EDITOR
                if (targetLocale == null)
                {
                    string localePath = language == GameLanguage.Turkish ? "Assets/Localization/Locales/tr.asset" : "Assets/Localization/Locales/en.asset";
                    targetLocale = UnityEditor.AssetDatabase.LoadAssetAtPath<Locale>(localePath);
                }
#endif

                if (targetLocale != null && LocalizationSettings.SelectedLocale != targetLocale)
                {
                    LocalizationSettings.SelectedLocale = targetLocale;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalizationManager] Failed to set SelectedLocale: {ex.Message}");
            }
        }

        public static string Get(string key, string fallback = "")
        {
            EnsureInitialized();

            // 1. Direct query against active StringTable loaded from StringDatabase
            var activeTable = currentLanguage == GameLanguage.Turkish ? trTable : enTable;
            if (activeTable != null)
            {
                var entry = activeTable.GetEntry(key);
                if (entry != null && !string.IsNullOrEmpty(entry.Value))
                {
                    return entry.Value;
                }
            }

            // 2. Query StringDatabase directly by collection name and locale
            if (LocalizationSettings.StringDatabase != null)
            {
                try
                {
                    string code = currentLanguage == GameLanguage.Turkish ? "tr" : "en";
                    var locale = LocalizationSettings.AvailableLocales?.GetLocale(code);
                    if (locale != null)
                    {
                        var localizedStr = LocalizationSettings.StringDatabase.GetLocalizedString(TableCollectionName, key, locale);
                        if (!string.IsNullOrEmpty(localizedStr))
                        {
                            return localizedStr;
                        }
                    }
                }
                catch { }
            }

            return string.IsNullOrEmpty(fallback) ? key : fallback;
        }
    }
}
