using System.Collections.Generic;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateBefore(typeof(TransformDefaultVariantSystem))]
public partial class DefaultVariantSystem : DefaultVariantSystemBase
{
    protected override void RegisterDefaultVariants(Dictionary<ComponentType, Rule> defaultVariants)
    {
        defaultVariants.Add(typeof(LocalTransform), Rule.ForAll(typeof(DontSerializeVariant)));
    }
}

[GhostComponentVariation(typeof(KinematicCharacterBody))]
[GhostComponent(SendTypeOptimization = GhostSendType.OnlyPredictedClients)]
public struct KinematicCharacterBody_GhostVariants
{
    [GhostField(Quantization = 1000)] public float3 RelativeVelocity;
    [GhostField] public bool IsGrounded;
}

[GhostComponentVariation(typeof(CharacterInterpolation))]
[GhostComponent(PrefabType = GhostPrefabType.PredictedClient)]
public struct CharacterInterpolation_GhostVariant
{
    
}