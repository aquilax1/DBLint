using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.IO.Compression;
using Ionic.Zip;
using Ionic.Zlib;

namespace Util
{
    static public class Zipper
    {
        static public void ZipFiles(IEnumerable<KeyValuePair<string, string>> files, string outFile, bool UseEncryption)
        {
            if (File.Exists(outFile))
                File.Delete(outFile);
            using (ZipFile zip = new ZipFile())
            {
                if (UseEncryption)
                {
                    zip.Encryption = EncryptionAlgorithm.WinZipAes256;
                    zip.Password = "zdsmkfl,hjfdsgkh4r5t.uydfiu4r5kwdfy923ewDrosdfnqlw4%^euoidf:DSFguewjr234usadfur;tmreldbn$#%&";
                }
                zip.CompressionLevel = CompressionLevel.Level6;
                foreach (var kvPair in files)
                    zip.AddEntry(kvPair.Key, kvPair.Value);
                zip.Save(outFile);
            }
        }

        static public void ZipFilesAddTo(IEnumerable<KeyValuePair<string, string>> files, string outFile)
        {
            using (ZipFile zip = new ZipFile(outFile))
            {
                zip.CompressionLevel = CompressionLevel.Level6;
                foreach (var kvPair in files)
                    zip.AddEntry(kvPair.Key, kvPair.Value);
                zip.Save(outFile);
            }
        }
    }
}
