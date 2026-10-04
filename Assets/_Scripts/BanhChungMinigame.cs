using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minigame tương tác bằng tay: Gói Bánh Chưng Ngày Tết (Bánh Chưng Wrapping Minigame).
/// - Khung cửa sổ Pop-up 2D Top-down giao diện chiếu cói & mâm cỗ truyền thống.
/// - Cơ chế Kéo-Thả (Drag & Drop) chuột 3 nguyên liệu vào chiếc khuôn vuông theo thứ tự:
///     1. Lá Dong  ->  2. Gạo Nếp  ->  3. Thịt Mỡ
/// - Kiểm tra thứ tự nghiêm ngặt (lót lá trước, rải nếp, rồi đặt nhân thịt).
/// - Khi đủ 3 lớp, bánh tự động gập lá và buộc lạt giang vuông vức kèm âm thanh hoàn thành rộn rã!
/// </summary>
[DisallowMultipleComponent]
public class BanhChungMinigame : MonoBehaviour
{
    public static BanhChungMinigame Instance { get; private set; }

    #region Enums & Structs
    public enum LayerStage
    {
        Empty = 0,    // Khuôn gỗ rỗng
        LaDong = 1,   // Đã lót lớp Lá Dong xanh mướt
        GaoNep = 2,   // Đã rải lớp Gạo Nếp trắng thơm
        ThitMo = 3,   // Đã đặt lớp Nhân Thịt Mỡ ướp tiêu
        Wrapped = 4   // Bánh đã gói xong hoàn chỉnh, buộc lạt chữ thập
    }

    [System.Serializable]
    public struct IngredientData
    {
        public string name;
        public string description;
        public Color itemColor;
    }
    #endregion

    #region Serialized Fields
    [Header("=== PHÍM TẮT & ĐIỀU KHIỂN ===")]
    [Tooltip("Phím tắt mở nhanh minigame gói bánh chưng (mặc định: Phím B).")]
    public KeyCode toggleKey = KeyCode.B;

    [Tooltip("Phím đóng minigame.")]
    public KeyCode closeKey = KeyCode.Escape;

    [Header("=== ÂM THANH MINIGAME (TỰ TỔNG HỢP NẾU TRỐNG) ===")]
    public AudioClip customRustleSound;
    public AudioClip customSuccessFanfare;
    #endregion

    #region Public Properties
    /// <summary> Minigame đang mở hay đóng </summary>
    public bool IsOpen => isMinigameOpen;

    /// <summary> Tiến độ lớp nguyên liệu hiện tại trong khuôn </summary>
    public LayerStage CurrentStage => currentStage;

    /// <summary> Đã gói thành công ít nhất 1 chiếc bánh chưng </summary>
    public bool HasCompletedAny => hasCompletedAtLeastOnce;
    #endregion

    #region Private State
    private PlayerController player;
    private AudioSource audioSource;
    private AudioClip leafSound;
    private AudioClip riceSound;
    private AudioClip meatSound;
    private AudioClip wrapSound;
    private AudioClip fanfareSound;

    private bool isMinigameOpen = false;
    private LayerStage currentStage = LayerStage.Empty;
    private bool hasCompletedAtLeastOnce = false;

    // Drag & Drop State
    private int draggingIngredientIndex = -1; // 0 = Lá Dong, 1 = Gạo Nếp, 2 = Thịt Mỡ
    private Vector2 dragMousePos;
    private string feedbackMessage = "Kéo từng nguyên liệu vào khuôn theo đúng thứ tự nhé!";
    private float feedbackTimer = 0f;

    // Wrapping Animation
    private float sparkleTimer = 0f;

    // GUI Textures & Styles
    private Texture2D whitePixel;
    private Texture2D darkPixel;
    private Texture2D matTex;
    private GUIStyle titleStyle;
    private GUIStyle trayLabelStyle;
    private GUIStyle feedbackStyle;
    private GUIStyle successHeaderStyle;
    private bool stylesInitialized = false;

    // Ingredient Metadata
    private readonly IngredientData[] ingredients = new IngredientData[]
    {
        new IngredientData { name = "1. Lá Dong", description = "Lá xanh mướt, thơm ngát mùi núi rừng", itemColor = new Color(0.24f, 0.58f, 0.28f, 1f) },
        new IngredientData { name = "2. Gạo Nếp", description = "Nếp cái hoa vàng mẩy tròn thơm dẻo", itemColor = new Color(0.96f, 0.95f, 0.90f, 1f) },
        new IngredientData { name = "3. Thịt Mỡ", description = "Thịt ba chỉ ướp tiêu đen đậm đà", itemColor = new Color(0.88f, 0.58f, 0.42f, 1f) }
    };
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        player = FindAnyObjectByType<PlayerController>();

        // Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Tự động tổng hợp âm thanh chân thực cho từng bước
        leafSound = GenerateLeafRustleClip();
        riceSound = GenerateRicePourClip();
        meatSound = GenerateMeatThudClip();
        wrapSound = GenerateWrapTieClip();
        fanfareSound = customSuccessFanfare != null ? customSuccessFanfare : GenerateVictoryFanfareClip();

        InitTextures();
    }

    private void Update()
    {
        HandleHotkeys();

        if (feedbackTimer > 0f)
        {
            feedbackTimer -= Time.deltaTime;
        }

        if (currentStage == LayerStage.Wrapped)
        {
            sparkleTimer += Time.deltaTime;
        }
    }
    #endregion

    #region Open / Close & Input
    private void HandleHotkeys()
    {
        // 1. Phím tắt B để bật/tắt Minigame
        if (Input.GetKeyDown(toggleKey))
        {
            // Tránh mở đè khi đang chụp ảnh hoặc hội thoại hoặc mở Album
            bool isDialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
            ViewfinderController vf = FindAnyObjectByType<ViewfinderController>();
            bool isVfActive = vf != null && vf.IsViewfinderActive;
            AlbumUIController album = AlbumUIController.Instance;
            bool isAlbumActive = album != null && album.IsOpen;

            if (isDialogueActive || isVfActive || isAlbumActive) return;

            ToggleMinigame();
        }

        // 2. Phím ESC để đóng
        if (isMinigameOpen && Input.GetKeyDown(closeKey))
        {
            CloseMinigame();
        }
    }

    public void ToggleMinigame()
    {
        if (isMinigameOpen) CloseMinigame();
        else OpenMinigame();
    }

    public void OpenMinigame()
    {
        if (isMinigameOpen) return;

        isMinigameOpen = true;
        draggingIngredientIndex = -1;
        feedbackMessage = "Kéo 3 nguyên liệu vào chiếc khuôn vuông theo thứ tự: Lá Dong ➔ Gạo Nếp ➔ Thịt Mỡ!";
        feedbackTimer = 5.0f;

        // Tạm khóa di chuyển nhân vật
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.isInputLocked = true;
        }

        PlayClip(leafSound, 0.7f);
        Debug.Log("[BanhChungMinigame] 🍱 Đã mở bàn gói Bánh Chưng truyền thống!");
    }

    public void CloseMinigame()
    {
        if (!isMinigameOpen) return;

        isMinigameOpen = false;
        draggingIngredientIndex = -1;

        // Trả lại quyền di chuyển cho người chơi
        if (player != null)
        {
            bool isDialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
            if (!isDialogueActive)
            {
                player.isInputLocked = false;
            }
        }

        Debug.Log("[BanhChungMinigame] 🍱 Đã đóng bàn gói bánh.");
    }
    #endregion

    #region Drag & Drop Logic
    /// <summary>
    /// Xử lý thả nguyên liệu vào khuôn vuông
    /// </summary>
    private void TryDropIngredient(int ingredientIndex, Rect moldRect, Vector2 dropPos)
    {
        // 1. Kiểm tra xem điểm thả có nằm trong lòng chiếc khuôn vuông không
        if (!moldRect.Contains(dropPos))
        {
            return;
        }

        // Nếu bánh đã gói xong rồi
        if (currentStage == LayerStage.Wrapped)
        {
            ShowFeedback("Chiếc bánh này đã gói xong vuông vức rồi! Hãy bấm [Gói thêm chiếc nữa] để gói lại nhé.");
            return;
        }

        // 2. KIỂM TRA THỨ TỰ NGHIÊM NGẶT
        // Bước 1: Lá Dong (index 0)
        // Bước 2: Gạo Nếp (index 1)
        // Bước 3: Thịt Mỡ (index 2)
        int expectedIndex = (int)currentStage; // 0, 1, 2 tương ứng với Empty, LaDong, GaoNep

        if (ingredientIndex == expectedIndex)
        {
            // ĐÚNG THỨ TỰ: Nạp lớp nguyên liệu vào khuôn
            currentStage = (LayerStage)(expectedIndex + 1);

            switch (currentStage)
            {
                case LayerStage.LaDong:
                    PlayClip(leafSound, 0.85f);
                    ShowFeedback("🌿 Đã lót 4 góc Lá Dong xanh mướt vào khuôn! Tiếp theo hãy rải Gạo Nếp.");
                    break;

                case LayerStage.GaoNep:
                    PlayClip(riceSound, 0.85f);
                    ShowFeedback("🌾 Đã rải một lớp Gạo Nếp trắng ngần thơm dẻo! Giờ hãy đặt nhân Thịt Mỡ vào.");
                    break;

                case LayerStage.ThitMo:
                    PlayClip(meatSound, 0.9f);
                    ShowFeedback("🥩 Đã đặt miếng Thịt Mỡ ướp tiêu thơm lừng vào giữa lòng bánh!");

                    // KHI KÉO ĐỦ 3 LỚP THEO THỨ TỰ: TỰ ĐỘNG BẮT ĐẦU HOẠT ẢNH GÓI BÁNH VÀ BUỘC LẠT
                    StartCoroutine(AutoWrapRoutine());
                    break;
            }
        }
        else
        {
            // SAI THỨ TỰ: Nhắc nhở người chơi
            PlayClip(meatSound, 0.4f);

            if (expectedIndex == 0)
            {
                ShowFeedback("⚠️ Cháu cần lót Lá Dong trước vào khuôn để định hình bánh nhé!");
            }
            else if (expectedIndex == 1)
            {
                ShowFeedback("⚠️ Cần rải lớp Gạo Nếp trước lên nền lá dong rồi mới cho nhân thịt vào!");
            }
            else if (expectedIndex == 2)
            {
                ShowFeedback("⚠️ Đã có lá và nếp rồi, giờ hãy cho nhân Thịt Mỡ vào lòng bánh nhé!");
            }
        }
    }

    /// <summary>
    /// Coroutine gập lá dong, buộc lạt giang và phát âm thanh hoàn thành
    /// </summary>
    private IEnumerator AutoWrapRoutine()
    {
        yield return new WaitForSeconds(0.45f);

        // Phát tiếng gập lá và buộc lạt giang
        PlayClip(wrapSound, 0.95f);

        yield return new WaitForSeconds(0.4f);

        // Chuyển sang trạng thái Wrapped
        currentStage = LayerStage.Wrapped;
        hasCompletedAtLeastOnce = true;
        sparkleTimer = 0f;

        // Phát âm thanh chuông mừng hoàn thành rộn rã
        PlayClip(fanfareSound, 1.0f);
        ShowFeedback("🎉 Bánh Chưng đã tự gập lá và buộc lạt vuông vắn! Một chiếc bánh Tết ấm áp trọn vẹn!");

        // THU HOẠCH PHẦN THƯỞNG HỮU HÌNH VÀO TÚI ĐỒ VÀ MỞ KHÓA KỶ NIỆM ALBUM
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddBanhChungReward();
        }
    }

    public void ResetMold()
    {
        currentStage = LayerStage.Empty;
        draggingIngredientIndex = -1;
        feedbackMessage = "Khuôn đã sẵn sàng. Hãy bắt đầu bằng việc kéo Lá Dong vào khuôn!";
        feedbackTimer = 4.0f;
        PlayClip(leafSound, 0.6f);
    }

    private void ShowFeedback(string msg)
    {
        feedbackMessage = msg;
        feedbackTimer = 4.5f;
    }

    private void PlayClip(AudioClip clip, float volume = 0.8f)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.pitch = UnityEngine.Random.Range(0.97f, 1.03f);
            audioSource.PlayOneShot(clip, volume);
        }
    }
    #endregion

    #region GUI Drawing - Màn hình Minigame Chiếu Cói & Khuôn Bánh
    private void OnGUI()
    {
        if (!isMinigameOpen) return;

        InitStyles();

        Color oldColor = GUI.color;

        // 1. MÀN MỜ NỀN TỐI ĐIỆN ẢNH (DIMMED BACKDROP)
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkPixel);

        // 2. KHUNG CHIẾU CÓI TRUYỀN THỐNG (BAMBOO MAT / TABLE CONTAINER)
        float matW = Mathf.Min(Screen.width * 0.85f, 920f);
        float matH = Mathf.Min(Screen.height * 0.85f, 540f);
        Rect matRect = new Rect((Screen.width - matW) * 0.5f, (Screen.height - matH) * 0.5f, matW, matH);

        // Bóng đổ mâm cỗ
        GUI.color = new Color(0f, 0f, 0f, 0.4f);
        GUI.DrawTexture(new Rect(matRect.x + 8, matRect.y + 10, matRect.width, matRect.height), darkPixel);

        // Nền chiếu cói vàng rơm ấm áp
        GUI.color = new Color(0.92f, 0.86f, 0.72f, 1f);
        GUI.DrawTexture(matRect, matTex);

        // Viền đỏ điều ngày Tết
        GUI.color = new Color(0.78f, 0.22f, 0.18f, 1f);
        DrawFrameBorders(matRect, 4f);

        // 3. TIÊU ĐỀ MINIGAME
        GUI.color = new Color(0.35f, 0.18f, 0.12f, 1f);
        Rect titleRect = new Rect(matRect.x, matRect.y + 14, matRect.width, 30);
        GUI.Label(titleRect, "🎋 GÓI BÁNH CHƯNG NGÀY TẾT - NÉT ẤM ÁP ĐẦU XUÂN 🎋", titleStyle);

        // 4. CHIẾC KHUÔN VUÔNG Ở CHÍNH GIỮA (SQUARE MOLD)
        float moldSize = 220f;
        Rect moldRect = new Rect(matRect.center.x - moldSize * 0.5f, matRect.center.y - moldSize * 0.5f - 10, moldSize, moldSize);

        DrawSquareMold(moldRect);

        // 5. 3 KHAY NGUYÊN LIỆU (LÁ DONG, GẠO NẾP, THỊT MỠ)
        DrawIngredientTrays(matRect, moldRect);

        // 6. DÒNG HƯỚNG DẪN & PHẢN HỒI (FEEDBACK BANNER)
        Rect feedbackRect = new Rect(matRect.x + 20, matRect.yMax - 54, matRect.width - 40, 36);
        GUI.color = new Color(0.18f, 0.15f, 0.12f, 0.9f);
        GUI.DrawTexture(feedbackRect, darkPixel);
        GUI.color = new Color(0.98f, 0.82f, 0.42f, 1f);
        DrawFrameBorders(feedbackRect, 1.2f);
        GUI.color = Color.white;
        GUI.Label(feedbackRect, feedbackMessage, feedbackStyle);

        // 7. NÚT ĐÓNG & NÚT GÓI TIẾP
        Rect closeBtn = new Rect(matRect.xMax - 110, matRect.y + 12, 95, 28);
        if (GUI.Button(closeBtn, "✕ Gấp Chiếu"))
        {
            CloseMinigame();
        }

        if (currentStage == LayerStage.Wrapped)
        {
            DrawHarvestRewardModal(matRect);
        }

        // 8. VẼ NGUYÊN LIỆU ĐANG ĐƯỢC KÉO THEO CHUỘT (DRAGGING ITEM)
        if (draggingIngredientIndex >= 0 && draggingIngredientIndex < ingredients.Length)
        {
            DrawDraggingItem(dragMousePos, draggingIngredientIndex);
        }

        // LẮNG NGHE SỰ KIỆN THẢ CHUỘT
        Event e = Event.current;
        if (e.type == EventType.MouseDrag)
        {
            dragMousePos = e.mousePosition;
        }
        else if (e.type == EventType.MouseUp)
        {
            if (draggingIngredientIndex >= 0)
            {
                TryDropIngredient(draggingIngredientIndex, moldRect, e.mousePosition);
                draggingIngredientIndex = -1;
                e.Use();
            }
        }

        GUI.color = oldColor;
    }

    /// <summary>
    /// Vẽ chiếc khuôn vuông và các lớp nguyên liệu bên trong
    /// </summary>
    private void DrawSquareMold(Rect moldRect)
    {
        // A. Bóng đổ khuôn gỗ
        GUI.color = new Color(0f, 0f, 0f, 0.35f);
        GUI.DrawTexture(new Rect(moldRect.x + 6, moldRect.y + 6, moldRect.width, moldRect.height), darkPixel);

        // B. Thành khuôn gỗ mộc dày dặn
        GUI.color = new Color(0.55f, 0.38f, 0.24f, 1f); // Gỗ nâu ấm
        GUI.DrawTexture(moldRect, whitePixel);

        // C. Lòng khuôn bên trong
        float borderThickness = 12f;
        Rect innerMold = new Rect(moldRect.x + borderThickness, moldRect.y + borderThickness, moldRect.width - borderThickness * 2, moldRect.height - borderThickness * 2);

        // Nền lòng khuôn rỗng
        GUI.color = new Color(0.85f, 0.78f, 0.65f, 1f);
        GUI.DrawTexture(innerMold, whitePixel);

        // D. VẼ NỘI DUNG TỪNG LỚP TRONG KHUÔN
        switch (currentStage)
        {
            case LayerStage.Empty:
                // Khuôn rỗng, hiện chữ nhắc nhở
                GUI.color = new Color(0.55f, 0.45f, 0.35f, 0.8f);
                GUI.Label(innerMold, "Khuôn Gỗ Rỗng\n\n[Hãy kéo Lá Dong vào đây]", trayLabelStyle);
                break;

            case LayerStage.LaDong:
                // Lớp 1: Lá dong xanh mướt lót chữ thập
                GUI.color = new Color(0.24f, 0.58f, 0.28f, 1f);
                GUI.DrawTexture(innerMold, whitePixel);

                // Gân lá dong và cuống lá chìa ra mép khuôn
                GUI.color = new Color(0.18f, 0.46f, 0.22f, 1f);
                DrawRect(new Rect(innerMold.center.x - 2f, innerMold.y, 4f, innerMold.height));
                DrawRect(new Rect(innerMold.x, innerMold.center.y - 2f, innerMold.width, 4f));

                GUI.color = new Color(1f, 1f, 1f, 0.9f);
                GUI.Label(innerMold, "🌿 Lớp Lá Dong Xanh\n\n[Hãy kéo tiếp Gạo Nếp vào]", trayLabelStyle);
                break;

            case LayerStage.GaoNep:
                // Lớp 2: Gạo nếp trắng ngần phủ lên lá dong (để lộ chút viền lá xanh)
                GUI.color = new Color(0.24f, 0.58f, 0.28f, 1f);
                GUI.DrawTexture(innerMold, whitePixel);

                float riceMargin = 14f;
                Rect riceRect = new Rect(innerMold.x + riceMargin, innerMold.y + riceMargin, innerMold.width - riceMargin * 2, innerMold.height - riceMargin * 2);
                GUI.color = new Color(0.97f, 0.96f, 0.92f, 1f);
                GUI.DrawTexture(riceRect, whitePixel);

                GUI.color = new Color(0.35f, 0.28f, 0.2f, 0.9f);
                GUI.Label(riceRect, "🌾 Lớp Gạo Nếp Trắng Dẻo\n\n[Hãy kéo tiếp Thịt Mỡ vào]", trayLabelStyle);
                break;

            case LayerStage.ThitMo:
                // Lớp 3: Nhân thịt mỡ ướp tiêu đặt ngay giữa nếp
                GUI.color = new Color(0.24f, 0.58f, 0.28f, 1f);
                GUI.DrawTexture(innerMold, whitePixel);

                float rMargin = 14f;
                Rect rRect = new Rect(innerMold.x + rMargin, innerMold.y + rMargin, innerMold.width - rMargin * 2, innerMold.height - rMargin * 2);
                GUI.color = new Color(0.97f, 0.96f, 0.92f, 1f);
                GUI.DrawTexture(rRect, whitePixel);

                // Miếng thịt mỡ vàng ươm ở giữa
                float meatSize = 85f;
                Rect meatRect = new Rect(innerMold.center.x - meatSize * 0.5f, innerMold.center.y - meatSize * 0.5f, meatSize, meatSize);
                GUI.color = new Color(0.88f, 0.58f, 0.38f, 1f);
                GUI.DrawTexture(meatRect, whitePixel);

                // Đậu xanh đồ chín bao quanh
                GUI.color = new Color(0.95f, 0.85f, 0.42f, 1f);
                DrawFrameBorders(meatRect, 6f);

                GUI.color = new Color(0.25f, 0.2f, 0.15f, 1f);
                GUI.Label(meatRect, "🥩 Thịt Mỡ\nƯớp Tiêu", trayLabelStyle);
                break;

            case LayerStage.Wrapped:
                // BÁNH TỰ GÓI LẠI HOÀN CHỈNH: Lá dong gập kín 4 cạnh, buộc lạt giang chữ thập
                GUI.color = new Color(0.22f, 0.54f, 0.26f, 1f);
                GUI.DrawTexture(innerMold, whitePixel);

                // Viền gấp mép lá dong
                GUI.color = new Color(0.16f, 0.42f, 0.20f, 1f);
                DrawFrameBorders(innerMold, 6f);

                // Lạt giang vàng óng buộc chữ thập
                float stringThick = 6f;
                GUI.color = new Color(0.95f, 0.86f, 0.48f, 1f);
                // Dây lạt dọc 1 & 2
                DrawRect(new Rect(innerMold.x + innerMold.width * 0.35f, innerMold.y, stringThick, innerMold.height));
                DrawRect(new Rect(innerMold.x + innerMold.width * 0.65f, innerMold.y, stringThick, innerMold.height));
                // Dây lạt ngang 1 & 2
                DrawRect(new Rect(innerMold.x, innerMold.y + innerMold.height * 0.35f, innerMold.width, stringThick));
                DrawRect(new Rect(innerMold.x, innerMold.y + innerMold.height * 0.65f, innerMold.width, stringThick));

                // Nút buộc lạt ở giữa
                GUI.color = new Color(0.98f, 0.90f, 0.55f, 1f);
                DrawRect(new Rect(innerMold.center.x - 10f, innerMold.center.y - 10f, 20f, 20f));

                // Tia lấp lánh (Sparkles)
                DrawSparkles(innerMold.center);

                // Dòng chữ chúc mừng
                GUI.color = new Color(1f, 0.96f, 0.85f, 1f);
                GUI.Label(new Rect(innerMold.x, innerMold.y + 12, innerMold.width, 40), "✨ BÁNH CHƯNG ĐÃ GÓI XONG! ✨", successHeaderStyle);
                break;
        }

        // Viền gỗ ngoài
        GUI.color = new Color(0.42f, 0.28f, 0.16f, 1f);
        DrawFrameBorders(moldRect, 2f);
    }

    /// <summary>
    /// Vẽ 3 khay nguyên liệu (Lá Dong bên trái, Gạo Nếp và Thịt Mỡ bên phải)
    /// </summary>
    private void DrawIngredientTrays(Rect matRect, Rect moldRect)
    {
        float trayW = 160f;
        float trayH = 130f;

        // KHAY 1: LÁ DONG (Bên trái khuôn)
        Rect tray0Rect = new Rect(moldRect.x - trayW - 40, moldRect.center.y - trayH * 0.5f, trayW, trayH);
        DrawIngredientCard(tray0Rect, 0);

        // KHAY 2: GẠO NẾP (Bên phải khuôn, phía trên)
        Rect tray1Rect = new Rect(moldRect.xMax + 40, moldRect.y - 10, trayW, trayH);
        DrawIngredientCard(tray1Rect, 1);

        // KHAY 3: THỊT MỠ (Bên phải khuôn, phía dưới)
        Rect tray2Rect = new Rect(moldRect.xMax + 40, moldRect.yMax - trayH + 10, trayW, trayH);
        DrawIngredientCard(tray2Rect, 2);
    }

    private void DrawIngredientCard(Rect r, int index)
    {
        IngredientData data = ingredients[index];

        // Bóng đổ
        GUI.color = new Color(0f, 0f, 0f, 0.25f);
        GUI.DrawTexture(new Rect(r.x + 3, r.y + 4, r.width, r.height), darkPixel);

        // Nền đĩa đựng
        GUI.color = new Color(0.98f, 0.96f, 0.92f, 1f);
        GUI.DrawTexture(r, whitePixel);

        // Khung màu đặc trưng của nguyên liệu
        Rect iconRect = new Rect(r.x + 14, r.y + 12, r.width - 28, 64);
        GUI.color = data.itemColor;
        GUI.DrawTexture(iconRect, whitePixel);
        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.2f);
        DrawFrameBorders(iconRect, 1.2f);

        // Chữ tên nguyên liệu
        GUI.color = new Color(0.28f, 0.22f, 0.16f, 1f);
        Rect labelRect = new Rect(r.x, iconRect.yMax + 6, r.width, 22);
        GUI.Label(labelRect, data.name, trayLabelStyle);

        // Hướng dẫn kéo
        GUI.color = new Color(0.55f, 0.45f, 0.35f, 0.9f);
        Rect subRect = new Rect(r.x, labelRect.yMax, r.width, 18);
        GUIStyle subStyle = new GUIStyle(trayLabelStyle) { fontSize = 10, fontStyle = FontStyle.Italic };
        GUI.Label(subRect, "[Nhấn giữ để Kéo]", subStyle);

        // Viền đĩa
        GUI.color = new Color(0.82f, 0.72f, 0.58f, 1f);
        DrawFrameBorders(r, 1.5f);

        // LẮNG NGHE BẮT ĐẦU KÉO HOẶC CLICK VÀO KHAY
        Event e = Event.current;
        if (e.type == EventType.MouseDown && r.Contains(e.mousePosition) && currentStage != LayerStage.Wrapped)
        {
            draggingIngredientIndex = index;
            dragMousePos = e.mousePosition;
            e.Use();
        }
    }

    /// <summary>
    /// Vẽ nguyên liệu lơ lửng dưới con trỏ chuột khi đang được kéo
    /// </summary>
    private void DrawDraggingItem(Vector2 mousePos, int index)
    {
        IngredientData data = ingredients[index];
        float dragSize = 75f;
        Rect dragRect = new Rect(mousePos.x - dragSize * 0.5f, mousePos.y - dragSize * 0.5f, dragSize, dragSize);

        // Bóng đổ chuột
        GUI.color = new Color(0f, 0f, 0f, 0.35f);
        GUI.DrawTexture(new Rect(dragRect.x + 8, dragRect.y + 8, dragSize, dragSize), darkPixel);

        // Khối nguyên liệu
        GUI.color = data.itemColor;
        GUI.DrawTexture(dragRect, whitePixel);

        // Viền nổi
        GUI.color = Color.white;
        DrawFrameBorders(dragRect, 2f);

        // Tên ngắn
        GUI.color = (index == 1) ? Color.black : Color.white;
        GUI.Label(dragRect, data.name.Split('.')[1].Trim(), trayLabelStyle);
    }

    /// <summary>
    /// Vẽ hiệu ứng hạt lấp lánh (Sparkles) khi bánh gói xong
    /// </summary>
    private void DrawSparkles(Vector2 center)
    {
        for (int i = 0; i < 8; i++)
        {
            float angle = (i * 45f + sparkleTimer * 90f) * Mathf.Deg2Rad;
            float radius = 105f + Mathf.Sin(sparkleTimer * 6f + i) * 14f;
            Vector2 pos = center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

            float spSize = 6f + Mathf.PingPong(sparkleTimer * 8f + i, 6f);
            Rect spRect = new Rect(pos.x - spSize * 0.5f, pos.y - spSize * 0.5f, spSize, spSize);

            GUI.color = new Color(1f, 0.95f, 0.45f, 0.9f);
            DrawRect(spRect);
        }
    }

    /// <summary>
    /// Bảng thông báo nhận phần thưởng hữu hình và mở khóa trang kỷ niệm Cuốn Album
    /// </summary>
    private void DrawHarvestRewardModal(Rect matRect)
    {
        float modalW = Mathf.Min(matRect.width * 0.85f, 560f);
        float modalH = 140f;
        Rect modalRect = new Rect(matRect.center.x - modalW * 0.5f, matRect.yMax - modalH - 52f, modalW, modalH);

        // Bóng đổ
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(modalRect.x + 4, modalRect.y + 4, modalRect.width, modalRect.height), darkPixel);

        // Nền tối sang trọng
        GUI.color = new Color(0.12f, 0.14f, 0.18f, 0.96f);
        GUI.DrawTexture(modalRect, darkPixel);

        // Viền vàng kim lễ hội
        GUI.color = new Color(0.95f, 0.82f, 0.42f, 1f);
        DrawFrameBorders(modalRect, 2f);

        // Tiêu đề nhận thưởng
        GUI.color = new Color(1f, 0.90f, 0.55f, 1f);
        Rect titleR = new Rect(modalRect.x, modalRect.y + 10, modalRect.width, 24);
        GUI.Label(titleR, "🎋 THU HOẠCH PHẦN THƯỞNG HỮU HÌNH 🎋", titleStyle);

        // Nội dung chi tiết
        int count = GameManager.Instance != null ? GameManager.Instance.BanhChungCount : 1;
        string detailText = $"🍱 Bạn nhận được: +1 Chiếc Bánh Chưng Xanh Tết (Đã cất vào Túi Kỷ Niệm • Tổng: {count} chiếc)\n" +
                            "📖 Đã mở khóa Trang Kỷ Niệm Bánh Chưng mới trong Cuốn Album của Arthur!";
        GUI.color = Color.white;
        Rect detailR = new Rect(modalRect.x + 16, titleR.yMax + 4, modalRect.width - 32, 42);
        GUI.Label(detailR, detailText, feedbackStyle);

        // 4 nút thao tác
        float btnW = (modalRect.width - 32f - 30f) / 4f;
        float btnH = 32f;
        float btnY = modalRect.yMax - btnH - 12f;

        // Nút 1: Ra sân Canh Nồi Luộc Đêm 30 Tết (Pha 2)
        Rect boilBtn = new Rect(modalRect.x + 16, btnY, btnW, btnH);
        if (GUI.Button(boilBtn, "🔥 Canh Nồi Đêm 30"))
        {
            CloseMinigame();
            if (BanhChungBoilingMinigame.Instance != null)
            {
                BanhChungBoilingMinigame.Instance.OpenMinigame();
            }
        }

        // Nút 2: Mở Cuốn Album ngay
        Rect albumBtn = new Rect(boilBtn.xMax + 10, btnY, btnW, btnH);
        if (GUI.Button(albumBtn, "📖 Mở Album (Tab)"))
        {
            CloseMinigame();
            if (AlbumUIController.Instance != null)
            {
                AlbumUIController.Instance.OpenAlbum();
                AlbumUIController.Instance.GoToPage(1);
            }
        }

        // Nút 3: Gói thêm chiếc nữa
        Rect wrapAgainBtn = new Rect(albumBtn.xMax + 10, btnY, btnW, btnH);
        if (GUI.Button(wrapAgainBtn, "✨ Gói Thêm"))
        {
            ResetMold();
        }

        // Nút 4: Cất vào túi & Dạo cảnh
        Rect continueBtn = new Rect(wrapAgainBtn.xMax + 10, btnY, btnW, btnH);
        if (GUI.Button(continueBtn, "✕ Cất Vào Túi"))
        {
            CloseMinigame();
        }
    }

    private void DrawRect(Rect r)
    {
        GUI.DrawTexture(r, whitePixel);
    }

    private void DrawFrameBorders(Rect r, float thickness)
    {
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), whitePixel);
        GUI.DrawTexture(new Rect(r.x, r.yMax - thickness, r.width, thickness), whitePixel);
        GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), whitePixel);
        GUI.DrawTexture(new Rect(r.xMax - thickness, r.y, thickness, r.height), whitePixel);
    }
    #endregion

    #region Procedural Audio & Textures
    private void InitTextures()
    {
        whitePixel = new Texture2D(1, 1);
        whitePixel.SetPixel(0, 0, Color.white);
        whitePixel.Apply();

        darkPixel = new Texture2D(1, 1);
        darkPixel.SetPixel(0, 0, new Color(0.1f, 0.12f, 0.15f, 0.95f));
        darkPixel.Apply();

        // Chiếu cói hoa văn kẻ sọc ấm cúng
        matTex = new Texture2D(32, 32);
        Color baseMat = new Color(0.92f, 0.86f, 0.72f);
        Color stripeMat = new Color(0.88f, 0.81f, 0.66f);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                Color c = ((x + y) % 4 == 0) ? stripeMat : baseMat;
                matTex.SetPixel(x, y, c);
            }
        }
        matTex.Apply();
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        titleStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = new Color(0.35f, 0.18f, 0.12f, 1f);

        trayLabelStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
        trayLabelStyle.normal.textColor = new Color(0.24f, 0.2f, 0.16f, 1f);

        feedbackStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Normal
        };
        feedbackStyle.normal.textColor = new Color(1f, 0.95f, 0.82f, 1f);

        successHeaderStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        successHeaderStyle.normal.textColor = new Color(1f, 0.95f, 0.75f, 1f);

        stylesInitialized = true;
    }

    private AudioClip GenerateLeafRustleClip()
    {
        int sampleRate = 44100;
        float duration = 0.25f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float env = Mathf.Sin(t * Mathf.PI);
            float noise = (UnityEngine.Random.value * 2f - 1f);
            samples[i] = noise * env * 0.35f;
        }

        AudioClip clip = AudioClip.Create("Synthesized_LeafRustle", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateRicePourClip()
    {
        int sampleRate = 44100;
        float duration = 0.35f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float env = 1f - t;
            float highNoise = (UnityEngine.Random.value - 0.5f) * Mathf.Sin((float)i * 0.25f);
            samples[i] = highNoise * env * 0.4f;
        }

        AudioClip clip = AudioClip.Create("Synthesized_RiceScatter", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateMeatThudClip()
    {
        int sampleRate = 44100;
        float duration = 0.2f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float env = Mathf.Pow(1f - t, 2.5f);
            float bass = Mathf.Sin(2f * Mathf.PI * 110f * ((float)i / sampleRate));
            samples[i] = bass * env * 0.55f;
        }

        AudioClip clip = AudioClip.Create("Synthesized_MeatThud", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateWrapTieClip()
    {
        int sampleRate = 44100;
        float duration = 0.45f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float env = Mathf.Sin(t * Mathf.PI);
            float snap = (i % 2500 < 200) ? 0.6f : 0f;
            float rustle = (UnityEngine.Random.value - 0.5f) * 0.3f;
            samples[i] = (snap + rustle) * env * 0.5f;
        }

        AudioClip clip = AudioClip.Create("Synthesized_WrapAndTie", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateVictoryFanfareClip()
    {
        int sampleRate = 44100;
        float duration = 0.85f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        // Hợp âm 3 nốt vui tươi C5 (523Hz), E5 (659Hz), G5 (784Hz)
        float[] freqs = new float[] { 523.25f, 659.25f, 783.99f };

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float env = Mathf.Pow(1f - t, 1.5f);
            float val = 0f;
            for (int f = 0; f < freqs.Length; f++)
            {
                val += Mathf.Sin(2f * Mathf.PI * freqs[f] * ((float)i / sampleRate)) * 0.33f;
            }
            samples[i] = val * env * 0.5f;
        }

        AudioClip clip = AudioClip.Create("Synthesized_BanhChungVictory", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
    #endregion
}
