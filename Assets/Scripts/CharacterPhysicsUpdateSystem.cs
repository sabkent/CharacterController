using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.CharacterController;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

[UpdateInGroup(typeof(KinematicCharacterPhysicsUpdateGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation |
                   WorldSystemFilterFlags.ServerSimulation)]
partial struct CharacterPhysicsUpdateSystem : ISystem
{
    private EntityQuery _characterQuery;
    private CharacterUpdateContext _context;
    private KinematicCharacterUpdateContext _baseContext;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _characterQuery = KinematicCharacterUtilities.GetBaseCharacterQueryBuilder()
            .WithAll<Character, CharacterControl>()
            .Build(ref state);

        _context = new CharacterUpdateContext();
        _baseContext = new KinematicCharacterUpdateContext();
        _baseContext.OnSystemCreate(ref state);
        
        state.RequireForUpdate(_characterQuery);
        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<NetworkTime>()) return;
        
        _baseContext.OnSystemUpdate(ref state, SystemAPI.Time, SystemAPI.GetSingleton<PhysicsWorldSingleton>());
        state.Dependency = new CharacterKinematicPhysicsJob
        {
            Context = _context,
            BaseContext = _baseContext
        }.Schedule(state.Dependency);
    }

    [WithAll(typeof(Simulate))]
    public partial struct CharacterKinematicPhysicsJob : IJobEntity, IJobEntityChunkBeginEnd
    {
        public CharacterUpdateContext Context;
        public KinematicCharacterUpdateContext BaseContext;
        
        void Execute(Entity entity,
            RefRW<LocalTransform> localTransform,
            RefRW<KinematicCharacterProperties> characterProperties,
            RefRW<KinematicCharacterBody> characterBody,
            RefRW<PhysicsCollider> physicsCollider,
            RefRW<Character> characterComponent,
            RefRW<CharacterControl> characterControl,
            DynamicBuffer<KinematicCharacterHit> characterHitsBuffer,
            DynamicBuffer<StatefulKinematicCharacterHit> statefulHitsBuffer,
            DynamicBuffer<KinematicCharacterDeferredImpulse> deferredImpulsesBuffer,
            DynamicBuffer<KinematicVelocityProjectionHit> velocityProjectionHits)
        {
            var characterProcessor = new KinematicCharacterProcessor
            {
                CharacterDataAccess = new KinematicCharacterDataAccess(

                    entity,
                    localTransform,
                    characterProperties,
                    characterBody,
                    physicsCollider,
                    characterHitsBuffer,
                    statefulHitsBuffer,
                    deferredImpulsesBuffer,
                    velocityProjectionHits
                ),
                Character = characterComponent,
                CharacterControl = characterControl,
            };

            characterProcessor.PhysicsUpdate(ref Context, ref BaseContext);
        }

        public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            BaseContext.EnsureCreationOfTmpCollections();
            return true;
        }

        public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask,
            bool chunkWasExecuted)
        {
        }
    }
}
