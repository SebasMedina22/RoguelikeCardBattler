namespace RoguelikeCardBattler.Save
{
    /// <summary>
    /// Punto único de acceso al sistema de guardado.
    /// Clase pura: se usa desde controllers cuando se necesite.
    /// </summary>
    public static class SaveService
    {
        private static ISaveService _instance;

        public static ISaveService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LocalFileSaveService();
                }

                return _instance;
            }
        }
    }
}
