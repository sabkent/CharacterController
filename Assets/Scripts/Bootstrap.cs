using Unity.NetCode;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class Bootstrap : ClientServerBootstrap
{
    public static ClientServerBootstrap Instance { get; private set; }

    public override bool Initialize(string defaultWorldName)
    {
        AutoConnectPort = 7979;
        Instance = this;
        
        InitializeGameManager();
        return true;
        //return base.Initialize(defaultWorldName);
    }

    private void InitializeGameManager()
    {
        var prefab = Resources.Load<GameManager>(ResourceNames.GameManager);
        var gameManager = GameObject.Instantiate<GameManager>(prefab);
        gameManager.Initialize();
    }
}


public static class ResourceNames
{
    public const string GameManager = nameof(GameManager);
}