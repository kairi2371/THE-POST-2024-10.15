using UnityEngine;

public class PositionSwap : MonoBehaviour
{
    public GameObject player; // プレイヤーのオブジェクト
    public Camera playerCamera; // プレイヤーのカメラ
    public float rayDistance = 20f; // Rayの長さ
    private GameObject targetEnemy; // 入れ替える対象の敵
    public float cameraMoveSpeed = 15f; // カメラの移動速度
    private bool isSwapping = false; // 入れ替え処理中のフラグ
    private Vector3 playerOriginalPosition; // 入れ替え前のプレイヤーの位置
    public float maxFOV = 100f; // 最大FOV
    public float fovChangeSpeed = 10f; // FOVの変化速度
    private MonoBehaviour playerMovement; // プレイヤー移動スクリプトの参照

    private void Start()
    {
        // プレイヤーの移動スクリプトを取得（適切なスクリプトを指定してください）
        playerMovement = player.GetComponent<FirstPersonMovement>(); // 例: FirstPersonMovement
    }

    private void Update()
    {
        // 入れ替えのキーが押されたときに入れ替えを開始
        if (Input.GetMouseButton(0) && !isSwapping)//マウスの左ボタン
        {
            // Raycastをプレイヤーのカメラから発射
            RaycastHit hit;
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, rayDistance))
            {
                if (hit.collider.CompareTag("Enemy")) // 敵のタグを比較
                {
                    targetEnemy = hit.collider.gameObject; // 入れ替える敵を設定
                    playerOriginalPosition = player.transform.position; // 入れ替え前のプレイヤーの位置を保存
                    StartCoroutine(SwapPositions());
                }
            }
        }
    }

    private System.Collections.IEnumerator SwapPositions()
    {
        isSwapping = true; // 入れ替え処理中に設定

        // プレイヤーの移動スクリプトを無効化
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // FOVを90に徐々に変更
        float originalFOV = playerCamera.fieldOfView; // 元のFOV
        float elapsedTime = 0f;
        while (elapsedTime < 1f) // 1秒でFOVを変更
        {
            playerCamera.fieldOfView = Mathf.Lerp(originalFOV, maxFOV, elapsedTime); // FOVを線形補間
            elapsedTime += Time.deltaTime * fovChangeSpeed;
            yield return null; // フレーム毎に待機
        }
        playerCamera.fieldOfView = maxFOV; // 最終的なFOVを設定

        // カメラを敵の位置にスムーズに移動
        Vector3 targetPosition = targetEnemy.transform.position + Vector3.up * 1.8f; // Y座標を1.488上げる
        elapsedTime = 0f; // 経過時間リセット

        while (Vector3.Distance(playerCamera.transform.position, targetPosition) > 0.01f) // 目的地に到達するまで移動
        {
            playerCamera.transform.position = Vector3.MoveTowards(playerCamera.transform.position, targetPosition, cameraMoveSpeed * Time.deltaTime); // 目的地に向けて移動
            yield return null; // フレーム毎に待機
        }

        // プレイヤーと敵の位置を入れ替え
        Vector3 tempPosition = player.transform.position;
        Quaternion tempRotation = player.transform.rotation; // プレイヤーの回転を保存

        player.transform.position = targetEnemy.transform.position;
        player.transform.rotation = targetEnemy.transform.rotation; // 敵の回転をプレイヤーに設定
        targetEnemy.transform.position = tempPosition;
        targetEnemy.transform.rotation = tempRotation; // プレイヤーの回転を敵に設定

        // カメラをプレイヤーの元の位置に戻し、向きをプレイヤーが切り替え前にいた場所に設定
        playerCamera.transform.position = player.transform.position + Vector3.up * 1.488f; // プレイヤーの頭の位置に設定
        Vector3 directionToOriginal = playerOriginalPosition - player.transform.position; // 切り替え前の位置への方向
        playerCamera.transform.rotation = Quaternion.LookRotation(directionToOriginal); // カメラを切り替え前の位置に向ける

        // FOVを元の値に戻す
        elapsedTime = 0f;
        while (elapsedTime < 1f) // 1秒でFOVを戻す
        {
            playerCamera.fieldOfView = Mathf.Lerp(maxFOV, originalFOV, elapsedTime); // FOVを線形補間
            elapsedTime += Time.deltaTime * fovChangeSpeed;
            yield return null; // フレーム毎に待機
        }
        playerCamera.fieldOfView = originalFOV; // 最終的なFOVを設定

        isSwapping = false; // 入れ替え処理終了

        // プレイヤーの移動スクリプトを再度有効化
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
    }
}
