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
                    Debug.Log("HostSingleton: Created new singleton instance.");
                }
                else
                {
                    Debug.Log("HostSingleton: Found existing instance in scene.");
                }
            }
            return instance;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        Debug.Log("HostSingleton: Marked as DontDestroyOnLoad.");
    }

    public void CreateHost()
    {
        GameManager = new HostGameManager();
        Debug.Log("HostSingleton: Creating HostGameManager.");
    }

    private void OnDestroy()
    {
        Debug.Log("HostSingleton: OnDestroy called, disposing GameManager.");
        if (Instance == this)
        {
            instance = null; // Clear the instance reference
            Debug.Log("HostSingleton: Instance cleared on destroy.");
        }
        GameManager?.Dispose();
    }
}