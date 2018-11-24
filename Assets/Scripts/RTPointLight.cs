using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTPointLight : RTLight {

    public Vector3 position;
    public float range;
    
    private void Awake() {

        intensity = GetComponent<Light>().intensity;
        color.x = GetComponent<Light>().color.r;
        color.y = GetComponent<Light>().color.g;
        color.z = GetComponent<Light>().color.b;
        position = transform.position;
        range = GetComponent<Light>().range;
    }

}
