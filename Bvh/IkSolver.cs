using System;
using System.Numerics;

namespace BvhViewer.Bvh;

public static class IkSolver
{
    /// <summary>
    /// 解析法 (Analytical IK) - 僅支援 2 根骨頭 (例如：大臂 + 小臂)
    /// </summary>
    public static (float Angle1, float Angle2) SolveAnalytical2Bone(Vector3 target, float length1, float length2)
    {
        float targetDist = target.Length();
        targetDist = Math.Min(targetDist, length1 + length2 - 0.001f);

        float cosElbow = (targetDist * targetDist - length1 * length1 - length2 * length2) / (2 * length1 * length2);
        float elbowAngle = MathF.Acos(Math.Clamp(cosElbow, -1f, 1f));

        float baseAngle = MathF.Atan2(target.Y, target.X);
        float cosShoulder = (length1 * length1 + targetDist * targetDist - length2 * length2) / (2 * length1 * targetDist);
        float shoulderAngle = baseAngle - MathF.Acos(Math.Clamp(cosShoulder, -1f, 1f));

        return (shoulderAngle, elbowAngle);
    }

    /// <summary>
    /// 循環座標下降法 (CCD IK) - 支援多根骨頭
    /// 由於直接修改 3D 旋轉較為複雜，此處示範 3D 空間中朝向目標的基礎迭代
    /// </summary>
    public static void SolveCCD(Vector3 target, Vector3[] positions, int maxIterations = 10)
    {
        if (positions.Length < 2) return;

        for (int iter = 0; iter < maxIterations; iter++)
        {
            for (int i = positions.Length - 2; i >= 0; i--)
            {
                Vector3 endEffector = positions[^1];
                Vector3 toEnd = Vector3.Normalize(endEffector - positions[i]);
                Vector3 toTarget = Vector3.Normalize(target - positions[i]);

                // 計算旋轉軸與角度
                Vector3 axis = Vector3.Cross(toEnd, toTarget);
                if (axis.LengthSquared() < 0.0001f) continue;
                axis = Vector3.Normalize(axis);
                
                float dot = Math.Clamp(Vector3.Dot(toEnd, toTarget), -1f, 1f);
                float angle = MathF.Acos(dot);

                Quaternion rotation = Quaternion.CreateFromAxisAngle(axis, angle);

                // 更新此關節之後的所有子關節位置 (假裝旋轉它們)
                for (int j = i + 1; j < positions.Length; j++)
                {
                    Vector3 relative = positions[j] - positions[i];
                    positions[j] = positions[i] + Vector3.Transform(relative, rotation);
                }
            }

            if (Vector3.Distance(positions[^1], target) < 0.01f) break;
        }
    }

    /// <summary>
    /// 前後延展法 (FABRIK) - 支援多根骨頭，計算極快且平滑
    /// </summary>
    public static void SolveFABRIK(Vector3 target, Vector3[] positions, float[] boneLengths, int maxIterations = 10)
    {
        if (positions.Length < 2) return;
        
        int numBones = boneLengths.Length;
        Vector3 rootPos = positions[0];

        // 檢查目標是否超出最大長度 (Unreachable)
        float totalLength = 0;
        foreach (var len in boneLengths) totalLength += len;

        if (Vector3.Distance(rootPos, target) >= totalLength)
        {
            // 目標太遠，直接全部拉成一條直線
            for (int i = 0; i < numBones; i++)
            {
                Vector3 dir = Vector3.Normalize(target - positions[i]);
                positions[i + 1] = positions[i] + dir * boneLengths[i];
            }
            return;
        }

        for (int iter = 0; iter < maxIterations; iter++)
        {
            // 1. Backward Pass (由外往內)
            positions[^1] = target;
            for (int i = numBones - 1; i >= 0; i--)
            {
                Vector3 dir = Vector3.Normalize(positions[i] - positions[i + 1]);
                positions[i] = positions[i + 1] + dir * boneLengths[i];
            }

            // 2. Forward Pass (由內往外)
            positions[0] = rootPos;
            for (int i = 0; i < numBones; i++)
            {
                Vector3 dir = Vector3.Normalize(positions[i + 1] - positions[i]);
                positions[i + 1] = positions[i] + dir * boneLengths[i];
            }

            if (Vector3.Distance(positions[^1], target) < 0.01f) break;
        }
    }
}

