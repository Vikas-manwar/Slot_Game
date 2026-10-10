using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.EditorTools
{
    internal static class AnimationAssetBuilder
    {
        public sealed class Result
        {
            public AnimatorController Lever;
            public AnimatorController Symbol;
            public AnimatorController Banner;
            public AnimatorController Popup;
        }

        public static Result BuildAll()
        {
            Directory.CreateDirectory(EditorPaths.Animations);
            return new Result
            {
                Lever = BuildLever(),
                Symbol = BuildSymbol(),
                Banner = BuildBanner(),
                Popup = BuildPopup(),
            };
        }


        private static AnimatorController BuildLever()
        {
            var idle = NewClip("Lever_Idle");
            SetActiveKeys(idle, "LeverUp", (0f, true));
            SetActiveKeys(idle, "LeverDown", (0f, false));

            var pull = NewClip("Lever_Pull");
            SetActiveKeys(pull, "LeverUp", (0f, true), (0.05f, false), (0.36f, true));
            SetActiveKeys(pull, "LeverDown", (0f, false), (0.05f, true), (0.36f, false));
            SetScaleY(pull, "LeverUp", (0f, 1f), (0.36f, 0.94f), (0.42f, 1.04f), (0.5f, 1f));

            var controller = NewController("Lever");
            controller.AddParameter("Pull", AnimatorControllerParameterType.Trigger);
            var sm = controller.layers[0].stateMachine;
            var idleState = AddState(sm, "Idle", idle, isDefault: true);
            var pullState = AddState(sm, "Pull", pull);

            var toPull = idleState.AddTransition(pullState);
            Instant(toPull);
            toPull.AddCondition(AnimatorConditionMode.If, 0, "Pull");

            var back = pullState.AddTransition(idleState);
            back.hasExitTime = true;
            back.exitTime = 1f;
            back.duration = 0f;
            return Save(controller);
        }


        private static AnimatorController BuildSymbol()
        {
            const string icon = "Content/Icon";
            const string frame = "Content/WinFrame";

            var idle = NewClip("Symbol_Idle");
            SetUniformScale(idle, icon, (0f, 1f));
            SetActiveKeys(idle, frame, (0f, false));

            var win = NewClip("Symbol_Win", loop: true);
            SetUniformScale(win, icon, (0f, 1f), (0.4f, 1.12f), (0.8f, 1f));
            SetActiveKeys(win, frame, (0f, true));
            SetFloat(win, frame, typeof(Image), "m_Color.a", (0f, 1f), (0.4f, 0.45f), (0.8f, 1f));

            var controller = NewController("SymbolView");
            controller.AddParameter("IsWinning", AnimatorControllerParameterType.Bool);
            var sm = controller.layers[0].stateMachine;
            var idleState = AddState(sm, "Idle", idle, isDefault: true);
            var winState = AddState(sm, "Win", win);
            BoolTransitions(idleState, winState, "IsWinning");
            return Save(controller);
        }


        private static AnimatorController BuildBanner()
        {
            var hidden = NewClip("Banner_Hidden");
            SetFloat(hidden, "", typeof(CanvasGroup), "m_Alpha", (0f, 0f));
            SetUniformScale(hidden, "", (0f, 0.3f));

            var show = NewClip("Banner_Show");
            SetFloat(show, "", typeof(CanvasGroup), "m_Alpha", (0f, 0f), (0.12f, 1f));
            SetUniformScale(show, "", (0f, 0.3f), (0.22f, 1.15f), (0.35f, 1f));

            var loop = NewClip("Banner_Loop", loop: true);
            SetFloat(loop, "", typeof(CanvasGroup), "m_Alpha", (0f, 1f));
            SetUniformScale(loop, "", (0f, 1f), (0.5f, 1.05f), (1f, 1f));

            var hide = NewClip("Banner_Hide");
            SetFloat(hide, "", typeof(CanvasGroup), "m_Alpha", (0f, 1f), (0.2f, 0f));
            SetUniformScale(hide, "", (0f, 1f), (0.2f, 0.8f));

            var controller = NewController("WinBanner");
            controller.AddParameter("Visible", AnimatorControllerParameterType.Bool);
            var sm = controller.layers[0].stateMachine;
            var hiddenState = AddState(sm, "Hidden", hidden, isDefault: true);
            var showState = AddState(sm, "Show", show);
            var loopState = AddState(sm, "Loop", loop);
            var hideState = AddState(sm, "Hide", hide);

            AddBool(hiddenState, showState, "Visible", true);
            var showToLoop = showState.AddTransition(loopState);
            showToLoop.hasExitTime = true;
            showToLoop.exitTime = 1f;
            showToLoop.duration = 0f;
            AddBool(showState, hideState, "Visible", false);
            AddBool(loopState, hideState, "Visible", false);
            AddBool(hideState, showState, "Visible", true);
            var hideToHidden = hideState.AddTransition(hiddenState);
            hideToHidden.hasExitTime = true;
            hideToHidden.exitTime = 1f;
            hideToHidden.duration = 0f;
            return Save(controller);
        }


        private static AnimatorController BuildPopup()
        {
            const string window = "Window";

            var hidden = NewClip("Popup_Hidden");
            SetFloat(hidden, "", typeof(CanvasGroup), "m_Alpha", (0f, 0f));
            SetUniformScale(hidden, window, (0f, 0.85f));

            var open = NewClip("Popup_Open");
            SetFloat(open, "", typeof(CanvasGroup), "m_Alpha", (0f, 0f), (0.15f, 1f));
            SetUniformScale(open, window, (0f, 0.85f), (0.16f, 1.03f), (0.24f, 1f));

            var close = NewClip("Popup_Close");
            SetFloat(close, "", typeof(CanvasGroup), "m_Alpha", (0f, 1f), (0.15f, 0f));
            SetUniformScale(close, window, (0f, 1f), (0.15f, 0.9f));

            var controller = NewController("Popup");
            controller.AddParameter("IsOpen", AnimatorControllerParameterType.Bool);
            var sm = controller.layers[0].stateMachine;
            var hiddenState = AddState(sm, "Hidden", hidden, isDefault: true);
            var openState = AddState(sm, "Open", open);
            var closeState = AddState(sm, "Close", close);

            AddBool(hiddenState, openState, "IsOpen", true);
            AddBool(openState, closeState, "IsOpen", false);
            AddBool(closeState, openState, "IsOpen", true);
            var done = closeState.AddTransition(hiddenState);
            done.hasExitTime = true;
            done.exitTime = 1f;
            done.duration = 0f;
            return Save(controller);
        }


        private static AnimationClip NewClip(string name, bool loop = false)
        {
            string path = $"{EditorPaths.Animations}/{name}.anim";
            AssetDatabase.DeleteAsset(path);

            var clip = new AnimationClip { name = name, frameRate = 60f };
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static AnimatorController NewController(string name)
        {
            string path = $"{EditorPaths.Animations}/{name}.controller";
            AssetDatabase.DeleteAsset(path);
            return AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        private static AnimatorController Save(AnimatorController controller)
        {
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimatorState AddState(AnimatorStateMachine sm, string name, Motion motion, bool isDefault = false)
        {
            var state = sm.AddState(name);
            state.motion = motion;
            state.writeDefaultValues = true;
            if (isDefault) sm.defaultState = state;
            return state;
        }

        private static void Instant(AnimatorStateTransition transition)
        {
            transition.hasExitTime = false;
            transition.duration = 0f;
        }

        private static void AddBool(AnimatorState from, AnimatorState to, string parameter, bool value)
        {
            var transition = from.AddTransition(to);
            Instant(transition);
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, parameter);
        }

        private static void BoolTransitions(AnimatorState off, AnimatorState on, string parameter)
        {
            AddBool(off, on, parameter, true);
            AddBool(on, off, parameter, false);
        }

        private static void SetFloat(AnimationClip clip, string path, System.Type type, string property,
            params (float time, float value)[] keys)
        {
            var curve = new AnimationCurve();
            foreach (var (time, value) in keys) curve.AddKey(new Keyframe(time, value));
            for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, property), curve);
        }

        private static void SetUniformScale(AnimationClip clip, string path, params (float time, float value)[] keys)
        {
            SetFloat(clip, path, typeof(RectTransform), "m_LocalScale.x", keys);
            SetFloat(clip, path, typeof(RectTransform), "m_LocalScale.y", keys);
            SetFloat(clip, path, typeof(RectTransform), "m_LocalScale.z", (0f, 1f));
        }

        private static void SetScaleY(AnimationClip clip, string path, params (float time, float value)[] keys)
        {
            SetFloat(clip, path, typeof(RectTransform), "m_LocalScale.y", keys);
        }

        private static void SetActiveKeys(AnimationClip clip, string path, params (float time, bool active)[] keys)
        {
            var curve = new AnimationCurve();
            foreach (var (time, active) in keys) curve.AddKey(new Keyframe(time, active ? 1f : 0f));
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(GameObject), "m_IsActive"), curve);
        }
    }
}
