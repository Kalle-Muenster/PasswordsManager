using System.Windows.Controls;
using System.Windows.Forms;

namespace Passwords.ICO
{            
    internal class Program : IDisposable
    { 
        private NotifyIcon notifyIcon;
        private ContextMenuStrip notificationMenu;
        private System.ComponentModel.IContainer components;
        private static DasBedienFeld panel;
        private Commander commands;
        private ToolStripMenuItem starter;
        private ToolStripMenuItem stopper;

        private bool disposing;
        private void Dispose( bool disposing )
        {
            if( disposing && ( components != null ) ) {
                this.disposing = true;
                components.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(!disposing);
        }

        private ToolStripMenuItem[] InitializeMenu()
        {
            System.Drawing.Image img = new Bitmap("karen96.ico");
            ToolStripMenuItem[] menu = new ToolStripMenuItem[] {
                new ToolStripMenuItem("Start TheGUI", img, MenuItem_Click),
                new ToolStripMenuItem("Start Server", img, MenuItem_Click),
                new ToolStripMenuItem("Stop Server", img, MenuItem_Click),
                new ToolStripMenuItem("Exit Application", img, MenuItem_Click),
            };
            menu[0].Tag = Commands.StartTheGUI;
            menu[1].Tag = Commands.StartServer;
            menu[2].Tag = Commands.StopServing;
            menu[3].Tag = Commands.ExitTheIcon;

            starter = menu[1];
            stopper = menu[2];

            return menu;
        }

        private void MenuItem_Click( object sender, EventArgs e )
        {
            Commands com = (Commands)(sender as ToolStripMenuItem).Tag;
            if( commands[com].CanExecute )
                commands[com].Execute();
        }

        private void ApplicationExitClick()
        {
            if( commands[Commands.StopServing].CanExecute )
                commands[Commands.StopServing].Execute();
            Thread.Sleep(2000);
            Application.Exit();
        }

        private void IconMouseClick( object sender, MouseEventArgs e )
        {
            if( e.Button == MouseButtons.Left ) {
                if( panel == null )
                    panel = new DasBedienFeld( commands );
                if( panel.Visible )
                    panel.Hide();
                else {
                    panel.Show();
                    panel.Location = new Point( 1600, 550 );
                }
            }
        }

        public Program()
        {
            commands = new Commander( new Command( Commands.ExitTheIcon, ApplicationExitClick, ()=>{return true;} ));
            Initialize();
        }

        public void Initialize()
        {
            components = new System.ComponentModel.Container();
            notifyIcon = new NotifyIcon(components);
            notificationMenu = new ContextMenuStrip(components);
            notificationMenu.Items.AddRange( InitializeMenu() );
            notificationMenu.Opened += NotificationMenu_Opened;
            notifyIcon.ContextMenuStrip = notificationMenu;
            notifyIcon.Icon = new Icon("karen96.ico");
            notifyIcon.MouseClick += IconMouseClick;
            components.Add( notifyIcon );
        }

        private void NotificationMenu_Opened( object? sender, EventArgs e )
        {
            starter.Enabled = commands[Commands.StartServer].CanExecute;
            stopper.Enabled = commands[Commands.StopServing].CanExecute;
        }

        [STAThread]
        static void Main()
        {
            // Consola.StdStream.Init();

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            Program prog = new Program();
            prog.notifyIcon.Visible = true;
            Application.Run();
            prog.notifyIcon.Dispose();
        }


    }
}