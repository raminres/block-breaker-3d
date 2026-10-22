using System.Collections;
using Arcade.Audio;
using Arcade.BlockBreaker;
using Arcade.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Arcade.UI
{
    /// <summary>
    /// Connects UI Toolkit In-Game HUD, quick action buttons, editable Level Settings modal,
    /// next level advancement, and state modals to ArcadeGameManager and LevelGenerator.
    /// Uses Unity 6 PanelRenderer component.
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    public class ArcadeUIManager : MonoBehaviour
    {
        private PanelRenderer panelRenderer;
        private VisualElement root;

        // Top bar elements
        private Label scoreLabel;
        private VisualElement scoreContainer;
        private Label highscoreLabel;
        private VisualElement[] lifePips;
        private Button btnQuickLevels;
        private Button btnQuickMute;
        private Button btnQuickOptions;
        private Button btnQuickPause;

        // Quick control icon visual elements
        private VisualElement iconQuickLevels;
        private VisualElement iconQuickMute;
        private VisualElement iconQuickOptions;
        private VisualElement iconQuickPause;
        private float settingsRotationAngle = 0f;

        // Banner
        private VisualElement launchBanner;
        private VisualElement launchFingerIcon;
        private VisualElement launchArrowLeft;
        private VisualElement launchArrowRight;
        private VisualElement levelClearBanner;
        private Label levelClearBannerText;
        private Label levelClearBannerSubtext;

        // Modals
        private VisualElement pauseModal;
        private VisualElement optionsModal;
        private VisualElement levelClearModal;
        private VisualElement gameOverModal;
        private VisualElement levelSettingsModal;
        private VisualElement highscoresModal;
        private VisualElement howToPlayModal;
        private VisualElement creditsModal;
        private VisualElement levelModal;

        // Modal Labels & Buttons
        private Label clearScoreLabel;
        private Label overScoreLabel;
        private Label overHighLabel;

        // Level Timer, Volley Combo & Score Delta
        private Label timerLabel;
        private Label scoreDeltaLabel;
        private VisualElement comboStatusBadge;
        private VisualElement comboStatusIcon;
        private Label comboLabel;
        private Coroutine comboEndedCoroutine;
        private int previousVolleyMultiplier = 1;
        private Coroutine scoreDeltaCoroutine;
        private Coroutine scorePulseCoroutine;
        private Coroutine scorecardStarsCoroutine;

        // Scorecard Modal Elements
        private Label scorecardTitle;
        private VisualElement scorecardStar1;
        private VisualElement scorecardStar2;
        private VisualElement scorecardStar3;
        private Label scorecardBlocksVal;
        private Label scorecardComboVal;
        private Label scorecardTimeVal;
        private Label scorecardTimeBonusVal;
        private Label scorecardFlawlessVal;
        private Button btnReplayLevel;

        private Button btnResume;
        private Button btnLevelSelectPause;
        private Button btnHighscoresPause;
        private Button btnHowToPlayPause;
        private Button btnLevelSettingsPause;
        private Button btnCreditsPause;
        private Button btnRestartPause;
        private Button btnMenuPause;
        private Button btnNextLevel;
        private Button btnClearMenu;
        private Button btnRetry;
        private Button btnOverMenu;
        private Button btnCloseHighscores;
        private Button btnResetHighscores;
        private Button btnCloseHowToPlay;
        private Button btnCloseCredits;
        private Button btnCloseLevelModal;
        private Button btnStartSelectedLevel;

        // Credit links
        private Button btnCreditEmail;
        private Button btnCreditWebsite;
        private Button btnCreditLinkedin;
        private Button btnCreditGithub;

        // Level Select controls
        private readonly System.Collections.Generic.List<Button> menuLevelTabButtons = new System.Collections.Generic.List<Button>();
        private Label menuLevelTag;
        private Label menuLevelDiff;
        private Label menuLevelName;
        private Label menuLevelDesc;
        private Label menuLevelStars;
        private Label menuLevelBestTime;
        private VisualElement levelPreviewThumb;
        private int selectedLevelNumber = 1;

        // Options controls
        private Slider sliderVolume;
        private Toggle toggleMute;
        private Slider sliderMusicVolume;
        private Toggle toggleMusicMute;
        private Button btnLangEn;
        private Button btnLangTr;
        private Button btnFps;
        private Button btnCloseOptions;
        private Button btnOptionsPause;

        // Localized Labels
        private Label livesCountLabel;
        private Label launchText;
        private Label modalTitleSettings;
        private Label labelSettingLang;
        private Label labelSettingSfx;
        private Label labelSettingSfxMute;
        private Label labelSettingMusic;
        private Label labelSettingMusicMute;
        private Label labelSettingFps;
        private Label modalTitleCredits;
        private Label modalTitleSelectLevel;
        private Label modalSubSelectLevel;
        private Label labelDiffSubtitle;
        private Label labelStarsSubtitle;
        private Label labelTimeSubtitle;
        private Label modalTitleHowToPlay;
        private Label guideH1;
        private Label guideP1;
        private Label guideH2;
        private Label guideP2;
        private Label guideH3;
        private Label guideP3;
        private Label guideH4;
        private Label guideP4;

        // Level Settings controls
        private readonly System.Collections.Generic.List<Button> hudLevelTabButtons = new System.Collections.Generic.List<Button>();
        private Label levelNameLabel;
        private Label levelDescLabel;

        private SliderInt sliderColumns;
        private Label valColumns;
        private SliderInt sliderRows;
        private Label valRows;
        private Slider sliderSpeed;
        private Label valSpeed;
        private SliderInt sliderMultiplier2x;
        private Label valMultiplier2x;
        private SliderInt sliderMultiplier3x;
        private Label valMultiplier3x;
        private SliderInt sliderExpander;
        private Label valExpander;
        private SliderInt sliderBomb;
        private Label valBomb;
        private SliderInt sliderGlass;
        private Label valGlass;
        private SliderInt sliderHeart;
        private Label valHeart;
        private SliderInt sliderShield;
        private Label valShield;
        private SliderInt sliderMultiBall;
        private Label valMultiBall;
        private SliderInt sliderLaser;
        private Label valLaser;

        public enum SpeedPopupType
        {
            Turbo,
            Dampen,
            ActiveStrike,
            Slowed,
            VolleyRamp
        }

        // Active Speed Pod
        private VisualElement speedContainer;
        private Label speedLabel;
        private VisualElement speedIcon;

        public VisualElement SpeedContainer => speedContainer;
        public Label SpeedLabel => speedLabel;

        // Active Powerup Badges
        private VisualElement shieldStatusBadge;
        private Label shieldTimerLabel;
        private VisualElement multiballStatusBadge;
        private Label multiballCountLabel;
        private VisualElement paddleStatusBadge;
        private Label paddleTimerLabel;
        private VisualElement multiplierStatusBadge;
        private Label multiplierValueLabel;
        private Label multiplierTimerLabel;
        private VisualElement laserStatusBadge;
        private Label laserTimerLabel;
        private VisualElement clutchStatusBadge;
        private Label clutchMultiplierLabel;
        private Label clutchTimerLabel;

        // Active Powerdown Badges
        private VisualElement paddleShrinkStatusBadge;
        private Label paddleShrinkTimerLabel;
        private VisualElement paddleSlowStatusBadge;
        private Label paddleSlowTimerLabel;
        private VisualElement ballShrinkStatusBadge;
        private Label ballShrinkTimerLabel;
        private VisualElement ballSlowStatusBadge;
        private Label ballSlowTimerLabel;
        private VisualElement paddleFreezeStatusBadge;
        private Label paddleFreezeTimerLabel;

        private Button btnApplyLevel;
        private Button btnCloseLevelSettings;

        [Header("Lives Icons")]
        [SerializeField] private Sprite heartFillSprite;
        [SerializeField] private Sprite heartEmptySprite;

        [Header("Power-Up Sprites")]
        [SerializeField] private PowerupIconSet iconSet;
        [SerializeField] private Sprite shieldSprite;
        [SerializeField] private Sprite multiBallSprite;
        [SerializeField] private Sprite paddleExpandSprite;
        [SerializeField] private Sprite multiplierSprite;
        [SerializeField] private Sprite laserSprite;

        [Header("Quick Control Icons")]
        [SerializeField] private Sprite levelSettingsSprite;
        [SerializeField] private Sprite volumeMuteSprite;
        [SerializeField] private Sprite volumeUpSprite;
        [SerializeField] private Sprite settingsSprite;
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private Sprite playSprite;

        // Public accessors for testing & verification
        public VisualElement LaunchBanner => launchBanner;
        public VisualElement LaunchFingerIcon => launchFingerIcon;
        public VisualElement LaunchArrowLeft => launchArrowLeft;
        public VisualElement LaunchArrowRight => launchArrowRight;
        public VisualElement IconQuickPause => iconQuickPause;
        public VisualElement IconQuickMute => iconQuickMute;
        public VisualElement IconQuickOptions => iconQuickOptions;
        public VisualElement IconQuickLevels => iconQuickLevels;
        public float SettingsRotationAngle => settingsRotationAngle;
        public Toggle ToggleMute => toggleMute;
        public Button BtnQuickPause => btnQuickPause;
        public Button BtnQuickMute => btnQuickMute;
        public Button BtnQuickOptions => btnQuickOptions;
        public Button BtnQuickLevels => btnQuickLevels;
        public bool WasPausedByOptions => wasPausedByOptions;
        public bool WasPausedByLevelSettings => wasPausedByLevelSettings;
        public VisualElement ShieldStatusBadge => shieldStatusBadge;
        public Label ShieldTimerLabel => shieldTimerLabel;
        public VisualElement MultiballStatusBadge => multiballStatusBadge;
        public Label MultiballCountLabel => multiballCountLabel;
        public VisualElement PaddleStatusBadge => paddleStatusBadge;
        public Label PaddleTimerLabel => paddleTimerLabel;
        public VisualElement MultiplierStatusBadge => multiplierStatusBadge;
        public Label MultiplierValueLabel => multiplierValueLabel;
        public Label MultiplierTimerLabel => multiplierTimerLabel;
        public VisualElement LaserStatusBadge => laserStatusBadge;
        public Label LaserTimerLabel => laserTimerLabel;
        public VisualElement ClutchStatusBadge => clutchStatusBadge;
        public Label ClutchMultiplierLabel => clutchMultiplierLabel;
        public Label ClutchTimerLabel => clutchTimerLabel;
        public VisualElement PaddleShrinkStatusBadge => paddleShrinkStatusBadge;
        public Label PaddleShrinkTimerLabel => paddleShrinkTimerLabel;
        public VisualElement PaddleSlowStatusBadge => paddleSlowStatusBadge;
        public Label PaddleSlowTimerLabel => paddleSlowTimerLabel;
        public VisualElement BallShrinkStatusBadge => ballShrinkStatusBadge;
        public Label BallShrinkTimerLabel => ballShrinkTimerLabel;
        public VisualElement BallSlowStatusBadge => ballSlowStatusBadge;
        public Label BallSlowTimerLabel => ballSlowTimerLabel;
        public VisualElement PaddleFreezeStatusBadge => paddleFreezeStatusBadge;
        public Label PaddleFreezeTimerLabel => paddleFreezeTimerLabel;
        public Label TimerLabel => timerLabel;
        public Label ScoreDeltaLabel => scoreDeltaLabel;
        public VisualElement ComboStatusBadge => comboStatusBadge;
        public VisualElement ComboStatusIcon => comboStatusIcon;
        public Label ComboLabel => comboLabel;
        public VisualElement ScorecardStar1 => scorecardStar1;
        public VisualElement ScorecardStar2 => scorecardStar2;
        public VisualElement ScorecardStar3 => scorecardStar3;
        public Label ScorecardBlocksVal => scorecardBlocksVal;
        public Label ScorecardComboVal => scorecardComboVal;
        public Label ScorecardTimeVal => scorecardTimeVal;
        public Label ScorecardTimeBonusVal => scorecardTimeBonusVal;
        public Label ScorecardFlawlessVal => scorecardFlawlessVal;
        public Button BtnReplayLevel => btnReplayLevel;
        public PowerupIconSet IconSet => iconSet;
        public void SetIconSet(PowerupIconSet set) => iconSet = set;
        public Sprite PaddleExpandSprite => paddleExpandSprite;
        public Sprite MultiplierSprite => multiplierSprite;
        public Sprite ShieldSprite => shieldSprite;
        public Sprite MultiBallSprite => multiBallSprite;
        public Sprite LaserSprite => laserSprite;
        public Sprite HeartSprite => heartFillSprite;
        public VisualElement LevelClearBanner => levelClearBanner;
        public Label LevelClearBannerText => levelClearBannerText;
        public Label LevelClearBannerSubtext => levelClearBannerSubtext;

        private bool wasPausedByOptions = false;
        private bool wasPausedByLevelSettings = false;
        private LevelConfiguration activeEditableConfig;
        private LevelGenerator levelGenerator;
        private int targetFps = 60;

        public static ArcadeUIManager Instance { get; private set; }

        public static void SetInstanceForTesting(ArcadeUIManager instance)
        {
            Instance = instance;
        }

        private void Awake()
        {
            Instance = this;
            panelRenderer = GetComponent<PanelRenderer>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (levelGenerator == null)
            {
                levelGenerator = FindAnyObjectByType<LevelGenerator>();
            }
            EnsureInitialized();
        }

        private void Update()
        {
            if (root == null || btnQuickPause == null)
            {
                EnsureInitialized();
            }

            UpdateLaunchBannerPosition();
        }

        private void UpdateLaunchBannerPosition()
        {
            if (launchBanner == null || launchBanner.ClassListContains("launch-banner-hidden")) return;
            if (launchBanner.style.display == DisplayStyle.None) return;

            Camera cam = Camera.main;
            if (cam != null && root != null && root.panel != null)
            {
                // Project world position comfortably above paddle and docked ball
                // Paddle is at Y = -6.0, docked ball is at Y = -5.12, shield is at Y = -7.75.
                // Placing world target at Y = -0.5f leaves ~4.2 units (>250px) of clear clearance above the ball,
                // and >340px below the lowest blocks row (~Y=5.0).
                Vector3 worldTarget = new Vector3(0f, -0.5f, 0f);
                Vector3 screenPt = cam.WorldToScreenPoint(worldTarget);
                Vector2 panelPt = RuntimePanelUtils.ScreenToPanel(root.panel, new Vector2(screenPt.x, Screen.height - screenPt.y));
                
                float bannerHeight = launchBanner.resolvedStyle.height;
                float topOffset = bannerHeight > 0f ? bannerHeight * 0.5f : 85f;
                launchBanner.style.top = panelPt.y - topOffset;
                launchBanner.style.bottom = StyleKeyword.Auto;
            }
        }

        public void EnsureInitialized()
        {
            SubscribeEvents();

            if (root == null)
            {
                root = GetRootVisualElement();
            }

            if (root != null && btnQuickPause == null)
            {
                UnbindElements();
                BindElements();
                InitializeDisplay();
            }
        }

        private VisualElement GetRootVisualElement()
        {
            if (root != null) return root;

            if (panelRenderer == null) panelRenderer = GetComponent<PanelRenderer>();
            if (panelRenderer != null)
            {
                var prop = panelRenderer.GetType().GetProperty("rootVisualElement", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (prop != null)
                {
                    root = prop.GetValue(panelRenderer) as VisualElement;
                }

                if (root == null)
                {
                    var panelProp = panelRenderer.GetType().GetProperty("containerPanel", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var panel = panelProp?.GetValue(panelRenderer) as IPanel;
                    if (panel != null)
                    {
                        root = panel.visualTree;
                    }
                }
            }

            if (root == null)
            {
                var doc = GetComponent<UIDocument>();
                if (doc != null) root = doc.rootVisualElement;
            }

            return root;
        }

        private void OnEnable()
        {
            if (panelRenderer == null) panelRenderer = GetComponent<PanelRenderer>();
            if (panelRenderer != null)
            {
                panelRenderer.RegisterUIReloadCallback(OnUIReload);
            }
            EnsureInitialized();
        }

        private void OnDisable()
        {
            if (panelRenderer != null)
            {
                panelRenderer.UnregisterUIReloadCallback(OnUIReload);
            }
            UnsubscribeEvents();
            UnbindElements();
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement newRoot, int version)
        {
            if (newRoot == null) return;
            root = newRoot;
            UnbindElements();
            BindElements();
            InitializeDisplay();
        }

        private void UnbindElements()
        {
            if (btnQuickLevels != null) btnQuickLevels.clicked -= ShowLevelSettings;
            if (btnQuickMute != null) btnQuickMute.clicked -= HandleQuickMuteClicked;
            if (btnQuickOptions != null) btnQuickOptions.clicked -= HandleQuickOptionsClicked;
            if (btnQuickPause != null) btnQuickPause.clicked -= HandleQuickPauseClicked;

            if (btnResume != null) btnResume.clicked -= HandleResumeClicked;
            if (btnLevelSelectPause != null) btnLevelSelectPause.clicked -= ShowLevelModal;
            if (btnHighscoresPause != null) btnHighscoresPause.clicked -= ShowHighScores;
            if (btnHowToPlayPause != null) btnHowToPlayPause.clicked -= ShowHowToPlay;
            if (btnLevelSettingsPause != null) btnLevelSettingsPause.clicked -= ShowLevelSettings;
            if (btnCreditsPause != null) btnCreditsPause.clicked -= ShowCredits;
            if (btnRestartPause != null) btnRestartPause.clicked -= HandleRestartClicked;
            if (btnMenuPause != null) btnMenuPause.clicked -= HandleMenuClicked;

            if (btnCloseCredits != null) btnCloseCredits.clicked -= HideCredits;
            if (btnCloseLevelModal != null) btnCloseLevelModal.clicked -= HideLevelModal;
            if (btnStartSelectedLevel != null) btnStartSelectedLevel.clicked -= HandleStartSelectedLevel;

            if (btnCreditEmail != null) btnCreditEmail.clicked -= OpenEmail;
            if (btnCreditWebsite != null) btnCreditWebsite.clicked -= OpenWebsite;
            if (btnCreditLinkedin != null) btnCreditLinkedin.clicked -= OpenLinkedIn;
            if (btnCreditGithub != null) btnCreditGithub.clicked -= OpenGitHub;
            menuLevelTabButtons.Clear();

            if (btnNextLevel != null) btnNextLevel.clicked -= HandleNextLevelClicked;
            if (btnClearMenu != null) btnClearMenu.clicked -= HandleMenuClicked;
            if (btnReplayLevel != null) btnReplayLevel.clicked -= HandleReplayLevelClicked;

            if (scoreDeltaCoroutine != null)
            {
                StopCoroutine(scoreDeltaCoroutine);
                scoreDeltaCoroutine = null;
            }
            if (scorePulseCoroutine != null)
            {
                StopCoroutine(scorePulseCoroutine);
                scorePulseCoroutine = null;
            }
            if (scorecardStarsCoroutine != null)
            {
                StopCoroutine(scorecardStarsCoroutine);
                scorecardStarsCoroutine = null;
            }
            scoreContainer = null;

            if (btnRetry != null) btnRetry.clicked -= HandleRestartClicked;
            if (btnOverMenu != null) btnOverMenu.clicked -= HandleMenuClicked;

            if (btnCloseOptions != null) btnCloseOptions.clicked -= HideOptions;
            if (btnCloseHighscores != null) btnCloseHighscores.clicked -= HideHighScores;
            if (btnResetHighscores != null) btnResetHighscores.clicked -= HandleResetHighScores;
            if (btnCloseHowToPlay != null) btnCloseHowToPlay.clicked -= HideHowToPlay;

            if (btnFps != null) btnFps.clicked -= ToggleFpsSetting;
            if (btnOptionsPause != null) btnOptionsPause.clicked -= ShowOptions;
            LocalizationManager.OnLanguageChanged -= UpdateLocalizedTexts;
            hudLevelTabButtons.Clear();

            if (btnApplyLevel != null) btnApplyLevel.clicked -= ApplyLevelSettingsAndRestart;
            if (btnCloseLevelSettings != null) btnCloseLevelSettings.clicked -= HideLevelSettings;

            levelClearBanner = null;
            levelClearBannerText = null;
            levelClearBannerSubtext = null;
            launchBanner = null;
            launchFingerIcon = null;
            launchArrowLeft = null;
            launchArrowRight = null;
        }

        private void BindElements()
        {
            if (root == null) return;

            scoreLabel = root.Q<Label>("score-label");
            scoreContainer = root.Q<VisualElement>("score-container");
            highscoreLabel = root.Q<Label>("highscore-label");
            speedContainer = root.Q<VisualElement>("speed-container");
            speedLabel = root.Q<Label>("speed-label");
            speedIcon = root.Q<VisualElement>("icon-speed");
            timerLabel = root.Q<Label>("timer-label");
            scoreDeltaLabel = root.Q<Label>("score-delta-label");
            comboStatusBadge = root.Q<VisualElement>("combo-status-badge");
            comboStatusIcon = root.Q<VisualElement>("combo-status-icon");
            comboLabel = root.Q<Label>("combo-label");

            scorecardTitle = root.Q<Label>("scorecard-title");
            scorecardStar1 = root.Q<VisualElement>("scorecard-star-1");
            scorecardStar2 = root.Q<VisualElement>("scorecard-star-2");
            scorecardStar3 = root.Q<VisualElement>("scorecard-star-3");
            scorecardBlocksVal = root.Q<Label>("scorecard-blocks-val");
            scorecardComboVal = root.Q<Label>("scorecard-combo-val");
            scorecardTimeVal = root.Q<Label>("scorecard-time-val");
            scorecardTimeBonusVal = root.Q<Label>("scorecard-timebonus-val");
            scorecardFlawlessVal = root.Q<Label>("scorecard-flawless-val");
            btnReplayLevel = root.Q<Button>("btn-replay-level");

            lifePips = new[]
            {
                root.Q<VisualElement>("life-pip-1"),
                root.Q<VisualElement>("life-pip-2"),
                root.Q<VisualElement>("life-pip-3"),
                root.Q<VisualElement>("life-pip-4"),
                root.Q<VisualElement>("life-pip-5")
            };

            if (heartFillSprite == null)
            {
#if UNITY_EDITOR
                heartFillSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Heart_Fill.png");
#endif
            }
            if (heartEmptySprite == null)
            {
#if UNITY_EDITOR
                heartEmptySprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Heart_Empty.png");
#endif
            }

            if (iconSet != null)
            {
                if (shieldSprite == null) shieldSprite = iconSet.ShieldSprite;
                if (multiBallSprite == null) multiBallSprite = iconSet.MultiBallSprite;
                if (paddleExpandSprite == null) paddleExpandSprite = iconSet.ExpanderSprite;
                if (multiplierSprite == null) multiplierSprite = iconSet.MultiplierSprite;
                if (laserSprite == null) laserSprite = iconSet.LaserSprite;
            }

#if UNITY_EDITOR
            if (levelSettingsSprite == null)
                levelSettingsSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Level_Settings.png");
            if (volumeMuteSprite == null)
                volumeMuteSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Volume_Mute.png");
            if (volumeUpSprite == null)
                volumeUpSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Volume_Up.png");
            if (settingsSprite == null)
                settingsSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Settings.png");
            if (pauseSprite == null)
                pauseSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Pause.png");
            if (playSprite == null)
                playSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Play.png");
            if (shieldSprite == null)
                shieldSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Shield.png");
            if (multiBallSprite == null)
                multiBallSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Multi_Ball.png");
            if (paddleExpandSprite == null)
            {
                var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Arrows_Outward.png");
                if (sp == null)
                {
                    var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/UI/Icons/TX_Powerup_Arrows_Outward.png");
                    if (all != null)
                    {
                        for (int i = 0; i < all.Length; i++)
                        {
                            if (all[i] is Sprite s) { sp = s; break; }
                        }
                    }
                }
                paddleExpandSprite = sp;
            }
            if (multiplierSprite == null)
            {
                var sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Extra_Points.png");
                if (sp == null)
                {
                    var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/UI/Icons/TX_Powerup_Extra_Points.png");
                    if (all != null)
                    {
                        for (int i = 0; i < all.Length; i++)
                        {
                            if (all[i] is Sprite s) { sp = s; break; }
                        }
                    }
                }
                multiplierSprite = sp;
            }
            if (laserSprite == null)
            {
                var gunSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Icons/TX_Powerup_Gun.png");
                if (gunSp == null)
                {
                    var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/UI/Icons/TX_Powerup_Gun.png");
                    if (all != null)
                    {
                        for (int i = 0; i < all.Length; i++)
                        {
                            if (all[i] is Sprite s) { gunSp = s; break; }
                        }
                    }
                }
                laserSprite = gunSp;
            }
#endif

            shieldStatusBadge = root.Q<VisualElement>("shield-status-badge");
            shieldTimerLabel = root.Q<Label>("shield-timer-label");
            multiballStatusBadge = root.Q<VisualElement>("multiball-status-badge");
            multiballCountLabel = root.Q<Label>("multiball-count-label");
            paddleStatusBadge = root.Q<VisualElement>("paddle-status-badge");
            paddleTimerLabel = root.Q<Label>("paddle-timer-label");
            multiplierStatusBadge = root.Q<VisualElement>("multiplier-status-badge");
            multiplierValueLabel = root.Q<Label>("multiplier-value-label");
            multiplierTimerLabel = root.Q<Label>("multiplier-timer-label");
            laserStatusBadge = root.Q<VisualElement>("laser-status-badge");
            laserTimerLabel = root.Q<Label>("laser-timer-label");
            clutchStatusBadge = root.Q<VisualElement>("clutch-status-badge");
            clutchMultiplierLabel = root.Q<Label>("clutch-multiplier-label");
            clutchTimerLabel = root.Q<Label>("clutch-timer-label");

            // Query Powerdown Badges
            paddleShrinkStatusBadge = root.Q<VisualElement>("paddle-shrink-status-badge");
            paddleShrinkTimerLabel = root.Q<Label>("paddle-shrink-timer-label");
            paddleSlowStatusBadge = root.Q<VisualElement>("paddle-slow-status-badge");
            paddleSlowTimerLabel = root.Q<Label>("paddle-slow-timer-label");
            ballShrinkStatusBadge = root.Q<VisualElement>("ball-shrink-status-badge");
            ballShrinkTimerLabel = root.Q<Label>("ball-shrink-timer-label");
            ballSlowStatusBadge = root.Q<VisualElement>("ball-slow-status-badge");
            ballSlowTimerLabel = root.Q<Label>("ball-slow-timer-label");
            paddleFreezeStatusBadge = root.Q<VisualElement>("paddle-freeze-status-badge");
            paddleFreezeTimerLabel = root.Q<Label>("paddle-freeze-timer-label");

            var iconShield = root.Q<VisualElement>("shield-status-icon");
            if (iconShield != null && shieldSprite != null)
                iconShield.style.backgroundImage = new StyleBackground(shieldSprite);

            var iconMulti = root.Q<VisualElement>("multiball-status-icon");
            if (iconMulti != null && multiBallSprite != null)
                iconMulti.style.backgroundImage = new StyleBackground(multiBallSprite);

            var iconPaddle = root.Q<VisualElement>("paddle-status-icon");
            if (iconPaddle != null && paddleExpandSprite != null)
                iconPaddle.style.backgroundImage = new StyleBackground(paddleExpandSprite);

            var iconMultiplier = root.Q<VisualElement>("multiplier-status-icon");
            if (iconMultiplier != null && multiplierSprite != null)
                iconMultiplier.style.backgroundImage = new StyleBackground(multiplierSprite);

            var iconLaser = root.Q<VisualElement>("laser-status-icon");
            if (iconLaser != null && laserSprite != null)
                iconLaser.style.backgroundImage = new StyleBackground(laserSprite);

            var iconClutch = root.Q<VisualElement>("clutch-status-icon");
            if (iconClutch != null && laserSprite != null)
                iconClutch.style.backgroundImage = new StyleBackground(laserSprite);

            if (iconSet != null)
            {
                var iconPaddleShrink = root.Q<VisualElement>("paddle-shrink-status-icon");
                if (iconPaddleShrink != null && iconSet.PaddleShortenerSprite != null)
                    iconPaddleShrink.style.backgroundImage = new StyleBackground(iconSet.PaddleShortenerSprite);

                var iconPaddleSlow = root.Q<VisualElement>("paddle-slow-status-icon");
                if (iconPaddleSlow != null && iconSet.PaddleSlowerSprite != null)
                    iconPaddleSlow.style.backgroundImage = new StyleBackground(iconSet.PaddleSlowerSprite);

                var iconBallShrink = root.Q<VisualElement>("ball-shrink-status-icon");
                if (iconBallShrink != null && iconSet.BallSizeDecreaserSprite != null)
                    iconBallShrink.style.backgroundImage = new StyleBackground(iconSet.BallSizeDecreaserSprite);

                var iconBallSlow = root.Q<VisualElement>("ball-slow-status-icon");
                if (iconBallSlow != null && iconSet.BallSlowerSprite != null)
                    iconBallSlow.style.backgroundImage = new StyleBackground(iconSet.BallSlowerSprite);

                var iconPaddleFreeze = root.Q<VisualElement>("paddle-freeze-status-icon");
                if (iconPaddleFreeze != null && iconSet.PaddleFreezerSprite != null)
                    iconPaddleFreeze.style.backgroundImage = new StyleBackground(iconSet.PaddleFreezerSprite);
            }

            if (comboStatusIcon != null && multiplierSprite != null)
                comboStatusIcon.style.backgroundImage = new StyleBackground(multiplierSprite);

            btnQuickLevels = root.Q<Button>("btn-quick-levels");
            btnQuickMute = root.Q<Button>("btn-quick-mute");
            btnQuickOptions = root.Q<Button>("btn-quick-options");
            btnQuickPause = root.Q<Button>("btn-quick-pause");

            iconQuickLevels = root.Q<VisualElement>("icon-quick-levels");
            iconQuickMute = root.Q<VisualElement>("icon-quick-mute");
            iconQuickOptions = root.Q<VisualElement>("icon-quick-options");
            iconQuickPause = root.Q<VisualElement>("icon-quick-pause");

            if (iconQuickLevels != null && levelSettingsSprite != null)
                iconQuickLevels.style.backgroundImage = new StyleBackground(levelSettingsSprite);
            if (iconQuickOptions != null && settingsSprite != null)
                iconQuickOptions.style.backgroundImage = new StyleBackground(settingsSprite);

            settingsRotationAngle = 0f;
            if (iconQuickOptions != null)
                iconQuickOptions.style.rotate = new StyleRotate(new Rotate(Angle.Degrees(0f)));

            launchBanner = root.Q<VisualElement>("launch-banner");
            launchFingerIcon = root.Q<VisualElement>("launch-finger-icon") ?? launchBanner?.Q(className: "launch-finger-icon");
            launchArrowLeft = root.Q<VisualElement>("launch-arrow-left") ?? launchBanner?.Q(className: "launch-arrow-left");
            launchArrowRight = root.Q<VisualElement>("launch-arrow-right") ?? launchBanner?.Q(className: "launch-arrow-right");
            levelClearBanner = root.Q<VisualElement>("level-clear-banner");
            levelClearBannerText = root.Q<Label>("level-clear-banner-text");
            levelClearBannerSubtext = root.Q<Label>("level-clear-banner-subtext");

            pauseModal = root.Q<VisualElement>("pause-modal");
            optionsModal = root.Q<VisualElement>("options-modal");
            levelClearModal = root.Q<VisualElement>("level-clear-modal");
            gameOverModal = root.Q<VisualElement>("game-over-modal");
            levelSettingsModal = root.Q<VisualElement>("level-settings-modal");
            highscoresModal = root.Q<VisualElement>("highscores-modal");
            howToPlayModal = root.Q<VisualElement>("how-to-play-modal");
            creditsModal = root.Q<VisualElement>("credits-modal");
            levelModal = root.Q<VisualElement>("level-modal");

            clearScoreLabel = root.Q<Label>("clear-score-label");
            overScoreLabel = root.Q<Label>("over-score-label");
            overHighLabel = root.Q<Label>("over-high-label");

            btnResume = root.Q<Button>("btn-resume");
            btnLevelSelectPause = root.Q<Button>("btn-level-select-pause") ?? root.Q<Button>("btn-level-settings-pause");
            btnHighscoresPause = root.Q<Button>("btn-highscores-pause");
            btnHowToPlayPause = root.Q<Button>("btn-how-to-play-pause");
            btnLevelSettingsPause = root.Q<Button>("btn-level-settings-pause");
            btnCreditsPause = root.Q<Button>("btn-credits-pause");
            btnRestartPause = root.Q<Button>("btn-restart-pause");
            btnMenuPause = root.Q<Button>("btn-menu-pause");
            btnNextLevel = root.Q<Button>("btn-next-level");
            btnClearMenu = root.Q<Button>("btn-clear-menu");
            btnRetry = root.Q<Button>("btn-retry");
            btnOverMenu = root.Q<Button>("btn-over-menu");
            btnCloseHighscores = root.Q<Button>("btn-close-highscores");
            btnResetHighscores = root.Q<Button>("btn-reset-highscores");
            btnCloseHowToPlay = root.Q<Button>("btn-close-how-to-play");
            btnCloseCredits = root.Q<Button>("btn-close-credits");
            btnCloseLevelModal = root.Q<Button>("btn-close-level-modal");
            btnStartSelectedLevel = root.Q<Button>("btn-start-selected-level");

            btnCreditEmail = root.Q<Button>("btn-credit-email");
            btnCreditWebsite = root.Q<Button>("btn-credit-website");
            btnCreditLinkedin = root.Q<Button>("btn-credit-linkedin");
            btnCreditGithub = root.Q<Button>("btn-credit-github");

            menuLevelTabButtons.Clear();
            var levelGridContainer = levelModal != null ? levelModal.Q<VisualElement>(className: "level-grid-container") : null;
            if (levelGridContainer != null)
            {
                levelGridContainer.Clear();
                if (levelGenerator == null) levelGenerator = FindAnyObjectByType<LevelGenerator>();
                int totalLevels = levelGenerator != null ? levelGenerator.TotalLevels : 30;
                for (int i = 0; i < totalLevels; i++)
                {
                    int lvlNum = i + 1;
                    int stars = HighScoreManager.GetLevelStars(lvlNum);
                    float bestTime = HighScoreManager.GetLevelBestTime(lvlNum);

                    var cardBtn = new Button();
                    cardBtn.AddToClassList("level-grid-card");
                    cardBtn.AddToClassList("level-tab-btn");

                    var numLbl = new Label(lvlNum.ToString());
                    numLbl.AddToClassList("level-card-num");
                    numLbl.pickingMode = PickingMode.Ignore;
                    cardBtn.Add(numLbl);

                    var starsRow = new VisualElement();
                    starsRow.AddToClassList("level-card-stars-row");
                    starsRow.pickingMode = PickingMode.Ignore;
                    for (int s = 0; s < 3; s++)
                    {
                        var star = new Label("★");
                        star.AddToClassList("card-star");
                        star.AddToClassList(s < stars ? "star-active" : "star-inactive");
                        star.pickingMode = PickingMode.Ignore;
                        starsRow.Add(star);
                    }
                    cardBtn.Add(starsRow);

                    var timeLbl = new Label(bestTime > 0f ? HighScoreManager.FormatTime(bestTime) : "--:--");
                    timeLbl.AddToClassList("level-card-time");
                    timeLbl.pickingMode = PickingMode.Ignore;
                    cardBtn.Add(timeLbl);

                    levelGridContainer.Add(cardBtn);
                    menuLevelTabButtons.Add(cardBtn);
                    int captureLvl = lvlNum;
                    cardBtn.clicked += () =>
                    {
                        UIAnimationHelper.PunchElement(this, cardBtn, "ui-punch-pop", 0.10f);
                        SelectLevel(captureLvl);
                    };
                }
            }

            menuLevelTag = root.Q<Label>("menu-level-tag");
            menuLevelDiff = root.Q<Label>("menu-level-diff");
            menuLevelName = root.Q<Label>("menu-level-name");
            menuLevelDesc = root.Q<Label>("menu-level-desc");
            menuLevelStars = root.Q<Label>("menu-level-stars");
            menuLevelBestTime = root.Q<Label>("menu-level-best-time");
            levelPreviewThumb = root.Q<VisualElement>("level-preview-thumb");

            modalTitleCredits = root.Q<Label>("modal-title-credits");
            modalTitleSelectLevel = root.Q<Label>("modal-title-select-level");
            modalSubSelectLevel = root.Q<Label>("modal-sub-select-level");
            labelDiffSubtitle = root.Q<Label>("label-diff-subtitle");
            labelStarsSubtitle = root.Q<Label>("label-stars-subtitle");
            labelTimeSubtitle = root.Q<Label>("label-time-subtitle");

            sliderVolume = root.Q<Slider>("slider-volume");
            toggleMute = root.Q<Toggle>("toggle-mute");
            sliderMusicVolume = root.Q<Slider>("slider-music-volume");
            toggleMusicMute = root.Q<Toggle>("toggle-music-mute");
            btnLangEn = root.Q<Button>("btn-lang-en");
            btnLangTr = root.Q<Button>("btn-lang-tr");
            btnFps = root.Q<Button>("btn-toggle-fps") ?? root.Q<Button>("btn-fps");
            btnCloseOptions = root.Q<Button>("btn-close-options");
            btnOptionsPause = root.Q<Button>("btn-options-pause");

            livesCountLabel = root.Q<Label>("lives-count-label");
            launchText = root.Q<Label>("launch-text");

            modalTitleSettings = root.Q<Label>("modal-title-settings");
            labelSettingLang = root.Q<Label>("label-setting-lang");
            labelSettingSfx = root.Q<Label>("label-setting-sfx");
            labelSettingSfxMute = root.Q<Label>("label-setting-sfx-mute");
            labelSettingMusic = root.Q<Label>("label-setting-music");
            labelSettingMusicMute = root.Q<Label>("label-setting-music-mute");
            labelSettingFps = root.Q<Label>("label-setting-fps");
            modalTitleHowToPlay = root.Q<Label>("modal-title-how-to-play");
            guideH1 = root.Q<Label>("guide-h1");
            guideP1 = root.Q<Label>("guide-p1");
            guideH2 = root.Q<Label>("guide-h2");
            guideP2 = root.Q<Label>("guide-p2");
            guideH3 = root.Q<Label>("guide-h3");
            guideP3 = root.Q<Label>("guide-p3");
            guideH4 = root.Q<Label>("guide-h4");
            guideP4 = root.Q<Label>("guide-p4");

            // Level Settings controls
            levelNameLabel = root.Q<Label>("level-name-label");
            levelDescLabel = root.Q<Label>("level-desc-label");

            sliderColumns = root.Q<SliderInt>("slider-columns");
            valColumns = root.Q<Label>("val-columns");
            sliderRows = root.Q<SliderInt>("slider-rows");
            valRows = root.Q<Label>("val-rows");
            sliderSpeed = root.Q<Slider>("slider-speed");
            valSpeed = root.Q<Label>("val-speed");
            sliderMultiplier2x = root.Q<SliderInt>("slider-multiplier2x");
            valMultiplier2x = root.Q<Label>("val-multiplier2x");
            sliderMultiplier3x = root.Q<SliderInt>("slider-multiplier3x");
            valMultiplier3x = root.Q<Label>("val-multiplier3x");
            sliderExpander = root.Q<SliderInt>("slider-expander");
            valExpander = root.Q<Label>("val-expander");
            sliderBomb = root.Q<SliderInt>("slider-bomb");
            valBomb = root.Q<Label>("val-bomb");
            sliderGlass = root.Q<SliderInt>("slider-glass");
            valGlass = root.Q<Label>("val-glass");
            sliderHeart = root.Q<SliderInt>("slider-heart");
            valHeart = root.Q<Label>("val-heart");
            sliderShield = root.Q<SliderInt>("slider-shield");
            valShield = root.Q<Label>("val-shield");
            sliderMultiBall = root.Q<SliderInt>("slider-multiball");
            valMultiBall = root.Q<Label>("val-multiball");
            sliderLaser = root.Q<SliderInt>("slider-laser");
            valLaser = root.Q<Label>("val-laser");

            btnApplyLevel = root.Q<Button>("btn-apply-level");
            btnCloseLevelSettings = root.Q<Button>("btn-close-level-settings");

            // Wire quick buttons
            if (btnQuickLevels != null) btnQuickLevels.clicked += ShowLevelSettings;
            if (btnQuickMute != null) btnQuickMute.clicked += HandleQuickMuteClicked;
            if (btnQuickOptions != null) btnQuickOptions.clicked += HandleQuickOptionsClicked;
            if (btnQuickPause != null) btnQuickPause.clicked += HandleQuickPauseClicked;

            // Wire modal buttons
            if (btnResume != null) btnResume.clicked += HandleResumeClicked;
            if (btnLevelSelectPause != null) btnLevelSelectPause.clicked += ShowLevelModal;
            if (btnOptionsPause != null) btnOptionsPause.clicked += ShowOptions;
            if (btnHighscoresPause != null) btnHighscoresPause.clicked += ShowHighScores;
            if (btnHowToPlayPause != null) btnHowToPlayPause.clicked += ShowHowToPlay;
            if (btnCreditsPause != null) btnCreditsPause.clicked += ShowCredits;
            if (btnLevelSettingsPause != null && btnLevelSettingsPause != btnLevelSelectPause) btnLevelSettingsPause.clicked += ShowLevelSettings;
            if (btnRestartPause != null) btnRestartPause.clicked += HandleRestartClicked;
            if (btnMenuPause != null) btnMenuPause.clicked += HandleMenuClicked;

            if (btnCloseCredits != null) btnCloseCredits.clicked += HideCredits;
            if (btnCloseLevelModal != null) btnCloseLevelModal.clicked += HideLevelModal;
            if (btnStartSelectedLevel != null) btnStartSelectedLevel.clicked += HandleStartSelectedLevel;

            if (btnCreditEmail != null) btnCreditEmail.clicked += OpenEmail;
            if (btnCreditWebsite != null) btnCreditWebsite.clicked += OpenWebsite;
            if (btnCreditLinkedin != null) btnCreditLinkedin.clicked += OpenLinkedIn;
            if (btnCreditGithub != null) btnCreditGithub.clicked += OpenGitHub;

            if (btnNextLevel != null) btnNextLevel.clicked += HandleNextLevelClicked;
            if (btnClearMenu != null) btnClearMenu.clicked += HandleMenuClicked;
            if (btnReplayLevel != null) btnReplayLevel.clicked += HandleReplayLevelClicked;

            if (btnRetry != null) btnRetry.clicked += HandleRestartClicked;
            if (btnOverMenu != null) btnOverMenu.clicked += HandleMenuClicked;

            if (btnCloseOptions != null) btnCloseOptions.clicked += HideOptions;
            if (btnLangEn != null) btnLangEn.clicked += () => SetLanguage(GameLanguage.English);
            if (btnLangTr != null) btnLangTr.clicked += () => SetLanguage(GameLanguage.Turkish);

            if (btnCloseHighscores != null) btnCloseHighscores.clicked += HideHighScores;
            if (btnResetHighscores != null) btnResetHighscores.clicked += HandleResetHighScores;
            if (btnCloseHowToPlay != null) btnCloseHowToPlay.clicked += HideHowToPlay;

            if (sliderVolume != null)
            {
                sliderVolume.RegisterValueChangedCallback(evt =>
                {
                    if (ArcadeAudioManager.Instance != null)
                        ArcadeAudioManager.Instance.SetVolume(evt.newValue);
                });
            }

            if (toggleMute != null)
            {
                toggleMute.RegisterValueChangedCallback(evt =>
                {
                    if (ArcadeAudioManager.Instance != null)
                        ArcadeAudioManager.Instance.IsMuted = evt.newValue;
                    UpdateMuteButtonIcon();
                });
            }

            if (sliderMusicVolume != null)
            {
                sliderMusicVolume.RegisterValueChangedCallback(evt =>
                {
                    if (ArcadeAudioManager.Instance != null)
                        ArcadeAudioManager.Instance.MusicVolume = evt.newValue;
                });
            }

            if (toggleMusicMute != null)
            {
                toggleMusicMute.RegisterValueChangedCallback(evt =>
                {
                    if (ArcadeAudioManager.Instance != null)
                        ArcadeAudioManager.Instance.IsMusicMuted = evt.newValue;
                    UpdateToggleMuteIcon(toggleMusicMute, evt.newValue);
                });
            }

            LocalizationManager.OnLanguageChanged += UpdateLocalizedTexts;
            UpdateLocalizedTexts();

            if (btnFps != null) btnFps.clicked += ToggleFpsSetting;

            // Level settings wiring
            hudLevelTabButtons.Clear();
            if (levelSettingsModal != null)
            {
                var tabsContainer = levelSettingsModal.Q<VisualElement>(className: "level-tabs-container");
                if (tabsContainer != null)
                {
                    tabsContainer.Clear();
                    int totalLevels = levelGenerator != null ? levelGenerator.TotalLevels : 30;
                    for (int i = 0; i < totalLevels; i++)
                    {
                        int lvlNum = i + 1;
                        int stars = HighScoreManager.GetLevelStars(lvlNum);
                        string starSuffix = stars > 0 ? $" ({stars}★)" : "";
                        var btn = new Button { text = $"LVL {lvlNum}{starSuffix}" };
                        btn.AddToClassList("level-tab-btn");
                        tabsContainer.Add(btn);
                        hudLevelTabButtons.Add(btn);
                        btn.clicked += () => SelectLevelTab(lvlNum);
                    }
                }
            }

            if (sliderColumns != null)
            {
                sliderColumns.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetColumns(evt.newValue);
                    if (valColumns != null) valColumns.text = evt.newValue.ToString();
                });
            }

            if (sliderRows != null)
            {
                sliderRows.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetRowsPerTier(evt.newValue);
                    if (valRows != null) valRows.text = $"{evt.newValue} ({evt.newValue * 3})";
                });
            }

            if (sliderSpeed != null)
            {
                sliderSpeed.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetBallSpeedMultiplier(evt.newValue);
                    if (valSpeed != null) valSpeed.text = $"{evt.newValue:F1}x";
                });
            }

            if (sliderMultiplier2x != null)
            {
                sliderMultiplier2x.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetMultiplier2xCount(evt.newValue);
                    if (valMultiplier2x != null) valMultiplier2x.text = evt.newValue.ToString();
                });
            }

            if (sliderMultiplier3x != null)
            {
                sliderMultiplier3x.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetMultiplier3xCount(evt.newValue);
                    if (valMultiplier3x != null) valMultiplier3x.text = evt.newValue.ToString();
                });
            }

            if (sliderExpander != null)
            {
                sliderExpander.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetPaddleExpanderCount(evt.newValue);
                    if (valExpander != null) valExpander.text = evt.newValue.ToString();
                });
            }

            if (sliderBomb != null)
            {
                sliderBomb.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetBombCount(evt.newValue);
                    if (valBomb != null) valBomb.text = evt.newValue.ToString();
                });
            }

            if (sliderGlass != null)
            {
                sliderGlass.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetGlassEnclosedCount(evt.newValue);
                    if (valGlass != null) valGlass.text = evt.newValue.ToString();
                });
            }

            if (sliderHeart != null)
            {
                sliderHeart.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetExtraHeartCount(evt.newValue);
                    if (valHeart != null) valHeart.text = evt.newValue.ToString();
                });
            }

            if (sliderShield != null)
            {
                sliderShield.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetShieldCount(evt.newValue);
                    if (valShield != null) valShield.text = evt.newValue.ToString();
                });
            }

            if (sliderMultiBall != null)
            {
                sliderMultiBall.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetMultiBallCount(evt.newValue);
                    if (valMultiBall != null) valMultiBall.text = evt.newValue.ToString();
                });
            }

            if (sliderLaser != null)
            {
                sliderLaser.RegisterValueChangedCallback(evt =>
                {
                    if (activeEditableConfig != null) activeEditableConfig.SetLaserCount(evt.newValue);
                    if (valLaser != null) valLaser.text = evt.newValue.ToString();
                });
            }

            if (btnApplyLevel != null) btnApplyLevel.clicked += ApplyLevelSettingsAndRestart;
            if (btnCloseLevelSettings != null) btnCloseLevelSettings.clicked += HideLevelSettings;
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();

            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.OnScoreChanged += UpdateScoreDisplay;
                ArcadeGameManager.Instance.OnLivesChanged += UpdateLivesDisplay;
                ArcadeGameManager.Instance.OnStateChanged += HandleGameStateChanged;
                ArcadeGameManager.Instance.OnShieldStateChanged += HandleShieldStateChanged;
                ArcadeGameManager.Instance.OnShieldTick += HandleShieldTick;
                ArcadeGameManager.Instance.OnActiveBallCountChanged += HandleActiveBallCountChanged;
                ArcadeGameManager.Instance.OnPaddleExpandStateChanged += HandlePaddleExpandStateChanged;
                ArcadeGameManager.Instance.OnPaddleExpandTick += HandlePaddleExpandTick;
                ArcadeGameManager.Instance.OnScoreMultiplierStateChanged += HandleScoreMultiplierStateChanged;
                ArcadeGameManager.Instance.OnScoreMultiplierTick += HandleScoreMultiplierTick;
                ArcadeGameManager.Instance.OnLaserPowerupStateChanged += HandleLaserPowerupStateChanged;
                ArcadeGameManager.Instance.OnLaserPowerupTick += HandleLaserPowerupTick;
                ArcadeGameManager.Instance.OnClutchStateChanged += HandleClutchStateChanged;
                ArcadeGameManager.Instance.OnClutchTick += HandleClutchTick;
                ArcadeGameManager.Instance.OnLevelTimerTick += HandleLevelTimerTick;
                ArcadeGameManager.Instance.OnVolleyComboChanged += HandleVolleyComboChanged;
                ArcadeGameManager.Instance.OnBlockPointsAwarded += HandleBlockPointsAwarded;
                ArcadeGameManager.Instance.OnLevelCompletedWithTally += HandleLevelCompletedWithTally;
                ArcadeGameManager.Instance.OnLevelClearPending += HandleLevelClearPending;
                ArcadeGameManager.Instance.OnPowerdownStateChanged += HandlePowerdownStateChanged;
                ArcadeGameManager.Instance.OnPowerdownTick += HandlePowerdownTick;
                BallController.OnAnyBallSpeedChanged -= HandleBallSpeedChanged;
                BallController.OnAnyBallSpeedChanged += HandleBallSpeedChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.OnScoreChanged -= UpdateScoreDisplay;
                ArcadeGameManager.Instance.OnLivesChanged -= UpdateLivesDisplay;
                ArcadeGameManager.Instance.OnStateChanged -= HandleGameStateChanged;
                ArcadeGameManager.Instance.OnShieldStateChanged -= HandleShieldStateChanged;
                ArcadeGameManager.Instance.OnShieldTick -= HandleShieldTick;
                ArcadeGameManager.Instance.OnActiveBallCountChanged -= HandleActiveBallCountChanged;
                ArcadeGameManager.Instance.OnPaddleExpandStateChanged -= HandlePaddleExpandStateChanged;
                ArcadeGameManager.Instance.OnPaddleExpandTick -= HandlePaddleExpandTick;
                ArcadeGameManager.Instance.OnScoreMultiplierStateChanged -= HandleScoreMultiplierStateChanged;
                ArcadeGameManager.Instance.OnScoreMultiplierTick -= HandleScoreMultiplierTick;
                ArcadeGameManager.Instance.OnLaserPowerupStateChanged -= HandleLaserPowerupStateChanged;
                ArcadeGameManager.Instance.OnLaserPowerupTick -= HandleLaserPowerupTick;
                ArcadeGameManager.Instance.OnClutchStateChanged -= HandleClutchStateChanged;
                ArcadeGameManager.Instance.OnClutchTick -= HandleClutchTick;
                ArcadeGameManager.Instance.OnLevelTimerTick -= HandleLevelTimerTick;
                ArcadeGameManager.Instance.OnVolleyComboChanged -= HandleVolleyComboChanged;
                ArcadeGameManager.Instance.OnBlockPointsAwarded -= HandleBlockPointsAwarded;
                ArcadeGameManager.Instance.OnLevelCompletedWithTally -= HandleLevelCompletedWithTally;
                ArcadeGameManager.Instance.OnLevelClearPending -= HandleLevelClearPending;
                ArcadeGameManager.Instance.OnPowerdownStateChanged -= HandlePowerdownStateChanged;
                ArcadeGameManager.Instance.OnPowerdownTick -= HandlePowerdownTick;
                BallController.OnAnyBallSpeedChanged -= HandleBallSpeedChanged;
            }
        }

        private void InitializeDisplay()
        {
            if (ArcadeGameManager.Instance != null)
            {
                UpdateScoreDisplay(ArcadeGameManager.Instance.Score, 0);
                UpdateLivesDisplay(ArcadeGameManager.Instance.Lives);
                HandleGameStateChanged(ArcadeGameManager.Instance.State);
                HandleShieldStateChanged(ArcadeGameManager.Instance.IsShieldActive, ArcadeGameManager.Instance.ShieldTimeRemaining);
                HandleActiveBallCountChanged(ArcadeGameManager.Instance.ActiveBallCount);
                HandlePaddleExpandStateChanged(ArcadeGameManager.Instance.IsPaddleExpanded, ArcadeGameManager.Instance.PaddleExpandTimeRemaining);
                HandleScoreMultiplierStateChanged(ArcadeGameManager.Instance.ActiveScoreMultiplier > 1, ArcadeGameManager.Instance.ActiveScoreMultiplier, ArcadeGameManager.Instance.MultiplierTimeRemaining);
                HandleLaserPowerupStateChanged(ArcadeGameManager.Instance.IsLaserActive, ArcadeGameManager.Instance.LaserTimeRemaining);
                HandleClutchStateChanged(ArcadeGameManager.Instance.IsClutchModeActive, ArcadeGameManager.Instance.ClutchTimeRemaining, ArcadeGameManager.Instance.ClutchMultiplier);
                HandlePowerdownStateChanged(BlockSpecialType.PaddleShortener, ArcadeGameManager.Instance.IsPaddleShortened, ArcadeGameManager.Instance.PaddleShortenTimeRemaining);
                HandlePowerdownStateChanged(BlockSpecialType.PaddleSlower, ArcadeGameManager.Instance.IsPaddleSlowed, ArcadeGameManager.Instance.PaddleSlowTimeRemaining);
                HandlePowerdownStateChanged(BlockSpecialType.BallSizeDecreaser, ArcadeGameManager.Instance.IsBallSizeDecreased, ArcadeGameManager.Instance.BallSizeDecreaseTimeRemaining);
                HandlePowerdownStateChanged(BlockSpecialType.BallSlower, ArcadeGameManager.Instance.IsBallSlowed, ArcadeGameManager.Instance.BallSlowTimeRemaining);
                HandlePowerdownStateChanged(BlockSpecialType.PaddleFreezer, ArcadeGameManager.Instance.IsPaddleFrozen, ArcadeGameManager.Instance.PaddleFreezeTimeRemaining);
                if (timerLabel != null) timerLabel.text = HighScoreManager.FormatTime(ArcadeGameManager.Instance.LevelElapsedTime);
                HandleVolleyComboChanged(ArcadeGameManager.Instance.CurrentVolleyStreak, ArcadeGameManager.Instance.CurrentVolleyMultiplier);
            }

            if (ArcadeAudioManager.Instance != null)
            {
                if (sliderVolume != null) sliderVolume.value = ArcadeAudioManager.Instance.Volume;
                if (toggleMute != null) toggleMute.value = ArcadeAudioManager.Instance.IsMuted;
                if (sliderMusicVolume != null) sliderMusicVolume.value = ArcadeAudioManager.Instance.MusicVolume;
                if (toggleMusicMute != null)
                {
                    toggleMusicMute.value = ArcadeAudioManager.Instance.IsMusicMuted;
                    UpdateToggleMuteIcon(toggleMusicMute, ArcadeAudioManager.Instance.IsMusicMuted);
                }
                UpdateMuteButtonIcon();
            }

            UpdateLocalizedTexts();

            UpdatePauseButtonIcon(ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State == GameState.Paused);
            UpdateSpeedDisplay(14f, 1.0f);

            targetFps = PlayerPrefs.GetInt("Arcade_TargetFPS", 60);
            Application.targetFrameRate = targetFps;
            if (btnFps != null) btnFps.text = $"{targetFps} FPS";

            if (levelClearBanner != null)
            {
                levelClearBanner.AddToClassList("level-clear-banner-hidden");
            }
        }

        private void SetLanguage(GameLanguage lang)
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            LocalizationManager.SetLanguage(lang);
        }

        private void UpdateLocalizedTexts()
        {
            bool isTr = LocalizationManager.CurrentLanguage == GameLanguage.Turkish;
            if (btnLangEn != null)
            {
                if (!isTr) btnLangEn.AddToClassList("lang-btn-active");
                else btnLangEn.RemoveFromClassList("lang-btn-active");
            }
            if (btnLangTr != null)
            {
                if (isTr) btnLangTr.AddToClassList("lang-btn-active");
                else btnLangTr.RemoveFromClassList("lang-btn-active");
            }

            if (launchText != null) launchText.text = LocalizationManager.Get("hud_tap_launch", "TAP TO LAUNCH");
            if (modalTitleSettings != null) modalTitleSettings.text = LocalizationManager.Get("settings_title", "SETTINGS");
            if (labelSettingLang != null) labelSettingLang.text = LocalizationManager.Get("settings_language", "Language");
            if (labelSettingSfx != null) labelSettingSfx.text = LocalizationManager.Get("settings_sfx_vol", "SFX Volume");
            if (labelSettingSfxMute != null) labelSettingSfxMute.text = LocalizationManager.Get("settings_sfx_mute", "Mute SFX");
            if (labelSettingMusic != null) labelSettingMusic.text = LocalizationManager.Get("settings_music_vol", "Music Volume");
            if (labelSettingMusicMute != null) labelSettingMusicMute.text = LocalizationManager.Get("settings_music_mute", "Mute Music");
            if (labelSettingFps != null) labelSettingFps.text = LocalizationManager.Get("settings_target_fps", "Target Frame Rate");
            if (btnCloseOptions != null) btnCloseOptions.text = LocalizationManager.Get("btn_back", "BACK");
            if (btnResume != null) btnResume.text = LocalizationManager.Get("btn_resume", "RESUME");
            if (btnLevelSelectPause != null) btnLevelSelectPause.text = LocalizationManager.Get("menu_select_level", "SELECT LEVEL");
            if (btnHighscoresPause != null) btnHighscoresPause.text = LocalizationManager.Get("menu_high_scores", "HIGH SCORES");
            if (btnHowToPlayPause != null) btnHowToPlayPause.text = LocalizationManager.Get("menu_how_to_play", "HOW TO PLAY");
            if (btnCreditsPause != null) btnCreditsPause.text = LocalizationManager.Get("menu_credits", "CREDITS");
            if (btnRestartPause != null) btnRestartPause.text = LocalizationManager.Get("btn_restart", "RESTART LEVEL");
            if (btnMenuPause != null) btnMenuPause.text = LocalizationManager.Get("btn_main_menu", "MAIN MENU");

            if (modalTitleCredits != null) modalTitleCredits.text = LocalizationManager.Get("credits_title", "CREDITS");
            if (modalTitleSelectLevel != null) modalTitleSelectLevel.text = LocalizationManager.Get("level_select_title", "SELECT LEVEL");
            if (modalSubSelectLevel != null) modalSubSelectLevel.text = LocalizationManager.Get("level_select_subtitle", "Choose your next challenge");
            if (labelDiffSubtitle != null) labelDiffSubtitle.text = LocalizationManager.Get("level_difficulty", "DIFFICULTY");
            if (labelStarsSubtitle != null) labelStarsSubtitle.text = "STARS";
            if (labelTimeSubtitle != null) labelTimeSubtitle.text = LocalizationManager.Get("level_best_time", "BEST TIME");
            if (btnStartSelectedLevel != null) btnStartSelectedLevel.text = LocalizationManager.Get("level_play_btn", "PLAY LEVEL");
            if (btnCloseCredits != null) btnCloseCredits.text = LocalizationManager.Get("btn_back", "BACK");
            if (btnCloseLevelModal != null) btnCloseLevelModal.text = LocalizationManager.Get("btn_back", "BACK");
            if (btnCloseHowToPlay != null) btnCloseHowToPlay.text = LocalizationManager.Get("btn_back", "BACK");

            if (modalTitleHowToPlay != null) modalTitleHowToPlay.text = LocalizationManager.Get("guide_title", "HOW TO PLAY");
            if (guideH1 != null) guideH1.text = LocalizationManager.Get("guide_h1", "1. CONTROLS & LAUNCH");
            if (guideP1 != null) guideP1.text = LocalizationManager.Get("guide_p1", "• Drag touch or mouse horizontally to guide the paddle.\n• Keyboard: A/D or Left/Right arrows.\n• Tap screen, click, or press Up/Space to launch.");
            if (guideH2 != null) guideH2.text = LocalizationManager.Get("guide_h2", "2. DYNAMIC PADDLE DEFLECTION");
            if (guideP2 != null) guideP2.text = LocalizationManager.Get("guide_p2", "• Rebounds calculate angle from hit offset! Center bounces vertical (90°); outer edges yield sharp angles (30°) for surgical precision.");
            if (guideH3 != null) guideH3.text = LocalizationManager.Get("guide_h3", "3. SCORING & GLASS BRICKS");
            if (guideP3 != null) guideP3.text = LocalizationManager.Get("guide_p3", "• Red = 10 pts • Green = 20 pts • Blue = 30 pts.\n• Glass Bricks require 2 hits: Hit 1 shatters the protective shell; Hit 2 breaks the brick for 2x points!");
            if (guideH4 != null) guideH4.text = LocalizationManager.Get("guide_h4", "4. POWERUPS & HAZARDS");
            if (guideP4 != null) guideP4.text = LocalizationManager.Get("guide_p4", "• Expander: Widens paddle by +10%.\n• Multipliers: 2X, 3X, 4X, 5X bonus points per block.\n• Shield: Kinetic barrier protects for 10s.\n• Multi-Ball: 3 balls! Extra balls don't lose lives.\n• Laser Blaster: Twin cannons pierce brick rows!\n• Bombs: Radial detonation explodes neighbors!\n• Extra Heart: Grants +1 life (up to 5 max).\n• Clutch Countdown: When 1 brick remains, 12s timer yields up to 10X score or Railgun vaporizes it!");
        }

        public void UpdateScoreDisplay(int currentScore, int delta)
        {
            if (scoreLabel != null) scoreLabel.text = $"SCORE: {currentScore}";
            if (highscoreLabel != null && ArcadeGameManager.Instance != null)
                highscoreLabel.text = $"BEST: {ArcadeGameManager.Instance.HighScore}";
        }

        public void UpdateLivesDisplay(int lives)
        {
            if (livesCountLabel != null)
            {
                livesCountLabel.text = $"x{lives}";
            }

            if (lifePips == null) return;
            for (int i = 0; i < lifePips.Length; i++)
            {
                if (lifePips[i] == null) continue;

                // Base 3 pips are always displayed (active or lost outline).
                // Bonus pips 4 and 5 are only shown when lives > 3 or when active.
                if (i >= 3)
                {
                    if (i < lives)
                    {
                        lifePips[i].RemoveFromClassList("pip-hidden");
                        lifePips[i].style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        lifePips[i].AddToClassList("pip-hidden");
                        lifePips[i].style.display = DisplayStyle.None;
                    }
                }
                else
                {
                    lifePips[i].RemoveFromClassList("pip-hidden");
                    lifePips[i].style.display = DisplayStyle.Flex;
                }

                if (i < lives)
                {
                    lifePips[i].AddToClassList("pip-active");
                    lifePips[i].RemoveFromClassList("pip-lost");
                    if (heartFillSprite != null)
                    {
                        lifePips[i].style.backgroundImage = new StyleBackground(heartFillSprite);
                        lifePips[i].style.unityBackgroundImageTintColor = new StyleColor(new Color(1f, 0.231f, 0.337f, 1f)); // #ff3b56
                    }
                }
                else
                {
                    lifePips[i].RemoveFromClassList("pip-active");
                    lifePips[i].AddToClassList("pip-lost");
                    if (heartEmptySprite != null)
                    {
                        lifePips[i].style.backgroundImage = new StyleBackground(heartEmptySprite);
                        lifePips[i].style.unityBackgroundImageTintColor = new StyleColor(new Color(1f, 0.231f, 0.337f, 0.35f)); // dimmed red outline
                    }
                }
            }
        }

        public void AnimateFlyingHeart(Vector3 worldPosition)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(DoFlyingHeartAnimation(worldPosition));
            }
        }

        private System.Collections.IEnumerator DoFlyingHeartAnimation(Vector3 worldPos)
        {
            if (root == null) yield break;

            Camera cam = Camera.main;
            Vector2 startPanelPos;
            if (cam != null)
            {
                Vector3 screenPt = cam.WorldToScreenPoint(worldPos);
                startPanelPos = new Vector2(screenPt.x, Screen.height - screenPt.y);
                if (root.panel != null)
                {
                    startPanelPos = RuntimePanelUtils.ScreenToPanel(root.panel, startPanelPos);
                }
            }
            else
            {
                startPanelPos = new Vector2(root.resolvedStyle.width * 0.5f, root.resolvedStyle.height * 0.5f);
            }

            int targetIndex = Mathf.Clamp(ArcadeGameManager.Instance != null ? ArcadeGameManager.Instance.Lives - 1 : 2, 0, lifePips.Length - 1);
            VisualElement targetPip = (lifePips != null && targetIndex >= 0 && targetIndex < lifePips.Length) ? lifePips[targetIndex] : null;

            Vector2 targetPanelPos;
            if (targetPip != null && targetPip.worldBound.width > 0)
            {
                targetPanelPos = targetPip.worldBound.center;
            }
            else
            {
                targetPanelPos = new Vector2(root.resolvedStyle.width * 0.5f, 35f);
            }

            VisualElement flyingHeart = new VisualElement();
            flyingHeart.AddToClassList("flying-heart");
            if (heartFillSprite != null)
            {
                flyingHeart.style.backgroundImage = new StyleBackground(heartFillSprite);
            }
            flyingHeart.pickingMode = PickingMode.Ignore;
            flyingHeart.style.left = startPanelPos.x - 20f;
            flyingHeart.style.top = startPanelPos.y - 20f;
            root.Add(flyingHeart);

            float duration = 0.55f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = Mathf.SmoothStep(0f, 1f, t);

                Vector2 currentPos = Vector2.Lerp(startPanelPos, targetPanelPos, curvedT);
                currentPos.y += Mathf.Sin(t * Mathf.PI) * -60f;

                flyingHeart.style.left = currentPos.x - 20f;
                flyingHeart.style.top = currentPos.y - 20f;

                float scale = Mathf.Lerp(1.5f, 1.0f, t);
                flyingHeart.style.scale = new StyleScale(new Scale(new Vector2(scale, scale)));

                yield return null;
            }

            if (root.Contains(flyingHeart))
            {
                root.Remove(flyingHeart);
            }

            if (targetPip != null)
            {
                targetPip.AddToClassList("pip-pop");
                yield return new WaitForSecondsRealtime(0.18f);
                targetPip.RemoveFromClassList("pip-pop");
            }
        }

        public System.Collections.Generic.List<VisualElement> GetPauseButtons()
        {
            var list = new System.Collections.Generic.List<VisualElement>();
            if (btnResume != null) list.Add(btnResume);
            if (btnLevelSelectPause != null) list.Add(btnLevelSelectPause);
            if (btnOptionsPause != null) list.Add(btnOptionsPause);
            if (btnHighscoresPause != null) list.Add(btnHighscoresPause);
            if (btnHowToPlayPause != null) list.Add(btnHowToPlayPause);
            if (btnCreditsPause != null) list.Add(btnCreditsPause);
            if (btnRestartPause != null) list.Add(btnRestartPause);
            if (btnMenuPause != null) list.Add(btnMenuPause);
            return list;
        }

        public System.Collections.Generic.List<VisualElement> GetLevelClearButtons()
        {
            var list = new System.Collections.Generic.List<VisualElement>();
            if (btnNextLevel != null) list.Add(btnNextLevel);
            if (btnReplayLevel != null) list.Add(btnReplayLevel);
            if (btnClearMenu != null) list.Add(btnClearMenu);
            return list;
        }

        public System.Collections.Generic.List<VisualElement> GetGameOverButtons()
        {
            var list = new System.Collections.Generic.List<VisualElement>();
            if (btnRetry != null) list.Add(btnRetry);
            if (btnOverMenu != null) list.Add(btnOverMenu);
            return list;
        }

        private void HandleGameStateChanged(GameState state)
        {
            UpdatePauseButtonIcon(state == GameState.Paused);

            // Banner visibility
            if (launchBanner != null)
            {
                if (state == GameState.ReadyToLaunch || state == GameState.BallLost)
                {
                    launchBanner.RemoveFromClassList("launch-banner-hidden");
                    UIAnimationHelper.StartBreathe(this, launchBanner, "launch-banner-pulse", 0.55f);
                    if (launchFingerIcon != null)
                    {
                        UIAnimationHelper.StartHorizontalSwipe(this, launchFingerIcon, UIAnimationHelper.ClassLaunchFingerLeft, UIAnimationHelper.ClassLaunchFingerRight, 0.55f);
                    }
                }
                else
                {
                    UIAnimationHelper.StopActiveAnimation(this, launchBanner);
                    if (launchFingerIcon != null)
                    {
                        UIAnimationHelper.StopActiveAnimation(this, launchFingerIcon);
                        launchFingerIcon.RemoveFromClassList(UIAnimationHelper.ClassLaunchFingerLeft);
                        launchFingerIcon.RemoveFromClassList(UIAnimationHelper.ClassLaunchFingerRight);
                    }
                    launchBanner.RemoveFromClassList("launch-banner-pulse");
                    launchBanner.AddToClassList("launch-banner-hidden");
                }
            }

            if (levelClearBanner != null && state != GameState.Playing)
            {
                levelClearBanner.AddToClassList("level-clear-banner-hidden");
            }

            // Pause Modal
            if (pauseModal != null)
            {
                var card = pauseModal.Q(className: "modal-card");
                if (state == GameState.Paused)
                {
                    UIAnimationHelper.ShowModal(this, pauseModal, card, GetPauseButtons());
                }
                else
                {
                    UIAnimationHelper.HideModal(this, pauseModal, card);
                }
            }

            // Level Clear Modal
            if (levelClearModal != null)
            {
                var card = levelClearModal.Q(className: "scorecard-card") ?? levelClearModal.Q(className: "modal-card");
                if (state == GameState.LevelClear)
                {
                    if (clearScoreLabel != null && ArcadeGameManager.Instance != null)
                        clearScoreLabel.text = ArcadeGameManager.Instance.Score.ToString("#,##0");

                    if (btnNextLevel != null)
                    {
                        if (levelGenerator == null) levelGenerator = FindAnyObjectByType<LevelGenerator>();
                        int currentLvl = levelGenerator != null && levelGenerator.CurrentConfig != null ? levelGenerator.CurrentConfig.LevelNumber : 1;
                        int nextLvl = currentLvl + 1;
                        int total = levelGenerator != null ? levelGenerator.TotalLevels : 30;
                        btnNextLevel.text = nextLvl > total ? "PLAY AGAIN (LOOP)" : $"NEXT LEVEL ({nextLvl})";
                    }

                    if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.CurrentLevelSummary.levelNumber > 0)
                    {
                        HandleLevelCompletedWithTally(ArcadeGameManager.Instance.CurrentLevelSummary);
                    }

                    UIAnimationHelper.ShowModal(this, levelClearModal, card, GetLevelClearButtons());
                }
                else
                {
                    UIAnimationHelper.HideModal(this, levelClearModal, card);
                }
            }

            // Game Over Modal
            if (gameOverModal != null)
            {
                var card = gameOverModal.Q(className: "modal-card");
                if (state == GameState.GameOver)
                {
                    if (overScoreLabel != null && ArcadeGameManager.Instance != null)
                        overScoreLabel.text = $"FINAL SCORE: {ArcadeGameManager.Instance.Score}";
                    if (overHighLabel != null && ArcadeGameManager.Instance != null)
                        overHighLabel.text = $"HIGH SCORE: {ArcadeGameManager.Instance.HighScore}";
                    UIAnimationHelper.ShowModal(this, gameOverModal, card, GetGameOverButtons());
                }
                else
                {
                    UIAnimationHelper.HideModal(this, gameOverModal, card);
                }
            }
        }

        public void HandleShieldStateChanged(bool active, float remaining)
        {
            if (shieldStatusBadge == null) return;
            if (active)
            {
                shieldStatusBadge.RemoveFromClassList("powerup-hidden");
                shieldStatusBadge.style.display = DisplayStyle.Flex;
                UIAnimationHelper.PunchElement(this, shieldStatusBadge, "ui-punch-pop", 0.14f);
                if (shieldTimerLabel != null) shieldTimerLabel.text = $"{Mathf.CeilToInt(remaining)}s";
            }
            else
            {
                shieldStatusBadge.AddToClassList("powerup-hidden");
                shieldStatusBadge.style.display = DisplayStyle.None;
            }
        }

        public void HandleShieldTick(float timeRemaining)
        {
            if (shieldTimerLabel != null)
                shieldTimerLabel.text = $"{Mathf.CeilToInt(timeRemaining)}s";
        }

        public void HandleActiveBallCountChanged(int count)
        {
            if (multiballStatusBadge == null) return;
            if (count > 1)
            {
                multiballStatusBadge.RemoveFromClassList("powerup-hidden");
                multiballStatusBadge.style.display = DisplayStyle.Flex;
                UIAnimationHelper.PunchElement(this, multiballStatusBadge, "ui-punch-pop", 0.14f);
                if (multiballCountLabel != null) multiballCountLabel.text = $"{count} BALLS";
            }
            else
            {
                multiballStatusBadge.AddToClassList("powerup-hidden");
                multiballStatusBadge.style.display = DisplayStyle.None;
            }
        }

        public void HandlePaddleExpandStateChanged(bool active, float remaining)
        {
            if (paddleStatusBadge == null) return;
            if (active)
            {
                paddleStatusBadge.RemoveFromClassList("powerup-hidden");
                paddleStatusBadge.style.display = DisplayStyle.Flex;
                UIAnimationHelper.PunchElement(this, paddleStatusBadge, "ui-punch-pop", 0.14f);
                if (paddleTimerLabel != null) paddleTimerLabel.text = $"{Mathf.CeilToInt(remaining)}s";
            }
            else
            {
                paddleStatusBadge.AddToClassList("powerup-hidden");
                paddleStatusBadge.style.display = DisplayStyle.None;
            }
        }

        public void HandlePaddleExpandTick(float timeRemaining)
        {
            if (paddleTimerLabel != null)
                paddleTimerLabel.text = $"{Mathf.CeilToInt(timeRemaining)}s";
        }

        public void HandleScoreMultiplierStateChanged(bool active, int multiplier, float remaining)
        {
            if (multiplierStatusBadge == null) return;
            if (active && multiplier > 1)
            {
                multiplierStatusBadge.RemoveFromClassList("powerup-hidden");
                multiplierStatusBadge.style.display = DisplayStyle.Flex;
                UIAnimationHelper.PunchElement(this, multiplierStatusBadge, "ui-punch-pop", 0.14f);

                var iconMultiplier = multiplierStatusBadge.Q<VisualElement>("multiplier-status-icon");
                if (iconMultiplier != null && multiplierSprite != null)
                {
                    iconMultiplier.style.backgroundImage = new StyleBackground(multiplierSprite);
                }

                multiplierStatusBadge.RemoveFromClassList("mult-tier-2x");
                multiplierStatusBadge.RemoveFromClassList("mult-tier-3x");
                multiplierStatusBadge.RemoveFromClassList("mult-tier-4x");
                multiplierStatusBadge.RemoveFromClassList("mult-tier-5x");
                multiplierStatusBadge.AddToClassList($"mult-tier-{multiplier}x");

                if (multiplierValueLabel != null) multiplierValueLabel.text = $"{multiplier}X";
                if (multiplierTimerLabel != null) multiplierTimerLabel.text = $"{Mathf.CeilToInt(remaining)}s";
            }
            else
            {
                multiplierStatusBadge.AddToClassList("powerup-hidden");
                multiplierStatusBadge.style.display = DisplayStyle.None;
            }
        }

        public void HandleScoreMultiplierTick(float timeRemaining)
        {
            if (multiplierTimerLabel != null)
                multiplierTimerLabel.text = $"{Mathf.CeilToInt(timeRemaining)}s";
        }

        public void HandleLaserPowerupStateChanged(bool active, float remaining)
        {
            if (laserStatusBadge == null) return;
            if (active)
            {
                laserStatusBadge.RemoveFromClassList("powerup-hidden");
                laserStatusBadge.style.display = DisplayStyle.Flex;
                UIAnimationHelper.PunchElement(this, laserStatusBadge, "ui-punch-pop", 0.14f);
                if (laserTimerLabel != null) laserTimerLabel.text = $"{Mathf.CeilToInt(remaining)}s";
            }
            else
            {
                laserStatusBadge.AddToClassList("powerup-hidden");
                laserStatusBadge.style.display = DisplayStyle.None;
            }
        }

        public void HandleLaserPowerupTick(float timeRemaining)
        {
            if (laserTimerLabel != null)
                laserTimerLabel.text = $"{Mathf.CeilToInt(timeRemaining)}s";
        }

        public void HandlePowerdownStateChanged(BlockSpecialType type, bool active, float remaining)
        {
            VisualElement badge = null;
            Label timerLbl = null;

            switch (type)
            {
                case BlockSpecialType.PaddleShortener:
                    badge = paddleShrinkStatusBadge;
                    timerLbl = paddleShrinkTimerLabel;
                    break;
                case BlockSpecialType.PaddleSlower:
                    badge = paddleSlowStatusBadge;
                    timerLbl = paddleSlowTimerLabel;
                    break;
                case BlockSpecialType.BallSizeDecreaser:
                    badge = ballShrinkStatusBadge;
                    timerLbl = ballShrinkTimerLabel;
                    break;
                case BlockSpecialType.BallSlower:
                    badge = ballSlowStatusBadge;
                    timerLbl = ballSlowTimerLabel;
                    break;
                case BlockSpecialType.PaddleFreezer:
                    badge = paddleFreezeStatusBadge;
                    timerLbl = paddleFreezeTimerLabel;
                    break;
            }

            if (badge == null) return;

            if (active)
            {
                badge.RemoveFromClassList("powerup-hidden");
                badge.style.display = DisplayStyle.Flex;
                UIAnimationHelper.PunchElement(this, badge, "ui-punch-pop", 0.14f);
                if (timerLbl != null) timerLbl.text = $"{Mathf.CeilToInt(remaining)}s";
            }
            else
            {
                badge.AddToClassList("powerup-hidden");
                badge.style.display = DisplayStyle.None;
            }
        }

        public void HandlePowerdownTick(BlockSpecialType type, float timeRemaining)
        {
            Label timerLbl = type switch
            {
                BlockSpecialType.PaddleShortener => paddleShrinkTimerLabel,
                BlockSpecialType.PaddleSlower => paddleSlowTimerLabel,
                BlockSpecialType.BallSizeDecreaser => ballShrinkTimerLabel,
                BlockSpecialType.BallSlower => ballSlowTimerLabel,
                BlockSpecialType.PaddleFreezer => paddleFreezeTimerLabel,
                _ => null
            };

            if (timerLbl != null)
            {
                timerLbl.text = $"{Mathf.CeilToInt(timeRemaining)}s";
            }
        }

        public void HandleClutchStateChanged(bool active, float remaining, int multiplier)
        {
            if (clutchStatusBadge == null) return;
            if (active)
            {
                clutchStatusBadge.RemoveFromClassList("powerup-hidden");
                clutchStatusBadge.style.display = DisplayStyle.Flex;
                UIAnimationHelper.PunchElement(this, clutchStatusBadge, "ui-punch-pop", 0.14f);
                if (clutchMultiplierLabel != null) clutchMultiplierLabel.text = $"{multiplier}X";
                if (clutchTimerLabel != null) clutchTimerLabel.text = $"{Mathf.CeilToInt(remaining)}s";
            }
            else
            {
                clutchStatusBadge.AddToClassList("powerup-hidden");
                clutchStatusBadge.style.display = DisplayStyle.None;
            }
        }

        public void HandleClutchTick(float timeRemaining, int multiplier)
        {
            if (clutchMultiplierLabel != null)
                clutchMultiplierLabel.text = $"{multiplier}X";
            if (clutchTimerLabel != null)
                clutchTimerLabel.text = $"{Mathf.CeilToInt(timeRemaining)}s";
        }

        public void HandleLevelTimerTick(float elapsed)
        {
            if (timerLabel != null)
            {
                timerLabel.text = HighScoreManager.FormatTime(elapsed);
            }
        }

        public void HandleVolleyComboChanged(int streak, int multiplier)
        {
            if (comboStatusBadge == null) return;

            if (multiplier > 1)
            {
                if (comboEndedCoroutine != null)
                {
                    StopCoroutine(comboEndedCoroutine);
                    comboEndedCoroutine = null;
                }

                previousVolleyMultiplier = multiplier;

                comboStatusBadge.RemoveFromClassList("powerup-hidden");
                comboStatusBadge.style.display = DisplayStyle.Flex;

                if (comboStatusIcon != null)
                {
                    comboStatusIcon.style.display = DisplayStyle.Flex;
                    if (multiplierSprite != null)
                    {
                        comboStatusIcon.style.backgroundImage = new StyleBackground(multiplierSprite);
                    }
                }

                comboStatusBadge.RemoveFromClassList("mult-tier-2x");
                comboStatusBadge.RemoveFromClassList("mult-tier-3x");
                comboStatusBadge.RemoveFromClassList("mult-tier-4x");
                comboStatusBadge.RemoveFromClassList("mult-tier-5x");
                comboStatusBadge.AddToClassList($"mult-tier-{Mathf.Clamp(multiplier, 2, 5)}x");
                UIAnimationHelper.PunchElement(this, comboStatusBadge, "combo-pop", 0.14f);

                if (comboLabel != null)
                {
                    comboLabel.text = $" x{multiplier} COMBO";
                }
            }
            else
            {
                if (previousVolleyMultiplier > 1)
                {
                    previousVolleyMultiplier = 1;
                    if (comboEndedCoroutine != null) StopCoroutine(comboEndedCoroutine);
                    if (comboLabel != null) comboLabel.text = "COMBO ENDED";
                    if (comboStatusIcon != null) comboStatusIcon.style.display = DisplayStyle.None;

                    if (Application.isPlaying && gameObject.activeInHierarchy)
                    {
                        comboEndedCoroutine = StartCoroutine(ShowComboEndedRoutine());
                    }
                    else
                    {
                        comboStatusBadge.AddToClassList("powerup-hidden");
                        comboStatusBadge.style.display = DisplayStyle.None;
                    }
                }
                else
                {
                    comboStatusBadge.AddToClassList("powerup-hidden");
                    comboStatusBadge.style.display = DisplayStyle.None;
                }
            }
        }

        private IEnumerator ShowComboEndedRoutine()
        {
            if (comboStatusBadge != null)
            {
                comboStatusBadge.RemoveFromClassList("powerup-hidden");
                comboStatusBadge.style.display = DisplayStyle.Flex;
                if (comboStatusIcon != null) comboStatusIcon.style.display = DisplayStyle.None;
                if (comboLabel != null) comboLabel.text = "COMBO ENDED";
            }

            yield return new WaitForSecondsRealtime(1.2f);

            if (comboStatusBadge != null)
            {
                comboStatusBadge.AddToClassList("powerup-hidden");
                comboStatusBadge.style.display = DisplayStyle.None;
                if (comboStatusIcon != null) comboStatusIcon.style.display = DisplayStyle.Flex;
            }
            comboEndedCoroutine = null;
        }

        public void HandleBlockPointsAwarded(Vector3 worldPos, int awardedPoints, int totalMultiplier, string bonusTag)
        {
            if (awardedPoints <= 0) return;

            if (worldPos != Vector3.zero)
            {
                AnimateFlyingScore(worldPos, awardedPoints, totalMultiplier, bonusTag);
            }
            else if (scoreDeltaLabel != null)
            {
                if (scoreDeltaCoroutine != null) StopCoroutine(scoreDeltaCoroutine);
                if (gameObject.activeInHierarchy) scoreDeltaCoroutine = StartCoroutine(DoScoreDeltaAnimation(awardedPoints, totalMultiplier));
            }
        }

        public void AnimateFlyingScore(Vector3 worldPosition, int awardedPoints, int totalMultiplier, string bonusTag)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(DoFlyingScoreAnimation(worldPosition, awardedPoints, totalMultiplier, bonusTag));
            }
        }

        private System.Collections.IEnumerator DoFlyingScoreAnimation(Vector3 worldPos, int points, int multiplier, string bonusTag)
        {
            if (root == null) yield break;

            Camera cam = Camera.main;
            Vector2 startPanelPos;
            if (cam != null)
            {
                Vector3 screenPt = cam.WorldToScreenPoint(worldPos);
                startPanelPos = new Vector2(screenPt.x, Screen.height - screenPt.y);
                if (root.panel != null)
                {
                    startPanelPos = RuntimePanelUtils.ScreenToPanel(root.panel, startPanelPos);
                }
            }
            else
            {
                startPanelPos = new Vector2(root.resolvedStyle.width * 0.5f, root.resolvedStyle.height * 0.5f);
            }

            Vector2 targetPanelPos;
            if (scoreContainer != null && scoreContainer.worldBound.width > 0)
            {
                targetPanelPos = scoreContainer.worldBound.center;
            }
            else if (scoreLabel != null && scoreLabel.worldBound.width > 0)
            {
                targetPanelPos = scoreLabel.worldBound.center;
            }
            else
            {
                targetPanelPos = new Vector2(root.resolvedStyle.width * 0.5f, 35f);
            }

            Label flyingLabel = new Label();
            flyingLabel.AddToClassList("flying-score");

            // Format label text and apply visual hierarchy classes
            if (!string.IsNullOrEmpty(bonusTag) && (bonusTag.Contains("BOMB") || bonusTag.Contains("CLUTCH")))
            {
                flyingLabel.text = $"+{points} {bonusTag}";
                if (bonusTag.Contains("CLUTCH"))
                    flyingLabel.AddToClassList("flying-score-clutch");
                else
                    flyingLabel.AddToClassList("flying-score-bomb");
            }
            else if (multiplier > 1)
            {
                flyingLabel.text = $"+{points} x{multiplier}";
                flyingLabel.AddToClassList("flying-score-combo");
            }
            else
            {
                flyingLabel.text = $"+{points}";
                flyingLabel.AddToClassList("flying-score-normal");
            }

            flyingLabel.pickingMode = PickingMode.Ignore;
            flyingLabel.style.left = startPanelPos.x - 30f;
            flyingLabel.style.top = startPanelPos.y - 12f;
            root.Add(flyingLabel);

            // Give a slight arc variance so multiple exploding blocks (e.g. bomb chains) fan out distinctly
            float arcX = (startPanelPos.x - targetPanelPos.x) * 0.25f + UnityEngine.Random.Range(-20f, 20f);
            float arcY = -40f - UnityEngine.Random.Range(10f, 30f);

            float duration = 0.65f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curvedT = Mathf.SmoothStep(0f, 1f, t);

                Vector2 currentPos = Vector2.Lerp(startPanelPos, targetPanelPos, curvedT);
                float sinT = Mathf.Sin(t * Mathf.PI);
                currentPos.x += sinT * arcX;
                currentPos.y += sinT * arcY;

                flyingLabel.style.left = currentPos.x - 30f;
                flyingLabel.style.top = currentPos.y - 12f;

                // Scale: Pop up quickly to 1.35x, then settle and scale down to 0.85x on arrival
                float scale = t < 0.2f
                    ? Mathf.Lerp(0.8f, 1.35f, t / 0.2f)
                    : Mathf.Lerp(1.35f, 0.85f, (t - 0.2f) / 0.8f);

                flyingLabel.style.scale = new StyleScale(new Scale(new Vector2(scale, scale)));

                // Fade out slightly near the very end
                if (t > 0.85f)
                {
                    flyingLabel.style.opacity = Mathf.Lerp(1f, 0.2f, (t - 0.85f) / 0.15f);
                }

                yield return null;
            }

            if (root.Contains(flyingLabel))
            {
                root.Remove(flyingLabel);
            }

            // Punch score container on arrival
            PulseScorePod();
        }

        public void PulseScorePod()
        {
            if (scoreContainer == null && scoreLabel == null) return;
            if (gameObject.activeInHierarchy)
            {
                if (scorePulseCoroutine != null) StopCoroutine(scorePulseCoroutine);
                scorePulseCoroutine = StartCoroutine(DoScorePulse());
            }
        }

        private System.Collections.IEnumerator DoScorePulse()
        {
            var target = scoreContainer ?? (VisualElement)scoreLabel;
            if (target == null) yield break;

            target.AddToClassList("score-pop");
            yield return new WaitForSecondsRealtime(0.10f);
            target.RemoveFromClassList("score-pop");
            scorePulseCoroutine = null;
        }

        public void HandleBallSpeedChanged(BallController ball, float currentSpeed, float speedMultiplier)
        {
            UpdateSpeedDisplay(currentSpeed, speedMultiplier);
        }

        public void UpdateSpeedDisplay(float currentSpeed, float speedMultiplier)
        {
            if (speedLabel != null)
            {
                speedLabel.text = $"{speedMultiplier.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}x";
            }

            if (speedContainer != null)
            {
                speedContainer.RemoveFromClassList("speed-tier-slow");
                speedContainer.RemoveFromClassList("speed-tier-normal");
                speedContainer.RemoveFromClassList("speed-tier-boost");
                speedContainer.RemoveFromClassList("speed-tier-turbo");

                if (currentSpeed < 12f)
                {
                    speedContainer.AddToClassList("speed-tier-slow");
                }
                else if (currentSpeed < 17f)
                {
                    speedContainer.AddToClassList("speed-tier-normal");
                }
                else if (currentSpeed < 20f)
                {
                    speedContainer.AddToClassList("speed-tier-boost");
                }
                else
                {
                    speedContainer.AddToClassList("speed-tier-turbo");
                }

                if (gameObject.activeInHierarchy)
                {
                    UIAnimationHelper.PunchElement(this, speedContainer, "ui-punch-pop", 0.12f);
                }
            }
        }

        public void AnimateSpeedPopup(Vector3 worldPosition, string text, SpeedPopupType type)
        {
            if (gameObject.activeInHierarchy && root != null)
            {
                StartCoroutine(DoSpeedPopupAnimation(worldPosition, text, type));
            }
        }

        private System.Collections.IEnumerator DoSpeedPopupAnimation(Vector3 worldPos, string text, SpeedPopupType type)
        {
            if (root == null) yield break;

            Camera cam = Camera.main;
            Vector2 panelPos;
            if (cam != null)
            {
                Vector3 screenPt = cam.WorldToScreenPoint(worldPos);
                panelPos = new Vector2(screenPt.x, Screen.height - screenPt.y);
                if (root.panel != null)
                {
                    panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, panelPos);
                }
            }
            else
            {
                panelPos = new Vector2(root.resolvedStyle.width * 0.5f, root.resolvedStyle.height * 0.5f);
            }

            Label popup = new Label(text);
            popup.AddToClassList("speed-popup");

            switch (type)
            {
                case SpeedPopupType.Turbo:
                    popup.AddToClassList("speed-popup-turbo");
                    break;
                case SpeedPopupType.Dampen:
                    popup.AddToClassList("speed-popup-dampen");
                    break;
                case SpeedPopupType.ActiveStrike:
                    popup.AddToClassList("speed-popup-strike");
                    break;
                case SpeedPopupType.Slowed:
                    popup.AddToClassList("speed-popup-slowed");
                    break;
                case SpeedPopupType.VolleyRamp:
                    popup.AddToClassList("speed-popup-ramp");
                    break;
            }

            popup.pickingMode = PickingMode.Ignore;
            popup.style.left = panelPos.x - 45f;
            popup.style.top = panelPos.y - 25f;
            root.Add(popup);

            float duration = 0.70f;
            float elapsed = 0f;
            float startY = panelPos.y - 25f;
            float driftDist = 42f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float easeT = Mathf.SmoothStep(0f, 1f, t);
                popup.style.top = startY - (easeT * driftDist);

                float scale = t < 0.20f
                    ? Mathf.Lerp(0.75f, 1.25f, t / 0.20f)
                    : Mathf.Lerp(1.25f, 1.0f, (t - 0.20f) / 0.80f);
                popup.style.scale = new StyleScale(new Scale(new Vector2(scale, scale)));

                if (t > 0.65f)
                {
                    popup.style.opacity = Mathf.Lerp(1f, 0f, (t - 0.65f) / 0.35f);
                }

                yield return null;
            }

            if (root.Contains(popup))
            {
                root.Remove(popup);
            }
        }

        private System.Collections.IEnumerator DoScoreDeltaAnimation(int points, int multiplier)
        {
            if (scoreDeltaLabel == null) yield break;

            scoreDeltaLabel.text = multiplier > 1 ? $"+{points} (x{multiplier})" : $"+{points}";
            scoreDeltaLabel.RemoveFromClassList("delta-hidden");
            scoreDeltaLabel.AddToClassList("delta-visible");

            yield return new WaitForSecondsRealtime(0.65f);

            scoreDeltaLabel.RemoveFromClassList("delta-visible");
            scoreDeltaLabel.AddToClassList("delta-hidden");
            scoreDeltaCoroutine = null;
        }

        public void HandleLevelClearPending(float delay, bool wasClearedWithLaser)
        {
            if (levelClearBanner != null)
            {
                if (levelClearBannerText != null)
                {
                    levelClearBannerText.text = "LEVEL CLEARED!";
                }

                if (levelClearBannerSubtext != null)
                {
                    if (wasClearedWithLaser)
                    {
                        levelClearBannerSubtext.text = "CLUTCH OVERCHARGE!";
                    }
                    else if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.LivesLostThisLevel == 0)
                    {
                        levelClearBannerSubtext.text = "FLAWLESS VICTORY!";
                    }
                    else
                    {
                        levelClearBannerSubtext.text = "STAGE COMPLETE!";
                    }
                }

                levelClearBanner.RemoveFromClassList("level-clear-banner-hidden");
            }
        }

        public void HandleLevelCompletedWithTally(LevelSummaryData tally)
        {
            if (levelClearBanner != null)
            {
                levelClearBanner.AddToClassList("level-clear-banner-hidden");
            }

            if (scorecardBlocksVal != null) scorecardBlocksVal.text = tally.blocksDestroyed.ToString();
            if (scorecardComboVal != null) scorecardComboVal.text = $"x{tally.highestCombo}";
            if (scorecardTimeVal != null) scorecardTimeVal.text = $"{HighScoreManager.FormatTime(tally.elapsedTime)} / {HighScoreManager.FormatTime(tally.parTime)}";
            if (scorecardTimeBonusVal != null)
            {
                scorecardTimeBonusVal.text = tally.speedBonus > 0
                    ? $"+{tally.timeBonus} (+500 PAR)"
                    : $"+{tally.timeBonus}";
            }
            if (scorecardFlawlessVal != null)
            {
                scorecardFlawlessVal.text = tally.isFlawless ? "+1,000 FLAWLESS!" : "---";
            }
            if (clearScoreLabel != null)
            {
                clearScoreLabel.text = tally.cumulativeScore.ToString("#,##0");
            }

            // Reset stars to empty initial state
            ResetScorecardStars();

            if (gameObject.activeInHierarchy)
            {
                if (scorecardStarsCoroutine != null) StopCoroutine(scorecardStarsCoroutine);
                scorecardStarsCoroutine = StartCoroutine(AnimateScorecardStars(tally.starsEarned));
            }
            else
            {
                SetScorecardStarsInstant(tally.starsEarned);
            }
        }

        private void ResetScorecardStars()
        {
            VisualElement[] stars = { scorecardStar1, scorecardStar2, scorecardStar3 };
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] != null)
                {
                    stars[i].RemoveFromClassList("star-earned");
                    stars[i].AddToClassList("star-empty");
                }
            }
        }

        private void SetScorecardStarsInstant(int starsEarned)
        {
            VisualElement[] stars = { scorecardStar1, scorecardStar2, scorecardStar3 };
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] != null)
                {
                    if (i < starsEarned)
                    {
                        stars[i].RemoveFromClassList("star-empty");
                        stars[i].AddToClassList("star-earned");
                    }
                    else
                    {
                        stars[i].RemoveFromClassList("star-earned");
                        stars[i].AddToClassList("star-empty");
                    }
                }
            }
        }

        private System.Collections.IEnumerator AnimateScorecardStars(int starsEarned)
        {
            VisualElement[] stars = { scorecardStar1, scorecardStar2, scorecardStar3 };
            yield return new WaitForSecondsRealtime(0.2f);

            for (int i = 0; i < starsEarned && i < stars.Length; i++)
            {
                if (stars[i] != null)
                {
                    stars[i].RemoveFromClassList("star-empty");
                    stars[i].AddToClassList("star-earned");
                    UIAnimationHelper.PunchElement(this, stars[i], "star-pop", 0.16f);
                    if (ArcadeAudioManager.Instance != null)
                    {
                        ArcadeAudioManager.Instance.PlayStarEarned(i + 1);
                    }
                }
                yield return new WaitForSecondsRealtime(0.28f);
            }
            scorecardStarsCoroutine = null;
        }

        private void HandleReplayLevelClicked()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (scorecardStarsCoroutine != null)
            {
                StopCoroutine(scorecardStarsCoroutine);
                scorecardStarsCoroutine = null;
            }
            if (levelClearModal != null) levelClearModal.AddToClassList("modal-hidden");
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.ReplayCurrentLevel();
            }
            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        private void HandleQuickMuteClicked()
        {
            if (ArcadeAudioManager.Instance != null)
            {
                ArcadeAudioManager.Instance.PlayButtonPress();
                ArcadeAudioManager.Instance.ToggleMute();
                if (toggleMute != null) toggleMute.value = ArcadeAudioManager.Instance.IsMuted;
                UpdateMuteButtonIcon();
            }
        }

        public void UpdateMuteButtonIcon(bool? isMutedOverride = null)
        {
            bool isMuted = isMutedOverride ?? (ArcadeAudioManager.Instance != null && ArcadeAudioManager.Instance.IsMuted);

            if (btnQuickMute != null)
            {
                btnQuickMute.text = string.Empty;
                btnQuickMute.tooltip = isMuted ? "Unmute Sound" : "Mute Sound";
            }

            if (iconQuickMute != null)
            {
                // Unmuted -> show TX_Volume_Mute (indicates ability to mute)
                // Muted -> show TX_Volume_Up (indicates ability to unmute)
                iconQuickMute.RemoveFromClassList("icon-volume-mute");
                iconQuickMute.RemoveFromClassList("icon-volume-up");

                if (isMuted)
                {
                    iconQuickMute.AddToClassList("icon-volume-up");
                    if (volumeUpSprite != null)
                    {
                        iconQuickMute.style.backgroundImage = new StyleBackground(volumeUpSprite);
                        iconQuickMute.style.unityBackgroundImageTintColor = new StyleColor(new Color(1f, 0.231f, 0.337f, 1f)); // #ff3b56
                    }
                }
                else
                {
                    iconQuickMute.AddToClassList("icon-volume-mute");
                    if (volumeMuteSprite != null)
                    {
                        iconQuickMute.style.backgroundImage = new StyleBackground(volumeMuteSprite);
                        iconQuickMute.style.unityBackgroundImageTintColor = new StyleColor(new Color(0.886f, 0.910f, 0.941f, 1f)); // #e2e8f0
                    }
                }
            }

            UpdateToggleMuteIcon(isMuted);
        }

        public void UpdateToggleMuteIcon(bool isMuted)
        {
            UpdateToggleMuteIcon(toggleMute, isMuted);
        }

        public void UpdateToggleMuteIcon(Toggle toggle, bool isMuted)
        {
            if (toggle == null) return;
            var checkmark = toggle.Q(className: "unity-toggle__checkmark");
            if (checkmark != null)
            {
                if (isMuted)
                {
                    if (volumeMuteSprite != null)
                        checkmark.style.backgroundImage = new StyleBackground(volumeMuteSprite);
                    checkmark.style.unityBackgroundImageTintColor = new StyleColor(new Color(1f, 0.231f, 0.337f, 1f)); // #ff3b56
                }
                else
                {
                    if (volumeUpSprite != null)
                        checkmark.style.backgroundImage = new StyleBackground(volumeUpSprite);
                    checkmark.style.unityBackgroundImageTintColor = new StyleColor(new Color(0.13f, 0.83f, 0.99f, 1f)); // #21d4fd
                }
            }
        }

        public void UpdatePauseButtonIcon(bool isPaused)
        {
            if (btnQuickPause != null)
            {
                btnQuickPause.text = string.Empty;
                btnQuickPause.tooltip = isPaused ? "Resume Game" : "Pause Game";
            }

            if (iconQuickPause != null)
            {
                iconQuickPause.RemoveFromClassList("icon-pause");
                iconQuickPause.RemoveFromClassList("icon-play");

                if (isPaused)
                {
                    // Paused: show Play icon to indicate ability to resume
                    iconQuickPause.AddToClassList("icon-play");
                    if (playSprite != null)
                    {
                        iconQuickPause.style.backgroundImage = new StyleBackground(playSprite);
                        iconQuickPause.style.unityBackgroundImageTintColor = new StyleColor(new Color(0.149f, 0.910f, 0.522f, 1f)); // #26e885
                    }
                }
                else
                {
                    // Playing / Ready: show Pause icon to indicate ability to pause
                    iconQuickPause.AddToClassList("icon-pause");
                    if (pauseSprite != null)
                    {
                        iconQuickPause.style.backgroundImage = new StyleBackground(pauseSprite);
                        iconQuickPause.style.unityBackgroundImageTintColor = new StyleColor(new Color(0.886f, 0.910f, 0.941f, 1f)); // #e2e8f0
                    }
                }
            }
        }

        public void TriggerSettingsButtonSpin()
        {
            if (iconQuickOptions == null) return;
            settingsRotationAngle += 360f;
            iconQuickOptions.style.rotate = new StyleRotate(new Rotate(Angle.Degrees(settingsRotationAngle)));
        }

        private void HandleQuickOptionsClicked()
        {
            if (optionsModal != null && !optionsModal.ClassListContains("modal-hidden"))
            {
                HideOptions();
            }
            else
            {
                ShowOptions();
            }
        }

        private void HandleQuickPauseClicked()
        {
            UIAnimationHelper.PunchButton(this, btnQuickPause);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.SuppressLaunch(0.35f);
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.TogglePause();
            }
        }

        private void HandleResumeClicked()
        {
            UIAnimationHelper.PunchButton(this, btnResume);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.SuppressLaunch(0.35f);
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.TogglePause();
            }
        }

        private void HandleRestartClicked()
        {
            UIAnimationHelper.PunchButton(this, btnRestartPause);
            UIAnimationHelper.PunchButton(this, btnRetry);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.RestartGame();
            }
        }

        private void HandleNextLevelClicked()
        {
            UIAnimationHelper.PunchButton(this, btnNextLevel);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (scorecardStarsCoroutine != null)
            {
                StopCoroutine(scorecardStarsCoroutine);
                scorecardStarsCoroutine = null;
            }
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.AdvanceToNextLevel();
            }
            else
            {
                if (levelGenerator == null) levelGenerator = FindAnyObjectByType<LevelGenerator>();
                if (levelGenerator != null)
                {
                    levelGenerator.AdvanceToNextLevel();
                }
            }

            if (levelClearModal != null) levelClearModal.AddToClassList("modal-hidden");
            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        private void HandleMenuClicked()
        {
            UIAnimationHelper.PunchButton(this, btnMenuPause);
            UIAnimationHelper.PunchButton(this, btnClearMenu);
            UIAnimationHelper.PunchButton(this, btnOverMenu);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.LoadMainMenu();
            }
        }

        public void ShowOptions()
        {
            TriggerSettingsButtonSpin();
            UIAnimationHelper.PunchButton(this, btnQuickOptions);
            UIAnimationHelper.PunchButton(this, btnOptionsPause);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();

            if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State != GameState.Paused)
            {
                wasPausedByOptions = true;
                ArcadeGameManager.Instance.PauseGame();
            }
            else
            {
                wasPausedByOptions = false;
            }

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }

            var pauseCard = pauseModal != null ? pauseModal.Q(className: "modal-card") : null;
            var optionsCard = optionsModal != null ? optionsModal.Q(className: "modal-card") : null;
            var optionsButtons = new System.Collections.Generic.List<VisualElement>();
            if (btnLangEn != null) optionsButtons.Add(btnLangEn);
            if (btnLangTr != null) optionsButtons.Add(btnLangTr);
            if (btnFps != null) optionsButtons.Add(btnFps);
            if (btnCloseOptions != null) optionsButtons.Add(btnCloseOptions);

            if (pauseModal != null && !pauseModal.ClassListContains("modal-hidden"))
            {
                UIAnimationHelper.HideModal(this, pauseModal, pauseCard, () =>
                {
                    UIAnimationHelper.ShowModal(this, optionsModal, optionsCard, optionsButtons);
                });
            }
            else
            {
                UIAnimationHelper.ShowModal(this, optionsModal, optionsCard, optionsButtons);
            }
        }

        public void HideOptions()
        {
            UIAnimationHelper.PunchButton(this, btnCloseOptions);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();

            var pauseCard = pauseModal != null ? pauseModal.Q(className: "modal-card") : null;
            var optionsCard = optionsModal != null ? optionsModal.Q(className: "modal-card") : null;

            UIAnimationHelper.HideModal(this, optionsModal, optionsCard, () =>
            {
                if (wasPausedByOptions)
                {
                    wasPausedByOptions = false;
                    if (ArcadeGameManager.Instance != null)
                    {
                        ArcadeGameManager.Instance.ResumeGame();
                    }
                }
                else
                {
                    if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State == GameState.Paused && pauseModal != null)
                    {
                        UIAnimationHelper.ShowModal(this, pauseModal, pauseCard, GetPauseButtons());
                    }
                }
            });

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.SuppressLaunch(0.35f);
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        public void ShowLevelSettings()
        {
            UIAnimationHelper.PunchButton(this, btnLevelSettingsPause);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();

            if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State != GameState.Paused)
            {
                wasPausedByLevelSettings = true;
                ArcadeGameManager.Instance.PauseGame();
            }
            else
            {
                wasPausedByLevelSettings = false;
            }

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }

            if (levelGenerator == null) levelGenerator = FindAnyObjectByType<LevelGenerator>();

            if (levelGenerator != null && levelGenerator.CurrentConfig != null)
            {
                activeEditableConfig = levelGenerator.CurrentConfig.Clone();
            }
            else
            {
                activeEditableConfig = ScriptableObject.CreateInstance<LevelConfiguration>();
            }

            UpdateLevelSettingsUI();

            var pauseCard = pauseModal != null ? pauseModal.Q(className: "modal-card") : null;
            var settingsCard = levelSettingsModal != null ? levelSettingsModal.Q(className: "modal-card") : null;

            if (pauseModal != null && !pauseModal.ClassListContains("modal-hidden"))
            {
                UIAnimationHelper.HideModal(this, pauseModal, pauseCard, () =>
                {
                    UIAnimationHelper.ShowModal(this, levelSettingsModal, settingsCard);
                });
            }
            else
            {
                UIAnimationHelper.ShowModal(this, levelSettingsModal, settingsCard);
            }
        }

        public void HideLevelSettings()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();

            var pauseCard = pauseModal != null ? pauseModal.Q(className: "modal-card") : null;
            var settingsCard = levelSettingsModal != null ? levelSettingsModal.Q(className: "modal-card") : null;

            UIAnimationHelper.HideModal(this, levelSettingsModal, settingsCard, () =>
            {
                if (wasPausedByLevelSettings)
                {
                    wasPausedByLevelSettings = false;
                    if (ArcadeGameManager.Instance != null)
                    {
                        ArcadeGameManager.Instance.ResumeGame();
                    }
                }
                else
                {
                    if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State == GameState.Paused && pauseModal != null)
                    {
                        UIAnimationHelper.ShowModal(this, pauseModal, pauseCard, GetPauseButtons());
                    }
                }
            });

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.SuppressLaunch(0.35f);
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        private void SelectLevelTab(int levelNumber)
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (levelGenerator == null) levelGenerator = FindAnyObjectByType<LevelGenerator>();

            LevelConfiguration baseConfig = levelGenerator != null ? levelGenerator.GetLevelConfig(levelNumber) : null;
            if (baseConfig != null)
            {
                activeEditableConfig = baseConfig.Clone();
            }

            UpdateLevelSettingsUI();
        }

        private void UpdateLevelSettingsUI()
        {
            if (activeEditableConfig == null) return;

            // Update tab button active states
            int lvl = activeEditableConfig.LevelNumber;
            for (int i = 0; i < hudLevelTabButtons.Count; i++)
            {
                if (i + 1 == lvl)
                    hudLevelTabButtons[i].AddToClassList("level-tab-active");
                else
                    hudLevelTabButtons[i].RemoveFromClassList("level-tab-active");
            }

            if (levelNameLabel != null) levelNameLabel.text = activeEditableConfig.LevelName;
            if (levelDescLabel != null) levelDescLabel.text = activeEditableConfig.Description;

            if (sliderColumns != null) sliderColumns.value = activeEditableConfig.Columns;
            if (valColumns != null) valColumns.text = activeEditableConfig.Columns.ToString();

            if (sliderRows != null) sliderRows.value = activeEditableConfig.RowsPerTier;
            if (valRows != null) valRows.text = $"{activeEditableConfig.RowsPerTier} ({activeEditableConfig.TotalRows})";

            if (sliderSpeed != null) sliderSpeed.value = activeEditableConfig.BallSpeedMultiplier;
            if (valSpeed != null) valSpeed.text = $"{activeEditableConfig.BallSpeedMultiplier:F1}x";

            if (sliderMultiplier2x != null) sliderMultiplier2x.value = activeEditableConfig.Multiplier2xCount;
            if (valMultiplier2x != null) valMultiplier2x.text = activeEditableConfig.Multiplier2xCount.ToString();

            if (sliderMultiplier3x != null) sliderMultiplier3x.value = activeEditableConfig.Multiplier3xCount;
            if (valMultiplier3x != null) valMultiplier3x.text = activeEditableConfig.Multiplier3xCount.ToString();

            if (sliderExpander != null) sliderExpander.value = activeEditableConfig.PaddleExpanderCount;
            if (valExpander != null) valExpander.text = activeEditableConfig.PaddleExpanderCount.ToString();

            if (sliderBomb != null) sliderBomb.value = activeEditableConfig.BombCount;
            if (valBomb != null) valBomb.text = activeEditableConfig.BombCount.ToString();

            if (sliderGlass != null) sliderGlass.value = activeEditableConfig.GlassEnclosedCount;
            if (valGlass != null) valGlass.text = activeEditableConfig.GlassEnclosedCount.ToString();

            if (sliderHeart != null) sliderHeart.value = activeEditableConfig.ExtraHeartCount;
            if (valHeart != null) valHeart.text = activeEditableConfig.ExtraHeartCount.ToString();

            if (sliderShield != null) sliderShield.value = activeEditableConfig.ShieldCount;
            if (valShield != null) valShield.text = activeEditableConfig.ShieldCount.ToString();

            if (sliderMultiBall != null) sliderMultiBall.value = activeEditableConfig.MultiBallCount;
            if (valMultiBall != null) valMultiBall.text = activeEditableConfig.MultiBallCount.ToString();

            if (sliderLaser != null) sliderLaser.value = activeEditableConfig.LaserCount;
            if (valLaser != null) valLaser.text = activeEditableConfig.LaserCount.ToString();
        }

        private void ApplyLevelSettingsAndRestart()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (levelGenerator == null) levelGenerator = FindAnyObjectByType<LevelGenerator>();

            wasPausedByLevelSettings = false;
            wasPausedByOptions = false;

            if (levelGenerator != null && activeEditableConfig != null)
            {
                levelGenerator.ApplyCustomConfigAndReload(activeEditableConfig);
            }

            if (levelSettingsModal != null) levelSettingsModal.AddToClassList("modal-hidden");

            // Level reloaded: unpause and transition to ReadyToLaunch so tap-to-launch works immediately
            Time.timeScale = 1f;
            if (ArcadeGameManager.Instance != null)
            {
                ArcadeGameManager.Instance.SetState(GameState.ReadyToLaunch);
            }

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        private void ToggleFpsSetting()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            targetFps = targetFps == 60 ? 120 : 60;
            Application.targetFrameRate = targetFps;
            PlayerPrefs.SetInt("Arcade_TargetFPS", targetFps);
            PlayerPrefs.Save();
            if (btnFps != null) btnFps.text = $"{targetFps} FPS";
        }

        public void ShowHighScores()
        {
            UIAnimationHelper.PunchButton(this, btnHighscoresPause);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            PopulateHighScoresTable();

            var pauseCard = pauseModal != null ? pauseModal.Q(className: "modal-card") : null;
            var scoresCard = highscoresModal != null ? highscoresModal.Q(className: "modal-card") : null;
            var buttons = new System.Collections.Generic.List<VisualElement>();
            if (btnResetHighscores != null) buttons.Add(btnResetHighscores);
            if (btnCloseHighscores != null) buttons.Add(btnCloseHighscores);

            if (pauseModal != null && !pauseModal.ClassListContains("modal-hidden"))
            {
                UIAnimationHelper.HideModal(this, pauseModal, pauseCard, () =>
                {
                    UIAnimationHelper.ShowModal(this, highscoresModal, scoresCard, buttons);
                });
            }
            else
            {
                UIAnimationHelper.ShowModal(this, highscoresModal, scoresCard, buttons);
            }

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        public void HideHighScores()
        {
            UIAnimationHelper.PunchButton(this, btnCloseHighscores);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();

            var pauseCard = pauseModal != null ? pauseModal.Q(className: "modal-card") : null;
            var scoresCard = highscoresModal != null ? highscoresModal.Q(className: "modal-card") : null;

            UIAnimationHelper.HideModal(this, highscoresModal, scoresCard, () =>
            {
                if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State == GameState.Paused && pauseModal != null)
                {
                    UIAnimationHelper.ShowModal(this, pauseModal, pauseCard, GetPauseButtons());
                }
            });

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.SuppressLaunch(0.35f);
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        private void HandleResetHighScores()
        {
            UIAnimationHelper.PunchButton(this, btnResetHighscores);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            HighScoreManager.ResetScores();
            PopulateHighScoresTable();
        }

        private void PopulateHighScoresTable()
        {
            if (root == null) return;
            var scores = HighScoreManager.GetTopScores();

            for (int i = 0; i < HighScoreManager.MAX_SCORES; i++)
            {
                var valLabel = root.Q<Label>($"score-val-{i}");
                var dateLabel = root.Q<Label>($"score-date-{i}");

                if (valLabel != null)
                {
                    if (i < scores.Count && scores[i].score > 0)
                        valLabel.text = scores[i].score.ToString("#,##0");
                    else
                        valLabel.text = "---";
                }

                if (dateLabel != null)
                {
                    if (i < scores.Count && scores[i].score > 0)
                    {
                        string timeStr = scores[i].time > 0f ? $" • {HighScoreManager.FormatTime(scores[i].time)}" : "";
                        string shortDate = HighScoreManager.FormatShortDate(scores[i].date);
                        dateLabel.text = $"L{scores[i].level}{timeStr} • {shortDate}";
                    }
                    else
                        dateLabel.text = "---";
                }
            }
        }

        public void ShowHowToPlay()
        {
            UIAnimationHelper.PunchButton(this, btnHowToPlayPause);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();

            var pauseCard = pauseModal != null ? pauseModal.Q(className: "modal-card") : null;
            var howCard = howToPlayModal != null ? howToPlayModal.Q(className: "modal-card") : null;
            var buttons = new System.Collections.Generic.List<VisualElement>();
            if (btnCloseHowToPlay != null) buttons.Add(btnCloseHowToPlay);

            if (pauseModal != null && !pauseModal.ClassListContains("modal-hidden"))
            {
                UIAnimationHelper.HideModal(this, pauseModal, pauseCard, () =>
                {
                    UIAnimationHelper.ShowModal(this, howToPlayModal, howCard, buttons);
                });
            }
            else
            {
                UIAnimationHelper.ShowModal(this, howToPlayModal, howCard, buttons);
            }

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        public void HideHowToPlay()
        {
            UIAnimationHelper.PunchButton(this, btnCloseHowToPlay);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();

            var pauseCard = pauseModal != null ? pauseModal.Q(className: "modal-card") : null;
            var howCard = howToPlayModal != null ? howToPlayModal.Q(className: "modal-card") : null;

            UIAnimationHelper.HideModal(this, howToPlayModal, howCard, () =>
            {
                if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State == GameState.Paused && pauseModal != null)
                {
                    UIAnimationHelper.ShowModal(this, pauseModal, pauseCard, GetPauseButtons());
                }
            });

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.SuppressLaunch(0.35f);
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        public void ShowCredits()
        {
            UIAnimationHelper.PunchButton(this, btnCreditsPause);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();

            var pauseCard = pauseModal != null ? pauseModal.Q(className: "modal-card") : null;
            var creditsCard = creditsModal != null ? creditsModal.Q(className: "modal-card") : null;
            var buttons = new System.Collections.Generic.List<VisualElement>();
            if (btnCreditWebsite != null) buttons.Add(btnCreditWebsite);
            if (btnCreditEmail != null) buttons.Add(btnCreditEmail);
            if (btnCreditLinkedin != null) buttons.Add(btnCreditLinkedin);
            if (btnCreditGithub != null) buttons.Add(btnCreditGithub);
            if (btnCloseCredits != null) buttons.Add(btnCloseCredits);

            if (pauseModal != null && !pauseModal.ClassListContains("modal-hidden"))
            {
                UIAnimationHelper.HideModal(this, pauseModal, pauseCard, () =>
                {
                    UIAnimationHelper.ShowModal(this, creditsModal, creditsCard, buttons);
                });
            }
            else
            {
                UIAnimationHelper.ShowModal(this, creditsModal, creditsCard, buttons);
            }

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        public void HideCredits()
        {
            UIAnimationHelper.PunchButton(this, btnCloseCredits);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();

            var pauseCard = pauseModal != null ? pauseModal.Q(className: "modal-card") : null;
            var creditsCard = creditsModal != null ? creditsModal.Q(className: "modal-card") : null;

            UIAnimationHelper.HideModal(this, creditsModal, creditsCard, () =>
            {
                if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State == GameState.Paused && pauseModal != null)
                {
                    UIAnimationHelper.ShowModal(this, pauseModal, pauseCard, GetPauseButtons());
                }
            });

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.SuppressLaunch(0.35f);
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        public void ShowLevelModal()
        {
            UIAnimationHelper.PunchButton(this, btnLevelSelectPause);
            UIAnimationHelper.PunchButton(this, btnQuickLevels);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            if (levelGenerator == null) levelGenerator = FindAnyObjectByType<LevelGenerator>();

            int currentLvl = 1;
            if (ArcadeGameManager.Instance != null)
            {
                currentLvl = ArcadeGameManager.Instance.CurrentLevel;
            }
            else if (levelGenerator != null && levelGenerator.CurrentConfig != null)
            {
                currentLvl = levelGenerator.CurrentConfig.LevelNumber;
            }
            selectedLevelNumber = currentLvl;
            SelectLevel(selectedLevelNumber, false);

            var pauseCard = pauseModal != null ? pauseModal.Q(className: "modal-card") : null;
            var lvlCard = levelModal != null ? (levelModal.Q(className: "level-modal-card") ?? levelModal.Q(className: "modal-card")) : null;
            var buttons = new System.Collections.Generic.List<VisualElement>();
            if (btnStartSelectedLevel != null) buttons.Add(btnStartSelectedLevel);
            if (btnCloseLevelModal != null) buttons.Add(btnCloseLevelModal);

            if (pauseModal != null && !pauseModal.ClassListContains("modal-hidden"))
            {
                UIAnimationHelper.HideModal(this, pauseModal, pauseCard, () =>
                {
                    UIAnimationHelper.ShowModal(this, levelModal, lvlCard, buttons);
                });
            }
            else
            {
                UIAnimationHelper.ShowModal(this, levelModal, lvlCard, buttons);
            }

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        public void HideLevelModal()
        {
            UIAnimationHelper.PunchButton(this, btnCloseLevelModal);
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();

            var pauseCard = pauseModal != null ? pauseModal.Q(className: "modal-card") : null;
            var lvlCard = levelModal != null ? (levelModal.Q(className: "level-modal-card") ?? levelModal.Q(className: "modal-card")) : null;

            UIAnimationHelper.HideModal(this, levelModal, lvlCard, () =>
            {
                if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.State == GameState.Paused && pauseModal != null)
                {
                    UIAnimationHelper.ShowModal(this, pauseModal, pauseCard, GetPauseButtons());
                }
            });

            if (Arcade.Input.ArcadeInputHandler.Instance != null)
            {
                Arcade.Input.ArcadeInputHandler.Instance.SuppressLaunch(0.35f);
                Arcade.Input.ArcadeInputHandler.Instance.ResetTouchState();
            }
        }

        private void SelectLevel(int levelNumber, bool playSound = true)
        {
            if (playSound && ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            selectedLevelNumber = levelNumber;

            for (int i = 0; i < menuLevelTabButtons.Count; i++)
            {
                if (i + 1 == levelNumber)
                {
                    menuLevelTabButtons[i].AddToClassList("level-tab-active");
                    menuLevelTabButtons[i].AddToClassList("level-card-selected");
                }
                else
                {
                    menuLevelTabButtons[i].RemoveFromClassList("level-tab-active");
                    menuLevelTabButtons[i].RemoveFromClassList("level-card-selected");
                }
            }

            if (levelGenerator == null) levelGenerator = FindAnyObjectByType<LevelGenerator>();
            LevelConfiguration config = levelGenerator != null ? levelGenerator.GetLevelConfig(levelNumber) : null;
            if (config != null)
            {
                if (menuLevelName != null) menuLevelName.text = config.LevelName;
                if (menuLevelDesc != null) menuLevelDesc.text = config.Description;
            }
            else
            {
                if (menuLevelName != null) menuLevelName.text = $"Level {levelNumber}";
                if (menuLevelDesc != null) menuLevelDesc.text = "Arcade block breaker challenge.";
            }

            if (menuLevelTag != null)
            {
                menuLevelTag.text = $"LEVEL {levelNumber}";
            }

            if (menuLevelDiff != null)
            {
                if (levelNumber <= 4)
                {
                    menuLevelDiff.text = LocalizationManager.Get("diff_easy", "EASY");
                    menuLevelDiff.style.color = new StyleColor(new Color(0.15f, 0.91f, 0.52f));
                }
                else if (levelNumber <= 10)
                {
                    menuLevelDiff.text = LocalizationManager.Get("diff_medium", "MEDIUM");
                    menuLevelDiff.style.color = new StyleColor(new Color(1f, 0.67f, 0f));
                }
                else
                {
                    menuLevelDiff.text = LocalizationManager.Get("diff_hard", "HARD");
                    menuLevelDiff.style.color = new StyleColor(new Color(1f, 0.23f, 0.34f));
                }
            }

            int levelStars = HighScoreManager.GetLevelStars(levelNumber);
            float bestTime = HighScoreManager.GetLevelBestTime(levelNumber);
            if (menuLevelStars != null)
            {
                string starStr = "";
                for (int s = 0; s < 3; s++) starStr += s < levelStars ? "★" : "☆";
                menuLevelStars.text = starStr;
                menuLevelStars.style.color = levelStars > 0 ? new StyleColor(new Color(1f, 0.843f, 0f, 1f)) : new StyleColor(new Color(0.6f, 0.65f, 0.75f, 0.4f));
            }
            if (menuLevelBestTime != null)
            {
                menuLevelBestTime.text = bestTime > 0 ? HighScoreManager.FormatTime(bestTime) : "--:--";
            }
        }

        private void HandleStartSelectedLevel()
        {
            if (ArcadeAudioManager.Instance != null) ArcadeAudioManager.Instance.PlayButtonPress();
            Time.timeScale = 1f;
            PlayerPrefs.SetInt("Arcade_LoadSavedGameOnStart", 0);
            PlayerPrefs.SetInt("Arcade_SelectedLevel", selectedLevelNumber);
            PlayerPrefs.Save();
            ArcadeGameManager.ClearSavedGame();
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private void OpenEmail() => Application.OpenURL("mailto:ramin.rasulzade@gmail.com");
        private void OpenWebsite() => Application.OpenURL("https://raminrasulzade.com");
        private void OpenLinkedIn() => Application.OpenURL("https://linkedin.com/in/ramin-rasulzade");
        private void OpenGitHub() => Application.OpenURL("https://github.com/raminres");

        private bool IsModalOpen(VisualElement modal)
        {
            return modal != null &&
                   !modal.ClassListContains("modal-hidden") &&
                   !modal.ClassListContains(UIAnimationHelper.ClassModalBackdropHidden);
        }

        public bool IsAnyModalVisible()
        {
            return IsModalOpen(optionsModal) ||
                   IsModalOpen(levelSettingsModal) ||
                   IsModalOpen(levelModal) ||
                   IsModalOpen(creditsModal) ||
                   IsModalOpen(pauseModal) ||
                   IsModalOpen(gameOverModal) ||
                   IsModalOpen(levelClearModal) ||
                   IsModalOpen(highscoresModal) ||
                   IsModalOpen(howToPlayModal);
        }

        public bool IsPointerOverUI(Vector2 screenPos)
        {
            if (IsAnyModalVisible()) return true;

            if (root != null && root.panel != null)
            {
                // In Unity, screenPos has origin at bottom-left (Y increasing up).
                // UI Toolkit ScreenToPanel expects GUI screen coordinates with origin at top-left (Y increasing down).
                float screenH = Screen.height > 0 ? Screen.height : 1080f;
                Vector2 guiPos = new Vector2(screenPos.x, screenH - screenPos.y);
                Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(root.panel, guiPos);
                VisualElement picked = root.panel.Pick(panelPos);
                if (picked != null)
                {
                    // Only interactive buttons (or elements inside a button, e.g. Pause button) block gameplay input.
                    // Passive HUD indicators (score, lives, timer, powerup badges, etc.) should never block paddle movement.
                    Button btn = (picked as Button) ?? picked.GetFirstAncestorOfType<Button>();
                    if (btn != null && btn.pickingMode != PickingMode.Ignore && btn.enabledInHierarchy)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
