using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class GameResourceAuthoring : MonoBehaviour
{
    public NetCodeConfig NetCodeConfig;
    
    public GameObject Player;
    public GameObject Character;

    private class Baker : Baker<GameResourceAuthoring>
    {
        public override void Bake(GameResourceAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);

            AddComponent(entity, new GameResource
            {
                Player = GetEntity(authoring.Player, TransformUsageFlags.Dynamic),
                Character = GetEntity(authoring.Character, TransformUsageFlags.Dynamic),
                ClientServerTickRate = authoring.NetCodeConfig.ClientServerTickRate
            });
        }
    }
}

public struct GameResource : IComponentData
{
    public Entity Player;
    public Entity Character;

    public ClientServerTickRate ClientServerTickRate;
}