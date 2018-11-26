using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTQuad : RTHitable {

    public Vector3 A, B, C;
    public Vector3 AB, AC;
    public float ABLengthSquared, ACLengthSquared;
    public Vector3 planeNormal;

    public HashSet<RTHitInfo> hitToRender;

    private void Awake() {
        position = transform.position;

        planeNormal = -transform.forward.normalized;

        A.x = -0.5f;
        A.y = 0.5f;
        A.z = 0.0f;
        B.x = 0.5f;
        B.y = 0.5f;
        B.z = 0.0f;
        C.x = -0.5f;
        C.y = -0.5f;
        C.z = 0.0f;

        A = transform.localToWorldMatrix * A;
        B = transform.localToWorldMatrix * B;
        C = transform.localToWorldMatrix * C;
        
        A += position; B += position; C += position;

        AB = B - A;
        AC = C - A;

        ABLengthSquared = Vector3.Dot(AB, AB);
        ACLengthSquared = Vector3.Dot(AC, AC);

        hitToRender = new HashSet<RTHitInfo>();
    }

    private void Update() {

        //A.x = -0.5f;
        //A.y = 0.5f;
        //A.z = 0.0f;
        //B.x = 0.5f;
        //B.y = 0.5f;
        //B.z = 0.0f;
        //C.x = -0.5f;
        //C.y = -0.5f;
        //C.z = 0.0f;

        //A = transform.localToWorldMatrix * A;
        //B = transform.localToWorldMatrix * B;
        //C = transform.localToWorldMatrix * C;

        //Debug.DrawLine(A, B, Color.green);
        //Debug.DrawLine(B, C, Color.green);
        //Debug.DrawLine(C, A, Color.green);
        //Debug.DrawRay(position, planeNormal, Color.blue);

        foreach(RTHitInfo hit in hitToRender) {
            Debug.DrawLine(hit.hitRay.origin, hit.hitPoint, Color.red);
            Debug.DrawRay(hit.hitPoint, hit.hitPointNormal, Color.blue);
            Debug.DrawRay(hit.reflection.origin, hit.reflection.direction, Color.green);
        }

    }

    public override RTHitInfo CheckCollision(RTRay ray) {

        Vector3 N = planeNormal;
        Vector3 V = ray.direction;
        float VDotN = Vector3.Dot(V, N);

        if(VDotN > 0) {
            N = -N;
            VDotN = -VDotN;
        }

        Vector3 AO = ray.origin - A;
        float AODotN = Vector3.Dot(AO, N);
        float t = AODotN / -VDotN;
        
        if(t < float.Epsilon) {
            //No Coliison - Hitpoint along reverse direction or on plane
            return null;
        }

        Vector3 I = ray.origin + V * t; // Actual Hitpoint

        Vector3 IA = I - A;

        float u = Vector3.Dot(IA, AB);
        float v = Vector3.Dot(IA, AC);

        if(u >= 0 && u <= ABLengthSquared && v >= 0 && v <= ACLengthSquared) {
            //Collision Detected

            RTRay reflection = null;
            RTRay refraction = null;

            if (reflectionRate > 0) {
                Vector3 NProjection = VDotN * N;
                Vector3 reflectionDir = V - 2.0f * NProjection;
                Vector3 reflectionHitPoint = I - planeNormal * RayTracer.HIT_POINT_OFFSET;
                reflection = new RTRay(reflectionHitPoint, reflectionDir, null);
            }

            if (refractionRate > 0) {
                Vector3 NProjection = VDotN * N;
                Vector3 orthoNProjection = V - NProjection;
                Vector3 refractionDir = NProjection + orthoNProjection * RayTracer.REFRACTION_FACTOR;
                Vector3 refractionHitPoint = I + planeNormal * RayTracer.HIT_POINT_OFFSET;
                refraction = new RTRay(refractionHitPoint, refractionDir, null);
            }

            RTHitInfo hitinfo = new RTHitInfo(this, I, N, ray, reflection, refraction);

            //if(reflectionRate > 0 && Random.Range(0, 1000) > 998) {
            //    hitToRender.Add(hitinfo);
            //}

            return hitinfo;

        }

        return null;
    }
}
