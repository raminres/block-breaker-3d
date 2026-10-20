using Arcade.BlockBreaker;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Arcade.Tests
{
    [TestFixture]
    public class PowerupPrefabTests
    {
        private const string PowerupPrefabPath = "Assets/Prefabs/Powerups/PF_Drop_Powerup.prefab";
        private const string HazardPrefabPath = "Assets/Prefabs/Powerups/PF_Drop_Hazard.prefab";

        [Test]
        public void DropPrefabs_AllExistOnDisk()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(PowerupPrefabPath), "PF_Drop_Powerup.prefab must exist.");
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(HazardPrefabPath), "PF_Drop_Hazard.prefab must exist.");
        }

        [Test]
        public void DropPrefab_Powerup_HasModularComponents()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PowerupPrefabPath);
            Assert.IsNotNull(go.GetComponent<BoxCollider>(), "PF_Drop_Powerup must have BoxCollider.");
            Assert.IsTrue(go.GetComponent<BoxCollider>().isTrigger, "BoxCollider must be a trigger.");
            Assert.IsNotNull(go.GetComponent<Rigidbody>(), "PF_Drop_Powerup must have Rigidbody.");
            Assert.IsTrue(go.GetComponent<Rigidbody>().isKinematic, "Rigidbody must be kinematic.");

            var cap = go.GetComponent<PowerupCapsule>();
            Assert.IsNotNull(cap, "PF_Drop_Powerup must have PowerupCapsule component.");

            // 1. Visual Model
            var visual = go.transform.Find("Visual_Capsule");
            Assert.IsNotNull(visual, "Must have child 'Visual_Capsule'.");
            var mf = visual.GetComponent<MeshFilter>();
            Assert.IsNotNull(mf, "Visual_Capsule must have MeshFilter.");
            Assert.IsTrue(mf.sharedMesh.name.Contains("PowerUp") || mf.sharedMesh.name.Contains("Capsule"), "Powerup mesh must be a PowerUp 3D model or Capsule.");
            Assert.IsNotNull(visual.GetComponent<MeshRenderer>(), "Visual_Capsule must have MeshRenderer.");

            // 2. Billboard Sprite Icon
            var icon = go.transform.Find("Icon_Billboard");
            Assert.IsNotNull(icon, "Must have child 'Icon_Billboard'.");
            Assert.AreEqual(LayerMask.NameToLayer("UI"), icon.gameObject.layer, "Icon_Billboard must be on the UI layer to bypass Bloom.");
            Assert.IsNotNull(icon.GetComponent<SpriteRenderer>(), "Icon_Billboard must have SpriteRenderer.");

            // 3. Falling Particle Trail VFX
            var vfx = go.transform.Find("Falling_Vfx");
            Assert.IsNotNull(vfx, "Must have child 'Falling_Vfx'.");
            var ps = vfx.GetComponent<ParticleSystem>();
            Assert.IsNotNull(ps, "Falling_Vfx must have ParticleSystem.");
            Assert.IsTrue(ps.main.loop, "Falling_Vfx particle system must be looping.");
        }

        [Test]
        public void DropPrefab_Hazard_HasModularComponents()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(HazardPrefabPath);
            var cap = go.GetComponent<PowerupCapsule>();
            Assert.IsNotNull(cap, "PF_Drop_Hazard must have PowerupCapsule component.");

            // 1. Visual Model (Diamond / Faceted Cube)
            var visual = go.transform.Find("Visual_Capsule");
            Assert.IsNotNull(visual, "Must have child 'Visual_Capsule'.");
            var mf = visual.GetComponent<MeshFilter>();
            Assert.IsNotNull(mf, "Visual_Capsule must have MeshFilter.");
            Assert.IsTrue(mf.sharedMesh.name.Contains("Hazard") || mf.sharedMesh.name.Contains("Cube"), "Hazard mesh must be a Hazard 3D model or faceted Cube diamond.");

            // 2. Billboard Sprite Icon
            var icon = go.transform.Find("Icon_Billboard");
            Assert.IsNotNull(icon, "Must have child 'Icon_Billboard'.");
            Assert.AreEqual(LayerMask.NameToLayer("UI"), icon.gameObject.layer, "Icon_Billboard must be on the UI layer to bypass Bloom.");
            Assert.IsNotNull(icon.GetComponent<SpriteRenderer>(), "Icon_Billboard must have SpriteRenderer.");

            // 3. Falling Particle Trail VFX
            var vfx = go.transform.Find("Falling_Vfx");
            Assert.IsNotNull(vfx, "Must have child 'Falling_Vfx'.");
            var ps = vfx.GetComponent<ParticleSystem>();
            Assert.IsNotNull(ps, "Falling_Vfx must have ParticleSystem.");
            Assert.IsTrue(ps.main.loop, "Falling_Vfx particle system must be looping.");
        }

        [Test]
        public void PowerupCapsule_Spawn_CreatesFullyConfiguredDrop()
        {
            var drop = PowerupCapsule.Spawn(new Vector3(0f, 5f, 0f), BlockSpecialType.Laser);
            try
            {
                Assert.IsNotNull(drop);
                Assert.IsNotNull(drop.VisualCapsuleTransform, "Spawned drop must have VisualCapsuleTransform.");
                Assert.IsNotNull(drop.IconTransform, "Spawned drop must have IconTransform.");
                Assert.IsNotNull(drop.FallingVfxTransform, "Spawned drop must have FallingVfxTransform.");
                Assert.IsNotNull(drop.IconRenderer.sprite, "Spawned drop must have resolved icon sprite.");
            }
            finally
            {
                Object.DestroyImmediate(drop.gameObject);
            }
        }

        [Test]
        public void PowerupCapsule_Update_OscillatesOnYAxisWithinClampedAngle()
        {
            var drop = PowerupCapsule.Spawn(new Vector3(0f, 5f, 0f), BlockSpecialType.Laser);
            try
            {
                // Call oscillation step with test timestamp
                drop.UpdateOscillation(1.5f);

                Vector3 euler = drop.VisualCapsuleTransform.localEulerAngles;
                float x = euler.x > 180f ? euler.x - 360f : euler.x;
                float y = euler.y > 180f ? euler.y - 360f : euler.y;
                float z = euler.z > 180f ? euler.z - 360f : euler.z;

                Assert.AreEqual(0f, x, 0.05f, "Visual capsule must have 0 X rotation to avoid clipping billboard sprite.");
                Assert.AreEqual(0f, z, 0.05f, "Visual capsule must have 0 Z rotation to avoid clipping billboard sprite.");
                Assert.IsTrue(Mathf.Abs(y) <= 20.01f, $"Y rotation must remain within [-20, 20] degrees. Actual: {y}");
            }
            finally
            {
                Object.DestroyImmediate(drop.gameObject);
            }
        }
    }
}
