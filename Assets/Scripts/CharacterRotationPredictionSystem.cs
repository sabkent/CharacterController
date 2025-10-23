using Unity.Burst;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderFirst = true)]
[UpdateBefore(typeof(PredictedFixedStepSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation |
                   WorldSystemFilterFlags.ServerSimulation)]
partial struct CharacterRotationPredictionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<LocalTransform, Character>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new CharacterPredictedRotationJob().Schedule(state.Dependency);
    }

    [WithAll(typeof(Simulate))]
    public partial struct CharacterPredictedRotationJob : IJobEntity
    {
        void Execute(ref LocalTransform localTransform, in Character character)
        {
            CharacterUtilities.ComputeRotationFromYAngleAndUp(character.CharacterYDegrees, math.up(), out quaternion rotation);
            localTransform.Rotation = rotation;
        }
    }
}
