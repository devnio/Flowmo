using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(PointTuple))]
public class PointTupleEditor : PropertyDrawer
{
	public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(rect, label, property);

		var p = property.FindPropertyRelative("p");
		var position = property.FindPropertyRelative("pos");
		var springW = property.FindPropertyRelative("springW");
		var springD = property.FindPropertyRelative("springD");

		label.text = "Point Constraint";
		Rect contentPosition = EditorGUI.PrefixLabel(rect, label);
		EditorGUIUtility.labelWidth = 40;

		contentPosition.width /= 4f;

		p.intValue = EditorGUI.IntField(contentPosition, "P: ", p.intValue);
		contentPosition.x += contentPosition.width;

		position.vector3Value = EditorGUI.Vector3Field(contentPosition, "Pos: ", position.vector3Value);
		contentPosition.x += contentPosition.width;

		springW.floatValue = EditorGUI.FloatField(contentPosition, "K: ", springW.floatValue);
		contentPosition.x += contentPosition.width;

		springD.floatValue = EditorGUI.FloatField(contentPosition, "D: ", springD.floatValue);

		EditorGUI.EndProperty();
	}
}
