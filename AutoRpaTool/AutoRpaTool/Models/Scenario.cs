using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AutoRpaTool.Models
{
    public class Scenario
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Tiêu đề cửa sổ cần đang active trước khi chạy (để kiểm tra điều kiện)
        public string? TriggerWindowTitle { get; set; }

        public List<ActionNode> Nodes { get; set; } = new();
    }
}
