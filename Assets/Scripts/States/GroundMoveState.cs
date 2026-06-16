
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.InputSystem;

public struct GroundMoveState:ICharacterState
{
    public void OnStateEnter(CharacterStates previousState, ref CharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext, in KinematicCharacterProcessor processor)
    {
        ref Character character = ref processor.Character.ValueRW;

        processor.SetCapsuleGeometry(character.StandingGeometry.ToCapsuleGeometry());
    }

    public void OnStateExit(CharacterStates nextState, ref CharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext, in KinematicCharacterProcessor processor)
    {
        ref Character character = ref processor.Character.ValueRW;

        //character.IsOnStickySurface = false;
        //character.IsSprinting = false;
    }

    public void PhysicsUpdate(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        in KinematicCharacterProcessor processor)
    {
        ref Character character = ref processor.Character.ValueRW;
        ref CharacterControl characterControl = ref processor.CharacterControl.ValueRW;

        ref KinematicCharacterBody characterBody = ref processor.CharacterDataAccess.CharacterBody.ValueRW;

        float deltaTime = baseContext.Time.DeltaTime;
        float elapsedTime = (float)baseContext.Time.ElapsedTime;

        processor.HandlePhysicsUpdatePhaseOne(ref context, ref baseContext, allowParentHandling:true, allowGroundingDetection:true);

        if (characterBody.ParentEntity != Entity.Null)
        {
            characterControl.Move = math.rotate(characterBody.RotationFromParent, characterControl.Move);
            characterBody.RelativeVelocity =
                math.rotate(characterBody.RotationFromParent, characterBody.RelativeVelocity);
        }

        if (characterBody.IsGrounded)
        {
            //character.IsSprinting = characterControl.SprintHeld;
            float speed = character.GroundMaxSpeed;

            float3 moveVectorOnPlane = math.normalizesafe(MathUtilities.ProjectOnPlane(characterControl.Move, characterBody.GroundingUp))
                * math.length(characterControl.Move);
            float3 velocity = moveVectorOnPlane * speed;
            CharacterControlUtilities.StandardGroundMove_Interpolated(ref characterBody.RelativeVelocity, velocity, character.GroundMovementSharpness,
                deltaTime, characterBody.GroundingUp, characterBody.GroundHit.Normal);

            if (characterControl.Jump)
            {
                CharacterControlUtilities.StandardJump(ref characterBody, characterBody.GroundingUp * character.GroundJumpSpeed,
                    cancelVelocityBeforeJump: true, characterBody.GroundingUp);
            }
        }

        processor.HandlePhysicsUpdatePhaseTwo(ref context, ref baseContext,
            allowPreventGroundingFromFutureSlopeChange: true, allowGroundingPushing: true,
            allowMovementAndDecollisions: true, allowMovingPlatformDetection: true, allowParentHandling: true);

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
        ref KinematicCharacterBody characterBody = ref processor.CharacterDataAccess.CharacterBody.ValueRW;
        ref CharacterStateMachine stateMachine = ref processor.StateMachine.ValueRW;

        if (!characterBody.IsGrounded)
        {
            stateMachine.Transition(CharacterStates.AirMove, ref context, ref baseContext, in processor);
            return;
        }

        processor.DetectGlobalTransition(ref context, ref baseContext);
    }
}
