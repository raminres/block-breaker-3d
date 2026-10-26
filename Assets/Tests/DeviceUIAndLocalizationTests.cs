using Arcade.Audio;
using Arcade.BlockBreaker;
using Arcade.Core;
using Arcade.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;
using UnityEditor.Localization;

namespace Arcade.Tests
{
    [TestFixture]
    public class DeviceUIAndLocalizationTests
    {
        [SetUp]
        public void SetUp()
        {
            LocalizationManager.SetLanguage(GameLanguage.English);
            ArcadeUIManager.SetInstanceForTesting(null);
        }

        [TearDown]
        public void Teardown()
        {
            LocalizationManager.SetLanguage(GameLanguage.English);
            ArcadeUIManager.SetInstanceForTesting(null);
        }

        [Test]
        public void Test_LocalizationSettings_RuntimeAPI()
        {
            var initOp = LocalizationSettings.InitializationOperation;
            if (!initOp.IsDone)
            {
                initOp.WaitForCompletion();
            }

            Assert.IsNotNull(LocalizationSettings.StringDatabase, "StringDatabase must not be null");
            Assert.IsNotNull(LocalizationSettings.AvailableLocales, "AvailableLocales must not be null");
            
            var enLocale = LocalizationSettings.AvailableLocales.GetLocale("en");
            var trLocale = LocalizationSettings.AvailableLocales.GetLocale("tr");
            Assert.IsNotNull(enLocale, "en locale must be found in AvailableLocales");
            Assert.IsNotNull(trLocale, "tr locale must be found in AvailableLocales");

            var tableEn = LocalizationSettings.StringDatabase.GetTable("ArcadeTable", enLocale);
            Assert.IsNotNull(tableEn, "GetTable('ArcadeTable', enLocale) must not be null");
            Assert.AreEqual("OPTIONS", tableEn.GetEntry("menu_options")?.Value);

            var tableTr = LocalizationSettings.StringDatabase.GetTable("ArcadeTable", trLocale);
            Assert.IsNotNull(tableTr, "GetTable('ArcadeTable', trLocale) must not be null");
            Assert.AreEqual("AYARLAR", tableTr.GetEntry("menu_options")?.Value);

            string strEn = LocalizationSettings.StringDatabase.GetLocalizedString("ArcadeTable", "menu_options", enLocale);
            Assert.AreEqual("OPTIONS", strEn);

            string strTr = LocalizationSettings.StringDatabase.GetLocalizedString("ArcadeTable", "menu_options", trLocale);
            Assert.AreEqual("AYARLAR", strTr);
        }

        [Test]
        public void LocalizationManager_DefaultLanguage_IsEnglish()
        {
            LocalizationManager.SetLanguage(GameLanguage.English);
            Assert.AreEqual(GameLanguage.English, LocalizationManager.CurrentLanguage);
            Assert.AreEqual("OPTIONS", LocalizationManager.Get("menu_options"));
            Assert.AreEqual("TAP TO LAUNCH", LocalizationManager.Get("hud_tap_launch"));
            Assert.AreEqual("SELECT LEVEL", LocalizationManager.Get("level_select_title"));
        }

        [Test]
        public void LocalizationManager_SetLanguage_SwitchesToTurkishAndFiresEvent()
        {
            bool eventFired = false;

            System.Action handler = () =>
            {
                eventFired = true;
            };

            LocalizationManager.OnLanguageChanged += handler;
            LocalizationManager.SetLanguage(GameLanguage.Turkish);
            LocalizationManager.OnLanguageChanged -= handler;

            Assert.IsTrue(eventFired, "OnLanguageChanged event must fire when language changes.");
            Assert.AreEqual(GameLanguage.Turkish, LocalizationManager.CurrentLanguage);

            Assert.AreEqual("AYARLAR", LocalizationManager.Get("menu_options"));
            Assert.AreEqual("FIRLATMAK İÇİN DOKUN", LocalizationManager.Get("hud_tap_launch"));
            Assert.AreEqual("BÖLÜM SEÇ", LocalizationManager.Get("level_select_title"));
            Assert.AreEqual("YAPIMCILAR", LocalizationManager.Get("credits_title"));
        }

        [Test]
        public void LocalizationTables_AssetsExistOnDisk_AndAreConfiguredCorrectly()
        {
            var enLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/en.asset");
            var trLocale = AssetDatabase.LoadAssetAtPath<Locale>("Assets/Localization/Locales/tr.asset");

            Assert.IsNotNull(enLocale, "en.asset must exist in Assets/Localization/Locales/");
            Assert.IsNotNull(trLocale, "tr.asset must exist in Assets/Localization/Locales/");
            Assert.AreEqual("en", enLocale.Identifier.Code);
            Assert.AreEqual("tr", trLocale.Identifier.Code);

            var collection = AssetDatabase.LoadAssetAtPath<StringTableCollection>("Assets/Localization/Tables/ArcadeTable.asset");
            Assert.IsNotNull(collection, "ArcadeTable.asset must exist in Assets/Localization/Tables/");

            var enTable = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/Tables/ArcadeTable_en.asset");
            var trTable = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/Tables/ArcadeTable_tr.asset");

            Assert.IsNotNull(enTable, "ArcadeTable_en.asset must exist in Assets/Localization/Tables/");
            Assert.IsNotNull(trTable, "ArcadeTable_tr.asset must exist in Assets/Localization/Tables/");

            Assert.IsTrue(enTable.Count >= 45, $"English table should have at least 45 entries, has {enTable.Count}");
            Assert.IsTrue(trTable.Count >= 45, $"Turkish table should have at least 45 entries, has {trTable.Count}");
        }

        [Test]
        public void LocalizationManager_ReadsDirectlyFromTable_AndAllowsChangingTranslations()
        {
            LocalizationManager.LoadTables();
            Assert.IsNotNull(LocalizationManager.EnglishTable, "LocalizationManager.EnglishTable must be loaded.");
            Assert.IsNotNull(LocalizationManager.TurkishTable, "LocalizationManager.TurkishTable must be loaded.");

            // Verify table values for English
            LocalizationManager.SetLanguage(GameLanguage.English);
            Assert.AreEqual(LocalizationManager.EnglishTable.GetEntry("menu_options")?.Value, LocalizationManager.Get("menu_options"));

            // Verify table values for Turkish
            LocalizationManager.SetLanguage(GameLanguage.Turkish);
            Assert.AreEqual(LocalizationManager.TurkishTable.GetEntry("menu_options")?.Value, LocalizationManager.Get("menu_options"));

            // Verify that modifying an entry in the Turkish table immediately affects LocalizationManager.Get
            var trEntry = LocalizationManager.TurkishTable.GetEntry("hud_tap_launch");
            Assert.IsNotNull(trEntry);
            string originalValue = trEntry.Value;

            try
            {
                trEntry.Value = "TEST_TURKISH_CUSTOM_LAUNCH";
                Assert.AreEqual("TEST_TURKISH_CUSTOM_LAUNCH", LocalizationManager.Get("hud_tap_launch"),
                    "LocalizationManager.Get must dynamically reflect modifications made to the Turkish StringTable!");
            }
            finally
            {
                trEntry.Value = originalValue;
            }
        }

        [Test]
        public void LocalizationManager_RuntimeStringDatabase_IsAuthoritativeSource()
        {
            var initOp = LocalizationSettings.InitializationOperation;
            if (!initOp.IsDone)
            {
                initOp.WaitForCompletion();
            }

            Assert.IsNotNull(LocalizationSettings.StringDatabase, "LocalizationSettings.StringDatabase must be available at runtime.");
            
            // Set language to English
            LocalizationManager.SetLanguage(GameLanguage.English);
            Assert.AreEqual("en", LocalizationSettings.SelectedLocale.Identifier.Code, "SelectedLocale must synchronize to 'en'.");

            var enTable = LocalizationManager.EnglishTable;
            Assert.IsNotNull(enTable, "English StringTable must be loaded from StringDatabase.");

            var entry = enTable.GetEntry("menu_start_new");
            Assert.IsNotNull(entry);
            string originalValue = entry.Value;

            try
            {
                // Temporarily alter the table entry to a custom runtime value
                entry.Value = "START RUNTIME ADVENTURE";
                string resolved = LocalizationManager.Get("menu_start_new");
                Assert.AreEqual("START RUNTIME ADVENTURE", resolved,
                    "LocalizationManager.Get must read directly from the StringTable!");
            }
            finally
            {
                entry.Value = originalValue;
            }
        }

        [Test]
        public void LocalizationManager_Synchronizes_SelectedLocale_BothWays()
        {
            var initOp = LocalizationSettings.InitializationOperation;
            if (!initOp.IsDone)
            {
                initOp.WaitForCompletion();
            }

            // 1. Manager -> Settings.SelectedLocale
            LocalizationManager.SetLanguage(GameLanguage.Turkish);
            Assert.AreEqual("tr", LocalizationSettings.SelectedLocale.Identifier.Code,
                "Setting language to Turkish in manager must update LocalizationSettings.SelectedLocale to 'tr'.");

            LocalizationManager.SetLanguage(GameLanguage.English);
            Assert.AreEqual("en", LocalizationSettings.SelectedLocale.Identifier.Code,
                "Setting language to English in manager must update LocalizationSettings.SelectedLocale to 'en'.");

            // 2. Settings.SelectedLocale -> Manager
            bool eventFired = false;
            System.Action handler = () => eventFired = true;
            LocalizationManager.OnLanguageChanged += handler;

            var trLocale = LocalizationSettings.AvailableLocales.GetLocale("tr");
            Assert.IsNotNull(trLocale);
            LocalizationSettings.SelectedLocale = trLocale;

            LocalizationManager.OnLanguageChanged -= handler;

            Assert.IsTrue(eventFired, "External change to SelectedLocale must fire LocalizationManager.OnLanguageChanged.");
            Assert.AreEqual(GameLanguage.Turkish, LocalizationManager.CurrentLanguage,
                "External change to SelectedLocale must synchronize LocalizationManager.CurrentLanguage.");
        }

        [Test]
        public void LocalizationManager_RetrievesKeys_DynamicallyAddedToTable()
        {
            var enTable = LocalizationManager.EnglishTable;
            Assert.IsNotNull(enTable);

            string customKey = "test_custom_unique_key_42";
            string customVal = "Custom Table Value 42";

            enTable.AddEntry(customKey, customVal);

            try
            {
                LocalizationManager.SetLanguage(GameLanguage.English);
                string retrieved = LocalizationManager.Get(customKey);
                Assert.AreEqual(customVal, retrieved,
                    "LocalizationManager.Get must retrieve keys created directly in the StringTable.");
            }
            finally
            {
                enTable.RemoveEntry(customKey);
            }
        }

        [Test]
        public void HowToPlaySection_IsLocalized_InBothLanguages_AcrossMainMenuAndHUD()
        {
            // 1. Verify StringTable entries exist for English and Turkish
            LocalizationManager.SetLanguage(GameLanguage.English);
            Assert.AreEqual("HOW TO PLAY", LocalizationManager.Get("guide_title"));
            Assert.AreEqual("1. CONTROLS & LAUNCH", LocalizationManager.Get("guide_h1"));
            Assert.IsTrue(LocalizationManager.Get("guide_p1").Contains("Drag touch or mouse"));
            Assert.AreEqual("4. POWERUPS & HAZARDS", LocalizationManager.Get("guide_h4"));
            Assert.IsTrue(LocalizationManager.Get("guide_p4").Contains("Clutch Countdown"));

            LocalizationManager.SetLanguage(GameLanguage.Turkish);
            Assert.AreEqual("NASIL OYNARIM?", LocalizationManager.Get("guide_title"));
            Assert.AreEqual("1. KONTROLLER VE FIRLATMA", LocalizationManager.Get("guide_h1"));
            Assert.IsTrue(LocalizationManager.Get("guide_p1").Contains("parmağınız") && LocalizationManager.Get("guide_p1").Contains("fareyi"));
            Assert.AreEqual("4. GÜÇLENDİRMELER VE TEHLİKELER", LocalizationManager.Get("guide_h4"));
            Assert.IsTrue(LocalizationManager.Get("guide_p4").Contains("Kritik Geri Sayım"));

            // 2. Verify MainMenuUI.uxml has named elements
            var menuTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/MainMenuUI.uxml");
            Assert.IsNotNull(menuTree);
            var menuRoot = menuTree.CloneTree();
            Assert.IsNotNull(menuRoot.Q<Label>("modal-title-how-to-play"), "modal-title-how-to-play must exist in MainMenuUI.");
            Assert.IsNotNull(menuRoot.Q<Label>("guide-h1"), "guide-h1 must exist in MainMenuUI.");
            Assert.IsNotNull(menuRoot.Q<Label>("guide-p1"), "guide-p1 must exist in MainMenuUI.");
            Assert.IsNotNull(menuRoot.Q<Label>("guide-h4"), "guide-h4 must exist in MainMenuUI.");
            Assert.IsNotNull(menuRoot.Q<Label>("guide-p4"), "guide-p4 must exist in MainMenuUI.");

            // 3. Verify BlockBreakerHUD.uxml has named elements
            var hudTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");
            Assert.IsNotNull(hudTree);
            var hudRoot = hudTree.CloneTree();
            Assert.IsNotNull(hudRoot.Q<Label>("modal-title-how-to-play"), "modal-title-how-to-play must exist in BlockBreakerHUD.");
            Assert.IsNotNull(hudRoot.Q<Label>("guide-h1"), "guide-h1 must exist in BlockBreakerHUD.");
            Assert.IsNotNull(hudRoot.Q<Label>("guide-p1"), "guide-p1 must exist in BlockBreakerHUD.");
            Assert.IsNotNull(hudRoot.Q<Label>("guide-h4"), "guide-h4 must exist in BlockBreakerHUD.");
            Assert.IsNotNull(hudRoot.Q<Label>("guide-p4"), "guide-p4 must exist in BlockBreakerHUD.");
        }

        [Test]
        public void ResponsiveCameraController_TabletAspect_CalculatesDistanceCorrectly()
        {
            var go = new GameObject("TestCamera", typeof(Camera), typeof(ResponsiveCameraController));
            var cam = go.GetComponent<Camera>();
            var controller = go.GetComponent<ResponsiveCameraController>();

            cam.fieldOfView = 38f;

            float width = controller.BoundsSize.x + controller.Padding;
            float height = controller.BoundsSize.y + controller.Padding;

            // iPad Mini 6th gen aspect: 1488 x 2266 ~= 0.6566
            float tabletAspect = 1488f / 2266f;
            float tabletDistance = ResponsiveCameraController.CalculateRequiredDistance(width, height + 7.5f, cam.fieldOfView, tabletAspect);

            // Phone aspect: 1179 x 2556 ~= 0.4612
            float phoneAspect = 1179f / 2556f;
            float phoneDistance = ResponsiveCameraController.CalculateRequiredDistance(width, height, cam.fieldOfView, phoneAspect);

            float unpaddedTabletDistance = ResponsiveCameraController.CalculateRequiredDistance(width, height, cam.fieldOfView, tabletAspect);

            Assert.IsTrue(tabletDistance >= 60f, $"Tablet distance ({tabletDistance}) should pull camera back to allow HUD pods clearance.");
            Assert.IsTrue(tabletDistance > unpaddedTabletDistance, $"Padded tablet distance ({tabletDistance}) must provide more clearance than unpadded tablet distance ({unpaddedTabletDistance}).");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ArcadeAudioManager_MusicControls_SetVolumeAndMuteCorrectly()
        {
            var go = new GameObject("TestAudioManager", typeof(ArcadeAudioManager));
            var audioMgr = go.GetComponent<ArcadeAudioManager>();

            bool volumeChanged = false;
            bool muteChanged = false;

            audioMgr.OnMusicVolumeChanged += _ => volumeChanged = true;
            audioMgr.OnMusicMuteToggled += _ => muteChanged = true;

            audioMgr.MusicVolume = 0.65f;
            Assert.IsTrue(volumeChanged, "OnMusicVolumeChanged should fire.");
            Assert.AreEqual(0.65f, audioMgr.MusicVolume, 0.001f);

            audioMgr.IsMusicMuted = true;
            Assert.IsTrue(muteChanged, "OnMusicMuteToggled should fire.");
            Assert.IsTrue(audioMgr.IsMusicMuted);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SafeAreaController_ProvidesDynamicIslandExtraTopPadding()
        {
            var go = new GameObject("TestSafeArea", typeof(PanelRenderer), typeof(SafeAreaController));
            var controller = go.GetComponent<SafeAreaController>();

            Assert.IsNotNull(controller);
            Assert.AreEqual(4.5f, controller.ExtraTopPercent, "Extra top percent should default to 4.5% for Dynamic Island clearance.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SafeAreaController_CalculateYogaInsets_ComputesWidthRelativePercentagesAccurately()
        {
            // iPhone 15 Pro resolution: 1179 x 2556
            // Dynamic Island cutout: 177px, bottom home indicator: 102px
            float screenW = 1179f;
            float screenH = 2556f;
            float topInset = 177f;
            float bottomInset = 102f;
            Rect safeArea = new Rect(0f, bottomInset, screenW, screenH - (topInset + bottomInset));

            var (left, right, top, bottom) = SafeAreaController.CalculateYogaInsets(safeArea, screenW, screenH, extraSide: 0f, extraTop: 4.5f, extraBottom: 2.5f);

            Assert.AreEqual(0f, left, 0.01f);
            Assert.AreEqual(0f, right, 0.01f);
            Assert.AreEqual((bottomInset / screenW) * 100f + 2.5f, bottom, 0.01f);
            Assert.AreEqual((topInset / screenW) * 100f + 4.5f, top, 0.01f);
        }

        [Test]
        public void BlockBreakerHUD_UXML_WrapsTopBarInSafeAreaContent()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");
            Assert.IsNotNull(uxml, "BlockBreakerHUD.uxml must exist.");

            var root = uxml.Instantiate();
            var safeAreaContent = root.Q("safe-area-content");
            Assert.IsNotNull(safeAreaContent, "BlockBreakerHUD.uxml must contain 'safe-area-content' wrapper.");

            var topBar = safeAreaContent.Q("top-bar");
            Assert.IsNotNull(topBar, "'top-bar' must be inside 'safe-area-content'.");

            var powerupContainer = safeAreaContent.Q("powerup-status-container");
            Assert.IsNotNull(powerupContainer, "'powerup-status-container' must be inside 'safe-area-content'.");

            // Modals must remain outside safe-area-content for 100% full-bleed backdrop coverage
            var pauseModal = root.Q("pause-modal");
            Assert.IsNotNull(pauseModal, "pause-modal must exist in visual tree.");
            Assert.IsNull(safeAreaContent.Q("pause-modal"), "pause-modal must NOT be inside safe-area-content (must remain full bleed).");
        }

        [Test]
        public void BlockBreakerHUD_UXML_DoesNotContainTouchGuideline()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");
            Assert.IsNotNull(uxml, "BlockBreakerHUD.uxml must exist.");

            var root = uxml.Instantiate();
            var guideline = root.Q("touch-guideline");
            Assert.IsNull(guideline, "touch-guideline must not exist in BlockBreakerHUD.uxml.");
        }

        [Test]
        public void ArcadeUIManager_IsPointerOverUI_BottomScreenTouchesDoNotHitUI()
        {
            var uiObj = new GameObject("UI_HUD_Test");
            uiObj.AddComponent<PanelRenderer>();
            var uiMgr = uiObj.AddComponent<ArcadeUIManager>();

            var root = new VisualElement { name = "hud-root" };
            var rootField = typeof(ArcadeUIManager).GetField("root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            rootField.SetValue(uiMgr, root);

            // A touch at the bottom 15% of the screen (e.g. y = 50 in 1080x1920 or 2532x1170)
            // must NOT be intercepted as UI when no modals are active.
            Assert.IsFalse(uiMgr.IsPointerOverUI(new Vector2(Screen.width * 0.5f, 50f)));
            Assert.IsFalse(uiMgr.IsPointerOverUI(new Vector2(100f, 20f)));

            Object.DestroyImmediate(uiObj);
        }

        [Test]
        public void PlayerSettings_iOS_DefersSystemGesturesModeToPreventInputLoss()
        {
            Assert.AreEqual(UnityEngine.iOS.SystemGestureDeferMode.All, UnityEditor.PlayerSettings.iOS.deferSystemGesturesMode,
                "PlayerSettings.iOS.deferSystemGesturesMode must be set to All so iOS does not swallow bottom edge drag inputs.");
        }

        [Test]
        public void CustomIcons_AllRequiredPngAssetsExistOnDisk()
        {
            string[] requiredIcons = new string[]
            {
                "Assets/UI/Icons/TX_Icon_Finger.png",
                "Assets/UI/Icons/TX_Icon_Arrow_Left.png",
                "Assets/UI/Icons/TX_Icon_Arrow_Right.png",
                "Assets/UI/Icons/TX_Flag_GB.png",
                "Assets/UI/Icons/TX_Flag_TR.png",
                "Assets/UI/Icons/TX_Icon_Mail.png",
                "Assets/UI/Icons/TX_Icon_Web.png",
                "Assets/UI/Icons/TX_Icon_LinkedIn.png",
                "Assets/UI/Icons/TX_Icon_GitHub.png"
            };

            foreach (var path in requiredIcons)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.IsNotNull(texture, $"Icon asset must exist and load at: {path}");
            }
        }

        [Test]
        public void BlockBreakerHUD_UXML_ContainsSelectLevelAndCreditsModalsAndButtons()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");
            Assert.IsNotNull(uxml, "BlockBreakerHUD.uxml should exist and load as VisualTreeAsset.");

            var root = uxml.CloneTree();
            var pauseModal = root.Q<VisualElement>("pause-modal");
            Assert.IsNotNull(pauseModal, "pause-modal must exist.");

            var btnLevelSelect = pauseModal.Q<Button>("btn-level-select-pause");
            Assert.IsNotNull(btnLevelSelect, "pause-modal must contain btn-level-select-pause.");

            var btnCredits = pauseModal.Q<Button>("btn-credits-pause");
            Assert.IsNotNull(btnCredits, "pause-modal must contain btn-credits-pause.");

            var howToPlayModal = root.Q<VisualElement>("how-to-play-modal");
            Assert.IsNotNull(howToPlayModal, "how-to-play-modal must exist in HUD.");

            var creditsModal = root.Q<VisualElement>("credits-modal");
            Assert.IsNotNull(creditsModal, "credits-modal must exist in HUD.");

            var levelModal = root.Q<VisualElement>("level-modal");
            Assert.IsNotNull(levelModal, "level-modal must exist in HUD.");
        }

        [Test]
        public void ArcadeUIManager_CreditsAndLevelModals_LifecycleAndVisibility()
        {
            var uiObj = new GameObject("UI_HUD_Test", typeof(PanelRenderer), typeof(ArcadeUIManager));
            var uiMgr = uiObj.GetComponent<ArcadeUIManager>();

            var root = new VisualElement();
            var pauseModal = new VisualElement { name = "pause-modal" };
            var creditsModal = new VisualElement { name = "credits-modal" };
            var levelModal = new VisualElement { name = "level-modal" };
            var howToPlayModal = new VisualElement { name = "how-to-play-modal" };

            pauseModal.AddToClassList("modal-hidden");
            creditsModal.AddToClassList("modal-hidden");
            levelModal.AddToClassList("modal-hidden");
            howToPlayModal.AddToClassList("modal-hidden");

            root.Add(pauseModal);
            root.Add(creditsModal);
            root.Add(levelModal);
            root.Add(howToPlayModal);

            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(ArcadeUIManager).GetField("root", flags).SetValue(uiMgr, root);
            typeof(ArcadeUIManager).GetField("pauseModal", flags).SetValue(uiMgr, pauseModal);
            typeof(ArcadeUIManager).GetField("creditsModal", flags).SetValue(uiMgr, creditsModal);
            typeof(ArcadeUIManager).GetField("levelModal", flags).SetValue(uiMgr, levelModal);
            typeof(ArcadeUIManager).GetField("howToPlayModal", flags).SetValue(uiMgr, howToPlayModal);

            Assert.IsFalse(uiMgr.IsAnyModalVisible());

            // Credits modal
            uiMgr.ShowCredits();
            Assert.IsTrue(uiMgr.IsAnyModalVisible(), "IsAnyModalVisible should be true when credits modal is shown.");
            Assert.IsFalse(creditsModal.ClassListContains("modal-hidden"));
            uiMgr.HideCredits();
            Assert.IsTrue(creditsModal.ClassListContains("modal-hidden"));

            // Level modal
            uiMgr.ShowLevelModal();
            Assert.IsTrue(uiMgr.IsAnyModalVisible(), "IsAnyModalVisible should be true when level modal is shown.");
            Assert.IsFalse(levelModal.ClassListContains("modal-hidden"));
            uiMgr.HideLevelModal();
            Assert.IsTrue(levelModal.ClassListContains("modal-hidden"));

            // How To Play modal
            uiMgr.ShowHowToPlay();
            Assert.IsTrue(uiMgr.IsAnyModalVisible(), "IsAnyModalVisible should be true when how to play modal is shown.");
            Assert.IsFalse(howToPlayModal.ClassListContains("modal-hidden"));
            uiMgr.HideHowToPlay();
            Assert.IsTrue(howToPlayModal.ClassListContains("modal-hidden"));

            Object.DestroyImmediate(uiObj);
        }

        [Test]
        public void OptionsModal_MainMenuAndHUD_ShareIdenticalStructureAndFlagPresentation()
        {
            var menuUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/MainMenuUI.uxml");
            var hudUxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");

            Assert.IsNotNull(menuUxml);
            Assert.IsNotNull(hudUxml);

            var menuRoot = menuUxml.CloneTree();
            var hudRoot = hudUxml.CloneTree();

            var menuOptions = menuRoot.Q<VisualElement>("options-modal");
            var hudOptions = hudRoot.Q<VisualElement>("options-modal");

            Assert.IsNotNull(menuOptions, "MainMenuUI must contain options-modal.");
            Assert.IsNotNull(hudOptions, "BlockBreakerHUD must contain options-modal.");

            // Both must have flags with no text labels (pure flag icons)
            foreach (var root in new[] { menuRoot, hudRoot })
            {
                var enBtn = root.Q<Button>("btn-lang-en");
                var trBtn = root.Q<Button>("btn-lang-tr");

                Assert.IsNotNull(enBtn);
                Assert.IsNotNull(trBtn);

                Assert.IsNotNull(enBtn.Q(className: "flag-gb"));
                Assert.IsNotNull(trBtn.Q(className: "flag-tr"));

                Assert.IsNull(enBtn.Q<Label>(), "EN button should not contain text Label, displaying flag icon only.");
                Assert.IsNull(trBtn.Q<Label>(), "TR button should not contain text Label, displaying flag icon only.");

                // Both must have SFX and Music mute toggles with setting-toggle class
                var sfxToggle = root.Q<Toggle>("toggle-mute");
                var musicToggle = root.Q<Toggle>("toggle-music-mute");

                Assert.IsNotNull(sfxToggle);
                Assert.IsNotNull(musicToggle);
                Assert.IsTrue(sfxToggle.ClassListContains("setting-toggle"));
                Assert.IsTrue(musicToggle.ClassListContains("setting-toggle"));

                // Both must have sliders and fps toggle
                Assert.IsNotNull(root.Q<Slider>("slider-volume"));
                Assert.IsNotNull(root.Q<Slider>("slider-music-volume"));
                Assert.IsNotNull(root.Q<Button>("btn-toggle-fps"));
                Assert.IsNotNull(root.Q<Button>("btn-close-options"));
            }
        }

        [Test]
        public void MainMenuUIManager_And_ArcadeUIManager_UpdateToggleMuteIconCorrectly()
        {
            var menuObj = new GameObject("TestMainMenuMgr", typeof(PanelRenderer), typeof(MainMenuUIManager));
            var menuMgr = menuObj.GetComponent<MainMenuUIManager>();

            var hudObj = new GameObject("TestArcadeUIMgr", typeof(PanelRenderer), typeof(ArcadeUIManager));
            var hudMgr = hudObj.GetComponent<ArcadeUIManager>();

            var toggle = new Toggle();
            var checkmark = toggle.Q(className: "unity-toggle__checkmark");
            if (checkmark == null)
            {
                checkmark = new VisualElement();
                checkmark.AddToClassList("unity-toggle__checkmark");
                toggle.Add(checkmark);
            }

            // Test unmuted state (#21d4fd: r=0.13, g=0.83, b=0.99)
            menuMgr.UpdateToggleMuteIcon(toggle, false);
            Assert.AreEqual(new Color(0.13f, 0.83f, 0.99f, 1f), checkmark.style.unityBackgroundImageTintColor.value);


            // Test muted state (#ff3b56: r=1.0, g=0.231, b=0.337)
            menuMgr.UpdateToggleMuteIcon(toggle, true);
            Assert.AreEqual(new Color(1f, 0.231f, 0.337f, 1f), checkmark.style.unityBackgroundImageTintColor.value);

            // Test ArcadeUIManager unmuted
            hudMgr.UpdateToggleMuteIcon(toggle, false);
            Assert.AreEqual(new Color(0.13f, 0.83f, 0.99f, 1f), checkmark.style.unityBackgroundImageTintColor.value);

            // Test ArcadeUIManager muted
            hudMgr.UpdateToggleMuteIcon(toggle, true);
            Assert.AreEqual(new Color(1f, 0.231f, 0.337f, 1f), checkmark.style.unityBackgroundImageTintColor.value);

            Object.DestroyImmediate(menuObj);
            Object.DestroyImmediate(hudObj);
        }

        [Test]
        public void PauseModal_ExtrudedGradientStyles_AreScopedAndPresent()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");
            Assert.IsNotNull(uxml, "BlockBreakerHUD.uxml must exist.");
            var root = uxml.CloneTree();

            var pauseModal = root.Q<VisualElement>("pause-modal");
            Assert.IsNotNull(pauseModal, "pause-modal must exist.");

            var pauseCard = pauseModal.Q<VisualElement>(className: "pause-card");
            Assert.IsNotNull(pauseCard, "pause-card must exist in pause-modal.");
            Assert.IsTrue(pauseCard.ClassListContains("modal-card"), "pause-card should also have modal-card class.");

            // Verify children buttons exist
            Assert.IsNotNull(pauseCard.Q<Button>("btn-resume"), "btn-resume must exist.");
            Assert.IsNotNull(pauseCard.Q<Button>("btn-level-select-pause"), "btn-level-select-pause must exist.");
            Assert.IsNotNull(pauseCard.Q<Button>("btn-options-pause"), "btn-options-pause must exist.");

            // Verify stylesheet contains scoped extruded 3D gradient rules
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI/BlockBreakerHUD.uss");
            Assert.IsNotNull(uss, "BlockBreakerHUD.uss must exist.");
            var ussText = System.IO.File.ReadAllText("Assets/UI/BlockBreakerHUD.uss");

            Assert.IsTrue(ussText.Contains("#pause-modal.modal-backdrop"), "USS must contain scoped #pause-modal backdrop styling.");
            Assert.IsTrue(ussText.Contains(".pause-card {"), "USS must contain .pause-card styling.");
            Assert.IsTrue(ussText.Contains("rgb(18, 24, 40)"), "pause-card must have solid opaque background.");
            Assert.IsTrue(ussText.Contains("TX_Grad_Card_Bg.png"), "pause-card must use card gradient texture.");
            Assert.IsTrue(ussText.Contains("border-bottom-width: 7px;"), "arcade-btn must have 3D extruded shelf.");
            Assert.IsTrue(ussText.Contains(".pause-card .btn-primary"), "pause-card must have scoped primary button.");
            Assert.IsTrue(ussText.Contains("TX_Grad_Ruby_Btn.png"), "primary button must use ruby gradient texture.");
            Assert.IsTrue(ussText.Contains(".pause-card .btn-secondary"), "pause-card must have scoped secondary button.");
            Assert.IsTrue(ussText.Contains("TX_Grad_Emerald_Btn.png"), "secondary button must use emerald gradient texture.");
            Assert.IsTrue(ussText.Contains(".pause-card .btn-default"), "pause-card must have scoped default button.");
            Assert.IsTrue(ussText.Contains("TX_Grad_Titanium_Btn.png"), "default button must use titanium gradient texture.");

            // Verify other modals do not use pause-card class
            var highscoresModal = root.Q<VisualElement>("highscores-modal");
            Assert.IsNotNull(highscoresModal);
            Assert.IsNull(highscoresModal.Q<VisualElement>(className: "pause-card"), "highscores-modal must NOT use pause-card class.");
        }

        [Test]
        public void Step4_FirstThreeLevels_HaveEnlargedBlockScale_AndValidWallClearance()
        {
            // First 3 levels have +20% block scale (1.20f) for beginner onboarding & touch comfort
            for (int i = 1; i <= 3; i++)
            {
                var config = AssetDatabase.LoadAssetAtPath<LevelConfiguration>($"Assets/Settings/Levels/SO_Level_{i:D2}.asset");
                Assert.IsNotNull(config, $"SO_Level_{i:D2} must exist.");
                Assert.AreEqual(1.20f, config.BlockSize, 0.001f, $"Level {i} must have +20% enlarged block size of 1.20f.");
                
                // Arena walls are at X = +/- 10.25. Base block mesh half-width is 1.15f / 2 = 0.575f.
                float scaledHalfWidth = (1.15f * config.BlockSize) * 0.5f;
                float halfGridSpan = (config.Columns - 1) * config.HorizontalSpacing * 0.5f;
                float maxBlockX = halfGridSpan + scaledHalfWidth;

                // Wall clearance must be strictly greater than 1.0 unit
                float wallX = 10.25f;
                float clearance = wallX - maxBlockX;
                Assert.Greater(clearance, 1.0f, $"Level {i} outer block edge (X={maxBlockX:F2}) must maintain > 1.0u clearance to wall (X={wallX}). Clearance was {clearance:F2}u.");
            }

            // Levels 4 through 15 retain standard 1.0f scale
            for (int i = 4; i <= 15; i++)
            {
                var config = AssetDatabase.LoadAssetAtPath<LevelConfiguration>($"Assets/Settings/Levels/SO_Level_{i:D2}.asset");
                Assert.IsNotNull(config, $"SO_Level_{i:D2} must exist.");
                Assert.AreEqual(1.00f, config.BlockSize, 0.001f, $"Level {i} must retain standard block size of 1.00f.");
            }
        }

        [Test]
        public void Step3_UIPropagation_ModalsAndButtons_HaveExtrudedStylesAndOpaqueGradients()
        {
            // Verify MainMenuUI.uss propagation
            var mainMenuUss = System.IO.File.ReadAllText("Assets/UI/MainMenuUI.uss");
            Assert.IsTrue(mainMenuUss.Contains(".modal-card {"), "MainMenuUI.uss must define .modal-card.");
            Assert.IsTrue(mainMenuUss.Contains(".level-modal-card {"), "MainMenuUI.uss must define .level-modal-card.");
            Assert.IsTrue(mainMenuUss.Contains("TX_Grad_Card_Bg.png"), "MainMenuUI.uss must use card gradient texture.");
            Assert.IsTrue(mainMenuUss.Contains("border-bottom-width: 7px;"), "MainMenuUI.uss arcade-button must have 7px extruded shelf.");
            Assert.IsTrue(mainMenuUss.Contains("TX_Grad_Ruby_Btn.png"), "MainMenuUI.uss must use ruby button gradient.");
            Assert.IsTrue(mainMenuUss.Contains("TX_Grad_Emerald_Btn.png"), "MainMenuUI.uss must use emerald button gradient.");
            Assert.IsTrue(mainMenuUss.Contains("TX_Grad_Titanium_Btn.png"), "MainMenuUI.uss must use titanium button gradient.");

            // Verify BlockBreakerHUD.uss propagation
            var hudUss = System.IO.File.ReadAllText("Assets/UI/BlockBreakerHUD.uss");
            Assert.IsTrue(hudUss.Contains(".modal-card {"), "BlockBreakerHUD.uss must define .modal-card.");
            Assert.IsTrue(hudUss.Contains(".level-modal-card {"), "BlockBreakerHUD.uss must define .level-modal-card.");
            Assert.IsTrue(hudUss.Contains(".scorecard-card {"), "BlockBreakerHUD.uss must define .scorecard-card.");
            Assert.IsTrue(hudUss.Contains(".warning-btn {"), "BlockBreakerHUD.uss must define .warning-btn.");
            Assert.IsTrue(hudUss.Contains("border-bottom-width: 6px;"), "Modals must have 6px bottom beveled border.");
        }

        [Test]
        public void Step5_HowToPlayModal_IsFullscreen_AndProtectedBySafeArea()
        {
            // 1. Verify UXML has safe-area-fullscreen class on how-to-play-card
            var menuTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/MainMenuUI.uxml");
            var menuRoot = menuTree.CloneTree();
            var menuCard = menuRoot.Q<VisualElement>("how-to-play-card");
            Assert.IsNotNull(menuCard, "how-to-play-card must exist in MainMenuUI.");
            Assert.IsTrue(menuCard.ClassListContains("safe-area-fullscreen"), "how-to-play-card must have safe-area-fullscreen class in MainMenuUI.");

            var hudTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/BlockBreakerHUD.uxml");
            var hudRoot = hudTree.CloneTree();
            var hudCard = hudRoot.Q<VisualElement>("how-to-play-card");
            Assert.IsNotNull(hudCard, "how-to-play-card must exist in BlockBreakerHUD.");
            Assert.IsTrue(hudCard.ClassListContains("safe-area-fullscreen"), "how-to-play-card must have safe-area-fullscreen class in BlockBreakerHUD.");

            // 2. Verify USS defines fullscreen dimensions (100% width and height) and flexible scroll container
            var menuUss = System.IO.File.ReadAllText("Assets/UI/MainMenuUI.uss");
            Assert.IsTrue(menuUss.Contains("width: 100%;"), "how-to-play-card must be 100% width.");
            Assert.IsTrue(menuUss.Contains("height: 100%;"), "how-to-play-card must be 100% height.");
            Assert.IsTrue(menuUss.Contains("flex-grow: 1;"), "guide-scroll must have flex-grow: 1.");

            var hudUss = System.IO.File.ReadAllText("Assets/UI/BlockBreakerHUD.uss");
            Assert.IsTrue(hudUss.Contains("width: 100%;"), "how-to-play-card must be 100% width in HUD.");
            Assert.IsTrue(hudUss.Contains("height: 100%;"), "how-to-play-card must be 100% height in HUD.");

            // 3. Verify SafeAreaController applies insets to safe-area-fullscreen elements
            var testObj = new GameObject("SafeAreaTestObj", typeof(PanelRenderer), typeof(SafeAreaController));
            var controller = testObj.GetComponent<SafeAreaController>();
            controller.ForceEditorSimulation = true;

            var testRoot = new VisualElement { name = "root" };
            var htpElem = new VisualElement { name = "how-to-play-card" };
            htpElem.AddToClassList("safe-area-fullscreen");
            testRoot.Add(htpElem);

            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(SafeAreaController).GetField("root", flags).SetValue(controller, testRoot);

            controller.ApplySafeArea();

            Assert.Greater(htpElem.style.paddingTop.value.value, 0f, "SafeAreaController must apply top padding to protect from notch.");
            Assert.Greater(htpElem.style.paddingBottom.value.value, 0f, "SafeAreaController must apply bottom padding to protect from home bar.");
            Assert.Greater(htpElem.style.paddingLeft.value.value, 0f, "SafeAreaController must apply side padding to protect from screen corners.");

            Object.DestroyImmediate(testObj);
        }

        [Test]
        public void Step6_UIAnimationHelper_ScalePopAndButtonCascade_AppliesTransitionsCorrectly()
        {
            // 1. Verify USS files define scale pop and motion classes
            var menuUss = System.IO.File.ReadAllText("Assets/UI/MainMenuUI.uss");
            Assert.IsTrue(menuUss.Contains(".modal-card-hidden"), "MainMenuUI.uss must define .modal-card-hidden.");
            Assert.IsTrue(menuUss.Contains(".modal-card-visible"), "MainMenuUI.uss must define .modal-card-visible.");
            Assert.IsTrue(menuUss.Contains(".modal-backdrop-hidden"), "MainMenuUI.uss must define .modal-backdrop-hidden.");
            Assert.IsTrue(menuUss.Contains(".modal-backdrop-visible"), "MainMenuUI.uss must define .modal-backdrop-visible.");
            Assert.IsTrue(menuUss.Contains(".ui-btn-pop-hidden"), "MainMenuUI.uss must define .ui-btn-pop-hidden.");
            Assert.IsTrue(menuUss.Contains(".ui-btn-pop-visible"), "MainMenuUI.uss must define .ui-btn-pop-visible.");
            Assert.IsTrue(menuUss.Contains(".ui-btn-punched"), "MainMenuUI.uss must define .ui-btn-punched.");

            var hudUss = System.IO.File.ReadAllText("Assets/UI/BlockBreakerHUD.uss");
            Assert.IsTrue(hudUss.Contains(".modal-card-hidden"), "BlockBreakerHUD.uss must define .modal-card-hidden.");
            Assert.IsTrue(hudUss.Contains(".modal-card-visible"), "BlockBreakerHUD.uss must define .modal-card-visible.");
            Assert.IsTrue(hudUss.Contains(".modal-backdrop-hidden"), "BlockBreakerHUD.uss must define .modal-backdrop-hidden.");
            Assert.IsTrue(hudUss.Contains(".modal-backdrop-visible"), "BlockBreakerHUD.uss must define .modal-backdrop-visible.");
            Assert.IsTrue(hudUss.Contains(".ui-btn-pop-hidden"), "BlockBreakerHUD.uss must define .ui-btn-pop-hidden.");
            Assert.IsTrue(hudUss.Contains(".ui-btn-pop-visible"), "BlockBreakerHUD.uss must define .ui-btn-pop-visible.");
            Assert.IsTrue(hudUss.Contains(".ui-btn-punched"), "BlockBreakerHUD.uss must define .ui-btn-punched.");
            Assert.IsTrue(hudUss.Contains(".combo-pop"), "BlockBreakerHUD.uss must define .combo-pop.");
            Assert.IsTrue(hudUss.Contains(".star-pop"), "BlockBreakerHUD.uss must define .star-pop.");
            Assert.IsTrue(hudUss.Contains(".launch-banner-pulse"), "BlockBreakerHUD.uss must define .launch-banner-pulse.");
            Assert.IsTrue(hudUss.Contains(".ui-punch-pop"), "BlockBreakerHUD.uss must define .ui-punch-pop.");

            // 2. Verify UIAnimationHelper CascadeIn and CascadeOut synchronous state application
            var button1 = new Button { name = "btn-1" };
            var button2 = new Button { name = "btn-2" };
            var button3 = new Button { name = "btn-3" };
            var buttons = new System.Collections.Generic.List<VisualElement> { button1, button2, button3 };

            UIAnimationHelper.CascadeIn(null, buttons);
            for (int i = 0; i < buttons.Count; i++)
            {
                Assert.IsTrue(buttons[i].ClassListContains(UIAnimationHelper.ClassBtnPopVisible), $"Button {i} must have ClassBtnPopVisible after CascadeIn.");
                Assert.IsFalse(buttons[i].ClassListContains(UIAnimationHelper.ClassBtnPopHidden), $"Button {i} must not have ClassBtnPopHidden after CascadeIn.");
            }

            UIAnimationHelper.CascadeOut(null, buttons);
            for (int i = 0; i < buttons.Count; i++)
            {
                Assert.IsTrue(buttons[i].ClassListContains(UIAnimationHelper.ClassBtnPopHidden), $"Button {i} must have ClassBtnPopHidden after CascadeOut.");
                Assert.IsFalse(buttons[i].ClassListContains(UIAnimationHelper.ClassBtnPopVisible), $"Button {i} must not have ClassBtnPopVisible after CascadeOut.");
            }

            // 3. Verify UIAnimationHelper ShowModal and HideModal synchronous state application
            var backdrop = new VisualElement { name = "modal-backdrop" };
            var card = new VisualElement { name = "modal-card" };
            backdrop.AddToClassList("modal-hidden");

            UIAnimationHelper.ShowModal(null, backdrop, card, buttons);
            Assert.IsFalse(backdrop.ClassListContains("modal-hidden"), "Backdrop must not have modal-hidden after ShowModal.");
            Assert.IsTrue(backdrop.ClassListContains(UIAnimationHelper.ClassModalBackdropVisible), "Backdrop must have ClassModalBackdropVisible after ShowModal.");
            Assert.IsTrue(card.ClassListContains(UIAnimationHelper.ClassModalCardVisible), "Card must have ClassModalCardVisible after ShowModal.");

            UIAnimationHelper.HideModal(null, backdrop, card);
            Assert.IsTrue(backdrop.ClassListContains("modal-hidden"), "Backdrop must have modal-hidden after HideModal.");

            // 4. Verify ArcadeUIManager button group helpers
            var uiObj = new GameObject("TestArcadeUIMgr", typeof(PanelRenderer), typeof(ArcadeUIManager));
            var uiMgr = uiObj.GetComponent<ArcadeUIManager>();

            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            typeof(ArcadeUIManager).GetField("btnResume", flags).SetValue(uiMgr, button1);
            typeof(ArcadeUIManager).GetField("btnLevelSelectPause", flags).SetValue(uiMgr, button2);
            typeof(ArcadeUIManager).GetField("btnNextLevel", flags).SetValue(uiMgr, button3);

            var pauseBtns = uiMgr.GetPauseButtons();
            Assert.IsNotNull(pauseBtns);
            Assert.IsTrue(pauseBtns.Contains(button1));
            Assert.IsTrue(pauseBtns.Contains(button2));

            var clearBtns = uiMgr.GetLevelClearButtons();
            Assert.IsNotNull(clearBtns);
            Assert.IsTrue(clearBtns.Contains(button3));

            Object.DestroyImmediate(uiObj);
        }

        [Test]
        public void Test_HighScoreManager_FormatShortDate_FormatsToDayAndMonth()
        {
            // Converts legacy yyyy-MM-dd to dd/MM
            Assert.AreEqual("22/10", HighScoreManager.FormatShortDate("2026-10-22"));
            Assert.AreEqual("05/03", HighScoreManager.FormatShortDate("2026-03-05"));

            // Preserves dd/MM format
            Assert.AreEqual("15/08", HighScoreManager.FormatShortDate("15/08"));

            // Handles empty or null strings gracefully
            Assert.AreEqual("", HighScoreManager.FormatShortDate(""));
            Assert.AreEqual("", HighScoreManager.FormatShortDate(null));
        }

        [Test]
        public void Test_HighScoreEntry_DefaultsToDayAndMonthShortDate()
        {
            var entry = new HighScoreEntry(1000, 1);
            Assert.IsNotNull(entry.date);
            Assert.AreEqual(5, entry.date.Length, "Date string should be in dd/MM format (length 5).");
            Assert.AreEqual('/', entry.date[2], "Date string should contain slash separator at index 2.");
        }

        [Test]
        public void Test_ScoreRowDate_USSStyling_HasExpandedMinWidthAndShortDateFormatting()
        {
            var mainMenuUss = System.IO.File.ReadAllText("Assets/UI/MainMenuUI.uss");
            Assert.IsTrue(mainMenuUss.Contains(".score-row-date {"), "MainMenuUI.uss must define .score-row-date.");
            Assert.IsTrue(mainMenuUss.Contains("min-width: 220px;"), "MainMenuUI.uss must define min-width: 220px for .score-row-date.");

            var hudUss = System.IO.File.ReadAllText("Assets/UI/BlockBreakerHUD.uss");
            Assert.IsTrue(hudUss.Contains(".score-row-date {"), "BlockBreakerHUD.uss must define .score-row-date.");
            Assert.IsTrue(hudUss.Contains("min-width: 220px;"), "BlockBreakerHUD.uss must define min-width: 220px for .score-row-date.");
        }

        [Test]
        public void Test_Campaign_AllThirtyLevels_ExistWithValidConfigurationAndProgressiveSpeed()
        {
            float lastSpeed = 0f;
            for (int i = 1; i <= 30; i++)
            {
                string path = $"Assets/Settings/Levels/SO_Level_{i:D2}.asset";
                var config = AssetDatabase.LoadAssetAtPath<LevelConfiguration>(path);
                Assert.IsNotNull(config, $"SO_Level_{i:D2}.asset must exist at {path}.");
                Assert.AreEqual(i, config.LevelNumber, $"Level {i} must have LevelNumber == {i}.");
                Assert.IsFalse(string.IsNullOrEmpty(config.LevelName), $"Level {i} must have non-empty LevelName.");
                Assert.IsFalse(string.IsNullOrEmpty(config.Description), $"Level {i} must have non-empty Description.");
                Assert.Greater(config.TotalBlocks, 0, $"Level {i} must contain active blocks.");

                Assert.Greater(config.BallSpeedMultiplier, lastSpeed, $"Level {i} speed ({config.BallSpeedMultiplier}) must be faster than previous ({lastSpeed}).");
                lastSpeed = config.BallSpeedMultiplier;
            }

            var lvl30 = AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_30.asset");
            Assert.AreEqual(1.80f, lvl30.BallSpeedMultiplier, 0.001f, "Level 30 speed must be 1.80f.");
            Assert.AreEqual(LevelLayoutType.Custom, lvl30.LayoutType, "Level 30 must be Custom layout archetype.");
            int lvl30Hazards = lvl30.PaddleShortenerCount + lvl30.PaddleSlowerCount + lvl30.BrickFreezerCount +
                               lvl30.BallSizeDecreaserCount + lvl30.BallSlowerCount + lvl30.PaddleFreezerCount;
            Assert.AreEqual(20, lvl30Hazards, "Level 30 must feature 20 hazard blocks for the grand master finale.");
        }

        [Test]
        public void Test_Campaign_ThirtyLevels_LevelSelectModalHeightAndScrollViewExpanded()
        {
            var mainMenuUss = System.IO.File.ReadAllText("Assets/UI/MainMenuUI.uss");
            Assert.IsTrue(mainMenuUss.Contains("height: 1850px;"), "MainMenuUI.uss .level-modal-card must have expanded height 1850px.");
            Assert.IsTrue(mainMenuUss.Contains("height: 920px;"), "MainMenuUI.uss .level-grid-scroll-view must have expanded height 920px.");

            var hudUss = System.IO.File.ReadAllText("Assets/UI/BlockBreakerHUD.uss");
            Assert.IsTrue(hudUss.Contains("height: 1850px;"), "BlockBreakerHUD.uss .level-modal-card must have expanded height 1850px.");
            Assert.IsTrue(hudUss.Contains("height: 920px;"), "BlockBreakerHUD.uss .level-grid-scroll-view must have expanded height 920px.");
        }

        [Test]
        public void Test_LevelGenerator_And_MainMenu_SupportThirtyLevels()
        {
            var go = new GameObject("TestLevelGen", typeof(LevelGenerator));
            var gen = go.GetComponent<LevelGenerator>();
            Assert.AreEqual(30, gen.TotalLevels, "LevelGenerator TotalLevels must default to 30.");
            var lvl30 = gen.GetLevelConfig(30);
            Assert.IsNotNull(lvl30, "GetLevelConfig(30) must return Level 30 configuration.");
            Assert.AreEqual(30, lvl30.LevelNumber);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Test_LaunchBanner_VerticalLayout_LargerIcons_And_FingerSwipeAnimation_Configured()
        {
            var hudUxml = System.IO.File.ReadAllText("Assets/UI/BlockBreakerHUD.uxml");
            Assert.IsTrue(hudUxml.Contains("class=\"launch-motion-cue\""), "BlockBreakerHUD.uxml must contain launch-motion-cue container.");
            Assert.IsTrue(hudUxml.Contains("class=\"launch-finger-icon\""), "BlockBreakerHUD.uxml must contain launch-finger-icon.");
            Assert.IsTrue(hudUxml.Contains("class=\"launch-arrow-left\""), "BlockBreakerHUD.uxml must contain launch-arrow-left.");
            Assert.IsTrue(hudUxml.Contains("class=\"launch-arrow-right\""), "BlockBreakerHUD.uxml must contain launch-arrow-right.");
            Assert.IsTrue(hudUxml.Contains("class=\"launch-text\""), "BlockBreakerHUD.uxml must contain launch-text.");

            // Verify text is placed below the icons row in UXML
            int motionCueIndex = hudUxml.IndexOf("launch-motion-cue");
            int launchTextIndex = hudUxml.IndexOf("launch-text");
            Assert.IsTrue(launchTextIndex > motionCueIndex, "launch-text must be positioned after (below) launch-motion-cue.");

            var hudUss = System.IO.File.ReadAllText("Assets/UI/BlockBreakerHUD.uss");
            Assert.IsTrue(hudUss.Contains("flex-direction: column;"), "BlockBreakerHUD.uss .launch-banner must use flex-direction: column.");
            Assert.IsTrue(hudUss.Contains("width: 96px;"), "BlockBreakerHUD.uss .launch-finger-icon must have enlarged width 96px.");
            Assert.IsTrue(hudUss.Contains("height: 96px;"), "BlockBreakerHUD.uss .launch-finger-icon must have enlarged height 96px.");
            Assert.IsTrue(hudUss.Contains("width: 80px;"), "BlockBreakerHUD.uss .launch-arrow-left/.launch-arrow-right must have enlarged width 80px.");
            Assert.IsTrue(hudUss.Contains(".launch-finger-swipe-left"), "BlockBreakerHUD.uss must define .launch-finger-swipe-left.");
            Assert.IsTrue(hudUss.Contains(".launch-finger-swipe-right"), "BlockBreakerHUD.uss must define .launch-finger-swipe-right.");

            // Verify UIAnimationHelper constants and method safety
            Assert.AreEqual("launch-finger-swipe-left", UIAnimationHelper.ClassLaunchFingerLeft);
            Assert.AreEqual("launch-finger-swipe-right", UIAnimationHelper.ClassLaunchFingerRight);
            var finger = new VisualElement();
            var coroutine = UIAnimationHelper.StartHorizontalSwipe(null, finger);
            Assert.IsNull(coroutine, "StartHorizontalSwipe must safely return null when called in EditMode without active host.");
        }

        [Test]
        public void Test_PowerupBadges_CompactDimensions_PreventBlockOcclusionOnTablets()
        {
            var hudUss = System.IO.File.ReadAllText("Assets/UI/BlockBreakerHUD.uss");
            
            // Verify badge height is compacted (<= 64px, down from original 108px)
            Assert.IsTrue(hudUss.Contains("height: 60px;"), "BlockBreakerHUD.uss .powerup-badge must use compact height (60px) to prevent block occlusion on iPad Mini.");
            
            // Verify icon dimensions are compacted (<= 40px, down from original 64px)
            Assert.IsTrue(hudUss.Contains("width: 36px;"), "BlockBreakerHUD.uss .powerup-status-icon must use compact width (36px).");
            Assert.IsTrue(hudUss.Contains("height: 36px;"), "BlockBreakerHUD.uss .powerup-status-icon must use compact height (36px).");

            // Verify status container has wrapping and tightened vertical margins
            Assert.IsTrue(hudUss.Contains("margin-top: 6px;"), ".powerup-status-container must use compact margin-top (6px).");
            Assert.IsTrue(hudUss.Contains("margin-bottom: 6px;"), ".powerup-status-container must use compact margin-bottom (6px).");
            Assert.IsTrue(hudUss.Contains("flex-wrap: wrap;"), ".powerup-status-container must support flex-wrap to prevent overflow.");

            // Verify top bar margin is tightened
            Assert.IsTrue(hudUss.Contains("margin-bottom: 8px;"), ".top-bar must use tightened margin-bottom (8px) to reduce vertical HUD footprint.");
        }

        [Test]
        public void Test_BallSpeed_VisualAndDurationTiers_CalculatesCorrectColorsAndScales()
        {
            var ballObj = new GameObject("TestBallSpeedObj", typeof(Rigidbody), typeof(BallController));
            var ball = ballObj.GetComponent<BallController>();

            // 1. Color tiers
            Color slowColor = ball.GetSpeedColor(9.5f);
            Assert.AreEqual(BallController.ColorSpeedSlow, slowColor, "Speeds < 12 u/s must return Frosted Ice Blue.");

            Color normalColor = ball.GetSpeedColor(12f);
            Assert.AreEqual(BallController.ColorSpeedNormal, normalColor, "Speed 12 u/s must return exact Electric Cyan.");

            Color midSpeedColor = ball.GetSpeedColor(14f);
            Assert.IsTrue(midSpeedColor.g > 0.8f && midSpeedColor.b > 0.5f, "Base speed 14 u/s must return Electric Cyan to Solar Gold blend.");

            Color boostColor = ball.GetSpeedColor(17f);
            Assert.AreEqual(BallController.ColorSpeedBoost, boostColor, "Speed 17 u/s must return exact Solar Gold.");

            Color turboColor = ball.GetSpeedColor(22f);
            Assert.AreEqual(BallController.ColorSpeedTurbo, turboColor, "Speeds >= 20 u/s must return Blazing Plasma Flame.");

            // 2. Trail duration scaling
            float slowDuration = ball.CalculateOuterTrailDuration(9.5f);
            float normalDuration = ball.CalculateOuterTrailDuration(14f);
            float turboDuration = ball.CalculateOuterTrailDuration(22f);

            Assert.Less(slowDuration, normalDuration, "Slow trail duration must be shorter than normal duration.");
            Assert.Less(normalDuration, turboDuration, "Normal trail duration must be shorter than turbo duration.");

            // 3. Event and dynamic visuals
            float recordedSpeed = 0f;
            float recordedMult = 0f;
            ball.OnSpeedChanged += (spd, mult) =>
            {
                recordedSpeed = spd;
                recordedMult = mult;
            };

            ball.SetCurrentSpeedForTesting(21f);
            ball.UpdateDynamicSpeedVisuals();

            Assert.AreEqual(21f, recordedSpeed, 0.01f);
            Assert.AreEqual(21f / ball.BaseSpeed, recordedMult, 0.01f);

            // 4. Custom multi-ball color preservation
            ball.SetTrailColor(Color.magenta);
            Assert.IsTrue(ball.HasCustomTrailColor, "Calling SetTrailColor must flag HasCustomTrailColor.");
            Assert.AreEqual(Color.magenta, ball.TrailColor);

            // Changing speed should NOT stomp custom color
            ball.SetCurrentSpeedForTesting(10f);
            ball.UpdateDynamicSpeedVisuals();
            Assert.AreEqual(Color.magenta, ball.TrailColor, "Custom trail color must be preserved across speed changes.");

            // Resetting custom trail color restores dynamic speed coloring
            ball.ResetCustomTrailColor();
            Assert.IsFalse(ball.HasCustomTrailColor, "ResetCustomTrailColor must clear custom flag.");
            Assert.AreEqual(BallController.ColorSpeedSlow, ball.TrailColor, "Restored trail color must reflect current speed tier.");

            Object.DestroyImmediate(ballObj);
        }

        [Test]
        public void Test_BallSpeed_HUDPodAndUXML_ConfiguredCorrectly()
        {
            var hudUxml = System.IO.File.ReadAllText("Assets/UI/BlockBreakerHUD.uxml");
            Assert.IsTrue(hudUxml.Contains("name=\"speed-container\""), "BlockBreakerHUD.uxml must define speed-container.");
            Assert.IsTrue(hudUxml.Contains("name=\"icon-speed\""), "BlockBreakerHUD.uxml must define icon-speed.");
            Assert.IsTrue(hudUxml.Contains("name=\"speed-label\""), "BlockBreakerHUD.uxml must define speed-label.");

            var hudUss = System.IO.File.ReadAllText("Assets/UI/BlockBreakerHUD.uss");
            Assert.IsTrue(hudUss.Contains(".speed-pod {"), "BlockBreakerHUD.uss must define .speed-pod.");
            Assert.IsTrue(hudUss.Contains(".speed-icon {"), "BlockBreakerHUD.uss must define .speed-icon.");
            Assert.IsTrue(hudUss.Contains(".speed-main {"), "BlockBreakerHUD.uss must define .speed-main.");
            Assert.IsTrue(hudUss.Contains(".speed-tier-slow {"), "BlockBreakerHUD.uss must define .speed-tier-slow.");
            Assert.IsTrue(hudUss.Contains(".speed-tier-normal {"), "BlockBreakerHUD.uss must define .speed-tier-normal.");
            Assert.IsTrue(hudUss.Contains(".speed-tier-boost {"), "BlockBreakerHUD.uss must define .speed-tier-boost.");
            Assert.IsTrue(hudUss.Contains(".speed-tier-turbo {"), "BlockBreakerHUD.uss must define .speed-tier-turbo.");

            // Verify all speed popup CSS classes exist
            Assert.IsTrue(hudUss.Contains(".speed-popup {"), "BlockBreakerHUD.uss must define .speed-popup.");
            Assert.IsTrue(hudUss.Contains(".speed-popup-turbo {"), "BlockBreakerHUD.uss must define .speed-popup-turbo.");
            Assert.IsTrue(hudUss.Contains(".speed-popup-dampen {"), "BlockBreakerHUD.uss must define .speed-popup-dampen.");
            Assert.IsTrue(hudUss.Contains(".speed-popup-strike {"), "BlockBreakerHUD.uss must define .speed-popup-strike.");
            Assert.IsTrue(hudUss.Contains(".speed-popup-slowed {"), "BlockBreakerHUD.uss must define .speed-popup-slowed.");
            Assert.IsTrue(hudUss.Contains(".speed-popup-ramp {"), "BlockBreakerHUD.uss must define .speed-popup-ramp.");
        }

        [Test]
        public void Test_BallSpeed_ArcadeUIManager_UpdateSpeedDisplayAndPopups()
        {
            var uiObj = new GameObject("TestSpeedUI", typeof(PanelRenderer), typeof(ArcadeUIManager));
            var uiMgr = uiObj.GetComponent<ArcadeUIManager>();

            var container = new VisualElement();
            var label = new Label("1.0x");
            var icon = new VisualElement();

            // Reflection to inject mock elements for isolated EditMode testing
            var speedContainerField = typeof(ArcadeUIManager).GetField("speedContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var speedLabelField = typeof(ArcadeUIManager).GetField("speedLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var speedIconField = typeof(ArcadeUIManager).GetField("speedIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            speedContainerField.SetValue(uiMgr, container);
            speedLabelField.SetValue(uiMgr, label);
            speedIconField.SetValue(uiMgr, icon);

            // Tier 1: Slow (<12 u/s)
            uiMgr.UpdateSpeedDisplay(9.5f, 0.68f);
            Assert.AreEqual("0.7x", label.text);
            Assert.IsTrue(container.ClassListContains("speed-tier-slow"));
            Assert.IsFalse(container.ClassListContains("speed-tier-normal"));

            // Tier 2: Normal (14 u/s)
            uiMgr.UpdateSpeedDisplay(14.0f, 1.0f);
            Assert.AreEqual("1.0x", label.text);
            Assert.IsTrue(container.ClassListContains("speed-tier-normal"));
            Assert.IsFalse(container.ClassListContains("speed-tier-slow"));

            // Tier 3: Boost (18.5 u/s)
            uiMgr.UpdateSpeedDisplay(18.5f, 1.32f);
            Assert.AreEqual("1.3x", label.text);
            Assert.IsTrue(container.ClassListContains("speed-tier-boost"));
            Assert.IsFalse(container.ClassListContains("speed-tier-normal"));

            // Tier 4: Turbo (22.0 u/s)
            uiMgr.UpdateSpeedDisplay(22.0f, 1.57f);
            Assert.AreEqual("1.6x", label.text);
            Assert.IsTrue(container.ClassListContains("speed-tier-turbo"));
            Assert.IsFalse(container.ClassListContains("speed-tier-boost"));

            // Test AnimateSpeedPopup executes safely without exceptions
            Assert.DoesNotThrow(() =>
            {
                uiMgr.AnimateSpeedPopup(Vector3.zero, "TURBO +10%", ArcadeUIManager.SpeedPopupType.Turbo);
                uiMgr.AnimateSpeedPopup(Vector3.zero, "DAMPEN -1.2", ArcadeUIManager.SpeedPopupType.Dampen);
                uiMgr.AnimateSpeedPopup(Vector3.zero, "STRIKE! +8%", ArcadeUIManager.SpeedPopupType.ActiveStrike);
            });

            Object.DestroyImmediate(uiObj);
        }

        [Test]
        public void Test_AllLocalizationKeys_ExistInBothEnglishAndTurkishTables()
        {
            var enTable = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/Tables/ArcadeTable_en.asset");
            var trTable = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/Tables/ArcadeTable_tr.asset");

            Assert.IsNotNull(enTable, "ArcadeTable_en must exist.");
            Assert.IsNotNull(trTable, "ArcadeTable_tr must exist.");
            Assert.AreEqual(enTable.Count, trTable.Count, "Both tables must have the exact same number of entries.");
            Assert.IsTrue(enTable.Count >= 70, $"Expected at least 70 localization keys, found {enTable.Count}");

            string[] requiredKeys = new[]
            {
                "hud_best", "hud_balls_count", "hud_combo_count", "hud_combo_ended",
                "banner_clutch", "banner_flawless", "banner_complete",
                "scorecard_title", "scorecard_blocks", "scorecard_combo", "scorecard_time_par", "scorecard_time_bonus",
                "scorecard_flawless_bonus", "scorecard_flawless_val", "scorecard_total_score",
                "btn_replay", "btn_next_level_n", "btn_play_again_loop", "btn_menu",
                "gameover_final_score", "gameover_high_score", "btn_try_again",
                "level_stars", "level_tag_format", "level_settings_title",
                "popup_strike", "popup_dampen", "popup_ball_slowed", "popup_turbo", "popup_speed_up",
                "popup_clutch", "popup_chain", "popup_combo", "popup_multiball", "popup_bomb_chain"
            };

            foreach (var key in requiredKeys)
            {
                var enEntry = enTable.GetEntry(key);
                Assert.IsNotNull(enEntry, $"Key '{key}' must exist in English table.");
                Assert.IsFalse(string.IsNullOrEmpty(enEntry.Value), $"English entry for '{key}' must not be empty.");

                var trEntry = trTable.GetEntry(key);
                Assert.IsNotNull(trEntry, $"Key '{key}' must exist in Turkish table.");
                Assert.IsFalse(string.IsNullOrEmpty(trEntry.Value), $"Turkish entry for '{key}' must not be empty.");
            }
        }

        [Test]
        public void Test_ArcadeUIManager_LocalizesAllModalsAndScorecardElements()
        {
            var uiObj = new GameObject("TestArcadeUIManager", typeof(PanelRenderer), typeof(ArcadeUIManager));
            var uiMgr = uiObj.GetComponent<ArcadeUIManager>();

            var rootVisual = new VisualElement();
            var modalTitlePause = new Label { name = "modal-title-pause" };
            var modalTitleHighscores = new Label { name = "modal-title-highscores" };
            var modalTitleLevelSettings = new Label { name = "modal-title-level-settings" };
            var modalTitleGameover = new Label { name = "modal-title-gameover" };
            var scorecardTitle = new Label { name = "scorecard-title" };
            var labelScorecardBlocks = new Label { name = "label-scorecard-blocks" };
            var labelScorecardCombo = new Label { name = "label-scorecard-combo" };
            var labelScorecardTimePar = new Label { name = "label-scorecard-time-par" };
            var labelScorecardTimeBonus = new Label { name = "label-scorecard-time-bonus" };
            var labelScorecardFlawless = new Label { name = "label-scorecard-flawless" };
            var labelScorecardTotal = new Label { name = "label-scorecard-total" };
            var labelStarsSubtitle = new Label { name = "label-stars-subtitle" };
            var btnReplayLevel = new Button { name = "btn-replay-level" };
            var btnClearMenu = new Button { name = "btn-clear-menu" };
            var btnRetry = new Button { name = "btn-retry" };
            var btnOverMenu = new Button { name = "btn-over-menu" };
            var btnApplyLevel = new Button { name = "btn-apply-level" };
            var scoreLabel = new Label { name = "score-label" };
            var highscoreLabel = new Label { name = "highscore-label" };

            rootVisual.Add(modalTitlePause);
            rootVisual.Add(modalTitleHighscores);
            rootVisual.Add(modalTitleLevelSettings);
            rootVisual.Add(modalTitleGameover);
            rootVisual.Add(scorecardTitle);
            rootVisual.Add(labelScorecardBlocks);
            rootVisual.Add(labelScorecardCombo);
            rootVisual.Add(labelScorecardTimePar);
            rootVisual.Add(labelScorecardTimeBonus);
            rootVisual.Add(labelScorecardFlawless);
            rootVisual.Add(labelScorecardTotal);
            rootVisual.Add(labelStarsSubtitle);
            rootVisual.Add(btnReplayLevel);
            rootVisual.Add(btnClearMenu);
            rootVisual.Add(btnRetry);
            rootVisual.Add(btnOverMenu);
            rootVisual.Add(btnApplyLevel);
            rootVisual.Add(scoreLabel);
            rootVisual.Add(highscoreLabel);

            var rootField = typeof(ArcadeUIManager).GetField("root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            rootField.SetValue(uiMgr, rootVisual);

            var bindMethod = typeof(ArcadeUIManager).GetMethod("BindElements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bindMethod.Invoke(uiMgr, null);

            // Test English - verify elements bind dynamically to localized table values
            LocalizationManager.SetLanguage(GameLanguage.English);
            Assert.AreEqual(LocalizationManager.Get("hud_paused"), uiMgr.ModalTitlePause.text);
            Assert.AreEqual(LocalizationManager.Get("menu_high_scores"), uiMgr.ModalTitleHighscores.text);
            Assert.AreEqual(LocalizationManager.Get("level_settings_title"), uiMgr.ModalTitleLevelSettings.text);
            Assert.AreEqual(LocalizationManager.Get("hud_game_over"), uiMgr.ModalTitleGameover.text);
            Assert.AreEqual(LocalizationManager.Get("scorecard_title"), uiMgr.ScorecardTitle.text);
            Assert.AreEqual(LocalizationManager.Get("scorecard_blocks"), uiMgr.LabelScorecardBlocks.text);
            Assert.AreEqual(LocalizationManager.Get("scorecard_combo"), uiMgr.LabelScorecardCombo.text);
            Assert.AreEqual(LocalizationManager.Get("scorecard_time_par"), uiMgr.LabelScorecardTimePar.text);
            Assert.AreEqual(LocalizationManager.Get("scorecard_time_bonus"), uiMgr.LabelScorecardTimeBonus.text);
            Assert.AreEqual(LocalizationManager.Get("scorecard_flawless_bonus"), uiMgr.LabelScorecardFlawless.text);
            Assert.AreEqual(LocalizationManager.Get("scorecard_total_score"), uiMgr.LabelScorecardTotal.text);
            Assert.AreEqual(LocalizationManager.Get("btn_replay"), uiMgr.BtnReplayLevel.text);
            Assert.AreEqual(LocalizationManager.Get("btn_menu"), uiMgr.BtnClearMenu.text);
            Assert.AreEqual(LocalizationManager.Get("btn_try_again"), uiMgr.BtnRetry.text);

            uiMgr.UpdateScoreDisplay(120, 0);
            Assert.IsTrue(scoreLabel.text.Contains("120"));

            // Test Turkish - verify elements update to Turkish table values without asserting rigid human copy
            LocalizationManager.SetLanguage(GameLanguage.Turkish);
            Assert.AreEqual(LocalizationManager.Get("hud_paused"), uiMgr.ModalTitlePause.text);
            Assert.AreEqual(LocalizationManager.Get("menu_high_scores"), uiMgr.ModalTitleHighscores.text);
            Assert.AreEqual(LocalizationManager.Get("level_settings_title"), uiMgr.ModalTitleLevelSettings.text);
            Assert.AreEqual(LocalizationManager.Get("hud_game_over"), uiMgr.ModalTitleGameover.text);
            Assert.AreEqual(LocalizationManager.Get("scorecard_title"), uiMgr.ScorecardTitle.text);
            Assert.AreEqual(LocalizationManager.Get("scorecard_blocks"), uiMgr.LabelScorecardBlocks.text);
            Assert.AreEqual(LocalizationManager.Get("scorecard_combo"), uiMgr.LabelScorecardCombo.text);
            Assert.AreEqual(LocalizationManager.Get("scorecard_time_par"), uiMgr.LabelScorecardTimePar.text);
            Assert.AreEqual(LocalizationManager.Get("scorecard_time_bonus"), uiMgr.LabelScorecardTimeBonus.text);
            Assert.AreEqual(LocalizationManager.Get("scorecard_flawless_bonus"), uiMgr.LabelScorecardFlawless.text);
            Assert.AreEqual(LocalizationManager.Get("scorecard_total_score"), uiMgr.LabelScorecardTotal.text);
            Assert.AreEqual(LocalizationManager.Get("btn_replay"), uiMgr.BtnReplayLevel.text);
            Assert.AreEqual(LocalizationManager.Get("btn_menu"), uiMgr.BtnClearMenu.text);
            Assert.AreEqual(LocalizationManager.Get("btn_try_again"), uiMgr.BtnRetry.text);

            uiMgr.UpdateScoreDisplay(120, 0);
            Assert.IsTrue(scoreLabel.text.Contains("120"));

            Object.DestroyImmediate(uiObj);
        }

        [Test]
        public void Test_MainMenuUIManager_LocalizesAllElements()
        {
            var menuObj = new GameObject("TestMainMenuUIManager", typeof(PanelRenderer), typeof(MainMenuUIManager));
            var menuMgr = menuObj.GetComponent<MainMenuUIManager>();

            var rootVisual = new VisualElement();
            var menuSubtitle = new Label { name = "menu-subtitle" };
            var modalTitleHighscores = new Label { name = "modal-title-highscores" };
            var labelStarsSubtitle = new Label { name = "label-stars-subtitle" };
            var creditAuthor = new Label { name = "credit-author" };
            var creditFootnote = new Label { name = "credit-footnote" };
            var menuLevelTag = new Label { name = "menu-level-tag" };

            rootVisual.Add(menuSubtitle);
            rootVisual.Add(modalTitleHighscores);
            rootVisual.Add(labelStarsSubtitle);
            rootVisual.Add(creditAuthor);
            rootVisual.Add(creditFootnote);
            rootVisual.Add(menuLevelTag);

            var rootField = typeof(MainMenuUIManager).GetField("root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            rootField.SetValue(menuMgr, rootVisual);

            var bindMethod = typeof(MainMenuUIManager).GetMethod("BindElements", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bindMethod.Invoke(menuMgr, null);

            // Test English
            LocalizationManager.SetLanguage(GameLanguage.English);
            Assert.AreEqual(LocalizationManager.Get("menu_subtitle"), menuSubtitle.text);
            Assert.AreEqual(LocalizationManager.Get("menu_high_scores"), modalTitleHighscores.text);
            Assert.AreEqual(LocalizationManager.Get("level_stars"), labelStarsSubtitle.text);
            Assert.AreEqual(LocalizationManager.Get("credits_dev_by"), creditAuthor.text);
            Assert.IsTrue(menuLevelTag.text.Contains("1"));

            // Test Turkish
            LocalizationManager.SetLanguage(GameLanguage.Turkish);
            Assert.AreEqual(LocalizationManager.Get("menu_subtitle"), menuSubtitle.text);
            Assert.AreEqual(LocalizationManager.Get("menu_high_scores"), modalTitleHighscores.text);
            Assert.AreEqual(LocalizationManager.Get("level_stars"), labelStarsSubtitle.text);
            Assert.AreEqual(LocalizationManager.Get("credits_dev_by"), creditAuthor.text);
            Assert.IsTrue(menuLevelTag.text.Contains("1"));

            Object.DestroyImmediate(menuObj);
        }

        [Test]
        public void Test_SpeedPopupsAndFlyingTags_LocalizeCorrectly()
        {
            string[] popupKeys = new[]
            {
                "popup_strike", "popup_dampen", "popup_ball_slowed", "popup_turbo", "popup_speed_up",
                "popup_bomb_chain"
            };

            // Test English
            LocalizationManager.SetLanguage(GameLanguage.English);
            foreach (var key in popupKeys)
            {
                string val = LocalizationManager.Get(key);
                Assert.IsFalse(string.IsNullOrEmpty(val), $"English popup text for '{key}' must not be empty.");
            }
            Assert.IsTrue(string.Format(LocalizationManager.Get("popup_clutch"), 10).Contains("10"));
            Assert.IsTrue(string.Format(LocalizationManager.Get("popup_chain"), 2).Contains("2"));
            Assert.IsTrue(string.Format(LocalizationManager.Get("popup_combo"), 3).Contains("3"));
            Assert.IsTrue(string.Format(LocalizationManager.Get("popup_multiball"), 2).Contains("2"));

            // Test Turkish
            LocalizationManager.SetLanguage(GameLanguage.Turkish);
            foreach (var key in popupKeys)
            {
                string val = LocalizationManager.Get(key);
                Assert.IsFalse(string.IsNullOrEmpty(val), $"Turkish popup text for '{key}' must not be empty.");
            }
            Assert.IsTrue(string.Format(LocalizationManager.Get("popup_clutch"), 10).Contains("10"));
            Assert.IsTrue(string.Format(LocalizationManager.Get("popup_chain"), 2).Contains("2"));
            Assert.IsTrue(string.Format(LocalizationManager.Get("popup_combo"), 3).Contains("3"));
            Assert.IsTrue(string.Format(LocalizationManager.Get("popup_multiball"), 2).Contains("2"));
        }
    }
}



