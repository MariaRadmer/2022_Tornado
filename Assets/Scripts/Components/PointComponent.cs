using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;


[GenerateAuthoringComponent]
public struct PointComponent : IComponentData
{
    public float3 pos;
    public float3 posOld;
    public bool anchor;
    public int neighborCount;
}

