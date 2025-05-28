#if UNITY_SERVER
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Multiplay;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using Unity.Services.Matchmaker.Models;
using Newtonsoft.Json;

public class ServerGameManager : IDisposable
{
    private string serverIP;
    private int serverPort;
    private int queryPort;
    private MultiplayAllocationService multiplayAllocationService;
    private MatchplayBackfiller matchplayBackfiller;
    private const string GameSceneName = "Game";
    private bool isLocalTest = true;

    private Dictionary<string, int> teamIdToTeamIndex = new Dictionary<string, int>();

    public NetworkServer NetworkServer { get; private set; }

    public ServerGameManager(string serverIP, int serverPort, int queryPort, NetworkManager manager)
    {
        this.serverIP = serverIP;
        this.serverPort = serverPort;
        this.queryPort = queryPort;
        NetworkServer = new NetworkServer(manager);
        Debug.Log("ServerGameManager: NetworkServer created");
        isLocalTest = !Application.isBatchMode || serverIP == "127.0.0.1" || serverIP == "172.27.205.68";
        multiplayAllocationService = new MultiplayAllocationService();
    }

    public async Task StartGameServerAsync()
    {
        Debug.Log("ServerGameManager: Starting game server...");

        if (!NetworkServer.OpenConnection(ApplicationData.IP(), serverPort))
        {
            Debug.LogError("ServerGameManager: Failed to open connection.");
            return;
        }

        Debug.Log("ServerGameManager: Loading Game scene...");
        NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);

        while (SceneManager.GetActiveScene().name != GameSceneName)
        {
            await Task.Delay(100);
        }

        Debug.Log("ServerGameManager: Game scene loaded. Waiting for clients...");

        try
        {
            await multiplayAllocationService.BeginServerCheck();
            Debug.Log("ServerGameManager: Server query handler started.");

            var payloadTask = multiplayAllocationService.SubscribeAndAwaitMatchmakerAllocation();
            if (await Task.WhenAny(payloadTask, Task.Delay(30000)) != payloadTask)
            {
                Debug.LogWarning("ServerGameManager: Timed out waiting for matchmaker payload. Shutting down.");
                Dispose();
                Application.Quit();
                return;
            }

            MatchmakingResults rawPayload = payloadTask.Result;

            if (rawPayload != null)
            {
                Debug.Log("ServerGameManager: Matchmaker payload received. Starting backfill and registering user events.");
                await StartBackfill(rawPayload);
                NetworkServer.OnUserJoined += UserJoined;
                NetworkServer.OnUserLeft += UserLeft;
            }
            else
            {
                Debug.LogWarning("ServerGameManager: No matchmaker payload received. Shutting down.");
                Dispose();
                Application.Quit();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"ServerGameManager: MultiplayAllocationService initialization failed: {e.Message}. Continuing without Multiplay.");
            multiplayAllocationService?.Dispose();
            multiplayAllocationService = null;
        }
    }

    private async Task StartBackfill(MatchmakingResults payload)
    {
        matchplayBackfiller = new MatchplayBackfiller($"{serverIP}:{serverPort}", 
            payload.QueueName, 
            payload.MatchProperties, 
            20);

        if (matchplayBackfiller.NeedsPlayers())
        {
            await matchplayBackfiller.BeginBackfilling();
            Debug.Log("ServerGameManager: Backfill started.");
        }
    }

    private void UserJoined(UserData user)
    {
        Team team = matchplayBackfiller.GetTeamByUserId(user.userAuthId);
        if (!teamIdToTeamIndex.TryGetValue(team.TeamId, out int teamIndex))
        {
            teamIndex = teamIdToTeamIndex.Count;
            teamIdToTeamIndex.Add(team.TeamId, teamIndex);
        }
        user.teamIndex = teamIndex;

        multiplayAllocationService?.AddPlayer();

        if (!matchplayBackfiller.NeedsPlayers() && matchplayBackfiller.IsBackfilling)
        {
            _ = matchplayBackfiller.StopBackfill();
            Debug.Log("ServerGameManager: Match full, stopped backfill.");
        }
    }

    private async void UserLeft(UserData user)
    {
        int playerCount = await matchplayBackfiller.RemovePlayerFromMatch(user.userAuthId);
        multiplayAllocationService?.RemovePlayer();

        if (playerCount <= 0)
        {
            Debug.Log("ServerGameManager: All players have left. Closing server.");
            CloseServer();
            return;
        }

        if (matchplayBackfiller.NeedsPlayers() && !matchplayBackfiller.IsBackfilling)
        {
            Debug.Log("ServerGameManager: Players needed after user left, restarting backfill.");
            _ = matchplayBackfiller.BeginBackfilling();
        }
    }   
        
    private async void CloseServer()
    {
        if (matchplayBackfiller != null && matchplayBackfiller.IsBackfilling)
        {
            Debug.Log("ServerGameManager: Stopping backfill before closing server.");
            await matchplayBackfiller.StopBackfill();
        }
        Debug.Log("ServerGameManager: Disposing and shutting down application.");
        Dispose();
        Application.Quit();
    }

    public void Dispose()
    {
        NetworkServer.OnUserJoined -= UserJoined;
        NetworkServer.OnUserLeft -= UserLeft;

        matchplayBackfiller?.Dispose();
        multiplayAllocationService?.Dispose();
        NetworkServer?.Dispose();
        Debug.Log("ServerGameManager: Disposed resources.");
    }
}
#endif