using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTQuad : RTHitable {

    public Vector3 A, B, C;
    public Vector3 AB, AC;
    public float ABLengthSquared, ACLengthSquared;
    public Vector3 planeNormal;

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

            RTHitInfo hitinfo = new RTHitInfo(this, I, N, ray);

            return hitinfo;

        }

        return null;
    }

    public override void UpdateParameters() {
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
    }
}
