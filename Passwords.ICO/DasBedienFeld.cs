namespace Passwords.ICO
{
    public partial class DasBedienFeld : Form
    {
        private Commander commands = null;

        public DasBedienFeld()
        {
            InitializeComponent();
            btn_TheGUI.Tag = Commands.StartTheGUI;
            btn_Start.Tag = Commands.StartServer;
            btn_Stop.Tag = Commands.StopServing;

        }

        public DasBedienFeld( Commander commands ) : this()
        {
            this.commands = commands;
            btn_Start.Enabled = commands[Commands.StartServer].CanExecute;
            btn_Stop.Enabled = commands[Commands.StopServing].CanExecute;
            commands.CommandChangedState += OnCommandChangedState;
        }

        private void OnCommandChangedState( Command com )
        {
            switch( (Commands)com.Id ) {
                case Commands.StartServer: {
                        btn_Start.Enabled = false;
                        btn_Stop.Enabled = true;
                    }
                    break;
                case Commands.StopServing: {
                        btn_Stop.Enabled = false;
                        btn_Start.Enabled = true;
                    }
                    break;
            }
            Invalidate(true);
            Update();
        }

        private void BedienfeldButton_TheGUIClick( object sender, EventArgs e )
        {
            Command command = commands[Commands.StartTheGUI];
            if( command.CanExecute ) command.Execute();
        }

        private void BedienfeldButton_StartClick( object sender, EventArgs e )
        {
            Command command = commands[Commands.StartServer];
            if( command.CanExecute ) command.Execute();
        }

        private void BedienfeldButton_StopClick( object sender, EventArgs e )
        {
            Command command = commands[Commands.StopServing];
            if( command.CanExecute ) command.Execute();
        }

        private void BedienfeldButton_OnHover( object sender, EventArgs e )
        {
            Button button = (Button)sender;
            button.Enabled = commands[(Commands)button.Tag].CanExecute;
        }

        new public void Show()
        {
            base.Show();
            btn_Start.Enabled = commands[Commands.StartServer].CanExecute;
            btn_Stop.Enabled = commands[Commands.StopServing].CanExecute;
        }
    }
}