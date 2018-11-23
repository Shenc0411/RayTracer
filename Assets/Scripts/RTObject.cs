using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RenderMode { Opaque, Transparent }

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public abstract class RTObject : MonoBehaviour {

    public float reflectionRate;
    public float refractionRate;

    public RenderMode renderMode;

    public Color albedo;

    public Vector3 position;

}