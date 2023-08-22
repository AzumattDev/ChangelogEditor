using System;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
#if DEBUG
using ServerSync;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChangelogEditor
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class ChangelogEditorPlugin : BaseUnityPlugin
    {
        internal const string ModName = "ChangelogEditor";
        internal const string ModVersion = "1.0.3";
        internal const string Author = "Azumatt";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ContentFile = ModGUID + ".txt";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        private static string ContentFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ContentFile;

        internal static string ConnectionError = "";
        internal static string customFileText = "";
        internal static GameObject ChangelogGameObject = null!;

        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource ChangelogEditorLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);
#if DEBUG
        private static readonly ConfigSync ConfigSync = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };
#endif
        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            shouldShowChangelog = config("1 - Changelog", "Should Show Changelog", Toggle.On, "If on, the changelog will be shown in the main menu. If off, it will not be shown.");
            shouldChangeText = config("1 - Changelog", "Should Change Text", Toggle.On, $"If on, your configuration file's text will be added to the changelog. (at the top). This pulls from the {ContentFile} found in your config folder.");
            overrideText = config("1 - Changelog", "Override Changelog Text", Toggle.Off, "If on, only your custom text that is set will show in the changlog. This deletes the default changelog text.");
            topicText = TextEntryConfig("1 - Changelog", "Title Text", "Changelog", "Change the title text of the changelog. This is the text that shows up in the top of the changelog.");

            // Create the content file if not exist
            if (!File.Exists(ContentFileFullPath))
            {
                File.WriteAllText(ContentFileFullPath, $"This is the content file for ChangelogEditor. You can edit this file to change the changelog text.{Environment.NewLine}This file can be found currently at: {ContentFileFullPath}{Environment.NewLine}");
            }
            else
            {
                customFileText = File.ReadAllText(ContentFileFullPath, Encoding.UTF8);
            }

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;

            FileSystemWatcher contentwatcher = new(Paths.ConfigPath, ConfigFileName);
            contentwatcher.Changed += ReadContent;
            contentwatcher.Created += ReadContent;
            contentwatcher.Renamed += ReadContent;
            contentwatcher.IncludeSubdirectories = true;
            contentwatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            contentwatcher.EnableRaisingEvents = true;

            FileSystemWatcher contentwatcher2 = new(Paths.ConfigPath, ContentFile);
            contentwatcher2.Changed += ReadContent;
            contentwatcher2.Created += ReadContent;
            contentwatcher2.Renamed += ReadContent;
            contentwatcher2.IncludeSubdirectories = true;
            contentwatcher2.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            contentwatcher2.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                ChangelogEditorLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                ChangelogEditorLogger.LogError($"There was an issue loading your {ConfigFileName}");
                ChangelogEditorLogger.LogError("Please check your config entries for spelling and format!");
            }
        }

        private void ReadContent(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ContentFileFullPath)) return;
            try
            {
                // Read the content file into a string that can be used later. Include UTF-8 formatting
                customFileText = File.ReadAllText(ContentFileFullPath, Encoding.UTF8);

                if (SceneManager.GetActiveScene().name == "main") return;
                if (ChangelogGameObject.gameObject == null) return;
                if (ChangelogGameObject.GetComponent<ChangeLog>())
                    ChangelogGameObject.GetComponent<ChangeLog>().UpdateChangelog();
            }
            catch
            {
                ChangelogEditorLogger.LogError($"There was an issue loading your {ConfigFileName}");
                ChangelogEditorLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        //private static ConfigEntry<Toggle> _serverConfigLocked = null!;
        internal static ConfigEntry<Toggle> shouldShowChangelog = null!;
        internal static ConfigEntry<Toggle> shouldChangeText = null!;
        internal static ConfigEntry<Toggle> overrideText = null!;
        internal static ConfigEntry<string> topicText = null!;
#if DEBUG
        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }
#else
        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);
            //var configEntry = Config.Bind(group, name, value, description);

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description)
        {
            return config(group, name, value, new ConfigDescription(description));
        }

        internal ConfigEntry<T> TextEntryConfig<T>(string group, string name, T value, string desc)
        {
            ConfigurationManagerAttributes attributes = new()
            {
                CustomDrawer = TextAreaDrawer
            };
            return config(group, name, value, new ConfigDescription(desc, null, attributes));
        }

        internal static void TextAreaDrawer(ConfigEntryBase entry)
        {
            GUILayout.ExpandHeight(true);
            GUILayout.ExpandWidth(true);
            entry.BoxedValue = GUILayout.TextArea((string)entry.BoxedValue, GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
        }
#endif

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order = null!;
            [UsedImplicitly] public bool? Browsable = null!;
            [UsedImplicitly] public string Category = null!;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }

        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() =>
                "# Acceptable values: " + string.Join(", ", UnityInput.Current.SupportedKeyCodes);
        }

        #endregion
    }
}