using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Passwords.API.Abstracts;
using Passwords.API.Models;
using Passwords.API.Xaml;
using Yps;
#if DEBUG
using Consola;
#endif
using System.Net.Http;
using System.Text.Json;
using System.Web;
using Microsoft.Win32;
using System.ComponentModel;

namespace Passwords.GUI
{

    public enum MainPanel : int
    {
        EmptyView = 0, EnterPassword = 1, UserLocations = 2, ServerFeatures = 4
    }


    public enum SidePanel : int
    {
        EmptyView = 0, ListedServers = 1<<8, UserLocations = 2<<8
    }

    [Flags]
    public enum ToolPanel : int
    {
        ServerTools = 0x01<<16, LocationTools = 0x02<<16, UserSelection = 0x04<<16
    }

    [Flags]
    public enum FullState : int
    {
        NoState = 0,
        EnterPassword = 0x01, 
        UserLocations = 0x02,
        ServerFeatures = 0x04,
        ListedServers = 0x01<<8,
        SideLocations = UserLocations<<8,
        ServerTools   = 0x01<<16,
        LocationTools = 0x02<<16,
        UserSelection = 0x04<<16
    }

    public class GuiState
    {
        private ThePasswords_TheAPI_TheGUI instance;

        private List<PasswordUsers> accounts;
        private UserLocations[]     locations;
        private PasswordUsers       user;
        private UserLocations       area;
        private CryptKey            key;
        private ToolPanel           tool;

        public ToolPanel            ToolPanel {
            get { return tool; }
            set { if( value != tool ) {
                    if ( value.HasFlag( ToolPanel.UserSelection ) ) {
                        if( tool.HasFlag( ToolPanel.UserSelection ) ) {
                            tool &= ~ToolPanel.UserSelection;
                            instance.bar_UsersSelect.Visibility = Visibility.Collapsed;   
                        } else {
                            tool |= ToolPanel.UserSelection;
                            instance.bar_UsersSelect.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
        }

        public MainPanel            MainPanel {
            get { return instance.MainPanel; }
            set { instance.MainPanel = value; }
        }
        public SidePanel            SidePanel {
            get { return instance.SidePanel; }
            set { instance.SidePanel = value; }
        }

        public FullState            FullState {
            get { return (FullState)( (int)tool | (int)SidePanel | (int)MainPanel ); }
            set { SidePanel = (SidePanel)((int)value & 0x0000ff00); 
                  MainPanel = (MainPanel)((int)value & 0x000000ff);
                  ToolPanel = (ToolPanel)((int)value & 0x00ff0000); }
        }

        public int SelectedUser { get { return user?.Id ?? -1; } }
        public string SelectedMail { get { return user?.Mail ?? string.Empty; } }
        public UserLocations SelectedArea { get { return area; } }
        public string UserName { get { return user?.Name ?? string.Empty; } }
        public int AreasLoaded { get { return locations?.Length ?? 0; } }
        public string LocationName { get { return area?.Area ?? string.Empty; } }

        public string EncryptedArgs( string cleartext )
        {
            return HttpUtility.UrlEncode(
                Crypt.EncryptW( key ?? PasswordClient.Registry.Key, Encoding.Default.GetBytes( cleartext ) )
            );
        }

        public string DecryptedValue( string cryptex )
        {
            return key?.Decrypt( cryptex ) ?? PasswordClient.Registry.Key.Decrypt( cryptex );
        }

        public string DecryptedValue( byte[] crypdat )
        {
            return Encoding.Default.GetString( Crypt.DecryptA<byte>(key ?? PasswordClient.Registry.Key, crypdat) );
        }

        public GuiState( ThePasswords_TheAPI_TheGUI application )
        {
            instance = application;
            tool = ToolPanel.ServerTools|ToolPanel.LocationTools;
            MainPanel = MainPanel.EmptyView;
            SidePanel = SidePanel.EmptyView;
        }

        public int UsersLoaded { get { return accounts?.Count ?? 0; } }

        public bool SetAccounts( List<PasswordUsers> loaded )
        {
            if( accounts != loaded ) {
                instance.cmb_Users.Items.Clear();
                accounts = loaded;
                if( UsersLoaded > 0 ) {
                    for( int i = 0; i < UsersLoaded; ++i )
                        instance.cmb_Users.Items.Add( accounts[i].Name );
                } return true;
            } else return false;
        }

        public bool SetLocations( UserLocations[] resetAreas )
        {
            if( locations != resetAreas ) {
                locations = resetAreas;
                return true;
            } return false;
        }

        public void SetUser( int usrindex, string password )
        {
            PasswordUsers select = user;
            if( password.Length > 0 ) {
                key = Crypt.CreateKey( password );
                select = accounts[usrindex];
            } else select = PasswordUsers.Invalid;
            
            if( user != select ) {
                user = select;
                SidePanel = SidePanel.EmptyView;
                MainPanel = MainPanel.EmptyView;
            }
        }

        public UserLocations SetArea( uint index )
        {
            if( AreasLoaded <= index ) return UserLocations.Invalid; 
            UserLocations select = locations?[index] ?? UserLocations.Invalid;
            if( area != select ) {
                area = select;
                MainPanel = MainPanel.UserLocations;
            } return area;
        }

        public UserLocations SetArea( string name )
        {
            if ( AreasLoaded == 0 ) return UserLocations.Invalid;
            for ( uint i = 0; i < locations.Length; ++i ) {
                if( locations[i].Name == name ) return SetArea(i);
            } return UserLocations.Invalid;
        }

        public void SetOk()
        {
            if( tool.HasFlag( ToolPanel.UserSelection ) ) {
                ToolPanel = ToolPanel.UserSelection;
                MainPanel = MainPanel.EnterPassword;
            } else if( MainPanel == MainPanel.EnterPassword ) {
                SetUser( instance.cmb_Users.SelectedIndex, instance.pwd_UserInputPass.Password ?? string.Empty);
                instance.SideBarShowLocations();
            }
        }

        public void Cancel()
        {
            if( tool.HasFlag( ToolPanel.UserSelection ) ) {
                ToolPanel = ToolPanel.UserSelection;
            } else if( MainPanel == MainPanel.EnterPassword ) {
                MainPanel = MainPanel.EmptyView;
            }
        }
    }

    public partial class ThePasswords_TheAPI_TheGUI : Window
    {
        private PasswordClient self;
        private HttpClient     http;
        private GuiState       TheState;
        private Tokken         auto;
        private InfoMessage    StatusInfoDialog;
        private CreateUser     CreateUserDialog;
        private ServerConfig   ConfigureServers;
        private NewLocation    CreateAreaDialog;
        private ResetPassword  ResetUserPassword;
        private static Status  SuccsessXaml = new Status(ResultCode.Success|ResultCode.Xaml);


        private GuiState         state;
        private System.IO.Stream file;

        private Random         rand;
        private uint lastNounce = 0;
        private int  server;

        private JsonDocumentOptions jsobtions;


        internal MainPanel           view;
        internal SidePanel           side;

        public ThePasswords_TheAPI_TheGUI()
        {
            self = PasswordClient.Registry;
            file = null;

            jsobtions = new JsonDocumentOptions();
            jsobtions.MaxDepth = 5;
            jsobtions.CommentHandling = JsonCommentHandling.Skip;
            jsobtions.AllowTrailingCommas = true;
#if DEBUG
            Consola.StdStream.Init( CreationFlags.AppendLog |
                                    CreationFlags.NewConsole |
                                    CreationFlags.NoInputLog);
#endif
            rand = new Random( (int)DateTime.Now.Ticks );

            InitializeComponent();

            HttpClientHandler handler = new HttpClientHandler();
            handler.UseDefaultCredentials = true;
            handler.AllowAutoRedirect = true;
            http = new HttpClient(handler);
            http.BaseAddress = self.TheAPI.Length == 0
                             ? new Uri("http://localhost:5000/")
                             : PasswordServer.SelectedServer.Url;

            view = MainPanel.UserLocations;
            side = SidePanel.EmptyView;
            auto = new Tokken( Tokken.CharSet.Base64, "8.7.7.7" );

            CreateUserDialog = new CreateUser(this, CreateNewUserAccount);
            ConfigureServers = new ServerConfig(this, SetupServerConnection);
            CreateAreaDialog = new NewLocation(this, CreateLocationPassword);
            ResetUserPassword = new ResetPassword(this, ResetUserMasterPassword);
            StatusInfoDialog = new InfoMessage(this, ConfirmInfoDialogMessage);

            txt_Info.AcceptsReturn = true;
            
            Loaded += ThePasswordsTheAPI_TheGUI_Loaded;
        }

        internal string AutoGeneratePassword()
        {
            return auto.Next();
        } 

        private void ThePasswordsTheAPI_TheGUI_Loaded( object sender, RoutedEventArgs e )
        {
            state = new GuiState(this);
            if( self.TheAPI.Length == 0 )
                ConfigureServers.Show();
            Loaded -= ThePasswordsTheAPI_TheGUI_Loaded;
        }

        internal MainPanel MainPanel {
            get { return view; }
            set {
                if( value != view ) {
                    switch( view ) {
                        case MainPanel.EmptyView: break;
                        case MainPanel.EnterPassword: pnl_Main_EnterPassword.Visibility = Visibility.Collapsed; break;
                        case MainPanel.UserLocations: pnl_Main_LocationsView.Visibility = Visibility.Collapsed; break;
                        case MainPanel.ServerFeatures: pnl_Main_StageFrame.Visibility = Visibility.Collapsed; break;
                    }
                    switch( value ) {
                        case MainPanel.EmptyView: break;
                        case MainPanel.EnterPassword: pnl_Main_EnterPassword.Visibility = Visibility.Visible; break;
                        case MainPanel.UserLocations: pnl_Main_LocationsView.Visibility = Visibility.Visible; break;
                        case MainPanel.ServerFeatures: pnl_Main_StageFrame.Visibility = Visibility.Visible; break;
                    }
                    view = value;
                }
            }
        }

        internal SidePanel SidePanel {
            get { return side; }
            set {
                if( value != side ) {
                    switch( side ) {
                        case SidePanel.EmptyView: break;
                        case SidePanel.UserLocations: cmb_UserLocations.Visibility = Visibility.Collapsed; break;
                        case SidePanel.ListedServers: break;
                    }
                    switch( value ) {
                        case SidePanel.EmptyView: break;
                        case SidePanel.UserLocations: cmb_UserLocations.Visibility = Visibility.Visible; break;
                        case SidePanel.ListedServers: break;
                    }
                    side = value;
                }
            }
        }

        private void HoverToolBarButtons( object sender, System.Windows.Input.MouseEventArgs e )
        {// make the toolbar buttons wobbeling when toolbar is hovered...

            Image buttonImage = sender as Image;
            if( buttonImage.IsEnabled ) {
                int v = 35 - (int)buttonImage.Width;
                buttonImage.Opacity = v < 0 ? 0.9 : 1.0;
                buttonImage.Height = buttonImage.Width = 35 + v;
                buttonImage.Margin = new Thickness( buttonImage.Margin.Left - v,
                                                    buttonImage.Margin.Top,
                                                    buttonImage.Margin.Right - v,
                                                    buttonImage.Margin.Bottom );
            } else buttonImage.Opacity = 0.2;
        }

        private void MenuItemClick( object sender, RoutedEventArgs e )
        {
            MenuItem item = sender as MenuItem;
            string commandName = item.Header.ToString();
            if(commandName.Contains(' ')) commandName = commandName.Split(' ')[0];
            ExecuteAppCommand( item.Tag.ToString(), commandName );
        }

        private void ToolButtonClick( object sender, RoutedEventArgs e )
        {
            Button button = sender as Button;
            string commandGroup = button.Tag.ToString() ?? string.Empty;

            string commandName = button.Content?.ToString() ?? string.Empty;
            if( commandName.Contains('.') || commandName == "Enter" || commandName == "Cancel" ) commandName = string.Empty;

            switch( commandGroup ) {
                case "User": commandName = "Select"; break;
                case "Area": commandName = "SideMenu"; break;

                case "Ok": {
                        if( commandName.Length == 0 ) {
                            commandGroup = "State";
                            commandName = "Ok";
                        } else if( commandName == "Store" ) {
                            commandGroup = "Area";
                        }
                    }
                    break;

                case "Ne": {
                        if( commandName.Length == 0 ) {
                            commandGroup = "State";
                            commandName = "Nö";
                        } else if( commandName == "Reset" ) {
                            commandGroup = "Area";
                        }
                    }
                    break;
            }

            if( commandGroup.Length > 0 )
                ExecuteAppCommand(commandGroup, commandName);
        }

        private void ExecuteAppCommand( string commandGroup, string commandName )
        {
            switch( commandGroup ) {
                case "Serv": {
                        switch( commandName ) {
                            case "Setup": { ConfigureServers.Show(); } break;
                            case "Configure": break;
                            case "Export": { GetDataBaseDump(); } break;
                            case "Exit": { App.Current.Shutdown(); } break;
                        }
                    }
                    break;
                case "User": {
                        switch( commandName ) {
                            case "Create": { CreateUserDialog.Show(); } break;
                            case "Select": { SelectUserAccount(); } break;
                            case "Reset": { ResetUserPassword.Show(); } break;
                            case "Delete": { DeleteUserAccount( null ); } break;
                        }
                    }
                    break;
                case "Area": {
                        switch( commandName ) {
                            case "Create": { CreateAreaDialog.Show(); } break;
                            case "Select": { SelectUserLocation(0); } break;
                            case "Store": { StoreUserLocation(); } break;
                            case "Reset": { ResetUserLocation(); } break;
                            case "Delete": { DeleteUserLocation( state.LocationName ); } break;
                            case "SideMenu": { SideBarShowLocations(); } break;
                        }
                    }
                    break;
                case "State": {
                        switch( commandName ) {
                            case "Ok": { state.SetOk(); } break;
                            case "Nö": { state.Cancel(); } break;
                        }
                    }
                    break;
            }
        }





        private void LoadPage( string xaml )
        {
            pnl_MainPanel.Content = System.Xaml.XamlServices.Parse( XamlView.StackPanel(xaml) );
        }

        private void SetupServerConnection( TheReturnData<ServerConfig.Model> e )
        {
            if( e.Ok ) {
                if( PasswordServer.Store( e.Data ).Ok ) {
                    PasswordServer.Select( e.Data.Name );
                    http.BaseAddress = PasswordServer.SelectedServer.Url;
                    Title = "Connected Passwords Server: " + PasswordServer.SelectedServer.Name;
                    e.Dialog.Hide();
                }
            } else App.Current.Shutdown();
        }

        private void CreateNewUserAccount( TheReturnData<PasswordUsers> e )
        {
            if( e.Canceled ) { e.Dialog.Hide(); return; }

            if( e.Ok ) {
                e.Dialog.Hide();

                string args = PasswordServer.SelectedServer.Key.Encrypt(
                    e.Data.Name + ".~." + e.Data.Mail + ".~." + e.Data.Is().Status.Data.ToString()
                                                      + ".~." + e.Data.Info );

                string call = String.Format( 
                    App.Current.Resources["PatchUser"] as string,
                    HttpUtility.UrlEncode( args ) );

                HttpRequestMessage request = new HttpRequestMessage(
                    HttpMethod.Patch, new Uri( http.BaseAddress + call) );

                request.Content = new ByteArrayContent(
                       Encoding.Default.GetBytes("{}") );

                HttpResponseMessage response = http.Send( request );
                if( response.IsSuccessStatusCode ) {
                    call = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    StatusInfoDialog.Show( (Status.Success + ResultCode.Xaml).WithText( response.ReasonPhrase ).WithData( call ) );
                    ReloadUserAccounts();
                } else {
                    StatusInfoDialog.Show( Status.Invalid.WithData( response.ReasonPhrase ) );
                }
            }
        }

        private void ResetUserMasterPassword( TheReturnData<ResetPassword.Data> e )
        {
            // reset the users master password (a users cryption key is generated from
            // a secret, just to the user known master password which at best is stored
            // nowhere else other then kept by user in minds.   
            e.Dialog.Hide();

            if( e.Ok ) {
                HttpResponseMessage response;
                string user = e.Data.Usr;
                if( user.Contains('@') )
                {
                    int userId = -1;
                    Uri rest = new Uri( http.BaseAddress + HttpUtility.UrlEncode(
                        Encoding.Default.GetBytes( (string)Resources["GetUsers"]) )
                                         );

                    response = http.Send( new HttpRequestMessage(HttpMethod.Get,rest) );
                    if( response.IsSuccessStatusCode )
                    {
                        string json = response.Content != null
                                    ? response.Content.ToString()
                                    : "[]";

                        PasswordUsers[] users = (PasswordUsers[])(
                            JsonSerializer.Deserialize( json, typeof(PasswordUsers[]) )
                                   ?? Array.Empty<PasswordUsers>() );

                        foreach( PasswordUsers usr in users ) {
                            if( usr.Mail == user ) {
                                userId = usr.Id;
                                break;
                            }
                        }
                    }
                    if( userId > 0 ) {
                        user = userId.ToString();
                    } else {
                        StatusInfoDialog.Show(
                            Status.Invalid.WithText("Not found user by email").WithData( user )
                        );
                        return;
                    }
                }
                CryptKey key = Crypt.CreateKey( e.Data.Old );
                if( key.IsValid() ) {

                    string args = key.Encrypt(
                        $"***{e.Data.Old}.<-.{e.Data.New}"
                    ).Trim();

                    args = string.Format(
                        (string)Resources["PatchUserPass"],
                        HttpUtility.UrlEncode(user),
                        HttpUtility.UrlEncode(args)
                    );
                    response = http.Send(
                        new HttpRequestMessage( HttpMethod.Patch, 
                        new Uri(http.BaseAddress + args) )
                    );
                    if( response.IsSuccessStatusCode )
                        StatusInfoDialog.Show( Status.Success.WithData(response.Content) );
                    else StatusInfoDialog.Show( Status.Invalid.WithText(response.ReasonPhrase).WithData(response.Content) );
                } else StatusInfoDialog.Show( Status.Cryptic.WithData(Crypt.Error) );
            }
        }

        private async void ConfirmInfoDialogMessage( TheReturnData<InfoMessage.Message> e )
        {
            if( e.Canceled ) { e.Dialog.Hide(); return; }
#if DEBUG
            StdStream.Out.WriteLine("Confirmed: {0}", e.Data.ToString());
#endif
            if ( e.Data.Text.StartsWith( "Delete" ) ) {
                switch ( e.Data.Text.Split(' ')[1] ) {
                    case "User": {
                        string pass = e.Data.Is().Status.Data.ToString() ?? string.Empty;
                        e.Dialog.Hide();
                        DeleteUserAccount( pass );
                    } break;
                    case "Location": {
                        deleteAreaConfirmed = true;
                        string args = e.Data.Is().Status.Data.ToString() ?? string.Empty;
                        e.Dialog.Hide();
                        DeleteUserLocation( args );
                    } break;
                } 
            } else
                e.Dialog.Hide();
        }

        private bool ReloadUserAccounts()
        {
            string call = (string)App.Current.Resources["GetUser"];
            HttpResponseMessage response = http.Send(
                new HttpRequestMessage(HttpMethod.Get,call)
                                                      );
            if( response.IsSuccessStatusCode ) {
                call = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                JsonDocument json = JsonDocument.Parse( call );
                List<PasswordUsers> loade = new List<PasswordUsers>();
                if( json.RootElement.ValueKind == JsonValueKind.Array ) {
                    int userscount = json.RootElement.GetArrayLength();
                    for( int i = 0; i < userscount; ++i ) {
                        PasswordUsers usr = new PasswordUsers();
                        JsonElement elm = json.RootElement[i];
                        usr.Id   = elm.GetProperty("id").GetInt32();
                        usr.Name = elm.GetProperty("name").GetString();
                        usr.Mail = elm.GetProperty("mail").GetString();
                        usr.Info = elm.GetProperty("info").GetString();
                        usr.Icon = elm.GetProperty("icon").GetBytesFromBase64();
                        loade.Add(usr);
                    }
                } return state.SetAccounts( loade );
            } else {
                string message = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                StatusInfoDialog.Show(Status.Invalid.WithText($"{response.StatusCode}: ").WithData(message));
                List<PasswordUsers> loade = new List<PasswordUsers>(0);
                return state.SetAccounts( loade );
            }
        }

        private void SelectUserAccount()
        {
            if( state.UsersLoaded == 0 ) {
                if( !ReloadUserAccounts() ) {
                    StatusInfoDialog.Show(
                        Status.Invalid.WithText("No Useraccounts")
                    );
                    return;
                }
            } state.ToolPanel = ToolPanel.UserSelection;
        }

        private void DeleteUserAccount( string? pass )
        {
            if( pass is null ) {
                if( state.SelectedUser > 0 ) {
                    StatusInfoDialog.Show(
                        Status.Unknown.WithText( "Delete User {0}? \n (All passwords of that user will be deleted as well)\n ...please enter master password:")
                                      .WithData( state.UserName )
                                            );
                }
            } else {
                string call = (string)App.Current.Resources["DeleteUser"];
                string args = Crypt.CreateKey( pass ).Encrypt( $"***{pass}.~.{state.SelectedMail}" );
                call = string.Format( call, state.SelectedUser, HttpUtility.UrlEncode( args ) );
                HttpResponseMessage reply = http.Send( new HttpRequestMessage( HttpMethod.Get, http.BaseAddress+call ) );
                if( reply.IsSuccessStatusCode ) {
                    StatusInfoDialog.Show( Status.Success.WithData(
                        reply.Content.ReadAsStringAsync().GetAwaiter().GetResult() ) );
                    state.SetAccounts( null );
                } else {
                    call = reply.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    StatusInfoDialog.Show( Status.Invalid.WithText( reply.ReasonPhrase ).WithData(call) );
                }
            }
        }

        private void CreateLocationPassword( TheReturnData<UserLocations> e )
        {
            e.Dialog.Hide();

            if( e.Canceled ) return;
            if( state.UsersLoaded == 0 ) return;

            string call = (string)App.Current.Resources["PatchArea"];
            string args = $"***{e.Data.Area}.~.{Encoding.Default.GetString(e.Data.Pass)}.~.{e.Data.Name}.~.{e.Data.Info}";
            call = string.Format( call, state.SelectedUser, state.EncryptedArgs( args ) );
            HttpResponseMessage resp = http.Send( new HttpRequestMessage( HttpMethod.Patch, call ) );
            call = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if( resp.IsSuccessStatusCode ) {
                StatusInfoDialog.Show(( Status.Success + ResultCode.Xaml ).WithText("Success").WithData(call));
            } else {
                StatusInfoDialog.Show(Status.Invalid.WithText(call));
            }
            state.SetLocations( null );
        }

        private UserLocations SelectUserLocation( uint index )
        {
            UserLocations selected = state.SetArea( index );
            if( selected.Is().Status.Bad ) {
                StatusInfoDialog.Show( selected.Is().Status.WithText("Error: loading location") );
                return selected;
            }
            txt_Area.Text = selected.Area;
            txt_Name.Text = selected.Name;
            txt_Info.Text = selected.Info;
            string pass = string.Empty;
            if( selected.Pass != null ) {
                if( selected.Pass.Length > 0 ) {
                    pass = Encoding.Default.GetString( selected.Pass );
                    pass = state.DecryptedValue( pass );
                    if( pass == null ) {
                        pass = String.Empty;
                        StatusInfoDialog.Show(
                        Status.Cryptic.WithText("Error when decrypting password")
                                      .WithData(Crypt.Error));
                    }
                }
            } else pass = string.Empty;
            txt_Pass.Text = pass;
            MainPanel = MainPanel.UserLocations;
            return selected;
        }

        private void ResetUserLocation()
        {
            UserLocations selected = state.SelectedArea;
            txt_Area.Text = selected.Area;
            txt_Name.Text = selected.Name;
            txt_Pass.Text = state.DecryptedValue( selected.Pass );
            txt_Info.Text = selected.Info;
        }

        private bool deleteAreaConfirmed = false;
        private void DeleteUserLocation( string areaOrPwd )
        {
            if ( !deleteAreaConfirmed ) {
                if( areaOrPwd != null ) {
                    if( state.SelectedArea.ToString() != areaOrPwd && state.LocationName != areaOrPwd ) {
                        if( state.SetArea( areaOrPwd ).Is().Status.Bad ) {
                            StatusInfoDialog.Show( Status.Invalid.WithText("Invalid location: ").WithData(areaOrPwd) );
                            return;
                        }
                    }
                }
                if( state.LocationName.Length > 0 ) {
                    StatusInfoDialog.Show(
                        Status.Unknown.WithText(
                            "Delete Location '{0}'?\n (enter master password to confirm...)"
                        ).WithData( state.LocationName )
                    );
                } else StatusInfoDialog.Show( Status.Invalid.WithText( "No Location is selected" ) );
            } else {
                deleteAreaConfirmed = false;
                string call = string.Format(
                    App.Current.Resources["DeleteArea"].ToString() ?? string.Empty,
                    state.UserName, state.LocationName,
                    state.EncryptedArgs( $"***{areaOrPwd}.~.{txt_Pass.Text}" )
                );
                HttpResponseMessage resp = http.Send( new HttpRequestMessage( HttpMethod.Get, call ) );
                if (resp.IsSuccessStatusCode) {
                    StatusInfoDialog.Show( Status.Success.WithData( resp.ReasonPhrase ) );
                    state.SetLocations( null );
                } else {
                    StatusInfoDialog.Show( Status.Invalid.WithText( resp.ReasonPhrase ).WithData(
                                           resp.Content.ReadAsStringAsync().GetAwaiter().GetResult() ) );
                }
            }
        }

        private void StoreUserLocation()
        {
            string args = string.Empty;
            string call = "PatchArea";
            int changes = 0;
            UserLocations area = state.SelectedArea;
            if( txt_Area.Text != state.LocationName ) changes = 8;
            if( txt_Name.Text != area.Name ) { changes |= 1; area.Name = txt_Name.Text; }
            if( txt_Info.Text != area.Info ) { changes |= 2; area.Info = txt_Info.Text; }
            string pass = state.DecryptedValue( Encoding.Default.GetString( area.Pass ) );
            if( txt_Pass.Text != pass ) { changes |= 4; pass = txt_Pass.Text; }
            switch( changes ) {
                case 1: call += "Name"; args = $"***{area.Name}"; break;
                case 2: call += "Info"; args = $"***{area.Info}"; break;
                case 4: call += "Pass"; args = $"***{pwd_UserInputPass.Password}.~.{pass}"; break;
                default: args = $"***{area.Area}.~.{pass}.~.{area.Name}.~.{area.Info}"; break;
            }
            call = string.Format( App.Current.Resources[call].ToString() ?? String.Empty,
                                  state.SelectedUser, area.Id, state.EncryptedArgs(args) );
            HttpRequestMessage request = new HttpRequestMessage( HttpMethod.Patch, call );
            HttpResponseMessage reply = http.Send(request);
            args = reply.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if ( reply.IsSuccessStatusCode ) {
                StatusInfoDialog.Show(Status.Success.WithData(args));
            } else {
                StatusInfoDialog.Show(Status.Invalid.WithText(reply.ReasonPhrase).WithData(args));
            }
        }

        internal void SideBarShowLocations()
        {
            if( state.UsersLoaded > 0 ) {
                cmb_UserLocations.Items.Clear();
                string call = string.Format( (string)App.Current.Resources["GetArea"], state.SelectedUser );
                HttpResponseMessage resp = http.Send( new HttpRequestMessage( HttpMethod.Get, call ) );
                if( resp.IsSuccessStatusCode ) {
                    call = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    JsonDocument loade = JsonDocument.Parse( call, jsobtions );
                    UserLocations[] loadcations = new UserLocations[loade.RootElement.GetArrayLength()];
                    for( int i = 0; i < loadcations.Length; ++i ) {
                        JsonElement obj = loade.RootElement[i];
                        UserLocations loc = new UserLocations();
                        JsonElement val;
                        if( obj.TryGetProperty("id", out val) ) loc.Id = val.GetInt32();
                        if( obj.TryGetProperty("user", out val) ) loc.User = val.GetInt32();
                        if( obj.TryGetProperty("area", out val) ) loc.Area = val.GetString();
                        if( obj.TryGetProperty("name", out val) ) loc.Name = val.GetString();
                        if( obj.TryGetProperty("info", out val) ) loc.Info = val.GetString();
                        if( obj.TryGetProperty("pass", out val) ) loc.Pass = val.GetBytesFromBase64();
                        loadcations[i] = loc;
                        ListBoxItem entry = new ListBoxItem();
                        entry.Content = loc.Area;
                        cmb_UserLocations.Items.Add( entry );
                    } state.SetLocations( loadcations );
                    MainPanel = MainPanel.EmptyView;
                    SidePanel = SidePanel.UserLocations;
                    cmb_UserLocations.InvalidateVisual();
                } else {
                    call = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    StatusInfoDialog.Show(Status.Invalid.WithText(call));
                }
            } else SelectUserAccount();
        }

        private void cmb_UserLocations_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            int selection = cmb_UserLocations.SelectedIndex;
            if( state.AreasLoaded == 0 ) return;
            if( selection < 0 ) return;
            SelectUserLocation( (uint)selection );
        }

        private void chk_Info_Checked( object sender, RoutedEventArgs e )
        {
            CheckBox check = sender as CheckBox;
            if( check.IsChecked.HasValue ) {
                txt_Info.Visibility = check.IsChecked.Value ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        private void txt_Info_IsVisibleChanged( object sender, System.Windows.Input.MouseEventArgs e )
        {
            TextBox info = sender as TextBox;
            if( !txt_Info.IsMouseOver ) {
                if( info.Text.ToString().Length == 0 ) {
                    chk_Info.Visibility = Visibility.Visible;
                    txt_Info.Visibility = Visibility.Collapsed;
                }
            } else {
                chk_Info.Visibility = Visibility.Collapsed;
            }
        }

        private void GetDataBaseDump()
        {
            DateTime now = DateTime.Now;
            string call = (string)App.Current.Resources["Export"];
            call = string.Format( call, state.SelectedUser,
                state.EncryptedArgs( "***" + now.ToString() ) );
            HttpResponseMessage resp = http.Send( new HttpRequestMessage( HttpMethod.Get, call ) );
            if( resp.IsSuccessStatusCode ) {
                file = resp.Content.ReadAsStream();
                SaveFileDialog save = new SaveFileDialog();
                save.DefaultExt = ".yps";
                save.CheckPathExists = true;
                save.FileOk += Save_FileOk;
                save.ShowDialog();
            } else {
                StatusInfoDialog.Show( Status.Invalid.WithText( resp.ReasonPhrase ) );
            } 
        }

        private void Save_FileOk( object sender, CancelEventArgs e )
        {
            if( e.Cancel ) { file.Close(); file = null; return; }

            int read = 0;
            byte[] buffer = new byte[512];
            System.IO.Stream store = (sender as SaveFileDialog).OpenFile();
            while( ( read = file.Read( buffer, 0, 512 ) ) > 0 )
                store.Write( buffer, 0, read );

            file.Close();
            store.Flush();
            store.Close();
            file = null;
        }

        private void window_Closed( object sender, EventArgs e )
        {
            App.Current.Shutdown(0);
        }

        private void btn_Pass_Generate_Click( object sender, RoutedEventArgs e )
        {
            txt_Pass.Text = auto.Next();
        }

        private void txt_Pass_GotFocus( object sender, RoutedEventArgs e )
        {
            btn_Pass_Generate.Visibility = Visibility.Visible;
        }

        private void txt_Pass_LostFocus( object sender, RoutedEventArgs e )
        {
            if((!btn_Pass_Generate.IsMouseOver)|(sender == btn_Pass_Generate) )
                btn_Pass_Generate.Visibility = Visibility.Collapsed;
        }

    }
}
