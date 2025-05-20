using Unity.Netcode;
using UnityEngine;
using TMPro;

public class GameHUD : MonoBehaviour
{
    private void Awake()
    {
        // Disable UI for dedicated server mode as early as possible
        if (ApplicationData.Mode() == "server" || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            Debug.Log("Dedicated server mode detected in GameHUD. Disabling UI elements.");

            // Disable this GameObject (the HUD)
            gameObject.SetActive(false);

            // Disable all Canvas components in the scene
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas != null && canvas.gameObject != null)
                {
                    canvas.gameObject.SetActive(false);
                }
            }

            // Disable all TMP_Text components in the scene
            var tmpTexts = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            foreach (var tmp in tmpTexts)
            {
                if (tmp != null && tmp.gameObject != null)
                {
                    tmp.gameObject.SetActive(false);
                }
            }

            // Disable all TMP_InputField components in the scene
            var tmpInputFields = FindObjectsByType<TMP_InputField>(FindObjectsSortMode.None);
            foreach (var inputField in tmpInputFields)
            {
                if (inputField != null && inputField.gameObject != null)
                {
                    inputField.gameObject.SetActive(false);
                }
            }
        }
    }

    public void LeaveGame()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            HostSingleton.Instance.GameManager.Shutdown();
        }

        ClientSingleton.Instance.GameManager.Disconnect();
    }
}