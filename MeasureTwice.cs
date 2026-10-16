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
    public const string PluginVersion = "1.1.0";

    internal static MeasureTwice Instance;
    internal static ConfigFile ConfigFile;
    internal static ConfigFileWatcher ConfigFileWatcher;

    internal CustomPiece HorzRulerPiece;
    internal const string HorzRulerName = "horz_ruler";
    internal const string HorzRulerPieceName = "Ruler (Horizontal)";

    internal CustomPiece VertRulerPiece;
    internal const string VertRulerName = "vert_ruler";
    internal const string VertRulerPieceName = "Ruler (Vertical)";

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
            30,
            "Number of seconds before spacer block self destructs.",
            acceptableValues: new AcceptableValueRange<int>(1, 120),
            synced: false
        );
    }

    public void OnDestroy()
    {
        Config.Save();
    }

    private void AddCustomPieces()
    {
        GameObject horzRuler = PrefabManager.Instance.CreateClonedPrefab(HorzRulerName, "wood_beam_1");
        horzRuler.transform.localScale = new Vector3(horzRuler.transform.localScale.x, horzRuler.transform.localScale.y *0.5f, horzRuler.transform.localScale.z* 0.5f);
        CustomRuler horzRulerComp = horzRuler.AddComponent<CustomRuler>();
        horzRulerComp.m_horiztonal = true;

        ZNetView horzNview = horzRuler.GetComponent<ZNetView>();
        horzNview.m_persistent = false;  // delete these on save.

        PieceConfig horzRulerPieceConfig = new()
        {
            Name = HorzRulerPieceName,
            Category = "Ruler",
            Requirements = new RequirementConfig[] { new RequirementConfig("Wood", 0, 0, false) },
            CraftingStation = CraftingStations.None,
            PieceTable = PieceTables.Hammer
        };
        HorzRulerPiece = new CustomPiece(horzRuler, true, horzRulerPieceConfig);

        PieceManager.Instance.AddPiece(HorzRulerPiece);


        GameObject vertRuler = PrefabManager.Instance.CreateClonedPrefab(VertRulerName, "wood_pole");
        vertRuler.transform.localScale = new Vector3(vertRuler.transform.localScale.x * 0.5f, vertRuler.transform.localScale.y, vertRuler.transform.localScale.z * 0.5f);
        CustomRuler vertRulerComp = vertRuler.AddComponent<CustomRuler>();
        vertRulerComp.m_horiztonal = false;
        vertRuler.GetComponent<ZNetView>().m_persistent = false;  // delete these on save.

        PieceConfig vertRulerPieceConfig = new()
        {
            Name = VertRulerPieceName,
            Category = "Ruler",
            Requirements = new RequirementConfig[] { new RequirementConfig("Wood", 0, 0, false) },
            CraftingStation = CraftingStations.None,
            PieceTable = PieceTables.Hammer
        };
        VertRulerPiece = new CustomPiece(vertRuler, true, vertRulerPieceConfig);

        PieceManager.Instance.AddPiece(VertRulerPiece);


        PrefabManager.OnVanillaPrefabsAvailable -= AddCustomPieces;
    }

}
