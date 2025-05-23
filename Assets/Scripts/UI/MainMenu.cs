// --------------------------------------------
// --------------------------------------------
// Trying to add dedicated server mode online 
// --------------------------------------------
// --------------------------------------------

using System.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeField;
    [SerializeField] private GameObject characterSelectionPanel;
    [SerializeField] private TMP_Text findMatchButtonText;
    [SerializeField] private TMP_Text queueTimerText;
    [SerializeField] private TMP_Text queueStatusText;
    [SerializeField] private HostSingleton hostPrefab;

    private int selectedCharacterId = -1;
    private bool isHosting = false;
    private bool isJoiningLobby = false;
    private bool isFindingMatchViaMatchmaking = false;
    private bool isMatchmaking;
    private bool isCancelling;
    private bool isBusy = false;
    private string pendingJoinCode = "";
    private Lobby pendingLobby = null;
    private float queueTimer = 0f;

    private void Awake()
    {
        if (ApplicationData.Mode() == "server")
        {
            Debug.Log("Dedicated server mode detected in MainMenu. Disabling UI elements.");
            gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (ClientSingleton.Instance == null) { return; }
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        characterSelectionPanel.SetActive(false);
        findMatchButtonText.text = "Find Match";
        queueTimerText.text = string.Empty;
        queueStatusText.text = string.Empty;
        Debug.Log($"MainMenu Start: selectedCharacterId={selectedCharacterId}, isHosting={isHosting}, " +
                $"isJoiningLobby={isJoiningLobby}, isFindingMatchViaMatchmaking={isFindingMatchViaMatchmaking}, " +
                $"pendingJoinCode={pendingJoinCode}, pendingLobby={pendingLobby?.Name ?? "null"}");
    }

    private void Update()
    {
        if (isMatchmaking)
        {
            // Update queue timer or other UI elements related to matchmaking
            queueTimer += Time.deltaTime;
            TimeSpan ts = TimeSpan.FromSeconds(queueTimer);
            queueTimerText.text = string.Format("{0:D2}:{1:D2}", ts.Minutes, ts.Seconds);
        }
    }

    public void StartHost()
    {
        isHosting = true;
        characterSelectionPanel.SetActive(true);
    }

    public void StartClient()
    {
        isHosting = false;
        pendingJoinCode = joinCodeField.text;
        characterSelectionPanel.SetActive(true);
    }

    public void FindMatch()
    {
        if (isMatchmaking)
        {
            CancelMatchmaking();
        }
        else if (!isBusy)
        {
            isFindingMatchViaMatchmaking = true;
            characterSelectionPanel.SetActive(true);
        }
    }

    // public async Task SelectCharacter(int characterId)
    public async Task LaunchGameMode()
    {
        if (isBusy) { return; }
        isBusy = true;

    //    selectedCharacterId = characterId;

        characterSelectionPanel.SetActive(false);
    /*
        if (selectedCharacterId < 0)
        {
            Debug.LogError("Invalid character ID selected.");
            isBusy = false;
            return;
        }
    */
        try
        {
            if (isHosting)
            {
                HostSingleton hostSingleton = Instantiate(hostPrefab);
                hostSingleton.CreateHost();

                while (hostSingleton.GameManager == null)
                {
                    Debug.Log("Waiting for GameManager to be created...");
                    await Task.Delay(100);
                }

                StartHostWithCharacter();
            }
            else if (isJoiningLobby)
            {
            //    PlayerPrefs.SetInt("SelectedCharacterId", selectedCharacterId);
                await JoinLobbyWithCharacter(pendingLobby);
                isJoiningLobby = false;
                pendingLobby = null;
            }
            else if (isFindingMatchViaMatchmaking)
            {
                if (isCancelling) { return; }
                StartMatchmakingWithCharacter();
                isFindingMatchViaMatchmaking = false;
            }
            else
            {
                StartClientWithCharacter();
            }
        }
        finally
        {
            isBusy = false;
        }
    }

    public async void CancelMatchmaking()
    {
        if (!isMatchmaking || isCancelling) { return; }
        queueStatusText.text = "Cancelling...";
        isCancelling = true;

        await ClientSingleton.Instance.GameManager.CancelMatchmakingAsync();

        isCancelling = false;
        isMatchmaking = false;
        findMatchButtonText.text = "Find Match";
        queueStatusText.text = string.Empty;
        queueTimerText.text = string.Empty;
        // Ensure the panel stays closed
        characterSelectionPanel.SetActive(false);
        isBusy = false;
    }

    private async void StartHostWithCharacter()
    {
    //    PlayerPrefs.SetInt("SelectedCharacterId", selectedCharacterId);
        await HostSingleton.Instance.GameManager.StartHostAsync();
    }

    private async void StartClientWithCharacter()
    {
    //    PlayerPrefs.SetInt("SelectedCharacterId", selectedCharacterId);
        await ClientSingleton.Instance.GameManager.StartClientAsync(pendingJoinCode);
    }

    public async Task JoinLobbyWithCharacter(Lobby lobby)
    {
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
    }

    private async void StartMatchmakingWithCharacter()
    {
        if (ClientSingleton.Instance == null)
        {
            Debug.LogError("ClientSingleton.Instance is null!");
            return;
        }
        if (ClientSingleton.Instance.GameManager == null)
        {
            Debug.LogError("ClientSingleton.Instance.GameManager is null!");
            return;
        }
        if (findMatchButtonText == null)
        {
            Debug.LogError("findMatchButtonText is null!");
            return;
        }
        if (queueStatusText == null)
        {
            Debug.LogError("queueStatusText is null!");
            return;
        }

        ClientSingleton.Instance.GameManager.MatchmakeAsync(OnMatchMade);
    //    PlayerPrefs.SetInt("SelectedCharacterId", selectedCharacterId);
        findMatchButtonText.text = "Cancel";
        queueStatusText.text = "Searching...";
        queueTimer = 0f;
        isMatchmaking = true;
    }

    public void CloseCharacterSelectionPanel()
    {
        characterSelectionPanel.SetActive(false);
        isBusy = false;
    }

    /*
    private async void StartLocalMatchWithCharacter()
    {
        PlayerPrefs.SetInt("SelectedCharacterId", selectedCharacterId);
        await ClientSingleton.Instance.GameManager.StartClientLocalAsync("172.27.205.68", 7777);
    }
    */

    private void OnMatchMade(MatchmakerPollingResult result)
    {
        switch (result)
        {
            case MatchmakerPollingResult.Success:
                queueStatusText.text = "Connecting...";
                break;
            case MatchmakerPollingResult.TicketCreationError:
                queueStatusText.text = "Ticket Creation Error";
                break;
            case MatchmakerPollingResult.TicketCancellationError:
                queueStatusText.text = "Ticket Cancellation Error";
                break;
            case MatchmakerPollingResult.TicketRetrievalError:
                queueStatusText.text = "Ticket Retrieval Error";
                break;
            case MatchmakerPollingResult.MatchAssignmentError:
                queueStatusText.text = "Match Assignment Error";
                break;
            default:
                queueStatusText.text = "Unknown Error";
                break;
        }
    }

    public void InitiateLobbyJoin(Lobby lobby)
    {
        isJoiningLobby = true;
        pendingLobby = lobby;
        characterSelectionPanel.SetActive(true);
    }
}

/*
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeField;
    [SerializeField] private GameObject characterSelectionPanel;
    [SerializeField] private TMP_Text findMatchButtonText;
    [SerializeField] private TMP_Text queueTimerText;
    [SerializeField] private TMP_Text queueStatusText;
    private int selectedCharacterId = -1;
    private bool isHosting = false;
    private bool isJoiningLobby = false;
    private bool isFindingLocalMatch = false;
    private string pendingJoinCode = "";
    private Lobby pendingLobby = null;

    private void Awake()
    {
        // Disable UI for dedicated server mode
        if (ApplicationData.Mode() == "server")
        {
            Debug.Log("Dedicated server mode detected in MainMenu. Disabling UI elements.");
            joinCodeField?.gameObject.SetActive(false);
            characterSelectionPanel?.SetActive(false);
            findMatchButtonText?.gameObject.SetActive(false);
            queueTimerText?.gameObject.SetActive(false);
            queueStatusText?.gameObject.SetActive(false);
            gameObject.SetActive(false); // Disable the entire MainMenu GameObject
        }
    }

    private void Start()
    {
        if (ClientSingleton.Instance == null) { return; }

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        characterSelectionPanel.SetActive(false);
        findMatchButtonText.text = "Find Match";
        queueTimerText.text = string.Empty;
        queueStatusText.text = string.Empty;
    }

    public void StartHost()
    {
        isHosting = true;
        characterSelectionPanel.SetActive(true);
    }

    public void StartClient()
    {
        isHosting = false;
        pendingJoinCode = joinCodeField.text;
        characterSelectionPanel.SetActive(true);
    }

    public void FindMatch()
    {
        isFindingLocalMatch = true;
        characterSelectionPanel.SetActive(true);
    }

    public void SelectCharacter(int characterId)
    {
        selectedCharacterId = characterId;
        characterSelectionPanel.SetActive(false);

        if (selectedCharacterId < 0)
        {
            Debug.LogError("Invalid character ID selected.");
            return;
        }

        if (isHosting)
        {
            StartHostWithCharacter();
        }
        else
        {
            if (isJoiningLobby)
            {
                StartLobbyJoinWithCharacter();
                isJoiningLobby = false;
            }
            else if (isFindingLocalMatch)
            {
                StartLocalMatchWithCharacter();
                isFindingLocalMatch = false;
            }
            else
            {
                StartClientWithCharacter();
            }
        }
    }

    private async void StartHostWithCharacter()
    {
        PlayerPrefs.SetInt("SelectedCharacterId", selectedCharacterId);
        await HostSingleton.Instance.GameManager.StartHostAsync();
    }

    private async void StartClientWithCharacter()
    {
        PlayerPrefs.SetInt("SelectedCharacterId", selectedCharacterId);
        await ClientSingleton.Instance.GameManager.StartClientAsync(pendingJoinCode);
    }

    private async void StartLobbyJoinWithCharacter()
    {
        PlayerPrefs.SetInt("SelectedCharacterId", selectedCharacterId);
        await LobbiesList.Instance.JoinLobbyWithCharacter(pendingLobby);
        pendingLobby = null;
    }

    private async void StartLocalMatchWithCharacter()
    {
        PlayerPrefs.SetInt("SelectedCharacterId", selectedCharacterId);
        await ClientSingleton.Instance.GameManager.StartClientLocalAsync("172.27.205.68", 7777);
    }

    public void InitiateLobbyJoin(Lobby lobby)
    {
        isJoiningLobby = true;
        pendingLobby = lobby;
        characterSelectionPanel.SetActive(true);
    }
}
*/