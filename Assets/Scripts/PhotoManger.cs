using UnityEngine;
using System.Collections.Generic;

public class PhotoManager : MonoBehaviour
{
    [Header("�ʐ^�ݒ�")]
    public Camera targetCamera;           // �ʐ^���B�e����J����
    public GameObject photoPrefab;        // �ʐ^�̃v���n�u
    public Transform[] photoSpawnPoints;  // �ʐ^�̃X�|�[���|�C���g

    private bool photosGenerated = false; // �ʐ^�����ς݂��Ǘ�
    private List<GameObject> spawnedPhotos = new List<GameObject>(); // �������ꂽ�ʐ^�̃��X�g

    void Start()
    {
        // �����ς݂Ȃ牽�����Ȃ�
        if (photosGenerated)
        {
            Debug.LogWarning("�ʐ^�͂��łɐ����ς݂ł��B");
            return;
        }

        // �K�v�ȃR���|�[�l���g���ݒ肳��Ă��邩�m�F
        if (photoPrefab == null || targetCamera == null || photoSpawnPoints == null)
        {
            Debug.LogError("�ʐ^�����̐ݒ肪����������܂���I");
            return;
        }

        GeneratePhotosAtStart();
        photosGenerated = true; // ��x����������t���O��ݒ�
    }

    /// <summary>
    /// �Q�[���J�n���Ɏʐ^�𐶐�
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
        Debug.Log("�ʐ^�𐶐����Ĕz�u���܂����I");
    }

    /// <summary>
    /// �J�����̃r���[���L���v�`�����ăe�N�X�`���𐶐�
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
    /// �ʐ^�I�u�W�F�N�g�𐶐�
    /// </summary>
    void CreatePhoto(Texture2D texture, Vector3 position, Quaternion rotation)
    {
        GameObject photo = Instantiate(photoPrefab, position, rotation); // �������w��
        PhotoItem photoItem = photo.GetComponent<PhotoItem>();
        if (photoItem != null)
        {
            photoItem.SetPhotoTexture(texture);
        }

        spawnedPhotos.Add(photo);
    }

    /// <summary>
    /// �ʐ^��S�폜
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
        photosGenerated = false; // �Đ������\�ɂ���
    }
}
