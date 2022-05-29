using System;

namespace Passwords.API.Abstracts
{
    public static class Extensions
    {
        public static Int32 ToInt32( this ResultCode value )
        {
            return Convert.ToInt32( value );
        }

        public static UInt32 ToUInt32( this ResultCode value )
        {
            return Convert.ToUInt32( value );
        }

        public static ResultCode ToError( this Int32 value )
        {
            return (ResultCode)value;
        }

        public static EntityBase<E> SetError<E>( this Status message ) where E : EntityBase<E>, new()
        {
            return new EntityBase<E>( message );
        }
    }

    [Flags]
    public enum ResultCode : uint
    {
        NoState = 0,
        Success = 1,
        Unknown = 2,
        IsError = 4,
        IsValid = 0xff000007,
        
        Invalid = 0x01000000,
        Cryptic = 0x02000000,

        Service = 0x00010000,

        User    = 0x00000100,
        Area    = 0x00000200,
        Password = 0x00000400,
        Json    = 0x00000800,
        Html    = 0x00001000,
        Id      = 0x00020000,
        Data    = 0x00040000,
        Name    = 0x00080000,
        Mail    = 0x00100000,
        Info    = 0x00200000,
        Icon    = 0x00400000,
        Xaml    = 0x00800000,
    }

    public enum ResultState : uint
    {
        Status,Success,Waiting,Error
    }

    public struct Status
    {
        public static readonly Status NoState = new Status(ResultCode.NoState);
        public static readonly Status Unknown = new Status(ResultCode.Unknown);
        public static readonly Status Success = new Status(ResultCode.Success);
        public static readonly Status Invalid = new Status(ResultCode.Invalid);
        public static readonly Status Cryptic = new Status(ResultCode.Cryptic|ResultCode.Data,"Data Encrypted");
        public static readonly Status Service = new Status(ResultCode.Service);
        public static readonly Status Waiting = new Status(ResultCode.Success|ResultCode.Unknown);


        public readonly ResultCode Code;
        public readonly string     Text;
        public readonly object     Data;


        public Status( ResultCode code )
            : this( code, code.ToString() )
        { }
        public Status( ResultCode code, string text )
        {
            Code = code;
            Text = text.Contains("{0}") ? text : text + " {0}";
            Data = String.Empty;
        }
        public Status( ResultCode code, string text, object data )
        {
            Code = code;
            Text = text.Contains("{0}") ? text : text + " {0}";
            Data = data;
        }
        private Status(in Status status, object with)
        {
            Code = status.Code;
            Text = status.Text;
            Data = with;
        }


        public Status WithData( object with )
        {
            return new Status( Code, Text, with );
        }

        public Status WithText( string text )
        {
            return new Status( Code, text, Data );
        }

        public static implicit operator bool( Status cast )
        {
            return (cast.Code & ResultCode.IsValid) <= (ResultCode.Unknown|ResultCode.Success) && cast.Code != 0;
        }

        public ResultState Result {
            get { ResultCode masked = Code & ResultCode.IsValid;
            return masked >= ResultCode.IsError
                 ? ResultState.Error
                 : ((masked.HasFlag(ResultCode.Unknown) && !masked.HasFlag(ResultCode.Success))||(masked == 0))
                 ? ResultState.Status  
                 : ResultState.Success;
            }
        }

        public override string ToString()
        {
            return string.Format($"{Result}-[{Code.ToUInt32()}]: {Text}", Data);
        }

        public static implicit operator string(Status cast)
        {
            return string.Format($"{cast.Result}-[{cast.Code}]: {cast.Text}", cast.Data);
        }

        public static string MessageFromStatusFlags( uint flags )
        {
            return ((ResultCode) flags).ToString();
        } 

        public static Status operator + ( Status own, Status add )
        {
            object data = add.Data;
            if( data == String.Empty && own.Data != String.Empty )
                data = own.Data;

            return new Status(
                own.Code | add.Code,
                $"{add.Text} => {own.Text}",
                data
            );
        }

        public static Status operator + ( Status status, ResultCode code )
        {
            return new Status( status.Code|code, status.Text, status.Data.ToString() );
        }

        public bool Bad
        {
            get { return (int)(Code & ResultCode.IsValid) >
                         (Code.HasFlag(ResultCode.Success) ? 3 : 1); }
        }

        public bool Ok
        {
            get { return (Code > 0) && ( (Code & ResultCode.IsValid) < ResultCode.IsError ) 
                                    && ( Code.HasFlag(ResultCode.Success) 
                                       | !Code.HasFlag(ResultCode.Unknown) ); }
        }

        public bool Intermediate {
            get {
                ResultCode check = Code & ResultCode.IsValid;
                return (check < ResultCode.IsError) && check.HasFlag( ResultCode.Unknown ) && (!check.HasFlag(ResultCode.Success));
            }
        }
    }
}
