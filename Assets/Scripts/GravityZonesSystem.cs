using Unity.Burst;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(CharacterVariableUpdateSystem))]
public partial class GravityZonesSystem : SystemBase
{
    protected override void OnUpdate()
    {
        World.GetOrCreateSystem<TransformSystemGroup>().Update(World.Unmanaged);

        new ResetGravitysJob().Schedule();

        if (SystemAPI.TryGetSingleton(out GlobalGravityZone globalGravityZone))
        {
            new GlobalGravityJob
            {
                GlobalGravityZone = globalGravityZone
            }.Schedule();
        }

        new ApplyGravityJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        }.Schedule();
    }
}


[BurstCompile]
public partial struct ResetGravitysJob : IJobEntity
{
    private void Execute(Entity entity, ref CustomGravity customGravity)
    {
        customGravity.LastZoneEntity = customGravity.CurrentZoneEntity;
        customGravity.TouchedByNonGlobalGravity = false;
    }
}

[BurstCompile]
public partial struct GlobalGravityJob : IJobEntity
{
    public GlobalGravityZone GlobalGravityZone;

    private void Execute(Entity entity, ref CustomGravity customGravity)
    {
        if (!customGravity.TouchedByNonGlobalGravity)
        {
            customGravity.Gravity = GlobalGravityZone.Gravity * customGravity.GravityMultiplier;
            customGravity.CurrentZoneEntity = Entity.Null;
        }
    }
}

[BurstCompile]
public partial struct ApplyGravityJob : IJobEntity
{
    public float DeltaTime;
    
    private void Execute(Entity entity, ref PhysicsVelocity physicsVelocity, in PhysicsMass physicsMass,
        in CustomGravity customGravity)
    {
        if(physicsMass.InverseMass > 0)
            CharacterControlUtilities.AccelerateVelocity(ref physicsVelocity.Linear, customGravity.Gravity, DeltaTime);
    }

}