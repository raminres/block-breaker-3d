using Arcade.BlockBreaker;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Arcade.Tests
{
    public class PaddlePrefabTests
    {
        public const string PaddlePrefabPath = "Assets/Prefabs/Paddle/PF_Paddle.prefab";

        [Test]
        public void PaddlePrefab_ExistsOnDisk()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            Assert.IsNotNull(go, $"Paddle prefab must exist at '{PaddlePrefabPath}'. Run 'Tools > Arcade > Generate Paddle Prefabs' if missing.");
        }

        [Test]
        public void PaddlePrefab_Root_HasPhysicsAndControllers()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            Assert.IsNotNull(go, "Prefab must exist.");

            Assert.AreEqual(Vector3.one, go.transform.localScale, "Paddle root scale must be normalized (1, 1, 1).");

            var col = go.GetComponent<BoxCollider>();
            Assert.IsNotNull(col, "Root must have a BoxCollider.");
            Assert.AreEqual(0.38f, col.center.y, 0.01f, "BoxCollider center Y must align with strike deck.");
            Assert.AreEqual(5.0f, col.size.x, 0.01f, "BoxCollider size.x must match default paddle width 5.0f for normalized parent root scale.");
            Assert.AreEqual(0.24f, col.size.y, 0.01f, "BoxCollider height must match strike deck thickness.");

            var rb = go.GetComponent<Rigidbody>();
            Assert.IsNotNull(rb, "Root must have a Rigidbody.");
            Assert.IsTrue(rb.isKinematic, "Rigidbody must be kinematic.");
            Assert.AreEqual(CollisionDetectionMode.ContinuousSpeculative, rb.collisionDetectionMode);

            var paddleCtrl = go.GetComponent<PaddleController>();
            Assert.IsNotNull(paddleCtrl, "Root must have PaddleController.");

            var laserCtrl = go.GetComponent<PaddleLaserController>();
            Assert.IsNotNull(laserCtrl, "Root must have PaddleLaserController.");
        }

        [Test]
        public void PaddlePrefab_Children_HasCompleteHierarchyAndNoCollidersOnVisuals()
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            Assert.IsNotNull(go, "Prefab must exist.");

            // 1. visuals
            var visuals = go.transform.Find("visuals");
            Assert.IsNotNull(visuals, "Paddle must have 'visuals' container child.");
            Assert.AreEqual(Vector3.one, visuals.localScale, "visuals container scale must be normalized (1, 1, 1).");
            var deck = visuals.Find("deck") ?? visuals.Find("Deck");
            var chassis = visuals.Find("chassis") ?? visuals.Find("Chassis");
            var keel = visuals.Find("keel") ?? visuals.Find("Keel");
            Assert.IsNotNull(deck, "visuals must contain 'deck' or 'Deck' (paddle top).");
            Assert.IsNotNull(chassis, "visuals must contain 'chassis' or 'Chassis' (paddle mid).");
            Assert.IsNotNull(keel, "visuals must contain 'keel' or 'Keel' (paddle bottom).");

            Assert.IsNull(deck.GetComponent<Collider>(), "deck must not have collider.");
            Assert.IsNull(chassis.GetComponent<Collider>(), "chassis must not have collider.");
            Assert.IsNull(keel.GetComponent<Collider>(), "keel must not have collider.");

            // 2. guns
            var guns = go.transform.Find("guns");
            Assert.IsNotNull(guns, "Paddle must have 'guns' container child.");
            Assert.AreEqual(Vector3.one, guns.localScale, "guns container scale must be normalized (1, 1, 1).");
            var gunLeft = guns.Find("Gun_Left");
            var gunRight = guns.Find("Gun_Right");
            Assert.IsNotNull(gunLeft, "guns must contain 'Gun_Left'.");
            Assert.IsNotNull(gunRight, "guns must contain 'Gun_Right'.");
            Assert.IsNotNull(gunLeft.Find("Muzzle_Left"), "Gun_Left must have 'Muzzle_Left' child transform.");
            Assert.IsNotNull(gunRight.Find("Muzzle_Right"), "Gun_Right must have 'Muzzle_Right' child transform.");
            Assert.IsNull(gunLeft.GetComponent<Collider>(), "Gun_Left must not have collider.");
            Assert.IsNull(gunRight.GetComponent<Collider>(), "Gun_Right must not have collider.");

            // 3. laser_gun
            var laserGun = go.transform.Find("laser_gun");
            Assert.IsNotNull(laserGun, "Paddle must have 'laser_gun' container child.");
            Assert.IsNotNull(laserGun.Find("Aperture"), "laser_gun must contain 'Aperture'.");
            Assert.IsNotNull(laserGun.Find("VFX_Railgun_HyperBeam"), "laser_gun must contain 'VFX_Railgun_HyperBeam'.");

            // 4. frost
            var frost = go.transform.Find("frost");
            Assert.IsNotNull(frost, "Paddle must have 'frost' container child.");
            Assert.AreEqual(Vector3.one, frost.localScale, "frost container scale must be normalized (1, 1, 1).");
            var iceShell = frost.Find("Ice_Shell");
            Assert.IsNotNull(iceShell, "frost must contain 'Ice_Shell'.");
            Assert.IsNull(iceShell.GetComponent<Collider>(), "Ice_Shell must not have collider.");

            // 5. vfx
            var vfx = go.transform.Find("vfx");
            Assert.IsNotNull(vfx, "Paddle must have 'vfx' container child.");
            Assert.IsNotNull(vfx.Find("Thruster_Left"), "vfx must contain 'Thruster_Left'.");
            Assert.IsNotNull(vfx.Find("Thruster_Right"), "vfx must contain 'Thruster_Right'.");
            Assert.IsNotNull(vfx.Find("Spark_Anchor"), "vfx must contain 'Spark_Anchor'.");

            // 6. dock_point
            var dockPoint = go.transform.Find("dock_point");
            Assert.IsNotNull(dockPoint, "Paddle must have 'dock_point' transform child.");
        }

        [Test]
        public void PaddlePrefab_Guns_ToggleWithLaserBlasterActivation()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            Assert.IsNotNull(prefab, "Prefab must exist.");

            var instance = Object.Instantiate(prefab);
            try
            {
                var laserCtrl = instance.GetComponent<PaddleLaserController>();
                Assert.IsNotNull(laserCtrl);

                var guns = instance.transform.Find("guns");
                Assert.IsNotNull(guns);
                Assert.IsFalse(guns.gameObject.activeSelf, "Guns should be inactive by default.");

                laserCtrl.ActivateLaserBlaster(10f);
                Assert.IsTrue(guns.gameObject.activeSelf, "Guns should be active when Laser Blaster is activated.");

                laserCtrl.DeactivateLaserBlaster();
                Assert.IsFalse(guns.gameObject.activeSelf, "Guns should be inactive when Laser Blaster is deactivated.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PaddlePrefab_Frost_TogglesWithFreezeState()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            Assert.IsNotNull(prefab, "Prefab must exist.");

            var instance = Object.Instantiate(prefab);
            try
            {
                var paddleCtrl = instance.GetComponent<PaddleController>();
                Assert.IsNotNull(paddleCtrl);

                var frost = instance.transform.Find("frost");
                Assert.IsNotNull(frost);
                Assert.IsFalse(frost.gameObject.activeSelf, "Frost should be inactive by default.");

                paddleCtrl.SetFrozen(true);
                Assert.IsTrue(frost.gameObject.activeSelf, "Frost should be active when paddle is frozen.");

                paddleCtrl.SetFrozen(false);
                Assert.IsFalse(frost.gameObject.activeSelf, "Frost should be inactive when paddle is defrosted.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PaddlePrefab_LaserGun_TogglesWithHyperBeam()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            Assert.IsNotNull(prefab, "Prefab must exist.");

            var instance = Object.Instantiate(prefab);
            try
            {
                var laserCtrl = instance.GetComponent<PaddleLaserController>();
                Assert.IsNotNull(laserCtrl);

                var laserGun = instance.transform.Find("laser_gun");
                Assert.IsNotNull(laserGun);
                Assert.IsFalse(laserGun.gameObject.activeSelf, "laser_gun should be inactive by default.");

                laserCtrl.FireRailgunHyperBeam(5f);
                Assert.IsTrue(laserGun.gameObject.activeSelf, "laser_gun should be active when hyper beam fires.");

                laserCtrl.DeactivateHyperBeam();
                Assert.IsFalse(laserGun.gameObject.activeSelf, "laser_gun should be inactive when hyper beam finishes.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PaddlePrefab_DockPoint_PositionedAboveDeck()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            Assert.IsNotNull(prefab, "Prefab must exist.");

            var instance = Object.Instantiate(prefab);
            try
            {
                var paddleCtrl = instance.GetComponent<PaddleController>();
                Assert.IsNotNull(paddleCtrl);
                Assert.IsNotNull(paddleCtrl.DockPoint, "DockPoint must be wired.");
                Assert.Greater(paddleCtrl.DockPoint.localPosition.y, 0.38f, "DockPoint must be positioned above top strike deck (Y = 0.38).");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PaddlePrefab_VisualsAndColliders_ScaleCorrectlyWithPowerups_WithoutDoubleScaling()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            Assert.IsNotNull(prefab, "Prefab must exist.");

            var instance = Object.Instantiate(prefab);
            try
            {
                var paddleCtrl = instance.GetComponent<PaddleController>();
                Assert.IsNotNull(paddleCtrl);

                // Initialize paddle to base width 5.0
                paddleCtrl.ResetWidth(5.0f);
                Assert.AreEqual(5.0f, paddleCtrl.Width, 0.01f);
                Assert.AreEqual(Vector3.one, instance.transform.localScale, "Paddle parent scale must remain normalized Vector3.one.");

                // BoxCollider world width must be 5.0f
                var col = instance.GetComponent<BoxCollider>();
                Assert.IsNotNull(col);
                float colWorldWidth = col.size.x * instance.transform.lossyScale.x;
                Assert.AreEqual(5.0f, colWorldWidth, 0.01f, "BoxCollider world width must match paddle width 5.0f.");

                // Strike deck visual world width must be 5.0f (NOT 25.0f or 27.5f)
                var deck = instance.transform.Find("visuals/deck") ?? instance.transform.Find("visuals/Deck");
                Assert.IsNotNull(deck, "visuals/deck or visuals/Deck must exist.");
                var deckMf = deck.GetComponent<MeshFilter>();
                Assert.IsNotNull(deckMf, "deck must have MeshFilter.");
                float deckMeshWidth = deckMf.sharedMesh.bounds.size.x;
                float deckWorldWidth = deckMeshWidth * deck.lossyScale.x;
                Assert.AreEqual(5.0f, deckWorldWidth, 0.05f, "Visual strike deck world width must match 5.0f without double scaling.");

                // Expand width by +10% (Expander powerup)
                paddleCtrl.ExpandWidth(0.10f);
                float expectedExpandedWidth = 5.0f * 1.10f; // 5.50f
                Assert.AreEqual(expectedExpandedWidth, paddleCtrl.Width, 0.01f);
                colWorldWidth = col.size.x * instance.transform.lossyScale.x;
                Assert.AreEqual(expectedExpandedWidth, colWorldWidth, 0.01f, "BoxCollider world width must expand with powerup.");
                deckWorldWidth = deckMeshWidth * deck.lossyScale.x;
                Assert.AreEqual(expectedExpandedWidth, deckWorldWidth, 0.05f, "Visual strike deck world width must expand with powerup without double scaling.");

                // Shrink width by -18% (Shortener hazard)
                paddleCtrl.ShrinkWidth(0.18f);
                float expectedShrunkWidth = expectedExpandedWidth * (1.0f - 0.18f); // ~4.51f
                Assert.AreEqual(expectedShrunkWidth, paddleCtrl.Width, 0.01f);
                colWorldWidth = col.size.x * instance.transform.lossyScale.x;
                Assert.AreEqual(expectedShrunkWidth, colWorldWidth, 0.01f, "BoxCollider world width must shrink with hazard.");
                deckWorldWidth = deckMeshWidth * deck.lossyScale.x;
                Assert.AreEqual(expectedShrunkWidth, deckWorldWidth, 0.05f, "Visual strike deck world width must shrink with hazard.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PaddlePrefab_Level1Width5Point0_CalibratesAccurately()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            Assert.IsNotNull(prefab, "Prefab must exist.");

            var instance = Object.Instantiate(prefab);
            try
            {
                var paddleCtrl = instance.GetComponent<PaddleController>();
                Assert.IsNotNull(paddleCtrl);

                // Level 1 starts with initialPaddleWidth = 5.0f
                paddleCtrl.ResetWidth(5.0f);
                Assert.AreEqual(5.0f, paddleCtrl.Width, 0.01f);
                Assert.AreEqual(Vector3.one, instance.transform.localScale, "Paddle parent scale must remain normalized Vector3.one.");

                var visuals = instance.transform.Find("visuals");
                Assert.IsNotNull(visuals, "visuals socket must exist.");
                Assert.AreEqual(Vector3.one, visuals.localScale, "Visuals local scale must be Vector3.one at standard 5.0 width.");

                var col = instance.GetComponent<BoxCollider>();
                float colWorldWidth = col.size.x * instance.transform.lossyScale.x;
                Assert.AreEqual(5.0f, colWorldWidth, 0.01f);

                var deck = instance.transform.Find("visuals/deck") ?? instance.transform.Find("visuals/Deck");
                var deckMf = deck.GetComponent<MeshFilter>();
                float deckWorldWidth = deckMf.sharedMesh.bounds.size.x * deck.lossyScale.x;
                Assert.AreEqual(5.0f, deckWorldWidth, 0.05f, "Level 1 paddle must be exactly 5.0 units wide visually and physically.");

                // When collecting Paddle Expander (+10%), visuals X scales to 1.10f
                paddleCtrl.ExpandWidth(0.10f);
                Assert.AreEqual(5.5f, paddleCtrl.Width, 0.01f);
                Assert.AreEqual(1.10f, visuals.localScale.x, 0.01f, "Visuals X scale must scale to 1.10 only upon paddle expander powerup.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void LaserBolt_PrefabAndSpawn_ScaleNormalizedToVector3One()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Paddle/PF_LaserBolt.prefab");
            Assert.IsNotNull(prefab, "PF_LaserBolt.prefab must exist in Assets/Prefabs/Paddle/.");
            Assert.AreEqual(Vector3.one, prefab.transform.localScale, "Laser bolt root scale must be normalized to Vector3.one.");

            var model = prefab.transform.Find("model");
            Assert.IsNotNull(model, "Laser bolt prefab must have child 'model'.");
            Assert.AreEqual(new Vector3(0.18f, 0.55f, 0.18f), model.localScale, "Child 'model' must be scaled to (0.18, 0.55, 0.18) for 1:1 visual dimensions.");
            Assert.IsNull(model.GetComponent<Collider>(), "Child 'model' must not have a collider.");

            var bolt = LaserBolt.Spawn(Vector3.zero);
            try
            {
                Assert.IsNotNull(bolt);
                Assert.AreEqual(Vector3.one, bolt.transform.localScale, "Spawned LaserBolt root scale must be normalized to Vector3.one.");
                Assert.IsNotNull(bolt.ModelChild, "Spawned LaserBolt must link ModelChild.");
                Assert.AreEqual(new Vector3(0.18f, 0.55f, 0.18f), bolt.ModelChild.transform.localScale);
            }
            finally
            {
                if (bolt != null) Object.DestroyImmediate(bolt.gameObject);
            }
        }

        [Test]
        public void Paddle_Level1PresetConfiguration_StartsAtScaleOne_AndExpandsTo1Point1OnPowerup()
        {
            var level1Config = AssetDatabase.LoadAssetAtPath<LevelConfiguration>("Assets/Settings/Levels/SO_Level_01.asset");
            Assert.IsNotNull(level1Config, "SO_Level_01 must exist.");
            Assert.AreEqual(5.0f, level1Config.InitialPaddleWidth, 0.01f, "Level 1 InitialPaddleWidth must be 5.0f so paddle starts unscaled.");

            var paddlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlePrefabPath);
            var paddleObj = Object.Instantiate(paddlePrefab);
            try
            {
                var paddle = paddleObj.GetComponent<PaddleController>();
                // Simulate LevelGenerator applying level config on game start
                paddle.ResetWidth(level1Config.InitialPaddleWidth);

                var visuals = paddleObj.transform.Find("visuals");
                Assert.IsNotNull(visuals, "visuals child socket must exist.");

                // When starting a new game, visuals X scale must be exactly 1.0f
                Assert.AreEqual(1.0f, visuals.localScale.x, 0.001f, "Visuals X scale must be 1.0f at start of game.");
                Assert.AreEqual(1.0f, visuals.localScale.y, 0.001f);
                Assert.AreEqual(1.0f, visuals.localScale.z, 0.001f);

                // When collecting paddle size increase powerup (+10% expander)
                paddle.ExpandWidth(BlockModifierExtensions.PADDLE_EXPANSION_PERCENT);
                Assert.AreEqual(5.50f, paddle.Width, 0.01f);
                Assert.AreEqual(1.10f, visuals.localScale.x, 0.001f, "Visuals X scale must scale to 1.10 only upon collecting paddle size increase powerup.");
            }
            finally
            {
                Object.DestroyImmediate(paddleObj);
            }
        }
    }
}
