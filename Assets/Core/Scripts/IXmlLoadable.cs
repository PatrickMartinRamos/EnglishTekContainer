namespace EnglishTek.Core
{
    /// <summary>
    /// Optional contract for new game managers that want to signal they support
    /// centralized XML loading. Implementing this is NOT required — it simply
    /// provides a consistent entry-point for future tooling or editor automation.
    /// </summary>
    public interface IXmlLoadable
    {
        /// <summary>Numeric ID of this interactive (e.g. 106).</summary>
        int GameID { get; }

        /// <summary>Active difficulty key used to select the correct XML file/node.</summary>
        string Difficulty { get; }

        /// <summary>
        /// Load all XML data needed for this interactive.
        /// Implementations should call XmlLoader methods here.
        /// </summary>
        void LoadXmlData();
    }
}
