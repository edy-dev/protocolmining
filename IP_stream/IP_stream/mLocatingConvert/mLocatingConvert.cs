﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Data.SqlClient;
using IP_stream.Linq;
using System.Threading;
using System.Windows.Forms;

namespace IP_stream
{
    class mLocatingConvert : streamType
    {
        #region
        //远程取总数
        #endregion

        private  DataClasses1DataContext localdb = new DataClasses1DataContext(streamType.LocalConnString);

        private ciType _ciType;
        private imeiTypeClass _imeiTypeClass;

        private uriType _uriType;
        private portType _portType;
        private responseType _responseType;
        private protocolType _protocolType;
        private ipType _ipType;

        private string serviceIP = null;
        private string response = null;
        private string protocol = null;

        //private decimal _messageid = 0;
        //private decimal messageid { get { return _messageid; } set { _messageid = value; } }
        //private static decimal messageid = 0;

        public mLocatingConvert()
        {
            localdb.CommandTimeout = 0;
            _ciType = new ciType(false);
            _imeiTypeClass = new imeiTypeClass(false);
            _uriType = new uriType();
            _portType = new portType();
            _responseType = new responseType();
            _protocolType = new protocolType();
            _ipType = new ipType();
            //--删除
            //alter   table   你的表   drop   constraint   主键名 
        }


        public IEnumerable<mLocatingType> mLocatingTypeLength(int filenum)
        {
            localdb = new DataClasses1DataContext(streamType.LocalConnString);
            localdb.CommandTimeout = 0;

            var ipstreamfilnum=localdb.IP_stream.Where(e => e.FileNum == filenum);

            foreach (var p in ipstreamfilnum)
            {
                mLocatingType down = new mLocatingType();

                //locating 过程中，pk冲突
                //messageid = messageid + 1;
                //down.MLocatingType_id = messageid;

                down.fileNum = p.FileNum;
                down.frame = p.PacketNum;
                down.bvci = p.FileNum + "-" + p.bvci;
                down.tlli = p.tlli;
                down.responseType = p.http_type;
                down.mLen = p.ip_length;

                if (p.link == "Down")
                {
                    down.portType = p.tcp_s + p.udp_s;
                    serviceIP = p.ip_s;
                    response = down.fileNum + "-" + down.tlli + "-" + p.tcp_d + "-" + p.tcp_s;
                }
                else
                {
                    down.portType = p.tcp_d + p.udp_d;
                    serviceIP = p.ip_d;
                    response = down.fileNum + "-" + down.tlli + "-" + p.tcp_s + "-" + p.tcp_d;
                }

                //var ci = _ciType.CiTypeCollection[down.fileNum + "-" + down.bvci];
                if (_ciType.CiTypeCollection.ContainsKey(down.bvci))
                {
                    var ci = _ciType.CiTypeCollection[down.bvci];
                    down.lacCI = ci.lacCi;
                    down.ciCoverAllocPDCH = ci.ciAllocPDCH;
                    down.ciCoverUsePDCH = ci.ciUsePDCH;
                }


                if (_imeiTypeClass.MsImeiCollection.ContainsKey(down.fileNum + "-" + down.tlli))
                {
                    var imei = _imeiTypeClass.MsImeiCollection[down.fileNum + "-" + down.tlli];
                    down.imsi = imei.imsi;
                    down.imei = imei.imei;
                    down.msimeiType = imei.imeitype;
                    down.msimeiClass = imei.imeiclass;
                }

                //初始化其值
                protocol = null;

                //协议识别
                if (p.mmse != null) protocol = "MMSE";
                if (p.rtsp_type != null) protocol = "rtsp";
                if (p.smtp_type != null) protocol = "smtp";
                if (p.bittorrent != null) protocol = "BitTorrent";
                if (p.edonkey != null) protocol = "eDonkey";
                if (p.oicqVersion != null) protocol = "oicq";
                down.protocolType = protocol;
                if (down.protocolType != null)
                    down.trafficType = _protocolType.ConvertProtocol2trType(down.protocolType);

                //端口识别

                if (down.portType != null && down.trafficType == null)
                    down.trafficType = _portType.ConvertPort2trType(down.portType);

                //ip识别
                if (serviceIP != null && down.trafficType == null)
                    down.trafficType = _ipType.ConvertIP2trType(serviceIP);

                //reponse识别流媒体业务
                if (down.trafficType == null)
                    if (down.responseType != null)
                        down.trafficType = _responseType.ConvertResponse2trType(down.responseType);

                //uri识别, uri中的裸ip识别
                var uri = _uriType.mUriCollection[response].Where(e => e.uri != null).FirstOrDefault();
                if (uri != null)
                {
                    down.uriType = uri.uri;
                    if (down.trafficType == null)
                        if (down.uriType != null)
                            down.trafficType = _ipType.ConvertIP2trType(down.uriType);
                    if (down.trafficType == null)
                        down.trafficType = uri.uriStreamType;
                }

                down.responseType += "-" + serviceIP;

                //上述都不能识别时，放入其他

                if (down.trafficType == null)
                    down.trafficType = "OtherCategory";

                yield return down;
            }
        }
        public void SendOrders(mLocatingConvert ml, int filenum)
        {
            //Stopwatch sw = new Stopwatch();
            //sw.Start();
            using (SqlConnection con = new SqlConnection(streamType.LocalConnString))
            {
                con.Open();
                using (SqlTransaction tran = con.BeginTransaction())
                {
                    var newOrders = ml.mLocatingTypeLength(filenum);
                    SqlBulkCopy bc = new SqlBulkCopy(con,
                      //SqlBulkCopyOptions.CheckConstraints |
                      //SqlBulkCopyOptions.FireTriggers |
                      SqlBulkCopyOptions.KeepNulls, tran);
                    bc.BulkCopyTimeout = 36000;
                    bc.BatchSize = 10000;
                    bc.DestinationTableName = "mLocatingType";
                    bc.WriteToServer(newOrders.AsDataReader());
                    tran.Commit();
                }
                con.Close();
            }
            Thread.Sleep(1); GC.Collect(); GC.Collect(); Application.DoEvents();
            //sw.Stop();
            //localdbageBox.Show(sw.Elapsed.TotalSeconds.ToString());
            //using (DataClasses1DataContext localdb = new DataClasses1DataContext(streamType.LocalConnString))
            //    filenum = localdb.mLocatingType.Count();
            //MessageBox.Show(filenum.ToString() + "---" + sw.Elapsed.TotalSeconds.ToString() + "---");
        }
    }
}
