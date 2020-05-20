using GalaxyTrucker.Model;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace GalaxyTrucker.Views
{
    /// <summary>
    /// Interaction logic for TextBlockWithImagesControl.xaml
    /// </summary>
    public partial class TextBlockWithImagesControl : UserControl
    {
        private static readonly Dictionary<string, Bitmap> Images = new Dictionary<string, Bitmap>()
        {
            { Ware.Red.ToString(), Properties.Resources.ware_red},
            { Ware.Yellow.ToString(), Properties.Resources.ware_yellow},
            { Ware.Green.ToString(), Properties.Resources.ware_green},
            { Ware.Blue.ToString(), Properties.Resources.ware_blue},
            { Direction.Top.ToString(), Properties.Resources.direction_top},
            { Direction.Right.ToString(), Properties.Resources.direction_right},
            { Direction.Bottom.ToString(), Properties.Resources.direction_bottom},
            { Direction.Left.ToString(), Properties.Resources.direction_left},
            { Projectile.MeteorSmall.ToString(), Properties.Resources.meteor_small},
            { Projectile.MeteorLarge.ToString(), Properties.Resources.meteor_large},
            { Projectile.ShotSmall.ToString(), Properties.Resources.shot_small},
            { Projectile.ShotLarge.ToString(), Properties.Resources.shot_large},
        };

        private bool isBeingModified = false;

        public TextBlockWithImagesControl()
        {
            InitializeComponent();

            var dp = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
            dp.AddValueChanged(mainTextBlock, (sender, args) =>
            {
                if (isBeingModified)
                {
                    return;
                }
                isBeingModified = true;
                string content = new string(mainTextBlock.Text);
                string[] words = content.Split(' ');
                mainTextBlock.Text = "";
                foreach(string word in words)
                {
                    //replace with icon
                    if (Images.ContainsKey(word))
                    {
                        System.Drawing.Image icon = Images[word];
                        InlineUIContainer container = new InlineUIContainer();
                        Binding source = new Binding()
                        {
                            Source = icon,
                            Converter = (IValueConverter)Resources["ImageConverter"],
                        };
                        System.Windows.Controls.Image img = new System.Windows.Controls.Image
                        {
                            Height = 20,
                            Width = 20,
                            Margin = new Thickness(5)
                        };
                        BindingOperations.SetBinding(img, System.Windows.Controls.Image.SourceProperty, source);
                        container.Child = img;

                        mainTextBlock.Inlines.Add(container);
                    }
                    else
                    {
                        mainTextBlock.Inlines.Add(word + " ");
                    }
                }
                isBeingModified = false;
            });
        }
    }
}
