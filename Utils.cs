namespace MusicBot
{
    using System.IO;
    class Utils
    {
        public void CreateMediaDirectory()
        {
            if (!Directory.Exists("MediaTemp")) CreateMediaDirectory();
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

        public void PurgeFile(string filePath)
        {
            if (File.Exists(filePath)) File.Delete(filePath);
            if (File.Exists(filePath.Replace(".webm", ".mp3"))) File.Delete(filePath.Replace(".webm", ".mp3"));
        }
        public Boolean CheckFile(string filePath)
        {
            if (File.Exists(filePath) && File.Exists(filePath.Replace(".webm", ".mp3"))) return true; else return false;
        }
    }
}