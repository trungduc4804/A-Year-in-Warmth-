using System;
using UnityEngine;

/// <summary>
/// Điểm Tương Tác Ngoài Thế Giới: Bếp Củi & Nồi Luộc Bánh Chưng Đêm 30 Tết (In-World Boiling Pot).
/// - Đặt tại góc sân nhà Bác An bên cạnh đống củi khô.
/// - Nhận diện khi Arthur đến gần trong bán kính 2.5 mét.
/// - Hiện bong bóng gợi ý:
///     + Nếu đã gói bánh: "🔥 [E] Canh Nồi Bánh Đêm 30"
///     + Nếu chưa gói bánh: "🍱 Hãy cùng Bác An gói bánh chưng trước nhé!"
/// - Bấm [E] để mở Minigame Pha 2: Canh Nồi Bánh Chưng Đêm 30 Tết.
/// </summary>
[DisallowMultipleComponent]
public class BanhChungBoilingPot : MonoBehaviour
{
    #region Serialized Fields
    [Header("=== CÀI ĐẶT TƯƠNG TÁC ===")]
    [Tooltip("Phím tương tác khi đứng cạnh nồi luộc bánh (mặc định: phím E).")]
    public KeyCode interactionKey = KeyCode.E;

    [Tooltip("Bán kính nhận diện người chơi (units).")]
    [Range(1.2f, 4.0f)]
    public float interactionRadius = 2.4f;

    [Tooltip("Độ cao của bong bóng gợi ý [E] phía trên nồi.")]
    public float promptOffsetY = 1.2f;
    #endregion

    #region Private State
    private PlayerController player;
    private bool isPlayerInRange = false;

    // GUI Visuals
    private Texture2D promptBgTex;
    private GUIStyle promptTextStyle;
    private bool stylesInitialized = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        promptBgTex = new Texture2D(1, 1);
        promptBgTex.SetPixel(0, 0, new Color(0.12f, 0.14f, 0.18f, 0.94f));
        promptBgTex.Apply();
    }

    private void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player == null) return;
        }

        float dist = Vector2.Distance(transform.position, player.transform.position);
        isPlayerInRange = dist <= interactionRadius;

        bool isDialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
        ViewfinderController viewfinder = FindAnyObjectByType<ViewfinderController>();
        bool isViewfinderActive = viewfinder != null && viewfinder.IsViewfinderActive;
        AlbumUIController album = AlbumUIController.Instance;
        bool isAlbumActive = album != null && album.IsOpen;

        BanhChungMinigame wrapMinigame = BanhChungMinigame.Instance;
        bool isWrapOpen = wrapMinigame != null && wrapMinigame.IsOpen;

        BanhChungBoilingMinigame boilMinigame = BanhChungBoilingMinigame.Instance;
        bool isBoilOpen = boilMinigame != null && boilMinigame.IsOpen;

        if (isPlayerInRange && !isDialogueActive && !isViewfinderActive && !isAlbumActive && !isWrapOpen && !isBoilOpen)
        {
            if (Input.GetKeyDown(interactionKey))
            {
                // Kiểm tra xem đã gói bánh chưng ở Pha 1 chưa
                bool hasWrapped = (GameManager.Instance != null && GameManager.Instance.BanhChungCount > 0) ||
                                  (wrapMinigame != null && wrapMinigame.HasCompletedAny);

                if (hasWrapped)
                {
                    if (boilMinigame != null)
                    {
                        boilMinigame.OpenMinigame();
                    }
                    else
                    {
                        Debug.LogWarning("[BanhChungBoilingPot] Chưa tìm thấy BanhChungBoilingMinigame trong Scene!");
                    }
                }
                else
                {
                    // Nhắc nhở người chơi gói bánh trước
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ShowToast("🍱 Cháu cần chuẩn bị và gói bánh chưng trước đã nhé!");
                    }
                }
            }
        }
    }
    #endregion

    #region GUI Drawing - Floating Bubble [E] Canh Nồi Bánh Đêm 30
    private void OnGUI()
    {
        if (!isPlayerInRange) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (BanhChungMinigame.Instance != null && BanhChungMinigame.Instance.IsOpen) return;
        if (BanhChungBoilingMinigame.Instance != null && BanhChungBoilingMinigame.Instance.IsOpen) return;
        if (AlbumUIController.Instance != null && AlbumUIController.Instance.IsOpen) return;

        ViewfinderController viewfinder = FindAnyObjectByType<ViewfinderController>();
        if (viewfinder != null && viewfinder.IsViewfinderActive) return;

        InitStyles();

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPromptPos = transform.position + new Vector3(0f, promptOffsetY, 0f);
        Vector3 screenPos = cam.WorldToScreenPoint(worldPromptPos);

        // Nằm ngoài tầm camera
        if (screenPos.z < 0f) return;

        float guiX = screenPos.x;
        float guiY = Screen.height - screenPos.y;

        bool hasWrapped = (GameManager.Instance != null && GameManager.Instance.BanhChungCount > 0) ||
                          (BanhChungMinigame.Instance != null && BanhChungMinigame.Instance.HasCompletedAny);

        string promptText = hasWrapped
            ? $"🔥 [{interactionKey}] Canh Nồi Bánh Đêm 30"
            : "🍱 Gói bánh chưng trước nhé";

        float bubbleW = hasWrapped ? 210f : 185f;
        float bubbleH = 34f;
        Rect bubbleRect = new Rect(guiX - bubbleW * 0.5f, guiY - bubbleH * 0.5f, bubbleW, bubbleH);

        Color oldColor = GUI.color;

        // Bóng đổ
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(bubbleRect.x + 2, bubbleRect.y + 2, bubbleRect.width, bubbleRect.height), promptBgTex);

        // Nền tối
        GUI.color = new Color(0.13f, 0.16f, 0.20f, 0.95f);
        GUI.DrawTexture(bubbleRect, promptBgTex);

        // Viền màu ấm nồng
        Color borderColor = hasWrapped ? new Color(0.95f, 0.45f, 0.18f, 1f) : new Color(0.6f, 0.6f, 0.6f, 0.8f);
        GUI.color = borderColor;
        GUI.DrawTexture(new Rect(bubbleRect.x, bubbleRect.y, bubbleRect.width, 1.5f), promptBgTex);
        GUI.DrawTexture(new Rect(bubbleRect.x, bubbleRect.yMax - 1.5f, bubbleRect.width, 1.5f), promptBgTex);
        GUI.DrawTexture(new Rect(bubbleRect.x, bubbleRect.y, 1.5f, bubbleRect.height), promptBgTex);
        GUI.DrawTexture(new Rect(bubbleRect.xMax - 1.5f, bubbleRect.y, 1.5f, bubbleRect.height), promptBgTex);

        // Chữ gợi ý
        GUI.color = Color.white;
        GUI.Label(bubbleRect, promptText, promptTextStyle);

        GUI.color = oldColor;
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        promptTextStyle = new GUIStyle
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        stylesInitialized = true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.95f, 0.45f, 0.18f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
    #endregion
}
