using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public abstract class RTHitable : MonoBehaviour {

    public float reflectionRate;

    public float lambertCoefficient;

    public float phongPower;
    public float phongCoefficient;

    public float blinnPhongPower;
    public float blinnPhongCoefficient;

    public float refractionRate;

    public Vector3 albedo;

    public Vector3 position;

    public abstract RTHitInfo CheckCollision(RTRay ray);

}