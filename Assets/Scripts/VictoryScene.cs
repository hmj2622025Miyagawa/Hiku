using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class VictoryManager : MonoBehaviour
{
        void Update()
    {
        // Tキーが押されたらタイトルシーンに戻る
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene("TitleScene");
            return;
        }
    }
}