using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

public class NetworkUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_Dropdown characterDropdown;
    [SerializeField] private PlayerSelectionManager selectionManager;

    private CharacterType selectedCharacterType = CharacterType.Knight;

    private void Start()
    {
        if (characterDropdown == null)
        {
            Debug.LogError($"Character Dropdown is not assigned or found in {gameObject.name}!");
            return;
        }
        if (hostButton == null) Debug.LogError("Host Button is not assigned in the Inspector!");
        if (joinButton == null) Debug.LogError("Join Button is not assigned in the Inspector!");
        if (selectionManager == null) Debug.LogError("Selection Manager is not assigned in the Inspector!");

        characterDropdown.ClearOptions();
        characterDropdown.AddOptions(new System.Collections.Generic.List<string> { "Knight", "Archer", "Priest" });

        characterDropdown.value = (int)selectedCharacterType;
        characterDropdown.onValueChanged.AddListener(OnCharacterDropdownChanged);

        hostButton.onClick.AddListener(StoreSelectionAndStartHost);
        joinButton.onClick.AddListener(StoreSelectionAndStartClient);
    }

    private void StoreSelectionAndStartHost()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.StartHost();
        gameObject.SetActive(false);
    }

    private void StoreSelectionAndStartClient()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.StartClient();
        gameObject.SetActive(false);
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"Client {clientId} connected.");
            selectionManager.SetCharacterSelectionServerRpc(clientId, selectedCharacterType);
        }
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnCharacterDropdownChanged(int value)
    {
        selectedCharacterType = (CharacterType)value;
        Debug.Log($"Selected character index: {value}, Type: {selectedCharacterType}");
    }

    private void OnDestroy()
    {
        if (hostButton != null) hostButton.onClick.RemoveAllListeners();
        if (joinButton != null) joinButton.onClick.RemoveAllListeners();
        if (characterDropdown != null) characterDropdown.onValueChanged.RemoveAllListeners();
    }
}