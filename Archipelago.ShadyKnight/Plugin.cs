using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

using UnityEngine;

namespace Archipelago.ShadyKnight;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginName = "Archipelago";
    public const string PluginGuid = "tipe.shadyknight.archipelago";
    public const string PluginVersion = "1.0.0";
    
    internal static new ManualLogSource Logger { get; private set; } = null!;

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"I'm tired ;-;");

        new Harmony(PluginGuid).PatchAll();
    }
}