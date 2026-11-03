using System.Collections.Generic;
using System.IO;
using Arcade.BlockBreaker;
using Arcade.Core;
using Arcade.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arcade.Editor
{
    /// <summary>
    /// Interactive EditorWindow for inspecting, editing, creating, and visually verifying BlockBreaker level layouts,
    /// block color tiers, power-up/hazard distributions, registering levels to scene generators, and managing level preview thumbnail sprites.
    /// </summary>
    public class LevelVisualizerWindow : EditorWindow
    {
        private const string LEVELS_DIR = "Assets/Settings/Levels";
        private const string GAMEPLAY_SCENE_PATH = "Assets/Scenes/LV_BlockBreaker.unity";
        private const string MAIN_MENU_SCENE_PATH = "Assets/Scenes/LV_BlockBreaker_MainMenu.unity";

        private int selectedLevelNumber = 1;
        private LevelConfiguration currentConfig;
        private SerializedObject serializedConfig;

        private readonly List<LevelConfiguration> allConfigs = new List<LevelConfiguration>();
        private string[] levelOptionNames = new string[0];

        private Vector2 scrollPosLeft;
        private Vector2 scrollPosRight;
        private bool liveScenePreview = false;

        // Create New Level form state
        private bool showCreatePanel = false;
        public enum CreationTemplateMode
        {
            CloneSelected = 0,
            CleanDefault = 1
        }
        private CreationTemplateMode creationMode = CreationTemplateMode.CloneSelected;
        private int newLevelNumber = 31;
        private string newLevelName = "Level 31: Custom Layout";
        private string newLevelDescription = "";
        private LevelLayoutType newLayoutType = LevelLayoutType.FullGrid;
        private BlockColorPattern newColorPattern = BlockColorPattern.InvertedTiered;
        private int newColumns = 10;
        private int newRowsPerTier = 2;
        private float newBlockSize = 1.0f;
        private float newHorizontalSpacing = 1.25f;
        private float newVerticalSpacing = 1.3f;
        private bool autoRegisterToLevelGenerator = true;
        private bool autoRegisterToMainMenu = true;
        private bool autoCapturePreview = true;

        // Inspector foldouts
        private bool foldoutIdentity = true;
        private bool foldoutGrid = true;
        private bool foldoutBalancing = true;
        private bool foldoutPowerups = true;
        private bool foldoutHazards = true;
        private bool foldoutScoring = true;

        private static readonly Color ColorBlueBlock = new Color(0.12f, 0.55f, 1.0f);
        private static readonly Color ColorGreenBlock = new Color(0.15f, 0.85f, 0.40f);
        private static readonly Color ColorRedBlock = new Color(1.0f, 0.22f, 0.35f);
        private static readonly Color ColorBombBlock = new Color(1.0f, 0.55f, 0.0f);
        private static readonly Color ColorGlassBlock = new Color(0.50f, 0.85f, 1.0f, 0.8f);
        private static readonly Color ColorEmptyCell = new Color(0.08f, 0.10f, 0.16f, 0.6f);

        [MenuItem("Tools/Arcade/Level Visualizer", priority = 50)]
        [MenuItem("Window/Arcade/Level Visualizer", priority = 100)]
        public static void OpenWindow()
        {
            var window = GetWindow<LevelVisualizerWindow>("Level Visualizer");
            window.minSize = new Vector2(850, 620);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshLevelList();
            if (allConfigs.Count > 0 && (selectedLevelNumber < 1 || selectedLevelNumber > GetMaxConfiguredLevelNumber()))
            {
                selectedLevelNumber = allConfigs[0].LevelNumber;
            }
            LoadLevel(selectedLevelNumber);
        }

        public void RefreshLevelList()
        {
            allConfigs.Clear();
            var guids = AssetDatabase.FindAssets("t:LevelConfiguration", new[] { LEVELS_DIR, "Assets/Settings" });
            var dict = new SortedDictionary<int, LevelConfiguration>();

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var cfg = AssetDatabase.LoadAssetAtPath<LevelConfiguration>(path);
                if (cfg != null && cfg.LevelNumber > 0 && !dict.ContainsKey(cfg.LevelNumber))
                {
                    dict[cfg.LevelNumber] = cfg;
                }
            }

            foreach (var kvp in dict)
            {
                allConfigs.Add(kvp.Value);
            }

            levelOptionNames = new string[allConfigs.Count];
            for (int i = 0; i < allConfigs.Count; i++)
            {
                var cfg = allConfigs[i];
                levelOptionNames[i] = $"Level {cfg.LevelNumber:D2}: {cfg.LevelName}";
            }
        }

        private int GetCurrentConfigIndex()
        {
            for (int i = 0; i < allConfigs.Count; i++)
            {
                if (allConfigs[i] != null && allConfigs[i].LevelNumber == selectedLevelNumber)
                {
                    return i;
                }
            }
            return 0;
        }

        private int GetMaxConfiguredLevelNumber()
        {
            int max = 0;
            foreach (var c in allConfigs)
            {
                if (c != null && c.LevelNumber > max)
                {
                    max = c.LevelNumber;
                }
            }
            return Mathf.Max(30, max);
        }

        private int GetNextAvailableLevelNumber()
        {
            int max = 0;
            foreach (var c in allConfigs)
            {
                if (c != null && c.LevelNumber > max)
                {
                    max = c.LevelNumber;
                }
            }
            return max + 1;
        }

        private void PrepareNewLevelDefaults()
        {
            newLevelNumber = GetNextAvailableLevelNumber();
            newLevelName = $"Level {newLevelNumber}: Custom Layout";
            newLevelDescription = $"Custom designed arcade stage {newLevelNumber}.";
        }

        private void LoadLevel(int levelNumber)
        {
            selectedLevelNumber = levelNumber;
            string path = $"{LEVELS_DIR}/SO_Level_{selectedLevelNumber:D2}.asset";
            currentConfig = AssetDatabase.LoadAssetAtPath<LevelConfiguration>(path);

            if (currentConfig == null && allConfigs.Count > 0)
            {
                currentConfig = allConfigs.Find(c => c != null && c.LevelNumber == selectedLevelNumber);
                if (currentConfig == null)
                {
                    currentConfig = allConfigs[0];
                    selectedLevelNumber = currentConfig.LevelNumber;
                }
            }

            if (currentConfig != null)
            {
                serializedConfig = new SerializedObject(currentConfig);
            }
            else
            {
                serializedConfig = null;
            }

            if (liveScenePreview)
            {
                ApplyLiveScenePreview();
            }
        }

        private void OnGUI()
        {
            DrawTopToolbar();

            if (showCreatePanel)
            {
                DrawCreateNewLevelPanel();
            }

            if (currentConfig == null)
            {
                EditorGUILayout.HelpBox($"Could not find LevelConfiguration at {LEVELS_DIR}/SO_Level_{selectedLevelNumber:D2}.asset", MessageType.Warning);
                if (GUILayout.Button("Create / Restore Default Level Presets", GUILayout.Height(30)))
                {
                    SetupBlockBreakerScenes.CreateOrUpdateLevelPresets();
                    RefreshLevelList();
                    LoadLevel(selectedLevelNumber);
                }
                return;
            }

            if (serializedConfig != null)
            {
                serializedConfig.Update();
            }

            EditorGUILayout.BeginHorizontal();

            // Left Pane: 2D Visual Grid & Metrics
            DrawLeftPane();

            // Right Pane: Inspector, Preview Thumbnail, and Controls
            DrawRightPane();

            EditorGUILayout.EndHorizontal();

            if (serializedConfig != null && serializedConfig.hasModifiedProperties)
            {
                serializedConfig.ApplyModifiedProperties();
                if (liveScenePreview)
                {
                    ApplyLiveScenePreview();
                }
            }
        }

        private void DrawTopToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            int currentIndex = GetCurrentConfigIndex();

            // Previous button
            GUI.enabled = currentIndex > 0;
            if (GUILayout.Button("◄ Prev", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                LoadLevel(allConfigs[currentIndex - 1].LevelNumber);
            }
            GUI.enabled = true;

            // Level selector popup
            if (levelOptionNames != null && levelOptionNames.Length > 0)
            {
                int safeIndex = Mathf.Clamp(currentIndex, 0, levelOptionNames.Length - 1);
                int newSelected = EditorGUILayout.Popup(safeIndex, levelOptionNames, EditorStyles.toolbarPopup, GUILayout.Width(230));
                if (newSelected != safeIndex && newSelected >= 0 && newSelected < allConfigs.Count)
                {
                    LoadLevel(allConfigs[newSelected].LevelNumber);
                }
            }
            else
            {
                EditorGUILayout.LabelField("No Levels Found", EditorStyles.toolbarButton, GUILayout.Width(230));
            }

            // Next button
            GUI.enabled = currentIndex < allConfigs.Count - 1;
            if (GUILayout.Button("Next ►", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                LoadLevel(allConfigs[currentIndex + 1].LevelNumber);
            }
            GUI.enabled = true;

            GUILayout.Space(8);

            // Direct object field
            var picked = (LevelConfiguration)EditorGUILayout.ObjectField(currentConfig, typeof(LevelConfiguration), false, GUILayout.Width(200));
            if (picked != null && picked != currentConfig)
            {
                currentConfig = picked;
                selectedLevelNumber = picked.LevelNumber;
                serializedConfig = new SerializedObject(currentConfig);
                RefreshLevelList();
            }

            GUILayout.Space(6);

            // + New Level Toggle Button
            GUI.backgroundColor = showCreatePanel ? new Color(0.65f, 1f, 0.65f) : Color.white;
            if (GUILayout.Button(showCreatePanel ? "✕ Close New Level" : "★ + New Level", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                showCreatePanel = !showCreatePanel;
                if (showCreatePanel)
                {
                    PrepareNewLevelDefaults();
                }
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();

            // Live Scene Preview Toggle
            bool prevLive = liveScenePreview;
            liveScenePreview = GUILayout.Toggle(liveScenePreview, "Live Scene Preview", EditorStyles.toolbarButton, GUILayout.Width(130));
            if (liveScenePreview != prevLive && liveScenePreview)
            {
                ApplyLiveScenePreview();
            }

            // Refresh / Reload button
            if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshLevelList();
                LoadLevel(selectedLevelNumber);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCreateNewLevelPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("✨ Create & Author New Level", EditorStyles.boldLabel);
            if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(24)))
            {
                showCreatePanel = false;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Template Source", GUILayout.Width(130));
            creationMode = (CreationTemplateMode)EditorGUILayout.EnumPopup(creationMode);
            EditorGUILayout.EndHorizontal();

            if (creationMode == CreationTemplateMode.CloneSelected && currentConfig != null)
            {
                EditorGUILayout.HelpBox($"Will clone all layout settings, block types, and dynamics from Level {currentConfig.LevelNumber} ({currentConfig.LevelName}).", MessageType.Info);
            }
            else if (creationMode == CreationTemplateMode.CloneSelected && currentConfig == null)
            {
                EditorGUILayout.HelpBox("No current level selected to clone from; falling back to clean template.", MessageType.Warning);
            }

            EditorGUILayout.Space(2);

            newLevelNumber = EditorGUILayout.IntField("Level Number", newLevelNumber);
            if (newLevelNumber < 1) newLevelNumber = 1;

            newLevelName = EditorGUILayout.TextField("Level Name", newLevelName);
            newLevelDescription = EditorGUILayout.TextField("Description", newLevelDescription);

            if (creationMode == CreationTemplateMode.CleanDefault)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Default Template Properties", EditorStyles.boldLabel);
                newLayoutType = (LevelLayoutType)EditorGUILayout.EnumPopup("Layout Archetype", newLayoutType);
                newColorPattern = (BlockColorPattern)EditorGUILayout.EnumPopup("Color Pattern", newColorPattern);
                newColumns = EditorGUILayout.IntSlider("Columns", newColumns, 4, 14);
                newRowsPerTier = EditorGUILayout.IntSlider("Rows Per Tier", newRowsPerTier, 1, 4);
                newBlockSize = EditorGUILayout.Slider("Block Size", newBlockSize, 0.8f, 1.4f);
                newHorizontalSpacing = EditorGUILayout.Slider("Horizontal Spacing", newHorizontalSpacing, 0.8f, 2.0f);
                newVerticalSpacing = EditorGUILayout.Slider("Vertical Spacing", newVerticalSpacing, 0.8f, 2.0f);
            }

            EditorGUILayout.Space(4);
            string targetPath = $"{LEVELS_DIR}/SO_Level_{newLevelNumber:D2}.asset";
            bool fileExists = File.Exists(targetPath);
            if (fileExists)
            {
                EditorGUILayout.HelpBox($"Target asset already exists at {targetPath}. Creating will overwrite it!", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField($"Target Asset: {targetPath}", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Registration & Workflow Options", EditorStyles.boldLabel);
            autoRegisterToLevelGenerator = EditorGUILayout.ToggleLeft("Register in LevelGenerator (LV_BlockBreaker.unity)", autoRegisterToLevelGenerator);
            autoRegisterToMainMenu = EditorGUILayout.ToggleLeft("Register in MainMenuUIManager (LV_BlockBreaker_MainMenu.unity)", autoRegisterToMainMenu);
            autoCapturePreview = EditorGUILayout.ToggleLeft("Generate Preview Thumbnail Sprite immediately", autoCapturePreview);

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.4f);
            if (GUILayout.Button("✔ Create & Author Level Asset", GUILayout.Height(30)))
            {
                CreateNewLevelAsset();
            }
            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Cancel", GUILayout.Height(30), GUILayout.Width(100)))
            {
                showCreatePanel = false;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        private void CreateNewLevelAsset()
        {
            if (!Directory.Exists(LEVELS_DIR))
            {
                Directory.CreateDirectory(LEVELS_DIR);
                AssetDatabase.Refresh();
            }

            string targetPath = $"{LEVELS_DIR}/SO_Level_{newLevelNumber:D2}.asset";
            if (File.Exists(targetPath))
            {
                if (!EditorUtility.DisplayDialog("Overwrite Level Asset?",
                        $"An asset already exists at {targetPath}.\nDo you want to overwrite it with this new configuration?",
                        "Yes, Overwrite", "Cancel"))
                {
                    return;
                }
            }

            LevelConfiguration newConfig;
            if (creationMode == CreationTemplateMode.CloneSelected && currentConfig != null)
            {
                newConfig = currentConfig.Clone();
                newConfig.SetIdentity(newLevelNumber, newLevelName,
                    string.IsNullOrEmpty(newLevelDescription) ? currentConfig.Description : newLevelDescription);
            }
            else
            {
                newConfig = ScriptableObject.CreateInstance<LevelConfiguration>();
                newConfig.SetIdentity(newLevelNumber, newLevelName, newLevelDescription);
                newConfig.SetLayoutType(newLayoutType);
                newConfig.SetColorPattern(newColorPattern);
                newConfig.SetColumns(newColumns);
                newConfig.SetRowsPerTier(newRowsPerTier);
                newConfig.SetBlockSize(newBlockSize);
                newConfig.SetHorizontalSpacing(newHorizontalSpacing);
                newConfig.SetVerticalSpacing(newVerticalSpacing);
                newConfig.SetStartCenterY(15.5f);
                newConfig.SetIncludeSideFlanks(true);
                newConfig.SetBallSpeedMultiplier(1.0f);
                newConfig.SetInitialPaddleWidth(5.0f);
                newConfig.SetMultiplier2xCount(1);
                newConfig.SetPaddleExpanderCount(1);
                newConfig.SetParTime(40f);
                newConfig.SetTimeBonusMax(3000);
                newConfig.SetStarThresholds(new int[] { 500, 1000, 2000 });
            }

            if (File.Exists(targetPath))
            {
                AssetDatabase.DeleteAsset(targetPath);
            }

            AssetDatabase.CreateAsset(newConfig, targetPath);
            EditorUtility.SetDirty(newConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (autoRegisterToLevelGenerator || autoRegisterToMainMenu)
            {
                SyncAllLevelPresetsToScenes(autoRegisterToMainMenu);
            }

            if (autoCapturePreview)
            {
                LevelPreviewGenerator.GeneratePreview(newLevelNumber, newConfig);
            }

            RefreshLevelList();
            LoadLevel(newLevelNumber);
            showCreatePanel = false;

            EditorGUIUtility.PingObject(newConfig);
            Selection.activeObject = newConfig;

            ShowNotification(new GUIContent($"Level {newLevelNumber} created & loaded!"));
            Debug.Log($"<color=green>[LevelVisualizerWindow] Created Level {newLevelNumber} at {targetPath} and registered in scene presets.</color>");
        }

        private bool IsCurrentLevelInLevelGenerator()
        {
            if (currentConfig == null) return true;
            var lg = Object.FindAnyObjectByType<LevelGenerator>();
            if (lg != null && lg.LevelPresets != null)
            {
                for (int i = 0; i < lg.LevelPresets.Length; i++)
                {
                    if (lg.LevelPresets[i] == currentConfig) return true;
                }
                return false;
            }
            return true;
        }

        private void DrawLeftPane()
        {
            float width = position.width * 0.52f;
            EditorGUILayout.BeginVertical(GUILayout.Width(width));

            scrollPosLeft = EditorGUILayout.BeginScrollView(scrollPosLeft);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"Grid Layout Visualizer — Level {currentConfig.LevelNumber}: {currentConfig.LevelName}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Layout: {currentConfig.LayoutType} | Pattern: {currentConfig.ColorPattern} | Grid: {currentConfig.Columns} cols × {currentConfig.TotalRows} rows ({currentConfig.RowsPerTier} per tier)", EditorStyles.miniLabel);

            EditorGUILayout.Space(6);

            // Draw interactive 2D Grid
            DrawGridMatrix(width - 25);

            EditorGUILayout.Space(10);

            // Metrics Pod
            DrawMetricsDashboard();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawGridMatrix(float availableWidth)
        {
            int cols = currentConfig.Columns;
            int totalRows = currentConfig.TotalRows;

            if (cols <= 0 || totalRows <= 0) return;

            float cellWidth = Mathf.Clamp((availableWidth - ((cols - 1) * 3f)) / cols, 18f, 42f);
            float cellHeight = cellWidth * 0.52f; // Proportional 1.15 : 0.55 brick ratio

            // Calculate active block positions and special items for preview
            int totalActive = 0;
            var activeIndices = new List<Vector2Int>();
            for (int r = 0; r < totalRows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (currentConfig.HasBlockAt(r, c))
                    {
                        totalActive++;
                        activeIndices.Add(new Vector2Int(r, c));
                    }
                }
            }

            // Mock distribution map for visualization
            var specialMap = GetSimulatedSpecialDistribution(totalActive);

            Rect gridRect = GUILayoutUtility.GetRect(availableWidth, (cellHeight + 3f) * totalRows + 10f);
            GUI.Box(gridRect, GUIContent.none, EditorStyles.helpBox);

            int blockIndex = 0;
            for (int r = 0; r < totalRows; r++)
            {
                float y = gridRect.y + 5f + (r * (cellHeight + 3f));

                for (int c = 0; c < cols; c++)
                {
                    float x = gridRect.x + 5f + (c * (cellWidth + 3f));
                    Rect cellRect = new Rect(x, y, cellWidth, cellHeight);

                    bool hasBlock = currentConfig.HasBlockAt(r, c);
                    if (!hasBlock)
                    {
                        // Draw empty cell socket
                        EditorGUI.DrawRect(cellRect, ColorEmptyCell);
                        continue;
                    }

                    // Determine block color
                    BlockColorTier? explicitTier = currentConfig.GetExplicitColorAt(r, c);
                    Color blockColor;
                    if (explicitTier.HasValue)
                    {
                        blockColor = explicitTier.Value switch
                        {
                            BlockColorTier.Blue => ColorBlueBlock,
                            BlockColorTier.Green => ColorGreenBlock,
                            _ => ColorRedBlock
                        };
                    }
                    else if (currentConfig.ColorPattern == BlockColorPattern.Checkerboard)
                    {
                        int check = (r + c) % 3;
                        blockColor = check == 0 ? ColorBlueBlock : (check == 1 ? ColorGreenBlock : ColorRedBlock);
                    }
                    else if (currentConfig.ColorPattern == BlockColorPattern.Randomized)
                    {
                        int pseudo = ((r * 17) + (c * 31)) % 3;
                        blockColor = pseudo == 0 ? ColorBlueBlock : (pseudo == 1 ? ColorGreenBlock : ColorRedBlock);
                    }
                    else // InvertedTiered
                    {
                        if (r < currentConfig.RowsPerTier) blockColor = ColorBlueBlock;
                        else if (r < currentConfig.RowsPerTier * 2) blockColor = ColorGreenBlock;
                        else blockColor = ColorRedBlock;
                    }

                    BlockSpecialType special = specialMap.TryGetValue(blockIndex, out var sType) ? sType : BlockSpecialType.Normal;
                    blockIndex++;

                    // Special overrides
                    if (special == BlockSpecialType.Bomb)
                    {
                        blockColor = ColorBombBlock;
                    }

                    // Draw block rectangle
                    EditorGUI.DrawRect(cellRect, blockColor);

                    // Outline / Glass shell
                    if (special == BlockSpecialType.GlassEnclosed)
                    {
                        DrawOutline(cellRect, 2, ColorGlassBlock);
                    }
                    else
                    {
                        DrawOutline(cellRect, 1, new Color(1f, 1f, 1f, 0.25f));
                    }

                    // Badge text
                    string badge = GetBadgeLabel(special);
                    if (!string.IsNullOrEmpty(badge))
                    {
                        var style = new GUIStyle(EditorStyles.miniLabel)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            normal = { textColor = Color.white },
                            fontSize = Mathf.Max(8, (int)(cellHeight * 0.55f)),
                            fontStyle = FontStyle.Bold
                        };
                        GUI.Label(cellRect, badge, style);
                    }
                }
            }
        }

        private static void DrawOutline(Rect rect, float thickness, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color); // Top
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color); // Bottom
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color); // Left
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color); // Right
        }

        private static string GetBadgeLabel(BlockSpecialType special) => special switch
        {
            BlockSpecialType.Bomb => "B",
            BlockSpecialType.GlassEnclosed => "G",
            BlockSpecialType.ScoreMultiplier2x => "2x",
            BlockSpecialType.ScoreMultiplier3x => "3x",
            BlockSpecialType.ScoreMultiplier4x => "4x",
            BlockSpecialType.ScoreMultiplier5x => "5x",
            BlockSpecialType.PaddleExpander => "EXP",
            BlockSpecialType.ExtraHeart => "+♥",
            BlockSpecialType.Shield => "SHD",
            BlockSpecialType.MultiBall => "MLB",
            BlockSpecialType.Laser => "LSR",
            BlockSpecialType.PaddleShortener => "SHT",
            BlockSpecialType.PaddleSlower => "SLW",
            BlockSpecialType.BrickFreezer => "FRZ",
            BlockSpecialType.BallSizeDecreaser => "SHR",
            BlockSpecialType.BallSlower => "BSL",
            BlockSpecialType.PaddleFreezer => "P-FZ",
            _ => string.Empty
        };

        private Dictionary<int, BlockSpecialType> GetSimulatedSpecialDistribution(int totalActive)
        {
            var map = new Dictionary<int, BlockSpecialType>();
            if (totalActive <= 0) return map;

            var types = new List<BlockSpecialType>();
            for (int i = 0; i < currentConfig.Multiplier2xCount; i++) types.Add(BlockSpecialType.ScoreMultiplier2x);
            for (int i = 0; i < currentConfig.Multiplier3xCount; i++) types.Add(BlockSpecialType.ScoreMultiplier3x);
            for (int i = 0; i < currentConfig.Multiplier4xCount; i++) types.Add(BlockSpecialType.ScoreMultiplier4x);
            for (int i = 0; i < currentConfig.Multiplier5xCount; i++) types.Add(BlockSpecialType.ScoreMultiplier5x);
            for (int i = 0; i < currentConfig.PaddleExpanderCount; i++) types.Add(BlockSpecialType.PaddleExpander);
            for (int i = 0; i < currentConfig.BombCount; i++) types.Add(BlockSpecialType.Bomb);
            for (int i = 0; i < currentConfig.GlassEnclosedCount; i++) types.Add(BlockSpecialType.GlassEnclosed);
            for (int i = 0; i < currentConfig.ExtraHeartCount; i++) types.Add(BlockSpecialType.ExtraHeart);
            for (int i = 0; i < currentConfig.ShieldCount; i++) types.Add(BlockSpecialType.Shield);
            for (int i = 0; i < currentConfig.MultiBallCount; i++) types.Add(BlockSpecialType.MultiBall);
            for (int i = 0; i < currentConfig.LaserCount; i++) types.Add(BlockSpecialType.Laser);
            for (int i = 0; i < currentConfig.PaddleShortenerCount; i++) types.Add(BlockSpecialType.PaddleShortener);
            for (int i = 0; i < currentConfig.PaddleSlowerCount; i++) types.Add(BlockSpecialType.PaddleSlower);
            for (int i = 0; i < currentConfig.BrickFreezerCount; i++) types.Add(BlockSpecialType.BrickFreezer);
            for (int i = 0; i < currentConfig.BallSizeDecreaserCount; i++) types.Add(BlockSpecialType.BallSizeDecreaser);
            for (int i = 0; i < currentConfig.BallSlowerCount; i++) types.Add(BlockSpecialType.BallSlower);
            for (int i = 0; i < currentConfig.PaddleFreezerCount; i++) types.Add(BlockSpecialType.PaddleFreezer);

            var rng = new System.Random(currentConfig.LevelNumber * 37 + 101);
            var indices = new List<int>();
            for (int i = 0; i < totalActive; i++) indices.Add(i);

            // Fisher-Yates shuffle
            for (int i = indices.Count - 1; i > 0; i--)
            {
                int k = rng.Next(i + 1);
                (indices[i], indices[k]) = (indices[k], indices[i]);
            }

            int count = Mathf.Min(types.Count, indices.Count);
            for (int i = 0; i < count; i++)
            {
                map[indices[i]] = types[i];
            }

            return map;
        }

        private void DrawMetricsDashboard()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Level Summary & Density Metrics", EditorStyles.boldLabel);

            int totalRows = currentConfig.TotalRows;
            int cols = currentConfig.Columns;
            int totalActive = 0;
            int blueCount = 0;
            int greenCount = 0;
            int redCount = 0;

            for (int r = 0; r < totalRows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (currentConfig.HasBlockAt(r, c))
                    {
                        totalActive++;
                        if (r < currentConfig.RowsPerTier) blueCount++;
                        else if (r < currentConfig.RowsPerTier * 2) greenCount++;
                        else redCount++;
                    }
                }
            }

            int powerupsTotal = currentConfig.Multiplier2xCount + currentConfig.Multiplier3xCount +
                                currentConfig.Multiplier4xCount + currentConfig.Multiplier5xCount +
                                currentConfig.PaddleExpanderCount + currentConfig.ExtraHeartCount +
                                currentConfig.ShieldCount + currentConfig.MultiBallCount + currentConfig.LaserCount;

            int hazardsTotal = currentConfig.PaddleShortenerCount + currentConfig.PaddleSlowerCount +
                               currentConfig.BrickFreezerCount + currentConfig.BallSizeDecreaserCount +
                               currentConfig.BallSlowerCount + currentConfig.PaddleFreezerCount;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Active Bricks: {totalActive}", GUILayout.Width(130));
            EditorGUILayout.LabelField($"Blue (Top): {blueCount}", GUILayout.Width(110));
            EditorGUILayout.LabelField($"Green (Mid): {greenCount}", GUILayout.Width(110));
            EditorGUILayout.LabelField($"Red (Bottom): {redCount}");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Power-up Drops: {powerupsTotal}", GUILayout.Width(130));
            EditorGUILayout.LabelField($"Hazards: {hazardsTotal}", GUILayout.Width(110));
            EditorGUILayout.LabelField($"Bombs: {currentConfig.BombCount}", GUILayout.Width(110));
            EditorGUILayout.LabelField($"Glass Bricks: {currentConfig.GlassEnclosedCount}");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawRightPane()
        {
            EditorGUILayout.BeginVertical();
            scrollPosRight = EditorGUILayout.BeginScrollView(scrollPosRight);

            // Level Generator preset status check
            if (!IsCurrentLevelInLevelGenerator())
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.HelpBox($"Level {currentConfig.LevelNumber} ({currentConfig.LevelName}) is not currently in the LevelGenerator presets list!", MessageType.Warning);
                if (GUILayout.Button($"➕ Register Level {currentConfig.LevelNumber} in LevelGenerator & MainMenu", GUILayout.Height(26)))
                {
                    int count = SyncAllLevelPresetsToScenes(true);
                    ShowNotification(new GUIContent($"Registered all {count} levels in scene presets!"));
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
            }

            // Level Preview Thumbnail Section
            DrawPreviewThumbnailSection();

            EditorGUILayout.Space(6);

            // Level Inspector Foldouts
            DrawInspectorFoldouts();

            EditorGUILayout.Space(10);

            // Bottom Actions
            DrawBottomActions();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewThumbnailSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Level Preview Thumbnail (Bundled into iOS Build)", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            // Thumbnail visual display (1:2 aspect ratio, e.g. 90x180)
            Rect thumbRect = GUILayoutUtility.GetRect(90, 180, GUILayout.Width(90), GUILayout.Height(180));
            GUI.Box(thumbRect, GUIContent.none, EditorStyles.helpBox);

            Sprite previewSprite = currentConfig.PreviewSprite;
            if (previewSprite != null && previewSprite.texture != null)
            {
                GUI.DrawTexture(thumbRect, previewSprite.texture, ScaleMode.ScaleAndCrop);
            }
            else
            {
                var labelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    wordWrap = true,
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(thumbRect, "No Preview\nAssigned", labelStyle);
            }

            GUILayout.Space(12);

            EditorGUILayout.BeginVertical();

            EditorGUILayout.HelpBox(
                previewSprite != null
                    ? $"Assigned: {previewSprite.name} ({previewSprite.rect.width}×{previewSprite.rect.height})\nDirectly serialized on SO_Level_{selectedLevelNumber:D2}.asset to guarantee iOS inclusion."
                    : "No preview sprite is assigned! Click 'Capture Preview Image' below to generate and link it.",
                previewSprite != null ? MessageType.Info : MessageType.Warning);

            // Object field to allow manual assignment / editing
            EditorGUI.BeginChangeCheck();
            Sprite newSprite = (Sprite)EditorGUILayout.ObjectField("Preview Sprite", previewSprite, typeof(Sprite), false);
            if (EditorGUI.EndChangeCheck())
            {
                currentConfig.SetPreviewSprite(newSprite);
                EditorUtility.SetDirty(currentConfig);
                AssetDatabase.SaveAssets();
            }

            EditorGUILayout.Space(8);

            // Capture Preview button
            if (GUILayout.Button("📷 Capture Preview Image for Level " + selectedLevelNumber, GUILayout.Height(28)))
            {
                bool success = LevelPreviewGenerator.GeneratePreview(selectedLevelNumber, currentConfig);
                if (success)
                {
                    LoadLevel(selectedLevelNumber);
                    ShowNotification(new GUIContent($"Level {selectedLevelNumber} preview captured & assigned!"));
                }
            }

            // Sync All button
            if (GUILayout.Button("🔄 Sync All Previews to Assets & Scenes", GUILayout.Height(24)))
            {
                int count = LevelPreviewGenerator.SyncAllPreviewSprites();
                LoadLevel(selectedLevelNumber);
                ShowNotification(new GUIContent($"Synced {count} preview sprites!"));
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawInspectorFoldouts()
        {
            if (serializedConfig == null) return;

            // Identity
            foldoutIdentity = EditorGUILayout.Foldout(foldoutIdentity, "Level Identity", true, EditorStyles.foldoutHeader);
            if (foldoutIdentity)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("levelNumber"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("levelName"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("levelDescription"));
                EditorGUI.indentLevel--;
            }

            // Grid & Layout
            foldoutGrid = EditorGUILayout.Foldout(foldoutGrid, "Grid Layout & Spacing", true, EditorStyles.foldoutHeader);
            if (foldoutGrid)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("layoutType"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("colorPattern"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("columns"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("rowsPerTier"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("blockSize"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("horizontalSpacing"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("verticalSpacing"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("startCenterY"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("includeSideFlanks"));

                if (currentConfig.LayoutType == LevelLayoutType.Custom)
                {
                    EditorGUILayout.PropertyField(serializedConfig.FindProperty("customLayout"));
                }
                EditorGUI.indentLevel--;
            }

            // Balancing
            foldoutBalancing = EditorGUILayout.Foldout(foldoutBalancing, "Balancing & Dynamics", true, EditorStyles.foldoutHeader);
            if (foldoutBalancing)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("ballSpeedMultiplier"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("initialPaddleWidth"));
                EditorGUI.indentLevel--;
            }

            // Power-ups
            foldoutPowerups = EditorGUILayout.Foldout(foldoutPowerups, "Power-up Modifiers", true, EditorStyles.foldoutHeader);
            if (foldoutPowerups)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("multiplier2xCount"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("multiplier3xCount"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("multiplier4xCount"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("multiplier5xCount"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("paddleExpanderCount"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("bombCount"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("glassEnclosedCount"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("extraHeartCount"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("shieldCount"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("multiBallCount"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("laserCount"));
                EditorGUI.indentLevel--;
            }

            // Hazards
            foldoutHazards = EditorGUILayout.Foldout(foldoutHazards, "Hazards & Debuffs", true, EditorStyles.foldoutHeader);
            if (foldoutHazards)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("paddleShortenerCount"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("paddleSlowerCount"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("brickFreezerCount"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("ballSizeDecreaserCount"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("ballSlowerCount"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("paddleFreezerCount"));
                EditorGUI.indentLevel--;
            }

            // Scoring & Par
            foldoutScoring = EditorGUILayout.Foldout(foldoutScoring, "Scoring & Speedrun Objectives", true, EditorStyles.foldoutHeader);
            if (foldoutScoring)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("parTime"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("timeBonusMax"));
                EditorGUILayout.PropertyField(serializedConfig.FindProperty("starThresholds"), true);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawBottomActions()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("💾 Save Level Asset", GUILayout.Height(30)))
            {
                EditorUtility.SetDirty(currentConfig);
                AssetDatabase.SaveAssets();
                ShowNotification(new GUIContent($"SO_Level_{selectedLevelNumber:D2}.asset saved!"));
            }

            if (GUILayout.Button("Revert to Disk", GUILayout.Height(30)))
            {
                RefreshLevelList();
                LoadLevel(selectedLevelNumber);
                ShowNotification(new GUIContent("Reverted to saved asset"));
            }

            if (GUILayout.Button("➕ New Level...", GUILayout.Height(30)))
            {
                showCreatePanel = true;
                PrepareNewLevelDefaults();
            }

            if (GUILayout.Button("🔗 Sync Levels to Scenes", GUILayout.Height(30)))
            {
                int count = SyncAllLevelPresetsToScenes(true);
                ShowNotification(new GUIContent($"Synced {count} levels to scenes!"));
            }

            if (GUILayout.Button("⚡ Generate All Previews", GUILayout.Height(30)))
            {
                LevelPreviewGenerator.GenerateAllPreviewsMenu();
                RefreshLevelList();
                LoadLevel(selectedLevelNumber);
            }

            EditorGUILayout.EndHorizontal();
        }

        public static int SyncAllLevelPresetsToScenes(bool syncMainMenu = true)
        {
            var guids = AssetDatabase.FindAssets("t:LevelConfiguration", new[] { LEVELS_DIR, "Assets/Settings" });
            var dict = new SortedDictionary<int, LevelConfiguration>();

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var cfg = AssetDatabase.LoadAssetAtPath<LevelConfiguration>(path);
                if (cfg != null && cfg.LevelNumber > 0 && !dict.ContainsKey(cfg.LevelNumber))
                {
                    dict[cfg.LevelNumber] = cfg;
                }
            }

            var list = new List<LevelConfiguration>(dict.Values);
            if (list.Count == 0) return 0;

            // 1. Sync to LV_BlockBreaker.unity (LevelGenerator and ArcadeUIManager)
            SyncLevelsToGameplayScene(GAMEPLAY_SCENE_PATH, list);

            // 2. Sync to LV_BlockBreaker_MainMenu.unity (MainMenuUIManager)
            if (syncMainMenu)
            {
                SyncLevelsToMainMenuScene(MAIN_MENU_SCENE_PATH, list);
            }

            return list.Count;
        }

        public static void SyncLevelsToGameplayScene(string scenePath, List<LevelConfiguration> configs)
        {
            if (!File.Exists(scenePath) || configs == null || configs.Count == 0) return;

            bool wasAlreadyOpen = false;
            Scene targetScene = default;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.path == scenePath && s.isLoaded)
                {
                    wasAlreadyOpen = true;
                    targetScene = s;
                    break;
                }
            }

            if (!wasAlreadyOpen)
            {
                targetScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }

            var rootObjs = targetScene.GetRootGameObjects();
            LevelGenerator lg = null;
            ArcadeUIManager hud = null;

            foreach (var root in rootObjs)
            {
                if (lg == null) lg = root.GetComponentInChildren<LevelGenerator>(true);
                if (hud == null) hud = root.GetComponentInChildren<ArcadeUIManager>(true);
            }

            bool modified = false;

            if (lg != null)
            {
                var so = new SerializedObject(lg);
                var presetsProp = so.FindProperty("levelPresets");
                if (presetsProp != null)
                {
                    presetsProp.arraySize = configs.Count;
                    for (int i = 0; i < configs.Count; i++)
                    {
                        presetsProp.GetArrayElementAtIndex(i).objectReferenceValue = configs[i];
                    }
                    so.ApplyModifiedProperties();
                    modified = true;
                }
            }

            if (hud != null)
            {
                var so = new SerializedObject(hud);
                var previewArrayProp = so.FindProperty("levelPreviewSprites");
                if (previewArrayProp != null)
                {
                    previewArrayProp.arraySize = configs.Count;
                    for (int i = 0; i < configs.Count; i++)
                    {
                        Sprite s = configs[i].PreviewSprite;
                        if (s == null)
                        {
                            s = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/UI/LevelPreviews/TX_LevelPreview_{configs[i].LevelNumber:D2}.png");
                        }
                        previewArrayProp.GetArrayElementAtIndex(i).objectReferenceValue = s;
                    }
                    so.ApplyModifiedProperties();
                    modified = true;
                }
            }

            if (modified)
            {
                EditorSceneManager.MarkSceneDirty(targetScene);
                EditorSceneManager.SaveScene(targetScene);
            }

            if (!wasAlreadyOpen)
            {
                EditorSceneManager.CloseScene(targetScene, true);
            }
        }

        public static void SyncLevelsToMainMenuScene(string scenePath, List<LevelConfiguration> configs)
        {
            if (!File.Exists(scenePath) || configs == null || configs.Count == 0) return;

            bool wasAlreadyOpen = false;
            Scene targetScene = default;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.path == scenePath && s.isLoaded)
                {
                    wasAlreadyOpen = true;
                    targetScene = s;
                    break;
                }
            }

            if (!wasAlreadyOpen)
            {
                targetScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            }

            var rootObjs = targetScene.GetRootGameObjects();
            MainMenuUIManager mm = null;

            foreach (var root in rootObjs)
            {
                if (mm == null) mm = root.GetComponentInChildren<MainMenuUIManager>(true);
            }

            bool modified = false;

            if (mm != null)
            {
                var so = new SerializedObject(mm);
                var presetsProp = so.FindProperty("levelPresets");
                if (presetsProp != null)
                {
                    presetsProp.arraySize = configs.Count;
                    for (int i = 0; i < configs.Count; i++)
                    {
                        presetsProp.GetArrayElementAtIndex(i).objectReferenceValue = configs[i];
                    }
                    modified = true;
                }

                var previewArrayProp = so.FindProperty("levelPreviewSprites");
                if (previewArrayProp != null)
                {
                    previewArrayProp.arraySize = configs.Count;
                    for (int i = 0; i < configs.Count; i++)
                    {
                        Sprite s = configs[i].PreviewSprite;
                        if (s == null)
                        {
                            s = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/UI/LevelPreviews/TX_LevelPreview_{configs[i].LevelNumber:D2}.png");
                        }
                        previewArrayProp.GetArrayElementAtIndex(i).objectReferenceValue = s;
                    }
                    modified = true;
                }

                if (modified)
                {
                    so.ApplyModifiedProperties();
                }
            }

            if (modified)
            {
                EditorSceneManager.MarkSceneDirty(targetScene);
                EditorSceneManager.SaveScene(targetScene);
            }

            if (!wasAlreadyOpen)
            {
                EditorSceneManager.CloseScene(targetScene, true);
            }
        }

        private void ApplyLiveScenePreview()
        {
            if (currentConfig == null) return;

            var lg = Object.FindAnyObjectByType<LevelGenerator>();
            if (lg != null)
            {
                lg.EnsurePrefabReferences();
                lg.ApplyCustomConfigAndReload(currentConfig);
                SceneView.RepaintAll();
            }
        }
    }
}
