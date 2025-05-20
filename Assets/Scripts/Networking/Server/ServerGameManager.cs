using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Multiplay;
using UnityEngine.SceneManagement;

public class ServerGameManager : System.IDisposable
{
    private string serverIP;
    private int serverPort;
    private int queryPort;
    private NetworkServer networkServer;
    private MultiplayAllocationService multiplayAllocationService;

    private const string GameSceneName = "Game";

    private bool isLocalTest = true;

    public ServerGameManager(string serverIP, int serverPort, int queryPort, NetworkManager manager)
    {
        this.serverIP = serverIP;
        this.serverPort = serverPort;
        this.queryPort = queryPort;
        networkServer = new NetworkServer(manager);
        Debug.Log("ServerGameManager: NetworkServer created");
        isLocalTest = !Application.isBatchMode || serverIP == "127.0.0.1" || serverIP == "172.27.205.68";
    }

    public async Task StartGameServerAsync()
    {
        if (!isLocalTest)
        {
            try
            {
                multiplayAllocationService = new MultiplayAllocationService();
                // await multiplayAllocationService.BeginServerCheck();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to initialize MultiplayAllocationService: {e.Message}. Continuing without Multiplay for local testing.");
                multiplayAllocationService?.Dispose();
                multiplayAllocationService = null;
            }
        }
        else
        {
            Debug.Log("Local test detected. Skipping MultiplayAllocationService.");
        }

        if (!networkServer.OpenConnection(ApplicationData.IP(), serverPort))
        {
            Debug.LogError("Failed to open connection.");
            return;
        }

        Debug.Log("ServerGameManager: Loading Game scene on server startup");
        NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);

        // Wait for the Game scene to load
        while (SceneManager.GetActiveScene().name != GameSceneName)
        {
            await Task.Delay(100);
        }

        Debug.Log("ServerGameManager: Game scene loaded, waiting for clients");
        while (NetworkManager.Singleton.ConnectedClients.Count == 0)
        {
            await Task.Delay(1000);
        }

        Debug.Log("ServerGameManager: Client connected, server ready");
    }

    public void Dispose()
    {
        multiplayAllocationService?.Dispose();
        networkServer?.Dispose();
    }
}