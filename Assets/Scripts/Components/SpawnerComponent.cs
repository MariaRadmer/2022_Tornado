using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

[GenerateAuthoringComponent]
public struct SpawnerComponent : IComponentData
{
    public Entity particle;
    public Entity bar;
    public Entity pointsEntity;
}
