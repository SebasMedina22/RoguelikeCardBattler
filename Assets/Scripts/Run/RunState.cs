using System.Collections.Generic;

namespace RoguelikeCardBattler.Run
{
    /// <summary>
    /// Estado en memoria de la run. No persiste a disco.
    /// </summary>
    public class RunState
    {
        public enum NodeOutcome
        {
            None,
            Victory,
            Defeat
        }

        public int CurrentPositionNodeId { get; set; } = -1;
        public int CurrentNodeId { get; set; } = -1;
        public int Gold { get; set; } = 0;
        public HashSet<int> CompletedNodes { get; } = new HashSet<int>();
        public HashSet<int> AvailableNodes { get; } = new HashSet<int>();
        public bool PendingReturnFromBattle { get; set; }
        public NodeOutcome LastNodeOutcome { get; set; } = NodeOutcome.None;
        public bool RunFailed { get; set; }

        public void EnsureInitialized(ActMap map)
        {
            if (map == null)
            {
                return;
            }

            if (AvailableNodes.Count > 0 || CompletedNodes.Count > 0)
            {
                return;
            }

            AvailableNodes.Clear();
            CompletedNodes.Clear();
            AvailableNodes.Add(map.StartNodeId);
            CurrentPositionNodeId = map.StartNodeId;
        }

        public void Reset(ActMap map)
        {
            CompletedNodes.Clear();
            AvailableNodes.Clear();
            CurrentNodeId = -1;
            CurrentPositionNodeId = -1;
            Gold = 0;
            PendingReturnFromBattle = false;
            LastNodeOutcome = NodeOutcome.None;
            RunFailed = false;
            EnsureInitialized(map);
        }

        public bool IsNodeAvailable(int nodeId) => AvailableNodes.Contains(nodeId);
        public bool IsNodeCompleted(int nodeId) => CompletedNodes.Contains(nodeId);
    }
}
