using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Relay;
using System;
using Unity.Services.Relay.Models;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Collections;
using Unity.Services.Authentication;
using System.Threading;


public class HostGameManager : IDisposable
{
    private Allocation allocation;
    private string joinCode;
    private string lobbyId;
    public NetworkServer NetworkServer { get; private set; }
    public bool IsPrivateServer { get; private set; }

    private CancellationTokenSource heartbeatCancellationTokenSource;

    private const int MaxConnections = 20;
    private const string GameSceneName = "Game";
    private const string MenuSceneName = "MainMenu";

    public async Task StartHostAsync(bool isPrivateServer)
    {
        IsPrivateServer = isPrivateServer;
        
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
        }
        catch (Exception ex)
        {
            Debug.LogError($"HostGameManager: Relay allocation failed: {ex.Message}");
            return;
        }

        try
        {
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"HostGameManager: Join code acquired: {joinCode}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"HostGameManager: Failed to get join code: {ex.Message}");
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        RelayServerData relayServerData = allocation.ToRelayServerData("dtls");
        transport.SetRelayServerData(relayServerData);

        try
        {
            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivateServer,
                Data = new Dictionary<string, DataObject>
                {
                    { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                }
            };

            string playerName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Unknown");
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync($"{playerName}'s Lobby", MaxConnections, lobbyOptions);
            lobbyId = lobby.Id;
            Debug.Log($"HostGameManager: Lobby created with ID: {lobbyId}");

            heartbeatCancellationTokenSource = new CancellationTokenSource();
            _ = HeartbeatLobbyAsync(15, heartbeatCancellationTokenSource.Token);
        }
        catch (LobbyServiceException lobbyException)
        {
            Debug.LogError($"HostGameManager: Lobby creation failed: {lobbyException.Message}");
            return;
        }

        NetworkServer = new NetworkServer(NetworkManager.Singleton);

        UserData userData = new UserData
        {
            userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "MissingName"),
            userAuthId = AuthenticationService.Instance.PlayerId,
            characterId = PlayerPrefs.GetInt("SelectedCharacterId", 0)
        };

        string payload = JsonUtility.ToJson(userData);
        byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(payload);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

        NetworkManager.Singleton.StartHost();
        Debug.Log($"HostGameManager: Host started. Join code: {joinCode}");

        NetworkServer.OnClientLeft += HandleClientLeft;

        NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
    }

    public string GetJoinCode()
    {
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogWarning("HostGameManager: Join code is not available.");
            return string.Empty;
        }
        return joinCode;
    }

    private async Task HeartbeatLobbyAsync(float waitTimeSeconds, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"HostGameManager: Heartbeat failed: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(waitTimeSeconds));
        }
    }

    public void Dispose()
    {
        Shutdown();
    }

    public async void Shutdown()
    {
        if (string.IsNullOrEmpty(lobbyId)) { return; }

        if (heartbeatCancellationTokenSource != null)
        {
            heartbeatCancellationTokenSource.Cancel();
            heartbeatCancellationTokenSource.Dispose();
            heartbeatCancellationTokenSource = null;
        }

        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
            Debug.Log($"HostGameManager: Lobby deleted: {lobbyId}");
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogWarning($"HostGameManager: Failed to delete lobby: {ex.Message}");
        }

        lobbyId = string.Empty;

        NetworkServer.OnClientLeft -= HandleClientLeft;
        NetworkServer?.Dispose();

        SceneManager.LoadScene(MenuSceneName, LoadSceneMode.Single);
        Debug.Log("HostGameManager: Returned to Main Menu scene.");
    }

    private async void HandleClientLeft(string authId)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(lobbyId, authId);
            Debug.Log($"HostGameManager: Removed player {authId} from lobby.");
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogWarning($"HostGameManager: Failed to remove player {authId}: {ex.Message}");
        }
    }
}