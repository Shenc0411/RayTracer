using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTSphere : RTHitable {
    
    public float radius;

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
            
            return new RTHitInfo(this, Q, normal, ray);
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

                return new RTHitInfo(this, Q, normal, ray);
            }
        }
        else {
            //On Surface - Ignore
            return null;
        }
    }

    public override void UpdateParameters() {
        radius = transform.localScale.x / 2.0f;
        position = transform.position;
    }
}