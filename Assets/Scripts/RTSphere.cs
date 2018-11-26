using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTSphere : RTHitable {
    
    public float radius;

    private void Awake() {
        radius = transform.localScale.x / 2.0f;
        position = transform.position;
    }

    public override RTHitInfo CheckCollision(RTRay ray) {
        Vector3 OC = position - ray.origin;
        float OCDist = OC.magnitude;

        if(OCDist < radius - float.Epsilon) {
            //Inside Sphere
            float OPDist = Vector3.Dot(OC, ray.direction);
            float CPDistSqr = OCDist * OCDist - OPDist * OPDist;
            float PQDist = Mathf.Sqrt(this.radius * this.radius - CPDistSqr);

            Vector3 Q = ray.origin + (OPDist + PQDist) * ray.direction; //Actual Hitpoint
            Vector3 normal = (position - Q).normalized;

            RTRay reflection = null;
            RTRay refraction = null;

            if (reflectionRate > 0) {
                Vector3 hitPoint = Q + normal * RayTracer.HIT_POINT_OFFSET;
                float dirDotNormal = Vector3.Dot(ray.direction, normal);
                Vector3 normalProjVec = normal * dirDotNormal;
                Vector3 reflectionDir = ray.direction - 2.0f * normalProjVec;
                reflection = new RTRay(hitPoint, reflectionDir, null);
            }
            if (refractionRate > 0) {
                float dirDotNormal = Vector3.Dot(ray.direction, normal);
                Vector3 normalProjVec = normal * dirDotNormal;
                Vector3 othroNomralProjVec = ray.direction - normalProjVec;

                Vector3 refractionHitPoint = Q - normal * RayTracer.HIT_POINT_OFFSET;
                Vector3 refractionDir = normalProjVec + othroNomralProjVec * RayTracer.REFRACTION_FACTOR;
                refraction = new RTRay(refractionHitPoint, refractionDir, null);
            }
            
            return new RTHitInfo(this, Q, normal, ray, reflection, refraction);
        }
        else if(OCDist > radius + float.Epsilon) {
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
                Vector3 Q = ray.origin + (OPDist - QPDist) * ray.direction; //actual hitpoint
                Vector3 normal = (Q - position).normalized;
                Vector3 refractionHitPoint = Q - normal * RayTracer.HIT_POINT_OFFSET;
                Vector3 hitPoint = Q + normal * RayTracer.HIT_POINT_OFFSET;

                RTRay reflection = null;
                RTRay refraction = null;

                float dirDotNormal = Vector3.Dot(ray.direction, normal);

                Vector3 normalProjVec = normal * dirDotNormal;
                Vector3 othroNomralProjVec = ray.direction - normalProjVec;

                if (reflectionRate > 0) {
                    Vector3 reflectionDir = ray.direction - 2.0f * normalProjVec;
                    reflection = new RTRay(hitPoint, reflectionDir, null);
                }
                if (refractionRate > 0) {
                    Vector3 refractionDir = normalProjVec + othroNomralProjVec * RayTracer.REFRACTION_FACTOR;
                    refraction = new RTRay(refractionHitPoint, refractionDir, null);
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