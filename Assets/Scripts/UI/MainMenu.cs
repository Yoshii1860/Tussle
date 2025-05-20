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