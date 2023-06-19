using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;

[UpdateAfter(typeof(ConstructionSystem))]
public partial class UpdateBarsSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem ecbSystem;
    
    private Unity.Mathematics.Random generator;

    
    private TornadoComponent tornado;

    private DynamicBuffer<PointComponentBuffer> pointsList;

    protected override void OnCreate()
    {
        ecbSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
        generator = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, 100000)); 
    }

    protected override void OnUpdate()
    {
        
        EntityCommandBuffer.ParallelWriter ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        tornado = GetSingleton<TornadoComponent>();

        float dt = Time.DeltaTime;
        float t = (float)Time.ElapsedTime;

        UpdatePointsJob updatePointsJob = new UpdatePointsJob
        {
            tornado = tornado,
            generator = generator,
            time = t,
        };
        JobHandle updatePointsHandle = updatePointsJob.ScheduleParallel();
        updatePointsHandle.Complete();

        Entity en = GetSingletonEntity<PointsTag>();
        
        pointsList = EntityManager.GetBuffer<PointComponentBuffer>(en);

        UpdateBarsJob updateBarsJob = new UpdateBarsJob
        {
            pointsList = pointsList,
            tornado = tornado,
            generator = generator,
        };
        JobHandle updateBarsHandle = updateBarsJob.Schedule();
        updateBarsHandle.Complete(); 
    }
}

[BurstCompile]
[UpdateBefore(typeof(UpdateBarsJob))]
public partial struct UpdatePointsJob : IJobEntity
{
    public TornadoComponent tornado;
    public Unity.Mathematics.Random generator;
    public float time;

    public void Execute([EntityInQueryIndex] int index, 
                                             ref DynamicBuffer<PointComponentBuffer> points)
    {

        

        for (int i = 0; i < points.Length; i++)
        {
            PointComponentBuffer p1_buffer = points[i];
            PointComponent point = p1_buffer.pointComponent;

            float invDamping = 1f - tornado.damping;

            if (point.anchor == false)
            {
                float startX = point.pos.x;
                float startY = point.pos.y;
                float startZ = point.pos.z;

                point.posOld.y += .01f;

                float sway = Mathf.Sin(point.pos.y / 5f + time / 4f) * 3f;
                // tornado force
                float tdx = tornado.tornadoPos.x + sway - point.pos.x;
                float tdz = tornado.tornadoPos.z - point.pos.z;
                float tornadoDist = Mathf.Sqrt(tdx * tdx + tdz * tdz);
                tdx /= tornadoDist;
                tdz /= tornadoDist;
            
                if (tornadoDist < tornado.tornadoMaxForceDist)
                {
                    
                    float force = (1f - tornadoDist / tornado.tornadoMaxForceDist);
                    float yFader = Mathf.Clamp01(1f - point.pos.y / tornado.tornadoHeight);
                    force *= tornado.tornadoFader * tornado.tornadoForce * generator.NextFloat(-.3f, 1.3f);
                
                    float forceY = tornado.tornadoUpForce;
                    float forceX = -tdz + tdx * tornado.tornadoInwardForce * yFader;
                    float forceZ = tdx + tdz * tornado.tornadoInwardForce * yFader;

                    point.posOld.x -= forceX * force;
                    point.posOld.y -= forceY * force;
                    point.posOld.z -= forceZ * force;
                }

                point.pos.x += (point.pos.x - point.posOld.x) * invDamping;
                point.pos.y += (point.pos.y - point.posOld.y) * invDamping;
                point.pos.z += (point.pos.z - point.posOld.z) * invDamping;

                point.posOld.x = startX;
                point.posOld.y = startY;
                point.posOld.z = startZ;
            
                if (point.pos.y < 0f)
                {
                    point.pos.y = 0f;
                    point.posOld.y = -point.posOld.y;
                    point.posOld.x += (point.pos.x - point.posOld.x) * tornado.friction;
                    point.posOld.z += (point.pos.z - point.posOld.z) * tornado.friction;
                }

                p1_buffer.pointComponent = point;


                points[i] = p1_buffer;
               
            }
        } 
    }
}

[BurstCompile]
public partial struct UpdateBarsJob : IJobEntity
{
    public TornadoComponent tornado;
    public Unity.Mathematics.Random generator;
    public DynamicBuffer<PointComponentBuffer> pointsList;
    public void Execute([EntityInQueryIndex] int index,
                                             ref Rotation rotation,
                                             ref Translation translation,
                                             ref NonUniformScale s,
                                             ref BarComponent bar)
    {

        //float3 scale = new float3(s.Value.x + 0.2f, 1, 1);
        //s.Value = scale;

        PointComponentBuffer p1_buffer = pointsList[bar.point1];
        PointComponentBuffer p2_buffer = pointsList[bar.point2];

        PointComponent p1 = p1_buffer.pointComponent;
        PointComponent p2 = p2_buffer.pointComponent;


        float dx = p2.pos.x - p1.pos.x;
        float dy = p2.pos.y - p1.pos.y;
        float dz = p2.pos.z - p1.pos.z;

        

        float dist = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        float extraDist = dist - bar.length;

        float pushX = (dx / dist * extraDist) * .5f;
        float pushY = (dy / dist * extraDist) * .5f;
        float pushZ = (dz / dist * extraDist) * .5f;

        if (p1.anchor == false && p2.anchor == false)
        {
            p1.pos.x += pushX;
            p1.pos.y += pushY;
            p1.pos.z += pushZ;
            p2.pos.x -= pushX;
            p2.pos.y -= pushY;
            p2.pos.z -= pushZ;
        }
        else if (p1.anchor)
        {
            p2.pos.x -= pushX * 2f;
            p2.pos.y -= pushY * 2f;
            p2.pos.z -= pushZ * 2f;
        }
        else if (p2.anchor)
        {
            p1.pos.x += pushX * 2f;
            p1.pos.y += pushY * 2f;
            p1.pos.z += pushZ * 2f;
        }

        translation.Value = new Vector3((p1.pos.x + p2.pos.x) * .5f, (p1.pos.y + p2.pos.y) * .5f, (p1.pos.z + p2.pos.z) * .5f);


        if (dx / dist * bar.oldDirection.x + dy / dist * bar.oldDirection.y + dz / dist * bar.oldDirection.z < .99f)
        {
            rotation.Value = Quaternion.LookRotation(new Vector3(dx, dy, dz));
            bar.oldDirection.x = dx / dist;
            bar.oldDirection.y = dy / dist;
            bar.oldDirection.z = dz / dist;
        }

        

        if (Mathf.Abs(extraDist) > tornado.breakResistance)
        {
            
            if (p2.neighborCount > 1)
            {
                p2.neighborCount--;
                PointComponent point2 = new PointComponent { pos = p2.pos, posOld = p2.posOld, anchor = p2.anchor, neighborCount = 1};
                pointsList.Add( new PointComponentBuffer { pointComponent = point2 });
                bar.point2 = pointsList.Length-1;

            }
            else if (p1.neighborCount > 1)
            {
                p1.neighborCount--;
                PointComponent point1 = new PointComponent { pos = p1.pos, posOld = p1.posOld, anchor = p1.anchor, neighborCount = 1 };
                pointsList.Add(new PointComponentBuffer { pointComponent = point1 });
                bar.point1 = pointsList.Length-1;
            }
        }

        bar.min = new float3(
                        Mathf.Min(p1.pos.x, p2.pos.x), 
                        Mathf.Min(p1.pos.y, p2.pos.y), 
                        Mathf.Min(p1.pos.z, p2.pos.z));
        bar.max = new float3(
                        Mathf.Max(p1.pos.x, p2.pos.x), 
                        Mathf.Max(p1.pos.y, p2.pos.y), 
                        Mathf.Max(p1.pos.z, p2.pos.z));


        p1_buffer.pointComponent = p1;
        p2_buffer.pointComponent = p2;

        pointsList[bar.point1] = p1_buffer;
        pointsList[bar.point2] = p2_buffer;

    }
}

