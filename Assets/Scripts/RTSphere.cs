using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTSphere : RTObject {

    public float radius;

    private void Awake() {
        albedo = gameObject.GetComponent<MeshRenderer>().material.color;
        radius = transform.localScale.x / 2.0f;
        position = transform.position;
    }

}