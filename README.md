# Libhttpsv
Libhttpsv é uma extensão de aplicativo (.dll) para windows, com projeto open-source no GitHub e programado na linguagem C#, com ele o usuário poderá implementar um servidor localhost em seu software de windows, essa extensão é perfeito para criar controle remoto e trocar informações através apenas de conexões http.
### Como utilizar em seu software
Primeiro baixe a dll ou o projeto que estiver mais atualizado na página de lançamentos do libhttpsv no GitHub. https://github.com/treviasxk/libhttpsv/releases, depois adiciona como referência no seu projeto.
***
## DOCUMENTAÇÃO
### AÇÕES
* HostStart(Integer) - HostStart irá ligar o servidor HTTP em um IP e Porta definido na sua rede local.
* HostStop() - HostStop irá desligar o servidor HTTP.
### EVENTOS
* ReceivedNewRequest(HttpListenerRequest) - A cada acesso solicitado pelo o usuário no localhost.
* ReceivedNewParameter(String, String) - A cada acesso solicitado com uma request no dominio do localhost (exemplo: localhost:8030/?page=download).
* ChangeStatusServer(StatusServer) - Sempre que o servidor for ligado ou desligado.
### VARIÁVEIS
* ConsoleLock - Ao definir valor True, o console não será finalizado ao término do código.
* Context - No Context você pode adicionar códigos HTML, CSS e JavaScript.
* ContextEncoding - O ContextEncoding irá codificar a String definido na variável Context, se não definir um encoding, por padrão a codificação será UTF8.
* ContextFolder - No 'ContextFolder' você pode adicionar a localização de um directório, se existir o arquivo index.html ele será adicionado no Context para a localização raiz do dominio.
* GetParameter(String) - Aqui você pode pegar os parâmetros de uma URL, por exemplo: no acesso http://127.0.0.1:8030/?download=ArquivoPDF, ao digitar GetParameter("download"), o valor 'ArquivoPDF' será retornado.
* ProcessLogs - Ao definir valor True nessa variável e caso seu software esteja sendo executado por aplicativo console, você verá todos os logs do HTTPServer.
* Request - [System.Net.HttpListenerRequest](https://docs.microsoft.com/pt-br/dotnet/api/system.net.httplistenerrequest?view=netcore-3.1)
* Response - [System.Net.HttpListenerResponse](https://docs.microsoft.com/en-us/dotnet/api/system.net.httplistenerresponse?view=netcore-3.1)
* Status - Status do servidor HTTP.
* User - [System.Security.Principal.IPrincipal](https://docs.microsoft.com/en-us/dotnet/api/system.security.principal.iprincipal?view=netcore-3.1)
