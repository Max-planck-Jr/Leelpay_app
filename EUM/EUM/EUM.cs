using EUM.Lecteur;
using EUM.Print;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EUM
{
    public partial class EUM : Form
    {
        private Http.Response response;
        private Models.Transaction transaction;
        bool Running = false; // Indicates the status of the main poll loop
        int pollTimer = 250; // Timer in ms between polls
        int reconnectionAttempts = 10, reconnectionInterval = 3; // Connection info to deal with retrying connection to validator
        volatile bool Connected = false, ConnectionFail = false; // Threading bools to indicate status of connection with validator
        CValidator Validator; // The main validator class - used to send commands to the unit
        System.Windows.Forms.Timer reconnectionTimer = new System.Windows.Forms.Timer(); // Timer used to give a delay between reconnect attempts
        Thread ConnectionThread; // Thread used to connect to the validator
        StreamWriter sw = null;

        private bool ImprimanteExist = false;

        public EUM()
        {
            InitializeComponent();
            initEvent();
            Validator = new CValidator();
            timer1.Interval = pollTimer;
            reconnectionTimer.Tick += new EventHandler(reconnectionTimer_Tick);
            workerLogin.RunWorkerAsync();
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
            Validator.CommandStructure.ComPort = "COM5";
            Validator.CommandStructure.SSPAddress = Global.Global.SSPAddress;
            Validator.CommandStructure.Timeout = 3000;

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
        private void initEvent()
        {
            inputAccount.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckKeys);
            inputAmount.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckKeys);
        }

        private void CheckKeys(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
                System.Media.SystemSounds.Asterisk.Play();
            }
        }

        private void btnCancelHs_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void workerLogin_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Http.Auth.login();
        }

        private void workerLogin_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(this.response == null)
            {
                HideAll();
                panelHS.Visible = true;
                timerNoService.Enabled = true;
                timerNoService.Start();
            }else if(this.response.status == 0)
            {
                loaderAcc.Visible = false;
                inputAccount.Enabled = true;
                btnNextAcc.Enabled = true;
                btnCancelAcc.Enabled = true;
                transaction = new Models.Transaction() { access_token = response.message.Split(';')[0], transactionId = response.transactionId };
                inputAccount.Focus();
                timerFourMinute.Enabled = true;
                timerFourMinute.Start();
            }
            else
            {
                HideAll();
                panelHS.Visible = true;
                timerNoService.Enabled = true;
                timerNoService.Start();
            }
        }

        private void HideAll()
        {
            panelHS.Visible = false;
            panelAccount.Visible = false;
            panelSender.Visible = false;
            panelAmount.Visible = false;
            panelInsert.Visible = false;
            panelConfirm.Visible = false;
            panelTerminate.Visible = false;
        }

        private void timerNoService_Tick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnCancelAcc_Click(object sender, EventArgs e)
        {
            btnCancelAcc.Enabled = false;
            btnNextAcc.Enabled = false;
            loaderAcc.Visible = true;
            inputAccount.Enabled = false;
            workerLogout.RunWorkerAsync();
        }

        private void workerLogout_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Http.Auth.logout(transaction);
        }

        private void workerLogout_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnCancelAcc.Enabled = !btnCancelAcc.Enabled;
            btnNextAcc.Enabled = !btnNextAcc.Enabled;
            loaderAcc.Visible = !loaderAcc.Visible;
            inputAccount.Enabled = true;
            if (this.response == null)
            {
                labErrAccount.Text = " Erreur Réessayer !";
                panelErrAccount.Visible = true;
            }
            else
            {
                Application.Exit();
            }
        }

        private void timerFourMinute_Tick(object sender, EventArgs e)
        {
            timerOneMinute.Enabled = true;
            timerOneMinute.Start();
            MessageBox.Show("Votre Session expire dans une minute Veuillez proceder à l'insertion de billet",
                "ERREUR", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void timerOneMinute_Tick(object sender, EventArgs e)
        {
            workerLogout.RunWorkerAsync();
        }

        private void btnNextAcc_Click(object sender, EventArgs e)
        {
            panelErrAccount.Visible = false;
           
            if (! Helpers.Helper.isPhone(inputAccount.Text))
            {
                labErrAccount.Text = "Numéro de Téléphone Invalide";
                panelErrAccount.Visible = true;
            }
            else 
            {
                btnCancelAcc.Enabled = false;
                loaderAcc.Visible = true;
                btnNextAcc.Enabled = false;
                transaction.phone = inputAccount.Text;
                workerCheckAccount.RunWorkerAsync();

            }
        }

        private void workerCheckAccount_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Http.Request.checkAccount(transaction);
        }

        private void workerCheckAccount_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnCancelAcc.Enabled = true;
            loaderAcc.Visible = false;
            btnNextAcc.Enabled = true;
            if (this.response == null)
            {
                labErrAccount.Text = " Erreur Réessayer SVP !";
                panelErrAccount.Visible = true;
            }else if(this.response.status == 23)
            {
                // account not found 
                labErrAccount.Text = "Le Compte n'existe pas ";
                panelErrAccount.Visible = true;
            }else if(this.response.status == 29)
            {
                // account not activate 
                labErrAccount.Text = "Votre Compte a été bloqué !";
                panelErrAccount.Visible = true;
            }
            else if(this.response.status == 0)
            {
                //
                transaction.receiver = this.response.message.Split(';')[0];
                transaction.agence = this.response.message.Split(';')[1];
                labTitularSend.Text = transaction.receiver.ToUpper();
                labPhoneSend.Text = transaction.phone;
                labAgenceSend.Text = transaction.agence.ToUpper();
                HideAll();
                panelSender.Visible = true;
                inputSender.Focus();
            }
            else
            {
                labErrAccount.Text = " Erreur Réessayer SVP !";
                panelErrAccount.Visible = true;
            }
        }

        private void btnbackSend_Click(object sender, EventArgs e)
        {
            HideAll();
            panelAccount.Visible = true;
        }

        private void btnNextSend_Click(object sender, EventArgs e)
        {
            panelErrSend.Visible = false;
            if (string.IsNullOrEmpty(inputSender.Text))
            {
                panelErrSend.Visible = true;
            }
            else
            {
                transaction.sender = inputSender.Text;
                HideAll();
                labAgencAmount.Text = transaction.agence.ToUpper();
                labSenderAmnt.Text = transaction.sender.ToUpper();
                labTitularAmount.Text = transaction.receiver.ToUpper();
                labNumAmount.Text = transaction.phone;
                panelAmount.Visible = true;
                inputAmount.Focus();
            }
        }

        private void offBtn(Button back, Button next, PictureBox loader)
        {
            back.Enabled = false;
            next.Enabled = false;
            loader.Visible = true;
        }

        private void onBtn(Button back, Button next, PictureBox loader)
        {
            back.Enabled = true;
            next.Enabled = true;
            loader.Visible = false;
        }

        private void btnBackAmnt_Click(object sender, EventArgs e)
        {
            HideAll();
            panelSender.Visible = true;
        }

        private void btnNextAmnt_Click(object sender, EventArgs e)
        {
            panelErrAmnt.Visible = false;
            if( !Helpers.Helper.isValidAmount(inputAmount.Text))
            {
                labErrorAmnt.Text = " Montant Invalide";
                panelErrAmnt.Visible = true;
            }
            else
            {
                transaction.amount = inputAmount.Text;
                offBtn(btnBackAmnt, btnNextAmnt, loaderAmnt);
                workerEnterInfo.RunWorkerAsync();

            }
        }

        private void workerEnterInfo_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Http.Request.enterInformation(transaction);

        }

        private void backgroundValidator_DoWork(object sender, DoWorkEventArgs e)
        {
            MainLoop();
        }

        private void workerEnterInfo_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            loaderAmnt.Visible = false;
            onBtn(btnBackAmnt, btnNextAmnt, loaderAmnt);
            if (this.response == null)
            {
                labErrorAmnt.Text = "Erreur Réésayer";
                panelErrAmnt.Visible = true;
            }else if(this.response.status == 21)
            {
                labErrorAmnt.Text = "Montant Maximal : "+this.response.message;
                panelErrAmnt.Visible = true;
            }else if(this.response.status == 27)
            {
                // hors service
                btnNextAmnt.Visible = false;
                btnBackAmnt.Visible = false;
                inputAmount.Enabled = false;
                btnCancelAmnt.Visible = true;

            }else if(this.response.status == 0)
            {
                timerFourMinute.Enabled = false;
                timerFourMinute.Stop();
                transaction.frais = int.Parse(this.response.message);
                labReceiverIA.Text = transaction.receiver.ToUpper();
                labPhoneIA.Text = transaction.phone;
                labAgenceIA.Text = transaction.agence.ToUpper();
                labSenderIA.Text = transaction.sender.ToUpper();
                labFraisIA.Text = transaction.frais.ToString();
                labnetIA.Text = transaction.net;
                labMontantIA.Text = transaction.amount;
                HideAll();
                panelInsert.Visible = true;
                try
                {
                    if (!Running)
                    {
                        backgroundValidator.RunWorkerAsync();
                    }
                }catch(Exception f)
                {

                }
            }
            else
            {
                labErrorAmnt.Text = "Erreur Réésayer";
                panelErrAmnt.Visible = true;
            }
        }

        private void backgroundValidator_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            printAmount(Global.Global.montant);
        }

        private void btnBackIA_Click(object sender, EventArgs e)
        {
            HideAll();
            panelAmount.Visible = true;
        }

        private void btnNextIA_Click(object sender, EventArgs e)
        {
            panelErrIA.Visible = false;
            if (transaction.Iamount == 0)
            {
                labErrIA.Text = "Veuillez Inserer les billets";
                panelErrIA.Visible = true;
            }else if(transaction.Iamount < int.Parse( transaction.amount))
            {
                labErrIA.Text = "Montant Inférieur ";
                panelErrIA.Visible = true;

            }else if(transaction.Iamount > int.Parse(transaction.amount))
            {
                labErrIA.Text = "Montant Supérieur ";
                panelErrIA.Visible = true;

            }else
            {
                offBtn(btnBackIA, btnNextIA, loaderIA);
                workerValidateInsert.RunWorkerAsync();
            }
        }

        private void workerValidateInsert_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Http.Request.validateInsert(transaction);
        }

        private void workerValidateInsert_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            onBtn(btnBackIA, btnNextIA, loaderIA);
            if (this.response == null)
            {
                labErrIA.Text = "Erreur Réessayez SVP !";
                panelErrIA.Visible = true;
            }
            else if(this.response.status == 0)
            {
                labTitularC.Text = transaction.receiver;
                labPhoneC.Text = transaction.phone;
                labAgenceC.Text = transaction.agence;
                labAmountC.Text = transaction.Iamount.ToString();
                labFraisC.Text = transaction.frais.ToString();
                labNetC.Text = transaction.net;
                //
                HideAll();
                panelConfirm.Visible = true;
            }
            else
            {
                labErrIA.Text = "Erreur Réessayez SVP !";
                panelErrIA.Visible = true;
            }
        }

        private void btnBackC_Click(object sender, EventArgs e)
        {
            HideAll();
            panelInsert.Visible = true;
        }

        private void workerTerminate_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Http.Request.terminate(transaction);
        }

        private void workerTerminate_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            onBtn(btnBackC, btnNextC, loaderC);
            startPrint();
            timerNoService.Enabled = true;
            timerNoService.Start();
            labPhoneSt.Text = transaction.phone;
            labAmntSt.Text = transaction.net;
            if(this.response == null)
            {
                HideAll();
                labMsgSt.Text = "Votre transaction a echouée";
                labStatus.Text = "Echec";
                labStatus.BackColor = Color.Red;
                panelTerminate.Visible = true;
                pictureSt.BackColor = Color.Red;
            }
            else
            {
                HideAll();
                panelTerminate.Visible = true;
            }
        }

        private void btnNextC_Click(object sender, EventArgs e)
        {
            offBtn(btnBackC, btnNextC, loaderC);
            workerTerminate.RunWorkerAsync();
        }

        private void pictureBox8_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnCancelAmnt_Click(object sender, EventArgs e)
        {
            offBtn(btnBackAmnt, btnNextAmnt, loaderAmnt);
            btnCancelAmnt.Enabled = false;
            workerLogout.RunWorkerAsync();
        }

        private void printAmount(int amount)
        {
            transaction.Iamount = amount;
            labIAmount.Text = amount.ToString();
        }

        private void startPrint()
        {
            List<string> parameterPlaceholders = new List<string>();
            parameterPlaceholders.Add(transaction.transactionId.ToString());
            parameterPlaceholders.Add(transaction.phone);
            parameterPlaceholders.Add(transaction.Iamount.ToString());
            parameterPlaceholders.Add(transaction.frais.ToString());
            parameterPlaceholders.Add(transaction.net);
            parameterPlaceholders.Add(Global.Global.ID);
            if (!DeviceCommunications.StartPrintOfTemplateTicket(parameterPlaceholders))
            {
                // MessageBox.Show("impossible d'imprimer");
            }
        }
    }
}
