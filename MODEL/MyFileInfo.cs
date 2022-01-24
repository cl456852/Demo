using System;
using System.Collections;
using System.Text;

using System.IO;


namespace MODEL
{
    public class MyFileInfo
    {
        string fileName;

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }
        double length;

        public double Length
        {
            get { return length; }
            set { length = value; }
        }
        string directoryName;

        public string DirectoryName
        {
            get { return directoryName; }
            set { directoryName = value; }
        }

        private string lastWriteTime;

        public string LastWriteTime
        {
            get { return lastWriteTime; }
            set { lastWriteTime = value; }
        }
        int fileId;

        public int FileId
        {
            get { return fileId; }
            set { fileId = value; }
        }
        int cdId;

        public int CdId
        {
        get { return cdId; }
            set { cdId = value; }
        }

        string directory;

        public string Directory
        {
            get { return directory; }
            set { directory = value; }
        }




        private string lastAccessTime;

        public string LastAccessTime
        {
            get { return lastAccessTime; }
            set { lastAccessTime = value; }
        }




        string extension;

        public string Extension
        {
            get { return extension; }
            set { extension = value; }
        }

        string mark;

        public string Mark
        {
            get { return mark; }
            set { mark = value; }
        }




    }
}
