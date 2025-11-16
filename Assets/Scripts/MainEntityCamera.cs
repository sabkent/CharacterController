using System;
using Unity.Entities;

public struct MainEntityCamera : IComponentData
{
    public float BaseFoV;
    public float CurrentFoV;
    
    public MainEntityCamera(float fov)
    {
        BaseFoV = CurrentFoV = fov;
    }
}
