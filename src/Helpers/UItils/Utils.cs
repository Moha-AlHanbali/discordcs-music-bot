namespace MusicBot
{
    using System.IO;
    public class Utils
    {
        public void CreateMediaDirectory()
        {
            if (!Directory.Exists("MediaTemp")) Directory.CreateDirectory("MediaTemp");;
            return;
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
            return;

        }
        public void PrepareMediaDirectory()
        {
            if (!Directory.Exists("MediaTemp")) CreateMediaDirectory();
            else ClearMediaDirectory();
            return;
        }

        public void PurgeFile(string filePath)
        {
            if (File.Exists(filePath)) File.Delete(filePath);
            if (File.Exists(filePath.Replace(".webm", ".mp3"))) File.Delete(filePath.Replace(".webm", ".mp3"));
            return;
        }
        public Boolean CheckFile(string filePath)
        {
            if (File.Exists(filePath) && File.Exists(filePath.Replace(".webm", ".mp3"))) return true; else return false;
        }
    }
}