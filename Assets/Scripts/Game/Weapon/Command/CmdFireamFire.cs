using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using QFramework;

/// <summary>
/// 开火指令（Fire Command）
/// 使用 QFramework 的 AbstractCommand，触发一次开火逻辑。
/// - 负责根据 SOFirearmConfig 生成子弹轨迹
/// - 先使用固定步长仿真完整预测轨迹（不包含碰撞）
/// - 再使用 UniTask 进行实时弹道检测（包含 Raycast 碰撞）
/// 
/// 注意：Command 内不能直接使用 await，需要使用 UniTaskVoid/Forget 派生异步任务。
/// </summary>
public class CmdFireamFire : AbstractCommand
{
    // -----------------------------
    // 枪械参数
    // -----------------------------
    private SOFirearmConfig firearmConfig; // 武器配置 ScriptableObject
    public Vector3 startPos;              // 子弹起始位置
    public Vector3 forward;               // 射击方向（单位向量）

    // -----------------------------
    // 弹道计算参数
    // -----------------------------
    public float gravity = 9.81f;          // 重力加速度
    public float simulationStep = 0.5f;   // 预测轨迹仿真步长（越小越精确）

    // -----------------------------
    // 实时碰撞检测参数
    // -----------------------------
    public float detectionStep = 0.05f;    // 实时检测间隔（20Hz）
    public bool showRealtimeTracer = true; // 是否绘制弹道调试可视化（可扩展）


    /// <summary>
    /// 构造函数（由业务层通过 new CmdFireamFire().Execute 调用）
    /// </summary>
    public CmdFireamFire(SOFirearmConfig firearmConfig, Vector3 startPos, Vector3 forward)
    {
        this.firearmConfig = firearmConfig;
        this.startPos = startPos;
        this.forward = forward;
    }


    /// <summary>
    /// QFramework 执行入口
    /// </summary>
    protected override void OnExecute()
    {
        Fire();
    }


    /// <summary>
    /// 执行一次开火
    /// 1. 如有正在运行的实时模拟，先标记停止
    /// 2. 计算完整预测轨迹（无碰撞）
    /// 3. 启动 UniTask 实时轨迹模拟（带碰撞）
    /// </summary>
    private void Fire()
    {
        // 为每发子弹生成独立预测轨迹副本
        List<Vector3> predictedPath = CalculatePredictedPath(startPos, forward);

        // 启动独立实时模拟任务
        RunRealtimeSimulation(predictedPath).Forget();
    }


    // --------------------------------------------------------------------
    // ✔ UniTask 版本实时弹道模拟（替代 Coroutine）
    // --------------------------------------------------------------------
    /// <summary>
    /// 实时弹道模拟：
    /// - 使用预测轨迹作为关键帧
    /// - 使用 UniTask.Delay 替代 WaitForSeconds
    /// - 在每个插值位置进行 Raycast 检测
    /// - 检测到碰撞时立即停止
    /// </summary>
    /// <summary>
    /// 每发子弹独立实时模拟任务
    /// </summary>
    private async UniTaskVoid RunRealtimeSimulation(List<Vector3> predictedPath)
    {
        List<Vector3> realtimePath = new List<Vector3>();
        bool hasHit = false;

        float elapsedTime = 0f;
        int predictedIndex = 0;

        realtimePath.Add(predictedPath[0]);

        while (elapsedTime < 10f && predictedIndex < predictedPath.Count - 1 && !hasHit)
        {
            // 当前段长度与时间
            float segmentTime = Vector3.Distance(predictedPath[predictedIndex], predictedPath[predictedIndex + 1]) /
                                firearmConfig.bulletSpeed;

            float segmentProgress = Mathf.Clamp01(
                (elapsedTime - predictedIndex * segmentTime) / segmentTime);

            Vector3 currPos = predictedPath[predictedIndex];
            Vector3 nextPos = predictedPath[predictedIndex + 1];
            Vector3 interpolatedPos = Vector3.Lerp(currPos, nextPos, segmentProgress);

            // Raycast 检测碰撞
            if (realtimePath.Count > 0)
            {
                Vector3 lastPos = realtimePath[^1];
                float moveDist = Vector3.Distance(interpolatedPos, lastPos);
                Vector3 dir = (interpolatedPos - lastPos).normalized;

                if (Physics.Raycast(lastPos, dir, out RaycastHit hit, moveDist))
                {
                    hasHit = true;
                    realtimePath.Add(hit.point);
                    Debug.Log($"【命中】目标 {hit.point} 飞行时间 {elapsedTime:F2}s");
                    break;
                }
            }

            realtimePath.Add(interpolatedPos);

            if (segmentProgress >= 1f)
                predictedIndex++;

            await UniTask.Delay(TimeSpan.FromSeconds(detectionStep));
            elapsedTime += detectionStep;
        }

        if (!hasHit)
        {
            Debug.Log($"弹道结束：未命中，飞行时间 {elapsedTime:F2}s");
        }

        // 可选：显示弹道调试
        if (showRealtimeTracer)
        {
            for (int i = 0; i < realtimePath.Count - 1; i++)
            {
                Debug.DrawLine(realtimePath[i], realtimePath[i + 1], Color.red, 1f);
            }
        }
    }


    // --------------------------------------------------------------------
    // ✔ 子弹预测轨迹（纯物理，不含碰撞检测）
    // --------------------------------------------------------------------
    /// <summary>
    /// 使用简单物理积分生成子弹的完整预测轨迹（用作实时模拟关键帧）
    /// 不包含 Raycast。
    /// </summary>
    private List<Vector3> CalculatePredictedPath(Vector3 startPos, Vector3 forward)
    {
        List<Vector3> path = new List<Vector3>();
        Vector3 pos = startPos;
        Vector3 vel = forward * firearmConfig.bulletSpeed;
        float t = 0f;

        path.Add(pos);

        float maxTime = firearmConfig.maxRange / firearmConfig.bulletSpeed * 2f;

        while (t < maxTime && path.Count < 1000)
        {
            Vector3 nextPos = pos + vel * simulationStep + 0.5f * Vector3.down * gravity * simulationStep * simulationStep;
            Vector3 nextVel = vel + Vector3.down * gravity * simulationStep;

            path.Add(nextPos);

            pos = nextPos;
            vel = nextVel;
            t += simulationStep;

            if (Vector3.Distance(startPos, pos) >= firearmConfig.maxRange)
                break;
        }

        return path;
    }

}
