
using Attendance.Classes;
using Attendance.Animation;
using System.Collections.ObjectModel;
using System.Windows;


namespace Attendance.View
{
    /// <summary>
    /// RollCallSettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class RollCallSettingsWindow : Window
    {

        public RollCallSettingsWindow(ObservableCollection<Cla> classes)
        {
            InitializeComponent();
            DataContext = new RollCallViewModel(classes);
        }

        private async void StartRollCall_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as RollCallViewModel;
            if (vm == null) return;
            if (vm.SelectedCount == 1) { 
                var animator = new SingleRollCallAnimator(
                    OriginalScrollviewer,          // 原始 ScrollViewer
                    OneScrolling,              // 横向 ScrollViewer
                    OriginalItemsControl,          // 原始 ItemsControl
                    OneItemsControl,        // 横向 ItemsControl
                    vm,                           // ViewModel
                    SettingsPanel,                 // 设置区域
                    OverlayCanvas // ✅ 传入浮层容器
                );
                await animator.StartAsync();
            }
            if (vm.SelectedCount == 2)
            {
                var animator = new DoubleRollCallAnimator(
                    vm,
                    OriginalScrollviewer,
                    OriginalItemsControl,
                    LeftItemsControl,
                    RightItemsControl,
                    LeftScrollViewer,
                    RightScrollViewer,
                    SettingsPanel,                 // 设置区域
                    OverlayCanvas // ✅ 传入浮层容器
                    );
                await animator.StartAsync();
            }
            if (vm.SelectedCount == 3)
            {
                var animator = new TripleRollCallAnimator(
                    vm,
                    OriginalScrollviewer,
                    TopLeftScrollViewer,
                    TopRightScrollViewer,
                    BottomScrollViewer,
                    OriginalItemsControl,
                    TopLeftItemsControl,
                    TopRightItemsControl,
                    BottomItemsControl,
                    SettingsPanel,                 // 设置区域
                    OverlayCanvas // ✅ 传入浮层容器
                    );
                await animator.StartAsync();
            }
            if (vm.SelectedCount >= 4 && vm.SelectedCount <= 10) 
            { 
                var animator = new NormalRollCallAnimator(
                        vm,
                        OriginalItemsControl,
                        OriginalScrollviewer,
                        SettingsPanel,
                        OverlayCanvas,
                        selectionCount: vm.SelectedCount); // 用户指定抽取人数
                await animator.StartAsync();
            }
        }
    }
}
