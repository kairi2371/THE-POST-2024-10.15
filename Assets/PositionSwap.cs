using UnityEngine;

public class PositionSwap : MonoBehaviour
{
    public GameObject player; // �v���C���[�̃I�u�W�F�N�g
    public Camera playerCamera; // �v���C���[�̃J����
    public float rayDistance = 20f; // Ray�̒���
    private GameObject targetEnemy; // ����ւ���Ώۂ̓G
    public float cameraMoveSpeed = 15f; // �J�����̈ړ����x
    private bool isSwapping = false; // ����ւ��������̃t���O
    private Vector3 playerOriginalPosition; // ����ւ��O�̃v���C���[�̈ʒu
    public float maxFOV = 100f; // �ő�FOV
    public float fovChangeSpeed = 10f; // FOV�̕ω����x
    private MonoBehaviour playerMovement; // �v���C���[�ړ��X�N���v�g�̎Q��

    private void Start()
    {
        // �v���C���[�̈ړ��X�N���v�g���擾�i�K�؂ȃX�N���v�g���w�肵�Ă��������j
        playerMovement = player.GetComponent<FirstPersonMovement>(); // ��: FirstPersonMovement
    }

    private void Update()
    {
        // ����ւ��̃L�[�������ꂽ�Ƃ��ɓ���ւ����J�n
        if (Input.GetMouseButton(0) && !isSwapping)//�}�E�X�̍��{�^��
        {
            // Raycast���v���C���[�̃J�������甭��
            RaycastHit hit;
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, rayDistance))
            {
                if (hit.collider.CompareTag("Enemy")) // �G�̃^�O���r
                {
                    targetEnemy = hit.collider.gameObject; // ����ւ���G��ݒ�
                    playerOriginalPosition = player.transform.position; // ����ւ��O�̃v���C���[�̈ʒu��ۑ�
                    StartCoroutine(SwapPositions());
                }
            }
        }
    }

    private System.Collections.IEnumerator SwapPositions()
    {
        isSwapping = true; // ����ւ��������ɐݒ�

        // �v���C���[�̈ړ��X�N���v�g�𖳌���
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // FOV��90�ɏ��X�ɕύX
        float originalFOV = playerCamera.fieldOfView; // ����FOV
        float elapsedTime = 0f;
        while (elapsedTime < 1f) // 1�b��FOV��ύX
        {
            playerCamera.fieldOfView = Mathf.Lerp(originalFOV, maxFOV, elapsedTime); // FOV����`���
            elapsedTime += Time.deltaTime * fovChangeSpeed;
            yield return null; // �t���[�����ɑҋ@
        }
        playerCamera.fieldOfView = maxFOV; // �ŏI�I��FOV��ݒ�

        // �J������G�̈ʒu�ɃX���[�Y�Ɉړ�
        Vector3 targetPosition = targetEnemy.transform.position + Vector3.up * 1.8f; // Y���W��1.488�グ��
        elapsedTime = 0f; // �o�ߎ��ԃ��Z�b�g

        while (Vector3.Distance(playerCamera.transform.position, targetPosition) > 0.01f) // �ړI�n�ɓ��B����܂ňړ�
        {
            playerCamera.transform.position = Vector3.MoveTowards(playerCamera.transform.position, targetPosition, cameraMoveSpeed * Time.deltaTime); // �ړI�n�Ɍ����Ĉړ�
            yield return null; // �t���[�����ɑҋ@
        }

        // �v���C���[�ƓG�̈ʒu�����ւ�
        Vector3 tempPosition = player.transform.position;
        Quaternion tempRotation = player.transform.rotation; // �v���C���[�̉�]��ۑ�

        player.transform.position = targetEnemy.transform.position;
        player.transform.rotation = targetEnemy.transform.rotation; // �G�̉�]���v���C���[�ɐݒ�
        targetEnemy.transform.position = tempPosition;
        targetEnemy.transform.rotation = tempRotation; // �v���C���[�̉�]��G�ɐݒ�

        // �J�������v���C���[�̌��̈ʒu�ɖ߂��A�������v���C���[���؂�ւ��O�ɂ����ꏊ�ɐݒ�
        playerCamera.transform.position = player.transform.position + Vector3.up * 1.488f; // �v���C���[�̓��̈ʒu�ɐݒ�
        Vector3 directionToOriginal = playerOriginalPosition - player.transform.position; // �؂�ւ��O�̈ʒu�ւ̕���
        playerCamera.transform.rotation = Quaternion.LookRotation(directionToOriginal); // �J������؂�ւ��O�̈ʒu�Ɍ�����

        // FOV�����̒l�ɖ߂�
        elapsedTime = 0f;
        while (elapsedTime < 1f) // 1�b��FOV��߂�
        {
            playerCamera.fieldOfView = Mathf.Lerp(maxFOV, originalFOV, elapsedTime); // FOV����`���
            elapsedTime += Time.deltaTime * fovChangeSpeed;
            yield return null; // �t���[�����ɑҋ@
        }
        playerCamera.fieldOfView = originalFOV; // �ŏI�I��FOV��ݒ�

        isSwapping = false; // ����ւ������I��

        // �v���C���[�̈ړ��X�N���v�g���ēx�L����
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }
    }
}
