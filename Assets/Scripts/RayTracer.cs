using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using System.Threading;

public class TraceColorJob {
    public int x, y;
    public float weight;
    public RTRay ray;
    public TraceColorJob(int x, int y, float weight, RTRay ray) {
        this.x = x;
        this.y = y;
        this.weight = weight;
        this.ray = ray;
    }
}

public class TraceColorThreadParameter {

    public List<TraceColorJob> traceColorJobs;

    public TraceColorThreadParameter(List<TraceColorJob> traceColorJobs) {
        this.traceColorJobs = traceColorJobs;
    }
}

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
    public Color[][] screenPixels;

    public Texture2D renderTexture;

    public static float REFRACTION_DISTANCE = 0.00001f;
    public static float REFRACTION_FACTOR = 0.6f;
    public static int MAX_RAY_DEPTH = 3;
    public static float SHADOW_FACTOR = 0.1f;

    public static float LIGHT_INTENSITY_FACTOR = 0.8f;

    public static int RAYS_SPAWNDED = 0;

    public static int CPU_NUM = 12;

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
                    if(x != 768 || y != 490) {
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

    public void RayTrace() {

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        RAYS_SPAWNDED = 0;

        GenerateScreenPoints();

        renderTexture = new Texture2D(mCamera.xResolution, mCamera.yResolution);

        screenRays = new RTRay[mCamera.yResolution][];

        List<Thread> TCThreads = new List<Thread>();

        List<TraceColorJob> jobs = new List<TraceColorJob>();

        int TCBatchNum = CPU_NUM;
        int TCBatchSize = mCamera.yResolution * mCamera.xResolution / TCBatchNum;
        int TCBatchIndex = 0;
        int TCBatchCurrent = 0;
        int TCBatchEnd = TCBatchSize;

        for (int y = 0; y < mCamera.yResolution; ++y) {
            screenRays[y] = new RTRay[mCamera.xResolution];
            for (int x = 0; x < mCamera.xResolution; ++x) {
                screenRays[y][x] = GetScreenRay(x, y);

                //if(TCBatchCurrent == TCBatchEnd) {

                //    Thread thread = new Thread(new ParameterizedThreadStart(TraceColorThread));
                //    thread.Start(jobs);
                //    TCThreads.Add(thread);

                //    TCBatchCurrent = 0;
                //    TCBatchIndex++;
                //    TCBatchEnd = Mathf.Min(TCBatchEnd + TCBatchSize, mCamera.yResolution * mCamera.xResolution);
                //    jobs = new List<TraceColorJob>();
                //}

                //jobs.Add(new TraceColorJob(x, y, 1.0f, screenRays[y][x]));

                Color pixel = Color.black;
                foreach (Vector3 offset in superSampleKernals[superSampleKernalIndex]) {
                    Vector3 pos = screenPoints[y][x];

                    pos.x += offset.x;
                    pos.y += offset.y;

                    RTRay ray = new RTRay(mCamera.position, pos - mCamera.position, null);

                    pixel += offset.z * TraceColor(screenRays[y][x], 1);

                }
                renderTexture.SetPixel(x, mCamera.yResolution - y, pixel);
            }
        }

        //foreach(Thread thread in TCThreads) {
        //    thread.Join();
        //}

        //for (int y = 0; y < mCamera.yResolution; ++y) {
        //    for (int x = 0; x < mCamera.xResolution; ++x) {
        //        renderTexture.SetPixel(x, mCamera.yResolution - y, screenPixels[y][x]);
        //    }
        //}

        renderTexture.Apply();

        renderTexture.filterMode = FilterMode.Point;

        rendererGO.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", renderTexture);

        sw.Stop();

        Debug.Log("Total Ray Spawned: " + RAYS_SPAWNDED);
        Debug.Log("Ray Tracing Finished: " + sw.Elapsed);
    }

    private void TraceColorThread(object parameter) {
        TraceColorThreadParameter realParameter = (TraceColorThreadParameter)parameter;
        foreach(TraceColorJob TCJ in realParameter.traceColorJobs) {
            screenPixels[TCJ.y][TCJ.x] = TCJ.weight * TraceColor(TCJ.ray, 1);
        }
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

            RTRay lightTraceRay = new RTRay(hitInfo.hitPoint, -L, null);

            float shadowFactor = 1.0f;

            foreach (RTHitable hitable in hitables) {
                if (hitable == hitInfo.hitable) {
                    continue;
                }
                RTHitInfo hit = hitable.CheckCollision(lightTraceRay);
                if (hit != null) {
                    shadowFactor = SHADOW_FACTOR;
                    break;
                }
            }

            result += intensityFactor * shadowFactor * PhongShadingColor(Ka, Kd, Ks, spec, N, L, E, NDotE) * light.color;
        }

        foreach (RTPointLight light in pointLights) {

            Vector3 L = H - light.position;

            float distance = L.magnitude;

            L.Normalize();

            if (distance > light.range) {
                continue;
            }

            float intensityFactor = light.intensity * (1.0f - distance / light.range);

            RTRay lightRay = new RTRay(hitInfo.hitPoint, -L, null);

            float shadowFactor = AccumulateShadowFactor(lightRay, hitInfo.hitable, distance, light.position);

            result += intensityFactor * shadowFactor * PhongShadingColor(Ka, Kd, Ks, spec, N, L, E, NDotE) * light.color;
        }

        ambientTerm = (mCamera.ambientLightColor * mCamera.ambientLightIntensity) * Ka;

        result += ambientTerm;

        return LIGHT_INTENSITY_FACTOR * result;
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

        Vector3 R = L + 2.0f * NDotL * N;
        R.Normalize();
        float RDotE = Mathf.Clamp01(Vector3.Dot(-R, E));
        float powedRDotE = Mathf.Pow(RDotE, spec);

        return NDotL * Kd + powedRDotE * Ks;
    }

    private RTHitInfo GetClosetHitInfo(RTRay ray) {
        return GetClosetHitInfo(ray, null);
    }

    private RTHitInfo GetClosetHitInfo(RTRay ray, HashSet<RTHitable> ignoreSet) {

        ray.direction.Normalize();

        RTHitInfo hitInfo = null;
        float hitPointDistance = float.MaxValue;

        foreach (RTHitable hitable in hitables) {
            if (ignoreSet != null && ignoreSet.Contains(hitable)) {
                continue;
            }
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

    private float AccumulateShadowFactor(RTRay lightTraceRay, RTHitable self, float LHDist, Vector3 lightPosition) {

        float shadowFactor = 1f;

        lightTraceRay.direction.Normalize();

        foreach (RTHitable hitable in hitables) {
            if (hitable == self) {
                continue;
            }
            RTHitInfo localHitInfo = hitable.CheckCollision(lightTraceRay);
            if (localHitInfo != null) {

                Vector3 localH = localHitInfo.hitPoint;
                Vector3 localLH = localH - lightPosition;

                if(Vector3.Dot(localLH, -lightTraceRay.direction) > 0 && localLH.magnitude < LHDist) {
                    shadowFactor *= Mathf.Clamp01(localHitInfo.hitable.refractionRate + SHADOW_FACTOR);
                }

            }
        }

        return shadowFactor;

    }

    private void OnGUI() {
        GUI.DrawTexture(new Rect(0, 0, mCamera.xResolution, mCamera.yResolution), renderTexture);
    }

}
