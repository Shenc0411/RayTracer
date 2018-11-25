using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTHitInfo {

    public Vector3 hitPoint;
    public Vector3 refractionPoint;
    public Vector3 hitPointNormal;
    public RTHitable hitable;
    public RTRay reflection;
    public RTRay refraction;
    public RTRay hitRay;

    public RTHitInfo(RTHitable hitable, Vector3 hitPoint, Vector3 refractionPoint, Vector3 hitPointNormal, RTRay hitRay, RTRay reflection, RTRay refraction) {
        this.hitable = hitable;
        this.hitPoint = hitPoint;
        this.refractionPoint = refractionPoint;
        this.hitPointNormal = hitPointNormal;
        this.hitRay = hitRay;
        this.reflection = reflection;
        this.refraction = refraction;
    }
}
