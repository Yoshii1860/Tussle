using System.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeField;
    [SerializeField] private GameObject characterSelectionPanel;
    private int selectedCharacterId = -1;
    private bool isHosting = false;
    private bool isJoiningLobby = false;
    private string pendingJoinCode = "";
    private Lobby pendingLobby = null;

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

    public void InitiateLobbyJoin(Lobby lobby)
    {
        isJoiningLobby = true;
        pendingLobby = lobby;
        characterSelectionPanel.SetActive(true);
    }
}
