using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class MainCameraSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (MainGameObjectCamera.Instance != null && SystemAPI.HasSingleton<MainEntityCamera>())
        {
            var cameraEntity = SystemAPI.GetSingletonEntity<MainEntityCamera>();
            var cameraComponent = SystemAPI.GetSingleton<MainEntityCamera>();

            var targetLocalToWorld = SystemAPI.GetComponent<LocalToWorld>(cameraEntity);
            
            MainGameObjectCamera.Instance.transform.SetPositionAndRotation(targetLocalToWorld.Position, targetLocalToWorld.Rotation);
            MainGameObjectCamera.Instance.fieldOfView = cameraComponent.CurrentFoV;
        }
    }
}
