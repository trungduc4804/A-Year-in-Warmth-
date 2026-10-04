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
    private Texture2D banhChungIllustrationTex;
    private Texture2D liXiIllustrationTex;
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
    /// Chuyển trực tiếp đến trang cụ thể
    /// </summary>
    public void GoToPage(int pageIndex)
    {
        currentPageIndex = Mathf.Clamp(pageIndex, 0, TotalPages - 1);
        PlayPageTurnSound();
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
            case 0: // Trang bìa đầu tiên của Chương 1: Cành Đào Phai của Bác An
                if (GameManager.Instance != null && GameManager.Instance.QuestPhoto != null)
                {
                    return GameManager.Instance.QuestPhoto;
                }
                if (ViewfinderController.Instance != null && ViewfinderController.Instance.LatestCapturedPhoto != null)
                {
                    return ViewfinderController.Instance.LatestCapturedPhoto;
                }
                return null;

            case 1: // Trang 2: Chiếc Bánh Chưng Xanh / Nồi Bánh Chưng Luộc Đêm 29 Tết
                bool hasWrapped = (GameManager.Instance != null && GameManager.Instance.BanhChungCount > 0) ||
                                  (BanhChungMinigame.Instance != null && BanhChungMinigame.Instance.HasCompletedAny);
                if (hasWrapped)
                {
                    if (banhChungIllustrationTex == null) banhChungIllustrationTex = GenerateBanhChungKeepsakeTexture();
                    return banhChungIllustrationTex;
                }
                return null;

            case 2: // Trang 3: The Perfect Shot & Phong Bao Lì Xì Đỏ "Bình An"
                if (GameManager.Instance != null && GameManager.Instance.NewYearEvePhoto != null)
                {
                    return GameManager.Instance.NewYearEvePhoto;
                }
                if (GameManager.Instance != null && GameManager.Instance.HasLiXiKeepsake)
                {
                    if (liXiIllustrationTex == null) liXiIllustrationTex = GenerateLiXiKeepsakeTexture();
                    return liXiIllustrationTex;
                }
                if (ViewfinderController.Instance != null && ViewfinderController.Instance.LatestCapturedPhoto != null && GameManager.Instance != null && GameManager.Instance.CurrentQuestState >= QuestState.NewYearEvePhotoTaken)
                {
                    return ViewfinderController.Instance.LatestCapturedPhoto;
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
                bool hasPeachPhoto = (GameManager.Instance != null && GameManager.Instance.QuestPhoto != null) ||
                                     (ViewfinderController.Instance != null && ViewfinderController.Instance.LatestCapturedPhoto != null);
                if (hasPeachPhoto)
                {
                    return "Hà Nội, chiều 29 Tết.\n" +
                           "Ngày 29 Tết - Cành đào đầu tiên của bác An.\n" +
                           "Gió bấc rít qua từng kẽ lá, nhưng vừa bước tới trước hiên nhà Bác An, sắc hồng phai đầu tiên đã sưởi ấm cả khoảng sân nhỏ. Từng cánh hoa mỏng manh khẽ rung rinh trong nắng sớm... Bác An nâng niu nhìn bức ảnh này mà rơm rớm khóe mắt. Mùa xuân thực sự đã về rồi.";
                }
                else
                {
                    return "Hà Nội, chiều 29 Tết.\n" +
                           "Bác An nhờ mình chụp lại cành đào phai chớm nở trước hiên nhà. Mình phải lấy máy ảnh [Phím Space] ngắm chụp để lưu lại trang bìa Chương 1 này mới được...";
                }

            case 1:
                bool hasBoiled = (GameManager.Instance != null && GameManager.Instance.HasBoiledBanhChung) ||
                                 (BanhChungBoilingMinigame.Instance != null && BanhChungBoilingMinigame.Instance.HasCompleted);
                bool hasWrapped = (GameManager.Instance != null && GameManager.Instance.BanhChungCount > 0) ||
                                  (BanhChungMinigame.Instance != null && BanhChungMinigame.Instance.HasCompletedAny);

                if (hasBoiled)
                {
                    int count = GameManager.Instance != null ? GameManager.Instance.BanhChungCount : 1;
                    return "Hà Nội, đêm 30 Tết bên nồi bánh chưng nghi ngút khói.\n" +
                           "Gió đông bắc luồn qua ngõ rêu, nhưng ngồi bên bếp củi than hồng, đôi tay già nua của mình chưa bao giờ thấy ấm áp đến thế.\n" +
                           $"Từng thanh củi khô cời vào bếp, từng ấm nước sôi châm thêm... Sau nhiều giờ canh lửa, mẻ bánh chưng ({count} chiếc) đã chín thơm lừng khắp góc sân! " +
                           "Bác An bưng ra chén trà sen ấm, nụ cười hiền hậu báo hiệu một mùa xuân mới an lành đã thực sự cận kề.";
                }
                else if (hasWrapped)
                {
                    int count = GameManager.Instance != null ? GameManager.Instance.BanhChungCount : 1;
                    return "Hà Nội, đêm 29 Tết bên bếp lửa hồng.\n" +
                           "Manh chiếu cói trải bên thềm, thoang thoảng mùi lá dong xanh, gạo nếp cái hoa vàng và thịt mỡ ướp tiêu thơm nồng.\n" +
                           $"Tự tay kéo từng lớp lá dong, rải nếp, đặt thịt rồi gói vuông vức... Chiếc bánh chưng đầu tiên đã hoàn thành và nằm gọn trong túi đồ kỷ niệm (hiện có: {count} chiếc), sẵn sàng cho nồi luộc bốc khói rực ánh lửa hồng đêm nay!";
                }
                else
                {
                    return "Hà Nội, ngày 29 Tết.\n" +
                           "Bên hiên nhà, Bác An đã chuẩn bị sẵn lá dong xanh mướt, thúng gạo nếp cái hoa vàng và thịt mỡ ướp tiêu đậm đà.\n" +
                           "Bác bảo: 'Tết này cháu hãy tự tay gói thử một chiếc bánh chưng vuông vức xem sao nhé!'. Mình nhất định sẽ lại manh chiếu bên hiên nhà [Phím E hoặc B] trổ tài ngay...";
                }

            case 2:
                bool hasMidnightCompleted = (GameManager.Instance != null && (GameManager.Instance.HasLiXiKeepsake || GameManager.Instance.CurrentQuestState >= QuestState.NewYearEvePhotoTaken));
                if (hasMidnightCompleted)
                {
                    return "Hà Nội, thời khắc 00:00 Giao Thừa.\n" +
                           "\"The Perfect Shot\" - Giây phút chuyển giao năm mới thiêng liêng.\n" +
                           "Tiếng pháo hoa rền vang từ phía hồ Gươm, ánh sáng lung linh hắt qua ô cửa sổ cổ kính soi bóng Bác An đang chắp tay thành kính trước bàn thờ tổ tiên. " +
                           "Chiếc phong bao lì xì đỏ thắm mang chữ \"Bình An\" Bác trao tặng như sưởi ấm trọn vẹn trái tim người lữ khách phương xa sau bao năm bôn ba...";
                }
                else
                {
                    return "Hà Nội, đêm 30 Tết.\n" +
                           "Thời khắc Giao Thừa 00:00 đang đến rất gần. Bác An sẽ thắp nén nhang trầm đầu năm khi tiếng pháo hoa nổ vang trên bầu trời thủ đô.\n" +
                           "Hãy sẵn sàng máy ảnh [Phím Space] để bắt trọn 'The Perfect Shot' của Chương 1!";
                }

            default:
                return "";
        }
    }

    public string GetCurrentPagePseudocode()
    {
        switch (currentPageIndex)
        {
            case 0:
                bool completed = (GameManager.Instance != null && GameManager.Instance.CurrentQuestState >= QuestState.PhotoTaken) ||
                                 (ViewfinderController.Instance != null && ViewfinderController.Instance.LatestCapturedPhoto != null);
                return "// Arthur's Heart.log()\n" +
                       "Memory peachBlossom = new Memory();\n" +
                       "peachBlossom.Title = \"Cành đào đầu tiên của bác An\";\n" +
                       "peachBlossom.Date = \"Ngày 29 Tết Giáp Thìn\";\n" +
                       "peachBlossom.Location = \"Hiên nhà Bác An\";\n" +
                       $"peachBlossom.Captured = {completed.ToString().ToLower()};\n" +
                       "Heart.Save(peachBlossom);";

            case 1:
                bool hasBoiledCode = (GameManager.Instance != null && GameManager.Instance.HasBoiledBanhChung) ||
                                     (BanhChungBoilingMinigame.Instance != null && BanhChungBoilingMinigame.Instance.HasCompleted);
                bool hasWrappedCode = (GameManager.Instance != null && GameManager.Instance.BanhChungCount > 0) ||
                                      (BanhChungMinigame.Instance != null && BanhChungMinigame.Instance.HasCompletedAny);

                if (hasBoiledCode)
                {
                    return "// Memory_01.cpp - Hanoi Tet Night\n" +
                           "void Tet_Nguyen_Dan() {\n" +
                           "    Heart.Warmth += WoodFire.Embers;\n" +
                           "    Heart.Peace += BacAn.LotusTea;\n" +
                           "    BanhChung.State = PerfectlyBoiled;\n" +
                           "    Worries.Clear();\n" +
                           "    IsLonely = false;\n" +
                           "}";
                }
                else if (hasWrappedCode)
                {
                    return "// Recipe.Craft()\n" +
                           "BanhChung myBanh = new BanhChung();\n" +
                           "myBanh.Ingredients = [ DongLeaf, StickyRice, Pork ];\n" +
                           "myBanh.FoldTight();\n" +
                           "myBanh.TieCrossLats();\n" +
                           "Inventory.Add(myBanh); // 1x Bánh Chưng Tết\n" +
                           "Soul.State = PureWarmth;";
                }
                else
                {
                    return "// Quest.Pending()\n" +
                           "if (Arthur.WalkTo(BanhChungMat)) {\n" +
                           "    Arthur.DragAndDropLayers();\n" +
                           "    Inventory.Add(BanhChung);\n" +
                           "}";
                }

            case 2:
                bool completedCh1 = (GameManager.Instance != null && GameManager.Instance.CurrentQuestState == QuestState.Chapter1Complete);
                return "// Chapter1_Hanoi_Tet.Finalize()\n" +
                       "void Midnight_00_00() {\n" +
                       "    Fireworks.SoundEcho(\"Ho Guom\");\n" +
                       "    BacAn.PrayFor(\"Binh An\");\n" +
                       "    Arthur.CapturePerfectShot();\n" +
                       "    Inventory.Add(RedEnvelope_BinhAn);\n" +
                       "    Heart.Warmth = MaxWarmth;\n" +
                       "    Heart.IsLonely = false;\n" +
                       $"    Chapter1.IsCompleted = {completedCh1.ToString().ToLower()};\n" +
                       "}";

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
        string leftPageTitle = (currentPageIndex == 0) ? "CHƯƠNG 1: TẾT ẤM ÁP NƠI PHỐ CỔ" :
                               (currentPageIndex == 1) ? "KỶ NIỆM: GÓI BÁNH CHƯNG TẾT" : "CHƯƠNG 1: THE PERFECT SHOT";
        GUI.Label(new Rect(pageRect.x, pageRect.y, pageRect.width, 24 * scale), leftPageTitle, bookTitleStyle);

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
        GUI.color = (currentPageIndex == 1) ? new Color(0.35f, 0.65f, 0.40f, 0.85f * openAnimProgress) :
                    (currentPageIndex == 2) ? new Color(0.85f, 0.22f, 0.20f, 0.85f * openAnimProgress) :
                    new Color(0.82f, 0.62f, 0.55f, 0.85f * openAnimProgress);
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

            // DÒNG GHI CHÚ VIẾT TAY DƯỚI ẢNH (Ví dụ: "Ngày 29 Tết - Cành đào đầu tiên của bác An")
            GUI.color = new Color(0.28f, 0.24f, 0.2f, openAnimProgress);
            Rect captionRect = new Rect(cardRect.x + 8, photoRect.yMax + 6, cardRect.width - 16, cardRect.yMax - photoRect.yMax - 10);
            string photoCaption = (currentPageIndex == 0)
                ? "Ngày 29 Tết - Cành đào đầu tiên của bác An"
                : (currentPageIndex == 1) ? "Ngày 29 Tết - Chiếc Bánh Chưng đầu tiên của Arthur" : "Giao Thừa 00:00 - Bác An thắp nhang cầu Bình An";
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
                ? "📷 [Chưa có ảnh]\nHãy mở máy ảnh [Space] chụp cành đào phai\nđể ghim trang trọng vào trang bìa này!"
                : (currentPageIndex == 1)
                ? "🎋 [Mẩu Kỷ Niệm Đang Chờ]\nHãy đến manh chiếu bên cạnh Bác An\nbấm [E] hoặc [B] để tự tay gói bánh chưng!"
                : "🎆 [The Perfect Shot Đang Chờ]\nKhi tiếng pháo hoa Giao thừa nổ vang,\nhãy mở máy ảnh [Space] chụp Bác An thắp nhang\nđể nhận Phong Bao Lì Xì và hoàn thành Chương 1!";
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
        string headerTitle = (currentPageIndex == 0) ? "KÝ ỨC #01: CÀNH ĐÀO ĐẦU TIÊN CỦA BÁC AN" :
                             (currentPageIndex == 1) ? "KÝ ỨC #02: NỒI BÁNH CHƯNG ĐÊM 29 TẾT" : "KÝ ỨC #03: GIAO THỪA 00:00 & THE PERFECT SHOT";
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

        // Tạo minh hoạ ảnh chụp chiếc Bánh Chưng xanh thu hoạch được
        banhChungIllustrationTex = GenerateBanhChungKeepsakeTexture();
    }

    private Texture2D GenerateBanhChungKeepsakeTexture()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color woodBg = new Color(0.85f, 0.76f, 0.62f, 1f);
        Color shadow = new Color(0.35f, 0.28f, 0.22f, 0.5f);
        Color leafBase = new Color(0.22f, 0.55f, 0.25f, 1f);
        Color leafDark = new Color(0.16f, 0.44f, 0.19f, 1f);
        Color latGiang = new Color(0.96f, 0.88f, 0.52f, 1f);
        Color redPaper = new Color(0.85f, 0.22f, 0.18f, 1f);
        Color goldDot = new Color(0.98f, 0.85f, 0.35f, 1f);

        int minB = 22;
        int maxB = 106;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = y * size + x;
                cols[idx] = ((x + y) % 6 == 0) ? new Color(0.80f, 0.72f, 0.58f, 1f) : woodBg;

                // Bóng đổ
                if (x >= minB + 6 && x <= maxB + 6 && y >= minB - 6 && y <= maxB - 6)
                {
                    cols[idx] = Color.Lerp(cols[idx], shadow, 0.5f);
                }

                // Chiếc bánh chưng vuông
                if (x >= minB && x <= maxB && y >= minB && y <= maxB)
                {
                    bool isBorder = (x == minB || x == maxB || y == minB || y == maxB);
                    bool isFold = (x - minB == y - minB) || (x - minB == maxB - y);
                    cols[idx] = (isBorder || isFold) ? leafDark : leafBase;

                    bool isVerticalLat = (x >= 46 && x <= 50) || (x >= 78 && x <= 82);
                    bool isHorizontalLat = (y >= 46 && y <= 50) || (y >= 78 && y <= 82);
                    if (isVerticalLat || isHorizontalLat)
                    {
                        cols[idx] = latGiang;
                    }

                    if (x >= 54 && x <= 74 && y >= 54 && y <= 74)
                    {
                        cols[idx] = redPaper;
                        if (x >= 62 && x <= 66 && y >= 62 && y <= 66)
                        {
                            cols[idx] = goldDot;
                        }
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    private Texture2D GenerateLiXiKeepsakeTexture()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] cols = new Color[size * size];

        Color parchmentBg = new Color(0.92f, 0.88f, 0.82f, 1f);
        Color shadow = new Color(0.2f, 0.15f, 0.12f, 0.45f);
        Color envRed = new Color(0.85f, 0.18f, 0.15f, 1f);
        Color flapDarkRed = new Color(0.70f, 0.12f, 0.10f, 1f);
        Color goldTrim = new Color(0.98f, 0.85f, 0.35f, 1f);
        Color goldEmblem = new Color(1.0f, 0.92f, 0.55f, 1f);

        int minX = 36;
        int maxX = 92;
        int minY = 18;
        int maxY = 110;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = y * size + x;
                cols[idx] = parchmentBg;

                // Bóng đổ
                if (x >= minX + 5 && x <= maxX + 5 && y >= minY - 5 && y <= maxY - 5)
                {
                    cols[idx] = Color.Lerp(cols[idx], shadow, 0.55f);
                }

                // Thân bao lì xì đỏ
                if (x >= minX && x <= maxX && y >= minY && y <= maxY)
                {
                    cols[idx] = envRed;

                    // Viền vàng kim 2px
                    bool isBorder = (x == minX || x == minX + 1 || x == maxX || x == maxX - 1 ||
                                     y == minY || y == minY + 1 || y == maxY || y == maxY - 1);
                    if (isBorder)
                    {
                        cols[idx] = goldTrim;
                    }

                    // Nắp gập tam giác phía trên
                    if (y >= maxY - 20)
                    {
                        cols[idx] = flapDarkRed;
                        if (y == maxY - 20 || y == maxY - 19)
                        {
                            cols[idx] = goldTrim;
                        }
                    }

                    // Hình thoi vàng dát kim chữ Bình An ở giữa
                    int midX = 64;
                    int midY = 56;
                    int dist = Mathf.Abs(x - midX) + Mathf.Abs(y - midY);
                    if (dist <= 16)
                    {
                        cols[idx] = (dist >= 14) ? goldTrim : flapDarkRed;
                        // Tâm chữ vàng
                        if (dist <= 6)
                        {
                            cols[idx] = goldEmblem;
                        }
                    }
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
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
