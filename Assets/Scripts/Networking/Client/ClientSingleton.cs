using UnityEngine;
using System.Threading.Tasks;

public class ClientSingleton : MonoBehaviour
{
    public ClientGameManager GameManager { get; private set; }
    
    private static ClientSingleton instance;
    public static ClientSingleton Instance
    {
        get
        {
            if (ApplicationData.Mode() == "server")
            {
                Debug.LogWarning("ClientSingleton: Attempted to access Instance in server mode. Returning null.");
                return null;
            }

            if (instance == null)
            {
                instance = FindFirstObjectByType<ClientSingleton>();
                if (instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(ClientSingleton).Name);
                    instance = singletonObject.AddComponent<ClientSingleton>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (ApplicationData.Mode() == "server")
        {
            Debug.Log("ClientSingleton: Destroying self in server mode.");
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (ApplicationData.Mode() == "server")
        {
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    public async Task<bool> CreateClient()
    {
        if (ApplicationData.Mode() == "server")
        {
            Debug.LogWarning("ClientSingleton: CreateClient called in server mode. Skipping.");
            return false;
        }

        GameManager = new ClientGameManager();
        return await GameManager.InitAsync();
    }

    public async Task StartClientAsync(string joinCode)
    {
        if (ApplicationData.Mode() == "server")
        {
            Debug.LogWarning("ClientSingleton: StartClientAsync called in server mode. Skipping.");
            return;
        }
        await GameManager.StartClientAsync(joinCode);
    }

    public async Task StartClientLocalAsync(string ip, int port)
    {
        if (ApplicationData.Mode() == "server")
        {
            Debug.LogWarning("ClientSingleton: StartClientLocalAsync called in server mode. Skipping.");
            return;
        }
        await GameManager.StartClientLocalAsync(ip, port);
    }

    private void OnDestroy()
    {
        if (ApplicationData.Mode() == "server")
        {
            return;
        }
        GameManager?.Dispose();
    }
}