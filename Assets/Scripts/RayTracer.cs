using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracer : MonoBehaviour {

    public RTCamera mCamera;
    public GameObject sceneParentGO;
    public List<RTHitable> hitables;
    public List<RTDirectionalLight> directionalLights;
    public List<RTPointLight> pointLights;
    public HashSet<RTRay> activeRays;
    public RTRay[,] screenRays;
    public MeshRenderer targetMeshRenderer;

    public static float EPS = 0.0001f;
    public static int MAX_RAY_DEPTH = 4;

    private void Awake() {

        hitables = new List<RTHitable>();
        directionalLights = new List<RTDirectionalLight>();
        pointLights = new List<RTPointLight>();
        activeRays = new HashSet<RTRay>();
        screenRays = new RTRay[mCamera.yResolution, mCamera.xResolution];

        foreach(RTHitable hitable in sceneParentGO.GetComponentsInChildren<RTHitable>()) {
            hitables.Add(hitable);
        }
        foreach(RTDirectionalLight DL in sceneParentGO.GetComponentsInChildren<RTDirectionalLight>()) {
            directionalLights.Add(DL);
        }
        foreach (RTPointLight PL in sceneParentGO.GetComponentsInChildren<RTPointLight>()) {
            pointLights.Add(PL);
        }

    }

    private void RayTrace() {
        Matrix4x4 rot = Matrix4x4.Rotate(Quaternion.FromToRotation(new Vector3(0, 0, 1), mCamera.direction));
        int halfYRes = mCamera.yResolution / 2;
        int halfXRes = mCamera.xResolution / 2;

        for (int y = 0; y < mCamera.yResolution; ++y) {
            for(int x = 0; x < mCamera.xResolution; ++x) {
                Vector3 screenPoint = new Vector3(-halfXRes + x, -halfYRes + y, mCamera.nearPlaneDistance);
                screenPoint = rot * screenPoint;
                RTRay ray = new RTRay(mCamera.position, screenPoint - mCamera.position, null);
                screenRays[y, x] = ray;
                activeRays.Add(ray);
            }
        }

        for(int i = 0; i < MAX_RAY_DEPTH; ++i) {
            RayTraceStep(i);
        }

        Texture2D renderTexture = new Texture2D(mCamera.xResolution, mCamera.yResolution);

        for (int y = 0; y < mCamera.yResolution; ++y) {
            for (int x = 0; x < mCamera.xResolution; ++x) {
                Vector3 colorVals = TraceRayColor(screenRays[y, x]);
                Color color = new Color(colorVals.x, colorVals.y, colorVals.z, 1);
                renderTexture.SetPixel(x, y, color);
            }
        }

        targetMeshRenderer.material.SetTexture("_MainTex", renderTexture);
    }

    private void RayTraceStep(int depth) {
        foreach(RTRay ray in activeRays) {

            ray.direction.Normalize();

            RTHitInfo hitInfo = null;
            float hitPointDistance = float.MaxValue;

            foreach(RTHitable hitable in hitables) {
                RTHitInfo localHitInfo = hitable.CheckCollision(ray);
                if (localHitInfo.hitable) {
                    if(hitInfo == null) {
                        hitInfo = localHitInfo;
                    }
                    else {
                        float localHitPointDistance = (localHitInfo.hitPoint - ray.origin).magnitude;
                        if(localHitPointDistance < hitPointDistance) {
                            hitInfo = localHitInfo;
                        }
                    }
                }
            }

            activeRays.Remove(ray);

            if (hitInfo != null) {
                ray.hitInfo = hitInfo;
                if(hitInfo.reflection != null) {
                    activeRays.Add(hitInfo.reflection);
                }
                if(hitInfo.refraction != null) {
                    activeRays.Add(hitInfo.refraction);
                }
            }
        }
    }


    private Vector3 TraceRayColor(RTRay ray) {
        Vector3 result = Vector3.zero;
        if (ray.hitInfo != null) {
            result.x += GatherLightColor(ray.hitInfo).x * ray.hitInfo.hitable.albedo.x;
            result.y += GatherLightColor(ray.hitInfo).y * ray.hitInfo.hitable.albedo.y;
            result.z += GatherLightColor(ray.hitInfo).z * ray.hitInfo.hitable.albedo.z;
            if (ray.hitInfo.reflection != null) {
                result += TraceRayColor(ray.hitInfo.reflection);
            }
            if (ray.hitInfo.refraction != null) {
                result += TraceRayColor(ray.hitInfo.refraction);
            }
        }
        return result.normalized;
    }

    private Vector3 GatherLightColor(RTHitInfo hitInfo) {

        Vector3 result = Vector3.zero;

        foreach(RTDirectionalLight light in directionalLights) {
            float dotProduct = Mathf.Abs(Vector3.Dot(hitInfo.hitPointNormal, light.direction));
            result.x += light.color.x * light.intensity * dotProduct;
            result.y += light.color.y * light.intensity * dotProduct;
            result.z += light.color.z * light.intensity * dotProduct;
        }

        return result.normalized;
    }

}
