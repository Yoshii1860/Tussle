// --------------------------------------------
// --------------------------------------------
// Trying to add dedicated server mode online 
// --------------------------------------------
// --------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbiesList : MonoBehaviour
{
    [SerializeField] private LobbyItem lobbyItemPrefab;
    [SerializeField] private Transform lobbyItemContainer;
    [SerializeField] public MainMenu MainMenu;
    private bool isJoining = false;
    private bool isRefreshing = false;

    public static LobbiesList Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        RefreshList();
    }

    public async void RefreshList()
    {
        if (isRefreshing) return;
        isRefreshing = true;

        try
        {
            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>()
                {
                    new QueryFilter(
                        field: QueryFilter.FieldOptions.AvailableSlots,
                        op: QueryFilter.OpOptions.GT,
                        value: "0"),
                    new QueryFilter(
                        field: QueryFilter.FieldOptions.IsLocked,
                        op: QueryFilter.OpOptions.EQ,
                        value: "false")
                },
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);
            foreach (Transform child in lobbyItemContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (Lobby lobby in response.Results)
            {
                LobbyItem lobbyItem = Instantiate(lobbyItemPrefab, lobbyItemContainer);
                lobbyItem.Initialize(this, lobby);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to refresh lobby list: {e.Message}");
        }

        isRefreshing = false;
    }

    public async Task JoinLobbyWithCharacter(Lobby lobby)
    {
        if (isJoining) return;
        isJoining = true;

        try
        {
            Lobby joiningLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
            string joinCode = joiningLobby.Data["JoinCode"].Value;
            Debug.Log($"LobbiesList: Join code: {joinCode}");

            await ClientSingleton.Instance.GameManager.StartClientAsync(joinCode);
            Debug.Log($"LobbiesList: Joined lobby: {lobby.Name}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message}");
        }

        isJoining = false;
    }
}

/*

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbiesList : MonoBehaviour
{
    [SerializeField] private LobbyItem lobbyItemPrefab;
    [SerializeField] private Transform lobbyItemContainer;
    [SerializeField] public MainMenu MainMenu;
    private bool isJoining = false;
    private bool isRefreshing = false;

    public static LobbiesList Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        RefreshList();
    }

    public async void RefreshList()
    {
        if (isRefreshing) return;
        isRefreshing = true;

        try
        {
            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>()
                {
                    new QueryFilter(
                        field: QueryFilter.FieldOptions.AvailableSlots,
                        op: QueryFilter.OpOptions.GT,
                        value: "0"),
                    new QueryFilter(
                        field: QueryFilter.FieldOptions.IsLocked,
                        op: QueryFilter.OpOptions.EQ,
                        value: "false")
                },
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);
            foreach (Transform child in lobbyItemContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (Lobby lobby in response.Results)
            {
                LobbyItem lobbyItem = Instantiate(lobbyItemPrefab, lobbyItemContainer);
                lobbyItem.Initialize(this, lobby);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to refresh lobby list: {e.Message}");
        }

        isRefreshing = false;
    }

    public async Task JoinLobbyWithCharacter(Lobby lobby)
    {
        if (isJoining) return;
        isJoining = true;

        try
        {
            Lobby joiningLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
            string joinCode = joiningLobby.Data["JoinCode"].Value;
            Debug.Log($"LobbiesList: Join code: {joinCode}");

            await ClientSingleton.Instance.GameManager.StartClientAsync(joinCode);
            Debug.Log($"LobbiesList: Joined lobby: {lobby.Name}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message}");
        }

        isJoining = false;
    }
}
*/
