using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.CharacterController;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[UpdateAfter(typeof(PredictedFixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PlayerMoveSystem))]
//[UpdateAfter(typeof(PlayerVariableStepControlSystem))]
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

        // Only write visual rotation on the final prediction tick to avoid visible jitter during resimulation
        var netTime = SystemAPI.GetSingleton<NetworkTime>();
        _context.WriteVisualRotation = netTime.IsFinalPredictionTick;

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
        
        public void Execute(CharacterAspect characterAspect)
        {
            characterAspect.VariableUpdate(ref Context, ref BaseContext);
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
