using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTHitInfo {

    public readonly Vector3 hitPoint;
    public readonly Vector3 hitPointNormal;
    public readonly RTHitable hitable;
    public readonly RTRay hitRay;

    public RTHitInfo(RTHitable hitable, Vector3 hitPoint, Vector3 hitPointNormal, RTRay hitRay) {
        this.hitable = hitable;
        this.hitPoint = hitPoint;
        this.hitPointNormal = hitPointNormal;
        this.hitRay = hitRay;
    }
}