using System.Collections.Generic;

namespace RoguelikeCardBattler.Run
{
    /// <summary>
    /// Estado en memoria de la run. No persiste a disco.
    /// </summary>
    public class RunState
    {
        public const int NodeCount = 3;

        public int CurrentNodeIndex { get; set; } = -1;
        public int Gold { get; set; } = 0;
        public List<bool> CompletedNodes { get; } = new List<bool>();

        public void EnsureInitialized()
        {
            if (CompletedNodes.Count == NodeCount)
            {
                return;
            }

            CompletedNodes.Clear();
            for (int i = 0; i < NodeCount; i++)
            {
                CompletedNodes.Add(false);
            }
        }
    }
}
