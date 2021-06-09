using UnityEngine;

// AABB - Axis-Aligned Bounding Box
public struct AABB
{
    public Vector3 MinPoint { get; private set; }

    public Vector3 MaxPoint { get; private set; }

    public AABB(params Vector3[] points)
    {
        Vector3 minPoint = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 maxPoint = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        foreach (Vector3 point in points)
        {
            for (int i = 0; i < 3; i++)
            {
                if (point[i] > maxPoint[i])
                {
                    maxPoint[i] = point[i];
                }

                if (point[i] < minPoint[i])
                {
                    minPoint[i] = point[i];
                }
            }
        }

        MinPoint = minPoint;
        MaxPoint = maxPoint;
    }

    public Vector3 GetCenter()
    {
        return (MinPoint + MaxPoint) * 0.5f;
    }

    public Vector3 GetSize()
    {
        return MaxPoint - MinPoint;
    }

    public float GetVolume(bool allowZero = false)
    {
        Vector3 size = GetSize();
        if (!allowZero)
        {
            for (int i = 0; i < 3; i++)
            {
                if (size[i] == 0)
                {
                    size[i] = float.Epsilon;
                }
            }
        }

        return size.x * size.y * size.z;
    }

    public bool Contains(in AABB box)
    {
        return Contains(box.MinPoint) && Contains(box.MaxPoint);
    }

    public bool Contains(in Vector3 point)
    {
        return point.x >= MinPoint.x && point.x <= MaxPoint.x
            && point.y >= MinPoint.y && point.y <= MaxPoint.y
            && point.z >= MinPoint.z && point.z <= MaxPoint.z;
    }

    public bool IsEmpty()
    {
        return MinPoint.x > MaxPoint.x
            || MinPoint.y > MaxPoint.y
            || MinPoint.z > MaxPoint.z;
    }

    public static AABB Merge(in AABB box, in Vector3 point)
    {
        return box.IsEmpty() ? new AABB(point) : new AABB(box.MinPoint, box.MaxPoint, point);
    }

    public static AABB Merge(in AABB firstBox, in AABB secondBox)
    {
        if (firstBox.IsEmpty())
        {
            return secondBox;
        }
        else if (secondBox.IsEmpty())
        {
            return firstBox;
        }

        return new AABB(
            firstBox.MinPoint,
            firstBox.MaxPoint,
            secondBox.MinPoint,
            secondBox.MaxPoint
        );
    }

    public static AABB CreateEmpty()
    {
        return new AABB
        {
            MinPoint = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue),
            MaxPoint = new Vector3(float.MinValue, float.MinValue, float.MinValue)
        };
    }

#if UNITY_EDITOR
    public void DebugDraw(Color color)
    {
        Color temp = Gizmos.color;
        Gizmos.color = color;
        Vector3 size = GetSize();
        int zerosCount = 0;
        for (int i = 0; i < 3; i++)
        {
            if (size[i] == 0)
            {
                zerosCount++;
                size[i] = float.Epsilon;
            }
        }

        switch (zerosCount)
        {
            case 0:
            case 1:
                Gizmos.DrawWireCube(GetCenter(), size);
                break;
            case 2:
                Gizmos.DrawLine(MinPoint, MaxPoint);
                break;
            case 3:
                Gizmos.DrawSphere(MinPoint, 0.4f);
                break;
        }

        Gizmos.color = temp;
    }
#endif
}
