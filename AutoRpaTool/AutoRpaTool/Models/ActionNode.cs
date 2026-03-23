using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Cần để dùng [NotMapped]
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows; // Cần để dùng Point của WPF

namespace AutoRpaTool.Models
{
    // Đại diện cho một Bước hành động (Một thẻ Node trên màn hình)
    // Kế thừa INotifyPropertyChanged để UI tự cập nhật khi dữ liệu thay đổi
    public class ActionNode : INotifyPropertyChanged
    {
        [Key]
        public string NodeId { get; set; } = string.Empty; // ID duy nhất (Guid)
        public int ScenarioId { get; set; } // Khóa ngoại link tới Scenario

        public string NodeType { get; set; } = string.Empty; // "Click", "Type"

        // Ô nhập text (ví dụ: mật khẩu, tên đăng nhập)
        private string? _inputValue;
        public string? InputValue
        {
            get => _inputValue;
            set { _inputValue = value; OnPropertyChanged(); }
        }

        // Lưu ID mục tiêu trên màn hình (bắt bằng FlaUI)
        private string? _targetData;
        public string? TargetData
        {
            get => _targetData;
            set { _targetData = value; OnPropertyChanged(); }
        }

        // Tọa độ gốc (double) để lưu xuống Database MySQL
        public double LocationX { get; set; }
        public double LocationY { get; set; }

        // Tọa độ (Point) dành riêng cho Giao diện Nodify vẽ (Bỏ qua khi lưu DB)
        [NotMapped]
        public Point Location
        {
            get => new Point(LocationX, LocationY);
            set
            {
                LocationX = value.X;
                LocationY = value.Y;
                OnPropertyChanged();
            }
        }

        // --- ĐOẠN MỚI THÊM CHO PHASE 2: Tọa độ đầu nối dây ---

        // Điểm để nhận dây nối (mặc định bên trái node)
        private Point _inputAnchor;
        [NotMapped]
        public Point InputAnchor
        {
            get => _inputAnchor;
            set { _inputAnchor = value; OnPropertyChanged(); }
        }

        // Điểm để kéo dây đi (mặc định bên phải node)
        private Point _outputAnchor;
        [NotMapped]
        public Point OutputAnchor
        {
            get => _outputAnchor;
            set { _outputAnchor = value; OnPropertyChanged(); }
        }

        // --- ĐOẠN MỚI THÊM CHO PHASE 2: ID của Node tiếp theo ---
        // Nối dây thì giá trị này sẽ được cập nhật
        public string? NextNodeOnSuccess { get; set; }
        public string? NextNodeOnFail { get; set; }

        public Scenario? Scenario { get; set; } // Navigation property

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}