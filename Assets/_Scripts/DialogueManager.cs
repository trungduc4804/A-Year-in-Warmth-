using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hệ thống quản lý hộp thoại (Dialogue Manager) cho "A Year in Warmth".
/// - Hiển thị hộp thoại phong cách ấm áp (Cozy Aesthetic).
/// - Hiệu ứng chữ chạy máy đánh chữ (Typewriter Effect) thư thái.
/// - Phát âm thanh gõ nhẹ (Soft Blip) ấm áp khi chữ xuất hiện.
/// - Tự động tạm khóa nhân vật khi đang nói chuyện, mở lại khi kết thúc.
/// - Tự động tạo NPC mẫu (NPC_Rowan) nếu trong Scene chưa có NPC nào để bạn test ngay.
/// </summary>
[DisallowMultipleComponent]
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    #region Serialized Fields
    [Header("=== CÀI ĐẶT HỘP THOẠI (DIALOGUE SETTINGS) ===")]
    [Tooltip("Phím để tiếp tục câu thoại hoặc hoàn tất nhanh câu đang gõ.")]
    public KeyCode continueKey = KeyCode.E;

    [Tooltip("Phím phụ để tiếp tục câu thoại (phím Cách Space).")]
    public KeyCode secondaryContinueKey = KeyCode.Space;

    [Tooltip("Tốc độ gõ chữ máy đánh chữ (giây/ký tự). 0.025 - 0.035s tạo nhịp điệu thư thái.")]
    [Range(0.01f, 0.08f)]
    public float typingSpeed = 0.03f;

    [Tooltip("Âm thanh gõ chữ nhẹ nhàng (nếu để trống, script sẽ tự tổng hợp âm thanh gỗ ấm).")]
    public AudioClip customTypeSound;
    #endregion

    #region Public Properties
    /// <summary> Đang có cuộc trò chuyện diễn ra hay không </summary>
    public bool IsDialogueActive => isDialogueActive;

    /// <summary> Tên NPC đang trò chuyện </summary>
    public string CurrentSpeakerName => currentSpeakerName;
    #endregion

    #region Events
    public event Action<string> OnDialogueStarted;
    public event Action OnDialogueEnded;
    public event Action<int> OnLineChanged;
    #endregion

    #region Private State
    private PlayerController player;
    private AudioSource audioSource;
    private AudioClip typeSoundClip;

    private bool isDialogueActive = false;
    private string currentSpeakerName = "";
    private string[] dialogueLines;
    private int currentLineIndex = 0;

    // Typewriter State
    private string targetFullText = "";
    private string displayedText = "";
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    // Box Animation
    private float boxAnimProgress = 0f;

    // GUI Textures & Styles
    private Texture2D whitePixel;
    private Texture2D darkPixel;
    private GUIStyle speakerNameStyle;
    private GUIStyle bodyTextStyle;
    private GUIStyle continuePromptStyle;
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

        // 1. Tìm PlayerController
        player = FindAnyObjectByType<PlayerController>();

        // 2. Cấu hình Audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        typeSoundClip = customTypeSound != null ? customTypeSound : GenerateSoftWoodClickClip();

        // 3. Khởi tạo Texture cơ bản
        whitePixel = new Texture2D(1, 1);
        whitePixel.SetPixel(0, 0, Color.white);
        whitePixel.Apply();

        darkPixel = new Texture2D(1, 1);
        darkPixel.SetPixel(0, 0, new Color(0.12f, 0.14f, 0.18f, 0.92f));
        darkPixel.Apply();
    }

    private void Update()
    {
        if (!isDialogueActive) return;

        // Hoạt ảnh xuất hiện của hộp thoại
        if (boxAnimProgress < 1.0f)
        {
            boxAnimProgress = Mathf.MoveTowards(boxAnimProgress, 1.0f, Time.deltaTime * 6.0f);
        }

        // Lắng nghe phím chuyển thoại
        if (Input.GetKeyDown(continueKey) || Input.GetKeyDown(secondaryContinueKey) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            OnContinuePressed();
        }
    }
    #endregion

    #region Dialogue Flow
    private Action onDialogueCompleteCallback;

    /// <summary>
    /// Bắt đầu một đoạn hội thoại với NPC, có thể truyền callback khi kết thúc hội thoại
    /// </summary>
    public void StartDialogue(string speakerName, string[] lines, Action onComplete = null)
    {
        if (lines == null || lines.Length == 0) return;

        if (player == null) player = FindAnyObjectByType<PlayerController>();

        isDialogueActive = true;
        currentSpeakerName = speakerName;
        dialogueLines = lines;
        currentLineIndex = 0;
        boxAnimProgress = 0f;
        onDialogueCompleteCallback = onComplete;

        // Khóa di chuyển nhân vật khi trò chuyện
        if (player != null)
        {
            player.isInputLocked = true;
        }

        OnDialogueStarted?.Invoke(speakerName);
        ShowLine(currentLineIndex);
    }

    /// <summary>
    /// Hiển thị câu thoại ở chỉ số index với hiệu ứng máy đánh chữ
    /// </summary>
    private void ShowLine(int index)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        targetFullText = dialogueLines[index];
        displayedText = "";
        typingCoroutine = StartCoroutine(TypewriterRoutine(targetFullText));
        OnLineChanged?.Invoke(index);
    }

    private IEnumerator TypewriterRoutine(string fullText)
    {
        isTyping = true;
        for (int i = 0; i < fullText.Length; i++)
        {
            displayedText += fullText[i];

            // Phát âm thanh gõ nhẹ (bỏ qua dấu cách)
            if (!char.IsWhiteSpace(fullText[i]) && i % 2 == 0)
            {
                PlayTypeSound();
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        typingCoroutine = null;
    }

    /// <summary>
    /// Xử lý khi người chơi bấm nút tiếp tục
    /// </summary>
    private void OnContinuePressed()
    {
        if (isTyping)
        {
            // Nếu chữ đang gõ, bấm nút sẽ hiện hết toàn bộ câu ngay lập tức (Skip Typewriter)
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            displayedText = targetFullText;
            isTyping = false;
            typingCoroutine = null;
        }
        else
        {
            // Nếu câu đã gõ xong, chuyển sang câu tiếp theo
            currentLineIndex++;
            if (currentLineIndex < dialogueLines.Length)
            {
                ShowLine(currentLineIndex);
            }
            else
            {
                EndDialogue();
            }
        }
    }

    /// <summary>
    /// Kết thúc hội thoại và trả lại quyền điều khiển cho người chơi
    /// </summary>
    public void EndDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isDialogueActive = false;
        displayedText = "";
        targetFullText = "";

        // Mở lại di chuyển nhân vật
        if (player != null)
        {
            player.isInputLocked = false;
        }

        OnDialogueEnded?.Invoke();
        onDialogueCompleteCallback?.Invoke();
        onDialogueCompleteCallback = null;
        Debug.Log("[DialogueManager] Đã kết thúc cuộc trò chuyện.");
    }

    private void PlayTypeSound()
    {
        if (audioSource != null && typeSoundClip != null)
        {
            audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(typeSoundClip, 0.45f);
        }
    }
    #endregion

    #region GUI Drawing - Hộp thoại ấm áp
    private void OnGUI()
    {
        if (!isDialogueActive) return;

        InitStyles();

        // 1. Kích thước và vị trí hộp thoại (nằm ở dưới màn hình)
        float boxW = Mathf.Min(Screen.width * 0.72f, 720f);
        float boxH = 150f;
        float finalY = Screen.height - boxH - 32f;
        float currentY = Mathf.Lerp(Screen.height, finalY, boxAnimProgress);

        Rect boxRect = new Rect((Screen.width - boxW) * 0.5f, currentY, boxW, boxH);

        Color oldColor = GUI.color;

        // 2. Bóng đổ phía sau hộp thoại
        GUI.color = new Color(0f, 0f, 0f, 0.4f * boxAnimProgress);
        GUI.DrawTexture(new Rect(boxRect.x + 6, boxRect.y + 6, boxRect.width, boxRect.height), whitePixel);

        // 3. Nền hộp thoại màu xám đen/ấm áp
        GUI.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
        GUI.DrawTexture(boxRect, darkPixel);

        // 4. Viền vàng nhạt ấm áp xung quanh hộp thoại
        GUI.color = new Color(0.85f, 0.75f, 0.52f, 0.9f);
        DrawFrameBorders(boxRect, 2f);

        // 5. Thẻ tên người nói (Speaker Name Tag)
        float nameTagW = Mathf.Max(180f, currentSpeakerName.Length * 14f + 30f);
        float nameTagH = 34f;
        Rect nameTagRect = new Rect(boxRect.x + 20, boxRect.y - 20, nameTagW, nameTagH);

        // Nền thẻ tên
        GUI.color = new Color(0.88f, 0.52f, 0.28f, 1f); // Màu cam đất ấm
        GUI.DrawTexture(nameTagRect, whitePixel);
        GUI.color = new Color(0.65f, 0.35f, 0.18f, 1f);
        DrawFrameBorders(nameTagRect, 1.5f);

        // Chữ tên người nói
        GUI.color = Color.white;
        GUI.Label(nameTagRect, currentSpeakerName, speakerNameStyle);

        // 6. Nội dung câu thoại (Typewriter Text)
        Rect bodyRect = new Rect(boxRect.x + 26, boxRect.y + 24, boxRect.width - 52, boxRect.height - 52);
        GUI.color = new Color(0.95f, 0.94f, 0.92f, 1f);
        GUI.Label(bodyRect, displayedText, bodyTextStyle);

        // 7. Dấu nhắc bấm phím tiếp tục [E / Cách] ▼ nhấp nháy ở góc dưới bên phải
        if (!isTyping)
        {
            float blink = Mathf.PingPong(Time.time * 2.2f, 1.0f);
            GUI.color = new Color(1f, 0.85f, 0.45f, 0.5f + blink * 0.5f);
            string prompt = (currentLineIndex < dialogueLines.Length - 1)
                ? $"[{continueKey} / Space] Tiếp tục ▼"
                : $"[{continueKey} / Space] Kết thúc ●";

            Rect promptRect = new Rect(boxRect.xMax - 220, boxRect.yMax - 32, 200, 24);
            GUI.Label(promptRect, prompt, continuePromptStyle);
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

    private void InitStyles()
    {
        if (stylesInitialized) return;

        speakerNameStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        speakerNameStyle.normal.textColor = Color.white;

        bodyTextStyle = new GUIStyle
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 15,
            wordWrap = true
        };
        bodyTextStyle.normal.textColor = new Color(0.96f, 0.95f, 0.92f, 1f);

        continuePromptStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = 12,
            fontStyle = FontStyle.Italic
        };
        continuePromptStyle.normal.textColor = new Color(1f, 0.85f, 0.45f, 1f);

        stylesInitialized = true;
    }
    #endregion

    /// <summary>
    /// Tạo âm thanh gõ nhẹ (Soft Wooden Tap) tổng hợp tự động cho hiệu ứng typewriter
    /// </summary>
    private AudioClip GenerateSoftWoodClickClip()
    {
        int sampleRate = 44100;
        float duration = 0.045f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            float env = 1f - t;
            float freq = Mathf.Lerp(480f, 180f, t);
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * ((float)i / sampleRate)) * env * 0.45f;
        }

        AudioClip clip = AudioClip.Create("Synthesized_WoodBlip", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
