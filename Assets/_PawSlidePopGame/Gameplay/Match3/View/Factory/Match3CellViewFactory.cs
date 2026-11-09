using _PawSlidePopGame._Scripts.Core.DesignPattern.Factory;
using _PawSlidePopGame.Scripts.DesignPattern.ObjectPooling;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View.Factory
{
    public class Match3CellViewFactory : IFactory<CellViewSpawnData, Match3CellView>
    {
        public Match3CellView CreateVisual(CellViewSpawnData data, Transform parent)
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

            Match3CellView view = manager.Spawn(data.Prefab, Vector3.zero, Quaternion.identity, parent);

            if (view == null)
            {
                return null;
            }

            view.name = data.Name;
            view.transform.localPosition = data.LocalPosition;
            view.Initialize(data.Cell);
            return view;
        }

        public void ReturnVisual(Match3CellView visual)
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

