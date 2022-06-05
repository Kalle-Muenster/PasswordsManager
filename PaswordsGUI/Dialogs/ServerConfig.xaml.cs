using System;
using Passwords.API.Abstracts;
using Passwords.API.Models;
using System.Windows;


namespace Passwords.GUI
{
    public partial class ServerConfig 
        : Window, IThePasswords_TheGUI_ADialog<ServerConfig.Model>
    {
        public class Model : EntityBase<Model>
        {
            public static readonly Model Invalid = new( 
                new Status( ResultCode.Invalid, "Invalid ServerConfig" )
            );

            public string Name { get; set; }
            public Uri    Url { get; set; }
            public string Key { get; set; }

            public Model()
            {
                Name = string.Empty;
                Key = string.Empty;
                Url = null;
            }

            public Model( Status bad ) : this()
            {
                Is().Status = bad;
            }

            public static implicit operator Model( Status cast )
            {
                return new Model( cast );
            }
        }


        ThePasswords_TheAPI_TheGUI ThePasswords_TheGUI_ItsDialogs_TheInterface.TheGUI { get; set; }
        IThePasswords_TheGUI_ADialog<Model>.ItsReturnAction IThePasswords_TheGUI_ADialog<Model>.TheAction { get; set; }
        IEntityBase ThePasswords_TheGUI_ItsDialogs_TheInterface.TheData { get; set; }
        public ThePasswords_TheGUI_ItsDialogs_TheInterface theDialog() { return this; }
        void ThePasswords_TheGUI_ItsDialogs_TheInterface.Returns()
        {
            (this as IThePasswords_TheGUI_ADialog<Model>).TheAction(TheReturnData<Model>.fromTheDialog(this));
        }
        void ThePasswords_TheGUI_ItsDialogs_TheInterface.TheReturnAction<T>(IThePasswords_TheGUI_ADialog<T>.ItsReturnAction onProcced)
        {
            if (typeof(T) == typeof(Model))
            {
                (this as IThePasswords_TheGUI_ADialog<Model>).TheAction = onProcced
                      as IThePasswords_TheGUI_ADialog<Model>.ItsReturnAction;
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


        public ServerConfig( ThePasswords_TheAPI_TheGUI main, IThePasswords_TheGUI_ADialog<Model>.ItsReturnAction action )
        {
            theDialog().TheGUI = main;
            theDialog().TheData = Model.Invalid;
            theDialog().TheReturnAction( action );
            InitializeComponent();
        }

        private void Button_Click( object sender, RoutedEventArgs e )
        {
            theDialog().TheData = Model.Invalid;
            theDialog().Returns();
        }

        private void btn_Save_Click( object sender, RoutedEventArgs e )
        {
            Model data = new Model(API.Abstracts.Status.Success);
            data.Name = txt_Host.Text;
            data.Key = txt_Key.Text;
            data.Url = new Uri($"http://{data.Name}:{txt_Port.Text}/");
            theDialog().TheData = data;
            theDialog().Returns();
        }

        private void InputTextChanged( object sender, System.Windows.Input.KeyEventArgs e )
        {
            if(btn_Save.IsEnabled) {
                if((txt_Host.Text.Length == 0)||(txt_Key.Text.Length == 0)||(txt_Port.Text.Length == 0))
                    btn_Save.IsEnabled = false;
            } else {
                if ((txt_Host.Text.Length > 0) && (txt_Key.Text.Length > 0) && (txt_Port.Text.Length > 0))
                    btn_Save.IsEnabled = true;
            }
        }
    }
}
