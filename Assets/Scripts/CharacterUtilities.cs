using System;
using Unity.CharacterController;
using Unity.Mathematics;

public static class CharacterUtilities
{
    public static quaternion GetCurrentWorldViewRotation(quaternion characterRotation,
        quaternion localCharacterViewRotation) => math.mul(characterRotation, localCharacterViewRotation);

    public static void GetCurrentWorldViewDirectionAndRotation(quaternion characterRotation,
        quaternion localCharacterViewRotation,
        out float3 worldCharacterViewDirection, out quaternion worldCharacterViewRotation)
    {
        worldCharacterViewRotation = math.mul(characterRotation, localCharacterViewRotation);
        worldCharacterViewDirection = math.mul(worldCharacterViewRotation, math.forward());
    }

    public static void ComputeRotationFromYAngleAndUp(float characterRotationYDegrees, float3 characterTransformUp,
        out quaternion characterRotation)
    {
        characterRotation = math.mul(MathUtilities.CreateRotationWithUpPriority(characterTransformUp, math.forward()),
            quaternion.Euler(0f, math.radians(characterRotationYDegrees), 0f));
    }


    public static quaternion CalculateRotationFrom(float pitchDegrees, float rollDegrees)
    {
        //pitch
        var rotation = quaternion.AxisAngle(-math.right(), math.radians(pitchDegrees));
        
        //roll
        rotation = math.mul(rotation, quaternion.AxisAngle(math.forward(), math.radians(rollDegrees)));

        return rotation;
    }
    
    
    public static void ComputeFinalRotationsFromRotationDelta(
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

        viewLocalRotation = CalculateRotationFrom(viewPitchDegrees, viewRollDegrees);
    }

}
