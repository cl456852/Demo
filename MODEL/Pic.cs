using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MODEL
{
    public class Pic
    {
        int id;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        string name;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        string path;

        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        float length;

        public float Length
        {
            get { return length; }
            set { length = value; }
        }

        string md5;

        public string Md5
        {
            get { return md5; }
            set { md5 = value; }
        }

        string mark="";

        public string Mark
        {
            get { return mark; }
            set { mark = value; }
        }

        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(Path)) return "";
                string match = Util.Regex.Match(Path).Value;
                return string.IsNullOrEmpty(match) ? System.IO.Path.GetFileName(Path) : match;
            }
        }
    }
}
