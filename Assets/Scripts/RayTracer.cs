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

    public static float REFRACTION_DISTANCE = 0.00001f;
    public static float REFRACTION_FACTOR = 0.8f;
    public static int MAX_RAY_DEPTH = 3;
    public static float SHADOW_FACTOR = 0.1f;
    

    public static Vector3[] superSampleKernalRegular = { new Vector3(-0.4f, 0.4f, 1.0f / 9.0f), new Vector3(0.0f, 0.4f, 1.0f / 9.0f), new Vector3(0.4f, 0.4f, 1.0f / 9.0f),
                                                    new Vector3(-0.4f, 0.0f, 1.0f / 9.0f), new Vector3(0.0f, 0.0f, 1.0f / 9.0f), new Vector3(0.4f, 0.0f, 1.0f / 9.0f),
                                                    new Vector3(-0.4f, -0.4f, 1.0f / 9.0f), new Vector3(0.0f, -0.4f, 1.0f / 9.0f), new Vector3(0.4f, -0.4f, 1.0f / 9.0f)};
    public static Vector3[] nonSuperSampleKernal = { new Vector3(0f, 0f, 1f)};

    public static Vector3[][] superSampleKernals = { nonSuperSampleKernal, superSampleKernalRegular };

    public static int superSampleKernalIndex = 0;

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
                if (screenRays != null && screenRays[y] != null) {
                    if(x % 256 != 0 || y % 256 != 0) {
                        continue;
                    }
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
            //RenderRay(ray.hitInfo.reflection);
            RenderRay(ray.hitInfo.refraction);
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
                Color pixel = Color.black;
                foreach(Vector3 offset in superSampleKernals[superSampleKernalIndex]) {
                    Vector3 pos = screenPoints[y][x];
                    pos.x += offset.x;
                    pos.y += offset.y;
                    RTRay ray = new RTRay(mCamera.position, pos - mCamera.position, null);
                    pixel += offset.z * TraceColor(screenRays[y][x], 1);
                }
                renderTexture.SetPixel(x, mCamera.yResolution - y, pixel);
            }
        }

        renderTexture.Apply();

        renderTexture.filterMode = FilterMode.Point;

        rendererGO.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", renderTexture);
    }

    private Color TraceColor(RTRay ray, int depth) {
        Color result = Color.black;

        if(depth > MAX_RAY_DEPTH || ray == null) {
            return result;
        }

        RTHitInfo hitInfo = GetClosetHitInfo(ray);

        if (hitInfo != null) {

            ray.hitInfo = hitInfo;

            Color lightColor = TraceLightColor(hitInfo);
            Color reflectionColor = Color.black;
            Color refractionColor = Color.black;

            reflectionColor = hitInfo.hitable.reflectionRate * TraceColor(hitInfo.reflection, depth + 1);
            refractionColor = hitInfo.hitable.refractionRate * TraceColor(hitInfo.refraction, depth + 1);

            float phongCoefficient = Mathf.Clamp01(1 - hitInfo.hitable.reflectionRate - hitInfo.hitable.refractionRate);
            result = phongCoefficient * lightColor + reflectionColor + refractionColor;
        }

        return result;
    }

    private Color TraceLightColor(RTHitInfo hitInfo) {

        Color result = Color.black;

        Color ambientTerm = Color.black; 
        
        hitInfo.hitPointNormal.Normalize();
        hitInfo.hitRay.direction.Normalize();
        Vector3 N = hitInfo.hitPointNormal;
        Vector3 E = hitInfo.hitRay.direction;
        Vector3 H = hitInfo.hitPoint;

        float NDotE = Vector3.Dot(N, -E);

        Color Kd = hitInfo.hitable.Kd;
        Color Ks = hitInfo.hitable.Ks;
        Color Ka = hitInfo.hitable.Ka;

        float spec = hitInfo.hitable.spec;

        foreach (RTDirectionalLight light in directionalLights) {

            Vector3 L = light.direction;

            float intensityFactor = light.intensity;

            float shadowFactor = 1f;
            //RTRay lightRay = new RTRay(hitInfo.hitPoint, -L, null);

            //foreach (RTHitable hitable in hitables) {
            //    RTHitInfo hit = hitable.CheckCollision(lightRay);
            //    if (hit != null) {
            //        shadowFactor = SHADOW_FACTOR;
            //        break;
            //    }
            //}

            //Color local = Color.black;

            result += intensityFactor * shadowFactor * PhongShadingColor(Ka, Kd, Ks, spec, N, L, E, NDotE) * light.color;
        }

        foreach (RTPointLight light in pointLights) {

            Vector3 L = (H - light.position).normalized;

            float distance = L.magnitude;

            if (distance > light.range) {
                continue;
            }

            float intensityFactor = light.intensity * (1.0f - distance / light.range);

            float shadowFactor = 1f;
            RTRay lightRay = new RTRay(hitInfo.hitPoint, -L, null);

            foreach (RTHitable hitable in hitables) {
                if(hitable == hitInfo.hitable) {
                    continue;
                }
                RTHitInfo hit = hitable.CheckCollision(lightRay);
                if (hit != null && !hit.hitable.isTransparent) {
                    shadowFactor = SHADOW_FACTOR;
                    break;
                }
            }

            result += intensityFactor * shadowFactor * PhongShadingColor(Ka, Kd, Ks, spec, N, L, E, NDotE) * light.color;
        }

        ambientTerm = (mCamera.ambientLightColor * mCamera.ambientLightIntensity) * Ka;

        result += ambientTerm;

        return result;
    }

    private Color PhongShadingColor(Color Ka, Color Kd, Color Ks, float spec, Vector3 N, Vector3 L, Vector3 E, float NDotE) {

        Color result = Color.black;

        Color local = Color.black;

        float NDotL = Vector3.Dot(N, -L);

        if (NDotE > 0 && NDotL > 0) {

        }
        else if (NDotE < 0 && NDotL < 0) {
            N = -N;
            NDotL = -NDotL;
            NDotE = -NDotE;
        }
        else {
            return result;
        }

        Vector3 R = L - 2.0f * NDotL * N;
        R.Normalize();
        float RDotE = Mathf.Clamp01(Vector3.Dot(-R, E));
        float powedRDotE = Mathf.Pow(RDotE, spec);

        return NDotL * Kd + powedRDotE * Ks;
    }

    private RTHitInfo GetClosetHitInfo(RTRay ray) {

        ray.direction.Normalize();

        RTHitInfo hitInfo = null;
        float hitPointDistance = float.MaxValue;

        foreach (RTHitable hitable in hitables) {
            RTHitInfo localHitInfo = hitable.CheckCollision(ray);
            if (localHitInfo != null) {
                float localHitPointDistance = (localHitInfo.hitPoint - ray.origin).magnitude;
                if (hitInfo == null) {
                    hitInfo = localHitInfo;
                    hitPointDistance = localHitPointDistance;
                }
                else {
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


    //private Vector3 TraceRayColor(RTRay ray) {
    //    Vector3 result = Vector3.zero;

    //    if (ray.hitInfo != null) {
    //        result.x += GatherLightColor(ray.hitInfo).x + ray.hitInfo.hitable.albedo.x;
    //        result.y += GatherLightColor(ray.hitInfo).y + ray.hitInfo.hitable.albedo.y;
    //        result.z += GatherLightColor(ray.hitInfo).z + ray.hitInfo.hitable.albedo.z;
    //        if (ray.hitInfo.reflection != null) {
    //            result += ray.hitInfo.hitable.reflectionRate * TraceRayColor(ray.hitInfo.reflection);
    //        }
    //        if (ray.hitInfo.refraction != null) {
    //            result += ray.hitInfo.hitable.refractionRate * TraceRayColor(ray.hitInfo.refraction);
    //        }
    //    }

    //    return result;
    //}

    //private Vector3 GatherLightColor(RTHitInfo hitInfo) {

    //    Vector3 result = Vector3.zero;

    //    foreach (RTDirectionalLight light in directionalLights) {

    //        bool isInShadow = false;
    //        RTRay lightRay = new RTRay(hitInfo.hitPoint, -light.direction, null);

    //        foreach (RTHitable hitable in hitables) {
    //            RTHitInfo local = hitable.CheckCollision(lightRay);
    //            if (local != null) {
    //                isInShadow = true;
    //                break;
    //            }
    //        }

    //        if (isInShadow) {
    //            continue;
    //        }

    //        float lightDirDotNormal = Vector3.Dot(light.direction, hitInfo.hitPointNormal);
    //        float rayDirDotNormal = Vector3.Dot(hitInfo.hitRay.direction, hitInfo.hitPointNormal);
    //        RTHitable hit = hitInfo.hitable;

    //        Lambertian Term
    //        float lambertianTerm = hit.lambertCoefficient * lightDirDotNormal;

    //        Phong Term
    //        float reflect = 2.0f * rayDirDotNormal;
    //        Vector3 phongDirection = hitInfo.hitRay.direction - reflect * hitInfo.hitPointNormal;
    //        float phongTerm = Mathf.Max(Vector3.Dot(phongDirection, hitInfo.hitRay.direction), 0f);
    //        phongTerm = hit.reflectionRate * Mathf.Pow(phongTerm, hit.phongPower) * hit.phongCoefficient;

    //        Blinn - Phong Term
    //        float blinnTerm = 0f;
    //        Vector3 blinnDirection = -light.direction - hitInfo.hitRay.direction;
    //        float temp = Mathf.Sqrt(Vector3.Dot(blinnDirection, blinnDirection));
    //        if (temp > 0f) {
    //            blinnDirection = (1f / temp) * blinnDirection;
    //            blinnTerm = Mathf.Max(Vector3.Dot(blinnDirection, hitInfo.hitPointNormal), 0f);
    //            blinnTerm = hit.reflectionRate * Mathf.Pow(blinnTerm, hit.blinnPhongPower) * hit.blinnPhongCoefficient;
    //        }

    //        result += light.color * light.intensity * (lambertianTerm + phongTerm + blinnTerm);
    //    }

    //    foreach (RTPointLight light in pointLights) {
    //        float distance = (hitInfo.hitPoint - light.position).magnitude;

    //        if (distance > light.range) {
    //            continue;
    //        }

    //        bool isInShadow = false;
    //        RTRay lightRay = new RTRay(light.position, hitInfo.hitPoint - light.position, null);

    //        foreach (RTHitable hitable in hitables) {
    //            RTHitInfo local = hitable.CheckCollision(lightRay);
    //            if (local != null) {
    //                float newDist = (local.hitPoint - light.position).magnitude;
    //                if (newDist < distance) {
    //                    isInShadow = true;
    //                    break;
    //                }
    //            }
    //        }

    //        float intensity = isInShadow ? 0 : light.intensity / (light.range - distance + 1);

    //        result.x += light.color.x * intensity;
    //        result.y += light.color.y * intensity;
    //        result.z += light.color.z * intensity;

    //    }

    //    return result;
    //}

    private void OnGUI() {
        GUI.DrawTexture(new Rect(0, 0, mCamera.xResolution, mCamera.yResolution), renderTexture);
    }

}
