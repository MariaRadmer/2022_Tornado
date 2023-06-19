using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;

public partial class UpdateCameraSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Transform camTransform = Camera.Instance.GetTransform();

        UpdateCameraJob updateCamera = new UpdateCameraJob
        {
            forward = camTransform.forward
        };

        JobHandle camJobHandle = updateCamera.ScheduleParallel();
        camJobHandle.Complete(); 
    }
}

public partial struct UpdateCameraJob : IJobEntity
{
    public Vector3 forward;
    public void Execute(ref TornadoComponent t, in LocalToWorld localToWorld)
    {   
        var pos = new Vector3( t.tornadoPos.x, 10f, t.tornadoPos.z) - forward * 60.0f;
        Camera.Instance.UpdateTargetPosition(pos);
    }
}