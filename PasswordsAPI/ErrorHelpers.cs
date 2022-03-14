using System;

namespace PasswordsAPI
{
    public static class Extensions
    {
        public static Int32 ToInt32( this ErrorCode value )
        {
            return Convert.ToInt32( value );
        }

        public static UInt32 ToUInt32( this ErrorCode value )
        {
            return Convert.ToUInt32( value );
        }

        public static ErrorCode ToError( this Int32 value )
        {
            return (ErrorCode)value;
        }

        public static EntityBase SetError( this Status message )
        {
            return new EntityBase( message );
        }
    }

    [Flags]
    public enum ErrorCode : uint
    {
        NoError = 0,
        Unknown = 2,
        Success = 1,
    //    Waiting = 2,

        IsValid = 0xff000003,
        
        Invalid = 0x01000000,
        Cryptic = 0x02000000,

        Service = 0x00010000,

        User    = 0x00000100,
        Area    = 0x00000200,
        Word    = 0x00000400,
        
        Id      = 0x00020000,
        Data    = 0x00040000,
        Name    = 0x00080000,
        Mail    = 0x00100000,
        Info    = 0x00200000,
        Icon    = 0x00400000
    }

    public struct Status
    {
        public static readonly Status NoError = new Status(ErrorCode.NoError);
        public static readonly Status Unknown = new Status(ErrorCode.Unknown);
        public static readonly Status Success = new Status(ErrorCode.Success);
        public static readonly Status Invalid = new Status(ErrorCode.Invalid);
        public static readonly Status Cryptic = new Status(ErrorCode.Cryptic|ErrorCode.Data,"Data Encrypted");
        public static readonly Status Service = new Status(ErrorCode.Service);

        public readonly ErrorCode Code;
        public readonly string    Text;
        public readonly object    Data;


        public Status( ErrorCode code )
            : this( code, code.ToString() )
        { }
        public Status( ErrorCode code, string text )
        {
            Code = code;
            Text = text.Contains( "{0}" ) ? text : text + " {0}";
            Data = String.Empty;
        }
        private Status( in Status status, object with )
            : this( status.Code, status.Text, with )
        { }
        public Status( ErrorCode code, string text, object data )
            : this( code, text )
        { Data = data; }



        public Status WithData( object with )
        {
            return new Status( this, with );
        }

        public Status WithText( string text )
        {
            return new Status( Code, text.Contains("{0}") 
                             ? text : text + " {0}"
                             , Data );
        }

        public static implicit operator bool( Status cast )
        {
            return cast.Code != 0;
        }

        public static implicit operator string( Status cast )
        {
            return string.Format( cast.Text, cast.Data );
        }

        public override string ToString()
        {
            ErrorCode masked = Code & ErrorCode.IsValid;
            string status = masked < ErrorCode.Unknown 
              ? "Success" : masked > ErrorCode.Unknown
                ? "Error" : "Status";

            return string.Format( $"{status}-[{Code}]: {Text}", Data );
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

        public static Status operator + ( Status status, ErrorCode code )
        {
            return new Status( status.Code|code, status.Text, status.Data );
        }

        public bool Bad
        {
            get { return (Code & ErrorCode.IsValid) > ErrorCode.Success; }
        }

        public bool Ok
        {
            get { return (Code & ErrorCode.IsValid) < ErrorCode.Unknown; }
        }

        public bool Waiting
        {
            get { return Code.HasFlag( ErrorCode.Unknown ); }
        }
    }
}
