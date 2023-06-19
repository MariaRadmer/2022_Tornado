using System.ComponentModel;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ResetSceneSystem : SystemBase
{
    public static ResetSceneSystem Instance;
    private BeginInitializationEntityCommandBufferSystem ecbSystem;

    protected override void OnCreate()
    {
        ecbSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

        if (Instance == null)
        {
            Instance = this;
        }
    }

    protected override void OnUpdate()
    {
        if (Input.GetKey(KeyCode.R)) 
        {
            World.EntityManager.CompleteAllJobs();
            World.Dispose();
            DefaultWorldInitialization.Initialize("Default World",  false);
            SceneManager.LoadScene("DOTS Tornado", LoadSceneMode.Single);
        }
    }
}