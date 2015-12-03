using System.Collections.Generic;
using Helpers;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.Scripts
{
    public class TerrainEditor
    {
        private bool _foldout = false;
        private bool _useSeed = false;

        public TerrainSettings _settings;

        public TerrainEditor(TerrainSettings settings)
        {
            _settings = settings;
        }

        public void DrawGUI(GUIStyle foldoutStyle)
        {
            _foldout = EditorGUILayout.Foldout(_foldout, "Terrain Settings", foldoutStyle);

            if (!_foldout)
            {
                return;
            }

            EditorGUI.indentLevel++;

            //Noise
            EditorGUILayout.LabelField("Noise Settings",EditorStyles.boldLabel);


            _settings.TerrainHeight = (int)EditorGUILayout.Slider("Terrain Height", _settings.TerrainHeight, 16.0f, 1024.0f);
           // _settings.HeightmapSize = (int)EditorGUILayout.Slider("Heightmap Size", _settings.HeightmapSize, 256.0f, 2048.0f);

            _useSeed = EditorGUILayout.Toggle("Use Seed", _useSeed);
            //noise Seed
            if (_useSeed)
            {
                _settings.GroundSeed = EditorGUILayout.IntField("Ground Seed", _settings.GroundSeed);
                _settings.MountainSeed = EditorGUILayout.IntField("Mountain Seed", _settings.MountainSeed);
            }

            _settings.GroundFrequency = EditorGUILayout.Slider("Ground Frequency", _settings.GroundFrequency,0,5000.0f);
            _settings.MountainFrequency = EditorGUILayout.Slider("Mountain Frequency", _settings.MountainFrequency,0,5000.0f);

            //Textures
            EditorGUILayout.LabelField("Splatmaps", EditorStyles.boldLabel);

            TextureEdit(_settings.RoadTexture,"Road texture");

            if (GUILayout.Button("+"))
            {
                if (_settings.SplatMaps.Count < 4)
                {
                    _settings.SplatMaps.Add(new SplatTexture());
                }
            }
            
            foreach (var map in _settings.SplatMaps)
            {
                TextureEdit(map);
            }


            EditorGUI.indentLevel--;

        }

        public TerrainSettings GetSettings()
        {
            if (!_useSeed)
            {
                _settings.GroundSeed = RandomHelper.RandomInt();
                _settings.MountainSeed = RandomHelper.RandomInt();
            }


            //only take filled textures
            var maps = new List<SplatTexture>();
            foreach (var splatTexture in _settings.SplatMaps)
            {
                if (splatTexture.Texture != null)
                {
                    maps.Add(splatTexture);
                }
            }

            _settings.SplatMaps = maps;

            return _settings;
        }

        private void TextureEdit(SplatTexture texture,string label = "")
        {
            if (label.Length >0)
            {
                EditorGUILayout.LabelField(label);
            }

            EditorGUILayout.BeginHorizontal();
            texture.Texture = (Texture2D)EditorGUILayout.ObjectField(texture.Texture, typeof (Texture2D),false);
            texture.TileSize = EditorGUILayout.FloatField("Tiling", texture.TileSize);
            EditorGUILayout.EndHorizontal();
        }
    }
}
