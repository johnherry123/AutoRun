using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Input; // Cần cho ICommand
using AutoRpaTool.Models;
using FlaUI.Core;
using FlaUI.UIA3;
using Microsoft.EntityFrameworkCore;

namespace AutoRpaTool
{
    public partial class MainWindow : Window
    {
        private AppDbContext _db;
        private ObservableCollection<ActionNode> _currentNodes = new ObservableCollection<ActionNode>();
        private Scenario? _currentScenario;

        // --- CÁC BIẾN QUẢN LÝ PHASE 2: DÂY NỐI ---
        // Danh sách dây nối đang hiển thị trên nền đen
        public ObservableCollection<UIConnection> CurrentConnections { get; set; } = new ObservableCollection<UIConnection>();
        // Lưu Node nào đang bị cầm chuột kéo dây đi
        public ActionNode? PendingWireSource { get; set; }
        // Lệnh chạy khi bạn thả dây vào Node khác
        public RelayCommand CreateWireCommand { get; set; }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);
        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point { public int X; public int Y; }

        public MainWindow()
        {
            InitializeComponent();
            _db = new AppDbContext();
            _db.Database.EnsureCreated();

            // Lệnh VÔ CÙNG QUAN TRỌNG giúp các Binding của Window hoạt động
            DataContext = this;

            // LOGIC KHI THẢ CHUỘT NỐI DÂY VÀO NODE KHÁC
            CreateWireCommand = new RelayCommand(target =>
            {
                var sourceNode = PendingWireSource;
                var targetNode = target as ActionNode; // Node được thả dây vào

                // Đảm bảo nối thành công từ Node A sang Node B (Không nối vào chính mình)
                if (sourceNode != null && targetNode != null && sourceNode != targetNode)
                {
                    // 1. Xóa dây cũ nếu Node A đã nối đi chỗ khác rồi (mỗi Node chỉ có 1 đầu ra Success)
                    var oldConn = CurrentConnections.FirstOrDefault(c => c.Source == sourceNode);
                    if (oldConn != null) CurrentConnections.Remove(oldConn);

                    // 2. Cập nhật ID Node tiếp theo vào Database
                    sourceNode.NextNodeOnSuccess = targetNode.NodeId;
                    _db.SaveChanges(); // Lưu ngay lập tức!

                    // 3. Vẽ dây mới lên màn hìnhLimeGreen
                    CurrentConnections.Add(new UIConnection { Source = sourceNode, Target = targetNode });
                }
            });

            LoadScenarios();
        }

        private void LoadScenarios()
        {
            lstScenarios.ItemsSource = _db.Scenarios.ToList();
        }

        private void BtnCreateScenario_Click(object sender, RoutedEventArgs e)
        {
            var newScenario = new Scenario { Name = "Kịch bản tạo lúc " + DateTime.Now.ToString("HH:mm:ss") };
            _db.Scenarios.Add(newScenario);
            _db.SaveChanges();
            LoadScenarios();
        }

        // KHI CHỌN KỊCH BẢN Ở BÊN TRÁI
        private void LstScenarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentScenario = lstScenarios.SelectedItem as Scenario;
            if (_currentScenario == null) return;

            // 1. Tải các Node lên
            var nodes = _db.ActionNodes.Where(n => n.ScenarioId == _currentScenario.Id).ToList();
            _currentNodes = new ObservableCollection<ActionNode>(nodes);
            FlowchartEditor.ItemsSource = _currentNodes;

            // 2. Tải các dây nối cũ từ Database lên màn hình
            CurrentConnections.Clear();
            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node.NextNodeOnSuccess))
                {
                    // Tìm Node đích dựa vào ID
                    var targetNode = nodes.FirstOrDefault(n => n.NodeId == node.NextNodeOnSuccess);
                    if (targetNode != null)
                    {
                        // Thêm vào list để vẽ dây LimeGreen
                        CurrentConnections.Add(new UIConnection { Source = node, Target = targetNode });
                    }
                }
            }
        }

        private void MenuAddClick_Click(object sender, RoutedEventArgs e)
        {
            if (_currentScenario == null) { MessageBox.Show("Vui lòng chọn kịch bản!"); return; }
            var newNode = new ActionNode { NodeId = Guid.NewGuid().ToString(), ScenarioId = _currentScenario.Id, NodeType = "Click Chuột", LocationX = 100, LocationY = 100 };
            _db.ActionNodes.Add(newNode);
            _db.SaveChanges();
            _currentNodes.Add(newNode);
        }

        private void MenuAddType_Click(object sender, RoutedEventArgs e)
        {
            if (_currentScenario == null) return;
            var newNode = new ActionNode { NodeId = Guid.NewGuid().ToString(), ScenarioId = _currentScenario.Id, NodeType = "Gõ Phím", LocationX = 150, LocationY = 150 };
            _db.ActionNodes.Add(newNode);
            _db.SaveChanges();
            _currentNodes.Add(newNode);
        }

        private void BtnSaveScenario_Click(object sender, RoutedEventArgs e)
        {
            _db.SaveChanges(); // Lưu mọi thứ (tọa độ, text nhập, NextNode ID)
            MessageBox.Show("Đã lưu mọi thay đổi xuống MySQL!", "Thành công");
        }

        private async void BtnPickElement_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var targetNode = button?.Tag as ActionNode;
            if (targetNode == null) return;

            this.WindowState = WindowState.Minimized;
            await Task.Delay(3000);

            using (var automation = new UIA3Automation())
            {
                Win32Point w32Pos = new Win32Point();
                GetCursorPos(ref w32Pos);
                var systemDrawingPoint = new System.Drawing.Point(w32Pos.X, w32Pos.Y);
                var element = automation.FromPoint(systemDrawingPoint);
                if (element != null)
                {
                    string targetId = element.Properties.AutomationId.ValueOrDefault ?? element.Properties.Name.ValueOrDefault;
                    if (!string.IsNullOrEmpty(targetId))
                    {
                        targetNode.TargetData = targetId;
                        _db.SaveChanges();
                        MessageBox.Show($"Bắt được Element thành công: {targetId}", "Training hoàn tất");
                    }
                }
            }
            this.WindowState = WindowState.Normal;
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Nối dây mượt mà rồi nhé. Phần sau chúng ta sẽ code Engine để tự động chạy theo sơ đồ nhé!");
        }
    }

    // --- 2 CLASS HỖ TRỢ VẼ DÂY NỐI (Đặt thẳng ở đây cho gọn) ---

    // Đại diện cho một sợi dây nối trên UI
    public class UIConnection
    {
        public ActionNode Source { get; set; } = null!;
        public ActionNode Target { get; set; } = null!;
    }

    // Lệnh xử lý kéo thả của WPF
    public class RelayCommand : ICommand
    {
        private Action<object> _execute;
        public RelayCommand(Action<object> execute) => _execute = execute;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute(parameter!);
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
}