using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minigame Pha 2: Canh Nồi Bánh Chưng Đêm 30 Tết (Bánh Chưng Boiling Minigame).
/// Thiết kế bám sát Mục 4.1 trong Game Design Document (GDD):
/// - Bối cảnh đêm 30 Tết se lạnh, ngọn lửa than củi ấm rực, nồi luộc bánh bốc hơi sùng sục.
/// - Quản lý 2 thanh trạng thái tương tác:
///     1. Nhiệt độ lửa (Fire Temp): Giảm dần theo gió lạnh -> Bấm [F] hoặc Click để Thêm Củi Khô.
///     2. Mực nước sôi (Water Level): Cạn dần do bốc hơi -> Bấm [W] hoặc Click để Châm Nước Sôi.
/// - Xen kẽ là các đoạn tự sự nội tâm của Arthur về sự cô đơn ở trời Âu đối lập với sự sum vầy quanh bếp lửa Việt Nam.
/// - Hoàn thành: Tăng điểm Gắn kết Bản địa (Cultural Affinity Meter) và mở khóa ký ức nồi bánh luộc trong Album!
/// </summary>
[DisallowMultipleComponent]
public class BanhChungBoilingMinigame : MonoBehaviour
{
    public static BanhChungBoilingMinigame Instance { get; private set; }

    #region Serialized Fields
    [Header("=== PHÍM TẮT & ĐIỀU KHIỂN ===")]
    [Tooltip("Phím thêm củi khô vào đáy nồi (mặc định: Phím F).")]
    public KeyCode addFirewoodKey = KeyCode.F;

    [Tooltip("Phím châm thêm nước sôi từ ấm đồng (mặc định: Phím W).")]
    public KeyCode addWaterKey = KeyCode.W;

    [Tooltip("Phím đóng / tạm dừng minigame.")]
    public KeyCode closeKey = KeyCode.Escape;

    [Header("=== THỜI LƯỢNG & TỐC ĐỘ BIẾN ĐỘNG ===")]
    [Tooltip("Thời gian luộc hoàn thành một mẻ bánh (giây). Khuyên dùng: 45 - 60s để trải nghiệm đủ 5 câu tự sự.")]
    [Range(20f, 90f)]
    public float boilDuration = 48f;

    [Tooltip("Tốc độ hạ nhiệt độ lửa (%/giây) do gió đông bắc thổi.")]
    [Range(2f, 10f)]
    public float fireDecayRate = 4.2f;

    [Tooltip("Lượng nhiệt tăng thêm mỗi lần thêm một thanh củi khô (%).")]
    [Range(8f, 25f)]
    public float firewoodHeatBonus = 14f;

    [Tooltip("Tốc độ bốc hơi nước cơ bản (%/giây).")]
    [Range(1f, 6f)]
    public float waterEvapBaseRate = 2.4f;

    [Tooltip("Lượng nước châm thêm mỗi lần rót từ ấm sôi (%).")]
    [Range(10f, 30f)]
    public float waterRefillBonus = 18f;

    [Header("=== ÂM THANH MINIGAME (TỰ TỔNG HỢP NẾU TRỐNG) ===")]
    public AudioClip customFireCrackleSound;
    public AudioClip customWaterPourSound;
    public AudioClip customSuccessFanfare;
    #endregion

    #region Public Properties
    /// <summary> Minigame đang mở hay đóng </summary>
    public bool IsOpen => isMinigameOpen;

    /// <summary> Tiến độ luộc chín nồi bánh (0.0 đến 1.0) </summary>
    public float Progress => boilDuration > 0f ? Mathf.Clamp01(currentTimer / boilDuration) : 1f;

    /// <summary> Nhiệt độ ngọn lửa hiện tại (0 đến 100%) </summary>
    public float FireTemperature => fireTemperature;

    /// <summary> Mực nước sôi trong nồi hiện tại (0 đến 100%) </summary>
    public float WaterLevel => waterLevel;

    /// <summary> Đã hoàn thành canh chín nồi bánh ít nhất 1 lần </summary>
    public bool HasCompleted => hasCompleted;
    #endregion

    #region Private State
    private PlayerController player;
    private AudioSource audioSource;
    private AudioSource ambientLoopSource;

    private AudioClip fireCrackleClip;
    private AudioClip waterPourClip;
    private AudioClip bubbleLoopClip;
    private AudioClip successClip;

    private bool isMinigameOpen = false;
    private bool isCompletedThisSession = false;
    private bool hasCompleted = false;

    private float currentTimer = 0f;
    private float fireTemperature = 72f; // 0 - 100
    private float waterLevel = 88f;      // 0 - 100

    // Vùng lý tưởng (Sweet Spot)
    private const float FireSweetMin = 50f;
    private const float FireSweetMax = 85f;
    private const float WaterSafeMin = 35f;

    // Phản hồi tương tác
    private string actionFeedback = "Giữ ngọn lửa ở vùng VÀNG CAM và đừng để nước cạn dưới vạch XANH nhé!";
    private float feedbackTimer = 4.0f;

    // Hiệu ứng hình ảnh
    private float flameAnimTimer = 0f;
    private float steamAnimTimer = 0f;
    private float finishSparkleTimer = 0f;

    // GUI Textures & Styles
    private Texture2D whitePixel;
    private Texture2D darkPixel;
    private Texture2D vignetteTex;
    private GUIStyle titleStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle monologueStyle;
    private GUIStyle gaugeLabelStyle;
    private GUIStyle feedbackStyle;
    private GUIStyle buttonStyle;
    private GUIStyle clockStyle;
    private bool stylesInitialized = false;

    // Tự sự của Arthur theo 5 mốc thời gian (GDD Section 4.1)
    private readonly string[] arthurMonologues = new string[]
    {
        "Đêm 30 Tết Hà Nội se se lạnh... Ngồi bên bếp củi rực hồng, đôi bàn tay già nua của mình chẳng còn run rẩy.",
        "Bốn mươi năm ở Munich, giao thừa chỉ là một hàm đếm ngược khô khốc trên màn hình máy tính. Chưa từng có một ngọn lửa nào ấm áp như thế này.",
        "Bác An mang ra một chén trà sen nóng: 'Người Việt thức canh bánh không chỉ để bánh chín, mà để đợi nhau qua một năm vất vả'.",
        "Tiếng củi nổ lách tách, tiếng nước sôi ùng ục... Giống như một khúc giao hưởng chậm rãi mà những thuật toán của mình chưa từng chạm tới được.",
        "Mùi lá dong luộc thơm nồng ngào ngạt khắp sân ngõ... Bánh đã chín rồi! Một năm mới bình yên sắp gõ cửa."
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

        // Audio Sources
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length > 0) audioSource = sources[0];
        else audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (sources.Length > 1) ambientLoopSource = sources[1];
        else ambientLoopSource = gameObject.AddComponent<AudioSource>();
        ambientLoopSource.playOnAwake = false;
        ambientLoopSource.loop = true;

        // Clip generation / fallback
        fireCrackleClip = customFireCrackleSound != null ? customFireCrackleSound : GenerateCrackleClip();
        waterPourClip = customWaterPourSound != null ? customWaterPourSound : GenerateWaterPourClip();
        bubbleLoopClip = GenerateBubbleLoopClip();
        successClip = customSuccessFanfare != null ? customSuccessFanfare : GenerateSuccessChimeClip();

        InitTextures();
    }

    private void Update()
    {
        if (!isMinigameOpen) return;

        // 1. Phím đóng nhanh hoặc hủy
        if (Input.GetKeyDown(closeKey))
        {
            CloseMinigame();
            return;
        }

        // Nếu đã hoàn thành mẻ bánh hiện tại, dừng đếm ngược
        if (isCompletedThisSession)
        {
            finishSparkleTimer += Time.deltaTime;
            return;
        }

        // 2. Nhận thao tác phím nóng
        if (Input.GetKeyDown(addFirewoodKey))
        {
            AddFirewood();
        }
        if (Input.GetKeyDown(addWaterKey))
        {
            AddWater();
        }

        // 3. Tiến trình luộc bánh
        flameAnimTimer += Time.deltaTime * (2.5f + (fireTemperature / 100f) * 4f);
        steamAnimTimer += Time.deltaTime * (1.8f + (fireTemperature / 100f) * 3f);

        if (feedbackTimer > 0f) feedbackTimer -= Time.deltaTime;

        // 4. Biến động Vật lý: Nhiệt độ lửa & Mực nước
        // Nhiệt độ giảm dần do gió lạnh
        fireTemperature = Mathf.Max(0f, fireTemperature - fireDecayRate * Time.deltaTime);

        // Nước bốc hơi tỉ lệ thuận với nhiệt độ
        float evapMultiplier = 0.5f + (fireTemperature / 100f) * 1.5f;
        waterLevel = Mathf.Max(0f, waterLevel - (waterEvapBaseRate * evapMultiplier) * Time.deltaTime);

        // 5. Điều kiện sôi hiệu quả: Lửa phải đạt ngưỡng tối thiểu và nước không được cạn khô
        bool isEffectivelyBoiling = fireTemperature >= 35f && waterLevel >= 15f;

        if (isEffectivelyBoiling)
        {
            // Tốc độ chín tối ưu khi nằm trong Sweet Spot
            float progressSpeed = 1.0f;
            if (fireTemperature >= FireSweetMin && fireTemperature <= FireSweetMax)
            {
                progressSpeed = 1.35f; // Chín nhanh hơn và thơm hơn
            }
            else if (fireTemperature < FireSweetMin)
            {
                progressSpeed = 0.65f; // Nước hơi nguội
            }

            currentTimer += Time.deltaTime * progressSpeed;

            // Âm thanh sôi ùng ục
            if (ambientLoopSource != null && !ambientLoopSource.isPlaying)
            {
                ambientLoopSource.clip = bubbleLoopClip;
                ambientLoopSource.volume = 0.45f;
                ambientLoopSource.Play();
            }
        }
        else
        {
            if (ambientLoopSource != null && ambientLoopSource.isPlaying)
            {
                ambientLoopSource.Stop();
            }
        }

        // 6. Kiểm tra hoàn thành mẻ bánh
        if (currentTimer >= boilDuration && !isCompletedThisSession)
        {
            CompleteBoilingSession();
        }
    }
    #endregion

    #region Public Actions
    public void OpenMinigame()
    {
        isMinigameOpen = true;
        isCompletedThisSession = false;
        currentTimer = 0f;
        fireTemperature = 70f;
        waterLevel = 85f;
        feedbackTimer = 4.5f;
        actionFeedback = "🔥 Chào mừng Arthur đến với bếp củi đêm 30 Tết! Hãy cùng Bác An canh lửa nhé.";

        if (ambientLoopSource != null && bubbleLoopClip != null)
        {
            ambientLoopSource.clip = bubbleLoopClip;
            ambientLoopSource.volume = 0.35f;
            ambientLoopSource.Play();
        }

        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.isInputLocked = true;
    }

    public void CloseMinigame()
    {
        isMinigameOpen = false;

        if (ambientLoopSource != null && ambientLoopSource.isPlaying)
        {
            ambientLoopSource.Stop();
        }

        if (player != null)
        {
            player.isInputLocked = false;
        }
    }

    /// <summary>
    /// Thêm 1 thanh củi khô vào đáy nồi (tăng nhiệt độ)
    /// </summary>
    public void AddFirewood()
    {
        if (!isMinigameOpen || isCompletedThisSession) return;

        fireTemperature = Mathf.Min(100f, fireTemperature + firewoodHeatBonus);
        PlayOneShot(fireCrackleClip, 0.9f);

        ShowFeedback("🪵 Đã cời thêm củi gòn khô! Đốm than bùng lên ánh đỏ ấm nồng.");
    }

    /// <summary>
    /// Châm thêm nước sôi từ ấm đồng (ngăn cạn nước)
    /// </summary>
    public void AddWater()
    {
        if (!isMinigameOpen || isCompletedThisSession) return;

        waterLevel = Mathf.Min(100f, waterLevel + waterRefillBonus);
        // Châm nước làm dịu nhẹ nhiệt độ lửa một chút
        fireTemperature = Mathf.Max(10f, fireTemperature - 3.5f);
        PlayOneShot(waterPourClip, 0.85f);

        ShowFeedback("🫖 Đã châm thêm nước sôi từ ấm đồng! Nước ngập kín mặt bánh.");
    }
    #endregion

    #region Completion & Rewards
    private void CompleteBoilingSession()
    {
        isCompletedThisSession = true;
        hasCompleted = true;
        currentTimer = boilDuration;

        if (ambientLoopSource != null && ambientLoopSource.isPlaying)
        {
            ambientLoopSource.Stop();
        }

        PlayOneShot(successClip, 1.0f);
        ShowFeedback("🎉 BÁNH CHƯNG ĐÃ CHÍN RỰC RỠ! Hương lá dong hòa quyện nếp dẻo thơm nồng khắp ngõ.");

        // Cập nhật GameManager: Điểm gắn kết bản địa & cờ nhiệm vụ
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteBanhChungBoiling();
            if (GameManager.Instance.CurrentQuestState == QuestState.BanhChungWrapped)
            {
                GameManager.Instance.SetQuestState(QuestState.BanhChungBoiled);
            }
        }
    }

    private void ShowFeedback(string msg)
    {
        actionFeedback = msg;
        feedbackTimer = 4.2f;
    }

    private void PlayOneShot(AudioClip clip, float volume = 0.85f)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
            audioSource.PlayOneShot(clip, volume);
        }
    }
    #endregion

    #region GUI Drawing (OnGUI Plug-and-Play)
    private void OnGUI()
    {
        if (!isMinigameOpen) return;

        InitStyles();

        // 1. Lớp phủ Vignette tối tạo không gian đêm 30 se lạnh
        GUI.color = new Color(0.04f, 0.05f, 0.08f, 0.94f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whitePixel);

        // 2. Khung cửa sổ chính
        float panelW = Mathf.Clamp(Screen.width * 0.82f, 740f, 960f);
        float panelH = Mathf.Clamp(Screen.height * 0.88f, 560f, 700f);
        Rect panelRect = new Rect((Screen.width - panelW) * 0.5f, (Screen.height - panelH) * 0.5f, panelW, panelH);

        // Ánh lửa hồng bập bùng hắt từ dưới lên
        float flicker = 0.85f + Mathf.Sin(flameAnimTimer * 2f) * 0.15f;
        Color ambientGlow = new Color(0.92f, 0.45f, 0.15f, 0.12f * flicker);
        GUI.color = ambientGlow;
        GUI.DrawTexture(new Rect(panelRect.x - 30, panelRect.y + panelRect.height * 0.3f, panelRect.width + 60, panelRect.height * 0.75f), whitePixel);

        // Nền cửa sổ bếp củi
        GUI.color = new Color(0.11f, 0.13f, 0.17f, 0.98f);
        GUI.DrawTexture(panelRect, whitePixel);

        // Viền đỏ điều ngày Tết
        GUI.color = new Color(0.85f, 0.32f, 0.22f, 1f);
        DrawFrameBorders(panelRect, 3f);

        // 3. Tiêu đề & Đồng hồ thời gian mô phỏng
        DrawHeader(panelRect);

        // 4. Đoạn tự sự của Arthur (Arthur's Monologue)
        DrawMonologueBox(panelRect);

        // 5. Minh họa trung tâm: Nồi gang, khói nghi ngút & bếp củi hồng
        Rect potArea = new Rect(panelRect.center.x - 170f, panelRect.y + 160f, 340f, 240f);
        DrawCentralPotIllustration(potArea);

        // 6. Hai thanh trạng thái: Nhiệt độ lửa (trái) & Mực nước (phải)
        DrawStatusGauges(panelRect, potArea);

        // 7. Khu vực thao tác nút bấm & Phím nóng [F] [W]
        DrawActionControls(panelRect);

        // 8. Nếu đã hoàn thành: Hiện bảng chúc mừng thu hoạch
        if (isCompletedThisSession)
        {
            DrawCompletionModal(panelRect);
        }
    }

    private void DrawHeader(Rect r)
    {
        // Tiêu đề
        GUI.color = new Color(1f, 0.90f, 0.55f, 1f);
        Rect titleR = new Rect(r.x, r.y + 14, r.width, 30);
        GUI.Label(titleR, "🏮 CANH NỒI BÁNH CHƯNG ĐÊM 30 TẾT 🏮", titleStyle);

        // Đồng hồ đêm 30 Tết mô phỏng
        float p = Progress;
        int startHour = 23;
        int totalHours = 6; // 23h đến 05h sáng
        float currentSimHour = startHour + p * totalHours;
        if (currentSimHour >= 24f) currentSimHour -= 24f;
        int hour = Mathf.FloorToInt(currentSimHour);
        int minute = Mathf.FloorToInt((currentSimHour - Mathf.Floor(currentSimHour)) * 60f);

        string timeString = $"🕒 Thời khắc: {hour:D2}:{minute:D2} Đêm Giao Thừa • Tiến độ: {Mathf.RoundToInt(p * 100f)}%";
        GUI.color = new Color(0.92f, 0.85f, 0.70f, 0.9f);
        Rect clockR = new Rect(r.x, titleR.yMax + 2, r.width, 22);
        GUI.Label(clockR, timeString, clockStyle);

        // Thanh tiến độ mỏng trên đỉnh
        Rect progBarBg = new Rect(r.x + 40, clockR.yMax + 6, r.width - 80, 7);
        GUI.color = new Color(0.2f, 0.22f, 0.26f, 1f);
        GUI.DrawTexture(progBarBg, whitePixel);

        Rect progBarFill = new Rect(progBarBg.x, progBarBg.y, progBarBg.width * p, progBarBg.height);
        GUI.color = new Color(0.95f, 0.78f, 0.35f, 1f);
        GUI.DrawTexture(progBarFill, whitePixel);
    }

    private void DrawMonologueBox(Rect r)
    {
        // Xác định câu tự sự tương ứng với tiến độ
        int monoIndex = Mathf.Clamp(Mathf.FloorToInt(Progress * arthurMonologues.Length), 0, arthurMonologues.Length - 1);
        string currentMonologue = arthurMonologues[monoIndex];

        float boxW = r.width - 100f;
        float boxH = 54f;
        Rect boxR = new Rect(r.x + 50f, r.y + 88f, boxW, boxH);

        // Nền mộc mạc như trang giấy ghi chép cổ
        GUI.color = new Color(0.18f, 0.16f, 0.14f, 0.85f);
        GUI.DrawTexture(boxR, whitePixel);
        GUI.color = new Color(0.75f, 0.62f, 0.42f, 0.6f);
        DrawFrameBorders(boxR, 1.2f);

        GUI.color = new Color(0.96f, 0.92f, 0.80f, 1f);
        Rect textR = new Rect(boxR.x + 16, boxR.y + 6, boxR.width - 32, boxR.height - 12);
        GUI.Label(textR, $"\"{currentMonologue}\"", monologueStyle);
    }

    private void DrawCentralPotIllustration(Rect pot)
    {
        // 1. Luồng khói bốc nghi ngút
        for (int i = 0; i < 5; i++)
        {
            float wave = Mathf.Sin(steamAnimTimer * 2f + i * 1.2f) * 16f;
            float steamAlpha = 0.25f + Mathf.PingPong(steamAnimTimer * 0.8f + i * 0.3f, 0.35f);
            float sY = pot.y - 45f - i * 14f;
            Rect steamR = new Rect(pot.center.x + wave - 25f, sY, 50f, 22f);

            GUI.color = new Color(0.95f, 0.95f, 0.98f, steamAlpha * (waterLevel > 15f ? 1f : 0.2f));
            GUI.DrawTexture(steamR, whitePixel);
        }

        // 2. Vung và thân Nồi Gang khổng lồ
        // Quai nồi 2 bên
        GUI.color = new Color(0.35f, 0.35f, 0.38f, 1f);
        GUI.DrawTexture(new Rect(pot.x - 16, pot.y + 40, 20, 36), whitePixel);
        GUI.DrawTexture(new Rect(pot.xMax - 4, pot.y + 40, 20, 36), whitePixel);

        // Thân nồi gang đen ánh thép
        Rect mainBody = new Rect(pot.x, pot.y + 25, pot.width, pot.height - 70);
        GUI.color = new Color(0.22f, 0.24f, 0.28f, 1f);
        GUI.DrawTexture(mainBody, whitePixel);

        // Vạch đai kim loại quanh nồi
        GUI.color = new Color(0.38f, 0.40f, 0.45f, 1f);
        GUI.DrawTexture(new Rect(mainBody.x, mainBody.y + mainBody.height * 0.4f, mainBody.width, 8), whitePixel);

        // Chữ BÁNH CHƯNG TẾT khắc mờ trên thân nồi
        GUI.color = new Color(0.85f, 0.75f, 0.55f, 0.8f);
        GUI.Label(mainBody, "✦ NỒI BÁNH CHƯNG GIA TRUYỀN ✦", gaugeLabelStyle);

        // Nắp vung nồi
        Rect lidRect = new Rect(pot.x + 10, pot.y + 8, pot.width - 20, 20);
        GUI.color = new Color(0.30f, 0.32f, 0.36f, 1f);
        GUI.DrawTexture(lidRect, whitePixel);

        // Núm vung
        GUI.color = new Color(0.75f, 0.55f, 0.25f, 1f);
        GUI.DrawTexture(new Rect(pot.center.x - 12, pot.y - 2, 24, 12), whitePixel);

        // 3. Kiềng sắt & Bếp than củi hồng rực
        Rect fireBase = new Rect(pot.x + 20, mainBody.yMax, pot.width - 40, 48);

        // 3 thanh củi bắt chéo
        GUI.color = new Color(0.28f, 0.16f, 0.08f, 1f);
        GUI.DrawTexture(new Rect(fireBase.x + 10, fireBase.y + 15, fireBase.width - 20, 14), whitePixel);

        // Đốm than củi & Ngọn lửa bập bùng
        float fireIntensity = fireTemperature / 100f;
        float flick = Mathf.Sin(flameAnimTimer * 5f) * 8f;

        // Lớp lửa đỏ cam
        GUI.color = new Color(0.95f, 0.30f, 0.10f, 0.85f * fireIntensity);
        GUI.DrawTexture(new Rect(fireBase.x + 25 + flick, fireBase.y - 8, fireBase.width - 50, 36), whitePixel);

        // Lớp lửa vàng rực tâm bếp
        GUI.color = new Color(1.0f, 0.88f, 0.25f, 0.95f * fireIntensity);
        GUI.DrawTexture(new Rect(fireBase.center.x - 30 + flick * 0.5f, fireBase.y - 2, 60, 24), whitePixel);
    }

    private void DrawStatusGauges(Rect panel, Rect pot)
    {
        float gaugeW = 145f;
        float gaugeH = 210f;

        // CỘT TRÁI: NHIỆT ĐỘ LỬA
        Rect leftGauge = new Rect(panel.x + 36, pot.y + 10, gaugeW, gaugeH);
        DrawSingleGauge(
            leftGauge,
            "🔥 NHIỆT ĐỘ LỬA",
            fireTemperature,
            new Color(0.95f, 0.42f, 0.15f, 1f),
            FireSweetMin,
            FireSweetMax,
            "[Vùng Xanh: 50-85%]"
        );

        // CỘT PHẢI: MỰC NƯỚC SÔI
        Rect rightGauge = new Rect(panel.xMax - gaugeW - 36, pot.y + 10, gaugeW, gaugeH);
        DrawSingleGauge(
            rightGauge,
            "💧 MỰC NƯỚC SÔI",
            waterLevel,
            new Color(0.25f, 0.68f, 0.95f, 1f),
            WaterSafeMin,
            100f,
            "[An toàn: > 35%]"
        );
    }

    private void DrawSingleGauge(Rect r, string title, float value, Color fillColor, float sweetMin, float sweetMax, string guide)
    {
        // Nền khung
        GUI.color = new Color(0.16f, 0.18f, 0.22f, 0.95f);
        GUI.DrawTexture(r, whitePixel);
        GUI.color = new Color(0.35f, 0.38f, 0.45f, 0.8f);
        DrawFrameBorders(r, 1.5f);

        // Tiêu đề
        GUI.color = Color.white;
        Rect tRect = new Rect(r.x, r.y + 8, r.width, 20);
        GUI.Label(tRect, title, gaugeLabelStyle);

        // Cột đo mức (Thanh đo dọc)
        float barW = 28f;
        float barH = r.height - 75f;
        Rect barBg = new Rect(r.center.x - barW * 0.5f, tRect.yMax + 8, barW, barH);

        GUI.color = new Color(0.08f, 0.09f, 0.12f, 1f);
        GUI.DrawTexture(barBg, whitePixel);

        // Vùng tối ưu (Sweet Zone highlight)
        float sweetNormMin = sweetMin / 100f;
        float sweetNormMax = sweetMax / 100f;
        float sweetY = barBg.yMax - sweetNormMax * barH;
        float sweetH = (sweetNormMax - sweetNormMin) * barH;
        Rect sweetRect = new Rect(barBg.x - 2, sweetY, barW + 4, sweetH);
        GUI.color = new Color(0.2f, 0.85f, 0.4f, 0.35f);
        GUI.DrawTexture(sweetRect, whitePixel);

        // Mức chất lỏng / lửa dâng lên
        float fillNorm = Mathf.Clamp01(value / 100f);
        float fillHeight = fillNorm * barH;
        Rect fillRect = new Rect(barBg.x, barBg.yMax - fillHeight, barW, fillHeight);
        GUI.color = fillColor;
        GUI.DrawTexture(fillRect, whitePixel);

        // Chỉ số %
        GUI.color = Color.white;
        Rect valRect = new Rect(r.x, barBg.yMax + 4, r.width, 18);
        GUI.Label(valRect, $"{Mathf.RoundToInt(value)}%", subHeaderStyle);

        // Chú thích
        GUI.color = new Color(0.7f, 0.75f, 0.8f, 0.85f);
        Rect guideRect = new Rect(r.x, valRect.yMax, r.width, 16);
        GUI.Label(guideRect, guide, monologueStyle);
    }

    private void DrawActionControls(Rect panel)
    {
        // 1. Dòng thông báo / nhắc nhở hướng dẫn
        float feedbackW = panel.width - 120f;
        Rect fbRect = new Rect(panel.center.x - feedbackW * 0.5f, panel.yMax - 110f, feedbackW, 26f);
        GUI.color = new Color(0.15f, 0.17f, 0.22f, 0.8f);
        GUI.DrawTexture(fbRect, whitePixel);

        GUI.color = new Color(1f, 0.92f, 0.65f, 1f);
        GUI.Label(fbRect, actionFeedback, feedbackStyle);

        // 2. Hai nút bấm thao tác tương tác
        float btnW = 240f;
        float btnH = 46f;
        float gap = 40f;

        // Nút 1: Thêm Củi Khô [F]
        Rect fireBtn = new Rect(panel.center.x - btnW - gap * 0.5f, fbRect.yMax + 12f, btnW, btnH);
        if (GUI.Button(fireBtn, "🪵 Thêm Củi Khô  [Phím F]", buttonStyle))
        {
            AddFirewood();
        }

        // Nút 2: Châm Nước Sôi [W]
        Rect waterBtn = new Rect(panel.center.x + gap * 0.5f, fbRect.yMax + 12f, btnW, btnH);
        if (GUI.Button(waterBtn, "🫖 Châm Nước Sôi  [Phím W]", buttonStyle))
        {
            AddWater();
        }
    }

    private void DrawCompletionModal(Rect panel)
    {
        float mW = Mathf.Min(panel.width * 0.9f, 620f);
        float mH = 260f;
        Rect mRect = new Rect(panel.center.x - mW * 0.5f, panel.center.y - mH * 0.5f, mW, mH);

        // Bóng đổ
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(mRect.x + 6, mRect.y + 6, mRect.width, mRect.height), darkPixel);

        // Nền tối sang trọng
        GUI.color = new Color(0.12f, 0.14f, 0.19f, 0.98f);
        GUI.DrawTexture(mRect, whitePixel);

        // Viền vàng kim rạng rỡ
        GUI.color = new Color(0.98f, 0.82f, 0.35f, 1f);
        DrawFrameBorders(mRect, 2.5f);

        // Tiêu đề
        GUI.color = new Color(1f, 0.92f, 0.55f, 1f);
        Rect tR = new Rect(mRect.x, mRect.y + 18, mRect.width, 32);
        GUI.Label(tR, "✨ BÁNH CHƯNG ĐÃ CHÍN THƠM LỪNG! ✨", titleStyle);

        // Nội dung chúc mừng
        string detail = "Hơi nước bốc lên ngào ngạt mùi lá dong và đỗ xanh bùi béo...\n" +
                        "Ngồi bên bếp lửa suốt đêm đông Hà Nội, Arthur đã thực sự cảm nhận được hơi ấm sum vầy của ngày Tết cổ truyền!\n\n" +
                        "🏆 Nhận được: +30 Điểm Gắn Kết Bản Địa (Ấm Trà Sen)\n" +
                        "📖 Đã cập nhật Kỷ Niệm Nồi Bánh Luộc vào Cuốn Album Kỷ Niệm!";
        GUI.color = Color.white;
        Rect dR = new Rect(mRect.x + 24, tR.yMax + 8, mRect.width - 48, 110);
        GUI.Label(dR, detail, feedbackStyle);

        // Nút hành động
        float btnW = 200f;
        float btnH = 38f;

        // Nút mở Album
        Rect albumBtn = new Rect(mRect.center.x - btnW - 12, mRect.yMax - btnH - 18, btnW, btnH);
        if (GUI.Button(albumBtn, "📖 Mở Cuốn Album (Tab)", buttonStyle))
        {
            CloseMinigame();
            if (AlbumUIController.Instance != null)
            {
                AlbumUIController.Instance.OpenAlbum();
                AlbumUIController.Instance.GoToPage(1);
            }
        }

        // Nút hoàn tất / tiếp tục dạo bước
        Rect doneBtn = new Rect(mRect.center.x + 12, mRect.yMax - btnH - 18, btnW, btnH);
        if (GUI.Button(doneBtn, "🎆 Đón Chờ Giao Thừa", buttonStyle))
        {
            CloseMinigame();
        }
    }

    private void DrawFrameBorders(Rect r, float thickness)
    {
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), whitePixel);
        GUI.DrawTexture(new Rect(r.x, r.yMax - thickness, r.width, thickness), whitePixel);
        GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), whitePixel);
        GUI.DrawTexture(new Rect(r.xMax - thickness, r.y, thickness, r.height), whitePixel);
    }
    #endregion

    #region Procedural Textures & Styles
    private void InitTextures()
    {
        whitePixel = new Texture2D(1, 1);
        whitePixel.SetPixel(0, 0, Color.white);
        whitePixel.Apply();

        darkPixel = new Texture2D(1, 1);
        darkPixel.SetPixel(0, 0, new Color(0.08f, 0.09f, 0.12f, 0.98f));
        darkPixel.Apply();
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        subHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        clockStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter
        };

        monologueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };

        gaugeLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        feedbackStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        stylesInitialized = true;
    }
    #endregion

    #region Procedural Audio Synthesizers
    private AudioClip GenerateCrackleClip()
    {
        int sampleRate = 44100;
        float duration = 0.45f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Exp(-t * 8f);
            float noise = (UnityEngine.Random.value * 2f - 1f);
            float pop = (UnityEngine.Random.value > 0.97f) ? (UnityEngine.Random.value * 2f - 1f) * 1.5f : 0f;
            samples[i] = (noise * 0.25f + pop) * env;
        }

        AudioClip clip = AudioClip.Create("Procedural_Crackle", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateWaterPourClip()
    {
        int sampleRate = 44100;
        float duration = 0.5f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Sin(t / duration * Mathf.PI);
            float noise = (UnityEngine.Random.value * 2f - 1f) * 0.35f;
            float tone = Mathf.Sin(2f * Mathf.PI * 450f * t) * 0.15f;
            samples[i] = (noise + tone) * env;
        }

        AudioClip clip = AudioClip.Create("Procedural_WaterPour", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateBubbleLoopClip()
    {
        int sampleRate = 44100;
        float duration = 2.0f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            // Tiếng sôi ùng ục trầm
            float b1 = Mathf.Sin(2f * Mathf.PI * (75f + Mathf.Sin(t * 8f) * 15f) * t) * 0.15f;
            float b2 = Mathf.Sin(2f * Mathf.PI * (120f + Mathf.Cos(t * 12f) * 20f) * t) * 0.12f;
            float noise = (UnityEngine.Random.value * 2f - 1f) * 0.06f;
            samples[i] = b1 + b2 + noise;
        }

        AudioClip clip = AudioClip.Create("Procedural_BubbleLoop", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateSuccessChimeClip()
    {
        int sampleRate = 44100;
        float duration = 1.2f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float[] chords = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }; // C - E - G - C

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float sum = 0f;
            for (int c = 0; c < chords.Length; c++)
            {
                float noteTime = t - c * 0.12f;
                if (noteTime > 0f)
                {
                    float env = Mathf.Exp(-noteTime * 3.5f);
                    sum += Mathf.Sin(2f * Mathf.PI * chords[c] * noteTime) * env;
                }
            }
            samples[i] = Mathf.Clamp(sum * 0.25f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Procedural_BoilSuccess", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
    #endregion
}
