using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine.InputSystem;

public struct AirMoveState: ICharacterState
{
    public void OnStateEnter(CharacterStates previousState, ref CharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext, in KinematicCharacterProcessor processor)
    {
        ref Character character = ref processor.Character.ValueRW;
        
        processor.SetCapsuleGeometry(character.StandingGeometry.ToCapsuleGeometry());
    }

    public void OnStateExit(CharacterStates nextState, ref CharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext, in KinematicCharacterProcessor processor) { }

    public void PhysicsUpdate(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        in KinematicCharacterProcessor processor)
    {
        float deltaTime = baseContext.Time.DeltaTime;
        float elapsedTime = (float)baseContext.Time.ElapsedTime;
        
        ref Character character = ref processor.Character.ValueRW;
        ref CharacterControl characterControl = ref processor.CharacterControl.ValueRW;
        
        ref KinematicCharacterBody characterBody = ref processor.CharacterDataAccess.CharacterBody.ValueRW;

        CustomGravity customGravity = processor.CustomGravity.ValueRO;
        
        processor.HandlePhysicsUpdatePhaseOne(ref context, ref baseContext, allowParentHandling:true, allowGroundingDetection:true);

        float3 airAcceleration = characterControl.Move * character.AirAcceleration;

        if (math.lengthsq(airAcceleration) > 0)
        {
            float3 currentVelocity = characterBody.RelativeVelocity;
            CharacterControlUtilities.StandardAirMove(ref characterBody.RelativeVelocity, airAcceleration,
                character.AirMaxSpeed, characterBody.GroundingUp, deltaTime, forceNoMaxSpeedExcess: false);

            if (KinematicCharacterUtilities.MovementWouldHitNonGroundedObstruction(in processor,
                    ref context,
                    ref baseContext,
                    processor.CharacterDataAccess.CharacterProperties.ValueRO,
                    processor.CharacterDataAccess.LocalTransform.ValueRO,
                    processor.CharacterDataAccess.CharacterEntity,
                    processor.CharacterDataAccess.PhysicsCollider.ValueRO,
                    characterBody.RelativeVelocity * deltaTime,
                    out ColliderCastHit hit))
            {
                characterBody.RelativeVelocity = currentVelocity;

                character.HasDetectedMoveAgainstWall = true;
                character.LastKnownWallNormal = hit.SurfaceNormal;
            }
        }
        
        CharacterControlUtilities.AccelerateVelocity(ref characterBody.RelativeVelocity, customGravity.Gravity, deltaTime);
        CharacterControlUtilities.ApplyDragToVelocity(ref characterBody.RelativeVelocity, deltaTime, character.AirDrag);

        processor.HandlePhysicsUpdatePhaseTwo(ref context, ref baseContext, allowPreventGroundingFromFutureSlopeChange:true, 
            allowGroundingPushing:true, allowMovementAndDecollisions:true, allowMovingPlatformDetection:true, allowParentHandling:true);
        
        DetectTransition(ref context, ref baseContext, in processor);
    }

    public void VariableUpdate(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        in KinematicCharacterProcessor processor)
    {
        // Rotation is owned by CharacterYDegrees/ViewPitchDegrees, matching OnlineFPS prediction.
    }

    public (Entity cameraTarget, bool calculateUpFromGravity) GetCameraParameters(in Character character)
    {
        throw new System.NotImplementedException();
    }

    public float3 GetMoveFromInput(in PlayerInput input, quaternion cameraRotation)
    {
        throw new System.NotImplementedException();
    }

    private void DetectTransition(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        in KinematicCharacterProcessor processor)
    {
        ref Character character = ref processor.Character.ValueRW;
        ref CharacterControl characterControl = ref processor.CharacterControl.ValueRW;
        ref CharacterStateMachine stateMachine = ref processor.StateMachine.ValueRW;

        ref KinematicCharacterBody characterBody = ref processor.CharacterDataAccess.CharacterBody.ValueRW;

        if (characterBody.IsGrounded)
        {
            stateMachine.Transition(CharacterStates.GroundMove, ref context, ref baseContext, in processor);
            return;
        }

        processor.DetectGlobalTransition(ref context, ref baseContext);
    }
}
