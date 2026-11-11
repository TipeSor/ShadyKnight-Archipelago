using System;
using System.Collections.Generic;
using System.Reflection;

using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;

using UnityEngine;

namespace Archipelago.ShadyKnight;

public class ArchipelagoClient : IDisposable
{
    private static bool jsonConfiguredForUnity;

    public delegate void ClientDisconnected(string reason);
    public event ClientDisconnected OnClientDisconnect;

    public string lastServerUrl { get; set; }
    public string lastSlotName { get; set; }
    public string lastPassword { get; set; }

    private ArchipelagoSession session;

    public bool reconnecting { get; set; } = false;
    public bool Connected => session != null && session.Socket.Connected;

    public void Connect(string url, string slotName, string password = null)
    {
        if (session != null && session.Socket.Connected)
        {
            Disconnect();
            return;
        }

        Log.LogInfo($"Attempting to connect to Archipelago at {url}.");
        lastServerUrl = url;
        lastSlotName = slotName;
        lastPassword = password;

        if (!ConfigureJsonForUnity())
        {
            OnClientDisconnect?.Invoke("Could not configure Newtonsoft.Json for this Unity runtime.");
            return;
        }

        try
        {
            session = ArchipelagoSessionFactory.CreateSession(url);
            session.Socket.SocketOpened += () => Log.LogInfo("Archipelago WebSocket opened.");
            session.Socket.PacketReceived += packet => Log.LogInfo($"Archipelago packet received: {packet.GetType().Name}.");
            session.Socket.PacketsSent += packets =>
            {
                foreach (var packet in packets)
                {
                    Log.LogInfo($"Archipelago packet sent: {packet.GetType().Name}.");
                }
            };
            session.Socket.ErrorReceived += (exception, message) =>
                Log.LogError($"Archipelago socket error: {message} {exception}");
            session.Socket.SocketClosed += reason => Log.LogWarning($"Archipelago WebSocket closed: {reason}");
        }
        catch (Exception e)
        {
            Log.LogError($"Could not create Archipelago session: {e}");
            OnClientDisconnect?.Invoke(e.Message);
            return;
        }

        // ItemLogic = new ArchipelagoItemLogicController(session);
        // itemCheckBar = null;
        // shrineCheckBar = null;
        // if (!isInGame) lastReceivedItemindex = 0;

        try
        {
            Log.LogInfo($"Connecting as slot '{slotName}' to game 'Shady Knight'.");
            LoginResult result = session.TryConnectAndLogin("Shady Knight", slotName, ItemsHandlingFlags.AllItems, new Version(0, 6, 4), password: password);

            if (!result.Successful)
            {
                LoginFailure failureResult = (LoginFailure)result;
                foreach (var err in failureResult.Errors)
                {
                    Log.LogError($"Archipelago login failed: {err}");
                }
                Dispose();
                return;
            }

            Log.LogInfo("Connected and logged in to Archipelago.");
        }
        catch (Exception e)
        {
            Log.LogError($"Archipelago connection/login threw an exception: {e}");
            OnClientDisconnect?.Invoke(e.Message);
            Dispose();
        }
    }

    private static bool ConfigureJsonForUnity()
    {
        if (jsonConfiguredForUnity)
        {
            return true;
        }

        try
        {
            Type reflector = typeof(Newtonsoft.Json.JsonConvert).Assembly.GetType(
                "Newtonsoft.Json.Serialization.JsonTypeReflector",
                throwOnError: true);
            MethodInfo setDynamicCodeGeneration = reflector.GetMethod(
                "SetDynamicCodeGeneration",
                BindingFlags.Static | BindingFlags.NonPublic);

            if (setDynamicCodeGeneration == null)
            {
                Log.LogError("This Newtonsoft.Json build does not expose the expected runtime compatibility switch.");
                return false;
            }

            setDynamicCodeGeneration.Invoke(null, [false]);
            jsonConfiguredForUnity = true;
            Log.LogInfo("Configured Newtonsoft.Json to use reflection instead of dynamic code generation.");
            return true;
        }
        catch (Exception e)
        {
            Log.LogError($"Could not configure Newtonsoft.Json for Unity: {e}");
            return false;
        }
    }

    public void Disconnect()
    {
        if (session != null && session.Socket.Connected)
        {
            // ArchipelagoConnectButtonController.ChangeButtonWhenDisconnected();
            session.Socket.DisconnectAsync();
        }
    }

    public IEnumerator<WaitForSeconds> AttemptReconnection()
    {
        Log.LogDebug("Attempting to reconnect!");
        var retryCounter = 0;
        // if (!isInGame)
        // {
        //    ArchipelagoConnectButtonController.ChangeButtonWhenDisconnected();
        // }

        while ((session == null || !session.Socket.Connected) && retryCounter < 5)
        {
            Log.LogInfo($"Connection attempt #{retryCounter + 1}");
            retryCounter++;
            yield return new WaitForSeconds(3f);
            Connect(lastServerUrl, lastSlotName, lastPassword);
        }

        if (session == null || !session.Socket.Connected)
        {
            Log.LogError("Could not connect to Archipelago.");
            Dispose();
        }
        else if (session != null && session.Socket.Connected)
        {
            Log.LogInfo("Established Archipelago connection.");
            // new ArchipelagoStartMessage().Send(NetworkDestination.Clients);
            // if (Locationhandler != null && isInGame)
            // {
            //    Locationhandler.CatchUpSceneLocations(LocationHandler.sceneDef.cachedName);
            //    Locationhandler.LoadItemPickupHooks();
            // }
        }

        reconnecting = false;
    }

    public void Dispose()
    {
        session = null;
    }
}
