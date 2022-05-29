using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Passwords.API.Xaml
{
    public class XamlView
    {
        private static string rootelm = "<StackPanel xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" HorizontalAlignment=\"Left\" Width=\"610\" Height=\"362\">\n";
        private static string rootend = "</StackPanel>";
        private static string styling = "' Margin='{2},{0},{2},0' Height='{1}' VerticalAlignment='Top' />\n";
        private static string labling = "'{0}' Margin='{1},{2},0,0' Height='{1}' VerticalAlignment='Top' Width='{3}' HorizontalAlignment='Left' />\n";
        private const int LH = 28;
        private const int SP = 10;
        public static string Frame( string content )
        {
            return $"{rootelm}{content}{rootend}";
        }

        private static string SpacingValue(int i)
        {
            return string.Format( styling, ((2*SP)+LH) + (2*i*(LH+SP)), LH, 2*LH );
        }
        private static string SpacingLabel(int i,string name)
        {
            return string.Format( labling, name, LH, SP + (2*i*(LH+SP)), (name.Length*LH) );
        }
        public static string Serialize( object obj )
        {
            System.Reflection.PropertyInfo[] props = obj.GetType().GetProperties();
            System.Text.StringBuilder docum = new StringBuilder();
            docum.Append( "<Grid>\n" );
            for (int i = 0; i < props.Length; ++i)
            {
                docum.Append( $"<Label ToolTip='{props[i].PropertyType.Name}' Content=" );
                docum.Append( SpacingLabel( i, props[i].Name ) );
                docum.Append( "<TextBox Text='" );
                docum.Append( props[i].GetMethod.Invoke( obj, Array.Empty<object>() ) );
                docum.Append( SpacingValue( i ) );
            }
            docum.Append( "</Grid>\n" );
            return docum.ToString();
        }
    }
}
