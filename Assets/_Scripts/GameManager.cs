using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hệ thống Trạng thái Game & Quản lý Nhiệm vụ (Game Manager & Quest Flags) cho "A Year in Warmth".
/// Quản lý vòng lặp nhiệm vụ thử nghiệm:
/// - Trạng thái 0: Chưa nhận việc.
/// - Trạng thái 1: Đã nhận việc từ Bác An (Chụp cành đào phai).
/// - Trạng thái 2: Đã chụp đúng bức ảnh yêu cầu (Cây đào nằm trong khung ngắm).
/// - Trạng thái 3: Hoàn thành nhiệm vụ (Đã trả ảnh và cảm ơn).
/// </summary>
public enum QuestState
{
    NotStarted = 0,     // Trạng thái 0: Chưa nhận việc
    QuestAccepted = 1,  // Trạng thái 1: Đã nhận việc từ Bác An (Chụp cành đào phai)
    PhotoTaken = 2,     // Trạng thái 2: Đã chụp đúng cành đào phai
    Completed = 3       // Trạng thái 3: Hoàn thành nhiệm vụ
}

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    #region Serialized Fields
    [Header("=== TRẠNG THÁI NHIỆM VỤ HIỆN TẠI (QUEST STATE) ===")]
    [Tooltip("Trạng thái hiện tại của nhiệm vụ chụp ảnh cành đào phai.")]
    public QuestState currentQuestState = QuestState.NotStarted;

    [Header("=== CÀI ĐẶT MỤC TIÊU ẢNH (TARGET SETTINGS) ===")]
    [Tooltip("ID của mục tiêu cần chụp cho nhiệm vụ.")]
    public string requiredTargetId = "PeachBlossom";

    [Tooltip("Tự động sinh NPC Bác An và Cây Đào Phai trong Scene nếu chưa có để test ngay.")]
    public bool autoSpawnQuestObjects = true;

    [Header("=== ÂM THANH HOÀN THÀNH MỤC TIÊU ===")]
    [Tooltip("Âm thanh chuông báo khi hoàn thành mục tiêu chụp ảnh.")]
    public AudioClip questUpdateSound;
    #endregion

    #region Public Properties & Events
    /// <summary> Trạng thái nhiệm vụ hiện tại </summary>
    public QuestState CurrentQuestState => currentQuestState;

    /// <summary> Bức ảnh cành đào phai đã chụp được lưu trong bộ nhớ </summary>
    public Texture2D QuestPhoto => questPhoto;

    public event Action<QuestState> OnQuestStateChanged;
    #endregion

    #region Private State
    private Texture2D questPhoto;
    private AudioSource audioSource;
    private AudioClip successChimeClip;

    // Toast Notification Banner
    private string toastMessage = "";
    private float toastTimer = 0f;
    private float toastDuration = 4.0f;

    // GUI Textures & Styles
    private Texture2D whitePixel;
    private Texture2D darkPixel;
    private GUIStyle questTitleStyle;
    private GUIStyle questObjectiveStyle;
    private GUIStyle toastStyle;
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

        // Cấu hình Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        successChimeClip = questUpdateSound != null ? questUpdateSound : GenerateSuccessChimeClip();

        // Texture nền GUI
        whitePixel = new Texture2D(1, 1);
        whitePixel.SetPixel(0, 0, Color.white);
        whitePixel.Apply();

        darkPixel = new Texture2D(1, 1);
        darkPixel.SetPixel(0, 0, new Color(0.12f, 0.14f, 0.18f, 0.93f));
        darkPixel.Apply();
    }

    private void Start()
    {
        if (autoSpawnQuestObjects)
        {
            SetupQuestWorldObjects();
        }
    }

    private void Update()
    {
        if (toastTimer > 0f)
        {
            toastTimer -= Time.deltaTime;
        }
    }
    #endregion

    #region Quest State Management
    /// <summary>
    /// Thay đổi trạng thái nhiệm vụ và kích hoạt thông báo
    /// </summary>
    public void SetQuestState(QuestState newState)
    {
        if (currentQuestState == newState) return;

        currentQuestState = newState;
        OnQuestStateChanged?.Invoke(newState);

        PlayQuestSound();

        // Hiển thị thông báo Toast tương ứng
        switch (newState)
        {
            case QuestState.NotStarted:
                ShowToast("📜 Nhiệm vụ: Đang chờ bắt đầu...");
                break;
            case QuestState.QuestAccepted:
                ShowToast("🌸 [Nhiệm vụ Mới]: Hãy chụp ảnh Cành Đào Phai trước hiên nhà!");
                break;
            case QuestState.PhotoTaken:
                ShowToast("✨ [Mục Tiêu Hoàn Thành]: Đã chụp đúng cành đào phai! Hãy mang về cho Bác An.");
                break;
            case QuestState.Completed:
                ShowToast("🎉 [Hoàn Thành Nhiệm Vụ]: Bác An rất thích bức ảnh của bạn!");
                break;
        }

        Debug.Log($"[GameManager] 🚩 Trạng thái nhiệm vụ chuyển sang: {newState}");
    }

    /// <summary>
    /// Được gọi mỗi khi máy ảnh Viewfinder bấm chụp, kiểm tra xem mục tiêu có nằm trong khung ngắm không
    /// </summary>
    public void OnPhotoSnapped(Camera cam, Rect viewfinderRect, Vector2 playerPosition, Texture2D snappedPhoto)
    {
        // Chỉ xử lý nếu người chơi đang ở Trạng thái 1 (Đã nhận việc và đang cần chụp ảnh)
        if (currentQuestState != QuestState.QuestAccepted) return;

        // Tìm tất cả PhotoTarget trong Scene
        PhotoTarget[] targets = FindObjectsByType<PhotoTarget>(FindObjectsSortMode.None);
        foreach (PhotoTarget target in targets)
        {
            if (target.targetId == requiredTargetId)
            {
                // Kiểm tra xem đối tượng có nằm trọn trong khung ngắm không
                if (target.IsInViewfinder(cam, viewfinderRect, playerPosition))
                {
                    // LƯU ẢNH TẠM VÀO BỘ NHỚ
                    questPhoto = snappedPhoto;

                    // Chuyển sang Trạng thái 2: Đã chụp đúng bức ảnh yêu cầu
                    SetQuestState(QuestState.PhotoTaken);
                    return;
                }
            }
        }
    }

    public void ShowToast(string message)
    {
        toastMessage = message;
        toastTimer = toastDuration;
    }

    private void PlayQuestSound()
    {
        if (audioSource != null && successChimeClip != null)
        {
            audioSource.pitch = UnityEngine.Random.Range(0.98f, 1.02f);
            audioSource.PlayOneShot(successChimeClip, 0.85f);
        }
    }
    #endregion

    #region Auto-Spawn Quest World (Bác An & Cây Đào Phai)
    /// <summary>
    /// Tự động tạo Bác An và Cây Đào Phai nếu chưa có trong Scene để bạn test ngay tức thì
    /// </summary>
    public void SetupQuestWorldObjects()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;

        // 1. Tạo Bác An (NPC_BacAn) nếu chưa có
        NPCInteractable existingNpc = FindAnyObjectByType<NPCInteractable>();
        if (existingNpc == null)
        {
            GameObject bacAnObj = new GameObject("NPC_BacAn");
            bacAnObj.transform.position = playerPos + new Vector3(2.5f, 0.5f, 0f);

            SpriteRenderer sr = bacAnObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSimpleSquareSprite();
            sr.color = new Color(0.88f, 0.48f, 0.28f, 1f); // Màu áo cam đất ấm
            sr.sortingOrder = 5;

            // Khăn choàng ấm áp
            GameObject scarfObj = new GameObject("Scarf");
            scarfObj.transform.SetParent(bacAnObj.transform);
            scarfObj.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            scarfObj.transform.localScale = new Vector3(0.75f, 0.25f, 1f);
            SpriteRenderer scarfSr = scarfObj.AddComponent<SpriteRenderer>();
            scarfSr.sprite = CreateSimpleSquareSprite();
            scarfSr.color = new Color(0.75f, 0.35f, 0.25f, 1f);
            scarfSr.sortingOrder = 6;

            NPCInteractable npc = bacAnObj.AddComponent<NPCInteractable>();
            npc.npcName = "Bác An (Chủ Nhà)";
        }

        // 2. Tạo Cây Đào Phai (PeachBlossomTree) nếu chưa có
        PhotoTarget existingTarget = FindAnyObjectByType<PhotoTarget>();
        if (existingTarget == null)
        {
            GameObject treeObj = new GameObject("Cây Đào Phai (Peach Blossom)");
            treeObj.transform.position = playerPos + new Vector3(5.5f, 2.0f, 0f);

            // Gốc thân cây
            GameObject trunk = new GameObject("Trunk");
            trunk.transform.SetParent(treeObj.transform);
            trunk.transform.localPosition = new Vector3(0f, -0.4f, 0f);
            trunk.transform.localScale = new Vector3(0.45f, 1.2f, 1f);
            SpriteRenderer trunkSr = trunk.AddComponent<SpriteRenderer>();
            trunkSr.sprite = CreateSimpleSquareSprite();
            trunkSr.color = new Color(0.42f, 0.28f, 0.18f, 1f); // Thân gỗ nâu sẫm
            trunkSr.sortingOrder = 3;

            // Tán hoa đào phai hồng phấn
            GameObject canopy = new GameObject("Canopy_Blossom");
            canopy.transform.SetParent(treeObj.transform);
            canopy.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            canopy.transform.localScale = new Vector3(2.2f, 2.0f, 1f);
            SpriteRenderer canopySr = canopy.AddComponent<SpriteRenderer>();
            canopySr.sprite = CreateSimpleSquareSprite();
            canopySr.color = new Color(1.0f, 0.72f, 0.80f, 0.95f); // Màu hồng đào phai
            canopySr.sortingOrder = 4;

            // Các đốm hoa đào nhỏ lung linh
            for (int i = 0; i < 4; i++)
            {
                GameObject flower = new GameObject("Petals_" + i);
                flower.transform.SetParent(canopy.transform);
                float angle = i * 90f * Mathf.Deg2Rad;
                flower.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.35f, Mathf.Sin(angle) * 0.35f, 0f);
                flower.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
                SpriteRenderer flowerSr = flower.AddComponent<SpriteRenderer>();
                flowerSr.sprite = CreateSimpleSquareSprite();
                flowerSr.color = new Color(1.0f, 0.88f, 0.92f, 1f);
                flowerSr.sortingOrder = 5;
            }

            // Gắn PhotoTarget để máy ảnh có thể nhận diện và chụp
            PhotoTarget pt = treeObj.AddComponent<PhotoTarget>();
            pt.targetId = requiredTargetId;
            pt.targetDisplayName = "Cành Đào Phai";
            pt.maxCaptureDistance = 14.0f;
            pt.targetSubjectRadius = 1.5f;

            Debug.Log("[GameManager] 🌸 Đã sinh Cây Đào Phai (PhotoTarget) tại toạ độ: " + treeObj.transform.position);
        }
    }

    private Sprite CreateSimpleSquareSprite()
    {
        Texture2D texture = new Texture2D(32, 32);
        Color[] cols = new Color[32 * 32];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.white;
        texture.SetPixels(cols);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
    }

    private AudioClip GenerateSuccessChimeClip()
    {
        int sampleRate = 44100;
        float duration = 0.5f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float env = Mathf.Pow(1f - t, 1.8f);
            // Hợp âm đôi E5 (659Hz) và G#5 (830Hz) ấm áp
            float tone1 = Mathf.Sin(2f * Mathf.PI * 659.25f * ((float)i / sampleRate));
            float tone2 = Mathf.Sin(2f * Mathf.PI * 830.61f * ((float)i / sampleRate));
            samples[i] = (tone1 * 0.6f + tone2 * 0.4f) * env * 0.5f;
        }

        AudioClip clip = AudioClip.Create("Synthesized_SuccessChime", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
    #endregion

    #region GUI Drawing - Bảng Nhiệm Vụ & Thông Báo Toast
    private void OnGUI()
    {
        InitGUIStyles();

        Color oldColor = GUI.color;

        // 1. BẢNG THEO DÕI NHIỆM VỤ (QUEST TRACKER HUD) - Nằm ở góc trên bên phải
        float hudW = 310f;
        float hudH = 100f;
        Rect hudRect = new Rect(Screen.width - hudW - 20f, 20f, hudW, hudH);

        // Bóng đổ
        GUI.color = new Color(0f, 0f, 0f, 0.35f);
        GUI.DrawTexture(new Rect(hudRect.x + 4, hudRect.y + 4, hudRect.width, hudRect.height), darkPixel);

        // Nền tối ấm áp
        GUI.color = new Color(0.12f, 0.14f, 0.18f, 0.93f);
        GUI.DrawTexture(hudRect, darkPixel);

        // Viền vàng kim
        GUI.color = new Color(0.85f, 0.72f, 0.45f, 0.95f);
        DrawFrameBorders(hudRect, 1.5f);

        // Tiêu đề bảng nhiệm vụ
        GUI.color = Color.white;
        Rect titleRect = new Rect(hudRect.x + 12, hudRect.y + 8, hudRect.width - 24, 24);
        GUI.Label(titleRect, "🌸 NHIỆM VỤ: MÙA ĐÀO PHAI", questTitleStyle);

        // Nội dung mục tiêu hiện tại theo QuestState
        Rect descRect = new Rect(hudRect.x + 12, hudRect.y + 36, hudRect.width - 24, 56);
        string questStatusText = "";
        Color statusColor = Color.white;

        switch (currentQuestState)
        {
            case QuestState.NotStarted:
                questStatusText = "• Bước 1: Đến gặp và trò chuyện cùng Bác An trước hiên nhà [Phím E].";
                statusColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                break;
            case QuestState.QuestAccepted:
                questStatusText = "• Bước 2: Dùng máy ảnh [Phím Space] chụp lại Cành Đào Phai trước hiên nhà [0/1].";
                statusColor = new Color(1.0f, 0.85f, 0.45f, 1f);
                break;
            case QuestState.PhotoTaken:
                questStatusText = "• Bước 3: Đã chụp được ảnh! Hãy mang về đưa cho Bác An [Phím E] [1/1].";
                statusColor = new Color(0.55f, 0.95f, 0.65f, 1f);
                break;
            case QuestState.Completed:
                questStatusText = "• Hoàn thành: Bác An đã nhận được bức ảnh cành đào phai ấm áp. ✓";
                statusColor = new Color(0.45f, 0.85f, 1.0f, 1f);
                break;
        }

        GUIStyle currentStatusStyle = new GUIStyle(questObjectiveStyle);
        currentStatusStyle.normal.textColor = statusColor;
        GUI.Label(descRect, questStatusText, currentStatusStyle);

        // 2. THÔNG BÁO TOAST NỔI KHI CẬP NHẬT TRẠNG THÁI
        if (toastTimer > 0f)
        {
            float alpha = Mathf.Clamp01(toastTimer / 0.8f);
            float toastW = Mathf.Min(Screen.width * 0.7f, 620f);
            float toastH = 46f;
            Rect toastRect = new Rect((Screen.width - toastW) * 0.5f, 90f, toastW, toastH);

            GUI.color = new Color(0.1f, 0.12f, 0.16f, 0.95f * alpha);
            GUI.DrawTexture(toastRect, darkPixel);

            GUI.color = new Color(0.95f, 0.78f, 0.38f, alpha);
            DrawFrameBorders(toastRect, 1.5f);

            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.Label(toastRect, toastMessage, toastStyle);
        }

        GUI.color = oldColor;
    }

    private void DrawFrameBorders(Rect r, float thickness)
    {
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, thickness), whitePixel);
        GUI.DrawTexture(new Rect(r.x, r.yMax - thickness, r.width, thickness), whitePixel);
        GUI.DrawTexture(new Rect(r.x, r.y, thickness, r.height), whitePixel);
        GUI.DrawTexture(new Rect(r.xMax - thickness, r.y, thickness, r.height), whitePixel);
    }

    private void InitGUIStyles()
    {
        if (stylesInitialized) return;

        questTitleStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        questTitleStyle.normal.textColor = new Color(0.98f, 0.85f, 0.45f, 1f);

        questObjectiveStyle = new GUIStyle
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 12,
            wordWrap = true
        };
        questObjectiveStyle.normal.textColor = Color.white;

        toastStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        toastStyle.normal.textColor = new Color(1.0f, 0.95f, 0.85f, 1f);

        stylesInitialized = true;
    }
    #endregion

    #region Context Menus for Testing
    [ContextMenu("Chuyển: Trạng thái 0 (Chưa nhận việc)")]
    public void DebugSetState0() => SetQuestState(QuestState.NotStarted);

    [ContextMenu("Chuyển: Trạng thái 1 (Đã nhận việc)")]
    public void DebugSetState1() => SetQuestState(QuestState.QuestAccepted);

    [ContextMenu("Chuyển: Trạng thái 2 (Đã chụp ảnh cành đào phai)")]
    public void DebugSetState2() => SetQuestState(QuestState.PhotoTaken);

    [ContextMenu("Chuyển: Trạng thái 3 (Hoàn thành nhiệm vụ)")]
    public void DebugSetState3() => SetQuestState(QuestState.Completed);

    [ContextMenu("Reset Nhiệm Vụ")]
    public void ResetQuest()
    {
        questPhoto = null;
        SetQuestState(QuestState.NotStarted);
    }
    #endregion
}
