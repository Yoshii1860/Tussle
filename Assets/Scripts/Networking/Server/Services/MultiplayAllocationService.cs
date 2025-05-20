// --------------------------------------------
// --------------------------------------------
// Trying to add dedicated server mode online 
// --------------------------------------------
// --------------------------------------------


#if UNITY_SERVER
using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;

public class MultiplayAllocationService : IDisposable
{
    private IMultiplayService multiplayService;
    private MultiplayEventCallbacks serverCallbacks;
    private IServerQueryHandler serverCheckManager;
    private IServerEvents serverEvents;
    private CancellationTokenSource serverCheckCancel;
    private string allocationId;

    public MultiplayAllocationService()
    {
        try
        {
            multiplayService = MultiplayService.Instance;
            serverCheckCancel = new CancellationTokenSource();
            Debug.Log("MultiplayAllocationService: Initialized successfully");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error creating Multiplay allocation service: {ex.Message}");
            multiplayService = null;
        }
    }

    public async Task<MatchmakingResults> SubscribeAndAwaitMatchmakerAllocation()
    {
        if (multiplayService == null) 
        {
            Debug.LogError("MultiplayAllocationService: Multiplay service is null, cannot subscribe to allocation");
            return null;
        }

        allocationId = null;
        serverCallbacks = new MultiplayEventCallbacks();
        serverCallbacks.Allocate += OnMultiplayAllocation;
        serverCallbacks.Deallocate += OnMultiplayDeAllocation;
        serverCallbacks.Error += OnMultiplayError;

        try
        {
            serverEvents = await multiplayService.SubscribeToServerEventsAsync(serverCallbacks);
            Debug.Log("MultiplayAllocationService: Subscribed to server events");
        }
        catch (Exception e)
        {
            Debug.LogError($"MultiplayAllocationService: Failed to subscribe to server events: {e.Message}");
            return null;
        }

        string allocationID = await AwaitAllocationID();
        if (string.IsNullOrEmpty(allocationID))
        {
            Debug.LogError("MultiplayAllocationService: Failed to get allocation ID");
            return null;
        }

        MatchmakingResults matchmakingPayload = await GetMatchmakerAllocationPayloadAsync();
        if (matchmakingPayload == null)
        {
            Debug.LogError("MultiplayAllocationService: Failed to get matchmaking payload");
        }

        return matchmakingPayload;
    }

    private async Task<string> AwaitAllocationID()
    {
        ServerConfig config = multiplayService.ServerConfig;
        Debug.Log($"Awaiting Allocation. Server Config is:\n" +
            $"-ServerID: {config.ServerId}\n" +
            $"-AllocationID: {config.AllocationId}\n" +
            $"-Port: {config.Port}\n" +
            $"-QPort: {config.QueryPort}\n" +
            $"-logs: {config.ServerLogDirectory}");

        while (string.IsNullOrEmpty(allocationId))
        {
            string configID = config.AllocationId;
            if (!string.IsNullOrEmpty(configID) && string.IsNullOrEmpty(allocationId))
            {
                Debug.Log($"Config had AllocationID: {configID}");
                allocationId = configID;
            }
            await Task.Delay(100);
        }
        return allocationId;
    }

    private async Task<MatchmakingResults> GetMatchmakerAllocationPayloadAsync()
    {
        try
        {
            MatchmakingResults payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();
            string modelAsJson = JsonConvert.SerializeObject(payloadAllocation, Formatting.Indented);
            Debug.Log($"{nameof(GetMatchmakerAllocationPayloadAsync)}:\n{modelAsJson}");
            return payloadAllocation;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get matchmaking payload: {e.Message}");
            return null;
        }
    }

    private void OnMultiplayAllocation(MultiplayAllocation allocation)
    {
        Debug.Log($"OnAllocation: {allocation.AllocationId}");
        if (string.IsNullOrEmpty(allocation.AllocationId)) { return; }
        allocationId = allocation.AllocationId;
    }

    public async Task BeginServerCheck()
    {
        if (multiplayService == null) 
        {
            Debug.LogError("MultiplayAllocationService: Cannot begin server check, multiplay service is null");
            return;
        }

        try
        {
            serverCheckManager = await multiplayService.StartServerQueryHandlerAsync((ushort)20, "", "", "0", "");
            Debug.Log("MultiplayAllocationService: Server query handler started");
            ServerCheckLoop(serverCheckCancel.Token);
        }
        catch (Exception e)
        {
            Debug.LogError($"MultiplayAllocationService: Failed to start server query handler: {e.Message}");
        }
    }

    public void SetServerName(string name) => serverCheckManager.ServerName = name;
    public void SetBuildID(string id) => serverCheckManager.BuildId = id;
    public void SetMaxPlayers(ushort players) => serverCheckManager.MaxPlayers = players;
    public void AddPlayer() => serverCheckManager.CurrentPlayers++;
    public void RemovePlayer() => serverCheckManager.CurrentPlayers--;
    public void SetMap(string newMap) => serverCheckManager.Map = newMap;
    public void SetMode(string mode) => serverCheckManager.GameType = mode;

    private async void ServerCheckLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            serverCheckManager.UpdateServerCheck();
            await Task.Delay(100);
        }
    }

    private void OnMultiplayDeAllocation(MultiplayDeallocation deallocation)
    {
        Debug.Log($"Multiplay Deallocated: ID: {deallocation.AllocationId}\nEvent: {deallocation.EventId}\nServer: {deallocation.ServerId}");
    }

    private void OnMultiplayError(MultiplayError error)
    {
        Debug.Log($"MultiplayError: {error.Reason}\n{error.Detail}");
    }

    public void Dispose()
    {
        if (serverCallbacks != null)
        {
            serverCallbacks.Allocate -= OnMultiplayAllocation;
            serverCallbacks.Deallocate -= OnMultiplayDeAllocation;
            serverCallbacks.Error -= OnMultiplayError;
        }
        if (serverCheckCancel != null)
        {
            serverCheckCancel.Cancel();
        }
        serverEvents?.UnsubscribeAsync();
    }
}
#endif


/*
using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using UnityEngine;

public class MultiplayAllocationService : IDisposable
{
    private IMultiplayService multiplayService;
    private MultiplayEventCallbacks serverCallbacks;
    private IServerQueryHandler serverCheckManager;
    private IServerEvents serverEvents;
    private CancellationTokenSource serverCheckCancel;
    string allocationId;

    public MultiplayAllocationService()
    {
        try
        {
            multiplayService = MultiplayService.Instance;
            serverCheckCancel = new CancellationTokenSource();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Error creating Multiplay allocation service.\n{ex}");
        }
    }

    public async Task<MatchmakingResults> SubscribeAndAwaitMatchmakerAllocation()
    {
        if (multiplayService == null) { return null; }

        allocationId = null;
        serverCallbacks = new MultiplayEventCallbacks();
        serverCallbacks.Allocate += OnMultiplayAllocation;
        serverEvents = await multiplayService.SubscribeToServerEventsAsync(serverCallbacks);

        string allocationID = await AwaitAllocationID();
        MatchmakingResults matchmakingPayload = await GetMatchmakerAllocationPayloadAsync();

        return matchmakingPayload;
    }

    private async Task<string> AwaitAllocationID()
    {
        ServerConfig config = multiplayService.ServerConfig;
        Debug.Log($"Awaiting Allocation. Server Config is:\n" +
            $"-ServerID: {config.ServerId}\n" +
            $"-AllocationID: {config.AllocationId}\n" +
            $"-Port: {config.Port}\n" +
            $"-QPort: {config.QueryPort}\n" +
            $"-logs: {config.ServerLogDirectory}");

        while (string.IsNullOrEmpty(allocationId))
        {
            string configID = config.AllocationId;

            if (!string.IsNullOrEmpty(configID) && string.IsNullOrEmpty(allocationId))
            {
                Debug.Log($"Config had AllocationID: {configID}");
                allocationId = configID;
            }

            await Task.Delay(100);
        }

        return allocationId;
    }

    private async Task<MatchmakingResults> GetMatchmakerAllocationPayloadAsync()
    {
        MatchmakingResults payloadAllocation = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();
        string modelAsJson = JsonConvert.SerializeObject(payloadAllocation, Formatting.Indented);
        Debug.Log(nameof(GetMatchmakerAllocationPayloadAsync) + ":" + Environment.NewLine + modelAsJson);
        return payloadAllocation;
    }

    private void OnMultiplayAllocation(MultiplayAllocation allocation)
    {
        Debug.Log($"OnAllocation: {allocation.AllocationId}");

        if (string.IsNullOrEmpty(allocation.AllocationId)) { return; }

        allocationId = allocation.AllocationId;
    }

    public async Task BeginServerCheck()
    {
        if (multiplayService == null) { return; }

        serverCheckManager = await multiplayService.StartServerQueryHandlerAsync((ushort)20, "", "", "0", "");

        ServerCheckLoop(serverCheckCancel.Token);
    }

    public void SetServerName(string name)
    {
        serverCheckManager.ServerName = name;
    }
    public void SetBuildID(string id)
    {
        serverCheckManager.BuildId = id;
    }

    public void SetMaxPlayers(ushort players)
    {
        serverCheckManager.MaxPlayers = players;
    }

    public void AddPlayer()
    {
        serverCheckManager.CurrentPlayers++;
    }

    public void RemovePlayer()
    {
        serverCheckManager.CurrentPlayers--;
    }

    public void SetMap(string newMap)
    {
        serverCheckManager.Map = newMap;
    }

    public void SetMode(string mode)
    {
        serverCheckManager.GameType = mode;
    }

    private async void ServerCheckLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            serverCheckManager.UpdateServerCheck();
            await Task.Delay(100);
        }
    }

    private void OnMultiplayDeAllocation(MultiplayDeallocation deallocation)
    {
        Debug.Log(
                $"Multiplay Deallocated : ID: {deallocation.AllocationId}\nEvent: {deallocation.EventId}\nServer{deallocation.ServerId}");
    }

    private void OnMultiplayError(MultiplayError error)
    {
        Debug.Log($"MultiplayError : {error.Reason}\n{error.Detail}");
    }

    public void Dispose()
    {
        if (serverCallbacks != null)
        {
            serverCallbacks.Allocate -= OnMultiplayAllocation;
            serverCallbacks.Deallocate -= OnMultiplayDeAllocation;
            serverCallbacks.Error -= OnMultiplayError;
        }

        if (serverCheckCancel != null)
        {
            serverCheckCancel.Cancel();
        }

        serverEvents?.UnsubscribeAsync();
    }
}
*/