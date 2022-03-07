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

        public static EntityBase SetError( this Error message )
        {
            return new EntityBase( message );
        }
    }

    [Flags]
    public enum ErrorCode : uint
    {
        NoError = 0,
        Unknown = 1,

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

    public struct Error
    {
        public static readonly Error NoError = new Error(ErrorCode.NoError);
        public static readonly Error Unknown = new Error(ErrorCode.Unknown);
        public static readonly Error Invalid = new Error(ErrorCode.Invalid);
        public static readonly Error Cryptic = new Error(ErrorCode.Cryptic|ErrorCode.Data,"Data Encrypted");
        public static readonly Error Service = new Error(ErrorCode.Service);

        public readonly ErrorCode Code;
        public readonly string    Text;
        public readonly object    Data;

        public Error( ErrorCode code ) : this( code, code.ToString() )
        { }
        public Error( ErrorCode code, string text )
        {
            Code = code;
            Text = text.Contains( "{0}" ) ? text : text + " {0}";
            Data = String.Empty;
        }
        public Error( ErrorCode code, string text, object data )
            : this( code, text )
        { Data = data; }

        private Error( in Error error, object with )
            : this( error.Code, error.Text, with )
        {}

        public Error WithData( object with )
        {
            return new Error( this, with );
        }

        public Error WithText( string text )
        {
            return new Error( Code, text.Contains("{0}") 
                            ? text : text + " {0}"
                            , Data );
        }

        public static implicit operator bool( Error cast )
        {
            return cast.Code != 0;
        }

        public static implicit operator string( Error cast )
        {
            return string.Format( cast.Text, cast.Data );
        }

        public override string ToString()
        {
            return string.Format( $"Error-[{Code}]: {Text}", Data );
        }

        public static Error operator + ( Error own, Error add )
        {
            object data = add.Data;
            if( data == String.Empty && own.Data != String.Empty )
                data = own.Data;

            return new Error(
                own.Code | add.Code,
                $"{add.Text} => {own.Text}",
                data
            );
        }

        public static Error operator + ( Error error, ErrorCode code )
        {
            return new Error( error.Code|code, error.Text, error.Data );
        }

    }
}
