﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Morada_da_paz_Forms.Cadastro;
using Morada_da_paz_Forms.Arquivo;
using Morada_da_paz_Biblioteca.basicas;
using Morada_da_paz_WebService;
using System.IO;
using System.Xml;
using Morada_da_paz_Forms.Edicao;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace Morada_da_paz_Forms
{
     
    public partial class PrincipalWindow : Form
    {
        #region Atributos de sockets
        private Socket socket;
        private Thread thread;
        private TcpClient tcpClient;

        private NetworkStream networkStream;
        public static BinaryWriter binaryWriter;
        private BinaryReader binaryReader;

        TcpListener tcpListener;
        #endregion
        public static usuario usuarioAtivo = new usuario();
        private string caminho;

        NovaOcorrenciaWindow now = new NovaOcorrenciaWindow();
        ServiceMoradaDaPaz sv;
        List<ocorrencia> ocorrenciaLista;

        public PrincipalWindow(usuario login)
        {
            usuarioAtivo = login;
            InitializeComponent();
            caminho = @"c:\xml\ocorrencias"+usuarioAtivo.Nome_completo+".xml";
            this.verificaUsuario(login);
            this.Text += " -> " + usuarioAtivo.Nome_completo;

            if (usuarioAtivo.Id_especializacao_usuario.Id == 1)
            {
                thread = new Thread(new ThreadStart(RunServidor));
                thread.Start();
            }else
            {
                thread = new Thread(new ThreadStart(runCliente));
                thread.Start();
            }



        }

        

        private void sobreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, "Projeto para o gerenciamento de ocorrencias em um condominio, Desenvolvido por: ", "Sobre o Projeto", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void sobreToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            MessageBox.Show(this, "MORADA DA PAZ SOLUÇÕES - Sistemas de gerenciamento de Ocorrências\n\nDesenvolvedores:\n\nHélio Ferreira\nJorge Marçal\nGabriel Rodrigo\nDayvson Wellerson", "Projeto para Gerenciamento de Ocorrencias", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void sairToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void PrincipalWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(usuarioAtivo.Id_especializacao_usuario.Id == 1)
            {
                tcpListener.Stop();
                Environment.Exit(0);
            }
            Application.Exit();
        }

        private void ajudaToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void unidadeResidencialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UnidadeResidRegWindow window = new UnidadeResidRegWindow();
            window.ShowDialog();
        }

        private void advertênciaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AdvertenciaRegWindow window = new AdvertenciaRegWindow();
            window.ShowDialog();
        }

        private void multaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MultaRegWindow window = new MultaRegWindow();
            window.ShowDialog();
        }

        private void usuárioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserRegWindow window = new UserRegWindow();
            window.ShowDialog();
        }

        private void gerarNovaOcorrênciaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (usuarioAtivo.Id_especializacao_usuario.Id > 1)
            {
                //thread = new Thread(new ThreadStart(runCliente));
                //thread.Abort();     
            }
            NovaOcorrenciaWindow window = new NovaOcorrenciaWindow();
            window.ShowDialog();
        }

        private void muralDeOcorrenciasPublicasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MuralWindow window = new MuralWindow();
            window.ShowDialog();
        }

        private void mudarUsuárioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult getResult = MessageBox.Show(this, "Deseja Trocar de Usuário?", "Aviso", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (getResult == DialogResult.Yes)
            {
            }
        }

        public void carregaOcorrencias()
        {
            try
            {
                this.sv = new ServiceMoradaDaPaz();

                if (usuarioAtivo.Id_especializacao_usuario.Id == 1)
                {
                    this.ocorrenciaLista = sv.listarOcorrencias();
                }else
                {
                    this.ocorrenciaLista = sv.listarOcorrenciasPorUsuario(usuarioAtivo);
                }
                
                listViewMinhasOcorrencias.Items.Clear();
                for (int index = 0; index < ocorrenciaLista.Count; index++)
                {

                    ListViewItem linha = listViewMinhasOcorrencias.Items.Add(ocorrenciaLista.ElementAt(index).Numero_ocorrencia);
                    linha.SubItems.Add(ocorrenciaLista.ElementAt(index).Descricao);
                    linha.SubItems.Add(ocorrenciaLista.ElementAt(index).Situacao);
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private void PrincipalWindow_Load(object sender, EventArgs e)
        {
            this.carregaOcorrencias();
        }

        private void buttonAtulizar_Click(object sender, EventArgs e)
        {
            this.carregaOcorrencias();

            if (usuarioAtivo.Id_especializacao_usuario.Id == 1)
            {
                try
                {
                    binaryWriter.Write("Ocorrencia Visualizada!");
                }
                catch (SocketException socketEx)
                {
                    MessageBox.Show(socketEx.Message, "Erro");
                }
                catch (Exception socketEx)
                {
                    MessageBox.Show(socketEx.Message, "Erro");
                }
            }
                
        }

        #region codigos de manipulação de XML
        public void criarArquivo()
        {
            try
            {
                if (File.Exists(this.caminho) == false)
                {
                    XmlDocument doc = new XmlDocument();
                    XmlNode raiz = doc.CreateElement("minhasOcorrencias");
                    doc.AppendChild(raiz);
                    doc.Save(this.caminho);
                }
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        #endregion

        #region add nova linha
        public void novaLinha(ocorrencia o)
        {
            try
            {
                #region abrir aquivo
                XmlDocument doc = new XmlDocument();
                doc.Load(this.caminho);
                #endregion

                #region definição dos elementos do xml
                XmlNode ocorrencia = doc.CreateElement("ocorrencia");
                XmlNode numero = doc.CreateElement("numero");
                XmlNode descricao = doc.CreateElement("descricao");
                XmlNode status = doc.CreateElement("status");
                #endregion

                

                
                #region colocar valores nos elementos xml
                numero.InnerText = o.Numero_ocorrencia;
                descricao.InnerText = o.Descricao;
                status.InnerText = o.Situacao;
                #endregion

                #region definido hierarquia
                ocorrencia.AppendChild(numero);
                ocorrencia.AppendChild(descricao);
                ocorrencia.AppendChild(status);
                #endregion
                

                #region adicionando ao elemento raiz
                doc.SelectSingleNode("/minhasOcorrencias").AppendChild(ocorrencia);
                doc.Save(this.caminho);
                #endregion

                


            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
        #endregion

        private void buttonGeraXml_Click(object sender, EventArgs e)
        {
            #region instancia do webservice e criação da lista de ocorrencia
            ServiceMoradaDaPaz sv = new ServiceMoradaDaPaz();
            List<ocorrencia> ocorrenciaLista = sv.listarOcorrenciasPorUsuario(usuarioAtivo);
            #endregion
            #region loop para preenchimento do XML
            for (int x = 0; x < ocorrenciaLista.Count; x++)
            {
                this.criarArquivo();
                this.novaLinha(ocorrenciaLista.ElementAt(x));
            }
            #endregion

            MessageBox.Show("Documento salvo em " + this.caminho);
            Process.Start("Explorer", @"C:\xml");
        }

        private void listViewMinhasOcorrencias_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            
            if(usuarioAtivo.Id_especializacao_usuario.Id == 1)
            {
                int indexListView = listViewMinhasOcorrencias.FocusedItem.Index;
                ocorrencia oc = this.ocorrenciaLista.ElementAt(indexListView);
                EditOcorrenciaWindow eo = new EditOcorrenciaWindow(oc);
                eo.ShowDialog();
            }
        }
            

        private void verificaUsuario(usuario u)
        {
            
            //muralDeOcorrenciasPublicasToolStripMenuItem.Enabled = false;
            
            if(u.Id_especializacao_usuario.Id > 1)
            {
                menuStrip1.Items[1].Visible = false;
                menuStrip1.Items[2].Visible = false;
            }
        }

        private void mostraMensagem(object oo)
        {
            if (usuarioAtivo.Id_especializacao_usuario.Id == 1)
            {
                Invoke(new MethodInvoker(
                         delegate { MessageBox.Show("" + oo); }
                                         ));
            }
                
        }

        public void RunServidor()
        {
            if (usuarioAtivo.Id_especializacao_usuario.Id == 1)
            {
                try
                {
                    IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2001);
                    tcpListener = new TcpListener(ipEndPoint);
                    tcpListener.Start();

                    //mostraMensagem("Servidor habilitado e escutando porta..." + "Server App");

                    socket = tcpListener.AcceptSocket();
                    networkStream = new NetworkStream(socket);
                    binaryWriter = new BinaryWriter(networkStream);
                    binaryReader = new BinaryReader(networkStream);

                    //AddToListBox("");
                    //binaryWriter.Write("\nOcorrência Recebida! (Server App)");

                    string messageReceived = "";
                    do
                    {
                        try
                        {
                            messageReceived = binaryReader.ReadString();

                            mostraMensagem("" + messageReceived);
                        }catch(Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                        

                    } while (socket.Connected);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    if (binaryReader != null)
                    {
                        binaryReader.Close();
                    }
                    if (binaryWriter != null)
                    {
                        binaryWriter.Close();
                    }
                    if (networkStream != null)
                    {
                        networkStream.Close();
                    }
                    if (socket != null)
                    {
                        socket.Close();
                    }
                    MessageBox.Show("conexão finalizada", "Server App");

                }
            }
                
        }
        public void runCliente()
        {
            try
            {
                tcpClient = new TcpClient();
                //conectando ao servidor
                tcpClient.Connect("127.0.0.1", 2001);

                networkStream = tcpClient.GetStream();
                binaryWriter = new BinaryWriter(networkStream);
                binaryReader = new BinaryReader(networkStream);
                //binaryWriter.Write("Uma nova ocorrência foi adcionada!\n\nAtualize a Lista!");
                String message = "";

                #region laço para receber mensagem do servidor
                do
                {
                    try
                    {
                        message = binaryReader.ReadString();
                        Invoke(new MethodInvoker(
                          delegate { MessageBox.Show("(Cliente App)" + message); }
                          ));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Erro");
                        message = "FIM";
                    }
                } while (message != "FIM");
                #endregion

                binaryWriter.Close();
                binaryReader.Close();
                networkStream.Close();
                tcpClient.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro");
            }
        }

    }

}

