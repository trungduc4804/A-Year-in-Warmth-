using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Giao diện Cuốn Album Kỷ Niệm (Album UI) - Trái tim dẫn dắt toàn bộ câu chuyện của Arthur.
/// - Bấm Tab (hoặc nút Menu): Mở / Gập cuốn sổ nhật ký ảnh.
/// - Trang bên trái: Khung ảnh Polaroid (nạp Texture động từ nhiệm vụ chụp ảnh).
/// - Trang bên phải: Dòng chữ viết tay của Arthur cùng đoạn pseudocode chan chứa cảm xúc.
/// - Hỗ trợ cả gán trực tiếp vào uGUI (RawImage, Image, Text) lẫn giao diện sổ vẽ tự động (OnGUI Plug-and-Play).
/// </summary>
[DisallowMultipleComponent]
public class AlbumUIController : MonoBehaviour
{
    public static AlbumUIController Instance { get; private set; }

    #region Serialized Fields - Cấu hình Phím & uGUI
    [Header("=== PHÍM ĐIỀU KHIỂN (CONTROLS) ===")]
    [Tooltip("Phím mở/đóng cuốn Album kỷ niệm (mặc định: Phím Tab).")]
    public KeyCode toggleKey = KeyCode.Tab;

    [Tooltip("Phím phụ để đóng nhanh Album.")]
    public KeyCode closeKey = KeyCode.Escape;

    [Header("=== LIÊN KẾT UGUI CANVAS (TÙY CHỌN NẾU TỰ DỰNG CANVAS) ===")]
    [Tooltip("Component RawImage trên trang album để nạp trực tiếp Texture ảnh vừa chụp.")]
    public RawImage polaroidRawImage;

    [Tooltip("Component Text hiển thị dòng chữ viết tay/nhật ký của Arthur.")]
    public Text diaryNotesText;

    [Tooltip("GameObject Panel chứa toàn bộ giao diện Album trong Canvas.")]
    public GameObject albumCanvasPanel;

    [Header("=== ÂM THANH LẬT TRANG (PAGE TURN AUDIO) ===")]
    [Tooltip("Âm thanh sột soạt nhẹ khi mở sổ hoặc lật trang (tự tổng hợp nếu để trống).")]
    public AudioClip customPageTurnSound;
    #endregion

    #region Public Properties
    /// <summary> Trạng thái cuốn Album đang mở hay đóng </summary>
    public bool IsOpen => isAlbumOpen;

    /// <summary> Trang album hiện tại (0-indexed) </summary>
    public int CurrentPageIndex => currentPageIndex;
    #endregion

    #region Private State
    private PlayerController player;
    private AudioSource audioSource;
    private AudioClip pageTurnClip;

    private bool isAlbumOpen = false;
    private int currentPageIndex = 0;
    private const int TotalPages = 3;

    // Hoạt ảnh mở sổ (Smooth Open Animation)
    private float openAnimProgress = 0f;

    // GUI Textures & Styles (Procedural Fallback)
    private Texture2D paperTex;
    private Texture2D coverTex;
    private Texture2D whitePixel;
    private Texture2D darkPixel;
    private GUIStyle bookTitleStyle;
    private GUIStyle polaroidLabelStyle;
    private GUIStyle diaryHeaderStyle;
    private GUIStyle diaryBodyStyle;
    private GUIStyle pseudocodeStyle;
    private GUIStyle pageNavStyle;
    private bool stylesInitialized = false;
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

        pageTurnClip = customPageTurnSound != null ? customPageTurnSound : GeneratePageTurnClip();

        // Khởi tạo Texture đồ họa
        InitTextures();

        // Nếu có gắn panel Canvas từ Inspector, ẩn đi lúc đầu
        if (albumCanvasPanel != null)
        {
            albumCanvasPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // Đăng ký lắng nghe sự kiện đổi trạng thái nhiệm vụ từ GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnQuestStateChanged += HandleQuestStateChanged;
        }

        // Cập nhật ảnh nếu đã có sẵn
        RefreshAlbumData();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnQuestStateChanged -= HandleQuestStateChanged;
        }
    }

    private void Update()
    {
        HandleInput();

        // Hoạt ảnh mở sổ mượt mà
        float targetAnim = isAlbumOpen ? 1.0f : 0.0f;
        if (Mathf.Abs(openAnimProgress - targetAnim) > 0.01f)
        {
            openAnimProgress = Mathf.MoveTowards(openAnimProgress, targetAnim, Time.deltaTime * 5.5f);
        }
    }
    #endregion

    #region Input Handling & Toggle
    private void HandleInput()
    {
        // 1. Phím Tab: Bật/Tắt Album
        if (Input.GetKeyDown(toggleKey))
        {
            // Không mở Album nếu đang chụp ảnh Viewfinder hoặc đang trong hội thoại hoặc đang gói bánh chưng
            bool isDialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
            ViewfinderController viewfinder = FindAnyObjectByType<ViewfinderController>();
            bool isViewfinderActive = viewfinder != null && viewfinder.IsViewfinderActive;
            bool isMinigameOpen = BanhChungMinigame.Instance != null && BanhChungMinigame.Instance.IsOpen;

            if (isDialogueActive || isViewfinderActive || isMinigameOpen) return;

            ToggleAlbum();
        }

        // 2. Phím ESC: Đóng Album nếu đang mở
        if (isAlbumOpen && Input.GetKeyDown(closeKey))
        {
            CloseAlbum();
        }

        // 3. Phím điều hướng lật trang khi mở sổ
        if (isAlbumOpen)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                PrevPage();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                NextPage();
            }
        }
    }

    public void ToggleAlbum()
    {
        if (isAlbumOpen) CloseAlbum();
        else OpenAlbum();
    }

    public void OpenAlbum()
    {
        if (isAlbumOpen) return;

        isAlbumOpen = true;
        PlayPageTurnSound();
        RefreshAlbumData();

        // Khóa di chuyển người chơi
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            player.isInputLocked = true;
        }

        // Nếu có UI Canvas panel
        if (albumCanvasPanel != null)
        {
            albumCanvasPanel.SetActive(true);
        }

        Debug.Log("[AlbumUI] 📖 Đã mở cuốn Album kỷ niệm của Arthur.");
    }

    public void CloseAlbum()
    {
        if (!isAlbumOpen) return;

        isAlbumOpen = false;
        PlayPageTurnSound();

        // Trả lại quyền di chuyển cho người chơi (nếu không có hội thoại nào khác)
        if (player != null)
        {
            bool isDialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;
            if (!isDialogueActive)
            {
                player.isInputLocked = false;
            }
        }

        // Nếu có UI Canvas panel
        if (albumCanvasPanel != null)
        {
            albumCanvasPanel.SetActive(false);
        }

        Debug.Log("[AlbumUI] 📕 Đã gấp cuốn Album lại.");
    }
    #endregion

    #region Page Navigation & Data Binding
    public void NextPage()
    {
        if (currentPageIndex < TotalPages - 1)
        {
            currentPageIndex++;
            PlayPageTurnSound();
            RefreshAlbumData();
        }
    }

    public void PrevPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            PlayPageTurnSound();
            RefreshAlbumData();
        }
    }

    private void HandleQuestStateChanged(QuestState newState)
    {
        RefreshAlbumData();
    }

    /// <summary>
    /// Nạp dữ liệu ảnh động (Dynamic Texture Assignment) và nội dung chữ viết tay vào giao diện
    /// </summary>
    public void RefreshAlbumData()
    {
        Texture2D currentPhoto = GetCurrentPagePhoto();
        string currentNotes = GetCurrentPageDiaryNotes();

        // 1. Gán Texture ảnh vào component RawImage trên Canvas (nếu người dùng liên kết uGUI)
        if (polaroidRawImage != null)
        {
            if (currentPhoto != null)
            {
                polaroidRawImage.texture = currentPhoto;
                polaroidRawImage.color = Color.white;
            }
            else
            {
                polaroidRawImage.texture = null;
                polaroidRawImage.color = new Color(0.2f, 0.2f, 0.2f, 0.3f); // Placeholder mờ
            }
        }

        // 2. Gán chữ viết tay vào component Text trên Canvas (nếu có)
        if (diaryNotesText != null)
        {
            diaryNotesText.text = currentNotes;
        }
    }

    /// <summary>
    /// Lấy ảnh tương ứng với từng trang trong Album
    /// </summary>
    public Texture2D GetCurrentPagePhoto()
    {
        switch (currentPageIndex)
        {
            case 0: // Trang 1: Cành Đào Phai của Bác An
                if (GameManager.Instance != null && GameManager.Instance.QuestPhoto != null)
                {
                    return GameManager.Instance.QuestPhoto;
                }
                return null;

            case 1: // Trang 2: Khu vườn dạo cảnh mùa thu (nếu có ảnh chụp từ Viewfinder)
            case 2: // Trang 3: Ảnh kỷ niệm tự do
                ViewfinderController vf = FindAnyObjectByType<ViewfinderController>();
                if (vf != null && vf.LatestCapturedPhoto != null)
                {
                    return vf.LatestCapturedPhoto;
                }
                return null;

            default:
                return null;
        }
    }

    /// <summary>
    /// Nội dung dòng nhật ký và pseudocode của Arthur cho từng trang
    /// </summary>
    public string GetCurrentPageDiaryNotes()
    {
        switch (currentPageIndex)
        {
            case 0:
                bool hasPeachPhoto = GameManager.Instance != null && GameManager.Instance.QuestPhoto != null;
                if (hasPeachPhoto)
                {
                    return "Hà Nội, chiều muộn 29 Tết.\n" +
                           "Gió bấc rít qua từng kẽ lá, nhưng vừa bước tới trước hiên nhà Bác An, sắc hồng phai đầu tiên đã sưởi ấm cả khoảng sân nhỏ. Từng cánh hoa mỏng manh khẽ rung rinh trong nắng... Bác An mừng rỡ lắm khi thấy bức ảnh này.";
                }
                else
                {
                    return "Hà Nội, ngày 29 Tết.\n" +
                           "Bác An nhờ mình chụp lại cành đào phai chớm nở trước hiên nhà. Mình phải lấy máy ảnh [Phím Space] chụp lại để lưu vào cuốn sổ này mới được...";
                }

            case 1:
                return "Lối Đi Công Viên Mùa Thu.\n" +
                       "Những viên sỏi ấm trải dài dưới tán lá vàng ươm. Mỗi bước chân tản bộ ở đây đều khiến tâm hồn nhẹ bẫng như một làn mây trôi.";

            case 2:
                return "Góc Kỷ Niệm Tự Do.\n" +
                       "Bất kỳ góc nhỏ bình yên nào ta vô tình bắt gặp trên hành trình đều xứng đáng có một trang riêng trong cuốn sổ ký ức này.";

            default:
                return "";
        }
    }

    public string GetCurrentPagePseudocode()
    {
        switch (currentPageIndex)
        {
            case 0:
                bool completed = GameManager.Instance != null && GameManager.Instance.CurrentQuestState >= QuestState.PhotoTaken;
                return "// Arthur's Heart.log()\n" +
                       "Memory peachBlossom = new Memory();\n" +
                       "peachBlossom.Subject = \"Cành Đào Phai\";\n" +
                       "peachBlossom.Location = \"Hiên nhà Bác An\";\n" +
                       "peachBlossom.Date = \"29 Tết Ấm Áp\";\n" +
                       $"peachBlossom.Captured = {completed.ToString().ToLower()};\n" +
                       "Heart.Save(peachBlossom);";

            case 1:
                return "// Environment.Trace()\n" +
                       "ScenicPark park = World.GetCozyPlace();\n" +
                       "park.BreatheAir();\n" +
                       "Soul.State = CalmLevitate;";

            case 2:
                return "// FreeLens.Capture()\n" +
                       "Texture2D instantMoment = Camera.Snap();\n" +
                       "Album.AddKeepsake(instantMoment);\n" +
                       "return true;";

            default:
                return "";
        }
    }

    private void PlayPageTurnSound()
    {
        if (audioSource != null && pageTurnClip != null)
        {
            audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(pageTurnClip, 0.75f);
        }
    }
    #endregion

    #region GUI Drawing - Cuốn Sổ Kỷ Niệm Mở Ra Tuyệt Đẹp (OnGUI)
    private void OnGUI()
    {
        // Chỉ vẽ khi đang mở sổ hoặc đang trong hoạt ảnh đóng/mở
        if (openAnimProgress <= 0.01f) return;

        InitStyles();

        Color oldColor = GUI.color;

        // 1. LỚP MÀN MỜ NỀN TỐI (DIMMED BACKDROP)
        float backdropAlpha = openAnimProgress * 0.72f;
        GUI.color = new Color(0f, 0f, 0f, backdropAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkPixel);

        // 2. KHUNG CUỐN SỔ MỞ HAI TRANG (OPEN 2-PAGE SPREAD)
        float bookW = Mathf.Min(Screen.width * 0.82f, 880f);
        float bookH = Mathf.Min(Screen.height * 0.82f, 520f);

        // Hoạt ảnh nảy nhẹ (Scale & Fade) khi mở
        float scale = 0.85f + 0.15f * openAnimProgress;
        float scaledW = bookW * scale;
        float scaledH = bookH * scale;

        Rect bookRect = new Rect((Screen.width - scaledW) * 0.5f, (Screen.height - scaledH) * 0.5f, scaledW, scaledH);

        // Bóng đổ của cuốn sổ
        GUI.color = new Color(0f, 0f, 0f, 0.45f * openAnimProgress);
        GUI.DrawTexture(new Rect(bookRect.x + 8, bookRect.y + 10, bookRect.width, bookRect.height), darkPixel);

        // Bìa da ngoài màu nâu gỗ ấm cổ điển
        GUI.color = new Color(0.38f, 0.25f, 0.16f, openAnimProgress);
        GUI.DrawTexture(new Rect(bookRect.x - 6, bookRect.y - 6, bookRect.width + 12, bookRect.height + 12), whitePixel);

        // Giấy sổ cổ điển màu kem ngà (Parchment Cream)
        GUI.color = new Color(0.96f, 0.94f, 0.88f, openAnimProgress);
        GUI.DrawTexture(bookRect, paperTex);

        // Gáy sổ ở giữa (Book Spine / Binding Crease)
        float spineX = bookRect.x + bookRect.width * 0.5f;
        GUI.color = new Color(0.82f, 0.78f, 0.70f, 0.8f * openAnimProgress);
        GUI.DrawTexture(new Rect(spineX - 1.5f, bookRect.y, 3f, bookRect.height), darkPixel);

        // Viền trang sổ
        GUI.color = new Color(0.78f, 0.72f, 0.62f, openAnimProgress);
        DrawFrameBorders(bookRect, 1.5f);

        // Phân chia hai trang: Trang trái (Polaroid) và Trang phải (Nhật ký & Pseudocode)
        float pagePadding = 24f * scale;
        float halfPageW = (bookRect.width * 0.5f) - (pagePadding * 2);
        float pageH = bookRect.height - (pagePadding * 2);

        Rect leftPageRect = new Rect(bookRect.x + pagePadding, bookRect.y + pagePadding, halfPageW, pageH);
        Rect rightPageRect = new Rect(spineX + pagePadding, bookRect.y + pagePadding, halfPageW, pageH);

        // 3. VẼ TRANG BÊN TRÁI: KHUNG ẢNH POLAROID
        DrawLeftPolaroidPage(leftPageRect, scale);

        // 4. VẼ TRANG BÊN PHẢI: NHẬT KÝ & PSEUDOCODE CỦA ARTHUR
        DrawRightDiaryPage(rightPageRect, scale);

        // 5. NÚT LẬT TRANG & ĐIỀU HƯỚNG DƯỚI GÁY SỔ
        DrawPageNavigation(bookRect, scale);

        GUI.color = oldColor;
    }

    /// <summary>
    /// Vẽ trang bên trái: Khung ảnh Polaroid kẹp băng dính cổ điển
    /// </summary>
    private void DrawLeftPolaroidPage(Rect pageRect, float scale)
    {
        // Tiêu đề trang trái
        GUI.color = new Color(0.4f, 0.32f, 0.25f, openAnimProgress);
        GUI.Label(new Rect(pageRect.x, pageRect.y, pageRect.width, 24 * scale), "KHOẢNH KHẮC LƯU GIỮ", bookTitleStyle);

        // KHUNG ẢNH POLAROID
        float cardW = pageRect.width * 0.88f;
        float cardH = cardW * 1.25f;
        Rect cardRect = new Rect(pageRect.x + (pageRect.width - cardW) * 0.5f, pageRect.y + 36 * scale, cardW, cardH);

        // Bóng đổ tấm ảnh Polaroid
        GUI.color = new Color(0f, 0f, 0f, 0.25f * openAnimProgress);
        GUI.DrawTexture(new Rect(cardRect.x + 4, cardRect.y + 4, cardRect.width, cardRect.height), darkPixel);

        // Khung viền trắng giấy ảnh Polaroid
        GUI.color = new Color(0.99f, 0.98f, 0.95f, openAnimProgress);
        GUI.DrawTexture(cardRect, whitePixel);
        GUI.color = new Color(0.85f, 0.82f, 0.75f, openAnimProgress);
        DrawFrameBorders(cardRect, 1.2f);

        // Băng dính Washi Tape trang trí góc trên tấm ảnh
        GUI.color = new Color(0.82f, 0.62f, 0.55f, 0.85f * openAnimProgress);
        GUI.DrawTexture(new Rect(cardRect.x + 12, cardRect.y - 8, 45 * scale, 16 * scale), whitePixel);
        GUI.DrawTexture(new Rect(cardRect.xMax - 55 * scale, cardRect.y - 8, 45 * scale, 16 * scale), whitePixel);

        // VÙNG HÌNH ẢNH TEXTURE BÊN TRONG KHUNG POLAROID
        float photoMargin = 12f * scale;
        float photoW = cardW - (photoMargin * 2);
        float photoH = photoW * 0.82f;
        Rect photoRect = new Rect(cardRect.x + photoMargin, cardRect.y + photoMargin + 6, photoW, photoH);

        Texture2D currentPhoto = GetCurrentPagePhoto();

        if (currentPhoto != null)
        {
            // ĐÃ CÓ ẢNH: Vẽ Texture động trực tiếp
            GUI.color = Color.white;
            GUI.DrawTexture(photoRect, currentPhoto, ScaleMode.ScaleAndCrop);
            GUI.color = new Color(0.7f, 0.65f, 0.55f, openAnimProgress);
            DrawFrameBorders(photoRect, 1f);

            // DÒNG GHI CHÚ VIẾT TAY DƯỚI ẢNH (Ví dụ: "Hà Nội ngày 29 Tết - Cành đào đầu tiên")
            GUI.color = new Color(0.28f, 0.24f, 0.2f, openAnimProgress);
            Rect captionRect = new Rect(cardRect.x + 8, photoRect.yMax + 6, cardRect.width - 16, cardRect.yMax - photoRect.yMax - 10);
            string photoCaption = (currentPageIndex == 0)
                ? "Hà Nội ngày 29 Tết - Cành đào đầu tiên"
                : (currentPageIndex == 1) ? "Công viên mùa thu rực rỡ" : "Góc kỷ niệm ấm áp";
            GUI.Label(captionRect, photoCaption, polaroidLabelStyle);
        }
        else
        {
            // CHƯA CÓ ẢNH: Khung nét đứt chờ chụp
            GUI.color = new Color(0.88f, 0.85f, 0.78f, openAnimProgress);
            GUI.DrawTexture(photoRect, whitePixel);
            GUI.color = new Color(0.68f, 0.62f, 0.52f, openAnimProgress);
            DrawFrameBorders(photoRect, 1.5f);

            GUI.color = new Color(0.55f, 0.48f, 0.4f, openAnimProgress);
            GUIStyle emptyHintStyle = new GUIStyle(polaroidLabelStyle) { fontSize = Mathf.RoundToInt(12 * scale) };
            string hint = (currentPageIndex == 0)
                ? "📷 [Chưa có ảnh]\nHãy nhận việc từ Bác An và\nbấm [Space] chụp cành đào phai!"
                : "📷 [Chưa có ảnh]\nHãy mở máy ảnh ghi lại khoảnh khắc!";
            GUI.Label(photoRect, hint, emptyHintStyle);
        }
    }

    /// <summary>
    /// Vẽ trang bên phải: Nhật ký tâm sự cùng pseudocode của Arthur
    /// </summary>
    private void DrawRightDiaryPage(Rect pageRect, float scale)
    {
        // 1. Tiêu đề nhật ký & Ngày tháng
        GUI.color = new Color(0.35f, 0.28f, 0.22f, openAnimProgress);
        string headerTitle = (currentPageIndex == 0) ? "KÝ ỨC #01: CÀNH ĐÀO ĐẦU TIÊN" :
                             (currentPageIndex == 1) ? "KÝ ỨC #02: LỐI ĐI MÙA THU" : "KÝ ỨC #03: KHOẢNH KHẮC TỰ DO";
        GUI.Label(new Rect(pageRect.x, pageRect.y, pageRect.width, 24 * scale), headerTitle, diaryHeaderStyle);

        // Đường kẻ gạch chân trang trí
        GUI.color = new Color(0.8f, 0.65f, 0.5f, 0.6f * openAnimProgress);
        DrawRect(new Rect(pageRect.x, pageRect.y + 26 * scale, pageRect.width, 1.2f));

        // 2. Dòng chữ viết tay của Arthur
        GUI.color = new Color(0.22f, 0.18f, 0.15f, openAnimProgress);
        Rect diaryBodyRect = new Rect(pageRect.x, pageRect.y + 36 * scale, pageRect.width, 120 * scale);
        string diaryText = GetCurrentPageDiaryNotes();
        GUI.Label(diaryBodyRect, diaryText, diaryBodyStyle);

        // 3. Khung Pseudocode / Nhật ký dòng suy nghĩ của Arthur (Coding Diary)
        float codeH = 140 * scale;
        Rect codeBoxRect = new Rect(pageRect.x, diaryBodyRect.yMax + 14 * scale, pageRect.width, codeH);

        // Nền tối thanh lịch cho code
        GUI.color = new Color(0.16f, 0.18f, 0.22f, 0.95f * openAnimProgress);
        GUI.DrawTexture(codeBoxRect, darkPixel);
        GUI.color = new Color(0.85f, 0.72f, 0.45f, 0.8f * openAnimProgress);
        DrawFrameBorders(codeBoxRect, 1.2f);

        // Nội dung Pseudocode
        GUI.color = new Color(0.92f, 0.88f, 0.72f, openAnimProgress);
        Rect codeTextRect = new Rect(codeBoxRect.x + 12, codeBoxRect.y + 10, codeBoxRect.width - 24, codeBoxRect.height - 20);
        string pseudocode = GetCurrentPagePseudocode();
        GUI.Label(codeTextRect, pseudocode, pseudocodeStyle);
    }

    /// <summary>
    /// Vẽ thanh chuyển trang và phím tắt thoát dưới chân cuốn sổ
    /// </summary>
    private void DrawPageNavigation(Rect bookRect, float scale)
    {
        GUI.color = new Color(0.45f, 0.38f, 0.32f, openAnimProgress);
        float footerY = bookRect.yMax - 30 * scale;

        // Trang hiện tại
        Rect pageNumRect = new Rect(bookRect.x, footerY, bookRect.width, 24);
        GUI.Label(pageNumRect, $"• Trang {currentPageIndex + 1} / {TotalPages} • [Tab / ESC]: Gập sổ lại", pageNavStyle);

        // Nút trang trước [<]
        if (currentPageIndex > 0)
        {
            Rect prevBtnRect = new Rect(bookRect.x + 30 * scale, footerY, 120 * scale, 24);
            if (GUI.Button(prevBtnRect, "◄ Trang trước"))
            {
                PrevPage();
            }
        }

        // Nút trang sau [>]
        if (currentPageIndex < TotalPages - 1)
        {
            Rect nextBtnRect = new Rect(bookRect.xMax - 150 * scale, footerY, 120 * scale, 24);
            if (GUI.Button(nextBtnRect, "Trang sau ►"))
            {
                NextPage();
            }
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

    #region Styles & Sound Helpers
    private void InitTextures()
    {
        whitePixel = new Texture2D(1, 1);
        whitePixel.SetPixel(0, 0, Color.white);
        whitePixel.Apply();

        darkPixel = new Texture2D(1, 1);
        darkPixel.SetPixel(0, 0, new Color(0.12f, 0.14f, 0.18f, 0.95f));
        darkPixel.Apply();

        // Tạo chất giấy kem ngà ấm áp
        paperTex = new Texture2D(16, 16);
        Color[] paperColors = new Color[16 * 16];
        Color basePaper = new Color(0.96f, 0.94f, 0.88f);
        for (int i = 0; i < paperColors.Length; i++)
        {
            float noise = (UnityEngine.Random.value - 0.5f) * 0.03f;
            paperColors[i] = new Color(basePaper.r + noise, basePaper.g + noise, basePaper.b + noise, 1f);
        }
        paperTex.SetPixels(paperColors);
        paperTex.Apply();
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        bookTitleStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        bookTitleStyle.normal.textColor = new Color(0.45f, 0.35f, 0.25f, 1f);

        polaroidLabelStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Italic,
            wordWrap = true
        };
        polaroidLabelStyle.normal.textColor = new Color(0.3f, 0.25f, 0.2f, 1f);

        diaryHeaderStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        diaryHeaderStyle.normal.textColor = new Color(0.35f, 0.26f, 0.18f, 1f);

        diaryBodyStyle = new GUIStyle
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 13,
            fontStyle = FontStyle.Normal,
            wordWrap = true
        };
        diaryBodyStyle.normal.textColor = new Color(0.24f, 0.2f, 0.16f, 1f);

        pseudocodeStyle = new GUIStyle
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
        pseudocodeStyle.normal.textColor = new Color(0.95f, 0.88f, 0.72f, 1f);

        pageNavStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Italic
        };
        pageNavStyle.normal.textColor = new Color(0.45f, 0.38f, 0.32f, 1f);

        stylesInitialized = true;
    }

    private AudioClip GeneratePageTurnClip()
    {
        int sampleRate = 44100;
        float duration = 0.28f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float noise = (UnityEngine.Random.value * 2f - 1f);
            float envelope = Mathf.Sin(t * Mathf.PI);
            samples[i] = noise * envelope * 0.2f;
        }

        AudioClip clip = AudioClip.Create("Synthesized_PageTurn", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
    #endregion

    #region Context Menus for Testing
    [ContextMenu("Mở Album (Open Album)")]
    public void DebugOpenAlbum() => OpenAlbum();

    [ContextMenu("Đóng Album (Close Album)")]
    public void DebugCloseAlbum() => CloseAlbum();
    #endregion
}
