using Unity.Burst;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct CharacterRotationInterpolationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<LocalTransform, Character>().Build());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new CharacterRotationInterpolationJob
        {
            LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: false)
        }.Schedule(state.Dependency);
    }

    [WithNone(typeof(GhostOwnerIsLocal))]
    public partial struct CharacterRotationInterpolationJob : IJobEntity
    {
        public ComponentLookup<LocalTransform> LocalTransformLookup;
        
        private void Execute(Entity entity, in Character character)
        {
            if (LocalTransformLookup.TryGetComponent(entity, out LocalTransform transform))
            {
                CharacterUtilities.ComputeRotationFromYAngleAndUp(character.CharacterYDegrees, math.up(), out quaternion rotation);
                transform.Rotation = rotation;
                LocalTransformLookup[entity] = transform;
            }
        }
    }
}
