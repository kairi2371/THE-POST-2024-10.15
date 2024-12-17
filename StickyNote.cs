using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Security.Cryptography;

public class StickyNote : MonoBehaviour
{
    public enum StickyNoteType
    {
        ScaleUp,
        MoveBackward,
        Jump,
        Glow,
        Clone
    }

    public StickyNoteType stickyNoteType;
    public GameObject stickyNotePrefab;
    private GameObject activeStickyNote = null;
    public float offsetDistance = 0.01f;
    public float scaleSpeed = 2f;
    public float pickupRange = 3f;
    public float moveSpeed = 1f;
    public float jumpForce = 150f;
    public int maxClones = 3; // �ő�N���[����
    public Material cloneMaterial; // �N���[���ɓK�p����}�e���A��
    private OutlineObjectChecker outlineChecker;

    private Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, List<(GameObject noteObject, StickyNoteType noteType)>> attachedStickyNotes = new Dictionary<Transform, List<(GameObject, StickyNoteType)>>();
    private Dictionary<Transform, Vector3> moveDirections = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Coroutine> jumpCoroutines = new Dictionary<Transform, Coroutine>();
    private Dictionary<Transform, List<GameObject>> clones = new Dictionary<Transform, List<GameObject>>(); // �N���[���Ǘ��p


    private Vector3 stickyNoteOriginalScale;
    private bool isHeldInHand = false;

    void Start()
    {
        if (cloneMaterial == null)
        {
            Debug.LogError("�N���[���p�̃}�e���A�����ݒ肳��Ă��܂���I");
            return;
        }
        if (stickyNotePrefab != null)
        {
            stickyNoteOriginalScale = new Vector3(stickyNotePrefab.transform.localScale.x, stickyNotePrefab.transform.localScale.y, 0.01f);
        }

        outlineChecker = FindObjectOfType<OutlineObjectChecker>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isHeldInHand)
                TryPickupStickyNote();
            else
                ReleaseStickyNote();
        }

        if (Input.GetMouseButtonDown(0) && isHeldInHand)
        {
            TryAttachToSurface();
        }

        if (Input.GetMouseButtonDown(1))
        {
            TryDetachStickyNote();
        }

        if (isHeldInHand && activeStickyNote != null)
        {
            PositionStickyNoteInHand();
        }
    }

    void FixedUpdate()
    {
        foreach (var target in new List<Transform>(moveDirections.Keys))
        {
            if (moveDirections[target] != Vector3.zero)
            {
                MoveObjectBackward(target);
            }
        }
    }

    void TryPickupStickyNote()
    {
        if (isHeldInHand)
        {
            Debug.Log("���łɕtⳂ���Ɏ����Ă��܂�");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            if (hit.collider != null && hit.collider.gameObject.CompareTag("StickyNote"))
            {
                // �V�����tⳂ𐶐�
                activeStickyNote = Instantiate(stickyNotePrefab, hit.point, Quaternion.identity);

                // �����ݒ�
                activeStickyNote.transform.SetParent(Camera.main.transform);
                activeStickyNote.transform.localPosition = Vector3.zero;
                activeStickyNote.transform.localScale = stickyNoteOriginalScale;

                if (activeStickyNote.GetComponent<Rigidbody>() != null)
                {
                    activeStickyNote.GetComponent<Rigidbody>().isKinematic = true;
                }
                if (activeStickyNote.GetComponent<Collider>() != null)
                {
                    activeStickyNote.GetComponent<Collider>().enabled = false;
                }

                SetLayerRecursively(activeStickyNote, LayerMask.NameToLayer("OutlineTarget"));

                isHeldInHand = true;
                outlineChecker?.ResetOutline();

                Debug.Log("�V�����tⳂ𐶐����A�E���܂���: " + activeStickyNote.name);
            }
        }
    }

    void ReleaseStickyNote()
    {
        if (activeStickyNote != null)
        {
            activeStickyNote.transform.SetParent(null);

            if (activeStickyNote.GetComponent<Rigidbody>() == null)
            {
                activeStickyNote.AddComponent<Rigidbody>();
            }
            else
            {
                activeStickyNote.GetComponent<Rigidbody>().isKinematic = false;
            }
            if (activeStickyNote.GetComponent<Collider>() != null)
            {
                activeStickyNote.GetComponent<Collider>().enabled = true;
            }

            SetLayerRecursively(activeStickyNote, LayerMask.NameToLayer("OutlineTarget"));

            activeStickyNote = null;
            isHeldInHand = false;
            Debug.Log("�tⳂ�������܂���");
        }
    }

    void PositionStickyNoteInHand()
    {
        Vector3 offset = new Vector3(-0.4f, -0.2f, 0.5f);
        Vector3 targetPosition = Camera.main.transform.position + Camera.main.transform.rotation * offset;
        activeStickyNote.transform.position = targetPosition;
        activeStickyNote.transform.localRotation = Quaternion.Euler(15, -20, 0);
        activeStickyNote.transform.localScale = stickyNoteOriginalScale;

        // �ǂƂ̋������m�F���A�ǂƂ̏Փ˂������
        Ray ray = new Ray(Camera.main.transform.position, targetPosition - Camera.main.transform.position);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, offset.magnitude))
        {
            // �ǂƏՓ˂��Ă���ꍇ�͕ǎ�O�ɔz�u����
            activeStickyNote.transform.position = hit.point - ray.direction * 0.05f; // �ǂ��班�������
        }
    }

    void TryAttachToSurface()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // pickupRange�ɃI�u�W�F�N�g�̃X�P�[���̑傫����ǉ�
        if (Physics.Raycast(ray, out hit, pickupRange + transform.localScale.magnitude))
        {
            Transform target = hit.transform.root;
            if (hit.collider.CompareTag("Pickable"))
            {
                AttachToSurface(target, hit.point, hit.normal);
            }
            else
            {
                Debug.Log("���̃I�u�W�F�N�g�ɂ͕tⳂ�\��܂���: " + hit.transform.name);
            }
        }
    }

    void AttachToSurface(Transform target, Vector3 hitPoint, Vector3 hitNormal)
    {
        // �V�����tⳂ𐶐�
        GameObject newStickyNote = Instantiate(stickyNotePrefab, hitPoint, Quaternion.LookRotation(-hitNormal));
        newStickyNote.transform.SetParent(target);
        newStickyNote.transform.localScale = stickyNoteOriginalScale;

        // �A�N�e�B�u�tⳂ����Z�b�g
        activeStickyNote = null;
        isHeldInHand = false;

        // �ݒ��ێ�����
        if (!originalScales.ContainsKey(target))
        {
            originalScales[target] = target.localScale;
        }
        if (!attachedStickyNotes.ContainsKey(target))
        {
            attachedStickyNotes[target] = new List<(GameObject, StickyNoteType)>();
        }

        StickyNoteType noteType = newStickyNote.GetComponent<StickyNote>().stickyNoteType;
        attachedStickyNotes[target].Add((newStickyNote, noteType));
        ApplyStickyNoteEffect(target, hitNormal, noteType);

        Debug.Log("�tⳂ𐶐����A�\��t���܂���: " + target.name);
    }

    void TryDetachStickyNote()
    {
        // ���ɕtⳂ���Ɏ����Ă���ꍇ�A�܂������Ă���tⳂ𗎂Ƃ�
        if (isHeldInHand)
        {
            ReleaseStickyNote();
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 5f))
        {
            Transform target = hit.transform.root;
            if (attachedStickyNotes.ContainsKey(target) && attachedStickyNotes[target].Count > 0)
            {
                DetachStickyNote(target);
            }
            else
            {
                Debug.Log("���C���tⳂ̓\���Ă���I�u�W�F�N�g�ɓ������Ă��܂���");
            }
        }
    }

    void DetachStickyNote(Transform target)
    {
        if (attachedStickyNotes.ContainsKey(target) && attachedStickyNotes[target].Count > 0)
        {
            var lastStickyNote = attachedStickyNotes[target][attachedStickyNotes[target].Count - 1];
            attachedStickyNotes[target].Remove(lastStickyNote);

            ResetStickyNoteEffect(target, lastStickyNote.noteType);

            activeStickyNote = lastStickyNote.noteObject;
            activeStickyNote.transform.SetParent(Camera.main.transform);
            activeStickyNote.transform.localPosition = Vector3.zero;
            activeStickyNote.transform.localScale = stickyNoteOriginalScale;
            activeStickyNote.SetActive(true);

            if (activeStickyNote.GetComponent<Rigidbody>() != null)
            {
                activeStickyNote.GetComponent<Rigidbody>().isKinematic = true;
            }
            if (activeStickyNote.GetComponent<Collider>() != null)
            {
                activeStickyNote.GetComponent<Collider>().enabled = false;
            }

            SetLayerRecursively(activeStickyNote, LayerMask.NameToLayer("OutlineTarget"));

            isHeldInHand = true;

            Debug.Log("�tⳂ��Ăю�Ɏ����܂���");
        }
    }

    void ApplyStickyNoteEffect(Transform target, Vector3 hitNormal, StickyNoteType noteType)
    {
        if (noteType == StickyNoteType.ScaleUp)
        {
            UpdatePrefabScale(target, increase: true);
        }
        else if (noteType == StickyNoteType.MoveBackward)
        {
            moveDirections[target] = -hitNormal.normalized;
        }
        else if (noteType == StickyNoteType.Jump)
        {
            // �n�ʂɐG��Ă���ꍇ�̂݃W�����v���J�n
            if (IsGrounded(target) && !jumpCoroutines.ContainsKey(target))
            {
                var jumpCoroutine = StartCoroutine(JumpRoutine(target));
                jumpCoroutines[target] = jumpCoroutine;
            }
        }
        else if (noteType == StickyNoteType.Glow)
        {
            ApplyGlowEffect(target, true);
        }
        if (noteType == StickyNoteType.Clone)
        {
            ApplyCloneEffect(target);
        }
    }

  System.Collections.IEnumerator JumpRoutine(Transform target)
{
    Rigidbody rb = target.GetComponent<Rigidbody>();

    while (true)
    {
        // �n�ʂɐG��Ă���ꍇ�̂݃W�����v
        if (rb != null && IsGrounded(target))
        {
           
            // Y�����̑��x�����Z�b�g���A�W�����v�͂�K�p
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); 
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                yield return new WaitForSeconds(2f); // �ڐG��1�b��ɃW�����v


            }
        else
        {
            yield return null; // �󒆂ɂ���ꍇ�̓W�����v���Ȃ�
        }
    }
}


    void ResetStickyNoteEffect(Transform target, StickyNoteType noteType)
    {
        if (noteType == StickyNoteType.ScaleUp)
        {
            // �c���ScaleUp�tⳂ̐����m�F
            int remainingScaleUpCount = attachedStickyNotes[target].FindAll(note => note.noteType == StickyNoteType.ScaleUp).Count;

            // ���̃X�P�[���ɑ΂��āAScaleUp�̕tⳂ̐��ɉ������V�����X�P�[�����v�Z
            Vector3 newScale = originalScales[target] * (1 + 1f * remainingScaleUpCount);

            // �X���[�Y�ɐV�����X�P�[���ɕω�
            StartCoroutine(SmoothScale(target, newScale));
        }
        else if (noteType == StickyNoteType.MoveBackward)
        {
            moveDirections[target] = Vector3.zero;
        }
        else if (noteType == StickyNoteType.Jump)
        {
            if (jumpCoroutines.ContainsKey(target))
            {
                StopCoroutine(jumpCoroutines[target]);
                jumpCoroutines.Remove(target);
            }
        }
        else if (noteType == StickyNoteType.Glow)
        {
            ApplyGlowEffect(target, false);
        }
        else if (noteType == StickyNoteType.Clone)
        {
            // �Ή�����N���[����1�폜
            RemoveClone(target);
        }
    }

    void RemoveClone(Transform target)
    {
        if (clones.ContainsKey(target) && clones[target].Count > 0)
        {
            // �N���[�����X�g����ŏ��̃N���[�����擾���č폜
            var cloneToRemove = clones[target][0];
            clones[target].RemoveAt(0);

            if (cloneToRemove != null)
            {
                StartCoroutine(DissolveAndDestroyClone(cloneToRemove));
            }

            Debug.Log("�N���[����1�폜����܂���: " + cloneToRemove.name);
        }
    }

    IEnumerator DissolveAndDestroyClone(GameObject clone)
    {
        Renderer[] renderers = clone.GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            if (renderer.material.HasProperty("_Dissolve"))
            {
                Material material = renderer.material;
                float dissolveValue = 0f;

                while (dissolveValue < 1f)
                {
                    dissolveValue += Time.deltaTime; // ���Ԃɉ�����Dissolve�l�𑝉�
                    material.SetFloat("_Dissolve", dissolveValue);
                    yield return null;
                }
            }
        }

        Destroy(clone); // ���S��Dissolve������폜
    }

    


    bool IsGrounded(Transform target)
    {
        float groundCheckRadius = 0.2f; // ���a���������ݒ肵�Đڒn����̐��x���グ��
        Vector3 groundCheckPosition = target.position + Vector3.down * target.localScale.y * 0.5f;

        bool grounded = Physics.CheckSphere(groundCheckPosition, groundCheckRadius);

        if (grounded)
        {
            Debug.Log("�n�ʂɐG��Ă��܂�: " + target.name);
        }
        else
        {
            Debug.Log("�n�ʂɐG��Ă��܂���: " + target.name);
        }

        return grounded;
    }




    void UpdatePrefabScale(Transform target, bool increase)
    {
        int scaleUpCount = attachedStickyNotes[target].FindAll(note => note.noteType == StickyNoteType.ScaleUp).Count;
        Vector3 newScale = increase
            ? originalScales[target] * (1 + 1f * scaleUpCount)
            : originalScales[target];
        StartCoroutine(SmoothScale(target, newScale));
    }


    void MoveObjectBackward(Transform target)
    {
        if (moveDirections.ContainsKey(target) && moveDirections[target] != Vector3.zero)
        {
            Vector3 movement = moveDirections[target] * moveSpeed * Time.deltaTime;
            if (!Physics.Raycast(target.position, moveDirections[target], 0.1f))
            {
                target.position += movement;
            }
        }
    }
    void ApplyCloneEffect(Transform target)
    {
        if (!clones.ContainsKey(target))
        {
            clones[target] = new List<GameObject>();
        }

        // �ő�N���[�������m�F
        if (clones[target].Count >= maxClones)
        {
            Debug.Log("�ő�N���[�����ɒB���Ă��܂�: " + target.name);
            return;
        }

        // �N���[�����쐬
        GameObject clone = Instantiate(target.gameObject, target.position, target.rotation);

        // �N���[���S�̂̃}�e���A����ύX
        Renderer[] renderers = clone.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (cloneMaterial != null)
            {
                // �ʂ̃}�e���A���C���X�^���X���쐬
                Material newMaterial = new Material(cloneMaterial);

                // �^�C�����O���^�[�Q�b�g�̃X�P�[���Ɋ�Â��Đݒ�
                Vector3 scale = renderer.transform.lossyScale; // ���[���h�X�P�[�����擾
                newMaterial.mainTextureScale = new Vector2(scale.x, scale.y); // X, Y �X�P�[���Ɋ�Â��� tiling �ݒ�

                // �}�e���A����Renderer�ɓK�p
                renderer.material = newMaterial;
            }
        }

        // �N���[���̕tⳂ��폜
        StickyNote[] stickyNotes = clone.GetComponentsInChildren<StickyNote>();
        foreach (var stickyNote in stickyNotes)
        {
            Destroy(stickyNote.gameObject);
        }

        // �N���[���̃��C���[��ύX
        SetLayerRecursively(clone, LayerMask.NameToLayer("OutlineTarget"));

        clones[target].Add(clone);
        Debug.Log("�N���[�����쐬����A�}�e���A�����ύX����܂���: " + clone.name);
    }



    void ApplyGlowEffect(Transform target, bool enable)
    {
        if (enable)
        {
            if (!jumpCoroutines.ContainsKey(target))
            {
                var glowCoroutine = StartCoroutine(UpdateGlowEffect(target));
                jumpCoroutines[target] = glowCoroutine;
            }
            Debug.Log($"Glow�G�t�F�N�g���K�p����܂���: {target.name}");
        }
        else
        {
            if (jumpCoroutines.ContainsKey(target))
            {
                StopCoroutine(jumpCoroutines[target]);
                jumpCoroutines.Remove(target);
            }
            RemoveGlowEffect(target);
            Debug.Log($"Glow�G�t�F�N�g����������܂���: {target.name}");
        }
    }

    IEnumerator UpdateGlowEffect(Transform target)
    {
        GameObject lightObject = null;
        Light light = target.GetComponentInChildren<Light>();

        if (light == null)
        {
            // ���C�g���Ȃ��ꍇ�͒ǉ�
            lightObject = new GameObject("GlowLight");
            lightObject.transform.SetParent(target);
            lightObject.transform.localPosition = Vector3.zero;
            light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.yellow;

            // UniversalAdditionalLightData��ǉ�
            if (lightObject.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>() == null)
            {
                lightObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
            }
        }

        light.enabled = true;

        // �v���n�u�S�̂̃G�~�b�V������L����
        SetEmissionForPrefab(target, Color.yellow * 1f);

        // �T�C�Y���Ď����Č��̋����Ɣ͈͂��X�V
        while (true)
        {
            if (target == null || light == null)
            {
                yield break; // ���C�g�܂��̓^�[�Q�b�g���j������Ă���ꍇ�A�R���[�`�����I��
            }

            float scaleFactor = target.localScale.magnitude; // �I�u�W�F�N�g�̃X�P�[���Ɋ�Â��ċ��x��ύX
            light.range = 10f * scaleFactor; // ���C�g�͈̔͂��X�V
            light.intensity = 0.5f * scaleFactor; // ���C�g�̋��x���X�V
            yield return null; // ���̃t���[���܂őҋ@
        }
    }

    void RemoveGlowEffect(Transform target)
    {
        // �v���n�u�S�̂̃G�~�b�V�����𖳌���
        SetEmissionForPrefab(target, Color.black);

        Light light = target.GetComponentInChildren<Light>();
        if (light != null)
        {
            Destroy(light.gameObject); // ���C�g�I�u�W�F�N�g�����S�ɍ폜
        }
    }

    void SetEmissionForPrefab(Transform prefab, Color emissionColor)
    {
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(); // �v���n�u�S�̂�Renderer���擾
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", emissionColor); // �G�~�b�V�����J���[��ݒ�
                renderer.SetPropertyBlock(block);
            }
        }
    }

    void RemoveClones(Transform target)
    {
        if (clones.ContainsKey(target))
        {
            foreach (var clone in clones[target])
            {
                if (clone != null)
                {
                    Destroy(clone);
                }
            }

            clones[target].Clear();
            Debug.Log("�N���[�����폜����܂���: " + target.name);
        }
    }






    System.Collections.IEnumerator SmoothScale(Transform target, Vector3 targetScale)
    {
        Vector3 startScale = target.localScale;
        float progress = 0f;

        while (progress < 1f)
        {
            target.localScale = Vector3.Lerp(startScale, targetScale, progress);
            progress += Time.deltaTime * scaleSpeed;
            yield return null;
        }

        target.localScale = targetScale;
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}