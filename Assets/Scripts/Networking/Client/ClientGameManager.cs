// --------------------------------------------
// --------------------------------------------
// Trying to add dedicated server mode online 
// --------------------------------------------
// --------------------------------------------

using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

public class ClientGameManager : IDisposable
{
    private JoinAllocation allocation;
    private NetworkClient networkClient;
    private UserData userData;
    private const string MenuSceneName = "MainMenu";
    private const string GameSceneName = "Game";
    private TaskCompletionSource<bool> connectionTask;

    public async Task<bool> InitAsync()
    {
        Debug.Log("ClientGameManager: Client Game Manager Initialized");
        networkClient = new NetworkClient(NetworkManager.Singleton);
        AuthState authState = await AuthenticationHandler.AuthenticateAsync(5);
        if (authState == AuthState.Authenticated)
        {
            Debug.Log("ClientGameManager: Client authenticated successfully.");
            userData = new UserData
            {
                userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "MissingName"),
                userAuthId = AuthenticationService.Instance.PlayerId,
                characterId = PlayerPrefs.GetInt("SelectedCharacterId", 0)
            };
/*
            var config = NetworkManager.Singleton.NetworkConfig;

            string protocolVersion = config.ProtocolVersion.ToString();
            ushort tickRate = (ushort)config.TickRate;
            string networkTransport = config.NetworkTransport?.GetType().Name ?? "None";

            var prefabNames = new System.Text.StringBuilder();
            foreach (var prefab in config.Prefabs.Prefabs)
            {
                prefabNames.AppendLine(prefab.Prefab.name);
            }

            Debug.Log($"NetworkConfig Client: ProtocolVersion={protocolVersion}, TickRate={tickRate}, NetworkTransport={networkTransport}, Prefabs=[{prefabNames}]");
*/
            return true;
        }
        else
        {
            Debug.LogError("ClientGameManager: Client authentication failed.");
            return false;
        }
    }

    public void GoToMenu()
    {
        Debug.Log("ClientGameManager: Transitioning to the main menu...");
        SceneManager.LoadScene(MenuSceneName);
    }

    public void StartClient(string ip, int port)
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, (ushort)port);


        ConnectClient();
    }

    public async Task StartClientAsync(string joinCode)
    {
        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log($"ClientGameManager: Successfully joined relay with join code: {joinCode}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"ClientGameManager: Failed to join relay. Exception: {ex.Message}");
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        RelayServerData relayServerData = allocation.ToRelayServerData("dtls");
        transport.SetRelayServerData(relayServerData);

        ConnectClient();
    }

    private void ConnectClient()
    {
        connectionTask = new TaskCompletionSource<bool>();
        string payload = JsonConvert.SerializeObject(userData);
        byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        bool hasClientStarted = NetworkManager.Singleton.StartClient();
        if (!hasClientStarted)
        {
            Debug.LogError("ClientGameManager: Failed to start client.");
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            connectionTask.SetResult(false);
            return;
        }
        Debug.Log("ClientGameManager: Client started successfully.");
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"ClientGameManager: Client connected with ID: {clientId}");
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;

        if (NetworkManager.Singleton.IsClient && SceneManager.GetActiveScene().name != GameSceneName)
        {
            Debug.Log("ClientGameManager: Loading Game scene...");
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
        }
        connectionTask?.TrySetResult(true);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"ClientGameManager: Client disconnected with ID: {clientId}");
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        connectionTask?.TrySetResult(false);
    }

    public async Task StartClientLocalAsync(string ip, int port)
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, (ushort)port);

        UserData userData = new UserData
        {
            userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "MissingName"),
            userAuthId = AuthenticationService.Instance.PlayerId,
            characterId = PlayerPrefs.GetInt("SelectedCharacterId", 0)
        };

        ConnectClient();

        await Task.CompletedTask;
    }

    public async void MatchmakeAsync(Action<MatchmakerPollingResult> onMatchmakeResponse)
    {
        Debug.Log("ClientGameManager: Starting matchmake...");
        if (MatchplayMatchmaker.Instance.IsMatchmaking)
        {
            Debug.Log("ClientGameManager: Already matchmaking, skipping...");
            return;
        }

        MatchmakerPollingResult matchResult = await GetMatchAsync();
        onMatchmakeResponse?.Invoke(matchResult);
    }

    private async Task<MatchmakerPollingResult> GetMatchAsync()
    {
        MatchmakingResult matchmakingResult = await MatchplayMatchmaker.Instance.Matchmake(userData);

        if (matchmakingResult.result == MatchmakerPollingResult.Success)
        {
            StartClient(matchmakingResult.ip, matchmakingResult.port);
            Debug.Log($"ClientGameManager: Successfully connected to server at {matchmakingResult.ip}:{matchmakingResult.port}");

            bool connected = await connectionTask.Task.WithTimeout(TimeSpan.FromSeconds(15));
            if (connected)
            {
                Debug.Log("ClientGameManager: Successfully connected to the server.");
                return MatchmakerPollingResult.Success;
            }
            else
            {
                Debug.LogError("ClientGameManager: Failed to connect to the server within the timeout period.");
                return MatchmakerPollingResult.MatchAssignmentError;
            }
        }

        return matchmakingResult.result;
    }

    public async Task CancelMatchmakingAsync()
    {
        await MatchplayMatchmaker.Instance.CancelMatchmaking();
    }

    public void Disconnect()
    {
        networkClient.Disconnect();
    }

    public void Dispose()
    {
        Debug.Log("ClientGameManager: Disposing resources.");
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        connectionTask?.TrySetCanceled();
        networkClient?.Dispose();
    }
}

public static class TaskExtensions
{
    public static async Task<bool> WithTimeout(this Task task, TimeSpan timeout)
    {
        var delayTask = Task.Delay(timeout);
        var completedTask = await Task.WhenAny(task, delayTask);
        return completedTask == task;
    }
}

/*
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientGameManager : IDisposable
{
    private JoinAllocation allocation;

    private NetworkClient networkClient;
    private const string MenuSceneName = "MainMenu";

    public async Task<bool> InitAsync()
    {
        Debug.Log("ClientGameManager: Client Game Manager Initialized");

        networkClient = new NetworkClient(NetworkManager.Singleton);

        AuthState authState = await AuthenticationHandler.AuthenticateAsync(5);

        if (authState == AuthState.Authenticated)
        {
            Debug.Log("ClientGameManager: Client authenticated successfully.");
            return true;
        }
        else
        {
            Debug.LogError("ClientGameManager: Client authentication failed.");
            return false;
        }
    }

    public void GoToMenu()
    {
        Debug.Log("ClientGameManager: Transitioning to the main menu...");
        SceneManager.LoadScene(MenuSceneName);
    }

    public async Task StartClientAsync(string joinCode)
    {
        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log($"ClientGameManager: Successfully joined relay with join code: {joinCode}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"ClientGameManager: Failed to join relay. Exception: {ex.Message}");
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        RelayServerData relayServerData = allocation.ToRelayServerData("dtls");
        transport.SetRelayServerData(relayServerData);

        UserData userData = new UserData
        {
            userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "MissingName"),
            userAuthId = AuthenticationService.Instance.PlayerId,
            characterId = PlayerPrefs.GetInt("SelectedCharacterId", 0)
        };

        string payload = JsonUtility.ToJson(userData);
        byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

        NetworkManager.Singleton.StartClient();
        Debug.Log("ClientGameManager: Client started successfully.");
    }

    public async Task StartClientLocalAsync(string ip, int port)
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, (ushort)port);

        UserData userData = new UserData
        {
            userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "MissingName"),
            userAuthId = AuthenticationService.Instance.PlayerId,
            characterId = PlayerPrefs.GetInt("SelectedCharacterId", 0)
        };

        string payload = JsonUtility.ToJson(userData);
        byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

        bool success = NetworkManager.Singleton.StartClient();
        if (success)
        {
            Debug.Log($"ClientGameManager: Client started successfully, connecting to {ip}:{port}");
        }
        else
        {
            Debug.LogError($"ClientGameManager: Failed to start client, connecting to {ip}:{port}");
        }

        await Task.CompletedTask;
    }

    public void Disconnect()
    {
        networkClient.Disconnect();
    }

    public void Dispose()
    {
        Debug.Log("ClientGameManager: Disposing resources.");
        networkClient?.Dispose();
    }
}

*/