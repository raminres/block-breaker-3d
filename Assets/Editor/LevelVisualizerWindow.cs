using System.Collections.Generic;
using Arcade.BlockBreaker;
using Arcade.Core;
using UnityEditor;
using UnityEngine;

namespace Arcade.Editor
{
    /// <summary>
    /// Interactive EditorWindow for inspecting, editing, and visually verifying BlockBreaker level layouts,
    /// block color tiers, power-up/hazard distributions, and managing level preview thumbnail sprites.
    /// </summary>
    public class LevelVisualizerWindow : EditorWindow
    {
        private const int TOTAL_LEVELS = 30;

        private int selectedLevelNumber = 1;
        private LevelConfiguration currentConfig;
        private SerializedObject serializedConfig;

        private Vector2 scrollPosLeft;
        private Vector2 scrollPosRight;
        private bool liveScenePreview = false;

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
            LoadLevel(selectedLevelNumber);
        }

        private void LoadLevel(int levelNumber)
        {
            selectedLevelNumber = Mathf.Clamp(levelNumber, 1, TOTAL_LEVELS);
            string path = $"Assets/Settings/Levels/SO_Level_{selectedLevelNumber:D2}.asset";
            currentConfig = AssetDatabase.LoadAssetAtPath<LevelConfiguration>(path);

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

            if (currentConfig == null)
            {
                EditorGUILayout.HelpBox($"Could not find LevelConfiguration at Assets/Settings/Levels/SO_Level_{selectedLevelNumber:D2}.asset", MessageType.Warning);
                if (GUILayout.Button("Create / Restore Default Level Presets", GUILayout.Height(30)))
                {
                    SetupBlockBreakerScenes.CreateOrUpdateLevelPresets();
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

            // Previous button
            GUI.enabled = selectedLevelNumber > 1;
            if (GUILayout.Button("◄ Prev", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                LoadLevel(selectedLevelNumber - 1);
            }
            GUI.enabled = true;

            // Level selector popup
            string[] levelOptions = new string[TOTAL_LEVELS];
            for (int i = 0; i < TOTAL_LEVELS; i++)
            {
                levelOptions[i] = $"Level {i + 1:D2}: {(currentConfig != null && selectedLevelNumber == i + 1 ? currentConfig.LevelName : $"Stage {i + 1}")}";
            }

            int newSelected = EditorGUILayout.Popup(selectedLevelNumber - 1, levelOptions, EditorStyles.toolbarPopup, GUILayout.Width(220));
            if (newSelected != selectedLevelNumber - 1)
            {
                LoadLevel(newSelected + 1);
            }

            // Next button
            GUI.enabled = selectedLevelNumber < TOTAL_LEVELS;
            if (GUILayout.Button("Next ►", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                LoadLevel(selectedLevelNumber + 1);
            }
            GUI.enabled = true;

            GUILayout.Space(10);

            // Direct object field
            var picked = (LevelConfiguration)EditorGUILayout.ObjectField(currentConfig, typeof(LevelConfiguration), false, GUILayout.Width(220));
            if (picked != null && picked != currentConfig)
            {
                currentConfig = picked;
                selectedLevelNumber = picked.LevelNumber;
                serializedConfig = new SerializedObject(currentConfig);
            }

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
                LoadLevel(selectedLevelNumber);
            }

            EditorGUILayout.EndHorizontal();
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
                LoadLevel(selectedLevelNumber);
                ShowNotification(new GUIContent("Reverted to saved asset"));
            }

            if (GUILayout.Button("⚡ Generate All 30 Previews", GUILayout.Height(30)))
            {
                LevelPreviewGenerator.GenerateAllPreviewsMenu();
                LoadLevel(selectedLevelNumber);
            }

            EditorGUILayout.EndHorizontal();
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
