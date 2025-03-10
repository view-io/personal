namespace View.Personal.Classes
{
    using System;

    public class FileViewModel
    {
        public string? Name { get; set; }
        public string? CreatedUtc { get; set; }
        public string? FilePath { get; set; }
        public string? DocumentType { get; set; }
        public string? ContentLength { get; set; }
        public Guid NodeGuid { get; set; }
    }
}