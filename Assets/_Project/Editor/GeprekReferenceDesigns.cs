using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Geprek.Core;
using Geprek.Customers;
using Geprek.Data;
using Geprek.Player;
using Geprek.Progression;
using Geprek.Stations;
using Geprek.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Geprek.EditorTools
{
    public sealed class ReferenceArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(GeprekReferenceDesigns.Art)) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Sprite;
            if (!assetPath.EndsWith("characters.png") && !assetPath.EndsWith("portraits.png")) t.spriteImportMode = SpriteImportMode.Single;
            t.spritePixelsPerUnit = assetPath.EndsWith("characters.png") ? 145 : 100;
            t.filterMode = FilterMode.Bilinear;
            t.mipmapEnabled = false;
            t.alphaIsTransparency = true;
            t.maxTextureSize = 2048;
            t.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }

    /// <summary>Editable gameplay geometry aligned to the supplied reference paintings.</summary>
    public static class GeprekReferenceDesigns
    {
        public const string Art = "Assets/_Project/Resources/Art/ReferenceWorld/";
        const string SkinDir = "Assets/_Project/Data/Skins/Illustrated";
        static readonly string[] Names = { "Mahasiswa", "Pelanggan", "Mahasiswi", "Pegawai", "JuruMasak", "Pramusaji", "Kasir", "Ibu" };
        public static Vector3 Pixel(float x, float y) => new((x - 836.5f) / 100f, (470f - y) / 100f, 0);

        [MenuItem("Geprek/5. Pasang Desain Referensi & Karakter")]
        public static void Install() => ApplyToOpenScene(true);

        public static void ApplyToOpenScene(bool save)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Keluar dari Play Mode dulu.");
            if (!File.Exists(Art + "warung.png")) return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var locations = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "Locations");
            if (locations == null) throw new InvalidOperationException("Buka Main.unity terlebih dahulu.");
            if (save)
            {
                Directory.CreateDirectory("ArtSource/Backups");
                string backup = "ArtSource/Backups/Main-before-reference-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity";
                EditorSceneManager.SaveScene(scene, backup, true);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (string file in new[] { "warung", "ruko", "restaurant", "campus", "home" })
                AssetDatabase.ImportAsset(Art + file + ".png", ImportAssetOptions.ForceUpdate);
            var skins = ImportCharacters();
            AssignSkins(skins);
            ImportPortraits();
            Undo.RegisterFullObjectHierarchyUndo(locations, "Pasang desain referensi GEPREK");
            ApplyLocation(locations.transform.Find("Warung"), "warung", 1);
            ApplyLocation(locations.transform.Find("Ruko"), "ruko", 2);
            ApplyLocation(locations.transform.Find("Restaurant"), "restaurant", 3);
            foreach (var manager in UnityEngine.Object.FindObjectsByType<LocationManager>(FindObjectsInactive.Include))
            {
                var so = new SerializedObject(manager);
                var entries = so.FindProperty("locations");
                for (int i = 0; i < entries.arraySize; i++)
                {
                    var e = entries.GetArrayElementAtIndex(i);
                    if (e.FindPropertyRelative("id").enumValueIndex == (int)LocationId.Home) continue;
                    e.FindPropertyRelative("cameraMin").vector2Value = new(-8.365f, -4.7f);
                    e.FindPropertyRelative("cameraMax").vector2Value = new(8.365f, 4.7f);
                    e.FindPropertyRelative("cameraSize").floatValue = 5.15f;
                    var spawn = e.FindPropertyRelative("playerSpawn").objectReferenceValue as Transform;
                    if (spawn != null) spawn.localPosition = Pixel(340, 570);
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var player = GameObject.Find("Player");
            if (player != null)
            {
                player.transform.position = Pixel(340, 570);
                Set(player.GetComponentInChildren<CharacterAnimator>(), "skin", skins[0]);
                player.transform.Find("Sprite").GetComponent<SpriteRenderer>().sprite = skins[0].GetIdle(Facing.Down);
            }
            if (Camera.main != null)
            {
                Camera.main.orthographicSize = 5.15f;
                Camera.main.transform.position = new(0, 0, -10);
                var follow = Camera.main.GetComponent<CameraFollow>();
                follow.SetBounds(new(-8.365f, -4.7f), new(8.365f, 4.7f));
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (save) EditorSceneManager.SaveScene(scene);
            Debug.Log("GEPREK: tiga lokasi referensi, 48 pose karakter, dan titik interaksi siap.");
        }

        static CharacterSkin[] ImportCharacters()
        {
            string path = Art + "characters.png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 145f;
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var old = provider.GetSpriteRects().ToDictionary(s => s.name, s => s.spriteID);
            var source = new Texture2D(2, 2); source.LoadImage(File.ReadAllBytes(path));
            var pixels = source.GetPixels32();
            int w = source.width, h = source.height;
            var rects = new List<SpriteRect>();
            var pairs = new List<SpriteNameFileIdPair>();
            for (int row = 0; row < 8; row++) for (int col = 0; col < 6; col++)
            {
                int x0 = col * w / 6, x1 = (col + 1) * w / 6;
                int y0 = h - (row + 1) * h / 8, y1 = h - row * h / 8;
                var trimmed = TrimCharacter(pixels, w, x0, x1, y0, y1);
                string name = Names[row] + "_" + col;
                var id = old.TryGetValue(name, out var known) ? known : GUID.Generate();
                rects.Add(new SpriteRect { name = name, spriteID = id,
                    rect = trimmed,
                    alignment = SpriteAlignment.Custom, pivot = col >= 4 ? new(.32f,.16f) : new(.5f,.035f) });
                pairs.Add(new SpriteNameFileIdPair(name, id));
            }
            UnityEngine.Object.DestroyImmediate(source);
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
            provider.Apply(); importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
            GeprekBuilder.EnsureFolder(SkinDir);
            var skins = new CharacterSkin[8];
            for (int r = 0; r < 8; r++)
            {
                var s = GeprekBuilder.Asset<CharacterSkin>(SkinDir, "Skin_" + Names[r]);
                s.displayName = Names[r]; s.framesPerDirection = 1; s.illustrated = true;
                s.frames = new[] { sprites[Names[r]+"_0"], sprites[Names[r]+"_1"], sprites[Names[r]+"_2"], sprites[Names[r]+"_3"] };
                s.seatedRight = sprites[Names[r]+"_4"]; s.orderingRight = sprites[Names[r]+"_5"];
                EditorUtility.SetDirty(s); skins[r] = s;
            }
            return skins;
        }

        // Atlas rows may contain a detached shoe pixel from the preceding row.
        // Keep the main connected silhouette instead of extending the crop to that fragment.
        static Rect TrimCharacter(Color32[] pixels, int textureWidth, int x0, int x1, int y0, int y1)
        {
            int width = x1-x0, height = y1-y0, largest = 0;
            var visited = new bool[width*height];
            var queue = new Queue<int>();
            Rect best = default;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                int start = y*width+x;
                if (visited[start] || pixels[(y+y0)*textureWidth+x+x0].a < 140) continue;
                int count = 0, minX = x, maxX = x, minY = y, maxY = y;
                visited[start] = true; queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    int point = queue.Dequeue(), px = point%width, py = point/width;
                    count++; minX = Math.Min(minX,px); maxX = Math.Max(maxX,px); minY = Math.Min(minY,py); maxY = Math.Max(maxY,py);
                    Visit(px-1,py); Visit(px+1,py); Visit(px,py-1); Visit(px,py+1);
                }
                if (count <= largest) continue;
                largest = count;
                minX = Math.Max(0,minX-1); minY = Math.Max(0,minY-1);
                maxX = Math.Min(width-1,maxX+1); maxY = Math.Min(height-1,maxY+1);
                best = new(x0+minX,y0+minY,maxX-minX+1,maxY-minY+1);
            }
            if (largest == 0) throw new InvalidOperationException("Atlas character cell is empty.");
            return best;

            void Visit(int x,int y)
            {
                if (x<0 || x>=width || y<0 || y>=height) return;
                int index = y*width+x;
                if (visited[index]) return;
                visited[index] = true;
                if (pixels[(y+y0)*textureWidth+x+x0].a >= 140) queue.Enqueue(index);
            }
        }

        static void AssignSkins(CharacterSkin[] skins)
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/_Project/Data/GameDatabase.asset");
            db.playerSkin = skins[0]; db.motherSkin = skins[7];
            db.staffSkins = new() { skins[4], skins[5], skins[6] };
            for (int i = 0; i < db.customerTypes.Count; i++)
            { db.customerTypes[i].skin = skins[1 + i % 3]; EditorUtility.SetDirty(db.customerTypes[i]); }
            EditorUtility.SetDirty(db);
            foreach (string name in new[] { "Player", "Staff", "Customer" })
            {
                string path = "Assets/_Project/Prefabs/Characters/" + name + ".prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var skin = skins[name == "Player" ? 0 : name == "Staff" ? 4 : 1];
                    var anim = root.GetComponentInChildren<CharacterAnimator>();
                    Set(anim, "skin", skin);
                    anim.GetComponent<SpriteRenderer>().sprite = skin.GetIdle(Facing.Down);
                    var bubble = root.transform.Find("Bubble");
                    if (bubble != null) { bubble.localPosition = new(0, 1.1f, 0); bubble.localScale = Vector3.one * .8f; }
                    var shadow = root.transform.Find("Shadow");
                    if (shadow != null) shadow.localScale = new(.65f, .5f, 1);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }

        static void ImportPortraits()
        {
            string path = Art + "portraits.png";
            if (!File.Exists(path)) return;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.spriteImportMode = SpriteImportMode.Multiple;
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var old = provider.GetSpriteRects().ToDictionary(s => s.name, s => s.spriteID);
            var source = new Texture2D(2, 2); source.LoadImage(File.ReadAllBytes(path));
            int w = source.width, h = source.height;
            var pixels = source.GetPixels32();
            string[] names = { "Kamu", "Ibu", "Ayah", "Dosen" };
            var rects = new List<SpriteRect>();
            var pairs = new List<SpriteNameFileIdPair>();
            for (int i = 0; i < 4; i++)
            {
                int x0 = i % 2 * w / 2, x1 = (i % 2 + 1) * w / 2;
                int y0 = h - (i / 2 + 1) * h / 2, y1 = h - i / 2 * h / 2;
                int minX = x1, maxX = x0, minY = y1, maxY = y0;
                for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
                    if (pixels[y * w + x].a > 180)
                    { minX = Math.Min(minX,x); maxX = Math.Max(maxX,x); minY = Math.Min(minY,y); maxY = Math.Max(maxY,y); }
                var id = old.TryGetValue(names[i], out var known) ? known : GUID.Generate();
                rects.Add(new SpriteRect { name = names[i], spriteID = id,
                    rect = new(minX,minY,maxX-minX+1,maxY-minY+1), alignment = SpriteAlignment.Center, pivot = new(.5f,.5f) });
                pairs.Add(new SpriteNameFileIdPair(names[i], id));
            }
            UnityEngine.Object.DestroyImmediate(source);
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);
            provider.Apply(); importer.SaveAndReimport();
            var portraits = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(x => x.name);
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/_Project/Data/GameDatabase.asset");
            var skins = new[] { db.playerSkin, db.motherSkin, db.fatherSkin, db.lecturerSkin };
            for (int i = 0; i < skins.Length; i++) { skins[i].portrait = portraits[names[i]]; EditorUtility.SetDirty(skins[i]); }
            foreach (var night in UnityEngine.Object.FindObjectsByType<NightManager>(FindObjectsInactive.Include))
            { Set(night,"motherIcon",portraits["Ibu"]); Set(night,"fatherIcon",portraits["Ayah"]); }
        }

        static void ApplyLocation(Transform root, string art, int tier)
        {
            if (root == null) return;
            foreach (string name in new[] { "ReferenceArt", "Floor", "WallDecor", "DiningDecor", "Entrance", "Dining", "Walls" })
            { var old = root.Find(name); if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject); }
            var layer = GeprekBuilder.Go("ReferenceArt", root).transform;
            var background = AssetDatabase.LoadAssetAtPath<Sprite>(Art + art + ".png");
            var sr = GeprekBuilder.Sr("IllustratedRoom", layer, background, Vector3.zero, -3000);
            sr.transform.localScale = new(16.73f / background.bounds.size.x, 9.4f / background.bounds.size.y, 1);
            var occlusion = layer.gameObject.AddComponent<ReferenceCounterOcclusion>();
            Set(occlusion, "painting", background);
            var walls = GeprekBuilder.Go("Walls", root).transform;
            GeprekBuilder.Wall(walls, "Left", new(-8.1f, 0), new(.25f, 9.4f));
            GeprekBuilder.Wall(walls, "Right", new(8.1f, 0), new(.25f, 9.4f));
            GeprekBuilder.Wall(walls, "Bottom", new(0, -4.5f), new(16.5f, .25f));
            GeprekBuilder.Wall(walls, "CounterBack", new(0, 1.75f), new(16.5f, .3f));
            GeprekBuilder.Wall(walls, "PrepCounter", Pixel(460,450), new(1.25f,.35f));
            GeprekBuilder.Wall(walls, "WashCounter", Pixel(1020,450), new(1.35f,.35f));
            var kitchen = root.Find("Kitchen");
            var placements = new Dictionary<string, Vector2> {
                {"SumberAyam", new(1565,455)}, {"SumberNasi", new(150,290)},
                {"Penggorengan1",new(290,290)}, {"Penggorengan2",new(423,290)},
                {"Cobek1",new(575,290)}, {"SumberSambalMerah",new(744,290)},
                {"SumberSambalIjo",new(886,290)}, {"SumberTelur",new(1025,290)},
                {"SumberTahu",new(1172,290)}, {"SumberLalapan",new(1324,290)},
                {"SumberKeju",new(1474,290)}, {"TempatSampah",new(170,445)},
                {"MejaPenyajian",new(757,445)}, {"Kasir",new(1336,445)} };
            foreach (Transform child in kitchen)
            {
                if (!placements.TryGetValue(child.name, out var xy)) { child.gameObject.SetActive(false); continue; }
                child.localPosition = Pixel(xy.x, xy.y);
                child.gameObject.SetActive(true);
                var upgradeToggle = child.GetComponent<UpgradeToggle>();
                if (upgradeToggle != null) Set(upgradeToggle,"startActive",tier > 1 || child.name != "Penggorengan2");
                foreach (var label in child.GetComponentsInChildren<TextMesh>(true)) label.gameObject.SetActive(false);
                foreach (var r in child.GetComponentsInChildren<SpriteRenderer>(true))
                    if (r.name is "Body" or "Shadow" or "Plaque" or "Label" or "NamePlate" or "Badge" or "Register" or "Pestle" or "OrderRail")
                        r.gameObject.SetActive(false);
                var body = child.Find("Body"); if (body != null) body.gameObject.SetActive(false);
                foreach (var collider in child.GetComponents<BoxCollider2D>())
                {
                    collider.size = new(child.name is "MejaPenyajian" or "Kasir" ? 1.3f : .65f, .28f);
                    collider.offset = new(0,.15f);
                }
                var icon = child.Find("ItemIcon"); if (icon != null) icon.localPosition = new(0,.65f,0);
                var bar = child.Find("Bar"); if (bar != null) bar.localPosition = new(0,.42f,0);
            }
            var biz = root.GetComponent<BusinessLocation>();
            var bso = new SerializedObject(biz); var fryers = bso.FindProperty("upgradeFryers");
            fryers.arraySize = tier == 1 ? 1 : 0;
            if (tier == 1) fryers.GetArrayElementAtIndex(0).objectReferenceValue = kitchen.Find("Penggorengan2").GetComponent<UpgradeToggle>();
            bso.ApplyModifiedPropertiesWithoutUndo();
            var chicken = kitchen.Find("SumberAyam");
            var oldLabel = chicken.Find("Label");
            if (oldLabel != null) UnityEngine.Object.DestroyImmediate(oldLabel.gameObject);
            Geprek.World.WorldLabel.Create(chicken, "AYAM", new(0,-.25f,0), .035f, new(1,.9f,.65f), 500);
            var dining = GeprekBuilder.Go("Dining", root).transform;
            float[] centers = tier == 1 ? new[]{200f,476f,763f,1031f,1310f} : tier == 2 ? new[]{156f,455f,753f,1050f,1336f} : new[]{176f,466f,773f,1064f,1338f};
            // Hip anchors measured at each painted chair, rather than a uniform offset from the table.
            float[] leftChairs = tier == 1 ? new[]{130f,402f,690f,958f,1240f} : tier == 2 ? new[]{78f,377f,655f,955f,1237f} : new[]{110f,388f,700f,984f,1258f};
            float[] rightChairs = tier == 1 ? new[]{267f,543f,818f,1090f,1370f} : tier == 2 ? new[]{254f,540f,810f,1084f,1403f} : new[]{251f,526f,837f,1122f,1411f};
            var seats = new List<Seat>();
            for (int i = 0; i < 5; i++)
            {
                var table = GeprekBuilder.Go("Meja"+(i+1), dining, Pixel(centers[i],715)).transform;
                GeprekBuilder.Box(table.gameObject,new(.85f,.45f),new(0,.12f));
                for (int side = 0; side < 2; side++)
                {
                    var go = GeprekBuilder.Go(side == 0 ? "SeatLeft" : "SeatRight", table);
                    var seat = go.AddComponent<Seat>();
                    float sign = side == 0 ? -1 : 1;
                    float chairX = side == 0 ? leftChairs[i] : rightChairs[i];
                    var sit = GeprekBuilder.Go("Sit",go.transform,Pixel(chairX,tier == 3 ? 699 : 698)-table.localPosition).transform;
                    var approach = GeprekBuilder.Go("Approach",go.transform,Pixel(chairX+sign*18,598)-table.localPosition).transform;
                    var dish = GeprekBuilder.Go("Dish",go.transform,Pixel(centers[i]+sign*22,tier == 3 ? 664 : 686)-table.localPosition).transform;
                    Set(seat,"sitPoint",sit); Set(seat,"approachPoint",approach); Set(seat,"dishPoint",dish);
                    Set(seat,"seatedFacing",side == 0 ? (int)Facing.Right : (int)Facing.Left);
                    bool upgrade = tier == 1 ? side == 1 : tier == 2 ? side == 1 && i > 2 : false;
                    Set(seat,"isUpgradeSeat",upgrade);
                    seats.Add(seat);
                }
            }
            var spawner = root.GetComponentInChildren<CustomerSpawner>(true);
            var sso = new SerializedObject(spawner); var list = sso.FindProperty("seats"); list.arraySize=seats.Count;
            for(int i=0;i<seats.Count;i++) list.GetArrayElementAtIndex(i).objectReferenceValue=seats[i];
            sso.FindProperty("laneY").floatValue=-1.25f;
            sso.ApplyModifiedPropertiesWithoutUndo();
            foreach (var r in spawner.GetComponentsInChildren<SpriteRenderer>(true)) r.enabled=false;
            foreach (var t in spawner.GetComponentsInChildren<TextMesh>(true)) t.gameObject.SetActive(false);
            root.Find("CustomerSpawner/DoorPoint").localPosition=Pixel(15,580);
            root.Find("CustomerSpawner/ExitPoint").localPosition=Pixel(-90,580);
            for(int i=0;i<5;i++) {var q=root.Find("CustomerSpawner/Queue"+i); if(q!=null) q.localPosition=Pixel(150+i*70,595);}
            root.Find("StaffIdle").localPosition=Pixel(600,510);
            foreach (var sort in root.GetComponentsInChildren<SortingByY>(true)) sort.Apply();
        }

        public static void Set(UnityEngine.Object target,string name,object value)
        {
            if (target == null) return;
            var so=new SerializedObject(target); var p=so.FindProperty(name);
            if(p==null) throw new InvalidOperationException(target.name+" missing "+name);
            if(value is UnityEngine.Object o) p.objectReferenceValue=o;
            else if(value is bool b) p.boolValue=b;
            else if(value is int i) p.intValue=i;
            else if(value is float f) p.floatValue=f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
