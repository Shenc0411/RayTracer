using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTDirectionalLight : RTLight {

    public Vector3 direction;

    private void Awake() {

        intensity = GetComponent<Light>().intensity;
        color.x = GetComponent<Light>().color.r;
        color.y = GetComponent<Light>().color.g;
        color.z = GetComponent<Light>().color.b;
        direction = transform.forward;

    }

}
