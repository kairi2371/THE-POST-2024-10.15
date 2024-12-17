using UnityEngine;

public class OutlineObjectChecker : MonoBehaviour
{
    public float rayDistance = 5f; // Rayの飛距離
    private Transform currentOutlinePrefab; // 現在視線が向いているOutlineTargetのPrefabのルート
    public LayerMask outlineTargetLayerMask; // 判定するOutlineTargetレイヤーを設定

    void Update()
    {
        // カメラから前方にRayを飛ばしてオブジェクトをチェック
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        // 視線が外れた場合、前のPrefabのレイヤーをOutlineTargetに戻す
        if (currentOutlinePrefab != null && !Physics.Raycast(ray, out hit, rayDistance, outlineTargetLayerMask))
        {
            ResetOutline();
        }

        // RaycastがOutlineTargetレイヤーのオブジェクトに当たった場合
        if (Physics.Raycast(ray, out hit, rayDistance, outlineTargetLayerMask))
        {
            Transform hitRoot = hit.collider.transform.root; // Prefabのルートオブジェクトを取得

            // 新しいPrefabに視線が当たった場合
            if (currentOutlinePrefab != hitRoot)
            {
                // 前のPrefabのレイヤーをOutlineTargetに戻す
                if (currentOutlinePrefab != null)
                {
                    ResetOutline();
                }

                // 新しいPrefabのレイヤーをOutlineLayerに変更
                currentOutlinePrefab = hitRoot;
                SetLayerRecursively(currentOutlinePrefab, LayerMask.NameToLayer("OutlineLayer"));
            }
        }
    }

    // Prefab全体のレイヤーを再帰的に変更するメソッド
    void SetLayerRecursively(Transform obj, int newLayer)
    {
        obj.gameObject.layer = newLayer;
        foreach (Transform child in obj)
        {
            SetLayerRecursively(child, newLayer);
        }
    }

    // Outlineをリセットするメソッド
    public void ResetOutline()
    {
        if (currentOutlinePrefab != null)
        {
            SetLayerRecursively(currentOutlinePrefab, LayerMask.NameToLayer("OutlineTarget"));
            currentOutlinePrefab = null;
        }
    }
}
