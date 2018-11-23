using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTHitInfo {

    public Vector3 hitPoint;
    public Vector3 hitPointNormal;
    public RTHitable hitable;
    public RTRay reflection;
    public RTRay refraction;

    public RTHitInfo(RTHitable hitable, Vector3 hitPoint, Vector3 hitPointNormal, RTRay reflection, RTRay refraction) {
        this.hitable = hitable;
        this.hitPoint = hitPoint;
        this.hitPointNormal = hitPointNormal;
        this.reflection = reflection;
        this.refraction = refraction;
    }
}
