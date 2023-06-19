using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Camera : MonoBehaviour
{

    public static Camera Instance;
    public Vector3 offset;
    Vector3 targetPosition;
    private UnityEngine.Camera mainCamera;

    private void Awake()
    {
        if (Instance != null)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            Instance = this;
        }
    }


    void Start()
    {
        mainCamera = UnityEngine.Camera.main;
    }

    private void LateUpdate()
    {
        mainCamera.gameObject.transform.position = targetPosition + offset;
    }

    public void UpdateTargetPosition(float3 position)
    {
        targetPosition = position;
    }

    public Transform GetTransform()
    {
        return mainCamera.transform;
    }
}
