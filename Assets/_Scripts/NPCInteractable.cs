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
    [Tooltip("Mã định danh của NPC (ví dụ: BacAn để liên kết nhiệm vụ cành đào phai).")]
    public string npcId = "BacAn";

    [Tooltip("Tên của NPC hiển thị trên hộp thoại.")]
    public string npcName = "Bác An (Chủ Nhà)";

    [Tooltip("Tự động liên kết lời thoại theo trạng thái nhiệm vụ (QuestState 0, 1, 2, 3).")]
    public bool useQuestDialogue = true;

    [Tooltip("Danh sách các câu thoại mặc định nếu không dùng hệ thống nhiệm vụ.")]
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

        // 1. Phân nhánh lời thoại theo trạng thái nhiệm vụ của Bác An
        if (useQuestDialogue && npcId == "BacAn" && GameManager.Instance != null)
        {
            QuestState state = GameManager.Instance.CurrentQuestState;
            string speaker = "Bác An (Chủ Nhà)";

            switch (state)
            {
                case QuestState.NotStarted:
                    string[] state0Lines = new string[]
                    {
                        "Chào Arthur! Người bạn già của tôi, cuối cùng cậu cũng sang tới Hà Nội rồi. Mấy mươi năm từ ngày tốt nghiệp đại học ở Munich rồi nhỉ?",
                        "Gió đông bắc 14 độ se lạnh thế này chắc cậu chưa quen. Chiều 29 Tết rồi, ngõ phố người ta bắt đầu tấp nập sắm sửa đón xuân rồi đấy.",
                        "Trước hiên nhà tôi có cành đào phai vừa chớm nụ mập mạp đẹp lắm. Mắt tôi dạo này mờ rồi, cậu mang theo chiếc máy ảnh cơ kìa, ra chụp giúp tôi một kiểu ảnh làm kỷ niệm được không?"
                    };
                    DialogueManager.Instance.StartDialogue(speaker, state0Lines, () =>
                    {
                        // Chuyển sang Trạng thái 1: Bác An nhờ đi chụp ảnh cành đào phai
                        GameManager.Instance.SetQuestState(QuestState.QuestAccepted);
                    });
                    return;

                case QuestState.QuestAccepted:
                    string[] state1Lines = new string[]
                    {
                        "Cành đào phai cánh hồng thắm ở ngay trước khoảng sân hiên nhà phía đông bắc đấy Arthur.",
                        "Cậu bấm phím [Space] để mở ống kính máy ảnh, cuộn con lăn chuột xoay vòng lấy nét [Focus Ring] cho cánh hoa thật rõ, căn theo lưới 1/3 rồi bấm chuột trái chụp nhé!"
                    };
                    DialogueManager.Instance.StartDialogue(speaker, state1Lines);
                    return;

                case QuestState.PhotoTaken:
                    string[] state2Lines = new string[]
                    {
                        "A, Arthur đã chụp được cành đào phai rồi đấy ư? Mau cho tôi xem bức ảnh nào...",
                        "(Bác An cẩn thận đón lấy tấm ảnh polaroid, xúc động ngắm nghía từng nụ hoa phai e ấp trong nắng chiều)",
                        "Ôi, bức ảnh có hồn quá Arthur ạ! Đúng là sắc đào phai của người Tràng An... Tôi cảm ơn cậu nhiều lắm!",
                        "À này! Đào đã có trong nhà rồi, giờ phải đến Bánh Chưng thôi! Cậu lại manh chiếu bên hiên kia [Phím E], tôi sẽ chỉ cho cậu cách tự tay gói một chiếc bánh chưng vuông vắn ngày Tết nhé!"
                    };
                    DialogueManager.Instance.StartDialogue(speaker, state2Lines, () =>
                    {
                        // Chuyển sang Trạng thái 3: Mở khóa gói Bánh Chưng
                        GameManager.Instance.SetQuestState(QuestState.BanhChungUnlocked);
                    });
                    return;

                case QuestState.BanhChungUnlocked:
                    string[] state3Lines = new string[]
                    {
                        "Cậu lại manh chiếu cói bên hiên nhà [Phím E], trên mâm đã có đủ 3 nguyên liệu: lá dong xanh mướt, thúng nếp cái hoa vàng và thịt ba chỉ ướp tiêu.",
                        "Nhớ kéo thả theo đúng thứ tự phong tục nhé: lót lá dong trước, rải nếp, rồi đặt miếng nhân thịt mỡ vào giữa lòng bánh!"
                    };
                    DialogueManager.Instance.StartDialogue(speaker, state3Lines);
                    return;

                case QuestState.BanhChungWrapped:
                    string[] state4Lines = new string[]
                    {
                        "Khéo tay lắm Arthur! Chiếc bánh chưng đầu tiên của cậu vuông vắn và buộc lạt chữ thập rất đều tay đấy.",
                        "Cậu cất bánh vào túi đồ kỷ niệm đi. Giờ trời sập tối se lạnh rồi, tôi với cậu sẽ cùng ra góc sân nhóm bếp củi, canh nồi luộc bánh đêm 30 Tết [Phím E cạnh nồi luộc] nhé!"
                    };
                    DialogueManager.Instance.StartDialogue(speaker, state4Lines);
                    return;

                case QuestState.BanhChungBoiled:
                    string[] state5Lines = new string[]
                    {
                        "Nồi bánh chưng đã chín thơm ngào ngạt khắp cả ngõ phố rồi Arthur ơi!",
                        "(Bác An rót một chén trà sen tỏa khói nghi ngút trao cho Arthur)",
                        "Cậu uống ngụm trà sen ấm này đi. Người Việt mình thức canh bánh không chỉ để bánh chín, mà để đợi nhau qua một năm vất vả...",
                        "Gió bấc đang thổi từng cơn se sắt... Chỉ ít phút nữa là tiếng pháo hoa Giao thừa sẽ nổ vang trên bầu trời Hà Nội rồi đấy!"
                    };
                    DialogueManager.Instance.StartDialogue(speaker, state5Lines, () =>
                    {
                        // Chuyển sang Trạng thái 6: Sẵn sàng đón Giao Thừa
                        GameManager.Instance.SetQuestState(QuestState.WaitingForMidnight);
                    });
                    return;

                case QuestState.WaitingForMidnight:
                    string[] state6Lines = new string[]
                    {
                        "Chỉ còn ít phút nữa là thời khắc chuyển giao năm mới 00:00 gõ cửa.",
                        "Tôi đã dọn dẹp bàn thờ gia tiên tinh tươm rồi. Tiếng pháo hoa nổ xa xa trên bầu trời chính là lúc tôi thắp nén nhang trầm đầu tiên...",
                        "Arthur hãy chuẩn bị sẵn máy ảnh [Phím Space] nhé. Bắt trọn được khoảnh khắc linh thiêng này sẽ là kỷ niệm tuyệt vời nhất của chuyến đi đấy!"
                    };
                    DialogueManager.Instance.StartDialogue(speaker, state6Lines);
                    return;

                case QuestState.NewYearEvePhotoTaken:
                    string[] state7Lines = new string[]
                    {
                        "Arthur... Cậu đã bấm máy bắt trọn được khoảnh khắc này rồi sao?",
                        "(Bác An đón lấy bức ảnh, đôi mắt rưng rưng xúc động phản chiếu ánh sáng pháo hoa lung linh từ ô cửa sổ)",
                        "Mấy mươi năm qua ở căn nhà cổ phố Hàng Bè này, đây là lần đầu tiên tôi có một bức ảnh đêm Giao Thừa trang trọng và ấm áp đến thế. Nhìn làn khói trầm bay lên, tôi cảm giác như ông bà tổ tiên và người thân đều đang mỉm cười...",
                        "Cảm ơn người bạn già phương xa của tôi! Tình bạn của chúng ta vẫn vẹn nguyên như tách trà sen ngày đông.",
                        "(Bác An kính cẩn rút từ túi áo ra một chiếc phong bao màu đỏ thắm, đặt trang trọng vào hai bàn tay Arthur)",
                        "Tục lệ người Việt mình đầu năm phải mừng tuổi. Chiếc phong bao này có hai chữ 'Bình An'. Chúc cho Arthur một năm mới an khang, ấm áp và tìm thấy sự bình yên sâu thẳm trong tâm hồn!"
                    };
                    DialogueManager.Instance.StartDialogue(speaker, state7Lines, () =>
                    {
                        // 1. Thưởng quà: Phong bao lì xì + 50 Điểm Gắn Kết Bản Địa
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.AddLiXiReward();
                            GameManager.Instance.SetQuestState(QuestState.Chapter1Complete);
                        }

                        // 2. Mở Modal hiển thị Bao Lì Xì trang trọng
                        if (NewYearEveEventManager.Instance != null)
                        {
                            NewYearEveEventManager.Instance.ShowLiXiModal();
                        }
                    });
                    return;

                case QuestState.Chapter1Complete:
                    string[] state8Lines = new string[]
                    {
                        "Chúc Mừng Năm Mới Arthur! Cậu hãy giữ gìn chiếc phong bao lì xì đỏ ấy nhé.",
                        "Sáng mùng Một Tết, không khí Hà Nội thanh tịnh và thiêng liêng lắm. Chúng ta sẽ cùng tản bộ ra hồ Gươm ngắm người dân trẩy hội hái lộc đầu xuân...",
                        "Cậu cứ tự nhiên mở Cuốn Album [Phím Tab] để ngắm lại trọn bộ những kỷ niệm của Chương 1 mà chúng ta vừa cùng nhau tạo nên nhé!"
                    };
                    DialogueManager.Instance.StartDialogue(speaker, state8Lines);
                    return;
            }
        }

        // 2. Mặc định nếu không thuộc chuỗi nhiệm vụ Bác An
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
