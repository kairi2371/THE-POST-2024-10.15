using UnityEngine;
using System.Collections.Generic;

public class SnapshotManager : MonoBehaviour
{
    private List<TransformSnapshot> snapshots = new List<TransformSnapshot>();

    public void TakeSnapshot()
    {
        snapshots.Clear();
        foreach (Transform obj in FindObjectsOfType<Transform>())
        {
            snapshots.Add(new TransformSnapshot(obj));
        }
        Debug.Log("ステージのスナップショットを保存しました！");
    }

    public void RestoreState()
    {
        foreach (var snapshot in snapshots)
        {
            snapshot.Restore();
        }
        Debug.Log("ステージの状態を復元しました！");
    }
}

public class TransformSnapshot
{
    private Transform target;
    private Vector3 position;
    private Quaternion rotation;

    public TransformSnapshot(Transform target)
    {
        this.target = target;
        position = target.position;
        rotation = target.rotation;
    }

    public void Restore()
    {
        if (target != null)
        {
            target.position = position;
            target.rotation = rotation;
        }
    }
}
