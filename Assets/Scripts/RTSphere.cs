using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTSphere : RTHitable {
    
    public float radius;

    private void Awake() {
        albedo.x = gameObject.GetComponent<MeshRenderer>().material.color.r;
        albedo.y = gameObject.GetComponent<MeshRenderer>().material.color.g;
        albedo.z = gameObject.GetComponent<MeshRenderer>().material.color.b;
        radius = transform.localScale.x / 2.0f;
        position = transform.position;
    }

    public override RTHitInfo CheckCollision(RTRay ray) {

        Vector3 OC = position - ray.origin;
        float OCDist = OC.magnitude;
        if(OCDist < RayTracer.EPS + radius) {
            //Inside Sphere - Refraction Case
            float OQDist = 2.0f * Vector3.Dot(OC, ray.direction);
            Vector3 Q = ray.origin + OQDist * ray.direction;
            Vector3 normal = (position - Q).normalized;
            RTRay reflection = new RTRay(Q, ray.direction - 2.0f * Vector3.Dot(ray.direction, normal) * normal, null);
            RTRay refraction = null;
            return new RTHitInfo(this, Q, normal, reflection, refraction);
        }
        else if(OCDist > RayTracer.EPS + radius) {
            //Outside Sphere
            float OPDist = Vector3.Dot(OC, ray.direction);
            float CPDist = Mathf.Sqrt(OCDist * OCDist + OPDist * OPDist);
            if(CPDist > radius) {
                //No Collision
                return null;
            }
            else {
                float QPDist = Mathf.Sqrt(radius * radius - CPDist * CPDist);
                Vector3 Q = ray.origin + (OPDist - QPDist) * ray.direction;
                Vector3 normal = (Q - position).normalized;
                RTRay reflection = new RTRay(Q, ray.direction - 2.0f * Vector3.Dot(ray.direction, normal) * normal, null);
                RTRay refraction = null;
                return new RTHitInfo(this, Q, normal, reflection, refraction);
            }
        }
        else {
            //On Surface - Ignore
            return null;
        }
    }

}