using System;
using System.IO;
using Arcade.Core;
using UnityEditor;
using UnityEngine;

namespace Arcade.Editor
{
    /// <summary>
    /// Interactive EditorWindow for configuring iOS Xcode project build settings,
    /// including bundle version, build number overrides, target SDK (Device vs. Simulator),
    /// output directory management, and direct access to Unity Player Settings.
    /// </summary>
    public class BuildXcodeWindow : EditorWindow
    {
        private string versionString = "0.1.0";
        private string buildNumber = "1";
        private iOSSdkVersion sdkVersion = iOSSdkVersion.DeviceSDK;
        private string outputRootDirectory = SetupIOSBuildProfile.OutputBuildPath;
        private bool autoIncrementBuild = true;
        private bool autoIncrementVersion = false;
        private bool applyStandardPresets = true;

        private Vector2 scrollPosition;

        public static string OutputRootPrefKey => $"Arcade.BuildXcodeWindow.OutputRoot.{Application.dataPath.GetHashCode():X8}";

        public static string GetSavedOutputRootDirectory()
        {
            string saved = EditorPrefs.GetString(OutputRootPrefKey, string.Empty);
            return string.IsNullOrWhiteSpace(saved) ? SetupIOSBuildProfile.OutputBuildPath : saved.Trim();
        }

        public static void SaveOutputRootDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                EditorPrefs.DeleteKey(OutputRootPrefKey);
            }
            else
            {
                EditorPrefs.SetString(OutputRootPrefKey, path.Trim());
            }
        }

        [MenuItem("Tools/Arcade/Build Xcode Project...", priority = 60)]
        [MenuItem("Window/Arcade/Build Xcode Project...", priority = 101)]
        public static void OpenWindow()
        {
            var window = GetWindow<BuildXcodeWindow>("Build Xcode Project");
            window.minSize = new Vector2(500, 540);
            window.Show();
        }

        private void OnEnable()
        {
            LoadCurrentSettings();
        }

        public void LoadCurrentSettings()
        {
            versionString = string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion)
                ? "0.1.0"
                : PlayerSettings.bundleVersion;

            buildNumber = string.IsNullOrWhiteSpace(PlayerSettings.iOS.buildNumber)
                ? "1"
                : PlayerSettings.iOS.buildNumber;

            sdkVersion = PlayerSettings.iOS.sdkVersion;
            outputRootDirectory = GetSavedOutputRootDirectory();
        }

        public void SaveSettingsToProject()
        {
            PlayerSettings.bundleVersion = versionString.Trim();
            PlayerSettings.iOS.buildNumber = buildNumber.Trim();
            PlayerSettings.iOS.sdkVersion = sdkVersion;

            if (string.IsNullOrWhiteSpace(outputRootDirectory))
            {
                outputRootDirectory = SetupIOSBuildProfile.OutputBuildPath;
            }
            else
            {
                outputRootDirectory = outputRootDirectory.Trim();
            }
            SaveOutputRootDirectory(outputRootDirectory);

            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildXcodeWindow] Saved Player Settings: Product = {PlayerSettings.productName}, Version = {PlayerSettings.bundleVersion}, iOS Build = #{PlayerSettings.iOS.buildNumber}, SDK = {PlayerSettings.iOS.sdkVersion}, OutputRoot = {outputRootDirectory}");
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(8);

            DrawVersioningSection();
            EditorGUILayout.Space(8);

            DrawTargetSdkSection();
            EditorGUILayout.Space(8);

            DrawOutputDirectorySection();
            EditorGUILayout.Space(8);

            DrawPlayerSettingsSection();
            EditorGUILayout.Space(12);

            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("iOS Xcode Project Builder", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Product: {PlayerSettings.productName} | Bundle ID: {PlayerSettings.applicationIdentifier}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Configure target SDK, version, and build number overrides before exporting to Xcode.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void DrawVersioningSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Versioning & Build Number", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                $"Current PlayerSettings on Disk:\nVersion: {PlayerSettings.bundleVersion}  |  iOS Build: #{PlayerSettings.iOS.buildNumber}",
                MessageType.None);

            EditorGUILayout.Space(4);

            // Bundle Version
            EditorGUILayout.BeginHorizontal();
            versionString = EditorGUILayout.TextField("Version (bundleVersion)", versionString);
            if (GUILayout.Button("+ Patch", GUILayout.Width(65)))
            {
                versionString = BuildVersionUtility.IncrementVersionString(versionString);
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            // Build Number
            EditorGUILayout.BeginHorizontal();
            buildNumber = EditorGUILayout.TextField("Build (iOS.buildNumber)", buildNumber);
            if (GUILayout.Button("+ 1", GUILayout.Width(65)))
            {
                if (int.TryParse(buildNumber, out int n))
                {
                    buildNumber = (n + 1).ToString();
                }
                else
                {
                    buildNumber = "1";
                }
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            autoIncrementBuild = EditorGUILayout.ToggleLeft("Auto-increment build number on build (+1)", autoIncrementBuild);
            autoIncrementVersion = EditorGUILayout.ToggleLeft("Auto-increment patch version on build", autoIncrementVersion);

            EditorGUILayout.EndVertical();
        }

        private void DrawTargetSdkSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Target SDK (Device vs Simulator)", EditorStyles.boldLabel);

            sdkVersion = (iOSSdkVersion)EditorGUILayout.EnumPopup("Target SDK", sdkVersion);

            if (sdkVersion == iOSSdkVersion.DeviceSDK)
            {
                EditorGUILayout.HelpBox(
                    "Device SDK: Exports for physical iOS hardware (arm64).\nRequired for TestFlight, AdHoc, and App Store builds.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Simulator SDK: Exports for Xcode iOS Simulator (x86_64 / arm64-simulator).\nAllows running and testing on Mac Xcode simulator without physical iOS devices.",
                    MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawOutputDirectorySection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Destination Folder", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            outputRootDirectory = EditorGUILayout.TextField("Output Root", outputRootDirectory);
            if (EditorGUI.EndChangeCheck())
            {
                SaveOutputRootDirectory(outputRootDirectory);
            }

            if (GUILayout.Button("Browse...", GUILayout.Width(75)))
            {
                string initialFolder = Directory.Exists(outputRootDirectory) ? outputRootDirectory : string.Empty;
                string chosen = EditorUtility.OpenFolderPanel("Choose iOS Builds Root Folder", initialFolder, string.Empty);
                if (!string.IsNullOrEmpty(chosen))
                {
                    // Convert to relative path only if genuinely inside the project directory
                    string projectPath = Path.GetFullPath(".").Replace('\\', '/').TrimEnd('/');
                    chosen = chosen.Replace('\\', '/').TrimEnd('/');
                    if (chosen.Equals(projectPath, StringComparison.OrdinalIgnoreCase))
                    {
                        chosen = ".";
                    }
                    else if (chosen.StartsWith(projectPath + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        chosen = chosen.Substring(projectPath.Length + 1);
                    }

                    outputRootDirectory = chosen;
                    SaveOutputRootDirectory(outputRootDirectory);
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();

            string effectiveRoot = string.IsNullOrWhiteSpace(outputRootDirectory)
                ? SetupIOSBuildProfile.OutputBuildPath
                : outputRootDirectory.Trim();

            // Calculate next folder name
            int parsedCurrent = 0;
            int.TryParse(buildNumber, out parsedCurrent);
            int nextNumber = autoIncrementBuild
                ? BuildVersionUtility.GetNextBuildNumber(effectiveRoot, parsedCurrent)
                : Mathf.Max(1, parsedCurrent);

            string folderName = BuildVersionUtility.FormatBuildFolderName(nextNumber);
            string destinationSubfolder = Path.Combine(effectiveRoot, folderName).Replace('\\', '/');

            EditorGUILayout.LabelField("Predicted Destination:", destinationSubfolder, EditorStyles.miniLabel);

            if (Directory.Exists(destinationSubfolder))
            {
                EditorGUILayout.HelpBox($"Target folder '{destinationSubfolder}' already exists and will be overwritten on build.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPlayerSettingsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Player Settings & Presets", EditorStyles.boldLabel);

            applyStandardPresets = EditorGUILayout.ToggleLeft("Apply Standard Arcade iOS Settings on Build (Swift, URP, Stripping)", applyStandardPresets);

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("⚙ Open Player Settings...", GUILayout.Height(26)))
            {
                SettingsService.OpenProjectSettings("Project/Player");
            }

            if (GUILayout.Button("Apply Presets Now", GUILayout.Height(26)))
            {
                SaveOutputRootDirectory(outputRootDirectory);
                SetupIOSBuildProfile.ApplySettings();
                LoadCurrentSettings();
                ShowNotification(new GUIContent("Arcade iOS Presets Applied!"));
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            // Save Settings Button
            if (GUILayout.Button("💾 Save Player Settings (No Build)", GUILayout.Height(34)))
            {
                SaveSettingsToProject();
                ShowNotification(new GUIContent("Player Settings saved!"));
            }

            // Build Button
            GUI.backgroundColor = new Color(0.25f, 0.75f, 1.0f);
            if (GUILayout.Button("🚀 Build Xcode Project", GUILayout.Height(34)))
            {
                ExecuteBuildFromWindow();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void ExecuteBuildFromWindow()
        {
            outputRootDirectory = string.IsNullOrWhiteSpace(outputRootDirectory)
                ? SetupIOSBuildProfile.OutputBuildPath
                : outputRootDirectory.Trim();
            SaveOutputRootDirectory(outputRootDirectory);

            if (applyStandardPresets)
            {
                SetupIOSBuildProfile.ApplySettings();
            }

            // Apply user overrides
            if (autoIncrementBuild)
            {
                int current = 0;
                int.TryParse(buildNumber, out current);
                int next = BuildVersionUtility.GetNextBuildNumber(outputRootDirectory, current);
                buildNumber = next.ToString();
            }

            if (autoIncrementVersion)
            {
                versionString = BuildVersionUtility.IncrementVersionString(versionString);
            }

            // Save changes to project
            SaveSettingsToProject();
            LoadCurrentSettings();

            // Ensure destination folder
            if (!Directory.Exists(outputRootDirectory))
            {
                Directory.CreateDirectory(outputRootDirectory);
            }

            string folderName = BuildVersionUtility.FormatBuildFolderName(int.Parse(buildNumber));
            string outPath = Path.Combine(outputRootDirectory, folderName).Replace('\\', '/');
            if (!Directory.Exists(outPath))
            {
                Directory.CreateDirectory(outPath);
            }

            // Platform check
            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                string msg = $"Player settings and build directory prepared successfully!\n\n" +
                             $"• Version: {PlayerSettings.bundleVersion}\n" +
                             $"• Build: #{PlayerSettings.iOS.buildNumber}\n" +
                             $"• SDK: {PlayerSettings.iOS.sdkVersion}\n" +
                             $"• Folder: {outPath}\n\n" +
                             $"Note: Generating and compiling native Xcode projects requires macOS with Xcode.\n" +
                             $"Your project settings have been saved to ProjectSettings.asset.";

                EditorUtility.DisplayDialog("iOS Settings & Folder Prepared", msg, "OK");
                Debug.LogWarning($"[BuildXcodeWindow] Windows host detected. Xcode project export skipped, but settings and destination prepared at {outPath}.");
                EditorUtility.RevealInFinder(outPath);
                return;
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                EditorUtility.DisplayDialog("iOS Module Missing",
                    "The iOS Build Support module is not installed in this Unity Editor installation.", "OK");
                Debug.LogError("[BuildXcodeWindow] iOS build support module is not installed.");
                return;
            }

            var buildOptions = new BuildPlayerOptions
            {
                scenes = SetupIOSBuildProfile.RequiredScenes,
                locationPathName = outPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None
            };

            Debug.Log($"[BuildXcodeWindow] Starting iOS build #{PlayerSettings.iOS.buildNumber} ({PlayerSettings.bundleVersion}, SDK: {PlayerSettings.iOS.sdkVersion}) to: {outPath}...");
            var report = BuildPipeline.BuildPlayer(buildOptions);
            Debug.Log($"[BuildXcodeWindow] Build completed with result: {report.summary.result} ({report.summary.totalErrors} errors). Output directory: {outPath}");

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(outPath);
                ShowNotification(new GUIContent("Xcode Project Export Succeeded!"));
            }
            else
            {
                EditorUtility.DisplayDialog("Build Failed",
                    $"Xcode build failed with {report.summary.totalErrors} errors. Check the Console for details.", "OK");
            }
        }
    }
}

