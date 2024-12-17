using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Gate : MonoBehaviour
{
    [Header("Gate Settings")]
    public Renderer[] gateRenderers; // ゲートに含まれるすべてのRenderer
    public Collider gateCollider; // ゲートのCollider
    public float dissolveSpeed = 1f; // Dissolveの速度
    public List<PressureSwitch> linkedSwitches; // リンクされたスイッチ
    public bool requireAllSwitches = true; // 全てのスイッチを押す必要があるか（falseならどれか一つ）
    public AudioSource gateSound; // ゲートの動作音

    private List<Material> gateMaterials = new List<Material>(); // ゲート内のすべてのマテリアルを保持
    private bool isOpen = false; // 現在の状態（開いているか）
    private Coroutine dissolveCoroutine = null; // Dissolve処理のコルーチン

    void Start()
    {
        if (gateRenderers == null || gateRenderers.Length == 0 || gateCollider == null)
        {
            Debug.LogError("GateRenderersまたはGateColliderが設定されていません！");
            return;
        }

        // ゲート内のすべてのマテリアルを収集
        foreach (var renderer in gateRenderers)
        {
            foreach (var material in renderer.materials)
            {
                if (material.HasProperty("_Dissolve"))
                {
                    gateMaterials.Add(material);
                }
                else
                {
                    Debug.LogWarning($"{renderer.gameObject.name} にDissolveプロパティを持たないマテリアルがあります");
                }
            }
        }

        // 初期状態のDissolveを0に設定
        foreach (var material in gateMaterials)
        {
            material.SetFloat("_Dissolve", 0f);
        }

        // スイッチのイベントを登録
        foreach (var pressureSwitch in linkedSwitches)
        {
            pressureSwitch.onPressed += CheckSwitches;
            pressureSwitch.onReleased += CheckSwitches;
        }
    }

    void CheckSwitches()
    {
        if (requireAllSwitches)
        {
            // 全てのスイッチが押されているか確認
            foreach (var pressureSwitch in linkedSwitches)
            {
                if (!pressureSwitch.IsPressed)
                {
                    CloseGate();
                    return;
                }
            }
            OpenGate();
        }
        else
        {
            // どれか一つでも押されていれば開く
            foreach (var pressureSwitch in linkedSwitches)
            {
                if (pressureSwitch.IsPressed)
                {
                    OpenGate();
                    return;
                }
            }
            CloseGate();
        }
    }

    public void OpenGate()
    {
        if (isOpen) return;

        isOpen = true;

        if (dissolveCoroutine != null)
        {
            StopCoroutine(dissolveCoroutine);
        }
        // 音を停止
        if (gateSound != null && gateSound.isPlaying)
        {
            gateSound.Stop();
        }
        dissolveCoroutine = StartCoroutine(DissolveGate(0f, 0.85f, false));
    }

    public void CloseGate()
    {
        if (!isOpen) return;

        isOpen = false;

        if (dissolveCoroutine != null)
        {
            StopCoroutine(dissolveCoroutine);
        }
        // 音を再生
        if (gateSound != null && !gateSound.isPlaying)
        {
            gateSound.Play();
        }
        dissolveCoroutine = StartCoroutine(DissolveGate(0.85f, 0f, true));
    }

    private IEnumerator DissolveGate(float startValue, float endValue, bool enableCollider)
    {
        float dissolveValue = startValue;

        while (Mathf.Abs(dissolveValue - endValue) > 0.01f)
        {
            dissolveValue = Mathf.MoveTowards(dissolveValue, endValue, Time.deltaTime * dissolveSpeed);

            // すべてのマテリアルのDissolveプロパティを更新
            foreach (var material in gateMaterials)
            {
                material.SetFloat("_Dissolve", dissolveValue);
            }

            yield return null;
        }

        // 最終値をセット
        foreach (var material in gateMaterials)
        {
            material.SetFloat("_Dissolve", endValue);
        }

        // Colliderを切り替える
        gateCollider.enabled = enableCollider;

        dissolveCoroutine = null;
    }
}