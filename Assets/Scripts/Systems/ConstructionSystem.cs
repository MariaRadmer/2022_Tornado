
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;


[UpdateBefore(typeof(UpdateBarsSystem))]
public partial class ConstructionSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem ecbSystem;
    
    private Unity.Mathematics.Random generator;

    private DynamicBuffer<PointComponentBuffer> pointsList;

    private Entity entity;

    private bool makePointsList = true;

    protected override void OnCreate()
    {
        ecbSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
        generator = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, 100000)); 
    }

    protected override void OnUpdate()
    {
        if (makePointsList)
        {
            entity = EntityManager.CreateEntity();
            pointsList = EntityManager.AddBuffer<PointComponentBuffer>(entity);

            EntityManager.AddComponent<PointsTag>(entity);
            makePointsList = false;
        }

        EntityCommandBuffer ecb = ecbSystem.CreateCommandBuffer();

        SpawnPointsJob spawnPointsJob = new SpawnPointsJob
        {
            generator = generator,
            ecb = ecb,
            pointsList = EntityManager.GetBuffer<PointComponentBuffer>(entity),
            entity = entity
            
        };
        JobHandle spawnPointsJobHandle = spawnPointsJob.Schedule();
        spawnPointsJobHandle.Complete();

        SpawnBuildingsJob spawnBuildingsJob = new SpawnBuildingsJob
        {
            generator = generator,
            ecb = ecb,
            buffer = EntityManager.GetBuffer<PointComponentBuffer>(entity),
        };

        JobHandle spawnJobHandle = spawnBuildingsJob.Schedule();
        spawnJobHandle.Complete();
        
        Enabled = false;
    }
}


[UpdateBefore(typeof(SpawnBuildingsJob))]
public partial struct SpawnPointsJob : IJobEntity
{
    public EntityCommandBuffer ecb;
    public Unity.Mathematics.Random generator;
    public DynamicBuffer<PointComponentBuffer> pointsList;
    public Entity entity;
    public void Execute([EntityInQueryIndex] int index, in SpawnerComponent spawn_comp)
    {
        List<PointComponent> pointComponents = new List<PointComponent>();

        for (int i = 0; i < 35; i++)
        {
            var height = generator.NextInt(4, 12);
            float3 pos = new float3(generator.NextFloat(-45, 45), 0f, generator.NextFloat(-45, 45));

            float spacing = 2f;

            for (int j = 0; j < height; j++)
            {
                bool anchor = false;
                if (j == 0) { anchor = true; }

                float x = pos.x + spacing;
                float y = j * spacing;
                float z = pos.z - spacing;
                float3 p1 = new float3(x, y, z);
                PointComponent point1 = new PointComponent { pos = p1, posOld = p1, anchor = anchor };
                pointComponents.Add(point1);
                pointsList.Add(new PointComponentBuffer { pointComponent = point1 });

                x = pos.x - spacing;
                y = j * spacing;
                z = pos.z - spacing;
                float3 p2 = new float3(x, y, z);
                PointComponent point2 = new PointComponent { pos = p2, posOld = p2, anchor = anchor };
                pointComponents.Add(point2);
                pointsList.Add(new PointComponentBuffer { pointComponent = point2 });

                x = pos.x + 0f;
                y = j * spacing;
                z = pos.z + spacing;
                float3 p3 = new float3(x, y, z);
                PointComponent point3 = new PointComponent { pos = p3, posOld = p3, anchor = anchor };
                pointComponents.Add(point3);
                pointsList.Add(new PointComponentBuffer { pointComponent = point3 });
            }
        }


        for (int i = 0; i < 600; i++)
        {
            float3 pos = new float3(generator.NextFloat(-55, 55), 0f, generator.NextFloat(-55, 55));

            var x = pos.x + generator.NextFloat(-0.2f, -0.1f);
            var y = pos.y + generator.NextFloat(0f, 3f);
            var z = pos.z + generator.NextFloat(0.1f, 0.2f);

            var p1 = new float3(x, y, z);
            PointComponent point1 = new PointComponent { pos = p1, posOld = p1, anchor = false };
            pointComponents.Add(point1);
            pointsList.Add(new PointComponentBuffer { pointComponent = point1 });

            x = pos.x + generator.NextFloat(0.2f, 0.1f);
            y = pos.y + generator.NextFloat(0f, 0.2f);
            z = pos.z + generator.NextFloat(-0.1f, -0.2f);
            var p2 = new float3(x, y, z);

            bool anchor = false;
            if (generator.NextFloat() < .1f)
            {
                anchor = true;
            }
            PointComponent point2 = new PointComponent { pos = p2, posOld = p2, anchor = anchor };
            pointComponents.Add(point2);
            pointsList.Add(new PointComponentBuffer { pointComponent = point2 });
        }

        
    }
}

[BurstCompile]
[UpdateAfter(typeof(SpawnPointsJob))]
public partial struct SpawnBuildingsJob : IJobEntity
{
    public EntityCommandBuffer ecb;
    public Unity.Mathematics.Random generator;
    public DynamicBuffer<PointComponentBuffer> buffer; 


    public void Execute([EntityInQueryIndex] int index, in SpawnerComponent spawn_comp)
    {
        for (int fst = 0; fst < buffer.Length; fst++)
        {
            for (int snd = fst + 1; snd < buffer.Length; snd++)
            {
                var pointComp1 = buffer[fst].pointComponent;
                var pointComp2 = buffer[snd].pointComponent;
                float3 point1pos = pointComp1.pos;
                float3 point2pos = pointComp2.pos;

                Vector3 delta = new Vector3(point2pos.x - point1pos.x, point2pos.y - point1pos.y, point2pos.z - point1pos.z);
                float length = delta.magnitude;
                float thickness = generator.NextFloat(.25f, .35f);

                float3 posBar = new float3(point1pos + point2pos) * .5f;
                Quaternion rot = Quaternion.LookRotation(delta);
                float3 scale = new float3(thickness, thickness, length);

                float3 min = new float3(Mathf.Min(point1pos.x, point2pos.x),
                                        Mathf.Min(point1pos.y, point2pos.y),
                                        Mathf.Min(point1pos.z, point2pos.z));

                float3 max = new float3(Mathf.Max(point1pos.x, point2pos.x),
                                        Mathf.Max(point1pos.y, point2pos.y),
                                        Mathf.Max(point1pos.z, point2pos.z));

                buffer[fst] = new PointComponentBuffer { pointComponent = pointComp1 };
                buffer[snd] = new PointComponentBuffer { pointComponent = pointComp2 };

                if (length < 5f && length > 0.2f)
                {
                    pointComp1.neighborCount++;
                    pointComp2.neighborCount++;

                    buffer[fst] = new PointComponentBuffer { pointComponent = pointComp1 };
                    buffer[snd] = new PointComponentBuffer { pointComponent = pointComp2 };

                    var entityBar = ecb.Instantiate(spawn_comp.bar);
                    ecb.AddComponent( entityBar, new Translation { Value = posBar });
                    ecb.AddComponent(entityBar, new Rotation { Value = rot });
                    ecb.AddComponent( entityBar, new NonUniformScale { Value = scale });

                    ecb.AddComponent( entityBar, new BarComponent
                    {
                        point1 = fst,
                        point2 = snd,
                        length = length,
                        oldDirection = 0,
                        min = min,
                        max = max,
                        color = UnityEngine.Color.white,
                        thickness = thickness
                    });
                }
            }
        }
    }
}
