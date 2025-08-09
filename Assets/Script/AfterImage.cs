using System.Collections.Generic;
using UnityEngine;

namespace AfterimageSample
{
    public class AfterImage
    {
        RenderParams[] _params;
        Mesh[] _meshes;
        Matrix4x4[] _matrices;

        /// 描画された回数.
        public int FrameCount { get; private set; }

        /// <summary>
        /// コンストラクタ.
        /// </summary>
        /// <param name="meshCount">描画するメッシュの数.</param>
        public AfterImage(int meshCount)
        {
            _params = new RenderParams[meshCount];
            _meshes = new Mesh[meshCount];
            _matrices = new Matrix4x4[meshCount];
            Reset();
        }

        /// <summary>
        /// 描画前もしくは後に実行する.
        /// </summary>
        public void Reset()
        {
            FrameCount = 0;
        }

        /// <summary>
        /// メッシュごとに使用するマテリアルを用意し、現在のメッシュの形状を記憶させる.
        /// </summary>
        /// <param name="material">使用するマテリアル. </param>
        /// <param name="layer">描画するレイヤー.</param>
        /// <param name="renderers">記憶させるSkinnedMeshRendereの配列.</param>
        public void Setup(Material material, int layer, SkinnedMeshRenderer[] renderers)
        {
            int count = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                //if (renderers[i].tag == "Remove") continue;
                // マテリアルにnullが渡されたらオブジェクトのマテリアルをそのまま使う.
                // if (material == null)
                for (int j = 0; j < renderers[i].sharedMesh.subMeshCount; j++)
                {
                    material = renderers[i].materials[j];
                    if (_params[count].material != material)
                    {
                        _params[count] = new RenderParams(material);
                    }
                    // レイヤーを設定する.
                    if (_params[count].layer != layer)
                    {
                        _params[count].layer = layer;
                    }
                    // 現在のメッシュの状態を格納する.
                    if (_meshes[count] == null)
                    {
                        _meshes[count] = new Mesh();
                    }
                    Mesh bakedMesh = new Mesh();
                    renderers[i].BakeMesh(bakedMesh);
                    _meshes[count] = ExtractTrueSubmesh(bakedMesh, j);

                    _matrices[count] = renderers[i].transform.localToWorldMatrix;
                    count++;
                }
            }
        }

        // helper function for extracting submeshes
        public static Mesh ExtractTrueSubmesh(Mesh mesh, int subMeshIndex)
        {
            int[] triangles = mesh.GetTriangles(subMeshIndex);
            Vector3[] originalVertices = mesh.vertices;
            Vector3[] originalNormals = mesh.normals;
            Vector2[] originalUVs = mesh.uv;

            Dictionary<int, int> oldToNewIndex = new Dictionary<int, int>();
            List<Vector3> newVertices = new List<Vector3>();
            List<Vector3> newNormals = new List<Vector3>();
            List<Vector2> newUVs = new List<Vector2>();
            List<int> newTriangles = new List<int>();

            for (int i = 0; i < triangles.Length; i++)
            {
                int oldIndex = triangles[i];
                int newIndex;
                if (!oldToNewIndex.TryGetValue(oldIndex, out newIndex))
                {
                    newIndex = newVertices.Count;
                    oldToNewIndex[oldIndex] = newIndex;
                    newVertices.Add(originalVertices[oldIndex]);
                    if (originalNormals.Length > oldIndex) newNormals.Add(originalNormals[oldIndex]);
                    if (originalUVs.Length > oldIndex) newUVs.Add(originalUVs[oldIndex]);
                }
                newTriangles.Add(newIndex);
            }

            Mesh subMesh = new Mesh();
            subMesh.vertices = newVertices.ToArray();
            if (newNormals.Count > 0) subMesh.normals = newNormals.ToArray();
            if (newUVs.Count > 0) subMesh.uv = newUVs.ToArray();
            subMesh.triangles = newTriangles.ToArray();
            subMesh.RecalculateBounds();
            if (newNormals.Count == 0) subMesh.RecalculateNormals();
            return subMesh;
        }

        /// <summary>
        /// 記憶したメッシュを全て描画する.
        /// </summary>
        public void RenderMeshes()
        {
            for (int i = 0; i < _meshes.Length; i++)
            {
                Graphics.RenderMesh(_params[i], _meshes[i], 0, _matrices[i]);
            }
            FrameCount++;
        }
    }
}
