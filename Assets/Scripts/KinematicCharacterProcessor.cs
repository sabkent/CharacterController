
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public struct KinematicCharacterProcessor: IKinematicCharacterProcessor<CharacterUpdateContext>
{
    public KinematicCharacterDataAccess CharacterDataAccess;
    public RefRW<Character> Character;
    public RefRW<CharacterControl> CharacterControl;

    public void PhysicsUpdate(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext)
    {
        ref Character character = ref Character.ValueRW;
        ref KinematicCharacterBody characterBody = ref CharacterDataAccess.CharacterBody.ValueRW;
        ref float3 position = ref CharacterDataAccess.LocalTransform.ValueRW.Position;

        KinematicCharacterUtilities.Update_Initialize(in this, ref context, ref baseContext, ref characterBody,
            CharacterDataAccess.CharacterHitsBuffer,
            CharacterDataAccess.DeferredImpulsesBuffer, 
            CharacterDataAccess.VelocityProjectionHits,
            baseContext.Time.DeltaTime);
        
        KinematicCharacterUtilities.Update_ParentMovement(in this, ref context, ref baseContext,
            CharacterDataAccess.CharacterEntity,
            ref characterBody,
            CharacterDataAccess.CharacterProperties.ValueRO,
            CharacterDataAccess.PhysicsCollider.ValueRO,
            CharacterDataAccess.LocalTransform.ValueRO,
            ref position, 
            characterBody.WasGroundedBeforeCharacterUpdate);
        
        KinematicCharacterUtilities.Update_Grounding(in this, ref context, ref baseContext, ref characterBody,
            CharacterDataAccess.CharacterEntity,
            CharacterDataAccess.CharacterProperties.ValueRO,
            CharacterDataAccess.PhysicsCollider.ValueRO,
            CharacterDataAccess.LocalTransform.ValueRO,
            CharacterDataAccess.VelocityProjectionHits,
            CharacterDataAccess.CharacterHitsBuffer,
            ref position);

        HandleVelocityControl(ref context, ref baseContext);
        
        KinematicCharacterUtilities.Update_PreventGroundingFromFutureSlopeChange(in this, ref context, ref baseContext,
            CharacterDataAccess.CharacterEntity,
            ref characterBody,
            CharacterDataAccess.CharacterProperties.ValueRO,
            CharacterDataAccess.PhysicsCollider.ValueRO,
            in character.StepAndSlopeHandling);
        
        KinematicCharacterUtilities.Update_GroundPushing(in this, ref context, ref baseContext, ref characterBody,
            CharacterDataAccess.CharacterProperties.ValueRO,
            CharacterDataAccess.LocalTransform.ValueRO,
            CharacterDataAccess.DeferredImpulsesBuffer,
            character.Gravity);
        
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
            ref position);
        
        KinematicCharacterUtilities.Update_MovingPlatformDetection(
            ref baseContext,
            ref characterBody);
        
        KinematicCharacterUtilities.Update_ParentMomentum(
            ref baseContext,
            ref characterBody,
            CharacterDataAccess.LocalTransform.ValueRO.Position);
        
        KinematicCharacterUtilities.Update_ProcessStatefulCharacterHits(
            CharacterDataAccess.CharacterHitsBuffer,
            CharacterDataAccess.StatefulHitsBuffer);
    }

    public void VariableUpdate(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext)
    {
        ref KinematicCharacterBody characterBody = ref CharacterDataAccess.CharacterBody.ValueRW;
        ref Character character = ref Character.ValueRW;
        ref quaternion characterRotation = ref CharacterDataAccess.LocalTransform.ValueRW.Rotation;
        ref CharacterControl characterControl = ref CharacterControl.ValueRW;
        
        CharacterUtilities.ComputeFinalRotationsFromRotationDelta(
            ref character.ViewPitchDegrees,
            ref character.CharacterYDegrees,
            math.up(),
            characterControl.LookYawPitchDegreesDelta,
            0, // don't include roll angle in simulation
            character.MinViewAngle,
            character.MaxViewAngle,
            out characterRotation,
            out float canceledPitchDegrees,
            out character.ViewLocalRotation);
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
                Debug.Log("JUMP");
                CharacterControlUtilities.StandardJump(ref characterBody, characterBody.GroundingUp * character.JumpSpeed, cancelVelocityBeforeJump: true, characterBody.GroundingUp);
            }
        }
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
