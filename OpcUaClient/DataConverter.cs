using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaClient
{
    public class DataConverter
    {
        public static DateTime GetDateTime(byte[] dtVal)
        {
            int year = 2000 + Convert.ToInt32(dtVal[0].ToString("X2"));
            int month = Convert.ToInt32(dtVal[1].ToString("X2"));
            int day = Convert.ToInt32(dtVal[2].ToString("X2"));
            int hour = Convert.ToInt32(dtVal[3].ToString("X2"));
            int minute = Convert.ToInt32(dtVal[4].ToString("X2"));
            int second = Convert.ToInt32(dtVal[5].ToString("X2"));
            byte msHigh = Convert.ToByte(dtVal[6].ToString("X2") );
            string strTest = dtVal[7].ToString("X2");
            byte msLow = byte.Parse(strTest.Substring(0, 1));            
            int milisecond = (msHigh * 10) + msLow;
            //byte weekday = byte.Parse(strTest.Substring(1, 1));

            DateTime dt = new DateTime(year, month, day, hour, minute, second, milisecond);

            return dt;
        }
    }
}
