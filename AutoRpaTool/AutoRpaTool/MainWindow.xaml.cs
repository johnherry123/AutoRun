using System.Windows;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using System.Windows.Media;
using System.Windows.Controls;
using Nodify;
using Nodify.Events;
using AutoRpaTool.Models;
using AutoRpaTool.ViewModels;

namespace AutoRpaTool
{
    public partial class MainWindow : Window
    {
        // Node dang cho ket noi (buoc 1 da click)
        private ActionNode? _connectSource;

        public MainWindow()
        {
            InitializeComponent();

            // Fallback: xu ly Nodify PendingConnection neu drag hoat dong
            Loaded += (s, e) =>
            {
                try
                {
                    Editor.AddHandler(
                        Connector.PendingConnectionCompletedEvent,
                        new PendingConnectionEventHandler(OnNodifyConnectionCompleted));
                }
                catch { /* Neu event khong ton tai thi bo qua */ }
            };
        }

        /// <summary>
        /// Click-to-connect: buoc 1 = chon node nguon, buoc 2 = chon node dich.
        /// </summary>
        private void NodeConnect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not ActionNode clickedNode) return;
            e.Handled = true; // Khong lan ra ngoai (tranh NodifyEditor chon node)

            if (_connectSource == null)
            {
                // BUOC 1: dat node nay lam nguon, highlight vang
                _connectSource = clickedNode;
                btn.Background = new SolidColorBrush(Color.FromRgb(0xff, 0xd7, 0x00)); // vang
                btn.Foreground = new SolidColorBrush(Colors.Black);
                btn.Content = "⚡ Chon dich...";
                UpdateStatus($"Da chon nguon: {clickedNode.NodeType}. Nhan nut Ket noi tren node dich.");
            }
            else if (_connectSource == clickedNode)
            {
                // Click vao node nguon lan 2 = huy
                ResetConnectButton(btn);
                _connectSource = null;
                UpdateStatus("Da huy ket noi.");
            }
            else
            {
                // BUOC 2: ket noi tu _connectSource sang clickedNode
                if (DataContext is MainViewModel vm)
                {
                    var source = _connectSource;
                    var prev = vm.SelectedNode;
                    vm.SelectedNode = source;
                    vm.ConnectSuccessToCommand.Execute(clickedNode);
                    vm.SelectedNode = prev;
                }

                // Reset tat ca nut ket noi tren canvas
                ResetAllConnectButtons();
                _connectSource = null;
                UpdateStatus("Da ket noi thanh cong!");
            }
        }

        private void ResetConnectButton(Button btn)
        {
            btn.Background = new SolidColorBrush(Color.FromRgb(0x1e, 0x3a, 0x5f));
            btn.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xae, 0xff));
            btn.Content = "🔗 Ket noi";
        }

        private void ResetAllConnectButtons()
        {
            // Tim tat ca Button ten BtnConnect trong visual tree cua Editor
            var buttons = FindVisualChildren<Button>(Editor);
            foreach (var b in buttons)
            {
                if (b.Content?.ToString()?.Contains("Chon dich") == true ||
                    b.Content?.ToString()?.Contains("Ket noi") == true)
                    ResetConnectButton(b);
            }
        }

        private void UpdateStatus(string msg)
        {
            if (DataContext is MainViewModel vm)
                vm.ConnectSuccessToCommand.CanExecuteChanged += delegate { }; // force refresh
            // Log qua ViewModel
            if (DataContext is MainViewModel vm2)
                vm2.LogConnectStatus(msg);
        }

        // Fallback neu Nodify drag hoat dong
        private void OnNodifyConnectionCompleted(object sender, PendingConnectionEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;
            var source = e.SourceConnector as ActionNode;
            var target = e.TargetConnector as ActionNode;
            if (source == null || target == null || source == target) return;
            var prev = vm.SelectedNode;
            vm.SelectedNode = source;
            vm.ConnectSuccessToCommand.Execute(target);
            vm.SelectedNode = prev;
        }

        // Helper: tim tat ca child control theo type
        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) yield return t;
                foreach (var grandchild in FindVisualChildren<T>(child))
                    yield return grandchild;
            }
        }
    }
}