﻿using System;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct TransformData
{
    public float3 position;
    public float3 rotation;
    public float3 scale;
    
    public static TransformData kDefault = new TransformData(){position = Vector3.zero,rotation = Vector3.zero,scale = Vector3.one};
    
    public HomogeneousMatrix3x4 transformMatrix=>HomogeneousMatrix3x4.TRS(position,quaternion.EulerZXY(rotation),scale);
}

public interface ITransform
{
    Vector3 position { get; }
}

public interface ITransformHandle
{
    Transform Transform { get; }
}

public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    static T instance;
    public static T Instance
    {
        get
        {
            if (!instance)
                instance = new GameObject().AddComponent<T>();
            return instance;
        }
    }

    protected virtual void Awake()
    {
        instance = this.GetComponent<T>();
        this.name = typeof(T).Name;
    }
    protected  virtual void OnDestroy()
    {
        instance = null;
    }
}