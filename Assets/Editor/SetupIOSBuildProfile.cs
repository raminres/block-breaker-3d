using System.IO;
using System.Linq;
using Arcade.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace Arcade.Editor
{
    /// <summary>
    /// Automated setup tool that configures the iOS Build Profile, Swift Xcode project settings,
    /// and required gameplay scenes. Can be run via menu or CLI to ensure consistent Xcode builds.
    /// </summary>
    public static class SetupIOSBuildProfile
    {
        public const string ProfilePath = "Assets/Settings/Build Profiles/iOS.asset";
        public const string OutputBuildPath = "Builds/BlockBreakerBuilds";
        public const string CompanyName = "RaminRasulzade";
        public const string ProductName = "BlockBreaker";
        public const string BundleIdentifier = "com.RaminRasulzade.BlockBreaker";
        public const string IosMinVersion = "26.0";

        public static readonly string[] RequiredScenes = new[]
        {
            "Assets/Scenes/LV_BlockBreaker_MainMenu.unity",
            "Assets/Scenes/LV_BlockBreaker.unity"
        };

        [MenuItem("Tools/Arcade/Setup iOS Build Profile & Player Settings")]
        public static void ApplySettings()
        {
            // 1. Ensure Build Profiles folder exists and activate iOS BuildProfile first
            // so PlayerSettings queries/writes target the iOS/Global profile rather than any active Web/Desktop override profile.
            string folder = Path.GetDirectoryName(ProfilePath);
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }

            var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(ProfilePath);
            if (profile == null)
            {
                var guid = new GUID("ad48d16a66894befa4d8181998c3cb09");
                var method = typeof(BuildProfile).GetMethod("CreateInstance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
                    null,
                    new[] { typeof(GUID), typeof(string) },
                    null);
                if (method != null)
                {
                    profile = method.Invoke(null, new object[] { guid, ProfilePath }) as BuildProfile;
                }
            }

            if (profile != null)
            {
                profile.overrideGlobalScenes = true;
                profile.scenes = RequiredScenes.Select(s => new EditorBuildSettingsScene(s, true)).ToArray();
                EditorUtility.SetDirty(profile);

                // Set as active build profile
                var prop = typeof(EditorUserBuildSettings).GetProperty("activeBuildProfile",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                prop?.SetValue(null, profile);
            }

            // 2. Configure Player Settings without overwriting user-authored product/company/bundle names
            if (string.IsNullOrWhiteSpace(PlayerSettings.companyName))
            {
                PlayerSettings.companyName = CompanyName;
            }
            if (string.IsNullOrWhiteSpace(PlayerSettings.productName))
            {
                PlayerSettings.productName = ProductName;
            }
            if (string.IsNullOrWhiteSpace(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS)))
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleIdentifier);
            }
            if (string.IsNullOrWhiteSpace(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone)))
            {
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, BundleIdentifier);
            }
            PlayerSettings.iOS.targetOSVersionString = IosMinVersion;
            PlayerSettings.xcodeProjectType = XcodeProjectType.Swift;

            // iOS Size & Performance Optimizations
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.iOS, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.iOS, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.stripEngineCode = true;

            AssetDatabase.SaveAssets();
            Debug.Log($"[SetupIOSBuildProfile] Successfully applied iOS build profile & player settings for {PlayerSettings.productName} ({PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS)}).");
        }

        [MenuItem("Tools/Arcade/Prepare Next iOS Build Folder & Version")]
        public static string PrepareNextBuildFolderMenu()
        {
            string path = BuildVersionUtility.PrepareNextBuild(BuildXcodeWindow.GetSavedOutputRootDirectory());
            Debug.Log($"[SetupIOSBuildProfile] Prepared next iOS build folder: {path} (buildNumber: {PlayerSettings.iOS.buildNumber}, bundleVersion: {PlayerSettings.bundleVersion})");
            return path;
        }

        /// <summary>
        /// Public helper for CLI or scripts to advance build/version and create the next subfolder.
        /// </summary>
        public static string PrepareNextBuildDirectory(string baseDirectory = OutputBuildPath)
        {
            return BuildVersionUtility.PrepareNextBuild(baseDirectory);
        }

        [MenuItem("Tools/Arcade/Build Xcode Project...", priority = 60)]
        public static void BuildXcode()
        {
            BuildXcodeWindow.OpenWindow();
        }

        /// <summary>
        /// Headless build method for CI or automated CLI build scripts.
        /// </summary>
        public static void BuildXcodeHeadless(string outPath = null, iOSSdkVersion? sdk = null, string versionOverride = null, string buildOverride = null)
        {
            ApplySettings();

            if (!string.IsNullOrEmpty(versionOverride))
            {
                PlayerSettings.bundleVersion = versionOverride;
            }

            if (!string.IsNullOrEmpty(buildOverride))
            {
                PlayerSettings.iOS.buildNumber = buildOverride;
            }

            if (sdk.HasValue)
            {
                PlayerSettings.iOS.sdkVersion = sdk.Value;
            }

            AssetDatabase.SaveAssets();

            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                Debug.LogWarning("[SetupIOSBuildProfile] Xcode project export requires macOS with Xcode. Skipping build execution on Windows.");
                return;
            }

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                Debug.LogError("[SetupIOSBuildProfile] iOS build support module is not installed in this Unity Editor installation.");
                return;
            }

            if (string.IsNullOrEmpty(outPath))
            {
                outPath = BuildVersionUtility.PrepareNextBuild(BuildXcodeWindow.GetSavedOutputRootDirectory());
            }

            var buildOptions = new BuildPlayerOptions
            {
                scenes = RequiredScenes,
                locationPathName = outPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None
            };

            Debug.Log($"[SetupIOSBuildProfile] Starting headless iOS build #{PlayerSettings.iOS.buildNumber} ({PlayerSettings.bundleVersion}, SDK: {PlayerSettings.iOS.sdkVersion}) to: {outPath}...");
            var report = BuildPipeline.BuildPlayer(buildOptions);
            Debug.Log($"[SetupIOSBuildProfile] Build completed with result: {report.summary.result} ({report.summary.totalErrors} errors). Output directory: {outPath}");
        }
    }
}
