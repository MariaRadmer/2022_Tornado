using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

[GenerateAuthoringComponent]
public struct BarComponent : IComponentData
{

    public int point1;
    public int point2;

    // Bar
    public float length;

    public float3 oldDirection;
    public float3 min;
    public float3 max;

    public Color color;
    public float thickness;



}
