using OpcUaClient.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaClientConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            
            string url = "opc.tcp://10.8.66.77:4840";


            string nodeMaxPos = "ns=3;s=\"DB_INTF_EXT\".\"MAX_POS\"";
            string nodeHmiDT = "ns=3;s=\"DB_HMI\".\"COMMON\".\"DateTime\"";
            //string nodeMaxPos = "ns=3;s=\"DB_MISAEL\".\"Int1\"";
            //string nodeHmiDT = "ns=3;s=\"DB_MISAEL\".\"DateTime\"";

            Console.WriteLine("Begin");

            OpcUaClient.OpcUaClient opcUaClient = new OpcUaClient.OpcUaClient();
            opcUaClient.Connect(url, Opc.Ua.MessageSecurityMode.None).GetAwaiter().GetResult();

            Console.WriteLine("Connect");

            List<string> opcNodes = new List<string>() {nodeMaxPos, nodeHmiDT};            

            List<OpcValue> opcValues = opcUaClient.ReadValues(opcNodes);

            Console.WriteLine("Read");

            int maxPos = Convert.ToInt32(opcValues[0].Value);
            DateTime dtLastChange = OpcUaClient.DataConverter.GetDateTime(opcValues[1].Value as byte[]);

            Console.WriteLine("maxPos = " + maxPos + ", dtLastChange = " + dtLastChange.ToString());

            Console.Read();

            //WriteData
            string nodeBoolWrite = "ns=3;s=\"DB_TEST\".\"BooleanValue\"";
            string nodeIntegerWrite = "ns=3;s=\"DB_TEST\".\"IntegerValue\"";
            //string nodeBoolWrite = "ns=3;s=\"DB_MISAEL\".\"Bool1\"";
            //string nodeIntegerWrite = "ns=3;s=\"DB_MISAEL\".\"Int1\"";

            opcNodes = new List<string>() { nodeBoolWrite, nodeIntegerWrite };
            List<object> objectValues = new List<object>() { true, (short)1234 };
            List<bool> writeStatus;

            bool returnOk = opcUaClient.WriteValues(opcNodes, objectValues, out writeStatus);

            Console.Read();            
        }
    }
}

