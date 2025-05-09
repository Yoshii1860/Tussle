using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkServer : IDisposable
{
    private NetworkManager networkManager;

    private Dictionary<ulong, string> clientIdToAuth = new Dictionary<ulong, string>();
    private Dictionary<string, UserData> authIdToUserData = new Dictionary<string, UserData>();

    public static NetworkServer Instance { get; private set; }

    public NetworkServer(NetworkManager networkManager)
    {
        Instance = this;
        this.networkManager = networkManager;

        networkManager.ConnectionApprovalCallback += ApprovalCheck;
        networkManager.OnServerStarted += OnNetworkReady;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string payload = System.Text.Encoding.UTF8.GetString(request.Payload);
        UserData userData = JsonUtility.FromJson<UserData>(payload);

        clientIdToAuth[request.ClientNetworkId] = userData.userAuthId;
        authIdToUserData[userData.userAuthId] = userData;

        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Position = Vector3.zero;
        response.Rotation = Quaternion.identity;
    }

    private void OnNetworkReady()
    {
        networkManager.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (clientIdToAuth.TryGetValue(clientId, out string authId))
        {
            clientIdToAuth.Remove(clientId);
            authIdToUserData.Remove(authId);
        }
    }

    public bool TryGetCharacterId(ulong clientId, out int characterId)
    {
        Debug.Log($"NetworkServer: Trying to get character ID for client {clientId}");
        if (clientIdToAuth.TryGetValue(clientId, out string authId))
        {
            Debug.Log($"NetworkServer: Found auth ID {authId} for client {clientId}");
            if (authIdToUserData.TryGetValue(authId, out UserData userData))
            {
                Debug.Log($"NetworkServer: Found user data for auth ID {authId}");
                characterId = userData.characterId;
                Debug.Log($"NetworkServer: Character ID for client {clientId} is {characterId}");
                return true;
            }
        }
        characterId = 0;
        return false;
    }

    public void Dispose()
    {
        if (networkManager != null)
        {
            networkManager.ConnectionApprovalCallback -= ApprovalCheck;
            networkManager.OnServerStarted -= OnNetworkReady;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
            
            if (networkManager.IsListening)
            {
                networkManager.Shutdown();
            }
        }
    }
}
