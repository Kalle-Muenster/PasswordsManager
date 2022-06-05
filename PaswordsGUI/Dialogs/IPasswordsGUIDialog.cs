using Passwords.API.Abstracts;
using System;
using System.Windows;

namespace Passwords.GUI
{
    public enum DialogReturnState
    {
        Canceled = 0, Ok = 1
    }

    public class TheReturnData<T> 
        : EventArgs 
        where T : IEntityBase
    {
        public T Data;
        public ThePasswords_TheGUI_ItsDialogs_TheInterface Dialog;
        public bool Ok { get { return Data.Is().Status.Ok; } }
        public bool Canceled { get { return Data.Is().Status.Bad; } }
        public TheReturnData( T data, ThePasswords_TheGUI_ItsDialogs_TheInterface dialog )
        {
            Data = data;
            Dialog = dialog;
        }

        public static TheReturnData<T> fromTheDialog( ThePasswords_TheGUI_ItsDialogs_TheInterface cast )
        {
            return new TheReturnData<T>( (T)cast.theDialog().TheData, cast );
        }
    }

    public interface ThePasswords_TheGUI_ItsDialogs_TheInterface
    {
        DialogReturnState Status { get; }
        ThePasswords_TheGUI_ItsDialogs_TheInterface theDialog();
        IEntityBase TheData { get; set; }
        ThePasswords_TheAPI_TheGUI TheGUI { get; set; }
        
        void Show();
        void Hide();

        void Returns();

        void TheReturnAction<T>(IThePasswords_TheGUI_ADialog<T>.ItsReturnAction onProcced)
            where T : IEntityBase;
    }

    public interface IThePasswords_TheGUI_ADialog<ItsData> 
        : ThePasswords_TheGUI_ItsDialogs_TheInterface
    where ItsData 
        : IEntityBase
    {
        public delegate void ItsReturnAction( TheReturnData<ItsData> reply );
        ItsReturnAction TheAction { get; set; }
    }
}
