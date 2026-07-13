#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class MutantAnimatorControllerSetup
{
    private const string ControllerPath = "Assets/Animations/Zombies/Mutant zombie/Mutant zombie.controller";
    private const string IdleClipPath = "Assets/Animations/Zombies/Mutant zombie/Mutant zombie Idle base.fbx";
    private const string WalkClipPath = "Assets/Animations/Zombies/Mutant zombie/Mutant zombie walking.fbx";
    private const string RunClipPath = "Assets/Animations/Zombies/Mutant zombie/Mutant zombie running.fbx";

    private const string IdleStateName = "Idle";
    private const string WalkStateName = "Walk";
    private const string RunStateName = "Run";
    private const string IsEnragedParam = "IsEnraged";
    private const string SpeedParam = "Speed";

    [MenuItem("Tools/Animation/Setup Mutant Phase Locomotion")]
    public static void SetupFromMenu()
    {
        if (!ApplyPhaseLocomotion(showDialog: true))
            return;

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Mutant Animator",
            "Phase locomotion applied.\n\n" +
            "- Parameter: IsEnraged (Bool)\n" +
            "- Base layer: Idle / Walk / Run\n" +
            "- Walk → Run when IsEnraged == true\n\n" +
            "Upper body (punch/swipe) unchanged.",
            "OK");
    }

    public static bool ApplyPhaseLocomotion(bool showDialog = false)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"[MutantAnimator] Missing controller: {ControllerPath}");
            if (showDialog)
                EditorUtility.DisplayDialog("Mutant Animator", $"Controller not found:\n{ControllerPath}", "OK");
            return false;
        }

        AnimationClip idleClip = LoadClip(IdleClipPath, "Mutant zombie Idle base");
        AnimationClip walkClip = LoadClip(WalkClipPath, "Mutant zombie walking");
        AnimationClip runClip = LoadClip(RunClipPath, "Mutant zombie running");

        if (idleClip == null || walkClip == null || runClip == null)
        {
            Debug.LogError("[MutantAnimator] Missing idle/walk/run clips.");
            if (showDialog)
                EditorUtility.DisplayDialog("Mutant Animator", "Missing idle, walk, or run animation clip.", "OK");
            return false;
        }

        EnsureBoolParameter(controller, IsEnragedParam);

        AnimatorStateMachine root = controller.layers[0].stateMachine;
        AnimatorStateMachine locomotionMachine = GetLocomotionMachine(root);

        ClearStateMachine(locomotionMachine);

        Vector3 idlePos = new Vector3(300f, 0f, 0f);
        Vector3 walkPos = new Vector3(540f, 60f, 0f);
        Vector3 runPos = new Vector3(540f, -80f, 0f);

        AnimatorState idleState = locomotionMachine.AddState(IdleStateName, idlePos);
        AnimatorState walkState = locomotionMachine.AddState(WalkStateName, walkPos);
        AnimatorState runState = locomotionMachine.AddState(RunStateName, runPos);

        idleState.motion = idleClip;
        walkState.motion = walkClip;
        runState.motion = runClip;

        locomotionMachine.defaultState = idleState;

        AddTransition(idleState, walkState, 0.1f,
            (SpeedParam, AnimatorConditionMode.Greater, 0.1f),
            (IsEnragedParam, AnimatorConditionMode.IfNot, 0f));

        AddTransition(idleState, runState, 0.1f,
            (SpeedParam, AnimatorConditionMode.Greater, 0.1f),
            (IsEnragedParam, AnimatorConditionMode.If, 0f));

        AddTransition(walkState, idleState, 0.1f,
            (SpeedParam, AnimatorConditionMode.Less, 0.1f));

        AddTransition(runState, idleState, 0.1f,
            (SpeedParam, AnimatorConditionMode.Less, 0.1f));

        AddTransition(walkState, runState, 0.12f,
            (IsEnragedParam, AnimatorConditionMode.If, 0f));

        EditorUtility.SetDirty(controller);
        Debug.Log("[MutantAnimator] Applied Idle/Walk/Run phase locomotion on Base Layer.");
        return true;
    }

    private static AnimatorStateMachine GetLocomotionMachine(AnimatorStateMachine root)
    {
        foreach (ChildAnimatorStateMachine child in root.stateMachines)
        {
            if (child.stateMachine != null && child.stateMachine.name == "Locomotion")
                return child.stateMachine;
        }

        return root;
    }

    private static void ClearStateMachine(AnimatorStateMachine stateMachine)
    {
        ChildAnimatorState[] states = stateMachine.states;
        for (int i = states.Length - 1; i >= 0; i--)
            stateMachine.RemoveState(states[i].state);

        ChildAnimatorStateMachine[] nested = stateMachine.stateMachines;
        for (int i = nested.Length - 1; i >= 0; i--)
            stateMachine.RemoveStateMachine(nested[i].stateMachine);

        stateMachine.anyStateTransitions = new AnimatorStateTransition[0];
        stateMachine.entryTransitions = new AnimatorTransition[0];
    }

    private static void EnsureBoolParameter(AnimatorController controller, string name)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters)
        {
            if (parameter.name == name)
                return;
        }

        controller.AddParameter(name, AnimatorControllerParameterType.Bool);
    }

    private static AnimationClip LoadClip(string assetPath, string clipName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (Object asset in assets)
        {
            if (asset is AnimationClip clip && clip.name == clipName)
                return clip;
        }

        Debug.LogError($"[MutantAnimator] Clip '{clipName}' not found in {assetPath}");
        return null;
    }

    private static void AddTransition(
        AnimatorState from,
        AnimatorState to,
        float duration,
        params (string parameter, AnimatorConditionMode mode, float threshold)[] conditions)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = duration;
        transition.offset = 0f;
        transition.interruptionSource = TransitionInterruptionSource.None;
        transition.orderedInterruption = true;

        foreach ((string parameter, AnimatorConditionMode mode, float threshold) condition in conditions)
            transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
    }
}
#endif
