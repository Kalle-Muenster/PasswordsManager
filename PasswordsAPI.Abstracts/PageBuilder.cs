using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Passwords.API.Xaml
{
    public class XamlView
    {
        private static string rootelm = "<StackPanel xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" HorizontalAlignment=\"Left\" Width=\"auto\" Height=\"auto\">\n";
        private static string rootend = "</StackPanel>";
        private static string styling = "' Margin='{2},{0},{2},0' Height='{1}' VerticalAlignment='Top' />\n";
        private static string labling = "'{0}' Margin='{1},{2},0,0' Height='{1}' VerticalAlignment='Top' Width='{3}' HorizontalAlignment='Left' />\n";
        private const int LH = 28;
        private const int SP = 10;

        private static string SpacingValue( int i )
        {
            return string.Format(styling, ( ( 2 * SP ) + LH ) + ( 2 * i * ( LH + SP ) ), LH, 2 * LH);
        }
        private static string SpacingLabel( int i, string name )
        {
            return string.Format(labling, name, LH, SP + ( 2 * i * ( LH + SP ) ), ( name.Length * LH ));
        }

        public static string StackPanel( string content )
        {
            return $"{rootelm}{content}{rootend}";
        }

        private static string StringValue( object prop )
        {
            return prop.GetType() == typeof(byte[])
                 ? Encoding.ASCII.GetString( (byte[])prop )
                 : prop.ToString();
        }

        public static string SerializeGroup( object obj )
        {
            System.Reflection.PropertyInfo[] props = obj.GetType().GetProperties();
            StringBuilder docum = new StringBuilder();
            docum.Append("<GroupBox Header='").Append(obj.GetType().Name).Append("' Orientation='Vertical'>\n");
            for( int i = 0; i < props.Length; ++i ) {
                docum.Append("<Rectangle Orientation='Horizontal'>\n<Label Content='");
                docum.Append(props[i].Name).Append("' HorizontalAlignment='Left' ToolTip='");
                docum.Append(props[i].PropertyType.Name).Append("' />\n<TextBox Text='").Append(
                    StringValue( props[i].GetMethod.Invoke(obj, Array.Empty<object>()) )
                ).Append("' HorizontalAlignment='Right' />\n</Rectangle>\n");
            } docum.Append("</GroupBox>\n");
            return docum.ToString();
        }

        public static string SerializeGrid( object obj )
        {
            System.Reflection.PropertyInfo[] props = obj.GetType().GetProperties();
            System.Text.StringBuilder docum = new StringBuilder();
            docum.Append( "<Grid>\n" );
            for (int i = 0; i < props.Length; ++i) {
                docum.Append( $"<Label ToolTip='{props[i].PropertyType.Name}' Content=" );
                docum.Append( SpacingLabel( i, props[i].Name ) );
                docum.Append( "<TextBox Text='" );
                docum.Append( StringValue( props[i].GetMethod.Invoke( obj, Array.Empty<object>() ) ) );
                docum.Append( SpacingValue( i ) );
            } docum.Append( "</Grid>\n" );
            return docum.ToString();
        }
    }
}
