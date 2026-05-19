using System.Collections.Generic;
using BoatGame.Boat;
using BoatGame.Discovery;
using BoatGame.Port;
using BoatGame.World;
using UnityEngine;

namespace BoatGame.Rumors
{
    [DefaultExecutionOrder(-35)]
    [DisallowMultipleComponent]
    public sealed class RumorManager : MonoBehaviour
    {
        public static RumorManager Instance { get; private set; }

        [Header("Rumors")]
        [SerializeField, Min(1)] private int rumorsPerPort = 4;
        [SerializeField, Min(250f)] private float minRumorDistance = 520f;
        [SerializeField, Min(500f)] private float maxRumorDistance = 2400f;
        [SerializeField] private List<Rumor> knownRumors = new List<Rumor>();

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;

        private readonly Dictionary<string, List<Rumor>> portRumors = new Dictionary<string, List<Rumor>>(16);
        private int generatedRumorCounter;
        private string lastMessage;

        public IReadOnlyList<Rumor> KnownRumors => knownRumors;
        public string LastMessage => lastMessage;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static RumorManager GetOrCreate()
        {
            if (Instance != null)
            {
                return Instance;
            }

            GameObject manager = new GameObject("RumorManager");
            return manager.AddComponent<RumorManager>();
        }

        public IReadOnlyList<Rumor> GetRumorsForPort(PortManager port)
        {
            string key = GetPortKey(port);
            if (!portRumors.TryGetValue(key, out List<Rumor> rumors))
            {
                rumors = BuildRumorsForPort(port);
                portRumors.Add(key, rumors);
            }

            return rumors;
        }

        public void AddRumor(Rumor rumor)
        {
            if (rumor == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(rumor.id))
            {
                rumor.id = $"rumor_reward_{++generatedRumorCounter}";
            }

            if (!knownRumors.Contains(rumor))
            {
                knownRumors.Add(rumor);
            }

            lastMessage = $"Nouvelle rumeur: {rumor.title}";
        }

        public void RevealRumor(Rumor rumor)
        {
            if (rumor == null)
            {
                return;
            }

            rumor.Reveal();
            AddRumor(rumor);
            lastMessage = $"Rumeur notee: {rumor.title}";
        }

        public void OnLocationDiscovered(DiscoverableLocation location)
        {
            if (location == null || !location.HasWorldPosition)
            {
                return;
            }

            for (int i = 0; i < knownRumors.Count; i++)
            {
                Rumor rumor = knownRumors[i];
                if (rumor == null || !rumor.hasApproximatePosition)
                {
                    continue;
                }

                if (Vector3.Distance(rumor.approximatePosition, location.WorldPosition) <= Mathf.Max(140f, rumor.uncertaintyRadius))
                {
                    rumor.Reveal();
                }
            }
        }

        public void ResetRumors()
        {
            knownRumors.Clear();
            portRumors.Clear();
            lastMessage = "Rumeurs effacees.";
        }

        public Rumor GenerateDebugRumorNear(Vector3 origin)
        {
            WorldManager world = WorldManager.Instance;
            if (world == null)
            {
                Rumor fallback = new Rumor("Chuchotement de quai", "Une lanterne aurait brille quelque part derriere la brume.", origin + Vector3.forward * 550f, true);
                AddRumor(fallback);
                return fallback;
            }

            WorldManager.WorldPoiInfo poi = world.EnsurePoiNear(origin, MaritimePoiType.Shipwreck, minRumorDistance, maxRumorDistance);
            Rumor rumor = CreateRumorFromPoi("Bois noye", "Un pecheur jure avoir vu une epave lever la poupe au creux des lames.", "Debug", poi);
            RevealRumor(rumor);
            return rumor;
        }

        private List<Rumor> BuildRumorsForPort(PortManager port)
        {
            List<Rumor> rumors = new List<Rumor>(rumorsPerPort);
            Vector3 origin = port != null ? port.transform.position : GetReferencePosition();
            string source = port != null ? port.PortName : "un port sans nom";
            WorldManager world = WorldManager.Instance;

            if (world == null)
            {
                for (int i = 0; i < rumorsPerPort; i++)
                {
                    Rumor rumor = new Rumor($"Rumeur de {source}", "Les anciens parlent d'une cote basse, quelque part sous un ciel clair.", origin + Quaternion.Euler(0f, i * 67f, 0f) * Vector3.forward * (700f + i * 180f), true);
                    rumor.sourceName = source;
                    rumors.Add(rumor);
                }

                return rumors;
            }

            MaritimePoiType[] requested =
            {
                MaritimePoiType.Shipwreck,
                MaritimePoiType.DangerZone,
                MaritimePoiType.Port,
                MaritimePoiType.LargeIsland,
                MaritimePoiType.SmallIsland
            };

            for (int i = 0; i < rumorsPerPort; i++)
            {
                MaritimePoiType type = requested[i % requested.Length];
                WorldManager.WorldPoiInfo poi = world.EnsurePoiNear(origin, type, minRumorDistance, maxRumorDistance);
                rumors.Add(CreateRumorForType(source, poi));
            }

            return rumors;
        }

        private Rumor CreateRumorForType(string source, WorldManager.WorldPoiInfo poi)
        {
            switch (poi.Type)
            {
                case MaritimePoiType.Port:
                    return CreateRumorFromPoi("Feu de port dans la brume", "Des marins disent qu'une lanterne de quai perce parfois la brume. Sa route n'est pas marquee sur les cartes communes.", source, poi);
                case MaritimePoiType.DangerZone:
                    return CreateRumorFromPoi("La passe qui gronde", "On raconte qu'une eau rouge de bouees cache des recifs bas et un courant mauvais.", source, poi);
                case MaritimePoiType.Shipwreck:
                    return CreateRumorFromPoi("Bois mort sur les lames", "Une epave travaille encore au rythme de la houle. Les mouettes tournent bas quand le temps tombe.", source, poi);
                case MaritimePoiType.LargeIsland:
                    return CreateRumorFromPoi("Hauteurs vertes", "Une grande ile porterait des falaises nettes, visibles quand le soleil passe derriere les nuages.", source, poi);
                default:
                    return CreateRumorFromPoi("Sable sans nom", "Une petite ile dormirait loin des routes. On la reconnaitrait a sa plage basse et ses rochers noirs.", source, poi);
            }
        }

        private Rumor CreateRumorFromPoi(string title, string text, string source, WorldManager.WorldPoiInfo poi)
        {
            generatedRumorCounter++;
            Vector2 scatter = Random.insideUnitCircle * 120f;
            Rumor rumor = new Rumor
            {
                id = $"rumor_{generatedRumorCounter}_{poi.Type}_{poi.Coordinate.x}_{poi.Coordinate.y}",
                title = title,
                text = text,
                sourceName = source,
                targetName = poi.DisplayName,
                approximatePosition = poi.Position + new Vector3(scatter.x, 0f, scatter.y),
                hasApproximatePosition = true,
                uncertaintyRadius = 260f,
                poiType = poi.Type
            };
            return rumor;
        }

        private static string GetPortKey(PortManager port)
        {
            if (port == null)
            {
                return "runtime_unknown";
            }

            Vector3 position = port.transform.position;
            return $"{port.PortName}_{Mathf.RoundToInt(position.x)}_{Mathf.RoundToInt(position.z)}";
        }

        private static Vector3 GetReferencePosition()
        {
            BoatHelmController boat = FindFirstObjectByType<BoatHelmController>();
            if (boat != null)
            {
                return boat.transform.position;
            }

            Camera camera = Camera.main;
            return camera != null ? camera.transform.position : Vector3.zero;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
            {
                return;
            }

            Gizmos.color = new Color(0.62f, 0.78f, 1f, 0.42f);
            for (int i = 0; i < knownRumors.Count; i++)
            {
                Rumor rumor = knownRumors[i];
                if (rumor == null || !rumor.hasApproximatePosition)
                {
                    continue;
                }

                Gizmos.DrawWireSphere(rumor.approximatePosition, Mathf.Max(40f, rumor.uncertaintyRadius));
            }
        }
    }
}
