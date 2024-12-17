using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Gate : MonoBehaviour
{
    [Header("Gate Settings")]
    public Renderer[] gateRenderers; // �Q�[�g�Ɋ܂܂�邷�ׂĂ�Renderer
    public Collider gateCollider; // �Q�[�g��Collider
    public float dissolveSpeed = 1f; // Dissolve�̑��x
    public List<PressureSwitch> linkedSwitches; // �����N���ꂽ�X�C�b�`
    public bool requireAllSwitches = true; // �S�ẴX�C�b�`�������K�v�����邩�ifalse�Ȃ�ǂꂩ��j
    public AudioSource gateSound; // �Q�[�g�̓��쉹

    private List<Material> gateMaterials = new List<Material>(); // �Q�[�g���̂��ׂẴ}�e���A����ێ�
    private bool isOpen = false; // ���݂̏�ԁi�J���Ă��邩�j
    private Coroutine dissolveCoroutine = null; // Dissolve�����̃R���[�`��

    void Start()
    {
        if (gateRenderers == null || gateRenderers.Length == 0 || gateCollider == null)
        {
            Debug.LogError("GateRenderers�܂���GateCollider���ݒ肳��Ă��܂���I");
            return;
        }

        // �Q�[�g���̂��ׂẴ}�e���A�������W
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
                    Debug.LogWarning($"{renderer.gameObject.name} ��Dissolve�v���p�e�B�������Ȃ��}�e���A��������܂�");
                }
            }
        }

        // ������Ԃ�Dissolve��0�ɐݒ�
        foreach (var material in gateMaterials)
        {
            material.SetFloat("_Dissolve", 0f);
        }

        // �X�C�b�`�̃C�x���g��o�^
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
            // �S�ẴX�C�b�`��������Ă��邩�m�F
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
            // �ǂꂩ��ł�������Ă���ΊJ��
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
        // �����~
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
        // �����Đ�
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

            // ���ׂẴ}�e���A����Dissolve�v���p�e�B���X�V
            foreach (var material in gateMaterials)
            {
                material.SetFloat("_Dissolve", dissolveValue);
            }

            yield return null;
        }

        // �ŏI�l���Z�b�g
        foreach (var material in gateMaterials)
        {
            material.SetFloat("_Dissolve", endValue);
        }

        // Collider��؂�ւ���
        gateCollider.enabled = enableCollider;

        dissolveCoroutine = null;
    }
}