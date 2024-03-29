﻿using System;
using System.Data.Linq;
using System.Xml.Linq;
using System.Windows.Forms;

namespace IP_stream
{
    class streamType
    {
        public static string configXmlPath = Application.StartupPath + @"\connStringConfig.xml";
        public static string streamTypeXmlPath = Application.StartupPath + @"\streamTypeDefine.xml";
        public static string imeiTypeXmlPath = Application.StartupPath + @"\imeiFactoryType.xml";
        public static string imeiTypeFile = Application.StartupPath + @"\imeiType.csv";

        public XElement dataConfig = XElement.Load(streamTypeXmlPath);
        //本地库，小区类型表，IMEI类型表
        public static string LocalConnString;
        //= @"Data Source=.\SQLEXPRESS;Initial Catalog=ip_stream;Integrated Security=True";
        //远程库，Gb数据或者Gn数据
        //public static string RemoteConnString;
        //生成的库的位置，3张表，，，，，TLLI-IMEI-IMSI表，CI-BVCI表  这2张可以插入本地，也可以插入远程库，M-TRAFFIC业务类型表
        //public static string InsertConnString;
        //进度条
        public delegate void SendPMessage(int i);
        public static event SendPMessage sendPEvent;
        public static void DoSendPMessage(int i)
        {
            sendPEvent(i);
        }
    }
    static class DataContextExtensions
    {
        public static ITable GetTableByName(this DataContext context, string tableName)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (tableName == null)
            {
                throw new ArgumentNullException("tableName");
            }
            //MyDBContext db = new MyDBContext();
            //Type t = db.GetType();
            //PropertyInfo p = t.GetProperty("Authors");
            //var table = p.GetValue(db, null);
            return (ITable)context.GetType().GetProperty(tableName).GetValue(context, null);
        }
    }
}
