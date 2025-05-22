using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NameSelector : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button connectButton;
    [SerializeField] private int minNameLength = 3;
    [SerializeField] private int maxNameLength = 20;

    public const string PlayerNameKey = "PlayerName";

    private void Start()
    {

#if UNITY_SERVER
        Debug.Log($"Server started at {System.DateTime.Now}");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        return;
#endif

        nameInputField.text = PlayerPrefs.GetString(PlayerNameKey, string.Empty);
        HandleNameChanged();
    }

    public void HandleNameChanged()
    {
        connectButton.interactable = 
            nameInputField.text.Length >= minNameLength &&
            nameInputField.text.Length <= maxNameLength;
    }

    public void Connect()
    {
        PlayerPrefs.SetString(PlayerNameKey, nameInputField.text);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
