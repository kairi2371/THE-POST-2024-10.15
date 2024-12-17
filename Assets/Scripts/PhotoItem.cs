using UnityEngine;
using UnityEngine.SceneManagement; // シーン管理に必要
using System.Collections;

public class PhotoItem : MonoBehaviour
{
    [Header("写真設定")]
    public GameObject photoPrefab; // 写真のプレハブ
    public Texture2D[] photoTextures; // 写真に適用するテクスチャのリスト
    public Transform[] photoPositions; // 写真を配置する位置リスト
    public Transform player; // プレイヤーの参照
    public Transform photoHolder; // プレイヤーが写真を持つ位置
    public Camera playerCamera; // プレイヤーのカメラ
    public float pickupRange = 3f; // 写真を拾える範囲
    public string nextSceneName = "NextScene"; // 次のシーン名

    public Texture2D photoTexture;
    private Renderer photoRenderer;
    private GameObject heldPhoto; // プレイヤーが持っている写真
    private Texture2D currentTexture; // 現在の写真のテクスチャ

    void Start()
    {
        ApplyPhotoTexture();
    }

    void Update()
    {
        HandlePickup();
        HandleUse();
    }

    public void SetPhotoTexture(Texture2D texture)
    {
        photoTexture = texture;
        ApplyPhotoTexture();
    }

    void HandlePickup()
    {
        if (Input.GetKeyDown(KeyCode.E) && heldPhoto == null)
        {
            TryPickupPhoto();

        }
    }

    private void ApplyPhotoTexture()
    {
        photoRenderer = GetComponent<Renderer>();
        if (photoRenderer != null && photoTexture != null)
        {
            Material material = photoRenderer.material;
            material.mainTexture = photoTexture;
        }
    }

    void TryPickupPhoto()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            if (hit.collider.CompareTag("Photo")) // レイヤーではなくタグで判定
            {
                heldPhoto = hit.collider.gameObject;
                heldPhoto.transform.SetParent(photoHolder);

                //// 左下に写真を持つためのオフセット
                //Vector3 offset = new Vector3(-0.35f, -0.3f, 0.6f); // カメラから見たオフセット位置
                //heldPhoto.transform.localPosition = offset;

                //// 見やすい角度で回転させる
                //Quaternion rotation = Quaternion.Euler(-90, -30, 0); // 左下に向けた角度
                //heldPhoto.transform.localRotation = rotation;

                Renderer renderer = heldPhoto.GetComponent<Renderer>();
                if (renderer != null)
                {
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    currentTexture = block.GetTexture("_MainTex") as Texture2D;
                }

                if (heldPhoto.GetComponent<Rigidbody>() != null)
                {
                    Destroy(heldPhoto.GetComponent<Rigidbody>());
                }

                Debug.Log("写真を拾いました！");
                StartCoroutine(LookAtPhotoAndSwitchScene());
            }
        }
    }

    void HandleUse()
    {
        if (Input.GetMouseButtonDown(1) && heldPhoto != null)
        {
            StartCoroutine(LookAtPhotoAndSwitchScene());
        }
    }

    IEnumerator LookAtPhotoAndSwitchScene()
    {
        Debug.Log("写真を中央に移動して覗き込みます。");

        Vector3 startPosition = heldPhoto.transform.localPosition;
        Quaternion startRotation = heldPhoto.transform.localRotation;
        Vector3 startScale = heldPhoto.transform.localScale;

        Vector3 centerPosition = new Vector3(0, 0, 0.35f);
        Quaternion centerRotation = Quaternion.Euler(-90, 0, 0);
        Vector3 centerScale = startScale * 1.75f; // スケールを1.4倍に拡大

        float duration = 0.5f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            heldPhoto.transform.localPosition = Vector3.Lerp(startPosition, centerPosition, t);
            heldPhoto.transform.localRotation = Quaternion.Lerp(startRotation, centerRotation, t);
            heldPhoto.transform.localScale = Vector3.Lerp(startScale, centerScale, t);
            
               
            
            yield return null;
        }
        Debug.Log("写真を使用しました。シーンを移動します。");
        SceneManager.LoadScene(nextSceneName);

    }
}
