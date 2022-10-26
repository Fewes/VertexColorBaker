using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "VertexColorBakeConfig", menuName = "Rendering/Vertex Color Bake Config", order = 1)]
public class VertexColorBakeConfig : ScriptableObject
{
	[System.Serializable]
	public enum OutputType
	{
		Keep,
		Curvature,
		Occlusion
	}

	[Range(16, 512)]
	public int sampleCount = 64;
	[Min(0)]
	public float surfaceBias = 0.000001f;

	[Header("Channel Output")]
	public OutputType redChannel = OutputType.Curvature;
	public OutputType greenChannel = OutputType.Occlusion;
	public OutputType blueChannel = OutputType.Keep;
	public OutputType alphaChannel = OutputType.Keep;

	[Header("Curvature Parameters")]
	[Min(0)]
	public float curvatureMaxDist = 0.01f;
	public Vector2 curvatureRange = new Vector2(0, 1);

	[Header("Occlusion Parameters")]
	[Min(0)]
	public float occlusionMaxDist = 1f;
	[Range(0, 1)]
	public float occlusionRayBias = 0.01f;
	[Range(0, 10)]
	public float occlusionExponent = 2;

	public Vector4 curvatureChannels => new Vector4(
		redChannel   == OutputType.Curvature ? 1 : 0,
		greenChannel == OutputType.Curvature ? 1 : 0,
		blueChannel  == OutputType.Curvature ? 1 : 0,
		alphaChannel == OutputType.Curvature ? 1 : 0
		);
	public Vector4 occlusionChannels => new Vector4(
		redChannel   == OutputType.Occlusion ? 1 : 0,
		greenChannel == OutputType.Occlusion ? 1 : 0,
		blueChannel  == OutputType.Occlusion ? 1 : 0,
		alphaChannel == OutputType.Occlusion ? 1 : 0
		);

	private void OnValidate()
	{
		curvatureRange.x = Mathf.Clamp01(curvatureRange.x);
		curvatureRange.y = Mathf.Clamp01(curvatureRange.y);
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(VertexColorBakeConfig))]
public class VertexBakeConfigEditor : Editor
{
	public override void OnInspectorGUI()
	{
		VertexColorBakeConfig config = target as VertexColorBakeConfig;

		DrawDefaultInspector();

		EditorGUILayout.Space();

		string configPath = AssetDatabase.GetAssetPath(config);

		int lastPeriod = configPath.LastIndexOf('.');

		if (lastPeriod < 0)
		{
			return;
		}

		string modelPath = configPath.Remove(lastPeriod) + ".fbx";

		AssetImporter importer = AssetImporter.GetAtPath(modelPath);
		Object modelObject = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
		if (importer && modelObject)
		{
			EditorGUILayout.ObjectField("Link established!", modelObject, typeof(GameObject), false);
		}
		else
		{
			EditorGUILayout.ObjectField("Link object not found!", modelObject, typeof(GameObject), false);
		}

		GUI.enabled = importer != null;
		if (GUILayout.Button("Reimport"))
		{
			importer.SaveAndReimport();
		}
		GUI.enabled = true;
	}
}
#endif