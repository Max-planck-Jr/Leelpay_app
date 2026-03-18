using Airtime.Lecteur;
using Airtime.Print;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Airtime
{
    public partial class Airtime : Form
    {
        private Models.Transaction transaction;
        private Http.Response response;

        bool Running = false; // Indicates the status of the main poll loop
        int pollTimer = 250; // Timer in ms between polls
        int reconnectionAttempts = 10, reconnectionInterval = 3; // Connection info to deal with retrying connection to validator
        volatile bool Connected = false, ConnectionFail = false; // Threading bools to indicate status of connection with validator
        CValidator Validator; // The main validator class - used to send commands to the unit
        System.Windows.Forms.Timer reconnectionTimer = new System.Windows.Forms.Timer(); // Timer used to give a delay between reconnect attempts
        Thread ConnectionThread; // Thread used to connect to the validator
        StreamWriter sw = null;

        private bool ImprimanteExist = false;
        public Airtime()
        {
            InitializeComponent();
            initEvent();
            workerLogin.RunWorkerAsync();
            timerFourMinute.Enabled = true;
            timerFourMinute.Start();
            Validator = new CValidator();
            timer1.Interval = pollTimer;
            reconnectionTimer.Tick += new EventHandler(reconnectionTimer_Tick);

            // initialisation des variables
            DeviceCommunications.DeviceSearchProgress += new DeviceCommunications.DeviceSearchProgressEventHandler(DeviceCommunications_DeviceSearchProgress);
            DeviceCommunications.DeviceSearchComplete += new DeviceCommunications.DeviceSearchCompleteEventHandler(DeviceCommunications_DeviceSearchComplete);
            //// handle pour ecouter les ecritures sur l'imprimantes
            DeviceCommunications.CommandInformation += new DeviceCommunications.SSPCommandInformationEventHandler(DeviceCommunications_CommandInformation);
            // on ajoute les addresses a rechercher
            DeviceCommunications.SetAddressesToSearchOn(new List<byte> { 65 });

            // gestionnaire d impression
            DeviceCommunications.DevicePrintComplete += new DeviceCommunications.DevicePrintCompleteEventHandler(DeviceCommunications_DevicePrintComplete);
            //
            if (!ImprimanteExist)
            {
                DeviceCommunications.StartDeviceSearch();
            }
        }

        //  print
        // handle device seach progress

        void DeviceCommunications_DeviceSearchProgress(object source, int progress)
        {
            //As the event is thrown from a different thread to the interface thread, we need to invoke
            //this code onto the interface thread to prevent illegal cross-thread operations
            if (this.InvokeRequired)
            {
                object[] paramsArray = new object[] { source, progress };

                this.BeginInvoke(new DeviceCommunications.DeviceSearchProgressEventHandler(DeviceCommunications_DeviceSearchProgress), paramsArray);
            }
            else
            {
                //updates the progress bar value

            }
        }
        // handle comple
        void DeviceCommunications_DeviceSearchComplete(object source, bool devicesFound)
        {
            if (this.InvokeRequired)
            {
                object[] paramsArray = new object[] { source, devicesFound };

                this.BeginInvoke(new DeviceCommunications.DeviceSearchCompleteEventHandler(DeviceCommunications_DeviceSearchComplete), paramsArray);
            }
            else
            {
                //for now we're just going to use the first one we find
                if (devicesFound)
                {
                    DeviceCommunications.SetDeviceInformation(DeviceCommunications.FoundDevices.ElementAt(0));
                    // UnlockStepTwo();
                }
                else
                {
                    // MessageBox.Show("Aucune imprimante trouvée");
                }
            }
        }
        // implementations de l handler de l ecriture des log 
        // afficher les log
        void DeviceCommunications_CommandInformation(object source, string LogAddition)
        {
            if (this.InvokeRequired)
            {
                object[] paramsArray = new object[] { source, LogAddition };

                this.BeginInvoke(new DeviceCommunications.SSPCommandInformationEventHandler(DeviceCommunications_CommandInformation), paramsArray);
            }
            else
            {
                // AppendLogTextNoScroll(LogAddition);
            }
        }
        // handle ki gere la progression du telechargement
        void DeviceCommunications_DeviceDownloadProgress(object source, ITLlib.DOWNLOAD_STATUS currentStatus, int percentageComplete)
        {
        }
        // 
        void DeviceCommunications_DeviceDownloadComplete(object source, ITLlib.DOWNLOAD_STATUS completionStatus)
        {
        }
        // echec d impression

        void DeviceCommunications_DevicePrintComplete(object source, DeviceCommunications.PrintCompleteStatuses completionStatus)
        {
            if (completionStatus != DeviceCommunications.PrintCompleteStatuses.Success)
            {
                // MessageBox.Show("Print failed with reason: " + completionStatus.ToString());
            }
        }

        private void initEvent()
        {
            inputFonePh.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckKeys);
            inputAmount.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckKeys);

        }
        // Lecteur de billet
        private bool ConnectToValidator()
        {
            // setup the timer
            reconnectionTimer.Interval = reconnectionInterval * 1000; // for ms

            // run for number of attempts specified
            for (int i = 0; i < reconnectionAttempts; i++)
            {
                // reset timer
                reconnectionTimer.Enabled = true;

                // close com port in case it was open
                Validator.SSPComms.CloseComPort();

                // turn encryption off for first stage
                Validator.CommandStructure.EncryptionStatus = false;

                // open com port and negotiate keys
                if (Validator.OpenComPort(sw) && Validator.NegotiateKeys(sw))
                {
                    Validator.CommandStructure.EncryptionStatus = true; // now encrypting
                    // find the max protocol version this validator supports
                    byte maxPVersion = FindMaxProtocolVersion();
                    if (maxPVersion > 6)
                    {
                        Validator.SetProtocolVersion(maxPVersion, sw);
                    }
                    else
                    {
                        // MessageBox.Show("This program does not support units under protocol version 6, update firmware.", "ERROR");
                        return false;
                    }
                    // get info from the validator and store useful vars
                    Validator.ValidatorSetupRequest(sw);
                    // Get Serial number
                    Validator.GetSerialNumber(sw);
                    // check this unit is supported by this program
                    if (!IsUnitTypeSupported(Validator.UnitType))
                    {
                        // MessageBox.Show("Unsupported unit type, this SDK supports the BV series and the NV series (excluding the NV11)");
                        Application.Exit();
                        return false;
                    }
                    // inhibits, this sets which channels can receive notes
                    Validator.SetInhibits(sw);
                    // enable, this allows the validator to receive and act on commands
                    Validator.EnableValidator(sw);

                    return true;
                }
                // while (reconnectionTimer.Enabled) Application.DoEvents(); // wait for reconnectionTimer to tick
            }
            return false;
        }
        private void ConnectToValidatorThreaded()
        {
            // setup the timer
            reconnectionTimer.Interval = reconnectionInterval * 1000; // for ms

            // run for number of attempts specified
            for (int i = 0; i < reconnectionAttempts; i++)
            {
                // reset timer
                reconnectionTimer.Enabled = true;

                // close com port in case it was open
                Validator.SSPComms.CloseComPort();

                // turn encryption off for first stage
                Validator.CommandStructure.EncryptionStatus = false;

                // open com port and negotiate keys
                if (Validator.OpenComPort() && Validator.NegotiateKeys())
                {
                    Validator.CommandStructure.EncryptionStatus = true; // now encrypting
                    // find the max protocol version this validator supports
                    byte maxPVersion = FindMaxProtocolVersion();
                    if (maxPVersion > 6)
                    {
                        Validator.SetProtocolVersion(maxPVersion);
                    }
                    else
                    {
                        // MessageBox.Show("This program does not support units under protocol version 6, update firmware.", "ERROR");
                        Connected = false;
                        return;
                    }
                    // get info from the validator and store useful vars
                    Validator.ValidatorSetupRequest();
                    // inhibits, this sets which channels can receive notes
                    Validator.SetInhibits();
                    // enable, this allows the validator to operate
                    Validator.EnableValidator();

                    Connected = true;
                    return;
                }
                // while (reconnectionTimer.Enabled) Application.DoEvents(); // wait for reconnectionTimer to tick
            }
            Connected = false;
            ConnectionFail = true;
        }
        private byte FindMaxProtocolVersion()
        {
            // not dealing with protocol under level 6
            // attempt to set in validator
            byte b = 0x06;
            while (true)
            {
                Validator.SetProtocolVersion(b);
                if (Validator.CommandStructure.ResponseData[0] == CCommands.SSP_RESPONSE_FAIL)
                    return --b;
                b++;
                if (b > 20)
                    return 0x06; // return default if protocol 'runs away'
            }
        }
        private bool IsUnitTypeSupported(char type)
        {
            if (type == (char)0x00)
                return true;
            return false;
        }
        private void reconnectionTimer_Tick(object sender, EventArgs e)
        {
            if (sender is System.Windows.Forms.Timer)
            {
                System.Windows.Forms.Timer t = sender as System.Windows.Forms.Timer;
                t.Enabled = false;
            }
        }
        private void MainLoop()
        {
            try
            {
                Validator.CommandStructure.ComPort = "COM5";
                Validator.CommandStructure.SSPAddress = Global.Global.SSPAddress;
                Validator.CommandStructure.Timeout = 3000;
            }catch(Exception f)
            {

            }
           

            // connect to the validator
            if (ConnectToValidator())
            {
                Running = true;
                // sw.WriteLine("\r\nPoll Loop\r\n*********************************\r\n");
            }

            while (Running)
            {


                // if the poll fails, try to reconnect
                if (!Validator.DoPoll(sw))
                {
                    // sw.WriteLine("Poll failed, attempting to reconnect...\r\n");
                    Connected = false;
                    ConnectionThread = new Thread(ConnectToValidatorThreaded);
                    ConnectionThread.Start();
                    while (!Connected)
                    {
                        if (ConnectionFail)
                        {
                            // sw.WriteLine("Failed to reconnect to validator\r\n");
                            return;
                        }
                    }
                    // sw.WriteLine("Reconnected successfully\r\n");
                }

                timer1.Enabled = true;
                this.backgroundValidator.ReportProgress((Int32)Global.Global.montant);
            }

            //close com port and threads
            Validator.SSPComms.CloseComPort();
        }
        /**
      * Le control qui n'autorise que les caracteres
      * */
        private void CheckKeys(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
                System.Media.SystemSounds.Asterisk.Play();
            }
        }

        private void btnCancelHS_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void workerLogin_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Http.Auth.login();
        }

        private void workerLogin_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            loaderPh.Visible = false;
            if(this.response == null)
            {
                timerCloseNoService.Enabled = true;
                timerCloseNoService.Start();
                panelPhone.Visible = false;
            }else if(this.response.status == Global.Global.OK)
            {
                btnNextPh.Enabled = true;
                btnCancelPh.Enabled = true;
                inputFonePh.Enabled = true;
                transaction = new Models.Transaction() { access_token = response.message.Split(';')[0], transactionId = response.transactionId };
                inputFonePh.Focus();
            }
            else
            {
                timerCloseNoService.Enabled = true;
                timerCloseNoService.Start();
                panelPhone.Visible = false;
            }
        }

        private void timerCloseNoService_Tick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnCancelPh_Click(object sender, EventArgs e)
        {
            btnNextPh.Enabled = false;
            btnCancelPh.Enabled = false;
            inputFonePh.Enabled = false;
            loaderPh.Visible = true;
            workerCancel.RunWorkerAsync();
        }

        private void workerCancel_DoWork(object sender, DoWorkEventArgs e)
        {
            Http.Auth.logout(transaction);
        }

        private void workerCancel_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Application.Exit();
        }

        private void HideAll()
        {
            panelHS.Visible = false;
            panelPhone.Visible = false;
            panelAmount.Visible = false;
            panelInsert.Visible = false;
            panelConfirm.Visible = false;
            panelStatus.Visible = false;
        }

        private void btnNextPh_Click(object sender, EventArgs e)
        {
            panelErrph.Visible = false;
            if ( !Helpers.Helper.isPhone(inputFonePh.Text))
            {
                labErrPhone.Text = "Numéro de téléphone invalide";
                panelErrph.Visible = true;
            }
            else
            {
                btnCancelPh.Enabled = false;
                btnNextPh.Enabled = false;
                transaction.phone = inputFonePh.Text;
                loaderPh.Visible = true;
                workerCheckOp.RunWorkerAsync();
                
            }
        }

        private void workerCheckOp_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Http.Request.checkOperator(transaction);
        }

        private void workerCheckOp_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            loaderPh.Visible = false;
            btnCancelPh.Enabled = true;
            btnNextPh.Enabled = true;
            panelErrph.Visible = false;
            if (this.response == null)
            {
                labErrPhone.Text = "Réessayer SVP !";
                panelErrph.Visible = true;
            }
            else if (this.response.status == 28)
            {  // op not exist
                labErrPhone.Text = "Opérateur Inconnu !";
                panelErrph.Visible = true;
            }
            else if(this.response.status == 8)
            {
                labErrPhone.Text = "Opérateur Momentanément suspendu !";
                panelErrph.Visible = true;
            }else if(this.response.status == 0)
            {
                HideAll();
                panelAmount.Visible = true;
                inputAmount.Focus();
                labPhoneA.Text = transaction.phone;
            }
            else
            {
                labErrPhone.Text = "Réessayer SVP !";
                panelErrph.Visible = true;
            }
               
        }

        private void btnBackA_Click(object sender, EventArgs e)
        {
            HideAll();
            panelPhone.Visible = true;
        }

        private void btnNextA_Click(object sender, EventArgs e)
        {
            panelErrA.Visible = false;
            if (!Helpers.Helper.isValidAmount(inputAmount.Text))
            {
                labErrA.Text = "Montant invalide";
                panelErrA.Visible = true;
            }else
            {
                btnBackA.Enabled = false;
                btnNextA.Enabled = false;
                transaction.amount = int.Parse(inputAmount.Text);
                loaderA.Visible = true;
                workerEnterInfo.RunWorkerAsync();
            }
        }

        private void workerEnterInfo_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Http.Request.enterInfo(transaction);
        }

        private void workerEnterInfo_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            loaderA.Visible = false;
            btnBackA.Enabled = true;
            btnNextA.Enabled = true;
            panelErrA.Visible = false;
            if (this.response == null)
            {
                labErrA.Text = "Réesayer SVP !";
                panelErrA.Visible = true;
            }else if(this.response.status == 21)
            {
                labErrA.Text = "Montant Maximal : " + this.response.message;
                panelErrA.Visible = true;
            }else if(this.response.status == 27)
            {
                labErrA.Text = "Nous ne puvons vous servir";
                panelErrA.Visible = true;
                btnBackA.Visible = false;
                btnNextA.Visible = false;
                btnCancelA.Visible = true;
            }else if(this.response.status == 0)
            {
                HideAll();
                panelInsert.Visible = true;
                timerFourMinute.Enabled = false;
                timerFourMinute.Stop();
                labAmountIA.Text = transaction.amount.ToString();
                labPhoneIA.Text = transaction.phone;
                try
                {
                    if (!Running)
                    {
                        backgroundValidator.RunWorkerAsync();
                    }


                }
                catch (Exception)
                {

                }
            }
            else
            {
                labErrA.Text = "Réesayer SVP !";
                panelErrA.Visible = true;
            }
        }

        private void timerFourMinute_Tick(object sender, EventArgs e)
        {
            timerOneMinute.Enabled = true;
            timerOneMinute.Start();
            MessageBox.Show("Votre Session expire dans une minute à moins que vous n'inseriez les billets", "ERREUR", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void backgroundValidator_DoWork(object sender, DoWorkEventArgs e)
        {
            MainLoop();
        }

        private void backgroundValidator_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            updateLabAmount(Global.Global.montant);
        }

        private void updateLabAmount(int amount)
        {
            transaction.Iamount = amount;
            labAmount.Text = amount.ToString();
        }

        private void btnBackIA_Click(object sender, EventArgs e)
        {
            HideAll();
            panelAmount.Visible = true;
        }

        private void btnNextIA_Click(object sender, EventArgs e)
        {
            panelErrIA.Visible = false;
            if(transaction.Iamount == 0)
            {
                labErrIA.Text = "Insérez les billets ";
                panelErrIA.Visible = true;
            }
            else if(transaction.amount> transaction.Iamount)
            {
                labErrIA.Text = "Montant Inférieur";
                panelErrIA.Visible = true;
            }else if(transaction.amount< transaction.Iamount)
            {
                labErrIA.Text = "Montant Inférieur";
                panelErrIA.Visible = true;
            }
            else
            {
                loaderIA.Visible = true;
                btnNextIA.Enabled = false;
                btnBackIA.Enabled = false;
                workerValidAmount.RunWorkerAsync();
            }
        }

        private void workerValidAmount_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Http.Request.validInsert(transaction);
        }

        private void workerValidAmount_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            loaderIA.Visible = false;
            btnNextIA.Enabled = true;
            btnBackIA.Enabled = true;
            if(this.response == null)
            {
                labErrIA.Text = "Réessayer SVP !";
                panelErrIA.Visible = true;
            }else if(this.response.status == 0)
            {
                labAmountC.Text = transaction.Iamount.ToString();
                labPhoneC.Text = transaction.phone;
                // ok
                HideAll();
                panelConfirm.Visible = true;
               

            }
            else
            {
                labErrIA.Text = "Réessayer SVP !";
                panelErrIA.Visible = true;
            }
        }

        private void btnBackC_Click(object sender, EventArgs e)
        {
            HideAll();
            panelInsert.Visible = true;
        }

        private void btnValidC_Click(object sender, EventArgs e)
        {
            btnBackC.Enabled = false;
            btnValidC.Enabled = false;
            loaderC.Visible = true;
            workerTerminate.RunWorkerAsync();
        }

        private void workerTerminate_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Http.Request.terminate(transaction);
        }

        private void workerTerminate_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            startPrint();
            btnBackC.Enabled = true;
            btnValidC.Enabled = true;
            loaderC.Visible = false;
            labPhoneS.Text = transaction.phone;
            labAmountS.Text = transaction.Iamount.ToString();
            HideAll();
            panelStatus.Visible = true;
            timerCloseNoService.Enabled = true;
            timerCloseNoService.Start();
            if (this.response == null)
            {
                labStatus.Text = "Echec";
                labStatus.BackColor = Color.Red;
                pictureStatus.BackColor = Color.Red;
                labMStatus.Text = "Désolé Votre Transaction n' a pas aboutie";
            }
            else if(this.response.status == 400)
            {
                labMStatus.Text = "Votre transfert va arriver avec un léger retard";
            }
            else if(this.response.status == 0)
            {

            }
            else
            {
                labMStatus.Text = "Votre transfert va arriver avec un léger retard";
            }
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void timerOneMinute_Tick(object sender, EventArgs e)
        {
            workerCancel.RunWorkerAsync();
        }

        private void btnCancelA_Click(object sender, EventArgs e)
        {
            inputAmount.Enabled = false;
            btnCancelA.Enabled = false;
            loaderA.Visible = true;
            workerCancel.RunWorkerAsync();
        }

        private void startPrint()
        {
            List<string> parameterPlaceholders = new List<string>();
            parameterPlaceholders.Add(transaction.transactionId.ToString());
            parameterPlaceholders.Add(transaction.phone);
            parameterPlaceholders.Add(transaction.Iamount.ToString());
            parameterPlaceholders.Add(Global.Global.ID);
            if (!DeviceCommunications.StartPrintOfTemplateTicket(parameterPlaceholders))
            {
                // MessageBox.Show("impossible d'imprimer");
            }
        }
    }
}
