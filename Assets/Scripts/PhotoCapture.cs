using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PhotoCapture : MonoBehaviour
{
    public Camera captureCamera; // 撮影用のカメラ
    public RenderTexture renderTexture; // 写真を保存するRenderTexture
    public Material photoMaterial; // 写真に使用するマテリアル
    public string sceneToCapture; // 撮影したいシーンの名前

    public void CapturePhoto()
    {
        // シーンを非アクティブでロード
        StartCoroutine(LoadSceneAndCapture(sceneToCapture));
    }

    private IEnumerator LoadSceneAndCapture(string sceneName)
    {
        // シーンを非アクティブでロード
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        loadOp.allowSceneActivation = false; // 非アクティブ状態にする
        yield return loadOp;

        // カメラをRenderTextureに設定
        if (captureCamera != null && renderTexture != null)
        {
            captureCamera.targetTexture = renderTexture;
            captureCamera.Render(); // カメラを手動で描画
            captureCamera.targetTexture = null;
        }

        // 写真をマテリアルに適用
        if (photoMaterial != null)
        {
            photoMaterial.mainTexture = renderTexture;
        }

        // シーンをアンロード
        SceneManager.UnloadSceneAsync(sceneName);
    }
}
