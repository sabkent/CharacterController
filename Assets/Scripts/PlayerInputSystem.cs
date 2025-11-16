
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
        float LookSensitivity = 5f;
        
        foreach (var (commands, player, entity) in SystemAPI
                     .Query<RefRW<PlayerCommands>, RefRW<Player>>()
                     .WithAll<GhostOwnerIsLocal>()
                     .WithEntityAccess())
        {
            commands.ValueRW.MoveInput = Vector2.ClampMagnitude(actions.Player.Move.ReadValue<Vector2>(), 1f);
            
            var lookDelta = (float2)actions.Player.LookDelta.ReadValue<Vector2>();
            var lookConst = (float2)actions.Player.LookConst.ReadValue<Vector2>();

            if (math.lengthsq(lookConst) > math.lengthsq(lookDelta))
            {
                InputDeltaUtilities.AddInputDelta(ref commands.ValueRW.LookYawPitchDegrees,  lookConst * deltaTime * LookSensitivity);
            }
            else
            {
                InputDeltaUtilities.AddInputDelta(ref commands.ValueRW.LookYawPitchDegrees, lookDelta * LookSensitivity);
            }
            
            //Debug.Log($"delta: {lookDelta} const:{lookConst} yawPitch:{commands.ValueRW.LookYawPitchDegree}");
            
            
            commands.ValueRW.JumpPressed = default;
            if(actions.Player.Jump.WasPressedThisFrame())
                commands.ValueRW.JumpPressed.Set();
        }
    }
}
