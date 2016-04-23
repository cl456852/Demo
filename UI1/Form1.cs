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

        private void Insert_Click(object sender, EventArgs e)
        {
            List<string> picList= FileBLL.movePic(textBox1.Text);
            ArrayList duplicatePics = new ArrayList(); ;
            foreach (string path in picList)
            {
                duplicatePics.AddRange( processPic(path));
            }
            foreach (Pic pic in duplicatePics)
            {
               
                    movePic(pic.Path);
            }
            string index = textBox1.Text.Split('\\')[1];
            string picPath = Path.GetPathRoot(textBox1.Text)+ "Pic" + index;
            removeEmptyFolder(picPath);
            List<MyFileInfo> duplicateList = new List<MyFileInfo>();
            duplicateList= FileBLL.InsertFiles(textBox1.Text.Replace("\\","\\\\"));
            if (duplicateList.Capacity > 0)
            {

                Form2 f2 = new Form2(duplicateList);
                foreach (MyFileInfo myFileInfo in duplicateList)
                {
                    if (myFileInfo.Length > 70)
                        moveFile(myFileInfo);
                }
                f2.Show();
            }
            else
                MessageBox.Show("seccuss CDID: "+FileDAL.getMaxCDID());
            refresh();
        }

        private void removeEmptyFolder(String path)
        {
            if (Directory.Exists(path))
            {
                String[] pathes = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
                foreach (string p in pathes)
                {
                    if (Directory.Exists(p)&& Directory.GetFiles(p, "*", SearchOption.AllDirectories).Length == 0)
                        Directory.Delete(p,true);
                }
            }
        }

        private ArrayList processPic(string path)
        {
            ArrayList duplicatePics = new ArrayList();
            String[] pathes = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach(string p in pathes)
            {
                FileInfo fileInfo=new FileInfo(p);

                Pic pic=new Pic();
                pic.Md5 = GetMd5(p);
                pic.Name=Path.GetFileName(path);
                pic.Length = fileInfo.Length/1024;
                pic.Path = p;

                if(!FileDAL.CheckPic(pic))
                {
                    FileDAL.InsertPic(pic);
                }
                else
                {
                    duplicatePics.Add(pic);
                }

            }
            return duplicatePics;
        }

        private void moveFile(MyFileInfo myFileInfo)
        {
            if(!Directory.Exists(myFileInfo.DirectoryName[0] + ":\\duplicate\\"))
                Directory.CreateDirectory(myFileInfo.DirectoryName[0] + ":\\duplicate\\");
            string newPath = Path.Combine(myFileInfo.DirectoryName[0] + ":\\duplicate\\", myFileInfo.FileName);
            while (File.Exists(newPath))
                newPath += "1";
           
            File.Move(Path.Combine(myFileInfo.DirectoryName, myFileInfo.FileName), newPath);
            
        
                
        }

        private void movePic(string path)
        {
            string fileName = Path.GetFileName(path);
            string directoryName = path.Split('\\')[3];

            string directory = path[0] + ":\\duplicate\\"+directoryName;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            string newPath = Path.Combine(directory, Path.GetFileName(path));
            while (File.Exists(newPath))
                newPath += "1";
            File.Move(path, newPath);

        }
        private void button1_Click(object sender, EventArgs e)
        {

            using (OpenFolderDialog openFolderDlg = new OpenFolderDialog())
            {
                if (openFolderDlg.ShowDialog() == DialogResult.OK)
                {
                    this.textBox1.Text = openFolderDlg.Path;
                    Console.WriteLine(this.textBox1.Text.Replace("\\","\\\\"));
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
            if (textBox2.Text.Contains("`"))
            {
                textBox2.Text = "";
                DBHelper.connstr = this.textBox3.Text;
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
            DBHelper.connstr = this.textBox3.Text;
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

    }
}
