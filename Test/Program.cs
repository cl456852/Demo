using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BLL;
using DAL;
using System.IO;
using MODEL;
using System.Reflection;
using DB;
using System.Data.SqlClient;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Program p = new Program();
            p.test();
            Console.Read();
            //p.checkCheck();
            //MyFileInfo tc = new MyFileInfo();
            //tc.Directory = "dddddddddd";
//            Type t = tc.GetType();
//           foreach (PropertyInfo pi in t.GetProperties())
//{
//    object value1 = pi.GetValue(tc, null);//用pi.GetValue获得值
//    string name = pi.Name;//获得属性的名字,后面就可以根据名字判断来进行些自己想要的操作
//    //获得属性的类型,进行判断然后进行以后的操作,例如判断获得的属性是整数
//    Console.WriteLine(name);
//}
           //Console.WriteLine( tc.GetType().GetProperty("Directory").GetValue(tc,null).ToString());
           //Console.WriteLine(tc.GetType().GetProperty("Directory").GetValue(tc,null).GetType());
           //Console.WriteLine(typeof(string));
           //Console.WriteLine(tc.GetType().GetProperty("Directory").GetValue(tc,null).GetType()==typeof(string));
           //Console.Read();
        }

        public void check()
        {
            string a = "fawef.awef";
            string name = a.Substring(0, a.LastIndexOf("."));
            Console.WriteLine(name);
            Console.ReadLine();
        }


        public void checkDirectory()
        {
            Console.WriteLine( Directory.Exists("h:\\37"));
        }

        public void test()
        {
            //string s = "中文にほんニホンABC심판";
            //foreach (char c in s)

            //{

            //    if (c >= 0x4E00 && c <= 0x9FA5) Console.WriteLine(c + "是汉字");

            //    else if (c >= 0x3040 && c <= 0x309F) Console.WriteLine(c + "是平假名");

            //    else if (c >= 0x30A0 && c <= 0x30FF) Console.WriteLine(c + "是片假名");

            //    else if(c>= 0x3100&&c<= 0x31BF||c>0xAC00 && c<=0xD7A3) Console.WriteLine(c + "是韩文");

            //}
            List<MyFileInfo> MyFileInfoList = new List<MyFileInfo>();
            SqlDataReader sdr=  DBHelper.SearchSql("select * from files where length>=100");
            while (sdr.Read())
            {

                MyFileInfo myFileInfo = new MyFileInfo();
                myFileInfo.Directory = sdr["directory"].ToString();
                myFileInfo.DirectoryName = sdr["directoryName"].ToString();
                myFileInfo.FileName = sdr["fileName"].ToString();
                myFileInfo.Extension = sdr["extension"].ToString();
                myFileInfo.LastAccessTime = sdr["lastAccessTime"].ToString();
                myFileInfo.LastWriteTime = sdr["lastWriteTime"].ToString();
                myFileInfo.FileId = Convert.ToInt32(sdr["fileId"]);
                myFileInfo.Length = Convert.ToDouble(sdr["length"]);
                myFileInfo.Mark = sdr["mark"].ToString();
                MyFileInfoList.Add(myFileInfo);

            }
            //foreach(MyFileInfo myFileInfo in MyFileInfoList)
            //{
            //    if()
            //}


        }
        bool CheckStringChar(string s)
        {
            foreach (char c in s)
            {
                if (c >= 0x4E00 && c <= 0x9FA5) return false;

                else if (c >= 0x3040 && c <= 0x309F) return false;

                else if (c >= 0x30A0 && c <= 0x30FF) return false;

                else if (c >= 0x3100 && c <= 0x31BF || c > 0xAC00 && c <= 0xD7A3) return false;
            }
            return true;
        }

        //public void Validate()
        //{
        //    FileDAL fd = new FileDAL();
        //    String[] path = Directory.GetFiles("D:\\temp", "*", SearchOption.TopDirectoryOnly);
        //    MyFileInfo fi = new MyFileInfo(path[0]);
        //    Console.WriteLine(fi.Directory.Root.Name);
        //    Console.WriteLine(fi.Directory.Name);
        //    Console.WriteLine(FileDAL.Validate(fi));
        //    Console.ReadLine();
        //}

        //public void checkCheck()
        //{
        //    MyFileInfo fi = new MyFileInfo("SBVD002.avi");
            
        //    Console.WriteLine( FileDAL.Check(fi));

        //    Console.Read();
        //}

    }
}
