using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Match;
using _PawSlidePopGame._Scripts.Feature.Match3.Logic.Move;

namespace _PawSlidePopGame._Scripts.Feature.Match3.Logic.Rules
{
    public sealed class BoardRuleSet
    {
        public static BoardRuleSet Default { get; } = new BoardRuleSet(
            new LineSlideMoveRule(),
            new BoardMatchFinder(),
            new List<IBoardPostMoveRule>());

        public IBoardMoveRule MoveRule { get; }
        public IBoardMatchRule MatchRule { get; }
        public IReadOnlyList<IBoardPostMoveRule> PostMoveRules { get; }

        public BoardRuleSet(IBoardMoveRule moveRule, IBoardMatchRule matchRule, IReadOnlyList<IBoardPostMoveRule> postMoveRules)
        {
            MoveRule = moveRule;
            MatchRule = matchRule;
            PostMoveRules = postMoveRules;
        }
    }
}

