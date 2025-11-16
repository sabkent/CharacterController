using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.CharacterController;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using Unity.Collections;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[UpdateAfter(typeof(PredictedFixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PlayerMoveSystem))]
[UpdateAfter(typeof(CharacterRotationPredictionSystem))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation |
                   WorldSystemFilterFlags.ServerSimulation)]
public partial struct CharacterVariableUpdateSystem : ISystem
{
    private EntityQuery _characterQuery;
    private CharacterUpdateContext _context;
    private KinematicCharacterUpdateContext _baseContext;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
        _characterQuery = KinematicCharacterUtilities.GetBaseCharacterQueryBuilder()
            .WithAll<Character, CharacterControl>()
            .Build(ref state);

        _context = new CharacterUpdateContext();
        _baseContext = new KinematicCharacterUpdateContext();
        _baseContext.OnSystemCreate(ref state);
        
        state.RequireForUpdate(_characterQuery);
        state.RequireForUpdate<NetworkTime>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _baseContext.OnSystemUpdate(ref state, SystemAPI.Time, SystemAPI.GetSingleton<PhysicsWorldSingleton>());
        
        state.Dependency = new CharacterVariableUpdateJob
        {
            Context = _context,
            BaseContext = _baseContext
        }.Schedule(state.Dependency);
    }

    [WithAll(typeof(Simulate))]
    public partial struct CharacterVariableUpdateJob : IJobEntity, IJobEntityChunkBeginEnd
    {
        public CharacterUpdateContext Context;
        public KinematicCharacterUpdateContext BaseContext;
        
        public void Execute(Entity entity,
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
            var characterProcessor = new KinematicCharacterProcessor()
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
                CharacterControl = characterControl
            };

            characterProcessor.VariableUpdate(ref Context, ref BaseContext);
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

    public partial struct CameraTargetJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<Character> CharacterLookup;

        void Execute(ref LocalTransform transform, in CameraTarget cameraTarget)
        {
            if (CharacterLookup.TryGetComponent(cameraTarget.Character, out Character character))
            {
                transform.Rotation = character.ViewLocalRotation;
            }
        }
    }
}
