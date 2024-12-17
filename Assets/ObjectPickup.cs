using UnityEngine;
using UnityEngine.Rendering.PostProcessing; // �|�X�g�v���Z�b�V���O�֘A�̖��O���
using System.Collections.Generic;

public class ObjectPickup : MonoBehaviour
{
    public Camera playerCamera;
    public float pickupRange = 3f;
    public float holdDistance = 10f;
    public float throwForce = 300f;
    public LayerMask collisionMask;
    public float smoothingSpeed = 5f; // ��ԑ��x
    public float maxPickupMass = 20f; // �����グ����ő县��

    private GameObject heldObject = null;
    private Quaternion initialRotation;
    private Collider heldObjectCollider;
    private Rigidbody heldObjectRigidbody;
    private Collider playerCollider;
    private bool isHoldingObject = false;        // �I�u�W�F�N�g�����܂�Ă��邩�ǂ���

    void Start()
    {
        playerCollider = GetComponent<Collider>();
    }

    void Update()
    {
        // �I�u�W�F�N�g�̃s�b�N�A�b�v/�h���b�v
        if (Input.GetKeyDown(KeyCode.E)) // ���ޑ���
        {
            if (!isHoldingObject)
            {
                // �I�u�W�F�N�g�����ޏ���
                PickupObject();
            }
            else
            {
                // �I�u�W�F�N�g�𗣂�����
                DropObject();
            }
        }

        if (isHoldingObject)
        {
            if (heldObjectRigidbody != null && heldObjectRigidbody.mass > 20f)
            {
                Debug.Log($"���ʂ�20�ȏ�̂��ߗ����܂�: {heldObject.name}");
                DropObject();
            }
            else
            {
                HoldObject();
            }
        }

        // F�ŃI�u�W�F�N�g�𓊂���
        if (Input.GetKeyDown(KeyCode.F) && heldObject != null)
        {
            ThrowObject();
        }
    }

    void PickupObject()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null && rb.mass <= maxPickupMass)
            {
                heldObject = hit.collider.gameObject;
                initialRotation = heldObject.transform.rotation;
                heldObjectRigidbody = rb;

                // �v���C���[�̓����蔻��𖳎�
                heldObjectCollider = heldObject.GetComponent<Collider>();
                Physics.IgnoreCollision(heldObjectCollider, playerCollider, true);
                isHoldingObject = true; // �I�u�W�F�N�g������ł����Ԃɂ���

                Debug.Log($"�I�u�W�F�N�g {heldObject.name} ���E���܂����B����: {rb.mass}");
            }
            else
            {
                Debug.LogWarning("���̃I�u�W�F�N�g�͏d�����Ď����グ���܂���I");
            }
        }
    }

    void DropObject()
    {
        if (heldObject != null)
        {
           // heldObjectRigidbody.useGravity = true;
            //heldObjectRigidbody.isKinematic = false; // �I�u�W�F�N�g���Ăѓ�������悤�ɂ���

            Physics.IgnoreCollision(heldObjectCollider, playerCollider, false);
            heldObject = null; // �I�u�W�F�N�g�𗣂�
            isHoldingObject = false; // �I�u�W�F�N�g������ł��Ȃ���Ԃɖ߂�
        }
    }

    void ThrowObject()
    {
        if (heldObject != null)
        {
            Rigidbody objRigidbody = heldObject.GetComponent<Rigidbody>();
            DropObject();
            //objRigidbody.isKinematic = false; // ������O��isKinematic��false�ɐݒ�
            objRigidbody.AddForce(playerCamera.transform.forward * throwForce);
        }
    }

    void HoldObject()
    {
        // �I�u�W�F�N�g�̉�]�𐧌�
        heldObject.transform.rotation = Quaternion.Euler(
            initialRotation.eulerAngles.x,
            playerCamera.transform.rotation.eulerAngles.y,
            initialRotation.eulerAngles.z
        );
        Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;

        // �ǂƂ̏Փ˂��`�F�b�N���邽�߂̃��C�L���X�g
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, holdDistance + 0.5f, collisionMask))
        {
            // �ǂɂԂ����Ă���ꍇ�A�I�u�W�F�N�g�̈ʒu���C��
            targetPosition = hit.point - hit.normal * 0.1f; // 0.1f�̃I�t�Z�b�g
            targetPosition += hit.normal * holdDistance / 4f; // �ǂ���̋�����ۂ�
        }

        float distanceToTarget = Vector3.Distance(heldObject.transform.position, targetPosition);
        if (distanceToTarget > 0.01f) // �ڕW�ʒu�ɔ��ɋ߂��ꍇ�͕�Ԃ��Ȃ�
        {
            heldObject.transform.position = Vector3.Lerp(heldObject.transform.position, targetPosition, Time.deltaTime * smoothingSpeed);
        }
        else
        {
            heldObject.transform.position = targetPosition; // �ŏI�ʒu�ɒ��ڃZ�b�g
        }
        //heldObjectRigidbody.isKinematic = false;
        heldObjectRigidbody.velocity = Vector3.zero; // �����̉e�����󂯂Ȃ��悤��
        heldObjectRigidbody.angularVelocity = Vector3.zero; // ��]�����Z�b�g
    }

}
