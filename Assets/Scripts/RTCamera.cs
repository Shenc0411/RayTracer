using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Camera))]
public class RTCamera : MonoBehaviour {

    public Vector3 position;
    public Vector3 direction;

    public int xResolution = 256;
    public int yResolution = 256;

    public float nearPlaneDistance;

    private void Awake() {
        position = transform.position;
        direction = transform.forward;
        nearPlaneDistance = GetComponent<Camera>().nearClipPlane;
    }

}
