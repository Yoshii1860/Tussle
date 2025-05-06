using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;
using System.Linq;

public class PlayerSelectionManager : NetworkBehaviour
{
    // Structure to hold client ID and their selected character type
    private struct PlayerSelection : INetworkSerializable, IEquatable<PlayerSelection>
    {
        public ulong ClientId;
        public CharacterType CharacterType;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref CharacterType);
        }

        public bool Equals(PlayerSelection other)
        {
            return ClientId == other.ClientId && CharacterType == other.CharacterType;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerSelection other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ClientId.GetHashCode() ^ CharacterType.GetHashCode();
        }
    }

    private NetworkList<PlayerSelection> playerSelections;

    private void Awake()
    {
        playerSelections = new NetworkList<PlayerSelection>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            playerSelections.Clear();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetCharacterSelectionServerRpc(ulong clientId, CharacterType characterType)
    {
        // Check if the client already has a selection
        for (int i = 0; i < playerSelections.Count; i++)
        {
            if (playerSelections[i].ClientId == clientId)
            {
                var selection = playerSelections[i];
                selection.CharacterType = characterType;
                playerSelections[i] = selection;
                Debug.Log($"Updated selection for Client {clientId}: {characterType}");
                return;
            }
        }

        // Add new selection
        playerSelections.Add(new PlayerSelection { ClientId = clientId, CharacterType = characterType });
        Debug.Log($"Stored selection for Client {clientId}: {characterType}");
    }

    public CharacterType GetCharacterSelection(ulong clientId)
    {
        foreach (var selection in playerSelections)
        {
            if (selection.ClientId == clientId)
            {
                Debug.Log($"Found selection for Client {clientId}: {selection.CharacterType}");
                return selection.CharacterType;
            }
        }
        Debug.LogWarning($"No selection found for Client {clientId}, defaulting to Knight");
        return CharacterType.Knight;
    }

    public bool HasSelection(ulong clientId)
    {
        foreach (var selection in playerSelections)
        {
            if (selection.ClientId == clientId)
            {
                return true;
            }
        }
        return false;
    }
}