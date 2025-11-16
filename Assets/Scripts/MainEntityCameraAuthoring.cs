using Unity.Entities;
using UnityEngine;

public class MainEntityCameraAuthoring : MonoBehaviour
{
    public float FOV = 75f;

    private class Baker : Baker<MainEntityCameraAuthoring>
    {
        public override void Bake(MainEntityCameraAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MainEntityCamera(authoring.FOV));
        }
    }
}
