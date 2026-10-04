using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý Sự Kiện Đêm Giao Thừa 00:00 & Khoảnh Khắc Vàng (The Perfect Shot) - Climax của Chương 1.
/// Bám sát Mục 3.2 và 4.1 của Game Design Document (GDD):
/// - Tiếng pháo hoa rền vang từ phía hồ Gươm trên bầu trời đêm Hà Nội se lạnh.
/// - Ánh sáng pháo hoa chớp nhẹ trên bầu trời (Sky Flash).
/// - Bác An đứng trước bàn thờ gia tiên thắp nén nhang trầm cầu bình an.
/// - Nhận diện mục tiêu "The Perfect Shot" khi Arthur chụp Bác An thắp nhang (targetId = "NewYearEveAltar").
/// - Nghi thức Mừng Tuổi: Bác An trao tặng Phong Bao Lì Xì đỏ có chữ "Bình An" (+50 Điểm Gắn Kết Bản Địa).
/// - Tự tổng hợp âm thanh pháo hoa thủ tục (Procedural Fireworks Synthesizer) nếu chưa gán file audio.
/// </summary>
[DisallowMultipleComponent]
public class NewYearEveEventManager : MonoBehaviour
{
    public static NewYearEveEventManager Instance { get; private set; }

    #region Serialized Fields
    [Header("=== CÀI ĐẶT SỰ KIỆN GIAO THỪA ===")]
    [Tooltip("Khoảng thời gian ngẫu nhiên giữa các đợt pháo hoa nổ xa xa (giây).")]
    public Vector2 fireworkIntervalRange = new Vector2(3.5f, 7.0f);

    [Tooltip("Bật hiệu ứng ánh sáng pháo hoa hắt sáng nhẹ lên màn hình.")]
    public bool enableSkyFlash = true;

    [Tooltip("Vị trí bàn thờ gia tiên trong nhà Bác An (để vẽ hiệu ứng khói nhang trầm).")]
    public Transform altarTransform;

    [Header("=== ÂM THANH PHÁO HOA & GIAO THỪA (TỰ TỔNG HỢP NẾU TRỐNG) ===")]
    public AudioClip customFireworkBoomSound;
    public AudioClip customLiXiFanfare;
    #endregion

    #region Public Properties
    /// <summary> Sự kiện Giao thừa đang diễn ra hay không </summary>
    public bool IsMidnightActive => isMidnightActive;

    /// <summary> Đang hiển thị bảng nhận Phong Bao Lì Xì mừng tuổi </summary>
    public bool IsLiXiModalOpen => showLiXiModal;
    #endregion

    #region Private State
    private PlayerController player;
    private AudioSource audioSource;
    private AudioClip fireworkBoomClip;
    private AudioClip lixiFanfareClip;

    private bool isMidnightActive = false;
    private float nextFireworkTimer = 2.0f;
    private float flashAlpha = 0f;
    private Color currentFlashColor = Color.white;

    // Li Xi Presentation Modal
    private bool showLiXiModal = false;
    private float lixiAnimTimer = 0f;

    // GUI Textures & Styles
    private Texture2D whitePixel;
    private Texture2D darkPixel;
    private Texture2D redEnvelopeTex;
    private GUIStyle lixiTitleStyle;
    private GUIStyle lixiCalligraphyStyle;
    private GUIStyle lixiBlessingStyle;
    private GUIStyle lixiRewardStyle;
    private GUIStyle buttonStyle;
    private bool stylesInitialized = false;

    private readonly Color[] fireworkColors = new Color[]
    {
        new Color(1.0f, 0.45f, 0.35f), // Đỏ đào
        new Color(1.0f, 0.85f, 0.40f), // Vàng hoàng yến
        new Color(0.95f, 0.60f, 0.90f), // Tím hồng
        new Color(0.45f, 0.85f, 1.00f)  // Xanh lam ngọc
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

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        fireworkBoomClip = customFireworkBoomSound != null ? customFireworkBoomSound : GenerateFireworkBoomClip();
        lixiFanfareClip = customLiXiFanfare != null ? customLiXiFanfare : GenerateLiXiFanfareClip();

        InitTextures();
    }

    private void Start()
    {
        // Tự động kiểm tra trạng thái nhiệm vụ nếu đã đến lúc Giao thừa
        CheckQuestSync();
    }

    private void Update()
    {
        // 1. Đồng bộ trạng thái Giao thừa với GameManager
        CheckQuestSync();

        // 2. Nếu đang trong đêm Giao thừa, định kỳ phát tiếng pháo hoa và chớp sáng
        if (isMidnightActive)
        {
            nextFireworkTimer -= Time.deltaTime;
            if (nextFireworkTimer <= 0f)
            {
                TriggerRandomFirework();
                nextFireworkTimer = UnityEngine.Random.Range(fireworkIntervalRange.x, fireworkIntervalRange.y);
            }
        }

        // 3. Giảm dần độ mờ chớp sáng pháo hoa
        if (flashAlpha > 0f)
        {
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.deltaTime * 0.85f);
        }

        // 4. Hoạt ảnh xoay lấp lánh bao lì xì
        if (showLiXiModal)
        {
            lixiAnimTimer += Time.deltaTime;
        }
    }
    #endregion

    #region Event Logic
    private void CheckQuestSync()
    {
        if (GameManager.Instance == null) return;

        QuestState state = GameManager.Instance.CurrentQuestState;
        bool shouldBeActive = state >= QuestState.WaitingForMidnight;

        if (shouldBeActive && !isMidnightActive)
        {
            StartMidnightCeremony();
        }
    }

    /// <summary>
    /// Kích hoạt không khí Giao Thừa (00:00 Đêm 30 Tết)
    /// </summary>
    public void StartMidnightCeremony()
    {
        isMidnightActive = true;
        nextFireworkTimer = 0.8f; // Bắt đầu bằng 1 đợt pháo hoa ngay lập tức

        if (GameManager.Instance != null && GameManager.Instance.CurrentQuestState < QuestState.WaitingForMidnight)
        {
            GameManager.Instance.SetQuestState(QuestState.WaitingForMidnight);
        }

        Debug.Log("[NewYearEve] 🎆 00:00 GIAO THỪA ĐÃ ĐIỂM! Bầu trời Hà Nội rực sáng tiếng pháo hoa đón mừng năm mới.");
    }

    /// <summary>
    /// Kích hoạt 1 phát pháo hoa trầm ấm xa xa
    /// </summary>
    public void TriggerRandomFirework()
    {
        if (audioSource != null && fireworkBoomClip != null)
        {
            audioSource.pitch = UnityEngine.Random.Range(0.85f, 1.15f);
            audioSource.PlayOneShot(fireworkBoomClip, UnityEngine.Random.Range(0.7f, 1.0f));
        }

        if (enableSkyFlash)
        {
            currentFlashColor = fireworkColors[UnityEngine.Random.Range(0, fireworkColors.Length)];
            flashAlpha = UnityEngine.Random.Range(0.18f, 0.28f);
        }
    }

    /// <summary>
    /// Mở bảng giao diện nhận Phong Bao Lì Xì "Bình An" từ Bác An
    /// </summary>
    public void ShowLiXiModal()
    {
        showLiXiModal = true;
        lixiAnimTimer = 0f;

        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.isInputLocked = true;

        if (audioSource != null && lixiFanfareClip != null)
        {
            audioSource.pitch = 1.0f;
            audioSource.PlayOneShot(lixiFanfareClip, 0.95f);
        }
    }

    public void CloseLiXiModal()
    {
        showLiXiModal = false;

        if (player != null)
        {
            player.isInputLocked = false;
        }
    }
    #endregion

    #region GUI Drawing - Sky Flash & Li Xi Modal
    private void OnGUI()
    {
        InitStyles();

        // 1. Ánh chớp pháo hoa hắt sáng màn hình
        if (flashAlpha > 0.005f)
        {
            Color oldC = GUI.color;
            GUI.color = new Color(currentFlashColor.r, currentFlashColor.g, currentFlashColor.b, flashAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whitePixel);
            GUI.color = oldC;
        }

        // 2. Bảng chúc Tết và nhận Phong Bao Lì Xì Đỏ "Bình An"
        if (showLiXiModal)
        {
            DrawLiXiCeremonyModal();
        }
    }

    private void DrawLiXiCeremonyModal()
    {
        // Lớp nền tối mờ
        GUI.color = new Color(0.05f, 0.06f, 0.08f, 0.92f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whitePixel);

        // Khung Modal chính
        float modalW = Mathf.Clamp(Screen.width * 0.75f, 620f, 780f);
        float modalH = Mathf.Clamp(Screen.height * 0.82f, 480f, 580f);
        Rect modalRect = new Rect((Screen.width - modalW) * 0.5f, (Screen.height - modalH) * 0.5f, modalW, modalH);

        // Bóng đổ
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(modalRect.x + 6, modalRect.y + 6, modalRect.width, modalRect.height), darkPixel);

        // Nền đỏ thẫm may mắn truyền thống
        GUI.color = new Color(0.16f, 0.08f, 0.08f, 0.98f);
        GUI.DrawTexture(modalRect, whitePixel);

        // Viền vàng kim rạng rỡ
        GUI.color = new Color(0.98f, 0.82f, 0.35f, 1f);
        DrawFrameBorders(modalRect, 3f);

        // Tiêu đề
        GUI.color = new Color(1f, 0.92f, 0.55f, 1f);
        Rect titleR = new Rect(modalRect.x, modalRect.y + 16, modalRect.width, 30);
        GUI.Label(titleR, "🏮 BÁC AN MỪNG TUỔI ĐẦU XUÂN 🏮", lixiTitleStyle);

        // MINH HỌA PHONG BAO LÌ XÌ ĐỎ "BÌNH AN" Ở GIỮA
        float envW = 160f;
        float envH = 220f;
        float floatBounce = Mathf.Sin(lixiAnimTimer * 3.5f) * 6f;
        Rect envRect = new Rect(modalRect.center.x - envW * 0.5f, modalRect.y + 62f + floatBounce, envW, envH);

        // Bóng bao lì xì
        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(envRect.x + 4, envRect.y + 4, envRect.width, envRect.height), darkPixel);

        // Thân bao lì xì màu đỏ điều may mắn
        GUI.color = new Color(0.85f, 0.18f, 0.15f, 1f);
        GUI.DrawTexture(envRect, whitePixel);

        // Viền hoa văn vàng kim
        GUI.color = new Color(0.98f, 0.85f, 0.45f, 1f);
        DrawFrameBorders(envRect, 2f);

        // Nắp gấp tam giác bao lì xì
        Rect flapRect = new Rect(envRect.x, envRect.y, envRect.width, 42f);
        GUI.color = new Color(0.72f, 0.12f, 0.10f, 1f);
        GUI.DrawTexture(flapRect, whitePixel);
        GUI.color = new Color(0.98f, 0.85f, 0.45f, 1f);
        GUI.DrawTexture(new Rect(flapRect.x, flapRect.yMax - 2, flapRect.width, 2), whitePixel);

        // Chữ Thư Pháp "BÌNH AN" dát vàng
        GUI.color = new Color(1.0f, 0.92f, 0.55f, 1f);
        Rect calligR = new Rect(envRect.x, envRect.y + 65f, envRect.width, 95f);
        GUI.Label(calligR, "BÌNH\nAN", lixiCalligraphyStyle);

        // Hoa văn đồng tiền cổ may mắn phía dưới
        GUI.color = new Color(0.95f, 0.82f, 0.40f, 0.8f);
        Rect coinR = new Rect(envRect.center.x - 14f, envRect.yMax - 38f, 28f, 28f);
        DrawFrameBorders(coinR, 2f);

        // LỜI CHÚC TẾT & Ý NGHĨA KỶ VẬT
        float blessY = envRect.yMax + 18f;
        Rect blessR = new Rect(modalRect.x + 30f, blessY, modalRect.width - 60f, 85f);
        string blessingText = "Bác An mỉm cười hiền hậu, hai tay trao chiếc phong bao đỏ thắm cho Arthur:\n" +
                              "\"Mừng tuổi người bạn già của tôi! Chúc Arthur một năm mới dồi dào sức khỏe, " +
                              "tâm hồn luôn bình yên và tìm thấy những điều ấm áp nhất trên đất nước Việt Nam này.\"\n\n" +
                              "🏆 Thưởng: +50 Điểm Gắn Kết Bản Địa (Tổng: " + (GameManager.Instance != null ? GameManager.Instance.CulturalAffinity : 95) + " điểm)\n" +
                              "📖 Chiếc Phong Bao Lì Xì \"Bình An\" đã được ghim trang trọng vào Cuốn Album Kỷ Niệm!";
        GUI.color = Color.white;
        GUI.Label(blessR, blessingText, lixiBlessingStyle);

        // 2 NÚT THAO TÁC HOÀN THÀNH
        float btnW = 230f;
        float btnH = 42f;
        float btnY = modalRect.yMax - btnH - 20f;

        // Nút 1: Mở Cuốn Album xem trang kỷ niệm hoàn chỉnh
        Rect albumBtn = new Rect(modalRect.center.x - btnW - 12f, btnY, btnW, btnH);
        if (GUI.Button(albumBtn, "📖 Mở Cuốn Album (Tab)", buttonStyle))
        {
            CloseLiXiModal();
            if (AlbumUIController.Instance != null)
            {
                AlbumUIController.Instance.OpenAlbum();
                AlbumUIController.Instance.GoToPage(2);
            }
        }

        // Nút 2: Chúc Mừng Năm Mới & Hoàn thành trọn vẹn Chương 1
        Rect doneBtn = new Rect(modalRect.center.x + 12f, btnY, btnW, btnH);
        if (GUI.Button(doneBtn, "🎆 Chúc Mừng Năm Mới!", buttonStyle))
        {
            CloseLiXiModal();
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
        darkPixel.SetPixel(0, 0, new Color(0.06f, 0.08f, 0.10f, 0.96f));
        darkPixel.Apply();
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        lixiTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        lixiCalligraphyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        lixiBlessingStyle = new GUIStyle(GUI.skin.label)
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
    private AudioClip GenerateFireworkBoomClip()
    {
        int sampleRate = 44100;
        float duration = 1.6f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            // Tiếng nổ trầm đục xa xa (low frequency boom)
            float boom = Mathf.Sin(2f * Mathf.PI * (55f - t * 25f) * t) * Mathf.Exp(-t * 3.5f);
            float noise = (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 2.8f) * 0.35f;
            float crackle = (t > 0.4f && UnityEngine.Random.value > 0.94f) ? (UnityEngine.Random.value * 2f - 1f) * Mathf.Exp(-t * 1.5f) * 0.8f : 0f;
            samples[i] = Mathf.Clamp(boom * 0.7f + noise + crackle, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Procedural_FireworkBoom", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip GenerateLiXiFanfareClip()
    {
        int sampleRate = 44100;
        float duration = 1.4f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        // Hợp âm rộn rã đầu xuân: G4, B4, D5, G5
        float[] notes = new float[] { 392f, 493.88f, 587.33f, 783.99f };

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float sum = 0f;
            for (int n = 0; n < notes.Length; n++)
            {
                float noteT = t - n * 0.14f;
                if (noteT > 0f)
                {
                    float env = Mathf.Exp(-noteT * 3.2f);
                    sum += Mathf.Sin(2f * Mathf.PI * notes[n] * noteT) * env;
                }
            }
            samples[i] = Mathf.Clamp(sum * 0.28f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Procedural_LiXiFanfare", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
    #endregion
}
