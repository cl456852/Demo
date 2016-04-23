using System;
using System.Collections;
using System.Text;
using System.Linq;
using MODEL;
using DB;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace DAL
{
    public class FileDAL
    {

        public static int cdNo;
        public static List<MyFileInfo> Insert(ArrayList list)
        {
            List<MyFileInfo> duplicateList = new List<MyFileInfo>();
            string sql1 = "insert cd values (getdate())";
            DBHelper.ExecuteSql(sql1);
            foreach (MyFileInfo fi in list)
            {
                if(Check(fi))
                {
                        duplicateList.Add(fi);
                 }
                 cdNo = DBHelper.ExecuteInsert_SP("dbo.sp_InsertCD", fi);

            }
            return duplicateList;

        }

        public static void InsertPic(Pic pic)
        {
            DBHelper.ExecuteInsertPic_SP(pic);
        }

        private static bool Check(MyFileInfo fi)
        {
            bool flag=false;
            string sql = "select * from files where [fileName] = '" + fi.FileName + "' and [length]= "+fi.Length +" and extension !='.vob'";
            //string sql = "select * from files where [fileName] = '" + fi.FileName + "' or [mark]='" + fi.Mark+"'";
            SqlDataReader sdr = DBHelper.SearchSql(sql);
            if (sdr.Read())
                flag = true;
            sdr.Close();
            sdr.Dispose();
            DBHelper.conn.Close();
            return flag;
        }

        public static int Update(MyFileInfo fi)
        {
            int res;
            string sql = "update files set fileName='"+fi.FileName+"',extension='"+fi.Extension+"', directoryName='"+fi.DirectoryName+"',directory='"+fi.Directory+"',length="+fi.Length+",lastAccessTime='"+fi.LastAccessTime+"',lastWriteTime='"+fi.LastWriteTime+"',mark='"+fi.Mark+"' where fileId="+fi.FileId;
            Console.WriteLine(sql);
            res= DBHelper.ExecuteSql(sql);
            return res;
        }


        public static List<MyFileInfo> selectMyFileInfo(string sortBy)
        {
            string sql="";
            if (sortBy != "")
            {
                sql = "select * from [cdfile],[files] where [cdfile].fileId=[files].fileId order by " + sortBy;
            
            if(sortBy=="FileId")
                
                 sql = "select * from [cdfile],[files] where [cdfile].fileId=[files].fileId order by files."+sortBy;
            }
            else
                sql = "select * from [cdfile],[files] where [cdfile].fileId=[files].fileId";
            List<MyFileInfo> MyFileInfoList=new List<MyFileInfo>();
           
            SqlDataReader sdr= DBHelper.SearchSql(sql);
            while (sdr.Read())
            {
                
                //FileInfo fileInfo=new FileInfo(sdr["directory"].ToString());
                
                MyFileInfo myFileInfo = new MyFileInfo();
                myFileInfo.Directory = sdr["directory"].ToString();
                myFileInfo.DirectoryName = sdr["directoryName"].ToString();
                myFileInfo.FileName = sdr["fileName"].ToString();
                myFileInfo.Extension = sdr["extension"].ToString();
                myFileInfo.LastAccessTime=sdr["lastAccessTime"].ToString();
                myFileInfo.LastWriteTime = sdr["lastWriteTime"].ToString();
                myFileInfo.FileId =Convert.ToInt32( sdr["fileId"]);
                myFileInfo.Length = Convert.ToDouble(sdr["length"]);
                myFileInfo.Mark = sdr["mark"].ToString();
                myFileInfo.CdId =Convert.ToInt32( sdr["cdId"]);
                MyFileInfoList.Add(myFileInfo);

            }
            sdr.Close();
            sdr.Dispose();
            DBHelper.conn.Close();
            //DBHelper.conn.Dispose();
            return MyFileInfoList;
        }

        public static int getMaxCDID()
        {
            int maxcd;
            string sql = "select max(cdID) from [cd]";
            SqlDataReader sdr= DBHelper.SearchSql(sql);
            sdr.Read();
            maxcd = (int)sdr[0];
            sdr.Close();
            sdr.Dispose();
            DBHelper.conn.Close();
            return maxcd;
        }

        public static bool CheckPic(Pic pic)
        {
            bool flag = false;
            string sql = "select * from pic where md5='"+pic.Md5+"'";
            SqlDataReader sdr = DBHelper.SearchSql(sql);
            if (sdr.Read())
                flag = true;
            sdr.Close();
            sdr.Dispose();
            DBHelper.conn.Close();
            return flag;
        }
    }
}
