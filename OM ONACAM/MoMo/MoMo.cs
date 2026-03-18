using MoMo.Helpers;
using MoMo.Http;
using MoMo.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MoMo.Global;
using MoMo.Lecteur;
using System.IO;
using System.Threading;
using MoMo.Print;

namespace MoMo
{
    public partial class MoMo : Form
    {
        private Response response;
        private Transaction transaction;
        bool Running = false; // Indicates the status of the main poll loop
        int pollTimer = 250; // Timer in ms between polls
        int reconnectionAttempts = 10, reconnectionInterval = 3; // Connection info to deal with retrying connection to validator
        volatile bool Connected = false, ConnectionFail = false; // Threading bools to indicate status of connection with validator
        CValidator Validator; // The main validator class - used to send commands to the unit
        System.Windows.Forms.Timer reconnectionTimer = new System.Windows.Forms.Timer(); // Timer used to give a delay between reconnect attempts
        Thread ConnectionThread; // Thread used to connect to the validator
        StreamWriter sw = null;

        private bool ImprimanteExist = false;
        public MoMo()
        {
            InitializeComponent();
            initEvent();
            workerLogin.RunWorkerAsync();
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

        private void initEvent()
        {
            inputPhone.KeyPress += new System.Windows.Forms.KeyPressEventHandler(CheckKeys);
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

        private void hideAllPanel()
        {
            panelHS.Visible = false;
            panelPhoneNumber.Visible = false;
            panelSender.Visible = false;
            panelAmount.Visible = false;
            panelInsert.Visible = false;
            panelConfirm.Visible = false;
            panelStatus.Visible = false;
        }

        private void btnCancelHs_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void workerLogin_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Auth.login();
        }

        private void workerLogin_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(this.response == null)
            {
                hideAllPanel();
                panelHS.Visible = true;
                timerClose.Enabled = true;
                timerClose.Start();

            }
            else if(this.response.status == 0)
            {
                loaderPhone.Visible = false;
                inputPhone.Enabled = true;
                btnCancelPhone.Enabled = true;
                btnNextPhone.Enabled = true;
                transaction = new Models.Transaction() { access_token = response.message.Split(';')[0], transactionId = response.transactionId };
                timerFourMinute.Enabled = true;
                timerFourMinute.Start();
                inputPhone.Focus();
            }else
            {
                hideAllPanel();
                panelHS.Visible = true;
                timerClose.Enabled = true;
                timerClose.Start();
            }
            
        }

        private void timerClose_Tick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnCancelPhone_Click(object sender, EventArgs e)
        {
            loaderPhone.Visible = true;
            workerLogout.RunWorkerAsync();
            btnCancelPhone.Enabled = false;
            btnNextPhone.Enabled = false;
        }

        private void workerLogout_DoWork(object sender, DoWorkEventArgs e)
        {
            Auth.logout(transaction);
        }

        private void timerFourMinute_Tick(object sender, EventArgs e)
        {
            timerOneMinute.Enabled = true;
            timerOneMinute.Start();
            MessageBox.Show("Votre Session expire dans une minute Veuillez proceder à l'insertion de billet",
                "ERREUR", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void workerLogout_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Application.Exit();
        }

        private void timerOneMinute_Tick(object sender, EventArgs e)
        {
            workerLogout.RunWorkerAsync();
        }

        private void onOffBtn(Button back, Button next, PictureBox loader)
        {
            back.Enabled = !back.Enabled;
            next.Enabled =! next.Enabled;
            loader.Visible = !loader.Visible;
        }

        private void btnNextPhone_Click(object sender, EventArgs e)
        {
            panelErrPhone.Visible = false;
            if (! Helper.isPhone(inputPhone.Text))
            {
                labErrPhone.Text = "Numéro de téléphone invalide";
                panelErrPhone.Visible = true;
                inputPhone.Focus();
            }else
            {
                transaction.phone = inputPhone.Text;
               
                onOffBtn(btnCancelPhone, btnNextPhone, loaderPhone);
                workerCheckPhone.RunWorkerAsync();
            }
        }

        private void workerCheckPhone_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Request.checkOp(this.transaction);
        }

        private void workerCheckPhone_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            onOffBtn(btnCancelPhone, btnNextPhone, loaderPhone);
            if (this.response == null)
            {
                labErrPhone.Text = "Une erreur est survenue réessayer";
                panelErrPhone.Visible = true;
            }else if(this.response.status == 28)
            {
                labErrPhone.Text = "Veuillez entrer un numero ORANGE";
                panelErrPhone.Visible = true;
            }else if(this.response.status == 0)
            {
                hideAllPanel();
                panelSender.Visible = true;
                labPhoneSender.Text = transaction.phone;
                inputSender.Focus();
            }
            else
            {
                labErrPhone.Text = "Mauvais numero";
                panelErrPhone.Visible = true;
            }
        }

        private void btnbackSender_Click(object sender, EventArgs e)
        {
            hideAllPanel();
            panelPhoneNumber.Visible = true;
        }

        private void btnNextSender_Click(object sender, EventArgs e)
        {
            panelErrSender.Visible = false;
            if(string.IsNullOrEmpty(inputSender.Text ) || string.IsNullOrWhiteSpace(inputSender.Text))
            {
                panelErrSender.Visible = true;
            }else
            {
                transaction.sender = inputSender.Text;
                labPhoneAmnt.Text = transaction.phone;
                labSenderAmnt.Text = transaction.sender.ToUpper();

                hideAllPanel();
                panelAmount.Visible = true;
                inputAmnt.Focus();
            }
        }

        private void btnbackAmnt_Click(object sender, EventArgs e)
        {
            hideAllPanel();
            panelSender.Visible = true;
        }

        private void btnNextAmnt_Click(object sender, EventArgs e)
        {
            panelErrAmnt.Visible = false;
            if ( !Helper.isValidAmnt(inputAmnt.Text))
            {
                labErrAMnt.Text = "Montant Invalide";
                panelErrAmnt.Visible = true;
            }else
            {
                transaction.amount = inputAmnt.Text;
                onOffBtn(btnbackAmnt, btnNextAmnt, loaderAmnt);
                workerEnterInfo.RunWorkerAsync();
            }
        }

        private void workerEnterInfo_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Request.enterInfo(transaction);
        }

        private void backgroundValidator_DoWork(object sender, DoWorkEventArgs e)
        {
            MainLoop();
        }

        private void backgroundValidator_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            updateLabInsert(Global.Global.montant);
            transaction.Iamount = Global.Global.montant;
        }

        private void workerEnterInfo_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            onOffBtn(btnbackAmnt, btnNextAmnt, loaderAmnt);
            if (this.response == null)
            {
                labErrAMnt.Text = "Une erreur est survenue réessayer";
                panelErrAmnt.Visible = true;
            }else if(this.response.status == 21)
            {
                labErrAMnt.Text = "Montant Maximale "+ response.message+" FCFA";
                panelErrAmnt.Visible = true;
            }else if(response.status == 27)
            {
               if(Global.Global.montant == 0)
                {
                    btnbackAmnt.Visible = false;
                    btnNextAmnt.Visible = false;
                    btnCancelAmnt.Visible = true;
                    labErrAMnt.Text = "Désolé nous ne pouvons vous servir";
                    panelErrAmnt.Visible = true;
                    timerOneMinute.Enabled = true;
                    timerOneMinute.Start();
                }
                else
                {
                    labErrAMnt.Text = "Nous ne pouvons vous servir pour ce montant ";
                    panelErrAmnt.Visible = true;
                }


            }else if(response.status == 0)
            {
                if (!Running)
                {
                    try
                    {
                        backgroundValidator.RunWorkerAsync();
                    }catch(Exception ed)
                    {

                    }
                }

                timerFourMinute.Enabled = false;
                labSenderInsert.Text = transaction.sender.ToUpper();
                labAmntInsert.Text = transaction.amount;
                labPhoneInsert.Text = transaction.phone;
                hideAllPanel();
                panelInsert.Visible = true;
            }
            else
            {
                labErrAMnt.Text = "Une erreur est survenue réessayer";
                panelErrAmnt.Visible = true;
            }
        }

        private void btnbackInsert_Click(object sender, EventArgs e)
        {
            hideAllPanel();
            panelAmount.Visible = true;
        }

        private void btnNextInsert_Click(object sender, EventArgs e)
        {
            panelErrInsert.Visible = false;
            if(int.Parse(transaction.amount) > transaction.Iamount)
            {
                labErrInsert.Text = "Le montant inséré est inférieur";
                panelErrInsert.Visible = true;
            }else if(int.Parse(transaction.amount) < transaction.Iamount)
            {
                labErrInsert.Text = "Le montant inséré est supérieur";
                panelErrInsert.Visible = true;
            }else
            {
                onOffBtn(btnbackInsert, btnNextInsert, loarderInsert);
                workerInsert.RunWorkerAsync();
            }
        }

        private void workerInsert_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Request.validInsert(this.transaction);
        }

        private void workerInsert_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            onOffBtn(btnbackInsert, btnNextInsert, loarderInsert);
            
            if (this.response == null)
            {
                labErrInsert.Text = "Une erreur réesayer SVP ";
                panelErrInsert.Visible = true;
            }else if(response.status == 0)
            {
                labPhoneC.Text = transaction.phone;
                labAmntC.Text = transaction.Iamount.ToString();
                hideAllPanel();
                panelConfirm.Visible = true;
            }else
            {
                labErrInsert.Text = "Une erreur réesayer SVP ";
                panelErrInsert.Visible = true;
            }
        }

        private void btnBackC_Click(object sender, EventArgs e)
        {
            hideAllPanel();
            panelInsert.Visible = true;
        }

        private void btnValidC_Click(object sender, EventArgs e)
        {
            onOffBtn(btnBackC, btnValidC, loaderC);
            workerTerminate.RunWorkerAsync();
        }

        private void workerTerminate_DoWork(object sender, DoWorkEventArgs e)
        {
            this.response = Request.terminate(transaction);
        }

        private void workerTerminate_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            hideAllPanel();
            panelStatus.Visible = true;
            onOffBtn(btnBackC, btnValidC, loaderC);
            labPhoneSt.Text = transaction.phone;
            labAmntSt.Text = transaction.amount;
            // print
            startPrint();
            if(this.response == null)
            {
                labMsgSt.Text = "Désolé une erreur est survenue";
                labStatus.Text = "Echec";
                labStatus.BackColor = Color.Red;
                pictureSt.BackColor = Color.Red;
                         
            }else if(response.status == 0)
            {

            }else
            {
                labMsgSt.Text = "Votre recharge aura un leger retard";
            }
            timerClose.Enabled = true;
            timerClose.Start();
        }

        private void pictureClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnCancelAmnt_Click(object sender, EventArgs e)
        {
            loaderAmnt.Visible = true;
            inputAmnt.Enabled = false;
            btnCancelAmnt.Enabled = false;
            workerLogout.RunWorkerAsync();
        }

        private void updateLabInsert(int amount)
        {
            labInsert.Text = amount.ToString();
        }
        private void startPrint()
        {
            List<string> parameterPlaceholders = new List<string>();
            parameterPlaceholders.Add(transaction.transactionId.ToString());
            parameterPlaceholders.Add(transaction.phone);
            parameterPlaceholders.Add(transaction.sender);
            parameterPlaceholders.Add(transaction.amount);
            parameterPlaceholders.Add(Global.Global.ID);
            if (!DeviceCommunications.StartPrintOfTemplateTicket(parameterPlaceholders))
            {
                // MessageBox.Show("impossible d'imprimer");
            }
        }
    }
}
