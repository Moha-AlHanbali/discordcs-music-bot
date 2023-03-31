namespace MusicBot
{
    using System.IO;
    class Utils
    {
        public void CreateMediaDirectory()
        {
            Directory.CreateDirectory("MediaTemp");
        }
        public void ClearMediaDirectory()
        {
            System.IO.DirectoryInfo di = new DirectoryInfo("MediaTemp");

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }
        public void PrepareMediaDirectory()
        {
            if (!Directory.Exists("MediaTemp")) CreateMediaDirectory();
            else ClearMediaDirectory();
        }
    }
}