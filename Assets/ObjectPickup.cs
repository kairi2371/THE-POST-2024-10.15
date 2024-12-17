using UnityEngine;
using UnityEngine.Rendering.PostProcessing; // ポストプロセッシング関連の名前空間
using System.Collections.Generic;

public class ObjectPickup : MonoBehaviour
{
    public Camera playerCamera;
    public float pickupRange = 3f;
    public float holdDistance = 10f;
    public float throwForce = 300f;
    public LayerMask collisionMask;
    public float smoothingSpeed = 5f; // 補間速度
    public float maxPickupMass = 20f; // 持ち上げられる最大質量

    private GameObject heldObject = null;
    private Quaternion initialRotation;
    private Collider heldObjectCollider;
    private Rigidbody heldObjectRigidbody;
    private Collider playerCollider;
    private bool isHoldingObject = false;        // オブジェクトがつかまれているかどうか

    void Start()
    {
        playerCollider = GetComponent<Collider>();
    }

    void Update()
    {
        // オブジェクトのピックアップ/ドロップ
        if (Input.GetKeyDown(KeyCode.E)) // つかむ操作
        {
            if (!isHoldingObject)
            {
                // オブジェクトをつかむ処理
                PickupObject();
            }
            else
            {
                // オブジェクトを離す処理
                DropObject();
            }
        }

        if (isHoldingObject)
        {
            if (heldObjectRigidbody != null && heldObjectRigidbody.mass > 20f)
            {
                Debug.Log($"質量が20以上のため離します: {heldObject.name}");
                DropObject();
            }
            else
            {
                HoldObject();
            }
        }

        // Fでオブジェクトを投げる
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

                // プレイヤーの当たり判定を無視
                heldObjectCollider = heldObject.GetComponent<Collider>();
                Physics.IgnoreCollision(heldObjectCollider, playerCollider, true);
                isHoldingObject = true; // オブジェクトをつかんでいる状態にする

                Debug.Log($"オブジェクト {heldObject.name} を拾いました。質量: {rb.mass}");
            }
            else
            {
                Debug.LogWarning("このオブジェクトは重すぎて持ち上げられません！");
            }
        }
    }

    void DropObject()
    {
        if (heldObject != null)
        {
           // heldObjectRigidbody.useGravity = true;
            //heldObjectRigidbody.isKinematic = false; // オブジェクトを再び動かせるようにする

            Physics.IgnoreCollision(heldObjectCollider, playerCollider, false);
            heldObject = null; // オブジェクトを離す
            isHoldingObject = false; // オブジェクトをつかんでいない状態に戻す
        }
    }

    void ThrowObject()
    {
        if (heldObject != null)
        {
            Rigidbody objRigidbody = heldObject.GetComponent<Rigidbody>();
            DropObject();
            //objRigidbody.isKinematic = false; // 投げる前にisKinematicをfalseに設定
            objRigidbody.AddForce(playerCamera.transform.forward * throwForce);
        }
    }

    void HoldObject()
    {
        // オブジェクトの回転を制御
        heldObject.transform.rotation = Quaternion.Euler(
            initialRotation.eulerAngles.x,
            playerCamera.transform.rotation.eulerAngles.y,
            initialRotation.eulerAngles.z
        );
        Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;

        // 壁との衝突をチェックするためのレイキャスト
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, holdDistance + 0.5f, collisionMask))
        {
            // 壁にぶつかっている場合、オブジェクトの位置を修正
            targetPosition = hit.point - hit.normal * 0.1f; // 0.1fのオフセット
            targetPosition += hit.normal * holdDistance / 4f; // 壁からの距離を保つ
        }

        float distanceToTarget = Vector3.Distance(heldObject.transform.position, targetPosition);
        if (distanceToTarget > 0.01f) // 目標位置に非常に近い場合は補間しない
        {
            heldObject.transform.position = Vector3.Lerp(heldObject.transform.position, targetPosition, Time.deltaTime * smoothingSpeed);
        }
        else
        {
            heldObject.transform.position = targetPosition; // 最終位置に直接セット
        }
        //heldObjectRigidbody.isKinematic = false;
        heldObjectRigidbody.velocity = Vector3.zero; // 物理の影響を受けないように
        heldObjectRigidbody.angularVelocity = Vector3.zero; // 回転もリセット
    }

}
