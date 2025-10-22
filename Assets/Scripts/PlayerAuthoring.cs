using System;
using UnityEngine;
using Unity.Collections;
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

public struct PlayerNetworkInput : IComponentData
{
    public float2 LastProcessedLookYawPitchDegrees;
}

[Serializable]
public struct PlayerCommands : IInputComponentData
{
    public float2 MoveInput;
    public float2 LookYawPitchDegree;
    public InputEvent JumpPressed;
}
