using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

[GenerateAuthoringComponent]
public struct ParticleComponent : IComponentData
{
    public float spinRate;
    public float upwardSpeed;
    public Matrix4x4 matrix;
    public float4 color;
    public float radiusMult;


}
