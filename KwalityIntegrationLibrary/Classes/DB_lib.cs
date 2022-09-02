using Focus.Common.DataStructs;
using Focus.Conn;
using Focus.DatabaseFactory;
using Focus.TranSettings.DataStructs;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Xml;

namespace KwalityIntegrationLibrary.Classes
{
    public class DB_lib
    {
        string error = "";
        Log_lib _log = new Log_lib();

        public  DataSet getFn(string Operation,int CompId)
        {
            string sql = "";
            sql = $@"
                exec pCore_CommonSp @Operation={Operation}";

            DataSet dst = GetData(sql,CompId, ref error);
            return dst;

        }
        public  int setFn(string Operation, string DocNo,int CompId)
        {
            string sql = "";
            sql = $@"
                exec pCore_CommonSp @Operation={Operation},@p1='{DocNo}'";

            int f = GetExecute(sql, CompId,ref error);
            return f;

        }
        public string getDBServer_Details(string tagname)
        {
            XmlDocument xmlDoc = new XmlDocument();
            string strFileName = "";
            string PrgmFilesPath = AppDomain.CurrentDomain.BaseDirectory;

            strFileName = PrgmFilesPath + "\\bin\\XMLFiles\\DBConfig.xml";
            //strFileName = PrgmFilesPath + "XMLFiles\\DBConfig.xml";
            _log.EventLog("getDBServer_Details  strFileName = " + strFileName );
            xmlDoc.Load(strFileName);
            XmlNodeList nodeList = xmlDoc.DocumentElement.SelectNodes("/DatabaseConfig/Database/" + tagname + "");
            string strValue;
            XmlNode node = nodeList[0];
            if (node != null)
                strValue = node.InnerText;
            else
                strValue = "";
            _log.EventLog("getDBServer_Details  tagname = " + tagname + "strValue = " + strValue );
            return strValue;
        }
        public string SQL_Details(int CompId)
        {
            string strReturn = "";
            try
            {
                //string[] dbDetails = Focus.DatabaseFactory.DatabaseWrapper.GetDatabaseDetails();
                string pwd = getDBServer_Details("Password");
                string User_ID = getDBServer_Details("User_Id");
                string Data_Source = getDBServer_Details("Data_Source");
                _log.EventLog("GetDatabaseDetails " + Data_Source + User_ID + pwd);
                string CompCode = DatabaseWrapper.GetCompanyCode(CompId);
                _log.EventLog("CompCode " + CompCode);
                string ESerName = Data_Source;
                string EDBName = "Focus8"+ CompCode;
                string EUID = User_ID;
                string EPWD = pwd;
                strReturn = $"Server={ESerName};Database={EDBName};User Id={EUID};Password={EPWD};";
                return strReturn;
            }
            catch (Exception e)
            {
                error = e.Message;
                return null;
            }
        }
        public DataSet GetData(string strSelQry, int CompId, ref string error)
        {
            try
            {
                string constr = SQL_Details(CompId);
                _log.EventLog("constr " + constr);
                SqlConnection con = new SqlConnection(constr);
                con.Open();
                _log.EventLog("sql con opened ");
                SqlCommand cmd = new SqlCommand(strSelQry, con);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                _log.EventLog("sql cmd executed ");
                DataSet ds = new DataSet();
                da.Fill(ds);
                con.Close();
                return ds;
                //Database obj = Focus.DatabaseFactory.DatabaseWrapper.GetDatabase(CompId);
                //return (obj.ExecuteDataSet(CommandType.Text, strSelQry));
            }
            catch (Exception e)
            {
                error = e.Message;
                return null;
            }
        }
        public int GetExecute(string strInsertOrUpdateQry, int CompId, ref string error)
        {
            try
            {
                string constr = SQL_Details(CompId);
                _log.EventLog("constr " + constr);
                int result = 0;
                using (SqlConnection connect = new SqlConnection(constr))
                {
                    string sql = $"{strInsertOrUpdateQry}";
                    using (SqlCommand command = new SqlCommand(sql, connect))
                    {
                        connect.Open();
                        result = command.ExecuteNonQuery();
                        connect.Close();
                    }
                }
                return result;
                //Database obj = Focus.DatabaseFactory.DatabaseWrapper.GetDatabase(CompId);
                //return (obj.ExecuteNonQuery(CommandType.Text, strInsertOrUpdateQry));
            }
            catch (Exception e)
            {
                error = e.Message;
                return 0;
            }
        }
        public DataSet GetData(string strSelQry, ref string error)
        {
            _log.EventLog("entered api getdata ");
            DataSet ds = new DataSet();
            string strError = "";
            try
            {
                Output obj = null;
                
                obj = Connection.CallServeRequest(ServiceType.ExternalCall, ExternalCallMethods.ExecuteSql, strSelQry, strError);//ExecuteSql  
                _log.EventLog("obj message "+obj.Message);
                ds = (DataSet)obj.ReturnData;
                _log.EventLog("ds count " + ds.Tables.Count);
                _log.EventLog("ds 1st table count " + ds.Tables[0].Rows.Count);
                error = obj.Message;
                return ds;
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            return ds;
        }
        public DataTable GetIRef(string RefNo,int cid)
        {
            try
            {
                DataSet ds = new DataSet();

                string str = $@"EXEC Proc_LN_Vnd_Blk_Pnd '{RefNo}'";
                ds = GetData(str,cid,ref error);

                if (ds.Tables[0] != null)
                {
                    if (ds.Tables[0].Rows.Count != 0)
                    {
                        return ds.Tables[0];
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _log.ErrLog(ex.ToString());
                return null;
            }
        }
        public static int GetDateToInt(DateTime dt)
        {
            int val;
            val = Convert.ToInt16(dt.Year) * 65536 + Convert.ToInt16(dt.Month) * 256 + Convert.ToInt16(dt.Day);
            return val;
        }
    }
}