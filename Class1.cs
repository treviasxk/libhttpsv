using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace libhttpsv{
    public partial class Eventos: EventArgs{
        public delegate void ReceivedNewRequest(HttpListenerRequest Request);
        public delegate void ReceivedNewParameter(string Key, string Value);
        public delegate void ChangeStatusServer(StatusServer Status);
    }

    public enum StatusServer : int{
        ServerOff = 0,
        ServerOn = 1,
        ServerLoading = 2
    }

    public class HTTPServer{
        private Dictionary<int, string> RequestPages = new Dictionary<int, string>(){
            {0, "<html><head><title>Libhttpsv</title><meta charset='UTF8'/></head><body><h1>Libhttpsv</h1>"},
            {1, "<hr/>Libhttpsv 1.0.3.0 - Criado por Trevias Xk</body></html>"},
            {404, "<html><head><title>404 Não encontrado</title><meta charset='UTF8'/></head><body><h1>Não encontrado</h1><p>A url solicitado não foi entrado nesse servidor.</p><hr/>Libhttpsv 1.0.3.0 - Criado por Trevias Xk</body></html>"}
        };
        private HttpListener listener = new HttpListener();
        private int tStatus;
        private bool tshowconsole;
        private bool tlockconsole;
        private HttpListenerContext EndGetContext = default;
        private Dictionary<string, string> Paraments = new Dictionary<string, string>();
        private string tContextFolder;
        private List<string> ContextFiles { get; set; } = new List<string>();
        public event Eventos.ReceivedNewRequest ReceivedNewRequest;
        public event Eventos.ReceivedNewParameter ReceivedNewParameter;
        public event Eventos.ChangeStatusServer ChangeStatusServer;

        /// <summary>
        /// No 'ContextFolder' você pode adicionar a localização de um directório, se existir o arquivo index.html ele será adicionado no Context para a localização raiz do dominio.
        /// </summary>
        public string ContextFolder{get {return tContextFolder;} set {ContextFiles.Clear(); ContextFiles.AddRange(Directory.GetFiles(value, "*.*", SearchOption.AllDirectories)); tContextFolder = value;}
        }
        /// <summary>
        /// O 'Context' irá mostrar a página inicial do site, você pode adicionar códigos HTML, CSS e JavaScript, essa váriavel não vai funcionar se o 'ContextFolder' tiver um valor.
        /// </summary>
        public string Context { get; set; }
        /// <summary>
        /// O ContextEncoding irá codificar a String definido na variável Context, se não definir um encoding, por padrão a codificação será UTF8.
        /// </summary>
        public Encoding ContextEncoding { get; set; } = Encoding.UTF8;
        /// <summary>
        /// Ao definir valor True nessa variável e caso seu software esteja sendo executado por aplicativo console, você verá todos os logs do HTTPServer.
        /// </summary>
        public bool ProcessLogs{get {return tshowconsole;} set {tshowconsole = value;}}
        /// <summary>
        /// Ao definir valor True, o console não será finalizado ao término do código.
        /// </summary>
        public bool ConsoleLock{get {return tlockconsole;} set {tlockconsole = value;}}
        /// <summary>
        /// Aqui você pode pegar os parâmetros de uma URL, por exemplo: no acesso http://127.0.0.1:8030/?download=ArquivoPDF, ao digitar GetParameter("download"), o valor 'ArquivoPDF' será retornado.
        /// </summary>
        public string GetParameter(string Key){
            if (Paraments.ContainsKey(Key)){
                Key = Paraments[Key];
            }
            else {
                Key = null;
            }
            return WebUtility.UrlDecode(Key);
        }

        public HttpListenerRequest Request{get {return EndGetContext.Request;}}

        public HttpListenerResponse Response{get {return EndGetContext.Response;}}

        public System.Security.Principal.IPrincipal User{get {return EndGetContext.User;}}

        /// <summary>
        /// Status do servidor HTTP.
        /// </summary>
        public StatusServer Status{get {return (StatusServer)tStatus;}}

        /// <summary>
        /// HostStart irá ligar o servidor HTTP em um IP e Porta definido na sua rede local.
        /// </summary>
        public void HostStart(int Port, string IP){
            IP = IPAddress.Parse(IP).ToString();
            if (listener.IsListening){
                CWrite("O servidor já está ligado!");
            }
            else {
                try {
                    CWrite("Iniciando servidor...");
                    tStatus = (int)StatusServer.ServerLoading;
                    ChangeStatusServer?.Invoke((StatusServer)tStatus);
                    listener.Prefixes.Add("http://" + IP + ":" + Port + "/");
                    listener.Start();
                    CWrite("Servidor iniciado com sucesso em: http://" + IP + ":" + Port + "/");
                    tStatus = (int)StatusServer.ServerOn;
                    ChangeStatusServer?.Invoke((StatusServer)tStatus);
                    int arglocation = 0;
                    listener.BeginGetContext(RequestHandler, Interlocked.Increment(ref arglocation));
                } catch(UnauthorizedAccessException ex){
                    CWrite("Erro: " + ex.ToString());
                }
            }
            LockConsole();
        }

        private void LockConsole(){
            if(ConsoleLock){
                Console.ReadKey(true);
                LockConsole();
            }
        }

        /// <summary>
        /// HostRestart irá reiniciar o servidor HTTP, é recomendado utilizar esse comando caso o servidor falhe. (Beta)
        /// </summary>

        public void HostRestart(){
            CWrite("Reiniciando servidor...");
            HostStop();
            HostStart();
        }

        /// <summary>
        /// HostStop irá desligar o servidor HTTP, porém comando só irá funcionar se o servidor estiver inicializado por completo.
        /// </summary>
        public void HostStop(){
            if (listener.IsListening){
                CWrite("Parando servidor...");
                tStatus = (int)StatusServer.ServerLoading;
                ChangeStatusServer?.Invoke((StatusServer)tStatus);
                listener.Stop();
                listener.Abort();
                CWrite("Servidor desligado com sucesso!");
                tStatus = (int)StatusServer.ServerOff;
                ChangeStatusServer?.Invoke((StatusServer)tStatus);
            }
            else {
                CWrite("O servidor já está parado!");
            }
        }

        private void RequestHandler(IAsyncResult result){
            try {
                EndGetContext = listener.EndGetContext(result);
                CWrite("Nova acesso solicitado.   (URL: " + EndGetContext.Request.Url.ToString() + ")");
                Paraments.Clear();
                var matches = Regex.Matches(EndGetContext.Request.RawUrl, @"(\?|\&)([^=]+)\=([^&]+)");
                foreach (Match m in matches){
                    if (!Paraments.ContainsKey(m.Groups[2].Value)){
                        CWrite("Novo parâmetro recebido.  (KEY: " + m.Groups[2].Value + ", VALUE: " + m.Groups[3].Value + ")");
                        Paraments.Add(m.Groups[2].Value, m.Groups[3].Value);
                        ReceivedNewParameter?.Invoke(m.Groups[2].Value, m.Groups[3].Value);
                    }
                }



                if (Directory.Exists(ContextFolder)){
                    if(File.Exists(ContextFolder + Request.Url.AbsolutePath + @"\index.html")){
                        byte[] buffer = ContextEncoding.GetBytes(File.ReadAllText(ContextFolder + Request.Url.AbsolutePath + @"\index.html"));
                        Response.OutputStream.Write(buffer, 0, buffer.Length);
                        Response.Close();
                    }
                    else {
                        var url = new Uri(ContextFolder + Request.Url.AbsolutePath);
                        if (File.Exists(url.LocalPath)){
                            var FileStream = File.OpenRead(url.LocalPath);
                            EndGetContext.Response.ContentLength64 = FileStream.Length;
                            FileStream.CopyTo(Response.OutputStream);
                            Response.Close();
                        }
                        else {
                            if(Directory.Exists(ContextFolder + Request.Url.AbsolutePath)){
                                Context = RequestPages[0] + "<p>Lista de arquivos disponíveis nesse diretório.</p><hr/>";
                                for (int i = 0, loopTo = ContextFiles.Count - 1; i <= loopTo; i++){
                                    if(ContextFiles[i].Replace(ContextFolder, "").StartsWith(Request.Url.AbsolutePath.Replace("/",@"\"))){
                                        Context += "<a href='" + ContextFiles[i].Replace(ContextFolder, "") + "'>" + ContextFiles[i].Replace(ContextFolder, "") + "</a></br>";
                                    }
                                }
                                Context += RequestPages[1];
                                byte[] buffer = ContextEncoding.GetBytes(Context);
                                Response.OutputStream.Write(buffer, 0, buffer.Length);
                                Response.Close();
                            }else{
                                byte[] buffer = ContextEncoding.GetBytes(RequestPages[404]);
                                Response.OutputStream.Write(buffer, 0, buffer.Length);
                                Response.Close();
                            }
                        }
                    }
                }
                else if (Context != default){
                    byte[] buffer = ContextEncoding.GetBytes(Context);
                    Response.OutputStream.Write(buffer, 0, buffer.Length);
                    Response.Close();
                }




                ReceivedNewRequest?.Invoke(EndGetContext.Request);
                if (Status == StatusServer.ServerOn){
                    int arglocation = 0;
                    listener.BeginGetContext(RequestHandler, Interlocked.Increment(ref arglocation));
                }
            }
            catch (Exception)
            {
            }
        }

        private void CWrite(string text){
            if (tshowconsole){
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[HTTPServer] " + text);
            }
        }
    }
}
