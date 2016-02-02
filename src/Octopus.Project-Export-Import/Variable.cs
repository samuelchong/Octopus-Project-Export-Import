namespace OctopusClient
{
    public class Variable
    {
        #region Public Properties

        public string Id { get; set; }
        public bool IsSensitive { get; set; }

        public string Name { get; set; }
        public string Value { get; set; }

        #endregion Public Properties
    }
}