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

namespace Passwords.GUI
{
    public enum MainPanel
    {
        EmptyView = 0, EnterPassword = 1, UserLocations = 2
    }
    public enum SidePanel
    {
        EmptyView = 0, ListedServers = 1, UserLocations = 2
    }

    public class GuiState
    {
        private ThePasswords_TheAPI_TheGUI instance;

        private List<PasswordUsers> accounts;
        private UserLocations[]     locations;
        private PasswordUsers       user;
        private UserLocations       area;
        private CryptKey            key;

        public MainPanel            MainPanel {
            get { return instance.MainPanel; }
            set { instance.MainPanel = value; }
        }
        public SidePanel            SidePanel {
            get { return instance.SidePanel; }
            set { instance.SidePanel = value; }
        }

        public int SelectedUser { get { return user?.Id ?? -1; } }
        public string SelectedMail { get { return user.Mail; } }
        public int SelectedArea { get { return area.Id; } }
        public string UserName { get { return user?.Name ?? string.Empty; } }
        public int AreasLoaded { get { return locations?.Length ?? 0; } }

        public string EncryptedArgs( string cleartex )
        {
            return HttpUtility.UrlEncode( key?.Encrypt(cleartex) 
                ?? PasswordClient.Instance.Key.Encrypt(cleartex) );
        }

        public string DecryptedValue( string cryptex )
        {
            return key?.Decrypt( cryptex ) 
                ?? PasswordClient.Instance.Key.Decrypt( cryptex );
        }

        public GuiState(ThePasswords_TheAPI_TheGUI application)
        {
            instance = application;
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
                        instance.cmb_Users.Items.Add(accounts[i].Name);
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


    }

    public partial class ThePasswords_TheAPI_TheGUI : Window
    {
        private PasswordClient self;
        private HttpClient     http;
        private GuiState       TheState;

        private InfoMessage    StatusInfoDialog;
        private CreateUser     CreateUserDialog;
        private ServerConfig   ConfigureServers;
        private NewLocation    CreateAreaDialog;
        private ResetPassword  ResetUserPassword;

        private GuiState       state;


        private Random         rand;
        private uint lastNounce = 0;
        private int  server;

        private JsonDocumentOptions jsobtions;


        internal MainPanel           view;
        internal SidePanel           side;

        public ThePasswords_TheAPI_TheGUI()
        {
            self = PasswordClient.Instance;

            jsobtions = new JsonDocumentOptions();
            jsobtions.MaxDepth = 5;
            jsobtions.CommentHandling = JsonCommentHandling.Skip;
            jsobtions.AllowTrailingCommas = true;
#if DEBUG
            Consola.StdStream.Init(CreationFlags.AppendLog |
                                    CreationFlags.NewConsole |
                                    CreationFlags.NoInputLog);
#endif
            rand = new Random((int)DateTime.Now.Ticks);

            InitializeComponent();

            HttpClientHandler handler = new HttpClientHandler();
            handler.UseDefaultCredentials = true;
            handler.AllowAutoRedirect = true;
            http = new HttpClient(handler);
            http.BaseAddress = self.TheAPI.Length == 0
                             ? new Uri("http://localhost:5000/")
                             : PasswordServer.SelectedServer.Url;

            view = MainPanel.EmptyView;
            side = SidePanel.EmptyView;

            CreateUserDialog = new CreateUser(this, CreateNewUserAccount);
            ConfigureServers = new ServerConfig(this, SetupServerConnection);
            CreateAreaDialog = new NewLocation(this, CreateLocationPassword);
            ResetUserPassword = new ResetPassword(this, ResetUserMasterPassword);
            StatusInfoDialog = new InfoMessage(this, ConfirmInfoDialogMessage);

            txt_Info.AcceptsReturn = true;
            Loaded += ThePasswordsTheAPI_TheGUI_Loaded;
        }

        private void ThePasswordsTheAPI_TheGUI_Loaded( object sender, RoutedEventArgs e )
        {
            if( self.TheAPI.Length == 0 )
                ConfigureServers.Show();
            state = new GuiState(this);
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
                    }
                    switch( value ) {
                        case MainPanel.EmptyView: break;
                        case MainPanel.EnterPassword: pnl_Main_EnterPassword.Visibility = Visibility.Visible; break;
                        case MainPanel.UserLocations: pnl_Main_LocationsView.Visibility = Visibility.Visible; break;
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
                buttonImage.Margin = new Thickness(buttonImage.Margin.Left - v,
                                                    buttonImage.Margin.Top,
                                                    buttonImage.Margin.Right - v,
                                                    buttonImage.Margin.Bottom);
            } else buttonImage.Opacity = 0.2;
        }

        private void MenuItemClick( object sender, RoutedEventArgs e )
        {
            MenuItem item = sender as MenuItem;
            ExecuteAppCommand(item.Tag.ToString(),
                item.Header.ToString().Split(' ')[0]);
        }

        private void ExecuteAppCommand( string commandGroup, string commandName )
        {
            switch( commandGroup ) {
                case "Conf": {
                        switch( commandName ) {
                            case "Setup": { ConfigureServers.Show(); } break;
                            case "Configure": { InfoDialogXamlTest(); } break;
                        }
                    }
                    break;
                case "User": {
                        switch( commandName ) {
                            case "Create": { CreateUserDialog.Show(); } break;
                            case "Select": { SelectUserAccount(); } break;
                            case "Set": { ResetUserPassword.Show(); } break;
                            case "Delete": { DeleteUserAccount(); } break;
                        }
                    }
                    break;
                case "Area": {
                        switch( commandName ) {
                            case "Create": { CreateAreaDialog.Show(); } break;
                            case "Select": { SelectUserLocation(0); } break;
                            case "Set": { } break;
                            case "Delete": { } break;
                            case "SideMenu": { SideBarShowLocations(); } break;
                        }
                    }
                    break;
            }
        }

        private void ToolButtonClick( object sender, RoutedEventArgs e )
        {
            Button button = sender as Button;
            string commandGroup = button.Tag.ToString() ?? string.Empty;
            string commandName = string.Empty;
            switch( commandGroup ) {
                case "User": commandName = "Select"; break;
                case "Area": commandName = "SideMenu"; break;

                case "Ok":
                if( bar_UsersSelect.Visibility == Visibility.Visible ) {
                    bar_UsersSelect.Visibility = Visibility.Collapsed;
                    MainPanel = MainPanel.EnterPassword;
                    commandGroup = string.Empty;
                } else if( MainPanel == MainPanel.EnterPassword ) {
                    state.SetUser( cmb_Users.SelectedIndex, pwd_UserInputPass.Password ?? string.Empty );
                    commandGroup = "Area"; commandName = "SideMenu";
                } break;

                case "Ne":
                commandGroup = string.Empty;
                if( bar_UsersSelect.Visibility == Visibility.Visible ) {
                    bar_UsersSelect.Visibility = Visibility.Collapsed;
                } else if( MainPanel == MainPanel.EnterPassword ) {
                    MainPanel = MainPanel.EmptyView;
                } break;
            }

            if( commandGroup.Length > 0 )
                ExecuteAppCommand( commandGroup, commandName );
        }

        private void InfoDialogXamlTest()
        {
            /*
            string testdata = StdStream.Inp.ReadTill( "</StackPanel>" );
            StatusInfoDialog.Show(new Status(ResultCode.Success | ResultCode.Xaml, "TestPanel", testdata));
            */
        }

        private void LoadPage( string xaml )
        {
            pnl_MainPanel.Content = System.Xaml.XamlServices.Parse(XamlView.Frame(xaml));
        }

        private void SetupServerConnection( TheReturnData<ServerConfig.Model> e )
        {
            if( e.Ok ) {
                if( PasswordServer.Store(e.Data).Ok ) {
                    PasswordServer.Select(e.Data.Name);
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

                //Consola.StdStream.Out.WriteLine("CreateUserNewUserAccount():");
                //Consola.StdStream.Out.WriteLine("e.Status.Is(): {0}", (string)e.Data.Is().Status);
                //Consola.StdStream.Out.WriteLine("e.Data: {0}, {1}, {2}", e.Data.Name, e.Data.Mail,
                //                                 e.Data.Is().Status.Data.ToString() );
                
                string args = PasswordServer.SelectedServer.Key.Encrypt(
                    e.Data.Name + ".~." + e.Data.Mail + ".~." + e.Data.Is().Status.Data.ToString()
                                                                            );

                // StdStream.Out.WriteLine( "Encrypted content: {0}", args );
                string call = (string)App.Current.Resources["PutUser"];
                string url = String.Format( call, HttpUtility.UrlEncode(args) );
                // StdStream.Out.WriteLine("UrlEncoded relativepath: {0}", url);

                HttpRequestMessage request = new HttpRequestMessage( HttpMethod.Put, new Uri( http.BaseAddress + url) );
                request.Content = new ByteArrayContent( Encoding.Default.GetBytes("{}") );
                HttpResponseMessage response = http.Send( request );
                if( response.IsSuccessStatusCode ) {
                    call = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    StatusInfoDialog.Show( Status.Success.WithText(response.ReasonPhrase).WithData(call) ); //new Status( ResultCode.Success|ResultCode.Xaml, response.ReasonPhrase, call ) );
                } else {
                    StatusInfoDialog.Show( Status.Invalid.WithData(response.ReasonPhrase) );
                }
            }
        }

        private void ResetUserMasterPassword( TheReturnData<ResetPassword.Data> e )
        {
            // reset the users master password (a users cryption key is generated from
            // a secret, just to the user known master password which at best is stored
            // nowhere else other then kept by users in their very own minds.   
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
                        (string)Resources["PutUserPass"],
                        HttpUtility.UrlEncode(user),
                        HttpUtility.UrlEncode(args)
                    );
                    response = http.Send(
                        new HttpRequestMessage( HttpMethod.Put, 
                        new Uri(http.BaseAddress + args) )
                    );
                    if( response.IsSuccessStatusCode )
                        StatusInfoDialog.Show( Status.Success.WithData(response.Content) );
                    else StatusInfoDialog.Show( Status.Invalid.WithData(response.Content) );
                } else StatusInfoDialog.Show( Status.Cryptic.WithData(Crypt.Error) );
            }
        }

        private async void ConfirmInfoDialogMessage( TheReturnData<InfoMessage.Message> e )
        {
            if( e.Canceled ) { e.Dialog.Hide(); return; }
#if DEBUG
            StdStream.Out.WriteLine("Confirmed: {0}", e.Data.ToString());
#endif
            if( e.Data.Text.StartsWith("Delete User") ) {
                string call = (string)App.Current.Resources["DeleteUser"];
                string pass = e.Data.Is().Status.Data.ToString() ?? string.Empty;
                string args = Crypt.CreateKey(pass).Encrypt( $"***{pass}.~.{state.SelectedMail}" );
                call = string.Format(call, state.SelectedUser, HttpUtility.UrlEncode(args));
                e.Dialog.Hide();
                HttpResponseMessage reply = http.Send( new HttpRequestMessage( HttpMethod.Delete, http.BaseAddress+call ) );
                if( reply.IsSuccessStatusCode ) {
                    StatusInfoDialog.Show(Status.Success.WithData(reply.ReasonPhrase));
                } else {
                    call = reply.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    StatusInfoDialog.Show(Status.Invalid.WithText(reply.ReasonPhrase).WithData(call));
                }
                return;
            }
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
            if( state.UsersLoaded == 0 )
                if ( ReloadUserAccounts() )
                    bar_UsersSelect.Visibility = Visibility.Visible;
        }

        private void DeleteUserAccount()
        {
            if( state.SelectedUser > 0 ) {
                StatusInfoDialog.Show(
                    Status.Unknown.WithText( "Delete User? \n (All passwords of that user will be deleted as well)\n ...please enter password for:")
                                  .WithData( state.UserName )
                                        );
                state.SetAccounts( null );
            }
        }

        private void CreateLocationPassword( TheReturnData<UserLocations> e )
        {
            e.Dialog.Hide();

            if( e.Canceled ) return;
            if( state.UsersLoaded == 0 ) return;

            string call = (string)App.Current.Resources["PutArea"];
            string args = $"***{e.Data.Area}.~.{Encoding.Default.GetString(e.Data.Pass)}.~.{e.Data.Name}";
            call = string.Format( call, state.SelectedUser, state.EncryptedArgs( args ) );
            HttpResponseMessage resp = http.Send( new HttpRequestMessage( HttpMethod.Put, call ) );
            call = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            if( resp.IsSuccessStatusCode ) {
                StatusInfoDialog.Show(Status.Success.WithText("Success").WithData(call)); //new Status( ResultCode.Success|ResultCode.Xaml, "Success" , call ) );
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

        private void SideBarShowLocations()
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

        //private PasswordUsers LoginWithUserAccount( int userindex, string password )
        //{
        //    if( password.Length > 0 ) {
        //        key = Crypt.CreateKey(password);
        //        return accounts[];
        //    } else return PasswordUsers.Invalid;
        //}



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

        private void txt_Info_TextInput( object sender, System.Windows.Input.TextCompositionEventArgs e )
        {
            
        }
    }
}
