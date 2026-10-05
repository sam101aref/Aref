using System;

namespace Arash.Combat
{
    /// <summary>A way to run a battle: the real-time modes, or the final arrow flight.</summary>
    public interface IBattleMode
    {
        /// <summary>Raised once; true when the player won.</summary>
        event Action<bool> BattleEnded;

        /// <summary>Starts the battle (after any intro dialogue).</summary>
        void Begin();

        /// <summary>0–1 measure of how well the player held up, used for the second star.</summary>
        float PlayerCondition { get; }
    }
}
