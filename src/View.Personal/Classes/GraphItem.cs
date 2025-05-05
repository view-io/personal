namespace View.Personal.Classes
{
    using System;

    public class GraphItem
    {
        public string Name { get; set; }
        public Guid GUID { get; set; }

        public DateTime CreatedUtc { get; set; }

        public DateTime LastUpdateUtc { get; set; }
    }
}