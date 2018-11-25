using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Camera))]
public class RTCamera : MonoBehaviour {

    public Vector3 position;
    public Vector3 direction;

    public int xResolution;
    public int yResolution;

    public float nearPlaneDistance;

    public Color ambientLightColor;
    public float ambientLightIntensity;

    private void Awake() {

        xResolution = Screen.width;
        yResolution = Screen.height;

        position = transform.position;
        direction = transform.forward;
        nearPlaneDistance = GetComponent<Camera>().nearClipPlane;
    }

}
