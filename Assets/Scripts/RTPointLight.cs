using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTPointLight : RTLight {

    public Vector3 position;
    public float range;
    
    private void Awake() {

        intensity = GetComponent<Light>().intensity;
        color = GetComponent<Light>().color;
        range = GetComponent<Light>().range;
        position = transform.position;

    }

}
