using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

public struct GoInGameRequest : IRpcCommand
{
    
}

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation| WorldSystemFilterFlags.ThinClientSimulation)]
partial struct GoInGameClient : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var commandBuffer = new EntityCommandBuffer(state.WorldUpdateAllocator);
        
        foreach (var (networkId, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithNone<NetworkStreamInGame>()
                     .WithEntityAccess())
        {
            commandBuffer.AddComponent<NetworkStreamInGame>(entity);

            var rpcEntity = commandBuffer.CreateEntity();
            commandBuffer.AddComponent(rpcEntity, new GoInGameRequest
            {
                
            });
            commandBuffer.AddComponent<SendRpcCommandRequest>(rpcEntity);
        }

        commandBuffer.Playback(state.EntityManager);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
partial struct GoInGameServer : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GameResource>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var commandBuffer = new EntityCommandBuffer(state.WorldUpdateAllocator);

        var gameResource = SystemAPI.GetSingleton<GameResource>();

        foreach (var (rpcCommand, goInGame, entity) in SystemAPI
                     .Query<RefRO<ReceiveRpcCommandRequest>, RefRO<GoInGameRequest>>()
                     .WithEntityAccess())
        {
            var connection = rpcCommand.ValueRO.SourceConnection;
            var networkId = SystemAPI.GetComponent<NetworkId>(connection);
            
            if(!SystemAPI.HasComponent<NetworkStreamInGame>(connection))
                commandBuffer.AddComponent<NetworkStreamInGame>(connection);

            var player = commandBuffer.Instantiate(gameResource.Player);
            var character = commandBuffer.Instantiate(gameResource.Character);
            
            commandBuffer.SetComponent(player, new GhostOwner { NetworkId = networkId.Value });
            commandBuffer.SetComponent(character, new GhostOwner{NetworkId = networkId.Value});
            
            commandBuffer.AddComponent(player, new Player
            {
                ControlledCharacter = character
            });
            
            commandBuffer.DestroyEntity(entity);
        }
        
        commandBuffer.Playback(state.EntityManager);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
