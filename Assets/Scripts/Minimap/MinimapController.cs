using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using MyToolz.DesignPatterns.Singleton;
using MyToolz.ScriptableObjects.MiniMap;

namespace MyToolz.MiniMap
{
    public enum MinimapMode
    {
        Mini, Fullscreen
    }

    [System.Serializable]
    public struct Minimap
    {
        public Sprite Sprite;
        public float GroundLevel;
    }

    public interface IMinimap
    {
        public void Register(IMinimapObject minimapObject, bool follow = false);
        public void Deregister(IMinimapObject minimapObject);
    }

    public interface IMinimapObject
    {
        public bool IsHidden { get; }
        public void Show();
        public void Hide();
    }

    public class MinimapController : Singleton<MinimapController>
    {
        private List<Minimap> maps = new();
        private Vector2 worldSize => miniMapSO.WorldSize;
        private Vector2 fullScreenDimensions => miniMapSO.FullScreenDimensions;
        private float zoomSpeed => miniMapSO.ZoomSpeed;
        private float maxZoom => miniMapSO.MaxZoom;
        private float minZoom => miniMapSO.MinZoom;
        [SerializeField] private RectTransform scrollViewRectTransform;
        [SerializeField] private RectTransform contentRectTransform;
        private MinimapIcon minimapIconPrefab => miniMapSO.MinimapIconPrefab;
        private Image mapImage;
        private Matrix4x4 transformationMatrix;
        private MinimapMode currentMiniMapMode = MinimapMode.Mini;
        private MinimapIcon followIcon;
        private MinimapWorldObject followObject;
        private Vector2 scrollViewDefaultSize;
        private Vector2 scrollViewDefaultPosition;
        private Dictionary<MinimapWorldObject, MinimapIcon> miniMapWorldObjectsLookup = new Dictionary<MinimapWorldObject, MinimapIcon>();
        private Dictionary<float, Sprite> heightMaps = new Dictionary<float, Sprite>();

        private MiniMapSO miniMapSO;

        [Inject]
        private void Construct(MiniMapSO miniMapSO)
        {
            this.miniMapSO = miniMapSO;
        }

        private void MapMinimapsToHeightmaps ()
        {
            heightMaps.Clear();
            maps = maps.OrderByDescending((map) => map.GroundLevel).ToList();
            foreach (var map in maps)
            {
                heightMaps.TryAdd(map.GroundLevel,map.Sprite);
            }
        }

        private Sprite GetMapForHeight(float y)
        {
            foreach (var groundLevel in heightMaps.Keys)
            {
                if (heightMaps.TryGetValue(groundLevel,out Sprite sprite))
                {
                    if (groundLevel > y) continue;
                    return sprite;
                }
            }
            return null;
        }

        private void Start()
        {
            mapImage = contentRectTransform.GetComponentInChildren<Image>();
            maps = miniMapSO.Maps.ToList();
            MapMinimapsToHeightmaps();
            scrollViewDefaultSize = scrollViewRectTransform.sizeDelta;
            scrollViewDefaultPosition = scrollViewRectTransform.anchoredPosition;
        }

        private void Update()
        {
            CalculateTransformationMatrix();
            UpdateMiniMapIcons();
            CenterMapOnIcon();
            UpdateMinimapSprite();
        }

        public void RegisterMinimapWorldObject(MinimapWorldObject miniMapWorldObject, bool followObject = false)
        {
            var minimapIcon = Instantiate(minimapIconPrefab);
            minimapIcon.transform.SetParent(contentRectTransform);
            minimapIcon.transform.SetParent(contentRectTransform);
            minimapIcon.Image.sprite = miniMapWorldObject.MinimapIcon;
            miniMapWorldObjectsLookup[miniMapWorldObject] = minimapIcon;

            if (followObject)
                followIcon = minimapIcon;
                this.followObject = miniMapWorldObject;
        }

        public void RemoveMinimapWorldObject(MinimapWorldObject minimapWorldObject)
        {
            if (miniMapWorldObjectsLookup.TryGetValue(minimapWorldObject, out MinimapIcon icon))
            {
                miniMapWorldObjectsLookup.Remove(minimapWorldObject);
                Destroy(icon.gameObject);
            }
        }


        private Vector2 halfVector2 = new Vector2(0.5f, 0.5f);
        public void SetMinimapMode(MinimapMode mode)
        {
            const float defaultScaleWhenFullScreen = 1.3f; // 1.3f looks good here but it could be anything

            if (mode == currentMiniMapMode)
                return;

            switch (mode)
            {
                case MinimapMode.Mini:
                    scrollViewRectTransform.sizeDelta = scrollViewDefaultSize;
                    scrollViewRectTransform.anchorMin = Vector2.one;
                    scrollViewRectTransform.anchorMax = Vector2.one;
                    scrollViewRectTransform.pivot = Vector2.one;
                    scrollViewRectTransform.anchoredPosition = scrollViewDefaultPosition;
                    currentMiniMapMode = MinimapMode.Mini;
                    break;
                case MinimapMode.Fullscreen:
                    scrollViewRectTransform.sizeDelta = fullScreenDimensions;
                    scrollViewRectTransform.anchorMin = halfVector2;
                    scrollViewRectTransform.anchorMax = halfVector2;
                    scrollViewRectTransform.pivot = halfVector2;
                    scrollViewRectTransform.anchoredPosition = Vector2.zero;
                    currentMiniMapMode = MinimapMode.Fullscreen;
                    contentRectTransform.transform.localScale = Vector3.one * defaultScaleWhenFullScreen;
                    break;
            }
        }

        private void ZoomMap(float zoom)
        {
            if (zoom == 0)
                return;

            float currentMapScale = contentRectTransform.localScale.x;
            float zoomAmount = (zoom > 0 ? zoomSpeed : -zoomSpeed) * currentMapScale;
            float newScale = currentMapScale + zoomAmount;
            float clampedScale = Mathf.Clamp(newScale, minZoom, maxZoom);
            contentRectTransform.localScale = Vector3.one * clampedScale;
        }

        private void CenterMapOnIcon()
        {
            if (followIcon != null)
            {
                float mapScale = contentRectTransform.transform.localScale.x;
                contentRectTransform.anchoredPosition = (-followIcon.RectTransform.anchoredPosition * mapScale);
            }
        }

        private void UpdateMinimapSprite()
        {
            if (followIcon == null || followObject == null) return;

            float objectHeight = followObject.transform.position.y;
            Sprite newMinimap = GetMapForHeight(objectHeight);

            if (newMinimap != null)
            {
                mapImage.sprite = newMinimap; 
            }
        }


        private void UpdateMiniMapIcons()
        {
            float iconScale = 1 / contentRectTransform.transform.localScale.x;
            foreach (var kvp in miniMapWorldObjectsLookup)
            {
                var miniMapWorldObject = kvp.Key;
                var miniMapIcon = kvp.Value;
                if (miniMapWorldObject.IsHidden)
                {
                    miniMapIcon.Hide();
                    continue;
                }
                else
                {
                    miniMapIcon.Show();
                }
                var mapPosition = WorldPositionToMapPosition(miniMapWorldObject.transform.position);
                miniMapIcon.RectTransform.anchoredPosition = mapPosition;
                var rotation = miniMapWorldObject.transform.rotation.eulerAngles;
                miniMapIcon.IconRectTransform.localRotation = Quaternion.AngleAxis(-rotation.y, Vector3.forward);
                miniMapIcon.IconRectTransform.localScale = Vector3.one * iconScale;
            }
        }

        private Vector2 WorldPositionToMapPosition(Vector3 worldPos)
        {
            var pos = new Vector2(worldPos.x, worldPos.z);
            return transformationMatrix.MultiplyPoint3x4(pos);
        }

        private void CalculateTransformationMatrix()
        {
            var minimapSize = contentRectTransform.rect.size;
            var worldSize = new Vector2(this.worldSize.x, this.worldSize.y);

            var translation = Vector2.zero; //-minimapSize / 2;
            var scaleRatio = minimapSize / worldSize;

            transformationMatrix = Matrix4x4.TRS(translation, Quaternion.identity, scaleRatio);

            //  {scaleRatio.x,   0,           0,   translation.x},
            //  {  0,        scaleRatio.y,    0,   translation.y},
            //  {  0,            0,           1,            0},
            //  {  0,            0,           0,            1}
        }
    }
}