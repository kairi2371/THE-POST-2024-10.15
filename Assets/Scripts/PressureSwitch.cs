using UnityEngine;
using System.Collections.Generic;
using System;

public class PressureSwitch : MonoBehaviour
{
    [Header("Switch Settings")]
    public float activationWeight = 1f; // スイッチを押すために必要な最小重量
    public LayerMask detectionLayer; // 検知する物体のレイヤー
    public event Action onPressed; // スイッチが押されたときのイベント
    public event Action onReleased; // スイッチが離されたときのイベント

    private List<Rigidbody> objectsOnSwitch = new List<Rigidbody>(); // スイッチ上の物体リスト
    private bool isPressed = false; // 現在のスイッチの状態
    private Renderer switchRenderer; // スイッチのRenderer
    private MaterialPropertyBlock materialPropertyBlock; // マテリアルプロパティブロック

    [Header("Emission Settings")]
    public Color emissionOnColor = Color.green; // 押されたときのエミッションカラー
    public Color emissionOffColor = Color.red; // 離されたときのエミッションカラー
    public float emissionIntensity = 2f; // エミッションの強さ

    public bool IsPressed => isPressed; // 外部から状態を取得可能

    void Start()
    {
        // RendererとMaterialPropertyBlockを初期化
        switchRenderer = GetComponent<Renderer>();
        if (switchRenderer != null)
        {
            materialPropertyBlock = new MaterialPropertyBlock();
            switchRenderer.GetPropertyBlock(materialPropertyBlock);
            UpdateEmission(emissionOffColor); // 初期状態を設定
        }

        // イベントにメソッドを登録
        onPressed += HandlePressed;
        onReleased += HandleReleased;
    }

    void FixedUpdate()
    {
        // スイッチ上の物体の合計重量を計算
        float totalWeight = 0f;
        for (int i = objectsOnSwitch.Count - 1; i >= 0; i--)
        {
            if (objectsOnSwitch[i] == null) // 物体が破棄されている場合リストから削除
            {
                objectsOnSwitch.RemoveAt(i);
                continue;
            }
            totalWeight += objectsOnSwitch[i].mass;
        }

        // スイッチの状態を更新
        if (totalWeight >= activationWeight && !isPressed)
        {
            isPressed = true;
            onPressed?.Invoke(); // スイッチが押されたときのイベントを呼び出す
        }
        else if (totalWeight < activationWeight && isPressed)
        {
            isPressed = false;
            onReleased?.Invoke(); // スイッチが離されたときのイベントを呼び出す
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // スイッチに触れた物体を検知してリストに追加
        if (IsInLayerMask(collision.gameObject, detectionLayer))
        {
            Rigidbody rb = collision.rigidbody;
            if (rb != null && !objectsOnSwitch.Contains(rb))
            {
                objectsOnSwitch.Add(rb);
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // スイッチから離れた物体をリストから削除
        if (IsInLayerMask(collision.gameObject, detectionLayer))
        {
            Rigidbody rb = collision.rigidbody;
            if (rb != null && objectsOnSwitch.Contains(rb))
            {
                objectsOnSwitch.Remove(rb);
            }
        }
    }

    private bool IsInLayerMask(GameObject obj, LayerMask layerMask)
    {
        return ((1 << obj.layer) & layerMask) != 0;
    }

    private void HandlePressed()
    {
        Debug.Log("スイッチが押されました！");
        UpdateEmission(emissionOnColor);
    }

    private void HandleReleased()
    {
        Debug.Log("スイッチが離されました！");
        UpdateEmission(emissionOffColor);
    }

    private void UpdateEmission(Color emissionColor)
    {
        if (switchRenderer != null && materialPropertyBlock != null)
        {
            // エミッションカラーを更新
            materialPropertyBlock.SetColor("_EmissionColor", emissionColor * emissionIntensity);
            switchRenderer.SetPropertyBlock(materialPropertyBlock);
        }
    }
}
