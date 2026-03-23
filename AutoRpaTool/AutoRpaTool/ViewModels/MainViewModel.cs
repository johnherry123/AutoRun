using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;
using Application = System.Windows.Application;
using WindowState = System.Windows.WindowState;
using AutoRpaTool.Models;
using AutoRpaTool.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace AutoRpaTool.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly AppDbContext _db;
        private readonly ImageCaptureService _captureService;
        private readonly ImageMatchingService _matchingService;
        private readonly InputService _inputService;
        private readonly RpaEngine _engine;

        public ObservableCollection<Scenario> Scenarios { get; } = new();
        public ObservableCollection<ActionNode> CurrentNodes { get; } = new();
        public ObservableCollection<UIConnection> CurrentConnections { get; } = new();

        [ObservableProperty]
        private Scenario? _selectedScenario;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNodeSelected))]
        [NotifyPropertyChangedFor(nameof(IsBranchNode))]
        [NotifyPropertyChangedFor(nameof(OtherNodes))]
        private ActionNode? _selectedNode;

        public bool IsNodeSelected => SelectedNode != null;
        public bool IsBranchNode => SelectedNode?.NodeType == NodeType.Branch;

        // Danh sach node khac (dung cho ComboBox ket noi)
        public IEnumerable<ActionNode> OtherNodes =>
            CurrentNodes.Where(n => n != SelectedNode);

        // Danh sach tat ca node (dung cho ComboBox branch case)
        public IEnumerable<ActionNode> AllNodes => CurrentNodes;

        // Nodify pending wire (giu lai phong khi drag hoat dong)
        [ObservableProperty]
        private object? _pendingWireSource;

        [ObservableProperty]
        private string _logText = "San sang.\n";

        private CancellationTokenSource? _runCts;

        [ObservableProperty]
        private bool _isRunning;

        // ===================== CONSTRUCTOR =====================
        public MainViewModel()
        {
            _captureService = new ImageCaptureService();
            _matchingService = new ImageMatchingService(_captureService);
            _inputService = new InputService();
            _engine = new RpaEngine(_matchingService, _inputService);
            _engine.OnLog += msg => LogText += msg + "\n";

            _db = new AppDbContext();
            _db.Database.EnsureCreated();
            LoadScenarios();
        }

        // ===================== SCENARIO =====================
        partial void OnSelectedScenarioChanged(Scenario? value) => LoadNodesForScenario();

        private void LoadScenarios()
        {
            Scenarios.Clear();
            foreach (var s in _db.Scenarios.ToList()) Scenarios.Add(s);
        }

        private void LoadNodesForScenario()
        {
            CurrentNodes.Clear();
            CurrentConnections.Clear();
            SelectedNode = null;
            if (SelectedScenario == null) return;

            var nodes = _db.ActionNodes
                .Include(n => n.BranchRules)
                .Where(n => n.ScenarioId == SelectedScenario.Id)
                .ToList();

            foreach (var node in nodes) CurrentNodes.Add(node);

            foreach (var node in nodes)
            {
                if (!string.IsNullOrEmpty(node.NextNodeOnSuccess))
                {
                    var t = nodes.FirstOrDefault(n => n.NodeId == node.NextNodeOnSuccess);
                    if (t != null) CurrentConnections.Add(new UIConnection { Source = node, Target = t, Label = "OK" });
                }
                if (!string.IsNullOrEmpty(node.NextNodeOnFail))
                {
                    var t = nodes.FirstOrDefault(n => n.NodeId == node.NextNodeOnFail);
                    if (t != null) CurrentConnections.Add(new UIConnection { Source = node, Target = t, Label = "FAIL" });
                }
                foreach (var rule in node.BranchRules)
                {
                    var t = nodes.FirstOrDefault(n => n.NodeId == rule.NextNodeId);
                    if (t != null) CurrentConnections.Add(new UIConnection { Source = node, Target = t, Label = rule.Label });
                }
            }
        }

        [RelayCommand]
        private void CreateScenario()
        {
            var s = new Scenario { Name = "Kich ban " + DateTime.Now.ToString("HH:mm:ss") };
            _db.Scenarios.Add(s);
            _db.SaveChanges();
            LoadScenarios();
            SelectedScenario = Scenarios.Last();
        }

        [RelayCommand]
        private void DeleteScenario()
        {
            if (SelectedScenario == null) return;
            if (MessageBox.Show($"Xoa kich ban '{SelectedScenario.Name}'?", "Xac nhan", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
            _db.Scenarios.Remove(SelectedScenario);
            _db.SaveChanges();
            LoadScenarios();
        }

        [RelayCommand]
        private void SaveScenario()
        {
            try { _db.SaveChanges(); AppendLog("Da luu!"); }
            catch (Exception ex) { MessageBox.Show($"Loi luu: {ex.Message}", "Loi"); }
        }

        // ===================== NODE =====================
        [RelayCommand] private void AddClickNode() => AddNode(NodeType.Click);
        [RelayCommand] private void AddTypeNode() => AddNode(NodeType.Type);
        [RelayCommand] private void AddWaitNode() => AddNode(NodeType.Wait);
        [RelayCommand] private void AddBranchNode() => AddNode(NodeType.Branch);

        private void AddNode(NodeType type)
        {
            if (SelectedScenario == null) { MessageBox.Show("Hay chon kich ban truoc!"); return; }
            var node = new ActionNode
            {
                NodeId = Guid.NewGuid().ToString(),
                ScenarioId = SelectedScenario.Id,
                NodeType = type,
                LocationX = 200 + CurrentNodes.Count * 30,
                LocationY = 200 + CurrentNodes.Count * 20,
                IsStartNode = CurrentNodes.Count == 0 // Node dầu tiên tự động là Start
            };
            _db.ActionNodes.Add(node);
            _db.SaveChanges();
            CurrentNodes.Add(node);
            OnPropertyChanged(nameof(OtherNodes));
            OnPropertyChanged(nameof(AllNodes));
        }

        [RelayCommand]
        private void DeleteNode()
        {
            if (SelectedNode == null) return;
            var deletedId = SelectedNode.NodeId;

            // Xoa connection lien quan (truoc khi xoa node khoi collection)
            var toRemove = CurrentConnections.Where(c => c.Source == SelectedNode || c.Target == SelectedNode).ToList();
            foreach (var c in toRemove) CurrentConnections.Remove(c);

            // Don dep reference trong cac node khac
            foreach (var node in CurrentNodes)
            {
                if (node.NextNodeOnSuccess == deletedId)
                    node.NextNodeOnSuccess = null;
                if (node.NextNodeOnFail == deletedId)
                    node.NextNodeOnFail = null;
                foreach (var rule in node.BranchRules)
                {
                    if (rule.NextNodeId == deletedId)
                        rule.NextNodeId = null;
                }
            }

            _db.ActionNodes.Remove(SelectedNode);
            _db.SaveChanges();
            CurrentNodes.Remove(SelectedNode);
            SelectedNode = null;
            OnPropertyChanged(nameof(OtherNodes));
            OnPropertyChanged(nameof(AllNodes));
        }

        // ===================== IMAGE CAPTURE =====================
        [RelayCommand]
        private async Task CaptureImageForNode(ActionNode? node)
        {
            if (node == null) return;
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
            await Task.Delay(800);
            var selector = new RegionSelectorWindow();
            if (selector.ShowDialog() == true)
            {
                var rect = selector.SelectedRect;
                string path = _captureService.CaptureRegion((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
                node.TargetImagePath = path;

                // Mở cửa sổ đánh dấu điểm click
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                var marker = new ClickMarkerWindow(path);
                if (marker.ShowDialog() == true)
                {
                    node.ClickOffsetX = marker.OffsetX;
                    node.ClickOffsetY = marker.OffsetY;
                }

                _db.SaveChanges();
                AppendLog($"Chup anh: {System.IO.Path.GetFileName(path)} | Offset: {node.ClickOffsetX:P0},{node.ClickOffsetY:P0}");
                OnPropertyChanged(nameof(SelectedNode));
            }
            else
            {
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            }
        }

        // ===================== CONNECTION VIA DROPDOWN =====================

        /// <summary>Ket noi SelectedNode toi node dich (nhanh Success) - goi tu ComboBox</summary>
        [RelayCommand]
        private void ConnectSuccessTo(ActionNode? target)
        {
            if (SelectedNode == null || target == null || SelectedNode == target) return;

            // Xoa connection cu
            var old = CurrentConnections.FirstOrDefault(c => c.Source == SelectedNode && c.Label == "OK");
            if (old != null) CurrentConnections.Remove(old);

            SelectedNode.NextNodeOnSuccess = target.NodeId;
            _db.SaveChanges();
            CurrentConnections.Add(new UIConnection { Source = SelectedNode, Target = target, Label = "OK" });
            AppendLog($"Noi OK: {SelectedNode.DisplayLabel} -> {target.DisplayLabel}");
        }

        /// <summary>Ket noi SelectedNode toi node dich (nhanh Fail)</summary>
        [RelayCommand]
        private void ConnectFailTo(ActionNode? target)
        {
            if (SelectedNode == null || target == null || SelectedNode == target) return;

            var old = CurrentConnections.FirstOrDefault(c => c.Source == SelectedNode && c.Label == "FAIL");
            if (old != null) CurrentConnections.Remove(old);

            SelectedNode.NextNodeOnFail = target.NodeId;
            _db.SaveChanges();
            CurrentConnections.Add(new UIConnection { Source = SelectedNode, Target = target, Label = "FAIL" });
            AppendLog($"Noi FAIL: {SelectedNode.DisplayLabel} -> {target.DisplayLabel}");
        }

        /// <summary>Xoa toan bo ket noi tu node dang chon</summary>
        [RelayCommand]
        private void DisconnectNode()
        {
            if (SelectedNode == null) return;
            var toRemove = CurrentConnections.Where(c => c.Source == SelectedNode).ToList();
            foreach (var c in toRemove) CurrentConnections.Remove(c);
            SelectedNode.NextNodeOnSuccess = null;
            SelectedNode.NextNodeOnFail = null;
            _db.SaveChanges();
            AppendLog($"Da xoa ket noi cua {SelectedNode.NodeType}");
        }

        // Nodify drag fallback
        [RelayCommand]
        private void CreateWire(object? target)
        {
            var source = PendingWireSource as ActionNode;
            PendingWireSource = null;
            if (source == null || target is not ActionNode t || source == t) return;
            ConnectSuccessTo(t);
            SelectedNode = source; // restore
        }

        // ===================== BRANCH RULES =====================
        [RelayCommand]
        private void AddBranchCase()
        {
            if (SelectedNode?.NodeType != NodeType.Branch) return;
            var rule = new BranchRule
            {
                NodeId = SelectedNode.NodeId,
                Label = $"Case {SelectedNode.BranchRules.Count + 1}",
                Priority = SelectedNode.BranchRules.Count
            };
            SelectedNode.BranchRules.Add(rule);
            _db.SaveChanges();
        }

        [RelayCommand]
        private void DeleteBranchCase(BranchRule? rule)
        {
            if (rule == null || SelectedNode == null) return;
     
            var oldConn = CurrentConnections.FirstOrDefault(c => c.Source == SelectedNode && c.Label == rule.Label);
            if (oldConn != null) CurrentConnections.Remove(oldConn);
            
            _db.BranchRules.Remove(rule);
            _db.SaveChanges();
            SelectedNode.BranchRules.Remove(rule);
        }

        /// <summary>Ket noi branch case toi node dich qua ComboBox</summary>
        [RelayCommand]
        private void ConnectBranchCaseTo(object? param)
        {
            // param la Tuple(BranchRule, ActionNode) tu XAML MultiBinding
            if (param is not object[] args || args.Length < 2) return;
            if (args[0] is not BranchRule rule || args[1] is not ActionNode target) return;
            if (SelectedNode == null) return;

            // Xoa connection cu cua case nay
            var old = CurrentConnections.FirstOrDefault(c => c.Source == SelectedNode && c.Label == rule.Label);
            if (old != null) CurrentConnections.Remove(old);

            rule.NextNodeId = target.NodeId;
            _db.SaveChanges();
            CurrentConnections.Add(new UIConnection { Source = SelectedNode, Target = target, Label = rule.Label });
            AppendLog($"Noi case '{rule.Label}': -> {target.DisplayLabel}");
        }

        [RelayCommand]
        private async Task CaptureImageForBranchCase(BranchRule? rule)
        {
            if (rule == null) return;
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
            await Task.Delay(800);
            var selector = new RegionSelectorWindow();
            if (selector.ShowDialog() == true)
            {
                var rect = selector.SelectedRect;
                string path = _captureService.CaptureRegion((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
                rule.TargetImagePath = path;
                _db.SaveChanges();
                Application.Current.MainWindow.WindowState = WindowState.Normal;
                AppendLog($"Chup anh case '{rule.Label}': {System.IO.Path.GetFileName(path)}");
            }
            else
            {
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            }
        }

        
        [RelayCommand]
        private async Task RunScenario()
        {
            if (SelectedScenario == null) { MessageBox.Show("Hay chon kich ban!"); return; }
            if (IsRunning) { MessageBox.Show("Dang co kich ban chay!"); return; }
            _db.SaveChanges();
            var nodes = _db.ActionNodes.Include(n => n.BranchRules)
                .Where(n => n.ScenarioId == SelectedScenario.Id).ToList();
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
            await Task.Delay(1000);
            IsRunning = true;
            _runCts = new CancellationTokenSource();
            LogText = string.Empty;
            try { await _engine.RunAsync(SelectedScenario, nodes, _runCts.Token); }
            catch (TaskCanceledException) { AppendLog("Da dung."); }
            catch (Exception ex) { AppendLog($"Loi: {ex.Message}"); }
            finally
            {
                IsRunning = false;
                Application.Current.MainWindow.WindowState = WindowState.Normal;
            }
        }

        [RelayCommand]
        private void StopScenario() => _runCts?.Cancel();

        private void AppendLog(string msg) => LogText += msg + "\n";

        public void LogConnectStatus(string msg) => AppendLog(msg);
    }

    public class UIConnection
    {
        public ActionNode Source { get; set; } = null!;
        public ActionNode Target { get; set; } = null!;
        public string Label { get; set; } = string.Empty;
    }
}
