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
using Consola;
using System.Net.Http;
using System.Text.Json;
using System.Web;
using Microsoft.Win32;

namespace Passwords.GUI
{
    enum MainPanel
    {
        EmptyView = 0, EnterPassword = 1, UserLocations = 2
    }
    enum SidePanel
    {
        EmptyView = 0, ListedServers = 1, UserLocations = 2
    }

    public partial class ThePasswordsTheAPI_TheGUI : Window
    {
        private PasswordClient self;
        private HttpClient     http;
        private InfoMessage    StatusInfoDialog;
        private CreateUser     CreateUserDialog;
        private ServerConfig   ConfigureServers;
        private NewLocation    CreateAreaDialog;
        private ResetPassword  ResetUserPassword;
        private Random generator;
        private uint lastNounce = 0;
        private int server;

        private JsonDocumentOptions jsobtions;
        private List<PasswordUsers> accounts;
        private UserLocations[]     locations;
        private PasswordUsers       user;
        private UserLocations       area;
        private CryptKey            key;
        private MainPanel           view;
        private SidePanel           side;

        // 

        public ThePasswordsTheAPI_TheGUI()
        {
            self = PasswordClient.Instance;

            jsobtions = new JsonDocumentOptions();
            jsobtions.MaxDepth = 5;
            jsobtions.CommentHandling = JsonCommentHandling.Skip;
            jsobtions.AllowTrailingCommas = true;

            Consola.StdStream.Init( CreationFlags.AppendLog|
                                    CreationFlags.NewConsole|
                                    CreationFlags.NoInputLog );
            generator = new Random( (int)DateTime.Now.Ticks );

            InitializeComponent();
 
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseDefaultCredentials = true;
            handler.AllowAutoRedirect = true;
            http = new HttpClient( handler );
            http.BaseAddress = self.TheAPI.Length == 0
                             ? new Uri("http://localhost:5000/")
                             : PasswordServer.Selected.Url;
            
            view = MainPanel.EmptyView;
            side = SidePanel.EmptyView;

            CreateUserDialog = new CreateUser( this, CreateNewUserAccount );
            ConfigureServers = new ServerConfig( this, SetupServerConnection );
            CreateAreaDialog = new NewLocation( this, CreateLocationPassword );
            ResetUserPassword = new ResetPassword( this, ResetUserMasterPassword );
            StatusInfoDialog = new InfoMessage( this, ConfirmInfoDialogMessage );

            Loaded += ThePasswordsTheAPI_TheGUI_Loaded;
        }

        private void ThePasswordsTheAPI_TheGUI_Loaded( object sender, RoutedEventArgs e )
        {
            if( self.TheAPI.Length == 0 )
                ConfigureServers.Show();
            Loaded -= ThePasswordsTheAPI_TheGUI_Loaded;
        }

        private MainPanel MainPanel
        {
            get { return view; }
            set { if (value != view) {
                    switch (view)
                    {
                        case MainPanel.EmptyView: break;
                        case MainPanel.EnterPassword: pnl_Main_EnterPassword.Visibility = Visibility.Collapsed; break;
                        case MainPanel.UserLocations: pnl_Main_LocationsView.Visibility = Visibility.Collapsed; break;
                    }
                    switch (value)
                    {
                        case MainPanel.EmptyView: break;
                        case MainPanel.EnterPassword: pnl_Main_EnterPassword.Visibility = Visibility.Visible; break;
                        case MainPanel.UserLocations: pnl_Main_LocationsView.Visibility = Visibility.Visible; break;
                    } 
                    view = value;
                } 
            }
        }

        private SidePanel SidePanel
        {
            get { return side; }
            set { if (value != side) {
                    switch (side)
                    {
                        case SidePanel.EmptyView: break;
                        case SidePanel.UserLocations: cmb_UserLocations.Visibility = Visibility.Collapsed; break;
                        case SidePanel.ListedServers: break;
                    }
                    switch (value)
                    {
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
            switch (item.Tag)
            {
                case "Conf": {
                    switch (item.Header.ToString().Split(' ')[0])
                    {
                        case "Setup": { ConfigureServers.Show(); } break;
                        case "Configure": { } break;
                    }    
                } break;
                case "User": {
                    switch (item.Header.ToString().Split(' ')[0])
                    {
                        case "Create": { CreateUserDialog.Show(); } break;
                        case "Select": { SelectUserAccount(); } break;
                        case "Set":    { ResetUserPassword.Show(); } break;
                        case "Delete": { DeleteUserAccount(); } break;
                    }
                } break;
                case "Area": {
                    switch (item.Header.ToString().Split(' ')[0])
                    {
                        case "Create": { CreateAreaDialog.Show(); } break;
                        case "Select": { SelectUserLocation( 0 ); } break;
                        case "Set": { } break;
                        case "Delete": { } break;
                    }
                } break;
            }
        }

        private void LoadPage( string xaml )
        {
            pnl_MainPanel.Content = System.Xaml.XamlServices.Parse( XamlView.Frame(xaml) );
        }

        private void SetupServerConnection( TheReturnData<ServerConfig.Model> e )
        {
            if( e.Ok ) {
                if( PasswordServer.Store( e.Data ).Ok ) {
                    PasswordServer.Select( e.Data.Name );
                    http.BaseAddress = PasswordServer.Selected.Url;
                    Title = "Connected Passwords Server: " + PasswordServer.Selected.Name;
                    e.Dialog.Hide();
                }
            } else App.Current.Shutdown();
        }

        private void CreateNewUserAccount( TheReturnData<PasswordUsers> e )
        {
            if( e.Canceled ) { e.Dialog.Hide(); return; }

            if( e.Ok ) {
                e.Dialog.Hide();

                Consola.StdStream.Out.WriteLine("CreateUserNewUserAccount():");
                Consola.StdStream.Out.WriteLine("e.Status.Is(): {0}", (string)e.Data.Is().Status );
                Consola.StdStream.Out.WriteLine("e.Data: {0}, {1}, {2}", e.Data.Name, e.Data.Mail,
                                                 e.Data.Is().Status.Data.ToString() );
                string args = PasswordServer.Selected.Key.Encrypt(
                    e.Data.Name + ".~." + e.Data.Mail + ".~." + e.Data.Is().Status.Data.ToString()
                                                                   );
                Consola.StdStream.Out.WriteLine("Encrypted content: {0}", args);
                string call = (string)App.Current.Resources["PutUser"];
                string url = String.Format( call, HttpUtility.UrlEncode(args) );
                Consola.StdStream.Out.WriteLine("UrlEncoded relativepath: {0}", url);
                HttpRequestMessage request = new HttpRequestMessage( HttpMethod.Put, new Uri( http.BaseAddress + url) );
                request.Content = new ByteArrayContent( Encoding.Default.GetBytes("{}") );
                HttpResponseMessage response = http.Send( request );
                if (response.IsSuccessStatusCode) {
                    call = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    StatusInfoDialog.Show( new Status( ResultCode.Success|ResultCode.Xaml, response.ReasonPhrase, call ) );
                } else {
                    StatusInfoDialog.Show( Status.Invalid.WithText( response.ReasonPhrase ) );
                }
            }
        }

        private void ResetUserMasterPassword( TheReturnData<ResetPassword.Data> e )
        {
            e.Dialog.Hide();

            if ( e.Ok ) {
                HttpResponseMessage response;
                string user = e.Data.Usr;
                if( user.Contains('@') ) {
                    int userId = -1;
                    Uri rest = new Uri(http.BaseAddress + HttpUtility.UrlEncode(Encoding.Default.GetBytes((string)Resources["GetUsers"])) );
                    response = http.Send( new HttpRequestMessage( HttpMethod.Get, rest ) );
                    if (response.IsSuccessStatusCode) {
                        string json = response.Content == null ? "[]" : response.Content.ToString();
                        PasswordUsers[] users = (PasswordUsers[])(JsonSerializer.Deserialize( json, typeof(PasswordUsers[])) ?? Array.Empty<PasswordUsers>());
                        foreach( PasswordUsers usr in users ) {
                            if( usr.Mail == user ) {
                                userId = usr.Id;
                                break;
                            }
                        }
                    } if (userId > 0) {
                        user = userId.ToString();
                    } else { 
                        StatusInfoDialog.Show( Status.Invalid.WithText("Not found user by email").WithData(user) );
                        return;
                    }
                }
                CryptKey key = Crypt.CreateKey( e.Data.Old );
                if (key.IsValid()) {
                    string args = key.Encrypt($"***{e.Data.Old}.<-.{e.Data.New}").Trim();
                    args = string.Format((string)Resources["PutUserPass"], HttpUtility.UrlEncode(user), HttpUtility.UrlEncode(args));
                    response = http.Send(new HttpRequestMessage(HttpMethod.Put, new Uri(http.BaseAddress + args)));
                    if (response.IsSuccessStatusCode)
                        StatusInfoDialog.Show(Status.Success.WithData(response.Content));
                    else StatusInfoDialog.Show(Status.Invalid.WithData(response.Content));
                } else StatusInfoDialog.Show(Status.Cryptic.WithData(Crypt.Error));
            }
        }

        private async void ConfirmInfoDialogMessage( TheReturnData<InfoMessage.Message> e )
        {
            if( e.Canceled ) { e.Dialog.Hide(); return; }

            StdStream.Out.WriteLine( "Confirmed: {0}", e.Data.ToString() );
            if( e.Data.Text.StartsWith("Delete User") ) { 
                string call = (string)App.Current.Resources["DeleteUser"];
                string pass = e.Data.Is().Status.Data.ToString() ?? string.Empty;
                string args = Crypt.CreateKey(pass).Encrypt( $"***{pass}.~.{user.Mail}" );
                call = string.Format( call, user.Id, HttpUtility.UrlEncode(args) );
                e.Dialog.Hide();
                HttpResponseMessage reply = http.Send( new HttpRequestMessage( HttpMethod.Delete, http.BaseAddress+call ) );
                if (reply.IsSuccessStatusCode) {
                    StatusInfoDialog.Show( Status.Success.WithData( reply.ReasonPhrase ) );
                } else {
                    call = reply.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    StatusInfoDialog.Show( Status.Invalid.WithText( reply.ReasonPhrase ).WithData( call ) );
                } return;
            } e.Dialog.Hide();
        }

        private List<PasswordUsers> ReloadUserAccounts()
        {
            string call = (string)App.Current.Resources["GetUser"];
            HttpResponseMessage response = http.Send(new HttpRequestMessage(HttpMethod.Get, call));
            if ( response.IsSuccessStatusCode )
            {
                call = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                JsonDocument json = JsonDocument.Parse( call );
                List<PasswordUsers> loade = new List<PasswordUsers>();
                if (json.RootElement.ValueKind == JsonValueKind.Array)
                {
                    int userscount = json.RootElement.GetArrayLength();
                    for (int i = 0; i < userscount; ++i)
                    {
                        PasswordUsers usr = new PasswordUsers();
                        JsonElement elm = json.RootElement[i];
                        usr.Id   = elm.GetProperty("id").GetInt32();
                        usr.Name = elm.GetProperty("name").GetString();
                        usr.Mail = elm.GetProperty("mail").GetString();
                        usr.Info = elm.GetProperty("info").GetString();
                        usr.Icon = elm.GetProperty("icon").GetBytesFromBase64();
                        loade.Add( usr );
                    }
                }
                return accounts = loade;
            } else {
                string message = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                StatusInfoDialog.Show( Status.Invalid.WithText( $"{response.StatusCode}: " ).WithData( message ) );
                return accounts = new List<PasswordUsers>(0);
            }
        }

        private void SelectUserAccount()
        {
            if ( accounts == null )
                accounts = ReloadUserAccounts();
            
            cmb_Users.Items.Clear();
            if (accounts.Count > 0) {
                for (int i = 0; i < accounts.Count; ++i)
                    cmb_Users.Items.Add( accounts[i].Name );
                bar_UsersSelect.Visibility = Visibility.Visible;
            }
        }

        private void DeleteUserAccount()
        {
            if ( user.IsValid() ) {
                StatusInfoDialog.Show(
                    Status.Unknown.WithText( "Delete User? \n ...please enter password for:" )
                                  .WithData( user.Name )
                                        );
                accounts = null;
            }
        }

        private void CreateLocationPassword( TheReturnData<UserLocations> e )
        {
            e.Dialog.Hide();

            if (e.Canceled) return;
            if (user == null) return;
            if (!user.IsValid()) return;

            string call = (string)App.Current.Resources["PutArea"];
            string args = $"***{e.Data.Area}.~.{Encoding.Default.GetString(e.Data.Pass)}.~.NoInfo.~.{e.Data.Name}";
            call = string.Format( call, user.Id, HttpUtility.UrlEncode( key.Encrypt( args ) ) );
            HttpResponseMessage resp = http.Send( new HttpRequestMessage( HttpMethod.Put, call ) );
            call = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if ( resp.IsSuccessStatusCode ) {
                StatusInfoDialog.Show( Status.Success.WithText( "Success" ).WithData( call ) );
            } else {
                StatusInfoDialog.Show( Status.Invalid.WithText( call ) );
            } locations = null;
        }

        private UserLocations SelectUserLocation( int index )
        {
            if (locations == null) return UserLocations.Invalid;
            if (locations.Length <= index) return UserLocations.Invalid;
            UserLocations selected = locations[index];
            txt_Area.Content = selected.Area;
            txt_Info.Content = selected.Info;
            txt_Name.Content = selected.Name;
            string pass = string.Empty;
            if ( selected.Pass != null ) {
                if( selected.Pass.Length > 0 ) {
                    pass = Encoding.Default.GetString( selected.Pass );
                    if ( key.IsValid() ) {
                        pass = key.Decrypt( pass );
                        if (pass == null) { pass = String.Empty;
                            StatusInfoDialog.Show(
                            Status.Cryptic.WithText("Error when decrypting password")
                                          .WithData(Crypt.Error) );
                        }
                    } 
                } else pass = string.Empty;
                txt_Pass.Content = pass;
            } else {
                StatusInfoDialog.Show( 
                    Status.Invalid.WithText( "Error: loading location" )
                                        );
                return UserLocations.Invalid;
            } MainPanel = MainPanel.UserLocations;
            return selected;
        }

        private void SideBarShowLocations()
        {
            if( user.IsValid() ) {
                cmb_UserLocations.Items.Clear();
                string call = string.Format( (string)App.Current.Resources["GetArea"], user.Id );
                HttpResponseMessage resp = http.Send( new HttpRequestMessage( HttpMethod.Get, call ) );
                if (resp.IsSuccessStatusCode) {
                    call = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    JsonDocument loade = JsonDocument.Parse( call, jsobtions );
                    locations = new UserLocations[loade.RootElement.GetArrayLength()];
                    for( int i = 0; i < locations.Length; ++i ) {
                        JsonElement obj = loade.RootElement[i];
                        UserLocations loc = new UserLocations();
                        JsonElement val;
                        if (obj.TryGetProperty("id",   out val))   loc.Id = val.GetInt32();
                        if (obj.TryGetProperty("user", out val)) loc.User = val.GetInt32();
                        if (obj.TryGetProperty("area", out val)) loc.Area = val.GetString();
                        if (obj.TryGetProperty("name", out val)) loc.Name = val.GetString();
                        if (obj.TryGetProperty("info", out val)) loc.Info = val.GetString();
                        if (obj.TryGetProperty("pass", out val)) loc.Pass = val.GetBytesFromBase64();
                        locations[i] = loc;
                        ListBoxItem entry = new ListBoxItem();
                        entry.Content = loc.Area;
                        cmb_UserLocations.Items.Add( entry );
                    } MainPanel = MainPanel.EmptyView;
                    SidePanel = SidePanel.UserLocations;
                    cmb_UserLocations.InvalidateVisual();
                } else {
                    call = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    StatusInfoDialog.Show( Status.Invalid.WithText( call ) );
                }
            } else SelectUserAccount();
        }

        private PasswordUsers LoginWithUserAccount( string password )
        {
            if( password.Length > 0 ) {
                key = Crypt.CreateKey( password );
                return accounts[cmb_Users.SelectedIndex];
            } else return PasswordUsers.Invalid;
        }

        private void ToolButtonClick( object sender, RoutedEventArgs e )
        {
            Button button = sender as Button;
            switch( button.Tag.ToString() )
            {
                case "User": SelectUserAccount(); break;
                case "Area": SideBarShowLocations(); break;

                case "Ok":
                if (bar_UsersSelect.Visibility == Visibility.Visible) {
                    bar_UsersSelect.Visibility = Visibility.Collapsed;
                    MainPanel = MainPanel.EnterPassword;
                } else if ( MainPanel == MainPanel.EnterPassword ) {
                    user = LoginWithUserAccount( pwd_UserInputPass.Password ?? string.Empty );
                    SideBarShowLocations();
                } break;

                case "Ne":
                if (bar_UsersSelect.Visibility == Visibility.Visible) {
                    bar_UsersSelect.Visibility = Visibility.Collapsed;
                } else if ( MainPanel == MainPanel.EnterPassword ) {
                    MainPanel = MainPanel.EmptyView;
                } break;
            }
        }

        private void cmb_UserLocations_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            int selection = cmb_UserLocations.SelectedIndex;
            if (locations == null) return;
            if (selection < 0) return;
            area = SelectUserLocation( selection );
        }
    }
}
