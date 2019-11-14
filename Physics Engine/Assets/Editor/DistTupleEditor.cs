using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(DistTuple))]
public class DistTupleEditor : PropertyDrawer
{
    public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(rect, label, property);

        var particle1 = property.FindPropertyRelative("Item1");
        var particle2 = property.FindPropertyRelative("Item2");
        var distance = property.FindPropertyRelative("Item3");

        label.text = "Distance Constraint";
        Rect contentPosition = EditorGUI.PrefixLabel(rect, label);
        EditorGUIUtility.labelWidth = 50;

        contentPosition.width /= 3f;

        particle1.intValue = EditorGUI.IntField(contentPosition, "P1: ", particle1.intValue);
        contentPosition.x += contentPosition.width;

        particle2.intValue = EditorGUI.IntField(contentPosition, "P2: ", particle2.intValue);
        contentPosition.x += contentPosition.width;

        distance.floatValue = EditorGUI.FloatField(contentPosition, "Dist: ", distance.floatValue);

        EditorGUI.EndProperty();
    }
}
