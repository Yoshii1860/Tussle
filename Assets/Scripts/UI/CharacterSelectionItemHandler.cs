using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using Unity.VisualScripting;

public class CharacterSelectionItemHandler : MonoBehaviour
{
    [SerializeField] GameObject characterSelectionPrefab;
    [SerializeField] Transform container;
    [SerializeField] MainMenu mainMenu;

    private void Start()
    {
        foreach (CharacterType characterType in Enum.GetValues(typeof(CharacterType)))
        {
            GameObject characterSelectionItem = Instantiate(characterSelectionPrefab, container);
            TMP_Text text = characterSelectionItem.GetComponentInChildren<TMP_Text>();
            Image image = characterSelectionItem.GetComponentInChildren<Image>();
            Button button = characterSelectionItem.GetComponentInChildren<Button>();

            if (text != null)
            {
                text.text = characterType.ToString();
            }
            else
            {
                Debug.LogWarning("TMP_Text component not found in character selection item prefab.");
            }

            Sprite characterImage = Resources.Load<Sprite>($"CharacterImages/{characterType}");
            if (characterImage != null)
            {
                image.sprite = characterImage;
            }
            else
            {
                Debug.LogWarning($"Character image for {characterType} not found in Resources/CharacterImages.");
            }

            if (button != null)
            {
                button.onClick.AddListener(() => OnCharacterSelected(characterType));
            }
            else
            {
                Debug.LogWarning("Button component not found in character selection item prefab.");
            }
        }
    }

    public void OnCharacterSelected(CharacterType characterType)
    {
        int characterId = (int)characterType;
        ClientSingleton.Instance.GameManager.SetCharacterId(characterId);
        PlayerPrefs.SetInt("SelectedCharacterId", characterId);
        Debug.Log($"Character selected: {characterType} with ID: {characterId}");

        // mainMenu.SelectCharacter(characterId);
        mainMenu.LaunchGameMode();
    }
}
