using UnityEngine;

public class EnemyLookAtPlayer : MonoBehaviour
{
    public Transform player; // プレイヤーのTransformをインスペクタで設定
    public float rotationSpeed = 5f; // 回転の速さ
    private LineRenderer lineRenderer; // LineRendererの参照

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>(); // LineRendererコンポーネントを取得
        lineRenderer.positionCount = 2; // ラインの点の数を2に設定
    }

    void Update()
    {
        if (player != null) // プレイヤーが設定されている場合
        {
            // プレイヤーの位置を見て、回転する
            Vector3 direction = player.position - transform.position; // プレイヤーへのベクトル
            direction.y = 0; // Y軸の回転を無視して水平方向のみに制限

            // 回転のQuaternionを計算
            Quaternion rotation = Quaternion.LookRotation(direction);

            // 現在の回転から目標の回転へスムーズに補間
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);

            // ラインの位置を更新
            UpdateLineRenderer();
        }
    }

    private void UpdateLineRenderer()
    {
        // 敵の向いている方向を取得
        Vector3 direction = transform.forward; // 敵の向き
        Vector3 startPoint = transform.position + Vector3.up * 1.5f; // ラインの始点（敵の位置）
        Vector3 endPoint = startPoint + direction * 5f; // ラインの終点（向いている方向に5の長さ）

        // ラインの位置を設定
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
    }
}
