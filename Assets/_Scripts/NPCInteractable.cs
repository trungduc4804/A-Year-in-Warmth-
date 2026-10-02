using UnityEngine;

/// <summary>
/// Thành phần gắn trên khối vuông (hoặc bất kỳ GameObject nào) đại diện cho NPC có thể tương tác.
/// - Nhận diện khi người chơi đến gần trong bán kính tương tác.
/// - Hiển thị biểu tượng / bong bóng nổi [E] Trò chuyện lơ lửng trên đầu NPC.
/// - Bấm phím E để kích hoạt hộp thoại trò chuyện thông qua DialogueManager.
/// </summary>
[DisallowMultipleComponent]
public class NPCInteractable : MonoBehaviour
{
    #region Serialized Fields
    [Header("=== THÔNG TIN NPC (NPC INFO) ===")]
    [Tooltip("Tên của NPC hiển thị trên hộp thoại.")]
    public string npcName = "Bác Rowan (Làm Vườn)";

    [Tooltip("Danh sách các câu thoại NPC sẽ nói theo thứ tự.")]
    [TextArea(2, 5)]
    public string[] dialogueLines = new string[]
    {
        "Chào cháu! Một ngày ngập tràn ánh nắng và gió mát trong khu vườn của chúng ta, phải không?",
        "Bác thấy cháu đang cầm theo chiếc máy ảnh xinh xắn đấy. Hãy bấm phím [Space] để thử chụp những tán cây mùa thu xem sao nhé!",
        "Mỗi góc nhỏ ở nơi này đều chất chứa những khoảnh khắc ấm áp đang chờ cháu lưu giữ lại đấy."
    };

    [Header("=== CÀI ĐẶT TƯƠNG TÁC (INTERACTION) ===")]
    [Tooltip("Phím bấm để bắt đầu trò chuyện.")]
    public KeyCode interactionKey = KeyCode.E;

    [Tooltip("Bán kính tối đa để người chơi có thể tương tác với NPC (units).")]
    [Range(1.0f, 5.0f)]
    public float interactionRadius = 2.0f;

    [Tooltip("Độ cao của bong bóng gợi ý [E] phía trên đầu NPC.")]
    public float promptOffsetY = 1.2f;

    [Header("=== HIỆU ỨNG TRỰC QUAN (VISUAL BOUNCE) ===")]
    [Tooltip("Bật hiệu ứng nảy nhẹ vui vẻ của NPC khi bắt đầu được trò chuyện.")]
    public bool enableTalkBounce = true;
    #endregion

    #region Private State
    private PlayerController player;
    private bool isPlayerInRange = false;
    private Vector3 initialScale;
    private float bounceTimer = 0f;

    // GUI Textures & Styles
    private Texture2D promptBgTex;
    private GUIStyle promptTextStyle;
    private bool stylesInitialized = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        initialScale = transform.localScale;

        promptBgTex = new Texture2D(1, 1);
        promptBgTex.SetPixel(0, 0, new Color(0.15f, 0.17f, 0.22f, 0.92f));
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

        // 1. Kiểm tra khoảng cách tới người chơi
        float dist = Vector2.Distance(transform.position, player.transform.position);
        isPlayerInRange = dist <= interactionRadius;

        // 2. Không cho phép tương tác nếu Dialogue đang mở hoặc máy ảnh Viewfinder đang mở
        bool isDialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
        ViewfinderController viewfinder = FindAnyObjectByType<ViewfinderController>();
        bool isViewfinderActive = viewfinder != null && viewfinder.IsViewfinderActive;

        if (isPlayerInRange && !isDialogueActive && !isViewfinderActive)
        {
            // 3. Lắng nghe phím tương tác
            if (Input.GetKeyDown(interactionKey))
            {
                TriggerDialogue();
            }
        }

        // 4. Hoạt ảnh nảy nhẹ khi nói chuyện
        if (bounceTimer > 0f)
        {
            bounceTimer -= Time.deltaTime;
            float bounce = Mathf.Sin(bounceTimer * Mathf.PI * 8f) * 0.12f;
            transform.localScale = new Vector3(initialScale.x * (1f - bounce * 0.5f), initialScale.y * (1f + bounce), initialScale.z);
        }
        else
        {
            transform.localScale = initialScale;
        }
    }
    #endregion

    #region Dialogue Trigger
    /// <summary>
    /// Kích hoạt đoạn hội thoại của NPC này
    /// </summary>
    public void TriggerDialogue()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("[NPCInteractable] Không tìm thấy DialogueManager trong scene!");
            return;
        }

        if (enableTalkBounce)
        {
            bounceTimer = 0.25f;
        }

        DialogueManager.Instance.StartDialogue(npcName, dialogueLines);
    }
    #endregion

    #region Floating Prompt GUI
    private void OnGUI()
    {
        // Chỉ hiện gợi ý nổi khi người chơi đứng gần và hộp thoại chưa mở
        if (!isPlayerInRange) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        ViewfinderController viewfinder = FindAnyObjectByType<ViewfinderController>();
        if (viewfinder != null && viewfinder.IsViewfinderActive) return;

        InitStyles();

        Camera cam = Camera.main;
        if (cam == null) return;

        // Chuyển toạ độ 3D trên đầu NPC thành toạ độ 2D màn hình
        float floatingBob = Mathf.Sin(Time.time * 4.0f) * 6.0f; // Nhấp nhô nhẹ lơ lửng
        Vector3 worldPos = transform.position + new Vector3(0f, promptOffsetY, 0f);
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        // Kiểm tra xem NPC có nằm phía trước camera không
        if (screenPos.z < 0) return;

        // Toạ độ GUI tính từ trên xuống
        float guiY = Screen.height - screenPos.y + floatingBob;
        float guiX = screenPos.x;

        string promptText = $"[{interactionKey}] Trò chuyện";
        float bubbleW = 120f;
        float bubbleH = 28f;
        Rect bubbleRect = new Rect(guiX - bubbleW * 0.5f, guiY - bubbleH * 0.5f, bubbleW, bubbleH);

        Color oldColor = GUI.color;

        // Nền bong bóng thoại nổi
        GUI.color = Color.white;
        GUI.DrawTexture(bubbleRect, promptBgTex);

        // Viền vàng cam ấm áp
        GUI.color = new Color(0.92f, 0.65f, 0.35f, 0.95f);
        DrawFrameBorders(bubbleRect, 1.5f);

        // Chữ [E] Trò chuyện
        GUI.color = Color.white;
        GUI.Label(bubbleRect, promptText, promptTextStyle);

        GUI.color = oldColor;
    }

    private void DrawFrameBorders(Rect r, float thickness)
    {
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), promptBgTex);
        GUI.DrawTexture(new Rect(r.x, r.yMax - thickness, r.width, thickness), promptBgTex);
        GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), promptBgTex);
        GUI.DrawTexture(new Rect(r.xMax - thickness, r.y, thickness, r.height), promptBgTex);
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
        promptTextStyle.normal.textColor = new Color(1f, 0.92f, 0.75f, 1f);

        stylesInitialized = true;
    }

    private void OnDrawGizmosSelected()
    {
        // Vẽ vòng tròn bán kính tương tác trong Editor Scene View
        Gizmos.color = new Color(0.9f, 0.6f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
    #endregion
}
