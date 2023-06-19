using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;
using Unity.Entities.UniversalDelegates;
using Unity.Burst;

public partial class ParticleSystem : SystemBase
{

    private BeginSimulationEntityCommandBufferSystem ecbSystem;
    private bool Enabled = false;
    private Unity.Mathematics.Random generator;

    protected override void OnCreate()
    {
        ecbSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
        generator = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, 100000));
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer.ParallelWriter ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        TornadoComponent tornado = GetSingleton<TornadoComponent>();


        if (!Enabled)
        {
            SpawnParticlesJob spawnParticlesJob = new SpawnParticlesJob
            {
                generator = generator,
                ecb = ecb
            };

            JobHandle spawnJobHandle = spawnParticlesJob.ScheduleParallel();
            spawnJobHandle.Complete();
            Enabled = true;
        }

        float dt = Time.DeltaTime;
        float t = (float) Time.ElapsedTime;

        SpinParticlesJob spinParticlesJob = new SpinParticlesJob
        {
            ecb = ecb,
            dt = dt,
            t = t,
            tornado = tornado
        };
        JobHandle spinParticlesJobHandle = spinParticlesJob.ScheduleParallel();
        spinParticlesJobHandle.Complete();

    }

}

[BurstCompile]
public partial struct SpawnParticlesJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;

    public Unity.Mathematics.Random generator;

    public void Execute([EntityInQueryIndex] int index, ref SpawnerComponent spawn_comp)
    {
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 100; j++)
            {
                var entity = ecb.Instantiate(index, spawn_comp.particle);

                var random_x = generator.NextFloat(-50, 50);
                var random_y = generator.NextFloat(0, 50);
                var random_z = generator.NextFloat(-50, 50);

                ecb.AddComponent(index, entity, new Translation { Value = new float3(random_x, random_y, random_z) });

                var mat = Matrix4x4.TRS(new Vector3(random_x, random_y, random_z), Quaternion.identity, Vector3.one * generator.NextFloat(0.2f, 0.7f));
                var radiusMult = generator.NextFloat();
                float4 col = new float4(1,1,1,1) * generator.NextFloat4(.3f, .7f);
                ecb.AddComponent(index, entity, new ParticleComponent { spinRate = 37, upwardSpeed = 6, matrix = mat, radiusMult = radiusMult, color = col }) ;
                
            }
        }
    }
}

[BurstCompile]
public partial struct SpinParticlesJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ecb;
    public float dt;
    public float t; 
    public TornadoComponent tornado;

    public void Execute([EntityInQueryIndex] int index, 
                                             ref Rotation rotation, 
                                             ref Translation trans,
                                             ref ParticleComponent pc)
    {

        float tornadoSway = Mathf.Sin(trans.Value.y / 5f + t / 4f) * 3f;
        float3 tornadoPos = new float3(tornado.tornadoPos.x + tornadoSway, trans.Value.y, tornado.tornadoPos.z);
        float3 delta = (tornadoPos - trans.Value);
        float dist = math.sqrt(delta.x*delta.x + delta.y*delta.y + delta.z*delta.z);
        delta /= dist;
        float inForce = dist - Mathf.Clamp01(tornadoPos.y / 50f) * 30f * pc.radiusMult + 2f;
        trans.Value += new float3(-delta.z * pc.spinRate + delta.x * inForce, pc.upwardSpeed, delta.x * pc.spinRate + delta.z * inForce) * dt;
        if (trans.Value.y > 50f)
        {
            trans.Value = new float3(trans.Value.x, 0f, trans.Value.z);
        }

        Matrix4x4 matrix = pc.matrix;
        matrix.m03 = trans.Value.x;
        matrix.m13 = trans.Value.y;
        matrix.m23 = trans.Value.z;
        pc.matrix = matrix;   
    }
}