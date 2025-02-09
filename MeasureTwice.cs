using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Utils;
using Jotunn.Managers;
using Configs;
using Logging;
using UnityEngine;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Extensions;
using MeasureTwice.Patches;

namespace MeasureTwice;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[BepInDependency(Jotunn.Main.ModGuid, Jotunn.Main.Version)]
[NetworkCompatibility(CompatibilityLevel.VersionCheckOnly, VersionStrictness.Patch)]
[SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
internal sealed class MeasureTwice : BaseUnityPlugin
{
    public const string PluginName = "MeasureTwice";
    internal const string Author = "Searica";
    public const string PluginGUID = $"{Author}.Valheim.{PluginName}";
    public const string PluginVersion = "0.1.0";

    internal static MeasureTwice Instance;
    internal static ConfigFile ConfigFile;
    internal static ConfigFileWatcher ConfigFileWatcher;

    internal CustomPiece SpacerBlockPiece;
    internal const string SpacerBlockName = "spacer_block";
    internal const string SpacerBlockPiecceName = "Ruler";

    // Global settings
    internal const string GlobalSection = "Global";
    public ConfigEntry<int> TimedDestruction;
    public ConfigEntry<float> ScrollSpeed;
    public ConfigEntry<KeyCode> LengthModifierKey;

    public void Awake()
    {
        Instance = this;
        ConfigFile = Config;
        Log.Init(Logger);

        Config.DisableSaveOnConfigSet();
        SetUpConfigEntries();
        Config.Save();
        Config.SaveOnConfigSet = true;

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);
        Game.isModded = true;


        // add custom TradeRouteMap
        PrefabManager.OnVanillaPrefabsAvailable += AddCustomPieces;

        // Re-initialization after reloading config and don't save since file was just reloaded
        ConfigFileWatcher = new(Config);
    }

    internal void SetUpConfigEntries()
    {
        Log.Verbosity = Config.BindConfigInOrder(
            GlobalSection,
            "Verbosity",
            Log.InfoLevel.Low,
            "Logging level."
        );

        LengthModifierKey = Config.BindConfigInOrder(
            GlobalSection,
            "Length Modifier Key",
            KeyCode.RightAlt,
            "Hold this key to scroll and adjust length of the spacer block",
            synced: false
        );

        ScrollSpeed = Config.BindConfigInOrder(
            GlobalSection,
            "Scroll Speed",
            0.05f,
            "Chnage in length for each tick of a the scroll wheel.",
            acceptableValues: new AcceptableValueRange<float>(-1f, 1f),
            synced: false
        );

        TimedDestruction = Config.BindConfigInOrder(
            GlobalSection,
            "Timed Destroy",
            20,
            "Number of seconds before spacer block self destructs.",
            acceptableValues: new AcceptableValueRange<int>(1, 30),
            synced: false
        );
    }

    public void OnDestroy()
    {
        Config.Save();
    }

    private void AddCustomPieces()
    {
        GameObject gameObject = PrefabManager.Instance.CreateClonedPrefab(SpacerBlockName, "wood_beam_1");
        gameObject.AddComponent<CustomRuler>();
        ZNetView nview = gameObject.GetComponent<ZNetView>();
        nview.m_persistent = false;  // delete these on save.

        PieceConfig pieceConfig = new()
        {
            Name = SpacerBlockPiecceName,
            Category = "Ruler",
            Requirements = new RequirementConfig[] { new RequirementConfig("Wood", 0, 0, false) },
            CraftingStation = CraftingStations.None,
            PieceTable = PieceTables.Hammer
        };
        SpacerBlockPiece = new CustomPiece(gameObject, true, pieceConfig);

        PieceManager.Instance.AddPiece(SpacerBlockPiece);

        PrefabManager.OnVanillaPrefabsAvailable -= AddCustomPieces;
    }

}
