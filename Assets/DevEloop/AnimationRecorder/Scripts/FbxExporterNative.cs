using AOT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace DevEloop.AnimationRecorder
{
	public enum FbxFileFormat
	{
		Binary = 0,
		Ascii = 1
	}

	public static class FbxExporterNative
	{
		enum FbxExporterLogType
		{
			Info = 0,
			Warning = 1,
			Error = 2
		};

		public static bool enableDebugLogs = false;

		private const string libraryName = "fbx_exporter";

		[DllImport(libraryName, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool fbxExporter_isExportSupported();

		[DllImport(libraryName)]
		private static extern int fbxExporter_export([MarshalAs(UnmanagedType.LPStr)] string outputFile, int fileFormat);

		[DllImport(libraryName)]
		private static extern void fbxExporter_clear();

		[DllImport(libraryName)]
		private static extern int fbxExporter_addNode([MarshalAs(UnmanagedType.LPStr)] string nodeName, int parentNodeIdx, float[] position, float[] rotationQuaternion, float[] scale);

		[DllImport(libraryName)]
		private static extern int fbxExporter_addMesh(int nodeIdx, [MarshalAs(UnmanagedType.LPStr)] string meshName, int verticesCount, IntPtr vertices, IntPtr normals, IntPtr uv,
			int facesIndicesCount, IntPtr faces);

		[DllImport(libraryName)]
		private static extern int fbxExporter_addSkinning(int meshIdx, int verticesCount, IntPtr boneWeights, IntPtr boneIndices);

		[DllImport(libraryName)]
		private static extern int fbxExporter_setNodeBindPose(int meshId, int nodeIdx, float[] position, float[] rotationQaternion, float[] scale);

		[DllImport(libraryName)]
		private static extern int fbxExporter_addTextureAndMaterial(int meshIdx, [MarshalAs(UnmanagedType.LPStr)] string materialName, [MarshalAs(UnmanagedType.LPStr)] string textureName, IntPtr imageData, int dataSize);

		[DllImport(libraryName)]
		private static extern int fbxExporter_createAnimation([MarshalAs(UnmanagedType.LPStr)] string animationName, IntPtr timeframes, int timeframesNumber,
			bool useTranslation, bool useRotation, bool useScale);

		[DllImport(libraryName)]
		private static extern int fbxExporter_addNodeAnimation([MarshalAs(UnmanagedType.LPStr)] string nodePath, IntPtr positions, IntPtr rotationQuaternions, IntPtr scales, int timeframesNumber);

		[DllImport(libraryName, CallingConvention = CallingConvention.Cdecl)]
		private static extern void RegisterDebugCallback(debugCallback cb);

		private delegate void debugCallback(IntPtr request, int size, FbxExporterLogType logType);

		static FbxExporterNative()
		{
			try
			{
				RegisterDebugCallback(OnDebugCallback);
			}
			catch
			{

			}
		}

		public static bool IsExportSupported()
		{
			try
			{
				return fbxExporter_isExportSupported();
			}
			catch
			{
				return false;
			}
		}

		public static void Initialize()
		{
			fbxExporter_clear();
		}

		public static int Export(string outputFile, FbxFileFormat fileFormat)
		{
			string outputDir = Path.GetDirectoryName(outputFile);
			if (!Directory.Exists(outputDir))
				Directory.CreateDirectory(outputDir);

			return fbxExporter_export(outputFile, (int)fileFormat);
		}

		public static int AddNode(string nodeName, int parentNodeIdx, Vector3 position, Quaternion rotation, Vector3 scale)
		{
			float[] positionArray = new float[] { position.x, position.y, position.z };
			float[] rotationQuaternionArray = new float[] { rotation.x, rotation.y, rotation.z, rotation.w };
			float[] scaleArray = new float[] { scale.x, scale.y, scale.z };

			return fbxExporter_addNode(nodeName, parentNodeIdx, positionArray, rotationQuaternionArray, scaleArray);
		}

		public static int SetNodeBindPose(int meshIdx, int nodeIdx, Vector3 position, Quaternion rotation, Vector3 scale)
		{
			float[] positionArray = new float[] { position.x, position.y, position.z };
			float[] rotationQuaternionArray = new float[] { rotation.x, rotation.y, rotation.z, rotation.w };
			float[] scaleArray = new float[] { scale.x, scale.y, scale.z };

			return fbxExporter_setNodeBindPose(meshIdx, nodeIdx, positionArray, rotationQuaternionArray, scaleArray);
		}

		public static int AddMesh(int nodeIdx, string meshName, Vector3[] vertices, Vector3[] normals, Vector2[] uv, int[] faces)
		{
			int verticesCount = vertices.Length;
			float[] verticesArray = new float[verticesCount * 3];
			for (int i = 0; i < verticesCount; i++)
			{
				verticesArray[i * 3] = vertices[i].x;
				verticesArray[i * 3 + 1] = vertices[i].y;
				verticesArray[i * 3 + 2] = vertices[i].z;
			}
			GCHandle verticesHandle = GCHandle.Alloc(verticesArray, GCHandleType.Pinned);
			IntPtr verticesPtr = verticesHandle.AddrOfPinnedObject();

			GCHandle normalsHandle = new GCHandle();
			IntPtr normalsPtr = IntPtr.Zero;
			if (normals != null)
			{
				float[] normalsArray = new float[normals.Length * 3];
				for (int i = 0; i < normals.Length; i++)
				{
					normalsArray[i * 3] = normals[i].x;
					normalsArray[i * 3 + 1] = normals[i].y;
					normalsArray[i * 3 + 2] = normals[i].z;
				}
				normalsHandle = GCHandle.Alloc(normalsArray, GCHandleType.Pinned);
				normalsPtr = normalsHandle.AddrOfPinnedObject();
			}

			GCHandle uvHandle = new GCHandle();
			IntPtr uvPtr = IntPtr.Zero;
			if (uv != null)
			{
				float[] uvArray = new float[uv.Length * 2];
				for (int i = 0; i < uv.Length; i++)
				{
					uvArray[i * 2] = uv[i].x;
					uvArray[i * 2 + 1] = uv[i].y;
				}
				uvHandle = GCHandle.Alloc(uvArray, GCHandleType.Pinned);
				uvPtr = uvHandle.AddrOfPinnedObject();
			}

			int facesIndicesCount = faces.Length;
			GCHandle facesHandle = GCHandle.Alloc(faces, GCHandleType.Pinned);
			IntPtr facesPtr = facesHandle.AddrOfPinnedObject();

			int result = fbxExporter_addMesh(nodeIdx, meshName, verticesCount, verticesPtr, normalsPtr, uvPtr, facesIndicesCount, facesPtr);

			verticesHandle.Free();
			if (normals != null)
				normalsHandle.Free();
			if (uv != null)
				uvHandle.Free();
			facesHandle.Free();

			return result;
		}

		public static int AddSkinning(int meshIdx, int verticesCount, float[] boneWeights, int[] boneIndices)
		{
			GCHandle boneWeightsHandle = GCHandle.Alloc(boneWeights, GCHandleType.Pinned);
			IntPtr boneWeightsPtr = boneWeightsHandle.AddrOfPinnedObject();
			GCHandle boneIndicesHandle = GCHandle.Alloc(boneIndices, GCHandleType.Pinned);
			IntPtr boneIndicesPtr = boneIndicesHandle.AddrOfPinnedObject();
			int result = fbxExporter_addSkinning(meshIdx, verticesCount, boneWeightsPtr, boneIndicesPtr);
			boneWeightsHandle.Free();
			boneIndicesHandle.Free();
			return result;
		}

		public static int AddTextureAndMaterial(int meshIdx, string materialName, string textureName, byte[] imageData)
		{
			int result = -10;
			if (imageData == null)
			{
				result = fbxExporter_addTextureAndMaterial(meshIdx, materialName, textureName, IntPtr.Zero, 0);
			}
			else
			{
				GCHandle imageDataHandle = GCHandle.Alloc(imageData, GCHandleType.Pinned);
				IntPtr imageDataPtr = imageDataHandle.AddrOfPinnedObject();
				result = fbxExporter_addTextureAndMaterial(meshIdx, materialName, textureName, imageDataPtr, imageData.Length);
				imageDataHandle.Free();
			}
			return result;
		}

		public static int CreateAnimation(string animationName, float[] timeframes, bool applyTranslations, bool applyRotations, bool applyScales)
		{
			int framesCount = timeframes.Length;
			GCHandle timeframesHandle = GCHandle.Alloc(timeframes, GCHandleType.Pinned);
			int result = fbxExporter_createAnimation(animationName, timeframesHandle.AddrOfPinnedObject(), framesCount, applyTranslations, applyRotations, applyScales);
			timeframesHandle.Free();
			return result;
		}

		public static int AddNodeAnimation(string nodePath, float[] positions, float[] rotationQuaternions, float[] scales, int framesCount)
		{
			GCHandle positionHandle = GCHandle.Alloc(positions, GCHandleType.Pinned);
			GCHandle rotationHandle = GCHandle.Alloc(rotationQuaternions, GCHandleType.Pinned);
			GCHandle scaleHandle = GCHandle.Alloc(scales, GCHandleType.Pinned);

			IntPtr positionPtr = positionHandle.AddrOfPinnedObject();
			IntPtr rotationPtr = rotationHandle.AddrOfPinnedObject();
			IntPtr scalePtr = scaleHandle.AddrOfPinnedObject();

			int result = fbxExporter_addNodeAnimation(nodePath, positionPtr, rotationPtr, scalePtr, framesCount);

			positionHandle.Free();
			rotationHandle.Free();
			scaleHandle.Free();

			return result;
		}

		[MonoPInvokeCallback(typeof(debugCallback))]
		private static void OnDebugCallback(IntPtr messagePtr, int size, FbxExporterLogType logType)
		{
			string message = Marshal.PtrToStringAnsi(messagePtr, size);
			message = "FbxExporter: " + message;

			switch (logType)
			{
				case FbxExporterLogType.Error:
					Debug.LogError(message);
					break;
				case FbxExporterLogType.Warning:
					Debug.LogWarning(message);
					break;
				default:
					if (enableDebugLogs)
						Debug.Log(message);
					break;
			}
		}
	}
}
