using KwalityIntegrationLibrary.Classes;
using KwalityIntegrationLibrary.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using static KwalityIntegrationLibrary.Models.APIResponse;

namespace KwalityIntegrationLibrary
{
    public class Trigger
    {
        Log_lib _log = new Log_lib();
        DB_lib _db = new DB_lib();
        public static int CompanyId;
        public static string sessionID;
        public static string baseUrl;
        string error = "";
        public bool Integration_Trigger(string ScreenNames, int CompId)
        {
            _log.EventLog("Entered  Integration_Trigger");
            bool rtnFlag = true;
            try
            {
                CompanyId = CompId;
                _log.EventLog("CompanyId " + CompanyId);
                sessionID = GetSessionId(CompanyId);
                _log.EventLog("sessionID " + sessionID);
                string strServer = getServiceLink("ServerName");
                _log.EventLog("getServiceLink " + strServer);
                baseUrl = "http://" + strServer + "/focus8API";
                _log.EventLog("baseUrl " + baseUrl);

                if (ScreenNames != "")
                {
                    string[] screens = ScreenNames.Split(',');
                    foreach (string s in screens)
                    {
                        _log.EventLog("ScreenName = " + s);
                        if (s == "Stock Transfer Issue - VAN")
                        {
                            StkTransferPIC();
                            StkTransfer_KIF();
                            StkTransfer_TB();
                        }
                        else if (s == "Stock Transfer Return - VAN")
                        {
                            StkTransferRet_PIC();
                            StkTransferRet_KIF();
                            StkTransferRet_TB();
                        }
                        else if (s == "Sales Invoice - VAN")
                        {
                            DataSet dsPIC = _db.getFn("getSalesInvoicePIC", CompanyId);
                            SalesInvoice_PIC(dsPIC);
                            DataSet dsKIF = _db.getFn("getSalesInvoiceKIF", CompanyId);
                            SalesInvoice_KIF(dsKIF);
                            DataSet dsTB = _db.getFn("getSalesInvoiceTB", CompanyId);
                            SalesInvoice_TB(dsTB);
                        }
                        else if (s == "Sales Return - VAN")
                        {
                            SalesReturn_PIC();
                            SalesReturn_KIF();
                            SalesReturn_TB();
                        }
                        else if (s == "Damage Stock")
                        {
                            DamageStock_PIC();
                            DamageStock_KIF();
                            DamageStock_TB();
                        }
                        else if (s == "Receipts")
                        {
                            Receipts_PIC();
                            Receipts_KIF();
                            Receipts_TB();
                        }
                        else if (s == "Post-Dated Receipts")
                        {
                            PDCReceipts_PIC();
                            PDCReceipts_KIF();
                            PDCReceipts_TB();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.ErrLog(ex.Message + "Trigger.Integration_Trigger()");
                rtnFlag = false;

            }

            return rtnFlag;
        }

        private void StkTransferPIC()
        {
            DataSet dsPIC = _db.getFn("getStkTransferPIC", CompanyId);
            if (dsPIC != null)
            {
                if (dsPIC.Tables.Count > 0)
                {
                    _log.EventLog("getStkTransferPIC" + dsPIC.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow Pic in dsPIC.Tables[0].Rows)
                    {
                        docno = Pic["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno " + docno);
                        string idate = Pic["intDate"].ToString().Trim();
                        string Warehouse = Pic["Warehouse"].ToString().Trim();
                        string Salesman = Pic["Salesman"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", Pic["DocumentNo"].ToString().Trim() },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "Warehouse__Code", Warehouse},
                                         { "Warehouse From__Code", Warehouse },
                                         { "Warehouse To__Code", Salesman },
                                         { "Company Master__Id", 3 }
                                     };
                        _log.EventLog("StkTransferPIC header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getStkTransferPIC_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("StkTransferPIC item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", item["DocumentNo"].ToString().Trim() },
                                         { "Item__Code", item["ItemCode"].ToString().Trim()},
                                         { "Description", item["Description1"].ToString().Trim()},
                                         { "Unit__Id", 24 },
                                         { "StockAC__Id", item["iStocksAccount"].ToString().Trim() },
                                         { "Quantity", Convert.ToDecimal(item["Qty"].ToString()) },
                                     };
                            lstBody.Add(objBody);
                        }
                        _log.EventLog("StkTransferPIC body Data ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("StkTransferPIC Content " + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Stock Transfer Issue - VAN";
                        _log.EventLog("StkTransferPIC url " + Url1);
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog("posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("Stock Transfer Issue - VAN Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + "Stock Transfer Issue - VAN Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                            }
                            else
                            {
                                _log.EventLog("Stock Transfer Issue - VAN Entry Posted Successfully with DocNo: " + docno);
                                int d = _db.setFn("setStkTransferPIC", docno, CompanyId);
                                if (d != 0)
                                {
                                    _log.EventLog(" StkTransferPIC FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                }
                                else
                                {
                                    _log.EventLog(" StkTransferPIC FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getStkTransferPIC dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getStkTransferPIC dataset is null");
            }
        }

        private void StkTransfer_KIF()
        {
            DataSet dsKIF = _db.getFn("getStkTransferKIF", CompanyId);
            if (dsKIF != null)
            {
                if (dsKIF.Tables.Count > 0)
                {
                    _log.EventLog("getStkTransferKIF" + dsKIF.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow Pic in dsKIF.Tables[0].Rows)
                    {
                        docno = Pic["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = Pic["intDate"].ToString().Trim();
                        string Warehouse = Pic["Warehouse"].ToString().Trim();
                        string Salesman = Pic["Salesman"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", Pic["DocumentNo"].ToString().Trim() },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "Warehouse__Code", Warehouse},
                                         { "Warehouse From__Code", Warehouse },
                                         { "Warehouse To__Code", Salesman },
                                         { "Company Master__Id", 4 }
                                     };
                        _log.EventLog("StkTransferKIF Header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getStkTransferKIF_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("StkTransferKIF item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", item["DocumentNo"].ToString().Trim() },
                                         { "Item__Code", item["ItemCode"].ToString().Trim()},
                                         { "Description", item["Description1"].ToString().Trim()},
                                         { "Unit__Id", 24 },
                                         { "StockAC__Id", item["iStocksAccount"].ToString().Trim() },
                                         { "Quantity", Convert.ToDecimal(item["Qty"].ToString().Trim()) },
                                     };
                            lstBody.Add(objBody);
                        }
                        _log.EventLog("StkTransferKIF BOdy Data ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("StkTransferKIF Content " + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Stock Transfer Issue - VAN";
                        _log.EventLog("StkTransferKIF url  " + Url1);
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("StkTransferKIF posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog("StkTransferKIF posting Response failed" + responseData1.result.ToString());
                                _log.EventLog(" StkTransferKIF Stock Transfer Issue - VAN Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + "Stock Transfer Issue - VAN Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                            }
                            else
                            {
                                _log.EventLog("StkTransferKIF Stock Transfer Issue - VAN Entry Posted Successfully with DocNo: " + docno);
                                int d = _db.setFn("setStkTransferKIF", docno, CompanyId);
                                if (d != 0)
                                {
                                    _log.EventLog(" StkTransfer_KIF FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                }
                                else
                                {
                                    _log.EventLog(" StkTransfer_KIF FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getStkTransferKIF dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getStkTransferKIF dataset is null");
            }
        }

        private void StkTransfer_TB()
        {
            DataSet dsTB = _db.getFn("getStkTransferTB", CompanyId);
            if (dsTB != null)
            {
                if (dsTB.Tables.Count > 0)
                {
                    _log.EventLog("getStkTransferTB" + dsTB.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow Pic in dsTB.Tables[0].Rows)
                    {
                        docno = Pic["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = Pic["intDate"].ToString().Trim();
                        string Warehouse = Pic["Warehouse"].ToString().Trim();
                        string Salesman = Pic["Salesman"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", Pic["DocumentNo"].ToString().Trim() },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "Warehouse__Code", Warehouse},
                                         { "Warehouse From__Code", Warehouse },
                                         { "Warehouse To__Code", Salesman },
                                         { "Company Master__Id",5}
                                     };
                        _log.EventLog("StkTransferTB Header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getStkTransferTB_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("StkTransferTB item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", item["DocumentNo"].ToString().Trim() },
                                         { "Item__Code", item["ItemCode"].ToString().Trim()},
                                         { "Description", item["Description1"].ToString().Trim()},
                                         { "Unit__Id", 24 },
                                         { "StockAC__Id", item["iStocksAccount"].ToString().Trim() },
                                         { "Quantity", Convert.ToDecimal(item["Qty"].ToString().Trim()) },
                                     };
                            lstBody.Add(objBody);
                        }
                        _log.EventLog("StkTransferTB Body Data ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("StkTransferTB Content " + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Stock Transfer Issue - VAN";
                        _log.EventLog("StkTransferTB url " + Url1);
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("StkTransferTB posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog("StkTransferTB posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("StkTransferTB Stock Transfer Issue - VAN Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + "Stock Transfer Issue - VAN Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                            }
                            else
                            {
                                _log.EventLog("StkTransferTB Stock Transfer Issue - VAN Entry Posted Successfully with DocNo: " + docno);
                                int d = _db.setFn("setStkTransferTB", docno, CompanyId);
                                if (d != 0)
                                {
                                    _log.EventLog(" StkTransfer_TB FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                }
                                else
                                {
                                    _log.EventLog(" StkTransfer_TB FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getStkTransferTB dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getStkTransferTB dataset is null");
            }
        }

        private void StkTransferRet_PIC()
        {
            DataSet dsPIC = _db.getFn("getStkTransferRetPIC", CompanyId);
            if (dsPIC != null)
            {
                if (dsPIC.Tables.Count > 0)
                {
                    _log.EventLog("getStkTransferRetPIC" + dsPIC.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow Pic in dsPIC.Tables[0].Rows)
                    {
                        docno = Pic["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = Pic["intDate"].ToString().Trim();
                        string Warehouse = Pic["Warehouse"].ToString().Trim();
                        string Salesman = Pic["Salesman"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", Pic["DocumentNo"].ToString().Trim() },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "Warehouse__Code", Salesman},
                                         { "Warehouse From__Code", Salesman },
                                         { "Warehouse To__Code",  Warehouse},
                                         { "Company Master__Id", 3 }
                                     };
                        _log.EventLog("StkTransferRetPIC Header Data Ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getStkTransferRetPIC_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("StkTransferRetPIC item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", item["DocumentNo"].ToString().Trim() },
                                         { "Item__Code", item["ItemCode"].ToString().Trim()},
                                         { "Description", item["Description1"].ToString().Trim()},
                                         { "Unit__Id", 24 },
                                         { "StockAC__Id", item["iStocksAccount"].ToString().Trim() },
                                         { "Quantity", Convert.ToDecimal(item["Qty"].ToString().Trim()) },
                                     };
                            lstBody.Add(objBody);
                        }
                        _log.EventLog("StkTransferRetPIC Body Data Ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("StkTransferRetPIC Content " + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Stock Transfer Return - VAN";
                        _log.EventLog("StkTransferRetPIC url " + Url1);
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("StkTransferRetPIC posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog("StkTransferRetPIC posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("StkTransferRetPIC Stock Transfer Return - VAN Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + "Stock Transfer Return - VAN Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                            }
                            else
                            {
                                _log.EventLog("StkTransferRetPIC Stock Transfer Return - VAN Entry Posted Successfully with DocNo: " + docno);
                                int d = _db.setFn("setStkTransferRetPIC", docno, CompanyId);
                                if (d != 0)
                                {
                                    _log.EventLog(" StkTransferRet_PIC FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                }
                                else
                                {
                                    _log.EventLog(" StkTransferRet_PIC FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getStkTransferRetPIC dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getStkTransferRetPIC dataset is null");
            }
        }

        private void StkTransferRet_KIF()
        {
            DataSet dsKIF = _db.getFn("getStkTransferRetKIF", CompanyId);
            if (dsKIF != null)
            {
                if (dsKIF.Tables.Count > 0)
                {
                    _log.EventLog("getStkTransferRetKIF" + dsKIF.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow Pic in dsKIF.Tables[0].Rows)
                    {
                        docno = Pic["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = Pic["intDate"].ToString().Trim();
                        string Warehouse = Pic["Warehouse"].ToString().Trim();
                        string Salesman = Pic["Salesman"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", Pic["DocumentNo"].ToString().Trim() },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "Warehouse__Code", Salesman},
                                         { "Warehouse From__Code", Salesman },
                                         { "Warehouse To__Code", Warehouse },
                                         { "Company Master__Id", 4 }
                                     };
                        _log.EventLog("StkTransferRetKIF Header Data Ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getStkTransferRetKIF_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("StkTransferRetKIF item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", item["DocumentNo"].ToString().Trim() },
                                         { "Item__Code", item["ItemCode"].ToString().Trim()},
                                         { "Description", item["Description1"].ToString().Trim()},
                                         { "Unit__Id", 24 },
                                         { "StockAC__Id", item["iStocksAccount"].ToString().Trim() },
                                         { "Quantity", Convert.ToDecimal(item["Qty"].ToString().Trim()) },
                                     };
                            lstBody.Add(objBody);
                        }
                        _log.EventLog("StkTransferRetKIF Body Data Ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("StkTransferRetKIF Content " + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Stock Transfer Return - VAN";
                        _log.EventLog("StkTransferRetKIF url " + Url1);
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog("posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("Stock Transfer Return - VAN Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + "Stock Transfer Return - VAN Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                            }
                            else
                            {
                                _log.EventLog("Stock Transfer Return - VAN Entry Posted Successfully with DocNo: " + docno);
                                int d = _db.setFn("setStkTransferRetKIF", docno, CompanyId);
                                if (d != 0)
                                {
                                    _log.EventLog(" StkTransferRet_KIF FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                }
                                else
                                {
                                    _log.EventLog(" StkTransferRet_KIF FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getStkTransferRetKIF dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getStkTransferRetKIF dataset is null");
            }
        }

        private void StkTransferRet_TB()
        {
            DataSet dsTB = _db.getFn("getStkTransferRetTB", CompanyId);
            if (dsTB != null)
            {
                if (dsTB.Tables.Count > 0)
                {
                    _log.EventLog("getStkTransferRetTB" + dsTB.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow Pic in dsTB.Tables[0].Rows)
                    {
                        docno = Pic["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = Pic["intDate"].ToString().Trim();
                        string Warehouse = Pic["Warehouse"].ToString().Trim();
                        string Salesman = Pic["Salesman"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", Pic["DocumentNo"].ToString().Trim() },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "Warehouse__Code", Salesman},
                                         { "Warehouse From__Code", Salesman },
                                         { "Warehouse To__Code", Warehouse },
                                         { "Company Master__Id",5}
                                     };
                        _log.EventLog("StkTransferRetTB Header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getStkTransferRetTB_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("StkTransferRetTB item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", item["DocumentNo"].ToString().Trim() },
                                         { "Item__Code", item["ItemCode"].ToString().Trim()},
                                         { "Description", item["Description1"].ToString().Trim()},
                                         { "Unit__Id", 24 },
                                         { "StockAC__Id", item["iStocksAccount"].ToString().Trim() },
                                         { "Quantity", Convert.ToDecimal(item["Qty"].ToString().Trim()) },
                                     };
                            lstBody.Add(objBody);
                        }
                        _log.EventLog("StkTransferRetTB Body Data ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("StkTransferRetTB Content " + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Stock Transfer Return - VAN";
                        _log.EventLog("StkTransferRetTB url " + Url1);
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog("posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("Stock Transfer Return - VAN Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + "Stock Transfer Return - VAN Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                            }
                            else
                            {
                                _log.EventLog("Stock Transfer Return - VAN Entry Posted Successfully with DocNo: " + docno);
                                int d = _db.setFn("setStkTransferRetTB", docno, CompanyId);
                                if (d != 0)
                                {
                                    _log.EventLog(" StkTransferRet_TB FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                }
                                else
                                {
                                    _log.EventLog(" StkTransferRet_TB FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getStkTransferRetTB dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getStkTransferRetTB dataset is null");
            }
        }

        bool InvFlag = true;
        private void SalesInvoice_PIC(DataSet dsPIC)
        {
            try
            {
                if (dsPIC != null)
                {
                    if (dsPIC.Tables.Count > 0)
                    {
                        _log.EventLog("getSalesInvoicePIC" + dsPIC.Tables.Count.ToString());
                        string docno = "";
                        foreach (DataRow Pic in dsPIC.Tables[0].Rows)
                        {
                            docno = Pic["DocumentNo"].ToString().Trim();
                            _log.EventLog("docno" + docno);
                            string idate = Pic["intDate"].ToString().Trim();
                            string SalesAccount = Pic["SalesAccount"].ToString().Trim();
                            string CustomerAC = Pic["CustomerAC"].ToString().Trim();
                            string DueDate = Pic["intDueDate"].ToString().Trim();
                            string WarehouseCode = Pic["Salesman"].ToString().Trim();
                            string RouteCode = Pic["Salesman"].ToString().Trim();
                            string Narration = Pic["Narration"].ToString().Trim();
                            string Grp = Pic["Grp"].ToString().Trim();
                            string LPONO = Pic["PONo"].ToString().Trim();
                            string Jurisdiction = Pic["Jurisdiction"].ToString().Trim();
                            string PlaceOfSupply = Pic["PlaceOfSupply"].ToString().Trim();
                            Hashtable header = new Hashtable
                                     {
                                         { "DocNo", Pic["DocumentNo"].ToString().Trim() },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "SalesAC__Name", SalesAccount},
                                         { "CustomerAC__Code", CustomerAC },
                                         { "DueDate", Convert.ToInt32(DueDate) },
                                         { "Company Master__Id", 3 },
                                         { "Warehouse__Code", WarehouseCode },
                                         { "Route__Code", RouteCode },
                                         { "sNarration", Narration },
                                         { "Group Customer Master__Name", Grp },
                                         { "LPO_No", LPONO },
                                         { "Place of supply__Id", PlaceOfSupply},
                                         { "Jurisdiction__Id", Jurisdiction }
                                     };
                            _log.EventLog("SalesInvoice_PIC header Data ready");
                            List<Hashtable> lstBody = new List<Hashtable>();
                            DataSet dsBody = _db.getFn("getSalesInvoicePIC_Body,@p1=" + docno, CompanyId);
                            _log.EventLog("SalesInvoice_PIC item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                            if (dsBody.Tables[0].Rows.Count == 0)
                            {
                                _log.EventLog("No Body");
                                continue;
                            }
                            foreach (DataRow item in dsBody.Tables[0].Rows)
                            {
                                Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", item["DocumentNo"].ToString().Trim() },
                                         { "Item__Code", item["ItemCode"].ToString().Trim()},
                                         { "Description", item["Description1"].ToString().Trim()},
                                         { "Unit__Id", 24 },
                                         { "StockAC__Id", item["iStocksAccount"].ToString().Trim() },
                                         { "Quantity", Convert.ToDecimal(item["Qty"].ToString().Trim()) },
                                         { "Rate", Convert.ToDecimal(item["Rate"].ToString().Trim()) },
                                         { "Discount %", Convert.ToDecimal(item["Discount2"].ToString().Trim()) },
                                         { "TaxCode__Code", "SR" },
                                         { "VAT", Convert.ToDecimal(item["VAT2"].ToString().Trim()) },
                                         { "Vat Amount", Convert.ToDecimal(item["TotalVATAmount"].ToString().Trim()) }
                                     };
                                lstBody.Add(objBody);
                            }
                            _log.EventLog("SalesInvoice_PIC Body Data ready");
                            var postingData1 = new PostingData();
                            postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                            string sContent1 = JsonConvert.SerializeObject(postingData1);
                            _log.EventLog("SalesInvoice_PIC Content" + sContent1);
                            string err1 = "";

                            string Url1 = baseUrl + "/Transactions/Vouchers/Sales Invoice - VAN";
                            _log.EventLog("SalesInvoice_PIC post url" + Url1);
                            sessionID = GetSessionId(CompanyId);
                            var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                            if (response1 != null)
                            {
                                _log.EventLog("SalesInvoice_PIC posting Response" + response1.ToString());
                                var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                                if (responseData1.result == -1)
                                {
                                    InvFlag = false;
                                    _log.EventLog(" SalesInvoice_PIC posting Response failed" + responseData1.result.ToString());
                                    _log.EventLog("SalesInvoice_PIC Sales Invoice - VAN Entry Posted Failed with DocNo: " + docno);
                                    _log.ErrLog(response1 + "\n " + " SalesInvoice_PIC Sales Invoice - VAN Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                                }
                                else
                                {
                                    _log.EventLog(" SalesInvoice_PIC Sales Invoice - VAN Entry Posted Successfully with DocNo: " + docno);
                                    int d = _db.setFn("setSalesInvoicePIC", docno, CompanyId);
                                    if (d != 0)
                                    {
                                        _log.EventLog(" SalesInvoice_PIC FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                    }
                                    else
                                    {
                                        _log.EventLog(" SalesInvoice_PIC FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                    }
                                }
                            }
                        }

                    }
                    else
                    {
                        InvFlag = false;
                        _log.EventLog("getSalesInvoicePIC dataset table count is zero");
                    }
                }
                else
                {
                    InvFlag = false;
                    _log.EventLog("getSalesInvoicePIC dataset is null");
                }
            }
            catch (Exception ex)
            {
                InvFlag = false;
                _log.ErrLog(ex.Message+"\n"+ex.StackTrace + "\n"+ex.Source + "\n"+ex.InnerException);
            }
        }

        private void SalesInvoice_KIF(DataSet dsKIF)
        {
            if (dsKIF != null)
            {
                if (dsKIF.Tables.Count > 0)
                {
                    _log.EventLog("getSalesInvoiceKIF" + dsKIF.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow KIF in dsKIF.Tables[0].Rows)
                    {
                        docno = KIF["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = KIF["intDate"].ToString().Trim();
                        string SalesAccount = KIF["SalesAccount"].ToString().Trim();
                        string CustomerAC = KIF["CustomerAC"].ToString().Trim();
                        string DueDate = KIF["intDueDate"].ToString().Trim();
                        string WarehouseCode = KIF["Salesman"].ToString().Trim();
                        string RouteCode = KIF["Salesman"].ToString().Trim();
                        string Narration = KIF["Narration"].ToString().Trim();
                        string Grp = KIF["Grp"].ToString().Trim();
                        string LPONO = KIF["PONo"].ToString().Trim();
                        string Jurisdiction = KIF["Jurisdiction"].ToString().Trim();
                        string PlaceOfSupply = KIF["PlaceOfSupply"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", KIF["DocumentNo"].ToString().Trim() },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "SalesAC__Name", SalesAccount},
                                         { "CustomerAC__Code", CustomerAC },
                                         { "DueDate", Convert.ToInt32(DueDate) },
                                         { "Company Master__Id", 4 },
                                         { "Warehouse__Code", WarehouseCode },
                                         { "Route__Code", RouteCode },
                                         { "sNarration", Narration },
                                         { "Group Customer Master__Name", Grp },
                                         { "LPO_No", LPONO },
                                         { "Place of supply__Id", PlaceOfSupply},
                                         { "Jurisdiction__Id", Jurisdiction }
                                     };
                        _log.EventLog("SalesInvoice_KIF header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getSalesInvoiceKIF_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("SalesInvoice_KIF item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", item["DocumentNo"].ToString().Trim() },
                                         { "Item__Code", item["ItemCode"].ToString().Trim()},
                                         { "Description", item["Description1"].ToString().Trim()},
                                         { "Unit__Id", 24 },
                                         { "StockAC__Id", item["iStocksAccount"].ToString().Trim() },
                                         { "Quantity", Convert.ToDecimal(item["Qty"].ToString().Trim()) },
                                         { "Rate", Convert.ToDecimal(item["Rate"].ToString().Trim()) },
                                         { "Discount %", Convert.ToDecimal(item["Discount2"].ToString().Trim()) },
                                         { "TaxCode__Code", "SR" },
                                         { "VAT", Convert.ToDecimal(item["VAT2"].ToString().Trim()) },
                                         { "Vat Amount", Convert.ToDecimal(item["TotalVATAmount"].ToString().Trim()) }
                                     };
                            lstBody.Add(objBody);
                        }
                        _log.EventLog("SalesInvoice_KIF Body Data ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("SalesInvoice_KIF Content" + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Sales Invoice - VAN";
                        _log.EventLog("SalesInvoice_KIF post url" + Url1);
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("SalesInvoice_KIF posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog("SalesInvoice_KIF posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("SalesInvoice_KIF Sales Invoice - VAN Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + "Sales Invoice - VAN Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                            }
                            else
                            {
                                _log.EventLog("SalesInvoice_KIF Sales Invoice - VAN Entry Posted Successfully with DocNo: " + docno);
                                int d = _db.setFn("setSalesInvoiceKIF", docno, CompanyId);
                                if (d != 0)
                                {
                                    _log.EventLog(" SalesInvoice_KIF FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                }
                                else
                                {
                                    _log.EventLog(" SalesInvoice_KIF FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getSalesInvoiceKIF dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getSalesInvoiceKIF dataset is null");
            }
        }

        private void SalesInvoice_TB(DataSet dsTB)
        {
            if (dsTB != null)
            {
                if (dsTB.Tables.Count > 0)
                {
                    _log.EventLog("getSalesInvoiceTB" + dsTB.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow TB in dsTB.Tables[0].Rows)
                    {
                        docno = TB["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = TB["intDate"].ToString().Trim();
                        string SalesAccount = TB["SalesAccount"].ToString().Trim();
                        string CustomerAC = TB["CustomerAC"].ToString().Trim();
                        string DueDate = TB["intDueDate"].ToString().Trim();
                        string WarehouseCode = TB["Salesman"].ToString().Trim();
                        string RouteCode = TB["Salesman"].ToString().Trim();
                        string Narration = TB["Narration"].ToString().Trim();
                        string Grp = TB["Grp"].ToString().Trim();
                        string LPONO = TB["PONo"].ToString().Trim();
                        string Jurisdiction = TB["Jurisdiction"].ToString().Trim();
                        string PlaceOfSupply = TB["PlaceOfSupply"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", TB["DocumentNo"].ToString().Trim() },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "SalesAC__Name", SalesAccount},
                                         { "CustomerAC__Code", CustomerAC },
                                         { "DueDate", Convert.ToInt32(DueDate) },
                                         { "Company Master__Id", 5 },
                                         { "Warehouse__Code", WarehouseCode },
                                         { "Route__Code", RouteCode },
                                         { "sNarration", Narration },
                                         { "Group Customer Master__Name", Grp },
                                         { "LPO_No", LPONO },
                                         { "Place of supply__Id", PlaceOfSupply},
                                         { "Jurisdiction__Id", Jurisdiction }
                                     };
                        _log.EventLog("SalesInvoice_TB header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getSalesInvoiceTB_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("SalesInvoice_TB item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", item["DocumentNo"].ToString().Trim() },
                                         { "Item__Code", item["ItemCode"].ToString().Trim()},
                                         { "Description", item["Description1"].ToString().Trim()},
                                         { "Unit__Id", 24 },
                                         { "StockAC__Id", item["iStocksAccount"].ToString().Trim() },
                                         { "Quantity", Convert.ToDecimal(item["Qty"].ToString().Trim()) },
                                         { "Rate", Convert.ToDecimal(item["Rate"].ToString().Trim()) },
                                         { "Discount %", Convert.ToDecimal(item["Discount2"].ToString().Trim()) },
                                         { "TaxCode__Code", "SR" },
                                         { "VAT", Convert.ToDecimal(item["VAT2"].ToString().Trim()) },
                                         { "Vat Amount", Convert.ToDecimal(item["TotalVATAmount"].ToString().Trim()) }
                                     };
                            lstBody.Add(objBody);
                        }
                        _log.EventLog("SalesInvoice_TB Body Data ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("SalesInvoice_TB Content" + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Sales Invoice - VAN";
                        _log.EventLog("SalesInvoice_TB post url" + Url1);
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("SalesInvoice_TB posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog("SalesInvoice_TB posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("SalesInvoice_TB Sales Invoice - VAN Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + " SalesInvoice_TB Sales Invoice - VAN Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                            }
                            else
                            {
                                _log.EventLog("SalesInvoice_TB Sales Invoice - VAN Entry Posted Successfully with DocNo: " + docno);
                                int d = _db.setFn("setSalesInvoiceTB", docno, CompanyId);
                                if (d != 0)
                                {
                                    _log.EventLog("SalesInvoice_TB FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                }
                                else
                                {
                                    _log.EventLog("SalesInvoice_TB FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getSalesInvoiceTB dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getSalesInvoiceTB dataset is null");
            }
        }

        private void SalesReturn_PIC()
        {
            DataSet dsPIC = _db.getFn("getSalesReturnPIC", CompanyId);
            if (dsPIC != null)
            {
                if (dsPIC.Tables.Count > 0)
                {
                    _log.EventLog("getSalesReturnPIC" + dsPIC.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow Pic in dsPIC.Tables[0].Rows)
                    {
                        docno = Pic["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = Pic["intDate"].ToString().Trim();
                        string SalesAccount = Pic["SalesAccount"].ToString().Trim();
                        string CustomerAC = Pic["CustomerAC"].ToString().Trim();
                        //string DueDate = Pic["intDueDate"].ToString().Trim();
                        string WarehouseCode = Pic["Salesman"].ToString().Trim();
                        string RouteCode = Pic["Salesman"].ToString().Trim();
                        string Narration = Pic["Narration"].ToString().Trim();
                        string Grp = Pic["Grp"].ToString().Trim();
                        string LPONO = Pic["PONo"].ToString().Trim();
                        string Jurisdiction = Pic["Jurisdiction"].ToString().Trim();
                        string PlaceOfSupply = Pic["PlaceOfSupply"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", Pic["DocumentNo"].ToString().Trim() },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "SalesAC__Name", SalesAccount},
                                         { "CustomerAC__Code", CustomerAC },
                                         //{ "DueDate", Convert.ToInt32(DueDate) },
                                         { "Company Master__Id", 3 },
                                         { "Warehouse__Code", WarehouseCode },
                                         { "Route__Code", RouteCode },
                                         { "sNarration", Narration },
                                         { "Group Customer Master__Name", Grp },
                                         { "LPO_No", LPONO },
                                         { "Place of supply__Id", PlaceOfSupply},
                                         { "Jurisdiction__Id", Jurisdiction }
                                     };
                        _log.EventLog("SalesReturn_PIC header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getSalesReturnPIC_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("SalesReturn_PIC item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", item["DocumentNo"].ToString().Trim() },
                                         { "Item__Code", item["ItemCode"].ToString().Trim()},
                                         { "Description", item["Description1"].ToString().Trim()},
                                         { "Unit__Id", 24 },
                                         { "StockAC__Id", item["iStocksAccount"].ToString().Trim() },
                                         { "Quantity", Convert.ToDecimal(item["Qty"].ToString().Trim()) },
                                         { "Rate", Convert.ToDecimal(item["Rate"].ToString().Trim()) },
                                         { "Discount %", Convert.ToDecimal(item["Discount2"].ToString().Trim()) },
                                         { "TaxCode__Code", "SR" },
                                         { "VAT", Convert.ToDecimal(item["VAT2"].ToString().Trim()) },
                                         { "Vat Amount", Convert.ToDecimal(item["TotalVATAmount"].ToString().Trim()) }
                                     };
                            lstBody.Add(objBody);
                        }
                        _log.EventLog("SalesReturn_PIC Body Data ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("SalesReturn_PIC Content" + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Sales Return - VAN";
                        _log.EventLog("SalesReturn_PIC post url" + Url1);
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("SalesReturn_PIC posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog(" SalesReturn_PIC posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("SalesReturn_PIC Sales Return - VAN Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + " SalesReturn_PIC Sales Return - VAN Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                            }
                            else
                            {
                                _log.EventLog(" SalesReturn_PIC Sales Return - VAN Entry Posted Successfully with DocNo: " + docno);
                                int d = _db.setFn("setSalesReturnPIC", docno, CompanyId);
                                if (d != 0)
                                {
                                    _log.EventLog(" SalesReturn_PIC FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                }
                                else
                                {
                                    _log.EventLog(" SalesReturn_PIC FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getSalesReturnPIC dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getSalesReturnPIC dataset is null");
            }
        }

        private void SalesReturn_KIF()
        {
            DataSet dsKIF = _db.getFn("getSalesReturnKIF", CompanyId);
            if (dsKIF != null)
            {
                if (dsKIF.Tables.Count > 0)
                {
                    _log.EventLog("getSalesReturnKIF" + dsKIF.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow KIF in dsKIF.Tables[0].Rows)
                    {
                        docno = KIF["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = KIF["intDate"].ToString().Trim();
                        string SalesAccount = KIF["SalesAccount"].ToString().Trim();
                        string CustomerAC = KIF["CustomerAC"].ToString().Trim();
                        //string DueDate = KIF["intDueDate"].ToString().Trim();
                        string WarehouseCode = KIF["Salesman"].ToString().Trim();
                        string RouteCode = KIF["Salesman"].ToString().Trim();
                        string Narration = KIF["Narration"].ToString().Trim();
                        string Grp = KIF["Grp"].ToString().Trim();
                        string LPONO = KIF["PONo"].ToString().Trim();
                        string Jurisdiction = KIF["Jurisdiction"].ToString().Trim();
                        string PlaceOfSupply = KIF["PlaceOfSupply"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", KIF["DocumentNo"].ToString().Trim() },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "SalesAC__Name", SalesAccount},
                                         { "CustomerAC__Code", CustomerAC },
                                         //{ "DueDate", Convert.ToInt32(DueDate) },
                                         { "Company Master__Id", 4 },
                                         { "Warehouse__Code", WarehouseCode },
                                         { "Route__Code", RouteCode },
                                         { "sNarration", Narration },
                                         { "Group Customer Master__Name", Grp },
                                         { "LPO_No", LPONO },
                                         { "Place of supply__Id", PlaceOfSupply},
                                         { "Jurisdiction__Id", Jurisdiction }
                                     };
                        _log.EventLog("SalesReturn_KIF header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getSalesReturnKIF_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("SalesReturn_KIF item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", item["DocumentNo"].ToString().Trim() },
                                         { "Item__Code", item["ItemCode"].ToString().Trim()},
                                         { "Description", item["Description1"].ToString().Trim()},
                                         { "Unit__Id", 24 },
                                         { "StockAC__Id", item["iStocksAccount"].ToString().Trim() },
                                         { "Quantity", Convert.ToDecimal(item["Qty"].ToString().Trim()) },
                                         { "Rate", Convert.ToDecimal(item["Rate"].ToString().Trim()) },
                                         { "Discount %", Convert.ToDecimal(item["Discount2"].ToString().Trim()) },
                                         { "TaxCode__Code", "SR" },
                                         { "VAT", Convert.ToDecimal(item["VAT2"].ToString().Trim()) },
                                         { "Vat Amount", Convert.ToDecimal(item["TotalVATAmount"].ToString().Trim()) }
                                     };
                            lstBody.Add(objBody);
                        }
                        _log.EventLog("SalesReturn_KIF Body Data ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("SalesReturn_KIF Content" + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Sales Return - VAN";
                        _log.EventLog("SalesReturn_KIF post url" + Url1);
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("SalesReturn_KIF posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog(" SalesReturn_KIF posting Response failed" + responseData1.result.ToString());
                                _log.EventLog(" SalesReturn_KIF Sales Return - VAN Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + "Sales Return - VAN Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                            }
                            else
                            {
                                _log.EventLog(" SalesReturn_KIF Sales Return - VAN Entry Posted Successfully with DocNo: " + docno);
                                int d = _db.setFn("setSalesReturnKIF", docno, CompanyId);
                                if (d != 0)
                                {
                                    _log.EventLog(" SalesReturn_KIF FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                }
                                else
                                {
                                    _log.EventLog(" SalesReturn_KIF FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getSalesReturnKIF dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getSalesReturnKIF dataset is null");
            }
        }

        private void SalesReturn_TB()
        {
            DataSet dsTB = _db.getFn("getSalesReturnTB", CompanyId);
            if (dsTB != null)
            {
                if (dsTB.Tables.Count > 0)
                {
                    _log.EventLog("getSalesReturnTB" + dsTB.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow TB in dsTB.Tables[0].Rows)
                    {
                        docno = TB["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = TB["intDate"].ToString().Trim();
                        string SalesAccount = TB["SalesAccount"].ToString().Trim();
                        string CustomerAC = TB["CustomerAC"].ToString().Trim();
                        //string DueDate = TB["intDueDate"].ToString().Trim();
                        string WarehouseCode = TB["Salesman"].ToString().Trim();
                        string RouteCode = TB["Salesman"].ToString().Trim();
                        string Narration = TB["Narration"].ToString().Trim();
                        string Grp = TB["Grp"].ToString().Trim();
                        string LPONO = TB["PONo"].ToString().Trim();
                        string Jurisdiction = TB["Jurisdiction"].ToString().Trim();
                        string PlaceOfSupply = TB["PlaceOfSupply"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", TB["DocumentNo"].ToString().Trim() },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "SalesAC__Name", SalesAccount},
                                         { "CustomerAC__Code", CustomerAC },
                                         //{ "DueDate", Convert.ToInt32(DueDate) },
                                         { "Company Master__Id", 5 },
                                         { "Warehouse__Code", WarehouseCode },
                                         { "Route__Code", RouteCode },
                                         { "sNarration", Narration },
                                         { "Group Customer Master__Name", Grp },
                                         { "LPO_No", LPONO },
                                         { "Place of supply__Id", PlaceOfSupply},
                                         { "Jurisdiction__Id", Jurisdiction }
                                     };
                        _log.EventLog("SalesReturn_TB header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getSalesReturnTB_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("SalesReturn_TB item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", item["DocumentNo"].ToString().Trim() },
                                         { "Item__Code", item["ItemCode"].ToString().Trim()},
                                         { "Description", item["Description1"].ToString().Trim()},
                                         { "Unit__Id", 24 },
                                         { "StockAC__Id", item["iStocksAccount"].ToString().Trim() },
                                         { "Quantity", Convert.ToDecimal(item["Qty"].ToString().Trim()) },
                                         { "Rate", Convert.ToDecimal(item["Rate"].ToString().Trim()) },
                                         { "Discount %", Convert.ToDecimal(item["Discount2"].ToString().Trim()) },
                                         { "TaxCode__Code", "SR" },
                                         { "VAT", Convert.ToDecimal(item["VAT2"].ToString().Trim()) },
                                         { "Vat Amount", Convert.ToDecimal(item["TotalVATAmount"].ToString().Trim()) }
                                     };
                            lstBody.Add(objBody);
                        }
                        _log.EventLog("SalesReturn_TB Body Data ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("SalesReturn_TB Content" + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Sales Return - VAN";
                        _log.EventLog("SalesReturn_TB post url" + Url1);
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("SalesReturn_TB posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog("SalesReturn_TB posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("SalesReturn_TB Sales Return - VAN Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + " SalesReturn_TB Sales Return - VAN Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                            }
                            else
                            {
                                _log.EventLog("SalesReturn_TB Sales Return - VAN Entry Posted Successfully with DocNo: " + docno);
                                int d = _db.setFn("setSalesReturnTB", docno, CompanyId);
                                if (d != 0)
                                {
                                    _log.EventLog("SalesReturn_TB FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                }
                                else
                                {
                                    _log.EventLog("SalesReturn_TB FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getSalesReturnTB dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getSalesReturnTB dataset is null");
            }
        }

        private void DamageStock_PIC()
        {
            DataSet dsPIC = _db.getFn("getDamageStockPIC", CompanyId);
            if(dsPIC!=null)
            {
                if (dsPIC.Tables.Count > 0)
                {
                    _log.EventLog("getDamageStockPIC" + dsPIC.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow Pic in dsPIC.Tables[0].Rows)
                    {
                        docno = Pic["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = Pic["intDate"].ToString().Trim();
                        string SalesAccount = Pic["SalesAccount"].ToString().Trim();
                        string CustomerAC = Pic["CustomerAC"].ToString().Trim();
                        //string DueDate = Pic["intDueDate"].ToString().Trim();
                        string WarehouseCode = Pic["Salesman"].ToString().Trim();
                        string RouteCode = Pic["Salesman"].ToString().Trim();
                        string Narration = Pic["Narration"].ToString().Trim();
                        string Grp = Pic["Grp"].ToString().Trim();
                        string LPONO = "";// Pic["PONo"].ToString().Trim();
                        string PlaceOfSupply = Pic["PlaceOfSupply"].ToString().Trim();
                        string Jurisdiction = Pic["Jurisdiction"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", Pic["DocumentNo"].ToString().Trim() },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "SalesAC__Name", SalesAccount},
                                         { "CustomerAC__Code", CustomerAC },
                                         { "CustomerName__Code", CustomerAC },
                                         //{ "DueDate", Convert.ToInt32(DueDate) },
                                         { "Company Master__Id", 3 },
                                         { "Warehouse__Code", WarehouseCode },
                                         { "Route__Code", RouteCode },
                                         { "sNarration", Narration },
                                         { "Group Customer Master__Name", Grp },
                                         { "LPO_No", LPONO },
                                         { "ReturnType", "1" },
                                         { "Place of supply__Id", PlaceOfSupply},
                                         { "Jurisdiction__Id", Jurisdiction }
                                     };
                        _log.EventLog("DamageStock_PIC header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getDamageStockPIC_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("DamageStock_PIC item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", item["DocumentNo"].ToString().Trim() },
                                         { "Item__Code", item["ItemCode"].ToString().Trim()},
                                         { "Description", item["Description1"].ToString().Trim()},
                                         { "Unit__Id", 24 },
                                         { "StockAC__Id", item["iStocksAccount"].ToString().Trim() },
                                         { "Quantity", Convert.ToDecimal(item["Qty"].ToString().Trim()) },
                                         { "Rate", Convert.ToDecimal(item["Rate"].ToString().Trim()) },
                                         { "Discount %", Convert.ToDecimal(item["Discount2"].ToString().Trim()) },
                                         { "TaxCode__Code", "SR" },
                                         { "VAT", Convert.ToDecimal(item["VAT2"].ToString().Trim()) },
                                         { "Vat Amount", Convert.ToDecimal(item["TotalVATAmount"].ToString().Trim()) }
                                     };
                            lstBody.Add(objBody);
                        }
                        _log.EventLog("DamageStock_PIC Body Data ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("DamageStock_PIC Content" + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Sales Return CN - VAN";
                        _log.EventLog("DamageStock_PIC post url" + Url1);
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("DamageStock_PIC Sales Return CN VAN posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog(" DamageStock_PIC Sales Return CN VAN posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("DamageStock_PIC Sales Return CN VAN Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + " DamageStock_PIC Sales Return CN VAN Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                                continue;
                            }
                            else
                            {
                                _log.EventLog(" DamageStock_PIC Sales Return CN VAN Entry Posted Successfully with DocNo: " + docno);


                                string Url2 = baseUrl + "/Transactions/Vouchers/Damage Stock";
                                _log.EventLog("DamageStock_PIC post url" + Url2);
                                sessionID = GetSessionId(CompanyId);
                                var response2= Focus8API.Post(Url2, sContent1, sessionID, ref err1);
                                if (response2 != null)
                                {
                                    _log.EventLog("DamageStock_PIC posting Response" + response2.ToString());
                                    var responseData2 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response2);
                                    if (responseData2.result == -1)
                                    {
                                        _log.EventLog(" DamageStock_PIC posting Response failed" + responseData2.result.ToString());
                                        _log.EventLog("DamageStock_PIC Damage Stock Entry Posted Failed with DocNo: " + docno);
                                        _log.ErrLog(response2 + "\n " + " DamageStock_PIC Damage Stock Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData2.message + "\n " + err1);
                                    }
                                    else
                                    {
                                        _log.EventLog(" DamageStock_PIC Damage Stock Entry Posted Successfully with DocNo: " + docno);
                                        int d = _db.setFn("setDamageStockPIC", docno, CompanyId);
                                        if (d != 0)
                                        {
                                            _log.EventLog(" DamageStock_PIC FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                        }
                                        else
                                        {
                                            _log.EventLog(" DamageStock_PIC FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getDamageStockPIC dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getDamageStockPIC dataset is null");
            }
        }

        private void DamageStock_KIF()
        {
            DataSet dsKIF = _db.getFn("getDamageStockKIF", CompanyId);
            if (dsKIF != null)
            {
                if (dsKIF.Tables.Count > 0)
                {
                    _log.EventLog("getDamageStockKIF" + dsKIF.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow KIF in dsKIF.Tables[0].Rows)
                    {
                        docno = KIF["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = KIF["intDate"].ToString().Trim();
                        string SalesAccount = KIF["SalesAccount"].ToString().Trim();
                        string CustomerAC = KIF["CustomerAC"].ToString().Trim();
                        //string DueDate = KIF["intDueDate"].ToString().Trim();
                        string WarehouseCode = KIF["Salesman"].ToString().Trim();
                        string RouteCode = KIF["Salesman"].ToString().Trim();
                        string Narration = KIF["Narration"].ToString().Trim();
                        string Grp = KIF["Grp"].ToString().Trim();
                        string LPONO = "";//KIF["PONo"].ToString().Trim();
                        string PlaceOfSupply = KIF["PlaceOfSupply"].ToString().Trim();
                        string Jurisdiction = KIF["Jurisdiction"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", KIF["DocumentNo"].ToString().Trim() },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "SalesAC__Name", SalesAccount},
                                         { "CustomerAC__Code", CustomerAC },
                                         { "CustomerName__Code", CustomerAC },
                                         //{ "DueDate", Convert.ToInt32(DueDate) },
                                         { "Company Master__Id", 4 },
                                         { "Warehouse__Code", WarehouseCode },
                                         { "Route__Code", RouteCode },
                                         { "sNarration", Narration },
                                         { "Group Customer Master__Name", Grp },
                                         { "LPO_No", LPONO },
                                         { "ReturnType", "1" },
                                         { "Place of supply__Id", PlaceOfSupply},
                                         { "Jurisdiction__Id", Jurisdiction }
                                     };
                        _log.EventLog("DamageStock_KIF header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getDamageStockKIF_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("DamageStock_KIF item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", item["DocumentNo"].ToString().Trim() },
                                         { "Item__Code", item["ItemCode"].ToString().Trim()},
                                         { "Description", item["Description1"].ToString().Trim()},
                                         { "Unit__Id", 24 },
                                         { "StockAC__Id", item["iStocksAccount"].ToString().Trim() },
                                         { "Quantity", Convert.ToDecimal(item["Qty"].ToString().Trim()) },
                                         { "Rate", Convert.ToDecimal(item["Rate"].ToString().Trim()) },
                                         { "Discount %", Convert.ToDecimal(item["Discount2"].ToString().Trim()) },
                                         { "TaxCode__Code", "SR" },
                                         { "VAT", Convert.ToDecimal(item["VAT2"].ToString().Trim()) },
                                         { "Vat Amount", Convert.ToDecimal(item["TotalVATAmount"].ToString().Trim()) }
                                     };
                            lstBody.Add(objBody);
                        }
                        _log.EventLog("DamageStock_KIF Body Data ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("DamageStock_KIF Content" + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Sales Return CN - VAN";
                        _log.EventLog("DamageStock_KIF post url" + Url1);
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("DamageStock_KIF Sales Return CN VAN posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog("DamageStock_KIF Sales Return CN VAN posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("DamageStock_KIF Sales Return CN VAN Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + "Sales Return CN VAN Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                                continue;
                            }
                            else
                            {
                                _log.EventLog("DamageStock_KIF Sales Return CN VAN Entry Posted Successfully with DocNo: " + docno);
                                string Url2 = baseUrl + "/Transactions/Vouchers/Damage Stock";
                                _log.EventLog("DamageStock_KIF post url" + Url2);
                                sessionID = GetSessionId(CompanyId);
                                var response2 = Focus8API.Post(Url2, sContent1, sessionID, ref err1);
                                if (response2 != null)
                                {
                                    _log.EventLog("DamageStock_PIC posting Response" + response2.ToString());
                                    var responseData2 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response2);
                                    if (responseData2.result == -1)
                                    {
                                        _log.EventLog(" DamageStock_KIF posting Response failed" + responseData2.result.ToString());
                                        _log.EventLog("DamageStock_KIF Damage Stock Entry Posted Failed with DocNo: " + docno);
                                        _log.ErrLog(response2 + "\n " + " DamageStock_KIF Damage Stock Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData2.message + "\n " + err1);
                                    }
                                    else
                                    {
                                        _log.EventLog(" DamageStock_KIF Damage Stock Entry Posted Successfully with DocNo: " + docno);
                                        int d = _db.setFn("setDamageStockKIF", docno, CompanyId);
                                        if (d != 0)
                                        {
                                            _log.EventLog(" DamageStock_KIF FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                        }
                                        else
                                        {
                                            _log.EventLog(" DamageStock_KIF FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getDamageStockKIF dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getDamageStockKIF dataset is null");
            }
        }

        private void DamageStock_TB()
        {
            DataSet dsTB = _db.getFn("getDamageStockTB", CompanyId);
            if (dsTB != null)
            {
                if (dsTB.Tables.Count > 0)
                {
                    _log.EventLog("getDamageStockTB" + dsTB.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow TB in dsTB.Tables[0].Rows)
                    {
                        docno = TB["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = TB["intDate"].ToString().Trim();
                        string SalesAccount = TB["SalesAccount"].ToString().Trim();
                        string CustomerAC = TB["CustomerAC"].ToString().Trim();
                        //string DueDate = TB["intDueDate"].ToString().Trim();
                        string WarehouseCode = TB["Salesman"].ToString().Trim();
                        string RouteCode = TB["Salesman"].ToString().Trim();
                        string Narration = TB["Narration"].ToString().Trim();
                        string Grp = TB["Grp"].ToString().Trim();
                        string LPONO = "";//TB["PONo"].ToString().Trim();
                        string PlaceOfSupply = TB["PlaceOfSupply"].ToString().Trim();
                        string Jurisdiction = TB["Jurisdiction"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", TB["DocumentNo"].ToString().Trim() },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "SalesAC__Name", SalesAccount},
                                         { "CustomerAC__Code", CustomerAC },
                                         { "CustomerName__Code", CustomerAC },
                                         //{ "DueDate", Convert.ToInt32(DueDate) },
                                         { "Company Master__Id", 5 },
                                         { "Warehouse__Code", WarehouseCode },
                                         { "Route__Code", RouteCode },
                                         { "sNarration", Narration },
                                         { "Group Customer Master__Name", Grp },
                                         { "LPO_No", LPONO },
                                         { "ReturnType", "1" },
                                         { "Place of supply__Id", PlaceOfSupply},
                                         { "Jurisdiction__Id", Jurisdiction }
                                     };
                        _log.EventLog("DamageStock_TB header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getDamageStockTB_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("DamageStock_TB item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", item["DocumentNo"].ToString().Trim() },
                                         { "Item__Code", item["ItemCode"].ToString().Trim()},
                                         { "Description", item["Description1"].ToString().Trim()},
                                         { "Unit__Id", 24 },
                                         { "StockAC__Id", item["iStocksAccount"].ToString().Trim() },
                                         { "Quantity", Convert.ToDecimal(item["Qty"].ToString().Trim()) },
                                         { "Rate", Convert.ToDecimal(item["Rate"].ToString().Trim()) },
                                         { "Discount %", Convert.ToDecimal(item["Discount2"].ToString().Trim()) },
                                         { "TaxCode__Code", "SR" },
                                         { "VAT", Convert.ToDecimal(item["VAT2"].ToString().Trim()) },
                                         { "Vat Amount", Convert.ToDecimal(item["TotalVATAmount"].ToString().Trim()) }
                                     };
                            lstBody.Add(objBody);
                        }
                        _log.EventLog("DamageStock_TB Body Data ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("DamageStock_TB Content" + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Sales Return CN - VAN";
                        _log.EventLog("DamageStock_TB post url" + Url1);
                        sessionID = GetSessionId(CompanyId); 
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("DamageStock_TB Sales Return CN - VAN posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog("DamageStock_TB Sales Return CN - VAN posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("DamageStock_TB Sales Return CN - VAN Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + " DamageStock_TB Sales Return CN - VAN Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                                continue;
                            }
                            else
                            {
                                _log.EventLog("DamageStock_TB Sales Return CN - VAN Entry Posted Successfully with DocNo: " + docno);
                                string Url2 = baseUrl + "/Transactions/Vouchers/Damage Stock";
                                _log.EventLog("DamageStock_TB post url" + Url2);
                                sessionID = GetSessionId(CompanyId);
                                var response2 = Focus8API.Post(Url2, sContent1, sessionID, ref err1);
                                if (response2 != null)
                                {
                                    _log.EventLog("DamageStock_TB posting Response" + response2.ToString());
                                    var responseData2 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response2);
                                    if (responseData2.result == -1)
                                    {
                                        _log.EventLog(" DamageStock_TB posting Response failed" + responseData2.result.ToString());
                                        _log.EventLog("DamageStock_TB Damage Stock Entry Posted Failed with DocNo: " + docno);
                                        _log.ErrLog(response2 + "\n " + " DamageStock_TB Damage Stock Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData2.message + "\n " + err1);
                                    }
                                    else
                                    {
                                        _log.EventLog(" DamageStock_TB Damage Stock Entry Posted Successfully with DocNo: " + docno);

                                        int d = _db.setFn("setDamageStockTB", docno, CompanyId);
                                        if (d != 0)
                                        {
                                            _log.EventLog("DamageStock_TB FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                        }
                                        else
                                        {
                                            _log.EventLog("DamageStock_TB FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    _log.EventLog("getDamageStockTB dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getDamageStockTB dataset is null");
            }
        }

        private void Receipts_PIC()
        {
            DataSet dsPIC = _db.getFn("getReceiptsPIC", CompanyId);
            if (dsPIC != null)
            {
                if (dsPIC.Tables.Count > 0)
                {
                    _log.EventLog("getReceiptsPIC" + dsPIC.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow Pic in dsPIC.Tables[0].Rows)
                    {
                        docno = Pic["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = Pic["intDate"].ToString().Trim();
                        string CashBankAC__Name = Pic["CashBankAC"].ToString().Trim();
                        string RouteCode = Pic["Salesman"].ToString().Trim();
                        string Narration = Pic["Narration"].ToString().Trim();
                        string Grp = Pic["Grp"].ToString().Trim();
                        string sChequeNo = Pic["ChequeNo"].ToString().Trim();
                        string CustomerAc = Pic["CustomerAC"].ToString().Trim();
                        string currencyId = Pic["currencyId"].ToString().Trim();
                        string AcId = Pic["customerid"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", docno },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "CashBankAC__Name", CashBankAC__Name},
                                         { "CollectedIn__Id", 3 },
                                         //{ "Route__Code", RouteCode },
                                         { "sNarration", Narration },
                                         { "Group Customer Master__Name", Grp },
                                         { "sChequeNo", sChequeNo },
                                         { "Currency__Id", currencyId },
                                         //{ "Salesman__Code", RouteCode }
                                     };
                        _log.EventLog("Receipts_PIC header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        List<Hashtable> listbillRef = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getReceiptsPIC_Body,@p1="+docno, CompanyId);
                        _log.EventLog("Receipts_PIC item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if(dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        bool BodyLoopBreaks = false;
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            string refDocNo = "";
                            string sql = $@"exec pCore_CommonSp @Operation=InvoiceCheckingPIC, @p1='{item["InvoiceNumber"].ToString().Trim()}'";
                            _log.EventLog("Receipts_PIC sql" + sql);
                            DataSet dsInvoice = _db.GetData(sql, CompanyId, ref error);
                            if (dsInvoice != null)
                            {
                                if (dsInvoice.Tables.Count > 0)
                                {
                                    if (dsInvoice.Tables[0].Rows.Count > 0)
                                    {
                                        if (dsInvoice.Tables[0].Rows[0][0].ToString() == "1") // Invoice Found
                                        {
                                            refDocNo = item["InvoiceNumber"].ToString().Trim();
                                            _log.EventLog("Invoice Found");
                                        }
                                        else if (dsInvoice.Tables[0].Rows[0][0].ToString() == "2") // Invoice Found in Opening Balance
                                        {
                                            refDocNo = dsInvoice.Tables[0].Rows[0]["sVoucherNo"].ToString();
                                            _log.EventLog("Invoice Found as Opening Balance");
                                        }
                                        else if (dsInvoice.Tables[0].Rows[0][0].ToString() == "-1") //New Reference
                                        {
                                            refDocNo = "";
                                            _log.EventLog("Invoice Not Found in Winit DB. Post as New Reference");
                                        }
                                        else if (dsInvoice.Tables[0].Rows[0][0].ToString() == "0")
                                        {
                                            if (dsInvoice.Tables.Count == 3)
                                            {
                                                DataSet dsInv = new DataSet();
                                                if (dsInvoice.Tables[1].Rows.Count > 0)
                                                {
                                                    if (Convert.ToDateTime(dsInvoice.Tables[1].Rows[0]["tDate"]) >= Convert.ToDateTime(dsInvoice.Tables[0].Rows[0]["Startdate"]))
                                                    {
                                                        dsInv.Merge(dsInvoice.Tables[1]);
                                                        dsInv.AcceptChanges();
                                                        if (dsInvoice.Tables[2].Rows.Count > 0)
                                                        {
                                                            dsInv.Merge(dsInvoice.Tables[2]);
                                                            dsInv.AcceptChanges();
                                                            SalesInvoice_PIC(dsInv);
                                                            if (InvFlag)
                                                            {
                                                                refDocNo = item["InvoiceNumber"].ToString().Trim();
                                                                _log.EventLog("Invoice Posted");
                                                            }
                                                            else
                                                            {
                                                                _log.EventLog("Invoice Not Posted");
                                                                BodyLoopBreaks = true;
                                                                break;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            _log.EventLog("NOT dsInvoice.Tables[2].Rows.Count > 0");
                                                            BodyLoopBreaks = true;
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        refDocNo = "";
                                                        _log.EventLog("Invoice Transaction date is behind the Accounting Date");
                                                    }
                                                }
                                                else
                                                {
                                                    _log.EventLog("NOT dsInvoice.Tables[1].Rows.Count > 0");
                                                    BodyLoopBreaks = true;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                _log.EventLog("NOT dsInvoice.Tables.Count == 3");
                                                BodyLoopBreaks = true;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        _log.EventLog("NOT dsInvoice.Tables[0].Rows.Count > 0");
                                        BodyLoopBreaks = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    _log.EventLog("NOT dsInvoice.Tables.Count > 0");
                                    BodyLoopBreaks = true;
                                    break;
                                }
                            }
                            else
                            {
                                _log.EventLog("dsInvoice is null \n" + error);
                                BodyLoopBreaks = true;
                                break;
                            }
                            if (refDocNo == "")
                            {

                            }
                            else
                            {
                                DataTable dt = _db.GetIRef(refDocNo, CompanyId);
                                string refid= "0";
                                if (dt != null)
                                {
                                    _log.EventLog("BillRef Datatable count = " + dt.Rows.Count);
                                    _log.EventLog("BillRef iref = " + dt.Rows[0]["RefId"]);
                                    refid = dt.Rows[0]["RefId"].ToString();
                                }
                                else
                                {
                                    _log.EventLog("could not find reference.");
                                }
                                Hashtable billRef = new Hashtable();
                                billRef.Add("CustomerId", AcId);
                                billRef.Add("Amount", Convert.ToDecimal(item["Amount"].ToString().Trim()));
                                billRef.Add("reftype", 2);
                                billRef.Add("Reference", refDocNo.Trim());
                                billRef.Add("ref", Convert.ToInt32(refid));
                                listbillRef.Add(billRef);
                                _log.EventLog("Added in BillRef Array");
                            }
                        }
                        if (BodyLoopBreaks)
                        {
                            _log.EventLog("BodyLoopBreaks");
                            continue;
                        }
                        
                        Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", docno },
                                         { "Amount", Convert.ToDecimal(Pic["Amount"].ToString().Trim()) },
                                         { "Reference", listbillRef },
                                         { "Account__Id", AcId },
                                         { "Route__Code", RouteCode },
                                         { "Company Master__Id", 3 },
                                         { "Group Customer Master__Name", Grp },
                                     };
                        lstBody.Add(objBody);
                        _log.EventLog("Receipts_PIC Body Data ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("Receipts_PIC Content" + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Receipts";
                        _log.EventLog("Receipts_PIC post url" + Url1);
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("Receipts_PIC posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog(" Receipts_PIC posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("Receipts_PIC Receipts Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + " Receipts_PIC Receipts Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                            }
                            else
                            {
                                _log.EventLog(" Receipts_PIC Receipts Entry Posted Successfully with DocNo: " + docno);
                                int d = _db.setFn("setReceiptsPIC", docno, CompanyId);
                                if (d != 0)
                                {
                                    _log.EventLog(" Receipts_PIC FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                }
                                else
                                {
                                    _log.EventLog(" Receipts_PIC FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getReceiptsPIC dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getReceiptsPIC dataset is null");
            }
        }

        private void Receipts_KIF()
        {
            DataSet dsKIF = _db.getFn("getReceiptsKIF", CompanyId);
            if (dsKIF != null)
            {
                if (dsKIF.Tables.Count > 0)
                {
                    _log.EventLog("getReceiptsKIF" + dsKIF.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow KIF in dsKIF.Tables[0].Rows)
                    {
                        docno = KIF["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = KIF["intDate"].ToString().Trim();
                        string CashBankAC__Name = KIF["CashBankAC"].ToString().Trim();
                        string RouteCode = KIF["Salesman"].ToString().Trim();
                        string Narration = KIF["Narration"].ToString().Trim();
                        string Grp = KIF["Grp"].ToString().Trim();
                        string sChequeNo = KIF["ChequeNo"].ToString().Trim();
                        string CustomerAc = KIF["CustomerAC"].ToString().Trim();
                        string currencyId = KIF["currencyId"].ToString().Trim();
                        string AcId = KIF["customerid"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", docno },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "CashBankAC__Name", CashBankAC__Name},
                                         { "CollectedIn__Id", 4 },
                                         //{ "Route__Code", RouteCode },
                                         { "sNarration", Narration },
                                         { "Group Customer Master__Name", Grp },
                                         { "sChequeNo", sChequeNo },
                                         { "Currency__Id", currencyId },
                                         //{ "Salesman__Code", RouteCode }
                                     };
                        _log.EventLog("Receipts_KIF header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        List<Hashtable> listbillRef = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getReceiptsKIF_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("Receipts_KIF item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        bool BodyLoopBreaks = false;
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            string refDocNo = "";
                            string sql = $@"exec pCore_CommonSp @Operation=InvoiceCheckingKIF, @p1='{item["InvoiceNumber"].ToString().Trim()}'";
                            DataSet dsInvoice = _db.GetData(sql, CompanyId, ref error);
                            if (dsInvoice != null)
                            {
                                if (dsInvoice.Tables.Count > 0)
                                {
                                    if (dsInvoice.Tables[0].Rows.Count > 0)
                                    {
                                        if (dsInvoice.Tables[0].Rows[0][0].ToString() == "1") // Invoice Found
                                        {
                                            refDocNo = item["InvoiceNumber"].ToString().Trim();
                                            _log.EventLog("Invoice Found");
                                        }
                                        if (dsInvoice.Tables[0].Rows[0][0].ToString() == "2") // Invoice Found in Opening Balance
                                        {
                                            refDocNo = dsInvoice.Tables[0].Rows[0]["sVoucherNo"].ToString();
                                            _log.EventLog("Invoice Found as Opening Balance");
                                        }
                                        else if (dsInvoice.Tables[0].Rows[0][0].ToString() == "-1") //New Reference
                                        {
                                            refDocNo = "";
                                            _log.EventLog("Invoice Not Found in Winit DB. Post as New Reference");
                                        }
                                        else if (dsInvoice.Tables[0].Rows[0][0].ToString() == "0")
                                        {
                                            if (dsInvoice.Tables.Count == 3)
                                            {
                                                DataSet dsInv = new DataSet();
                                                if (dsInvoice.Tables[1].Rows.Count > 0)
                                                {
                                                    if (Convert.ToDateTime(dsInvoice.Tables[1].Rows[0]["tDate"]) >= Convert.ToDateTime(dsInvoice.Tables[0].Rows[0]["Startdate"]))
                                                    {
                                                        dsInv.Merge(dsInvoice.Tables[1]);
                                                        dsInv.AcceptChanges();
                                                        if (dsInvoice.Tables[2].Rows.Count > 0)
                                                        {
                                                            dsInv.Merge(dsInvoice.Tables[2]);
                                                            dsInv.AcceptChanges();
                                                            SalesInvoice_PIC(dsInv);
                                                            if (InvFlag)
                                                            {
                                                                refDocNo = item["InvoiceNumber"].ToString().Trim();
                                                                _log.EventLog("Invoice Posted");
                                                            }
                                                            else
                                                            {
                                                                _log.EventLog("Invoice Not Posted");
                                                                BodyLoopBreaks = true;
                                                                break;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            _log.EventLog("NOT dsInvoice.Tables[2].Rows.Count > 0");
                                                            BodyLoopBreaks = true;
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        refDocNo = "";
                                                        _log.EventLog("Invoice Transaction date is behind the Accounting Date");
                                                    }
                                                }
                                                else
                                                {
                                                    _log.EventLog("NOT dsInvoice.Tables[1].Rows.Count > 0");
                                                    BodyLoopBreaks = true;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                _log.EventLog("NOT dsInvoice.Tables.Count == 3");
                                                BodyLoopBreaks = true;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        _log.EventLog("NOT dsInvoice.Tables[0].Rows.Count > 0");
                                        BodyLoopBreaks = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    _log.EventLog("NOT dsInvoice.Tables.Count > 0");
                                    BodyLoopBreaks = true;
                                    break;
                                }
                            }
                            else
                            {
                                _log.EventLog("dsInvoice is null \n" + error);
                                BodyLoopBreaks = true;
                                break;
                            }
                            if (refDocNo == "")
                            {

                            }
                            else
                            {
                                DataTable dt = _db.GetIRef(refDocNo, CompanyId);
                                string refid = "0";
                                if (dt != null)
                                {
                                    _log.EventLog("BillRef Datatable count = " + dt.Rows.Count);
                                    _log.EventLog("BillRef iref = " + dt.Rows[0]["RefId"]);
                                    refid = dt.Rows[0]["RefId"].ToString();
                                }
                                else
                                {
                                    _log.EventLog("could not find reference.");
                                }
                                Hashtable billRef = new Hashtable();
                                billRef.Add("CustomerId", AcId);
                                billRef.Add("Amount", Convert.ToDecimal(item["Amount"].ToString().Trim()));
                                billRef.Add("reftype", 2);
                                billRef.Add("Reference", refDocNo.Trim());
                                billRef.Add("ref", Convert.ToInt32(refid));
                                listbillRef.Add(billRef);
                            }
                        }
                        if (BodyLoopBreaks)
                        {
                            _log.EventLog("BodyLoopBreaks");
                            continue;
                        }
                        Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", docno },
                                         { "Amount", Convert.ToDecimal(KIF["Amount"].ToString().Trim()) },
                                         { "Reference", listbillRef },
                                         { "Account__Id", AcId },
                                         { "Route__Code", RouteCode },
                                         { "Company Master__Id", 4 },
                                         { "Group Customer Master__Name", Grp },
                                     };
                        lstBody.Add(objBody);
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Receipts";
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog("posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("Receipts Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + "Receipts Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                            }
                            else
                            {
                                _log.EventLog("Receipts Entry Posted Successfully with DocNo: " + docno);
                                int d = _db.setFn("setReceiptsKIF", docno, CompanyId);
                                if (d != 0)
                                {
                                    _log.EventLog(" Receipts_KIF FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                }
                                else
                                {
                                    _log.EventLog(" Receipts_KIF FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getReceiptsKIF dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getReceiptsKIF dataset is null");
            }
        }

        private void Receipts_TB()
        {
            DataSet dsTB = _db.getFn("getReceiptsTB", CompanyId);
            if (dsTB != null)
            {
                if (dsTB.Tables.Count > 0)
                {
                    _log.EventLog("getReceiptsTB" + dsTB.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow TB in dsTB.Tables[0].Rows)
                    {
                        docno = TB["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = TB["intDate"].ToString().Trim();
                        string CashBankAC__Name = TB["CashBankAC"].ToString().Trim();
                        string RouteCode = TB["Salesman"].ToString().Trim();
                        string Narration = TB["Narration"].ToString().Trim();
                        string Grp = TB["Grp"].ToString().Trim();
                        string sChequeNo = TB["ChequeNo"].ToString().Trim();
                        string CustomerAc = TB["CustomerAC"].ToString().Trim();
                        string currencyId = TB["currencyId"].ToString().Trim();
                        string AcId = TB["customerid"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", docno },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "CashBankAC__Name", CashBankAC__Name},
                                         { "CollectedIn__Id", 5 },
                                         //{ "Route__Code", RouteCode },
                                         { "sNarration", Narration },
                                         { "Group Customer Master__Name", Grp },
                                         { "sChequeNo", sChequeNo },
                                         { "Currency__Id", currencyId },
                                         //{ "Salesman__Code", RouteCode }
                                     };
                        _log.EventLog("Receipts_TB header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        List<Hashtable> listbillRef = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getReceiptsTB_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("Receipts_TB item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        bool BodyLoopBreaks = false;
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            string refDocNo = "";
                            string sql = $@"exec pCore_CommonSp @Operation=InvoiceCheckingTB, @p1='{item["InvoiceNumber"].ToString().Trim()}'";
                            DataSet dsInvoice = _db.GetData(sql, CompanyId, ref error);
                            if (dsInvoice != null)
                            {
                                if (dsInvoice.Tables.Count > 0)
                                {
                                    if (dsInvoice.Tables[0].Rows.Count > 0)
                                    {
                                        if (dsInvoice.Tables[0].Rows[0][0].ToString() == "1") // Invoice Found
                                        {
                                            refDocNo = item["InvoiceNumber"].ToString().Trim();
                                            _log.EventLog("Invoice Found");
                                        }
                                        if (dsInvoice.Tables[0].Rows[0][0].ToString() == "2") // Invoice Found in Opening Balance
                                        {
                                            refDocNo = dsInvoice.Tables[0].Rows[0]["sVoucherNo"].ToString();
                                            _log.EventLog("Invoice Found as Opening Balance");
                                        }
                                        else if (dsInvoice.Tables[0].Rows[0][0].ToString() == "-1") //New Reference
                                        {
                                            refDocNo = "";
                                            _log.EventLog("Invoice Not Found in Winit DB. Post as New Reference");
                                        }
                                        else if (dsInvoice.Tables[0].Rows[0][0].ToString() == "0")
                                        {
                                            if (dsInvoice.Tables.Count == 3)
                                            {
                                                DataSet dsInv = new DataSet();
                                                if (dsInvoice.Tables[1].Rows.Count > 0)
                                                {
                                                    if (Convert.ToDateTime(dsInvoice.Tables[1].Rows[0]["tDate"]) >= Convert.ToDateTime(dsInvoice.Tables[0].Rows[0]["Startdate"]))
                                                    {
                                                        dsInv.Merge(dsInvoice.Tables[1]);
                                                        dsInv.AcceptChanges();
                                                        if (dsInvoice.Tables[2].Rows.Count > 0)
                                                        {
                                                            dsInv.Merge(dsInvoice.Tables[2]);
                                                            dsInv.AcceptChanges();
                                                            SalesInvoice_PIC(dsInv);
                                                            if (InvFlag)
                                                            {
                                                                refDocNo = item["InvoiceNumber"].ToString().Trim();
                                                                _log.EventLog("Invoice Posted");
                                                            }
                                                            else
                                                            {
                                                                _log.EventLog("Invoice Not Posted");
                                                                BodyLoopBreaks = true;
                                                                break;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            _log.EventLog("NOT dsInvoice.Tables[2].Rows.Count > 0");
                                                            BodyLoopBreaks = true;
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        refDocNo = "";
                                                        _log.EventLog("Invoice Transaction date is behind the Accounting Date");
                                                    }
                                                }
                                                else
                                                {
                                                    _log.EventLog("NOT dsInvoice.Tables[1].Rows.Count > 0");
                                                    BodyLoopBreaks = true;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                _log.EventLog("NOT dsInvoice.Tables.Count == 3");
                                                BodyLoopBreaks = true;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        _log.EventLog("NOT dsInvoice.Tables[0].Rows.Count > 0");
                                        BodyLoopBreaks = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    _log.EventLog("NOT dsInvoice.Tables.Count > 0");
                                    BodyLoopBreaks = true;
                                    break;
                                }
                            }
                            else
                            {
                                _log.EventLog("dsInvoice is null \n" + error);
                                BodyLoopBreaks = true;
                                break;
                            }
                            if (refDocNo == "")
                            {

                            }
                            else
                            {
                                DataTable dt = _db.GetIRef(refDocNo, CompanyId);
                                string refid = "0";
                                if (dt != null)
                                {
                                    _log.EventLog("BillRef Datatable count = " + dt.Rows.Count);
                                    _log.EventLog("BillRef iref = " + dt.Rows[0]["RefId"]);
                                    refid = dt.Rows[0]["RefId"].ToString();
                                }
                                else
                                {
                                    _log.EventLog("could not find reference.");
                                }
                                Hashtable billRef = new Hashtable();
                                billRef.Add("CustomerId", AcId);
                                billRef.Add("Amount", Convert.ToDecimal(item["Amount"].ToString().Trim()));
                                billRef.Add("reftype", 2);
                                billRef.Add("Reference", refDocNo.Trim());
                                billRef.Add("ref", Convert.ToInt32(refid));
                                listbillRef.Add(billRef);
                            }
                        }
                        if (BodyLoopBreaks)
                        {
                            _log.EventLog("BodyLoopBreaks");
                            continue;
                        }
                        Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", docno },
                                         { "Amount", Convert.ToDecimal(TB["Amount"].ToString().Trim()) },
                                         { "Reference", listbillRef },
                                         { "Account__Id", AcId },
                                         { "Route__Code", RouteCode },
                                         { "Company Master__Id", 5 },
                                         { "Group Customer Master__Name", Grp },
                                     };
                        lstBody.Add(objBody);
                        _log.EventLog("Receipts_TB Body Data ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("Receipts_TB Content" + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Receipts";
                        _log.EventLog("Receipts_TB post url" + Url1);
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("Receipts_TB posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog("Receipts_TB posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("Receipts_TB Receipts Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + " Receipts_TB Receipts Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                            }
                            else
                            {
                                _log.EventLog("Receipts_TB Receipts Entry Posted Successfully with DocNo: " + docno);
                                int d = _db.setFn("setReceiptsTB", docno, CompanyId);
                                if (d != 0)
                                {
                                    _log.EventLog("Receipts_TB FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                }
                                else
                                {
                                    _log.EventLog("Receipts_TB FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getReceiptsTB dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getReceiptsTB dataset is null");
            }
        }

        private void PDCReceipts_PIC()
        {
            DataSet dsPIC = _db.getFn("getPDCReceiptsPIC", CompanyId);
            if (dsPIC != null)
            {
                if (dsPIC.Tables.Count > 0)
                {
                    _log.EventLog("getPDCReceiptsPIC" + dsPIC.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow Pic in dsPIC.Tables[0].Rows)
                    {
                        docno = Pic["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = Pic["intDate"].ToString().Trim();
                        int mDate = DB_lib.GetDateToInt(Convert.ToDateTime(Pic["mDate"].ToString().Trim()));
                        string CashBankAC__Name = Pic["CashBankAC"].ToString().Trim();
                        string RouteCode = Pic["Salesman"].ToString().Trim();
                        string Narration = Pic["Narration"].ToString().Trim();
                        string Grp = Pic["Grp"].ToString().Trim();
                        string sChequeNo = Pic["ChequeNo"].ToString().Trim();
                        string CustomerAc = Pic["CustomerAC"].ToString().Trim();
                        string currencyId = Pic["currencyId"].ToString().Trim();
                        string AcId = Pic["customerid"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", docno },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "CashBankAC__Name", CashBankAC__Name},
                                         { "CollectedIn__Id", 3 },
                                         { "MaturityDate", mDate },
                                         { "sNarration", Narration },
                                         { "Group Customer Master__Name", Grp },
                                         { "sChequeNo", sChequeNo },
                                         { "Currency__Id", currencyId },
                                         //{ "Salesman__Code", RouteCode }
                                     };
                        _log.EventLog("PDCReceipts_PIC header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        List<Hashtable> listbillRef = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getPDCReceiptsPIC_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("PDCReceipts_PIC item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        bool BodyLoopBreaks = false;
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            string refDocNo = "";
                            string sql = $@"exec pCore_CommonSp @Operation=InvoiceCheckingPIC, @p1='{item["InvoiceNumber"].ToString().Trim()}'";
                            DataSet dsInvoice = _db.GetData(sql, CompanyId,ref error);
                            if(dsInvoice != null)
                            {
                                if (dsInvoice.Tables.Count > 0)
                                {
                                    if (dsInvoice.Tables[0].Rows.Count > 0)
                                    {
                                        if (dsInvoice.Tables[0].Rows[0][0].ToString() == "1") // Invoice Found
                                        {
                                            refDocNo = item["InvoiceNumber"].ToString().Trim();
                                            _log.EventLog("Invoice Found");
                                        }
                                        if (dsInvoice.Tables[0].Rows[0][0].ToString() == "2") // Invoice Found in Opening Balance
                                        {
                                            refDocNo = dsInvoice.Tables[0].Rows[0]["sVoucherNo"].ToString();
                                            _log.EventLog("Invoice Found as Opening Balance");
                                        }
                                        else if (dsInvoice.Tables[0].Rows[0][0].ToString() == "-1") //New Reference
                                        {
                                            refDocNo = "";
                                            _log.EventLog("Invoice Not Found in Winit DB. Post as New Reference");
                                        }
                                        else if (dsInvoice.Tables[0].Rows[0][0].ToString() == "0")
                                        {
                                            if (dsInvoice.Tables.Count == 3)
                                            {
                                                DataSet dsInv = new DataSet();
                                                if (dsInvoice.Tables[1].Rows.Count > 0)
                                                {
                                                    if (Convert.ToDateTime(dsInvoice.Tables[1].Rows[0]["tDate"]) >= Convert.ToDateTime(dsInvoice.Tables[0].Rows[0]["Startdate"]))
                                                    {
                                                        dsInv.Merge(dsInvoice.Tables[1]);
                                                        dsInv.AcceptChanges();
                                                        if (dsInvoice.Tables[2].Rows.Count > 0)
                                                        {
                                                            dsInv.Merge(dsInvoice.Tables[2]);
                                                            dsInv.AcceptChanges();
                                                            SalesInvoice_PIC(dsInv);
                                                            if (InvFlag)
                                                            {
                                                                refDocNo = item["InvoiceNumber"].ToString().Trim();
                                                                _log.EventLog("Invoice Posted");
                                                            }
                                                            else
                                                            {
                                                                _log.EventLog("Invoice Not Posted");
                                                                BodyLoopBreaks = true;
                                                                break;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            _log.EventLog("NOT dsInvoice.Tables[2].Rows.Count > 0");
                                                            BodyLoopBreaks = true;
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        refDocNo = "";
                                                        _log.EventLog("Invoice Transaction date is behind the Accounting Date");
                                                    }
                                                }
                                                else
                                                {
                                                    _log.EventLog("NOT dsInvoice.Tables[1].Rows.Count > 0");
                                                    BodyLoopBreaks = true;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                _log.EventLog("NOT dsInvoice.Tables.Count == 3");
                                                BodyLoopBreaks = true;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        _log.EventLog("NOT dsInvoice.Tables[0].Rows.Count > 0");
                                        BodyLoopBreaks = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    _log.EventLog("NOT dsInvoice.Tables.Count > 0");
                                    BodyLoopBreaks = true;
                                    break;
                                }
                            }
                            else
                            {
                                _log.EventLog("dsInvoice is null \n" + error);
                                BodyLoopBreaks = true;
                                break;
                            }
                            if (refDocNo == "")
                            {

                            }
                            else
                            {
                                DataTable dt = _db.GetIRef(refDocNo, CompanyId);
                                string refid = "0";
                                if (dt != null)
                                {
                                    _log.EventLog("BillRef Datatable count = " + dt.Rows.Count);
                                    _log.EventLog("BillRef iref = " + dt.Rows[0]["RefId"]);
                                    refid = dt.Rows[0]["RefId"].ToString();
                                }
                                else
                                {
                                    _log.EventLog("could not find reference.");
                                }
                                Hashtable billRef = new Hashtable();
                                billRef.Add("CustomerId", AcId);
                                billRef.Add("Amount", Convert.ToDecimal(item["Amount"].ToString().Trim()));
                                billRef.Add("reftype", 2);
                                billRef.Add("Reference", refDocNo.Trim());
                                billRef.Add("ref", Convert.ToInt32(refid));
                                listbillRef.Add(billRef);
                            }
                        }
                        if (BodyLoopBreaks)
                        {
                            _log.EventLog("BodyLoopBreaks");
                            continue;
                        }
                        Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", docno },
                                         { "Amount", Convert.ToDecimal(Pic["Amount"].ToString().Trim()) },
                                         { "Reference", listbillRef },
                                         { "Account__Id", AcId },
                                         { "Route__Code", RouteCode },
                                         { "Company Master__Id", 3 },
                                         { "Group Customer Master__Name", Grp },
                                     };
                        lstBody.Add(objBody);
                        _log.EventLog("PDCReceipts_PIC Body Data ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("PDCReceipts_PIC Content" + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Post-Dated Receipts";
                        _log.EventLog("PDCReceipts_PIC post url" + Url1);
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("PDCReceipts_PIC posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog(" PDCReceipts_PIC posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("PDCReceipts_PIC PDCReceipts Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + " PDCReceipts_PIC PDCReceipts Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                            }
                            else
                            {
                                _log.EventLog(" PDCReceipts_PIC PDCReceipts Entry Posted Successfully with DocNo: " + docno);
                                int d = _db.setFn("setPDCReceiptsPIC", docno, CompanyId);
                                if (d != 0)
                                {
                                    _log.EventLog(" PDCReceipts_PIC FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                }
                                else
                                {
                                    _log.EventLog(" PDCReceipts_PIC FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getPDCReceiptsPIC dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getPDCReceiptsPIC dataset is null");
            }
        }

        private void PDCReceipts_KIF()
        {
            DataSet dsKIF = _db.getFn("getPDCReceiptsKIF", CompanyId);
            if (dsKIF != null)
            {
                if (dsKIF.Tables.Count > 0)
                {
                    _log.EventLog("getPDCReceiptsKIF" + dsKIF.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow KIF in dsKIF.Tables[0].Rows)
                    {
                        docno = KIF["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = KIF["intDate"].ToString().Trim();
                        int mDate = DB_lib.GetDateToInt(Convert.ToDateTime(KIF["mDate"].ToString().Trim()));
                        string CashBankAC__Name = KIF["CashBankAC"].ToString().Trim();
                        string RouteCode = KIF["Salesman"].ToString().Trim();
                        string Narration = KIF["Narration"].ToString().Trim();
                        string Grp = KIF["Grp"].ToString().Trim();
                        string sChequeNo = KIF["ChequeNo"].ToString().Trim();
                        string CustomerAc = KIF["CustomerAC"].ToString().Trim();
                        string currencyId = KIF["currencyId"].ToString().Trim();
                        string AcId = KIF["customerid"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", docno },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "CashBankAC__Name", CashBankAC__Name},
                                         { "CollectedIn__Id", 4 },
                                         { "MaturityDate", mDate },
                                         { "sNarration", Narration },
                                         { "Group Customer Master__Name", Grp },
                                         { "sChequeNo", sChequeNo },
                                         { "Currency__Id", currencyId },
                                         //{ "Salesman__Code", RouteCode }
                                     };
                        _log.EventLog("PDCReceipts_KIF header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        List<Hashtable> listbillRef = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getPDCReceiptsKIF_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("PDCReceipts_KIF item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        bool BodyLoopBreaks = false;
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            string refDocNo = "";
                            string sql = $@"exec pCore_CommonSp @Operation=InvoiceCheckingKIF, @p1='{item["InvoiceNumber"].ToString().Trim()}'";
                            DataSet dsInvoice = _db.GetData(sql, CompanyId, ref error);
                            if (dsInvoice != null)
                            {
                                if (dsInvoice.Tables.Count > 0)
                                {
                                    if (dsInvoice.Tables[0].Rows.Count > 0)
                                    {
                                        if (dsInvoice.Tables[0].Rows[0][0].ToString() == "1") // Invoice Found
                                        {
                                            refDocNo = item["InvoiceNumber"].ToString().Trim();
                                            _log.EventLog("Invoice Found");
                                        }
                                        if (dsInvoice.Tables[0].Rows[0][0].ToString() == "2") // Invoice Found in Opening Balance
                                        {
                                            refDocNo = dsInvoice.Tables[0].Rows[0]["sVoucherNo"].ToString();
                                            _log.EventLog("Invoice Found as Opening Balance");
                                        }
                                        else if (dsInvoice.Tables[0].Rows[0][0].ToString() == "-1") //New Reference
                                        {
                                            refDocNo = "";
                                            _log.EventLog("Invoice Not Found in Winit DB. Post as New Reference");
                                        }
                                        else if (dsInvoice.Tables[0].Rows[0][0].ToString() == "0")
                                        {
                                            if (dsInvoice.Tables.Count == 3)
                                            {
                                                DataSet dsInv = new DataSet();
                                                if (dsInvoice.Tables[1].Rows.Count > 0)
                                                {
                                                    if (Convert.ToDateTime(dsInvoice.Tables[1].Rows[0]["tDate"]) >= Convert.ToDateTime(dsInvoice.Tables[0].Rows[0]["Startdate"]))
                                                    {
                                                        dsInv.Merge(dsInvoice.Tables[1]);
                                                        dsInv.AcceptChanges();
                                                        if (dsInvoice.Tables[2].Rows.Count > 0)
                                                        {
                                                            dsInv.Merge(dsInvoice.Tables[2]);
                                                            dsInv.AcceptChanges();
                                                            SalesInvoice_PIC(dsInv);
                                                            if (InvFlag)
                                                            {
                                                                refDocNo = item["InvoiceNumber"].ToString().Trim();
                                                                _log.EventLog("Invoice Posted");
                                                            }
                                                            else
                                                            {
                                                                _log.EventLog("Invoice Not Posted");
                                                                BodyLoopBreaks = true;
                                                                break;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            _log.EventLog("NOT dsInvoice.Tables[2].Rows.Count > 0");
                                                            BodyLoopBreaks = true;
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        refDocNo = "";
                                                        _log.EventLog("Invoice Transaction date is behind the Accounting Date");
                                                    }
                                                }
                                                else
                                                {
                                                    _log.EventLog("NOT dsInvoice.Tables[1].Rows.Count > 0");
                                                    BodyLoopBreaks = true;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                _log.EventLog("NOT dsInvoice.Tables.Count == 3");
                                                BodyLoopBreaks = true;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        _log.EventLog("NOT dsInvoice.Tables[0].Rows.Count > 0");
                                        BodyLoopBreaks = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    _log.EventLog("NOT dsInvoice.Tables.Count > 0");
                                    BodyLoopBreaks = true;
                                    break;
                                }
                            }
                            else
                            {
                                _log.EventLog("dsInvoice is null \n" + error);
                                BodyLoopBreaks = true;
                                break;
                            }
                            if (refDocNo == "")
                            {

                            }
                            else
                            {
                                DataTable dt = _db.GetIRef(refDocNo, CompanyId);
                                string refid = "0";
                                if (dt != null)
                                {
                                    _log.EventLog("BillRef Datatable count = " + dt.Rows.Count);
                                    _log.EventLog("BillRef iref = " + dt.Rows[0]["RefId"]);
                                    refid = dt.Rows[0]["RefId"].ToString();
                                }
                                else
                                {
                                    _log.EventLog("could not find reference.");
                                }
                                Hashtable billRef = new Hashtable();
                                billRef.Add("CustomerId", AcId);
                                billRef.Add("Amount", Convert.ToDecimal(item["Amount"].ToString().Trim()));
                                billRef.Add("reftype", 2);
                                billRef.Add("Reference", refDocNo.Trim());
                                billRef.Add("ref", Convert.ToInt32(refid));
                                listbillRef.Add(billRef);
                            }
                        }
                        if (BodyLoopBreaks)
                        {
                            _log.EventLog("BodyLoopBreaks");
                            continue;
                        }
                        Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", docno },
                                         { "Amount", Convert.ToDecimal(KIF["Amount"].ToString().Trim()) },
                                         { "Reference", listbillRef },
                                         { "Account__Id", AcId },
                                         { "Route__Code", RouteCode },
                                         { "Company Master__Id", 4 },
                                         { "Group Customer Master__Name", Grp },
                                     };
                        lstBody.Add(objBody);
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Post-Dated Receipts";
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog("posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("PDCReceipts Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + "PDCReceipts Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                            }
                            else
                            {
                                _log.EventLog("PDCReceipts Entry Posted Successfully with DocNo: " + docno);
                                int d = _db.setFn("setPDCReceiptsKIF", docno, CompanyId);
                                if (d != 0)
                                {
                                    _log.EventLog(" PDCReceipts_KIF FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                }
                                else
                                {
                                    _log.EventLog(" PDCReceipts_KIF FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getPDCReceiptsKIF dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getPDCReceiptsKIF dataset is null");
            }
        }

        private void PDCReceipts_TB()
        {
            DataSet dsTB = _db.getFn("getPDCReceiptsTB", CompanyId);
            if (dsTB != null)
            {
                if (dsTB.Tables.Count > 0)
                {
                    _log.EventLog("getPDCReceiptsTB" + dsTB.Tables.Count.ToString());
                    string docno = "";
                    foreach (DataRow TB in dsTB.Tables[0].Rows)
                    {
                        docno = TB["DocumentNo"].ToString().Trim();
                        _log.EventLog("docno" + docno);
                        string idate = TB["intDate"].ToString().Trim();
                        int mDate = DB_lib.GetDateToInt(Convert.ToDateTime(TB["mDate"].ToString().Trim()));
                        string CashBankAC__Name = TB["CashBankAC"].ToString().Trim();
                        string RouteCode = TB["Salesman"].ToString().Trim();
                        string Narration = TB["Narration"].ToString().Trim();
                        string Grp = TB["Grp"].ToString().Trim();
                        string sChequeNo = TB["ChequeNo"].ToString().Trim();
                        string CustomerAc = TB["CustomerAC"].ToString().Trim();
                        string currencyId = TB["currencyId"].ToString().Trim();
                        string AcId = TB["customerid"].ToString().Trim();
                        Hashtable header = new Hashtable
                                     {
                                         { "DocNo", docno },
                                         { "Date", Convert.ToInt32(idate)},
                                         { "CashBankAC__Name", CashBankAC__Name},
                                         { "CollectedIn__Id", 5},
                                         { "MaturityDate", mDate },
                                         { "sNarration", Narration },
                                         { "Group Customer Master__Name", Grp },
                                         { "sChequeNo", sChequeNo },
                                         { "Currency__Id", currencyId },
                                         //{ "Salesman__Code", RouteCode }
                                     };
                        _log.EventLog("PDCReceipts_TB header Data ready");
                        List<Hashtable> lstBody = new List<Hashtable>();
                        List<Hashtable> listbillRef = new List<Hashtable>();
                        DataSet dsBody = _db.getFn("getPDCReceiptsTB_Body,@p1=" + docno, CompanyId);
                        _log.EventLog("PDCReceipts_TB item rows count" + dsBody.Tables[0].Rows.Count.ToString());
                        if (dsBody.Tables[0].Rows.Count == 0)
                        {
                            _log.EventLog("No Body");
                            continue;
                        }
                        bool BodyLoopBreaks = false;
                        foreach (DataRow item in dsBody.Tables[0].Rows)
                        {
                            string refDocNo = "";
                            string sql = $@"exec pCore_CommonSp @Operation=InvoiceCheckingTB, @p1='{item["InvoiceNumber"].ToString().Trim()}'";
                            DataSet dsInvoice = _db.GetData(sql, CompanyId, ref error);
                            if (dsInvoice != null)
                            {
                                if (dsInvoice.Tables.Count > 0)
                                {
                                    if (dsInvoice.Tables[0].Rows.Count > 0)
                                    {
                                        if (dsInvoice.Tables[0].Rows[0][0].ToString() == "1") // Invoice Found
                                        {
                                            refDocNo = item["InvoiceNumber"].ToString().Trim();
                                            _log.EventLog("Invoice Found");
                                        }
                                        if (dsInvoice.Tables[0].Rows[0][0].ToString() == "2") // Invoice Found in Opening Balance
                                        {
                                            refDocNo = dsInvoice.Tables[0].Rows[0]["sVoucherNo"].ToString();
                                            _log.EventLog("Invoice Found as Opening Balance");
                                        }
                                        else if (dsInvoice.Tables[0].Rows[0][0].ToString() == "-1") //New Reference
                                        {
                                            refDocNo = "";
                                            _log.EventLog("Invoice Not Found in Winit DB. Post as New Reference");
                                        }
                                        else if (dsInvoice.Tables[0].Rows[0][0].ToString() == "0")
                                        {
                                            if (dsInvoice.Tables.Count == 3)
                                            {
                                                DataSet dsInv = new DataSet();
                                                if (dsInvoice.Tables[1].Rows.Count > 0)
                                                {
                                                    if (Convert.ToDateTime(dsInvoice.Tables[1].Rows[0]["tDate"]) >= Convert.ToDateTime(dsInvoice.Tables[0].Rows[0]["Startdate"]))
                                                    {
                                                        dsInv.Merge(dsInvoice.Tables[1]);
                                                        dsInv.AcceptChanges();
                                                        if (dsInvoice.Tables[2].Rows.Count > 0)
                                                        {
                                                            dsInv.Merge(dsInvoice.Tables[2]);
                                                            dsInv.AcceptChanges();
                                                            SalesInvoice_PIC(dsInv);
                                                            if (InvFlag)
                                                            {
                                                                refDocNo = item["InvoiceNumber"].ToString().Trim();
                                                                _log.EventLog("Invoice Posted");
                                                            }
                                                            else
                                                            {
                                                                _log.EventLog("Invoice Not Posted");
                                                                BodyLoopBreaks = true;
                                                                break;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            _log.EventLog("NOT dsInvoice.Tables[2].Rows.Count > 0");
                                                            BodyLoopBreaks = true;
                                                            break;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        refDocNo = "";
                                                        _log.EventLog("Invoice Transaction date is behind the Accounting Date");
                                                    }
                                                }
                                                else
                                                {
                                                    _log.EventLog("NOT dsInvoice.Tables[1].Rows.Count > 0");
                                                    BodyLoopBreaks = true;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                _log.EventLog("NOT dsInvoice.Tables.Count == 3");
                                                BodyLoopBreaks = true;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        _log.EventLog("NOT dsInvoice.Tables[0].Rows.Count > 0");
                                        BodyLoopBreaks = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    _log.EventLog("NOT dsInvoice.Tables.Count > 0");
                                    BodyLoopBreaks = true;
                                    break;
                                }
                            }
                            else
                            {
                                _log.EventLog("dsInvoice is null \n" + error);
                                BodyLoopBreaks = true;
                                break;
                            }
                            if (refDocNo == "")
                            {

                            }
                            else
                            {
                                DataTable dt = _db.GetIRef(refDocNo, CompanyId);
                                string refid = "0";
                                if (dt != null)
                                {
                                    _log.EventLog("BillRef Datatable count = " + dt.Rows.Count);
                                    _log.EventLog("BillRef iref = " + dt.Rows[0]["RefId"]);
                                    refid = dt.Rows[0]["RefId"].ToString();
                                }
                                else
                                {
                                    _log.EventLog("could not find reference.");
                                }
                                Hashtable billRef = new Hashtable();
                                billRef.Add("CustomerId", AcId);
                                billRef.Add("Amount", Convert.ToDecimal(item["Amount"].ToString().Trim()));
                                billRef.Add("reftype", 2);
                                billRef.Add("Reference", refDocNo.Trim());
                                billRef.Add("ref", Convert.ToInt32(refid));
                                listbillRef.Add(billRef);
                            }
                        }
                        if (BodyLoopBreaks)
                        {
                            _log.EventLog("BodyLoopBreaks");
                            continue;
                        }
                        Hashtable objBody = new Hashtable
                                     {
                                         { "DocNo", docno },
                                         { "Amount", Convert.ToDecimal(TB["Amount"].ToString().Trim()) },
                                         { "Reference", listbillRef },
                                         { "Account__Id", AcId },
                                         { "Route__Code", RouteCode },
                                         { "Company Master__Id", 5 },
                                         { "Group Customer Master__Name", Grp },
                                     };
                        lstBody.Add(objBody);
                        _log.EventLog("PDCReceipts_TB Body Data ready");
                        var postingData1 = new PostingData();
                        postingData1.data.Add(new Hashtable { { "Header", header }, { "Body", lstBody } });
                        string sContent1 = JsonConvert.SerializeObject(postingData1);
                        _log.EventLog("PDCReceipts_TB Content" + sContent1);
                        string err1 = "";

                        string Url1 = baseUrl + "/Transactions/Vouchers/Post-Dated Receipts";
                        _log.EventLog("PDCReceipts_TB post url" + Url1);
                        sessionID = GetSessionId(CompanyId);
                        var response1 = Focus8API.Post(Url1, sContent1, sessionID, ref err1);
                        if (response1 != null)
                        {
                            _log.EventLog("PDCReceipts_TB posting Response" + response1.ToString());
                            var responseData1 = JsonConvert.DeserializeObject<APIResponse.PostResponse>(response1);
                            if (responseData1.result == -1)
                            {
                                _log.EventLog("PDCReceipts_TB posting Response failed" + responseData1.result.ToString());
                                _log.EventLog("PDCReceipts_TB PDCReceipts Entry Posted Failed with DocNo: " + docno);
                                _log.ErrLog(response1 + "\n " + " PDCReceipts_TB PDCReceipts Entry Posted Failed with DocNo: " + docno + " \n Error Message : " + responseData1.message + "\n " + err1);
                            }
                            else
                            {
                                _log.EventLog("PDCReceipts_TB PDCReceipts Entry Posted Successfully with DocNo: " + docno);
                                int d = _db.setFn("setPDCReceiptsTB", docno, CompanyId);
                                if (d != 0)
                                {
                                    _log.EventLog("PDCReceipts_TB FOCUS_WINIT DB updation successed with DocNo = " + docno);
                                }
                                else
                                {
                                    _log.EventLog("PDCReceipts_TB FOCUS_WINIT DB Updation failed with DocNo=" + docno);
                                }
                            }
                        }
                    }

                }
                else
                {
                    _log.EventLog("getPDCReceiptsTB dataset table count is zero");
                }
            }
            else
            {
                _log.EventLog("getPDCReceiptsTB dataset is null");
            }
        }

        public string getServiceLink(string tagname)
        {
            XmlDocument xmlDoc = new XmlDocument();
            string strFileName = "";
            string PrgmFilesPath = AppDomain.CurrentDomain.BaseDirectory;
            
            strFileName = PrgmFilesPath + "\\bin\\XMLFiles\\Settings.xml";
            xmlDoc.Load(strFileName);
            XmlNodeList nodeList = xmlDoc.DocumentElement.SelectNodes("/ServSetting/ExternalModule/"+ tagname + "");
            string strValue;
            XmlNode node = nodeList[0];
            if (node != null)
                strValue = node.InnerText;
            else
                strValue = "";
            return strValue;
        }

        public string GetSessionId(int CompId)
        {
            string sSessionId = "";
            try
            {
                string strServer = getServiceLink("ServerName");
                _log.EventLog("strServer " + strServer);
                int ccode = CompId;
                string User_Name = getServiceLink("UserName");
                string Password = getServiceLink("Password");


                var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://" + strServer + "/focus8api/Login");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json = "{" + "\"data\": [{" + "\"Username\":\"" + User_Name + "\"," + "\"password\":\"" + Password + "\"," + "\"CompanyId\":\"" + ccode + "\"}]}";
                    streamWriter.Write(json);
                    _log.EventLog("json " + json);
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                StreamReader Updatereader = new StreamReader(httpResponse.GetResponseStream());
                string Udtcontent = Updatereader.ReadToEnd();

                JObject odtbj = JObject.Parse(Udtcontent);
                Temperatures Updtresult = JsonConvert.DeserializeObject<Temperatures>(Udtcontent);
                _log.EventLog("updateresult " + Updtresult.Result.ToString());
                if (Updtresult.Result == 1)
                {
                    sSessionId = Updtresult.Data[0].FSessionId;
                }


                return sSessionId;
            }
            catch (Exception ex)
            {
                _log.ErrLog(ex.ToString());
            }
            return sSessionId;
        }

    }
}
