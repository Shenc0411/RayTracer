using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using System.Threading;

public enum RenderMode { CPU, GPU };

public static class ComputeShaderStrings{
    public const string CSMain = "CSMain";
    public const string Result = "Result";
    public const string RaySphereCollision = "RaySphereCollision";
    public const string RaySpherePairs = "_RaySpherePairs";
    public const string RayQuadPairs = "_RayQuadPairs";
    public const string RQHitInfos = "_RQHitInfos";
    public const string RSPHitInfos = "_RSPHitInfos";
    public const string RayQuadCollision = "RayQuadCollision";
}

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
    public List<RTSphere> spheres;
    public List<RTQuad> quads;
    public List<RTDirectionalLight> directionalLights;
    public List<RTPointLight> pointLights;
    public RTRay[][] screenRays;
    public Vector3[][] screenPoints;
    public Color[][] screenPixels;

    public Texture2D renderTexture;

    public ComputeShader CS;

    public static Vector3[] superSampleKernal = { new Vector3(-0.52f, 0.38f, 0.128f), new Vector3(0.41f, 0.56f, 0.119f), new Vector3(0.27f, 0.08f, 0.294f),
                                                    new Vector3(-0.17f, -0.29f, 0.249f), new Vector3(0.58f, -0.55f, 0.104f), new Vector3(-0.31f, -0.71f, 0.106f)};

    public static Vector3[] nonSuperSampleKernal = { new Vector3(0f, 0f, 1f)};

    public static Vector3[][] superSampleKernals = { nonSuperSampleKernal, superSampleKernal };

    public static int superSampleKernalIndex = 0;

    public static RayTracer instance;

    public float DIRECTIONAL_LIGHT_DISTANCE = 1000f;
    public float HIT_POINT_OFFSET = 0.00001f;
    public float REFRACTION_FACTOR = 0.6f;
    public float SHADOW_FACTOR = 0.1f;
    public float LIGHT_INTENSITY_FACTOR = 0.8f;
    public int MAX_RAY_DEPTH = 1;
    public int THREAD_NUM = 12;

    public bool multiThreadMode = true;
    public bool renderShadows = true;
    public bool enableSuperSampling = true;
    public bool enableRealTimeRendering = true;

    public RenderMode renderMode;

    public static float xScale, yScale;

    private void Awake() {

        if(instance != null) {
            Destroy(this);
        }

        instance = this;

        hitables = new List<RTHitable>();
        spheres = new List<RTSphere>();
        quads = new List<RTQuad>();

        directionalLights = new List<RTDirectionalLight>();
        pointLights = new List<RTPointLight>();

        renderTexture = null;

        foreach (RTHitable hitable in sceneParentGO.GetComponentsInChildren<RTHitable>()) {
            hitables.Add(hitable);
        }
        foreach (RTSphere sphere in sceneParentGO.GetComponentsInChildren<RTSphere>()) {
            spheres.Add(sphere);
        }
        foreach (RTQuad quad in sceneParentGO.GetComponentsInChildren<RTQuad>()) {
            quads.Add(quad);
        }
        foreach (RTDirectionalLight DL in sceneParentGO.GetComponentsInChildren<RTDirectionalLight>()) {
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
        if (enableRealTimeRendering) {
            Render();
        }

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
        
        Debug.DrawRay(ray.origin, ray.direction, Color.red);

    }

    public void Render() {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        RTRay.RAYS_SPAWNDED = 0;
        GenerateScreenPoints();
        renderTexture = new Texture2D(mCamera.xResolution, mCamera.yResolution);
        renderTexture.filterMode = FilterMode.Point;
        if (renderMode == RenderMode.CPU) {
            CPURayTrace();
        }
        else {
            GPURayTrace();
        }
        renderTexture.Apply();
        sw.Stop();
        Debug.Log("Render Finished: " + sw.Elapsed);
    }

    #region GPURayTracing

    private void GPURayTrace() {

        int raySphereCollisionKernalIndex = CS.FindKernel(ComputeShaderStrings.RaySphereCollision);
        int rayQuadCollisionKernalIndex = CS.FindKernel(ComputeShaderStrings.RayQuadCollision);

        List<RTRay> activeRays = new List<RTRay>();

        screenRays = new RTRay[mCamera.yResolution][];
        
        superSampleKernalIndex = enableSuperSampling ? 1 : 0;

        for (int y = 0; y < mCamera.yResolution; ++y) {
            screenRays[y] = new RTRay[mCamera.xResolution];
            for (int x = 0; x < mCamera.xResolution; ++x) {
                screenRays[y][x] = GetScreenRay(x, y);
                foreach (Vector3 offset in superSampleKernals[superSampleKernalIndex]) {
                    Vector3 pos = screenPoints[y][x];
                    pos.x += offset.x * xScale;
                    pos.y += offset.y * yScale;
                    activeRays.Add(new RTRay(mCamera.position, pos - mCamera.position));
                }
            }
        }

        int raySpherePairCount = spheres.Count * activeRays.Count;
        HitInfoGPU[] raySphereHitInfos = new HitInfoGPU[raySpherePairCount];
        RaySpherePairGPU[] raySpherePairs = new RaySpherePairGPU[raySpherePairCount];
        for (int j = 0; j < spheres.Count; ++j) {
            for (int k = 0; k < activeRays.Count; ++k) {
                raySpherePairs[j * activeRays.Count + k].rayOrigin = activeRays[k].origin;
                raySpherePairs[j * activeRays.Count + k].rayDirection = activeRays[k].direction;
                raySpherePairs[j * activeRays.Count + k].spherePosition = spheres[j].position;
                raySpherePairs[j * activeRays.Count + k].sphereRadius = spheres[j].radius;
            }
        }
        ComputeBuffer raySpherePairsBuffer = new ComputeBuffer(raySpherePairCount, 10 * 4);
        raySpherePairsBuffer.SetData(raySpherePairs);
        ComputeBuffer raySphereHitInfosBuffer = new ComputeBuffer(raySpherePairCount, 7 * 4);
        CS.SetBuffer(raySphereCollisionKernalIndex, ComputeShaderStrings.RaySpherePairs, raySpherePairsBuffer);
        CS.SetBuffer(raySphereCollisionKernalIndex, ComputeShaderStrings.RSPHitInfos, raySphereHitInfosBuffer);
        CS.Dispatch(raySphereCollisionKernalIndex, raySpherePairCount / 32, 1, 1);
        raySphereHitInfosBuffer.GetData(raySphereHitInfos);

        int rayQuadPairCount = quads.Count * activeRays.Count;
        HitInfoGPU[] rayQuadHitInfos = new HitInfoGPU[rayQuadPairCount];
        RayQuadPairGPU[] rayQuadPairs = new RayQuadPairGPU[rayQuadPairCount];
        for (int j = 0; j < quads.Count; ++j) {
            for (int k = 0; k < activeRays.Count; ++k) {
                rayQuadPairs[j * activeRays.Count + k].rayOrigin = activeRays[k].origin;
                rayQuadPairs[j * activeRays.Count + k].rayDirection = activeRays[k].direction;
                rayQuadPairs[j * activeRays.Count + k].A = quads[j].A;
                rayQuadPairs[j * activeRays.Count + k].B = quads[j].B;
                rayQuadPairs[j * activeRays.Count + k].C = quads[j].C;
                rayQuadPairs[j * activeRays.Count + k].AB = quads[j].AB;
                rayQuadPairs[j * activeRays.Count + k].AC = quads[j].AC;
                rayQuadPairs[j * activeRays.Count + k].ABLengthSquared = quads[j].ABLengthSquared;
                rayQuadPairs[j * activeRays.Count + k].ACLengthSquared = quads[j].ACLengthSquared;
                rayQuadPairs[j * activeRays.Count + k].planeNormal = quads[j].planeNormal;
            }
        }
        ComputeBuffer rayQuadPairsBuffer = new ComputeBuffer(rayQuadPairCount, 104);
        rayQuadPairsBuffer.SetData(rayQuadPairs);
        ComputeBuffer rayQuadHitInfosBuffer = new ComputeBuffer(rayQuadPairCount, 7 * 4);
        CS.SetBuffer(rayQuadCollisionKernalIndex, ComputeShaderStrings.RayQuadPairs, rayQuadPairsBuffer);
        CS.SetBuffer(rayQuadCollisionKernalIndex, ComputeShaderStrings.RQHitInfos, rayQuadHitInfosBuffer);
        CS.Dispatch(rayQuadCollisionKernalIndex, rayQuadPairCount / 32, 1, 1);
        rayQuadHitInfosBuffer.GetData(rayQuadHitInfos);


        for(int y = 0; y < mCamera.yResolution; ++y) {
            for (int x = 0; x < mCamera.xResolution; ++x) {
               renderTexture.SetPixel(x, mCamera.yResolution - y - 1, Color.black);
            }
        }

        for (int y = 0; y < mCamera.yResolution; ++y) {
            for (int x = 0; x < mCamera.xResolution; ++x) {

                for (int j = 0; j < quads.Count; ++j) {
                    int index = mCamera.yResolution * mCamera.xResolution * j + y * mCamera.xResolution + x;
                    if (rayQuadHitInfos[index].isHit > 0) {
                        renderTexture.SetPixel(x, mCamera.yResolution - y - 1, Color.blue * Vector3.Dot(-rayQuadHitInfos[index].hitNormal, rayQuadPairs[index].rayDirection));
                    }
                }

                for (int j = 0; j < spheres.Count; ++j) {
                    int index = mCamera.yResolution * mCamera.xResolution * j + y * mCamera.xResolution + x;
                    if (raySphereHitInfos[index].isHit > 0) {
                        renderTexture.SetPixel(x, mCamera.yResolution - y - 1, Color.red * Vector3.Dot(-raySphereHitInfos[index].hitNormal, raySpherePairs[index].rayDirection));
                    }
                }
            }
        }

    }

    #endregion

    
    private void GenerateScreenPoints() {

        screenPoints = new Vector3[mCamera.yResolution][];

        float realHalfY = Mathf.Tan(Mathf.Deg2Rad * mCamera.FOV / 2.0f) * mCamera.nearPlaneDistance;
        float realHalfX = realHalfY * mCamera.xResolution / mCamera.yResolution;

        int halfXRes = mCamera.xResolution / 2;
        int halfYRes = mCamera.yResolution / 2;

        xScale = realHalfX / halfXRes;
        yScale = realHalfY / halfYRes;

        for (int y = 0; y < mCamera.yResolution; ++y) {
            screenPoints[y] = new Vector3[mCamera.xResolution];
            for (int x = 0; x < mCamera.xResolution; ++x) {
                screenPoints[y][x] = new Vector3(-realHalfX + x * xScale, realHalfY - y * yScale, mCamera.nearPlaneDistance);
                screenPoints[y][x] = mCamera.transform.TransformPoint(screenPoints[y][x]);
            }
        }

    }

    private RTRay GetScreenRay(int x, int y) {
        return new RTRay(mCamera.position, screenPoints[y][x] - mCamera.position);
    }

    #region CPURayTracing
    private void CPURayTrace() {

        screenRays = new RTRay[mCamera.yResolution][];
        screenPixels = new Color[mCamera.yResolution][];

        List<Thread> TCThreads = new List<Thread>();

        List<TraceColorJob> jobs = new List<TraceColorJob>();

        int TCBatchNum = THREAD_NUM;
        int TCBatchSize = mCamera.yResolution * mCamera.xResolution * superSampleKernals[superSampleKernalIndex].Length / TCBatchNum;

        superSampleKernalIndex = enableSuperSampling ? 1 : 0;

        for (int y = 0; y < mCamera.yResolution; ++y) {
            screenRays[y] = new RTRay[mCamera.xResolution];
            screenPixels[y] = new Color[mCamera.xResolution];
            for (int x = 0; x < mCamera.xResolution; ++x) {
                screenRays[y][x] = GetScreenRay(x, y);
                screenPixels[y][x] = Color.black;

                if (multiThreadMode) {

                    foreach (Vector3 offset in superSampleKernals[superSampleKernalIndex]) {

                        if (jobs.Count >= TCBatchSize) {
                            Thread thread = new Thread(new ParameterizedThreadStart(TraceColorThread));
                            thread.Start(jobs);
                            TCThreads.Add(thread);
                            jobs = new List<TraceColorJob>();
                        }

                        Vector3 pos = screenPoints[y][x];
                        pos.x += offset.x * xScale;
                        pos.y += offset.y * yScale;
                        jobs.Add(new TraceColorJob(x, y, offset.z, new RTRay(mCamera.position, pos - mCamera.position)));
                    }

                }
                else {
                    Color pixel = Color.black;

                    foreach (Vector3 offset in superSampleKernals[superSampleKernalIndex]) {
                        Vector3 pos = screenPoints[y][x];
                        pos.x += offset.x * xScale;
                        pos.y += offset.y * yScale;
                        pixel += offset.z * TraceColor(new RTRay(mCamera.position, pos - mCamera.position), 1);
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
    }

    private void TraceColorThread(object parameter) {
        List<TraceColorJob> jobList = (List<TraceColorJob>)parameter;
        foreach (TraceColorJob job in jobList) {
            screenPixels[job.y][job.x] += job.weight * TraceColor(job.ray, 1);
        }
    }

    private Color TraceColor(RTRay ray, int depth) {

        Color result = Color.black;

        if (depth > MAX_RAY_DEPTH || ray == null) {
            return result;
        }

        RTHitInfo hitInfo = GetClosetHitInfo(ray);

        if (hitInfo != null) {

            hitInfo.hitRay = ray;

            result = Mathf.Clamp01(1 - hitInfo.hitable.reflectionRate - hitInfo.hitable.refractionRate) * TraceLightColor(hitInfo);

            if (hitInfo.hitable.reflectionRate > 0 && hitInfo.hitable.refractionRate > 0) {

                Vector3 normalProjVec = hitInfo.hitPointNormal * Vector3.Dot(ray.direction, hitInfo.hitPointNormal);

                result += hitInfo.hitable.reflectionRate * TraceColor(new RTRay(hitInfo.hitPoint + hitInfo.hitPointNormal * HIT_POINT_OFFSET, ray.direction - 2.0f * normalProjVec), depth + 1);

                result += hitInfo.hitable.refractionRate * TraceColor(new RTRay(hitInfo.hitPoint - hitInfo.hitPointNormal * HIT_POINT_OFFSET, normalProjVec + (ray.direction - normalProjVec) * REFRACTION_FACTOR), depth + 1);

            }
            else if (hitInfo.hitable.reflectionRate > 0) {
                Vector3 normalProjVec = hitInfo.hitPointNormal * Vector3.Dot(ray.direction, hitInfo.hitPointNormal);

                result += hitInfo.hitable.reflectionRate * TraceColor(new RTRay(hitInfo.hitPoint + hitInfo.hitPointNormal * HIT_POINT_OFFSET, ray.direction - 2.0f * normalProjVec), depth + 1);
            }
            else if (hitInfo.hitable.refractionRate > 0) {

                Vector3 normalProjVec = hitInfo.hitPointNormal * Vector3.Dot(ray.direction, hitInfo.hitPointNormal);

                result += hitInfo.hitable.refractionRate * TraceColor(new RTRay(hitInfo.hitPoint - hitInfo.hitPointNormal * HIT_POINT_OFFSET, normalProjVec + (ray.direction - normalProjVec) * REFRACTION_FACTOR), depth + 1);
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

            float intensityFactor = light.intensity;

            RTRay lightRay = new RTRay(H, -light.direction);

            float shadowFactor = AccumulateShadowFactor(lightRay, hitInfo.hitable, DIRECTIONAL_LIGHT_DISTANCE * DIRECTIONAL_LIGHT_DISTANCE);

            result += intensityFactor * shadowFactor * PhongShadingColor(hitInfo.hitable.Kd, hitInfo.hitable.Ks, hitInfo.hitable.spec, N, light.direction, E, NDotE) * light.color;
        }

        foreach (RTPointLight light in pointLights) {

            Vector3 L = H - light.position;

            float distance = L.magnitude;

            if (distance > light.range) {
                continue;
            }

            float intensityFactor = light.intensity * (1.0f - distance / light.range);

            RTRay lightRay = new RTRay(H, -L);

            float shadowFactor = AccumulateShadowFactor(lightRay, hitInfo.hitable, distance * distance);

            result += intensityFactor * shadowFactor * PhongShadingColor(hitInfo.hitable.Kd, hitInfo.hitable.Ks, hitInfo.hitable.spec, N, -lightRay.direction, E, NDotE) * light.color;
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
        RTHitInfo hitInfo = null;
        float hitPointDistanceSqr = float.MaxValue;

        foreach (RTHitable hitable in hitables) {

            if (ignoreSet != null && ignoreSet.Contains(hitable)) {
                continue;
            }

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

                if (Vector3.Dot(localLH, localLH) < LHDistSqr) {
                    shadowFactor *= Mathf.Clamp01(localHitInfo.hitable.refractionRate + SHADOW_FACTOR);
                }

            }
        }

        return shadowFactor;

    } 
    #endregion

    private void OnGUI() {
        if(renderTexture != null) {
            GUI.DrawTexture(new Rect(0, 0, mCamera.xResolution, mCamera.yResolution), renderTexture, ScaleMode.ScaleToFit, false);
        }
    }

}
