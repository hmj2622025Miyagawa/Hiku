using UnityEngine;
using UnityEngine.InputSystem;

public class CardController : MonoBehaviour
{
    [Header("Card Components")]
    [SerializeField] private SpriteRenderer cardRenderer;

    [Header("Card Settings")]
    [SerializeField] private Sprite r_backSprite;

    private Sprite frontSprite;
    private int cardValue;
    private string cardSuit;

    private bool isFront = false;
    private bool isHold = false;
    private Collider2D myCollider;

    private Vector3 originalPosition;
    private bool isPositionSet = false;
    private float holdOffset = 0.5f;


    void Awake()
    {
        originalPosition = transform.position;
        isPositionSet = true;

        Application.targetFrameRate = 60;

        // コンポーネントの自動取得
        if (cardRenderer == null) cardRenderer = GetComponentInChildren<SpriteRenderer>();
        myCollider = GetComponent<Collider2D>();
        originalPosition = transform.position;

        // 初期状態は裏面
        Showback();
    }
    //void Start()
    //{
    //}

    // GameManagerから呼び出せるように「Data」に名前を修正
    public void SetCardData(int value, string suit, Sprite frontImage)
    {
        this.cardValue = value;
        this.cardSuit = suit;
        this.frontSprite = frontImage;

        isFront = false;
        isHold = false;

        if (isPositionSet)
        {
            transform.position = originalPosition;
        }

        Showback();
    }

    void Update()
    {
        // マウスの左クリック（またはスマホのタップ）が「押された瞬間」だけを確実に検知
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            // 画面上のマウスのクリック位置を取得
            Vector2 mousePos = Pointer.current.position.ReadValue();

            // マウス位置をゲーム内の3D/2D空間の座標（ワールド座標）に変換
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Mathf.Abs(Camera.main.transform.position.z)));
            Vector2 clickPos2D = new Vector2(worldPos.x, worldPos.y);

            // 自分のカードのコライダー（当たり判定）の中に、クリックした座標が含まれているか直接チェック
            if (myCollider != null && myCollider.OverlapPoint(clickPos2D))
            {
                ToggleHold();
            }
        }
    }

    private void ToggleHold()
    {
        if (cardRenderer == null) return;

        isHold = !isHold;

        if (isHold)
        {
            // カードを持っている状態であれば、カードを少し上に移動させる
            transform.position = originalPosition + new Vector3(0, holdOffset, 0);
        }
        else
        {
            // カードを持っていない状態であれば、元の位置に戻す
            transform.position = originalPosition;
        }

        Debug.Log($"{gameObject.name} (数字:{cardValue} マーク:{cardSuit}) ホールド: {isHold}");
    }

    public void OpenCard()
    {
        isFront = true;
        if (cardRenderer != null) cardRenderer.sprite = frontSprite;
    }

    public void Showback()
    {
        isFront = false;
        if (cardRenderer != null) cardRenderer.sprite = r_backSprite;
    }

    public void CloseCard()
    {
        gameObject.SetActive(false);
    }

    // 情報取得用の関数
    public int GetValue() => cardValue;
    public string GetSuit() => cardSuit;
    public bool IsFront() => isFront;
    public bool IsHold() => isHold;
}