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
        Clone,
        Stop
    }
    public Rigidbody targetRigidbody;
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
    private Transform lastTarget; // 最後にグレイスケールを適用したオブジェクト

    private Dictionary<Renderer, Shader> originalShaders = new Dictionary<Renderer, Shader>();


    private Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, List<(GameObject noteObject, StickyNoteType noteType)>> attachedStickyNotes = new Dictionary<Transform, List<(GameObject, StickyNoteType)>>();
    private Dictionary<Transform, Vector3> moveDirections = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Coroutine> jumpCoroutines = new Dictionary<Transform, Coroutine>();
    private Dictionary<Transform, List<GameObject>> clones = new Dictionary<Transform, List<GameObject>>(); // クローン管理用
    private Dictionary<Transform, bool> kinematicStates = new Dictionary<Transform, bool>(); // isKinematicの元の状態を保持

    private Vector3 stickyNoteOriginalScale;
    private bool isHeldInHand = false;

    public Shader stopShader; // stop 用の白黒シェーダー
    private Shader originalShader; // 元のシェーダーを保持
    private Renderer targetRenderer; // 対象オブジェクトのRenderer

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
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, pickupRange) && Input.GetKeyDown(KeyCode.E))
        {
            if (hit.collider != null && hit.collider.gameObject.CompareTag("StickyNote"))
            {
                ReleaseStickyNote();
            }
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!isHeldInHand)
                TryPickupStickyNote();
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
                ReleaseStickyNote();
                activeStickyNote = hit.collider.gameObject;
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

                Debug.Log("付箋を拾いました: " + activeStickyNote.name + " - 命令タイプ: " + activeStickyNote.GetComponent<StickyNote>().stickyNoteType);
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
        Vector3 stickyNotePosition = hitPoint + hitNormal * offsetDistance;
        activeStickyNote.transform.position = stickyNotePosition;
        activeStickyNote.transform.rotation = Quaternion.LookRotation(-hitNormal);
        activeStickyNote.transform.SetParent(target);
        activeStickyNote.transform.localScale = stickyNoteOriginalScale;
        isHeldInHand = false;

        // 付箋の種類をターゲットの種類に合わせる
        StickyNote targetStickyNote = target.GetComponent<StickyNote>();
        if (targetStickyNote != null)
        {
            StickyNote activeNoteStickyNote = activeStickyNote.GetComponent<StickyNote>();
            if (activeNoteStickyNote != null)
            {
                activeNoteStickyNote.stickyNoteType = targetStickyNote.stickyNoteType;
                Debug.Log($"付箋のタイプを同期: {activeNoteStickyNote.stickyNoteType}");
            }
        }

        if (!originalScales.ContainsKey(target))
        {
            originalScales[target] = target.localScale;
        }

        if (!attachedStickyNotes.ContainsKey(target))
        {
            attachedStickyNotes[target] = new List<(GameObject, StickyNoteType)>();
        }

        StickyNoteType noteType = activeStickyNote.GetComponent<StickyNote>().stickyNoteType;
        attachedStickyNotes[target].Add((activeStickyNote, noteType));
        ApplyStickyNoteEffect(target, hitNormal, noteType);

        Debug.Log("付箋が貼られました: " + target.name + " - 命令タイプ: " + noteType);

        activeStickyNote = null;
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
            if (noteType == StickyNoteType.Jump)
            {
                if (!jumpCoroutines.ContainsKey(target))
                {
                    Debug.Log($"[Jump Effect] ジャンプ開始: {target.name}");
                    var coroutine = StartCoroutine(JumpRoutine(target));
                    jumpCoroutines[target] = coroutine;
                }
            }
        }
        else if (noteType == StickyNoteType.Glow)
        {
            ApplyGlowEffect(target, true);
        }
        else if (noteType == StickyNoteType.Clone)
        {
            ApplyCloneEffect(target);
        }
        else if (noteType == StickyNoteType.Stop)
        {
            StopObjectMovement(target); // 動きを静止
        }
    }

    IEnumerator JumpRoutine(Transform target)
    {
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"[JumpRoutine] Rigidbodyが見つかりません: {target.name}");
            yield break;
        }

        while (true)
        {
            if (IsGrounded(target))
            {
                Debug.Log($"[JumpRoutine] {target.name} がジャンプします！");
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); // Y軸速度をリセット
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); // ジャンプ力を適用
                yield return new WaitForSeconds(2f); // 次のジャンプまで待機
            }
            else
            {
                Debug.Log($"[JumpRoutine] {target.name} は地面にいません。待機中...");
                yield return null; // 空中の場合は待機
            }
        }
    }


    void ResetStickyNoteEffect(Transform target, StickyNoteType noteType)
    {
        if (noteType == StickyNoteType.ScaleUp)
        {
            Rigidbody targetRigidbody = target.GetComponent<Rigidbody>();
            targetRigidbody.mass /= 2;
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
        if (noteType == StickyNoteType.Stop)
        {
            ResumeObjectMovement(target); // 動きを再開
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

                while (dissolveValue < 0.85f)
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
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"[IsGrounded] Rigidbodyが見つかりません: {target.name}");
            return false;
        }

        // 接地判定用のレイキャスト
        float groundCheckDistance = 0.3f * transform.localScale.y; // レイキャストの距離
        Vector3 groundCheckOrigin = target.position + Vector3.down * (rb.transform.localScale.y / 2);

        bool isGrounded = Physics.Raycast(groundCheckOrigin, Vector3.down, groundCheckDistance);
        Debug.Log($"[IsGrounded] {target.name} の接地判定: {isGrounded}");
        return isGrounded;
    }






    void UpdatePrefabScale(Transform target, bool increase)
    {
        Rigidbody targetRigidbody = target.GetComponent<Rigidbody>();
        int scaleUpCount = attachedStickyNotes[target].FindAll(note => note.noteType == StickyNoteType.ScaleUp).Count;
        Vector3 newScale = increase
            ? originalScales[target] * (2f * scaleUpCount)
            : originalScales[target];
        StartCoroutine(SmoothScale(target, newScale));
        targetRigidbody.mass *= 2; // 質量を2倍にする
    }


    void MoveObjectBackward(Transform target)
    {
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (moveDirections.ContainsKey(target) && moveDirections[target] != Vector3.zero && rb.isKinematic == false)
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
    void StopObjectMovement(Transform target)
    {
        // 対象オブジェクト内のすべてのRendererを取得
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            // 付箋のRendererは除外
            if (IsStickyNote(renderer))
            {
                continue;
            }

            // 元のシェーダーを保存
            if (renderer != null && !originalShaders.ContainsKey(renderer))
            {
                originalShaders[renderer] = renderer.material.shader;
            }

            // グレイスケール用シェーダーに変更
            if (renderer != null && stopShader != null)
            {
                renderer.material.shader = stopShader;
            }
        }

        // Rigidbodyを静止
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!kinematicStates.ContainsKey(target))
            {
                kinematicStates[target] = rb.isKinematic; // 元の状態を保存
                rb.isKinematic = true; // 物体を静止させる
            }
        }
    }

    void ResumeObjectMovement(Transform target)
    {
        // 対象オブジェクト内のすべてのRendererを取得
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            // 付箋のRendererは除外
            if (IsStickyNote(renderer))
            {
                continue;
            }

            // 元のシェーダーに戻す
            if (renderer != null && originalShaders.ContainsKey(renderer))
            {
                renderer.material.shader = originalShaders[renderer];
                originalShaders.Remove(renderer);
            }
        }

        // Rigidbodyの動きを再開
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null && kinematicStates.ContainsKey(target))
        {
            rb.isKinematic = kinematicStates[target];
            kinematicStates.Remove(target);
        }
    }

    bool IsStickyNote(Renderer renderer)
    {
        // Rendererが付箋の一部かどうかを確認
        StickyNote stickyNote = renderer.GetComponentInParent<StickyNote>();
        return stickyNote != null;
    }
    public void ApplyStopEffect(Transform target)
    {
        // 前のオブジェクトの状態を元に戻す
        if (lastTarget != null && lastTarget != target)
        {
            ResetStopEffect(lastTarget);
        }

        // 新しいオブジェクトにグレイスケール効果を適用
        targetRenderer = target.GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            // 元のシェーダーを保存
            originalShader = targetRenderer.material.shader;

            // グレイスケールシェーダーを適用
            targetRenderer.material.shader = stopShader;
            Debug.Log($"Stop効果を適用しました: {target.name}");

            // 静止効果を適用
            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            // 最後に適用したオブジェクトを更新
            lastTarget = target;
        }
        else
        {
            Debug.LogWarning($"Rendererが見つかりません: {target.name}");
        }
    }

    public void ResetStopEffect(Transform target)
    {
        // グレイスケールを解除して元のシェーダーに戻す
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null && originalShader != null)
        {
            renderer.material.shader = originalShader;
            Debug.Log($"Stop効果を解除しました: {target.name}");
        }

        // 静止効果を解除
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // 状態をリセット
        if (lastTarget == target)
        {
            lastTarget = null;
        }
    }


    /// <summary>
    /// トランスフォームをその場に固定し続ける
    /// </summary>

}