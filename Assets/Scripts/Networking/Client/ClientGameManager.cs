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
        // Initialize the client game manager here
        Debug.Log("ClientGameManager: Client Game Manager Initialized");

        await UnityServices.InitializeAsync();

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
        // Logic to transition to the main menu
        Debug.Log("ClientGameManager: Transitioning to the main menu...");

        SceneManager.LoadScene(MenuSceneName);
    }

    internal async Task StartClientAsync(string joinCode)
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

    public void Dispose()
    {
        // Dispose of any resources if necessary
        Debug.Log("ClientGameManager: Disposing resources.");
        networkClient?.Dispose();
    }
}