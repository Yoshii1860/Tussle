using UnityEngine;
using TMPro;
using Unity.Collections;
using System;

public class PlayerNameDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private TMP_Text playerNameText;
    
    private void Start()
    {
        HandlePlayerNameChanged(string.Empty, player.PlayerName.Value);
        
        player.PlayerName.OnValueChanged += HandlePlayerNameChanged;
    }

    private void HandlePlayerNameChanged(FixedString32Bytes oldName, FixedString32Bytes newName)
    {
        playerNameText.text = newName.ToString();
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.PlayerName.OnValueChanged -= HandlePlayerNameChanged;
        }
    }
}
