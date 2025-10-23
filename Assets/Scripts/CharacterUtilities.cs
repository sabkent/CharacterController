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
}
