using System;
using System.Runtime.InteropServices;


namespace DestinyCustomBlocks
{
    static class NativeMethods
    {
        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        public static bool OpenFileDialog(string title, out string filename)
        {
            OpenFileName ofn = new OpenFileName();
            ofn.structSize = Marshal.SizeOf(ofn);
            ofn.filter = "All Files\0*.*\0\0";
            ofn.file = new string(new char[256]);
            ofn.maxFile = ofn.file.Length;
            ofn.fileTitle = new string(new char[64]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
            ofn.initialDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            ofn.title = title;
            ofn.defExt = "PNG";
            ofn.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008;//OFN_EXPLORER|OFN_FILEMUSTEXIST|OFN_PATHMUSTEXIST|OFN_NOCHANGEDIR
            if (GetOpenFileName(ofn))
            {
                filename = ofn.file;
                return true;
            }
            filename = null;
            return false;
        }
    }
}