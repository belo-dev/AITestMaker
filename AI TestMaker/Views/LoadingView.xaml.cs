using System;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace AI_TestMaker.Views
{
    public partial class LoadingView : UserControl
    {
        public LoadingView()
        {
            InitializeComponent();
        }

        public void FadeOutAndRemove()
        {
            var anim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
            anim.Completed += (s, e) =>
            {
                if (this.Parent is Panel parentPanel)
                    parentPanel.Children.Remove(this);
            };

            this.BeginAnimation(OpacityProperty, anim);
        }
    }
}