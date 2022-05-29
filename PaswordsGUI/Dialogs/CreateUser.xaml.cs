using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Passwords.API.Abstracts;
using Passwords.API.Models;

namespace Passwords.GUI {

    public partial class CreateUser : Window, IThePasswords_TheGUI_ADialog<PasswordUsers>
    {
        private bool neu = true;
        private DateTime click;
        private Microsoft.Win32.OpenFileDialog file;

        ThePasswords_TheAPI_TheGUI ThePasswords_TheGUI_ItsDialogs_TheInterface.TheGUI { get; set; }
        IThePasswords_TheGUI_ADialog<PasswordUsers>.ItsReturnAction IThePasswords_TheGUI_ADialog<PasswordUsers>.TheAction { get; set; }
        IEntityBase ThePasswords_TheGUI_ItsDialogs_TheInterface.TheData { get; set; }
        public ThePasswords_TheGUI_ItsDialogs_TheInterface theDialog() { return this; }
        void ThePasswords_TheGUI_ItsDialogs_TheInterface.Returns() {
            (this as IThePasswords_TheGUI_ADialog<PasswordUsers>).TheAction( TheReturnData<PasswordUsers>.fromTheDialog(this) );
        }
        void ThePasswords_TheGUI_ItsDialogs_TheInterface.TheReturnAction<T>(IThePasswords_TheGUI_ADialog<T>.ItsReturnAction onProcced) {
            if (typeof(T) == typeof(PasswordUsers)) {
                (this as IThePasswords_TheGUI_ADialog<PasswordUsers>).TheAction = onProcced
                      as IThePasswords_TheGUI_ADialog<PasswordUsers>.ItsReturnAction;
            } else throw new Exception("the dialog it's data type is not: " + typeof(T).Name);
        }

        public DialogReturnState Status {
            get { return theDialog().TheData.Status
                       ? DialogReturnState.Ok
                       : DialogReturnState.Canceled;
            }
        }

        public CreateUser( ThePasswords_TheAPI_TheGUI main, IThePasswords_TheGUI_ADialog<PasswordUsers>.ItsReturnAction action )
        {
            theDialog().TheGUI = main;
            theDialog().TheReturnAction( action ); 
            theDialog().TheData = PasswordUsers.Invalid;
            InitializeComponent();
            neu = false;
            click = DateTime.Now;
        }


        private void txt_InputField( object sender, TextChangedEventArgs e )
        {
            if (neu) return;
            if (txt_UserName.Text.Length > 0 && txt_UserMail.Text.Length > 0 && txt_UserPass.Text.Length > 0)
            {
                if (!btn_Create.IsEnabled) btn_Create.IsEnabled = true;
            }
            else if (btn_Create.IsEnabled)
            {
                btn_Create.IsEnabled = false;
            }
        }

        private void btn_Cancel_Click( object sender, RoutedEventArgs e )
        {
            theDialog().TheData = PasswordUsers.Invalid;
            theDialog().Returns();
        }

        private void btn_Create_Click(object sender, RoutedEventArgs e)
        {
            PasswordUsers data = new PasswordUsers();
            data.Name = txt_UserName.Text;
            data.Mail = txt_UserMail.Text;
            data.Info = txt_UserInfo.Text;
            if ( img_UserIcon.Source != null )
                data.Icon = Encoding.Default.GetBytes( img_UserIcon.DataContext.ToString() );
            data.Is().Status = API.Abstracts.Status.Success.WithText("Pass").WithData( txt_UserPass.Text );
            theDialog().TheData = data;
            theDialog().Returns();
        }

        private void txt_UserInfo_LostFocus( object sender, RoutedEventArgs e )
        {
            (sender as TextBox).Height = 28;
            Thickness margin = lbl_UserInfo.Margin;
            margin.Top = 205;
            lbl_UserInfo.Margin = margin;
            img_UserIcon.Visibility = Visibility.Visible;
            lbl_UserIcon.Visibility = Visibility.Visible;
        }

        private void txt_UserInfo_GotFocus( object sender, RoutedEventArgs e )
        {
            ( sender as TextBox ).Height = 250;
            Thickness margin = lbl_UserInfo.Margin;
            margin.Top = 420;
            lbl_UserInfo.Margin = margin;
            img_UserIcon.Visibility = Visibility.Collapsed;
            lbl_UserIcon.Visibility = Visibility.Collapsed;
        }

        private void img_UserIcon_MouseLeftButtonDown( object sender, System.Windows.Input.MouseButtonEventArgs e )
        {
            DateTime time = DateTime.Now;
            if ( e.LeftButton == System.Windows.Input.MouseButtonState.Pressed ) {
                click = time;
            } else if ( (time - click) < TimeSpan.FromMilliseconds(500) ) {
                if( file is null ) {
                    file = new Microsoft.Win32.OpenFileDialog();
                    file.DefaultExt = "*.png;*.jpg";
                    file.FileOk += File_FileOk;
                } file.ShowDialog();
            }
        }

        private void File_FileOk( object sender, System.ComponentModel.CancelEventArgs e )
        {
            img_UserIcon.Source = new System.Windows.Media.Imaging.BitmapImage( new Uri(file.FileName) );
        }
    }
}
