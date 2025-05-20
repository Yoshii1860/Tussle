using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;

public class ApplicationController : MonoBehaviour
{
    [SerializeField] private ClientSingleton clientPrefab;
    [SerializeField] private HostSingleton hostPrefab;
#if UNITY_SERVER
    [SerializeField] private ServerSingleton serverPrefab;
#endif

    private ApplicationData appData;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        appData = new ApplicationData();

        // Initialize Unity Services only if not in server mode
        if (ApplicationData.Mode() != "server")
        {
            try
            {
                await UnityServices.InitializeAsync();
                Debug.Log("Unity Services initialized successfully.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
                return;
            }
        }

        await LaunchInMode(ApplicationData.Mode());
    }

    private async Task LaunchInMode(string mode)
    {
        if (mode == "server")
        {

#if UNITY_SERVER
            ServerSingleton serverSingleton = Instantiate(serverPrefab);
            await serverSingleton.CreateServer();
            await serverSingleton.GameManager.StartGameServerAsync();
#endif

        }
        else // if (mode == "host")
        {
            HostSingleton hostSingleton = Instantiate(hostPrefab);
            hostSingleton.CreateHost();

            ClientSingleton clientSingleton = Instantiate(clientPrefab);
            bool authenticated = await clientSingleton.CreateClient();
            if (authenticated)
            {
                clientSingleton.GameManager.GoToMenu();
            }
            else
            {
                Debug.LogError("ApplicationController: Client authentication failed.");
            }
        }
        /*
        else
        {
            ClientSingleton clientSingleton = Instantiate(clientPrefab);
            bool authenticated = await clientSingleton.CreateClient();
            if (authenticated)
            {
                clientSingleton.GameManager.GoToMenu();
            }
            else
            {
                Debug.LogError("ApplicationController: Client authentication failed.");
            }
        }
        */
    }

    private void OnDestroy()
    {
    }
}