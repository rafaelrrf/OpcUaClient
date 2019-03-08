using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using OpcUaClient.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaClient
{
    /// <summary>
    /// Class that implements the basic OPC UA Client calls : Connect, Disconnect, ReadValues, WriteValues.
    /// </summary>
    public class OpcUaClient
    {
        /// <summary>
        /// Main constructor
        /// </summary>
        public OpcUaClient()
        {
            ApplicationInstance application = new ApplicationInstance();
            application.ConfigSectionName = "ReferenceClient";
            application.ApplicationType = ApplicationType.Client;

            // load the application configuration.
            application.LoadApplicationConfiguration(false).Wait();            

            // check the application certificate.
            application.CheckApplicationInstanceCertificate(false, 0).Wait();

            //sets the application configuration
            m_configuration = application.ApplicationConfiguration;

            //event handler to validate the server certificate
            m_CertificateValidation = new CertificateValidationEventHandler(Notification_ServerCertificate);
        }

        #region Private Fields        
        private int m_Timeout = 5000;
        private Opc.Ua.ApplicationConfiguration m_configuration;
        private CertificateValidationEventHandler m_CertificateValidation;
        #endregion

        #region Public Members               

        private Session session;

        public bool Connected { get { return this.session != null && this.session.Connected; } }

        /// <summary>
        /// Provides the event handling for server certificates.
        /// </summary>        
        #endregion

        #region Event Handlers    

        private void Notification_ServerCertificate(CertificateValidator cert, CertificateValidationEventArgs e)
        {
            //Handle certificate
            //To accept a certificate manually move it to the root folder (current user) (Start > mmc.exe > add snap-in > certificates)         

            try
            {
                X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);
                X509CertificateCollection certCol = store.Certificates.Find(X509FindType.FindByThumbprint, e.Certificate.Thumbprint, true);
                store.Close();

                if (certCol.Capacity > 0)
                {
                    e.Accept = true;
                }
                else
                {
                    e.Accept = m_configuration.SecurityConfiguration.AutoAcceptUntrustedCertificates;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception OpcUaClient::Notification_ServerCertificate: " + ex.Message);
            }
        }

        #endregion

        #region public methods
        /// <summary>
        /// Establishes the connection to the OPC Server
        /// </summary>
        /// <param name="serverUrl">URL to the server, ocp.tcp://server:port </param>
        /// <param name="securityMode"> None, Sign or SignAndEncrypt </param>
        /// <returns></returns>
        public async Task Connect(string serverUrl, MessageSecurityMode securityMode)
        {
            ConfiguredEndpoint endpoint = null;
            EndpointDescription endpointDescription = null;
            EndpointConfiguration endpointConfiguration = null;
            UserIdentity userIdentity = null;
            string[] preferredLocales = null;

            try
            {
                if (m_configuration == null)
                {
                    throw new ArgumentNullException("m_configuration");
                }

                if (session != null && !session.Disposed)
                {
                    //remove existing subscriptions (not implemented yet)
                    this.Disconnect(); //close the session
                }

                //List the server endpoints
                List<EndpointDescription> endpointsList = ListEndpoints(serverUrl);

                //Get the endpoint that matches the securityMode (like None, Sign or SignAndEncrypt)
                endpointDescription = endpointsList.Find(ep => ep.SecurityMode == securityMode);

                if (endpointDescription != null)
                {
                    //Hook up a validator function for a CertificateValidation event
                    m_configuration.CertificateValidator.CertificateValidation += CertificateValidator_CertificateValidation;

                    //creates the endpointConfiguration object
                    endpointConfiguration = EndpointConfiguration.Create(m_configuration);

                    //creates the endpoint object
                    endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

                    //establishes the connection and creates the session 
                    this.session = await Session.Create(
                                    m_configuration,
                                    endpoint,
                                    updateBeforeConnect: false,
                                    checkDomain: true,
                                    sessionName: m_configuration.ApplicationName,
                                    sessionTimeout: 60000,
                                    identity: userIdentity,
                                    preferredLocales: preferredLocales);

                    this.session.KeepAlive += Session_KeepAlive;

                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(String.Format("Exception OpcUaClient::Connect: " + ex.ToString()));
            }
        }

        private void Session_KeepAlive(Session session, KeepAliveEventArgs e)
        {
            if (e.Status != null && ServiceResult.IsNotGood(e.Status))
            {
                Console.WriteLine("{0} {1}/{2}", e.Status, session.OutstandingRequestCount, session.DefunctRequestCount);
                System.Diagnostics.Debug.WriteLine("Session_KeepAlive {0} {1}/{2}", e.Status, session.OutstandingRequestCount, session.DefunctRequestCount);

                /*
                if (reconnectHandler == null)
                {
                    Console.WriteLine("--- RECONNECTING ---");
                    reconnectHandler = new SessionReconnectHandler();
                    reconnectHandler.BeginReconnect(sender, ReconnectPeriod * 1000, Client_ReconnectComplete);
                }
                */
            }            
        }

        /// <summary>Closes an existing session and disconnects from the server.</summary>        
        public void Disconnect()
        {
            try
            {
                if (session != null)
                {
                    session.Close(m_Timeout);

                    if (!session.Disposed)
                    {
                        session.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception OpcUaClient::Disconnect " + ex.Message);
            }
        }

        /// <summary>
        /// Read the values from the server
        /// </summary>
        /// <param name="opcNodes">opc ua nodes</param>
        /// <returns>null (error) or List<OpcValue> with the Values and StatusCodes</OpcValue></returns>
        public List<OpcValue> ReadValues(List<string> opcNodes)
        {
            List<OpcValue> opcValues = null;

            try
            {
                int count = opcNodes.Count;
                List<object> values;
                List<ServiceResult> statusCodes;
                List<NodeId> nodeIds = new List<NodeId>(count);
                List<Type> expectedTypes = new List<Type>(count);

                foreach (string node in opcNodes)
                {
                    nodeIds.Add(new NodeId(node));
                    expectedTypes.Add(null);  //no need to specify the data type
                }                

                session.ReadValues(nodeIds, expectedTypes, out values, out statusCodes);

                opcValues = new List<OpcValue>(count);

                OpcValue opcValue;
                for (int i = 0; i < count; i++)
                {
                    opcValue = new OpcValue(values[i], statusCodes[i]);
                    opcValues.Add(opcValue);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception OpcUaClient::ReadValues " + ex.Message);
                opcValues = null;
            }

            return opcValues;
        }

        public bool WriteValues(List<string> opcNodes, List<object> values, out List<bool> status)
        {
            bool ret = false;

            try
            {
                if (opcNodes.Count != values.Count)
                    throw new Exception("opcNodes.Count != values.Count");

                int count = opcNodes.Count;

                status = new List<bool>(count);
                List<Type> types = new List<Type>(count);
                List<NodeId> nodeIds = new List<NodeId>(count);

                DiagnosticInfoCollection diags;
                StatusCodeCollection statusCodes;

                WriteValueCollection writeValues = new WriteValueCollection(count);

                for (int i = 0; i < count; i++)
                {
                    Variant variant = new Variant(values[i]);
                    DataValue dataVal = new DataValue(variant);

                    WriteValue writeVal = new WriteValue();
                    writeVal.Value = dataVal;
                    writeVal.NodeId = new NodeId(opcNodes[i]);
                    writeVal.AttributeId = Attributes.Value;

                    writeValues.Add(writeVal);
                }

                ResponseHeader rh = session.Write(null, writeValues, out statusCodes, out diags);

                ret = StatusCode.IsGood(rh.ServiceResult.Code);

                for (int i = 0; i < count; i++)
                {
                    //status[i] = StatusCode.IsGood(statusCodes[i]);
                    status.Add(StatusCode.IsGood(statusCodes[i]));
                    ret = ret & status[i];
                }
            }
            catch (Exception ex)
            {
                ret = false;
                status = null;
                System.Diagnostics.Debug.WriteLine("Exception OpcUaClient::WriteValues " + ex.Message);
            }

            return ret;
        }

        public bool WriteUDT(List<string> opcNodes, List<object> values, out List<bool> status)
        {
            bool ret = false;

            try
            {
                if (opcNodes.Count != values.Count)
                    throw new Exception("opcNodes.Count != values.Count");

                int count = opcNodes.Count;

                status = new List<bool>(count);
                List<Type> types = new List<Type>(count);
                List<NodeId> nodeIds = new List<NodeId>(count);

                DiagnosticInfoCollection diags;
                StatusCodeCollection statusCodes;

                WriteValueCollection writeValues = new WriteValueCollection(count);

                for (int i = 0; i < count; i++)
                {
                    //Variant variant = new Variant(values[i]);
                    

                    Opc.Ua.ExpandedNodeId expNodeId = new Opc.Ua.ExpandedNodeId(opcNodes[i]);
                    Opc.Ua.ExtensionObject extObj = new Opc.Ua.ExtensionObject(expNodeId, values[i]);

                    DataValue dataVal = new DataValue(extObj);

                    WriteValue writeVal = new WriteValue();
                    //writeVal.
                    writeVal.Value = dataVal;
                    writeVal.NodeId = new NodeId(opcNodes[i]);
                    writeVal.AttributeId = Attributes.Value;

                    writeValues.Add(writeVal);
                }

                ResponseHeader rh = session.Write(null, writeValues, out statusCodes, out diags);

                

                ret = StatusCode.IsGood(rh.ServiceResult.Code);

                for (int i = 0; i < count; i++)
                {
                    //status[i] = StatusCode.IsGood(statusCodes[i]);
                    status.Add(StatusCode.IsGood(statusCodes[i]));
                    ret = ret & status[i];
                }
            }
            catch (Exception ex)
            {
                ret = false;
                status = null;
                System.Diagnostics.Debug.WriteLine("Exception OpcUaClient::WriteValues " + ex.Message);
            }

            return ret;
        }

        #endregion

        #region private methods

        private void CertificateValidator_CertificateValidation(CertificateValidator sender, CertificateValidationEventArgs e)
        {
            m_CertificateValidation(sender, e);
        }

        private List<EndpointDescription> ListEndpoints(string serverUrl)
        {
            List<EndpointDescription> endPointsList = new List<EndpointDescription>();

            try
            {
                ApplicationDescriptionCollection servers = FindServers(serverUrl);

                foreach (ApplicationDescription ad in servers)
                {
                    foreach (string url in ad.DiscoveryUrls)
                    {
                        EndpointDescriptionCollection endpoints = GetEndpoints(url);
                        foreach (EndpointDescription ep in endpoints)
                        {
                            endPointsList.Add(ep);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                endPointsList?.Clear();
                endPointsList = null;
                System.Diagnostics.Debug.WriteLine("OpcUaClient::ListEndpoints" + ex.Message);
            }

            return endPointsList;
        }

        private ApplicationDescriptionCollection FindServers(string url)
        {
            ApplicationDescriptionCollection servers = null;

            //Create a URI using the discovery URL
            Uri uri = new Uri(url);
            try
            {
                //Ceate a DiscoveryClient
                DiscoveryClient client = DiscoveryClient.Create(uri);

                //Find servers                
                servers = client.FindServers(null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("OpcUaClient::FindServers" + ex.Message);
            }

            return servers;
        }

        private EndpointDescriptionCollection GetEndpoints(string serverUrl)
        {
            EndpointDescriptionCollection endpoints = null;

            //Create a URI using the server's URL
            Uri uri = new Uri(serverUrl);
            try
            {
                //Create a DiscoveryClient
                DiscoveryClient client = DiscoveryClient.Create(uri);
                //Search for available endpoints
                endpoints = client.GetEndpoints(null);
                return endpoints;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("OpcUaClient::GetEndpoints" + ex.Message);
            }

            return endpoints;
        }

        #endregion
    }
}
