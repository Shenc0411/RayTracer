using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTDirectionalLight : RTLight {

    public Vector3 direction;

    public override void UpdateParameters() {
        intensity = GetComponent<Light>().intensity;
        color = GetComponent<Light>().color;
        direction = transform.forward.normalized;
    }

}
