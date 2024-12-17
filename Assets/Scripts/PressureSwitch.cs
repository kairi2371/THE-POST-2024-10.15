using UnityEngine;
using System.Collections.Generic;
using System;

public class PressureSwitch : MonoBehaviour
{
    [Header("Switch Settings")]
    public float activationWeight = 1f; // �X�C�b�`���������߂ɕK�v�ȍŏ��d��
    public LayerMask detectionLayer; // ���m���镨�̂̃��C���[
    public event Action onPressed; // �X�C�b�`�������ꂽ�Ƃ��̃C�x���g
    public event Action onReleased; // �X�C�b�`�������ꂽ�Ƃ��̃C�x���g

    private List<Rigidbody> objectsOnSwitch = new List<Rigidbody>(); // �X�C�b�`��̕��̃��X�g
    private bool isPressed = false; // ���݂̃X�C�b�`�̏��
    private Renderer switchRenderer; // �X�C�b�`��Renderer
    private MaterialPropertyBlock materialPropertyBlock; // �}�e���A���v���p�e�B�u���b�N

    [Header("Emission Settings")]
    public Color emissionOnColor = Color.green; // �����ꂽ�Ƃ��̃G�~�b�V�����J���[
    public Color emissionOffColor = Color.red; // �����ꂽ�Ƃ��̃G�~�b�V�����J���[
    public float emissionIntensity = 2f; // �G�~�b�V�����̋���

    public bool IsPressed => isPressed; // �O�������Ԃ��擾�\

    void Start()
    {
        // Renderer��MaterialPropertyBlock��������
        switchRenderer = GetComponent<Renderer>();
        if (switchRenderer != null)
        {
            materialPropertyBlock = new MaterialPropertyBlock();
            switchRenderer.GetPropertyBlock(materialPropertyBlock);
            UpdateEmission(emissionOffColor); // ������Ԃ�ݒ�
        }

        // �C�x���g�Ƀ��\�b�h��o�^
        onPressed += HandlePressed;
        onReleased += HandleReleased;
    }

    void FixedUpdate()
    {
        // �X�C�b�`��̕��̂̍��v�d�ʂ��v�Z
        float totalWeight = 0f;
        for (int i = objectsOnSwitch.Count - 1; i >= 0; i--)
        {
            if (objectsOnSwitch[i] == null) // ���̂��j������Ă���ꍇ���X�g����폜
            {
                objectsOnSwitch.RemoveAt(i);
                continue;
            }
            totalWeight += objectsOnSwitch[i].mass;
        }

        // �X�C�b�`�̏�Ԃ��X�V
        if (totalWeight >= activationWeight && !isPressed)
        {
            isPressed = true;
            onPressed?.Invoke(); // �X�C�b�`�������ꂽ�Ƃ��̃C�x���g���Ăяo��
        }
        else if (totalWeight < activationWeight && isPressed)
        {
            isPressed = false;
            onReleased?.Invoke(); // �X�C�b�`�������ꂽ�Ƃ��̃C�x���g���Ăяo��
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // �X�C�b�`�ɐG�ꂽ���̂����m���ă��X�g�ɒǉ�
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
        // �X�C�b�`���痣�ꂽ���̂����X�g����폜
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
        Debug.Log("�X�C�b�`��������܂����I");
        UpdateEmission(emissionOnColor);
    }

    private void HandleReleased()
    {
        Debug.Log("�X�C�b�`��������܂����I");
        UpdateEmission(emissionOffColor);
    }

    private void UpdateEmission(Color emissionColor)
    {
        if (switchRenderer != null && materialPropertyBlock != null)
        {
            // �G�~�b�V�����J���[���X�V
            materialPropertyBlock.SetColor("_EmissionColor", emissionColor * emissionIntensity);
            switchRenderer.SetPropertyBlock(materialPropertyBlock);
        }
    }
}
