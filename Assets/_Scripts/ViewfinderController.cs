using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cơ chế ngắm chụp ảnh (Viewfinder Prototype) cho game "A Year in Warmth".
/// - Bấm Space: Mở/Đóng khung ngắm máy ảnh (Viewfinder).
/// - Di chuyển ống kính: Di chuột hoặc WASD để lia góc máy ngắm cảnh; Lăn chuột để Zoom.
/// - Bấm chụp: Nhấp chuột trái (hoặc Enter) để chụp ảnh, có chớp sáng flash và âm thanh màn trập.
/// - Lưu ảnh: Lưu Texture2D và hiển thị tấm ảnh Polaroid xinh xắn ở góc màn hình.
/// </summary>
[DisallowMultipleComponent]
public class ViewfinderController : MonoBehaviour
{
    #region Serialized Fields - Cấu hình Phím & Ống kính
    [Header("=== PHÍM ĐIỀU KHIỂN (KEY BINDINGS) ===")]
    [Tooltip("Phím mở/tắt khung ngắm máy ảnh (mặc định: Phím cách Space).")]
    public KeyCode toggleKey = KeyCode.Space;

    [Tooltip("Phím bấm chụp ảnh (mặc định: Chuột trái hoặc Enter).")]
    public KeyCode captureKey = KeyCode.Mouse0;

    [Tooltip("Phím thoát nhanh khỏi chế độ chụp ảnh.")]
    public KeyCode exitKey = KeyCode.Escape;

    [Header("=== THÔNG SỐ ỐNG KÍNH (LENS & PANNING) ===")]
    [Tooltip("Bán kính tối đa ống kính có thể lia ra xa khỏi nhân vật để ngắm cảnh (units).")]
    [Range(2.0f, 12.0f)]
    public float maxLensPanRadius = 6.0f;

    [Tooltip("Độ nhạy khi lia ống kính theo vị trí chuột.")]
    [Range(1.0f, 10.0f)]
    public float mouseLookSensitivity = 4.5f;

    [Tooltip("Tốc độ lia ống kính khi dùng phím WASD / Mũi tên trong chế độ ngắm.")]
    [Range(2.0f, 12.0f)]
    public float keyboardPanSpeed = 5.0f;

    [Tooltip("Độ mượt mà của ống kính khi lia góc chụp (0.1 - 0.25s).")]
    [Range(0.05f, 0.5f)]
    public float lensSmoothTime = 0.18f;

    [Header("=== THU PHÓNG TIÊU CỰ (ZOOM) ===")]
    [Tooltip("Độ Zoom gần nhất (Camera Orthographic Size nhỏ nhất).")]
    [Range(1.5f, 4.0f)]
    public float minZoom = 2.5f;

    [Tooltip("Độ Zoom xa nhất (Góc rộng - Camera Orthographic Size lớn nhất).")]
    [Range(4.0f, 8.0f)]
    public float maxZoom = 6.0f;

    [Tooltip("Độ nhạy khi lăn con lăn chuột để zoom.")]
    [Range(0.5f, 5.0f)]
    public float zoomSensitivity = 2.0f;

    [Header("=== HIỆU ỨNG MÀN TRẬP & ÂM THANH (SHUTTER & FLASH) ===")]
    [Tooltip("Bật hiệu ứng chớp sáng trắng khi chụp ảnh.")]
    public bool enableFlash = true;

    [Tooltip("Âm thanh màn trập (nếu để trống, script sẽ tự động tạo âm thanh cơ học click-clack).")]
    public AudioClip customShutterSound;

    [Header("=== GIAO DIỆN & TẤM ẢNH GÓC MÀN HÌNH (POLAROID THUMBNAIL) ===")]
    [Tooltip("Tỉ lệ khung ngắm (Rộng / Cao). Mặc định 4:3 chuẩn nhiếp ảnh.")]
    public Vector2 frameAspectRatio = new Vector2(4f, 3f);

    [Tooltip("Hiển thị tấm ảnh Polaroid đã chụp ở góc màn hình.")]
    public bool showCornerThumbnail = true;

    [Tooltip("Kích thước chiều rộng của tấm ảnh nhỏ ở góc màn hình (pixel).")]
    [Range(120f, 260f)]
    public float thumbnailWidth = 180f;
    #endregion

    #region Public Properties & Gallery
    /// <summary> Đang ở trong chế độ ngắm chụp ảnh hay không </summary>
    public bool IsViewfinderActive => isViewfinderActive;

    /// <summary> Bức ảnh Texture2D mới nhất vừa chụp được </summary>
    public Texture2D LatestCapturedPhoto => latestCapturedPhoto;

    /// <summary> Tổng số ảnh đã chụp trong phiên chơi </summary>
    public int PhotoCount => photoGallery.Count;

    /// <summary> Bộ sưu tập tất cả ảnh đã chụp </summary>
    public IReadOnlyList<Texture2D> PhotoGallery => photoGallery;
    #endregion

    #region Private Runtime State
    private PlayerController player;
    private Camera targetCamera;
    private AudioSource audioSource;
    private AudioClip shutterClip;

    private bool isViewfinderActive = false;
    private bool isCapturing = false;

    // Camera & Lens State
    private float defaultCameraSize = 5.0f;
    private float targetZoomSize = 5.0f;
    private Vector2 keyboardPanOffset;
    private Vector2 currentLensOffset;
    private Vector2 lensVelocityRef;

    // Flash & Thumbnail Animations
    private float flashAlpha = 0f;
    private float thumbnailAnimTimer = 0f;
    private bool isEnlargedPreview = false;

    // Photo Storage
    private Texture2D latestCapturedPhoto;
    private readonly List<Texture2D> photoGallery = new List<Texture2D>();

    // GUI Textures & Styles (Procedural)
    private Texture2D whitePixel;
    private Texture2D darkPixel;
    private GUIStyle headerStyle;
    private GUIStyle hintStyle;
    private GUIStyle polaroidLabelStyle;
    private bool stylesInitialized = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 1. Tự động tìm PlayerController
        if (player == null)
        {
            player = GetComponent<PlayerController>() ?? FindAnyObjectByType<PlayerController>();
        }

        // 2. Tự động tìm Main Camera
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null)
        {
            defaultCameraSize = targetCamera.orthographicSize;
            targetZoomSize = defaultCameraSize;
        }

        // 3. Cấu hình AudioSource & Âm thanh màn trập
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        shutterClip = customShutterSound != null ? customShutterSound : GenerateSynthesizedShutterClip();

        // 4. Tạo Texture 1x1 thủ công cho việc vẽ viền và bóng đổ GUI
        whitePixel = new Texture2D(1, 1);
        whitePixel.SetPixel(0, 0, Color.white);
        whitePixel.Apply();

        darkPixel = new Texture2D(1, 1);
        darkPixel.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
        darkPixel.Apply();
    }

    private void Update()
    {
        HandleHotkeys();

        if (isViewfinderActive)
        {
            HandleLensPanningAndZoom();
        }
        else
        {
            // Trả dần camera về kích thước tiêu chuẩn khi đóng khung ngắm
            if (targetCamera != null && Mathf.Abs(targetCamera.orthographicSize - defaultCameraSize) > 0.01f)
            {
                targetCamera.orthographicSize = Mathf.Lerp(targetCamera.orthographicSize, defaultCameraSize, Time.deltaTime * 8f);
            }
        }

        // Làm mờ dần hiệu ứng Flash
        if (flashAlpha > 0f)
        {
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.deltaTime * 4.5f);
        }

        if (thumbnailAnimTimer < 1.0f)
        {
            thumbnailAnimTimer += Time.deltaTime * 2.5f;
        }
    }
    #endregion

    #region Input & Viewfinder Mode Controls
    private void HandleHotkeys()
    {
        // Không mở hoặc thao tác máy ảnh khi đang trong đoạn hội thoại với NPC
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

        // 1. Phím Space: Bật/Tắt chế độ ngắm chụp ảnh
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleViewfinder();
        }

        // 2. Phím ESC: Thoát khỏi chế độ ngắm hoặc đóng ảnh xem phóng to
        if (Input.GetKeyDown(exitKey))
        {
            if (isEnlargedPreview)
            {
                isEnlargedPreview = false;
            }
            else if (isViewfinderActive)
            {
                SetViewfinderActive(false);
            }
        }

        // 3. Phím Chụp ảnh khi đang trong khung ngắm
        if (isViewfinderActive && !isCapturing)
        {
            // Nhấp chuột trái hoặc phím Enter
            if (Input.GetKeyDown(captureKey) || Input.GetKeyDown(KeyCode.Return))
            {
                StartCoroutine(CapturePhotoRoutine());
            }
        }
    }

    /// <summary>
    /// Chuyển đổi trạng thái khung ngắm
    /// </summary>
    public void ToggleViewfinder()
    {
        SetViewfinderActive(!isViewfinderActive);
    }

    /// <summary>
    /// Kích hoạt hoặc thoát khỏi chế độ ngắm chụp ảnh
    /// </summary>
    public void SetViewfinderActive(bool active)
    {
        if (isViewfinderActive == active) return;

        isViewfinderActive = active;

        if (player != null)
        {
            // Khóa/Mở di chuyển nhân vật và camera follow
            player.isInputLocked = isViewfinderActive;
            player.isCameraFollowLocked = isViewfinderActive;
        }

        if (isViewfinderActive)
        {
            // Reset offset lia ống kính về vị trí nhân vật
            keyboardPanOffset = Vector2.zero;
            currentLensOffset = Vector2.zero;
            targetZoomSize = defaultCameraSize;
            isEnlargedPreview = false;
        }
    }
    #endregion

    #region Lens Panning & Zoom Logic
    /// <summary>
    /// Xử lý di chuyển ống kính (lia góc máy quanh cảnh quan và thu phóng tiêu cự)
    /// </summary>
    private void HandleLensPanningAndZoom()
    {
        if (targetCamera == null || player == null) return;

        // 1. Thu phóng ống kính (Zoom In / Zoom Out) bằng con lăn chuột
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetZoomSize = Mathf.Clamp(targetZoomSize - scroll * zoomSensitivity, minZoom, maxZoom);
        }
        targetCamera.orthographicSize = Mathf.Lerp(targetCamera.orthographicSize, targetZoomSize, Time.deltaTime * 10f);

        // 2. Lia ống kính theo bàn phím (WASD / Mũi tên)
        float panH = Input.GetAxisRaw("Horizontal");
        float panV = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(panH) > 0.01f || Mathf.Abs(panV) > 0.01f)
        {
            Vector2 panDir = new Vector2(panH, panV).normalized;
            keyboardPanOffset += panDir * (keyboardPanSpeed * Time.deltaTime);
            keyboardPanOffset = Vector2.ClampMagnitude(keyboardPanOffset, maxLensPanRadius);
        }

        // 3. Lia ống kính nhẹ theo con trỏ chuột (tạo cảm giác người chụp đang xoay máy ngắm nghía)
        Vector2 mousePos = Input.mousePosition;
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 mouseNorm = (mousePos - screenCenter) / (Screen.height * 0.5f);
        Vector2 mousePan = mouseNorm * mouseLookSensitivity;

        // Tổng hợp vector lia ống kính
        Vector2 desiredOffset = Vector2.ClampMagnitude(keyboardPanOffset + mousePan, maxLensPanRadius);

        currentLensOffset = Vector2.SmoothDamp(
            currentLensOffset,
            desiredOffset,
            ref lensVelocityRef,
            lensSmoothTime,
            Mathf.Infinity,
            Time.deltaTime
        );

        // Cập nhật vị trí Camera
        Vector3 playerPos = player.transform.position;
        Vector3 targetCamPos = new Vector3(
            playerPos.x + currentLensOffset.x,
            playerPos.y + currentLensOffset.y,
            targetCamera.transform.position.z
        );

        targetCamera.transform.position = targetCamPos;
    }
    #endregion

    #region Photo Capture Routine
    /// <summary>
    /// Coroutine chụp ảnh: ẩn tạm UI, chụp chính xác vùng khung ngắm, lưu Texture và kích hoạt hiệu ứng
    /// </summary>
    private IEnumerator CapturePhotoRoutine()
    {
        isCapturing = true; // Ẩn UI khung ngắm để bức ảnh trong trẻo, chỉ có phong cảnh

        yield return new WaitForEndOfFrame();

        // 1. Chụp lại toàn bộ màn hình
        Texture2D fullScreenshot = ScreenCapture.CaptureScreenshotAsTexture();

        // 2. Tính toán chính xác toạ độ vùng khung ngắm (Viewfinder Rect)
        Rect viewRect = GetViewfinderScreenRect();

        int cropX = Mathf.Clamp(Mathf.RoundToInt(viewRect.x), 0, fullScreenshot.width - 1);
        // Lưu ý đảo trục Y vì GUI tính từ trên xuống, Texture tính từ dưới lên
        int cropY = Mathf.Clamp(Mathf.RoundToInt(Screen.height - (viewRect.y + viewRect.height)), 0, fullScreenshot.height - 1);
        int cropW = Mathf.Clamp(Mathf.RoundToInt(viewRect.width), 1, fullScreenshot.width - cropX);
        int cropH = Mathf.Clamp(Mathf.RoundToInt(viewRect.height), 1, fullScreenshot.height - cropY);

        // 3. Cắt lấy vùng ảnh bên trong khung ngắm
        Color[] pixels = fullScreenshot.GetPixels(cropX, cropY, cropW, cropH);
        Texture2D croppedPhoto = new Texture2D(cropW, cropH, TextureFormat.RGB24, false);
        croppedPhoto.SetPixels(pixels);
        croppedPhoto.Apply();

        Destroy(fullScreenshot); // Giải phóng texture tạm

        // 4. Lưu vào biến mới nhất và thư viện
        latestCapturedPhoto = croppedPhoto;
        photoGallery.Add(croppedPhoto);

        // 5. Kích hoạt hiệu ứng Flash, âm thanh màn trập và hoạt ảnh ảnh góc
        flashAlpha = 1.0f;
        thumbnailAnimTimer = 0f;
        PlayShutterSound();

        isCapturing = false;
        Debug.Log($"[Viewfinder] 📸 Đã chụp thành công bức ảnh #{photoGallery.Count} ({cropW}x{cropH})!");
    }

    private void PlayShutterSound()
    {
        if (audioSource != null && shutterClip != null)
        {
            audioSource.pitch = Random.Range(0.97f, 1.03f);
            audioSource.PlayOneShot(shutterClip, 0.9f);
        }
    }
    #endregion

    #region GUI Drawing - Viewfinder & Polaroid Thumbnail
    private void OnGUI()
    {
        InitGUIStyles();

        // 1. Vẽ Khung ngắm máy ảnh (chỉ khi đang bật và không ở giữa frame chụp)
        if (isViewfinderActive && !isCapturing)
        {
            DrawViewfinderOverlay();
        }

        // 2. Vẽ Hiệu ứng chớp sáng trắng khi chụp
        if (flashAlpha > 0.005f)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, flashAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whitePixel);
            GUI.color = oldColor;
        }

        // 3. Vẽ Tấm ảnh nhỏ Polaroid ở góc màn hình
        if (showCornerThumbnail && latestCapturedPhoto != null && !isCapturing)
        {
            DrawPolaroidThumbnail();
        }

        // 4. Nếu người dùng bấm xem ảnh phóng to
        if (isEnlargedPreview && latestCapturedPhoto != null)
        {
            DrawEnlargedPhoto();
        }
    }

    /// <summary>
    /// Vẽ lớp phủ khung ngắm (Letterbox, 4 góc ngắm, lưới bố cục 1/3, thông số ống kính)
    /// </summary>
    private void DrawViewfinderOverlay()
    {
        Rect box = GetViewfinderScreenRect();

        Color oldColor = GUI.color;

        // A. Làm tối vùng ngoài khung ngắm (Letterbox mờ điện ảnh)
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, box.y), darkPixel); // Top
        GUI.DrawTexture(new Rect(0, box.yMax, Screen.width, Screen.height - box.yMax), darkPixel); // Bottom
        GUI.DrawTexture(new Rect(0, box.y, box.x, box.height), darkPixel); // Left
        GUI.DrawTexture(new Rect(box.xMax, box.y, Screen.width - box.xMax, box.height), darkPixel); // Right

        // B. Vẽ 4 góc ngắm máy ảnh [ ┌ ┐ └ ┘ ]
        GUI.color = new Color(1f, 1f, 1f, 0.9f);
        float cornerLen = 28f;
        float lineThick = 3f;

        // Góc trên - trái
        DrawRect(new Rect(box.x, box.y, cornerLen, lineThick));
        DrawRect(new Rect(box.x, box.y, lineThick, cornerLen));

        // Góc trên - phải
        DrawRect(new Rect(box.xMax - cornerLen, box.y, cornerLen, lineThick));
        DrawRect(new Rect(box.xMax - lineThick, box.y, lineThick, cornerLen));

        // Góc dưới - trái
        DrawRect(new Rect(box.x, box.yMax - lineThick, cornerLen, lineThick));
        DrawRect(new Rect(box.x, box.yMax - cornerLen, lineThick, cornerLen));

        // Góc dưới - phải
        DrawRect(new Rect(box.xMax - cornerLen, box.yMax - lineThick, cornerLen, lineThick));
        DrawRect(new Rect(box.xMax - lineThick, box.yMax - cornerLen, lineThick, cornerLen));

        // C. Vẽ tâm ngắm trung tâm [+]
        float crossSize = 14f;
        Vector2 center = box.center;
        DrawRect(new Rect(center.x - crossSize, center.y - 1f, crossSize * 2, 2f));
        DrawRect(new Rect(center.x - 1f, center.y - crossSize, 2f, crossSize * 2));

        // D. Vẽ lưới bố cục 1/3 nhẹ nhàng
        GUI.color = new Color(1f, 1f, 1f, 0.15f);
        float thirdW = box.width / 3f;
        float thirdH = box.height / 3f;
        DrawRect(new Rect(box.x + thirdW, box.y, 1f, box.height));
        DrawRect(new Rect(box.x + thirdW * 2, box.y, 1f, box.height));
        DrawRect(new Rect(box.x, box.y + thirdH, box.width, 1f));
        DrawRect(new Rect(box.x, box.y + thirdH * 2, box.width, 1f));

        // E. Thanh thông tin máy ảnh (Camera HUD)
        GUI.color = Color.white;
        float zoomRatio = defaultCameraSize / Mathf.Max(targetCamera.orthographicSize, 0.1f);

        // Header trên
        string topText = $"🔴 PHOTO MODE   |   ZOOM: {zoomRatio:F1}x   |   ISO 100   F/2.8";
        GUI.Label(new Rect(box.x + 8, box.y - 28, box.width, 24), topText, headerStyle);

        // Hướng dẫn phím bấm dưới
        string bottomText = $"[CHUỘT TRÁI]: CHỤP ẢNH   |   [CUỘN CHUỘT]: ZOOM   |   [WASD/CHUỘT]: LIA ỐNG KÍNH   |   [{toggleKey} / ESC]: ĐÓNG";
        GUI.Label(new Rect(box.x, box.yMax + 10, box.width, 26), bottomText, hintStyle);

        GUI.color = oldColor;
    }

    /// <summary>
    /// Vẽ tấm ảnh Polaroid nhỏ xinh ở góc dưới màn hình
    /// </summary>
    private void DrawPolaroidThumbnail()
    {
        // Hiệu ứng nảy vào (Spring Bounce Animation) khi vừa chụp
        float animScale = 1.0f;
        if (thumbnailAnimTimer < 1.0f)
        {
            float t = thumbnailAnimTimer;
            animScale = 0.7f + 0.3f * Mathf.Sin(t * Mathf.PI * 0.5f);
        }

        float cardW = thumbnailWidth * animScale;
        float cardH = (thumbnailWidth * 1.25f) * animScale;
        float margin = 20f;

        Rect cardRect = new Rect(Screen.width - cardW - margin, Screen.height - cardH - margin, cardW, cardH);

        Color oldColor = GUI.color;

        // 1. Bóng đổ nhẹ dưới tấm ảnh
        GUI.color = new Color(0f, 0f, 0f, 0.35f);
        GUI.DrawTexture(new Rect(cardRect.x + 4, cardRect.y + 4, cardRect.width, cardRect.height), darkPixel);

        // 2. Viền trắng Polaroid
        GUI.color = new Color(0.98f, 0.97f, 0.94f, 1f);
        GUI.DrawTexture(cardRect, whitePixel);

        // 3. Khung viền mỏng xung quanh
        GUI.color = new Color(0.85f, 0.82f, 0.78f, 1f);
        DrawFrameBorders(cardRect, 1.5f);

        // 4. Bức ảnh Texture2D bên trong
        float photoMargin = 10f * animScale;
        float photoW = cardW - (photoMargin * 2);
        float photoH = photoW * (frameAspectRatio.y / frameAspectRatio.x);
        Rect photoRect = new Rect(cardRect.x + photoMargin, cardRect.y + photoMargin, photoW, photoH);

        GUI.color = Color.white;
        GUI.DrawTexture(photoRect, latestCapturedPhoto, ScaleMode.ScaleAndCrop);

        // 5. Chữ viết tay nhỏ ở chân tấm ảnh Polaroid
        float labelH = cardH - photoH - (photoMargin * 2);
        Rect labelRect = new Rect(cardRect.x, photoRect.yMax + 2, cardRect.width, labelH);
        GUI.Label(labelRect, $"A Year in Warmth  #{photoGallery.Count:D2}", polaroidLabelStyle);

        // 6. Nhấp chuột vào ảnh nhỏ để xem phóng to
        if (Event.current.type == EventType.MouseDown && cardRect.Contains(Event.current.mousePosition))
        {
            isEnlargedPreview = !isEnlargedPreview;
            Event.current.Use();
        }

        GUI.color = oldColor;
    }

    /// <summary>
    /// Vẽ ảnh phóng to giữa màn hình khi người chơi nhấp vào tấm ảnh ở góc
    /// </summary>
    private void DrawEnlargedPhoto()
    {
        // Lớp nền đen mờ
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), darkPixel);

        // Khung ảnh phóng to ở trung tâm
        float maxW = Screen.width * 0.7f;
        float maxH = Screen.height * 0.75f;
        float picW = maxW;
        float picH = picW * (frameAspectRatio.y / frameAspectRatio.x);

        if (picH > maxH)
        {
            picH = maxH;
            picW = picH * (frameAspectRatio.x / frameAspectRatio.y);
        }

        Rect centerCard = new Rect((Screen.width - picW) * 0.5f, (Screen.height - picH) * 0.5f - 20, picW, picH + 50);

        // Khung trắng Polaroid
        GUI.color = new Color(0.98f, 0.97f, 0.94f, 1f);
        GUI.DrawTexture(centerCard, whitePixel);

        // Ảnh
        Rect innerPic = new Rect(centerCard.x + 14, centerCard.y + 14, centerCard.width - 28, centerCard.height - 64);
        GUI.color = Color.white;
        GUI.DrawTexture(innerPic, latestCapturedPhoto, ScaleMode.ScaleAndCrop);

        // Chú thích
        Rect bigLabelRect = new Rect(centerCard.x, innerPic.yMax + 4, centerCard.width, 40);
        GUIStyle bigLabelStyle = new GUIStyle(polaroidLabelStyle) { fontSize = 16 };
        GUI.Label(bigLabelRect, $"✨ Bức ảnh #{photoGallery.Count} - [Nhấp chuột hoặc ESC để đóng]", bigLabelStyle);

        // Nhấp ra ngoài để đóng
        if (Event.current.type == EventType.MouseDown)
        {
            isEnlargedPreview = false;
            Event.current.Use();
        }

        GUI.color = oldColor;
    }

    /// <summary>
    /// Tính toán hình chữ nhật của khung ngắm chính giữa màn hình
    /// </summary>
    public Rect GetViewfinderScreenRect()
    {
        float targetW = Screen.width * 0.65f;
        float targetH = targetW * (frameAspectRatio.y / frameAspectRatio.x);

        // Giới hạn không vượt quá 75% chiều cao màn hình
        if (targetH > Screen.height * 0.75f)
        {
            targetH = Screen.height * 0.75f;
            targetW = targetH * (frameAspectRatio.x / frameAspectRatio.y);
        }

        float x = (Screen.width - targetW) * 0.5f;
        float y = (Screen.height - targetH) * 0.5f;

        return new Rect(x, y, targetW, targetH);
    }

    private void DrawRect(Rect r)
    {
        GUI.DrawTexture(r, whitePixel);
    }

    private void DrawFrameBorders(Rect r, float thickness)
    {
        DrawRect(new Rect(r.x, r.y, r.width, thickness));
        DrawRect(new Rect(r.x, r.yMax - thickness, r.width, thickness));
        DrawRect(new Rect(r.x, r.y, thickness, r.height));
        DrawRect(new Rect(r.xMax - thickness, r.y, thickness, r.height));
    }
    #endregion

    #region Helpers & Audio Synthesis
    private void InitGUIStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        headerStyle.normal.textColor = new Color(1f, 0.88f, 0.45f, 0.95f);

        hintStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Normal
        };
        hintStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f, 0.85f);

        polaroidLabelStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11,
            fontStyle = FontStyle.Italic
        };
        polaroidLabelStyle.normal.textColor = new Color(0.35f, 0.32f, 0.28f, 0.9f);

        stylesInitialized = true;
    }

    /// <summary>
    /// Tự động tổng hợp âm thanh màn trập máy ảnh cơ học (click-clack) nếu người dùng chưa có file audio
    /// </summary>
    private AudioClip GenerateSynthesizedShutterClip()
    {
        int sampleRate = 44100;
        float duration = 0.24f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        int click1Index = 0;
        int click2Index = Mathf.RoundToInt(sampleRate * 0.085f);

        for (int i = 0; i < sampleCount; i++)
        {
            float val = 0f;

            // Xung nhịp 1: Mở màn trập
            int d1 = i - click1Index;
            if (d1 >= 0 && d1 < 900)
            {
                float env = 1f - (float)d1 / 900f;
                val += Mathf.Sin(d1 * 0.55f) * env * 0.75f;
                val += (Random.value * 2f - 1f) * env * 0.25f; // Nhiễu cơ học
            }

            // Xung nhịp 2: Đóng màn trập
            int d2 = i - click2Index;
            if (d2 >= 0 && d2 < 1400)
            {
                float env = 1f - (float)d2 / 1400f;
                val += Mathf.Sin(d2 * 0.4f) * env * 0.85f;
                val += (Random.value * 2f - 1f) * env * 0.2f;
            }

            samples[i] = Mathf.Clamp(val, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Synthesized_ShutterClick", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
    #endregion
}
