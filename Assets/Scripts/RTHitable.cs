using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public abstract class RTHitable : MonoBehaviour {

    public float reflectionRate;

    public float refractionRate;

    public Color Ks;
    public Color Kd;
    public Color Ka;

    public float spec;

    public Vector3 position;

    public abstract RTHitInfo CheckCollision(RTRay ray);

}