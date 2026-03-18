using Arrete.SSP;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arrete.Print
{
    internal static class DeviceCommunications
    {


        #region Structures
        /// <summary>
        /// Returned info for connection to a device found during a device search
        /// </summary>
        internal struct FoundDeviceInformation
        {
            /// <summary>
            /// The Com port that the device is located on
            /// </summary>
            public string DevicePort;

            /// <summary>
            /// The SSP address used to communicate with the device
            /// </summary>
            public byte DeviceAddress;

            /// <summary>
            /// Constructor for the FoundDeviceInformation object
            /// </summary>
            /// <param name="DevPort">The value to set DevicePort to</param>
            /// <param name="DevAddress">The value to set DeviceAddress to</param>
            public FoundDeviceInformation(string DevPort, byte DevAddress)
            {
                DevicePort = DevPort;
                DeviceAddress = DevAddress;
            }
        }
        #endregion Structures

        #region Enums

        /// <summary>
        /// Statuses that can be returned at the end of a print (whether it is successful or not)
        /// </summary>
        internal enum PrintCompleteStatuses
        {
            Success,
            ConnectionFail,
            PlaceholderSetFail,
            CouldNotStartPrintFail,
            PollFail,
            PaperLoadFail,
            PrinterHeadRemovedFail,
            CutFail,
            PaperJamFail,
        }

        #endregion

        #region Private Members
        /// <summary>
        /// A list of all the devices found during a search
        /// </summary>
        private static List<FoundDeviceInformation> m_FoundDevicesInfo;

        /// <summary>
        /// List of all the addresses that should be used to try to find an SSP device on
        /// </summary>
        private static List<byte> m_addressesToSearchOn;

        /// <summary>
        /// An event that fires when the search for devices is complete
        /// </summary>
        private static event DeviceSearchCompleteEventHandler m_e_DeviceSearchCompleteEvent;

        /// <summary>
        /// An event that fires when the download of a file to the device is complete
        /// </summary>
        private static event DeviceDownloadCompleteEventHandler m_e_DeviceDownloadCompleteEvent;

        /// <summary>
        /// An event that fires several times during the download process to give an update on the progress of the download
        /// </summary>
        private static event DeviceDownloadProgressEventHandler m_e_DeviceDownloadProgressEvent;

        /// <summary>
        /// An event that fires several times during the search for devices to give an update on the progress of the search
        /// </summary>
        private static event DeviceSearchProgressEventHandler m_e_DeviceSearchProgressEvent;

        /// <summary>
        /// An event that fires once a print has finished. This fires whether the print was successful, or stopped because of a failure.
        /// </summary>
        private static event DevicePrintCompleteEventHandler m_e_DevicePrintCompleteEvent;

        /// <summary>
        /// An event that fires whenever an SSP command is sent to a device
        /// </summary>
        private static event SSPCommandInformationEventHandler m_e_DeviceCommsEvent;

        /// <summary>
        /// Provides handling of the low level SSP communications with the device
        /// (Building packets, checksums, encryption etc)
        /// </summary>
        private static ITLlib.SSPComms m_CommsConnection;

        /// <summary>
        /// A structure that holds SSP command data to be sent, as well as storing returned data, and several other
        /// parameters used when sending and receiving an SSP command.
        /// </summary>
        private static ITLlib.SSP_COMMAND m_CommsCommandStructure;

        /// <summary>
        /// Stores the ssp key information
        /// </summary>
        private static ITLlib.SSP_KEYS m_eSSPKeys;

        /// <summary>
        /// The COM port to connect to the device on
        /// </summary>
        private static string m_ComPort;

        /// <summary>
        /// The SSP address to use to speak to the device
        /// </summary>
        private static byte m_Address;

        /// <summary>
        /// Background worker that runs the search for device task
        /// </summary>
        private static BackgroundWorker m_SearchWorker;

        /// <summary>
        /// Background worker than runs the download task
        /// </summary>
        private static BackgroundWorker m_DownloadWorker;

        /// <summary>
        /// Background worker that runs the print task
        /// </summary>
        private static BackgroundWorker m_PrintTicketWorker;

        /// <summary>
        /// Flagged that is set when a background comms process is running so
        /// that other method calls can cancel
        /// </summary>
        private static bool m_Busy;

        /// <summary>
        /// The filename (and path) of the resource file to download to the device
        /// </summary>
        private static string m_DownloadFileName;

        /// <summary>
        /// A list of the placeholders to set on the device
        /// </summary>
        private static List<string> m_Placeholders;

        #endregion Private Members

        #region Public (Internal) Accessors
        /// <summary>
        /// Returns a list of devices found in a device search as a read only 
        /// object that cannot be manipluated
        /// </summary>
        internal static IEnumerable<FoundDeviceInformation> FoundDevices
        {
            get { return m_FoundDevicesInfo.AsReadOnly(); }
        }

        /// <summary>
        /// An event that fires when the search for devices is complete
        /// </summary>
        internal static event DeviceSearchCompleteEventHandler DeviceSearchComplete
        {
            add { m_e_DeviceSearchCompleteEvent += value; }
            remove { m_e_DeviceSearchCompleteEvent -= value; }
        }

        /// <summary>
        /// An event that fires several times during the search for devices to give an update on the progress of the search
        /// </summary>
        internal static event DeviceSearchProgressEventHandler DeviceSearchProgress
        {
            add { m_e_DeviceSearchProgressEvent += value; }
            remove { m_e_DeviceSearchProgressEvent -= value; }
        }

        /// <summary>
        /// An event that fires when the download of a file to the device is complete
        /// </summary>
        internal static event DeviceDownloadCompleteEventHandler DeviceDownloadComplete
        {
            add { m_e_DeviceDownloadCompleteEvent += value; }
            remove { m_e_DeviceDownloadCompleteEvent -= value; }
        }

        /// <summary>
        /// An event that fires several times during the download process to give an update on the progress of the download
        /// </summary>
        internal static event DeviceDownloadProgressEventHandler DeviceDownloadProgress
        {
            add { m_e_DeviceDownloadProgressEvent += value; }
            remove { m_e_DeviceDownloadProgressEvent -= value; }
        }

        /// <summary>
        /// An event that fires once a print has finished. This fires whether the print was successful, or stopped because of a failure.
        /// </summary>
        internal static event DevicePrintCompleteEventHandler DevicePrintComplete
        {
            add { m_e_DevicePrintCompleteEvent += value; }
            remove { m_e_DevicePrintCompleteEvent -= value; }
        }

        /// <summary>
        /// An event that fires when an SSP command is sent to give information about what was sent
        /// </summary>
        internal static event SSPCommandInformationEventHandler CommandInformation
        {
            add { m_e_DeviceCommsEvent += value; }
            remove { m_e_DeviceCommsEvent -= value; }
        }

        #endregion Public (Internal) Accessors

        #region Public Delegates

        /// <summary>
        /// Delegate for the search complete event
        /// </summary>
        /// <param name="source">Source object which raised the event</param>
        /// <param name="devicesFound">Whether or not any devices were found during the search</param>
        /// <remarks>
        /// As this object is static, the source parameter will always be null, but is
        /// kept for conistency with most other event handler delegates
        /// </remarks>
        internal delegate void DeviceSearchCompleteEventHandler(object source, bool devicesFound);

        /// <summary>
        /// Delegate for the search progress event
        /// </summary>
        /// <param name="source">Source object which raised the event</param>
        /// <param name="progress">The percentage progress of how far through the search we are</param>
        /// <remarks>
        /// As this object is static, the source parameter will always be null, but is
        /// kept for conistency with most other event handler delegates
        /// </remarks>
        internal delegate void DeviceSearchProgressEventHandler(object source, int progress);

        /// <summary>
        /// Delegate for the download complete event
        /// </summary>
        /// <param name="source">Source object which raised the event</param>
        /// <param name="completionStatus">The status at the end of the download</param>
        /// <remarks>
        /// As this object is static, the source parameter will always be null, but is
        /// kept for conistency with most other event handler delegates
        /// </remarks>
        internal delegate void DeviceDownloadCompleteEventHandler(object source, ITLlib.DOWNLOAD_STATUS completionStatus);

        /// <summary>
        /// Delegate for the download progress event
        /// </summary>
        /// <param name="source">Source object which raised the event</param>
        /// <param name="currentStatus">The current status of the download</param>
        /// <param name="percentageComplete">The percentage progress of how far through the download we are</param>
        /// <remarks>
        /// As this object is static, the source parameter will always be null, but is
        /// kept for conistency with most other event handler delegates
        /// </remarks>
        internal delegate void DeviceDownloadProgressEventHandler(object source, ITLlib.DOWNLOAD_STATUS currentStatus, int percentageComplete);

        /// <summary>
        /// Delegate for the print complete event
        /// </summary>
        /// <param name="source">Source object which raised the event</param>
        /// <param name="completionStatus">Final status of the print</param>
        /// <remarks>
        /// As this object is static, the source parameter will always be null, but is
        /// kept for conistency with most other event handler delegates
        /// </remarks>
        internal delegate void DevicePrintCompleteEventHandler(object source, PrintCompleteStatuses completionStatus);

        /// <summary>
        /// Delegate for the ssp information event
        /// </summary>
        /// <param name="source">Source object which raised the event</param>
        /// <param name="LogAddition">The text to add to the log</param>
        /// <remarks>
        /// As this object is static, the source parameter will always be null, but is
        /// kept for conistency with most other event handler delegates
        /// </remarks>
        internal delegate void SSPCommandInformationEventHandler(object source, string LogAddition);

        #endregion Public Delegates

        #region Constructor

        /// <summary>
        /// Private static constructor that will be called the first time anything in the program tries to access
        /// this class, and then never called again. Cannot be used to instantiate the class
        /// </summary>
        static DeviceCommunications()
        {
            m_SearchWorker = new BackgroundWorker();
            m_DownloadWorker = new BackgroundWorker();
            m_PrintTicketWorker = new BackgroundWorker();

            m_SearchWorker.WorkerReportsProgress = true;
            m_DownloadWorker.WorkerReportsProgress = true;


            m_addressesToSearchOn = new List<byte>();
            m_FoundDevicesInfo = new List<FoundDeviceInformation>();
            m_Placeholders = new List<string>();


            m_CommsConnection = new ITLlib.SSPComms();
            m_CommsCommandStructure = new ITLlib.SSP_COMMAND();
            m_eSSPKeys = new ITLlib.SSP_KEYS();

            m_SearchWorker.DoWork += new DoWorkEventHandler(m_SearchWorker_DoWork);
            m_DownloadWorker.DoWork += new DoWorkEventHandler(m_DownloadWorker_DoWork);
            m_SearchWorker.ProgressChanged += new ProgressChangedEventHandler(m_SearchWorker_ProgressChanged);
            m_DownloadWorker.ProgressChanged += new ProgressChangedEventHandler(m_DownloadWorker_ProgressChanged);
            m_PrintTicketWorker.DoWork += new DoWorkEventHandler(m_PrintTicketWorker_DoWork);

            m_e_DeviceSearchCompleteEvent += new DeviceSearchCompleteEventHandler(DeviceCommunications_m_e_DeviceSearchCompleteEvent);
            m_e_DeviceSearchProgressEvent += new DeviceSearchProgressEventHandler(DeviceCommunications_m_e_DeviceSearchProgressEvent);
            m_e_DeviceDownloadCompleteEvent += new DeviceDownloadCompleteEventHandler(DeviceCommunications_m_e_DeviceDownloadCompleteEvent);
            m_e_DeviceDownloadProgressEvent += new DeviceDownloadProgressEventHandler(DeviceCommunications_m_e_DeviceDownloadProgressEvent);
            m_e_DevicePrintCompleteEvent += new DevicePrintCompleteEventHandler(DeviceCommunications_m_e_DevicePrintCompleteEvent);
        }

        #endregion Constructor

        #region Private Methods

        /// <summary>
        /// Dummy event handler so that null checking isn't required every time the search complete event is thrown
        /// </summary>
        /// <param name="source"></param>
        /// <param name="devicesFound"></param>		
        private static void DeviceCommunications_m_e_DeviceSearchCompleteEvent(object source, bool devicesFound)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Dummy event handler so that null checking isn't required every time the download progress event is raised
        /// </summary>
        /// <param name="source"></param>
        /// <param name="currentStatus"></param>
        /// <param name="percentageComplete"></param>
        static void DeviceCommunications_m_e_DeviceDownloadProgressEvent(object source, ITLlib.DOWNLOAD_STATUS currentStatus, int percentageComplete)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Dummy event handler so that null checking isn't required every time the download complete event is raised
        /// </summary>
        /// <param name="source"></param>
        /// <param name="completionStatus"></param>
        static void DeviceCommunications_m_e_DeviceDownloadCompleteEvent(object source, ITLlib.DOWNLOAD_STATUS completionStatus)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Dummy event handler so that null checking isn't required every time the search progress event is thrown
        /// </summary>
        /// <param name="source"></param>
        /// <param name="progress"></param>
        static void DeviceCommunications_m_e_DeviceSearchProgressEvent(object source, int progress)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Dummy event handler so that null checking isn't required every time the print event is raised
        /// </summary>
        /// <param name="source"></param>
        /// <param name="completionStatus"></param>
        static void DeviceCommunications_m_e_DevicePrintCompleteEvent(object source, DeviceCommunications.PrintCompleteStatuses completionStatus)
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Runs the device search in the background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void m_SearchWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //used when raising the search complete event, tracks if a device is found during the search
            bool anyDevicesFound = false;

            //if there are no addresses to search on, nothing could ever be found, so just return now
            if (m_addressesToSearchOn.Count < 1)
            {
                m_e_DeviceSearchCompleteEvent(null, anyDevicesFound);
                m_Busy = false;
                return;
            }

            //gets a list of all the com ports on the system
            string[] comPortNames = System.IO.Ports.SerialPort.GetPortNames();

            //same as above, if there's no ports, there's nothing to search across
            if (comPortNames.Length < 1)
            {
                m_e_DeviceSearchCompleteEvent(null, anyDevicesFound);
                m_Busy = false;
                return;
            }

            m_FoundDevicesInfo.Clear();

            //iterate through the ports, opening each one in turn and searching each address
            //on that port, then closing the port
            m_CommsCommandStructure.RetryLevel = 2;
            m_CommsCommandStructure.Timeout = 1000;
            for (int i = 0; i < comPortNames.Length; i++)
            {
                m_CommsCommandStructure.ComPort = comPortNames[i];
                m_CommsCommandStructure.EncryptionStatus = false;

                m_SearchWorker.ReportProgress(CalculateNestedForPercentDone(i, 0, comPortNames.Length, 1));

                //attempt to open the comms port
                if (!m_CommsConnection.OpenSSPComPort(m_CommsCommandStructure))
                {
                    //if we can't open the port, move onto the text one
                    continue;
                }

                for (int j = 0; j < m_addressesToSearchOn.Count; j++)
                {
                    m_SearchWorker.ReportProgress(CalculateNestedForPercentDone(i, j, comPortNames.Length, m_addressesToSearchOn.Count));

                    //set the address to the current address
                    m_CommsCommandStructure.SSPAddress = m_addressesToSearchOn[j];
                    //set up a sync command to establish a connection
                    m_CommsCommandStructure.CommandData[0] = (byte)SSPCommandBytes.SSP_Command_Header_Bytes.Sync;
                    m_CommsCommandStructure.CommandDataLength = 1;
                    ITLlib.SSP_COMMAND_INFO testCommandInfo = new ITLlib.SSP_COMMAND_INFO();
                    if (!m_CommsConnection.SSPSendCommand(m_CommsCommandStructure, testCommandInfo) ||
                        m_CommsCommandStructure.ResponseDataLength < 1 ||
                        m_CommsCommandStructure.ResponseData[0] != (byte)SSPCommandBytes.SSP_Generic_Reply_Bytes.OK)
                    {
                        UpdateLog(testCommandInfo, true);
                        continue;
                    }

                    UpdateLog(testCommandInfo, false);

                    //next we need to check it can do a key exchange
                    if (!NegotiateKeyExchange())
                    {
                        //log will be updating in the negotiate method
                        continue;
                    }

                    //try to do an encrypted sync to check that it works
                    m_CommsCommandStructure.EncryptionStatus = true;
                    m_CommsCommandStructure.CommandData[0] = (byte)SSPCommandBytes.SSP_Command_Header_Bytes.Sync;
                    m_CommsCommandStructure.CommandDataLength = 1;
                    if (!m_CommsConnection.SSPSendCommand(m_CommsCommandStructure, testCommandInfo) ||
                        m_CommsCommandStructure.ResponseDataLength < 1 ||
                        m_CommsCommandStructure.ResponseData[0] != (byte)SSPCommandBytes.SSP_Generic_Reply_Bytes.OK)
                    {
                        UpdateLog(testCommandInfo, true);
                        continue;
                    }

                    UpdateLog(testCommandInfo, false);

                    //get the device type, and check it's a standalone printer
                    m_CommsCommandStructure.EncryptionStatus = true;
                    m_CommsCommandStructure.CommandData[0] = (byte)SSPCommandBytes.SSP_Command_Header_Bytes.Setup_Request;
                    m_CommsCommandStructure.CommandDataLength = 1;
                    if (!m_CommsConnection.SSPSendCommand(m_CommsCommandStructure, testCommandInfo) ||
                        m_CommsCommandStructure.ResponseDataLength < 16 ||
                        m_CommsCommandStructure.ResponseData[0] != (byte)SSPCommandBytes.SSP_Generic_Reply_Bytes.OK ||
                        m_CommsCommandStructure.ResponseData[1] != 11)
                    {
                        UpdateLog(testCommandInfo, true);
                        continue;
                    }

                    UpdateLog(testCommandInfo, false);

                    //get the firmware and check that is is Coupon Printer firmware
                    m_CommsCommandStructure.EncryptionStatus = true;
                    m_CommsCommandStructure.CommandData[0] = (byte)SSPCommandBytes.SSP_Command_Header_Bytes.Get_Full_Firmware;
                    m_CommsCommandStructure.CommandDataLength = 1;
                    if (!m_CommsConnection.SSPSendCommand(m_CommsCommandStructure, testCommandInfo) ||
                        m_CommsCommandStructure.ResponseDataLength < 17 ||
                        m_CommsCommandStructure.ResponseData[0] != (byte)SSPCommandBytes.SSP_Generic_Reply_Bytes.OK ||
                        m_CommsCommandStructure.ResponseData[1] != (byte)'C' ||
                        m_CommsCommandStructure.ResponseData[2] != (byte)'P' ||
                        m_CommsCommandStructure.ResponseData[3] != (byte)'0' ||
                        m_CommsCommandStructure.ResponseData[4] != (byte)'0')
                    {
                        UpdateLog(testCommandInfo, true);
                        continue;
                    }

                    UpdateLog(testCommandInfo, false);

                    //passed all this stuff so add this device
                    anyDevicesFound = true;
                    m_FoundDevicesInfo.Add(new FoundDeviceInformation(comPortNames[i], m_addressesToSearchOn[j]));
                }
                m_CommsConnection.CloseComPort();
            }

            m_SearchWorker.ReportProgress(100);

            m_e_DeviceSearchCompleteEvent(null, anyDevicesFound);
            m_Busy = false;
        }

        /// <summary>
        /// Runs the file download in the background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void m_DownloadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //open the port for the download
            if (!m_CommsConnection.OpenSSPComPort(m_CommsCommandStructure))
            {
                m_e_DeviceDownloadCompleteEvent(null, ITLlib.DOWNLOAD_STATUS.PORT_CLOSED);
                m_Busy = false;
                return;
            }
            ITLlib.SSP_COMMAND_INFO downloadCommsInfo = new ITLlib.SSP_COMMAND_INFO();

            //start up the download
            m_CommsConnection.DownloadFile(m_DownloadFileName, m_CommsCommandStructure, downloadCommsInfo);
            //check the status immediately to make sure the file is a valid SSP download
            if (m_CommsConnection.GetDownloadStatus() == ITLlib.DOWNLOAD_STATUS.INVALID_FILE)
            {
                m_e_DeviceDownloadCompleteEvent(null, ITLlib.DOWNLOAD_STATUS.INVALID_FILE);
                m_Busy = false;
                return;
            }
            float Progress = 0;
            ITLlib.DOWNLOAD_STATUS DownloadStatus;
            //monitor the download
            while (true)
            {
                //get the current progress and status of the download
                Progress = m_CommsConnection.GetDownloadProgress();
                DownloadStatus = m_CommsConnection.GetDownloadStatus();

                m_DownloadWorker.ReportProgress((int)Progress, DownloadStatus);

                //if we're not in progress, either we're finished, or failed, so just report the current state and finish up
                if (DownloadStatus != ITLlib.DOWNLOAD_STATUS.IN_PROGRESS)
                {
                    m_CommsConnection.CloseComPort();
                    m_CommsCommandStructure = new ITLlib.SSP_COMMAND();
                    m_CommsConnection = new ITLlib.SSPComms();
                    m_e_DeviceDownloadCompleteEvent(null, DownloadStatus);
                    m_Busy = false;
                    return;
                }
                System.Threading.Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Runs the ticket print in the background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void m_PrintTicketWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //connect
            ITLlib.SSP_COMMAND_INFO printCommandInfo = new ITLlib.SSP_COMMAND_INFO();
            m_CommsCommandStructure.RetryLevel = 2;
            m_CommsCommandStructure.Timeout = 1000;

            m_CommsCommandStructure.ComPort = m_ComPort;
            m_CommsCommandStructure.EncryptionStatus = false;

            if (!m_CommsConnection.OpenSSPComPort(m_CommsCommandStructure))
            {
                //if we can't open the port, fail
                m_e_DevicePrintCompleteEvent(null, PrintCompleteStatuses.ConnectionFail);
                m_Busy = false;
                return;
            }

            //set the address to the current address
            m_CommsCommandStructure.SSPAddress = m_Address;
            //set up a sync command to establish a connection
            m_CommsCommandStructure.CommandData[0] = (byte)SSPCommandBytes.SSP_Command_Header_Bytes.Sync;
            m_CommsCommandStructure.CommandDataLength = 1;

            if (!m_CommsConnection.SSPSendCommand(m_CommsCommandStructure, printCommandInfo) ||
                m_CommsCommandStructure.ResponseDataLength < 1 ||
                m_CommsCommandStructure.ResponseData[0] != (byte)SSPCommandBytes.SSP_Generic_Reply_Bytes.OK)
            {
                UpdateLog(printCommandInfo, true);
                //if we can't sync, fail
                m_CommsConnection.CloseComPort();
                m_e_DevicePrintCompleteEvent(null, PrintCompleteStatuses.ConnectionFail);
                m_Busy = false;
                return;
            }

            UpdateLog(printCommandInfo, false);

            //next we need to check it can do a key exchange
            if (!NegotiateKeyExchange())
            {
                UpdateLog(printCommandInfo, true);
                //if we can't do keys
                m_CommsConnection.CloseComPort();
                m_e_DevicePrintCompleteEvent(null, PrintCompleteStatuses.ConnectionFail);
                m_Busy = false;
                return;
            }

            UpdateLog(printCommandInfo, false);

            //try to do an encrypted sync to check that it works
            m_CommsCommandStructure.EncryptionStatus = true;
            m_CommsCommandStructure.CommandData[0] = (byte)SSPCommandBytes.SSP_Command_Header_Bytes.Sync;
            m_CommsCommandStructure.CommandDataLength = 1;
            if (!m_CommsConnection.SSPSendCommand(m_CommsCommandStructure, printCommandInfo) ||
                m_CommsCommandStructure.ResponseDataLength < 1 ||
                m_CommsCommandStructure.ResponseData[0] != (byte)SSPCommandBytes.SSP_Generic_Reply_Bytes.OK)
            {
                UpdateLog(printCommandInfo, true);
                //if we can't sync with encryption, fail
                m_CommsConnection.CloseComPort();
                m_e_DevicePrintCompleteEvent(null, PrintCompleteStatuses.ConnectionFail);
                m_Busy = false;
                return;
            }

            UpdateLog(printCommandInfo, false);

            //fill in placeholders

            for (int i = 0; i < 3; i++)
            {
                m_CommsCommandStructure.CommandData[0] = (byte)SSPCommandBytes.SSP_Command_Header_Bytes.Print_Command;
                m_CommsCommandStructure.CommandData[1] = (byte)SSPCommandBytes.SSP_Print_Command_Sub_Commands.Setup;
                m_CommsCommandStructure.CommandData[2] = (byte)SSPCommandBytes.SSP_Print_Setup_Sub_Commands.Set_Placeholder;
                m_CommsCommandStructure.CommandData[3] = (byte)(i + 1);

                //put in the placeholder
                byte[] placeholderBytes = UnicodeStringToByteArray(m_Placeholders[i]);


                for (int j = 0; j < placeholderBytes.Length; j++)
                {
                    m_CommsCommandStructure.CommandData[4 + j] = placeholderBytes[j];
                }

                m_CommsCommandStructure.CommandDataLength = (byte)(placeholderBytes.Length + 4);

                if (!m_CommsConnection.SSPSendCommand(m_CommsCommandStructure, printCommandInfo) ||
                    m_CommsCommandStructure.ResponseDataLength < 1 ||
                    m_CommsCommandStructure.ResponseData[0] != (byte)SSPCommandBytes.SSP_Generic_Reply_Bytes.OK)
                {
                    UpdateLog(printCommandInfo, true);
                    //if we can't set the placeholder, fail
                    m_CommsConnection.CloseComPort();
                    m_e_DevicePrintCompleteEvent(null, PrintCompleteStatuses.PlaceholderSetFail);
                    m_Busy = false;
                    return;
                }

                UpdateLog(printCommandInfo, false);
            }

            //enable the device
            m_CommsCommandStructure.CommandData[0] = (byte)SSPCommandBytes.SSP_Command_Header_Bytes.Enable;
            m_CommsCommandStructure.CommandDataLength = 1;
            if (!m_CommsConnection.SSPSendCommand(m_CommsCommandStructure, printCommandInfo) ||
                    m_CommsCommandStructure.ResponseDataLength < 1 ||
                    m_CommsCommandStructure.ResponseData[0] != (byte)SSPCommandBytes.SSP_Generic_Reply_Bytes.OK)
            {
                UpdateLog(printCommandInfo, true);
                //if we can't start the print, fail
                m_CommsConnection.CloseComPort();
                m_e_DevicePrintCompleteEvent(null, PrintCompleteStatuses.CouldNotStartPrintFail);
                m_Busy = false;
                return;
            }

            UpdateLog(printCommandInfo, false);

            //send print
            m_CommsCommandStructure.CommandData[0] = (byte)SSPCommandBytes.SSP_Command_Header_Bytes.Print_Command;
            m_CommsCommandStructure.CommandData[1] = (byte)SSPCommandBytes.SSP_Print_Command_Sub_Commands.Dispense_Ticket;
            m_CommsCommandStructure.CommandData[2] = (byte)12; // the template number 
            m_CommsCommandStructure.CommandDataLength = 3;
            if (!m_CommsConnection.SSPSendCommand(m_CommsCommandStructure, printCommandInfo) ||
                    m_CommsCommandStructure.ResponseDataLength < 1 ||
                    m_CommsCommandStructure.ResponseData[0] != (byte)SSPCommandBytes.SSP_Generic_Reply_Bytes.OK)
            {
                UpdateLog(printCommandInfo, true);
                //if we can't start the print, fail
                m_CommsConnection.CloseComPort();
                m_e_DevicePrintCompleteEvent(null, PrintCompleteStatuses.CouldNotStartPrintFail);
                m_Busy = false;
                return;
            }

            UpdateLog(printCommandInfo, false);

            //poll until the print is finished.

            bool printing;
            bool printHasFailed;
            SSPCommandBytes.SSP_Print_Fail_Reasons failReason = SSPCommandBytes.SSP_Print_Fail_Reasons.NoFail;
            while (true)
            {
                printing = false;
                printHasFailed = false;
                if (!PollDevice())
                {
                    //if we can't start the print, fail
                    m_CommsConnection.CloseComPort();
                    m_e_DevicePrintCompleteEvent(null, PrintCompleteStatuses.PollFail);
                    m_Busy = false;
                    return;
                }
                //read the responses
                for (int i = 0; i < m_CommsCommandStructure.ResponseDataLength - 1; i++)
                {
                    switch (m_CommsCommandStructure.ResponseData[i + 1])
                    {
                        //Only a selection of poll responses are handled here.
                        //All poll replies appropriate to the device should be monitored and handled appropriately normally. 
                        case (byte)SSPCommandBytes.SSP_Poll_Responses.Disabled:
                            break;
                        case (byte)SSPCommandBytes.SSP_Poll_Responses.Could_Not_Print_Ticket:
                            printHasFailed = true;
                            printing = false;
                            failReason = (SSPCommandBytes.SSP_Print_Fail_Reasons)m_CommsCommandStructure.ResponseData[i + 2];
                            i++;
                            break;
                        case (byte)SSPCommandBytes.SSP_Poll_Responses.Printing_Ticket:
                            printing = true;
                            break;
                        case (byte)SSPCommandBytes.SSP_Poll_Responses.Printed_Ticket:
                            printing = false;
                            printHasFailed = false;
                            break;
                    }
                }

                if (!printing || printHasFailed)
                {
                    break;
                }
                //200 ms delay between polls
                System.Threading.Thread.Sleep(200);
            }

            m_CommsConnection.CloseComPort();

            m_Busy = false;

            if (!printHasFailed)
            {
                m_e_DevicePrintCompleteEvent(null, PrintCompleteStatuses.Success);
            }
            else
            {
                PrintCompleteStatuses failStatus = PrintCompleteStatuses.Success;
                switch (failReason)
                {
                    case SSPCommandBytes.SSP_Print_Fail_Reasons.CutFail:
                        failStatus = PrintCompleteStatuses.CutFail;
                        break;
                    case SSPCommandBytes.SSP_Print_Fail_Reasons.JamFail:
                        failStatus = PrintCompleteStatuses.PaperJamFail;
                        break;
                    case SSPCommandBytes.SSP_Print_Fail_Reasons.LoadFail:
                        failStatus = PrintCompleteStatuses.PaperLoadFail;
                        break;
                    case SSPCommandBytes.SSP_Print_Fail_Reasons.NoHead:
                        failStatus = PrintCompleteStatuses.PrinterHeadRemovedFail;
                        break;
                    case SSPCommandBytes.SSP_Print_Fail_Reasons.NoPaper:
                        failStatus = PrintCompleteStatuses.PaperLoadFail;
                        break;
                }
                m_e_DevicePrintCompleteEvent(null, failStatus);
            }
        }

        /// <summary>
        /// Uses the progress changed event from the search background worker and raises a second event that can be seen outside the object
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void m_SearchWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            m_e_DeviceSearchProgressEvent(sender, e.ProgressPercentage);
        }

        /// <summary>
        ///  Uses the progress changed event from the download background worker and raises a second event that can be seen outside the object
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void m_DownloadWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            m_e_DeviceDownloadProgressEvent(sender, (ITLlib.DOWNLOAD_STATUS)e.UserState, e.ProgressPercentage);
        }

        /// <summary>
        /// Builds a string of command text information and then raises an event so subscribers can display it
        /// </summary>
        /// <param name="CommandInfo">Structure containing information about the command</param>
        /// <param name="failedCommand">Flag that is true if the command failed, so no return data is to be displayed</param>
        private static void UpdateLog(ITLlib.SSP_COMMAND_INFO CommandInfo, bool failedCommand)
        {
            StringBuilder outputStringBuilder = new StringBuilder();

            outputStringBuilder.Append(DateTime.Now.ToLongTimeString());
            outputStringBuilder.Append(".");
            outputStringBuilder.AppendLine(DateTime.Now.Millisecond.ToString());

            if (!CommandInfo.Encrypted)
                outputStringBuilder.Append("Data Out: ");
            else
                outputStringBuilder.Append("Data Out (Pre-Encryption): ");
            outputStringBuilder.AppendLine(BitConverter.ToString(CommandInfo.PreEncryptedTransmit.PacketData, 0, CommandInfo.PreEncryptedTransmit.PacketLength));

            if (CommandInfo.Encrypted)
            {
                outputStringBuilder.Append("Data Out (Post-Encryption): ");
                outputStringBuilder.AppendLine(BitConverter.ToString(CommandInfo.Transmit.PacketData, 0, CommandInfo.Transmit.PacketLength));
            }

            if (failedCommand)
            {
                outputStringBuilder.AppendLine("No Response Received");
            }
            else
            {
                if (CommandInfo.Encrypted)
                {
                    outputStringBuilder.Append("Data In (Pre-Decryption): ");
                    outputStringBuilder.AppendLine(BitConverter.ToString(CommandInfo.Transmit.PacketData, 0, CommandInfo.Transmit.PacketLength));
                }

                if (!CommandInfo.Encrypted)
                    outputStringBuilder.Append("Data In: ");
                else
                    outputStringBuilder.Append("Data In (Post-Decryption): ");
                outputStringBuilder.AppendLine(BitConverter.ToString(CommandInfo.PreEncryptedTransmit.PacketData, 0, CommandInfo.PreEncryptedTransmit.PacketLength));
            }

            //Line separator before next bit of info
            outputStringBuilder.AppendLine();

            m_e_DeviceCommsEvent(null, outputStringBuilder.ToString());

        }

        /// <summary>
        /// Polls the device to get the latest events
        /// </summary>
        /// <returns>Returns true if the command was sent and replied to properly, otherwise false</returns>
        private static bool PollDevice()
        {
            ITLlib.SSP_COMMAND_INFO printCommandInfo = new ITLlib.SSP_COMMAND_INFO();

            m_CommsCommandStructure.CommandData[0] = (byte)SSPCommandBytes.SSP_Command_Header_Bytes.Poll;
            m_CommsCommandStructure.CommandDataLength = 1;
            if (!m_CommsConnection.SSPSendCommand(m_CommsCommandStructure, printCommandInfo) ||
                    m_CommsCommandStructure.ResponseDataLength < 1 ||
                    m_CommsCommandStructure.ResponseData[0] != (byte)SSPCommandBytes.SSP_Generic_Reply_Bytes.OK)
            {
                UpdateLog(printCommandInfo, true);
                return false;
            }

            UpdateLog(printCommandInfo, false);

            return true;
        }

        /// <summary>
        /// Performs a key exchange with the device so that encrypted communications can be performed
        /// </summary>
        /// <returns>Returns true if the key exchange was successful, otherwise false</returns>
        private static bool NegotiateKeyExchange()
        {
            ITLlib.SSP_COMMAND_INFO commandInfo = new ITLlib.SSP_COMMAND_INFO();

            m_CommsCommandStructure.EncryptionStatus = false;
            //sync first
            m_CommsCommandStructure.CommandData[0] = (byte)SSPCommandBytes.SSP_Command_Header_Bytes.Sync;
            m_CommsCommandStructure.CommandDataLength = 1;

            if (!m_CommsConnection.SSPSendCommand(m_CommsCommandStructure, commandInfo) ||
                m_CommsCommandStructure.ResponseDataLength < 1 ||
                m_CommsCommandStructure.ResponseData[0] != (byte)SSPCommandBytes.SSP_Generic_Reply_Bytes.OK)
            {
                //didn't get an OK back from a sync. This is really unsual, but still should cause a fail
                UpdateLog(commandInfo, true);
                return false;
            }

            UpdateLog(commandInfo, false);

            m_CommsConnection.InitiateSSPHostKeys(m_eSSPKeys, m_CommsCommandStructure);

            //set up the generator, and the set up the command to send it
            m_CommsCommandStructure.CommandData[0] = (byte)SSPCommandBytes.SSP_Command_Header_Bytes.Set_Generator;
            m_CommsCommandStructure.CommandDataLength = 9;
            for (int i = 0; i < 8; i++)
            {
                m_CommsCommandStructure.CommandData[i + 1] = (byte)(m_eSSPKeys.Generator >> (8 * i));
            }
            if (!m_CommsConnection.SSPSendCommand(m_CommsCommandStructure, commandInfo) ||
                m_CommsCommandStructure.ResponseDataLength < 1 ||
                m_CommsCommandStructure.ResponseData[0] != (byte)SSPCommandBytes.SSP_Generic_Reply_Bytes.OK)
            {
                //didn't get an OK back from a sync. This is really unsual, but still should cause a fail
                UpdateLog(commandInfo, true);
                return false;
            }

            UpdateLog(commandInfo, false);

            //set modulus command
            m_CommsCommandStructure.CommandData[0] = (byte)SSPCommandBytes.SSP_Command_Header_Bytes.Set_Modulus;
            m_CommsCommandStructure.CommandDataLength = 9;
            for (int i = 0; i < 8; i++)
            {
                m_CommsCommandStructure.CommandData[i + 1] = (byte)(m_eSSPKeys.Modulus >> (8 * i));
            }
            if (!m_CommsConnection.SSPSendCommand(m_CommsCommandStructure, commandInfo) ||
                m_CommsCommandStructure.ResponseDataLength < 1 ||
                m_CommsCommandStructure.ResponseData[0] != (byte)SSPCommandBytes.SSP_Generic_Reply_Bytes.OK)
            {
                //didn't get an OK back from a sync. This is really unsual, but still should cause a fail
                UpdateLog(commandInfo, true);
                return false;
            }

            UpdateLog(commandInfo, false);

            //do key exchange			
            m_CommsCommandStructure.CommandData[0] = (byte)SSPCommandBytes.SSP_Command_Header_Bytes.Request_Key_Exchange;
            m_CommsCommandStructure.CommandDataLength = 9;
            for (int i = 0; i < 8; i++)
            {
                m_CommsCommandStructure.CommandData[i + 1] = (byte)(m_eSSPKeys.HostInter >> (8 * i));
            }
            if (!m_CommsConnection.SSPSendCommand(m_CommsCommandStructure, commandInfo) ||
                m_CommsCommandStructure.ResponseDataLength < 9 ||
                m_CommsCommandStructure.ResponseData[0] != (byte)SSPCommandBytes.SSP_Generic_Reply_Bytes.OK)
            {
                //didn't get an OK back from a sync. This is really unsual, but still should cause a fail
                UpdateLog(commandInfo, true);
                return false;
            }

            UpdateLog(commandInfo, false);

            //get the slave inter key from the response
            m_eSSPKeys.SlaveInterKey = 0;
            for (int i = 0; i < 8; i++)
            {
                m_eSSPKeys.SlaveInterKey += (UInt64)m_CommsCommandStructure.ResponseData[1 + i] << (8 * i);
            }

            //create the encryption key from the various keys
            m_CommsConnection.CreateSSPHostEncryptionKey(m_eSSPKeys);
            //set these keys in the commands
            m_CommsCommandStructure.Key.FixedKey = 0x0123456701234567;
            m_CommsCommandStructure.Key.VariableKey = m_eSSPKeys.KeyHost;
            return true;

        }

        /// <summary>
        /// Converts a string into a unicode byte array to be sent to the printer as a placeholder
        /// </summary>
        /// <param name="unicodeString">The string to convert</param>
        /// <returns></returns>
        private static byte[] UnicodeStringToByteArray(string unicodeString)
        {
            byte[] returnArray;

            char[] charArray = unicodeString.ToCharArray();

            returnArray = new byte[charArray.Length * 2];

            for (int i = 0; i < unicodeString.Length; i++)
            {
                UInt16 u16Val;

                u16Val = Convert.ToUInt16(charArray[i]);

                returnArray[i * 2] = BitConverter.GetBytes(u16Val)[0];
                returnArray[i * 2 + 1] = BitConverter.GetBytes(u16Val)[1];
            }

            return returnArray;
        }

        /// <summary>
        /// Calculates the percentage complete a nested for loop is
        /// </summary>
        /// <param name="i">current iteration of the outside loop</param>
        /// <param name="j">current iteration of the inside loop</param>
        /// <param name="iLength">max value of the outside loop</param>
        /// <param name="jLength">max value of the inside loop</param>
        /// <returns>The calculated percentage</returns>
        private static int CalculateNestedForPercentDone(int i, int j, int iLength, int jLength)
        {
            int basePercent = (int)(((double)i / (double)iLength) * 100);

            int secondPercent = (int)(((double)j / (double)jLength) * 100) / iLength;

            return basePercent + secondPercent;
        }

        #endregion

        #region Public (Internal) Methods

        /// <summary>
        /// Starts a search for devices connected to the PC
        /// Use the DeviceSearchProgress and DeviceSearchComplete events to track the progress of this search
        /// </summary>
        internal static bool StartDeviceSearch()
        {
            if (m_Busy)
                return false;
            m_Busy = true;
            m_SearchWorker.RunWorkerAsync();
            return true;

        }

        /// <summary>
        /// Sets the information to use from a device search
        /// </summary>
        /// <param name="DeviceInfo">The FoundDeviceInformation object to use</param>
        internal static bool SetDeviceInformation(FoundDeviceInformation DeviceInfo)
        {
            //Check we aren't busy before trying to do this
            if (m_Busy)
                return false;
            m_ComPort = DeviceInfo.DevicePort;
            m_Address = DeviceInfo.DeviceAddress;
            return true;
        }

        /// <summary>
        /// Sets the addresses to search through when trying to find the device
        /// </summary>
        /// <param name="AddressesToSearch">A list of all the addresses</param>
        internal static bool SetAddressesToSearchOn(List<byte> AddressesToSearch)
        {
            //Check we aren't busy before trying to do this
            if (m_Busy)
                return false;
            m_addressesToSearchOn.Clear();

            for (int i = 0; i < AddressesToSearch.Count; i++)
            {
                m_addressesToSearchOn.Add(AddressesToSearch[i]);
            }
            return true;
        }

        /// <summary>
        /// Starts the download of a file to the device
        /// Use the DeviceDownloadProgress and DeviceDownloadComplete eventsto track the progress of the download
        /// </summary>
        /// <param name="fileLocation">The file path and location of the file to download</param>
        /// <returns></returns>
        internal static bool StartDownloadToDevice(string fileLocation)
        {
            //Check we aren't busy before trying to do this, and also that we have a com port set and the
            //file we are trying to donwload exists
            bool file = System.IO.File.Exists(fileLocation);
            if (m_Busy || m_ComPort == "" || !System.IO.File.Exists(fileLocation))
                return false;

            m_DownloadFileName = fileLocation;

            m_Busy = true;

            m_DownloadWorker.RunWorkerAsync();

            return true;
        }

        /// <summary>
        /// Starts the process for printing a template ticket from the device
        /// </summary>
        /// <param name="placeHolders">List containing the values to fill in on the ticket before printing</param>
        /// <returns></returns>
        internal static bool StartPrintOfTemplateTicket(List<string> placeHolders)
        {
            //Check we aren't busy before trying to do this, and also that we have a com port set 
            if (m_Busy || m_ComPort == "")
                return false;

            //Check we have the appropriate number of placeholders
            if (placeHolders.Count < 3)
                return false;

            //copy the placeholders over
            m_Placeholders.Clear();
            for (int i = 0; i < placeHolders.Count; i++)
            {
                m_Placeholders.Add(placeHolders[i]);
            }

            m_Busy = true;

            m_PrintTicketWorker.RunWorkerAsync();

            return true;
        }

        #endregion

    }
}
