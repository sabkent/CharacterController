using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class GlobalGravityAuthoring: MonoBehaviour
{
    public float3 Gravity;

    private class Baker : Baker<GlobalGravityAuthoring>
    {
        public override void Bake(GlobalGravityAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new GlobalGravityZone
            {
                Gravity = authoring.Gravity
            });
        }
    }
}
