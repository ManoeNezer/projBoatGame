using System.Collections.Generic;
using BoatGame.Quests;
using BoatGame.Rumors;
using BoatGame.World;
using UnityEngine;

namespace BoatGame.Port
{
    [DisallowMultipleComponent]
    public sealed class PortManager : MonoBehaviour
    {
        private static readonly List<PortManager> ActivePorts = new List<PortManager>(16);

        [Header("Port")]
        [SerializeField] private string portName = "Port sans nom";
        [SerializeField] private Transform dockAnchor;
        [SerializeField] private DockingZone dockingZone;
        [SerializeField] private List<PortServicePoint> servicePoints = new List<PortServicePoint>();

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;

        public string PortName => portName;
        public Transform DockAnchor => dockAnchor;
        public DockingZone DockingZone => dockingZone;
        public IReadOnlyList<PortServicePoint> ServicePoints => servicePoints;

        private void OnEnable()
        {
            if (!ActivePorts.Contains(this))
            {
                ActivePorts.Add(this);
            }
        }

        private void OnDisable()
        {
            ActivePorts.Remove(this);
        }

        public void Configure(string newPortName, Transform newDockAnchor, DockingZone newDockingZone)
        {
            portName = string.IsNullOrWhiteSpace(newPortName) ? "Port" : newPortName;
            dockAnchor = newDockAnchor;
            dockingZone = newDockingZone;
            if (dockingZone != null)
            {
                dockingZone.Configure(this, dockAnchor);
            }
        }

        public void RegisterService(PortServicePoint service)
        {
            if (service != null && !servicePoints.Contains(service))
            {
                servicePoints.Add(service);
                service.ConfigurePort(this);
            }
        }

        public static PortManager FindNearest(Vector3 position, float maxDistance)
        {
            PortManager nearest = null;
            float bestSqr = maxDistance * maxDistance;
            for (int i = ActivePorts.Count - 1; i >= 0; i--)
            {
                PortManager port = ActivePorts[i];
                if (port == null)
                {
                    ActivePorts.RemoveAt(i);
                    continue;
                }

                float sqr = (port.transform.position - position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    nearest = port;
                }
            }

            return nearest;
        }

        public static PortManager CreateRuntimePort(Vector3 position, Quaternion rotation)
        {
            int worldLayer = LayerMask.NameToLayer("World");
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (worldLayer < 0)
            {
                worldLayer = 0;
            }

            if (interactableLayer < 0)
            {
                interactableLayer = 0;
            }

            GameObject root = new GameObject("RuntimePort");
            root.transform.SetPositionAndRotation(position, rotation);
            PortManager port = root.AddComponent<PortManager>();
            Material wood = CreateRuntimeMaterial("RuntimePortWood", new Color(0.31f, 0.18f, 0.09f));
            Material wall = CreateRuntimeMaterial("RuntimePortWall", new Color(0.43f, 0.36f, 0.25f));

            GameObject portZoneObject = new GameObject("PortZone");
            portZoneObject.layer = worldLayer;
            portZoneObject.transform.SetParent(root.transform, false);
            portZoneObject.transform.localPosition = new Vector3(0f, 2f, 8f);
            BoxCollider portCollider = portZoneObject.AddComponent<BoxCollider>();
            portCollider.isTrigger = true;
            portCollider.size = new Vector3(130f, 20f, 150f);
            portZoneObject.AddComponent<PortZone>();

            CreateBox(root.transform, "Dock", new Vector3(0f, 0.55f, 0f), new Vector3(8f, 0.7f, 72f), Quaternion.identity, wood, worldLayer, true);
            CreateBox(root.transform, "CrossDock", new Vector3(0f, 0.58f, 30f), new Vector3(46f, 0.7f, 7f), Quaternion.identity, wood, worldLayer, true);
            CreateBox(root.transform, "Warehouse", new Vector3(-17f, 4f, -14f), new Vector3(12f, 7f, 12f), Quaternion.identity, wall, worldLayer, true);
            CreateBox(root.transform, "ShipwrightHouse", new Vector3(15f, 3.6f, -8f), new Vector3(10f, 6.2f, 10f), Quaternion.identity, wall, worldLayer, true);

            Transform anchor = new GameObject("DockAnchor").transform;
            anchor.SetParent(root.transform, false);
            anchor.localPosition = new Vector3(0f, 0.8f, 18f);
            anchor.localRotation = Quaternion.identity;

            GameObject dockZoneObject = new GameObject("DockingZone");
            dockZoneObject.layer = interactableLayer;
            dockZoneObject.transform.SetParent(root.transform, false);
            dockZoneObject.transform.localPosition = new Vector3(0f, 0.8f, 18f);
            BoxCollider dockCollider = dockZoneObject.AddComponent<BoxCollider>();
            dockCollider.isTrigger = true;
            dockCollider.size = new Vector3(30f, 8f, 34f);
            DockingZone docking = dockZoneObject.AddComponent<DockingZone>();

            port.Configure("Port de passage", anchor, docking);
            port.RegisterService(CreateMerchant<ContractBoard>(root.transform, "TableauContrats", new Vector3(0f, 1.2f, 23f), interactableLayer, wood));
            port.RegisterService(CreateMerchant<ResourceMerchant>(root.transform, "MarchandRessources", new Vector3(-8f, 1.25f, 25f), interactableLayer, wall));
            port.RegisterService(CreateMerchant<ShipUpgradeMerchant>(root.transform, "ChantierNaval", new Vector3(8f, 1.25f, 25f), interactableLayer, wall));
            port.RegisterService(CreateMerchant<RepairMerchant>(root.transform, "Reparateur", new Vector3(0f, 1.25f, 33f), interactableLayer, wall));
            port.RegisterService(CreateMerchant<RumorSource>(root.transform, "MaitreRumeurs", new Vector3(-14f, 1.25f, 34f), interactableLayer, wall));

            GameObject currentObject = new GameObject("HarborCalmWater");
            currentObject.transform.SetParent(root.transform, false);
            currentObject.transform.localPosition = new Vector3(0f, 0f, 18f);
            CurrentZone current = currentObject.AddComponent<CurrentZone>();
            current.Configure(46f, rotation.eulerAngles.y + 180f, 0.45f, 18f);
            return port;
        }

        private static T CreateMerchant<T>(Transform parent, string name, Vector3 localPosition, int layer, Material material) where T : PortServicePoint
        {
            GameObject merchant = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            merchant.name = name;
            merchant.layer = layer;
            merchant.transform.SetParent(parent, false);
            merchant.transform.localPosition = localPosition;
            merchant.transform.localScale = new Vector3(0.8f, 1.25f, 0.8f);
            Collider collider = merchant.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            Renderer renderer = merchant.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return merchant.AddComponent<T>();
        }

        private static GameObject CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material, int layer, bool collider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.layer = layer;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = localScale;
            box.transform.localRotation = localRotation;
            Renderer renderer = box.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            if (!collider)
            {
                Collider hit = box.GetComponent<Collider>();
                if (hit != null)
                {
                    Object.Destroy(hit);
                }
            }

            return box;
        }

        private static Material CreateRuntimeMaterial(string materialName, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader) { name = materialName };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            return material;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.78f, 0.25f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, 22f);
            if (dockAnchor != null)
            {
                Gizmos.DrawLine(dockAnchor.position, dockAnchor.position + dockAnchor.forward * 12f);
            }
        }
    }
}
