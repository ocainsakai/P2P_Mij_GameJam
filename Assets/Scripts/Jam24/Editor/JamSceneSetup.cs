#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Jam24.Editor
{
    public static class JamSceneSetup
    {
        private const string InitPath = "Assets/Scenes/Init.unity";
        private const string HomePath = "Assets/Scenes/Home.unity";
        private const string GameplayPath = "Assets/Scenes/Gameplay.unity";
        private const string LoadingPath = "Assets/Scenes/Loading.unity";
        private const string CutscenePath = "Assets/Scenes/Cutscene.unity";
        private const string InitRoot = "Jam24_Managers";
        private const string HomeRoot = "Jam24_HomeUI";
        private const string GameplayRoot = "Jam24_Gameplay";
        private const string LoadingRoot = "Jam24_Loading";
        private const string CutsceneRoot = "Jam24_Cutscene";

        [MenuItem("Jam24/Build Complete Beach Flow Game _F7")]
        public static void SetupScenes()
        {
            SetupArtImport();
            SetupInit();
            SetupLoading();
            SetupCutscene();
            SetupHome();
            SetupGameplay();
            SetupBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("Jam24: Beach Flow game scenes built successfully.");
        }

        private static void SetupArtImport()
        {
            const string path = "Assets/Resources/SoleFlow/soleflow_sheet.png";
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
            if (importer.isReadable && importer.textureCompression == TextureImporterCompression.Uncompressed) return;
            importer.isReadable = true;
            importer.textureType = TextureImporterType.Default;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        [MenuItem("Jam24/Play Gameplay Smoke Test _F8")]
        private static void PlayGameplaySmokeTest()
        {
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            PlayerPrefs.SetInt("jam24.automatedSmoke", 1);
            PlayerPrefs.Save();
            EditorSceneManager.OpenScene(GameplayPath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Jam24/Play Full Intro Smoke Test _F9")]
        private static void PlayFullIntroSmokeTest()
        {
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            PlayerPrefs.DeleteKey(GameFlow.CutsceneSeenKey);
            PlayerPrefs.Save();
            EditorSceneManager.OpenScene(InitPath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static void SetupInit()
        {
            Scene scene = Open(InitPath, out bool close);
            GameObject root = FindRoot(scene, InitRoot);
            if (root == null)
            {
                root = new GameObject(InitRoot);
                SceneManager.MoveGameObjectToScene(root, scene);
            }
            if (root.GetComponent<GameFlow>() == null) root.AddComponent<GameFlow>();
            SaveClose(scene, close);
        }

        private static void SetupHome()
        {
            Scene scene = Open(HomePath, out bool close);
            RemoveRoot(scene, HomeRoot);
            EnsureEventSystem(scene);

            GameObject root = CanvasRoot(HomeRoot, scene);
            GameUI ui = root.AddComponent<GameUI>();
            Image bg = Panel("SummerBeach", root.transform, new Color32(14, 126, 174, 255));
            Stretch(bg.rectTransform);
            AddBackgroundTexture(bg.gameObject);

            Image shade = Panel("OceanShade", bg.transform, new Color(0.02f, .16f, .28f, .42f));
            Stretch(shade.rectTransform);

            Image card = Panel("MenuCard", bg.transform, new Color32(255, 247, 219, 242));
            SetRect(card.rectTransform, new Vector2(.5f, .5f), new Vector2(900, 900), Vector2.zero);

            Text title = Label("Title", card.transform, "SOLE FLOW", 72, FontStyle.Bold, new Color32(10, 103, 145, 255));
            SetRect(title.rectTransform, new Vector2(.5f, 1), new Vector2(800, 100), new Vector2(0, -85));
            Text subtitle = Label("Subtitle", card.transform, "Help Octo guide every lost slipper home.", 25, FontStyle.Italic, new Color32(43, 118, 132, 255));
            SetRect(subtitle.rectTransform, new Vector2(.5f, 1), new Vector2(780, 55), new Vector2(0, -155));

            Image collection = Panel("OctoCollection", bg.transform, new Color32(40, 30, 73, 235));
            SetRect(collection.rectTransform, new Vector2(.5f,.5f), new Vector2(350,720), new Vector2(675,0));
            Text collectionTitle = Label("CollectionTitle", collection.transform, "OCTO'S SOLE SHELF", 29, FontStyle.Bold, new Color32(255,210,104,255));
            SetRect(collectionTitle.rectTransform,new Vector2(.5f,1),new Vector2(310,70),new Vector2(0,-55));
            Text collectionContent = Label("CollectionContent", collection.transform, "No slippers yet.", 20, FontStyle.Normal, Color.white);
            SetRect(collectionContent.rectTransform,new Vector2(.5f,.5f),new Vector2(300,560),new Vector2(0,-35));
            collectionContent.alignment = TextAnchor.UpperLeft;
            var display = collection.gameObject.AddComponent<CollectionDisplay>();
            display.Configure(collectionContent);
            EditorUtility.SetDirty(display);

            GameObject grid = new GameObject("LevelGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(card.transform, false);
            SetRect(grid.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(700, 430), new Vector2(0, 25));
            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(125, 125);
            layout.spacing = new Vector2(18, 18);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;
            layout.childAlignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < SoleLevelCatalog.Count; i++)
            {
                Button button = Button($"Level_{i + 1}", grid.transform, (i + 1).ToString(), new Color32(255, 126, 82, 255), new Vector2(125, 125));
                Text text = button.GetComponentInChildren<Text>();
                var levelButton = button.gameObject.AddComponent<LevelButton>();
                levelButton.Configure(i, text);
                UnityEventTools.AddPersistentListener(button.onClick, levelButton.Play);
            }

            Button play = Button("Continue", card.transform, "CONTINUE", new Color32(0, 176, 190, 255), new Vector2(360, 82));
            SetRect(play.GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(360, 82), new Vector2(0, 115));
            UnityEventTools.AddPersistentListener(play.onClick, ui.Play);

            Button reset = Button("ResetSave", card.transform, "RESET SAVE", new Color32(70, 119, 140, 255), new Vector2(250, 60));
            SetRect(reset.GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(250, 60), new Vector2(145, 40));
            UnityEventTools.AddPersistentListener(reset.onClick, ui.ResetSave);

            Button story = Button("WatchStory", card.transform, "WATCH STORY", new Color32(255, 151, 73, 255), new Vector2(250, 60));
            SetRect(story.GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(250, 60), new Vector2(-145, 40));
            UnityEventTools.AddPersistentListener(story.onClick, ui.ShowCutscene);

            SaveClose(scene, close);
        }

        private static void SetupLoading()
        {
            Scene scene = Open(LoadingPath, out bool close);
            RemoveRoot(scene, LoadingRoot);
            Camera camera = FindCamera(scene);
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(8, 89, 124, 255);
            GameObject root = CanvasRoot(LoadingRoot, scene);
            LoadingController controller = root.AddComponent<LoadingController>();

            Image bg = Panel("BeachBackground", root.transform, new Color32(13, 133, 174, 255));
            Stretch(bg.rectTransform);
            AddBackgroundTexture(bg.gameObject);
            Image shade = Panel("OceanShade", bg.transform, new Color(0f, .12f, .2f, .58f));
            Stretch(shade.rectTransform);

            Text title = Label("Title", bg.transform, "SOLE FLOW", 70, FontStyle.Bold, Color.white);
            SetRect(title.rectTransform, new Vector2(.5f, .5f), new Vector2(850, 100), new Vector2(0, 105));
            Text status = Label("Status", bg.transform, "FOLLOWING THE TIDE...  0%", 23, FontStyle.Bold, new Color32(255, 236, 166, 255));
            SetRect(status.rectTransform, new Vector2(.5f, .5f), new Vector2(700, 55), new Vector2(0, -105));

            Image track = Panel("ProgressTrack", bg.transform, new Color(1f, 1f, 1f, .3f));
            SetRect(track.rectTransform, new Vector2(.5f, .5f), new Vector2(620, 34), new Vector2(0, -35));
            Image fill = Panel("ProgressFill", track.transform, new Color32(255, 126, 82, 255));
            Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            controller.Configure(fill, status);
            EditorUtility.SetDirty(controller);
            SaveClose(scene, close);
        }

        private static void SetupCutscene()
        {
            Scene scene = Open(CutscenePath, out bool close);
            RemoveRoot(scene, CutsceneRoot);
            Camera camera = FindCamera(scene);
            camera.transform.position = new Vector3(0, 0, -10);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(8, 89, 124, 255);
            EnsureEventSystem(scene);
            GameObject root = CanvasRoot(CutsceneRoot, scene);
            CutsceneController controller = root.AddComponent<CutsceneController>();

            Image bg = Panel("BeachBackground", root.transform, new Color32(13, 133, 174, 255));
            Stretch(bg.rectTransform);
            AddBackgroundTexture(bg.gameObject);
            Image shade = Panel("CinematicShade", bg.transform, new Color(0f, .08f, .15f, .46f));
            Stretch(shade.rectTransform);

            Image card = Panel("StoryCard", bg.transform, new Color32(255, 248, 219, 242));
            SetRect(card.rectTransform, new Vector2(.5f, .5f), new Vector2(920, 480), new Vector2(0, -80));
            CanvasGroup group = card.gameObject.AddComponent<CanvasGroup>();
            Text title = Label("StoryTitle", card.transform, "OCTO'S TREASURE", 54, FontStyle.Bold, new Color32(10, 103, 145, 255));
            SetRect(title.rectTransform, new Vector2(.5f, 1), new Vector2(810, 90), new Vector2(0, -85));
            Text story = Label("StoryBody", card.transform, "Deep below the waves, Octo keeps the ocean's strangest slipper collection.", 30, FontStyle.Italic, new Color32(48, 104, 119, 255));
            SetRect(story.rectTransform, new Vector2(.5f, .5f), new Vector2(780, 130), new Vector2(0, 20));
            Text page = Label("Page", card.transform, "1  /  3", 20, FontStyle.Bold, new Color32(255, 126, 82, 255));
            SetRect(page.rectTransform, new Vector2(.5f, 0), new Vector2(220, 45), new Vector2(0, 48));

            Button next = Button("Next", bg.transform, "NEXT", new Color32(255, 126, 82, 255), new Vector2(250, 72));
            SetRect(next.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(250, 72), new Vector2(-165, 70));
            UnityEventTools.AddPersistentListener(next.onClick, controller.Next);
            Button skip = Button("Skip", bg.transform, "SKIP", new Color32(7, 91, 126, 235), new Vector2(180, 60));
            SetRect(skip.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(180, 60), new Vector2(115, -55));
            UnityEventTools.AddPersistentListener(skip.onClick, controller.Skip);

            controller.Configure(title, story, page, group);
            EditorUtility.SetDirty(controller);
            SaveClose(scene, close);
        }

        private static void SetupBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(InitPath, true),
                new EditorBuildSettingsScene(LoadingPath, true),
                new EditorBuildSettingsScene(CutscenePath, true),
                new EditorBuildSettingsScene(HomePath, true),
                new EditorBuildSettingsScene(GameplayPath, true)
            };
        }

        private static void SetupGameplay()
        {
            Scene scene = Open(GameplayPath, out bool close);
            RemoveRoot(scene, GameplayRoot);
            EnsureEventSystem(scene);

            Camera camera = FindCamera(scene);
            camera.orthographic = true;
            camera.orthographicSize = 5.3f;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.backgroundColor = new Color32(25, 173, 200, 255);

            var root = new GameObject(GameplayRoot);
            SceneManager.MoveGameObjectToScene(root, scene);
            var backdrop = new GameObject("BeachBackdrop", typeof(SpriteRenderer), typeof(BeachBackdrop));
            backdrop.transform.SetParent(root.transform, false);
            var boardObject = new GameObject("SoleFlowPuzzle", typeof(SoleFlowPuzzle));
            boardObject.transform.SetParent(root.transform, false);
            SoleFlowPuzzle puzzle = boardObject.GetComponent<SoleFlowPuzzle>();

            GameObject canvasRoot = CanvasRoot("GameplayCanvas", scene);
            canvasRoot.transform.SetParent(root.transform, false);
            GameUI ui = canvasRoot.AddComponent<GameUI>();

            Image top = Panel("TopBar", canvasRoot.transform, new Color32(5, 78, 112, 230));
            top.rectTransform.anchorMin = new Vector2(0, 1);
            top.rectTransform.anchorMax = Vector2.one;
            top.rectTransform.pivot = new Vector2(.5f, 1);
            top.rectTransform.sizeDelta = new Vector2(0, 105);
            top.rectTransform.anchoredPosition = Vector2.zero;
            Text level = Label("Level", top.transform, "LEVEL 1/10", 28, FontStyle.Bold, Color.white);
            SetRect(level.rectTransform, new Vector2(.5f, .5f), new Vector2(600, 70), Vector2.zero);
            Text actions = Label("Actions", top.transform, "ACTIONS  0", 23, FontStyle.Bold, new Color32(255, 221, 128, 255));
            SetRect(actions.rectTransform, new Vector2(0, .5f), new Vector2(260, 70), new Vector2(155, 0));
            Text mode = Label("Mode", top.transform, "CURRENT STARTING", 21, FontStyle.Bold, new Color32(133, 239, 225, 255));
            SetRect(mode.rectTransform, new Vector2(1, .5f), new Vector2(380, 70), new Vector2(-210, 0));

            Image tutorialPanel = Panel("TutorialPanel", canvasRoot.transform, new Color(0.02f,.18f,.25f,.76f));
            SetRect(tutorialPanel.rectTransform, new Vector2(.5f,1), new Vector2(950,70), new Vector2(0,-140));
            Text tutorial = Label("Tutorial", tutorialPanel.transform, "Observe the flow, set up the environment, then RELEASE.", 21, FontStyle.Italic, Color.white);
            Stretch(tutorial.rectTransform);

            Button home = Button("Home", canvasRoot.transform, "HOME", new Color32(8, 98, 132, 245), new Vector2(150, 64));
            SetRect(home.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(150, 64), new Vector2(95, 55));
            UnityEventTools.AddPersistentListener(home.onClick, ui.Home);
            Button undo = Button("Undo", canvasRoot.transform, "UNDO", new Color32(8, 98, 132, 245), new Vector2(150, 64));
            SetRect(undo.GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(150, 64), new Vector2(-100, 55));
            UnityEventTools.AddPersistentListener(undo.onClick, ui.Undo);
            Button reset = Button("Reset", canvasRoot.transform, "RESET", new Color32(255, 112, 81, 245), new Vector2(150, 64));
            SetRect(reset.GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(150, 64), new Vector2(100, 55));
            UnityEventTools.AddPersistentListener(reset.onClick, ui.ResetPuzzle);
            Button pause = Button("Pause", canvasRoot.transform, "PAUSE", new Color32(8, 98, 132, 245), new Vector2(150, 64));
            SetRect(pause.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(150, 64), new Vector2(-95, 55));
            UnityEventTools.AddPersistentListener(pause.onClick, ui.Pause);

            GameObject pausePopup = Popup(canvasRoot.transform, "PausePopup", "PAUSED", "The tide can wait.", out Transform pauseCard);
            Button resume = Button("Resume", pauseCard, "RESUME", new Color32(0, 176, 190, 255), new Vector2(300, 75));
            SetRect(resume.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(300, 75), new Vector2(0, -70));
            UnityEventTools.AddPersistentListener(resume.onClick, ui.Resume);

            GameObject winPopup = Popup(canvasRoot.transform, "WinPopup", "SLIPPER COLLECTED!", "Octo has a new treasure for the collection.", out Transform winCard);
            Text stars = Label("Stars", winCard, "★★★", 44, FontStyle.Bold, new Color32(255,185,60,255));
            SetRect(stars.rectTransform, new Vector2(.5f,.5f), new Vector2(350,65), new Vector2(0,-10));
            Button next = Button("Next", winCard, "NEXT LEVEL", new Color32(255, 126, 82, 255), new Vector2(300, 75));
            SetRect(next.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(300, 75), new Vector2(0, -70));
            UnityEventTools.AddPersistentListener(next.onClick, ui.Next);

            GameObject losePopup = Popup(canvasRoot.transform, "LosePopup", "SOLE STUCK!", "Reset the setup and redirect the current.", out Transform loseCard);
            Button retry = Button("Retry", loseCard, "RETRY", new Color32(255, 126, 82, 255), new Vector2(300, 75));
            SetRect(retry.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(300, 75), new Vector2(0, -70));
            UnityEventTools.AddPersistentListener(retry.onClick, ui.Restart);

            pausePopup.SetActive(false);
            winPopup.SetActive(false);
            losePopup.SetActive(false);
            ui.Configure(puzzle, actions, mode, level, tutorial, stars, pausePopup, winPopup, losePopup);
            EditorUtility.SetDirty(ui);
            SaveClose(scene, close);
        }

        private static GameObject Popup(Transform parent, string name, string title, string subtitle, out Transform cardTransform)
        {
            Image overlay = Panel(name, parent, new Color(0, .08f, .14f, .76f));
            Stretch(overlay.rectTransform);
            Image card = Panel("Card", overlay.transform, new Color32(255, 248, 219, 255));
            SetRect(card.rectTransform, new Vector2(.5f, .5f), new Vector2(600, 380), Vector2.zero);
            Text titleText = Label("Title", card.transform, title, 48, FontStyle.Bold, new Color32(10, 103, 145, 255));
            SetRect(titleText.rectTransform, new Vector2(.5f, .5f), new Vector2(540, 80), new Vector2(0, 95));
            Text sub = Label("Subtitle", card.transform, subtitle, 24, FontStyle.Italic, new Color32(57, 118, 127, 255));
            SetRect(sub.rectTransform, new Vector2(.5f, .5f), new Vector2(520, 55), new Vector2(0, 35));
            cardTransform = card.transform;
            return overlay.gameObject;
        }

        private static GameObject CanvasRoot(string name, Scene scene)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(root, scene);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            return root;
        }

        private static void AddBackgroundTexture(GameObject parent)
        {
            Texture2D texture = Resources.Load<Texture2D>("BeachFlow/beach_background");
            if (texture == null) return;
            var go = new GameObject("BeachArt", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(parent.transform, false);
            var raw = go.GetComponent<RawImage>();
            raw.texture = texture;
            raw.color = new Color(1, 1, 1, .78f);
            Stretch(raw.rectTransform);
            go.transform.SetAsFirstSibling();
        }

        private static void EnsureEventSystem(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.GetComponentInChildren<EventSystem>(true) != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(go, scene);
        }

        private static Camera FindCamera(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Camera camera = root.GetComponentInChildren<Camera>(true);
                if (camera != null) return camera;
            }
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            SceneManager.MoveGameObjectToScene(go, scene);
            return go.GetComponent<Camera>();
        }

        private static Image Panel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text Label(string name, Transform parent, string content, int size, FontStyle style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = size;
            return text;
        }

        private static Button Button(string name, Transform parent, string label, Color color, Vector2 size)
        {
            Image image = Panel(name, parent, color);
            image.rectTransform.sizeDelta = size;
            var button = image.gameObject.AddComponent<Button>();
            Text text = Label("Label", image.transform, label, 26, FontStyle.Bold, Color.white);
            Stretch(text.rectTransform);
            return button;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Scene Open(string path, out bool close)
        {
            Scene loaded = SceneManager.GetSceneByPath(path);
            if (loaded.IsValid() && loaded.isLoaded) { close = false; return loaded; }
            close = true;
            if (File.Exists(path)) return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Scene created = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SaveScene(created, path);
            return created;
        }

        private static void RemoveRoot(Scene scene, string name)
        {
            GameObject root = FindRoot(scene, name);
            if (root != null) Object.DestroyImmediate(root);
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
            return null;
        }

        private static void SaveClose(Scene scene, bool close)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (close) EditorSceneManager.CloseScene(scene, true);
        }
    }
}
#endif
