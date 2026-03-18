using ERA.Lecteur;
using ERA.Print;
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

namespace ERA
{
    public partial class ERA : Form
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

        public ERA()
        {
            InitializeComponent();
            initEvent();
            Validator = new CValidator();
            timer1.Interval = pollTimer;
            reconnectionTimer.Tick += new EventHandler(reconnectionTimer_Tick);
            workerLogin.RunWorkerAsync();
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
            inputPhone.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckKeys);
            inputPhoneDest.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckKeys);
            inputAmnt.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckKeys);

        }

        private void CheckKeys(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
                System.Media.SystemSounds.Asterisk.Play();
            }
        }
        private void HideAll()
        {
            panelHS.Visible = false;
            panelPhone.Visible = false;
            panelExp.Visible = false;
            panelDest.Visible = false;
            panelAmount.Visible = false;
            panelInsert.Visible = false;
            panelConfirm.Visible = false;
            panelStatus.Visible = false;
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
            loaderPhone.Visible = false;
            if (this.response == null)
            {
                timerNoService.Enabled = true;
                timerNoService.Start();
                HideAll();
                panelHS.Visible = true;

            }else if(this.response.status == 0)
            {
                timerFourMinute.Enabled = true;
                timerFourMinute.Start();
                btnnextPhone.Enabled = true;
                btnCancelPhone.Enabled = true;
                inputPhone.Enabled = true;
                inputPhone.Focus();
                transaction = new Models.Transaction() { access_token = response.message.Split(';')[0], transactionId = response.transactionId };
                

            }
            else
            {
                timerNoService.Enabled = true;
                timerNoService.Start();
                HideAll();
                panelHS.Visible = true;
            }
        }

        private void btnCancelPhone_Click(object sender, EventArgs e)
        {
            btnCancelPhone.Enabled = false;
            btnnextPhone.Enabled = false;
            loaderPhone.Visible = true;
            workerLogout.RunWorkerAsync();
        }

        private void workerLogout_DoWork(object sender, DoWorkEventArgs e)
        {
            Http.Auth.logout(transaction);
        }

        private void timerFourMinute_Tick(object sender, EventArgs e)
        {
            timerOneMinute.Enabled = true;
            timerOneMinute.Start();
            MessageBox.Show("Votre session expire dans moins d'une minute ", "ERREUR", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void timerOneMinute_Tick(object sender, EventArgs e)
        {
            workerLogout.RunWorkerAsync();
        }

        private void workerLogout_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            loaderPhone.Visible = false;
            Application.Exit();
        }

        private void timerNoService_Tick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnnextPhone_Click(object sender, EventArgs e)
        {
            panelErrPhone.Visible = false;
            if (string.IsNullOrEmpty(inputPhone.Text) || string.IsNullOrWhiteSpace(inputPhone.Text))
            {
                labErrPhone.Text = "Veuillez entrer le numéro";
                panelErrPhone.Visible = true;
                inputPhone.Focus();
            }
            else if (!Helpers.Helper.isPhone(inputPhone.Text))
            {
                labErrPhone.Text = "Numéro invalide";
                panelErrPhone.Visible = true;
                inputPhone.Focus();
            }
            else
            {
                transaction.sender_phone = inputPhone.Text;
                panelErrPhone.Visible = false;
                offBtn(btnCancelPhone, btnnextPhone, loaderPhone);
                workercheckPhone.RunWorkerAsync();

            }

        }

        private void offBtn(Button back, Button next, PictureBox loader)
        {
            back.Enabled = false;
            next.Enabled = false;
            loader.Visible = true;
        }
        private void onBtn(Button back, Button next , PictureBox loader)
        {
            back.Enabled = true;
            next.Enabled = true;
            loader.Visible = false;
        }

        private void workercheckPhone_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Http.Request.checkPhone(transaction);
        }

        private void workercheckPhone_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            onBtn(btnCancelPhone, btnnextPhone, loaderPhone);
            if (this.response == null)
            {
                labErrPhone.Text = "Erreur Réesayer SVP !";
                panelErrPhone.Visible = true;
            }else if(this.response.status == 29)
            {
                labErrPhone.Text = "Vous avez été bloqué";
                panelErrPhone.Visible = true;
            }else if(this.response.status == 0)
            {
               
                HideAll();
                panelExp.Visible = true;
                if(this.response.message == null)
                {
                    labExp.Text = "C'est votre premier envoi d'argent enregistrez-vous";
                }
                else
                {
                    labExp.Text = "Vos informations";
                    inputNameExp.Text = response.message;
                }
                labPhoneExp.Text = inputPhone.Text;
                inputNameExp.Focus();
            }
            else
            {
                labErrPhone.Text = "Erreur Réesayer SVP !";
                panelErrPhone.Visible = true;
            }
        }

        private void btnBackExp_Click(object sender, EventArgs e)
        {
            HideAll();
            panelPhone.Visible = true;
        }

        private void btnNextExp_Click(object sender, EventArgs e)
        {
            panelExpErr.Visible = false;
            if (string.IsNullOrEmpty(inputNameExp.Text) || string.IsNullOrWhiteSpace(inputNameExp.Text))
            {
                panelExpErr.Visible = true;
            }
            else
            {
                HideAll();
                panelDest.Visible = true;
                transaction.sender_name = inputNameExp.Text;
                transaction.sender_phone = labPhoneExp.Text;
                labPhoneDest.Text = transaction.sender_phone;
                labNameDest.Text = transaction.sender_name.ToUpper();
                inputNameDest.Focus();
            }
            
        }

        private void btnBackDest_Click(object sender, EventArgs e)
        {
            HideAll();
            panelExp.Visible = true;
        }

        private void btnNextDest_Click(object sender, EventArgs e)
        {
            panelErrDest.Visible = false;
            if (string.IsNullOrEmpty(inputNameDest.Text) || string.IsNullOrWhiteSpace(inputNameDest.Text))
            {
                labErrDest.Text = "Veuillez remplir le nom";
                panelErrDest.Visible = true;
                inputNameDest.Focus();
            }else if(string.IsNullOrEmpty(inputPhoneDest.Text) || string.IsNullOrWhiteSpace(inputPhoneDest.Text))
            {
                labErrDest.Text = "Veuillez remplir le numéro";
                panelErrDest.Visible = true;
                inputPhoneDest.Focus();
            }else if (!Helpers.Helper.isPhone(inputPhoneDest.Text))
            {
                labErrDest.Text = "Numéro invalide";
                panelErrDest.Visible = true;
                inputPhoneDest.Focus();
            }
            else
            {
                transaction.receiver_name = inputNameDest.Text;
                transaction.receiver_phone = inputPhoneDest.Text;
                labNameDestAmnt.Text = transaction.receiver_name.ToUpper();
                labPhoneDestAmnt.Text = transaction.receiver_phone;
                HideAll();
                panelAmount.Visible = true;
                inputAmnt.Focus();
            }
        }

        private void btnBackAmnt_Click(object sender, EventArgs e)
        {
            HideAll();
            panelDest.Visible = true;
        }

        private void btnNextAmnt_Click(object sender, EventArgs e)
        {
            panelErrAmnt.Visible = false;
            if (string.IsNullOrEmpty(inputAmnt.Text) || string.IsNullOrWhiteSpace(inputAmnt.Text))
            {
                labErrAmnt.Text = "Veuillez inserer le montant";
                panelErrAmnt.Visible = true;
            }else if(!Helpers.Helper.isValidAmount(inputAmnt.Text))
            {
                labErrAmnt.Text = "Montant invalide";
                panelErrAmnt.Visible = true;
            }
            else
            {
                transaction.amount =int.Parse(inputAmnt.Text);
                offBtn(btnBackAmnt, btnNextAmnt, loaderAmnt);
                workerEnterInfo.RunWorkerAsync();
            }
        }

        private void workerEnterInfo_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Http.Request.enterInfo(transaction);
        }

        private void workerEnterInfo_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            onBtn(btnBackAmnt, btnNextAmnt, loaderAmnt);
            if (this.response == null)
            {
                labErrAmnt.Text = "Erreur Réesayer SVP !";
                panelErrAmnt.Visible = true;
            }else if(this.response.status == 21)
            {
                labErrAmnt.Text = "Montant maximal : "+this.response.message;
                panelErrAmnt.Visible = true;
            }else if(response.status == 27)
            {
                if (Global.Global.montant == 0)
                {
                    labErrAmnt.Text = "Nous ne pouvons pas vous servir";
                    panelErrAmnt.Visible = true;
                    btnNextAmnt.Visible = false;
                    btnBackAmnt.Visible = false;
                    btnCancelAmnt.Visible = true;
                }
                else
                {
                    labErrAmnt.Text = "Nous ne pouvons pas vous servir ce montant";
                    panelErrAmnt.Visible = true;
                }
            }else if(this.response.status == 0)
            {
                transaction.frais =int.Parse( this.response.message);
                timerFourMinute.Enabled = false;
                timerOneMinute.Enabled = false;
                timerFourMinute.Stop();
                timerOneMinute.Stop();

                labExpNameInsert.Text = transaction.sender_name.ToUpper();
                labExpPhoneInsert.Text = transaction.sender_phone;
                labDestNameInsert.Text = transaction.receiver_name.ToUpper();
                labDestPhoneInsert.Text = transaction.receiver_phone;
                labAmntInsert.Text = transaction.amount.ToString();
                labFraisInsert.Text = transaction.frais.ToString();
                labNetInsert.Text = transaction.net;

                HideAll();
                panelInsert.Visible = true;
                try
                {
                    if (!Running)
                    {
                        backgroundValidator.RunWorkerAsync();
                    }
                }catch(Exception ex)
                {

                }

            }
            else
            {
                labErrAmnt.Text = "Erreur Réesayer SVP !";
                panelErrAmnt.Visible = true;
            }
        }

        private void backgroundValidator_DoWork(object sender, DoWorkEventArgs e)
        {
            MainLoop();
        }

        private void backgroundValidator_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            printAmount(Global.Global.montant);
        }

        private void btnCancelAmnt_Click(object sender, EventArgs e)
        {
            btnBackAmnt.Enabled = false;
            btnNextAmnt.Enabled = false;
            btnCancelAmnt.Enabled = false;
            loaderAmnt.Visible = true;
            workerLogout.RunWorkerAsync();
        }

        private void btnNextInsert_Click(object sender, EventArgs e)
        {
            panelErrInsert.Visible = false;
            if (transaction.Iamount == 0)
            {
                labErrInsert.Text = "Inserez les billets SVP !";
                panelErrInsert.Visible = true;
            }else if(transaction.amount > transaction.Iamount)
            {
                labErrInsert.Text = "Montant Inférieur";
                panelErrInsert.Visible = true;
            }else if(transaction.amount < transaction.Iamount)
            {
                labErrInsert.Text = "Montant Supérieur";
                panelErrInsert.Visible = true;
            }
            else
            {
                offBtn(btnBackInsert, btnNextInsert, loaderInsert);
                workerInsert.RunWorkerAsync();
            }
        }

        private void workerInsert_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Http.Request.validInsert(transaction);
        }

        private void workerInsert_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            onBtn(btnBackInsert, btnNextInsert, loaderInsert);
            if (this.response == null)
            {
                labErrInsert.Text = "Erreur Réesayer SVP !";
                panelErrPhone.Visible = true;
            }else if(this.response.status == 0)
            {
                labExptNameConf.Text = transaction.sender_name.ToUpper();
                labExpPhoneConfirm.Text = transaction.sender_phone;
                labDestNameConf.Text = transaction.receiver_name.ToUpper();
                labDestPhoneConf.Text = transaction.receiver_phone;
                labAmntConf.Text = transaction.Iamount.ToString();
                labFraisConf.Text = transaction.frais.ToString();
                labNetConf.Text = transaction.net;

                HideAll();
                panelConfirm.Visible = true;
            }
            else
            {
                labErrInsert.Text = "Erreur Réesayer SVP !";
                panelErrPhone.Visible = true;
            }
        }

        private void btnBackConfig_Click(object sender, EventArgs e)
        {
            HideAll();
            panelInsert.Visible = true;
        }

        private void btnValid_Click(object sender, EventArgs e)
        {
            offBtn(btnBackConfig, btnValid, loaderConf);
            workerValid.RunWorkerAsync();
        }

        private void pictureClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void workerValid_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Http.Request.terminate(transaction);
        }

        private void workerValid_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // onBtn(btnBackConfig, btnValid, loaderConf);
           
            timerNoService.Enabled = true;
            timerNoService.Start();
            HideAll();
            panelStatus.Visible = true;
            labPhoneStatus.Text = transaction.receiver_phone;
            labAmntStatus.Text = transaction.net;
            if(this.response == null)
            {
                pictureStatus.BackColor = Color.Red;
                labStatus.BackColor = Color.Red;
                labMsgStatus.Text = "Transaction Echouée";
                transaction.bordo = "-1";
            }
            else if(this.response.status == 0)
            {
                transaction.bordo = this.response.message;
            }
            else
            {
                labMsgStatus.Text = "Votre transaction aura un retard";
                transaction.bordo = "0000";
            }
            startPrint();
        }

        private void btnBackInsert_Click(object sender, EventArgs e)
        {
            HideAll();
            panelAmount.Visible = true;
        }

        private void printAmount(int amount)
        {
            transaction.Iamount = amount;
            labInsert.Text = amount.ToString();
        }

        private void startPrint()
        {
            List<string> parameterPlaceholders = new List<string>();
            parameterPlaceholders.Add(transaction.receiver_name.ToUpper());
            parameterPlaceholders.Add(transaction.receiver_phone);
            parameterPlaceholders.Add(transaction.sender_name.ToUpper());
            parameterPlaceholders.Add(transaction.bordo);
            parameterPlaceholders.Add(transaction.Iamount.ToString());
            parameterPlaceholders.Add(transaction.frais.ToString());
            parameterPlaceholders.Add(Global.Global.ID);
            if (!DeviceCommunications.StartPrintOfTemplateTicket(parameterPlaceholders))
            {
                // MessageBox.Show("impossible d'imprimer");
            }
        }
    }
}
