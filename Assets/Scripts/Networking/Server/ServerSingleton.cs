#if UNITY_SERVER
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class ServerSingleton : MonoBehaviour
{
    public ServerGameManager GameManager { get; private set; }
    
    private static ServerSingleton instance;
    public static ServerSingleton Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ServerSingleton>();
                if (instance == null)
                {
                    Debug.LogError("ServerSingleton instance not found in the scene.");
                }
            }
            return instance;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public async Task CreateServer()
    {
        // Remove UnityServices.InitializeAsync() since ApplicationController handles it
        GameManager = new ServerGameManager(
            ApplicationData.IP(),
            ApplicationData.Port(),
            ApplicationData.QPort(),
            NetworkManager.Singleton
        );
        Debug.Log("ServerSingleton: Server created.");
        await Task.CompletedTask;
    }

    private void OnDestroy()
    {
        GameManager?.Dispose();
    }
}
#endif