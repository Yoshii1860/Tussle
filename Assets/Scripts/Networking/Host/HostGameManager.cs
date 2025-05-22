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

    private CancellationTokenSource heartbeatCancellationTokenSource;

    private const int MaxConnections = 20;
    private const string GameSceneName = "Game";
    public async Task StartHostAsync()
    {
        try
        {
            allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
        }
        catch (Exception ex)
        {
            Debug.LogError($"HostGameManager: Failed to create allocation. Exception: {ex.Message}");
            return;
        }

        try
        {
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"HostGameManager: Join code: {joinCode}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"HostGameManager: Failed to get join code. Exception: {ex.Message}");
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        
        RelayServerData relayServerData = allocation.ToRelayServerData("dtls");
        transport.SetRelayServerData(relayServerData);

        try
        {
            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions();
            lobbyOptions.IsPrivate = false; // Set to true for private lobby
            lobbyOptions.Data = new Dictionary<string, DataObject>()
            {
                { 
                    "JoinCode", new DataObject(
                        visibility: DataObject.VisibilityOptions.Public, 
                        value: joinCode
                    ) 
                }
            };

            string playerName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Unknown");

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(
                $"{playerName}'s Lobby", MaxConnections, lobbyOptions);
            lobbyId = lobby.Id;

            heartbeatCancellationTokenSource = new CancellationTokenSource();
            _ = HeartbeatLobbyAsync(15, heartbeatCancellationTokenSource.Token); // Start the heartbeat asynchronously
        }
        catch(LobbyServiceException lobbyException)
        {
            Debug.LogError(lobbyException.Message);
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
        Debug.Log($"HostGameManager: Host started with join code: {joinCode}");

        NetworkServer.OnClientLeft += HandleClientLeft;

        NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
    }

    private async Task HeartbeatLobbyAsync(float waitTimeSeconds, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId); // Send the heartbeat asynchronously
            }
            catch (Exception ex)
            {
                Debug.LogError($"HostGameManager: Failed to send heartbeat. Exception: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(waitTimeSeconds)); // Wait asynchronously
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
            Debug.Log($"HostGameManager: Lobby {lobbyId} is deleting.");
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"HostGameManager: Failed to delete lobby. Exception: {ex.Message}");
        }

        lobbyId = string.Empty;

        NetworkServer.OnClientLeft -= HandleClientLeft;

        NetworkServer?.Dispose();
    }

    private async void HandleClientLeft(string authId)
    {
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(lobbyId, authId);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"HostGameManager: Failed to handle client left. Exception: {ex.Message}");
        }
    }
}