using System;
using System.Collections.Generic;
using System.Numerics;
using Unity.Netcode;
using UnityEngine;

public class NetworkServer : IDisposable
{
    private NetworkManager networkManager;

    public Action<string> OnClientLeft;

    private Dictionary<ulong, string> clientIdToAuth = new Dictionary<ulong, string>();
    private Dictionary<string, UserData> authIdToUserData = new Dictionary<string, UserData>();
    private Dictionary<ulong, float[]> clientIdToSpawnPosition = new Dictionary<ulong, float[]>();

    public static NetworkServer Instance { get; private set; }

    private Dictionary<ulong, int> clientKills = new Dictionary<ulong, int>();

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

        // Get and store a random spawn position
        float[] spawnPosition = SpawnPoint.GetRandomSpawnPos();
        clientIdToSpawnPosition[request.ClientNetworkId] = spawnPosition;
        response.Position = new UnityEngine.Vector3(spawnPosition[0], spawnPosition[1], spawnPosition[2]);
        Debug.Log($"ApprovalCheck: ClientId={request.ClientNetworkId}, AuthId={userData.userAuthId}, Position={response.Position}");
        
        response.Approved = true;
        response.CreatePlayerObject = false;

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
            OnClientLeft?.Invoke(authId);
            Debug.Log($"Client {clientId} disconnected. AuthId: {authId}");
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

    public bool TryGetSpawnPosition(ulong clientId, out float[] spawnPosition)
    {
        return clientIdToSpawnPosition.TryGetValue(clientId, out spawnPosition);
    }

    public void AddKill(ulong killerClientId)
    {
        if (clientKills.ContainsKey(killerClientId))
        {
            clientKills[killerClientId]++;
        }
        else
        {
            clientKills[killerClientId] = 1;
        }

        Debug.Log($"Client {killerClientId} has {clientKills[killerClientId]} kills.");

        Leaderboard.Instance.UpdateKills(killerClientId, clientKills[killerClientId]);
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
