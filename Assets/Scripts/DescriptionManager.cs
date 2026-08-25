using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DescriptionManager : MonoBehaviour
{
    [Header("戻る先のタイトルシーンの名前")]
    [SerializeField] private string titleSceneName = "TitleScene";

    [Header("オーディオ設定")]
    [SerializeField] private AudioSource seSource;       // 効果音再生用のAudioSource
    [SerializeField] private AudioClip clickSe;          // ボタンクリック時の効果音（SE）

    private bool isReturning = false; // 連打防止フラグ

    /// <summary>
    /// 戻るボタンが押されたときにUI（Button）から呼び出す関数
    /// </summary>
    public void OnBackButtonClicked()
    {
        // すでに遷移中なら何もしない
        if (isReturning) return;
        isReturning = true;

        // コルーチンを開始して、音を鳴らしてから遷移する
        StartCoroutine(BackToTitleSequence());
    }

    private IEnumerator BackToTitleSequence()
    {
        // 1. クリック効果音を鳴らす
        if (seSource != null && clickSe != null)
        {
            seSource.PlayOneShot(clickSe);
        }

        // 効果音が鳴り終わるまで、または適度な時間待つ
        float waitTime = clickSe != null ? clickSe.length : 0.5f;
        yield return new WaitForSeconds(waitTime);

        // タイトルシーンを読み込む
        SceneManager.LoadScene(titleSceneName);
    }
}