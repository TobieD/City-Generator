using System.Collections.Generic;
using Helpers;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor
{
    public class TerrainEditor
    {
        private bool _propFoldout = false;
        private bool _treeFoldout = false;
        private bool _textureFoldout = false;
        private bool _grassFoldout = false;
        private bool _foldout = true;
        private bool _useSeed = false;
        readonly EditorWindow _parentWindow;
        
        public TerrainSettings Settings;
        private GameObject _selectedProp = null;
        private string _selectedPropLabel = "Prop";

        private GameObject _selectedTree = null;
        private string _selectedTreeLabel = "Tree";

        private SplatTexture _selectedTexture = new SplatTexture();
        private GrassTexture _selectedGrassTexture = new GrassTexture();
        private string _selectedGrassLabel = "Grass";

        public TerrainEditor(TerrainSettings settings, EditorWindow parentWindow)
        {
            Settings = settings;
            _parentWindow = parentWindow;
        }

        public void DrawGUI(GUIStyle foldoutStyle)
        {
            _foldout = EditorGUILayout.Foldout(_foldout, "Terrain Settings", foldoutStyle);

            if (!_foldout)
            {
                return;
            }

            EditorGUI.indentLevel++;
            
            Settings.TerrainHeight = (int)EditorGUILayout.Slider("Terrain Height", Settings.TerrainHeight, 16.0f, 1024.0f);
            //Settings.TerrainScaleFactor = UnityEditor.EditorGUILayout.IntSlider("Scale", Settings.TerrainScaleFactor,1, 10);
            // Settings.HeightmapSize = (int)EditorGUILayout.Slider("Heightmap Size", Settings.HeightmapSize, 256.0f, 2048.0f);

            //Noise
            EditorGUILayout.LabelField("Noise Settings", EditorStyles.boldLabel);
            _useSeed = EditorGUILayout.Toggle("Use Seed", _useSeed);

            //noise Seed
            if (_useSeed)
            {
                Settings.GroundSeed = EditorGUILayout.IntField("Ground Seed", Settings.GroundSeed);
                Settings.MountainSeed = EditorGUILayout.IntField("Mountain Seed", Settings.MountainSeed);
            }

            Settings.GroundFrequency = EditorGUILayout.Slider("Ground Frequency", Settings.GroundFrequency,0,5000.0f);
            Settings.MountainFrequency = EditorGUILayout.Slider("Mountain Frequency", Settings.MountainFrequency,0,5000.0f);
            Settings.TreeFrequency = EditorGUILayout.Slider("Tree Frequency", Settings.TreeFrequency, 0, 5000.0f);
            Settings.DetailFrequency = EditorGUILayout.Slider("Detail Frequency", Settings.DetailFrequency, 0, 5000.0f);

            //Textures
            _textureFoldout = EditorGUILayout.Foldout(_textureFoldout,"Textures", foldoutStyle);
            if(_textureFoldout)
                TerrainTextureGUI();

            //Trees
            _treeFoldout = EditorGUILayout.Foldout(_treeFoldout, "Trees", foldoutStyle);
            if(_treeFoldout)
                TerrainTreeGUI();

            //Details
            _propFoldout = EditorGUILayout.Foldout(_propFoldout, "Props", foldoutStyle);
            if(_propFoldout)
                TerrainDetailGUI();

            //Details
            _grassFoldout = EditorGUILayout.Foldout(_grassFoldout, "Grass", foldoutStyle);
            if (_grassFoldout)
                TerrainGrassGUI();

            EditorGUI.indentLevel--;

        }

        public TerrainSettings GetSettings()
        {
            if (!_useSeed)
            {
                Settings.GroundSeed = RandomHelper.RandomInt();
                Settings.MountainSeed = RandomHelper.RandomInt();
            }


            //only take filled textures
            var maps = new List<SplatTexture>();
            foreach (var splatTexture in Settings.SplatMaps)
            {
                if (splatTexture.Texture != null)
                {
                    maps.Add(splatTexture);
                }
            }

            Settings.SplatMaps = maps;

            return Settings;
        }
        
        /// <summary>
        /// Editor for the splatmaps of the terrain
        /// </summary>
        private void TerrainTextureGUI()
        {
            var windowWidth = _parentWindow.maxSize.x;
            var spacing = windowWidth*0.05f;
            var buttonWidth = windowWidth/2 - spacing;

            //Selected Texture GUI
            EditorGUILayout.BeginHorizontal(GUILayout.Width(windowWidth), GUILayout.Height(20));
            GUILayout.Space(spacing/2);

            _selectedTexture.Texture = (Texture2D)EditorGUILayout.ObjectField(_selectedTexture.Texture, typeof (Texture2D),false,GUILayout.Width(buttonWidth * 1.5f));
            EditorGUILayout.LabelField(_selectedTexture.ID);
            GUILayout.Space(spacing/2);
            EditorGUILayout.EndHorizontal();

            //texture previews of added textures
            int size = 64;
            
            GUILayout.BeginHorizontal();
            GUILayout.Space(spacing);

            //the splatmaps are limited to 4 + 1 road
            for(int i = 0; i < Settings.SplatMaps.Count; ++i)
            {
                var splatTexture = Settings.SplatMaps[i];

                GUILayout.BeginVertical();
                splatTexture.ID = (i == 0) ? splatTexture.ID = "Road" : "Terrain " + i;
                GUILayout.Label(splatTexture.ID);
                if (GUILayout.Button(splatTexture.Texture,GUIStyle.none, GUILayout.Width(size + (size * 0.2f)), GUILayout.Height(size)))
                {
                    _selectedTexture = splatTexture;
                }
                GUILayout.EndVertical();
            }
            GUILayout.Space(spacing);
            GUILayout.EndHorizontal();

            //Adding / Removing
            EditorGUILayout.BeginHorizontal(GUILayout.Width(windowWidth), GUILayout.Height(20));
            GUILayout.Space(spacing);
            if (GUILayout.Button("Add terrain Texture", GUILayout.Width(buttonWidth)))
            {
                if (Settings.SplatMaps.Count < 4)
                {
                    var splat = new SplatTexture();
                    splat.Texture = _selectedTexture.Texture;
                    splat.TileSize = _selectedTexture.TileSize;

                    Settings.SplatMaps.Add(splat);
                }
            }

            //remove the selected splatTexture
            if (GUILayout.Button("Remove terrain Texture", GUILayout.Width(buttonWidth)))
            {
                Settings.SplatMaps.Remove(_selectedTexture);
            }
            GUILayout.Space(spacing);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Editor for the trees of the terrain
        /// </summary>
        private void TerrainTreeGUI()
        {
            var windowWidth = _parentWindow.maxSize.x;
            var spacing = windowWidth * 0.05f;
            var buttonWidth = windowWidth / 2 - spacing;

            //Selected Tree GUI
            EditorGUILayout.BeginHorizontal(GUILayout.Width(windowWidth), GUILayout.Height(20));
            GUILayout.Space(spacing / 2);
            _selectedTree = (GameObject)EditorGUILayout.ObjectField(_selectedTree, typeof(GameObject), false, GUILayout.Width(buttonWidth * 1.5f));
            EditorGUILayout.LabelField(_selectedTreeLabel);
            GUILayout.Space(spacing / 2);
            EditorGUILayout.EndHorizontal();

            //Show Added Trees
            //texture previews of added textures
            int size = 64;

            GUILayout.BeginHorizontal();
            GUILayout.Space(spacing);

            //the splatmaps are limited to 4 + 1 road
            for (int i = 0; i < Settings.Trees.Count; ++i)
            {
                var tree = Settings.Trees[i];
                GUILayout.BeginVertical();

                var prefabPreview = AssetPreview.GetAssetPreview(tree);
                string label = "Tree " + i;;
                GUILayout.Label(label);
                if (GUILayout.Button(prefabPreview, GUIStyle.none, GUILayout.Width(size + (size * 0.2f)), GUILayout.Height(size)))
                {
                    _selectedTree = tree;
                    _selectedTreeLabel = label;
                }
                GUILayout.EndVertical();
            }
            GUILayout.Space(spacing);
            GUILayout.EndHorizontal();


            //Adding / Removing
            EditorGUILayout.BeginHorizontal(GUILayout.Width(windowWidth), GUILayout.Height(20));
            GUILayout.Space(spacing);
            if (GUILayout.Button("Add Tree", GUILayout.Width(buttonWidth)))
            {
                if (!Settings.Trees.Contains(_selectedTree))
                {
                    Settings.Trees.Add(_selectedTree);
                }
            }

            //remove the selected splatTexture
            if (GUILayout.Button("Remove Tree", GUILayout.Width(buttonWidth)))
            {
                Settings.Trees.Remove(_selectedTree);
            }
            GUILayout.Space(spacing);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Editor for the detail props of the terrain
        /// </summary>
        private void TerrainDetailGUI()
        {
            
            var windowWidth = _parentWindow.maxSize.x;
            var spacing = windowWidth * 0.05f;
            var buttonWidth = windowWidth / 2 - spacing;

            //Selected Tree GUI
            EditorGUILayout.BeginHorizontal(GUILayout.Width(windowWidth), GUILayout.Height(20));
            GUILayout.Space(spacing / 2);
            _selectedProp = (GameObject)EditorGUILayout.ObjectField(_selectedProp, typeof(GameObject), false, GUILayout.Width(buttonWidth * 1.5f));
            EditorGUILayout.LabelField(_selectedPropLabel);
            GUILayout.Space(spacing / 2);
            EditorGUILayout.EndHorizontal();

            //Show Added Trees
            //texture previews of added textures
            int size = 64;

            GUILayout.BeginHorizontal();
            GUILayout.Space(spacing);

            //the splatmaps are limited to 4 + 1 road
            for (int i = 0; i < Settings.Props.Count; ++i)
            {
                var prop = Settings.Props[i];
                GUILayout.BeginVertical();

                var prefabPreview = AssetPreview.GetAssetPreview(prop);
                string label = "Prop " + i; ;
                GUILayout.Label(label);
                if (GUILayout.Button(prefabPreview, GUIStyle.none, GUILayout.Width(size + (size * 0.2f)), GUILayout.Height(size)))
                {
                    _selectedProp = prop;
                    _selectedPropLabel = label;
                }
                GUILayout.EndVertical();
            }
            GUILayout.Space(spacing);
            GUILayout.EndHorizontal();


            //Adding / Removing
            EditorGUILayout.BeginHorizontal(GUILayout.Width(windowWidth), GUILayout.Height(20));
            GUILayout.Space(spacing);
            if (GUILayout.Button("Add Prop", GUILayout.Width(buttonWidth)))
            {
                if (!Settings.Props.Contains(_selectedProp))
                {
                    Settings.Props.Add(_selectedProp);
                }
            }

            //remove the selected splatTexture
            if (GUILayout.Button("Remove Prop", GUILayout.Width(buttonWidth)))
            {
                Settings.Props.Remove(_selectedProp);
            }
            GUILayout.Space(spacing);
            EditorGUILayout.EndHorizontal();
        }

        private void TerrainGrassGUI()
        {
            var windowWidth = _parentWindow.maxSize.x;
            var spacing = windowWidth * 0.05f;
            var buttonWidth = windowWidth / 2 - spacing;

            //Selected Tree GUI
            EditorGUILayout.BeginHorizontal(GUILayout.Width(windowWidth), GUILayout.Height(20));
            GUILayout.Space(spacing / 2);
            _selectedGrassTexture.Texture = (Texture2D)EditorGUILayout.ObjectField(_selectedGrassTexture.Texture, typeof(Texture2D), false, GUILayout.Width(buttonWidth * 1.5f));
            EditorGUILayout.LabelField(_selectedGrassLabel);
            GUILayout.Space(spacing / 2);
            EditorGUILayout.EndHorizontal();

            //Show Added Trees
            //texture previews of added textures
            int size = 64;

            GUILayout.BeginHorizontal();
            GUILayout.Space(spacing);

            //the splatmaps are limited to 4 + 1 road
            for (int i = 0; i < Settings.GrassTextures.Count; ++i)
            {
                var grass = Settings.GrassTextures[i];
                GUILayout.BeginVertical();

                string label = "Grass " + i; ;
                GUILayout.Label(label);
                if (GUILayout.Button(grass.Texture, GUIStyle.none, GUILayout.Width(size + (size * 0.2f)), GUILayout.Height(size)))
                {
                    _selectedGrassTexture = grass;
                    _selectedGrassLabel = label;
                }

                //grass.HealthyColor = EditorGUILayout.ColorField("Healthy", grass.HealthyColor);
                //grass.DryColor = EditorGUILayout.ColorField("Dry", grass.DryColor);

                GUILayout.EndVertical();
            }
            GUILayout.Space(spacing);
            GUILayout.EndHorizontal();


            //Adding / Removing
            EditorGUILayout.BeginHorizontal(GUILayout.Width(windowWidth), GUILayout.Height(20));
            GUILayout.Space(spacing);
            if (GUILayout.Button("Add Grass", GUILayout.Width(buttonWidth)))
            {
                if (!Settings.GrassTextures.Contains(_selectedGrassTexture))
                {
                    Settings.GrassTextures.Add(_selectedGrassTexture);
                }
            }

            //remove the selected splatTexture
            if (GUILayout.Button("Remove Grass", GUILayout.Width(buttonWidth)))
            {
                Settings.GrassTextures.Remove(_selectedGrassTexture);
            }
            GUILayout.Space(spacing);
            EditorGUILayout.EndHorizontal();
        }
    }
}
