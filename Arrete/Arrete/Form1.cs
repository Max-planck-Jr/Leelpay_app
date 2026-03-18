using Arrete.Http;
using Arrete.Print;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Arrete
{
    public partial class Form1 : Form
    {
        private bool ImprimanteExist = false;
        private Response response;
        private User user;
        public Form1()
        {
            InitializeComponent();
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

        private void startPrint()
        {
            List<string> parameterPlaceholders = new List<string>();
            parameterPlaceholders.Add(labAmount.Text);
            parameterPlaceholders.Add(labAmount.Text);
            parameterPlaceholders.Add(labAmount.Text);
           
            

            if (!DeviceCommunications.StartPrintOfTemplateTicket(parameterPlaceholders))
            {
                // MessageBox.Show("impossible d'imprimer");
            }
        }



        private void btnNext_Click(object sender, EventArgs e)
        {
            if(string.IsNullOrEmpty(InputLogin.Text) || string.IsNullOrWhiteSpace(InputLogin.Text))
            {
                labError.Text = " Veuillez entrer le Login";
                panelError.Visible = true;
                return;
            }else if(string.IsNullOrEmpty(inputPass.Text) || string.IsNullOrWhiteSpace(inputPass.Text))
            {
                labError.Text = " Veuillez entrer le Mot de Passe";
                panelError.Visible = true;
                return;
            }else if(string.IsNullOrEmpty(inputKey.Text) || string.IsNullOrWhiteSpace(inputKey.Text))
            {
                labError.Text = " Veuillez entrer la Clé";
                panelError.Visible = true;
                return;
            }
            else
            {
                panelError.Visible = false;
                btnNext.Enabled = false;
                btnCancel.Enabled = false;
                loader.Visible = true;
                user = new User(InputLogin.Text, inputPass.Text, inputKey.Text);
                workerAuth.RunWorkerAsync();
            }
            
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            panelAuth.Visible = true;
            panelConfirm.Visible = false;
        }

        private void workerAuth_DoWork(object sender, DoWorkEventArgs e)
        {
            response = Request.auth(user);
        }

        private void workerAuth_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnNext.Enabled = true;
            btnCancel.Enabled = true;
            loader.Visible = false;
            if(response == null)
            {
                labError.Text = " Manque de Connexion A internet";
                panelError.Visible = true;
            }else if(response.status == 101)
            {
                labError.Text = "Veuillez bien remplir les champs";
                panelError.Visible = true;
            }else if(response.status == 102 )
            {
                labError.Text = "Authentification echouée";
                panelError.Visible = true;
            }else if(response.status == 103)
            {
                labError.Text = "Vous n etes pas un Agent";
                panelError.Visible = true;
            }else if(response.status == 104)
            {
                labError.Text = "Aucun arrete ne vous été affecté ";
                panelError.Visible = true;
            }else if(response.status == 105)
            {
                labError.Text = "La Clé d'arrete est mauvaise ";
                panelError.Visible = true;
            }else if(response.status == 106)
            {
                labError.Text = "Cet arrete a deja été traité ";
                panelError.Visible = true;
            }else if(response.status == 107)
            {
                labError.Text = "Vous etes sur la mauvaise Borne";
                panelError.Visible = true;
            }
            else if(response.status == 200)
            {
                panelAuth.Visible = false;
                panelConfirm.Visible = true;
                btnNext.Visible = false;
                btnValid.Visible = true;
                labAmount.Text = response.amount.ToString();
            }
            else
            {
                labError.Text = "Une erreur est survenue ";
                panelError.Visible = true;
            }

        }

        private void InputLogin_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnValid_Click(object sender, EventArgs e)
        {
            loader.Visible = true;
            btnCancel.Enabled = false;
            btnValid.Enabled = false;
            workerConfirm.RunWorkerAsync();
        }

        private void workerConfirm_DoWork(object sender, DoWorkEventArgs e)
        {
            response = Request.confirm(user);
        }

        private void workerConfirm_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            loader.Visible = false;
            btnCancel.Enabled = true;
            btnValid.Enabled = true;
            if (response == null)
            {
                labError.Text = " Reesayez soucis avec internet";
                panelError.Visible = true;
            }
            else if (response.status == 101)
            {
                labError.Text = "Veuillez bien remplir les champs";
                panelError.Visible = true;
            }
            else if (response.status == 102)
            {
                labError.Text = "Authentification echouée";
                panelError.Visible = true;
            }
            else if (response.status == 103)
            {
                labError.Text = "Vous n etes pas un Agent";
                panelError.Visible = true;
            }
            else if (response.status == 104)
            {
                labError.Text = "Aucun arrete ne vous été affecté ";
                panelError.Visible = true;
            }
            else if (response.status == 105)
            {
                labError.Text = "La Clé d'arrete est mauvaise ";
                panelError.Visible = true;
            }
            else if (response.status == 106)
            {
                labError.Text = "Cet arrete a deja été traité ";
                panelError.Visible = true;
            }
            else if(response.status == 200)
            {
                btnCancel.Enabled = false;
                btnValid.Enabled = false;
                startPrint();
                timerStop.Enabled = true;
                timerStop.Start();
            }
            else
            {
                labError.Text = "Une erreur est survenue ";
                panelError.Visible = true;
            }
        }

        private void timerStop_Tick(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
