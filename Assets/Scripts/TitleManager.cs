using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("読み込むゲームシーンの名前")]
    [SerializeField] private string gameSceneName = "MainScene";

    [Header("読み込む操作説明シーンの名前")]
    [SerializeField] private string descriptionSceneName = "DescriptionScene"; // ⭐追記項目

    [Header("オーディオ設定")]
    [SerializeField] private AudioSource bgmSource;      // BGM再生用のAudioSource
    [SerializeField] private AudioClip titleBgm;         // タイトル用のBGM
    [SerializeField] private AudioClip clickSe;          // ボタンクリック時の効果音（SE）

    private bool isStarting = false; // 連打防止フラグ

    void Start()
    {
        if (bgmSource != null && titleBgm != null)
        {
            bgmSource.clip = titleBgm;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    // ボタンが押されたときに呼び出す関数
    public void OnStartButtonClicked()
    {
        // 遷移処理中なら何もしない
        if (isStarting) return;
        isStarting = true;

        // コルーチンを開始して、音を鳴らしてから遷移する
        StartCoroutine(StartGameSequence());
    }

    // ⭐操作説明ボタンが押されたときに呼び出す関数（追記）
    public void OnDescriptionButtonClicked()
    {
        // 遷移処理中なら何もしない
        if (isStarting) return;
        isStarting = true;

        // コルーチンを開始して、音を鳴らしてから操作説明シーンへ遷移する
        StartCoroutine(StartDescriptionSequence());
    }

    private IEnumerator StartGameSequence()
    {
        // 1. クリック効果音を鳴らす
        if (bgmSource != null && clickSe != null)
        {
            // BGMの音量を下げるか、止める
            bgmSource.Stop();

            // 効果音を1回だけ再生
            bgmSource.PlayOneShot(clickSe);
        }

        // 効果音が鳴り終わるまで、または適度な時間待つ
        // clickSe.length秒待つと、効果音の長さにピッタリ合わせられます
        float waitTime = clickSe != null ? clickSe.length : 0.5f;
        yield return new WaitForSeconds(waitTime);

        // シーンを読み込む
        SceneManager.LoadScene(gameSceneName);
    }

    // ⭐操作説明シーンへの遷移用コルーチン（追記）
    private IEnumerator StartDescriptionSequence()
    {
        if (bgmSource != null && clickSe != null)
        {
            bgmSource.Stop();
            bgmSource.PlayOneShot(clickSe);
        }

        float waitTime = clickSe != null ? clickSe.length : 0.5f;
        yield return new WaitForSeconds(waitTime);

        // 操作説明シーンを読み込む
        SceneManager.LoadScene(descriptionSceneName);
    }
}