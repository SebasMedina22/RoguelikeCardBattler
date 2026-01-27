namespace RoguelikeCardBattler.Save
{
    /// <summary>
    /// Abstracción para guardado/carga de progreso meta.
    /// Permite reemplazar implementación (local, cloud, etc).
    /// </summary>
    public interface ISaveService
    {
        void SaveMeta(SaveData data);
        bool TryLoadMeta(out SaveData data);
        bool HasValidSave();
        void DeleteSave();
    }
}
