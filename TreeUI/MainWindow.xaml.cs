using DT_Algorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TreeUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Tree curTree = null;
        public MainWindow(Tree tree)
        {
            this.Activated += MainWindow_Activated;
            InitializeComponent();
            curTree = tree;
            //DrawTree(tree);
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            DrawTree(curTree);
        }

        public void DrawTree(Tree tree)
        {
            TVizualizer.Draw(tree);
        }
       
    }
}
