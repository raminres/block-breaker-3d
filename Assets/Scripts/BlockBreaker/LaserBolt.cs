using Arcade.Audio;
using Arcade.Core;
using UnityEngine;

namespace Arcade.BlockBreaker
{
    /// <summary>
    /// High-velocity vertical laser projectile fired from the paddle.
    /// Traverses the playfield at high speed and shatters blocks upon contact.
    /// </summary>
    public class LaserBolt : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private float speed = 34f;
        [SerializeField] private float topBoundaryY = 25.5f;
        [SerializeField] private Color boltColor = new Color(1f, 0.18f, 0.28f, 1f); // Electric Ruby / Crimson

        [Header("Modular Sockets")]
        [SerializeField] private GameObject modelChild;

        private bool hasHit = false;
        private static Material sharedLaserBoltMat;
        private static GameObject cachedLaserBoltPrefab;
        public const string PREFAB_PATH = "Assets/Prefabs/Paddle/PF_LaserBolt.prefab";

        public float Speed => speed;
        public Color BoltColor => boltColor;
        public GameObject ModelChild => modelChild != null ? modelChild : (modelChild = transform.Find("model")?.gameObject);

        public static void SetPrefab(GameObject prefab)
        {
            cachedLaserBoltPrefab = prefab;
        }

        public static LaserBolt Spawn(Vector3 position, Color? color = null)
        {
            GameObject boltGo = null;
#if UNITY_EDITOR
            if (cachedLaserBoltPrefab == null)
            {
                cachedLaserBoltPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            }
#endif
            if (cachedLaserBoltPrefab != null)
            {
                boltGo = Object.Instantiate(cachedLaserBoltPrefab, new Vector3(position.x, position.y, 0f), Quaternion.identity);
                boltGo.name = "LaserBolt";
            }
            else
            {
                boltGo = new GameObject("LaserBolt");
                boltGo.transform.position = new Vector3(position.x, position.y, 0f);
                boltGo.transform.localScale = Vector3.one;

                var col = boltGo.AddComponent<CapsuleCollider>();
                col.isTrigger = true;
                col.radius = 0.10f;
                col.height = 0.54f;
                col.center = new Vector3(0f, 0.27f, 0f);
                col.direction = 1;

                var modelGo = new GameObject("model");
                modelGo.transform.SetParent(boltGo.transform, false);
                modelGo.transform.localPosition = Vector3.zero;
                modelGo.transform.localRotation = Quaternion.identity;
                modelGo.transform.localScale = Vector3.one;

                boltGo.AddComponent<LaserBolt>();
            }

            var rb = boltGo.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = boltGo.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            var bolt = boltGo.GetComponent<LaserBolt>();
            if (bolt.modelChild == null)
            {
                var m = boltGo.transform.Find("model");
                if (m != null) bolt.modelChild = m.gameObject;
            }

            if (color.HasValue)
            {
                bolt.boltColor = color.Value;
                var targetRend = bolt.modelChild != null ? bolt.modelChild.GetComponent<MeshRenderer>() : boltGo.GetComponent<MeshRenderer>();
                if (targetRend != null)
                {
                    targetRend.sharedMaterial = GetOrCreateBoltMaterial(bolt.boltColor);
                }
            }

            return bolt;
        }

        private static Material GetOrCreateBoltMaterial(Color col)
        {
            if (sharedLaserBoltMat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit")
                    ?? Shader.Find("Arcade/VFX_ParticleBurst")
                    ?? Shader.Find("Sprites/Default");

                sharedLaserBoltMat = new Material(shader)
                {
                    name = "M_LaserBolt_URP"
                };
            }

            var mat = new Material(sharedLaserBoltMat);
            mat.SetColor("_BaseColor", col);
            mat.SetColor("_Color", col);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", col * 2.0f);
            }
            return mat;
        }

        private void Update()
        {
            if (hasHit) return;
            if (ArcadeGameManager.Instance != null && ArcadeGameManager.Instance.IsLevelClearPending) return;

            SimulateStep(Time.deltaTime);
        }

        public void SimulateStepForTesting(float deltaTime)
        {
            SimulateStep(deltaTime);
        }

        private void SimulateStep(float dt)
        {
            if (hasHit) return;

            Vector3 prevPos = transform.position;
            float effectiveSpeed = speed > 0f ? speed : 34f;
            float stepDistance = effectiveSpeed * dt;
            Vector3 nextPos = prevPos + Vector3.up * stepDistance;
            const float bulletRadius = 0.11f;
            const float bulletTipHeight = 0.54f;

            // 1. Immediate volume overlap check along current/swept segment to catch penetrated or spawned blocks
            Vector3 sweepCenter = prevPos + Vector3.up * ((stepDistance + bulletTipHeight) * 0.5f);
            Vector3 halfExtents = new Vector3(bulletRadius + 0.10f, (stepDistance + bulletTipHeight) * 0.5f, 0.5f);
            Collider[] overlaps = Physics.OverlapBox(sweepCenter, halfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < overlaps.Length; i++)
            {
                var block = overlaps[i].GetComponent<Block>() ?? overlaps[i].GetComponentInParent<Block>();
                if (block != null && !block.IsDestroyed)
                {
                    ProcessHit(block, overlaps[i].ClosestPoint(prevPos));
                    return;
                }
            }

            // 2. Continuous volumetric sweep (SphereCastAll) with bullet radius.
            // Using SphereCast matching bullet mesh radius ensures grazing impacts on block edges are cleanly detected.
            // Filtering all hits ensures powerup drops, triggers, or non-block colliders never obstruct detection of blocks behind them.
            Ray ray = new Ray(prevPos, Vector3.up);
            RaycastHit[] hits = Physics.SphereCastAll(ray, bulletRadius, stepDistance + bulletTipHeight, ~0, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                RaycastHit closestHit = default;
                Block closestBlock = null;
                float closestDist = float.MaxValue;

                for (int i = 0; i < hits.Length; i++)
                {
                    var block = hits[i].collider.GetComponent<Block>() ?? hits[i].collider.GetComponentInParent<Block>();
                    if (block != null && !block.IsDestroyed)
                    {
                        if (hits[i].distance < closestDist)
                        {
                            closestDist = hits[i].distance;
                            closestHit = hits[i];
                            closestBlock = block;
                        }
                    }
                }

                if (closestBlock != null)
                {
                    Vector3 hitPt = closestHit.point != Vector3.zero ? closestHit.point : closestBlock.transform.position;
                    ProcessHit(closestBlock, hitPt);
                    return;
                }
            }

            transform.position = nextPos;

            // Despawn when exceeding top boundary
            if (transform.position.y >= topBoundaryY)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            var block = other.GetComponent<Block>() ?? other.GetComponentInParent<Block>();
            if (block != null && !block.IsDestroyed)
            {
                ProcessHit(block, other.ClosestPoint(transform.position));
            }
        }

        private void ProcessHit(Block block, Vector3 hitPoint)
        {
            if (hasHit) return;
            hasHit = true;
            transform.position = hitPoint;
            block.TakeHit(Vector3.down);

            if (BlockVFXManager.Instance != null)
            {
                BlockVFXManager.Instance.PlayPowerupCollect(hitPoint, boltColor);
            }

            if (Application.isPlaying)
                Destroy(gameObject);
            else
                DestroyImmediate(gameObject);
        }

        /// <summary>
        /// Cleans up and destroys all active in-flight laser bolt projectiles in the scene.
        /// Called on ball loss, level cleared, or level restart to prevent orphan bolts from hitting newly spawned blocks.
        /// </summary>
        public static void ClearAllActiveBolts()
        {
            var bolts = FindObjectsByType<LaserBolt>();
            for (int i = 0; i < bolts.Length; i++)
            {
                if (bolts[i] != null)
                {
                    if (Application.isPlaying)
                        Destroy(bolts[i].gameObject);
                    else
                        DestroyImmediate(bolts[i].gameObject);
                }
            }
        }
    }
}
