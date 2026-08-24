using System.Collections;    
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [Header("読み込むゲームシーンの名前")]
    [SerializeField] private string gameSceneName = "MainScene";

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
}