using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // ★新しいInput Systemを使うために追加

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

    private List<CardData> currentDeck = new List<CardData>(); // 現在の山札（シャッフル用）

    void Start()
    {
        // 52枚の山札を作成してシャッフルし、手札に配る
        ResetAndDeal();
    }

    // ゲームをリセットして新しく配り直す
    public void ResetAndDeal()
    {
        currentDeck = new List<CardData>(allDeckCards);

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
            }
        }

        Debug.Log("5枚の手札を新しく配りました！カードをクリックしてホールド（残すカード）を選べます。");
    }

    void Update()
    {
        // ★古い Input.GetKeyDown から 新しい Input System の書き方に修正
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            foreach (var card in handCards)
            {
                if (card != null) card.OpenCard();
            }
            Debug.Log("手札を一斉にオープンしました！");
        }
    }
}