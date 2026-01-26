using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // UI 버튼에 연결할 함수
    public void OnClickSlot()
    {
        SceneManager.LoadScene(1); // 게임 씬
    }
}
