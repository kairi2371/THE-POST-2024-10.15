using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PhotoCapture : MonoBehaviour
{
    public Camera captureCamera; // �B�e�p�̃J����
    public RenderTexture renderTexture; // �ʐ^��ۑ�����RenderTexture
    public Material photoMaterial; // �ʐ^�Ɏg�p����}�e���A��
    public string sceneToCapture; // �B�e�������V�[���̖��O

    public void CapturePhoto()
    {
        // �V�[�����A�N�e�B�u�Ń��[�h
        StartCoroutine(LoadSceneAndCapture(sceneToCapture));
    }

    private IEnumerator LoadSceneAndCapture(string sceneName)
    {
        // �V�[�����A�N�e�B�u�Ń��[�h
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        loadOp.allowSceneActivation = false; // ��A�N�e�B�u��Ԃɂ���
        yield return loadOp;

        // �J������RenderTexture�ɐݒ�
        if (captureCamera != null && renderTexture != null)
        {
            captureCamera.targetTexture = renderTexture;
            captureCamera.Render(); // �J�������蓮�ŕ`��
            captureCamera.targetTexture = null;
        }

        // �ʐ^���}�e���A���ɓK�p
        if (photoMaterial != null)
        {
            photoMaterial.mainTexture = renderTexture;
        }

        // �V�[�����A�����[�h
        SceneManager.UnloadSceneAsync(sceneName);
    }
}
