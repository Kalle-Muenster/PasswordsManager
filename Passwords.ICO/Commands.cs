<<<<<<< HEAD
using Microsoft.Win32;
=======
﻿using Microsoft.Win32;
>>>>>>> refs/remotes/fork/main
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace Passwords.ICO
{
    public static class Extensions
    {
        public static uint ToUInt32(this Enum cast)
        {
            return Convert.ToUInt32(cast, System.Globalization.CultureInfo.CurrentCulture);
        }
    }

    internal class Command
    {
        private Enum id;
        private Delegate command;
        private Func<bool> check;

        public uint Id {
            get { return id.ToUInt32(); }
        }
        public string Name {
            get { return id.ToString(); }
        }
        public bool CanExecute {
            get { return check(); }
        }
        public void Execute( params object[] args )
        {
            command.DynamicInvoke( args );
        }
        internal Command( Enum Id, Delegate Call, Func<bool> Test )
        {
            id = Id;
            command = Call;
            check = Test;
        }
    }

    internal enum Commands
    {
        Cancel = 0,
        StartTheGUI = 1,
        StartServer = 2,
        StopServing = 3,
        ExitTheIcon = 4
    }

    public class Commander
    {
        private string ThePathToTheGui = "HKEY_CURRENT_USER\\Software\\ThePasswords\\TheGUI\\TheClient";
        private bool TheServerIsRunning {
            get { return PwdApiSc.Status == ServiceControllerStatus.Running; }
        }

        private ServiceController        PwdApiSc;
        private Dictionary<uint,Command> commands;
        private Task                     switcher;
        internal delegate void StateChange( Command com );
        internal event StateChange CommandChangedState;
<<<<<<< HEAD

=======
        
>>>>>>> refs/remotes/fork/main
        private void ReadRegistry()
        {
            if( ThePathToTheGui.StartsWith("HKEY") ) {
                ThePathToTheGui = Registry.GetValue( ThePathToTheGui, "ThePath", string.Empty ) as string;
                ThePathToTheGui += "\\Passwords.Gui.exe";
            }
        }

        private void PasswordsAPI()
        {
            PwdApiSc = new ServiceController("PasswordsAPI","EVE");
        }

        private bool CanStartGui()
        {
            return !( ThePathToTheGui.StartsWith("\\") || ThePathToTheGui.StartsWith("HKEY") );
        }

        private void LetStartGui()
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo( ThePathToTheGui )
            );
        }

        private bool CanStartApi()
        {
            PwdApiSc.Refresh();
            return PwdApiSc.Status == ServiceControllerStatus.Stopped; 
        }

        private void LetStartApi()
        {
            if( CanStartApi() ) {
                PwdApiSc.Start();
            }
        }

        private bool CanStopApi()
        {
            PwdApiSc.Refresh();
            return PwdApiSc.Status == ServiceControllerStatus.Running;
        }

        private void LetStopApi()
        {
            if( CanStopApi() ) {
                PwdApiSc.Stop();
            }
        }

        internal Commander( Command exiter )
        {
            switcher = null;
            ReadRegistry();
            PasswordsAPI();
            commands = new Dictionary<uint,Command>();
            commands.Add(1, new Command(Commands.StartTheGUI, LetStartGui, CanStartGui));
            commands.Add(2, new Command(Commands.StartServer, LetStartApi, CanStartApi));
            commands.Add(3, new Command(Commands.StopServing, LetStopApi, CanStopApi));
            commands.Add(4, exiter);
        }

        internal Command this[Commands idx] {
            get { return commands[idx.ToUInt32()]; }
        }
    }
}
