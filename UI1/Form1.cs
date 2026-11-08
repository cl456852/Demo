using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MODEL;
using BLL;
using DAL;
using DB;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UI1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        List<MyFileInfo> list = new List<MyFileInfo>();
        MD5 md5 = MD5.Create();

        public void refresh()
        {
            list = FileBLL.getFileList();
            dataGridView1.DataSource = list;
            dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.RowCount - 20;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            //refresh();
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            MyFileInfo myFileInfo = new MyFileInfo();
            try
            {
                
                int row = e.RowIndex;
                myFileInfo.FileId = Convert.ToInt32(dataGridView1["FileId", row].Value);
                myFileInfo.FileName = dataGridView1["FileName", row].Value.ToString();
                myFileInfo.Directory = dataGridView1["Directory", row].Value.ToString();
                myFileInfo.DirectoryName = dataGridView1["DirectoryName", row].Value.ToString();
                myFileInfo.Extension = dataGridView1["Extension", row].Value.ToString();
                myFileInfo.LastAccessTime = dataGridView1["LastAccessTime", row].Value.ToString();
                myFileInfo.LastWriteTime = dataGridView1["LastWriteTime", row].Value.ToString();
                myFileInfo.Length = Convert.ToInt32(dataGridView1["length", row].Value);
                myFileInfo.Mark = dataGridView1["mark", row].Value.ToString();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            if (FileDAL.Update(myFileInfo) > 0)
                //MessageBox.Show("Seccess");
                ;
            else
                MessageBox.Show("Fail");
        }

        public void AppendLog(string message)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<string>(AppendLog), message);
                return;
            }
            string timeStamp = DateTime.Now.ToString("HH:mm:ss");
            string line = $"[{timeStamp}] {message}\r\n";
            if (txtLog.TextLength > 300000)
            {
                txtLog.Select(0, 50000);
                txtLog.SelectedText = "";
            }
            txtLog.AppendText(line);
            txtLog.ScrollToCaret();
        }

        private void buttonClearLog_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
        }

        private async void Insert_Click(object sender, EventArgs e)
        {
            string targetPath = textBox1.Text.Trim();
            if (string.IsNullOrEmpty(targetPath) || !Directory.Exists(targetPath))
            {
                MessageBox.Show("目标路径不存在或无法访问: " + targetPath, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Insert.Enabled = false;
            button1.Enabled = false;
            lblStatus.Text = "正在扫描...";
            AppendLog($"================ 开始扫描: {targetPath} ================");

            bool checkMd5AndDup = checkBoxMovePic.Checked;
            AppendLog($"【计算MD5并查重】模式: {(checkMd5AndDup ? "开启 (计算MD5并排重)" : "关闭 (图片不计算MD5、不查重，直接快速入库)")}");

            List<MyFileInfo> duplicateList = new List<MyFileInfo>();

            try
            {
                await Task.Run(() =>
                {
                    // 1. 图片处理：移动图片文件夹并入库到 pic 表
                    AppendLog("正在检测图片文件夹并移动...");
                    try
                    {
                        List<string> picList = FileBLL.movePic(targetPath, AppendLog);
                        AppendLog($"已找到并移动图片文件夹 {picList.Count} 个");

                        ArrayList duplicatePics = new ArrayList();
                        foreach (string path in picList)
                        {
                            duplicatePics.AddRange(processPic(path, checkMd5AndDup, AppendLog));
                        }

                        // 若开启了MD5查重，将发现的重复图片移到 duplicate 目录
                        if (checkMd5AndDup && duplicatePics.Count > 0)
                        {
                            AppendLog($"正在移动 {duplicatePics.Count} 张重复图片到 duplicate 文件夹...");
                            foreach (Pic pic in duplicatePics)
                            {
                                movePic(pic.Path);
                            }
                        }

                        // 清理 Pic 目录下的空文件夹
                        string cleanPath = targetPath.TrimEnd('\\');
                        string[] parts = cleanPath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 1)
                        {
                            string index = parts[1];
                            string picPath = Path.Combine(Path.GetPathRoot(targetPath), "pic" + index);
                            removeEmptyFolder(picPath);
                        }
                        removeEmptyFolder(targetPath);
                        AppendLog("图片处理与入库完毕。");
                    }
                    catch (Exception picEx)
                    {
                        AppendLog($"[警告] 图片处理过程异常: {picEx.Message}");
                    }

                    // 2. 流式安全扫描主目录中的视频/多媒体文件并实时入库 (已剔除图片格式，不再产生图片[入库]日志)
                    duplicateList = FileBLL.InsertFilesStream(
                        targetPath,
                        AppendLog,
                        status =>
                        {
                            if (this.InvokeRequired)
                            {
                                this.BeginInvoke(new Action(() => lblStatus.Text = status));
                            }
                            else
                            {
                                lblStatus.Text = status;
                            }
                        });
                });

                AppendLog("================ 扫描流程结束 ================");
                lblStatus.Text = "扫描完成";

                if (duplicateList != null && duplicateList.Count > 0)
                {
                    AppendLog($"发现 {duplicateList.Count} 个重复文件，准备弹出确认窗口...");
                    Form2 f2 = new Form2(duplicateList);
                    foreach (MyFileInfo myFileInfo in duplicateList)
                    {
                        if (myFileInfo.Length > 70)
                        {
                            try
                            {
                                moveFile(myFileInfo, ":\\duplicate\\");
                                AppendLog($"[移动重复大文件 >70M] {myFileInfo.FileName}");
                            }
                            catch (Exception ex)
                            {
                                AppendLog($"[移动文件失败] {myFileInfo.FileName}: {ex.Message}");
                            }
                        }
                    }
                    f2.Show();
                }
                else
                {
                    MessageBox.Show("扫描入库成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                refresh();
            }
            catch (Exception ex)
            {
                AppendLog($"[致命错误] 扫描过程发生异常: {ex.Message}");
                MessageBox.Show("扫描发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Insert.Enabled = true;
                button1.Enabled = true;
            }
        }

        private void removeEmptyFolder(String path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    string[] pathes = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
                    Array.Reverse(pathes);
                    foreach (string p in pathes)
                    {
                        if (Directory.Exists(p) && Directory.GetFileSystemEntries(p).Length == 0)
                            Directory.Delete(p, false);
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[清理空目录异常] {path}: {ex.Message}");
            }
        }

        private ArrayList processPic(string path, bool checkMd5AndDup, Action<string> logAction = null)
        {
            ArrayList duplicatePics = new ArrayList();
            try
            {
                String[] pathes = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                int total = pathes.Length;
                int current = 0;
                int insertedCount = 0;
                int duplicateCount = 0;

                foreach (string p in pathes)
                {
                    try
                    {
                        current++;
                        FileInfo fileInfo = new FileInfo(p);

                        Pic pic = new Pic();
                        pic.Name = Path.GetFileName(path);
                        pic.Length = fileInfo.Length / 1024;
                        pic.Path = p;
                        pic.Mark = "";

                        if (checkMd5AndDup)
                        {
                            pic.Md5 = GetMd5(p);
                            if (!FileDAL.CheckPic(pic))
                            {
                                FileDAL.InsertPic(pic);
                                insertedCount++;
                                logAction?.Invoke($"  [图片入库] ({current}/{total}) {fileInfo.Name} (MD5: {pic.Md5})");
                            }
                            else
                            {
                                duplicatePics.Add(pic);
                                duplicateCount++;
                                logAction?.Invoke($"  [重复图片] ({current}/{total}) {fileInfo.Name}");
                            }
                        }
                        else
                        {
                            pic.Md5 = "";
                            FileDAL.InsertPic(pic);
                            insertedCount++;
                            if (current % 20 == 0 || current == total || total <= 20)
                            {
                                logAction?.Invoke($"  [图片直接入库(免MD5/免查重)] ({current}/{total}) {fileInfo.Name}");
                            }
                        }
                    }
                    catch (Exception fileEx)
                    {
                        logAction?.Invoke($"  [单文件入库跳过] {Path.GetFileName(p)}: {fileEx.Message}");
                    }
                }

                logAction?.Invoke($"[图片目录处理完毕] {path}: 共 {total} 张, 新入库 {insertedCount} 张{(checkMd5AndDup ? $", 重复 {duplicateCount} 张" : "")}");
            }
            catch (Exception ex)
            {
                logAction?.Invoke($"[图片处理异常] {path}: {ex.Message}");
            }
            return duplicatePics;
        }

        public static void moveFile(MyFileInfo myFileInfo, string subDir)
        {
            try
            {
                if (string.IsNullOrEmpty(myFileInfo.DirectoryName)) return;
                string dirPrefix = myFileInfo.DirectoryName[0] + subDir;
                if (!Directory.Exists(dirPrefix))
                    Directory.CreateDirectory(dirPrefix);
                string newPath = Path.Combine(dirPrefix, myFileInfo.FileName);
                int retry = 0;
                while (File.Exists(newPath) && retry++ < 50)
                    newPath += "1";

                File.Move(Path.Combine(myFileInfo.DirectoryName, myFileInfo.FileName), newPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("moveFile error: " + ex.Message);
            }
        }

        private void movePic(string path)
        {
            try
            {
                string fileName = Path.GetFileName(path);
                string[] parts = path.Split('\\');
                string directoryName = parts.Length > 3 ? parts[3] : "unknown";

                string directory = path[0] + ":\\duplicate\\" + directoryName;
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                string newPath = Path.Combine(directory, Path.GetFileName(path));
                int retry = 0;
                while (File.Exists(newPath) && retry++ < 50)
                    newPath += "1";
                File.Move(path, newPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("movePic error: " + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFolderDialog openFolderDlg = new OpenFolderDialog())
            {
                if (openFolderDlg.ShowDialog() == DialogResult.OK)
                {
                    this.textBox1.Text = openFolderDlg.Path;
                    Console.WriteLine(this.textBox1.Text.Replace("\\", "\\\\"));
                }
            }
        }
        string dataClicked="" ;
        bool flag = false;
        private void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            List<MyFileInfo> sortList = new List<MyFileInfo>();
            sortList = (List<MyFileInfo>)dataGridView1.DataSource;
            string dataclick = dataGridView1.Columns[e.ColumnIndex].DataPropertyName;
            if (flag = true && dataclick == dataClicked)
            {
                sortList.Reverse();

            }
            else
            {
                sortList = FileBLL.Sort(list, dataclick);

            }
            dataGridView1.DataSource = sortList;
            dataGridView1.Refresh();
            flag = true;
            dataClicked = dataclick;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked && !textBox2.Text.Contains(" ")&&!textBox2.Text.Contains("-") &&(!FileBLL.IsEnCh(textBox2.Text)&&!FileBLL.IsNum(textBox2.Text)))
            {
                string[] id = FileBLL.GetId(textBox2.Text);
                textBox2.Text = id[0] + " " + id[1];
            }
            if (checkBox2.Checked)
            {
                string output = Regex.Replace(textBox2.Text, "(\\B[A-Z])", " $1");
                textBox2.Text = output;
            }
            if (textBox2.Text.Contains("`"))
            {
                textBox2.Text = "";
                refresh();
            }
            string[] searchStr = textBox2.Text.ToLower().Split(' ');
            bool flag = true;
            List<MyFileInfo> newList=new List<MyFileInfo>();
            for (int i = 0; i < list.Count; i++)
            {

                flag = true;
                for (int j = 0; j < searchStr.Length; j++)
                {
                    if (!(list[i].Directory.ToLower().Contains(searchStr[j]) || list[i].FileName.Substring(0, list[i].FileName.LastIndexOf('.') >= 0 ? list[i].FileName.LastIndexOf('.') : list[i].FileName.Length).ToLower().Contains(searchStr[j]) || list[i].Mark.ToLower().Contains(searchStr[j])))
                    {
                        flag = false;
                        break;
                        
                    }
                    
                    
                }
                if (flag)
                    newList.Add(list[i]);
                
            }
            this.dataGridView1.DataSource = newList;
            //list = newList;
            dataGridView1.Refresh();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            refresh();
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            textBox2.Focus();
        }


        public static string GetMd5(string pathName)
        {
            string strResult = "";
            string strHashData = "";
            byte[] arrbytHashValue;

            System.IO.FileStream oFileStream = null;

            System.Security.Cryptography.MD5CryptoServiceProvider oMD5Hasher = new System.Security.Cryptography.MD5CryptoServiceProvider();

            try
            {
                oFileStream = new System.IO.FileStream(pathName.Replace("\"", ""), System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);

                arrbytHashValue = oMD5Hasher.ComputeHash(oFileStream); //计算指定Stream 对象的哈希值

                oFileStream.Close();

                //由以连字符分隔的十六进制对构成的String，其中每一对表示value 中对应的元素；例如“F-2C-4A”

                strHashData = System.BitConverter.ToString(arrbytHashValue);

                //替换-
                strHashData = strHashData.Replace("-", "");

                strResult = strHashData;
            }

            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return strResult;
        }
        static string pattern = @"([A-Z]+[0-9A-Z]*-\d+)";
        private void button3_Click(object sender, EventArgs e)
        {
            long sizeLimit = 500L * 1024 * 1024; // 500MB
            
            TraverseDirectory(textBox1.Text, sizeLimit);
        }
        
        static void TraverseDirectory(string path, long sizeLimit)
        {
            try
            {
                // 遍历当前文件夹下的所有文件
                foreach (var file in Directory.EnumerateFiles(path))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        if (fi.Length > sizeLimit)
                        {
                            if(fi.Name.EndsWith("-C.mp4") || fi.Name.EndsWith("-UC.mp4")||(fi.Name.EndsWith(".mkv")&&fi.Name.Contains("[4K]")))
                                continue;
                            string vid=  Regex.Match(fi.Name, pattern).Value;
                            if (vid != string.Empty)
                            {
                                string fileName = FileDAL.Check4k(vid);
                                if (fileName != "" && fileName != fi.Name)
                                {
                                    MyFileInfo myFileInfo = new MyFileInfo();
                                    myFileInfo.FileName = fi.Name;
                                    myFileInfo.DirectoryName = fi.DirectoryName;
                                    moveFile(myFileInfo, ":\\duplicate\\4k\\");
                                    Console.WriteLine($"{fi.FullName} ({fi.Length / (1024.0 * 1024.0):F2} MB) {vid}");
                                    Console.WriteLine(fileName);
                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"无法读取文件信息: {file}, 错误: {ex.Message}");
                    }
                }

                // 递归遍历子文件夹
                foreach (var dir in Directory.EnumerateDirectories(path))
                {
                    TraverseDirectory(dir, sizeLimit);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"无法访问目录: {path}, 错误: {ex.Message}");
            }
        }
        
        private void button4_Click(object sender, EventArgs e)
        {
            string filePath = @"D:\abcd\output.txt";
            Regex regex = new Regex("[^\\/]+\\.jpg");
            try
            {
                // Reads one line at a time, keeping memory usage low
                foreach (string line in File.ReadLines(filePath))
                {

                    string name = regex.Match(line).Value;
                    if (!FileDAL.CheckLiguiPic(name))
                    {
                        Console.WriteLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
