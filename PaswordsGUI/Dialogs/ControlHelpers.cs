<<<<<<< HEAD
using System.Windows.Media;
=======
﻿using System.Windows.Media;
>>>>>>> refs/remotes/fork/main

namespace Passwords.GUI.Dialogs
{
    public struct ColorStyle
    {
        public readonly Color Back,Bord,Fore;
        public ColorStyle( Color back, Color bord, Color fore )
        {
            Back = back;
            Bord = bord;
            Fore = fore;
        }
    }

    public enum ControlColorsState
    {
        Enabled, Disable, Hovered, Clicked
    }

    public class ControlColors
    {
        private static Color ColorValue( uint value )
        {
            return Color.FromArgb(
                (byte)( ( value & 0xff000000 ) >> 24 ),
                (byte)( ( value & 0x00ff0000 ) >> 16 ),
                (byte)( ( value & 0x0000ff00 ) >> 8 ),
                  (byte)( value & 0x000000ff )
            );
        }

        private ColorStyle EnabledState;
        private ColorStyle DisableState;
        private ColorStyle HoveredState;
        private ColorStyle ClickedState;
        private ControlColorsState state;

        public SolidColorBrush Background;
        public SolidColorBrush BorderBrush;
        public SolidColorBrush Foreground;

        public ControlColorsState State {
            get { return state; }
            set {
                if( value != state ) {
                    switch( value ) {
                        case ControlColorsState.Enabled:
                        Background.Color = EnabledState.Back;
                        BorderBrush.Color = EnabledState.Bord;
                        Foreground.Color = EnabledState.Fore;
                        break;
                        case ControlColorsState.Disable:
                        Background.Color = DisableState.Back;
                        BorderBrush.Color = DisableState.Bord;
                        Foreground.Color = DisableState.Fore;
                        break;
                        case ControlColorsState.Hovered:
                        Background.Color = HoveredState.Back;
                        BorderBrush.Color = HoveredState.Bord;
                        Foreground.Color = HoveredState.Fore;
                        break;
                        case ControlColorsState.Clicked:
                        Background.Color = ClickedState.Back;
                        BorderBrush.Color = ClickedState.Bord;
                        Foreground.Color = ClickedState.Fore;
                        break;
                    }
                    state = value;
                }
            }
        }

        public ControlColors( uint[] colors )
        {
            EnabledState = new ColorStyle(ColorValue(colors[0]), ColorValue(colors[1]), ColorValue(colors[2]));
            DisableState = new ColorStyle(ColorValue(colors[3]), ColorValue(colors[4]), ColorValue(colors[5]));
            if( colors.Length >= 9 )
                HoveredState = new ColorStyle(ColorValue(colors[6]), ColorValue(colors[7]), ColorValue(colors[8]));
            else
                HoveredState = EnabledState;
            if( colors.Length >= 12 )
                ClickedState = new ColorStyle(ColorValue(colors[9]), ColorValue(colors[10]), ColorValue(colors[11]));
            else
                ClickedState = HoveredState;

            Background = new SolidColorBrush(EnabledState.Back);
            BorderBrush = new SolidColorBrush(EnabledState.Bord);
            Foreground = new SolidColorBrush(EnabledState.Fore);

            state = ControlColorsState.Enabled;
        }
    }
}
