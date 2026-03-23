using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AutoRpaTool.Models;

namespace AutoRpaTool.Services
{
    /// <summary>
    /// Engine chạy kịch bản RPA theo flow node, hỗ trợ switch-case branching.
    /// </summary>
    public class RpaEngine
    {
        private readonly ImageMatchingService _matcher;
        private readonly InputService _input;

        public event Action<string>? OnLog; // Ghi log ra UI

        #region P/Invoke kiểm tra cửa sổ active
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
        #endregion

        public RpaEngine(ImageMatchingService matcher, InputService input)
        {
            _matcher = matcher;
            _input = input;
        }

        public async Task RunAsync(Scenario scenario, List<ActionNode> nodes, CancellationToken ct = default)
        {
            // --- Kiểm tra điều kiện cửa sổ ---
            if (!string.IsNullOrWhiteSpace(scenario.TriggerWindowTitle))
            {
                var sb = new System.Text.StringBuilder(256);
                GetWindowText(GetForegroundWindow(), sb, 256);
                string activeTitle = sb.ToString();

                if (!activeTitle.Contains(scenario.TriggerWindowTitle, StringComparison.OrdinalIgnoreCase))
                {
                    Log($"❌ Cửa sổ active '{activeTitle}' không khớp với yêu cầu '{scenario.TriggerWindowTitle}'. Dừng lại.");
                    return;
                }
            }

            // --- Tìm node bắt đầu ---
            var current = nodes.FirstOrDefault(n => n.IsStartNode);
            if (current == null)
            {
                Log("❌ Không tìm thấy node StartNode. Hãy tích 'Bắt đầu' cho một node.");
                return;
            }

            Log($"▶ Bắt đầu chạy kịch bản: {scenario.Name}");

            while (current != null && !ct.IsCancellationRequested)
            {
                Log($"→ Thực thi node [{current.NodeType}] — {current.NodeId[..8]}...");

                string? nextId = null;

                switch (current.NodeType)
                {
                    case NodeType.Click:
                        nextId = await ExecuteClickAsync(current);
                        break;

                    case NodeType.Type:
                        nextId = await ExecuteTypeAsync(current);
                        break;

                    case NodeType.Wait:
                        Log($"  ⏳ Chờ {current.WaitMs}ms...");
                        await Task.Delay(current.WaitMs, ct);
                        nextId = current.NextNodeOnSuccess;
                        break;

                    case NodeType.Branch:
                        nextId = await ExecuteBranchAsync(current);
                        break;
                }

                if (nextId == null)
                {
                    Log("✅ Kịch bản hoàn tất (không còn node tiếp theo).");
                    break;
                }

                current = nodes.FirstOrDefault(n => n.NodeId == nextId);
                await Task.Delay(300, ct); // Nhịp thở giữa các node
            }
        }

        // --- Click Node ---
        private async Task<string?> ExecuteClickAsync(ActionNode node)
        {
            if (string.IsNullOrEmpty(node.TargetImagePath))
            {
                Log("  ⚠ Click node không có ảnh mẫu. Bỏ qua.");
                return node.NextNodeOnFail;
            }

            var pos = await Task.Run(() => _matcher.FindTemplateWithOffset(
                node.TargetImagePath, node.MatchThreshold, node.ClickOffsetX, node.ClickOffsetY));
            if (pos != null)
            {
                Log($"  🖱 Tìm thấy ảnh, click tại ({pos.Value.X}, {pos.Value.Y}) [offset {node.ClickOffsetX:P0},{node.ClickOffsetY:P0}]");
                _input.Click(pos.Value.X, pos.Value.Y);
                await Task.Delay(500);
                return node.NextNodeOnSuccess;
            }
            else
            {
                Log($"  ❌ Không tìm thấy ảnh mẫu (ngưỡng {node.MatchThreshold:P0}). Sang nhánh Fail.");
                return node.NextNodeOnFail;
            }
        }

        // --- Type Node ---
        private async Task<string?> ExecuteTypeAsync(ActionNode node)
        {
            // Focus bằng cách click vào vị trí tìm thấy (nếu có ảnh)
            if (!string.IsNullOrEmpty(node.TargetImagePath))
            {
                var pos = await Task.Run(() => _matcher.FindTemplateWithOffset(
                    node.TargetImagePath, node.MatchThreshold, node.ClickOffsetX, node.ClickOffsetY));
                if (pos != null)
                {
                    _input.Click(pos.Value.X, pos.Value.Y);
                    await Task.Delay(300);
                }
            }

            if (!string.IsNullOrEmpty(node.InputValue))
            {
                Log($"  ⌨ Gõ: \"{node.InputValue}\"");
                _input.Type(node.InputValue);
            }
            return node.NextNodeOnSuccess;
        }

        // --- Branch Node (switch-case) ---
        private async Task<string?> ExecuteBranchAsync(ActionNode node)
        {
            if (node.BranchRules == null || node.BranchRules.Count == 0)
            {
                Log("  ⚠ Branch node không có case nào.");
                return node.NextNodeOnSuccess; // fallback
            }

            // Sắp xếp theo Priority, Default case (null image) ở cuối
            var ordered = node.BranchRules
                .OrderBy(r => r.TargetImagePath == null ? int.MaxValue : r.Priority)
                .ToList();

            foreach (var rule in ordered)
            {
                // Default case
                if (rule.TargetImagePath == null)
                {
                    Log($"  🔀 Case '{rule.Label}' (Default) → {rule.NextNodeId?[..8]}...");
                    return rule.NextNodeId;
                }

                // Kiểm tra ảnh
                var pos = await Task.Run(() => _matcher.FindTemplate(rule.TargetImagePath, node.MatchThreshold));
                if (pos != null)
                {
                    Log($"  🔀 Case '{rule.Label}' khớp (score≥{node.MatchThreshold:P0}) → {rule.NextNodeId?[..8]}...");
                    return rule.NextNodeId;
                }
            }

            Log("  ⚠ Không có case nào khớp và không có Default.");
            return null;
        }

        private void Log(string msg) => OnLog?.Invoke(msg);
    }
}
