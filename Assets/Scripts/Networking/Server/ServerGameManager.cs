// --------------------------------------------
// --------------------------------------------
// Trying to add dedicated server mode online 
// --------------------------------------------
// --------------------------------------------

#if UNITY_SERVER
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Multiplay;
using UnityEngine.SceneManagement;
using System;

public class ServerGameManager : IDisposable
{
    private string serverIP;
    private int serverPort;
    private int queryPort;
    private MultiplayAllocationService multiplayAllocationService;
    private MatchplayBackfiller matchplayBackfiller;
    private const string GameSceneName = "Game";
    private bool isLocalTest = true;

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
            Debug.LogError("Failed to open connection.");
            return;
        }

        Debug.Log("ServerGameManager: Loading Game scene on server startup");
        NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);

        while (SceneManager.GetActiveScene().name != GameSceneName)
        {
            await Task.Delay(100);
        }

        Debug.Log("ServerGameManager: Game scene loaded, waiting for clients");

        try
        {
            await multiplayAllocationService.BeginServerCheck();
            Debug.Log("ServerGameManager: Server check started");

            MatchmakingResults matchmakerPayload = await multiplayAllocationService.SubscribeAndAwaitMatchmakerAllocation();
            Debug.Log("ServerGameManager: Multiplay allocation service initialized");

            if (matchmakerPayload != null)
            {
                Debug.Log($"ServerGameManager: Matchmaker payload received: {JsonUtility.ToJson(matchmakerPayload)}");
                await StartBackfill(matchmakerPayload);
                NetworkServer.OnUserJoined += UserJoined;
                NetworkServer.OnUserLeft += UserLeft;
            }
            else
            {
                Debug.LogWarning("ServerGameManager: No matchmaker payload received.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to initialize MultiplayAllocationService: {e.Message}. Continuing without Multiplay.");
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
        }
    }

    private void UserJoined(UserData user)
    {
        matchplayBackfiller.AddPlayerToMatch(user);
        multiplayAllocationService?.AddPlayer();

        if (!matchplayBackfiller.NeedsPlayers() && matchplayBackfiller.IsBackfilling)
        {
            _ = matchplayBackfiller.StopBackfill();
        }
    }

    private void UserLeft(UserData user)
    {
        int playerCount = matchplayBackfiller.RemovePlayerFromMatch(user.userAuthId);
        multiplayAllocationService?.RemovePlayer();
        if (playerCount <= 0)
        {
            Debug.Log("ServerGameManager: No players left, closing server.");
            CloseServer();
            return;
        }

        if (matchplayBackfiller.NeedsPlayers() && !matchplayBackfiller.IsBackfilling)
        {
            _ = matchplayBackfiller.BeginBackfilling();
        }
    }   
    
    private async void CloseServer()
    {
        await matchplayBackfiller.StopBackfill();
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
    }
}
#endif


/*
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Multiplay;
using UnityEngine.SceneManagement;

public class ServerGameManager : System.IDisposable
{
    private string serverIP;
    private int serverPort;
    private int queryPort;
    private NetworkServer networkServer;
    private MultiplayAllocationService multiplayAllocationService;

    private const string GameSceneName = "Game";

    private bool isLocalTest = true;

    public ServerGameManager(string serverIP, int serverPort, int queryPort, NetworkManager manager)
    {
        this.serverIP = serverIP;
        this.serverPort = serverPort;
        this.queryPort = queryPort;
        networkServer = new NetworkServer(manager);
        Debug.Log("ServerGameManager: NetworkServer created");
        isLocalTest = !Application.isBatchMode || serverIP == "127.0.0.1" || serverIP == "172.27.205.68";
    }

    public async Task StartGameServerAsync()
    {
        if (!isLocalTest)
        {
            try
            {
                multiplayAllocationService = new MultiplayAllocationService();
                // await multiplayAllocationService.BeginServerCheck();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to initialize MultiplayAllocationService: {e.Message}. Continuing without Multiplay for local testing.");
                multiplayAllocationService?.Dispose();
                multiplayAllocationService = null;
            }
        }
        else
        {
            Debug.Log("Local test detected. Skipping MultiplayAllocationService.");
        }

        if (!networkServer.OpenConnection(ApplicationData.IP(), serverPort))
        {
            Debug.LogError("Failed to open connection.");
            return;
        }

        Debug.Log("ServerGameManager: Loading Game scene on server startup");
        NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);

        // Wait for the Game scene to load
        while (SceneManager.GetActiveScene().name != GameSceneName)
        {
            await Task.Delay(100);
        }

        Debug.Log("ServerGameManager: Game scene loaded, waiting for clients");
        while (NetworkManager.Singleton.ConnectedClients.Count == 0)
        {
            await Task.Delay(1000);
        }

        Debug.Log("ServerGameManager: Client connected, server ready");
    }

    public void Dispose()
    {
        multiplayAllocationService?.Dispose();
        networkServer?.Dispose();
    }
}
*/