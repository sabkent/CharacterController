using System;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public class PlayerAuthoring : MonoBehaviour
{
    private class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new Player
            {

            });
            AddComponent(entity, new PlayerNetworkInput());
            AddComponent<PlayerCommands>(entity);
        }
    }
}

[GhostComponent]
public struct Player : IComponentData
{
    [GhostField] public Entity ControlledCharacter;
}

[GhostComponent(SendTypeOptimization = GhostSendType.OnlyPredictedClients)]
public struct PlayerNetworkInput : IComponentData
{
    [GhostField] public float2 LastProcessedLookYawPitchDegrees;
}

[Serializable]
public struct PlayerCommands : IInputComponentData
{
    public float2 MoveInput;
    public float2 LookYawPitchDegrees;
    public InputEvent JumpPressed;
    
}
