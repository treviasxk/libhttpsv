Imports System.Net
Imports System.Text
Imports System.IO
Imports System.Threading
Imports System.Text.RegularExpressions


Public Class Eventos
    Delegate Sub ReceivedNewRequest(ByVal Request As HttpListenerRequest)
    Delegate Sub ReceivedNewParameter(ByVal Key As String, Value As String)
    Delegate Sub ChangeStatusServer(ByVal Status As StatusServer)
End Class

Public Enum StatusServer As Integer
    ServerOff = 0
    ServerOn = 1
    ServerLoading = 2
End Enum

Public Class HTTPServer
    Private listener As New HttpListener
    Private tStatus As Integer
    Private tshowconsole As Boolean
    Private EndGetContext As HttpListenerContext = Nothing
    Private Paraments As New Dictionary(Of String, String)
    Private tContextFolder As String
    Private Property ContextFiles As New List(Of String)

    Event ReceivedNewRequest As Eventos.ReceivedNewRequest
    Event ReceivedNewParameter As Eventos.ReceivedNewParameter
    Event ChangeStatusServer As Eventos.ChangeStatusServer

    ''' <summary>
    ''' No 'ContextFolder' você pode adicionar a localização de um directório, se existir o arquivo index.html ele será adicionado no Context para a localização raiz do dominio.
    ''' </summary>
    Public Property ContextFolder As String
        Get
            Return tContextFolder
        End Get
        Set(value As String)
            ContextFiles.Clear()
            ContextFiles.AddRange(Directory.GetFiles(value, "*.*", SearchOption.AllDirectories))
            tContextFolder = value
        End Set
    End Property
    ''' <summary>
    ''' O 'Context' irá mostrar a página inicial do site, você pode adicionar códigos HTML, CSS e JavaScript, essa váriavel não vai funcionar se o 'ContextFolder' tiver um valor.
    ''' </summary>
    Public Property Context As String
    ''' <summary>
    ''' O ContextEncoding irá codificar a String definido na variável Context, se não definir um encoding, por padrão a codificação será UTF8.
    ''' </summary>
    Public Property ContextEncoding As Encoding = Encoding.UTF8
    ''' <summary>
    ''' Ao definir valor True nessa variável e caso seu software esteja sendo executado por aplicativo console, você verá todos os logs do HTTPServer.
    ''' </summary>
    Public Property ProcessLogs As Boolean
        Get
            Return tshowconsole
        End Get
        Set(value As Boolean)
            tshowconsole = value
        End Set
    End Property
    ''' <summary>
    ''' Aqui você pode pegar os parâmetros de uma URL, por exemplo: no acesso http://127.0.0.1:8030/?download=ArquivoPDF, ao digitar GetParameter("download"), o valor 'ArquivoPDF' será retornado.
    ''' </summary>
    Public ReadOnly Property GetParameter(ByVal Key As String)
        Get
            If Paraments.ContainsKey(Key) Then
                Key = Paraments(Key)
            Else
                Key = Nothing
            End If
            Return WebUtility.UrlDecode(Key)
        End Get
    End Property

    Public ReadOnly Property Request As HttpListenerRequest
        Get
            Return EndGetContext.Request
        End Get
    End Property

    Public ReadOnly Property Response As HttpListenerResponse
        Get
            Return EndGetContext.Response
        End Get
    End Property

    Public ReadOnly Property User As System.Security.Principal.IPrincipal
        Get
            Return EndGetContext.User
        End Get
    End Property

    ''' <summary>
    ''' Status do servidor HTTP.
    ''' </summary>
    Public ReadOnly Property Status As StatusServer
        Get
            Return tStatus
        End Get
    End Property

    ''' <summary>
    ''' HostStart irá ligar o servidor HTTP em um IP Local com uma porta definido.
    ''' </summary>
    Public Sub HostStart(ByVal Port As Integer)
        If listener.IsListening Then
            CWrite("O servidor já está ligado!")
        Else
            CWrite("Iniciando servidor...")
            tStatus = StatusServer.ServerLoading
            RaiseEvent ChangeStatusServer(tStatus)
            Dim strHostName = System.Net.Dns.GetHostName()
            Dim strIPAddress = System.Net.Dns.GetHostByName(strHostName).AddressList(0).ToString()
            listener.Prefixes.Add("http://" & strIPAddress & ":" & Port & "/")
            listener.Start()
            CWrite("Servidor iniciado com sucesso em: http://" & strIPAddress & ":" & Port & "/")
            tStatus = StatusServer.ServerOn
            RaiseEvent ChangeStatusServer(tStatus)
            listener.BeginGetContext(AddressOf RequestHandler, Interlocked.Increment(0))
        End If
    End Sub

    ''' <summary>
    ''' HostStop irá desligar o servidor HTTP.
    ''' </summary>
    Public Sub HostStop()
        If listener.IsListening Then
            CWrite("Parando servidor...")
            tStatus = StatusServer.ServerLoading
            RaiseEvent ChangeStatusServer(tStatus)
            listener.Stop()
            listener.Abort()
            CWrite("Servidor desligado com sucesso!")
            tStatus = StatusServer.ServerOff
            RaiseEvent ChangeStatusServer(tStatus)
        Else
            CWrite("O servidor já está parado!")
        End If
    End Sub

    Private Sub RequestHandler(ByVal result As IAsyncResult)
        Try
            EndGetContext = listener.EndGetContext(result)
            CWrite("Nova acesso solicitado.   (URL: " & EndGetContext.Request.Url.ToString & ")")
            Paraments.Clear()

            Dim matches As MatchCollection = Regex.Matches(EndGetContext.Request.RawUrl, "(\?|\&)([^=]+)\=([^&]+)")
            For Each m As Match In matches
                If Not Paraments.ContainsKey(m.Groups(2).Value) Then
                    CWrite("Novo parâmetro recebido.  (KEY: " & m.Groups(2).Value & ", VALUE: " & m.Groups(3).Value & ")")
                    Paraments.Add(m.Groups(2).Value, m.Groups(3).Value)
                    RaiseEvent ReceivedNewParameter(m.Groups(2).Value, m.Groups(3).Value)
                End If
            Next

            Dim t As New Thread(Sub() Connection())
            t.Start()

            RaiseEvent ReceivedNewRequest(EndGetContext.Request)
            If Status = StatusServer.ServerOn Then
                listener.BeginGetContext(AddressOf RequestHandler, Interlocked.Increment(0))
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Sub Connection()
        If Directory.Exists(ContextFolder) Then
            If File.Exists(ContextFolder + "\index.html") And Request.Url.AbsolutePath = "/" Then
                Dim buffer() As Byte = ContextEncoding.GetBytes(File.ReadAllText(ContextFolder + "\index.html"))
                Response.OutputStream.Write(buffer, 0, buffer.Length)
                Response.Close()
            Else
                Context = Nothing
                Dim url As New Uri(ContextFolder + Request.Url.AbsolutePath)
                If File.Exists(url.LocalPath) Then
                    Try
                        Dim FileStream = File.OpenRead(url.LocalPath)
                        EndGetContext.Response.ContentLength64 = FileStream.Length
                        FileStream.CopyTo(Response.OutputStream)
                        Response.Close()
                    Catch ex As Exception

                    End Try
                Else
                    For i As Integer = 0 To ContextFiles.Count - 1
                        Context += "<a href='" & ContextFiles(i).Replace(ContextFolder, "") & "'>" & ContextFiles(i).Replace(ContextFolder, "") & "</a></br>"
                    Next i
                    Dim buffer() As Byte = ContextEncoding.GetBytes(Context)
                    Response.OutputStream.Write(buffer, 0, buffer.Length)
                    Response.Close()
                End If
            End If
        Else
            If Not Context = Nothing Then
                Dim buffer() As Byte = ContextEncoding.GetBytes(Context)
                Response.OutputStream.Write(buffer, 0, buffer.Length)
                Response.Close()
            End If
        End If
    End Sub

    Private Sub CWrite(ByVal text As String)
        If tshowconsole Then
            Console.ForegroundColor = ConsoleColor.Green
            Console.WriteLine("[HTTPServer] " & text)
        End If
    End Sub
End Class