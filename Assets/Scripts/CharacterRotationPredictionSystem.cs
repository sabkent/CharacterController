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
        // Intentionally left blank.
        // Rotation is authored in one place only:
        // - Predicted/local ghosts: via CharacterAspect.VariableUpdate() in CharacterVariableUpdateSystem
        // - Interpolated/remote ghosts: via CharacterRotationInterpolationSystem
        // Having this system also write LocalTransform.Rotation causes double-writes per frame and visible jitter.
    }
}
