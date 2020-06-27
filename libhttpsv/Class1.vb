Imports System.Net
Imports System.Text
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

    Private Class Hostx
        Public Sub Start()

        End Sub
    End Class

    Event ReceivedNewRequest As Eventos.ReceivedNewRequest
    Event ReceivedNewParameter As Eventos.ReceivedNewParameter
    Event ChangeStatusServer As Eventos.ChangeStatusServer

    ''' <summary>
    ''' No Context você pode adicionar códigos HTML, CSS e JavaScript.
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
    ''' Aqui você pode pegar os parâmetros de uma URL, por exemplo: no acesso http://localhost:8030/?download=ArquivoPDF, ao digitar GetParameter("download"), o valor 'ArquivoPDF' será retornado.
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
    ''' Ligar servidor HTTP em localhost com uma porta definido.
    ''' </summary>
    Public Sub HostStart(ByVal Port As Integer)
        CWrite("Iniciando servidor...")
        tStatus = StatusServer.ServerLoading
        RaiseEvent ChangeStatusServer(tStatus)
        listener.Prefixes.Add("http://localhost:" & Port & "/")
        listener.Start()
        CWrite("Servidor iniciado com sucesso!")
        tStatus = StatusServer.ServerOn
        RaiseEvent ChangeStatusServer(tStatus)
        listener.BeginGetContext(AddressOf RequestHandler, Interlocked.Increment(0))
    End Sub

    ''' <summary>
    ''' Desligar servidor HTTP.
    ''' </summary>
    Public Sub HostStop()
        CWrite("Parando servidor...")
        tStatus = StatusServer.ServerLoading
        RaiseEvent ChangeStatusServer(tStatus)
        listener.Stop()
        listener.Abort()
        CWrite("Servidor desligado com sucesso!")
        tStatus = StatusServer.ServerOff
        RaiseEvent ChangeStatusServer(tStatus)
    End Sub

    Private Sub RequestHandler(ByVal result As IAsyncResult)
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

        If Not Context = Nothing Then
            Dim response As HttpListenerResponse = EndGetContext.Response
            Dim buffer() As Byte = ContextEncoding.GetBytes(Context)
            response.OutputStream.Write(buffer, 0, buffer.Length)
            response.Close()
        End If
        RaiseEvent ReceivedNewRequest(EndGetContext.Request)
        If Status = StatusServer.ServerOn Then
            listener.BeginGetContext(AddressOf RequestHandler, Interlocked.Increment(0))
        End If
    End Sub

    Private Sub CWrite(ByVal text As String)
        If tshowconsole Then
            Console.ForegroundColor = ConsoleColor.Green
            Console.WriteLine("[HTTPServer] " & text)
        End If
    End Sub
End Class