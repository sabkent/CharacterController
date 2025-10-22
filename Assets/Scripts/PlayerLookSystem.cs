using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
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

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }

    public partial struct PlayerLookJob : IJobEntity
    {
        public ComponentLookup<CharacterControl> CharacterControlLookup;
        
        void Execute(ref PlayerCommands commands, ref PlayerNetworkInput networkInput, 
            in Player player, in CommandDataInterpolationDelay interpolationDelay)
        {
            var lookYawPitchDegreeDelta =
                GetInputDelta(commands.LookYawPitchDegree, networkInput.LastProcessedLookYawPitchDegrees);
            
            networkInput.LastProcessedLookYawPitchDegrees = commands.LookYawPitchDegree;

            if (CharacterControlLookup.HasComponent(player.ControlledCharacter))
            {
                var characterControl = CharacterControlLookup[player.ControlledCharacter];
                characterControl.LookYawPitchDegreeDelta = lookYawPitchDegreeDelta;
                CharacterControlLookup[player.ControlledCharacter] = characterControl;
            }
        }
        
        
        public static float2 GetInputDelta(float2 currentValue, float2 previousValue)
        {
            float InputWrapAroundValue = 3000f;
            float2 delta = currentValue - previousValue;

            // When delta is very large, consider that the input has wrapped around
            if (math.abs(delta.x) > (InputWrapAroundValue * 0.5f))
            {
                delta.x += (math.sign(previousValue.x - currentValue.x) * InputWrapAroundValue);
            }

            if (math.abs(delta.y) > (InputWrapAroundValue * 0.5f))
            {
                delta.y += (math.sign(previousValue.y - currentValue.y) * InputWrapAroundValue);
            }

            return delta;
        }
    }
}
