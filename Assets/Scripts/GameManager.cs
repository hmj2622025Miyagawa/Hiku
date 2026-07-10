using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; // ★TextMesh Proを使うために追加

[System.Serializable]
public struct CardData
{
    public int value;     // 1〜13
    public string suit;   // "Spade", "Heart", "Diamond", "Club"
    public Sprite sprite; // 表面の画像
}

public class GameManager : MonoBehaviour
{
    [Header("5枚の手札")]
    [SerializeField] private CardController[] handCards = new CardController[5];

    [Header("トランプ52枚の画像データ")]
    [SerializeField] private List<CardData> allDeckCards = new List<CardData>();

    [Header("UI設定")]
    [SerializeField] private TextMeshProUGUI resultText;  // ★判定結果を表示するUIテキスト
    [SerializeField] private TextMeshProUGUI guideText;   // ★操作案内を表示するUIテキスト

    private List<CardData> currentDeck = new List<CardData>(); // 現在の山札（シャッフル用）
    private bool isFirstTurn = true; // 初回配られた状態（true）か、交換後（false）か

    void Start()
    {
        // UIの初期化
        UpdateGuideText("ゲーム開始");
        if (resultText != null) resultText.text = "";

        // 52枚の山札を作成してシャッフルし、手札に配る
        ResetAndDeal();
    }

    // ゲームをリセットして新しく配り直す
    public void ResetAndDeal()
    {
        isFirstTurn = true;
        currentDeck = new List<CardData>(allDeckCards);

        if (resultText != null) resultText.text = ""; // 結果テキストを空にする
        UpdateGuideText("ホールド選択");

        // 山札をシャッフル（フィッシャー・イェーツのアルゴリズム）
        for (int i = currentDeck.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            CardData tmp = currentDeck[i];
            currentDeck[i] = currentDeck[r];
            currentDeck[r] = tmp;
        }

        // シャッフルした山札の上から5枚を手札のオブジェクトに配る
        for (int i = 0; i < handCards.Length; i++)
        {
            if (handCards[i] != null && currentDeck.Count > 0)
            {
                CardData drawnCard = currentDeck[0];
                currentDeck.RemoveAt(0); // 山札から引いたカードを消す

                // 手札のスクリプトにデータを送る
                handCards[i].SetCardData(drawnCard.value, drawnCard.suit, drawnCard.sprite);

                // 最初はカードを表面にする
                handCards[i].OpenCard();
            }
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (isFirstTurn)
            {
                // 1回目のスペース：ホールドされていないカードを交換して役を判定
                ExchangeCards();
                CheckHandResult();
                isFirstTurn = false;
                UpdateGuideText("ゲーム終了");
            }
            else
            {
                // 2回目のスペース：新しく配り直す
                ResetAndDeal();
            }
        }
    }

    // ホールドされていないカードを新しいカードと入れ替える
    private void ExchangeCards()
    {
        for (int i = 0; i < handCards.Length; i++)
        {
            if (handCards[i] == null) continue;

            // カードがホールド（選択）されていなければ交換する
            if (!handCards[i].IsHold())
            {
                if (currentDeck.Count > 0)
                {
                    CardData drawnCard = currentDeck[0];
                    currentDeck.RemoveAt(0);

                    handCards[i].SetCardData(drawnCard.value, drawnCard.suit, drawnCard.sprite);
                }
            }

            // すべてのカードを表面にする
            handCards[i].OpenCard();
        }
    }

    // ★ 5枚の手札からポーカーの役を判定するロジック
    private void CheckHandResult()
    {
        List<int> values = new List<int>();
        List<string> suits = new List<string>();

        foreach (var card in handCards)
        {
            if (card != null)
            {
                values.Add(card.GetValue());
                suits.Add(card.GetSuit());
            }
        }

        values.Sort();

        bool isFlush = suits.Distinct().Count() == 1;
        bool isStraight = false;

        bool isNormalStraight = true;
        for (int i = 0; i < values.Count - 1; i++)
        {
            if (values[i + 1] != values[i] + 1)
            {
                isNormalStraight = false;
                break;
            }
        }

        bool isRoyalStraight = (values[0] == 1 && values[1] == 10 && values[2] == 11 && values[3] == 12 && values[4] == 13);

        if (isNormalStraight || isRoyalStraight)
        {
            isStraight = true;
        }

        var numberGroups = values.GroupBy(v => v)
                                 .Select(g => g.Count())
                                 .OrderByDescending(c => c)
                                 .ToList();

        string handName = "ノーペア";

        if (isStraight && isFlush)
        {
            if (isRoyalStraight) handName = "ロイヤルストレートフラッシュ";
            else handName = "ストレートフラッシュ";
        }
        else if (numberGroups[0] == 4) handName = "フォーカード";
        else if (numberGroups[0] == 3 && numberGroups[1] == 2) handName = "フルハウス";
        else if (isFlush) handName = "フラッシュ";
        else if (isStraight) handName = "ストレート";
        else if (numberGroups[0] == 3) handName = "スリーカード";
        else if (numberGroups[0] == 2 && numberGroups[1] == 2) handName = "ツーペア";
        else if (numberGroups[0] == 2) handName = "ワンペア";

        // ★【修正】UIテキストに結果を表示する
        if (resultText != null)
        {
            resultText.text = $"役：{handName}";
        }
    }

    // ★【追加】現在の状況に合わせて案内テキストを書き換える関数
    private void UpdateGuideText(string state)
    {
        if (guideText == null) return;

        switch (state)
        {
            case "ホールド選択":
                guideText.text = "残したいカードをタップして [Space] で交換！";
                break;
            case "ゲーム終了":
                guideText.text = "[Space] を押して次のゲームへ";
                break;
            default:
                guideText.text = "";
                break;
        }
    }
}