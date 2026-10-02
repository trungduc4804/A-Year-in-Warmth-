using UnityEngine;

/// <summary>
/// Bộ điều khiển nhân vật Top-Down 8 hướng (PlayerController).
/// Thiết kế tối ưu cho trải nghiệm thư thái, dạo bước ngắm cảnh (Cozy / Scenic Pacing).
/// Tương thích chuẩn Unity 6 (rb.linearVelocity) và hệ thống Physics 2D / Transform.
/// </summary>
[DisallowMultipleComponent]
public class PlayerController : MonoBehaviour
{
    #region Enums & Structures
    public enum FacingMode
    {
        [Tooltip("Lật mặt nhân vật qua trục X (chuẩn cho game 2D RPG/Pixel Art như Stardew Valley).")]
        FlipHorizontalOnly,

        [Tooltip("Xoay góc mượt mà hướng về phía đang di chuyển (chuẩn cho góc nhìn 360 độ).")]
        SmoothRotateTowardsDirection,

        [Tooltip("Xoay cố định theo 8 góc chuẩn (0, 45, 90, 135, 180, 225, 270, 315 độ).")]
        Snap8WayRotation,

        [Tooltip("Không can thiệp xoay/lật (dành cho trường hợp Animator điều khiển Sprite).")]
        None
    }

    public enum Direction8Way
    {
        Idle,
        Up,
        Down,
        Left,
        Right,
        UpLeft,
        UpRight,
        DownLeft,
        DownRight
    }
    #endregion

    #region Serialized Fields - Pacing & Movement
    [Header("=== TỐC ĐỘ DI CHUYỂN (COZY PACING) ===")]
    [Tooltip("Tốc độ đi bộ thư thả, ngắm cảnh (Khuyên dùng: 2.8 - 3.2 để người chơi cảm nhận được không gian, cảnh quan).")]
    [Range(1.0f, 6.0f)]
    public float walkSpeed = 3.0f;

    [Tooltip("Tốc độ khi chạy nhẹ / rảo bước nhanh (khi giữ phím Shift).")]
    [Range(2.0f, 10.0f)]
    public float jogSpeed = 4.8f;

    [Tooltip("Cho phép người chơi chuyển sang chạy nhẹ khi cần.")]
    public bool enableJog = true;

    [Tooltip("Phím giữ để chạy nhẹ.")]
    public KeyCode jogKey = KeyCode.LeftShift;

    [Header("=== ĐỘ MƯỢT MÀ & QUÁN TÍNH (SMOOTHNESS) ===")]
    [Tooltip("Dừng lại ngay lập tức khi thả phím (khuyên dùng bật để nhân vật đứng yên dứt khoát, không bị trôi tiếp 1 đoạn).")]
    public bool stopImmediatelyOnRelease = true;

    [Tooltip("Thời gian tăng tốc mượt khi bắt đầu di chuyển (0 = tức thì, 0.04s = mượt mà tự nhiên).")]
    [Range(0.0f, 0.2f)]
    public float accelerationTime = 0.04f;

    [Tooltip("Thời gian hãm phanh khi thả phím (giây). Chỉ có tác dụng khi tắt 'Stop Immediately On Release'.")]
    [Range(0.0f, 0.4f)]
    public float decelerationTime = 0.0f;

    [Header("=== HƯỚNG NHÌN 8 HƯỚNG & XOAY (FACING) ===")]
    [Tooltip("Chế độ hướng nhìn khi di chuyển 8 hướng.")]
    public FacingMode facingMode = FacingMode.FlipHorizontalOnly;

    [Tooltip("Tốc độ xoay (độ/giây) khi dùng chế độ SmoothRotateTowardsDirection.")]
    public float rotationSpeed = 720.0f;

    [Tooltip("Góc bù ban đầu nếu Sprite gốc quay về hướng khác (mặc định 0 độ = Hướng Phải, 90 độ = Hướng Lên).")]
    public float rotationAngleOffset = -90.0f;

    [Tooltip("SpriteRenderer của nhân vật (tự động nhận nếu để trống).")]
    public SpriteRenderer spriteRenderer;

    [Header("=== HIỆU ỨNG BƯỚC ĐI THƯ THẢ (WALK BOBBING) ===")]
    [Tooltip("Tạo độ nhấp nhô nhẹ nhàng khi rảo bước, làm tăng cảm giác thư thái và sống động.")]
    public bool enableWalkBobbing = true;

    [Tooltip("Transform phần đồ họa cần nhấp nhô (để trống sẽ tạo tự động hoặc dùng chính SpriteRenderer).")]
    public Transform visualTransform;

    [Tooltip("Nhịp điệu bước chân (tần số nhấp nhô theo giây).")]
    [Range(4.0f, 16.0f)]
    public float bobFrequency = 9.0f;

    [Tooltip("Độ nảy nhấp nhô theo trục Y (nên để nhỏ 0.03 - 0.06 để bước đi tự nhiên).")]
    [Range(0.01f, 0.12f)]
    public float bobAmplitude = 0.04f;

    [Header("=== TRẠNG THÁI KHÓA ĐIỀU KHIỂN ===")]
    [Tooltip("Khóa di chuyển nhân vật (dùng khi đang mở khung ngắm máy ảnh, hội thoại...).")]
    public bool isInputLocked = false;

    [Tooltip("Khóa camera theo sau nhân vật (để ống kính máy ảnh tự do di chuyển ngắm cảnh).")]
    public bool isCameraFollowLocked = false;

    [Header("=== THEO DÕI CAMERA (SMOOTH CAMERA FOLLOW) ===")]
    [Tooltip("Gắn Main Camera vào đây để camera bám theo nhân vật một cách êm ái.")]
    public Transform cameraTransform;

    [Tooltip("Bật/tắt camera tự động bám theo.")]
    public bool enableCameraFollow = true;

    [Tooltip("Độ trễ làm mượt của camera (0.2 - 0.35s cho cảm giác ngắm cảnh thư thái như phim).")]
    [Range(0.05f, 0.6f)]
    public float cameraSmoothTime = 0.25f;

    [Tooltip("Khoảng cách bù vị trí Camera so với nhân vật (Z thường là -10 trong 2D).")]
    public Vector3 cameraOffset = new Vector3(0f, 0f, -10f);

    [Header("=== DEBUG & KIỂM TRA TRẢI NGHIỆM ===")]
    [Tooltip("Hiển thị bảng thông tin tốc độ và hướng nhìn trực tiếp trên màn hình Play Mode để dễ kiểm tra cảm giác lái.")]
    public bool showDebugHUD = true;
    #endregion

    #region Private Runtime State
    private Rigidbody2D rb;
    private Animator animator;

    // Movement state
    private Vector2 inputVector;
    private Vector2 normalizedMoveDirection;
    private Vector2 currentVelocity;
    private Vector2 velocityDampRef;
    private bool isJogging;
    private bool isMoving;

    // 8-Direction state
    private Direction8Way current8WayDirection = Direction8Way.Idle;
    private Vector2 lastFacingVector = Vector2.down; // Mặc định nhìn về phía trước/dưới

    // Visual Bobbing state
    private Vector3 initialVisualLocalPosition;
    private float bobTimer;

    // Camera follow state
    private Vector3 cameraVelocityRef;

    // Animator parameter hashes (nếu có Animator gắn trên nhân vật)
    private static readonly int AnimHashMoveX = Animator.StringToHash("MoveX");
    private static readonly int AnimHashMoveY = Animator.StringToHash("MoveY");
    private static readonly int AnimHashLastMoveX = Animator.StringToHash("LastMoveX");
    private static readonly int AnimHashLastMoveY = Animator.StringToHash("LastMoveY");
    private static readonly int AnimHashSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimHashIsMoving = Animator.StringToHash("IsMoving");
    private static readonly int AnimHashIsJogging = Animator.StringToHash("IsJogging");
    #endregion

    #region Public Properties
    /// <summary> Thuộc tính cũ để tương thích ngược (ánh xạ sang walkSpeed) </summary>
    [System.Obsolete("Dùng walkSpeed thay thế để kiểm soát nhịp độ đi bộ thư thả.")]
    public float moveSpeed
    {
        get => walkSpeed;
        set => walkSpeed = value;
    }

    /// <summary> Vận tốc di chuyển thực tế hiện tại (units/giây) </summary>
    public Vector2 CurrentVelocity => currentVelocity;

    /// <summary> Tốc độ di chuyển thực tế hiện tại (magnitude) </summary>
    public float CurrentSpeed => currentVelocity.magnitude;

    /// <summary> Hướng nhìn 8 hướng hiện tại </summary>
    public Direction8Way Current8WayDirection => current8WayDirection;

    /// <summary> Vector hướng nhìn cuối cùng (chuẩn hoá) </summary>
    public Vector2 LastFacingVector => lastFacingVector;

    /// <summary> Đang ở trạng thái di chuyển hay đứng yên </summary>
    public bool IsMoving => isMoving;

    /// <summary> Đang chạy nhẹ hay đi bộ </summary>
    public bool IsJogging => isJogging;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // 1. Tự động lấy các component liên quan nếu chưa gán
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (visualTransform == null)
        {
            // Ưu tiên tìm GameObject con chứa SpriteRenderer để nhấp nhô độc lập với toạ độ gốc
            foreach (Transform child in transform)
            {
                if (child.GetComponent<SpriteRenderer>() != null)
                {
                    visualTransform = child;
                    break;
                }
            }
        }

        if (visualTransform != null && visualTransform != transform)
        {
            initialVisualLocalPosition = visualTransform.localPosition;
        }

        // 2. Cấu hình Rigidbody2D nếu có
        if (TryGetComponent<Rigidbody2D>(out rb))
        {
            rb.gravityScale = 0f; // Tránh trọng lực kéo rơi nhân vật trong game Top-Down
            rb.freezeRotation = true; // Tránh va chạm làm xoay lật nhân vật vật lý
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Khử hiện tượng giật hình (micro-stutter)
        }

        // 3. Kiểm tra Animator nếu có
        if (TryGetComponent<Animator>(out animator))
        {
            CacheAnimatorParameters();
        }

        // 4. Tự động tìm Main Camera nếu cameraTransform đang trống
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // 5. Tự động gắn ViewfinderController (Cơ chế ngắm chụp ảnh) nếu chưa có
        if (FindAnyObjectByType<ViewfinderController>() == null)
        {
            gameObject.AddComponent<ViewfinderController>();
        }

        // 6. Tự động gắn DialogueManager (Hệ thống hộp thoại tương tác NPC) nếu chưa có
        if (FindAnyObjectByType<DialogueManager>() == null)
        {
            gameObject.AddComponent<DialogueManager>();
        }

        // 7. Tự động gắn GameManager (Hệ thống Trạng thái Game & Quản lý Nhiệm vụ) nếu chưa có
        if (FindAnyObjectByType<GameManager>() == null)
        {
            gameObject.AddComponent<GameManager>();
        }

        // 8. Tự động gắn AlbumUIController (Cuốn Album Kỷ Niệm của Arthur) nếu chưa có
        if (FindAnyObjectByType<AlbumUIController>() == null)
        {
            gameObject.AddComponent<AlbumUIController>();
        }

        // 9. Tự động gắn BanhChungMinigame (Minigame gói bánh chưng truyền thống) nếu chưa có
        if (FindAnyObjectByType<BanhChungMinigame>() == null)
        {
            gameObject.AddComponent<BanhChungMinigame>();
        }
    }

    private void Update()
    {
        ReadInput();
        Update8WayDirection();
        CalculateSmoothMovement();
        HandleFacing();
        HandleWalkBobbing();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        // Di chuyển bằng Rigidbody2D trong FixedUpdate để đảm bảo va chạm vật lý chuẩn xác
        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = currentVelocity;
#else
            rb.velocity = currentVelocity;
#endif
        }
    }

    private void LateUpdate()
    {
        // Nếu không có Rigidbody2D, di chuyển mượt mà trực tiếp qua transform (bao gồm cả quán tính khi thả phím)
        if (rb == null && currentVelocity.sqrMagnitude > 0.00001f)
        {
            transform.position += (Vector3)(currentVelocity * Time.deltaTime);
        }

        HandleCameraFollow();
    }
    #endregion

    #region Input & Direction Logic
    /// <summary>
    /// Thu thập input bàn phím (WASD / Phím mũi tên / Shift)
    /// </summary>
    private void ReadInput()
    {
        bool isMinigameOpen = BanhChungMinigame.Instance != null && BanhChungMinigame.Instance.IsOpen;
        if (isInputLocked || isMinigameOpen)
        {
            inputVector = Vector2.zero;
            normalizedMoveDirection = Vector2.zero;
            isMoving = false;
            isJogging = false;
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        inputVector = new Vector2(h, v);

        // Chuẩn hoá vector để di chuyển đường chéo có cùng tốc độ với đường thẳng (không bị tăng tốc 1.414 lần)
        if (inputVector.sqrMagnitude > 0.001f)
        {
            normalizedMoveDirection = inputVector.normalized;
            lastFacingVector = normalizedMoveDirection;
            isMoving = true;
        }
        else
        {
            normalizedMoveDirection = Vector2.zero;
            isMoving = false;
        }

        // Kiểm tra chạy nhẹ khi giữ phím Shift
        isJogging = enableJog && Input.GetKey(jogKey);
    }

    /// <summary>
    /// Phân loại chính xác 8 hướng di chuyển Top-Down dựa trên góc Vector
    /// </summary>
    private void Update8WayDirection()
    {
        if (!isMoving)
        {
            current8WayDirection = Direction8Way.Idle;
            return;
        }

        // Tính góc từ -180 đến 180 độ
        float angle = Mathf.Atan2(normalizedMoveDirection.y, normalizedMoveDirection.x) * Mathf.Rad2Deg;

        // Phân chia 8 rẻ quạt 45 độ (mỗi góc có biên độ +-22.5 độ)
        if (angle >= -22.5f && angle < 22.5f)
        {
            current8WayDirection = Direction8Way.Right;
        }
        else if (angle >= 22.5f && angle < 67.5f)
        {
            current8WayDirection = Direction8Way.UpRight;
        }
        else if (angle >= 67.5f && angle < 112.5f)
        {
            current8WayDirection = Direction8Way.Up;
        }
        else if (angle >= 112.5f && angle < 157.5f)
        {
            current8WayDirection = Direction8Way.UpLeft;
        }
        else if (angle >= 157.5f || angle < -157.5f)
        {
            current8WayDirection = Direction8Way.Left;
        }
        else if (angle >= -157.5f && angle < -112.5f)
        {
            current8WayDirection = Direction8Way.DownLeft;
        }
        else if (angle >= -112.5f && angle < -67.5f)
        {
            current8WayDirection = Direction8Way.Down;
        }
        else if (angle >= -67.5f && angle < -22.5f)
        {
            current8WayDirection = Direction8Way.DownRight;
        }
    }
    #endregion

    #region Smooth Movement Calculation
    /// <summary>
    /// Tính toán gia tốc (Acceleration) và hãm tốc (Deceleration) mượt mà bằng SmoothDamp
    /// </summary>
    private void CalculateSmoothMovement()
    {
        // 1. Khi người chơi thả phím (không còn input di chuyển)
        if (!isMoving)
        {
            if (stopImmediatelyOnRelease || decelerationTime <= 0.001f)
            {
                currentVelocity = Vector2.zero;
                velocityDampRef = Vector2.zero;

                if (rb != null)
                {
#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = Vector2.zero;
#else
                    rb.velocity = Vector2.zero;
#endif
                }
                return;
            }

            // Hãm phanh mượt nếu người dùng chủ động tắt stopImmediatelyOnRelease
            currentVelocity = Vector2.SmoothDamp(
                currentVelocity,
                Vector2.zero,
                ref velocityDampRef,
                decelerationTime,
                Mathf.Infinity,
                Time.deltaTime
            );

            if (currentVelocity.sqrMagnitude < 0.0001f)
            {
                currentVelocity = Vector2.zero;
                velocityDampRef = Vector2.zero;
            }
            return;
        }

        // 2. Khi đang có input di chuyển
        float targetSpeed = isJogging ? jogSpeed : walkSpeed;
        Vector2 targetVelocity = normalizedMoveDirection * targetSpeed;

        if (accelerationTime <= 0.001f)
        {
            currentVelocity = targetVelocity;
            velocityDampRef = Vector2.zero;
        }
        else
        {
            currentVelocity = Vector2.SmoothDamp(
                currentVelocity,
                targetVelocity,
                ref velocityDampRef,
                accelerationTime,
                Mathf.Infinity,
                Time.deltaTime
            );
        }
    }
    #endregion

    #region Visual Facing & Bobbing
    /// <summary>
    /// Xử lý hướng nhìn nhân vật (Lật mặt hoặc xoay 8 hướng)
    /// </summary>
    private void HandleFacing()
    {
        switch (facingMode)
        {
            case FacingMode.FlipHorizontalOnly:
                if (spriteRenderer != null && Mathf.Abs(normalizedMoveDirection.x) > 0.05f)
                {
                    // Lật mặt sang trái khi đi về bên trái, giữ nguyên khi đi về bên phải
                    spriteRenderer.flipX = normalizedMoveDirection.x < 0f;
                }
                break;

            case FacingMode.SmoothRotateTowardsDirection:
                if (isMoving)
                {
                    float targetAngle = Mathf.Atan2(normalizedMoveDirection.y, normalizedMoveDirection.x) * Mathf.Rad2Deg + rotationAngleOffset;
                    float currentAngle = transform.eulerAngles.z;
                    float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
                    transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
                }
                break;

            case FacingMode.Snap8WayRotation:
                if (isMoving)
                {
                    float rawAngle = Mathf.Atan2(normalizedMoveDirection.y, normalizedMoveDirection.x) * Mathf.Rad2Deg + rotationAngleOffset;
                    // Làm tròn về bội số 45 độ gần nhất
                    float snappedAngle = Mathf.Round(rawAngle / 45.0f) * 45.0f;
                    transform.rotation = Quaternion.Euler(0f, 0f, snappedAngle);
                }
                break;

            case FacingMode.None:
            default:
                break;
        }
    }

    /// <summary>
    /// Tạo hiệu ứng nhấp nhô nhịp bước chân thư thái khi đi dạo
    /// </summary>
    private void HandleWalkBobbing()
    {
        if (!enableWalkBobbing || visualTransform == null || visualTransform == transform) return;

        float speedRatio = currentVelocity.magnitude / Mathf.Max(walkSpeed, 0.1f);

        if (speedRatio > 0.1f)
        {
            // Tốc độ nhịp bước tỉ lệ với tốc độ di chuyển
            bobTimer += Time.deltaTime * bobFrequency * speedRatio;
            float offsetY = Mathf.Abs(Mathf.Sin(bobTimer)) * bobAmplitude;

            visualTransform.localPosition = initialVisualLocalPosition + new Vector3(0f, offsetY, 0f);
        }
        else
        {
            // Dần đưa về vị trí chuẩn khi dừng bước
            bobTimer = 0f;
            visualTransform.localPosition = Vector3.Lerp(
                visualTransform.localPosition,
                initialVisualLocalPosition,
                Time.deltaTime * 12.0f
            );
        }
    }
    #endregion

    #region Camera Follow
    /// <summary>
    /// Camera bám theo nhân vật một cách êm ái, tạo góc nhìn điện ảnh thanh bình
    /// </summary>
    private void HandleCameraFollow()
    {
        if (!enableCameraFollow || isCameraFollowLocked || cameraTransform == null) return;

        Vector3 targetCamPos = transform.position + cameraOffset;

        cameraTransform.position = Vector3.SmoothDamp(
            cameraTransform.position,
            targetCamPos,
            ref cameraVelocityRef,
            cameraSmoothTime
        );
    }
    #endregion

    #region Animator Integration
    private readonly System.Collections.Generic.HashSet<int> animatorParamHashes = new System.Collections.Generic.HashSet<int>();

    private void CacheAnimatorParameters()
    {
        animatorParamHashes.Clear();
        if (animator == null || animator.runtimeAnimatorController == null) return;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            animatorParamHashes.Add(param.nameHash);
        }
    }

    /// <summary>
    /// Đồng bộ thông số với Animator nếu có gắn trên nhân vật (an toàn, chỉ set param nào thực sự tồn tại)
    /// </summary>
    private void UpdateAnimator()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        if (animatorParamHashes.Contains(AnimHashMoveX)) animator.SetFloat(AnimHashMoveX, normalizedMoveDirection.x);
        if (animatorParamHashes.Contains(AnimHashMoveY)) animator.SetFloat(AnimHashMoveY, normalizedMoveDirection.y);
        if (animatorParamHashes.Contains(AnimHashLastMoveX)) animator.SetFloat(AnimHashLastMoveX, lastFacingVector.x);
        if (animatorParamHashes.Contains(AnimHashLastMoveY)) animator.SetFloat(AnimHashLastMoveY, lastFacingVector.y);
        if (animatorParamHashes.Contains(AnimHashSpeed)) animator.SetFloat(AnimHashSpeed, currentVelocity.magnitude);
        if (animatorParamHashes.Contains(AnimHashIsMoving)) animator.SetBool(AnimHashIsMoving, isMoving);
        if (animatorParamHashes.Contains(AnimHashIsJogging)) animator.SetBool(AnimHashIsJogging, isJogging);
    }
    #endregion

    #region In-Editor Gizmos & Debug HUD
    private void OnDrawGizmosSelected()
    {
        // 1. Vẽ vòng tròn bán kính đi bộ & tầm nhìn
        Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, 0.6f);

        // 2. Vẽ mũi tên hướng di chuyển hiện tại
        if (Application.isPlaying && currentVelocity.sqrMagnitude > 0.01f)
        {
            Gizmos.color = Color.cyan;
            Vector3 velDir = (Vector3)currentVelocity;
            Gizmos.DrawRay(transform.position, velDir);
        }
        else
        {
            Gizmos.color = new Color(1f, 0.9f, 0.4f, 0.8f);
            Gizmos.DrawRay(transform.position, (Vector3)lastFacingVector * 0.8f);
        }
    }

    private void OnGUI()
    {
        if (!showDebugHUD || !Application.isPlaying) return;

        // Bảng giao diện HUD tiện lợi để theo dõi cảm giác di chuyển khi Play Mode
        GUILayout.BeginArea(new Rect(18, 18, 330, 330), GUI.skin.box);
        
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.95f, 0.85f, 0.45f) }
        };

        GUILayout.Label("🌿 A Year in Warmth - Character Controller", headerStyle);
        GUILayout.Space(4);

        string paceState = (BanhChungMinigame.Instance != null && BanhChungMinigame.Instance.IsOpen) ? "Đang gói bánh chưng ngày Tết" :
                           (AlbumUIController.Instance != null && AlbumUIController.Instance.IsOpen) ? "Đang mở Cuốn Album Kỷ Niệm" :
                           (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) ? "Đang trò chuyện cùng NPC" :
                           isInputLocked ? "Đang ngắm máy ảnh (Viewfinder Mode)" :
                           !isMoving ? "Đang dừng chân ngắm cảnh (Idle)" :
                           isJogging ? "Rảo bước nhanh (Jogging)" : "Đi bộ thư thả (Scenic Walk)";

        GUILayout.Label($"• Trạng thái: {paceState}");
        GUILayout.Label($"• Vận tốc hiện tại: {currentVelocity.magnitude:F2} units/s");
        GUILayout.Label($"• Hướng 8 hướng: {current8WayDirection}");
        GUILayout.Label($"• Chế độ xoay: {facingMode}");

        int bagBanhChung = GameManager.Instance != null ? GameManager.Instance.BanhChungCount : 0;
        if (bagBanhChung > 0)
        {
            GUIStyle bagStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.45f, 0.95f, 0.65f) }
            };
            GUILayout.Label($"• 🎒 Túi Kỷ Niệm: 🍱 {bagBanhChung}x Bánh Chưng Tết", bagStyle);
        }
        
        GUILayout.Space(6);
        GUILayout.Label("💡 [WASD / Phím mũi tên]: Di chuyển 8 hướng");
        GUILayout.Label($"💡 [{jogKey}]: Giữ để rảo bước nhanh ({jogSpeed:F1} u/s)");
        GUILayout.Label("💡 Thả phím: Nhân vật dừng lại ngay lập tức");
        GUILayout.Label("📸 [Phím cách Space]: Mở/Đóng khung ngắm chụp ảnh");
        GUILayout.Label("💬 [E]: Tương tác trò chuyện khi lại gần NPC");
        GUILayout.Label("📖 [Tab]: Mở Cuốn Album Kỷ Niệm của Arthur");
        GUILayout.Label("🍱 [B]: Minigame Gói Bánh Chưng Ngày Tết");

        GUILayout.EndArea();
    }
    #endregion
}
