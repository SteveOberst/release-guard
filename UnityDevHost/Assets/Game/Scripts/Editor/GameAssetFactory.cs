using System.Collections.Generic;
using AttackSurfaceFixture.Game.Core;
using AttackSurfaceFixture.Game.Data;
using AttackSurfaceFixture.Game.Runtime;
using AttackSurfaceFixture.Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AttackSurfaceFixture.Game.Editor
{
    /// <summary>
    /// Deterministic asset and scene authoring tool.
    ///
    /// Invoking <b>Attack Surface Fixture → Author Baseline Fixture Content</b> from the
    /// Unity menu rebuilds all data assets, prefabs and scenes from scratch, making the
    /// project fully reproducible from source control without binary scene assets.
    ///
    /// Editor-only. Not included in player builds.
    /// </summary>
    public static class GameAssetFactory
    {
        // -- Path constants

        private const string DataFolderPath = "Assets/Game/Data";
        private const string ConfigFolderPath = "Assets/Game/Configs";
        private const string SceneFolderPath = "Assets/Game/Scenes";
        private const string GamePrefabFolderPath = "Assets/Game/Prefabs";
        private const string TestTargetPrefabFolder = "Assets/TestTargets/Prefabs";

        private const string BootstrapScenePath = SceneFolderPath + "/Bootstrap.unity";
        private const string MainMenuScenePath = SceneFolderPath + "/MainMenu.unity";
        private const string GameplayScenePath = SceneFolderPath + "/Gameplay.unity";
        private const string ShopScenePath = SceneFolderPath + "/Shop.unity";

        private const string PlayerRigPrefabPath = GamePrefabFolderPath + "/PlayerRig.prefab";
        private const string EnemyDummyPrefabPath = GamePrefabFolderPath + "/EnemyDummy.prefab";
        private const string ShopStandPrefabPath = GamePrefabFolderPath + "/ShopStand.prefab";
        private const string TestTargetPrefabPath = TestTargetPrefabFolder + "/AdBeaconTarget.prefab";

        private static readonly string[] BaselineItemPaths =
        {
            DataFolderPath + "/health_potion.asset",
            DataFolderPath + "/iron_sword.asset",
            DataFolderPath + "/swift_boots.asset",
        };

        private static readonly string[] BaselineEnemyPaths =
        {
            DataFolderPath + "/slime_basic.asset",
            DataFolderPath + "/goblin_raider.asset",
            DataFolderPath + "/stone_golem.asset",
        };

        // -- Menu items

        [MenuItem("Attack Surface Fixture/Ensure Baseline Data Assets")]
        private static void EnsureBaselineDataAssetsMenu() => EnsureBaselineDataAssets();

        [MenuItem("Attack Surface Fixture/Author Baseline Fixture Content")]
        private static void AuthorBaselineFixtureContentMenu() => AuthorBaselineFixtureContent();

        // -- Public entry points

        public static void EnsureBaselineDataAssets()
        {
            EnsureFolder("Assets/Game", "Data");
            EnsureFolder("Assets/Game", "Configs");

            // -- Items
            CreateOrUpdateAsset<ItemDefinition>(BaselineItemPaths[0], so =>
            {
                SetString(so, "itemId", "health_potion");
                SetString(so, "displayName", "Health Potion");
                SetString(so, "description", "Restores 30 HP when used in battle.");
                SetInt(so, "softCurrencyPrice", 25);
                SetBool(so, "premiumOnly", false);
                SetString(so, "effectId", "heal");
            });

            CreateOrUpdateAsset<ItemDefinition>(BaselineItemPaths[1], so =>
            {
                SetString(so, "itemId", "iron_sword");
                SetString(so, "displayName", "Iron Sword");
                SetString(so, "description", "A reliable starter weapon. Boosts damage for 10 s when used.");
                SetInt(so, "softCurrencyPrice", 60);
                SetBool(so, "premiumOnly", false);
                SetString(so, "effectId", "damage_boost");
            });

            CreateOrUpdateAsset<ItemDefinition>(BaselineItemPaths[2], so =>
            {
                SetString(so, "itemId", "swift_boots");
                SetString(so, "displayName", "Swift Boots");
                SetString(so, "description", "Lightweight enchanted boots. Grants a speed bonus for 8 s.");
                SetInt(so, "softCurrencyPrice", 45);
                SetBool(so, "premiumOnly", false);
                SetString(so, "effectId", "speed_boost");
            });

            // -- Enemies
            CreateOrUpdateAsset<EnemyDefinition>(BaselineEnemyPaths[0], so =>
            {
                SetString(so, "enemyId", "slime_basic");
                SetString(so, "displayName", "Green Slime");
                SetInt(so, "maxHealth", 20);
                SetInt(so, "attackPower", 3);
                SetInt(so, "rewardSoftCurrency", 8);
                SetBool(so, "boss", false);
            });

            CreateOrUpdateAsset<EnemyDefinition>(BaselineEnemyPaths[1], so =>
            {
                SetString(so, "enemyId", "goblin_raider");
                SetString(so, "displayName", "Goblin Raider");
                SetInt(so, "maxHealth", 35);
                SetInt(so, "attackPower", 6);
                SetInt(so, "rewardSoftCurrency", 14);
                SetBool(so, "boss", false);
            });

            CreateOrUpdateAsset<EnemyDefinition>(BaselineEnemyPaths[2], so =>
            {
                SetString(so, "enemyId", "stone_golem");
                SetString(so, "displayName", "Stone Golem");
                SetInt(so, "maxHealth", 80);
                SetInt(so, "attackPower", 12);
                SetInt(so, "rewardSoftCurrency", 35);
                SetBool(so, "boss", true);
            });

            // -- Economy config
            CreateOrUpdateAsset<EconomyConfig>(ConfigFolderPath + "/economy.asset", so =>
            {
                SetInt(so, "startingSoftCurrency", 150);
                SetInt(so, "shopRefreshCost", 25);
                SetInt(so, "reviveCost", 50);

                var costs = FindProperty(so, "upgradeCosts");
                costs.arraySize = 3;
                costs.GetArrayElementAtIndex(0).intValue = 25;
                costs.GetArrayElementAtIndex(1).intValue = 50;
                costs.GetArrayElementAtIndex(2).intValue = 100;
            });

            // -- Feature flags
            CreateOrUpdateAsset<FeatureFlags>(ConfigFolderPath + "/feature_flags.asset", so =>
            {
                SetBool(so, "shopEnabled", true);
                SetBool(so, "eventBannerEnabled", true);
                SetBool(so, "debugPanelEnabled", false);
                SetBool(so, "premiumOffersEnabled", true);
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void AuthorBaselineFixtureContent()
        {
            EnsureBaselineDataAssets();
            EnsureFolder("Assets/Game", "Scenes");
            EnsureFolder("Assets/Game", "Prefabs");
            EnsureFolder("Assets/TestTargets", "Prefabs");

            var ctx = LoadAuthoringContext();

            CreateOrUpdatePrefab(PlayerRigPrefabPath, CreatePlayerRigPrefabRoot);
            CreateOrUpdatePrefab(EnemyDummyPrefabPath, CreateEnemyDummyPrefabRoot);
            CreateOrUpdatePrefab(ShopStandPrefabPath, CreateShopStandPrefabRoot);
            CreateOrUpdatePrefab(TestTargetPrefabPath, CreateTestTargetPrefabRoot);

            AuthorBootstrapScene(ctx);
            AuthorMainMenuScene(ctx);
            AuthorGameplayScene(ctx);
            AuthorShopScene(ctx);
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // -- Scene authoring

        private static void AuthorBootstrapScene(AuthoringContext ctx)
        {
            var scene = NewFixtureScene();
            CreateSharedSceneRig();
            CreateGroundPlane(new Vector3(12f, 1f, 12f));

            // -- Persistent systems (DontDestroyOnLoad)
            var persistentRoot = new GameObject("PersistentSystems");

            var gsm = persistentRoot.AddComponent<GameStateManager>();
            var ss = new GameObject("SaveSystem");
            ss.transform.SetParent(persistentRoot.transform, false);
            ss.AddComponent<SaveSystem>();

            var am = new GameObject("AudioManager");
            am.transform.SetParent(persistentRoot.transform, false);
            var audioManager = am.AddComponent<AudioManager>();
            // Add a music AudioSource that AudioManager can reference
            var musicSrc = am.AddComponent<AudioSource>();
            musicSrc.playOnAwake = false;

            // -- ThirdParty services
            var thirdParty = new GameObject("ThirdPartyServices");
            var analytics = thirdParty.AddComponent<AnalyticsLite.AnalyticsService>();
            var rcClient = thirdParty.AddComponent<RemoteConfigKit.RemoteConfigClient>();

            // Wire RemoteConfig to the local-defaults JSON
            var rcDefaults = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/ThirdParty/RemoteConfigKit/RemoteConfigDefaults.json");
            if (rcDefaults != null)
            {
                var rcSo = new SerializedObject(rcClient);
                rcSo.FindProperty("localDefaults")?.SetObjectReferenceValue(rcDefaults);
                rcSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // -- Bootstrap systems
            var bootstrapRoot = new GameObject("BootstrapSystems");

            var inventoryObj = new GameObject("InventorySystem");
            inventoryObj.transform.SetParent(bootstrapRoot.transform, false);
            var inventorySystem = inventoryObj.AddComponent<InventorySystem>();

            var shopObj = new GameObject("ShopSystem");
            shopObj.transform.SetParent(bootstrapRoot.transform, false);
            var shopSystem = shopObj.AddComponent<ShopSystem>();

            var bootstrapper = bootstrapRoot.AddComponent<Bootstrapper>();
            bootstrapper.ApplyFixtureReferences(
                ctx.EconomyConfig, ctx.FeatureFlags,
                ctx.StarterItems, ctx.EnemyCatalog,
                inventorySystem, shopSystem);
            EditorUtility.SetDirty(bootstrapper);

            // Wire shop catalog in Inspector so it survives the first save
            var shopSo = new SerializedObject(shopSystem);
            shopSo.FindProperty("economyConfig")?.SetObjectReferenceValue(ctx.EconomyConfig);
            shopSo.FindProperty("featureFlags")?.SetObjectReferenceValue(ctx.FeatureFlags);
            shopSo.ApplyModifiedPropertiesWithoutUndo();

            // -- Preview props
            var playerPrefab = LoadPrefab(PlayerRigPrefabPath);
            if (playerPrefab != null)
                InstantiatePrefab(playerPrefab, "BootstrapPlayerPreview", new Vector3(-2f, 0f, 0f));

            var targetPrefab = LoadPrefab(TestTargetPrefabPath);
            if (targetPrefab != null)
                InstantiatePrefab(targetPrefab, "TestBeacon", new Vector3(2.5f, 0f, 1.5f));

            SaveFixtureScene(scene, BootstrapScenePath);
        }

        private static void AuthorMainMenuScene(AuthoringContext ctx)
        {
            var scene = NewFixtureScene();
            CreateSharedSceneRig();
            CreateGroundPlane(new Vector3(10f, 1f, 10f));

            // -- 3D set dressing
            var menuRoot = new GameObject("MainMenuSet");
            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backdrop.name = "Backdrop";
            backdrop.transform.SetParent(menuRoot.transform, false);
            backdrop.transform.position = new Vector3(0f, 2.5f, 6f);
            backdrop.transform.localScale = new Vector3(8f, 5f, 0.25f);

            var shopStandPrefab = LoadPrefab(ShopStandPrefabPath);
            if (shopStandPrefab != null)
                InstantiatePrefab(shopStandPrefab, "FeaturedOfferStand", new Vector3(0f, 0f, 1.5f));

            var playerPrefab = LoadPrefab(PlayerRigPrefabPath);
            if (playerPrefab != null)
                InstantiatePrefab(playerPrefab, "HeroDisplay", new Vector3(-2.25f, 0f, 1f));

            // -- UI canvas
            var canvas = CreateScreenSpaceCanvas("MainMenuCanvas");
            var ctrl = canvas.AddComponent<MainMenuController>();

            // Title label
            var titleObj = CreateUIText(canvas.transform, "TitleLabel",
                "Attack Surface Demo", 48, TextAnchor.UpperCenter,
                new Vector2(0.1f, 0.7f), new Vector2(0.9f, 0.95f));

            // Version label
            var versionObj = CreateUIText(canvas.transform, "VersionLabel",
                "v0.1.0", 18, TextAnchor.LowerRight,
                new Vector2(0.7f, 0.02f), new Vector2(0.98f, 0.08f));

            // Play button
            var playBtn = CreateUIButton(canvas.transform, "PlayButton", "Play",
                new Vector2(0.35f, 0.4f), new Vector2(0.65f, 0.52f));

            // Quit button
            var quitBtn = CreateUIButton(canvas.transform, "QuitButton", "Quit",
                new Vector2(0.35f, 0.26f), new Vector2(0.65f, 0.38f));

            // Wire controller references
            var ctrlSo = new SerializedObject(ctrl);
            ctrlSo.FindProperty("playButton")?.SetObjectReferenceValue(playBtn.GetComponent<Button>());
            ctrlSo.FindProperty("quitButton")?.SetObjectReferenceValue(quitBtn.GetComponent<Button>());
            ctrlSo.FindProperty("titleLabel")?.SetObjectReferenceValue(titleObj.GetComponent<Text>());
            ctrlSo.FindProperty("versionLabel")?.SetObjectReferenceValue(versionObj.GetComponent<Text>());
            ctrlSo.ApplyModifiedPropertiesWithoutUndo();

            SaveFixtureScene(scene, MainMenuScenePath);
        }

        private static void AuthorGameplayScene(AuthoringContext ctx)
        {
            var scene = NewFixtureScene();
            CreateSharedSceneRig();
            CreateGroundPlane(new Vector3(20f, 1f, 20f));

            // -- Arena props
            var lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lane.name = "CombatLane";
            lane.transform.position = new Vector3(0f, 0.05f, 0f);
            lane.transform.localScale = new Vector3(4f, 0.1f, 12f);

            // -- Player
            var playerPrefab = LoadPrefab(PlayerRigPrefabPath);
            GameObject playerInstance = null;
            if (playerPrefab != null)
                playerInstance = InstantiatePrefab(playerPrefab, "Player", new Vector3(0f, 0f, -3.5f));

            if (playerInstance != null)
            {
                var playerCtrl = playerInstance.AddComponent<PlayerController>();
                var pcSo = new SerializedObject(playerCtrl);
                pcSo.FindProperty("economyConfig")?.SetObjectReferenceValue(ctx.EconomyConfig);
                pcSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // -- Enemies
            var enemyPrefab = LoadPrefab(EnemyDummyPrefabPath);
            if (enemyPrefab != null && ctx.EnemyCatalog.Length > 0)
            {
                var e0 = InstantiatePrefab(enemyPrefab, "Enemy_Slime", new Vector3(0f, 0f, 3.5f));
                var e1 = InstantiatePrefab(enemyPrefab, "Enemy_GoblinRaider", new Vector3(2.5f, 0f, 5.5f));
                var e2 = InstantiatePrefab(enemyPrefab, "Enemy_StoneGolem", new Vector3(-2.5f, 0f, 7f));

                WireEnemyController(e0, ctx.EnemyCatalog.Length > 0 ? ctx.EnemyCatalog[0] : null);
                WireEnemyController(e1, ctx.EnemyCatalog.Length > 1 ? ctx.EnemyCatalog[1] : null);
                WireEnemyController(e2, ctx.EnemyCatalog.Length > 2 ? ctx.EnemyCatalog[2] : null);
            }

            // -- Game systems
            var systemsRoot = new GameObject("GameplaySystems");
            var combatSystem = systemsRoot.AddComponent<CombatSystem>();
            var responderHost = systemsRoot.AddComponent<EventResponderHost>();

            // -- Gameplay HUD
            var canvas = CreateScreenSpaceCanvas("GameplayHUDCanvas");
            var hud = canvas.AddComponent<GameplayHUD>();

            var healthSlider = CreateUISlider(canvas.transform, "HealthBar",
                new Vector2(0.02f, 0.9f), new Vector2(0.35f, 0.97f));
            var currencyText = CreateUIText(canvas.transform, "CurrencyLabel",
                "150g", 20, TextAnchor.UpperRight,
                new Vector2(0.65f, 0.9f), new Vector2(0.98f, 0.97f));
            var killText = CreateUIText(canvas.transform, "KillCountLabel",
                "Kills: 0", 18, TextAnchor.UpperLeft,
                new Vector2(0.02f, 0.82f), new Vector2(0.3f, 0.89f));
            var combatText = CreateUIText(canvas.transform, "CombatStatusLabel",
                "", 20, TextAnchor.UpperCenter,
                new Vector2(0.3f, 0.82f), new Vector2(0.7f, 0.89f));
            var buffText = CreateUIText(canvas.transform, "BuffStatusLabel",
                "", 16, TextAnchor.UpperLeft,
                new Vector2(0.02f, 0.74f), new Vector2(0.35f, 0.81f));
            var rewardText = CreateUIText(canvas.transform, "LastRewardLabel",
                "", 18, TextAnchor.UpperCenter,
                new Vector2(0.38f, 0.74f), new Vector2(0.62f, 0.81f));
            var shopBtn = CreateUIButton(canvas.transform, "ShopButton", "Shop",
                new Vector2(0.78f, 0.02f), new Vector2(0.98f, 0.1f));
            var retreatBtn = CreateUIButton(canvas.transform, "RetreatButton", "Retreat",
                new Vector2(0.55f, 0.02f), new Vector2(0.75f, 0.1f));

            var hudSo = new SerializedObject(hud);
            hudSo.FindProperty("healthBar")?.SetObjectReferenceValue(healthSlider.GetComponent<Slider>());
            hudSo.FindProperty("currencyLabel")?.SetObjectReferenceValue(currencyText.GetComponent<Text>());
            hudSo.FindProperty("killCountLabel")?.SetObjectReferenceValue(killText.GetComponent<Text>());
            hudSo.FindProperty("combatStatusLabel")?.SetObjectReferenceValue(combatText.GetComponent<Text>());
            hudSo.FindProperty("buffStatusLabel")?.SetObjectReferenceValue(buffText.GetComponent<Text>());
            hudSo.FindProperty("lastRewardLabel")?.SetObjectReferenceValue(rewardText.GetComponent<Text>());
            hudSo.FindProperty("shopButton")?.SetObjectReferenceValue(shopBtn.GetComponent<Button>());
            hudSo.FindProperty("retreatButton")?.SetObjectReferenceValue(retreatBtn.GetComponent<Button>());
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            SaveFixtureScene(scene, GameplayScenePath);
        }

        private static void AuthorShopScene(AuthoringContext ctx)
        {
            var scene = NewFixtureScene();
            CreateSharedSceneRig();
            CreateGroundPlane(new Vector3(14f, 1f, 14f));

            // -- 3D props
            var shopStandPrefab = LoadPrefab(ShopStandPrefabPath);
            if (shopStandPrefab != null)
            {
                InstantiatePrefab(shopStandPrefab, "CentralShopStand", Vector3.zero);
                InstantiatePrefab(shopStandPrefab, "OfferStandLeft", new Vector3(-3.5f, 0f, 2f));
                InstantiatePrefab(shopStandPrefab, "OfferStandRight", new Vector3(3.5f, 0f, 2f));
            }

            var targetPrefab = LoadPrefab(TestTargetPrefabPath);
            if (targetPrefab != null)
                InstantiatePrefab(targetPrefab, "TelemetryBeacon", new Vector3(0f, 0f, -3.5f));

            // -- Shop UI canvas
            var canvas = CreateScreenSpaceCanvas("ShopCanvas");
            var ctrl = canvas.AddComponent<ShopController>();

            var currencyText = CreateUIText(canvas.transform, "CurrencyLabel",
                "150g", 22, TextAnchor.UpperRight,
                new Vector2(0.65f, 0.9f), new Vector2(0.98f, 0.97f));
            var feedbackText = CreateUIText(canvas.transform, "FeedbackLabel",
                "", 18, TextAnchor.UpperCenter,
                new Vector2(0.2f, 0.82f), new Vector2(0.8f, 0.89f));

            // Scroll list root for item slots
            var listRoot = new GameObject("ItemListRoot");
            listRoot.transform.SetParent(canvas.transform, false);
            var listRect = listRoot.AddComponent<RectTransform>();
            SetAnchoredRect(listRect, new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.78f));

            var closeBtn = CreateUIButton(canvas.transform, "CloseButton", "Close",
                new Vector2(0.78f, 0.02f), new Vector2(0.98f, 0.1f));
            var refreshBtn = CreateUIButton(canvas.transform, "RefreshButton", "Refresh Shop",
                new Vector2(0.02f, 0.02f), new Vector2(0.35f, 0.1f));

            var ctrlSo = new SerializedObject(ctrl);
            ctrlSo.FindProperty("itemListRoot")?.SetObjectReferenceValue(listRoot.transform);
            ctrlSo.FindProperty("currencyLabel")?.SetObjectReferenceValue(currencyText.GetComponent<Text>());
            ctrlSo.FindProperty("feedbackLabel")?.SetObjectReferenceValue(feedbackText.GetComponent<Text>());
            ctrlSo.FindProperty("closeButton")?.SetObjectReferenceValue(closeBtn.GetComponent<Button>());
            ctrlSo.FindProperty("refreshButton")?.SetObjectReferenceValue(refreshBtn.GetComponent<Button>());
            ctrlSo.ApplyModifiedPropertiesWithoutUndo();

            SaveFixtureScene(scene, ShopScenePath);
        }

        // -- Prefab builders

        private static GameObject CreatePlayerRigPrefabRoot()
        {
            var root = new GameObject("PlayerRig");
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "InteractionMarker";
            marker.transform.SetParent(root.transform, false);
            marker.transform.localPosition = new Vector3(0f, 2f, 0f);
            marker.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            return root;
        }

        private static GameObject CreateEnemyDummyPrefabRoot()
        {
            var root = new GameObject("EnemyDummy");
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

            var weakPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            weakPoint.name = "WeakPoint";
            weakPoint.transform.SetParent(root.transform, false);
            weakPoint.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            weakPoint.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
            return root;
        }

        private static GameObject CreateShopStandPrefabRoot()
        {
            var root = new GameObject("ShopStand");
            var counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "Counter";
            counter.transform.SetParent(root.transform, false);
            counter.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            counter.transform.localScale = new Vector3(3f, 1f, 1.5f);

            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "Sign";
            sign.transform.SetParent(root.transform, false);
            sign.transform.localPosition = new Vector3(0f, 2f, -0.4f);
            sign.transform.localScale = new Vector3(2f, 1f, 0.15f);
            return root;
        }

        private static GameObject CreateTestTargetPrefabRoot()
        {
            var root = new GameObject("AdBeaconTarget");
            var beacon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beacon.name = "BeaconBody";
            beacon.transform.SetParent(root.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            beacon.transform.localScale = new Vector3(0.75f, 3f, 0.75f);

            var trigger = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            trigger.name = "BeaconTrigger";
            trigger.transform.SetParent(root.transform, false);
            trigger.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            trigger.transform.localScale = Vector3.one * 0.5f;
            return root;
        }

        // -- UI helpers

        private static GameObject CreateScreenSpaceCanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private static GameObject CreateUIText(
            Transform parent, string name, string text,
            int fontSize, TextAnchor alignment,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            SetAnchoredRect(rect, anchorMin, anchorMax);
            var label = go.AddComponent<Text>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return go;
        }

        private static GameObject CreateUIButton(
            Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            SetAnchoredRect(rect, anchorMin, anchorMax);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.4f, 0.8f, 0.9f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var textComp = textGo.AddComponent<Text>();
            textComp.text = label;
            textComp.fontSize = 20;
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.white;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return go;
        }

        private static GameObject CreateUISlider(
            Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            SetAnchoredRect(rect, anchorMin, anchorMax);

            // Background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

            // Fill area
            var fillGo = new GameObject("FillArea");
            fillGo.transform.SetParent(go.transform, false);
            var fillAreaRect = fillGo.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = fillAreaRect.offsetMax = Vector2.zero;

            var fillImgGo = new GameObject("Fill");
            fillImgGo.transform.SetParent(fillGo.transform, false);
            var fillImgRect = fillImgGo.AddComponent<RectTransform>();
            fillImgRect.anchorMin = Vector2.zero;
            fillImgRect.anchorMax = Vector2.one;
            fillImgRect.offsetMin = fillImgRect.offsetMax = Vector2.zero;
            var fillImg = fillImgGo.AddComponent<Image>();
            fillImg.color = new Color(0.8f, 0.15f, 0.15f, 1f);

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillImgRect;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            return go;
        }

        private static void SetAnchoredRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void WireEnemyController(GameObject instance, EnemyDefinition def)
        {
            if (instance == null) return;
            var ec = instance.AddComponent<EnemyController>();
            if (def == null) return;
            var so = new SerializedObject(ec);
            so.FindProperty("definition")?.SetObjectReferenceValue(def);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // -- Scene helpers

        private static Scene NewFixtureScene() =>
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        private static void CreateSharedSceneRig()
        {
            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            cam.transform.position = new Vector3(0f, 6f, -10f);
            cam.transform.rotation = Quaternion.Euler(25f, 0f, 0f);
            var c = cam.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.Skybox;
            c.nearClipPlane = 0.1f;
            c.farClipPlane = 100f;

            var light = new GameObject("Directional Light");
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var l = light.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.1f;
        }

        private static void CreateGroundPlane(Vector3 scale)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = scale;
        }

        private static void SaveFixtureScene(Scene scene, string path) =>
            EditorSceneManager.SaveScene(scene, path);

        private static void UpdateBuildSettings()
        {
            var required = new[]
            {
                BootstrapScenePath, MainMenuScenePath, GameplayScenePath, ShopScenePath
            };

            var result = new List<EditorBuildSettingsScene>(required.Length);
            foreach (var p in required)
                result.Add(new EditorBuildSettingsScene(p, true));

            foreach (var existing in EditorBuildSettings.scenes)
                if (!IsRequiredScene(existing.path))
                    result.Add(existing);

            EditorBuildSettings.scenes = result.ToArray();
        }

        private static bool IsRequiredScene(string path) =>
            path == BootstrapScenePath || path == MainMenuScenePath ||
            path == GameplayScenePath || path == ShopScenePath;

        // -- Asset helpers

        private static void CreateOrUpdateAsset<TAsset>(
            string assetPath, System.Action<SerializedObject> initialize)
            where TAsset : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<TAsset>(assetPath);
            if (asset == null)
            {
                var stale = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (stale != null) AssetDatabase.DeleteAsset(assetPath);

                asset = ScriptableObject.CreateInstance<TAsset>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            var so = new SerializedObject(asset);
            initialize(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static SerializedProperty FindProperty(SerializedObject so, string name)
        {
            var prop = so.FindProperty(name);
            if (prop == null)
                throw new System.InvalidOperationException($"Missing serialized field: {name}");
            return prop;
        }

        private static void SetString(SerializedObject so, string name, string val) =>
            FindProperty(so, name).stringValue = val;

        private static void SetInt(SerializedObject so, string name, int val) =>
            FindProperty(so, name).intValue = val;

        private static void SetBool(SerializedObject so, string name, bool val) =>
            FindProperty(so, name).boolValue = val;

        private static void CreateOrUpdatePrefab(string prefabPath, System.Func<GameObject> createRoot)
        {
            var root = createRoot();
            try
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject LoadPrefab(string path) =>
            AssetDatabase.LoadAssetAtPath<GameObject>(path);

        private static GameObject InstantiatePrefab(
            GameObject prefab, string instanceName, Vector3 position)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return null;
            instance.name = instanceName;
            instance.transform.position = position;
            return instance;
        }

        private static AuthoringContext LoadAuthoringContext()
        {
            EnsureBaselineDataAssets();
            return new AuthoringContext
            {
                EconomyConfig = LoadPrimaryAsset<EconomyConfig>(ConfigFolderPath + "/economy.asset"),
                FeatureFlags = LoadPrimaryAsset<FeatureFlags>(ConfigFolderPath + "/feature_flags.asset"),
                StarterItems = LoadAssets<ItemDefinition>(BaselineItemPaths),
                EnemyCatalog = LoadAssets<EnemyDefinition>(BaselineEnemyPaths)
            };
        }

        private static TAsset LoadPrimaryAsset<TAsset>(string preferredPath)
            where TAsset : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<TAsset>(preferredPath);
            if (a != null) return a;

            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(TAsset).Name))
            {
                a = AssetDatabase.LoadAssetAtPath<TAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (a != null) return a;
            }

            return null;
        }

        private static TAsset[] LoadAssets<TAsset>(string[] assetPaths)
            where TAsset : Object
        {
            var list = new List<TAsset>(assetPaths.Length);
            foreach (var p in assetPaths)
            {
                var a = AssetDatabase.LoadAssetAtPath<TAsset>(p);
                if (a != null) list.Add(a);
            }

            return list.ToArray();
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        // -- Inner types

        private sealed class AuthoringContext
        {
            public EconomyConfig EconomyConfig;
            public FeatureFlags FeatureFlags;
            public ItemDefinition[] StarterItems;
            public EnemyDefinition[] EnemyCatalog;
        }
    }

    // Workaround: SetObjectReferenceValue is not a method on SerializedProperty in older
    // Unity versions; this extension encapsulates the standard approach.
    internal static class SerializedPropertyExtensions
    {
        public static void SetObjectReferenceValue(this SerializedProperty prop, Object obj)
        {
            if (prop != null) prop.objectReferenceValue = obj;
        }
    }
}