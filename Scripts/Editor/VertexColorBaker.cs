using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
 
public class VertexColorBaker : AssetPostprocessor
{
	private void OnPostprocessModel(GameObject gameObject)
	{
		if (assetPath.EndsWith(".fbx") == false &&
			assetPath.EndsWith(".FBX") == false)
			return;

		int lastPeriod = assetImporter.assetPath.LastIndexOf('.');

		if (lastPeriod < 0)
		{
			return;
		}
		
		string configPath = assetImporter.assetPath.Remove(lastPeriod) + ".asset";

		if (!File.Exists(configPath))
		{
			return;
		}

		VertexColorBakeConfig config = AssetDatabase.LoadAssetAtPath<VertexColorBakeConfig>(configPath);

		if (config == null)
		{
			return;
		}

		MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
		foreach (MeshFilter meshFilter in meshFilters)
		{
			if (meshFilter.sharedMesh != null)
			{
				ProcessMesh(meshFilter.sharedMesh, config);
			}
		}
		
		// For MeshRenderer
		var meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
		foreach (var meshFilter in meshFilters)
		{
			if (meshFilter.sharedMesh != null)
				ProcessMesh(meshFilter.sharedMesh, config);
		}

		// For SkinnedMeshRenderer
		var skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
		foreach(var skinnedMeshRenderer in skinnedMeshRenderers)
		{
			if (skinnedMeshRenderer != null)
				ProcessMesh(skinnedMeshRenderer.sharedMesh, config);
		}

	}

	private void ProcessMesh(Mesh mesh, VertexColorBakeConfig config)
	{
		ComputeShader compute = GetComputeShader();

		Color[] colors = mesh.colors;
		if (colors == null || colors.Length != mesh.vertexCount)
		{
			colors = new Color[mesh.vertexCount];
		}

		Vector4[] sampleKernel = new Vector4[config.sampleCount];
		FibonacciSphere(sampleKernel, 0, config.sampleCount);
		compute.SetVectorArray("_SampleKernel", sampleKernel);
		compute.SetFloat("_SampleCount", config.sampleCount);

		compute.SetFloat("_SurfaceBias", config.surfaceBias);

		compute.SetFloat("_CurvatureMaxDist", config.curvatureMaxDist);
		compute.SetVector("_CurvatureRange", config.curvatureRange);
		compute.SetFloat("_OcclusionMaxDist", config.occlusionMaxDist);
		compute.SetFloat("_OcclusionRayBias", config.occlusionRayBias);
		compute.SetFloat("_OcclusionExponent", Mathf.Max(0.001f, config.occlusionExponent));

		compute.SetVector("_CurvatureChannels", config.curvatureChannels);
		compute.SetVector("_OcclusionChannels", config.occlusionChannels);

		ComputeBuffer vertexBuffer = InitializeBuffer(mesh.vertices, sizeof(float) * 3);
		ComputeBuffer normalBuffer = InitializeBuffer(mesh.normals, sizeof(float) * 3);
		ComputeBuffer triangleBuffer = InitializeBuffer(mesh.triangles, sizeof(int));
		ComputeBuffer colorBuffer = InitializeBuffer(colors, sizeof(float) * 4);
		compute.SetBuffer(0, "_Vertices", vertexBuffer);
		compute.SetBuffer(0, "_Normals", normalBuffer);
		compute.SetBuffer(0, "_Triangles", triangleBuffer);
		compute.SetBuffer(0, "_Colors", colorBuffer);
		compute.SetFloat("_TriangleCount", triangleBuffer.count / 3);
		compute.SetFloat("_VertexCount", vertexBuffer.count);
		int threadGroupCount = Mathf.CeilToInt((float)vertexBuffer.count / 16);
		compute.Dispatch(0, threadGroupCount, 1, 1);
		
		// Get result from GPU
		colorBuffer.GetData(colors);

		mesh.SetColors(colors);

		vertexBuffer.Release();
		normalBuffer.Release();
		triangleBuffer.Release();
		colorBuffer.Release();
	}

	private ComputeBuffer InitializeBuffer<T>(T[] data, int stride)
	{
		ComputeBuffer buffer = new ComputeBuffer(data.Length, stride);
		buffer.SetData(data);
		return buffer;
	}

	private ComputeShader GetComputeShader()
	{
		return Resources.Load<ComputeShader>("VertexColorBaker");
	}

	private static void FibonacciSphere(Vector4[] output, int startIndex, int n)
	{
		float phi = Mathf.PI * (3f - Mathf.Sqrt(5f)); // Golden angle in radians

		for (int i = 0; i < n; i++)
		{
			float y = 1 - ((float)i / (n - 1)) * 2; // Y goes from 1 to -1
			float radius = Mathf.Sqrt(1 - y * y); // Radius at y

			float theta = phi * i; // Golden angle increment

			float x = Mathf.Cos(theta) * radius;
			float z = Mathf.Sin(theta) * radius;
			
			output[startIndex + i] = new Vector3(x, y, z);
		}
	}
}
