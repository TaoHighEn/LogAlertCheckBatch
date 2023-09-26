using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LogAlertCheckBatch
{
    /// <summary>
    /// Check Progarm Execute Status
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            string conn_str = ConfigurationManager.AppSettings["ERP"];
            string log_table = ConfigurationManager.AppSettings["LogTable"];
            string col_status = ConfigurationManager.AppSettings["Status"];
            using (var conn = new SqlConnection(conn_str))
            {
                conn.Open();
                string query = string.Format(@"Select * From {0} Where {1} = 0", log_table, col_status);
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dataTable = new DataTable();
                da.Fill(dataTable);
                if (dataTable.Rows.Count != 0)
                {
                    foreach (DataRow dr in dataTable.Rows)
                    {
                        string content = string.Format(
                            @"程式執行錯誤請確認：
                            GUID：{0}
                            Log Message：{1}",
                            dr["GUID"], dr["LogMessage"]);
                        SendMail(content);
                    }
                    query = string.Format(@"Update {0} Set AlertStatus = 1,AckedTime = GETDate() Where AlertStatus = 0 ", log_table);
                    using (SqlTransaction tsc = conn.BeginTransaction(IsolationLevel.ReadCommitted))
                    {
                        try
                        {
                            cmd = new SqlCommand(query, conn);
                            cmd.Transaction = tsc;
                            cmd.ExecuteNonQuery();
                            tsc.Commit();
                        }
                        catch (Exception ex)
                        {
                            tsc.Rollback();
                            throw ex;
                        }
                    }
                }
            }
        }
        public static void SendMail(string mailContent)
        {
            SmtpClient MySMTP = new SmtpClient("192.168.0.19");
            MySMTP.Credentials = new NetworkCredential("itadmin", "A1@345b");
            MailMessage message = new MailMessage();
            message.From = new MailAddress("itadmin@web-pro.com.tw");
            message.Subject = "程式執行錯誤提醒";
            message.To.Add(new MailAddress("alan.hsieh@web-pro.com.tw"));
            message.Body = mailContent;
            MySMTP.Send(message);
        }
    }
}
