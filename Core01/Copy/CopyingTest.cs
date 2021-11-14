using System.IO;
using System.Threading.Tasks;

namespace MarcinGajda.Copy
{
    public static class CopyingTest
    {
        public static T? Idk<T>(T? t)
            where T : struct
        {
            return t;
        }
        public static T Idk<T>(T? t)
            where T : class
        {
            return t;
        }

        public static async Task CopyFileAsync(string sourcePath, string destinationPath, int bufferSize = 4096, bool overwrite = true)
        {
            FileMode createMode = overwrite ? FileMode.Create : FileMode.CreateNew;

            await using var source =
                new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using var destination =
                new FileStream(destinationPath, createMode, FileAccess.Write, FileShare.None, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await source.CopyToAsync(destination);
        }

        public static void CopyFileSync(string sourcePath, string destinationPath)
        {
            File.Copy(sourcePath, destinationPath);
        }
    }
}
