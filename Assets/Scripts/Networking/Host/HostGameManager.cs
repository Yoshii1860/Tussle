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


public class HostGameManager
{
    private Allocation allocation;
    private string joinCode;
    private string lobbyId;

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

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(
                "MyLobby", MaxConnections, lobbyOptions);
            lobbyId = lobby.Id;

            _ = HeartbeatLobbyAsync(15); // Start the heartbeat asynchronously
        }
        catch(LobbyServiceException lobbyException)
        {
            Debug.LogError(lobbyException.Message);
            return;
        }

        NetworkManager.Singleton.StartHost();
        Debug.Log($"HostGameManager: Host started with join code: {joinCode}");

        NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
    }

    private async Task HeartbeatLobbyAsync(float waitTimeSeconds)
    {
        while (true)
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
}
