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
            if (instance == null)
            {
                instance = FindFirstObjectByType<ClientSingleton>();
                if (instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(ClientSingleton).Name);
                    instance = singletonObject.AddComponent<ClientSingleton>();
                    Debug.Log("ClientSingleton: Created new singleton instance.");
                }
                else
                {
                    Debug.Log("ClientSingleton: Found existing instance in scene.");
                }
            }
            return instance;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        Debug.Log("ClientSingleton: Marked as DontDestroyOnLoad.");
    }

    public async Task<bool> CreateClient()
    {
        Debug.Log("ClientSingleton: Creating ClientGameManager and initializing client.");
        GameManager = new ClientGameManager();
        bool result = await GameManager.InitAsync();
        Debug.Log($"ClientSingleton: ClientGameManager initialization result: {result}");
        return result;
    }

    public async Task StartClientAsync(string joinCode)
    {
        Debug.Log($"ClientSingleton: Starting client with join code: {joinCode}");
        await GameManager.StartClientAsync(joinCode);
    }

    public async Task StartClientLocalAsync(string ip, int port)
    {
        Debug.Log($"ClientSingleton: Starting local client with IP: {ip}, Port: {port}");
        await GameManager.StartClientLocalAsync(ip, port);
    }

    private void OnDestroy()
    {
        Debug.Log("ClientSingleton: OnDestroy called, disposing GameManager.");
        GameManager?.Dispose();
    }
}