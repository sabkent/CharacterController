using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Scenes;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

public class GameSession
{
    public static GameSession CreateClientServer()
    {
        var session = new GameSession();

        var clientWorld = session.CreateClientWorld("127.0.0.1", 7676, "bob");
        var serverWorld = session.CreateServerWorld();

        session._worlds.Add(clientWorld);
        session._worlds.Add(serverWorld);

        return session;
    }

    private List<World> _worlds = new List<World>();

    public void LoadIntoWorlds(WeakObjectSceneReference sceneReference)
    {
        LoadIntoWorlds(sceneReference.Id.GlobalId.AssetGUID);
    }

    public void LoadIntoWorlds(Hash128 sceneGuid)
    {
        if (!sceneGuid.IsValid)
        {
            return;
        }

        foreach (var world in _worlds.Where(world=>world.IsCreated))
        {
            SceneSystem.LoadSceneAsync(world.Unmanaged, sceneGuid, new SceneSystem.LoadParameters
            {
                Flags = SceneLoadFlags.BlockOnImport,
            });
        }
    }


    private World CreateClientWorld(string ip, ushort port, string playerName)
    {
        var world = ClientServerBootstrap.CreateClientWorld("ClientWorld");

        if (NetworkEndpoint.TryParse(ip, port, out NetworkEndpoint endpoint))
        {

        }

        return world;
    }

    private World CreateServerWorld()
    {
        var world = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        return world;
    }
}
