using UnityEngine;

public class EnemyLookAtPlayer : MonoBehaviour
{
    public Transform player; // �v���C���[��Transform���C���X�y�N�^�Őݒ�
    public float rotationSpeed = 5f; // ��]�̑���
    private LineRenderer lineRenderer; // LineRenderer�̎Q��

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>(); // LineRenderer�R���|�[�l���g���擾
        lineRenderer.positionCount = 2; // ���C���̓_�̐���2�ɐݒ�
    }

    void Update()
    {
        if (player != null) // �v���C���[���ݒ肳��Ă���ꍇ
        {
            // �v���C���[�̈ʒu�����āA��]����
            Vector3 direction = player.position - transform.position; // �v���C���[�ւ̃x�N�g��
            direction.y = 0; // Y���̉�]�𖳎����Đ��������݂̂ɐ���

            // ��]��Quaternion���v�Z
            Quaternion rotation = Quaternion.LookRotation(direction);

            // ���݂̉�]����ڕW�̉�]�փX���[�Y�ɕ��
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);

            // ���C���̈ʒu���X�V
            UpdateLineRenderer();
        }
    }

    private void UpdateLineRenderer()
    {
        // �G�̌����Ă���������擾
        Vector3 direction = transform.forward; // �G�̌���
        Vector3 startPoint = transform.position + Vector3.up * 1.5f; // ���C���̎n�_�i�G�̈ʒu�j
        Vector3 endPoint = startPoint + direction * 5f; // ���C���̏I�_�i�����Ă��������5�̒����j

        // ���C���̈ʒu��ݒ�
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
    }
}
