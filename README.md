# OpcUaClient Library
This library was created to encapsulate the methods that I use in projetcs.

reference:
https://github.com/OPCFoundation/UA-.NETStandard 

methods:
- Connect
- Disconnect
- ReadValues
- WriteValues

Simple usage (ignored the values status and timestamp, try/catch,  only to demonstrate how to read and write) :

```C#
 //connection URL
string url = "opc.tcp://10.8.66.112:4840";
OpcUaClient.OpcUaClient opcUaClient = new OpcUaClient.OpcUaClient();

//Connect method
opcUaClient.Connect(url, Opc.Ua.MessageSecurityMode.None).GetAwaiter().GetResult();

//Nodes to Read
string nodeMaxPos = "ns=3;s=\"DB_INTF_EXT\".\"MAX_POS\"";
string nodeHmiDT = "ns=3;s=\"DB_HMI\".\"COMMON\".\"DateTime\"";            
//Add the nodes to the list
List<string> opcNodes = new List<string>() {nodeMaxPos, nodeHmiDT};            

//Read the OPC Values
List<OpcValue> opcValues = opcUaClient.ReadValues(opcNodes);
            
//convert the objects to the corresponding types (integer and DateTime)
int maxPos = Convert.ToInt32(opcValues[0].Value);  
DateTime dtLastChange = OpcUaClient.DataConverter.GetDateTime(opcValues[1].Value as byte[]);

//NodesToWrite
string nodeBoolWrite = "ns=3;s=\"DB_TEST\".\"BooleanValue\"";
string nodeIntegerWrite = "ns=3;s=\"DB_TEST\".\"IntegerValue\"";
opcNodes = new List<string>() { nodeBoolWrite, nodeIntegerWrite };

//Values to Write
List<object> objectValues = new List<object>() { true, (short)1234 };

//List of writing status (each node)
List<bool> writeStatus;

//Write the values
bool returnOk = opcUaClient.WriteValues(opcNodes, objectValues, out writeStatus);

Console.Read();  
```
