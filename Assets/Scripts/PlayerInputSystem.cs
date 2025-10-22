
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct PlayerInputSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Player, PlayerCommands>().Build());
        state.RequireForUpdate<NetworkTime>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        var elapsedTime = (float)SystemAPI.Time.ElapsedTime;

        InputActions actions = GameInput.InputActions;   
        float LookSensitivity = 2f;
        
        foreach (var (commands, player, entity) in SystemAPI.Query<RefRW<PlayerCommands>, RefRW<Player>>()
                     .WithAll<GhostOwnerIsLocal>()
                     .WithEntityAccess())
        {
            commands.ValueRW.MoveInput = Vector2.ClampMagnitude(actions.Player.Move.ReadValue<Vector2>(), 1f);
            var lookDelta = (float2)actions.Player.LookDelta.ReadValue<Vector2>();
            
            var lookYawPitch = math.fmod(commands.ValueRO.LookYawPitchDegree + (lookDelta * LookSensitivity), GameInput.InputWrapAround);

            commands.ValueRW.LookYawPitchDegree = lookYawPitch;
            
            commands.ValueRW.JumpPressed = default;
            if(actions.Player.Jump.WasPressedThisFrame())
                commands.ValueRW.JumpPressed.Set();
        }
    }
}
