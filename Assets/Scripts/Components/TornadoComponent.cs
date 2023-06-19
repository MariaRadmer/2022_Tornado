using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

[GenerateAuthoringComponent]
public struct TornadoComponent : IComponentData
{
    [Range(0f, 1f)]
    public float tornadoForce;
    public float tornadoMaxForceDist;
    public float tornadoHeight;
    public float tornadoUpForce;
    public float tornadoInwardForce;

    public float3 tornadoPos;
    public float tornadoFader;

    [Range(0f, 1f)]
    public float damping;
    [Range(0f, 1f)]
    public float friction;
    public float breakResistance;
    public float expForce;
}
