using UnityEngine;

namespace Game
{
    namespace Core
    {
        public class PhaseManager
        {
            public GamePhase CurrentPhase { get; private set; } = GamePhase.None;

            public void SetPhase(GamePhase newPhase)
            {
                if (CurrentPhase == newPhase)
                    return;

                Debug.Log($"[PhaseManager] Phase Change : {CurrentPhase} -> {newPhase}");
                CurrentPhase = newPhase;
            }

            public bool IsPhase(GamePhase phase)
            {
                return CurrentPhase == phase;
            }
        }
    }
}
