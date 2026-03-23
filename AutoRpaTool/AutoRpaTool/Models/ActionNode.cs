using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AutoRpaTool.Models
{
    /// <summary>
    /// Các loại node trong flow kịch bản
    /// </summary>
    public enum NodeType
    {
        Click,      // Tìm ảnh → click chuột vào vị trí tìm được
        Type,       // Tìm ảnh (focus) → gõ phím
        Wait,       // Chờ một khoảng ms
        Branch      // Switch-case: kiểm tra nhiều điều kiện ảnh → rẽ nhánh
    }

    public partial class ActionNode : ObservableObject
    {
        [Key]
        public string NodeId { get; set; } = string.Empty;
        public int ScenarioId { get; set; }

        public NodeType NodeType { get; set; } = NodeType.Click;

        // Tên hiển thị do người dùng đặt
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayLabel))]
        private string? _displayName;

        // Nhãn hiển thị tự động: DisplayName nếu có, nếu không = "NodeType (ID ngắn)"
        [NotMapped]
        public string DisplayLabel =>
            !string.IsNullOrWhiteSpace(DisplayName)
                ? DisplayName
                : $"{NodeType} ({(NodeId.Length > 6 ? NodeId[..6] : NodeId)})";

        [ObservableProperty]
        private bool _isStartNode;

        // Ảnh mẫu để tìm kiếm trên màn hình (dùng cho Click, Type)
        [ObservableProperty]
        private string? _targetImagePath;

        // Nội dung gõ phím (dành cho Type node)
        [ObservableProperty]
        private string? _inputValue;

        // Độ khớp tối thiểu của OpenCV (0.0 - 1.0), mặc định 0.85
        public double MatchThreshold { get; set; } = 0.85;

        // Offset điểm click so với ảnh mẫu (0.0–1.0, 0.5=tâm)
        public double ClickOffsetX { get; set; } = 0.5;
        public double ClickOffsetY { get; set; } = 0.5;

        // Thời gian chờ (ms) — dành cho Wait node
        public int WaitMs { get; set; } = 1000;

        // Routing cho node không phải Branch
        public string? NextNodeOnSuccess { get; set; }
        public string? NextNodeOnFail { get; set; }

        // Tọa độ node trên canvas
        public double LocationX { get; set; }
        public double LocationY { get; set; }

        [NotMapped]
        public System.Windows.Point Location
        {
            get => new(LocationX, LocationY);
            set
            {
                LocationX = value.X;
                LocationY = value.Y;
                OnPropertyChanged(nameof(Location));
            }
        }

        // Anchors cho dây nối Nodify (không lưu DB)
        [ObservableProperty]
        [property: NotMapped]
        private System.Windows.Point _inputAnchor;

        [ObservableProperty]
        [property: NotMapped]
        private System.Windows.Point _outputAnchor;

        // Các case của Branch node
        public ObservableCollection<BranchRule> BranchRules { get; set; } = new();

        public Scenario? Scenario { get; set; }
    }
}