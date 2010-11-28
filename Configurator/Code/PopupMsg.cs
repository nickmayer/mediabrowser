using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Configurator.Code
{
    public class PopupMsg
    {
        private Window popUp;
        private TextBlock msg;

        public PopupMsg()
        {
            //Create a Window
            popUp = new Window();
            popUp.Name = "PopUp";

            //The following properties are used to create a irregular window
            popUp.AllowsTransparency = true;
            popUp.Background = Brushes.Transparent;
            popUp.WindowStyle = WindowStyle.None;

            popUp.ShowInTaskbar = false;
            popUp.Topmost = true;
            popUp.Height = 200;
            popUp.Width = 400;

            //Create a inner Grid
            Grid g = new Grid();

            //Create a Image for irregular background display
            Image img = new Image();
            img.Stretch = Stretch.Fill;
            img.Source = new BitmapImage(new Uri("pack://application:,,,/Configurator;component/Images/popup_message.png"));
            img.Effect = new System.Windows.Media.Effects.DropShadowEffect();
            g.Children.Add(img);

            //Create a TextBlock for message display
            msg = new TextBlock();
            msg.Padding = new Thickness(20);
            msg.VerticalAlignment = VerticalAlignment.Center;
            msg.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            msg.TextWrapping = TextWrapping.Wrap;
            msg.FontSize = 18;
            g.Children.Add(msg);

            popUp.Content = g;
            //Register the window's name, this is necessary for creating Storyboard using codes instead of XAML
            NameScope.SetNameScope(popUp, new NameScope());
            popUp.RegisterName(popUp.Name, popUp);

            //Create the fade in & fade out animation
            DoubleAnimationUsingKeyFrames winFadeInAni = new DoubleAnimationUsingKeyFrames();
            LinearDoubleKeyFrame keyframe = new LinearDoubleKeyFrame();
            keyframe.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2));
            keyframe.Value = 1;
            winFadeInAni.KeyFrames.Add(keyframe);
            //keyframe = new LinearDoubleKeyFrame();

            winFadeInAni.Duration = new Duration(TimeSpan.FromSeconds(4));        
            winFadeInAni.AutoReverse = true;
            winFadeInAni.AccelerationRatio = .2;
            winFadeInAni.DecelerationRatio = .7;
            winFadeInAni.Completed += delegate(object sender, EventArgs e)            //Close the window when this animation is completed
            {
                popUp.Close();
            };

            // Configure the animation to target the window's opacity property
            Storyboard.SetTargetName(winFadeInAni, popUp.Name);
            Storyboard.SetTargetProperty(winFadeInAni, new PropertyPath(Window.OpacityProperty));

            // Add the fade in & fade out animation to the Storyboard
            Storyboard winFadeInStoryBoard = new Storyboard();
            winFadeInStoryBoard.Children.Add(winFadeInAni);

            // Set event trigger, make this animation played on window.Loaded
            popUp.Loaded += delegate(object sender, RoutedEventArgs e)
            {
                winFadeInStoryBoard.Begin(popUp);
            };
            popUp.MouseLeftButtonDown += delegate(object sender, System.Windows.Input.MouseButtonEventArgs e)
            {
                popUp.Close();
            };
        }

        ~PopupMsg()
        {
            //destroy our window
            popUp = null;
        }

        public void DisplayMessage(string message, double x, double y)
        {
            msg.Text = message;
            popUp.Left = x;
            popUp.Top = y;
            popUp.Opacity = 0;
            popUp.Show();
        }

        
        
    }
}
