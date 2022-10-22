using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Passwords.API.Abstracts;
using Passwords.API.Models;

namespace Passwords.GUI
{
    public partial class ResetPassword : Window, IThePasswords_TheGUI_ADialog<ResetPassword.Data>
    {
        public class Data : EntityBase<Data>, IDisposable
        {
            internal static ResetPassword dialog;

            public string Old { get { return dialog.txt_OldPassword.Text; } }
            public string New { get { return dialog.txt_NewPassword.Text; } }
            public string Usr { get { return dialog.txt_UserAccount.Text; } }

            private void CheckFields( object sender, RoutedEventArgs e ) {
                if (Old.Length == 0 || New.Length == 0 || Usr.Length == 0) {
                    if ( Is().Status ) Is().Status = API.Abstracts.Status.Invalid;
                } else if ( Is().Status.Bad ) {
                    Is().Status = API.Abstracts.Status.Success;
                }
            }

            public void Dispose()
            {
                dialog.txt_OldPassword.TextChanged -= CheckFields;
                dialog.txt_NewPassword.TextChanged -= CheckFields;
                dialog.txt_UserAccount.TextChanged -= CheckFields;
            }

            public Data() : base( API.Abstracts.Status.Unknown ) {
                dialog.txt_OldPassword.TextChanged += CheckFields;
                dialog.txt_NewPassword.TextChanged += CheckFields;
                dialog.txt_UserAccount.TextChanged += CheckFields;
            }

            public Data(Status bad) : base(bad) {}
            public static implicit operator Data( Status cast ) {
                return new Data(cast);
            }
        }

        private Data data;

        ThePasswords_TheAPI_TheGUI ThePasswords_TheGUI_ItsDialogs_TheInterface.TheGUI { get; set; }
        IThePasswords_TheGUI_ADialog<Data>.ItsReturnAction IThePasswords_TheGUI_ADialog<Data>.TheAction { get; set; }
        IEntityBase ThePasswords_TheGUI_ItsDialogs_TheInterface.TheData { get; set; }
        public ThePasswords_TheGUI_ItsDialogs_TheInterface theDialog() { return this; }
        void ThePasswords_TheGUI_ItsDialogs_TheInterface.Returns()
        {
            (this as IThePasswords_TheGUI_ADialog<Data>).TheAction(TheReturnData<Data>.fromTheDialog(this));
        }
        void ThePasswords_TheGUI_ItsDialogs_TheInterface.TheReturnAction<T>(IThePasswords_TheGUI_ADialog<T>.ItsReturnAction onProcced)
        {
            if (typeof(T) == typeof(Data))
            {
                (this as IThePasswords_TheGUI_ADialog<Data>).TheAction = onProcced
                      as IThePasswords_TheGUI_ADialog<Data>.ItsReturnAction;
            }
            else throw new Exception("the dialog it's data type is not: " + typeof(T).Name);
        }

        public DialogReturnState Status
        {
            get
            {
                return theDialog().TheData.Status
                     ? DialogReturnState.Ok
                     : DialogReturnState.Canceled;
            }
        }


        public ResetPassword( ThePasswords_TheAPI_TheGUI main, IThePasswords_TheGUI_ADialog<Data>.ItsReturnAction action )
        {
            Data.dialog = this;
            theDialog().TheGUI = main;
            theDialog().TheReturnAction( action );

            InitializeComponent();

            btn_Reset.Click += Btn_Reset_Click;
            btn_Cancel.Click += Btn_Cancel_Click;

            data = new Data();
            theDialog().TheData = data;
        }

        public new void Show()
        {
            txt_OldPassword.Text = String.Empty;
            base.Show();
        }

        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            txt_OldPassword.Text = String.Empty; 
            theDialog().Returns();
        }

        private void Btn_Reset_Click(object sender, RoutedEventArgs e)
        {
            if ( !data.IsValid() ) {
                txt_OldPassword.Text = String.Empty;
            } theDialog().Returns();
        }
    }
}
