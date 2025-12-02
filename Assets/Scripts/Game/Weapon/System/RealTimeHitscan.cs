using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class RealTimeHitscan : MonoBehaviour
{
    [Header("弹道参数")]
    public float bulletSpeed = 100f;        // 米/秒
    public float gravity = 9.81f;           // 重力加速度
    public float maxRange = 300f;           // 最大射程
    public float simulationStep = 0.01f;    // 弹道模拟步长（秒）

    [Header("实时检测设置")]
    public float detectionStep = 0.02f;     // 实时检测步长（约50Hz）
    public bool showRealtimeTracer = true;  // 显示实时轨迹

    [Header("可视化设置")]
    public bool isEnableVisual = true;
    public Color predictedPathColor = Color.gray;  // 预测路径颜色
    public Color realtimeTracerColor = Color.yellow; // 实时轨迹颜色
    public Color hitColor = Color.red;             // 命中颜色
    public float lineWidth = 0.05f;
    public GameObject hitEffectPrefab;
    private GameObject lineObj;
    private LineRenderer lr;
    private GameObject realtimeLineObj;
    private LineRenderer realtimeLR;


    [Header("调试信息")]
    [SerializeField] private float realHitTime = 0f;
    [SerializeField] private Vector3 realHitPoint = Vector3.zero;
    [SerializeField] private bool isRealtimeSimulating = false;

    private List<Vector3> predictedPath = new List<Vector3>();
    private List<Vector3> realtimePath = new List<Vector3>();
    private List<GameObject> visualizationObjects = new List<GameObject>();
    private Coroutine realtimeSimulationCoroutine;

    // 弹道路径点信息
    public struct PathPoint
    {
        public Vector3 position;
        public Vector3 velocity;
        public float timeFromStart;

        public PathPoint(Vector3 pos, Vector3 vel, float time)
        {
            position = pos;
            velocity = vel;
            timeFromStart = time;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            FireWeapon();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearVisualization();
        }
    }

    private void FireWeapon()
    {
        if (isRealtimeSimulating)
        {
            // 如果正在模拟，先停止
            StopCoroutine(realtimeSimulationCoroutine);
            isRealtimeSimulating = false;
        }

        if (isEnableVisual)
        {
            ClearVisualization();
        }


        // 1. 先计算完整的预测弹道
        CalculatePredictedPath();

        // 2. 可视化预测弹道（灰色虚线）
        if (isEnableVisual)
        {
            VisualizePredictedPath();
        }

        // 3. 启动实时模拟协程
        realtimeSimulationCoroutine = StartCoroutine(RealtimePathSimulation());
    }

    // 步骤1：计算完整的预测弹道
    void CalculatePredictedPath()
    {
        predictedPath.Clear();
        realtimePath.Clear();

        Vector3 currentPosition = transform.position;
        Vector3 currentVelocity = transform.forward * bulletSpeed;
        float currentTime = 0f;

        // 添加起始点
        predictedPath.Add(currentPosition);

        // 最大模拟时间（子弹能飞的最大时间）
        float maxSimulationTime = maxRange / bulletSpeed * 2f; // 乘以2确保覆盖

        while (currentTime < maxSimulationTime && predictedPath.Count < 1000)
        {
            // 计算下一步
            Vector3 nextPosition = CalculateNextPosition(currentPosition, currentVelocity, simulationStep);
            Vector3 nextVelocity = CalculateNextVelocity(currentVelocity, simulationStep);

            // 添加点
            predictedPath.Add(nextPosition);

            // 更新
            currentPosition = nextPosition;
            currentVelocity = nextVelocity;
            currentTime += simulationStep;

            // 检查是否超出射程
            if (Vector3.Distance(transform.position, currentPosition) >= maxRange)
            {
                break;
            }
        }

        Debug.Log($"预测弹道计算完成，共{predictedPath.Count}个点，总时长{currentTime:F2}秒");
    }

    // 步骤2：可视化预测弹道（作为参考）
    void VisualizePredictedPath()
    {
        if (predictedPath.Count < 2) return;
        if (lineObj == null)
        {
            lineObj = new GameObject("PredictedPath");
            lr = lineObj.AddComponent<LineRenderer>();
        }


        lr.positionCount = predictedPath.Count;
        lr.SetPositions(predictedPath.ToArray());
        lr.startColor = predictedPathColor;
        lr.endColor = predictedPathColor;
        lr.startWidth = lineWidth * 0.5f;
        lr.endWidth = lineWidth * 0.5f;
        lr.material = new Material(Shader.Find("Sprites/Default"));

        // 设置为虚线
        lr.material = CreateDashedLineMaterial();

        //visualizationObjects.Add(lineObj);

        // 添加起点标记
        CreateSphereMarker(predictedPath[0], Color.green, "Start", 0.2f);

        // 添加终点标记
        CreateSphereMarker(predictedPath[predictedPath.Count - 1], Color.blue, "PredictedEnd", 0.2f);
    }

    // 步骤3：实时弹道模拟协程
    IEnumerator RealtimePathSimulation()
    {
        isRealtimeSimulating = true;
        realtimePath.Clear();
        realHitPoint = Vector3.zero;
        realHitTime = 0f;

        // 用于绘制实时轨迹
        if (realtimeLineObj == null)
        {
            realtimeLineObj = new GameObject("RealtimePath");
            realtimeLR = realtimeLineObj.AddComponent<LineRenderer>();
        }

        realtimeLR.positionCount = 0;
        realtimeLR.startColor = realtimeTracerColor;
        realtimeLR.endColor = realtimeTracerColor;
        realtimeLR.startWidth = lineWidth;
        realtimeLR.endWidth = lineWidth;
        realtimeLR.material = new Material(Shader.Find("Sprites/Default"));
        //visualizationObjects.Add(realtimeLineObj);

        // 使用插值在预测路径上移动
        float elapsedTime = 0f;
        int predictedIndex = 0;
        bool hasHit = false;

        // 添加起始点到实时路径
        realtimePath.Add(predictedPath[0]);

        while (elapsedTime < 10f && predictedIndex < predictedPath.Count - 1 && !hasHit) // 安全限制
        {
            // 根据当前时间确定在预测路径上的位置
            float targetTime = elapsedTime;

            // 找到预测路径中对应时间的点
            int nextIndex = Mathf.Min(predictedIndex + 1, predictedPath.Count - 1);
            Vector3 currentPos = predictedPath[predictedIndex];
            Vector3 nextPos = predictedPath[nextIndex];

            // 计算两点间的时间（假设匀速）
            float segmentTime = Vector3.Distance(currentPos, nextPos) / bulletSpeed;
            float segmentProgress = Mathf.Clamp01((elapsedTime - predictedIndex * segmentTime) / segmentTime);

            // 插值得到当前位置
            Vector3 interpolatedPos = Vector3.Lerp(currentPos, nextPos, segmentProgress);

            // 在当前帧进行射线检测
            RaycastHit hit;
            if (predictedIndex > 0) // 从上一个点到当前点进行检测
            {
                Vector3 lastPos = realtimePath[realtimePath.Count - 1];
                Vector3 moveDir = (interpolatedPos - lastPos).normalized;
                float moveDist = Vector3.Distance(interpolatedPos, lastPos);

                Debug.DrawRay(lastPos, moveDir * moveDist, Color.white, 0.1f);

                if (Physics.Raycast(lastPos, moveDir, out hit, moveDist, LayerMask.GetMask("Default")))
                {
                    // 命中！
                    realHitPoint = hit.point;
                    realHitTime = elapsedTime;
                    hasHit = true;

                    // 添加命中点到路径
                    realtimePath.Add(hit.point);

                    // 显示命中效果
                    ShowHitEffect(hit.point, hit.normal);

                    Debug.Log($"实时检测命中！飞行时间：{realHitTime:F2}秒，距离：{Vector3.Distance(transform.position, hit.point):F2}米 检测次数{predictedIndex + 1}");

                    // 添加命中标记
                    CreateSphereMarker(hit.point, hitColor, "RealHit", 0.3f);

                    break;
                }
            }

            // 添加当前点到路径
            realtimePath.Add(interpolatedPos);

            // 更新实时轨迹显示
            realtimeLR.positionCount = realtimePath.Count;
            realtimeLR.SetPositions(realtimePath.ToArray());

            // 更新预测索引
            if (segmentProgress >= 1.0f && predictedIndex < predictedPath.Count - 1)
            {
                predictedIndex++;
            }

            // 等待下一次检测
            yield return new WaitForSeconds(detectionStep);
            elapsedTime += detectionStep;

            // 调试信息
            if (Time.frameCount % 10 == 0)
            {
                Debug.DrawLine(transform.position, interpolatedPos, Color.green, 0.5f);
            }
        }

        if (!hasHit)
        {
            Debug.Log($"弹道模拟完成，未命中目标。总时长：{elapsedTime:F2}秒");
            realHitPoint = realtimePath[realtimePath.Count - 1];
            realHitTime = elapsedTime;

            // 添加终点标记
            CreateSphereMarker(realHitPoint, Color.white, "RealEnd", 0.2f);
        }

        // 最终更新一次轨迹
        realtimeLR.positionCount = realtimePath.Count;
        realtimeLR.SetPositions(realtimePath.ToArray());

        // 模拟结束后改变颜色
        StartCoroutine(FadeOutTrajectory(realtimeLR, hasHit ? hitColor : Color.white));

        isRealtimeSimulating = false;
    }

    // 基于时间的弹道计算（使用积分方法）
    List<PathPoint> CalculatePathWithTime(Vector3 startPos, Vector3 startVelocity, float totalTime)
    {
        List<PathPoint> pathPoints = new List<PathPoint>();

        Vector3 currentPos = startPos;
        Vector3 currentVel = startVelocity;
        float currentTime = 0f;

        pathPoints.Add(new PathPoint(currentPos, currentVel, currentTime));

        while (currentTime < totalTime)
        {
            // 计算时间步长（自适应，确保精度）
            float dt = Mathf.Min(simulationStep, totalTime - currentTime);

            // 使用物理积分
            Vector3 acceleration = Vector3.down * gravity + CalculateDrag(currentVel);

            // 半步长积分（更精确）
            Vector3 halfVelocity = currentVel + acceleration * dt * 0.5f;
            Vector3 nextPos = currentPos + halfVelocity * dt;
            Vector3 nextVel = currentVel + acceleration * dt;

            // 添加点
            currentTime += dt;
            pathPoints.Add(new PathPoint(nextPos, nextVel, currentTime));

            // 更新
            currentPos = nextPos;
            currentVel = nextVel;
        }

        return pathPoints;
    }

    Vector3 CalculateDrag(Vector3 velocity)
    {
        // 简单的空气阻力模型（与速度平方成正比）
        float dragCoefficient = 0.001f;
        float speed = velocity.magnitude;
        return -velocity.normalized * dragCoefficient * speed * speed;
    }

    Vector3 CalculateNextPosition(Vector3 position, Vector3 velocity, float deltaTime)
    {
        // 使用运动方程：s = ut + 0.5at²
        return position + velocity * deltaTime +
               0.5f * Vector3.down * gravity * deltaTime * deltaTime;
    }

    Vector3 CalculateNextVelocity(Vector3 velocity, float deltaTime)
    {
        return velocity + Vector3.down * gravity * deltaTime;
    }

    void ShowHitEffect(Vector3 position, Vector3 normal)
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.LookRotation(normal));
            visualizationObjects.Add(effect);
            Destroy(effect, 2f);
        }
        else
        {
            // 创建简单的命中标记
            GameObject hitMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hitMarker.transform.position = position;
            hitMarker.transform.localScale = Vector3.one * 0.5f;

            Renderer renderer = hitMarker.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = hitColor;

            DestroyImmediate(hitMarker.GetComponent<Collider>());
            visualizationObjects.Add(hitMarker);
        }
    }

    void CreateSphereMarker(Vector3 position, Color color, string name, float size)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = name;
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * size;

        Renderer renderer = marker.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = color;

        DestroyImmediate(marker.GetComponent<Collider>());
        visualizationObjects.Add(marker);
    }

    Material CreateDashedLineMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));

        // 创建简单的虚线效果（通过纹理）
        Texture2D dashTex = new Texture2D(8, 2);
        Color[] pixels = new Color[16];

        // 创建黑白相间的像素
        for (int i = 0; i < 16; i++)
        {
            pixels[i] = (i % 2 == 0) ? Color.white : new Color(0, 0, 0, 0);
        }

        dashTex.SetPixels(pixels);
        dashTex.Apply();

        mat.mainTexture = dashTex;
        mat.SetTextureScale("_MainTex", new Vector2(10, 1)); // 拉伸纹理形成虚线

        return mat;
    }

    IEnumerator FadeOutTrajectory(LineRenderer lr, Color finalColor)
    {
        yield return new WaitForSeconds(0.5f);

        float fadeDuration = 2f;
        float elapsed = 0f;
        Color startColor = lr.startColor;

        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            Color currentColor = Color.Lerp(startColor, finalColor, t);

            lr.startColor = currentColor;
            lr.endColor = currentColor;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 最后设为最终颜色
        lr.startColor = finalColor;
        lr.endColor = finalColor;
    }

    void ClearVisualization()
    {
        foreach (GameObject obj in visualizationObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        visualizationObjects.Clear();

        if (realtimeSimulationCoroutine != null)
        {
            StopCoroutine(realtimeSimulationCoroutine);
            isRealtimeSimulating = false;
        }
    }

    // 在Scene视图中显示调试信息
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // 显示实时命中点
        if (realHitPoint != Vector3.zero)
        {
            Gizmos.color = hitColor;
            Gizmos.DrawSphere(realHitPoint, 0.3f);
            Gizmos.DrawWireSphere(realHitPoint, 0.5f);

            // 显示从枪口到命中点的线
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, realHitPoint);
        }

        // 显示子弹的当前位置（如果有实时模拟）
        if (isRealtimeSimulating && realtimePath.Count > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(realtimePath[realtimePath.Count - 1], 0.2f);

            // 显示子弹速度方向
            if (realtimePath.Count > 1)
            {
                Vector3 lastPos = realtimePath[realtimePath.Count - 2];
                Vector3 currentPos = realtimePath[realtimePath.Count - 1];
                Vector3 direction = (currentPos - lastPos).normalized;
                Gizmos.DrawRay(currentPos, direction * 2f);
            }
        }
    }
}