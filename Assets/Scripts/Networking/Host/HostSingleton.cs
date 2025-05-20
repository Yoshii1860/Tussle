using UnityEngine;
using System.Threading.Tasks;

public class HostSingleton : MonoBehaviour
{
    public HostGameManager GameManager { get; private set; }
    
    private static HostSingleton instance;
    public static HostSingleton Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<HostSingleton>();
                if (instance == null)
                {
                    GameObject singletonObject = new GameObject(typeof(HostSingleton).Name);
                    instance = singletonObject.AddComponent<HostSingleton>();
                }
            }
            return instance;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void CreateHost()
    {
        GameManager = new HostGameManager();
    }

    private void OnDestroy()
    {
        GameManager?.Dispose();
    }
}