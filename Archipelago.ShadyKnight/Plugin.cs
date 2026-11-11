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

    public static BepInEx.Configuration.ConfigEntry<string> SlotNameEntry { get; set; }
    public static BepInEx.Configuration.ConfigEntry<string> ServerNameEntry { get; set; }
    public static BepInEx.Configuration.ConfigEntry<int> PortEntry { get; set; }
    public static BepInEx.Configuration.ConfigEntry<string> PasswordEntry { get; set; }

    internal static string apServerUri = "archipelago.gg";
    internal static int apServerPort = 38281;
    internal static string apSlotName = "";
    internal static string apPassword;

    private ArchipelagoClient AP;

    void Awake()
    {
        Log.Init(Logger);
        CreateConfigurations();

        apSlotName = SlotNameEntry.Value;
        apServerUri = ServerNameEntry.Value;
        apServerPort = PortEntry.Value;
        apPassword = PasswordEntry.Value;

        AP = new ArchipelagoClient();

        new Harmony(PluginGuid).PatchAll();
    }

    void OnGUI()
    {
        if (!AP.Connected)
        {
            apServerUri = GUILayout.TextArea(apServerUri);
            apSlotName = GUILayout.TextArea(apSlotName);
            apPassword = GUILayout.TextArea(apPassword);
            if (GUILayout.Button("Connect"))
            {
                AP.Connect(apServerUri + ":" + apServerPort, apSlotName, apPassword);
                Log.LogDebug($"Server {apServerUri} Port: {apServerPort} Slot: {apSlotName} Password: {apPassword}");
            }
        }
        else
        {
            if (GUILayout.Button("Disconnect"))
            {
                AP.Disconnect();
            }
        }
    }

    private void CreateConfigurations()
    {
        SlotNameEntry = Config.Bind(
            "SlotName",
            "slotName",
            "",
            "Change the default slot name");
        ServerNameEntry = Config.Bind(
            "ServerName",
            "serverName",
            "archipelago.gg",
            "Change the default server name");
        PortEntry = Config.Bind(
            "Port",
            "port",
            38281,
            "Change the default port");
        PasswordEntry = Config.Bind(
            "Password",
            "password",
            "",
            "Change the default password");

    }
}
