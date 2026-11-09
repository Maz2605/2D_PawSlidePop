using System;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Enum;
using _PawSlidePopGame._Scripts.Feature.Match3.Core.Interfaces;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Factory;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities
{
    public sealed class OverlayContentModel : TileModel
    {
        public OverlayContentModel(int instanceId, BoardContentDefinitionSO definition)
            : base(instanceId, definition)
        {
        }

        public OverlayDefinitionSO OverlayDefinition => Definition as OverlayDefinitionSO;
        public new IOverlayLogic OverlayLogic => Logic as IOverlayLogic;
        public override BoardLayer Layer => BoardLayer.Overlay;

        protected override object CreateLogic(BoardContentDefinitionSO definition)
        {
            if (definition is OverlayDefinitionSO overlayDefinition)
            {
                return OverlayLogicFactory.CreateLogic(overlayDefinition);
            }

            return definition is TileDefinitionSO tileDefinition
                ? OverlayLogicFactory.CreateLogic(tileDefinition)
                : null;
        }
    }
}

