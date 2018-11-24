﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTRay {

    public Vector3 origin;
    public Vector3 direction;
    public RTHitInfo hitInfo;

    public RTRay(Vector3 origin, Vector3 direction, RTHitInfo hitInfo) {
        this.origin = origin;
        this.direction = direction;
        this.hitInfo = hitInfo;
    }

}