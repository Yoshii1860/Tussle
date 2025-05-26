using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using Newtonsoft.Json;

public class NetworkServer : IDisposable
{
    private NetworkManager networkManager;

    public Action<string> OnClientLeft;
    public Action<UserData> OnUserJoined;
    public Action<UserData> OnUserLeft;

    private Dictionary<ulong, string> clientIdToAuth = new Dictionary<ulong, string>();
    private Dictionary<string, UserData> authIdToUserData = new Dictionary<string, UserData>();

    public static NetworkServer Instance { get; private set; }

    public NetworkServer(NetworkManager networkManager)
    {
        Instance = this;
        this.networkManager = networkManager;

        networkManager.ConnectionApprovalCallback += ApprovalCheck;
        networkManager.OnServerStarted += OnNetworkReady;
        Debug.Log("NetworkServer: Initialized and registered approval and server started callbacks.");
    }

    public bool OpenConnection(string ip, int port)
    {
        UnityTransport transport = networkManager.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, (ushort)port);

        bool started = networkManager.StartServer();
        Debug.Log($"NetworkServer: OpenConnection called. IP: {ip}, Port: {port}, Started: {started}");
        return started;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string payload = System.Text.Encoding.UTF8.GetString(request.Payload);
        UserData userData = JsonConvert.DeserializeObject<UserData>(payload);

        clientIdToAuth[request.ClientNetworkId] = userData.userAuthId;
        authIdToUserData[userData.userAuthId] = userData;

        OnUserJoined?.Invoke(userData);

        response.Approved = true;
        response.CreatePlayerObject = false;

        Debug.Log($"NetworkServer: Approved connection for ClientId={request.ClientNetworkId}, AuthId={userData.userAuthId}, CharacterId={userData.characterId}");
    }

    private void OnNetworkReady()
    {
        networkManager.OnClientDisconnectCallback += OnClientDisconnect;
        Debug.Log("NetworkServer: Server started and ready for client connections.");
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (clientIdToAuth.TryGetValue(clientId, out string authId))
        {
            clientIdToAuth.Remove(clientId);
            if (authIdToUserData.TryGetValue(authId, out UserData userData))
            {
                OnUserLeft?.Invoke(userData);
                authIdToUserData.Remove(authId);
            }
            OnClientLeft?.Invoke(authId);
            Debug.Log($"NetworkServer: Client {clientId} disconnected. AuthId: {authId}");
        }
        else
        {
            Debug.LogWarning($"NetworkServer: Client {clientId} disconnected but no auth mapping found.");
        }
    }

    public bool TryGetCharacterId(ulong clientId, out int characterId)
    {
        if (clientIdToAuth.TryGetValue(clientId, out string authId))
        {
            if (authIdToUserData.TryGetValue(authId, out UserData userData))
            {
                characterId = userData.characterId;
                return true;
            }
        }
        characterId = 0;
        return false;
    }

    public UserData TryGetUserData(ulong clientId)
    {
        if (clientIdToAuth.TryGetValue(clientId, out string authId))
        {
            if (authIdToUserData.TryGetValue(authId, out UserData userData))
            {
                return userData;
            }
        }
        return null;
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
                Debug.Log("NetworkServer: Shutdown and cleaned up.");
            }
        }
    }
}