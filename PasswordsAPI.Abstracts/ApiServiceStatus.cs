using System;
using Passwords.API.Extensions;

namespace Passwords.API.Abstracts
{


    [Flags]
    public enum ResultCode : uint
    {
        NoState = 0,
        Unknown = 1,
        Success = 2,
        IsError = 4,
        Service = 5,
        IsValid = 0xff000007,
        WebMask = 0x00000007,
        Invalid = 0x01000000,
        Missing = 0x02000000,
        Cryptic = 0x04000000,

        User    = 0x00000100,
        Area    = 0x00000200,
        Password = 0x00000400,
        Json    = 0x00000800,
        Html    = 0x00001000,
        Empty   = 0x00002000,
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
        private readonly string    text;
        public readonly object     Data;

        public int Http {
            get { return Code.ToHttpStatusCode(); }
        }

        public string Text {
            get { return ToString(); }
        }

        public string GetText()
        {
            return text;
        }

        public Status( ResultCode code )
            : this( code, code.ToString() )
        { }
        public Status( ResultCode code, string text )
        {
            Code = code;
            this.text = text.Contains("{0}") ? text : text + " {0}";
            Data = String.Empty;
        }
        public Status( ResultCode code, string text, object data )
        {
            Code = code;
            this.text = text.Contains("{0}") ? text : text + " {0}";
            Data = data;
        }
        private Status( in Status status, object with )
        {
            Code = status.Code;
            text = status.text;
            Data = with;
        }


        public Status WithData( object with )
        {
            return new Status( Code, Text, with );
        }

        public Status WithText( string text )
        {
            return new Status( Code, text.Contains("{0}")
                             ? text : text + " {0}"
                             , Data);
        }

        public static implicit operator bool( Status cast )
        {
            return (cast.Code & ResultCode.IsValid) <= (ResultCode.Unknown|ResultCode.Success) && cast.Code != 0;
        }

        public System.Diagnostics.EventLogEntryType Event {
            get { switch( Result ) {
                    case ResultState.Status: return System.Diagnostics.EventLogEntryType.Information;
                    case ResultState.Success: return System.Diagnostics.EventLogEntryType.SuccessAudit;
                    case ResultState.Waiting: return System.Diagnostics.EventLogEntryType.Warning;
                    case ResultState.Error: return System.Diagnostics.EventLogEntryType.Error;
                    default: return System.Diagnostics.EventLogEntryType.FailureAudit;
                }
            }
        }

        public ResultState Result {
            get { ResultCode masked = Code & ResultCode.IsValid;
            return masked >= ResultCode.IsError
                 ? ResultState.Error
                 : masked < ResultCode.Success
                 ? ResultState.Status  
                 : ResultState.Success;
            }
        }

        public override string ToString()
        {
            return string.Format($"{Result}-[{Code.ToUInt32()}]: {text}", Data);
        }

        public static implicit operator string(Status cast)
        {
            return string.Format($"{cast.Result}-[{cast.Code}]: {cast.text}", cast.Data);
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
                $"{add.text} => {own.text}",
                data
            );
        }

        public static Status operator + ( Status status, ResultCode code )
        {
            return new Status( status.Code|code, status.text, status.Data.ToString() );
        }

        public bool Bad {
            get { return (int)( Code & ResultCode.IsValid ) >
                         ( Code.HasFlag(ResultCode.Success) ? 3 : 0 );
            }
        }

        public bool Ok {
            get { return ( ( Code & ResultCode.IsValid ) > ResultCode.Unknown ) 
                      && ( ( Code & ResultCode.IsValid ) < ResultCode.IsError );
            }
        }

        public bool Intermediate {
            get { ResultCode check = Code & ResultCode.IsValid;
                return ( check < ResultCode.IsError )
                      && check.HasFlag(ResultCode.Unknown)
                   && ( !check.HasFlag(ResultCode.Success) );
            }
        }
    }
}
