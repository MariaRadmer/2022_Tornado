using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Entities.SystemBaseDelegates;

public partial class TornadoSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem ecbSystem;
    

    protected override void OnCreate()
    {
        ecbSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer.ParallelWriter ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        float t = (float)Time.ElapsedTime;
        float dt = Time.DeltaTime;

        UpdateTornadoJob updateTornadoJob = new UpdateTornadoJob
        {
            ecb = ecb,
            t = t,
            dt = dt,
        };
        JobHandle updateTornadoJobHandle = updateTornadoJob.ScheduleParallel();
        updateTornadoJobHandle.Complete();
    }

}

[BurstCompile]
public partial struct UpdateTornadoJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;
    public float t;
    public float dt;

    public void Execute([EntityInQueryIndex] int index,
                                             ref Rotation rotation,
                                             ref Translation translation,
                                             ref TornadoComponent tornadoComponent)
    {

        float tornadoX = Mathf.Cos(t / 6f) * 30f;
        float tornadoZ = Mathf.Sin(t / 6f * 1.618f) * 30f;
        
        tornadoComponent.tornadoPos.x = tornadoX;
        tornadoComponent.tornadoPos.z = tornadoZ;

        tornadoComponent.tornadoFader = Mathf.Clamp01(tornadoComponent.tornadoFader + dt/ 10f);
    }
}