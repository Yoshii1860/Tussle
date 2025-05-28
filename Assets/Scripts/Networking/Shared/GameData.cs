using System;
using Newtonsoft.Json;

public enum Map
{
    Default = 0
}

public enum GameMode
{
    Default = 0
}

public enum GameQueue
{
    Solo = 0, 
    Team = 1
}

[Serializable]
public class UserData
{
    public string userName;
    public string userAuthId;
    public int characterId;
    public int teamIndex = -1;
    public GameInfo userGamePreferences = new GameInfo();
}

[Serializable]
public class GameInfo
{
    public Map map;
    public GameMode gameMode;
    public GameQueue gameQueue;

    public string ToMultiplayQueue()
    {
        return gameQueue switch
        {
            GameQueue.Solo => "solo-queue",
            GameQueue.Team => "team-queue",
            _ => "solo-queue"
        };
    }
}
