using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTRay {

    public static int RAYS_SPAWNDED = 0;

    public readonly Vector3 origin;
    public readonly Vector3 direction;
    public RTHitInfo hitInfo;

    public RTRay(Vector3 origin, Vector3 direction, RTHitInfo hitInfo) {
        ++RAYS_SPAWNDED;
        this.origin = origin;
        this.direction = direction.normalized;
        this.hitInfo = hitInfo;
    }

}
