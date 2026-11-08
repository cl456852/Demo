using System;
using System.Collections.Generic;
using System.Text;
using DAL;
using System.Linq;
using System.IO;
using System.Collections;
using MODEL;
using System.Text.RegularExpressions;

namespace BLL
{
    public class FileBLL
    {
        public static List<MyFileInfo> InsertFilesStream(string directoryStr, Action<string> logAction = null, Action<string> statusAction = null)
        {
            directoryStr = directoryStr.Trim().TrimEnd('\\');
            List<MyFileInfo> duplicateList = new List<MyFileInfo>();
            string[] extensions = { 
                ".avi", ".wmv", ".rmvb", ".iso", ".rm", ".afs", ".flv", ".pdf", ".vob", 
                ".rar", ".mpg", ".mpeg", ".mds", ".mkv", ".dat", 
                ".mp4", ".zip", "zip", ".mov", ".mpe", ".bik", ".asx", ".wvx", ".mpa", 
                ".bt!", ".m4v", ".divx", ".asf", ".nrg", ".ogm", ".mdf", ".md0", ".md1", 
                ".md2", ".md3", ".md4", ".ogv", ".exe", ".msi", ".7z", ".r00", ".m2ts",
                ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff"
            };
            HashSet<string> extSet = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);

            Queue<string> queue = new Queue<string>();
            queue.Enqueue(directoryStr);

            int dirCount = 0;
            int matchedCount = 0;
            int insertedCount = 0;
            int duplicateCount = 0;

            logAction?.Invoke($"开始流式扫描路径: {directoryStr}");

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();

                // 1. 获取子目录加入队列（递归深入）
                try
                {
                    foreach (string subDir in Directory.EnumerateDirectories(current))
                    {
                        queue.Enqueue(subDir);
                    }
                }
                catch (UnauthorizedAccessException uex)
                {
                    logAction?.Invoke($"  [跳过无权限子目录] {current} - {uex.Message}");
                }
                catch (PathTooLongException)
                {
                    logAction?.Invoke($"  [跳过超长路径子目录] {current}");
                }
                catch (Exception ex)
                {
                    logAction?.Invoke($"  [跳过无法访问子目录] {current} - {ex.Message}");
                }

                // 2. 检查当前目录是否为纯图片目录（没有视频，只有图片与小文件）
                // 若为纯图片目录，由 pic 逻辑管理入库 pic 表，跳过入库 files 表
                DirectoryInfo curDirInfo = new DirectoryInfo(current);
                if (IsPicFolder(curDirInfo))
                {
                    logAction?.Invoke($"  [跳过纯图片目录(由pic表管理)] {current}");
                    continue;
                }

                // 3. 当前目录有视频（或者包含视频的影视目录）：
                // 该文件夹下的所有文件（包括视频、压缩包以及附带的海报/图片）都入库 files 表！
                dirCount++;
                statusAction?.Invoke($"正在扫描: {current} | 已匹配: {matchedCount} | 入库: {insertedCount} | 重复: {duplicateCount}");
                logAction?.Invoke($"[正在扫描目录(含视频)] {current}");

                // 4. 获取当前目录文件并立即入库 files 表
                IEnumerable<string> files = null;
                try
                {
                    files = Directory.EnumerateFiles(current);
                }
                catch (Exception ex)
                {
                    logAction?.Invoke($"  [跳过无法读取文件列表] {current} - {ex.Message}");
                    continue;
                }

                foreach (string file in files)
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        string ext = fileInfo.Extension;
                        if (!string.IsNullOrEmpty(ext) && extSet.Contains(ext.ToLower()))
                        {
                            matchedCount++;
                            MyFileInfo myFileInfo = new MyFileInfo();
                            myFileInfo.FileName = fileInfo.Name.Replace("'", "''");
                            myFileInfo.Directory = fileInfo.Directory != null ? fileInfo.Directory.Name : "";
                            myFileInfo.DirectoryName = fileInfo.DirectoryName ?? "";
                            myFileInfo.Extension = fileInfo.Extension;
                            myFileInfo.LastAccessTime = fileInfo.LastAccessTime.ToString();
                            myFileInfo.LastWriteTime = fileInfo.LastWriteTime.ToString();
                            myFileInfo.Length = (double)((int)(fileInfo.Length / 1024.0 / 1024.0 * 100)) / 100;
                            myFileInfo.Mark = "";

                            bool isDuplicate = FileDAL.InsertSingle(myFileInfo);
                            if (isDuplicate)
                            {
                                duplicateList.Add(myFileInfo);
                                duplicateCount++;
                                logAction?.Invoke($"  -> [重复] {fileInfo.Name} ({myFileInfo.Length:F2} MB)");
                            }
                            else
                            {
                                insertedCount++;
                                logAction?.Invoke($"  -> [入库] {fileInfo.Name} ({myFileInfo.Length:F2} MB)");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logAction?.Invoke($"  [文件处理异常] {file} - {ex.Message}");
                    }
                }
            }

            logAction?.Invoke($"扫描完成！遍历目录数: {dirCount}, 匹配文件: {matchedCount}, 新入库: {insertedCount}, 重复: {duplicateCount}");
            statusAction?.Invoke($"扫描完成 | 目录: {dirCount} | 匹配: {matchedCount} | 入库: {insertedCount} | 重复: {duplicateCount}");

            return duplicateList;
        }

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

        private static readonly HashSet<string> VideoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".avi", ".wmv", ".rmvb", ".iso", ".rm", ".flv", ".mov",
            ".m4v", ".mpg", ".mpeg", ".vob", ".ts", ".m2ts", ".divx", ".asf", ".webm",
            ".bik", ".dat", ".ogv", ".tp", ".3gp"
        };

        private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".tif", ".tiff"
        };

        public static string GetPicTargetPath(string sourcePath)
        {
            sourcePath = Path.GetFullPath(sourcePath).TrimEnd('\\');
            string root = Path.GetPathRoot(sourcePath);
            string rel = sourcePath.Substring(root.Length);
            string[] parts = rel.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                if (!parts[0].StartsWith("pic", StringComparison.OrdinalIgnoreCase))
                {
                    parts[0] = "pic" + parts[0];
                }
                return Path.Combine(root, string.Join("\\", parts));
            }
            return sourcePath;
        }

        public static bool IsPicFolder(DirectoryInfo dir)
        {
            try
            {
                if (!dir.Exists) return false;
                FileInfo[] files = dir.GetFiles("*", SearchOption.AllDirectories);
                if (files.Length == 0) return false;

                bool hasImage = false;
                foreach (FileInfo fi in files)
                {
                    string ext = fi.Extension;
                    if (!string.IsNullOrEmpty(ext) && VideoExtensions.Contains(ext))
                        return false;

                    if (fi.Length > 25L * 1024 * 1024)
                        return false;

                    if (!string.IsNullOrEmpty(ext) && ImageExtensions.Contains(ext))
                        hasImage = true;
                }

                return hasImage;
            }
            catch
            {
                return false;
            }
        }

        private static string MoveDirectorySafe(string sourceDir, string targetDir, Action<string> logAction = null)
        {
            try
            {
                if (string.Equals(sourceDir, targetDir, StringComparison.OrdinalIgnoreCase))
                {
                    return sourceDir;
                }

                string parent = Path.GetDirectoryName(targetDir);
                if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                string dest = targetDir;
                int retry = 0;
                while (Directory.Exists(dest) && retry++ < 50)
                {
                    dest = targetDir + "_" + retry;
                }

                Directory.Move(sourceDir, dest);
                logAction?.Invoke($"[移动图片文件夹] {sourceDir} -> {dest}");
                return dest;
            }
            catch (Exception ex)
            {
                logAction?.Invoke($"[移动文件夹失败] {sourceDir} -> {targetDir}: {ex.Message}");
                return null;
            }
        }

        public static List<string> movePic(string path, Action<string> logAction = null)
        {
            path = path.TrimEnd('\\');
            List<string> picList = new List<string>();
            DirectoryInfo rootDir = new DirectoryInfo(path);
            if (!rootDir.Exists)
            {
                logAction?.Invoke($"[错误] 目录不存在: {path}");
                return picList;
            }

            // 情况1：如果扫描的目录本身就是一个图片文件夹（没有视频，只有图片或小文件）
            if (IsPicFolder(rootDir))
            {
                string targetPath = GetPicTargetPath(rootDir.FullName);
                string movedPath = MoveDirectorySafe(rootDir.FullName, targetPath, logAction);
                if (!string.IsNullOrEmpty(movedPath))
                {
                    picList.Add(movedPath);
                }
                return picList;
            }

            // 情况2：如果扫描的是父目录，遍历子目录查找图片文件夹
            try
            {
                foreach (DirectoryInfo subDir in rootDir.GetDirectories("*", SearchOption.TopDirectoryOnly))
                {
                    if (IsPicFolder(subDir))
                    {
                        string targetPath = GetPicTargetPath(subDir.FullName);
                        string movedPath = MoveDirectorySafe(subDir.FullName, targetPath, logAction);
                        if (!string.IsNullOrEmpty(movedPath))
                        {
                            picList.Add(movedPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logAction?.Invoke($"[图片检测异常] 读取子目录失败: {path} - {ex.Message}");
            }

            return picList;
        }


        public static string[] GetId(string id)
        {
            Regex reg1 = new Regex("[a-z]|[A-Z]");
            string letter = "";
            string number = "";
            bool isEndofLetter = false;
            for (int i = 0; i < id.Length; i++)                        //修改   对于出现KIDM235A  KIDM235B
                if (reg1.IsMatch(id[i].ToString()))
                {
                    if (isEndofLetter)
                        break;
                    else
                        letter += id[i];
                }
                else
                {
                    number += id[i];
                    isEndofLetter = true;
                }

            string[] searchStr = { letter, number };
            return searchStr;
        }

        public static bool IsEnCh(string input)
        {
            string pattern = @"^[A-Za-z]+$";
            Regex regex = new Regex(pattern);
            return regex.IsMatch(input);
        }

        public static bool IsNum(string input)
        {
            string pattern = @"^[0-9]+$";
            Regex regex = new Regex(pattern);
            return regex.IsMatch(input);
        }

    }


}