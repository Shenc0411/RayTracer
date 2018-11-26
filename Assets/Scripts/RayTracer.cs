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

public class RayTracer : MonoBehaviour {

    public RTCamera mCamera;
    public GameObject sceneParentGO;
    public List<RTHitable> hitables;
    public List<RTDirectionalLight> directionalLights;
    public List<RTPointLight> pointLights;
    public RTRay[][] screenRays;
    public Vector3[][] screenPoints;
    public Color[][] screenPixels;

    public Texture2D renderTexture;
    
    public static Vector3[] superSampleKernalRegular = { new Vector3(-0.4f, 0.4f, 1.0f / 9.0f), new Vector3(0.0f, 0.4f, 1.0f / 9.0f), new Vector3(0.4f, 0.4f, 1.0f / 9.0f),
                                                    new Vector3(-0.4f, 0.0f, 1.0f / 9.0f), new Vector3(0.0f, 0.0f, 1.0f / 9.0f), new Vector3(0.4f, 0.0f, 1.0f / 9.0f),
                                                    new Vector3(-0.4f, -0.4f, 1.0f / 9.0f), new Vector3(0.0f, -0.4f, 1.0f / 9.0f), new Vector3(0.4f, -0.4f, 1.0f / 9.0f)};
    public static Vector3[] nonSuperSampleKernal = { new Vector3(0f, 0f, 1f)};

    public static Vector3[][] superSampleKernals = { nonSuperSampleKernal, superSampleKernalRegular };

    public static int superSampleKernalIndex = 0;

    public float DIRECTIONAL_LIGHT_DISTANCE = 1000f;
    public float HIT_POINT_OFFSET = 0.00001f;
    public float REFRACTION_FACTOR = 0.6f;
    public float SHADOW_FACTOR = 0.1f;
    public float LIGHT_INTENSITY_FACTOR = 0.8f;
    public int MAX_RAY_DEPTH = 1;
    public bool multiThreadMode = true;
    public bool renderShadows = true;
    public int CPU_NUM = 12;

    private void Awake() {

        hitables = new List<RTHitable>();
        directionalLights = new List<RTDirectionalLight>();
        pointLights = new List<RTPointLight>();

        renderTexture = new Texture2D(mCamera.xResolution, mCamera.yResolution);


        foreach (RTHitable hitable in sceneParentGO.GetComponentsInChildren<RTHitable>()) {
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
        //RayTrace();
    }

    private void Update() {

        //RenderRays();
        //RenderScreenPlane();

    }

    private void RenderScreenPlane() {

        float realHalfY = Mathf.Tan(Mathf.Deg2Rad * mCamera.FOV / 2.0f) * mCamera.nearPlaneDistance;
        float realHalfX = realHalfY * mCamera.xResolution / mCamera.yResolution;


        int halfXRes = mCamera.xResolution / 2;
        int halfYRes = mCamera.yResolution / 2;

        Vector3[] screenVertices = new Vector3[4];
        screenVertices[0] = new Vector3(-realHalfX, realHalfY, mCamera.nearPlaneDistance);
        screenVertices[1] = new Vector3(realHalfX, realHalfY, mCamera.nearPlaneDistance);
        screenVertices[2] = new Vector3(realHalfX, -realHalfY, mCamera.nearPlaneDistance);
        screenVertices[3] = new Vector3(-realHalfX, -realHalfY, mCamera.nearPlaneDistance);

        screenVertices[0] = mCamera.transform.TransformPoint(screenVertices[0]);
        screenVertices[1] = mCamera.transform.TransformPoint(screenVertices[1]);
        screenVertices[2] = mCamera.transform.TransformPoint(screenVertices[2]);
        screenVertices[3] = mCamera.transform.TransformPoint(screenVertices[3]);

        Debug.DrawLine(screenVertices[0], screenVertices[1], Color.green);
        Debug.DrawLine(screenVertices[1], screenVertices[2], Color.green);
        Debug.DrawLine(screenVertices[2], screenVertices[3], Color.green);
        Debug.DrawLine(screenVertices[3], screenVertices[0], Color.green);
    }

    private void RenderRays() {
        for (int y = 0; y < mCamera.yResolution; ++y) {
            for (int x = 0; x < mCamera.xResolution; ++x) {
                if (screenRays != null && screenRays[y] != null) {
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
            Debug.DrawRay(ray.hitInfo.hitPoint, ray.hitInfo.hitPointNormal, Color.blue);
        }
        else {
            Debug.DrawRay(ray.origin, ray.direction, Color.red);
        }
    }

    private void GenerateScreenPoints() {

        screenPoints = new Vector3[mCamera.yResolution][];

        float realHalfY = Mathf.Tan(Mathf.Deg2Rad * mCamera.FOV / 2.0f) * mCamera.nearPlaneDistance;
        float realHalfX = realHalfY * mCamera.xResolution / mCamera.yResolution;

        int halfXRes = mCamera.xResolution / 2;
        int halfYRes = mCamera.yResolution / 2;

        float xScale = realHalfX / halfXRes;
        float yScale = realHalfY / halfYRes;

        for (int y = 0; y < mCamera.yResolution; ++y) {
            screenPoints[y] = new Vector3[mCamera.xResolution];
            for (int x = 0; x < mCamera.xResolution; ++x) {
                screenPoints[y][x] = new Vector3(-realHalfX + x * xScale, realHalfY - y * yScale, mCamera.nearPlaneDistance);
                screenPoints[y][x] = mCamera.transform.TransformPoint(screenPoints[y][x]);
            }
        }

    }

    private RTRay GetScreenRay(int x, int y) {
        return new RTRay(mCamera.position, screenPoints[y][x] - mCamera.position, null);
    }

    public void RayTrace() {

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        RTRay.RAYS_SPAWNDED = 0;

        GenerateScreenPoints();

        screenRays = new RTRay[mCamera.yResolution][];
        screenPixels = new Color[mCamera.yResolution][];
        List<Thread> TCThreads = new List<Thread>();

        List<TraceColorJob> jobs = new List<TraceColorJob>();

        int TCBatchNum = CPU_NUM;
        int TCBatchSize = mCamera.yResolution * mCamera.xResolution / TCBatchNum;

        for (int y = 0; y < mCamera.yResolution; ++y) {
            screenRays[y] = new RTRay[mCamera.xResolution];
            screenPixels[y] = new Color[mCamera.xResolution];
            for (int x = 0; x < mCamera.xResolution; ++x) {
                screenRays[y][x] = GetScreenRay(x, y);
                screenPixels[y][x] = Color.black;

                if (multiThreadMode) {
                    if (jobs.Count >= TCBatchSize) {
                        Thread thread = new Thread(new ParameterizedThreadStart(TraceColorThread));
                        thread.Start(jobs);
                        TCThreads.Add(thread);
                        jobs = new List<TraceColorJob>();

                    }

                    jobs.Add(new TraceColorJob(x, y, 1.0f, screenRays[y][x]));
                }
                else {
                    Color pixel = Color.black;

                    foreach (Vector3 offset in superSampleKernals[superSampleKernalIndex]) {
                        Vector3 pos = screenPoints[y][x];

                        pos.x += offset.x;
                        pos.y += offset.y;

                        RTRay ray = new RTRay(mCamera.position, pos - mCamera.position, null);

                        pixel += offset.z * TraceColor(screenRays[y][x], 1);

                    }

                    renderTexture.SetPixel(x, mCamera.yResolution - y - 1, pixel);
                }

            }
        }


        if (multiThreadMode) {
            Thread threadl = new Thread(new ParameterizedThreadStart(TraceColorThread));
            threadl.Start(jobs);
            TCThreads.Add(threadl);

            foreach (Thread thread in TCThreads) {
                thread.Join();
            }

            for (int y = 0; y < mCamera.yResolution; ++y) {
                for (int x = 0; x < mCamera.xResolution; ++x) {
                    renderTexture.SetPixel(x, mCamera.yResolution - y - 1, screenPixels[y][x]);
                }
            }
        }
        

        renderTexture.Apply();

        renderTexture.filterMode = FilterMode.Point;

        //rendererGO.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", renderTexture);

        sw.Stop();

        Debug.Log("Total Ray Spawned: " + RTRay.RAYS_SPAWNDED);
        Debug.Log("Ray Tracing Finished: " + sw.Elapsed);
    }

    private void TraceColorThread(object parameter) {
        List<TraceColorJob> jobList = (List<TraceColorJob>)parameter;
        foreach(TraceColorJob job in jobList) {
            screenPixels[job.y][job.x] = job.weight * TraceColor(job.ray, 1);
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

            result = Mathf.Clamp01(1 - hitInfo.hitable.reflectionRate - hitInfo.hitable.refractionRate) * TraceLightColor(hitInfo);

            if (hitInfo.hitable.reflectionRate > 0 && hitInfo.hitable.refractionRate > 0) {

                Vector3 normalProjVec = hitInfo.hitPointNormal * Vector3.Dot(ray.direction, hitInfo.hitPointNormal);

                result += hitInfo.hitable.reflectionRate * TraceColor(new RTRay(hitInfo.hitPoint + hitInfo.hitPointNormal * HIT_POINT_OFFSET, ray.direction - 2.0f * normalProjVec, null), depth + 1);

                result += hitInfo.hitable.refractionRate * TraceColor(new RTRay(hitInfo.hitPoint - hitInfo.hitPointNormal * HIT_POINT_OFFSET, normalProjVec + (ray.direction - normalProjVec) * REFRACTION_FACTOR, null), depth + 1);

            }
            else if (hitInfo.hitable.reflectionRate > 0) {
                Vector3 normalProjVec = hitInfo.hitPointNormal * Vector3.Dot(ray.direction, hitInfo.hitPointNormal);

                result += hitInfo.hitable.reflectionRate * TraceColor(new RTRay(hitInfo.hitPoint + hitInfo.hitPointNormal * HIT_POINT_OFFSET, ray.direction - 2.0f * normalProjVec, null), depth + 1);
            }
            else if (hitInfo.hitable.refractionRate > 0) {

                Vector3 normalProjVec = hitInfo.hitPointNormal * Vector3.Dot(ray.direction, hitInfo.hitPointNormal);

                result += hitInfo.hitable.refractionRate * TraceColor(new RTRay(hitInfo.hitPoint - hitInfo.hitPointNormal * HIT_POINT_OFFSET, normalProjVec + (ray.direction - normalProjVec) * REFRACTION_FACTOR, null), depth + 1);
            }
        }
        
        return result;
    }

    private Color TraceLightColor(RTHitInfo hitInfo) {

        Color result = Color.black;
        
        Vector3 N = hitInfo.hitPointNormal;
        Vector3 E = hitInfo.hitRay.direction;
        Vector3 H = hitInfo.hitPoint;

        float NDotE = Vector3.Dot(N, -E);

        foreach (RTDirectionalLight light in directionalLights) {

            Vector3 L = light.direction;

            float intensityFactor = light.intensity;

            RTRay lightRay = new RTRay(H, -L, null);

            float shadowFactor = AccumulateShadowFactor(lightRay, hitInfo.hitable, DIRECTIONAL_LIGHT_DISTANCE * DIRECTIONAL_LIGHT_DISTANCE);

            result += intensityFactor * shadowFactor * PhongShadingColor(hitInfo.hitable.Kd, hitInfo.hitable.Ks, hitInfo.hitable.spec, N, L, E, NDotE) * light.color;
        }

        foreach (RTPointLight light in pointLights) {

            Vector3 L = H - light.position;

            float distance = L.magnitude;

            if (distance > light.range) {
                continue;
            }

            L = L / distance;

            float intensityFactor = light.intensity * (1.0f - distance / light.range);

            RTRay lightRay = new RTRay(hitInfo.hitPoint, -L, null);

            float shadowFactor = AccumulateShadowFactor(lightRay, hitInfo.hitable, distance * distance);

            result += intensityFactor * shadowFactor * PhongShadingColor(hitInfo.hitable.Kd, hitInfo.hitable.Ks, hitInfo.hitable.spec, N, L, E, NDotE) * light.color;
        }

        result += (mCamera.ambientLightColor * mCamera.ambientLightIntensity) * hitInfo.hitable.Ka;

        return LIGHT_INTENSITY_FACTOR * result;
    }

    private Color PhongShadingColor(Color Kd, Color Ks, float spec, Vector3 N, Vector3 L, Vector3 E, float NDotE) {

        float NDotL = Vector3.Dot(N, -L);

        if (NDotE > 0 && NDotL > 0) {

        }
        else if (NDotE < 0 && NDotL < 0) {
            N = -N;
            NDotL = -NDotL;
            NDotE = -NDotE;
        }
        else {
            return Color.black;
        }

        Vector3 R = L + 2.0f * NDotL * N;
        R.Normalize();
        float RDotE = Mathf.Clamp01(Vector3.Dot(-R, E));
        float powedRDotE = Mathf.Pow(RDotE, spec);

        return NDotL * Kd + powedRDotE * Ks;
    }

    private RTHitInfo GetClosetHitInfo(RTRay ray, HashSet<RTHitable> ignoreSet) {
        throw new System.Exception("Not Implemented");
    }

    private RTHitInfo GetClosetHitInfo(RTRay ray) {

        RTHitInfo hitInfo = null;
        float hitPointDistanceSqr = float.MaxValue;

        foreach (RTHitable hitable in hitables) {
            RTHitInfo localHitInfo = hitable.CheckCollision(ray);
            if (localHitInfo != null) {
                Vector3 hitVec = localHitInfo.hitPoint - ray.origin;
                if (hitInfo == null) {
                    hitInfo = localHitInfo;
                    hitPointDistanceSqr = Vector3.Dot(hitVec, hitVec);
                }
                else if (Vector3.Dot(hitVec, hitVec) < hitPointDistanceSqr) {
                    hitInfo = localHitInfo;
                }
            }
        }

        return hitInfo;
    }

    private float AccumulateShadowFactor(RTRay lightTraceRay, RTHitable self, float LHDistSqr) {

        if (!renderShadows) {
            return 1.0f;
        }

        float shadowFactor = 1f;

        foreach (RTHitable hitable in hitables) {
            if (hitable == self) {
                continue;
            }
            
            RTHitInfo localHitInfo = hitable.CheckCollision(lightTraceRay);

            if (localHitInfo != null) {

                Vector3 localLH = localHitInfo.hitPoint - lightTraceRay.origin;

                if(Vector3.Dot(localLH, localLH) < LHDistSqr) {
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
