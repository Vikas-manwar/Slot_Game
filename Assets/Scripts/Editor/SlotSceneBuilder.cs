using System.IO;
using System.Linq;
using SlotGame.Audio;
using SlotGame.Core;
using SlotGame.Data;
using SlotGame.Game;
using SlotGame.Reels;
using SlotGame.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static SlotGame.EditorTools.SerializedWiring;
using static SlotGame.EditorTools.UIFactory;

namespace SlotGame.EditorTools
{
    internal static class SlotSceneBuilder
    {
        private const float CabinetScale = 1.68f;

        private const float ReelX = 229f, ReelY = 246f, ReelWidth = 108f, ReelHeight = 210f, ReelPitch = 130f;
        private const float SymbolSpacing = 70f;
        private const float SymbolSize = 64f;

        private sealed class PrefabSet
        {
            public GameObject SymbolView, Reel, Cabinet, ControlPanel, PaytableRow, PaytablePanel, ConfirmDialog;
        }

        private sealed class DataSet
        {
            public SlotConfig Config;
            public ReelAnimationProfile Profile;
        }

        [MenuItem(EditorPaths.MenuRoot + "Build Scene and Assets", priority = 0)]
        public static void BuildFromMenu()
        {
            if (File.Exists(EditorPaths.Scene) &&
                !EditorUtility.DisplayDialog("Rebuild slot game?",
                    "This regenerates the scene, prefabs and animations in place. Continue?", "Rebuild", "Cancel"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildAll();
            EditorSceneManager.OpenScene(EditorPaths.Scene);
            EditorUtility.DisplayDialog("Slot game ready",
                "Scene generated at " + EditorPaths.Scene + ".\nPress Play to try it, or use Slot Game > Build WebGL.", "OK");
        }

        public static void BuildAll()
        {
            try
            {
                Progress("Configuring sprites", 0.1f);
                SpriteImportConfigurator.ConfigureAll();
                UIFactory.Font = AssetDatabase.LoadAssetAtPath<Font>(EditorPaths.Font)
                       ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                Progress("Creating data assets", 0.25f);
                var data = CreateDataAssets();

                Progress("Building animations", 0.4f);
                var animations = AnimationAssetBuilder.BuildAll();

                Progress("Building prefabs", 0.6f);
                var prefabs = CreatePrefabs(data, animations);

                Progress("Building scene", 0.85f);
                CreateScene(data, prefabs);

                WebGLBuilder.ApplyPlayerSettings();
                AssetDatabase.SaveAssets();
                Debug.Log("[Slot Game] Scene and assets generated.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void Progress(string step, float t) =>
            EditorUtility.DisplayProgressBar("Slot Game setup", step, t);

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) Debug.LogWarning($"[Slot Game] Sprite not found: {path}");
            return sprite;
        }

        private static Sprite ButtonSprite(string name) => LoadSprite($"{EditorPaths.Buttons}/{name}.png");


        private static DataSet CreateDataAssets()
        {
            Directory.CreateDirectory(EditorPaths.SymbolData);

            var definitions = new (int id, string name, string file, SymbolKind kind, int pay, int weight, string note)[]
            {
                (0, "Cherry", "Symbol_Cherry", SymbolKind.Regular, 3, 10, ""),
                (1, "Bell", "Symbol_Bell", SymbolKind.Regular, 6, 8, ""),
                (2, "Bar", "Symbol_Bar", SymbolKind.Regular, 12, 6, ""),
                (3, "Seven", "Symbol_Seven", SymbolKind.Regular, 35, 4, ""),
                (4, "Wild", "Symbol_Wild", SymbolKind.Wild, 100, 1, "Substitutes for any symbol except Scatter and doubles the win."),
                (5, "Scatter", "Symbol_Scatter", SymbolKind.Scatter, 0, 2, "3 anywhere trigger free spins."),
            };

            var symbols = definitions.Select(d =>
            {
                var asset = LoadOrCreate<SymbolData>($"{EditorPaths.SymbolData}/Symbol_{d.name}.asset");
                asset.EditorInitialize(d.id, d.name, LoadSprite($"{EditorPaths.Symbols}/{d.file}.png"),
                    d.kind, d.pay, d.weight, d.note);
                EditorUtility.SetDirty(asset);
                return asset;
            }).ToArray();

            var config = LoadOrCreate<SlotConfig>(EditorPaths.SlotConfig);
            config.EditorInitialize(symbols);
            EditorUtility.SetDirty(config);

            var profile = LoadOrCreate<ReelAnimationProfile>(EditorPaths.ReelProfile);
            AssetDatabase.SaveAssets();
            return new DataSet { Config = config, Profile = profile };
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }


        private static PrefabSet CreatePrefabs(DataSet data, AnimationAssetBuilder.Result animations)
        {
            Directory.CreateDirectory(EditorPaths.Prefabs);
            var set = new PrefabSet();
            set.SymbolView = SavePrefab(BuildSymbolView(animations.Symbol), "SymbolView");
            set.Reel = SavePrefab(BuildReel(set.SymbolView.GetComponent<SymbolView>(), data.Profile), "Reel");
            set.Cabinet = SavePrefab(BuildCabinet(set.Reel, animations), "SlotMachine");
            set.ControlPanel = SavePrefab(BuildControlPanel(), "ControlPanel");
            set.PaytableRow = SavePrefab(BuildPaytableRow(), "PaytableRow");
            set.PaytablePanel = SavePrefab(BuildPaytablePanel(set.PaytableRow.GetComponent<PaytableRowView>(), animations.Popup), "PaytablePanel");
            set.ConfirmDialog = SavePrefab(BuildConfirmDialog(animations.Popup), "ConfirmDialog");
            return set;
        }

        private static GameObject SavePrefab(RectTransform root, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, $"{EditorPaths.Prefabs}/{name}.prefab");
            Object.DestroyImmediate(root.gameObject);
            return prefab;
        }

        private static RectTransform BuildSymbolView(AnimatorController controller)
        {
            var root = Place(Create("SymbolView", null), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(SymbolSize, SymbolSize));
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var animator = root.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            var content = Stretch(Create("Content", root));
            var icon = AddImage(Stretch(Create("Icon", content)), LoadSprite($"{EditorPaths.Symbols}/Symbol_Cherry.png"));
            icon.preserveAspect = true;

            var frame = Stretch(Create("WinFrame", content));
            frame.offsetMin = new Vector2(-4f, -4f);
            frame.offsetMax = new Vector2(4f, 4f);
            AddImage(frame, LoadSprite($"{EditorPaths.Sprites}/Win_Frame.png"));
            frame.gameObject.SetActive(false);

            var view = root.gameObject.AddComponent<SymbolView>();
            Wire(view, ("content", content), ("iconImage", icon), ("canvasGroup", group), ("animator", animator));
            return root;
        }

        private static RectTransform BuildReel(SymbolView symbolPrefab, ReelAnimationProfile profile)
        {
            var root = Place(Create("Reel", null), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(ReelWidth, ReelHeight));
            AddImage(Stretch(Create("Background", root)), LoadSprite($"{EditorPaths.Machine}/Reel_Background.png"));

            var viewport = Stretch(Create("Viewport", root));
            viewport.gameObject.AddComponent<RectMask2D>();
            var symbols = Place(Create("Symbols", viewport), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var reel = root.gameObject.AddComponent<ReelController>();
            Wire(reel,
                ("symbolContainer", symbols),
                ("symbolPrefab", symbolPrefab),
                ("profile", profile),
                ("symbolSpacing", SymbolSpacing),
                ("symbolSize", new Vector2(SymbolSize, SymbolSize)));
            return root;
        }

        private static RectTransform BuildCabinet(GameObject reelPrefab, AnimationAssetBuilder.Result animations)
        {
            var root = Create("SlotMachine", null);
            root.sizeDelta = new Vector2(816f, 624f);

            var reels = Stretch(Create("Reels", root));
            for (int i = 0; i < 3; i++)
            {
                var reel = (GameObject)PrefabUtility.InstantiatePrefab(reelPrefab);
                reel.name = $"Reel_{i}";
                var rt = (RectTransform)reel.transform;
                rt.SetParent(reels, false);
                PlacePixels(rt, ReelX + ReelPitch * i, ReelY, ReelWidth, ReelHeight);
            }

            var line = PlacePixels(Create("PaylineLine", root), ReelX, ReelY + ReelHeight / 2f - 2f, ReelPitch * 2f + ReelWidth, 4f);
            AddImage(line, LoadSprite($"{EditorPaths.Sprites}/White.png")).color = new Color(1f, 0.3f, 0.1f, 0.45f);

            BuildLever(root, animations.Lever);

            AddImage(PlacePixels(Create("Frame", root), 0f, 0f, 816f, 624f), LoadSprite($"{EditorPaths.Machine}/Machine_Frame.png"));

            var marker = LoadSprite($"{EditorPaths.Sprites}/Payline_Marker.png");
            AddImage(PlacePixelsCentered(Create("PaylineMarker_Left", root), 207f, 341f, 16f, 20f), marker);
            var right = PlacePixelsCentered(Create("PaylineMarker_Right", root), 603f, 341f, 16f, 20f);
            right.localScale = new Vector3(-1f, 1f, 1f);
            AddImage(right, marker);

            AddText(PlacePixels(Create("MessageText", root), 214f, 572f, 388f, 46f), "PULL THE LEVER OR PRESS SPIN", 26, Color.white);

            BuildBanner(root, animations.Banner);
            return root;
        }

        private static void BuildLever(RectTransform cabinet, AnimatorController controller)
        {
            var lever = PlacePixels(Create("Lever", cabinet), 673f, 296f, 92f, 328f);
            var animator = lever.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            var view = lever.gameObject.AddComponent<LeverView>();
            Wire(view, ("animator", animator));

            var up = PlacePixels(Create("LeverUp", lever), 0f, 0f, 92f, 270f);
            AddImage(up, LoadSprite($"{EditorPaths.Machine}/Lever_Up.png"), raycast: true);
            up.pivot = new Vector2(0.5f, 0f);
            up.anchoredPosition = new Vector2(46f, -270f);

            var down = PlacePixels(Create("LeverDown", lever), 0f, 268f, 92f, 60f);
            AddImage(down, LoadSprite($"{EditorPaths.Machine}/Lever_Down.png"), raycast: true);
            down.gameObject.SetActive(false);
        }

        private static void BuildBanner(RectTransform cabinet, AnimatorController controller)
        {
            var banner = PlacePixelsCentered(Create("WinBanner", cabinet), 203f, 266f, 420f, 170f);
            var group = banner.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            banner.localScale = new Vector3(0.3f, 0.3f, 1f);
            var animator = banner.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            AddImage(Stretch(Create("Background", banner)), LoadSprite($"{EditorPaths.Panels}/Panel_Orange.png"), sliced: true);

            var title = Create("Title", banner);
            title.anchorMin = new Vector2(0f, 0.45f);
            title.anchorMax = Vector2.one;
            title.offsetMin = new Vector2(16f, 0f);
            title.offsetMax = new Vector2(-16f, -10f);
            var titleText = AddText(title, "BIG WIN!", 64, Gold);

            var amount = Create("Amount", banner);
            amount.anchorMin = Vector2.zero;
            amount.anchorMax = new Vector2(1f, 0.5f);
            amount.offsetMin = new Vector2(16f, 18f);
            amount.offsetMax = new Vector2(-16f, 0f);
            var amountText = AddText(amount, "0", 52, Color.white);

            var view = banner.gameObject.AddComponent<WinBanner>();
            Wire(view, ("animator", animator), ("titleText", titleText), ("amountText", amountText));
        }

        private static RectTransform PlacePixelsCentered(RectTransform rt, float x, float y, float width, float height)
        {
            PlacePixels(rt, x, y, width, height);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x + width / 2f, -(y + height / 2f));
            return rt;
        }

        private static RectTransform BuildControlPanel()
        {
            var root = Place(Create("ControlPanel", null), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 900f));
            AddImage(root, LoadSprite($"{EditorPaths.Panels}/Panel_Blue.png"), sliced: true);
            var orange = LoadSprite($"{EditorPaths.Panels}/Panel_Orange.png");
            var top = new Vector2(0.5f, 1f);

            AddText(Place(Create("Title", root), top, new Vector2(0f, -70f), new Vector2(520f, 90f)), "LUCKY SEVENS", 76, Gold);

            Text Readout(string label, float y, float width, out RectTransform box)
            {
                AddText(Place(Create(label + "Label", root), top, new Vector2(0f, y), new Vector2(480f, 40f)), label.ToUpperInvariant(), 32, Ice);
                box = Place(Create(label + "Box", root), top, new Vector2(0f, y - 62f), new Vector2(width, 84f));
                AddImage(box, orange, sliced: true);
                return AddLabel(box, "Value", "0", 54, Color.white, 10f);
            }

            var balanceText = Readout("Credits", -150f, 480f, out _);
            var winText = Readout("Win", -300f, 480f, out _);
            var betText = Readout("Bet", -450f, 260f, out _);

            var betDown = AddSpriteSwapButton(Place(Create("BetDownButton", root), top, new Vector2(-190f, -512f), new Vector2(84f, 86f)),
                ButtonSprite("ArrowLeft_Normal"), ButtonSprite("ArrowLeft_Highlighted"), ButtonSprite("ArrowLeft_Pressed"), ButtonSprite("ArrowLeft_Disabled"));
            var betUp = AddSpriteSwapButton(Place(Create("BetUpButton", root), top, new Vector2(190f, -512f), new Vector2(84f, 86f)),
                ButtonSprite("ArrowRight_Normal"), ButtonSprite("ArrowRight_Highlighted"), ButtonSprite("ArrowRight_Pressed"), ButtonSprite("ArrowRight_Disabled"));

            var badge = Place(Create("FreeSpinsBadge", root), top, new Vector2(0f, -596f), new Vector2(480f, 58f));
            AddImage(badge, orange, sliced: true).color = new Color(1f, 0.85f, 0.4f, 1f);
            var freeSpinsText = AddLabel(badge, "Value", "FREE SPINS: 0", 40, Gold, 6f);
            badge.gameObject.SetActive(false);

            var spin = Place(Create("SpinButton", root), top, new Vector2(0f, -700f), new Vector2(440f, 130f));
            var spinButton = AddTintButton(spin, orange);
            var spinLabel = AddLabel(spin, "Label", "SPIN", 84, Color.white, 12f);

            var paytable = Place(Create("PaytableButton", root), top, new Vector2(-122f, -808f), new Vector2(226f, 70f));
            var paytableButton = AddTintButton(paytable, orange);
            AddLabel(paytable, "Label", "PAYTABLE", 36, Color.white, 8f);

            var mute = Place(Create("MuteButton", root), top, new Vector2(122f, -808f), new Vector2(226f, 70f));
            var muteButton = AddTintButton(mute, orange);
            var muteLabel = AddLabel(mute, "Label", "SOUND: ON", 36, Color.white, 8f);

            var hud = root.gameObject.AddComponent<SlotHUD>();
            Wire(hud,
                ("balanceText", balanceText), ("betText", betText), ("winText", winText),
                ("freeSpinsBadge", badge.gameObject), ("freeSpinsText", freeSpinsText),
                ("spinButton", spinButton), ("spinButtonLabel", spinLabel),
                ("betDownButton", betDown), ("betUpButton", betUp),
                ("paytableButton", paytableButton), ("muteButton", muteButton), ("muteButtonLabel", muteLabel));
            return root;
        }

        private static RectTransform BuildPaytableRow()
        {
            var root = Place(Create("PaytableRow", null), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 80f));
            AddImage(root, LoadSprite($"{EditorPaths.Sprites}/White.png")).color = new Color(1f, 1f, 1f, 0.07f);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 80f;

            var icon = AddImage(Place(Create("Icon", root), new Vector2(0f, 0.5f), new Vector2(54f, 0f), new Vector2(70f, 70f)), null);
            icon.preserveAspect = true;
            var name = AddText(Place(Create("Name", root), new Vector2(0f, 0.5f), new Vector2(240f, 0f), new Vector2(260f, 70f)),
                "SYMBOL", 44, Color.white, TextAnchor.MiddleLeft);
            var payout = AddText(Place(Create("Payout", root), new Vector2(1f, 0.5f), new Vector2(-270f, 0f), new Vector2(500f, 70f)),
                "3 = x0", 36, Gold, TextAnchor.MiddleRight);

            var view = root.gameObject.AddComponent<PaytableRowView>();
            Wire(view, ("iconImage", icon), ("nameText", name), ("payoutText", payout));
            return root;
        }

        private static RectTransform BuildPopupShell(string name, Vector2 windowSize, AnimatorController controller,
            out RectTransform window, out Animator animator)
        {
            var root = Create(name, null);
            Stretch(root);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            animator = root.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            AddImage(Stretch(Create("Dimmer", root)), LoadSprite($"{EditorPaths.Sprites}/White.png"), raycast: true)
                .color = new Color(0.02f, 0f, 0.08f, 0.7f);

            window = Place(Create("Window", root), new Vector2(0.5f, 0.5f), Vector2.zero, windowSize);
            AddImage(window, LoadSprite($"{EditorPaths.Panels}/Panel_Blue.png"), sliced: true, raycast: true);
            return root;
        }

        private static RectTransform BuildPaytablePanel(PaytableRowView rowPrefab, AnimatorController popup)
        {
            var root = BuildPopupShell("PaytablePanel", new Vector2(1000f, 840f), popup, out var window, out var animator);
            var top = new Vector2(0.5f, 1f);

            AddText(Place(Create("Title", window), top, new Vector2(0f, -72f), new Vector2(700f, 96f)), "PAYTABLE", 76, Gold);

            var close = AddSpriteSwapButton(Place(Create("CloseButton", window), Vector2.one, new Vector2(-72f, -72f), new Vector2(92f, 92f)),
                ButtonSprite("Close_Normal"), ButtonSprite("Close_Highlighted"), ButtonSprite("Close_Pressed"), ButtonSprite("Close_Disabled"));

            var rows = Create("Rows", window);
            rows.anchorMin = rows.anchorMax = top;
            rows.pivot = top;
            rows.anchoredPosition = new Vector2(0f, -140f);
            rows.sizeDelta = new Vector2(860f, 530f);
            var layout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var rules = AddText(Place(Create("Rules", window), new Vector2(0.5f, 0f), new Vector2(0f, 105f), new Vector2(880f, 110f)),
                "Rules", 30, Ice);
            rules.resizeTextMinSize = 18;

            var panel = root.gameObject.AddComponent<PaytablePanel>();
            Wire(panel, ("animator", animator), ("rowContainer", rows), ("rowPrefab", rowPrefab),
                ("rulesText", rules), ("closeButton", close));
            return root;
        }

        private static RectTransform BuildConfirmDialog(AnimatorController popup)
        {
            var root = BuildPopupShell("ConfirmDialog", new Vector2(880f, 520f), popup, out var window, out var animator);
            var top = new Vector2(0.5f, 1f);
            var bottom = new Vector2(0.5f, 0f);

            var title = AddText(Place(Create("Title", window), top, new Vector2(0f, -80f), new Vector2(760f, 90f)), "TITLE", 68, Gold);
            var message = AddText(Place(Create("Message", window), top, new Vector2(0f, -205f), new Vector2(740f, 140f)), "Message", 40, Color.white);

            var yes = AddSpriteSwapButton(Place(Create("YesButton", window), bottom, new Vector2(-195f, 118f), new Vector2(300f, 178f)),
                ButtonSprite("Yes_Normal"), ButtonSprite("Yes_Highlighted"), ButtonSprite("Yes_Pressed"), null);
            var no = AddSpriteSwapButton(Place(Create("NoButton", window), bottom, new Vector2(195f, 118f), new Vector2(300f, 178f)),
                ButtonSprite("No_Normal"), ButtonSprite("No_Highlighted"), ButtonSprite("No_Pressed"), null);

            var dialog = root.gameObject.AddComponent<ConfirmDialog>();
            Wire(dialog, ("animator", animator), ("titleText", title), ("messageText", message),
                ("yesButton", yes), ("noButton", no));
            return root;
        }


        private static void CreateScene(DataSet data, PrefabSet prefabs)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(EditorPaths.Scene));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(12, 6, 40, 255);

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("GameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = 5;
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var canvas = (RectTransform)canvasGo.transform;

            var background = Stretch(Create("Background", canvas));
            AddImage(background, LoadSprite($"{EditorPaths.Machine}/Background.png"));
            var fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;

            var cabinet = Instantiate(prefabs.Cabinet, canvas);
            cabinet.anchorMin = cabinet.anchorMax = new Vector2(0.5f, 0f);
            cabinet.pivot = Vector2.zero;
            cabinet.sizeDelta = new Vector2(816f, 624f);
            cabinet.localScale = new Vector3(CabinetScale, CabinetScale, 1f);
            cabinet.anchoredPosition = new Vector2(-900f - 137f * CabinetScale, -8f);

            var panel = Instantiate(prefabs.ControlPanel, canvas);
            Place(panel, new Vector2(0.5f, 0.5f), new Vector2(560f, 10f), new Vector2(600f, 900f));

            var paytable = Stretch(Instantiate(prefabs.PaytablePanel, canvas));
            var dialog = Stretch(Instantiate(prefabs.ConfirmDialog, canvas));

            var systems = new GameObject("SlotMachineSystems");
            var audioManager = BuildAudio(systems);
            var presenter = systems.AddComponent<WinPresenter>();
            var controller = systems.AddComponent<SlotMachineController>();

            var hud = panel.GetComponent<SlotHUD>();
            Wire(hud, ("messageText", cabinet.Find("MessageText").GetComponent<Text>()));

            var reels = cabinet.GetComponentsInChildren<ReelController>(true).OrderBy(r => r.name).ToArray();

            Wire(presenter,
                ("hud", hud),
                ("banner", cabinet.GetComponentInChildren<WinBanner>(true)),
                ("audioManager", audioManager));

            Wire(controller,
                ("config", data.Config),
                ("animationProfile", data.Profile),
                ("reels", reels.Cast<Object>().ToArray()),
                ("hud", hud),
                ("winPresenter", presenter),
                ("lever", cabinet.GetComponentInChildren<LeverView>(true)),
                ("audioManager", audioManager),
                ("confirmDialog", dialog.GetComponent<ConfirmDialog>()),
                ("paytablePanel", paytable.GetComponent<PaytablePanel>()));

            EditorSceneManager.SaveScene(scene, EditorPaths.Scene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(EditorPaths.Scene, true) };
        }

        private static RectTransform Instantiate(GameObject prefab, Transform parent)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            return (RectTransform)instance.transform;
        }

        private static AudioManager BuildAudio(GameObject host)
        {
            var oneShot = host.AddComponent<AudioSource>();
            oneShot.playOnAwake = false;

            var accent = host.AddComponent<AudioSource>();
            accent.playOnAwake = false;

            var loop = host.AddComponent<AudioSource>();
            loop.playOnAwake = false;
            loop.loop = true;
            loop.volume = 0.45f;

            var manager = host.AddComponent<AudioManager>();
            AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{EditorPaths.Sounds}/{name}.wav");

            var entries = new (SoundEffect effect, string clip, float volume)[]
            {
                (SoundEffect.ButtonClick, "Button_Click", 0.6f),
                (SoundEffect.LeverPull, "Lever_Pull", 0.9f),
                (SoundEffect.ReelStop, "Reel_Stop", 0.8f),
                (SoundEffect.CoinTick, "Coin_Tick", 0.35f),
                (SoundEffect.WinSmall, "Win_Small", 0.8f),
                (SoundEffect.WinBig, "Win_Big", 0.9f),
                (SoundEffect.FreeSpins, "Free_Spins", 0.9f),
            };

            var so = new SerializedObject(manager);
            so.FindProperty("oneShotSource").objectReferenceValue = oneShot;
            so.FindProperty("accentSource").objectReferenceValue = accent;
            so.FindProperty("loopSource").objectReferenceValue = loop;
            so.FindProperty("spinLoopClip").objectReferenceValue = Clip("Reel_Spin_Loop");
            var sounds = so.FindProperty("sounds");
            sounds.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                var element = sounds.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("effect").enumValueIndex = (int)entries[i].effect;
                element.FindPropertyRelative("clip").objectReferenceValue = Clip(entries[i].clip);
                element.FindPropertyRelative("volume").floatValue = entries[i].volume;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return manager;
        }
    }
}
