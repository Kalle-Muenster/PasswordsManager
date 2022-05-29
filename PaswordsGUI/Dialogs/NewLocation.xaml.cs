using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Passwords.API.Models;
using Passwords.API.Abstracts;

namespace Passwords.GUI
{
    /// <summary>
    /// Interaktionslogik für NewLocation.xaml
    /// </summary>
    public partial class NewLocation : Window, IThePasswords_TheGUI_ADialog<UserLocations>
    {
        ThePasswords_TheAPI_TheGUI ThePasswords_TheGUI_ItsDialogs_TheInterface.TheGUI { get; set; }
        IThePasswords_TheGUI_ADialog<UserLocations>.ItsReturnAction IThePasswords_TheGUI_ADialog<UserLocations>.TheAction { get; set; }
        IEntityBase ThePasswords_TheGUI_ItsDialogs_TheInterface.TheData { get; set; }
        public ThePasswords_TheGUI_ItsDialogs_TheInterface theDialog() { return this; }
        void ThePasswords_TheGUI_ItsDialogs_TheInterface.Returns() {
            (this as IThePasswords_TheGUI_ADialog<UserLocations>).TheAction(
                           TheReturnData<UserLocations>.fromTheDialog(this) );
        }
        void ThePasswords_TheGUI_ItsDialogs_TheInterface.TheReturnAction<T>(IThePasswords_TheGUI_ADialog<T>.ItsReturnAction onProcced)
        {
            if ( typeof(T) == typeof(UserLocations) ) {
                (this as IThePasswords_TheGUI_ADialog<UserLocations>).TheAction = onProcced
                      as IThePasswords_TheGUI_ADialog<UserLocations>.ItsReturnAction;
            } else throw new Exception( "the dialog it's data type is not: " + typeof(T).Name );
        }

        public DialogReturnState Status
        {
            get {
                return theDialog().TheData.Status
                     ? DialogReturnState.Ok
                     : DialogReturnState.Canceled;
            }
        }

        public NewLocation(ThePasswords_TheAPI_TheGUI main, IThePasswords_TheGUI_ADialog<UserLocations>.ItsReturnAction action)
        {
            theDialog().TheGUI = main;
            theDialog().TheData = UserLocations.Invalid;
            theDialog().TheReturnAction( action );
            InitializeComponent();
        }

        private void btn_Cancel_Click( object sender, RoutedEventArgs e )
        {
            theDialog().TheData = UserLocations.Invalid;
            theDialog().Returns();
        }

        private void btn_Ok_Click( object sender, RoutedEventArgs e )
        {
            UserLocations data = new UserLocations();
            data.Area = txt_LocationName.Text;
            data.Name = txt_LocationLogin.Text;
            data.Pass = Encoding.Default.GetBytes( txt_LocationPass.Text );
            theDialog().TheData = data;
            theDialog().Returns();
        }

        public new void Show()
        {
            txt_LocationName.Text =
            txt_LocationLogin.Text =
            txt_LocationPass.Text = String.Empty;
            btn_Ok.IsEnabled = false;
            base.Show();
        }

        private void txt_TextChanged( object sender, RoutedEventArgs e )
        {
            if (!btn_Ok.IsEnabled)
            {
                if (txt_LocationName.Text.Length > 0 && txt_LocationPass.Text.Length > 0)
                    btn_Ok.IsEnabled = true;
            } else
            {
                if (txt_LocationName.Text.Length == 0 || txt_LocationPass.Text.Length == 0 )
                    btn_Ok.IsEnabled = false;
            }
        }

        private void btn_autoPasswort_Click( object sender, RoutedEventArgs e )
        {
            txt_LocationPass.Text = theDialog().TheGUI.AutoGeneratePassword();
        }

    }
}
