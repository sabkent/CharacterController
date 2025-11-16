using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.CharacterController;
using UnityEngine;

[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup), OrderFirst = true)]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
partial struct PlayerMoveSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Player, PlayerCommands>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new PlayerMoveJob
        {
            LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly:true),
            CharacterControlLookup = SystemAPI.GetComponentLookup<CharacterControl>(isReadOnly:false)
        }.Schedule(state.Dependency);
    }

    [WithAll(typeof(Simulate))]
    public partial struct PlayerMoveJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
        public ComponentLookup<CharacterControl> CharacterControlLookup;

        void Execute(in PlayerCommands commands, in Player player)
        {
            if (CharacterControlLookup.HasComponent(player.ControlledCharacter))
            {
                var characterControl = CharacterControlLookup[player.ControlledCharacter];
                var characterRotation = LocalTransformLookup[player.ControlledCharacter].Rotation;

                var forward = math.mul(characterRotation, math.forward());
                var right = math.mul(characterRotation, math.right());
                var move = (commands.MoveInput.y * forward) + (commands.MoveInput.x * right);
                
                characterControl.Move = MathUtilities.ClampToMaxLength(move, 1f);
                characterControl.Jump = commands.JumpPressed.IsSet;

                CharacterControlLookup[player.ControlledCharacter] = characterControl;
            }
        }
    }
}
