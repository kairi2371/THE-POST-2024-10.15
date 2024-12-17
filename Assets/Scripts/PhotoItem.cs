using UnityEngine;
using UnityEngine.SceneManagement; // �V�[���Ǘ��ɕK�v
using System.Collections;

public class PhotoItem : MonoBehaviour
{
    [Header("�ʐ^�ݒ�")]
    public GameObject photoPrefab; // �ʐ^�̃v���n�u
    public Texture2D[] photoTextures; // �ʐ^�ɓK�p����e�N�X�`���̃��X�g
    public Transform[] photoPositions; // �ʐ^��z�u����ʒu���X�g
    public Transform player; // �v���C���[�̎Q��
    public Transform photoHolder; // �v���C���[���ʐ^�����ʒu
    public Camera playerCamera; // �v���C���[�̃J����
    public float pickupRange = 3f; // �ʐ^���E����͈�
    public string nextSceneName = "NextScene"; // ���̃V�[����

    public Texture2D photoTexture;
    private Renderer photoRenderer;
    private GameObject heldPhoto; // �v���C���[�������Ă���ʐ^
    private Texture2D currentTexture; // ���݂̎ʐ^�̃e�N�X�`��

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
            if (hit.collider.CompareTag("Photo")) // ���C���[�ł͂Ȃ��^�O�Ŕ���
            {
                heldPhoto = hit.collider.gameObject;
                heldPhoto.transform.SetParent(photoHolder);

                //// �����Ɏʐ^�������߂̃I�t�Z�b�g
                //Vector3 offset = new Vector3(-0.35f, -0.3f, 0.6f); // �J�������猩���I�t�Z�b�g�ʒu
                //heldPhoto.transform.localPosition = offset;

                //// ���₷���p�x�ŉ�]������
                //Quaternion rotation = Quaternion.Euler(-90, -30, 0); // �����Ɍ������p�x
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

                Debug.Log("�ʐ^���E���܂����I");
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
        Debug.Log("�ʐ^�𒆉��Ɉړ����Ĕ`�����݂܂��B");

        Vector3 startPosition = heldPhoto.transform.localPosition;
        Quaternion startRotation = heldPhoto.transform.localRotation;
        Vector3 startScale = heldPhoto.transform.localScale;

        Vector3 centerPosition = new Vector3(0, 0, 0.35f);
        Quaternion centerRotation = Quaternion.Euler(-90, 0, 0);
        Vector3 centerScale = startScale * 1.75f; // �X�P�[����1.4�{�Ɋg��

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
        Debug.Log("�ʐ^���g�p���܂����B�V�[�����ړ����܂��B");
        SceneManager.LoadScene(nextSceneName);

    }
}
