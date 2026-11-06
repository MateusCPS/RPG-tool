#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;

static class WindowsFileDialog
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    class OpenFileName
    {
        public int structSize = Marshal.SizeOf(typeof(OpenFileName));
        public IntPtr owner = IntPtr.Zero;
        public IntPtr instance = IntPtr.Zero;
        public string filter;
        public string customFilter;
        public int maxCustFilter;
        public int filterIndex;
        public string file;
        public int maxFile;
        public string fileTitle;
        public int maxFileTitle;
        public string initialDir;
        public string title;
        public int flags;
        public short fileOffset;
        public short fileExtension;
        public string defaultExt;
        public IntPtr customData;
        public IntPtr hook;
        public string templateName;
        public IntPtr reservedPtr;
        public int reservedInt;
        public int flagsEx;
    }
    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode)]
    static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

    const int OFN_EXPLORER = 0x00080000;
    const int OFN_FILEMUSTEXIST = 0x00001000;
    const int OFN_PATHMUSTEXIST = 0x00000800;
    const int OFN_NOCHANGEDIR = 0x00000008;

    public static string OpenFile(string title)
    {
        var ofn = new OpenFileName
        {
            filter = "Imagens (*.png, *.jpg)\0*.png;*.jpg;*.jpeg\0\0",
            file = new string('\0', 1024),
            maxFile = 1024,
            fileTitle = new string('\0', 256),
            maxFileTitle = 256,
            title = title,
            flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR
        };

        return GetOpenFileName(ofn) ? ofn.file.TrimEnd('\0') : null;
    }
}
#endif