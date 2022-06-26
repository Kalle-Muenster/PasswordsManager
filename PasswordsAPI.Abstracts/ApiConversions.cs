using System;
using Stepflow;
using System.Runtime.InteropServices;
using Passwords.API.Abstracts;

namespace Passwords.API.Extensions
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 8)]
    public struct ReInterpret
    {
        [FieldOffset(0)] public double  AsFloat64;
        [FieldOffset(0)] public float   AsFloat32;
        [FieldOffset(0)] public long   AsSigned64;
        [FieldOffset(0)] public int    AsSigned32;
        [FieldOffset(0)] public Int24  AsSigned24;
        [FieldOffset(0)] public short  AsSigned16;
        [FieldOffset(0)] internal ulong      data;
        [FieldOffset(0)] internal unsafe fixed byte bytes[8];

        public bool IsNegative {
            get { unsafe { bool isneg = false;
                    fixed( void* ptr = bytes ) {
                        sbyte* chrs = (sbyte*)ptr;
                        if( chrs[7] < 0 ) return true;
                        for( int i = 7; i >= 0; --i ) {
                            if(chrs[i] != 0) {
                                isneg = chrs[i] < 0;
                                break;
                            }
                        }
                    } return isneg;
                }
            }
        }

        public new string ToString {
            get { unsafe { fixed ( void* ptr = bytes )
                  return new String( (sbyte*)ptr, 0, 8 ).Trim(); } 
            }
        }

        public ReInterpret( byte[] data ) : this()
        {
            if( data == null ) this.data = 0; else 
            if( data.Length > 8 ) AsSigned64 = -1; else
            for( int i = 0; i < data.Length; ++i ) unsafe {
                bytes[i] = data[i];
            }
        }
        
        public ReInterpret( short value ) : this() { AsSigned16 = value; }
        public ReInterpret( int value ) : this() { AsSigned32 = value; }
        public ReInterpret( Int24 value ) : this() { AsSigned24 = value; }
        public ReInterpret( long value ) : this() { AsSigned64 = value; }
        public ReInterpret( float value ) : this() { AsFloat32 = value; }
        public ReInterpret( double value ) : this() { AsFloat64 = value; }
        public ReInterpret( string data ) : this( System.Text.Encoding.Default.GetBytes(data) ) {}

        public static ReInterpret Cast( short value ) {
            return new ReInterpret( value );
        }
        public static ReInterpret Cast( int value ) {
            return new ReInterpret( value );
        }
        public static ReInterpret Cast( Int24 value ) {
            return new ReInterpret( value );
        }
        public static ReInterpret Cast( long value ) {
            return new ReInterpret( value );
        }
        public static ReInterpret Cast( float value ) {
            return new ReInterpret( value );
        }
        public static ReInterpret Cast( double value ) {
            return new ReInterpret( value );
        }
        public static ReInterpret Cast( string data ) {
            return new ReInterpret( data );
        }
        public static ReInterpret Cast( byte[] data ) {
            return new ReInterpret( data );
        }
    }

    public static class NumericValue
    {
        public static readonly char DecimalSeparator =
            System.Globalization.CultureInfo.CurrentCulture
           .NumberFormat.CurrencyDecimalSeparator[0];

        public static byte[] GetBytes( object any )
        {
            byte[] data = Array.Empty<byte>();
            if (any == null) return data;
            if (any is byte[]) return any as byte[];
            ReInterpret converse = new ReInterpret(0);
            if ( any is Enum ) unsafe {
                converse.AsSigned64 = Convert.ToInt64( any as Enum );
            } else {
                string rep = any is string ? any as string : $"{any}";
                if (rep.Contains('.') || rep.Contains(',')) {
                    rep.Replace('.', DecimalSeparator);
                    rep.Replace(',', DecimalSeparator);
                    if ( double.TryParse( rep, out converse.AsFloat64 ) )
                        if ( converse.AsFloat64 == 0.0 ) return new byte[8];
                } else {
                    if ( long.TryParse( rep, out converse.AsSigned64 ) )
                        if ( converse.AsSigned64 == 0 ) return new byte[4];
                }
            } bool seemsNegative = converse.IsNegative;
            for ( int i = 7; i >= 0; --i ) unsafe {
                if ( seemsNegative ? converse.bytes[i] < 0 : converse.bytes[i] > 0 ) {
                    data = new byte[i+1]; break; }
            } for ( int i = 0; i < data.Length; ++i ) unsafe {
                data[i] = converse.bytes[i];
            } return data;
        }

        public static long GetInteger( byte[] data )
        {
            if ( data == null ) { return (long)Convert.DBNull; }
            switch( data.Length ) {
                case 0: return (long)Convert.DBNull;
                case 1: return (sbyte)data[0];
                case 2: return ReInterpret.Cast(data).AsSigned16;
                case 3: return ReInterpret.Cast(data).AsSigned24;
                case 4: return ReInterpret.Cast(data).AsSigned32;
               default: return ReInterpret.Cast(data).AsSigned64;
            }
        }

        public static double GetDecimal( byte[] data )
        {
            if ( data == null ) { return (double)Convert.DBNull; }
            switch ( data.Length ) {
                case 0: return (double)Convert.DBNull;
                case 1:
                case 2:
                case 3:
                case 4: return ReInterpret.Cast( data ).AsFloat32;
               default: return ReInterpret.Cast( data ).AsFloat64;
            }
        }
    }

    public static class Extensions
    {
        public static Int32 ToInt32( this ResultCode value )
        {
            return Convert.ToInt32(value);
        }

        public static UInt32 ToUInt32( this ResultCode value )
        {
            return Convert.ToUInt32(value);
        }

        public static ResultCode ToError( this Int32 value )
        {
            return (ResultCode)value;
        }

        public static EntityBase<E> SetError<E>( this Status message ) where E : EntityBase<E>, new()
        {
            return new EntityBase<E>(message);
        }

        public static int ToHttpStatusCode( this ResultCode code )
        {
            return (int)( ResultCode.WebMask & code ) * 100;
        }

        public static Int32 ToFourCC( this string data )
        {
            return ReInterpret.Cast(data).AsSigned32;
        }

        public static Int64 ToLongCC( this string data )
        {
            return ReInterpret.Cast(data).AsSigned64;
        }
    }
}
