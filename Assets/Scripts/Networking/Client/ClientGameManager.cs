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