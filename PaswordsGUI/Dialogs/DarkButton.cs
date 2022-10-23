using System.Windows.Controls;
using System.Windows.Input;


namespace Passwords.GUI.Dialogs
{
    public class DarkButton : Button
    {
        private ControlColors ColorSet;

        public DarkButton() : base()
        {
            ColorSet = new ControlColors( new uint[] {
                0xff404040,0xff181818,0xFFF5DEB3,
                0xff1e1e1e,0xff404040,0xFFF5DEB3,
                0xff202020,0xff404040,0xffffefde}
            );

            Background = ColorSet.Background;
            BorderBrush = ColorSet.BorderBrush;
            Foreground = ColorSet.Foreground;

            IsEnabledChanged += DarkButton_IsEnabledChanged;
        }

        private void UpdateColorState( ControlColorsState newstate )
        {
            ColorSet.State = newstate;
            Background = ColorSet.Background;
            BorderBrush = ColorSet.BorderBrush;
            Foreground = ColorSet.Foreground;
            InvalidateVisual();
        }

        private void DarkButton_IsEnabledChanged( object sender, System.Windows.DependencyPropertyChangedEventArgs e )
        {
            DarkButton button = sender as DarkButton;
            if((bool)e.NewValue) {
                UpdateColorState( ControlColorsState.Enabled );
            } else {
                UpdateColorState( ControlColorsState.Disable );
            }
        }

        protected override void OnMouseEnter( MouseEventArgs e )
        {
            base.OnMouseEnter(e);
            if( IsEnabled ) UpdateColorState( ControlColorsState.Hovered );
        }

        protected override void OnMouseLeave( MouseEventArgs e )
        {
            base.OnMouseLeave(e);
            UpdateColorState( IsEnabled 
                            ? ControlColorsState.Enabled
                            : ControlColorsState.Disable );
        }
    }
}
