using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BoatGame.Boat;
using BoatGame.Damage;
using BoatGame.Debugging;
using BoatGame.Discovery;
using BoatGame.Economy;
using BoatGame.Environment;
using BoatGame.Events;
using BoatGame.Interaction;
using BoatGame.Physics;
using BoatGame.Player;
using BoatGame.Port;
using BoatGame.Quests;
using BoatGame.Rumors;
using BoatGame.Upgrades;
using BoatGame.Water;
using BoatGame.Weather;
using BoatGame.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BoatGame.EditorTools
{
    public static class WaterPrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/WaterPrototype.unity";
        private const string OceanMaterialPath = "Assets/Materials/OceanWater.mat";
        private const string BoatPrefabPath = "Assets/Prefabs/PrototypeBoat.prefab";
        private const string CratePrefabPath = "Assets/Prefabs/FloatingCrate.prefab";
        private const string BarrelPrefabPath = "Assets/Prefabs/FloatingBarrel.prefab";
        private const string PlayerPrefabPath = "Assets/Prefabs/FPSPlayer.prefab";
        private const string OceanMeshPath = "Assets/Meshes/OceanGrid.asset";
        private const string HullMeshPath = "Assets/Meshes/PrototypeBoatHull.asset";
        private const string SailMeshPath = "Assets/Meshes/PrototypeSail.asset";
        private const string UrpAssetPath = "Assets/Settings/PrototypeURPAsset.asset";
        private const string UrpRendererPath = "Assets/Settings/PrototypeURPAsset_Renderer.asset";
        private const string SkyboxMaterialPath = "Assets/Materials/PrototypeSkybox.mat";
        private const string WaterNormalAPath = "Assets/Art/Water/WaterNormalA.png";
        private const string WaterNormalBPath = "Assets/Art/Water/WaterNormalB.png";
        private const int PrototypeWorldSeed = 23051926;

        [MenuItem("Tools/Ocean Prototype/Rebuild Complete Water Prototype")]
        public static void BuildPrototype()
        {
            EnsureFolders();
            EnsureLayers();
            ConfigureProjectSettings();
            Material oceanMaterial = CreateOceanMaterial();
            Material woodMaterial = CreateLitMaterial("Assets/Materials/WarmBoatWood.mat", new Color(0.46f, 0.28f, 0.14f), 0.36f, 0.15f);
            Material darkWoodMaterial = CreateLitMaterial("Assets/Materials/DarkWetWood.mat", new Color(0.24f, 0.13f, 0.07f), 0.42f, 0.11f);
            Material canvasMaterial = CreateLitMaterial("Assets/Materials/AgedCanvas.mat", new Color(0.74f, 0.67f, 0.52f), 0.62f, 0.08f);
            Material crateMaterial = CreateLitMaterial("Assets/Materials/CrateWood.mat", new Color(0.54f, 0.34f, 0.18f), 0.48f, 0.1f);
            Material barrelMaterial = CreateLitMaterial("Assets/Materials/BarrelWood.mat", new Color(0.38f, 0.21f, 0.11f), 0.4f, 0.12f);
            Material metalMaterial = CreateLitMaterial("Assets/Materials/DarkIron.mat", new Color(0.12f, 0.11f, 0.1f), 0.28f, 0.55f);
            WorldMaterialSet worldMaterials = CreateWorldMaterials();

            Mesh oceanMesh = CreateOceanMesh();
            Mesh hullMesh = CreateHullMesh();
            Mesh sailMesh = CreateSailMesh();

            GameObject boatPrefab = CreateBoatPrefab(hullMesh, sailMesh, woodMaterial, darkWoodMaterial, canvasMaterial, metalMaterial);
            GameObject cratePrefab = CreateCratePrefab(crateMaterial);
            GameObject barrelPrefab = CreateBarrelPrefab(barrelMaterial, metalMaterial);
            GameObject playerPrefab = CreatePlayerPrefab();

            CreateScene(oceanMesh, oceanMaterial, boatPrefab, cratePrefab, barrelPrefab, playerPrefab, worldMaterials);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Ocean prototype rebuilt: {ScenePath}");
        }

        public static void BuildFromCommandLine()
        {
            BuildPrototype();
            EditorApplication.Exit(0);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Art");
            EnsureFolder("Assets/Art", "Water");
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets", "Meshes");
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets", "Scenes");
            EnsureFolder("Assets", "Settings");
        }

        private static void EnsureLayers()
        {
            EnsureLayer("Player");
            EnsureLayer("Boat");
            EnsureLayer("Interactable");
            EnsureLayer("World");
        }

        private static int EnsureLayer(string layerName)
        {
            int existingLayer = LayerMask.NameToLayer(layerName);
            if (existingLayer >= 0)
            {
                return existingLayer;
            }

            Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            SerializedObject tagManager = new SerializedObject(tagManagerAsset);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = layerName;
                    tagManager.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                    return i;
                }
            }

            throw new System.InvalidOperationException($"No free Unity layer slot available for {layerName}.");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void ConfigureProjectSettings()
        {
            UniversalRenderPipelineAsset urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (urpAsset == null)
            {
                ScriptableRendererData rendererData = CreateRendererData();
                urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(urpAsset, UrpAssetPath);
            }

            GraphicsSettings.defaultRenderPipeline = urpAsset;
            QualitySettings.renderPipeline = urpAsset;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Time.fixedDeltaTime = 0.02f;
            UnityEngine.Physics.defaultSolverIterations = 12;
            UnityEngine.Physics.defaultSolverVelocityIterations = 6;

            EditorUtility.SetDirty(urpAsset);
        }

        private static ScriptableRendererData CreateRendererData()
        {
            ScriptableRendererData existingRenderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(UrpRendererPath);
            if (existingRenderer != null)
            {
                return existingRenderer;
            }

            MethodInfo createRendererAsset = typeof(UniversalRenderPipelineAsset).GetMethod(
                "CreateRendererAsset",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (createRendererAsset != null)
            {
                object renderer = createRendererAsset.Invoke(
                    null,
                    new object[] { UrpAssetPath, RendererType.UniversalRenderer, true, "Renderer" });

                if (renderer is ScriptableRendererData rendererData)
                {
                    return rendererData;
                }
            }

            UniversalRendererData fallbackRenderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(fallbackRenderer, UrpRendererPath);
            return fallbackRenderer;
        }

        private static Material CreateOceanMaterial()
        {
            CreateNormalTexture(WaterNormalAPath, 0.075f, 4.2f, 11);
            CreateNormalTexture(WaterNormalBPath, 0.14f, 2.2f, 37);

            Shader shader = Shader.Find("BoatGame/Ocean Gerstner URP");
            if (shader == null)
            {
                throw new MissingReferenceException("BoatGame/Ocean Gerstner URP shader is missing.");
            }

            Material material = LoadOrCreateMaterial(OceanMaterialPath, shader);
            material.SetColor("_DeepColor", new Color(0.015f, 0.16f, 0.23f, 1f));
            material.SetColor("_ShallowColor", new Color(0.08f, 0.39f, 0.46f, 1f));
            material.SetColor("_FoamColor", new Color(0.88f, 0.96f, 0.92f, 1f));
            material.SetFloat("_Alpha", 0.84f);
            material.SetFloat("_Smoothness", 0.58f);
            material.SetFloat("_SpecularStrength", 0.78f);
            material.SetFloat("_FresnelPower", 3.2f);
            material.SetFloat("_FresnelStrength", 0.48f);
            material.SetFloat("_NormalTilingA", 0.052f);
            material.SetFloat("_NormalTilingB", 0.125f);
            material.SetVector("_NormalSpeedA", new Vector4(0.032f, 0.017f, 0f, 0f));
            material.SetVector("_NormalSpeedB", new Vector4(-0.024f, 0.029f, 0f, 0f));
            material.SetFloat("_NormalStrength", 0.36f);
            material.SetFloat("_MicroWaveScale", 1.45f);
            material.SetFloat("_MicroWaveSpeed", 1.15f);
            material.SetFloat("_MicroWaveStrength", 0.075f);
            material.SetFloat("_FoamIntensity", 0.2f);
            material.SetFloat("_FoamThreshold", 0.79f);
            material.SetTexture("_NormalMapA", AssetDatabase.LoadAssetAtPath<Texture2D>(WaterNormalAPath));
            material.SetTexture("_NormalMapB", AssetDatabase.LoadAssetAtPath<Texture2D>(WaterNormalBPath));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateLitMaterial(string path, Color baseColor, float smoothness, float metallic)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = LoadOrCreateMaterial(path, shader);
            SetMaterialColor(material, baseColor);
            SetMaterialFloat(material, "_Smoothness", smoothness);
            SetMaterialFloat(material, "_Metallic", metallic);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static WorldMaterialSet CreateWorldMaterials()
        {
            return new WorldMaterialSet
            {
                islandGrass = CreateLitMaterial("Assets/Materials/WorldIslandGrass.mat", new Color(0.16f, 0.32f, 0.15f), 0.42f, 0f),
                beachSand = CreateLitMaterial("Assets/Materials/WorldBeachSand.mat", new Color(0.62f, 0.53f, 0.37f), 0.58f, 0f),
                cliffRock = CreateLitMaterial("Assets/Materials/WorldCliffRock.mat", new Color(0.28f, 0.28f, 0.25f), 0.32f, 0f),
                dockWood = CreateLitMaterial("Assets/Materials/WorldDockWood.mat", new Color(0.28f, 0.16f, 0.08f), 0.38f, 0f),
                buildingWall = CreateLitMaterial("Assets/Materials/WorldBuildingWall.mat", new Color(0.46f, 0.38f, 0.28f), 0.5f, 0f),
                buildingRoof = CreateLitMaterial("Assets/Materials/WorldBuildingRoof.mat", new Color(0.23f, 0.08f, 0.06f), 0.36f, 0f),
                buoyRed = CreateLitMaterial("Assets/Materials/WorldBuoyRed.mat", new Color(0.62f, 0.08f, 0.06f), 0.28f, 0f),
                buoyWhite = CreateLitMaterial("Assets/Materials/WorldBuoyWhite.mat", new Color(0.86f, 0.83f, 0.72f), 0.42f, 0f),
                wreckWood = CreateLitMaterial("Assets/Materials/WorldWreckWood.mat", new Color(0.17f, 0.1f, 0.06f), 0.34f, 0f),
                silhouette = CreateLitMaterial("Assets/Materials/WorldSilhouette.mat", new Color(0.08f, 0.12f, 0.11f), 0.65f, 0f)
            };
        }

        private static Material LoadOrCreateMaterial(string path, Shader shader)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            return material;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private static void SetMaterialFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static void CreateNormalTexture(string assetPath, float frequency, float strength, int seed)
        {
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float hL = SampleNormalHeight(x - 1, y, size, frequency, seed);
                    float hR = SampleNormalHeight(x + 1, y, size, frequency, seed);
                    float hD = SampleNormalHeight(x, y - 1, size, frequency, seed);
                    float hU = SampleNormalHeight(x, y + 1, size, frequency, seed);
                    float dx = (hR - hL) * strength;
                    float dy = (hU - hD) * strength;
                    Vector3 normal = new Vector3(-dx, -dy, 1f).normalized;
                    Color encoded = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
                    texture.SetPixel(x, y, encoded);
                }
            }

            texture.Apply(true, false);
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = false;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.mipmapEnabled = true;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
        }

        private static float SampleNormalHeight(int x, int y, int size, float frequency, int seed)
        {
            float u = Mathf.Repeat((float)x / size, 1f);
            float v = Mathf.Repeat((float)y / size, 1f);
            float waveA = Mathf.Sin((u * 6.2831853f * frequency + seed) + Mathf.Cos(v * 6.2831853f * (frequency * 0.47f)));
            float waveB = Mathf.Sin((u + v) * 6.2831853f * (frequency * 0.61f) + seed * 0.37f);
            float noise = Mathf.PerlinNoise(u * frequency * 4f + seed, v * frequency * 4f + seed * 1.7f);
            return waveA * 0.45f + waveB * 0.35f + noise * 0.2f;
        }

        private static Mesh CreateOceanMesh()
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(OceanMeshPath);
            const int segments = 192;
            const float size = 560f;
            int verticesPerSide = segments + 1;
            Vector3[] vertices = new Vector3[verticesPerSide * verticesPerSide];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[segments * segments * 6];

            for (int z = 0; z < verticesPerSide; z++)
            {
                for (int x = 0; x < verticesPerSide; x++)
                {
                    int index = z * verticesPerSide + x;
                    float px = ((float)x / segments - 0.5f) * size;
                    float pz = ((float)z / segments - 0.5f) * size;
                    vertices[index] = new Vector3(px, 0f, pz);
                    uvs[index] = new Vector2((float)x / segments, (float)z / segments);
                }
            }

            int triangleIndex = 0;
            for (int z = 0; z < segments; z++)
            {
                for (int x = 0; x < segments; x++)
                {
                    int i0 = z * verticesPerSide + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + verticesPerSide;
                    int i3 = i2 + 1;
                    triangles[triangleIndex++] = i0;
                    triangles[triangleIndex++] = i2;
                    triangles[triangleIndex++] = i1;
                    triangles[triangleIndex++] = i1;
                    triangles[triangleIndex++] = i2;
                    triangles[triangleIndex++] = i3;
                }
            }

            Mesh mesh = existing != null ? existing : new Mesh();
            mesh.name = "OceanGrid";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(size, 80f, size));
            mesh.RecalculateNormals();

            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, OceanMeshPath);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
        }

        private static Mesh CreateHullMesh()
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(HullMeshPath);
            float[] zPositions = { -3.8f, -2.2f, 0f, 2.3f, 3.9f };
            float[] topHalfWidths = { 0.85f, 1.55f, 1.7f, 1.25f, 0.18f };
            float[] bottomHalfWidths = { 0.36f, 0.55f, 0.62f, 0.42f, 0.04f };
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            for (int i = 0; i < zPositions.Length; i++)
            {
                vertices.Add(new Vector3(-topHalfWidths[i], 0.12f, zPositions[i]));
                vertices.Add(new Vector3(topHalfWidths[i], 0.12f, zPositions[i]));
                vertices.Add(new Vector3(-bottomHalfWidths[i], -0.78f, zPositions[i]));
                vertices.Add(new Vector3(bottomHalfWidths[i], -0.78f, zPositions[i]));
            }

            for (int i = 0; i < zPositions.Length - 1; i++)
            {
                int a = i * 4;
                int b = (i + 1) * 4;
                AddQuad(triangles, a, b, b + 2, a + 2);
                AddQuad(triangles, a + 1, a + 3, b + 3, b + 1);
                AddQuad(triangles, a + 2, b + 2, b + 3, a + 3);
                AddQuad(triangles, a, a + 1, b + 1, b);
            }

            AddQuad(triangles, 0, 2, 3, 1);
            int last = (zPositions.Length - 1) * 4;
            AddQuad(triangles, last, last + 1, last + 3, last + 2);

            Mesh mesh = existing != null ? existing : new Mesh();
            mesh.name = "PrototypeBoatHull";
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, HullMeshPath);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
        }

        private static Mesh CreateSailMesh()
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(SailMeshPath);
            Mesh mesh = existing != null ? existing : new Mesh();
            mesh.name = "PrototypeSail";
            mesh.Clear();
            mesh.vertices = new[]
            {
                new Vector3(-1.25f, -1.1f, 0f),
                new Vector3(1.15f, -0.95f, 0f),
                new Vector3(0.95f, 1.15f, 0f),
                new Vector3(-0.75f, 0.95f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, SailMeshPath);
            }
            else
            {
                EditorUtility.SetDirty(mesh);
            }

            return mesh;
        }

        private static void AddQuad(List<int> triangles, int a, int b, int c, int d)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
        }

        private static GameObject CreateBoatPrefab(Mesh hullMesh, Mesh sailMesh, Material wood, Material darkWood, Material canvas, Material metal)
        {
            int boatLayer = LayerMask.NameToLayer("Boat");
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            GameObject root = new GameObject("PrototypeBoat");
            root.layer = boatLayer;
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 2400f;
            body.linearDamping = 0.08f;
            body.angularDamping = 0.72f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxAngularVelocity = 3.5f;

            BoxCollider hullCollider = root.AddComponent<BoxCollider>();
            hullCollider.center = new Vector3(0f, -0.28f, 0f);
            hullCollider.size = new Vector3(3.35f, 1.05f, 7.45f);

            GameObject hull = CreateMeshChild(root.transform, "Hull", hullMesh, wood, boatLayer);
            hull.transform.localPosition = Vector3.zero;

            GameObject deck = CreatePrimitiveChild(root.transform, PrimitiveType.Cube, "Deck", darkWood, true, boatLayer);
            deck.transform.localPosition = new Vector3(0f, 0.22f, -0.15f);
            deck.transform.localScale = new Vector3(2.55f, 0.12f, 5.45f);

            GameObject bowRail = CreatePrimitiveChild(root.transform, PrimitiveType.Cube, "BowRail", darkWood, true, boatLayer);
            bowRail.transform.localPosition = new Vector3(0f, 0.62f, 2.85f);
            bowRail.transform.localScale = new Vector3(2.05f, 0.16f, 0.16f);

            GameObject sternRail = CreatePrimitiveChild(root.transform, PrimitiveType.Cube, "SternRail", darkWood, true, boatLayer);
            sternRail.transform.localPosition = new Vector3(0f, 0.62f, -2.95f);
            sternRail.transform.localScale = new Vector3(2.3f, 0.16f, 0.16f);

            GameObject portRail = CreatePrimitiveChild(root.transform, PrimitiveType.Cube, "PortRail", darkWood, true, boatLayer);
            portRail.transform.localPosition = new Vector3(-1.48f, 0.55f, -0.2f);
            portRail.transform.localScale = new Vector3(0.12f, 0.14f, 4.8f);

            GameObject starboardRail = CreatePrimitiveChild(root.transform, PrimitiveType.Cube, "StarboardRail", darkWood, true, boatLayer);
            starboardRail.transform.localPosition = new Vector3(1.48f, 0.55f, -0.2f);
            starboardRail.transform.localScale = new Vector3(0.12f, 0.14f, 4.8f);

            GameObject mast = CreatePrimitiveChild(root.transform, PrimitiveType.Cylinder, "Mast", darkWood, true, boatLayer);
            mast.transform.localPosition = new Vector3(0f, 1.55f, 0.45f);
            mast.transform.localScale = new Vector3(0.12f, 1.55f, 0.12f);

            GameObject boom = CreatePrimitiveChild(root.transform, PrimitiveType.Cylinder, "Boom", darkWood, false, boatLayer);
            boom.transform.localPosition = new Vector3(0f, 1.35f, 0.45f);
            boom.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            boom.transform.localScale = new Vector3(0.07f, 1.35f, 0.07f);

            GameObject sail = CreateMeshChild(root.transform, "MainSail", sailMesh, canvas, boatLayer);
            sail.transform.localPosition = new Vector3(0f, 1.9f, 0.5f);
            sail.transform.localRotation = Quaternion.identity;

            GameObject rudder = CreatePrimitiveChild(root.transform, PrimitiveType.Cube, "Rudder", darkWood, false, boatLayer);
            rudder.transform.localPosition = new Vector3(0f, -0.45f, -3.72f);
            rudder.transform.localScale = new Vector3(0.18f, 0.78f, 0.12f);

            GameObject helm = CreatePrimitiveChild(root.transform, PrimitiveType.Cylinder, "HelmWheel", metal, false, boatLayer);
            helm.transform.localPosition = new Vector3(0f, 0.95f, -1.95f);
            helm.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            helm.transform.localScale = new Vector3(0.45f, 0.06f, 0.45f);

            Transform floatRoot = new GameObject("FloatPoints").transform;
            floatRoot.SetParent(root.transform, false);
            floatRoot.gameObject.layer = boatLayer;
            List<Transform> floatPoints = CreateFloatPoints(floatRoot, new[]
            {
                new Vector3(-1.18f, -0.58f, 2.65f),
                new Vector3(1.18f, -0.58f, 2.65f),
                new Vector3(-1.35f, -0.62f, 0.35f),
                new Vector3(1.35f, -0.62f, 0.35f),
                new Vector3(-1.08f, -0.58f, -2.55f),
                new Vector3(1.08f, -0.58f, -2.55f)
            });

            FloatingObject floatingObject = root.AddComponent<FloatingObject>();
            floatingObject.SetFloatPoints(floatPoints);
            floatingObject.Configure(0.95f, 1.15f, 1.35f, 4.2f, 0.82f, 78000f, new Vector3(0f, -0.65f, -0.12f));
            RepairResource repairResource = root.AddComponent<RepairResource>();
            repairResource.Configure(10, 16);

            Transform sailForcePoint = new GameObject("SailForcePoint").transform;
            sailForcePoint.SetParent(root.transform, false);
            sailForcePoint.gameObject.layer = boatLayer;
            sailForcePoint.localPosition = new Vector3(0f, 0.65f, 0.85f);

            Transform rudderForcePoint = new GameObject("RudderForcePoint").transform;
            rudderForcePoint.SetParent(root.transform, false);
            rudderForcePoint.gameObject.layer = boatLayer;
            rudderForcePoint.localPosition = new Vector3(0f, -0.18f, -3.45f);

            BoatHelmController helmController = root.AddComponent<BoatHelmController>();
            helmController.ConfigurePrototypeRig(sailForcePoint, rudderForcePoint, sail.transform, boom.transform, rudder.transform, helm.transform);
            BoatDamageSystem damageSystem = root.AddComponent<BoatDamageSystem>();
            damageSystem.Configure(helmController, floatingObject, repairResource);
            root.AddComponent<BoatUpgradeSystem>();

            Transform helmStandAnchor = CreateAnchor(root.transform, "HelmStandAnchor", new Vector3(0f, 0.34f, -2.08f), Quaternion.identity, boatLayer);
            Transform helmCameraAnchor = CreateAnchor(root.transform, "HelmCameraAnchor", new Vector3(0f, 1.74f, -2.25f), Quaternion.Euler(8f, 0f, 0f), boatLayer);
            GameObject helmStationObject = CreateStationTrigger(root.transform, "HelmStation", new Vector3(0f, 1.02f, -1.95f), new Vector3(1.45f, 1.55f, 1.35f), interactableLayer);
            HelmStation helmStation = helmStationObject.AddComponent<HelmStation>();
            helmStation.Configure(helmController, helmStandAnchor, helmCameraAnchor);

            Transform sailStandAnchor = CreateAnchor(root.transform, "SailStandAnchor", new Vector3(-0.78f, 0.34f, 0.15f), Quaternion.Euler(0f, 26f, 0f), boatLayer);
            Transform sailCameraAnchor = CreateAnchor(root.transform, "SailCameraAnchor", new Vector3(-0.78f, 1.72f, 0.1f), Quaternion.Euler(4f, 24f, 0f), boatLayer);
            GameObject sailStationObject = CreateStationTrigger(root.transform, "SailStation", new Vector3(-0.72f, 1.0f, 0.45f), new Vector3(1.3f, 1.55f, 1.55f), interactableLayer);
            SailStation sailStation = sailStationObject.AddComponent<SailStation>();
            sailStation.Configure(helmController, sailStandAnchor, sailCameraAnchor);

            CreateRepairPoint(root.transform, "HullRepairPoint", new Vector3(0.95f, 0.62f, 1.55f), new Vector3(0.42f, 0.22f, 0.42f), damageSystem, repairResource, BoatPartType.Hull, wood, interactableLayer);
            CreateRepairPoint(root.transform, "SailRepairPoint", new Vector3(-0.92f, 0.68f, 0.55f), new Vector3(0.38f, 0.22f, 0.38f), damageSystem, repairResource, BoatPartType.Sail, canvas, interactableLayer);
            CreateRepairPoint(root.transform, "RudderRepairPoint", new Vector3(0.72f, 0.62f, -2.85f), new Vector3(0.36f, 0.22f, 0.36f), damageSystem, repairResource, BoatPartType.Rudder, metal, interactableLayer);
            CreateRepairPoint(root.transform, "MastRepairPoint", new Vector3(0.35f, 0.62f, 0.42f), new Vector3(0.36f, 0.22f, 0.36f), damageSystem, repairResource, BoatPartType.Mast, darkWood, interactableLayer);

            GameObject prefab = SavePrefab(root, BoatPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreatePlayerPrefab()
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            GameObject root = new GameObject("FPSPlayer");
            root.layer = playerLayer;
            root.transform.position = Vector3.zero;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 75f;
            body.linearDamping = 0f;
            body.angularDamping = 0.05f;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.radius = 0.3f;
            capsule.center = new Vector3(0f, 0.9f, 0f);

            Transform cameraRoot = new GameObject("CameraRoot").transform;
            cameraRoot.SetParent(root.transform, false);
            cameraRoot.localPosition = new Vector3(0f, 1.62f, 0f);
            cameraRoot.gameObject.layer = playerLayer;

            GameObject cameraObject = new GameObject("FPSCamera");
            cameraObject.transform.SetParent(cameraRoot, false);
            cameraObject.layer = playerLayer;
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 74f;
            camera.nearClipPlane = 0.04f;
            camera.farClipPlane = 950f;
            cameraObject.AddComponent<AudioListener>();

            FpsPlayerController controller = root.AddComponent<FpsPlayerController>();
            LayerMask groundMask = ~((1 << playerLayer) | (1 << interactableLayer));
            controller.Configure(camera, cameraRoot, groundMask);
            root.AddComponent<RepairTool>();
            PlayerCurrency currency = root.AddComponent<PlayerCurrency>();
            currency.Configure(120);
            root.AddComponent<ResourceInventory>();
            cameraObject.AddComponent<WeatherCameraFeedback>();

            InteractionSystem interactionSystem = root.AddComponent<InteractionSystem>();
            LayerMask interactionMask = ~(1 << playerLayer);
            interactionSystem.Configure(camera, controller, null, interactionMask);

            GameObject prefab = SavePrefab(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateCratePrefab(Material crateMaterial)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "FloatingCrate";
            root.transform.localScale = new Vector3(1.15f, 0.95f, 1.15f);

            Renderer renderer = root.GetComponent<Renderer>();
            renderer.sharedMaterial = crateMaterial;

            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 85f;
            body.linearDamping = 0.12f;
            body.angularDamping = 0.55f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxAngularVelocity = 5f;

            Transform floatRoot = new GameObject("FloatPoints").transform;
            floatRoot.SetParent(root.transform, false);
            List<Transform> floatPoints = CreateFloatPoints(floatRoot, new[]
            {
                new Vector3(-0.48f, -0.48f, -0.48f),
                new Vector3(0.48f, -0.48f, -0.48f),
                new Vector3(-0.48f, -0.48f, 0.48f),
                new Vector3(0.48f, -0.48f, 0.48f)
            });

            FloatingObject floating = root.AddComponent<FloatingObject>();
            floating.SetFloatPoints(floatPoints);
            floating.Configure(0.42f, 1.22f, 2.6f, 4.7f, 0.9f, 8500f, new Vector3(0f, -0.12f, 0f));

            GameObject prefab = SavePrefab(root, CratePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateBarrelPrefab(Material barrelMaterial, Material metalMaterial)
        {
            GameObject root = new GameObject("FloatingBarrel");
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.mass = 110f;
            body.linearDamping = 0.1f;
            body.angularDamping = 0.48f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxAngularVelocity = 6f;

            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.direction = 0;
            collider.radius = 0.43f;
            collider.height = 1.45f;

            GameObject barrel = CreatePrimitiveChild(root.transform, PrimitiveType.Cylinder, "BarrelBody", barrelMaterial);
            barrel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            barrel.transform.localScale = new Vector3(0.48f, 0.72f, 0.48f);

            GameObject bandA = CreatePrimitiveChild(root.transform, PrimitiveType.Cylinder, "IronBandA", metalMaterial);
            bandA.transform.localPosition = new Vector3(-0.46f, 0f, 0f);
            bandA.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            bandA.transform.localScale = new Vector3(0.5f, 0.035f, 0.5f);

            GameObject bandB = CreatePrimitiveChild(root.transform, PrimitiveType.Cylinder, "IronBandB", metalMaterial);
            bandB.transform.localPosition = new Vector3(0.46f, 0f, 0f);
            bandB.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            bandB.transform.localScale = new Vector3(0.5f, 0.035f, 0.5f);

            Transform floatRoot = new GameObject("FloatPoints").transform;
            floatRoot.SetParent(root.transform, false);
            List<Transform> floatPoints = CreateFloatPoints(floatRoot, new[]
            {
                new Vector3(-0.58f, -0.25f, -0.25f),
                new Vector3(0.58f, -0.25f, -0.25f),
                new Vector3(-0.58f, -0.25f, 0.25f),
                new Vector3(0.58f, -0.25f, 0.25f)
            });

            FloatingObject floating = root.AddComponent<FloatingObject>();
            floating.SetFloatPoints(floatPoints);
            floating.Configure(0.38f, 1.18f, 2.15f, 3.9f, 0.55f, 9000f, new Vector3(0f, -0.05f, 0f));

            GameObject prefab = SavePrefab(root, BarrelPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreatePrimitiveChild(Transform parent, PrimitiveType primitiveType, string name, Material material, bool keepCollider = false, int layer = -1)
        {
            GameObject child = GameObject.CreatePrimitive(primitiveType);
            child.name = name;
            child.transform.SetParent(parent, false);
            if (layer >= 0)
            {
                child.layer = layer;
            }

            if (!keepCollider)
            {
                foreach (Collider collider in child.GetComponents<Collider>())
                {
                    Object.DestroyImmediate(collider);
                }
            }
            else
            {
                foreach (Collider collider in child.GetComponents<Collider>())
                {
                    collider.isTrigger = false;
                }
            }

            Renderer renderer = child.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return child;
        }

        private static RepairablePart CreateRepairPoint(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            BoatDamageSystem damageSystem,
            RepairResource repairResource,
            BoatPartType partType,
            Material material,
            int layer)
        {
            GameObject point = GameObject.CreatePrimitive(PrimitiveType.Cube);
            point.name = name;
            point.layer = layer;
            point.transform.SetParent(parent, false);
            point.transform.localPosition = localPosition;
            point.transform.localScale = localScale;

            Collider collider = point.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            Renderer renderer = point.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            RepairablePart repairable = point.AddComponent<RepairablePart>();
            repairable.Configure(damageSystem, repairResource, partType, partType == BoatPartType.Hull ? 0.34f : 0.28f, 1);
            return repairable;
        }

        private static GameObject CreateMeshChild(Transform parent, string name, Mesh mesh, Material material, int layer = -1)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            if (layer >= 0)
            {
                child.layer = layer;
            }

            MeshFilter meshFilter = child.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
            MeshRenderer renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return child;
        }

        private static List<Transform> CreateFloatPoints(Transform parent, IReadOnlyList<Vector3> localPositions)
        {
            List<Transform> points = new List<Transform>();
            for (int i = 0; i < localPositions.Count; i++)
            {
                Transform point = new GameObject($"FloatPoint_{i + 1:00}").transform;
                point.SetParent(parent, false);
                point.localPosition = localPositions[i];
                point.gameObject.layer = parent.gameObject.layer;
                points.Add(point);
            }

            return points;
        }

        private static Transform CreateAnchor(Transform parent, string name, Vector3 localPosition, Quaternion localRotation, int layer)
        {
            Transform anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = localPosition;
            anchor.localRotation = localRotation;
            anchor.gameObject.layer = layer;
            return anchor;
        }

        private static GameObject CreateStationTrigger(Transform parent, string name, Vector3 localPosition, Vector3 size, int layer)
        {
            GameObject station = new GameObject(name);
            station.transform.SetParent(parent, false);
            station.transform.localPosition = localPosition;
            station.layer = layer;
            BoxCollider collider = station.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = size;
            return station;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (prefab == null)
            {
                throw new IOException($"Failed to save prefab at {path}.");
            }

            return prefab;
        }

        private static void CreateScene(Mesh oceanMesh, Material oceanMaterial, GameObject boatPrefab, GameObject cratePrefab, GameObject barrelPrefab, GameObject playerPrefab, WorldMaterialSet worldMaterials)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "WaterPrototype";

            GameObject waterManagerObject = new GameObject("WaterManager");
            WaterManager waterManager = waterManagerObject.AddComponent<WaterManager>();
            waterManager.WaterLevel = 0f;
            waterManager.UsePrototypeWaves();

            GameObject windObject = new GameObject("WindManager");
            WindManager windManager = windObject.AddComponent<WindManager>();
            windManager.DirectionDegrees = 42f;
            windManager.BaseStrength = 8.5f;

            GameObject weatherObject = new GameObject("WeatherManager");
            weatherObject.AddComponent<WeatherManager>();

            GameObject ocean = new GameObject("Ocean");
            MeshFilter oceanMeshFilter = ocean.AddComponent<MeshFilter>();
            oceanMeshFilter.sharedMesh = oceanMesh;
            MeshRenderer oceanRenderer = ocean.AddComponent<MeshRenderer>();
            oceanRenderer.sharedMaterial = oceanMaterial;

            GameObject boat = PrefabUtility.InstantiatePrefab(boatPrefab) as GameObject;
            boat.name = "PrototypeBoat";
            boat.transform.position = new Vector3(0f, 0.62f, 0f);
            boat.transform.rotation = Quaternion.Euler(0f, 25f, 0f);

            GameObject worldObject = new GameObject("WorldManager");
            WorldManager worldManager = worldObject.AddComponent<WorldManager>();
            ChunkStreamer chunkStreamer = worldObject.GetComponent<ChunkStreamer>();
            if (chunkStreamer == null)
            {
                chunkStreamer = worldObject.AddComponent<ChunkStreamer>();
            }

            worldManager.Configure(boat.transform, worldMaterials, PrototypeWorldSeed);
            chunkStreamer.Configure(worldManager);

            GameObject stormZoneObject = new GameObject("PrototypeStormZone");
            stormZoneObject.transform.position = new Vector3(220f, 0f, 135f);
            StormZone stormZone = stormZoneObject.AddComponent<StormZone>();
            stormZone.Configure(145f, 52f, 1f, 235f, 3.2f, 0.85f);

            GameObject currentZoneObject = new GameObject("PrototypeCurrentZone");
            currentZoneObject.transform.position = new Vector3(95f, 0f, 70f);
            CurrentZone currentZone = currentZoneObject.AddComponent<CurrentZone>();
            currentZone.Configure(68f, 118f, 3.1f, 20f);

            GameObject eventObject = new GameObject("MaritimeEventManager");
            eventObject.AddComponent<MaritimeEventManager>();

            GameObject portUI = new GameObject("PortUIController");
            portUI.AddComponent<PortUIController>();

            GameObject portDebug = new GameObject("PortDebugTools");
            portDebug.AddComponent<PortDebugTools>();

            GameObject questManagerObject = new GameObject("QuestManager");
            questManagerObject.AddComponent<QuestManager>();

            GameObject rumorManagerObject = new GameObject("RumorManager");
            rumorManagerObject.AddComponent<RumorManager>();

            GameObject discoveryManagerObject = new GameObject("DiscoveryManager");
            discoveryManagerObject.AddComponent<DiscoveryManager>();

            GameObject contractUI = new GameObject("ContractBoardUI");
            contractUI.AddComponent<ContractBoardUI>();

            GameObject rumorUI = new GameObject("RumorUI");
            rumorUI.AddComponent<RumorUI>();

            GameObject trackerUI = new GameObject("ObjectiveTrackerUI");
            trackerUI.AddComponent<ObjectiveTrackerUI>();

            GameObject discoveryUI = new GameObject("DiscoveryNotificationUI");
            discoveryUI.AddComponent<DiscoveryNotificationUI>();

            GameObject questDebug = new GameObject("QuestDebugTools");
            questDebug.AddComponent<QuestDebugTools>();

            PortManager.CreateRuntimePort(new Vector3(165f, 0f, 95f), Quaternion.Euler(0f, 205f, 0f));

            GameObject player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            player.name = "FPSPlayer";
            Vector3 playerLocalPosition = new Vector3(0f, 0.34f, -0.75f);
            player.transform.position = boat.transform.TransformPoint(playerLocalPosition);
            player.transform.rotation = Quaternion.Euler(0f, boat.transform.eulerAngles.y, 0f);

            InteractionPromptUI promptUI = CreatePromptUI();
            InteractionSystem interactionSystem = player.GetComponent<InteractionSystem>();
            FpsPlayerController playerController = player.GetComponent<FpsPlayerController>();
            if (interactionSystem != null && playerController != null)
            {
                int playerLayer = LayerMask.NameToLayer("Player");
                LayerMask interactionMask = ~(1 << playerLayer);
                interactionSystem.Configure(playerController.PlayerCamera, playerController, promptUI, interactionMask);
            }

            PlacePrefab(cratePrefab, "FloatingCrate_A", new Vector3(7f, 1.4f, 8f), Quaternion.Euler(0f, 35f, 0f));
            PlacePrefab(cratePrefab, "FloatingCrate_B", new Vector3(-8f, 1.3f, 4f), Quaternion.Euler(0f, -20f, 12f));
            PlacePrefab(barrelPrefab, "FloatingBarrel_A", new Vector3(5f, 1.2f, -8f), Quaternion.Euler(0f, 0f, 18f));
            PlacePrefab(barrelPrefab, "FloatingBarrel_B", new Vector3(-6f, 1.25f, -6f), Quaternion.Euler(0f, 70f, -10f));

            GameObject sun = new GameObject("Sun");
            Light sunLight = sun.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.intensity = 2.8f;
            sunLight.color = new Color(1f, 0.93f, 0.78f);
            sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            GameObject debugProbe = new GameObject("WaterDebugProbe");
            debugProbe.transform.position = Vector3.zero;
            debugProbe.AddComponent<WaterDebugProbe>();

            GameObject dangerDebug = new GameObject("DangerDebugUI");
            dangerDebug.AddComponent<DangerDebugUI>();

            RenderSettings.skybox = CreateSkyboxMaterial();
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.28f, 0.38f, 0.48f);
            RenderSettings.ambientEquatorColor = new Color(0.18f, 0.26f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.1f, 0.11f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0038f;
            RenderSettings.fogColor = new Color(0.42f, 0.62f, 0.68f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        private static GameObject PlacePrefab(GameObject prefab, string name, Vector3 position, Quaternion rotation)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            instance.name = name;
            instance.transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        private static InteractionPromptUI CreatePromptUI()
        {
            GameObject canvasObject = new GameObject("InteractionPromptCanvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();

            GameObject textObject = new GameObject("PromptText");
            textObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 92f);
            rect.sizeDelta = new Vector2(620f, 42f);

            Text text = textObject.AddComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 26;
            text.color = new Color(0.92f, 0.96f, 0.92f, 0.95f);
            text.raycastTarget = false;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            InteractionPromptUI promptUI = canvasObject.AddComponent<InteractionPromptUI>();
            promptUI.Configure(text, canvasGroup);
            return promptUI;
        }

        private static Material CreateSkyboxMaterial()
        {
            Shader shader = Shader.Find("Skybox/Procedural");
            Material skybox = LoadOrCreateMaterial(SkyboxMaterialPath, shader);
            if (skybox.HasProperty("_SkyTint"))
            {
                skybox.SetColor("_SkyTint", new Color(0.45f, 0.58f, 0.66f));
            }

            if (skybox.HasProperty("_GroundColor"))
            {
                skybox.SetColor("_GroundColor", new Color(0.19f, 0.24f, 0.25f));
            }

            if (skybox.HasProperty("_AtmosphereThickness"))
            {
                skybox.SetFloat("_AtmosphereThickness", 0.9f);
            }

            if (skybox.HasProperty("_Exposure"))
            {
                skybox.SetFloat("_Exposure", 1.05f);
            }

            EditorUtility.SetDirty(skybox);
            return skybox;
        }
    }
}
