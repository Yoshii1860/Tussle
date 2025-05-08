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
                }
            }
            return instance;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public async Task<bool> CreateClient()
    {
        GameManager = new ClientGameManager();
        return await GameManager.InitAsync();
    }

    public async Task StartClientAsync(string joinCode)
    {
        await GameManager.StartClientAsync(joinCode);
    }
}
