
using System;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public struct GlobalGravityZone:IComponentData
{
    public float3 Gravity;
}
