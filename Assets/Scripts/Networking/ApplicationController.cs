using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class ApplicationController : MonoBehaviour
{
    [SerializeField] private ClientSingleton clientPrefab;
    [SerializeField] private HostSingleton hostPrefab;

    private bool isServer;

    private async void Start()
    {
        DontDestroyOnLoad(gameObject);

        #if UNITY_SERVER

            isServer = true;

        #else

            isServer = false;

        #endif

        await LaunchInMode(isServer);
    }

    private async Task LaunchInMode(bool isDedicatedServer)
    {
        if (isDedicatedServer)
        {
            // Start server logic here
        }
        else
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
    }
}
