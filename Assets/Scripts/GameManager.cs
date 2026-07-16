using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; // UIのボタンを制御するために必要

[System.Serializable]
public struct CardData
{
    public int value;     // 1〜13
    public string suit;   // "Spade", "Heart", "Diamond", "Club"
    public Sprite sprite; // 表面の画像
}

public class GameManager : MonoBehaviour
{
    private enum GameState
    {
        BetTime,    // ベット額を決める状態
        PlayerTurn, // プレイヤーがホールドを選んでスペースを押すまで
        ComTurn,    // COMが自動でホールドを選んで交換する番
        Result,     // 両者の役を比較して勝敗を決める状態
        GameOver    // どちらかが破産した状態
    }

    [Header("プレイヤーの手札 (5枚)")]
    [SerializeField] private CardController[] playerHand = new CardController[5];

    [Header("COMの手札 (5枚)")]
    [SerializeField] private CardController[] comHand = new CardController[5];

    [Header("トランプ52枚の画像データ")]
    [SerializeField] private List<CardData> allDeckCards = new List<CardData>();

    [Header("UI設定")]
    [SerializeField] private TextMeshProUGUI resultText;  // 勝敗や役を表示
    [SerializeField] private TextMeshProUGUI guideText;   // 操作案内を表示
    [SerializeField] private TextMeshProUGUI coinText;    // 所持コインとベット額を表示
    [SerializeField] private Button retryButton;          // リトライボタンの登録枠

    [Header("コインシステム設定")]
    [SerializeField] private int initialCoins = 100;     // 初期コイン
    [SerializeField] private int defaultBetAmount = 10;   // 1回の基本ベット額

    [Header("オーディオ設定")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip gameBgm;
    [SerializeField] private AudioClip resultBgm;

    private List<CardData> currentDeck = new List<CardData>();
    private GameState currentState;

    private int currentCoins;
    private int comCoins;
    private int currentBet;

    private void PlayBGM(AudioClip clip, bool loop)
    {
        if (bgmSource == null || clip == null) return;

        // 現在再生中のBGMと同じなら、かけ直さない
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    void Start()
    {
        // 最初はリトライボタンを隠しておく
        if (retryButton != null) retryButton.gameObject.SetActive(false);

        // ゲームの初期化
        InitGameSettings();
    }

    // コインも含めて完全に初期化する関数
    private void InitGameSettings()
    {
        currentCoins = initialCoins;
        comCoins = initialCoins;
        UpdateCoinUI();
        PrepareNewGame();
    }

    private void PrepareNewGame()
    {
        currentState = GameState.BetTime;
        currentBet = defaultBetAmount;

        if (resultText != null) resultText.text = "";
        UpdateGuideText("ベットタイム");
        UpdateCoinUI();

        foreach (var card in playerHand) if (card != null) card.Showback();
        foreach (var card in comHand) if (card != null) card.Showback();

        // 通常BGMループを再生する
        PlayBGM(gameBgm, true);
    }

    public void ResetAndDeal()
    {
        // お互いの財布からベット額を引いて「場」に出す
        currentCoins -= currentBet;
        comCoins -= currentBet; // COMからもベット額を引く

        UpdateCoinUI();

        currentState = GameState.PlayerTurn;
        currentDeck = new List<CardData>(allDeckCards);

        UpdateGuideText("プレイヤーホールド");

        // （以下、シャッフルと配る処理はそのまま）
        for (int i = currentDeck.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            CardData tmp = currentDeck[i];
            currentDeck[i] = currentDeck[r];
            currentDeck[r] = tmp;
        }

        for (int i = 0; i < 5; i++)
        {
            if (currentDeck.Count > 0)
            {
                CardData pCard = currentDeck[0];
                currentDeck.RemoveAt(0);
                playerHand[i].SetCardData(pCard.value, pCard.suit, pCard.sprite);
                playerHand[i].OpenCard();
            }
        }

        for (int i = 0; i < 5; i++)
        {
            if (currentDeck.Count > 0)
            {
                CardData cCard = currentDeck[0];
                currentDeck.RemoveAt(0);
                comHand[i].SetCardData(cCard.value, cCard.suit, cCard.sprite);
                comHand[i].Showback();
            }
        }
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // ゲームオーバー状態なら入力を受け付けない
        if (currentState == GameState.GameOver) return;

        if (currentState == GameState.BetTime)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                if (currentBet + 10 <= currentCoins && currentBet + 10 <= comCoins)
                {
                    currentBet += 10;
                }
                UpdateCoinUI();
            }
            if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                if (currentBet - 10 >= 10) currentBet -= 10;
                UpdateCoinUI();
            }
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            switch (currentState)
            {
                case GameState.BetTime:
                    if (currentCoins >= currentBet && comCoins >= currentBet)
                    {
                        ResetAndDeal();
                    }
                    else
                    {
                        if (resultText != null) resultText.text = "<color=red>コインが足りません！</color>";
                    }
                    break;

                case GameState.PlayerTurn:
                    ExchangeCards(playerHand);
                    currentState = GameState.ComTurn;
                    UpdateGuideText("COM思考中");
                    ProcessComTurn();
                    break;

                case GameState.Result:
                    if (currentCoins >= 10 && comCoins >= 10)
                    {
                        PrepareNewGame();
                    }
                    break;
            }
        }
    }

    private void ExchangeCards(CardController[] hand)
    {
        for (int i = 0; i < hand.Length; i++)
        {
            if (hand[i] == null) continue;

            if (currentState == GameState.PlayerTurn)
            {
                if (!hand[i].IsHold())
                {
                    if (currentDeck.Count > 0)
                    {
                        CardData drawnCard = currentDeck[0];
                        currentDeck.RemoveAt(0);
                        hand[i].SetCardData(drawnCard.value, drawnCard.suit, drawnCard.sprite);
                    }
                }
                hand[i].OpenCard();
            }
            else if (currentState == GameState.ComTurn)
            {
                if (ShouldComExchangeCard(hand, i))
                {
                    if (currentDeck.Count > 0)
                    {
                        CardData drawnCard = currentDeck[0];
                        currentDeck.RemoveAt(0);
                        hand[i].SetCardData(drawnCard.value, drawnCard.suit, drawnCard.sprite);
                    }
                }
                hand[i].Showback();
            }
        }
    }

    private bool ShouldComExchangeCard(CardController[] hand, int index)
    {
        List<int> allValues = new List<int>();
        foreach (var card in hand)
        {
            if (card != null) allValues.Add(card.GetValue());
        }

        int targetValue = hand[index].GetValue();
        int count = allValues.Count(v => v == targetValue);

        if (count >= 2) return false;

        List<string> allSuits = hand.Where(c => c != null).Select(c => c.GetSuit()).ToList();
        string targetSuit = hand[index].GetSuit();
        int suitCount = allSuits.Count(s => s == targetSuit);
        if (suitCount == 4) return true;

        return true;
    }

    private void ProcessComTurn()
    {
        ExchangeCards(comHand);

        for (int i = 0; i < comHand.Length; i++)
        {
            if (comHand[i] != null) comHand[i].OpenCard();
        }

        currentState = GameState.Result;
        UpdateGuideText("結果発表");
        DetermineWinner();
    }

    private void DetermineWinner()
    {
        string playerHandName = GetHandName(playerHand, out int playerRank);
        string comHandName = GetHandName(comHand, out int comRank);

        string resultMessage = $"あなた: {playerHandName}\nCOM: {comHandName}\n\n";

        if (playerRank > comRank)
        {
            // プレイヤーの勝利：ベット額 × 役の倍率 が払い戻される
            int odds = GetPayoutOdds(playerRank);
            int payout = currentBet * odds;

            // COMの残りコイン以上の配当は支払えない
            int maxPayout = currentBet + comCoins;
            if (payout > maxPayout) payout = maxPayout;

            currentCoins += payout;

            // COM側の財布の辻褄を合わせる
            // （COMはすでにベットで currentBet 失っているので、追加で失うのは payout - currentBet 分）
            int comLoss = payout - currentBet;
            comCoins -= comLoss;

            resultMessage += $"<color=green>あなたの勝ち！</color>\n配当: {payout} コイン (オッズ: {odds}倍)";
        }
        else if (playerRank < comRank)
        {
            // COMの勝利：COMに配当を支払う（ここではシンプルに2倍＝相手の賭け金を奪う形）
            int comOdds = GetPayoutOdds(comRank);
            int payout = currentBet * comOdds;

            int maxPayout = currentBet + currentCoins;
            if (payout > maxPayout) payout = maxPayout;

            comCoins += payout;

            int playerLoss = payout - currentBet;
            currentCoins -= playerLoss;

            resultMessage += $"<color=red>COMの勝ち！</color>\nCOMに {payout} コイン持っていかれました...";
        }
        else
        {
            // 引き分け：賭け金をそのままお互いに返却
            currentCoins += currentBet;
            comCoins += currentBet;
            resultMessage += "引き分け！ (ベット額が返却されました)";
        }

        if (resultText != null) resultText.text = resultMessage;
        UpdateCoinUI();

        // 破産時のゲームオーバー判定 (最低ベット額10枚未満で終了)
        if (currentCoins < 10)
        {
            currentState = GameState.GameOver;
            if (resultText != null) resultText.text += "\n<color=red>ゲームオーバー！(あなたの破産)</color>";
            ShowRetryButton();
        }
        else if (comCoins < 10)
        {
            currentState = GameState.GameOver;
            if (resultText != null) resultText.text += "\n<color=yellow>完全勝利！(COMを破産させました！)</color>";
            ShowRetryButton();
        }

        // 結果発表BGMを再生
        PlayBGM(resultBgm, false);
    }

    // ゲームオーバー時にリトライボタンを表示する処理
    private void ShowRetryButton()
    {
        if (guideText != null) guideText.text = "コインがなくなりました。";
        if (retryButton != null) retryButton.gameObject.SetActive(true);

        // 破産したらBGMを止める
        if (bgmSource != null) bgmSource.Stop();
    }

    // リトライボタンが押された時にUIから呼び出す関数
    public void OnRetryButtonClicked()
    {
        // ボタンを再び隠す
        if (retryButton != null) retryButton.gameObject.SetActive(false);

        // お互いのコインをリセットして再スタート
        InitGameSettings();
    }

    private int GetPayoutOdds(int rank)
    {
        switch (rank)
        {
            case 9: return 20;
            case 8: return 10;
            case 7: return 7;
            case 6: return 5;
            case 5: return 4;
            case 4: return 3;
            case 3: return 2;
            case 2: return 2;
            default: return 1;
        }
    }

    private void UpdateCoinUI()
    {
        if (coinText == null) return;

        coinText.text = $"【所持コイン】\n" +
            $"あなた: {currentCoins}枚\n" + $"ＣＯＭ: {comCoins}枚\n\n" + $"現在のベット: {currentBet}枚";
    }
    private string GetHandName(CardController[] hand, out int rank) 
    {
        List<int> values = new List<int>();
        List<string> suits = new List<string>();
        foreach (var card in hand) 
        {
            if (card != null)
            {
                values.Add(card.GetValue());
                suits.Add(card.GetSuit()); 
            }
        }
        values.Sort(); 
        bool isNormalStraight = true;
        for (int i = 0; i < values.Count - 1; i++) 
        {
            if (values[i + 1] != values[i] + 1) 
            {
                isNormalStraight = false; break; 
            } 
        }
        bool isAceHighStraight = (values.Count == 5 && values[0] == 1 && values[1] == 10 && values[2] == 11 && values[3] == 12 && values[4] == 13);
        bool isStraight = isNormalStraight || isAceHighStraight; 
        bool isFlush = suits.Distinct().Count() == 1;
        var numberGroups = values.GroupBy(v => v).Select(g => g.Count()).OrderByDescending(c => c).ToList();
        if (isStraight && isFlush) 
        { 
            if (isAceHighStraight) 
            { 
                rank = 9;
                return "ロイヤルストレートフラッシュ"; 
            }
            rank = 8;
            return "ストレートフラッシュ";
        }
        if (numberGroups[0] == 4) 
        {
            rank = 7;
            return "フオーカード"; 
        }
        if (numberGroups[0] == 3 && numberGroups[1] == 2)
        { 
            rank = 6;
            return "フルハウス"; 
        } 
        if (isFlush) 
        { 
            rank = 5;
            return "フラッシュ"; 
        }
        if (isStraight)
        { 
            rank = 4;
            return "ストレート";
        }
        if (numberGroups[0] == 3)
        {
            rank = 3; return "スリーカード"; 
        }
        if (numberGroups[0] == 2 && numberGroups[1] == 2) 
        {
            rank = 2; 
            return "ツーペア";
        }
        if (numberGroups[0] == 2) 
        {
            rank = 1;
            return "ワンペア"; 
        }
        rank = 0;
        return "ノーペア";
    }
    private void UpdateGuideText(string state)
    {
        if (guideText == null) return; 
        switch (state)
        {
            case "ベットタイム": guideText.text = "【↑ / ↓】でベット額変更\n【Space】で勝負開始！";
                break;
            case "プレイヤーホールド": guideText.text = "残したいカードをタップして\n【Space】で交換！";
                break;
            case "COM思考中": guideText.text = "COMがカードを選んでいます..."; 
                break;
            case "結果発表": guideText.text = "【Space】を押して次の勝負へ！"; 
                break;
        }
    }


}