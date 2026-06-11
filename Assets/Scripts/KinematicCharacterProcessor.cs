
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using CapsuleCollider = Unity.Physics.CapsuleCollider;

public struct KinematicCharacterProcessor: IKinematicCharacterProcessor<CharacterUpdateContext>
{
    public KinematicCharacterDataAccess CharacterDataAccess;
    public RefRW<Character> Character;
    public RefRW<CharacterControl> CharacterControl;
    public RefRW<CharacterStateMachine> StateMachine;
    public RefRW<CustomGravity> CustomGravity;

    public void PhysicsUpdate(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext)
    {
        ref Character character = ref Character.ValueRW;
        ref CharacterControl characterControl = ref CharacterControl.ValueRW;
        ref CharacterStateMachine stateMachine = ref StateMachine.ValueRW;
        ref KinematicCharacterBody characterBody = ref CharacterDataAccess.CharacterBody.ValueRW;
        
        if(stateMachine.CurrentState == CharacterStates.Uninitialized)
            stateMachine.Transition(CharacterStates.AirMove, ref context, ref baseContext, in this);
        
        character.HasDetectedMoveAgainstWall = false;
        
        stateMachine.PhysicsUpdate(ref context, ref baseContext, in this);
        
        character.JumpPressedBeforeBecameGrounded = false;
    }

    public void VariableUpdate(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext)
    {
        ref KinematicCharacterBody characterBody = ref CharacterDataAccess.CharacterBody.ValueRW;
        ref CharacterStateMachine stateMachine = ref StateMachine.ValueRW;
        ref quaternion characterRotation = ref CharacterDataAccess.LocalTransform.ValueRW.Rotation;

        KinematicCharacterUtilities.AddVariableRateRotationFromFixedRateRotation(ref characterRotation,
            characterBody.RotationFromParent, baseContext.Time.DeltaTime, characterBody.LastPhysicsUpdateDeltaTime);
        stateMachine.VariableUpdate(ref context, ref baseContext, in this);
    }

    public void HandlePhysicsUpdatePhaseOne(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        bool allowParentHandling, bool allowGroundingDetection)
    {
        ref KinematicCharacterBody characterBody = ref CharacterDataAccess.CharacterBody.ValueRW;
        ref float3 characterPosition = ref CharacterDataAccess.LocalTransform.ValueRW.Position;
        
        KinematicCharacterUtilities.Update_Initialize(in this, ref context, ref baseContext, 
            ref characterBody,
            CharacterDataAccess.CharacterHitsBuffer,
            CharacterDataAccess.DeferredImpulsesBuffer, 
            CharacterDataAccess.VelocityProjectionHits,
            baseContext.Time.DeltaTime);

        if (allowParentHandling)
        {
            KinematicCharacterUtilities.Update_ParentMovement(in this, ref context, ref baseContext, 
                CharacterDataAccess.CharacterEntity,
                ref characterBody,
                CharacterDataAccess.CharacterProperties.ValueRO,
                CharacterDataAccess.PhysicsCollider.ValueRO,
                CharacterDataAccess.LocalTransform.ValueRO,
                ref characterPosition,
                characterBody.WasGroundedBeforeCharacterUpdate);
        }

        if (allowGroundingDetection)
        {
            KinematicCharacterUtilities.Update_Grounding(in this, ref context, ref baseContext,
                ref characterBody,
                CharacterDataAccess.CharacterEntity,
                CharacterDataAccess.CharacterProperties.ValueRO,
                CharacterDataAccess.PhysicsCollider.ValueRO,
                CharacterDataAccess.LocalTransform.ValueRO,
                CharacterDataAccess.VelocityProjectionHits,
                CharacterDataAccess.CharacterHitsBuffer,
                ref characterPosition);
        }
    }
    
    public void HandlePhysicsUpdatePhaseTwo(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        bool allowPreventGroundingFromFutureSlopeChange,
        bool allowGroundingPushing,
        bool allowMovementAndDecollisions,
        bool allowMovingPlatformDetection,
        bool allowParentHandling)
    {
        ref Character character = ref Character.ValueRW;
        ref KinematicCharacterBody characterBody = ref CharacterDataAccess.CharacterBody.ValueRW;
        ref float3 characterPosition = ref CharacterDataAccess.LocalTransform.ValueRW.Position;
        CustomGravity customGravity = CustomGravity.ValueRO;

        if (allowPreventGroundingFromFutureSlopeChange)
        {
            KinematicCharacterUtilities.Update_PreventGroundingFromFutureSlopeChange(
                in this,
                ref context,
                ref baseContext,
                CharacterDataAccess.CharacterEntity,
                ref characterBody,
                CharacterDataAccess.CharacterProperties.ValueRO,
                CharacterDataAccess.PhysicsCollider.ValueRO,
                in character.StepAndSlopeHandling);
        }
        if (allowGroundingPushing)
        {
            KinematicCharacterUtilities.Update_GroundPushing(
                in this,
                ref context,
                ref baseContext,
                ref characterBody,
                CharacterDataAccess.CharacterProperties.ValueRO,
                CharacterDataAccess.LocalTransform.ValueRO,
                CharacterDataAccess.DeferredImpulsesBuffer,
                customGravity.Gravity); 
        }
        if (allowMovementAndDecollisions)
        {
            KinematicCharacterUtilities.Update_MovementAndDecollisions(
                in this,
                ref context,
                ref baseContext,
                CharacterDataAccess.CharacterEntity,
                ref characterBody,
                CharacterDataAccess.CharacterProperties.ValueRO,
                CharacterDataAccess.PhysicsCollider.ValueRO,
                CharacterDataAccess.LocalTransform.ValueRO,
                CharacterDataAccess.VelocityProjectionHits,
                CharacterDataAccess.CharacterHitsBuffer,
                CharacterDataAccess.DeferredImpulsesBuffer,
                ref characterPosition);
        }
        if (allowMovingPlatformDetection)
        {
            KinematicCharacterUtilities.Update_MovingPlatformDetection(
                ref baseContext,
                ref characterBody);
        }
        if (allowParentHandling)
        {
            KinematicCharacterUtilities.Update_ParentMomentum(
                ref baseContext,
                ref characterBody,
                CharacterDataAccess.LocalTransform.ValueRO.Position);
        }

        KinematicCharacterUtilities.Update_ProcessStatefulCharacterHits(CharacterDataAccess.CharacterHitsBuffer,
            CharacterDataAccess.StatefulHitsBuffer);
    }
    
    private void HandleVelocityControl(ref CharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext)
    {
        var deltaTime = baseContext.Time.DeltaTime;

        ref KinematicCharacterBody characterBody = ref CharacterDataAccess.CharacterBody.ValueRW;
        ref Character character = ref Character.ValueRW;
        ref CharacterControl characterControl = ref CharacterControl.ValueRW;

        if (characterBody.ParentEntity != Entity.Null)
        {
            characterControl.Move = math.rotate(characterBody.RotationFromParent, characterControl.Move);
            characterBody.RelativeVelocity = math.rotate(characterBody.RotationFromParent, characterBody.RelativeVelocity);
        }

        if (characterBody.IsGrounded)
        {
            var targetVelocity = characterControl.Move * character.GroundMaxSpeed;
            CharacterControlUtilities.StandardGroundMove_Interpolated(ref characterBody.RelativeVelocity,
                targetVelocity, character.GroundMovementSharpness, deltaTime, characterBody.GroundingUp,
                characterBody.GroundHit.Normal);

            if (characterControl.Jump)
            {
                CharacterControlUtilities.StandardJump(ref characterBody, characterBody.GroundingUp * character.JumpSpeed, cancelVelocityBeforeJump: true, characterBody.GroundingUp);
            }
        }
        else
        {
            // Move in air
            var airAcceleration = characterControl.Move * character.AirAcceleration;
            if (math.lengthsq(airAcceleration) > 0f)
            {
                var tmpVelocity = characterBody.RelativeVelocity;
                CharacterControlUtilities.StandardAirMove(ref characterBody.RelativeVelocity, airAcceleration,
                    character.AirMaxSpeed, characterBody.GroundingUp, deltaTime, false);

                if (character.PreventAirAccelerationAgainstUngroundedHits &&
                    KinematicCharacterUtilities.MovementWouldHitNonGroundedObstruction(
                        in this,
                        ref context,
                        ref baseContext,
                        CharacterDataAccess.CharacterProperties.ValueRO,
                        CharacterDataAccess.LocalTransform.ValueRO,
                        CharacterDataAccess.CharacterEntity,
                        CharacterDataAccess.PhysicsCollider.ValueRO,
                        characterBody.RelativeVelocity * deltaTime,
                        out ColliderCastHit hit))
                {
                    characterBody.RelativeVelocity = tmpVelocity;
                }
            }

            // Gravity
            CharacterControlUtilities.AccelerateVelocity(ref characterBody.RelativeVelocity,
                character.Gravity, deltaTime);

            // Drag
            CharacterControlUtilities.ApplyDragToVelocity(ref characterBody.RelativeVelocity, deltaTime,
                character.AirDrag);
        }
    }


    public bool DetectGlobalTransition(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext)
    {
        return false;
    }

    public unsafe void SetCapsuleGeometry(CapsuleGeometry capsuleGeometry)
    {
        ref PhysicsCollider physicsCollider = ref CharacterDataAccess.PhysicsCollider.ValueRW;
        
        if (!physicsCollider.IsValid)
        {
            Debug.LogError("Character PhysicsCollider is invalid");
            return;
        }

        if (physicsCollider.ColliderPtr->Type != ColliderType.Capsule)
        {
            Debug.LogError($"Expected Capsule collider, got {physicsCollider.ColliderPtr->Type}");
            return;
        }

        CapsuleCollider* capsuleCollider = (CapsuleCollider*)physicsCollider.ColliderPtr;
        capsuleCollider->Geometry = capsuleGeometry;
    }
    
    
    #region IKinematicCharacterProcessor
    public void UpdateGroundingUp(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext)
    {
        ref KinematicCharacterBody characterBody = ref CharacterDataAccess.CharacterBody.ValueRW;

        KinematicCharacterUtilities.Default_UpdateGroundingUp(
            ref characterBody,
            CharacterDataAccess.LocalTransform.ValueRO.Rotation);
    }

    public bool CanCollideWithHit(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        in BasicHit hit) => PhysicsUtilities.IsCollidable(hit.Material);

    public bool IsGroundedOnHit(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        in BasicHit hit, int groundingEvaluationType) =>
        KinematicCharacterUtilities.Default_IsGroundedOnHit(
            in this,
            ref context,
            ref baseContext,
            CharacterDataAccess.CharacterEntity,
            CharacterDataAccess.PhysicsCollider.ValueRO,
            CharacterDataAccess.CharacterBody.ValueRO,
            CharacterDataAccess.CharacterProperties.ValueRO,
            in hit,
            in Character.ValueRO.StepAndSlopeHandling,
            groundingEvaluationType);
    

    public void OnMovementHit(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        ref KinematicCharacterHit hit, ref float3 remainingMovementDirection, ref float remainingMovementLength,
        float3 originalVelocityDirection, float hitDistance)
    {
        ref KinematicCharacterBody characterBody = ref CharacterDataAccess.CharacterBody.ValueRW;
        ref float3 characterPosition = ref CharacterDataAccess.LocalTransform.ValueRW.Position;
        Character character = Character.ValueRO;

        KinematicCharacterUtilities.Default_OnMovementHit(
            in this,
            ref context,
            ref baseContext,
            ref characterBody,
            CharacterDataAccess.CharacterEntity,
            CharacterDataAccess.CharacterProperties.ValueRO,
            CharacterDataAccess.PhysicsCollider.ValueRO,
            CharacterDataAccess.LocalTransform.ValueRO,
            ref characterPosition,
            CharacterDataAccess.VelocityProjectionHits,
            ref hit,
            ref remainingMovementDirection,
            ref remainingMovementLength,
            originalVelocityDirection,
            hitDistance,
            character.StepAndSlopeHandling.StepHandling,
            character.StepAndSlopeHandling.MaxStepHeight,
            character.StepAndSlopeHandling.CharacterWidthForStepGroundingCheck);
    }

    public void ProjectVelocityOnHits(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        ref float3 velocity, ref bool characterIsGrounded, ref BasicHit characterGroundHit,
        in DynamicBuffer<KinematicVelocityProjectionHit> velocityProjectionHits, float3 originalVelocityDirection) => 
            KinematicCharacterUtilities.Default_ProjectVelocityOnHits(
                ref velocity,
                ref characterIsGrounded,
                ref characterGroundHit,
                in velocityProjectionHits,
                originalVelocityDirection,
                Character.ValueRO.StepAndSlopeHandling.ConstrainVelocityToGroundPlane,
                in CharacterDataAccess.CharacterBody.ValueRO);

    public void OverrideDynamicHitMasses(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        ref PhysicsMass characterMass, ref PhysicsMass otherMass, BasicHit hit)
    {
        
    }
    #endregion
}
