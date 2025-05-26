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
                    Debug.LogError("ServerSingleton: Instance not found in the scene.");
                }
                else
                {
                    Debug.Log("ServerSingleton: Found existing instance in scene.");
                }
            }
            return instance;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        Debug.Log("ServerSingleton: Marked as DontDestroyOnLoad.");
    }

    public async Task CreateServer()
    {
        Debug.Log("ServerSingleton: Creating ServerGameManager...");
        GameManager = new ServerGameManager(
            ApplicationData.IP(),
            ApplicationData.Port(),
            ApplicationData.QPort(),
            NetworkManager.Singleton
        );
        Debug.Log("ServerSingleton: ServerGameManager created.");
        await Task.CompletedTask;
    }

    private void OnDestroy()
    {
        Debug.Log("ServerSingleton: OnDestroy called, disposing ServerGameManager.");
        GameManager?.Dispose();
    }
}
#endif