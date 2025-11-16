using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[UpdateBefore(typeof(CharacterVariableUpdateSystem))]
[UpdateAfter(typeof(CharacterRotationPredictionSystem))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
partial struct PlayerLookSystem : ISystem
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
        state.Dependency = new PlayerLookJob
        {
            CharacterControlLookup = SystemAPI.GetComponentLookup<CharacterControl>(isReadOnly:false)
        }.Schedule(state.Dependency);
    }

    [WithAll(typeof(Simulate))]
    public partial struct PlayerLookJob : IJobEntity
    {
        public ComponentLookup<CharacterControl> CharacterControlLookup;
        
        void Execute(ref PlayerCommands commands, ref PlayerNetworkInput networkInput, 
            in Player player, in CommandDataInterpolationDelay interpolationDelay)
        {
            var lookYawPitchDegreeDelta =
                InputDeltaUtilities.GetInputDelta(commands.LookYawPitchDegrees,
                    networkInput.LastProcessedLookYawPitchDegrees);
            
            networkInput.LastProcessedLookYawPitchDegrees = commands.LookYawPitchDegrees;

            if (CharacterControlLookup.HasComponent(player.ControlledCharacter))
            {
                var characterControl = CharacterControlLookup[player.ControlledCharacter];
                characterControl.LookYawPitchDegreesDelta = lookYawPitchDegreeDelta;
                CharacterControlLookup[player.ControlledCharacter] = characterControl;
            }
        }
    }
}
