using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Interfaces;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Factory;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities
{
    public sealed class UnderlayContentModel : TileModel
    {
        public UnderlayContentModel(int instanceId, UnderlayDefinitionSO definition)
            : base(instanceId, definition)
        {
        }

        public UnderlayDefinitionSO UnderlayDefinition => Definition as UnderlayDefinitionSO;
        public new IUnderlayLogic UnderlayLogic => Logic as IUnderlayLogic;
        public override BoardLayer Layer => BoardLayer.Underlay;

        protected override object CreateLogic(BoardContentDefinitionSO definition)
        {
            return definition is UnderlayDefinitionSO underlayDefinition
                ? UnderlayLogicFactory.CreateLogic(underlayDefinition)
                : null;
        }
    }
}

