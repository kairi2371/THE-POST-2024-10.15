using UnityEngine;
using System.Collections.Generic;

public class PhotoManager : MonoBehaviour
{
    [Header("写真設定")]
    public Camera targetCamera;           // 写真を撮影するカメラ
    public GameObject photoPrefab;        // 写真のプレハブ
    public Transform[] photoSpawnPoints;  // 写真のスポーンポイント

    private bool photosGenerated = false; // 写真生成済みか管理
    private List<GameObject> spawnedPhotos = new List<GameObject>(); // 生成された写真のリスト

    void Start()
    {
        // 生成済みなら何もしない
        if (photosGenerated)
        {
            Debug.LogWarning("写真はすでに生成済みです。");
            return;
        }

        // 必要なコンポーネントが設定されているか確認
        if (photoPrefab == null || targetCamera == null || photoSpawnPoints == null)
        {
            Debug.LogError("写真生成の設定が正しくありません！");
            return;
        }

        GeneratePhotosAtStart();
        photosGenerated = true; // 一度生成したらフラグを設定
    }

    /// <summary>
    /// ゲーム開始時に写真を生成
    /// </summary>
    void GeneratePhotosAtStart()
    {
        foreach (var spawnPoint in photoSpawnPoints)
        {
            if (spawnPoint != null)
            {
                Texture2D photoTexture = CaptureCameraView();
                CreatePhoto(photoTexture, spawnPoint.position, spawnPoint.rotation);
            }
        }
        Debug.Log("写真を生成して配置しました！");
    }

    /// <summary>
    /// カメラのビューをキャプチャしてテクスチャを生成
    /// </summary>
    Texture2D CaptureCameraView()
    {
        RenderTexture renderTexture = new RenderTexture(2100, 1240, 24);
        targetCamera.targetTexture = renderTexture;
        targetCamera.Render();

        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(2100, 1240, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, 2100, 1240), 0, 0);
        texture.Apply();

        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);

        return texture;
    }

    /// <summary>
    /// 写真オブジェクトを生成
    /// </summary>
    void CreatePhoto(Texture2D texture, Vector3 position, Quaternion rotation)
    {
        GameObject photo = Instantiate(photoPrefab, position, rotation); // 向きを指定
        PhotoItem photoItem = photo.GetComponent<PhotoItem>();
        if (photoItem != null)
        {
            photoItem.SetPhotoTexture(texture);
        }

        spawnedPhotos.Add(photo);
    }

    /// <summary>
    /// 写真を全削除
    /// </summary>
    public void ClearPhotos()
    {
        foreach (var photo in spawnedPhotos)
        {
            if (photo != null)
            {
                Destroy(photo);
            }
        }
        spawnedPhotos.Clear();
        photosGenerated = false; // 再生成を可能にする
    }
}
