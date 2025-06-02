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
    public UserData UserData { get; private set; }
    private const string MenuSceneName = "MainMenu";
    private const string GameSceneName = "Game";
    private TaskCompletionSource<bool> connectionTask;

    public async Task<bool> InitAsync()
    {
        networkClient = new NetworkClient(NetworkManager.Singleton);
        AuthState authState = await AuthenticationHandler.AuthenticateAsync(5);
        if (authState == AuthState.Authenticated)
        {
            UserData = new UserData
            {
                userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "MissingName"),
                userAuthId = AuthenticationService.Instance.PlayerId,
                teamIndex = -1,
                characterId = PlayerPrefs.GetInt("SelectedCharacterId", 0)
            };
            Debug.Log("ClientGameManager: Authentication succeeded.");
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
        string payload = JsonConvert.SerializeObject(UserData);
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
        Debug.Log("ClientGameManager: Client started, awaiting connection...");
    }

    private void OnClientConnected(ulong clientId)
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;

        if (NetworkManager.Singleton.IsClient && SceneManager.GetActiveScene().name != GameSceneName)
        {
            Debug.Log("ClientGameManager: Connected, loading Game scene...");
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
        }
        connectionTask?.TrySetResult(true);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        Debug.Log($"ClientGameManager: Client disconnected with ID: {clientId}");
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
            teamIndex = -1,
            characterId = PlayerPrefs.GetInt("SelectedCharacterId", 0)
        };

        ConnectClient();

        await Task.CompletedTask;
    }

    public async void MatchmakeAsync(bool isTeamQueue, Action<MatchmakerPollingResult> onMatchmakeResponse)
    {
        if (MatchplayMatchmaker.Instance.IsMatchmaking)
        {
            Debug.Log("ClientGameManager: Already matchmaking, skipping request.");
            return;
        }

        UserData.userGamePreferences.gameQueue = isTeamQueue ? GameQueue.Team : GameQueue.Solo;
        UserData.teamIndex = -1;
        Debug.Log($"ClientGameManager: Starting matchmaking in mode: {UserData.userGamePreferences.gameQueue}");
        MatchmakerPollingResult matchResult = await GetMatchAsync();
        onMatchmakeResponse?.Invoke(matchResult);
    }

    private async Task<MatchmakerPollingResult> GetMatchAsync()
    {
        MatchmakingResult matchmakingResult = await MatchplayMatchmaker.Instance.Matchmake(UserData);

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

        Debug.LogWarning($"ClientGameManager: Matchmaking failed with result: {matchmakingResult.resultMessage}");
        return matchmakingResult.result;
    }

    public async Task CancelMatchmakingAsync()
    {
        Debug.Log("ClientGameManager: Cancelling matchmaking...");
        await MatchplayMatchmaker.Instance.CancelMatchmaking();
    }

    public void SetCharacterId(int characterId)
    {
        if (UserData != null)
        {
            UserData.characterId = characterId;
            Debug.Log($"ClientGameManager: Character ID set to {characterId}");
        }
        else
        {
            Debug.LogError("ClientGameManager: UserData is null, cannot set character ID.");
        }
    }

    public void Disconnect()
    {
        networkClient.Disconnect();
    }

    public void Dispose()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        connectionTask?.TrySetCanceled();
        networkClient?.Dispose();
        Debug.Log("ClientGameManager: Disposed.");
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