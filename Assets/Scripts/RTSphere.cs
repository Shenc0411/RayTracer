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
        if(OCDist < radius - RayTracer.EPS) {
            //Inside Sphere
            float OQDist = 2.0f * Vector3.Dot(OC, ray.direction);
            if(OQDist < 0) {
                return null;
            }
            Vector3 Q = ray.origin + OQDist * ray.direction;
            Vector3 normal = (position - Q).normalized;
            RTRay reflection = null;
            RTRay refraction = null;

            if (reflectionRate > 0) {
                reflection = new RTRay(Q, ray.direction - 2.0f * Vector3.Dot(ray.direction, normal) * normal, null);
            }
            if (refractionRate > 0) {
                
            }
            
            return new RTHitInfo(this, Q, normal, ray, reflection, refraction);
        }
        else if(OCDist > radius + RayTracer.EPS) {
            //Outside Sphere
            float OPDist = Vector3.Dot(OC, ray.direction);
            if (OPDist < 0) {
                //No Collision
                return null;
            }
            float CPDist = Mathf.Sqrt(OCDist * OCDist - OPDist * OPDist);
            if(CPDist > radius) {
                //No Collision
                return null;
            }
            else {
                float QPDist = Mathf.Sqrt(radius * radius - CPDist * CPDist);
                if (OPDist < QPDist) {
                    //No Collision
                    return null;
                }
                Vector3 Q = ray.origin + (OPDist - QPDist) * ray.direction;
                Vector3 normal = (Q - position).normalized;

                RTRay reflection = null;
                RTRay refraction = null;

                if (reflectionRate > 0) {
                    reflection = new RTRay(Q, ray.direction - 2.0f * Vector3.Dot(ray.direction, normal) * normal, null);
                }
                if (refractionRate > 0) {

                }
                return new RTHitInfo(this, Q, normal, ray, reflection, refraction);
            }
        }
        else {
            //On Surface - Ignore
            return null;
        }
    }

}