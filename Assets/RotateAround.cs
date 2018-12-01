using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAround : MonoBehaviour {

    public RTHitable center;
    public float anglePerSecond;
    public float angle;
    public float radius;

    private void Awake() {
        radius = (center.position - transform.position).magnitude;
    }

    // Update is called once per frame
    void FixedUpdate () {
        if (RayTracer.instance.enableRealTimeRendering) {
            angle += Time.fixedDeltaTime * anglePerSecond;
            if(angle > 360) {
                angle -= 360;
            }
            if (angle < -360) {
                angle += 360;
            }
            transform.position = new Vector3(center.position.x + Mathf.Cos(Mathf.Deg2Rad * angle) * radius, transform.position.y, center.position.z + Mathf.Sin(Mathf.Deg2Rad * angle) * radius);
        }
	}
}
