using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private GameObject blackscreen;
    [SerializeField] private GameObject minimap;
    [SerializeField] private TMP_Text joinCodeText;

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

        StartCoroutine(HideBlackscreen(1f));
    }

    private IEnumerator HideBlackscreen(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        blackscreen.SetActive(false);
    }

    public void LeaveGame()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("GameHUD: Leaving game as Host. Shutting down the server.");
            HostSingleton.Instance.GameManager.Shutdown();
            return;
        }

        ClientSingleton.Instance.GameManager.Disconnect();
    }
}