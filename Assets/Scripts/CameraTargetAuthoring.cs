
using Unity.Entities;
using UnityEngine;

public class CameraTargetAuthoring: MonoBehaviour
{
    public GameObject Character;

    public class Baker : Baker<CameraTargetAuthoring>
    {
        public override void Bake(CameraTargetAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CameraTarget
            {
                Character = GetEntity(authoring.Character, TransformUsageFlags.Dynamic)
            } );
        }
    }
}

public struct CameraTarget : IComponentData
{
    public Entity Character;
}
