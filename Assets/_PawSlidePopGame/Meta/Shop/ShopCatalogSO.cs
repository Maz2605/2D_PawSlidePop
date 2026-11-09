using System.Collections.Generic;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Meta.Shop
{
    /// <summary>
    /// Danh mục tổng hợp tất cả vật phẩm bày bán trên Shop.
    /// Kéo thả các ShopItemSO vào đây để quản lý danh sách hàng hóa.
    /// </summary>
    [CreateAssetMenu(fileName = "ShopCatalog", menuName = "_PawSlidePopGame/Meta/Shop/Shop Catalog")]
    public sealed class ShopCatalogSO : ScriptableObject
    {
        [SerializeField] private List<ShopItemSO> items = new List<ShopItemSO>();

        public IReadOnlyList<ShopItemSO> Items => items;
    }
}
