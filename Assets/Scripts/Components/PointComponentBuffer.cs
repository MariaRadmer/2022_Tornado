using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;



[GenerateAuthoringComponent]
public struct PointComponentBuffer : IBufferElementData
{
    public PointComponent pointComponent;
}