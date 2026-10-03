#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Công cụ tự động thiết lập Animator Controller 4 hướng cho nhân vật Arthur trong Unity Editor.
/// - Tự động tạo 4 Animation Clip Đứng yên (IdleDown, IdleRight, IdleUp, IdleLeft).
/// - Cấu hình 2 Blend Tree 2D Simple Directional (Idle Tree & Walk Tree) trong Player.controller.
/// - Tự động liên kết các tham số: MoveX, MoveY, LastMoveX, LastMoveY, IsMoving, Speed.
/// - Thiết lập chuyển trạng thái (Transitions) tức thời mượt mà (Has Exit Time = false, Duration = 0).
/// </summary>
public static class PlayerAnimatorSetup
{
    private const string ControllerPath = "Assets/_Animation/Player.controller";
    private const string SpriteSheetPath = "Assets/_Sprite/Arthur/Arthur_4Direction_48px.png";
    private const string AnimationFolder = "Assets/_Animation";

    [MenuItem("Tools/A Year in Warmth/Setup Player 4-Direction Animator")]
    public static void SetupAnimator()
    {
        Debug.Log("[PlayerAnimatorSetup] 🚀 Bắt đầu thiết lập hệ thống Animation 4 hướng cho Arthur...");

        // 1. Tải Controller
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"[PlayerAnimatorSetup] Không tìm thấy AnimatorController tại {ControllerPath}!");
            return;
        }

        // 2. Tải các sprite từ SpriteSheet
        Sprite[] allSprites = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();

        if (allSprites.Length == 0)
        {
            Debug.LogError($"[PlayerAnimatorSetup] Không tìm thấy Sprite nào trong {SpriteSheetPath}!");
            return;
        }

        // Theo README:
        // Hàng 0: Down  (0, 1, 2, 3) -> Idle là frame 1 (Arthur_4Direction_48px_1)
        // Hàng 1: Right (4, 5, 6, 7) -> Idle là frame 5 (Arthur_4Direction_48px_5)
        // Hàng 2: Up    (8, 9, 10, 11) -> Idle là frame 9 (Arthur_4Direction_48px_9)
        // Hàng 3: Left  (12, 13, 14, 15) -> Idle là frame 13 (Arthur_4Direction_48px_13)
        Sprite idleDownSprite = FindSpriteByName(allSprites, "Arthur_4Direction_48px_1") ?? allSprites[1];
        Sprite idleRightSprite = FindSpriteByName(allSprites, "Arthur_4Direction_48px_5") ?? allSprites[5];
        Sprite idleUpSprite = FindSpriteByName(allSprites, "Arthur_4Direction_48px_9") ?? allSprites[9];
        Sprite idleLeftSprite = FindSpriteByName(allSprites, "Arthur_4Direction_48px_13") ?? allSprites[13];

        // 3. Tạo hoặc lấy 4 Animation Clip Idle
        AnimationClip idleDownClip = GetOrCreateIdleClip("IdleDown", idleDownSprite);
        AnimationClip idleRightClip = GetOrCreateIdleClip("IdleRight", idleRightSprite);
        AnimationClip idleUpClip = GetOrCreateIdleClip("IdleUp", idleUpSprite);
        AnimationClip idleLeftClip = GetOrCreateIdleClip("IdleLeft", idleLeftSprite);

        // 4. Lấy 4 Animation Clip Walk đã có sẵn
        AnimationClip walkDownClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationFolder}/Walk.anim");
        AnimationClip walkRightClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationFolder}/WalkRight.anim");
        AnimationClip walkUpClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationFolder}/WalkUp.anim");
        AnimationClip walkLeftClip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimationFolder}/WalkLeft.anim");

        if (walkDownClip == null || walkRightClip == null || walkUpClip == null || walkLeftClip == null)
        {
            Debug.LogError("[PlayerAnimatorSetup] Thiếu một trong các clip Walk (Walk, WalkRight, WalkUp, WalkLeft) trong Assets/_Animation!");
            return;
        }

        // 5. Cấu hình Parameters trong AnimatorController
        EnsureParameter(controller, "MoveX", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "MoveY", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "LastMoveX", AnimatorControllerParameterType.Float, 0f);
        EnsureParameter(controller, "LastMoveY", AnimatorControllerParameterType.Float, -1f); // Mặc định nhìn xuống
        EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "IsJogging", AnimatorControllerParameterType.Bool);

        // 6. Xóa các State cũ trong Layer 0 để dựng cây chuẩn
        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState childState in sm.states.ToArray())
        {
            sm.RemoveState(childState.state);
        }

        // 7. Tạo Blend Tree cho Trạng thái Đứng Yên (IDLE)
        AnimatorState idleState = controller.CreateBlendTreeInController("Idle", out BlendTree idleTree, 0);
        idleState.name = "Idle";
        idleTree.name = "Idle Blend Tree";
        idleTree.blendType = BlendTreeType.SimpleDirectional2D;
        idleTree.blendParameter = "LastMoveX";
        idleTree.blendParameterY = "LastMoveY";

        idleTree.AddChild(idleDownClip, new Vector2(0f, -1f));
        idleTree.AddChild(idleUpClip, new Vector2(0f, 1f));
        idleTree.AddChild(idleLeftClip, new Vector2(-1f, 0f));
        idleTree.AddChild(idleRightClip, new Vector2(1f, 0f));

        // 8. Tạo Blend Tree cho Trạng thái Di Chuyển (WALK)
        AnimatorState walkState = controller.CreateBlendTreeInController("Walk", out BlendTree walkTree, 0);
        walkState.name = "Walk";
        walkTree.name = "Walk Blend Tree";
        walkTree.blendType = BlendTreeType.SimpleDirectional2D;
        walkTree.blendParameter = "MoveX";
        walkTree.blendParameterY = "MoveY";

        walkTree.AddChild(walkDownClip, new Vector2(0f, -1f));
        walkTree.AddChild(walkUpClip, new Vector2(0f, 1f));
        walkTree.AddChild(walkLeftClip, new Vector2(-1f, 0f));
        walkTree.AddChild(walkRightClip, new Vector2(1f, 0f));

        // Vị trí các node trên đồ thị Animator
        ChildAnimatorState[] states = sm.states;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].state == idleState)
            {
                states[i].position = new Vector3(300f, 100f, 0f);
            }
            else if (states[i].state == walkState)
            {
                states[i].position = new Vector3(300f, 220f, 0f);
            }
        }
        sm.states = states;
        sm.defaultState = idleState;

        // 9. Tạo Chuyển trạng thái (Transitions) giữa Idle và Walk
        // Idle -> Walk (khi bắt đầu di chuyển)
        AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
        toWalk.hasExitTime = false;
        toWalk.hasFixedDuration = true;
        toWalk.duration = 0f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");

        // Walk -> Idle (khi dừng lại)
        AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.hasFixedDuration = true;
        toIdle.duration = 0f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");

        // 10. Lưu lại thay đổi
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[PlayerAnimatorSetup] 🎉 ĐÃ THIẾT LẬP HOÀN TẤT ANIMATOR 4 HƯỚNG CHO ARTHUR!\n" +
                  "• 2 Blend Tree 2D: Idle (theo LastMoveX, LastMoveY) & Walk (theo MoveX, MoveY).\n" +
                  "• 4 Hướng: Xuống (0, -1), Lên (0, 1), Trái (-1, 0), Phải (1, 0).\n" +
                  "• Chuyển đổi tức thì khi IsMoving = true/false (Duration = 0s).");
    }

    /// <summary>
    /// Tự động chạy thiết lập 1 lần khi Unity nạp xong nếu controller chưa có tham số nào
    /// </summary>
    [InitializeOnLoadMethod]
    private static void AutoSetupIfEmpty()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller != null && controller.parameters.Length == 0)
        {
            SetupAnimator();
        }
    }

    private static Sprite FindSpriteByName(Sprite[] sprites, string name)
    {
        return sprites.FirstOrDefault(s => s.name.Equals(name, System.StringComparison.OrdinalIgnoreCase));
    }

    private static AnimationClip GetOrCreateIdleClip(string clipName, Sprite sprite)
    {
        string path = $"{AnimationFolder}/{clipName}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = clipName };

            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = "",
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[1];
            keyframes[0] = new ObjectReferenceKeyframe { time = 0f, value = sprite };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, path);
            Debug.Log($"[PlayerAnimatorSetup] Đã tạo clip: {path}");
        }
        return clip;
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type, float defaultVal = 0f)
    {
        if (controller.parameters.Any(p => p.name == name)) return;

        controller.AddParameter(new AnimatorControllerParameter
        {
            name = name,
            type = type,
            defaultFloat = defaultVal
        });
    }
}
#endif
