using UnityEditor;
using UnityEngine;

namespace Tofunaut.TofuECS.Unity.Editor
{
    [CustomEditor(typeof(ECSDatabase))]
    public class ECSDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var ecsDatabase = (ECSDatabase)target;
            
            EditorGUI.BeginDisabledGroup(ecsDatabase.IsBuilt);
            var doBuild = GUILayout.Button("Build Database");
            EditorGUI.EndDisabledGroup();

            if (doBuild)
                ecsDatabase.Build();
            
            GUILayout.Space(20);
            
            base.OnInspectorGUI();
        }
    }
}