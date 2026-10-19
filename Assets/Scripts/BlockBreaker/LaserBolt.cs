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
        private const string PREFAB_PATH = "Assets/Prefabs/Paddle/PF_LaserBolt.prefab";

        public float Speed => speed;
        public Color BoltColor => boltColor;
        public GameObject ModelChild => modelChild != null ? modelChild : (modelChild = transform.Find("model")?.gameObject);

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
                col.radius = 0.09f;
                col.height = 1.1f;
                col.direction = 1;

                var modelGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                modelGo.name = "model";
                modelGo.transform.SetParent(boltGo.transform, false);
                modelGo.transform.localPosition = Vector3.zero;
                modelGo.transform.localRotation = Quaternion.identity;
                modelGo.transform.localScale = new Vector3(0.18f, 0.55f, 0.18f);

                var mCol = modelGo.GetComponent<Collider>();
                if (mCol != null) Object.DestroyImmediate(mCol);

                boltGo.AddComponent<LaserBolt>();
            }

            var bolt = boltGo.GetComponent<LaserBolt>();
            if (bolt.modelChild == null)
            {
                var m = boltGo.transform.Find("model");
                if (m != null) bolt.modelChild = m.gameObject;
            }

            if (color.HasValue) bolt.boltColor = color.Value;

            var targetRend = bolt.modelChild != null ? bolt.modelChild.GetComponent<MeshRenderer>() : boltGo.GetComponent<MeshRenderer>();
            if (targetRend != null)
            {
                targetRend.sharedMaterial = GetOrCreateBoltMaterial(bolt.boltColor);
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

            Vector3 prevPos = transform.position;
            float stepDistance = speed * Time.deltaTime;
            Vector3 nextPos = prevPos + Vector3.up * stepDistance;

            // Perform continuous sweep raycast to prevent tunneling through blocks at high velocity
            Ray ray = new Ray(prevPos, Vector3.up);
            if (Physics.Raycast(ray, out RaycastHit hit, stepDistance))
            {
                var block = hit.collider.GetComponent<Block>() ?? hit.collider.GetComponentInParent<Block>();
                if (block != null && !block.IsDestroyed)
                {
                    hasHit = true;
                    transform.position = hit.point;
                    block.TakeHit(Vector3.down);

                    if (BlockVFXManager.Instance != null)
                    {
                        BlockVFXManager.Instance.PlayPowerupCollect(hit.point, boltColor);
                    }

                    Destroy(gameObject);
                    return;
                }
            }

            transform.position = nextPos;

            // Despawn when exceeding top boundary
            if (transform.position.y >= topBoundaryY)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            var block = other.GetComponent<Block>() ?? other.GetComponentInParent<Block>();
            if (block != null && !block.IsDestroyed)
            {
                hasHit = true;
                block.TakeHit(Vector3.down);

                if (BlockVFXManager.Instance != null)
                {
                    BlockVFXManager.Instance.PlayPowerupCollect(transform.position, boltColor);
                }

                Destroy(gameObject);
            }
        }

        public void SimulateStepForTesting(float deltaTime)
        {
            if (hasHit) return;

            Vector3 prevPos = transform.position;
            float effectiveSpeed = speed > 0f ? speed : 34f;
            float stepDistance = effectiveSpeed * deltaTime;
            Vector3 nextPos = prevPos + Vector3.up * stepDistance;

            Ray ray = new Ray(prevPos, Vector3.up);
            if (Physics.Raycast(ray, out RaycastHit hit, stepDistance, ~0, QueryTriggerInteraction.Collide))
            {
                var block = hit.collider.GetComponent<Block>() ?? hit.collider.GetComponentInParent<Block>();
                if (block != null && !block.IsDestroyed)
                {
                    hasHit = true;
                    transform.position = hit.point;
                    block.TakeHit(Vector3.down);
                    return;
                }
            }

            // Volume sweep check along projectile path
            Collider[] overlaps = Physics.OverlapBox(prevPos + Vector3.up * (stepDistance * 0.5f), new Vector3(0.5f, stepDistance * 0.5f, 0.5f), Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < overlaps.Length; i++)
            {
                var block = overlaps[i].GetComponent<Block>() ?? overlaps[i].GetComponentInParent<Block>();
                if (block != null && !block.IsDestroyed)
                {
                    hasHit = true;
                    transform.position = overlaps[i].transform.position;
                    block.TakeHit(Vector3.down);
                    return;
                }
            }

            transform.position = nextPos;
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
