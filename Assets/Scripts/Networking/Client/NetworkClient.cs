using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkClient : IDisposable
{
    private NetworkManager networkManager;

    private const string MenuSceneName = "MainMenu";

    public NetworkClient(NetworkManager networkManager)
    {
        this.networkManager = networkManager;
        networkManager.OnClientDisconnectCallback += OnClientDisconnect;
        Debug.Log("NetworkClient: Subscribed to OnClientDisconnectCallback.");
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (clientId != 0 && clientId != networkManager.LocalClientId) { return; }

        Debug.LogWarning($"NetworkClient: Disconnected from server (ClientID: {clientId}). Initiating disconnect sequence.");
        Disconnect();
    }

    public void Disconnect()
    {
        if (SceneManager.GetActiveScene().name != MenuSceneName)
        {
            Debug.Log("NetworkClient: Loading MainMenu scene on disconnect.");
            SceneManager.LoadScene(MenuSceneName);
        }

        if (networkManager.IsConnectedClient)
        {
            Debug.Log("NetworkClient: Shutting down NetworkManager.");
            networkManager.Shutdown();
        }
        else
        {
            Debug.Log("NetworkClient: NetworkManager already shut down or not connected.");
        }
    }

    public void Dispose()
    {
        if (networkManager != null)
        {
            networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
            Debug.Log("NetworkClient: Unsubscribed from OnClientDisconnectCallback and disposed.");
            networkManager = null;
        }
    }
}