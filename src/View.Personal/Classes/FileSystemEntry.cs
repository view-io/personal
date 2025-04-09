namespace View.Personal.Classes
{
    using Material.Icons;

    public class FileSystemEntry
    {
        public string Name { get; set; }
        public string Size { get; set; }
        public string LastModified { get; set; }
        public string FullPath { get; set; }
        public bool IsDirectory { get; set; }

        public MaterialIconKind IconKind =>
            IsDirectory ? MaterialIconKind.Folder : MaterialIconKind.FileOutline;
    }
}