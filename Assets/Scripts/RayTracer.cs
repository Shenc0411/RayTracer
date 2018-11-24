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
    public RTRay[][] screenRays;
    public GameObject rendererGO;
    public Vector3[][] screenPoints;

    public Texture2D renderTexture;

    public static float EPS = float.Epsilon;
    public static int MAX_RAY_DEPTH = 2;

    private void Awake() {

        hitables = new List<RTHitable>();
        directionalLights = new List<RTDirectionalLight>();
        pointLights = new List<RTPointLight>();
        activeRays = new HashSet<RTRay>();
        

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

    private void Start() {
        RayTrace();
    }

    private void Update() {

        RenderRays();
        RenderScreenPlane();

    }

    private void RenderScreenPlane() {
        float realHalfX = rendererGO.transform.localScale.x / 2.0f;
        float realHalfY = rendererGO.transform.localScale.y / 2.0f;

        Matrix4x4 rot = Matrix4x4.Rotate(Quaternion.FromToRotation(new Vector3(0, 0, 1), mCamera.direction));

        int halfXRes = mCamera.xResolution / 2;
        int halfYRes = mCamera.yResolution / 2;

        Vector3[] screenVertices = new Vector3[4];
        screenVertices[0] = mCamera.position + new Vector3(-realHalfX, realHalfY, mCamera.nearPlaneDistance);
        screenVertices[1] = mCamera.position + new Vector3(realHalfX, realHalfY, mCamera.nearPlaneDistance);
        screenVertices[2] = mCamera.position + new Vector3(realHalfX, -realHalfY, mCamera.nearPlaneDistance);
        screenVertices[3] = mCamera.position + new Vector3(-realHalfX, -realHalfY, mCamera.nearPlaneDistance);

        screenVertices[0] = rot * screenVertices[0];
        screenVertices[1] = rot * screenVertices[1];
        screenVertices[2] = rot * screenVertices[2];
        screenVertices[3] = rot * screenVertices[3];

        Debug.DrawLine(screenVertices[0], screenVertices[1], Color.green);
        Debug.DrawLine(screenVertices[1], screenVertices[2], Color.green);
        Debug.DrawLine(screenVertices[2], screenVertices[3], Color.green);
        Debug.DrawLine(screenVertices[3], screenVertices[0], Color.green);
    }

    private void RenderRays() {
        for (int y = 0; y < mCamera.yResolution; ++y) {
            for (int x = 0; x < mCamera.xResolution; ++x) {
                if (screenRays != null && screenRays[y] != null && x % 32 == 0 && y % 32 == 0) {
                    RenderRay(screenRays[y][x]);
                }
            }
        }
    }

    private void RenderRay(RTRay ray) {
        if(ray == null) {
            return;
        }
        if (ray.hitInfo != null) {
            Debug.DrawLine(ray.origin, ray.hitInfo.hitPoint, Color.red);
            RenderRay(ray.hitInfo.reflection);
            //Debug.DrawRay(ray.hitInfo.reflection.origin, ray.hitInfo.reflection.direction, Color.green);
            Debug.DrawRay(ray.hitInfo.hitPoint, ray.hitInfo.hitPointNormal, Color.blue);
        }
        else {
            Debug.DrawRay(ray.origin, ray.direction, Color.red);
        }
    }

    private void GenerateScreenPoints() {

        screenPoints = new Vector3[mCamera.yResolution][];

        rendererGO.transform.forward = mCamera.direction;
        rendererGO.transform.position = mCamera.position + mCamera.nearPlaneDistance * mCamera.transform.forward;

        float realHalfX = rendererGO.transform.localScale.x / 2.0f;
        float realHalfY = rendererGO.transform.localScale.y / 2.0f;

        Matrix4x4 rot = Matrix4x4.Rotate(Quaternion.FromToRotation(new Vector3(0, 0, 1), mCamera.direction));

        int halfXRes = mCamera.xResolution / 2;
        int halfYRes = mCamera.yResolution / 2;

        float xScale = realHalfX / halfXRes;
        float yScale = realHalfY / halfYRes;

        for (int y = 0; y < mCamera.yResolution; ++y) {
            screenPoints[y] = new Vector3[mCamera.xResolution];
            for (int x = 0; x < mCamera.xResolution; ++x) {
                screenPoints[y][x] = mCamera.position + new Vector3(-realHalfX + x * xScale, realHalfY - y * yScale, mCamera.nearPlaneDistance);
                screenPoints[y][x] = rot * screenPoints[y][x];
            }
        }

    }

    private RTRay GetScreenRay(int x, int y) {
        return new RTRay(mCamera.position, screenPoints[y][x] - mCamera.position, null);
    }

    private void RayTrace() {

        GenerateScreenPoints();

        renderTexture = new Texture2D(mCamera.xResolution, mCamera.yResolution);

        screenRays = new RTRay[mCamera.yResolution][];

        for (int y = 0; y < mCamera.yResolution; ++y) {
            screenRays[y] = new RTRay[mCamera.xResolution];
            for (int x = 0; x < mCamera.xResolution; ++x) {
                screenRays[y][x] = GetScreenRay(x, y);
                renderTexture.SetPixel(x, y, TraceColor(screenRays[y][x], 1));
            }
        }

        //for (int i = 0; i < MAX_RAY_DEPTH; ++i) {
        //    RayTraceStep();
        //}

        //for (int y = 0; y < mCamera.yResolution; ++y) {
        //    for (int x = 0; x < mCamera.xResolution; ++x) {
        //        Vector3 colorVals = TraceRayColor(screenRays[y][x]);
        //        Color color = new Color(colorVals.x, colorVals.y, colorVals.z, 1);
        //        renderTexture.SetPixel(x, y, color);
        //    }
        //}

        renderTexture.Apply();

        renderTexture.filterMode = FilterMode.Point;

        rendererGO.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", renderTexture);
    }

    private Color TraceColor(RTRay ray, int depth) {
        Color result = Color.black;

        if(depth > MAX_RAY_DEPTH) {
            return result;
        }

        RTHitInfo hitInfo = GetClosetHitInfo(ray);

        if(hitInfo != null) {
            
            ray.hitInfo = hitInfo;

            Color albedoColor = new Color(hitInfo.hitable.albedo.x, hitInfo.hitable.albedo.y, hitInfo.hitable.albedo.z);

            Color lightColor = TraceLight(hitInfo);

            result = albedoColor + lightColor;

        }

        return result;
    }

    private Color TraceLight(RTHitInfo hitInfo) {
        Color result = Color.black;

        foreach (RTDirectionalLight light in directionalLights) {

            //bool isInShadow = false;
            //RTRay lightRay = new RTRay(hitInfo.hitPoint, -light.direction, null);

            //foreach (RTHitable hitable in hitables) {
            //    RTHitInfo local = hitable.CheckCollision(lightRay);
            //    if (local != null) {
            //        isInShadow = true;
            //        break;
            //    }
            //}

            //if (isInShadow) {
            //    continue;
            //}

            Vector3 lightDir = -light.direction.normalized;

            float lightDirDotNormal = Vector3.Dot(lightDir, hitInfo.hitPointNormal);
            float rayDirDotNormal = Vector3.Dot(hitInfo.hitRay.direction, hitInfo.hitPointNormal);
            RTHitable hit = hitInfo.hitable;

            //Lambertian Term
            float lambertianTerm = hit.lambertCoefficient * lightDirDotNormal;

            //Phong Term
            float reflect = 2.0f * rayDirDotNormal;
            Vector3 phongDirection = hitInfo.hitRay.direction - reflect * hitInfo.hitPointNormal;
            float phongTerm = Mathf.Max(Vector3.Dot(phongDirection, hitInfo.hitRay.direction), 0f);
            phongTerm = hit.reflectionRate * Mathf.Pow(phongTerm, hit.phongPower) * hit.phongCoefficient;

            //Blinn-Phong Term
            float blinnTerm = 0f;
            Vector3 blinnDirection = lightDir - hitInfo.hitRay.direction;
            float temp = Mathf.Sqrt(Vector3.Dot(blinnDirection, blinnDirection));
            if (temp > 0f) {
                blinnDirection = (1f / temp) * blinnDirection;
                blinnTerm = Mathf.Max(Vector3.Dot(blinnDirection, hitInfo.hitPointNormal), 0f);
                blinnTerm = hit.reflectionRate * Mathf.Pow(blinnTerm, hit.blinnPhongPower) * hit.blinnPhongCoefficient;
            }

            Vector3 termVal = light.color * light.intensity * (lambertianTerm + phongTerm + blinnTerm);
            result += new Color(termVal.x, termVal.y, termVal.z);
        }

        return result;
    }

    private RTHitInfo GetClosetHitInfo(RTRay ray) {

        ray.direction.Normalize();

        RTHitInfo hitInfo = null;
        float hitPointDistance = float.MaxValue;

        foreach (RTHitable hitable in hitables) {
            RTHitInfo localHitInfo = hitable.CheckCollision(ray);
            if (localHitInfo != null) {
                if (hitInfo == null) {
                    hitInfo = localHitInfo;
                }
                else {
                    float localHitPointDistance = (localHitInfo.hitPoint - ray.origin).magnitude;
                    if (localHitPointDistance < hitPointDistance) {
                        hitInfo = localHitInfo;
                    }
                }
            }
        }

        return hitInfo;

    }

    private void RayTraceStep() {

        HashSet<RTRay> raysToCheck = new HashSet<RTRay>(activeRays);

        foreach(RTRay ray in raysToCheck) {

            ray.direction.Normalize();

            RTHitInfo hitInfo = null;
            float hitPointDistance = float.MaxValue;

            foreach(RTHitable hitable in hitables) {
                RTHitInfo localHitInfo = hitable.CheckCollision(ray);
                if (localHitInfo != null) {
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
            result.x += GatherLightColor(ray.hitInfo).x + ray.hitInfo.hitable.albedo.x;
            result.y += GatherLightColor(ray.hitInfo).y + ray.hitInfo.hitable.albedo.y;
            result.z += GatherLightColor(ray.hitInfo).z + ray.hitInfo.hitable.albedo.z;
            if (ray.hitInfo.reflection != null) {
                result += ray.hitInfo.hitable.reflectionRate * TraceRayColor(ray.hitInfo.reflection);
            }
            if (ray.hitInfo.refraction != null) {
                result += ray.hitInfo.hitable.refractionRate * TraceRayColor(ray.hitInfo.refraction);
            }
        }

        return result;
    }

    private Vector3 GatherLightColor(RTHitInfo hitInfo) {

        Vector3 result = Vector3.zero;

        foreach (RTDirectionalLight light in directionalLights) {

            bool isInShadow = false;
            RTRay lightRay = new RTRay(hitInfo.hitPoint, -light.direction, null);

            foreach (RTHitable hitable in hitables) {
                RTHitInfo local = hitable.CheckCollision(lightRay);
                if (local != null) {
                    isInShadow = true;
                    break;
                }
            }

            if (isInShadow) {
                continue;
            }

            float lightDirDotNormal = Vector3.Dot(light.direction, hitInfo.hitPointNormal);
            float rayDirDotNormal = Vector3.Dot(hitInfo.hitRay.direction, hitInfo.hitPointNormal);
            RTHitable hit = hitInfo.hitable;

            //Lambertian Term
            float lambertianTerm = hit.lambertCoefficient * lightDirDotNormal;

            //Phong Term
            float reflect = 2.0f * rayDirDotNormal;
            Vector3 phongDirection = hitInfo.hitRay.direction - reflect * hitInfo.hitPointNormal;
            float phongTerm = Mathf.Max(Vector3.Dot(phongDirection, hitInfo.hitRay.direction), 0f);
            phongTerm = hit.reflectionRate * Mathf.Pow(phongTerm, hit.phongPower) * hit.phongCoefficient;

            //Blinn-Phong Term
            float blinnTerm = 0f;
            Vector3 blinnDirection = -light.direction - hitInfo.hitRay.direction;
            float temp = Mathf.Sqrt(Vector3.Dot(blinnDirection, blinnDirection));
            if (temp > 0f) {
                blinnDirection = (1f / temp) * blinnDirection;
                blinnTerm = Mathf.Max(Vector3.Dot(blinnDirection, hitInfo.hitPointNormal), 0f);
                blinnTerm = hit.reflectionRate * Mathf.Pow(blinnTerm, hit.blinnPhongPower) * hit.blinnPhongCoefficient;
            }

            result += light.color * light.intensity * (lambertianTerm + phongTerm + blinnTerm);
        }

        //foreach (RTPointLight light in pointLights) {
        //    float distance = (hitInfo.hitPoint - light.position).magnitude;

        //    if (distance > light.range) {
        //        continue;
        //    }

        //    bool isInShadow = false;
        //    RTRay lightRay = new RTRay(light.position, hitInfo.hitPoint - light.position, null);

        //    foreach (RTHitable hitable in hitables) {
        //        RTHitInfo local = hitable.CheckCollision(lightRay);
        //        if (local != null) {
        //            float newDist = (local.hitPoint - light.position).magnitude;
        //            if (newDist < distance) {
        //                isInShadow = true;
        //                break;
        //            }
        //        }
        //    }

        //    float intensity = isInShadow ? 0 : light.intensity / (light.range - distance + 1);

        //    result.x += light.color.x * intensity;
        //    result.y += light.color.y * intensity;
        //    result.z += light.color.z * intensity;

        //}

        return result;
    }

}
