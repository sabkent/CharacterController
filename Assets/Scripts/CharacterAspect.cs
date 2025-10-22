using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.CharacterController;
using UnityEngine;

public readonly partial struct CharacterAspect : IAspect, IKinematicCharacterProcessor<CharacterUpdateContext>
{
    public readonly RefRW<Character> Character;
    public readonly RefRW<CharacterControl> CharacterControl;
    public readonly KinematicCharacterAspect KinematicCharacter;

    public void PhysicsUpdate(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext)
    {
        ref Character character = ref Character.ValueRW;
        ref KinematicCharacterBody characterBody = ref KinematicCharacter.CharacterBody.ValueRW;
        ref float3 characterPosition = ref KinematicCharacter.LocalTransform.ValueRW.Position;
        
        float deltaTime = baseContext.Time.DeltaTime;
        
        KinematicCharacter.Update_Initialize(in this, ref context, ref baseContext, ref characterBody, baseContext.Time.DeltaTime);
        KinematicCharacter.Update_ParentMovement(in this, ref context, ref baseContext, ref characterBody, ref characterPosition, characterBody.WasGroundedBeforeCharacterUpdate);
        KinematicCharacter.Update_Grounding(in this, ref context, ref baseContext, ref characterBody, ref characterPosition);

        if (characterBody.IsGrounded)
        {
            var targetVelocity = CharacterControl.ValueRO.Move * Character.ValueRO.GroundMaxSpeed;

            CharacterControlUtilities.StandardGroundMove_Interpolated(ref characterBody.RelativeVelocity, targetVelocity, 
                character.GroundMovementSharpness, deltaTime, characterBody.GroundingUp, characterBody.GroundHit.Normal);
        }
        else
        {
            var airAcceleration = CharacterControl.ValueRO.Move * Character.ValueRO.AirAcceleration;
            if (math.lengthsq(airAcceleration) > 0f)
            {
                var tmpVelocity = characterBody.RelativeVelocity;
                
                CharacterControlUtilities.StandardAirMove(ref characterBody.RelativeVelocity, airAcceleration, character.AirMaxSpeed, characterBody.GroundingUp, deltaTime, false);
            }
            
            //gravity
            CharacterControlUtilities.AccelerateVelocity(ref characterBody.RelativeVelocity, character.Gravity, deltaTime);
            
            //drag
            CharacterControlUtilities.ApplyDragToVelocity(ref characterBody.RelativeVelocity, deltaTime, character.AirDrag);
        }
        
        KinematicCharacter.Update_PreventGroundingFromFutureSlopeChange(in this, ref context, ref baseContext,
            ref characterBody, in character.StepAndSlopeHandling);
        KinematicCharacter.Update_GroundPushing(in this, ref context, ref baseContext, character.Gravity);
        KinematicCharacter.Update_MovementAndDecollisions(in this, ref context, ref baseContext, ref characterBody,
            ref characterPosition);
        KinematicCharacter.Update_MovingPlatformDetection(ref baseContext, ref characterBody);
        KinematicCharacter.Update_ParentMomentum(ref baseContext, ref characterBody);
        KinematicCharacter.Update_ProcessStatefulCharacterHits();
    }

    public void VariableUpdate(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext)
    {
        ref KinematicCharacterBody characterBody = ref KinematicCharacter.CharacterBody.ValueRW;
        // We'll compute the desired rotation, but only write it to the LocalTransform when requested
        ref quaternion characterRotationRef = ref KinematicCharacter.LocalTransform.ValueRW.Rotation;
        ref Character character = ref Character.ValueRW;
        ref CharacterControl characterControl = ref CharacterControl.ValueRW;
        
        // Compute desired rotations based on input deltas
        ComputeFinalRotationsFromRotationDelta(ref character.ViewPitchDegrees,
            ref character.CharacterYDegrees,
            math.up(),
            characterControl.LookYawPitchDegreeDelta,
            0,
            character.MinViewAngle, 
            character.MaxViewAngle,
            out var computedCharacterRotation,
            out float canceledPitchDegrees,
            out character.ViewLocalRotation);

        // Only write to the visual rotation this tick if requested (e.g., final prediction tick)
        if (context.WriteVisualRotation)
        {
            characterRotationRef = computedCharacterRotation;
        }
    }
    
    private void ComputeFinalRotationsFromRotationDelta(
        ref float viewPitchDegrees,
        ref float characterRotationYDegrees,
        float3 characterTransformUp,
        float2 yawPitchDeltaDegrees,
        float viewRollDegrees,
        float minPitchDegrees,
        float maxPitchDegrees,
        out quaternion characterRotation,
        out float canceledPitchDegrees,
        out quaternion viewLocalRotation)
    {
        // Yaw
        characterRotationYDegrees += yawPitchDeltaDegrees.x;
        ComputeRotationFromYAngleAndUp(characterRotationYDegrees, characterTransformUp, out characterRotation);

        // Pitch
        viewPitchDegrees += yawPitchDeltaDegrees.y;
        float viewPitchAngleDegreesBeforeClamp = viewPitchDegrees;
        viewPitchDegrees = math.clamp(viewPitchDegrees, minPitchDegrees, maxPitchDegrees);
        canceledPitchDegrees = yawPitchDeltaDegrees.y - (viewPitchAngleDegreesBeforeClamp - viewPitchDegrees);

        viewLocalRotation = CalculateLocalViewRotation(viewPitchDegrees, viewRollDegrees);
    }
    
    private void ComputeRotationFromYAngleAndUp(
        float characterRotationYDegrees,
        float3 characterTransformUp,
        out quaternion characterRotation)
    {
        characterRotation =
            math.mul(MathUtilities.CreateRotationWithUpPriority(characterTransformUp, math.forward()),
                quaternion.Euler(0f, math.radians(characterRotationYDegrees), 0f));
    }
    
    private quaternion CalculateLocalViewRotation(float viewPitchDegrees, float viewRollDegrees)
    {
        // Pitch
        quaternion viewLocalRotation = quaternion.AxisAngle(-math.right(), math.radians(viewPitchDegrees));

        // Roll
        viewLocalRotation = math.mul(viewLocalRotation,
            quaternion.AxisAngle(math.forward(), math.radians(viewRollDegrees)));

        return viewLocalRotation;
    }
    
    #region IKinematicCharacterProcessor
    public void UpdateGroundingUp(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext)
    {
        ref KinematicCharacterBody characterBody = ref KinematicCharacter.CharacterBody.ValueRW;
        KinematicCharacter.Default_UpdateGroundingUp(ref characterBody);
    }

    public bool CanCollideWithHit(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        in BasicHit hit) => PhysicsUtilities.IsCollidable(hit.Material);

    public bool IsGroundedOnHit(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        in BasicHit hit, int groudingEvaluationType)
    {
        var character = Character.ValueRO;

        return KinematicCharacter.Default_IsGroundedOnHit(in this,
            ref context,
            ref baseContext,
            in hit,
            in character.StepAndSlopeHandling,
            groudingEvaluationType);
    }

    public void OnMovementHit(ref CharacterUpdateContext context, ref KinematicCharacterUpdateContext baseContext,
        ref KinematicCharacterHit hit,
        ref float3 remainingMovementDirection,
        ref float remainingMovementLength,
        float3 originalVelocityDirection,
        float hitDistance)
    {
        ref KinematicCharacterBody characterBody = ref KinematicCharacter.CharacterBody.ValueRW;
        ref float3 characterPosition = ref KinematicCharacter.LocalTransform.ValueRW.Position;
        var character = Character.ValueRO;

        KinematicCharacter.Default_OnMovementHit(
            in this,
            ref context,
            ref baseContext,
            ref characterBody,
            ref characterPosition,
            ref hit,
            ref remainingMovementDirection,
            ref remainingMovementLength,
            originalVelocityDirection,
            hitDistance,
            character.StepAndSlopeHandling.StepHandling,
            character.StepAndSlopeHandling.MaxStepHeight,
            character.StepAndSlopeHandling.CharacterWidthForStepGroundingCheck);
    }

    public void OverrideDynamicHitMasses(ref CharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext,
        ref PhysicsMass characterMass,
        ref PhysicsMass otherMass,
        BasicHit hit)
    {
        
    }

    public void ProjectVelocityOnHits(ref CharacterUpdateContext context,
        ref KinematicCharacterUpdateContext baseContext,
        ref float3 velocity,
        ref bool characterIsGrounded,
        ref BasicHit characterGroundHit,
        in DynamicBuffer<KinematicVelocityProjectionHit> velocityProjectionHits,
        float3 originalVelocityDirection)
    {
        var character = Character.ValueRO;

        KinematicCharacter.Default_ProjectVelocityOnHits(
            ref velocity,
            ref characterIsGrounded,
            ref characterGroundHit,
            in velocityProjectionHits,
            originalVelocityDirection,
            character.StepAndSlopeHandling.ConstrainVelocityToGroundPlane);
    }
    #endregion
}

public struct CharacterUpdateContext
{
    public bool WriteVisualRotation;
}


