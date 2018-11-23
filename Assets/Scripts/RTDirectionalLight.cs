using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTDirectionalLight : RTLight {

    public Vector3 direction;

    private void Awake() {

        intensity = GetComponent<Light>().intensity;
        color = GetComponent<Light>().color;
        direction = transform.forward;

    }

}
