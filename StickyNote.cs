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
    public int maxClones = 3; // 最大クローン数
    public Material cloneMaterial; // クローンに適用するマテリアル
    private OutlineObjectChecker outlineChecker;

    private Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, List<(GameObject noteObject, StickyNoteType noteType)>> attachedStickyNotes = new Dictionary<Transform, List<(GameObject, StickyNoteType)>>();
    private Dictionary<Transform, Vector3> moveDirections = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Coroutine> jumpCoroutines = new Dictionary<Transform, Coroutine>();
    private Dictionary<Transform, List<GameObject>> clones = new Dictionary<Transform, List<GameObject>>(); // クローン管理用


    private Vector3 stickyNoteOriginalScale;
    private bool isHeldInHand = false;

    void Start()
    {
        if (cloneMaterial == null)
        {
            Debug.LogError("クローン用のマテリアルが設定されていません！");
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
            Debug.Log("すでに付箋を手に持っています");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            if (hit.collider != null && hit.collider.gameObject.CompareTag("StickyNote"))
            {
                // 新しい付箋を生成
                activeStickyNote = Instantiate(stickyNotePrefab, hit.point, Quaternion.identity);

                // 初期設定
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

                Debug.Log("新しい付箋を生成し、拾いました: " + activeStickyNote.name);
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
            Debug.Log("付箋を手放しました");
        }
    }

    void PositionStickyNoteInHand()
    {
        Vector3 offset = new Vector3(-0.4f, -0.2f, 0.5f);
        Vector3 targetPosition = Camera.main.transform.position + Camera.main.transform.rotation * offset;
        activeStickyNote.transform.position = targetPosition;
        activeStickyNote.transform.localRotation = Quaternion.Euler(15, -20, 0);
        activeStickyNote.transform.localScale = stickyNoteOriginalScale;

        // 壁との距離を確認し、壁との衝突を避ける
        Ray ray = new Ray(Camera.main.transform.position, targetPosition - Camera.main.transform.position);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, offset.magnitude))
        {
            // 壁と衝突している場合は壁手前に配置する
            activeStickyNote.transform.position = hit.point - ray.direction * 0.05f; // 壁から少し離れる
        }
    }

    void TryAttachToSurface()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // pickupRangeにオブジェクトのスケールの大きさを追加
        if (Physics.Raycast(ray, out hit, pickupRange + transform.localScale.magnitude))
        {
            Transform target = hit.transform.root;
            if (hit.collider.CompareTag("Pickable"))
            {
                AttachToSurface(target, hit.point, hit.normal);
            }
            else
            {
                Debug.Log("このオブジェクトには付箋を貼れません: " + hit.transform.name);
            }
        }
    }

    void AttachToSurface(Transform target, Vector3 hitPoint, Vector3 hitNormal)
    {
        // 新しい付箋を生成
        GameObject newStickyNote = Instantiate(stickyNotePrefab, hitPoint, Quaternion.LookRotation(-hitNormal));
        newStickyNote.transform.SetParent(target);
        newStickyNote.transform.localScale = stickyNoteOriginalScale;

        // アクティブ付箋をリセット
        activeStickyNote = null;
        isHeldInHand = false;

        // 設定を保持する
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

        Debug.Log("付箋を生成し、貼り付けました: " + target.name);
    }

    void TryDetachStickyNote()
    {
        // 既に付箋を手に持っている場合、まず持っている付箋を落とす
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
                Debug.Log("レイが付箋の貼られているオブジェクトに当たっていません");
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

            Debug.Log("付箋を再び手に持ちました");
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
            // 地面に触れている場合のみジャンプを開始
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
        // 地面に触れている場合のみジャンプ
        if (rb != null && IsGrounded(target))
        {
           
            // Y方向の速度をリセットし、ジャンプ力を適用
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); 
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                yield return new WaitForSeconds(2f); // 接触後1秒後にジャンプ


            }
        else
        {
            yield return null; // 空中にいる場合はジャンプしない
        }
    }
}


    void ResetStickyNoteEffect(Transform target, StickyNoteType noteType)
    {
        if (noteType == StickyNoteType.ScaleUp)
        {
            // 残りのScaleUp付箋の数を確認
            int remainingScaleUpCount = attachedStickyNotes[target].FindAll(note => note.noteType == StickyNoteType.ScaleUp).Count;

            // 元のスケールに対して、ScaleUpの付箋の数に応じた新しいスケールを計算
            Vector3 newScale = originalScales[target] * (1 + 1f * remainingScaleUpCount);

            // スムーズに新しいスケールに変化
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
            // 対応するクローンを1つ削除
            RemoveClone(target);
        }
    }

    void RemoveClone(Transform target)
    {
        if (clones.ContainsKey(target) && clones[target].Count > 0)
        {
            // クローンリストから最初のクローンを取得して削除
            var cloneToRemove = clones[target][0];
            clones[target].RemoveAt(0);

            if (cloneToRemove != null)
            {
                StartCoroutine(DissolveAndDestroyClone(cloneToRemove));
            }

            Debug.Log("クローンが1つ削除されました: " + cloneToRemove.name);
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
                    dissolveValue += Time.deltaTime; // 時間に応じてDissolve値を増加
                    material.SetFloat("_Dissolve", dissolveValue);
                    yield return null;
                }
            }
        }

        Destroy(clone); // 完全にDissolveしたら削除
    }

    


    bool IsGrounded(Transform target)
    {
        float groundCheckRadius = 0.2f; // 半径を小さく設定して接地判定の精度を上げる
        Vector3 groundCheckPosition = target.position + Vector3.down * target.localScale.y * 0.5f;

        bool grounded = Physics.CheckSphere(groundCheckPosition, groundCheckRadius);

        if (grounded)
        {
            Debug.Log("地面に触れています: " + target.name);
        }
        else
        {
            Debug.Log("地面に触れていません: " + target.name);
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

        // 最大クローン数を確認
        if (clones[target].Count >= maxClones)
        {
            Debug.Log("最大クローン数に達しています: " + target.name);
            return;
        }

        // クローンを作成
        GameObject clone = Instantiate(target.gameObject, target.position, target.rotation);

        // クローン全体のマテリアルを変更
        Renderer[] renderers = clone.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (cloneMaterial != null)
            {
                // 個別のマテリアルインスタンスを作成
                Material newMaterial = new Material(cloneMaterial);

                // タイリングをターゲットのスケールに基づいて設定
                Vector3 scale = renderer.transform.lossyScale; // ワールドスケールを取得
                newMaterial.mainTextureScale = new Vector2(scale.x, scale.y); // X, Y スケールに基づいて tiling 設定

                // マテリアルをRendererに適用
                renderer.material = newMaterial;
            }
        }

        // クローンの付箋を削除
        StickyNote[] stickyNotes = clone.GetComponentsInChildren<StickyNote>();
        foreach (var stickyNote in stickyNotes)
        {
            Destroy(stickyNote.gameObject);
        }

        // クローンのレイヤーを変更
        SetLayerRecursively(clone, LayerMask.NameToLayer("OutlineTarget"));

        clones[target].Add(clone);
        Debug.Log("クローンが作成され、マテリアルが変更されました: " + clone.name);
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
            Debug.Log($"Glowエフェクトが適用されました: {target.name}");
        }
        else
        {
            if (jumpCoroutines.ContainsKey(target))
            {
                StopCoroutine(jumpCoroutines[target]);
                jumpCoroutines.Remove(target);
            }
            RemoveGlowEffect(target);
            Debug.Log($"Glowエフェクトが解除されました: {target.name}");
        }
    }

    IEnumerator UpdateGlowEffect(Transform target)
    {
        GameObject lightObject = null;
        Light light = target.GetComponentInChildren<Light>();

        if (light == null)
        {
            // ライトがない場合は追加
            lightObject = new GameObject("GlowLight");
            lightObject.transform.SetParent(target);
            lightObject.transform.localPosition = Vector3.zero;
            light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.yellow;

            // UniversalAdditionalLightDataを追加
            if (lightObject.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>() == null)
            {
                lightObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
            }
        }

        light.enabled = true;

        // プレハブ全体のエミッションを有効化
        SetEmissionForPrefab(target, Color.yellow * 1f);

        // サイズを監視して光の強さと範囲を更新
        while (true)
        {
            if (target == null || light == null)
            {
                yield break; // ライトまたはターゲットが破棄されている場合、コルーチンを終了
            }

            float scaleFactor = target.localScale.magnitude; // オブジェクトのスケールに基づいて強度を変更
            light.range = 10f * scaleFactor; // ライトの範囲を更新
            light.intensity = 0.5f * scaleFactor; // ライトの強度を更新
            yield return null; // 次のフレームまで待機
        }
    }

    void RemoveGlowEffect(Transform target)
    {
        // プレハブ全体のエミッションを無効化
        SetEmissionForPrefab(target, Color.black);

        Light light = target.GetComponentInChildren<Light>();
        if (light != null)
        {
            Destroy(light.gameObject); // ライトオブジェクトを安全に削除
        }
    }

    void SetEmissionForPrefab(Transform prefab, Color emissionColor)
    {
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(); // プレハブ全体のRendererを取得
        foreach (var renderer in renderers)
        {
            if (renderer != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", emissionColor); // エミッションカラーを設定
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
            Debug.Log("クローンが削除されました: " + target.name);
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