using System;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.Serialization;

public class CharacterAuthoring : MonoBehaviour
{
    public Character Character = Character.Create();
    public AuthoringKinematicCharacterProperties CharacterProperties = AuthoringKinematicCharacterProperties.GetDefault();
    
    public GameObject CameraTarget;
    
    private class Baker : Baker<CharacterAuthoring>
    {
        public override void Bake(CharacterAuthoring authoring)
        {
            KinematicCharacterUtilities.BakeCharacter(this, authoring, authoring.CharacterProperties);
            
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            authoring.Character.CameraTarget = GetEntity(authoring.CameraTarget, TransformUsageFlags.Dynamic);
            
            AddComponent(entity, authoring.Character);
            AddComponent(entity, new CharacterControl());
            AddComponent(entity, new CharacterInitialized());
            SetComponentEnabled<CharacterInitialized>(entity, false);
        }
    }
}

[Serializable]
[GhostComponent]
public struct Character : IComponentData
{
    public static Character Create() => new Character()
    {
        GroundMaxSpeed = 10f,
        GroundMovementSharpness = 15f,
        
        AirAcceleration = 50f,
        AirMaxSpeed = 10f,
        AirDrag = 0f,
        
        JumpSpeed = 10f,
        
        Gravity = math.up() * -30f,
        
        StepAndSlopeHandling = BasicStepAndSlopeHandlingParameters.GetDefault(),
        
        MinViewAngle = -90f,
        MaxViewAngle = 90f
        
    };

    public float GroundMaxSpeed;
    public float GroundMovementSharpness;

    public float AirAcceleration;
    public float AirMaxSpeed;
    public float AirDrag;

    public float JumpSpeed;
    
    public float3 Gravity;
    
    public BasicStepAndSlopeHandlingParameters StepAndSlopeHandling;


    public float MinViewAngle;
    public float MaxViewAngle;

    public Entity CameraTarget;
    
    [HideInInspector] [GhostField(Quantization = 1000, Smoothing = SmoothingAction.InterpolateAndExtrapolate)]
    public float CharacterYDegrees;
    [HideInInspector] [GhostField(Quantization = 1000, Smoothing = SmoothingAction.InterpolateAndExtrapolate)]
    public float ViewPitchDegrees;

    [HideInInspector]public float ViewRollDegrees;
    [HideInInspector] public quaternion ViewLocalRotation;
    [FormerlySerializedAs("CameraTargetRollAmount")] public float ViewRollAmount;
    public float ViewRollSharpNess;
}

[Serializable]
public struct CharacterControl : IComponentData
{
    public float3 Move;
    public float2 LookYawPitchDegreesDelta;
    public bool Jump;
}

public struct CharacterInitialized : IComponentData, IEnableableComponent { }

public struct MinMax<T> where T: struct
{
    public T Min;
    public T Max;
}