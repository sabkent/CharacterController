using System;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;
using UnityEngine.Serialization;

public class CharacterAuthoring : MonoBehaviour
{
    public Character Character = default;
    public AuthoringKinematicCharacterProperties KinematicProperties = AuthoringKinematicCharacterProperties.GetDefault();
    
    public GameObject CameraTarget;
    
    private class Baker : Baker<CharacterAuthoring>
    {
        public override void Bake(CharacterAuthoring authoring)
        {
            KinematicCharacterUtilities.BakeCharacter(this, authoring, authoring.KinematicProperties);
            
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            authoring.Character.CameraTarget = GetEntity(authoring.CameraTarget, TransformUsageFlags.Dynamic);

            authoring.Character.ViewLocalRotation = quaternion.identity;
            
            AddComponent(entity, authoring.Character);
            AddComponent(entity, new CharacterControl());
            AddComponent(entity, new CharacterInitialized());
            AddComponent(entity, new CharacterStateMachine());
            SetComponentEnabled<CharacterInitialized>(entity, false);
        }
    }
}

[Serializable]
[GhostComponent]
public struct Character : IComponentData
{
    public float GroundMaxSpeed;
    public float GroundMovementSharpness;
    public float GroundedRotationSharpness;

    public float AirAcceleration;
    public float AirMaxSpeed;
    public float AirDrag;
    public float AirRotationSharpness;
    
    public bool PreventAirAccelerationAgainstUngroundedHits;
    
    public float JumpSpeed;
    
    public float3 Gravity;
    
    public BasicStepAndSlopeHandlingParameters StepAndSlopeHandling;

    public float MinViewAngle;
    public float MaxViewAngle;

    public Entity CameraTarget;
    
    public float UpOrientationAdaptationSharpness;

    
    public CapsuleGeometryDefinition StandingGeometry;
    
    
    [HideInInspector] [GhostField(Quantization = 1000, Smoothing = SmoothingAction.InterpolateAndExtrapolate)]
    public float CharacterYDegrees;
    [HideInInspector] [GhostField(Quantization = 1000, Smoothing = SmoothingAction.InterpolateAndExtrapolate)]
    public float ViewPitchDegrees;

    [HideInInspector]public float ViewRollDegrees;
    [HideInInspector] public quaternion ViewLocalRotation;
    [FormerlySerializedAs("CameraTargetRollAmount")] public float ViewRollAmount;
    public float ViewRollSharpNess;
    
    [HideInInspector] public bool HasDetectedMoveAgainstWall;
    [HideInInspector] public float3 LastKnownWallNormal;

    [HideInInspector] public bool JumpPressedBeforeBecameGrounded;
}

[Serializable]
public struct CharacterControl : IComponentData
{
    public float3 Move;
    public float2 LookYawPitchDegreesDelta;
    public bool Jump;
}

public struct CharacterInitialized : IComponentData, IEnableableComponent { }

[Serializable]
public struct CapsuleGeometryDefinition
{
    public float Radius;
    public float Height;
    public float3 Center;

    public CapsuleGeometry ToCapsuleGeometry()
    {
        Height = math.max(Height, (Radius + math.EPSILON) * 2f);
        float halfHeight = Height * .5f;

        return new CapsuleGeometry
        {
            Radius = Radius,
            Vertex0 = Center + (-math.up() * (halfHeight - Radius)),
            Vertex1 = Center + (math.up() * (halfHeight - Radius))
        };
    }
}

public struct MinMax<T> where T: struct
{
    public T Min;
    public T Max;
}