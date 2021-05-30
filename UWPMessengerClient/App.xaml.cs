using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Notifications;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI;
using UWPMessengerClient.MSNP;
using Microsoft.QueryStringDotNET;
using Windows.ApplicationModel.Background;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UWPMessengerClient
{
    /// <resumo>
    ///Fornece o comportamento específico do aplicativo para complementar a classe Application padrão.
    /// </summary>
    sealed partial class App : Application
    {
        public NotificationServerConnection NotificationServerConnection { get; set; }

        /// <summary>
        /// Inicializa o objeto singleton do aplicativo. Essa é a primeira linha do código criado
        /// executado e, por isso, é o equivalente lógico de main() ou WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            Task task = DatabaseAccess.InitializeDatabase();
        }

        /// <summary>
        /// Invocado quando o aplicativo é iniciado normalmente pelo usuário final. Outros pontos de entrada
        /// serão usados, por exemplo, quando o aplicativo for iniciado para abrir um arquivo específico.
        /// </summary>
        /// <param name="e">Detalhes sobre a solicitação e o processo de inicialização.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            await OnLaunchedOrActived(e);
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            await OnLaunchedOrActived(args);
        }

        private async Task OnLaunchedOrActived(IActivatedEventArgs e)
        {
            await RegisterBackgroundTask();
            Frame rootFrame = Window.Current.Content as Frame;

            // Não repita a inicialização do aplicativo quando a Janela já tiver conteúdo,
            // apenas verifique se a janela está ativa
            if (rootFrame == null)
            {
                // Crie um Quadro para atuar como o contexto de navegação e navegue para a primeira página
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Carregue o estado do aplicativo suspenso anteriormente
                }

                // Coloque o quadro na Janela atual
                Window.Current.Content = rootFrame;
            }

            if (e is LaunchActivatedEventArgs)
            {
                var args = e as LaunchActivatedEventArgs;
                if (args.PrelaunchActivated == false)
                {
                    if (rootFrame.Content == null)
                    {
                        // Quando a pilha de navegação não for restaurada, navegar para a primeira página,
                        // configurando a nova página passando as informações necessárias como um parâmetro
                        // de navegação
                        rootFrame.Navigate(typeof(LoginPage), args.Arguments);
                    }
                    // Verifique se a janela atual está ativa
                    Window.Current.Activate();
                    ExtendAcrylicIntoTitleBar();
                }
            }
            else if (e is ToastNotificationActivatedEventArgs)
            {
                var args = e as ToastNotificationActivatedEventArgs;
                if (rootFrame.Content == null)
                {
                    // Quando a pilha de navegação não for restaurada, navegar para a primeira página,
                    // configurando a nova página passando as informações necessárias como um parâmetro
                    // de navegação
                    rootFrame.Navigate(typeof(LoginPage));
                    // Verifique se a janela atual está ativa
                    Window.Current.Activate();
                    ExtendAcrylicIntoTitleBar();
                }
                else
                {
                    ToastNotificationHistory notificationHistory = ToastNotificationManager.History;
                    QueryString arguments = QueryString.Parse(args.Argument);
                    switch (arguments["action"])
                    {
                        case "newMessage":
                            notificationHistory.RemoveGroup("messages");
                            if (rootFrame.Content is ChatPage && (rootFrame.Content as ChatPage).ConversationId.Equals(arguments["conversationId"]))
                            {
                                break;
                            }
                            _ = rootFrame.Navigate(typeof(ChatPage), new ChatPageNavigationParameters()
                            {
                                NotificationServerConnection = NotificationServerConnection,
                                SbConversationId = arguments["conversationId"]
                            });
                            break;
                    }
                }
            }
        }

        private async Task RegisterBackgroundTask()
        {
            const string taskName = "ToastBackgroundTask";
            if (BackgroundTaskRegistration.AllTasks.Any(i => i.Value.Name.Equals(taskName)))
                return;
            BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
            BackgroundTaskBuilder builder = new BackgroundTaskBuilder()
            {
                Name = taskName
            };
            builder.SetTrigger(new ToastNotificationActionTrigger());
            BackgroundTaskRegistration registration = builder.Register();
        }

        protected override async void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            var deferral = args.TaskInstance.GetDeferral();
            switch (args.TaskInstance.Task.Name)
            {
                case "ToastBackgroundTask":
                    var details = args.TaskInstance.TriggerDetails as ToastNotificationActionTriggerDetail;
                    if (details != null)
                    {
                        QueryString arguments = QueryString.Parse(details.Argument);
                        var userInput = details.UserInput;
                        ToastNotificationHistory notificationHistory = ToastNotificationManager.History;
                        switch (arguments["action"])
                        {
                            case "DismissMessages":
                                notificationHistory.RemoveGroup("messages");
                                break;
                            case "ReplyMessage":
                                SBConversation conversation = NotificationServerConnection.ReturnConversationFromConversationId(arguments["conversationId"]);
                                string reply = (string)userInput["ReplyBox"];
                                await conversation.SendTextMessage(reply);
                                break;
                            case "acceptContact":
                                Contact contact = JsonConvert.DeserializeObject<Contact>(arguments["contact"]);
                                await NotificationServerConnection.AcceptNewContact(contact);
                                break;
                        }
                    }
                    break;
            }
            deferral.Complete();
        }

        /// <summary>
        /// Chamado quando ocorre uma falha na Navegação para uma determinada página
        /// </summary>
        /// <param name="sender">O Quadro com navegação com falha</param>
        /// <param name="e">Detalhes sobre a falha na navegação</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invocado quando a execução do aplicativo é suspensa. O estado do aplicativo é salvo
        /// sem saber se o aplicativo será encerrado ou retomado com o conteúdo
        /// da memória ainda intacto.
        /// </summary>
        /// <param name="sender">A origem da solicitação de suspensão.</param>
        /// <param name="e">Detalhes sobre a solicitação de suspensão.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Salvar o estado do aplicativo e parar qualquer atividade em segundo plano
            deferral.Complete();
        }

        private void ExtendAcrylicIntoTitleBar()
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }
    }
}
