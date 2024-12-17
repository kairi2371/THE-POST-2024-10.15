using UnityEngine;

public class OutlineObjectChecker : MonoBehaviour
{
    public float rayDistance = 5f; // Ray�̔򋗗�
    private Transform currentOutlinePrefab; // ���ݎ����������Ă���OutlineTarget��Prefab�̃��[�g
    public LayerMask outlineTargetLayerMask; // ���肷��OutlineTarget���C���[��ݒ�

    void Update()
    {
        // �J��������O����Ray���΂��ăI�u�W�F�N�g���`�F�b�N
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        // �������O�ꂽ�ꍇ�A�O��Prefab�̃��C���[��OutlineTarget�ɖ߂�
        if (currentOutlinePrefab != null && !Physics.Raycast(ray, out hit, rayDistance, outlineTargetLayerMask))
        {
            ResetOutline();
        }

        // Raycast��OutlineTarget���C���[�̃I�u�W�F�N�g�ɓ��������ꍇ
        if (Physics.Raycast(ray, out hit, rayDistance, outlineTargetLayerMask))
        {
            Transform hitRoot = hit.collider.transform.root; // Prefab�̃��[�g�I�u�W�F�N�g���擾

            // �V����Prefab�Ɏ��������������ꍇ
            if (currentOutlinePrefab != hitRoot)
            {
                // �O��Prefab�̃��C���[��OutlineTarget�ɖ߂�
                if (currentOutlinePrefab != null)
                {
                    ResetOutline();
                }

                // �V����Prefab�̃��C���[��OutlineLayer�ɕύX
                currentOutlinePrefab = hitRoot;
                SetLayerRecursively(currentOutlinePrefab, LayerMask.NameToLayer("OutlineLayer"));
            }
        }
    }

    // Prefab�S�̂̃��C���[���ċA�I�ɕύX���郁�\�b�h
    void SetLayerRecursively(Transform obj, int newLayer)
    {
        obj.gameObject.layer = newLayer;
        foreach (Transform child in obj)
        {
            SetLayerRecursively(child, newLayer);
        }
    }

    // Outline�����Z�b�g���郁�\�b�h
    public void ResetOutline()
    {
        if (currentOutlinePrefab != null)
        {
            SetLayerRecursively(currentOutlinePrefab, LayerMask.NameToLayer("OutlineTarget"));
            currentOutlinePrefab = null;
        }
    }
}
