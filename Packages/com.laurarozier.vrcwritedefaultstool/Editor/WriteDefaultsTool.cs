using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using static VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;

namespace VRCWriteDefaultsTool.Editor
{
    public class WriteDefaultsTool : EditorWindow
    {
        [MenuItem("Tools/LauraRozier/Animator Controller/WD Tool")]
        public static void ShowWindow() =>
            GetWindow<WriteDefaultsTool>(true, "Write Defaults Tool", true);

        private const float CGUISpacing = 12f;

        private VRCAvatarDescriptor _avatarDescriptor = null;
        private Vector2 _scrollPosition = Vector2.zero;
        private StringBuilder _resultText = new StringBuilder();

        void OnGUI()
        {
            EditorGUILayout.LabelField("Configure IFacialMocap BlendShapes", EditorStyles.boldLabel);
            EditorGUILayout.Space(CGUISpacing);

            EditorGUI.BeginChangeCheck();
            {
                _avatarDescriptor = (VRCAvatarDescriptor)EditorGUILayout.ObjectField(
                    "VRC Avatar Descriptor",
                    _avatarDescriptor,
                    typeof(VRCAvatarDescriptor),
                    true);
            }
            EditorGUI.EndChangeCheck();

            EditorGUILayout.Space(CGUISpacing);

            ProcessAvatarDescriptor();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, false, true);
            {
                EditorGUILayout.TextArea(_resultText.ToString(), EditorStyles.textField, GUILayout.ExpandHeight(true));
            }
            EditorGUILayout.EndScrollView();
        }

        private void ProcessAvatarDescriptor()
        {
            _resultText.Clear();

            if (_avatarDescriptor == null)
            {
                _resultText.Append("No VRC Avatar Descriptor selected");
                return;
            }

            string result = string.Empty;

            foreach (var layer in _avatarDescriptor.baseAnimationLayers)
            {
                ProcessAnimatorController(layer);
                _resultText.AppendLine();
            }

            foreach (var layer in _avatarDescriptor.specialAnimationLayers)
            {
                ProcessAnimatorController(layer);
                _resultText.AppendLine();
            }
        }

        private void ProcessAnimatorController(CustomAnimLayer layer)
        {
            _resultText.Append($"Layer \"{layer.type}\" - ");
            var animCtrl = layer.animatorController as AnimatorController;

            if (animCtrl == null)
            {
                _resultText.AppendLine($"No controller assigned");
                return;
            }

            _resultText.AppendLine($"Controller: {animCtrl.name}");

            foreach (var ctrlLayer in animCtrl.layers)
            {
                _resultText.AppendLine($"  Layer: {ctrlLayer.name}");

                if (!ScanStateMachineForWD(ctrlLayer.stateMachine))
                    _resultText.AppendLine($"    - No WD found");
            }
        }

        private bool ScanStateMachineForWD(AnimatorStateMachine stateMachine, string path = "")
        {
            bool result = false;
            
            foreach (ChildAnimatorState childState in stateMachine.states)
            {
                if (!childState.state.writeDefaultValues)
                    continue;

                // Ignore blend trees using the Direct blend type, because disabling WD on these reportedly causes side-effects.
                BlendTree blendTree = childState.state.motion as BlendTree;

                if (blendTree == null || blendTree.blendType != BlendTreeType.Direct)
                {
                    _resultText.AppendLine($"    - WD ON > {path}{childState.state.name}");
                    result = true; // Found any state that writes defaults.
                }
            }

            // This state machine could itself contain nested state machines. Recursively search those too.
            foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
            {
                var machine = childStateMachine.stateMachine;
                bool subResult = ScanStateMachineForWD(machine, $"{path}{machine.name}.");

                if (subResult)
                    result = true;
            }

            return result;
        }
    }
}
