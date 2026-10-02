using UnityEngine;

/// <summary>
/// Chiếu Gói Bánh Chưng Ngày Tết (In-World Interactive Bánh Chưng Table/Mat).
/// - Đặt cạnh Bác An bên hiên nhà.
/// - Nhận diện khi người chơi đến gần trong bán kính 2 mét.
/// - Hiện bong bóng nổi [E] Gói Bánh Chưng.
/// - Bấm [E] để mở Minigame kéo thả gói bánh chưng truyền thống.
/// </summary>
[DisallowMultipleComponent]
public class BanhChungTable : MonoBehaviour
{
    #region Serialized Fields
    [Header("=== CÀI ĐẶT TƯƠNG TÁC ===")]
    [Tooltip("Phím tương tác khi đứng gần bàn gói bánh (mặc định: phím E).")]
    public KeyCode interactionKey = KeyCode.E;

    [Tooltip("Bán kính nhận diện người chơi (units).")]
    [Range(1.2f, 4.0f)]
    public float interactionRadius = 2.2f;

    [Tooltip("Độ cao của bong bóng gợi ý [E] phía trên manh chiếu.")]
    public float promptOffsetY = 1.0f;
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
        BanhChungMinigame minigame = BanhChungMinigame.Instance;
        bool isMinigameOpen = minigame != null && minigame.IsOpen;

        if (isPlayerInRange && !isDialogueActive && !isViewfinderActive && !isAlbumActive && !isMinigameOpen)
        {
            if (Input.GetKeyDown(interactionKey))
            {
                if (minigame != null)
                {
                    minigame.OpenMinigame();
                }
            }
        }
    }
    #endregion

    #region GUI Drawing - Floating Bubble [E] Gói Bánh Chưng
    private void OnGUI()
    {
        if (!isPlayerInRange) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;
        if (BanhChungMinigame.Instance != null && BanhChungMinigame.Instance.IsOpen) return;
        if (AlbumUIController.Instance != null && AlbumUIController.Instance.IsOpen) return;

        ViewfinderController viewfinder = FindAnyObjectByType<ViewfinderController>();
        if (viewfinder != null && viewfinder.IsViewfinderActive) return;

        InitStyles();

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPromptPos = transform.position + new Vector3(0f, promptOffsetY, 0f);
        Vector3 screenPos = cam.WorldToScreenPoint(worldPromptPos);

        // Nếu nằm ngoài tầm nhìn camera
        if (screenPos.z < 0f) return;

        float guiX = screenPos.x;
        float guiY = Screen.height - screenPos.y;

        float bubbleW = 165f;
        float bubbleH = 32f;
        Rect bubbleRect = new Rect(guiX - bubbleW * 0.5f, guiY - bubbleH * 0.5f, bubbleW, bubbleH);

        Color oldColor = GUI.color;

        // Bóng đổ
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(bubbleRect.x + 2, bubbleRect.y + 2, bubbleRect.width, bubbleRect.height), promptBgTex);

        // Nền bóng bẩy
        GUI.color = new Color(0.14f, 0.17f, 0.22f, 0.95f);
        GUI.DrawTexture(bubbleRect, promptBgTex);

        // Viền đỏ điều ngày Tết
        GUI.color = new Color(0.85f, 0.28f, 0.22f, 1f);
        GUI.DrawTexture(new Rect(bubbleRect.x, bubbleRect.y, bubbleRect.width, 1.5f), promptBgTex);
        GUI.DrawTexture(new Rect(bubbleRect.x, bubbleRect.yMax - 1.5f, bubbleRect.width, 1.5f), promptBgTex);
        GUI.DrawTexture(new Rect(bubbleRect.x, bubbleRect.y, 1.5f, bubbleRect.height), promptBgTex);
        GUI.DrawTexture(new Rect(bubbleRect.xMax - 1.5f, bubbleRect.y, 1.5f, bubbleRect.height), promptBgTex);

        // Chữ nhắc nhở
        GUI.color = Color.white;
        string promptText = $"🍱 [{interactionKey}] Gói Bánh Chưng";
        GUI.Label(bubbleRect, promptText, promptTextStyle);

        GUI.color = oldColor;
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        promptTextStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
        promptTextStyle.normal.textColor = new Color(1f, 0.92f, 0.65f, 1f); // Vàng ấm

        stylesInitialized = true;
    }
    #endregion
}
