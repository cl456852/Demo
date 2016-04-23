using System;
using System.Collections.Generic;
using System.Text;
using DAL;
using System.Linq;
using System.IO;
using System.Collections;
using MODEL;

namespace BLL
{
    public class FileBLL
    {
        public static List<MyFileInfo> InsertFiles(string directoryStr)
        {
      
            String[] path = Directory.GetFiles(directoryStr, "*", SearchOption.AllDirectories);
            ArrayList fis = new ArrayList();
            String lowerCase;
            String[] extensions = { ".avi", ".wmv", ".rmvb", ".iso", ".rm", ".afs", ".flv", ".pdf", ".vob", ".rar", ".mpg", ".mpeg", ".mds", ".jpg", ".bmp", ".jpeg", ".mkv", ".dat", ".tif", ".mp4", "zip", ".mov", ".mpe", ".dat", ".bik", ".asx", ".wvx", ".mpa", ".bt!", ".m4v", ".divx", ".asf", ".nrg", ".ogm", ".mdf", ".md0", ".md1", ".md2", ".md3", ".md4", ".m4v", ".ogv", ".exe", ".rar", ".msi", ".7z", ".r00", ".m2ts" };

            foreach (String p in path)
            {
                FileInfo fileInfo = new FileInfo(p);
                
                string sub = fileInfo.Extension;
                lowerCase = sub.ToLower();
                Console.WriteLine(fileInfo.Extension);

              
                if (extensions.Contains(lowerCase))
                {

                    MyFileInfo myFileInfo = new MyFileInfo();
                    myFileInfo.FileName = fileInfo.Name.Replace("'","''");
                    myFileInfo.Directory = fileInfo.Directory.Name;
                    myFileInfo.DirectoryName = fileInfo.DirectoryName;
                    myFileInfo.Extension = fileInfo.Extension;
                    myFileInfo.LastAccessTime = fileInfo.LastAccessTime.ToString();
                    myFileInfo.LastWriteTime = fileInfo.LastWriteTime.ToString();
                    myFileInfo.Length = (double)((int)(fileInfo.Length / 1024.0 / 1024.0 * 100)) / 100;
                       
                    myFileInfo.Mark = "";
                    fis.Add(myFileInfo);
                }
            }

            return FileDAL.Insert(fis);
        }

        public static List<MyFileInfo> getFileList()
        {
            return FileDAL.selectMyFileInfo("");
        }

        public static List<MyFileInfo> Sort(List<MyFileInfo> oldList, string sortBy)
        {
            List<MyFileInfo> newList = new List<MyFileInfo>();
             if (oldList[0].GetType().GetProperty(sortBy).GetValue(oldList[0], null).GetType() == typeof(string))
                newList = StringSort(oldList, sortBy);
            else
                newList = IntSort(oldList, sortBy);
            return newList;
        }

        private static List<MyFileInfo> IntSort(List<MyFileInfo> oldList, string sortBy)
        {
            List<MyFileInfo> newList = new List<MyFileInfo>();
            MyFileInfo minMyFileInfo = oldList[0];
            while (oldList.Count > 1)
            {
                for (int i = 0; i < oldList.Count - 1; i++)
                {
                    //Console.WriteLine("for");
                    if (Convert.ToInt32(minMyFileInfo.GetType().GetProperty(sortBy).GetValue(minMyFileInfo, null)) > Convert.ToInt32(oldList[i+1].GetType().GetProperty(sortBy).GetValue(oldList[i+1], null)))
                    {
                        minMyFileInfo = oldList[i + 1];
                       
                    }
                    
                }
                newList.Add(minMyFileInfo);
                oldList.Remove(minMyFileInfo);
                minMyFileInfo = oldList[0];

            }
            newList.Add(oldList[0]);
            return newList;
        }

        private static List<MyFileInfo> StringSort(List<MyFileInfo> oldList, string sortBy)
        {
            List<MyFileInfo> newList = new List<MyFileInfo>();
            MyFileInfo minMyFileInfo = oldList[0];
            while (oldList.Count > 1)
            {
                for (int i = 0; i < oldList.Count - 1; i++)
                {
                    //Console.WriteLine("for");
                    if (minMyFileInfo.GetType().GetProperty(sortBy).GetValue(minMyFileInfo, null).ToString().CompareTo(oldList[i+1].GetType().GetProperty(sortBy).GetValue(oldList[i+1], null).ToString())>0)
                    {
                        minMyFileInfo = oldList[i + 1];
                        
                    }
      
                }
                newList.Add(minMyFileInfo);
                oldList.Remove(minMyFileInfo);
                minMyFileInfo = oldList[0];
            }
            newList.Add(oldList[0]);

            return newList;
        }

        public static List<string> movePic(string path)
        {
            List<string> picList = new List<string>();
            ArrayList moveList = new ArrayList();
            DirectoryInfo TheFolder = new DirectoryInfo(path);
            bool isPicFolder;
            foreach (DirectoryInfo NextFolder in TheFolder.GetDirectories("*", SearchOption.TopDirectoryOnly))
            {
                isPicFolder = true;
                FileInfo[] fileInfos = NextFolder.GetFiles("*", SearchOption.AllDirectories);
                foreach (FileInfo fileInfo in fileInfos)
                {
                    if (fileInfo.Length / 1024 / 2024 > 25)
                    {
                        isPicFolder = false;
                        break;
                    }

                }
                if (isPicFolder)
                {
                    moveList.Add(NextFolder);
                }

            }
            
            foreach (DirectoryInfo info in moveList)
            {
                string dateDirectory = path.Split(new string[] { "\\" }, StringSplitOptions.None)[2];
                string indexDirectory = path.Split(new string[] { "\\" }, StringSplitOptions.None)[1];
                string newPath = Path.Combine(Path.Combine(Path.GetPathRoot(path), "pic" + indexDirectory), dateDirectory).ToString();
                if (!Directory.Exists(newPath))
                    Directory.CreateDirectory(newPath);
                newPath= Path.Combine(newPath,info.ToString());
                picList.Add(newPath);
                while (true)
                {
                    try
                    {
                        Directory.Move(info.FullName, newPath);
                        break;
                    }
                    catch (Exception e)
                    {
                        newPath += "1";
                    }
                    
                }
            }
            return picList;
        }

    }


}