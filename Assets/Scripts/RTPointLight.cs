using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTPointLight : RTLight {

    public Vector3 position;
    public float range;

    public override void UpdateParameters() {
        intensity = GetComponent<Light>().intensity;
        color = GetComponent<Light>().color;
        position = transform.position;
        range = GetComponent<Light>().range;
    }
}
