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
    /// Editor utility for rendering pixel-perfect 128x256 level preview thumbnails
    /// and guaranteeing their serialized assignment onto LevelConfiguration ScriptableObjects
    /// and scene UI managers so they are bundled into standalone and mobile (iOS) builds.
    /// </summary>
    public static class LevelPreviewGenerator
    {
        public const string PREVIEWS_DIR = "Assets/UI/LevelPreviews";
        public const int PREVIEW_WIDTH = 128;
        public const int PREVIEW_HEIGHT = 256;
        public const string GAMEPLAY_SCENE_PATH = "Assets/Scenes/LV_BlockBreaker.unity";
        public const string MAIN_MENU_SCENE_PATH = "Assets/Scenes/LV_BlockBreaker_MainMenu.unity";

        [MenuItem("Tools/Arcade/Generate All Level Previews")]
        public static void GenerateAllPreviewsMenu()
        {
            try
            {
                int total = GenerateAllPreviews((current, count) =>
                {
                    EditorUtility.DisplayProgressBar(
                        "Generating Level Previews",
                        $"Rendering Level {current} of {count}...",
                        (float)current / count);
                });

                Debug.Log($"<color=green>[LevelPreviewGenerator] Successfully rendered and assigned {total} level previews!</color>");
                EditorUtility.DisplayDialog("Level Previews Generated",
                    $"Successfully rendered all {total} level previews to {PREVIEWS_DIR} and linked them to LevelConfiguration assets.",
                    "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Tools/Arcade/Sync All Level Previews to Assets and Scenes")]
        public static void SyncAllPreviewSpritesMenu()
        {
            int updated = SyncAllPreviewSprites();
            Debug.Log($"<color=green>[LevelPreviewGenerator] Synced {updated} preview sprites to LevelConfiguration assets and scene UI managers.</color>");
            EditorUtility.DisplayDialog("Level Previews Synced",
                $"Successfully synced {updated} preview sprites to LevelConfiguration assets and scene UI managers for iOS build inclusion.",
                "OK");
        }

        /// <summary>
        /// Renders a single level's preview into a 128x256 texture, saves as PNG,
        /// configures TextureImporter as Sprite, and assigns to the LevelConfiguration asset.
        /// </summary>
        public static bool GeneratePreview(int levelNumber, LevelConfiguration config = null, string outputPath = null)
        {
            if (config == null)
            {
                string configPath = $"Assets/Settings/Levels/SO_Level_{levelNumber:D2}.asset";
                config = AssetDatabase.LoadAssetAtPath<LevelConfiguration>(configPath);
                if (config == null)
                {
                    Debug.LogWarning($"[LevelPreviewGenerator] Could not find LevelConfiguration for Level {levelNumber} at {configPath}");
                    return false;
                }
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = $"{PREVIEWS_DIR}/TX_LevelPreview_{levelNumber:D2}.png";
            }

            if (!Directory.Exists(PREVIEWS_DIR))
            {
                Directory.CreateDirectory(PREVIEWS_DIR);
                AssetDatabase.Refresh();
            }

            // Ensure gameplay scene is open
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != GAMEPLAY_SCENE_PATH)
            {
                if (File.Exists(GAMEPLAY_SCENE_PATH))
                {
                    EditorSceneManager.OpenScene(GAMEPLAY_SCENE_PATH, OpenSceneMode.Single);
                }
                else
                {
                    Debug.LogError($"[LevelPreviewGenerator] Cannot find gameplay scene at {GAMEPLAY_SCENE_PATH}");
                    return false;
                }
            }

            var lg = Object.FindAnyObjectByType<LevelGenerator>();
            var cam = Camera.main;

            if (lg == null || cam == null)
            {
                Debug.LogError($"[LevelPreviewGenerator] Missing LevelGenerator or MainCamera in {GAMEPLAY_SCENE_PATH}");
                return false;
            }

            // Ensure block prefabs are loaded and generate the level
            lg.EnsurePrefabReferences();
            lg.ApplyCustomConfigAndReload(config);

            // Clean any dynamic projectiles or capsules
            PowerupCapsule.ClearAllFallingCapsules();
            LaserBolt.ClearAllActiveBolts();

            // Adjust framing for 1:2 aspect ratio (128x256)
            var respCam = cam.GetComponent<ResponsiveCameraController>();
            if (respCam != null)
            {
                respCam.AdjustCameraFraming();
            }

            // Render to 128x256 RenderTexture
            var rt = new RenderTexture(PREVIEW_WIDTH, PREVIEW_HEIGHT, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            var prevRt = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prevRt;

            RenderTexture.active = rt;
            var tex = new Texture2D(PREVIEW_WIDTH, PREVIEW_HEIGHT, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, PREVIEW_WIDTH, PREVIEW_HEIGHT), 0, 0);
            tex.Apply();
            RenderTexture.active = null;

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(outputPath, bytes);

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);

            // Import texture and configure as Sprite
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(outputPath) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }
                if (importer.filterMode != FilterMode.Bilinear)
                {
                    importer.filterMode = FilterMode.Bilinear;
                    dirty = true;
                }
                if (importer.wrapMode != TextureWrapMode.Clamp)
                {
                    importer.wrapMode = TextureWrapMode.Clamp;
                    dirty = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }
                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }

            // Assign sprite directly to LevelConfiguration asset
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(outputPath);
            if (sprite != null)
            {
                config.SetPreviewSprite(sprite);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            return true;
        }

        /// <summary>
        /// Batch renders all 30 levels and assigns their sprites.
        /// </summary>
        public static int GenerateAllPreviews(System.Action<int, int> onProgress = null)
        {
            int successCount = 0;
            const int totalLevels = 30;

            for (int lvl = 1; lvl <= totalLevels; lvl++)
            {
                onProgress?.Invoke(lvl, totalLevels);
                if (GeneratePreview(lvl))
                {
                    successCount++;
                }
            }

            SyncAllPreviewSprites();
            return successCount;
        }

        /// <summary>
        /// Synchronizes all 30 preview sprites from Assets/UI/LevelPreviews to:
        /// 1. Each LevelConfiguration ScriptableObject asset (previewSprite field)
        /// 2. MainMenuUIManager.levelPreviewSprites array in LV_BlockBreaker_MainMenu.unity
        /// 3. ArcadeUIManager.levelPreviewSprites array in LV_BlockBreaker.unity
        /// </summary>
        public static int SyncAllPreviewSprites()
        {
            int count = 0;

            // 1. Assign to each LevelConfiguration asset
            for (int lvl = 1; lvl <= 30; lvl++)
            {
                string spritePath = $"{PREVIEWS_DIR}/TX_LevelPreview_{lvl:D2}.png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite == null) continue;

                string configPath = $"Assets/Settings/Levels/SO_Level_{lvl:D2}.asset";
                var config = AssetDatabase.LoadAssetAtPath<LevelConfiguration>(configPath);
                if (config != null)
                {
                    config.SetPreviewSprite(sprite);
                    EditorUtility.SetDirty(config);
                    count++;
                }
            }
            AssetDatabase.SaveAssets();

            // 2. Sync to MainMenu scene if exists
            if (File.Exists(MAIN_MENU_SCENE_PATH))
            {
                var scene = EditorSceneManager.OpenScene(MAIN_MENU_SCENE_PATH, OpenSceneMode.Single);
                var menuMgr = Object.FindAnyObjectByType<MainMenuUIManager>();
                if (menuMgr != null)
                {
                    var so = new SerializedObject(menuMgr);
                    var previewArrayProp = so.FindProperty("levelPreviewSprites");
                    if (previewArrayProp != null)
                    {
                        previewArrayProp.arraySize = 30;
                        for (int i = 0; i < 30; i++)
                        {
                            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PREVIEWS_DIR}/TX_LevelPreview_{i + 1:D2}.png");
                            previewArrayProp.GetArrayElementAtIndex(i).objectReferenceValue = sprite;
                        }
                    }
                    so.ApplyModifiedProperties();
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }

            // 3. Sync to Gameplay scene if exists
            if (File.Exists(GAMEPLAY_SCENE_PATH))
            {
                var scene = EditorSceneManager.OpenScene(GAMEPLAY_SCENE_PATH, OpenSceneMode.Single);
                var hudMgr = Object.FindAnyObjectByType<ArcadeUIManager>();
                if (hudMgr != null)
                {
                    var so = new SerializedObject(hudMgr);
                    var previewArrayProp = so.FindProperty("levelPreviewSprites");
                    if (previewArrayProp != null)
                    {
                        previewArrayProp.arraySize = 30;
                        for (int i = 0; i < 30; i++)
                        {
                            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PREVIEWS_DIR}/TX_LevelPreview_{i + 1:D2}.png");
                            previewArrayProp.GetArrayElementAtIndex(i).objectReferenceValue = sprite;
                        }
                    }
                    so.ApplyModifiedProperties();
                }

                // Also ensure LevelGenerator levelPresets array is intact
                var lg = Object.FindAnyObjectByType<LevelGenerator>();
                if (lg != null)
                {
                    var lgSo = new SerializedObject(lg);
                    var presetsProp = lgSo.FindProperty("levelPresets");
                    if (presetsProp != null && presetsProp.arraySize == 30)
                    {
                        for (int i = 0; i < 30; i++)
                        {
                            var lvl = AssetDatabase.LoadAssetAtPath<LevelConfiguration>($"Assets/Settings/Levels/SO_Level_{i + 1:D2}.asset");
                            presetsProp.GetArrayElementAtIndex(i).objectReferenceValue = lvl;
                        }
                        lgSo.ApplyModifiedProperties();
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            return count;
        }
    }
}
