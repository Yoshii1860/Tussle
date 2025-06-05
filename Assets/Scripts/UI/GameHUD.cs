using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using System;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private GameObject blackscreen;
    [SerializeField] private GameObject minimap;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private HealthDisplay healthDisplay;
    [SerializeField] private InputReader inputReader;

    [SerializeField] private Transform attackIconsContainer;

    private GameObject localCharacter;

    private const string JoinCodePrefix = "Code: ";

    private void Awake()
    {

#if UNITY_SERVER
        return;
#endif

        blackscreen.SetActive(true);

        if (minimap != null)
        {
            minimap = Instantiate(minimap, transform.GetChild(0));
            Debug.Log("Minimap instantiated");
        }

        var hostSingleton = FindFirstObjectByType<HostSingleton>();
        if (hostSingleton != null && joinCodeText != null)
        {
            if (hostSingleton.IsPrivateServer)
            {
                string joinCode = hostSingleton.GameManager.GetJoinCode();
                joinCodeText.text = JoinCodePrefix + joinCode;
                Debug.Log($"Join code set: {joinCodeText.text}");
            }
        }

        if (inputReader != null)
        {
            inputReader.ChangeAttackEvent += OnChangeAttack;
            OnChangeAttack(0);
        }

        StartCoroutine(HideBlackscreen(1f));
    }

    private void OnChangeAttack(int index)
    {
        Debug.Log($"GameHUD: OnChangeAttack called with index {index}");
        foreach (Transform child in attackIconsContainer)
        {
            if (child == attackIconsContainer.GetChild(index))
            {
                child.GetChild(0).gameObject.SetActive(true);
            }
            else
            {
                child.GetChild(0).gameObject.SetActive(false);
            }
        }
    }

    public void SetLocalCharacter(GameObject character)
    {
        localCharacter = character;
        healthDisplay.InitializeGameHUDHealthBar(character);
    }

    public void SetIcons(Attack[] attacks, Attack secondaryAttack)
    {
        for (int i = 0; i < attacks.Length; i++)
        {
            if (i < attackIconsContainer.childCount)
            {
                Image icon = attackIconsContainer.GetChild(i).GetComponentInChildren<Image>();
                if (icon != null)
                {
                    icon.sprite = attacks[i].icon;
                }
            }
        }

        attackIconsContainer.GetChild(attacks.Length).GetComponentInChildren<Image>().color = new Color(0f, 0f, 0f, 0f);

        if (secondaryAttack != null)
        {
            attackIconsContainer.GetChild(attacks.Length + 1).GetComponentInChildren<Image>().sprite = secondaryAttack.icon;
        }
        else
        {
            attackIconsContainer.GetChild(attacks.Length + 1).GetComponentInChildren<Image>().color = new Color(0f, 0f, 0f, 0f);
        }
    }

    private IEnumerator HideBlackscreen(float delay)
    {
        yield return new WaitForSeconds(delay);

        blackscreen.SetActive(false);
    }

    public void LeaveGame()
    {
        inputReader.ChangeAttackEvent -= OnChangeAttack;

        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("GameHUD: Leaving game as Host. Shutting down the server.");
            HostSingleton.Instance.GameManager.Shutdown();
            return;
        }

        
        ClientSingleton.Instance.GameManager.Disconnect();
    }
}