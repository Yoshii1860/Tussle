using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private GameObject blackscreen;
    [SerializeField] private GameObject minimap;

    private void Awake()
    {
        if (NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsHost) { return; }
        
        blackscreen.SetActive(true);

        if (minimap != null)
        {
            minimap = Instantiate(minimap, transform.GetChild(0));
            Debug.Log("Minimap instantiated");
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