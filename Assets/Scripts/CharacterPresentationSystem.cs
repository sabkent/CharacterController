using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct CharacterPresentationSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new CharacterCameraTargetRoll
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            CameraTargetLookup = SystemAPI.GetComponentLookup<CameraTarget>(true),
            LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false)
        }.Schedule(state.Dependency);
    }

    [WithAll(typeof(Simulate))]
    public partial struct CharacterCameraTargetRoll : IJobEntity
    {
        public float DeltaTime;
        [ReadOnly] public ComponentLookup<CameraTarget> CameraTargetLookup;
        public ComponentLookup<LocalTransform> LocalTransformLookup;

        void Execute(Entity entity, ref Character character, in KinematicCharacterBody characterBody)
        {
            if (LocalTransformLookup.TryGetComponent(entity, out LocalTransform characterTransform) &&
                LocalTransformLookup.TryGetComponent(character.CameraTarget, out LocalTransform cameraTargetTransform) &&
                CameraTargetLookup.TryGetComponent(character.CameraTarget, out CameraTarget cameraTarget))
            {
                var characterRight = MathUtilities.GetRightFromRotation(characterTransform.Rotation);
                var characterMaxSpeed = characterBody.IsGrounded ? character.GroundMaxSpeed : character.AirMaxSpeed;
                var characterLateralVelocity = math.projectsafe(characterBody.RelativeVelocity, characterRight);
                var characterLateralVelocityRatio =
                    math.clamp(math.length(characterLateralVelocity) / characterMaxSpeed, 0f, 1f);
                var isVelocityRight = math.dot(characterBody.RelativeVelocity, characterRight) > 0f;
                var targetTiltAngle = math.lerp(0f, character.ViewRollAmount, characterLateralVelocityRatio);

                targetTiltAngle = isVelocityRight ? -targetTiltAngle : targetTiltAngle;
                character.ViewRollDegrees = math.lerp(character.ViewRollDegrees, targetTiltAngle,
                    MathUtilities.GetSharpnessInterpolant(character.ViewRollSharpNess, DeltaTime));

                character.ViewLocalRotation =
                    CharacterUtilities.CalculateRotationFrom(character.ViewPitchDegrees, character.ViewRollDegrees);
                cameraTargetTransform.Rotation = character.ViewLocalRotation;
                LocalTransformLookup[character.CameraTarget] = cameraTargetTransform;
            }
        }
    }
}
