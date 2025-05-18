using System.Drawing;

namespace ImageSheetCreatorAvalonia
{
    public class ImageData
    {
        public string Path { get; }
        public string FileName { get; }
        public Image Image { get; }
        public int Limit { get; }
        public string DisplayLimit { get; }

        public ImageData(string path, int limit)
        {
            Path = path;
            Image = Image.FromFile(Path);
            FileName = System.IO.Path.GetFileName(Path);
            Limit = limit;
            DisplayLimit = limit < 1 ? "mind" : limit.ToString();
        }
    }
}
