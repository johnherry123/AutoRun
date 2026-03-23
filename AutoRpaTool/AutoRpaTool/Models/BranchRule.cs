using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoRpaTool.Models
{
    /// <summary>
    /// Một "case" trong Branch node — tương tự một nhánh switch-case.
    /// Engine duyệt theo Priority (nhỏ trước), case nào khớp ảnh đầu tiên thì đi theo NextNodeId đó.
    /// Case có TargetImagePath = null là nhánh Default (luôn khớp nếu không case nào khớp trước).
    /// </summary>
    public class BranchRule
    {
        [Key]
        public int Id { get; set; }

        // FK về ActionNode cha
        public string NodeId { get; set; } = string.Empty;

        // Nhãn hiển thị, vd: "Thấy OK", "Thấy lỗi", "Default"
        public string Label { get; set; } = string.Empty;

        // Ảnh mẫu cần tìm. Null = nhánh Default
        public string? TargetImagePath { get; set; }

        // Node sẽ chạy tiếp nếu case này khớp
        public string? NextNodeId { get; set; }

        // Thứ tự ưu tiên (số nhỏ hơn = kiểm tra trước)
        public int Priority { get; set; } = 0;

        [ForeignKey(nameof(NodeId))]
        public ActionNode? Node { get; set; }
    }
}
