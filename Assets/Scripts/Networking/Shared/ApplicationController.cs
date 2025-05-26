using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using UnityEngine.SceneManagement;

public class ApplicationController : MonoBehaviour
{
    [SerializeField] private ClientSingleton clientPrefab;
    [SerializeField] private HostSingleton hostPrefab;

#if UNITY_SERVER
    [SerializeField] private ServerSingleton serverPrefab;

    private ApplicationData appData;
#endif

    private const string GameSceneName = "Game";

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
#if UNITY_SERVER
        appData = new ApplicationData();
#endif

        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("ApplicationController: Unity Services initialized.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ApplicationController: Failed to initialize Unity Services: {e.Message}");
            return;
        }

        await Launch();
    }

    private async Task Launch()
    {
#if UNITY_SERVER
        StartCoroutine(LoadGameSceneAsync());
        return;
#endif

        ClientSingleton clientSingleton = Instantiate(clientPrefab);
        bool authenticated = await clientSingleton.CreateClient();
        if (authenticated)
        {
            Debug.Log("ApplicationController: Client authenticated, loading main menu.");
            clientSingleton.GameManager.GoToMenu();
        }
        else
        {
            Debug.LogError("ApplicationController: Client authentication failed.");
        }
    }

#if UNITY_SERVER
    private IEnumerator LoadGameSceneAsync()
    {
        Application.targetFrameRate = 60;

        ServerSingleton serverSingleton = Instantiate(serverPrefab);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(GameSceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Task createServerTask = serverSingleton.CreateServer();
        yield return new WaitUntil(() => createServerTask.IsCompleted);

        Task startServerTask = serverSingleton.GameManager.StartGameServerAsync();
        yield return new WaitUntil(() => startServerTask.IsCompleted);

        Debug.Log("ApplicationController: Server started and game scene loaded.");
    }
#endif

    private void OnDestroy()
    {
    }
}