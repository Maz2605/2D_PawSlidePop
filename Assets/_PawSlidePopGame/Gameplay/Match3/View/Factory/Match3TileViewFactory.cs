using _PawSlidePopGame._Scripts.Core.DesignPattern.Factory;
using _PawSlidePopGame.Scripts.DesignPattern.ObjectPooling;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View.Factory
{
    public class Match3TileViewFactory : IFactory<TileViewSpawnData, Match3TileView>
    {
        public Match3TileView CreateVisual(TileViewSpawnData data, Transform parent)
        {
            if (data.Prefab == null)
            {
                return null;
            }

            PoolingManager manager = PoolingManager.Instance;
            if (manager == null)
            {
                return null;
            }

            Match3TileView view = manager.Spawn(data.Prefab, Vector3.zero, Quaternion.identity, parent);

            if (view == null)
            {
                return null;
            }

            view.name = data.Name;
            view.Bind(data.Tile, data.Definition);
            view.SnapToLocalPosition(data.LocalPosition);
            view.SetIdleEnabled(data.IsIdleEnabled);
            return view;
        }

        public void ReturnVisual(Match3TileView visual)
        {
            if (visual == null)
            {
                return;
            }

            PoolingManager manager = PoolingManager.Instance;
            if (manager != null)
            {
                manager.Despawn(visual.gameObject);
                return;
            }

            Object.DestroyImmediate(visual.gameObject);
        }
    }
}

