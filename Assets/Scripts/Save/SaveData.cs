using System;
using System.Collections.Generic;

namespace RoguelikeCardBattler.Save
{
    /// <summary>
    /// Datos persistidos a disco para progreso meta (no gameplay activo).
    /// Versionado explícito para migraciones futuras.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string lastSavedUtc;
        public MetaProgress meta = new MetaProgress();

        public void TouchTimestampUtc()
        {
            lastSavedUtc = DateTime.UtcNow.ToString("o");
        }
    }

    /// <summary>
    /// Progreso meta mínimo. Placeholder para desbloqueos y contadores.
    /// </summary>
    [Serializable]
    public class MetaProgress
    {
        public int totalRuns = 0;
        public int totalVictories = 0;
        public List<string> unlockedCards = new List<string>();
        public List<string> unlockedCharacters = new List<string>();
    }
}
